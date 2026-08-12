namespace Chroniques.Simulation.Autonomy;

using Chroniques.Simulation.Actions;
using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;

/// <summary>
/// Source d'Intent ENGINE-017 pour les Ambitions génériques.
///
/// Elle ne modifie jamais le World et n'interprète jamais ObjectivePayload.
/// </summary>
public sealed class AmbitionIntentSource : IAutonomousIntentSource
{
    private readonly IReadOnlyDictionary<string, IAmbitionRule> _rulesByType;

    public AmbitionIntentSource(IEnumerable<IAmbitionRule> rules)
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

        _rulesByType = materialized.ToDictionary(
            rule => rule.AmbitionTypeId,
            StringComparer.Ordinal);
    }

    public Intent? CreateIntent(
        Entity actor,
        World world,
        Tick currentTick)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(world);

        if (!actor.TryGet<AmbitionComponent>(out var component))
        {
            return null;
        }

        var candidate = component.Ambitions
            .Select((ambition, index) => new
            {
                Ambition = ambition,
                Index = index,
            })
            .Where(item => IsCandidate(
                item.Ambition,
                actor,
                world,
                currentTick))
            .OrderByDescending(item => item.Ambition.Intensity)
            .ThenByDescending(item => item.Ambition.Progress)
            .ThenBy(item => item.Ambition.CreatedAt.Value)
            .ThenBy(item => item.Index)
            .FirstOrDefault();

        if (candidate is null)
        {
            return null;
        }

        return new Intent(
            actor.Id,
            candidate.Ambition.IntentObjective,
            Priorite: 1);
    }

    private bool IsCandidate(
        AmbitionState ambition,
        Entity actor,
        World world,
        Tick currentTick)
    {
        ValidateStoredState(ambition);

        if (ambition.Intensity <= 0
            || ambition.Progress >= 100
            || ambition.IsAbandoned)
        {
            return false;
        }

        if (!_rulesByType.TryGetValue(ambition.AmbitionTypeId, out var rule))
        {
            return false;
        }

        return rule.IsIntentTreatable(
            ambition,
            actor,
            world,
            currentTick);
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
}
