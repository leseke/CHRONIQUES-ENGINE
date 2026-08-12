namespace Chroniques.Simulation.Autonomy;

using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;
using Chroniques.Simulation.Systems;

/// <summary>
/// ENGINE-019 : création et évolution générationnelle génériques de la Mémoire du Monde.
/// Les règles injectées qualifient les faits ; le System applique uniquement les transitions GDB-002B.
/// </summary>
public sealed class WorldMemoryEvolutionSystem : ISystem
{
    private readonly IReadOnlyList<IWorldMemoryRule> _rules;
    private readonly IReadOnlyDictionary<string, IWorldMemoryRule> _rulesByType;
    private readonly IWorldMemoryGenerationResolver _generationResolver;

    public WorldMemoryEvolutionSystem(
        IEnumerable<IWorldMemoryRule> rules,
        IWorldMemoryGenerationResolver generationResolver)
    {
        ArgumentNullException.ThrowIfNull(rules);

        var materialized = rules.ToArray();
        if (materialized.Any(rule => rule is null))
        {
            throw new ArgumentException(
                "La collection de règles de mémoire ne peut contenir de valeur null.",
                nameof(rules));
        }

        if (materialized.Any(rule => string.IsNullOrWhiteSpace(rule.MemoryTypeId)))
        {
            throw new ArgumentException(
                "Chaque règle de mémoire doit posséder un MemoryTypeId non vide.",
                nameof(rules));
        }

        var duplicate = materialized
            .GroupBy(rule => rule.MemoryTypeId, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
        {
            throw new ArgumentException(
                $"Plusieurs règles de mémoire portent le MemoryTypeId \"{duplicate.Key}\".",
                nameof(rules));
        }

        _rules = materialized;
        _rulesByType = materialized.ToDictionary(
            rule => rule.MemoryTypeId,
            StringComparer.Ordinal);
        _generationResolver = generationResolver
            ?? throw new ArgumentNullException(nameof(generationResolver));
    }

    public void Update(World world, Tick currentTick)
    {
        ArgumentNullException.ThrowIfNull(world);

        var currentGeneration = _generationResolver.ResolveGeneration(world, currentTick);
        if (currentGeneration < 0)
        {
            throw new InvalidOperationException(
                "Le marqueur de génération de Mémoire du Monde doit être non négatif.");
        }

        var memories = world.Entities
            .Select(entity =>
            {
                entity.TryGet<WorldMemoryComponent>(out var memory);
                return (Entity: entity, Memory: memory);
            })
            .Where(item => item.Memory is not null)
            .Select(item => (item.Entity, Memory: item.Memory!))
            .ToList();

        ValidateExistingMemories(memories, currentGeneration);

        var identities = new HashSet<string>(
            memories.Select(item => Identity(item.Memory.MemoryTypeId, item.Memory.MemoryKey)),
            StringComparer.Ordinal);

        foreach (var rule in _rules)
        {
            var candidates = rule.FindCreationCandidates(world, currentTick, currentGeneration)
                ?? throw new InvalidOperationException(
                    $"La règle de mémoire \"{rule.MemoryTypeId}\" a retourné une collection de candidates null.");

            foreach (var candidate in candidates)
            {
                ValidateCreationCandidate(rule, candidate);

                var identity = Identity(candidate.MemoryTypeId, candidate.MemoryKey);
                if (!identities.Add(identity))
                {
                    continue;
                }

                var entity = world.Spawn();
                var component = new WorldMemoryComponent
                {
                    MemoryTypeId = candidate.MemoryTypeId,
                    MemoryKey = candidate.MemoryKey,
                    Payload = candidate.Payload,
                    Tier = WorldMemoryTier.Anecdote,
                    IsForgotten = false,
                    SourceRefs = candidate.SourceRefs.ToList(),
                    CreatedGeneration = currentGeneration,
                    LastEvaluatedGeneration = currentGeneration,
                };

                entity.Set(component);
                memories.Add((entity, component));
            }
        }

        foreach (var (entity, memory) in memories)
        {
            if (memory.IsForgotten)
            {
                continue;
            }

            if (!_rulesByType.TryGetValue(memory.MemoryTypeId, out var rule))
            {
                continue;
            }

            EvaluateMissingGenerations(entity, memory, rule, world, currentGeneration);
        }
    }

    private static void EvaluateMissingGenerations(
        Entity entity,
        WorldMemoryComponent memory,
        IWorldMemoryRule rule,
        World world,
        long currentGeneration)
    {
        var generation = memory.LastEvaluatedGeneration;

        while (!memory.IsForgotten && generation < currentGeneration)
        {
            generation++;

            var evidence = rule.EvaluateGeneration(entity, memory, world, generation)
                ?? throw new InvalidOperationException(
                    $"La règle de mémoire \"{rule.MemoryTypeId}\" a retourné une preuve null.");

            ValidateEvidence(memory.Tier, evidence);
            MergeSources(memory.SourceRefs, evidence.SourceRefs);
            ApplyGeneration(memory, evidence, generation);
            memory.LastEvaluatedGeneration = generation;
        }
    }

    private static void ApplyGeneration(
        WorldMemoryComponent memory,
        WorldMemoryGenerationEvidence evidence,
        long generation)
    {
        var previousTier = memory.Tier;
        var becameForgotten = false;

        switch (memory.Tier)
        {
            case WorldMemoryTier.Anecdote:
                if (evidence.HasLinkOrTransmission)
                {
                    memory.Tier = WorldMemoryTier.Memory;
                    ResetCounters(memory);
                }
                else
                {
                    memory.IsForgotten = true;
                    becameForgotten = true;
                }
                break;

            case WorldMemoryTier.Memory:
                if (evidence.HasRegionalInfluence)
                {
                    memory.Tier = WorldMemoryTier.Legend;
                    ResetCounters(memory);
                }
                else if (evidence.HasLinkOrTransmission || evidence.HasQualifyingReference)
                {
                    memory.ConsecutiveUnreferencedGenerations = 0;
                    memory.ConsecutiveReferencedGenerations++;

                    if (memory.ConsecutiveReferencedGenerations >= 2)
                    {
                        memory.Tier = WorldMemoryTier.Legend;
                        ResetCounters(memory);
                    }
                }
                else
                {
                    memory.ConsecutiveReferencedGenerations = 0;
                    memory.ConsecutiveUnreferencedGenerations++;

                    if (memory.ConsecutiveUnreferencedGenerations >= 2)
                    {
                        memory.IsForgotten = true;
                        becameForgotten = true;
                    }
                }
                break;

            case WorldMemoryTier.Legend:
                if (evidence.HasQualifyingPractice)
                {
                    memory.Tier = WorldMemoryTier.Tradition;
                }
                else if (evidence.HasEqualOrGreaterContradiction)
                {
                    memory.Tier = WorldMemoryTier.Memory;
                    ResetCounters(memory);
                }
                break;

            case WorldMemoryTier.Tradition:
                if (!evidence.HasQualifyingPractice)
                {
                    memory.Tier = WorldMemoryTier.Legend;
                }
                break;

            default:
                throw new InvalidOperationException(
                    $"Palier de Mémoire du Monde inconnu : {memory.Tier}.");
        }

        if (memory.Tier != previousTier || becameForgotten)
        {
            memory.Transitions.Add(new WorldMemoryTransitionTrace(
                generation,
                previousTier,
                memory.Tier,
                becameForgotten,
                evidence.SourceRefs.ToArray()));
        }
    }

    private static void ValidateExistingMemories(
        IReadOnlyList<(Entity Entity, WorldMemoryComponent Memory)> memories,
        long currentGeneration)
    {
        var duplicate = memories
            .GroupBy(
                item => Identity(item.Memory.MemoryTypeId, item.Memory.MemoryKey),
                StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
        {
            throw new InvalidOperationException(
                $"Plusieurs éléments de Mémoire du Monde possèdent la même identité \"{duplicate.Key}\".");
        }

        foreach (var (_, memory) in memories)
        {
            ValidateComponent(memory);

            if (currentGeneration < memory.LastEvaluatedGeneration
                || currentGeneration < memory.CreatedGeneration)
            {
                throw new InvalidOperationException(
                    $"Le marqueur de génération courant ne peut précéder l'historique de la mémoire \"{memory.MemoryTypeId}/{memory.MemoryKey}\".");
            }
        }
    }

    private static void ValidateComponent(WorldMemoryComponent memory)
    {
        if (string.IsNullOrWhiteSpace(memory.MemoryTypeId)
            || string.IsNullOrWhiteSpace(memory.MemoryKey))
        {
            throw new InvalidOperationException(
                "Une Mémoire du Monde persistée exige MemoryTypeId et MemoryKey non vides.");
        }

        if (memory.Payload is null)
        {
            throw new InvalidOperationException("Le Payload d'une Mémoire du Monde ne peut pas être null.");
        }

        ValidateSourceRefs(memory.SourceRefs, requireAtLeastOne: true, "mémoire persistée");

        if (memory.CreatedGeneration < 0
            || memory.LastEvaluatedGeneration < memory.CreatedGeneration)
        {
            throw new InvalidOperationException(
                "Les marqueurs générationnels persistés d'une Mémoire du Monde sont invalides.");
        }

        if (!Enum.IsDefined(memory.Tier))
        {
            throw new InvalidOperationException(
                $"Palier de Mémoire du Monde inconnu : {memory.Tier}.");
        }

        if (memory.ConsecutiveReferencedGenerations < 0
            || memory.ConsecutiveUnreferencedGenerations < 0
            || (memory.ConsecutiveReferencedGenerations > 0
                && memory.ConsecutiveUnreferencedGenerations > 0))
        {
            throw new InvalidOperationException(
                "Les compteurs générationnels d'une Mémoire du Monde sont invalides.");
        }

        if (memory.Tier != WorldMemoryTier.Memory
            && (memory.ConsecutiveReferencedGenerations != 0
                || memory.ConsecutiveUnreferencedGenerations != 0))
        {
            throw new InvalidOperationException(
                "Les compteurs de référence ne sont autorisés qu'au palier Souvenir/Memory.");
        }

        long previousGeneration = memory.CreatedGeneration;
        var forgottenSeen = false;

        foreach (var trace in memory.Transitions)
        {
            if (trace.Generation <= previousGeneration
                || trace.Generation > memory.LastEvaluatedGeneration)
            {
                throw new InvalidOperationException(
                    "L'historique des transitions de Mémoire du Monde n'est pas chronologique.");
            }

            if (!Enum.IsDefined(trace.PreviousTier) || !Enum.IsDefined(trace.NewTier))
            {
                throw new InvalidOperationException(
                    "Une trace de Mémoire du Monde contient un palier inconnu.");
            }

            ValidateTransitionShape(trace);
            ValidateSourceRefs(trace.EvidenceSourceRefs, requireAtLeastOne: false, "trace de transition");

            if (forgottenSeen)
            {
                throw new InvalidOperationException(
                    "Une Mémoire du Monde oubliée ne peut posséder de transition ultérieure.");
            }

            forgottenSeen = trace.BecameForgotten;
            previousGeneration = trace.Generation;
        }

        if (forgottenSeen != memory.IsForgotten)
        {
            throw new InvalidOperationException(
                "L'état d'oubli d'une Mémoire du Monde ne concorde pas avec son historique de transitions.");
        }
    }

    private static void ValidateTransitionShape(WorldMemoryTransitionTrace trace)
    {
        var valid = trace switch
        {
            { PreviousTier: WorldMemoryTier.Anecdote, NewTier: WorldMemoryTier.Memory, BecameForgotten: false } => true,
            { PreviousTier: WorldMemoryTier.Anecdote, NewTier: WorldMemoryTier.Anecdote, BecameForgotten: true } => true,
            { PreviousTier: WorldMemoryTier.Memory, NewTier: WorldMemoryTier.Legend, BecameForgotten: false } => true,
            { PreviousTier: WorldMemoryTier.Memory, NewTier: WorldMemoryTier.Memory, BecameForgotten: true } => true,
            { PreviousTier: WorldMemoryTier.Legend, NewTier: WorldMemoryTier.Tradition, BecameForgotten: false } => true,
            { PreviousTier: WorldMemoryTier.Legend, NewTier: WorldMemoryTier.Memory, BecameForgotten: false } => true,
            { PreviousTier: WorldMemoryTier.Tradition, NewTier: WorldMemoryTier.Legend, BecameForgotten: false } => true,
            _ => false,
        };

        if (!valid)
        {
            throw new InvalidOperationException(
                "Une trace contient une transition de palier interdite par GDB-002B.");
        }
    }

    private static void ValidateCreationCandidate(
        IWorldMemoryRule rule,
        WorldMemoryCreationCandidate candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate.MemoryTypeId)
            || string.IsNullOrWhiteSpace(candidate.MemoryKey))
        {
            throw new InvalidOperationException(
                "Une candidate de Mémoire du Monde exige MemoryTypeId et MemoryKey non vides.");
        }

        if (!string.Equals(rule.MemoryTypeId, candidate.MemoryTypeId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"La règle \"{rule.MemoryTypeId}\" ne peut pas créer le Type \"{candidate.MemoryTypeId}\".");
        }

        if (candidate.Payload is null)
        {
            throw new InvalidOperationException(
                "Le Payload d'une candidate de Mémoire du Monde ne peut pas être null.");
        }

        ValidateSourceRefs(candidate.SourceRefs, requireAtLeastOne: true, "candidate de mémoire");
    }

