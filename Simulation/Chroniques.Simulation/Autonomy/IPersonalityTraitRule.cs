namespace Chroniques.Simulation.Autonomy;

using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;

/// <summary>
/// Candidat générique de création d'un Trait concret.
/// </summary>
public sealed record PersonalityTraitCreationCandidate(
    string TraitName,
    double InitialValue,
    double ReferenceWeight);

/// <summary>
/// Inflexion concrète proposée par une règle de Trait.
/// </summary>
public sealed record PersonalityInflexion(
    string CauseId,
    PersonalityInflexionKind Kind,
    double ValueDelta,
    double? NewReferenceWeight = null);

/// <summary>
/// Frontière ENGINE-018 entre la mécanique générique de personnalité et les
/// Traits concrets autorisés par GDB.
/// </summary>
public interface IPersonalityTraitRule
{
    string TraitName { get; }

    PersonalityTraitCreationCandidate? FindCreationCandidate(
        Entity actor,
        World world,
        Tick currentTick);

    PersonalityInflexion? FindInflexion(
        PersonalityTraitState trait,
        Entity actor,
        World world,
        Tick currentTick);
}
