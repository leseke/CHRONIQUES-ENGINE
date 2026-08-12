namespace Chroniques.Simulation.Components;

using Chroniques.Simulation.Kernel;

/// <summary>
/// Données persistantes ENGINE-017 des Ambitions d'un habitant.
///
/// Le Component ne contient aucune règle de création, d'évaluation ou de
/// décision. Ces responsabilités appartiennent aux services d'Autonomy.
/// </summary>
public sealed class AmbitionComponent : IComponent
{
    public List<AmbitionState> Ambitions { get; set; } = new();
}

/// <summary>
/// État persistant d'une Ambition générique.
///
/// ObjectivePayload reste opaque pour le moteur générique : seule la règle
/// associée à AmbitionTypeId sait l'interpréter.
/// </summary>
public sealed record AmbitionState(
    string AmbitionTypeId,
    string InstanceKey,
    string ObjectivePayload,
    string IntentObjective,
    double Intensity,
    double Progress,
    bool IsAbandoned,
    Tick CreatedAt);
