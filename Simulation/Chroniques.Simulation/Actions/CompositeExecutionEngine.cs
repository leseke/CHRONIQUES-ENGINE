namespace Chroniques.Simulation.Actions;

using Chroniques.Simulation.Kernel;

/// <summary>
/// Route une Action vers l'Execution Engine explicitement enregistré pour son Verbe.
/// </summary>
public sealed class CompositeExecutionEngine : IExecutionEngine
{
    private readonly IReadOnlyDictionary<string, IExecutionEngine> _engines;

    public CompositeExecutionEngine(
        IReadOnlyDictionary<string, IExecutionEngine> engines)
    {
        ArgumentNullException.ThrowIfNull(engines);

        if (engines.Any(entry =>
                string.IsNullOrWhiteSpace(entry.Key)
                || entry.Value is null))
        {
            throw new ArgumentException(
                "Chaque Verbe doit référencer un Execution Engine valide.",
                nameof(engines));
        }

        _engines = new Dictionary<string, IExecutionEngine>(
            engines,
            StringComparer.Ordinal);
    }

    public Outcome Execute(ActionInstance instance, World world)
    {
        if (!_engines.TryGetValue(instance.Definition.Verbe, out var engine))
        {
            return new Outcome(OutcomeForme.Echec);
        }

        return engine.Execute(instance, world);
    }
}
