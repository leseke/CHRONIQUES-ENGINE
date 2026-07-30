namespace Chroniques.Simulation.Actions;

/// <summary>
/// Réversibilité d'une Conséquence (ACT-007-A, section 4). Une Conséquence
/// Irréversible ne peut jamais être annulée par une Action ultérieure ---
/// seule une compensation explicite est possible, jamais une annulation
/// (ACT-007-A, section 7).
/// </summary>
public enum ConsequenceReversibilite
{
    Reversible,
    Irreversible
}
