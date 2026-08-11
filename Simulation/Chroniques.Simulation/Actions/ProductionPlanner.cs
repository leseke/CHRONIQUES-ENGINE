namespace Chroniques.Simulation.Actions;

using Chroniques.Simulation.Actions.Exemples;
using Chroniques.Simulation.Autonomy;
using Chroniques.Simulation.Kernel;

/// <summary>
/// Planner ENGINE-013 pour l'objectif produire_denree.
/// </summary>
public sealed class ProductionPlanner : IPlanner
{
    private readonly IProductiveActivityResolver _resolver;

    public ProductionPlanner(IProductiveActivityResolver resolver)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    public Plan CreatePlan(Intent intent, World world)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (!string.Equals(
                intent.Objectif,
                ProductiveActivityIntentSource.ProduceFoodObjective,
                StringComparison.Ordinal))
        {
            throw new NotSupportedException(
                $"ProductionPlanner ne reconnaît pas l'objectif \"{intent.Objectif}\".");
        }

        if (!world.TryGetEntity(intent.Acteur, out var actor))
        {
            throw new InvalidOperationException(
                "Impossible de planifier une production pour un Acteur absent du World.");
        }

        var operation = _resolver.FindExecutableOperation(
            actor,
            world,
            world.CurrentTick)
            ?? throw new InvalidOperationException(
                "Aucune opération productive exécutable n'est disponible.");

        var step = new PlanStep(
            ProduireDenreeDefinition.Definition,
            PlanStepMode.Sequentiel,
            Array.Empty<PlanStep>())
        {
            Cibles = new[]
            {
                new CibleRef(
                    operation.OutputFoodProductId,
                    RoleCible.Principale),
                new CibleRef(
                    operation.InputResourceId,
                    RoleCible.Secondaire)
            }
        };

        return new Plan(intent, new[] { step });
    }
}
