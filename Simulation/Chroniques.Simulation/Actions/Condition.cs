namespace Chroniques.Simulation.Actions;

/// <summary>
/// Une Condition à satisfaire pour qu'une Action soit éligible --- une
/// Precondition ou une Constraint de l'Action Contract (ACT-002-E, sections
/// 5 et 6), catégorisée selon ACT-006-A, section 3, avec sa polarité
/// (ACT-006-A, section 5).
///
/// Ce type ne distingue pas Precondition de Constraint : cette distinction
/// reste au niveau d'<see cref="ActionContract"/>, qui porte deux listes
/// séparées (ACT-002-E) --- <see cref="Condition"/> décrit uniquement ce
/// qu'une Condition contient, indépendamment de la liste qui la porte.
/// </summary>
public sealed record Condition(
    string Description,
    ConditionCategorie Categorie,
    ConditionPolarite Polarite);
