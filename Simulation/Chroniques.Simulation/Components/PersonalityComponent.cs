namespace Chroniques.Simulation.Components;

using Chroniques.Simulation.Kernel;

/// <summary>
/// Nature d'une Inflexion de personnalité conforme à GDB-004D.
/// </summary>
public enum PersonalityInflexionKind
{
    Light,
    Deep,
}

/// <summary>
/// État persistant d'un Trait de personnalité.
/// </summary>
public sealed record PersonalityTraitState(
    string Name,
    double Value,
    double ReferenceWeight,
    Tick CreatedAt);

/// <summary>
/// Trace persistante d'une Inflexion réellement appliquée.
/// </summary>
public sealed record PersonalityInflexionTrace(
    string TraitName,
    string CauseId,
    PersonalityInflexionKind Kind,
    double ValueDelta,
    double PreviousReferenceWeight,
    double NewReferenceWeight,
    Tick AppliedAt);

/// <summary>
/// Données persistantes de personnalité d'un habitant.
///
/// Le Component reste strictement data-only : aucune création, évolution,
/// stabilisation ou décision n'est exécutée ici.
/// </summary>
public sealed class PersonalityComponent : IComponent
{
    public List<PersonalityTraitState> Traits { get; set; } = new();
    public List<PersonalityInflexionTrace> Inflexions { get; set; } = new();
}
