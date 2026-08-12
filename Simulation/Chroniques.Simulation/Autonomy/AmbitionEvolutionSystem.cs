namespace Chroniques.Simulation.Autonomy;

using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;
using Chroniques.Simulation.Systems;

/// <summary>
/// Crée et évalue les Ambitions génériques à partir des règles injectées.
///
/// Le System ne connaît aucun Type métier concret et n'avance jamais le Tick.
/// </summary>
public sealed class AmbitionEvolutionSystem : ISystem
{
    private readonly IReadOnlyList<IAmbitionRule> _rules;
    private readonly IReadOnlyDictionary<string, IAmbitionRule> _rulesByType;

    public AmbitionEvolutionSystem(IEnumerable<IAmbitionRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);

        var materialized = rules.ToArray();
        if (materialized.Any(rule => rule is null))
        {
            throw new ArgumentException(
                "La collection de règles d'Ambition ne peut contenir de valeur null.",
                nameof(rules));
        }

        if (materialized.Any(rule => string.IsNullOrWhiteSpace(rule.AmbitionTypeId)))
        {
            throw new ArgumentException(
                "Chaque règle d'Ambition doit posséder un AmbitionTypeId non vide.",
                nameof(rules));
        }

        var duplicate = materialized
            .GroupBy(rule => rule.AmbitionTypeId, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
        {
            throw new ArgumentException(
                $"Le Type d'Ambition \"{duplicate.Key}\" est enregistré plusieurs fois.",
                nameof(rules));
        }

        _rules = materialized;
        _rulesByType = materialized.ToDictionary(
            rule => rule.AmbitionTypeId,
            StringComparer.Ordinal);
    }

    public void Update(World world, Tick currentTick)
    {
        ArgumentNullException.ThrowIfNull(world);

        foreach (var actor in world.Entities)
        {
            CreateCandidates(actor, world, currentTick);
            EvaluateExisting(actor, world, currentTick);
        }
    }

    private void CreateCandidates(
        Entity actor,
        World world,
        Tick currentTick)
    {
        foreach (var rule in _rules)
        {
            var candidates = rule.FindCreationCandidates(actor, world, currentTick)
                ?? throw new InvalidOperationException(
                    $"La règle d'Ambition \"{rule.AmbitionTypeId}\" a retourné une collection de candidates null.");

            foreach (var candidate in candidates)
            {
                ValidateCandidate(rule, candidate);

                if (!actor.TryGet<AmbitionComponent>(out var component))
                {
                    component = new AmbitionComponent();
                    actor.Set(component);
                }

                if (component.Ambitions.Any(existing => SameIdentity(existing, candidate)))
                {
                    continue;
                }

                component.Ambitions.Add(
                    new AmbitionState(
                        candidate.AmbitionTypeId,
                        candidate.InstanceKey,
                        candidate.ObjectivePayload,
                        candidate.IntentObjective,
                        candidate.InitialIntensity,
                        Progress: 0d,
                        IsAbandoned: false,
                        CreatedAt: currentTick));
            }
        }
    }

    private void EvaluateExisting(
        Entity actor,
        World world,
        Tick currentTick)
    {
        if (!actor.TryGet<AmbitionComponent>(out var component))
        {
            return;
        }

        for (var index = component.Ambitions.Count - 1; index >= 0; index--)
        {
            var ambition = component.Ambitions[index];
            ValidateStoredState(ambition);

            if (ambition.Intensity <= 0)
            {
                component.Ambitions.RemoveAt(index);
                continue;
            }

            if (ambition.IsAbandoned || ambition.Progress >= 100)
            {
                continue;
            }

            if (!_rulesByType.TryGetValue(ambition.AmbitionTypeId, out var rule))
            {
                continue;
            }

            var evaluation = rule.Evaluate(
                ambition,
                actor,
                world,
                currentTick)
                ?? throw new InvalidOperationException(
                    $"La règle d'Ambition \"{rule.AmbitionTypeId}\" a retourné une évaluation null.");

            if (!double.IsFinite(evaluation.Progress))
            {
                throw new InvalidOperationException(
                    $"La règle d'Ambition \"{rule.AmbitionTypeId}\" doit produire un Progrès fini.");
            }

            component.Ambitions[index] = ambition with
            {
                Progress = Math.Clamp(evaluation.Progress, 0d, 100d),
                IsAbandoned = ambition.IsAbandoned || evaluation.ShouldAbandon,
            };
        }
    }

    private static void ValidateCandidate(
        IAmbitionRule rule,
        AmbitionCreationCandidate candidate)
    {
        if (candidate is null)
        {
            throw new InvalidOperationException(
                $"La règle d'Ambition \"{rule.AmbitionTypeId}\" ne peut retourner une candidate null.");
        }

        if (!string.Equals(
                rule.AmbitionTypeId,
                candidate.AmbitionTypeId,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"La règle \"{rule.AmbitionTypeId}\" ne peut créer une candidate du Type \"{candidate.AmbitionTypeId}\".");
        }

        if (string.IsNullOrWhiteSpace(candidate.InstanceKey)
            || string.IsNullOrWhiteSpace(candidate.IntentObjective)
            || candidate.ObjectivePayload is null)
        {
            throw new InvalidOperationException(
                $"Une candidate d'Ambition \"{rule.AmbitionTypeId}\" doit fournir InstanceKey, ObjectivePayload et IntentObjective valides.");
        }

        if (!double.IsFinite(candidate.InitialIntensity)
            || candidate.InitialIntensity <= 0
            || candidate.InitialIntensity > 100)
        {
            throw new InvalidOperationException(
                $"Une candidate d'Ambition \"{rule.AmbitionTypeId}\" exige InitialIntensity dans ]0,100].");
        }
    }

    private static void ValidateStoredState(AmbitionState ambition)
    {
        if (string.IsNullOrWhiteSpace(ambition.AmbitionTypeId)
            || string.IsNullOrWhiteSpace(ambition.InstanceKey)
            || string.IsNullOrWhiteSpace(ambition.IntentObjective)
            || ambition.ObjectivePayload is null)
        {
            throw new InvalidOperationException(
                "Une Ambition persistée possède une identité ou un Objectif invalide.");
        }

        if (!double.IsFinite(ambition.Intensity)
            || ambition.Intensity < 0
            || ambition.Intensity > 100
            || !double.IsFinite(ambition.Progress)
            || ambition.Progress < 0
            || ambition.Progress > 100)
        {
            throw new InvalidOperationException(
                "Une Ambition persistée doit conserver Intensité et Progrès dans [0,100].");
        }
    }

    private static bool SameIdentity(
        AmbitionState ambition,
        AmbitionCreationCandidate candidate)
        => string.Equals(
               ambition.AmbitionTypeId,
               candidate.AmbitionTypeId,
               StringComparison.Ordinal)
           && string.Equals(
               ambition.InstanceKey,
               candidate.InstanceKey,
               StringComparison.Ordinal);
}
