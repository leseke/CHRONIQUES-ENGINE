using Chroniques.Simulation.Kernel;

namespace Chroniques.Simulation.Actions.Exemples;

/// <summary>
/// Orchestre le pipeline complet ENGINE-006 pour les Actions simples
/// actuellement planifiées par le moteur.
///
/// Depuis ENGINE-012, les Cibles proviennent du PlanStep et l'application
/// des Effects est déléguée à des <see cref="IActionEffectApplicator"/>.
/// Le runner ne contient ainsi aucune règle propre à VERB-001 ou VERB-002.
/// </summary>
public sealed class PipelineRunner
{
    private readonly IPlanner _planner;
    private readonly IExecutionEngine _executionEngine;
    private readonly IReadOnlyList<IActionEffectApplicator> _effectApplicators;

    public PipelineRunner(
        IPlanner planner,
        IExecutionEngine executionEngine,
        params IActionEffectApplicator[] effectApplicators)
    {
        _planner = planner
            ?? throw new ArgumentNullException(nameof(planner));
        _executionEngine = executionEngine
            ?? throw new ArgumentNullException(nameof(executionEngine));

        _effectApplicators = effectApplicators.Length == 0
            ? new IActionEffectApplicator[]
            {
                new RestActionEffectApplicator()
            }
            : effectApplicators;
    }

    /// <summary>
    /// Exécute le chemin générique d'une Action simple à partir des Cibles
    /// matérialisées par le Plan.
    /// </summary>
    public ActionInstance Execute(Intent intent, World world)
    {
        var plan = _planner.CreatePlan(intent, world);

        if (plan.Steps.Count != 1)
        {
            throw new NotSupportedException(
                "PipelineRunner ENGINE-012 exécute uniquement un Plan simple à une étape.");
        }

        var etape = plan.Steps[0];

        if (etape.Cibles.Count == 0)
        {
            throw new InvalidOperationException(
                "Le PlanStep doit porter ses Cibles avant exécution (ENGINE-012)." );
        }

        return ExecuteStep(
            intent,
            etape,
            world);
    }

    /// <summary>
    /// Compatibilité historique ENGINE-006/011.
    ///
    /// La Cible passée explicitement est convertie en Cible principale du
    /// PlanStep, puis le même chemin commun est utilisé.
    /// </summary>
    public ActionInstance ExecuterSeReposer(
        Intent intent,
        EntityId cible,
        World world)
    {
        var plan = _planner.CreatePlan(intent, world);

        if (plan.Steps.Count != 1)
        {
            throw new NotSupportedException(
                "ExecuterSeReposer attend un Plan simple à une étape.");
        }

        var etape = plan.Steps[0] with
        {
            Cibles = new[]
            {
                new CibleRef(
                    cible,
                    RoleCible.Principale)
            }
        };

        return ExecuteStep(
            intent,
            etape,
            world);
    }

    private ActionInstance ExecuteStep(
        Intent intent,
        PlanStep etape,
        World world)
    {
        var instance = new ActionInstance(
            world.CurrentTick,
            etape.Definition,
            intent.Acteur,
            etape.Cibles);

        instance.Transition(ActionState.Validated);
        instance.Transition(ActionState.Planned);
        instance.Transition(ActionState.Prepared);
        instance.Transition(ActionState.Running);

        var outcome = _executionEngine.Execute(instance, world);

        instance.Transition(
            outcome.Forme == OutcomeForme.Echec
                ? ActionState.Failed
                : ActionState.Succeeded);
        instance.DefinirOutcome(outcome);

        if (outcome.Forme != OutcomeForme.Echec)
        {
            ApplyEffects(instance, world);
        }

        instance.Transition(ActionState.Resolved);
        instance.Transition(ActionState.Committed);
        instance.Transition(ActionState.Archived);

        return instance;
    }

    private void ApplyEffects(
        ActionInstance instance,
        World world)
    {
        var applicator = _effectApplicators.FirstOrDefault(
            candidate => candidate.CanApply(instance));

        if (applicator is null)
        {
            if (instance.Definition.Contract.Effects.Count == 0)
            {
                return;
            }

            throw new InvalidOperationException(
                $"Aucun IActionEffectApplicator n'est enregistré pour le Verbe \"{instance.Definition.Verbe}\".");
        }

        applicator.Apply(instance, world);
    }
}
