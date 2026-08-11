namespace Chroniques.Simulation.Components;

using Chroniques.Simulation.Kernel;

/// <summary>
/// Trace persistante d'une production matérielle conforme à GDB-005C v1.2.
/// </summary>
public sealed record ProductionTrace(
    string OperationId,
    EntityId InputResourceId,
    Tick ProducedAt);

/// <summary>
/// Conserve les origines successives d'un stock produit.
///
/// Cette donnée complète World.Events : les Events restent observables,
/// tandis que ce Component conserve la causalité durable du produit après
/// sauvegarde/rechargement.
/// </summary>
public sealed class ProductionProvenanceComponent : IComponent
{
    public List<ProductionTrace> Traces { get; set; } = new();
}
