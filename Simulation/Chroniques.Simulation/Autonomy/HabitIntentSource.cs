namespace Chroniques.Simulation.Autonomy;

using Chroniques.Simulation.Actions;
using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;

/// <summary>
/// Source générique d'Intent issue des Habitudes formées (ENGINE-016).
///
/// Elle ne modifie jamais le World : la sélection runtime est mémorisée dans
/// HabitSelectionRegistry puis l'activation persistante est appliquée par
/// HabitLearningObserver après l'exécution.
/// </summary>
public sealed class HabitIntentSource : IAutonomousIntentSource
{
    private readonly IReadOnlyDictionary<string, IHabitRule> _rules;
    private readonly HabitSelectionRegistry _selectionRegistry;

    public HabitIntentSource(
        IEnumerable<IHabitRule> rules,
        HabitSelectionRegistry selectionRegistry)
    {
        ArgumentNullException.ThrowIfNull(rules);
        _selectionRegistry = selectionRegistry
            ?? throw new ArgumentNullException(nameof(selectionRegistry));

        var materialized = rules.ToArray();

        if (materialized.Any(rule => rule is null))
        {
            throw new ArgumentException(
                "La collection de règles d'Habitude ne peut contenir de valeur null.",
                nameof(rules));
        }

        if (materialized.Any(rule => string.IsNullOrWhiteSpace(rule.HabitTypeId)))
        {
            throw new ArgumentException(
                "Chaque règle d'Habitude doit posséder un HabitTypeId non vide.",
                nameof(rules));
        }

        var duplicates = materialized
            .GroupBy(rule => rule.HabitTypeId, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        if (duplicates.Length > 0)
        {
            throw new ArgumentException(
                $"Plusieurs règles portent le même HabitTypeId : {string.Join(", ", duplicates)}.",
                nameof(rules));
        }

        _rules = materialized.ToDictionary(
            rule => rule.HabitTypeId,
            StringComparer.Ordinal);
    }

    public Intent? CreateIntent(
        Entity actor,
        World world,
        Tick currentTick)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(world);

        _selectionRegistry.Clear(actor.Id);

        if (!actor.TryGet<HabitComponent>(out var component))
        {
            return null;
        }

        foreach (var habit in component.Habits
                     .OrderByDescending(habit => habit.Force)
                     .ThenBy(habit => habit.CreatedAt.Value))
        {
            if (habit.Force <= 0
                || string.IsNullOrWhiteSpace(habit.IntentObjective)
                || !_rules.TryGetValue(habit.HabitTypeId, out var rule)
                || !rule.IsTriggered(habit, actor, world, currentTick)
                || !rule.IsIntentTreatable(habit, actor, world, currentTick))
            {
                continue;
            }

            _selectionRegistry.Record(
                new HabitSelection(
                    actor.Id,
                    habit.HabitTypeId,
                    habit.IntentObjective,
                    habit.FormationSignature,
                    currentTick));

            return new Intent(
                actor.Id,
                habit.IntentObjective,
                Priorite: 1);
        }

        return null;
    }
}
