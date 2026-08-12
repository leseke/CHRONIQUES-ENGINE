namespace Chroniques.Simulation.Autonomy;

using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;

/// <summary>
/// Candidate de création produite par un Type concret d'Ambition.
///
/// ObjectivePayload est volontairement opaque pour ENGINE-017.
/// </summary>
public sealed record AmbitionCreationCandidate(
    string AmbitionTypeId,
    string InstanceKey,
    string ObjectivePayload,
    string IntentObjective,
    double InitialIntensity);

/// <summary>
/// Résultat déterministe de l'évaluation d'une Ambition existante.
/// </summary>
public sealed record AmbitionEvaluation(
    double Progress,
    bool ShouldAbandon);

/// <summary>
/// Frontière générique ENGINE-017 vers un Type concret d'Ambition.
///
/// Le moteur ne fournit aucune implémentation métier par défaut.
/// </summary>
public interface IAmbitionRule
{
    string AmbitionTypeId { get; }

    IReadOnlyList<AmbitionCreationCandidate> FindCreationCandidates(
        Entity actor,
        World world,
        Tick currentTick);

    AmbitionEvaluation Evaluate(
        AmbitionState ambition,
        Entity actor,
        World world,
        Tick currentTick);

    bool IsIntentTreatable(
        AmbitionState ambition,
        Entity actor,
        World world,
        Tick currentTick);
}
