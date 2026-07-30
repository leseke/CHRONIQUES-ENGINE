namespace Chroniques.Simulation.Actions;

/// <summary>
/// Une Condition peut exiger la présence ou l'absence d'un critère, sans
/// changer de catégorie (ACT-006-A, section 5).
/// </summary>
public enum ConditionPolarite
{
    Presence,
    Absence
}
