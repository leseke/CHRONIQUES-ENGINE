namespace Chroniques.Simulation.Autonomy;

using Chroniques.Simulation.Actions;
using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;

/// <summary>
/// Observer ENGINE-016 responsable de la formation, de l'activation et du
/// renforcement des Habitudes à partir des exécutions autonomes observées.
/// </summary>
public sealed class HabitLearningObserver : IAutonomousIntentExecutionObserver
{
    private readonly IReadOnlyList<IHabitRule> _rules;
    private readonly IHabitFormationParameterResolver _formationParameters;
    private readonly IHabitStrengthPolicy _strengthPolicy;
    private readonly HabitSelectionRegistry _selectionRegistry;
    private readonly Dictionary<EntityId, IReadOnlyList<HabitFormationCandidate>> _pendingFormation = new();

    public HabitLearningObserver(
        IEnumerable<IHabitRule> rules,
        IHabitFormationParameterResolver formationParameters,
        IHabitStrengthPolicy strengthPolicy,
        HabitSelectionRegistry selectionRegistry)
    {
        ArgumentNullException.ThrowIfNull(rules);

        var materialized = rules.ToArray();
        if (materialized.Any(rule => rule is null))
        {
            throw new ArgumentException(
                "La collection de règles d'Habitude ne peut contenir de valeur null.",
                nameof(rules));
        }

        _rules = materialized;
        _formationParameters = formationParameters
            ?? throw new ArgumentNullException(nameof(formationParameters));
        _strengthPolicy = strengthPolicy
            ?? throw new ArgumentNullException(nameof(strengthPolicy));
        _selectionRegistry = selectionRegistry
            ?? throw new ArgumentNullException(nameof(selectionRegistry));
    }

    public void BeforeExecution(
        Intent intent,
        Entity actor,
        World world,
        Tick currentTick)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(world);

        var candidates = _rules
            .Select(rule => new
            {
                Rule = rule,
                Candidate = rule.ObserveFormation(intent, actor, world, currentTick),
            })
            .Where(item => item.Candidate is not null)
            .Where(item => string.Equals(
                item.Rule.HabitTypeId,
                item.Candidate!.HabitTypeId,
                StringComparison.Ordinal))
            .Select(item => item.Candidate!)
            .Where(IsValidCandidate)
            .Distinct()
            .ToArray();

