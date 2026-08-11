using Chroniques.Simulation.Actions;
using Chroniques.Simulation.Kernel;

namespace Chroniques.Simulation.Actions.Exemples;

/// <summary>
/// Planner minimal actuellement limité à l'unique Verbe canonique exécutable
/// de bout en bout : <c>VERB-001 — Se reposer</c>.
///
/// Il reconnaît uniquement l'objectif <c>"se_reposer"</c> et produit un Plan
/// à une seule étape. Il ne constitue pas encore un Planner général : un
/// second Verbe réel devra d'abord démontrer le besoin d'un dispatch plus
/// large conformément à ACT-008-A et MASTER-006.
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
