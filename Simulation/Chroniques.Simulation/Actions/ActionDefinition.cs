namespace Chroniques.Simulation.Actions;

/// <summary>
/// Décrit ce qui peut être exécuté --- ne représente jamais une exécution
/// elle-même (ACT-002-D, section 3).
///
/// Porte la chaîne de traçabilité exigée par ACT-002-D, section 8 : toute
/// Action doit pouvoir remonter jusqu'à un Principe (ACT-002-C, section 9).
/// Ce type ne valide pas lui-même l'existence du Pattern ou du Principe
/// nommés --- cette responsabilité appartient à VERBS/PATTERNS (pilotées
/// par GDB, ACT-008-A), pas au Kernel du moteur.
/// </summary>
public sealed record ActionDefinition(
    string Verbe,
    string Pattern,
    string Principe,
    ActionContract Contract);
