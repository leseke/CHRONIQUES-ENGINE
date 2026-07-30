namespace Chroniques.Simulation.Actions;

/// <summary>
/// Gabarit d'une Conséquence prévue par un Action Contract (ACT-002-E,
/// section Effects), catégorisée selon ACT-007-A.
///
/// <paramref name="CibleVisee"/> détermine si la Conséquence est directe
/// (<see cref="RoleCible.Principale"/>) ou indirecte
/// (<see cref="RoleCible.Secondaire"/>) --- ACT-007-A, section 5. Cette
/// distinction n'est jamais stockée séparément : elle se dérive toujours de
/// <paramref name="CibleVisee"/>, pour ne jamais risquer une incohérence
/// entre les deux.
/// </summary>
public sealed record ConsequenceTemplate(
    string Description,
    ConsequenceCategorie Categorie,
    ConsequenceTemporalite Temporalite,
    ConsequenceReversibilite Reversibilite,
    ConsequencePortee Portee,
    RoleCible CibleVisee)
{
    /// <summary>
    /// Vrai si cette Conséquence est directe (vise la Cible principale),
    /// faux si elle est indirecte (ACT-007-A, section 5).
    /// </summary>
    public bool EstDirecte => CibleVisee == RoleCible.Principale;
}
