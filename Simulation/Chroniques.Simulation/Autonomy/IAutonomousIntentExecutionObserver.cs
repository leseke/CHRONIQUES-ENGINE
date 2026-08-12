namespace Chroniques.Simulation.Autonomy;

using Chroniques.Simulation.Actions;
using Chroniques.Simulation.Kernel;

/// <summary>
/// Observe l'exécution réelle d'un Intent autonome sans modifier le contrat
/// historique d'IAutonomousIntentExecutor (ENGINE-015).
/// </summary>
public interface IAutonomousIntentExecutionObserver
{
    /// <summary>
    /// Appelé avant l'entrée dans PipelineRunner. Cette phase sert à capturer
    /// le contexte pré-Effects ; elle ne constitue pas une seconde source
    /// d'Action ou d'Effects.
    /// </summary>
    void BeforeExecution(
        Intent intent,
        Entity actor,
        World world,
        Tick currentTick);

    /// <summary>
    /// Appelé lorsque PipelineRunner a retourné une ActionInstance terminée.
    /// L'Action est alors archivée et son Outcome est disponible.
    /// </summary>
    void AfterExecution(
        Intent intent,
        Entity actor,
        ActionInstance action,
        World world,
        Tick requestedAt);

    /// <summary>
    /// Appelé lorsque le pipeline lève une exception avant de retourner une
    /// ActionInstance. L'exception originale est ensuite relancée.
    /// </summary>
    void ExecutionAborted(
        Intent intent,
        Entity actor,
        World world,
        Tick requestedAt,
        Exception error);
}
