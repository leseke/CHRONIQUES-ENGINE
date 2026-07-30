using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;

namespace Chroniques.Simulation.Actions.Exemples;

/// <summary>
/// Orchestre le pipeline complet (ENGINE-006, section 5) pour l'unique
/// Verbe de démonstration « Se reposer » --- d'un Intent jusqu'à
/// l'archivage de l'Action Instance, Effect appliqué et Event publié.
///
/// L'application des Effects reste ici câblée spécifiquement pour « Se
/// reposer » --- un interprète générique, capable de traiter n'importe
/// quel <see cref="ConsequenceTemplate"/>, n'existe pas encore et ne doit
/// pas être anticipé avant qu'un second Verbe réel n'en démontre le
/// besoin (MASTER-006).
/// </summary>
public sealed class PipelineRunner
{
    private readonly IPlanner _planner;
    private readonly IExecutionEngine _executionEngine;

    public PipelineRunner(IPlanner planner, IExecutionEngine executionEngine)
    {
        _planner = planner;
        _executionEngine = executionEngine;
    }

    public ActionInstance ExecuterSeReposer(Intent intent, EntityId cible, World world)
    {
        var plan = _planner.CreatePlan(intent, world);
        var etape = plan.Steps[0];

        var instance = new ActionInstance(
            world.CurrentTick,
            etape.Definition,
            intent.Acteur,
            new[] { new CibleRef(cible, RoleCible.Principale) });

        instance.Transition(ActionState.Validated);
        instance.Transition(ActionState.Planned);
        instance.Transition(ActionState.Prepared);
        instance.Transition(ActionState.Running);

        var outcome = _executionEngine.Execute(instance, world);

        instance.Transition(outcome.Forme == OutcomeForme.Echec ? ActionState.Failed : ActionState.Succeeded);
        instance.DefinirOutcome(outcome);

        if (outcome.Forme != OutcomeForme.Echec)
        {
            AppliquerEffets(instance, world);
        }

        instance.Transition(ActionState.Resolved);
        instance.Transition(ActionState.Committed);
        instance.Transition(ActionState.Archived);

        return instance;
    }

    private static void AppliquerEffets(ActionInstance instance, World world)
    {
        if (!world.TryGetEntity(instance.Acteur, out var acteur) || !acteur.TryGet<NeedsComponent>(out var besoins))
        {
            return;
        }

        besoins.Fatigue = Math.Min(100.0, besoins.Fatigue + SeReposerDefinition.FatigueRestauree);

        foreach (var eventTemplate in instance.Definition.Contract.Events)
        {
            world.Publish(GameEvent.Create(world.CurrentTick, eventTemplate.Kind, instance.Acteur));
        }
    }
}
