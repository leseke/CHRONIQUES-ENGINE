namespace Chroniques.Simulation.Actions;

/// <summary>
/// Gabarit d'un Event prévu par un Action Contract (ACT-002-E, section
/// Events), catégorisé selon ACT-010-A, section 3.
///
/// <paramref name="Kind"/> suit la convention déjà en usage dans le moteur
/// (ENGINE-001 : notation pointée, ex. <c>"vie.mort"</c>) --- ce type ne
/// redéfinit pas cette convention, il s'y conforme.
/// </summary>
public sealed record EventTemplate(string Kind, EventCategorie Categorie);
