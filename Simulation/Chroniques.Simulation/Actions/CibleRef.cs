using Chroniques.Simulation.Kernel;

namespace Chroniques.Simulation.Actions;

/// <summary>
/// Référence d'une Cible d'une Action, avec son rôle (ACT-005-A, section 6).
/// </summary>
public sealed record CibleRef(EntityId Cible, RoleCible Role);
