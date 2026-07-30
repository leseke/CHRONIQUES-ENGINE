namespace Chroniques.Simulation.Actions;

/// <summary>
/// Un coût requis par un Action Contract (ACT-002-E, section Costs).
/// </summary>
public sealed record CostSlot(string Ressource, double Quantite);
