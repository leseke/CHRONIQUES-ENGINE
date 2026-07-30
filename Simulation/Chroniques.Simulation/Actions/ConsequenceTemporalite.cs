namespace Chroniques.Simulation.Actions;

/// <summary>
/// Temporalité d'une Conséquence (ACT-007-A, section 4). Une Conséquence
/// Différée reste entièrement déterminée au moment de sa programmation ---
/// sa valeur ne dépend jamais d'un événement intermédiaire (ACT-007-A,
/// section 7 ; ACT-002-G, déterminisme de l'Outcome).
/// </summary>
public enum ConsequenceTemporalite
{
    Immediate,
    Differee
}
