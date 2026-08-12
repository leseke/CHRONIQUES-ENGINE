namespace Chroniques.Simulation.Autonomy;

using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;
using Chroniques.Simulation.Systems;

/// <summary>
/// ENGINE-018 : création, stabilisation et Inflexions génériques des Traits.
///
/// Le System ne connaît aucun Trait métier concret et ne produit jamais
/// d'Intent. Toute cause ou création provient des règles injectées.
/// </summary>
public sealed class PersonalityEvolutionSystem : ISystem
{
    private readonly IReadOnlyList<IPersonalityTraitRule> _rules;
    private readonly IReadOnlyDictionary<string, IPersonalityTraitRule> _rulesByName;
    private readonly IPersonalityStabilizationParameterResolver _stabilizationParameters;

    public PersonalityEvolutionSystem(
        IEnumerable<IPersonalityTraitRule> rules,
        IPersonalityStabilizationParameterResolver stabilizationParameters)
    {
        ArgumentNullException.ThrowIfNull(rules);

        var materialized = rules.ToArray();
        if (materialized.Any(rule => rule is null))
        {
            throw new ArgumentException(
                "La collection de règles de personnalité ne peut contenir de valeur null.",
                nameof(rules));
        }

        if (materialized.Any(rule => string.IsNullOrWhiteSpace(rule.TraitName)))
        {
            throw new ArgumentException(
                "Chaque règle de personnalité doit posséder un TraitName non vide.",
                nameof(rules));
        }

        var duplicate = materialized
            .GroupBy(rule => rule.TraitName, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
        {
            throw new ArgumentException(
                $"Plusieurs règles de personnalité portent le TraitName \"{duplicate.Key}\".",
                nameof(rules));
        }

        _rules = materialized;
        _rulesByName = materialized.ToDictionary(
            rule => rule.TraitName,
            StringComparer.Ordinal);
        _stabilizationParameters = stabilizationParameters
            ?? throw new ArgumentNullException(nameof(stabilizationParameters));
    }

    public void Update(World world, Tick currentTick)
    {
        ArgumentNullException.ThrowIfNull(world);

        foreach (var actor in world.Entities)
        {
            ApplyToActor(actor, world, currentTick);
        }
    }

    private void ApplyToActor(
        Entity actor,
        World world,
        Tick currentTick)
    {
        actor.TryGet<PersonalityComponent>(out var component);
        var createdThisTick = new HashSet<string>(StringComparer.Ordinal);

        foreach (var rule in _rules)
        {
            var candidate = rule.FindCreationCandidate(actor, world, currentTick);
            if (candidate is null)
            {
                continue;
            }

            ValidateCreationCandidate(rule, candidate);

            component ??= new PersonalityComponent();
            ValidateComponent(component);

            if (component.Traits.Any(trait =>
                    string.Equals(trait.Name, candidate.TraitName, StringComparison.Ordinal)))
            {
                continue;
            }

            component.Traits.Add(new PersonalityTraitState(
                candidate.TraitName,
                candidate.InitialValue,
                candidate.ReferenceWeight,
                currentTick));
            createdThisTick.Add(candidate.TraitName);
        }

        if (component is null)
        {
            return;
        }

        if (!actor.Has<PersonalityComponent>())
        {
            actor.Set(component);
        }

        ValidateComponent(component);

        for (var index = 0; index < component.Traits.Count; index++)
        {
            var trait = component.Traits[index];

            if (createdThisTick.Contains(trait.Name))
            {
                continue;
            }

            if (!_rulesByName.TryGetValue(trait.Name, out var rule))
            {
                continue;
            }

            var inflexion = rule.FindInflexion(trait, actor, world, currentTick);

            if (inflexion is not null)
            {
                ValidateInflexion(trait, inflexion);

                if (!HasAlreadyAppliedCause(component, trait.Name, inflexion.CauseId))
                {
                    component.Traits[index] = ApplyInflexion(
                        component,
                        trait,
                        inflexion,
                        currentTick);
                    continue;
                }
            }

            component.Traits[index] = Stabilize(
                trait,
                actor,
                world,
                currentTick);
        }
    }

    private PersonalityTraitState Stabilize(
        PersonalityTraitState trait,
        Entity actor,
        World world,
        Tick currentTick)
    {
        var distance = trait.ReferenceWeight - trait.Value;
        if (distance == 0d)
        {
            return trait;
        }

        var maxStep = _stabilizationParameters.ResolveMaxConvergencePerTick(
            trait.Name,
            actor,
            world,
            currentTick);

        if (!double.IsFinite(maxStep) || maxStep <= 0d)
        {
            throw new InvalidOperationException(
                $"Le Trait \"{trait.Name}\" exige une vitesse de convergence finie et strictement positive.");
        }

        var movement = Math.Min(Math.Abs(distance), maxStep);
        var newValue = distance > 0d
            ? trait.Value + movement
            : trait.Value - movement;

        return trait with
        {
            Value = Math.Clamp(newValue, 0d, 100d),
        };
    }

    private static PersonalityTraitState ApplyInflexion(
        PersonalityComponent component,
        PersonalityTraitState trait,
        PersonalityInflexion inflexion,
        Tick currentTick)
    {
        var newValue = Math.Clamp(trait.Value + inflexion.ValueDelta, 0d, 100d);
        if (newValue == trait.Value)
        {
            throw new InvalidOperationException(
                $"L'Inflexion \"{inflexion.CauseId}\" du Trait \"{trait.Name}\" ne produit aucun déplacement réel de Valeur.");
        }

        var previousReference = trait.ReferenceWeight;
        var newReference = inflexion.Kind == PersonalityInflexionKind.Deep
            ? inflexion.NewReferenceWeight!.Value
            : previousReference;

        component.Inflexions.Add(new PersonalityInflexionTrace(
            trait.Name,
            inflexion.CauseId,
            inflexion.Kind,
            newValue - trait.Value,
            previousReference,
            newReference,
            currentTick));

        return trait with
        {
            Value = newValue,
            ReferenceWeight = newReference,
        };
    }

    private static void ValidateCreationCandidate(
        IPersonalityTraitRule rule,
        PersonalityTraitCreationCandidate candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate.TraitName))
        {
            throw new InvalidOperationException(
                "Une candidate de Trait doit posséder un TraitName non vide.");
        }