    private static void ValidateEvidence(
        WorldMemoryTier tier,
        WorldMemoryGenerationEvidence evidence)
    {
        var hasPositiveEvidence = evidence.HasLinkOrTransmission
            || evidence.HasQualifyingReference
            || evidence.HasRegionalInfluence
            || evidence.HasEqualOrGreaterContradiction
            || evidence.HasQualifyingPractice;

        ValidateSourceRefs(
            evidence.SourceRefs,
            requireAtLeastOne: hasPositiveEvidence,
            "preuve générationnelle");

        var invalidForTier = tier switch
        {
            WorldMemoryTier.Anecdote => evidence.HasQualifyingReference
                || evidence.HasRegionalInfluence
                || evidence.HasEqualOrGreaterContradiction
                || evidence.HasQualifyingPractice,

            WorldMemoryTier.Memory => evidence.HasEqualOrGreaterContradiction
                || evidence.HasQualifyingPractice,

            WorldMemoryTier.Legend => evidence.HasLinkOrTransmission
                || evidence.HasQualifyingReference
                || evidence.HasRegionalInfluence
                || (evidence.HasQualifyingPractice && evidence.HasEqualOrGreaterContradiction),

            WorldMemoryTier.Tradition => evidence.HasLinkOrTransmission
                || evidence.HasQualifyingReference
                || evidence.HasRegionalInfluence
                || evidence.HasEqualOrGreaterContradiction,

            _ => true,
        };

        if (invalidForTier)
        {
            throw new InvalidOperationException(
                $"La combinaison de preuves fournie est incompatible avec le palier {tier}.");
        }
    }

