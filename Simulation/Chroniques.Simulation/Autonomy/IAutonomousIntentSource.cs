namespace Chroniques.Simulation.Autonomy;

using Chroniques.Simulation.Actions;
using Chroniques.Simulation.Kernel;

/// <summary>
/// Source déterministe d'Intent pour un habitant autonome (ENGINE-010).
///
/// Cette abstraction décide éventuellement d'un objectif, mais ne modifie
/// jamais directement le World et n'exécute aucune Action.
/// </summary>
public interface IAutonomousIntentSource
{
    Intent? CreateIntent(Entity actor, World world, Tick currentTick);
}
