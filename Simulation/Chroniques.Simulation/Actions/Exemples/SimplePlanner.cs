using Chroniques.Simulation.Actions;
using Chroniques.Simulation.Kernel;

namespace Chroniques.Simulation.Actions.Exemples;

/// <summary>
/// Planner minimal de démonstration (ACT-002-H, section Production).
/// Reconnaît uniquement l'objectif <c>"se_reposer"</c> et produit un Plan
/// à une seule étape --- ce n'est jamais un Planner destiné à un usage
/// réel, seulement une preuve que le pipeline (ENGINE-006) fonctionne de
/// bout en bout.
/// </summary>
public sealed class SimplePlanner : IPlanner
{
    public Plan CreatePlan(Intent intent, World world)
    {
        if (intent.Objectif != "se_reposer")
        {
            throw new NotSupportedException(
                $"SimplePlanner ne reconnaît que l'objectif \"se_reposer\", reçu : \"{intent.Objectif}\".");
        }

        var etape = new PlanStep(
            SeReposerDefinition.Definition,
            PlanStepMode.Sequentiel,
            Array.Empty<PlanStep>());

        return new Plan(intent, new[] { etape });
    }
}