    private static void ValidateSourceRefs(
        IReadOnlyList<string>? sourceRefs,
        bool requireAtLeastOne,
        string context)
    {
        if (sourceRefs is null)
        {
            throw new InvalidOperationException(
                $"Les SourceRefs d'une {context} ne peuvent pas être null.");
        }

        if (requireAtLeastOne && sourceRefs.Count == 0)
        {
            throw new InvalidOperationException(
                $"Une {context} exige au moins une SourceRef stable.");
        }

        if (sourceRefs.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException(
                $"Une {context} contient une SourceRef vide.");
        }

        if (sourceRefs.Distinct(StringComparer.Ordinal).Count() != sourceRefs.Count)
        {
            throw new InvalidOperationException(
                $"Une {context} contient des SourceRefs dupliquées.");
        }
    }

    private static void MergeSources(List<string> target, IReadOnlyList<string> sources)
    {
        foreach (var source in sources)
        {
            if (!target.Contains(source, StringComparer.Ordinal))
            {
                target.Add(source);
            }
        }
    }

    private static void ResetCounters(WorldMemoryComponent memory)
    {
        memory.ConsecutiveReferencedGenerations = 0;
        memory.ConsecutiveUnreferencedGenerations = 0;
    }

    private static string Identity(string typeId, string memoryKey)
        => $"{typeId}\u001f{memoryKey}";
}
