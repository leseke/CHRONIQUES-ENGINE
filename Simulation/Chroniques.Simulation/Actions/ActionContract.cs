namespace Chroniques.Simulation.Actions;

/// <summary>
/// Spécifie ce qu'une Action requiert et produit (ACT-002-E). Immuable ---
/// un Action Contract ne change jamais pendant l'exécution d'une
/// <see cref="ActionInstance"/> (ACT-002-F, section 5).
///
/// <paramref name="Preconditions"/> et <paramref name="Constraints"/> sont
/// deux listes distinctes de <see cref="Condition"/> --- ACT-002-E les
/// sépare (sections 5 et 6) et ce type respecte cette séparation, même si
/// <see cref="Condition"/> lui-même ne distingue pas les deux (voir
/// Condition.cs).
/// </summary>
public sealed record ActionContract(
    IReadOnlyList<InputSlot> Inputs,
    IReadOnlyList<Condition> Preconditions,
    IReadOnlyList<Condition> Constraints,
    IReadOnlyList<CostSlot> Costs,
    IReadOnlyList<ConsequenceTemplate> Effects,
    IReadOnlyList<EventTemplate> Events);
