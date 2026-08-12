namespace Chroniques.Simulation.Autonomy;

using Chroniques.Simulation.Kernel;

/// <summary>
/// Paramètres déterministes de formation d'un type d'Habitude.
/// </summary>
public sealed record HabitFormationParameters(
    int RequiredRepetitions,
    long WindowTicks,
    double InitialForce);

/// <summary>
/// Résout les paramètres de formation sans imposer de constantes universelles.
/// Cette frontière pourra ultérieurement intégrer un mapping Trait/Habitude
/// explicitement autorisé par GDB-004D.
/// </summary>
public interface IHabitFormationParameterResolver
{
    HabitFormationParameters Resolve(
        string habitTypeId,
        Entity actor,
        World world,
        Tick currentTick);
}
