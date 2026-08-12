namespace Chroniques.Simulation.Components;

using Chroniques.Simulation.Kernel;

/// <summary>
/// Habitude formée d'un habitant (GDB-004E v1.2 / ENGINE-016).
///
/// Pure donnée : les règles de Déclencheur, de formation, de renforcement
/// et d'érosion restent dans les services compétents.
/// </summary>
public sealed record HabitState(
    string HabitTypeId,
    string IntentObjective,
    string FormationSignature,
    double Force,
    Tick? LastActivatedAt,
    Tick CreatedAt);

/// <summary>
/// Séquence persistante de répétitions encore insuffisante pour former une
/// Habitude.
/// </summary>
public sealed class HabitFormationTrace
{
    public string HabitTypeId { get; set; } = string.Empty;
    public string IntentObjective { get; set; } = string.Empty;
    public string FormationSignature { get; set; } = string.Empty;
    public List<Tick> ObservedAt { get; set; } = new();
}

/// <summary>
/// Données persistantes des Habitudes et de leur formation pour un habitant.
/// </summary>
public sealed class HabitComponent : IComponent
{
    public List<HabitState> Habits { get; set; } = new();
    public List<HabitFormationTrace> FormationTraces { get; set; } = new();
}