        _pendingFormation[actor.Id] = candidates;
    }

    public void AfterExecution(
        Intent intent,
        Entity actor,
        ActionInstance action,
        World world,
        Tick requestedAt)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(world);

        var hasPending = _pendingFormation.Remove(actor.Id, out var candidates);
        var hasFormationCandidates = hasPending && candidates!.Count > 0;
        var hasSelection = _selectionRegistry.TryTake(actor.Id, out var selection);

        if (!hasFormationCandidates && !hasSelection)
        {
            return;
        }

        if (!actor.TryGet<HabitComponent>(out var component))
        {
            component = new HabitComponent();
            actor.Set(component);
        }

        if (hasFormationCandidates)
        {
            foreach (var candidate in candidates!)
            {
                CommitFormation(
                    candidate,
                    component,
                    actor,
                    world,
                    requestedAt);
            }
        }

        if (hasSelection)
        {
            CommitActivationAndReinforcement(
                selection!,
                component,
                actor,
                action,
                world);
        }
    }

    public void ExecutionAborted(
        Intent intent,
        Entity actor,
        World world,
        Tick requestedAt,
        Exception error)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(error);

        _pendingFormation.Remove(actor.Id);

        if (!_selectionRegistry.TryTake(actor.Id, out var selection)
            || !actor.TryGet<HabitComponent>(out var component))
        {
            return;
        }

        CommitActivationOnly(selection, component);
    }

    private void CommitFormation(
        HabitFormationCandidate candidate,
        HabitComponent component,
        Entity actor,
        World world,
        Tick currentTick)
    {
        if (component.Habits.Any(habit => SameIdentity(habit, candidate)))
        {
            component.FormationTraces.RemoveAll(trace => SameIdentity(trace, candidate));
            return;
        }

        var parameters = _formationParameters.Resolve(
            candidate.HabitTypeId,
            actor,
            world,
            currentTick);

        ValidateFormationParameters(parameters, candidate.HabitTypeId);

        var trace = component.FormationTraces.FirstOrDefault(
            existing => SameIdentity(existing, candidate));

        if (trace is null)
        {
            trace = new HabitFormationTrace
            {
                HabitTypeId = candidate.HabitTypeId,
                IntentObjective = candidate.IntentObjective,
                FormationSignature = candidate.FormationSignature,
            };
            component.FormationTraces.Add(trace);
        }

        trace.ObservedAt.RemoveAll(observedAt =>
        {
            var age = currentTick.Value - observedAt.Value;
            return age < 0 || age >= parameters.WindowTicks;
        });

        trace.ObservedAt.Add(currentTick);

        if (trace.ObservedAt.Count < parameters.RequiredRepetitions)
        {
            return;
        }

        component.Habits.Add(
            new HabitState(
                candidate.HabitTypeId,
                candidate.IntentObjective,
                candidate.FormationSignature,
                parameters.InitialForce,
                LastActivatedAt: null,
                CreatedAt: currentTick));

        component.FormationTraces.Remove(trace);
    }

    private void CommitActivationAndReinforcement(
        HabitSelection selection,
        HabitComponent component,
        Entity actor,
        ActionInstance action,
        World world)
    {
        var index = component.Habits.FindIndex(habit => SameIdentity(habit, selection));
        if (index < 0)
        {
            return;
        }

        var current = component.Habits[index];
        var force = current.Force;

        if (action.Outcome?.Forme is OutcomeForme.Reussite or OutcomeForme.ReussitePartielle)
        {
            force = NormalizeStrength(
                _strengthPolicy.Reinforce(
                    current,
                    actor,
                    world,
                    selection.SelectedAt));

            if (force < current.Force)
            {
                throw new InvalidOperationException(
                    "Une politique de renforcement d'Habitude ne peut pas diminuer la Force.");
            }
        }

        component.Habits[index] = current with
        {
            Force = force,
            LastActivatedAt = selection.SelectedAt,
        };
    }

    private static void CommitActivationOnly(
        HabitSelection selection,
        HabitComponent component)
    {
        var index = component.Habits.FindIndex(habit => SameIdentity(habit, selection));
        if (index < 0)
        {
            return;
        }

        component.Habits[index] = component.Habits[index] with
        {
            LastActivatedAt = selection.SelectedAt,
        };
    }

    private static bool IsValidCandidate(HabitFormationCandidate candidate)
        => !string.IsNullOrWhiteSpace(candidate.HabitTypeId)
           && !string.IsNullOrWhiteSpace(candidate.IntentObjective)
           && !string.IsNullOrWhiteSpace(candidate.FormationSignature);

    private static void ValidateFormationParameters(
        HabitFormationParameters parameters,
        string habitTypeId)
    {
        if (parameters.RequiredRepetitions <= 0)
        {
            throw new InvalidOperationException(
                $"Le type d'Habitude \"{habitTypeId}\" exige RequiredRepetitions > 0.");
        }

        if (parameters.WindowTicks <= 0)
        {
            throw new InvalidOperationException(
                $"Le type d'Habitude \"{habitTypeId}\" exige WindowTicks > 0.");
        }

        if (!double.IsFinite(parameters.InitialForce)
            || parameters.InitialForce <= 0
            || parameters.InitialForce >= 100)
        {
            throw new InvalidOperationException(
                $"Le type d'Habitude \"{habitTypeId}\" exige InitialForce dans ]0,100[.");
        }
    }

    private static double NormalizeStrength(double value)
    {
        if (!double.IsFinite(value))
        {
            throw new InvalidOperationException(
                "Une politique de Force d'Habitude doit retourner une valeur finie.");
        }

        return Math.Clamp(value, 0d, 100d);
    }

    private static bool SameIdentity(
        HabitState habit,
        HabitFormationCandidate candidate)
        => string.Equals(habit.HabitTypeId, candidate.HabitTypeId, StringComparison.Ordinal)
           && string.Equals(habit.IntentObjective, candidate.IntentObjective, StringComparison.Ordinal)
           && string.Equals(habit.FormationSignature, candidate.FormationSignature, StringComparison.Ordinal);

    private static bool SameIdentity(
        HabitFormationTrace trace,
        HabitFormationCandidate candidate)
        => string.Equals(trace.HabitTypeId, candidate.HabitTypeId, StringComparison.Ordinal)
           && string.Equals(trace.IntentObjective, candidate.IntentObjective, StringComparison.Ordinal)
           && string.Equals(trace.FormationSignature, candidate.FormationSignature, StringComparison.Ordinal);

    private static bool SameIdentity(
        HabitState habit,
        HabitSelection selection)
        => string.Equals(habit.HabitTypeId, selection.HabitTypeId, StringComparison.Ordinal)
           && string.Equals(habit.IntentObjective, selection.IntentObjective, StringComparison.Ordinal)
           && string.Equals(habit.FormationSignature, selection.FormationSignature, StringComparison.Ordinal);
}