        if (!string.Equals(rule.TraitName, candidate.TraitName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"La règle \"{rule.TraitName}\" ne peut pas créer le Trait \"{candidate.TraitName}\".");
        }

        ValidateBoundedFinite(candidate.InitialValue, "InitialValue", candidate.TraitName);
        ValidateBoundedFinite(candidate.ReferenceWeight, "ReferenceWeight", candidate.TraitName);
    }

    private static void ValidateComponent(PersonalityComponent component)
    {
        var duplicate = component.Traits
            .GroupBy(trait => trait.Name, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
        {
            throw new InvalidOperationException(
                $"PersonalityComponent contient plusieurs Traits nommés \"{duplicate.Key}\".");
        }

        foreach (var trait in component.Traits)
        {
            if (string.IsNullOrWhiteSpace(trait.Name))
            {
                throw new InvalidOperationException(
                    "Un Trait persisté doit posséder un Nom non vide.");
            }

            ValidateBoundedFinite(trait.Value, "Value", trait.Name);
            ValidateBoundedFinite(trait.ReferenceWeight, "ReferenceWeight", trait.Name);
        }

        foreach (var trace in component.Inflexions)
        {
            if (string.IsNullOrWhiteSpace(trace.TraitName)
                || string.IsNullOrWhiteSpace(trace.CauseId))
            {
                throw new InvalidOperationException(
                    "Une trace d'Inflexion persistée exige TraitName et CauseId non vides.");
            }

            if (!double.IsFinite(trace.ValueDelta) || trace.ValueDelta == 0d)
            {
                throw new InvalidOperationException(
                    "Une trace d'Inflexion persistée exige un ValueDelta fini et non nul.");
            }

            ValidateBoundedFinite(
                trace.PreviousReferenceWeight,
                "PreviousReferenceWeight",
                trace.TraitName);
            ValidateBoundedFinite(
                trace.NewReferenceWeight,
                "NewReferenceWeight",
                trace.TraitName);

            if (trace.Kind == PersonalityInflexionKind.Light
                && trace.PreviousReferenceWeight != trace.NewReferenceWeight)
            {
                throw new InvalidOperationException(
                    "Une trace d'Inflexion légère ne peut modifier le Poids de référence.");
            }

            if (trace.Kind == PersonalityInflexionKind.Deep
                && trace.PreviousReferenceWeight == trace.NewReferenceWeight)
            {
                throw new InvalidOperationException(
                    "Une trace d'Inflexion profonde doit modifier le Poids de référence.");
            }
        }
    }

    private static void ValidateInflexion(
        PersonalityTraitState trait,
        PersonalityInflexion inflexion)
    {
        if (string.IsNullOrWhiteSpace(inflexion.CauseId))
        {
            throw new InvalidOperationException(
                $"Une Inflexion du Trait \"{trait.Name}\" exige un CauseId non vide.");
        }

        if (!double.IsFinite(inflexion.ValueDelta) || inflexion.ValueDelta == 0d)
        {
            throw new InvalidOperationException(
                $"Une Inflexion du Trait \"{trait.Name}\" exige un ValueDelta fini et non nul.");
        }

        switch (inflexion.Kind)
        {
            case PersonalityInflexionKind.Light:
                if (inflexion.NewReferenceWeight is not null)
                {
                    throw new InvalidOperationException(
                        "Une Inflexion légère ne peut pas modifier le Poids de référence.");
                }
                break;

            case PersonalityInflexionKind.Deep:
                if (inflexion.NewReferenceWeight is null)
                {
                    throw new InvalidOperationException(
                        "Une Inflexion profonde exige un nouveau Poids de référence.");
                }

                ValidateBoundedFinite(
                    inflexion.NewReferenceWeight.Value,
                    "NewReferenceWeight",
                    trait.Name);

                if (inflexion.NewReferenceWeight.Value == trait.ReferenceWeight)
                {
                    throw new InvalidOperationException(
                        "Une Inflexion profonde doit modifier le Poids de référence.");
                }
                break;

            default:
                throw new InvalidOperationException(
                    $"Type d'Inflexion inconnu : {inflexion.Kind}.");
        }
    }

    private static bool HasAlreadyAppliedCause(
        PersonalityComponent component,
        string traitName,
        string causeId)
        => component.Inflexions.Any(trace =>
            string.Equals(trace.TraitName, traitName, StringComparison.Ordinal)
            && string.Equals(trace.CauseId, causeId, StringComparison.Ordinal));

    private static void ValidateBoundedFinite(
        double value,
        string fieldName,
        string traitName)
    {
        if (!double.IsFinite(value) || value < 0d || value > 100d)
        {
            throw new InvalidOperationException(
                $"Le champ {fieldName} du Trait \"{traitName}\" doit être fini et compris dans [0,100].");
        }
    }
}
