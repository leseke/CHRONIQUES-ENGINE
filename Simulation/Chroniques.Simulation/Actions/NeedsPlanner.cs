namespace Chroniques.Simulation.Actions;

using Chroniques.Simulation.Actions.Exemples;
using Chroniques.Simulation.Kernel;

/// <summary>
/// Planner minimal des besoins autonomes validés de v0.4.
///
/// Il sait uniquement matérialiser les Intents « se_reposer » et « manger ».
/// La Cible concrète de « manger » est résolue ici, jamais dans l'Intent,
/// conformément à ACT-005-A et ENGINE-012.
/// </summary>
public sealed class NeedsPlanner : IPlanner
{
    private readonly IAccessibleFoodResolver _foodResolver;

    public NeedsPlanner(IAccessibleFoodResolver foodResolver)
    {
        _foodResolver = foodResolver
            ?? throw new ArgumentNullException(nameof(foodResolver));
    }

    public Plan CreatePlan(Intent intent, World world)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (!world.TryGetEntity(intent.Acteur, out var acteur))
        {
            throw new InvalidOperationException(
                "Impossible de planifier une Action pour un Acteur absent du World.");
        }

        return intent.Objectif switch
        {
            Autonomy.NeedsIntentSource.RestObjective
                => CreateRestPlan(intent),

            Autonomy.NeedsIntentSource.EatObjective
                => CreateEatPlan(intent, acteur, world),

            _ => throw new NotSupportedException(
                $"NeedsPlanner ne reconnaît pas l'objectif \"{intent.Objectif}\".")
        };
    }

    private static Plan CreateRestPlan(Intent intent)
    {
        var etape = new PlanStep(
            SeReposerDefinition.Definition,
            PlanStepMode.Sequentiel,
            Array.Empty<PlanStep>())
        {
            Cibles = new[]
            {
                new CibleRef(
                    intent.Acteur,
                    RoleCible.Principale)
            }
        };

        return new Plan(intent, new[] { etape });
    }

    private Plan CreateEatPlan(
        Intent intent,
        Entity acteur,
        World world)
    {
        var foodId = _foodResolver.FindAccessibleFood(
            acteur,
            world,
            world.CurrentTick);

        if (foodId is null)
        {
            throw new InvalidOperationException(
                "Aucun produit alimentaire accessible ne permet de planifier l'Intent manger.");
        }

        var etape = new PlanStep(
            MangerDefinition.Definition,
            PlanStepMode.Sequentiel,
            Array.Empty<PlanStep>())
        {
            Cibles = new[]
            {
                new CibleRef(
                    foodId.Value,
                    RoleCible.Principale),
                new CibleRef(
                    intent.Acteur,
                    RoleCible.Secondaire)
            }
        };

        return new Plan(intent, new[] { etape });
    }
}
