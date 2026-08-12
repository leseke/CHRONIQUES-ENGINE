namespace Chroniques.Simulation.Autonomy;

using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;

/// <summary>
/// Politique injectable de dynamique de Force.
///
/// GDB-004E fixe les invariants (renforcement après réussite, érosion après
/// inactivité, bornes 0..100) mais laisse la forme numérique paramétrable.
/// </summary>
public interface IHabitStrengthPolicy
{
    double Reinforce(
        HabitState habit,
        Entity actor,
        World world,
        Tick currentTick);

    double Erode(
        HabitState habit,
        Entity actor,
        World world,
        Tick currentTick);
}
