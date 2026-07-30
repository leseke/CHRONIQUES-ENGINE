namespace Chroniques.Simulation.Actions;

/// <summary>
/// Un emplacement d'Input attendu par un Action Contract (ACT-002-E,
/// section Inputs --- ex. : acteur, cible, outil, matériau, quantité).
/// </summary>
public sealed record InputSlot(string Nom, string TypeAttendu);
