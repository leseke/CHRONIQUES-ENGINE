namespace Chroniques.Simulation.Actions;

using Chroniques.Simulation.Kernel;

/// <summary>
/// Route un Intent vers un Planner explicitement enregistré pour son objectif.
/// </summary>
public sealed class CompositePlanner : IPlanner
{
    private readonly IReadOnlyDictionary<string, IPlanner> _planners;

    public CompositePlanner(
        IReadOnlyDictionary<string, IPlanner> planners)
    {
        ArgumentNullException.ThrowIfNull(planners);

        if (planners.Any(entry =>
                string.IsNullOrWhiteSpace(entry.Key)
                || entry.Value is null))
        {
            throw new ArgumentException(
                "Chaque objectif doit référencer un Planner valide.",
                nameof(planners));
        }

        _planners = new Dictionary<string, IPlanner>(
            planners,
            StringComparer.Ordinal);
    }

    public Plan CreatePlan(Intent intent, World world)
    {
        if (!_planners.TryGetValue(intent.Objectif, out var planner))
        {
            throw new NotSupportedException(
                $"Aucun Planner n'est enregistré pour l'objectif \"{intent.Objectif}\".");
        }

        return planner.CreatePlan(intent, world);
    }
}
