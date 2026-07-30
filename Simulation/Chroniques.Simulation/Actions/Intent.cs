using Chroniques.Simulation.Kernel;

namespace Chroniques.Simulation.Actions;

/// <summary>
/// Représente un objectif recherché par un Acteur --- jamais la manière d'y
/// parvenir (ACT-002-H).
///
/// Un Intent ne connaît jamais les Actions, les Effets ou les Events qu'il
/// produira (ACT-002-H, section Indépendance) --- c'est pourquoi ce type ne
/// référence, et ne référencera jamais, ni <see cref="ActionInstance"/> ni
/// <see cref="Outcome"/> (ENGINE-006, section 4.1).
/// </summary>
public sealed record Intent(EntityId Acteur, string Objectif, int Priorite);
