namespace Chroniques.Simulation.Actions;

/// <summary>
/// Portée d'une Conséquence (ACT-007-A, section 4) : Locale si elle
/// n'affecte que l'Acteur et les Cibles de l'Action, Globale si elle
/// affecte l'état du monde au-delà des Entity impliquées.
/// </summary>
public enum ConsequencePortee
{
    Locale,
    Globale
}
