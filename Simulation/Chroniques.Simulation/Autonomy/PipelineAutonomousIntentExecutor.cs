namespace Chroniques.Simulation.Autonomy;

using Chroniques.Simulation.Actions;
using Chroniques.Simulation.Actions.Exemples;
using Chroniques.Simulation.Kernel;

/// <summary>
/// Adaptateur ENGINE-015 entre l'orchestration autonome et PipelineRunner.
///
/// Il conserve le contrat historique void d'IAutonomousIntentExecutor tout
/// en exposant le contexte pré-exécution et l'Action terminée à des
/// observateurs injectés.
/// </summary>
public sealed class PipelineAutonomousIntentExecutor : IAutonomousIntentExecutor
{
    private readonly PipelineRunner _pipeline;
    private readonly IReadOnlyList<IAutonomousIntentExecutionObserver> _observers;

    public PipelineAutonomousIntentExecutor(
        PipelineRunner pipeline,
        params IAutonomousIntentExecutionObserver[] observers)
    {
        _pipeline = pipeline
            ?? throw new ArgumentNullException(nameof(pipeline));

        ArgumentNullException.ThrowIfNull(observers);

        if (observers.Any(observer => observer is null))
        {
            throw new ArgumentException(
                "La liste des observateurs ne peut pas contenir de valeur null.",
                nameof(observers));
        }

        _observers = observers.ToArray();
    }

    public void Execute(Intent intent, World world)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(world);

        if (!world.TryGetEntity(intent.Acteur, out var actor))
        {
            throw new InvalidOperationException(
                "Un Intent autonome ne peut pas être exécuté si son Acteur est absent du World.");
        }

        var requestedAt = world.CurrentTick;

        foreach (var observer in _observers)
        {
            observer.BeforeExecution(
                intent,
                actor,
                world,
                requestedAt);
        }

        ActionInstance action;

        try
        {
            action = _pipeline.Execute(intent, world);
        }
        catch (Exception error)
        {
            foreach (var observer in _observers)
            {
                observer.ExecutionAborted(
                    intent,
                    actor,
                    world,
                    requestedAt,
                    error);
            }

            throw;
        }

        foreach (var observer in _observers)
        {
            observer.AfterExecution(
                intent,
                actor,
                action,
                world,
                requestedAt);
        }
    }
}
