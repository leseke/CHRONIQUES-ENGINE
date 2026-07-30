using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;

namespace Chroniques.Simulation.Actions.Exemples;

/// <summary>
/// Execution Engine minimal de démonstration (ACT-002-F). Vérifie
/// uniquement que l'Acteur possède un <see cref="NeedsComponent"/> --- un
/// garde-fou technique, pas une Condition formelle (ACT-006-A) --- et
/// produit l'Outcome correspondant. Ne modifie jamais le World lui-même :
/// conformément à ACT-002-G, l'Outcome précède toujours les Effects, qui
/// sont appliqués séparément par <see cref="PipelineRunner"/>.
/// </summary>
public sealed class SimpleExecutionEngine : IExecutionEngine
{
    public Outcome Execute(ActionInstance instance, World world)
    {
        if (!world.TryGetEntity(instance.Acteur, out var acteur) || !acteur.Has<NeedsComponent>())
        {
            return new Outcome(OutcomeForme.Echec);
        }

        return new Outcome(OutcomeForme.Reussite);
    }
}
