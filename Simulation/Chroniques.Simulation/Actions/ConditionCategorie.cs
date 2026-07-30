namespace Chroniques.Simulation.Actions;

/// <summary>
/// Catégories stables de Conditions (ACT-006-A, section 3). Une Condition
/// n'appartient jamais qu'à une seule catégorie (ACT-006-A, section 4).
/// </summary>
public enum ConditionCategorie
{
    Physique,
    Possession,
    Cout,
    Etat,
    Legal,
    Social,
    Temporel,
    Narratif
}
