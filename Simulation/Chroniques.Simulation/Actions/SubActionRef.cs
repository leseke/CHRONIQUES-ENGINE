namespace Chroniques.Simulation.Actions;

/// <summary>
/// Référence à une sous-Action au sein d'une Action Instance composite
/// (ACT-009-A). <paramref name="Essentielle"/> détermine la propagation
/// d'échec (ACT-009-A, section 4) : l'échec d'une sous-Action essentielle
/// fait toujours échouer l'ensemble, celui d'une sous-Action non
/// essentielle jamais à lui seul.
/// </summary>
public sealed record SubActionRef(ActionInstance Instance, bool Essentielle);
