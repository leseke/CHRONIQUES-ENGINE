namespace Chroniques.Simulation.Actions;

/// <summary>
/// Une étape d'un <see cref="Plan"/> (ACT-002-I, section 4 --- Composition).
///
/// <paramref name="DependsOn"/> ne peut jamais former de cycle (ACT-002-I,
/// section 6 : « les dépendances circulaires sont interdites ») --- cet
/// invariant n'est pas vérifié par ce type lui-même (un record n'a pas de
/// vue sur le graphe complet), il appartient à <see cref="Plan"/>.
/// </summary>
public sealed record PlanStep(
    ActionDefinition Definition,
    PlanStepMode Mode,
    IReadOnlyList<PlanStep> DependsOn);
