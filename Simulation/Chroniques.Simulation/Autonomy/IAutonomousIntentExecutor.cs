namespace Chroniques.Simulation.Autonomy;

using Chroniques.Simulation.Actions;
using Chroniques.Simulation.Kernel;

/// <summary>
/// Frontière minimale entre l'orchestration autonome et un exécuteur
/// conforme à ENGINE-006.
///
/// Cette interface n'est pas un second Action Pipeline : elle permet
/// uniquement à AutonomousActionSystem de remettre un Intent à la couche
/// d'exécution sans connaître les Verbes, Planner ou Effects concrets.
/// </summary>
public interface IAutonomousIntentExecutor
{
    void Execute(Intent intent, World world);
}
