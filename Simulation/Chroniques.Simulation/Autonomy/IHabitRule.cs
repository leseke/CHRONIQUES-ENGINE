namespace Chroniques.Simulation.Autonomy;

using Chroniques.Simulation.Actions;
using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;

/// <summary>
/// Observation déterministe susceptible d'alimenter la formation d'une
/// Habitude. Le framework générique ne devine jamais cette signature.
/// </summary>
public sealed record HabitFormationCandidate(
    string HabitTypeId,
    string IntentObjective,
    string FormationSignature);

/// <summary>
/// Règle injectée d'un type concret d'Habitude.
///
/// ENGINE-016 ne fournit aucune implémentation métier par défaut.
/// </summary>
public interface IHabitRule
{
    string HabitTypeId { get; }

    HabitFormationCandidate? ObserveFormation(
        Intent intent,
        Entity actor,
        World world,
        Tick currentTick);

    bool IsTriggered(
        HabitState habit,
        Entity actor,
        World world,
        Tick currentTick);

    bool IsIntentTreatable(
        HabitState habit,
        Entity actor,
        World world,
        Tick currentTick);
}
