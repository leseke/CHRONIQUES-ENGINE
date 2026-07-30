namespace Chroniques.Simulation.Actions;

/// <summary>
/// Résultat observé d'une Action exécutée (ACT-002-G). Précède toujours la
/// production des Effects et des Events (ACT-002-G, section
/// Caractéristiques) --- ce type ne contient donc aucune référence vers un
/// Effect ni un Event : les produire est une étape séparée et postérieure
/// (ENGINE-006, section 5).
///
/// Un Outcome ne connaît jamais l'Intent qui l'a motivé (ACT-002-G, section
/// Indépendance) --- ce type ne référence donc jamais <see cref="Intent"/>.
/// </summary>
public sealed record Outcome(OutcomeForme Forme);
