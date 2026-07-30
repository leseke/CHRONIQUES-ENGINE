namespace Chroniques.Simulation.Actions;

/// <summary>
/// Rôle d'une Cible au sein d'une Action (ACT-005-A, section 6). Une Action
/// possède toujours exactement une Cible de rôle <see cref="Principale"/>,
/// et zéro ou plusieurs de rôle <see cref="Secondaire"/>.
/// </summary>
public enum RoleCible
{
    Principale,
    Secondaire
}
