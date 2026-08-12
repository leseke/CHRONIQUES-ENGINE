namespace Chroniques.Simulation.Autonomy;

using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;
using Chroniques.Simulation.Systems;

/// <summary>
/// Applique l'érosion déterministe des Habitudes inactives.
/// </summary>
public sealed class HabitEvolutionSystem : ISystem
{
    private readonly long _inactivityThresholdTicks;
    private readonly IHabitStrengthPolicy _strengthPolicy;

    public HabitEvolutionSystem(
        long inactivityThresholdTicks,
        IHabitStrengthPolicy strengthPolicy)
    {
        if (inactivityThresholdTicks < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(inactivityThresholdTicks),
                "Le seuil d'inactivité ne peut pas être négatif.");
        }

        _inactivityThresholdTicks = inactivityThresholdTicks;
        _strengthPolicy = strengthPolicy
            ?? throw new ArgumentNullException(nameof(strengthPolicy));
    }

    public void Update(World world, Tick currentTick)
    {
        ArgumentNullException.ThrowIfNull(world);

        foreach (var actor in world.Entities)
        {
            if (!actor.TryGet<HabitComponent>(out var component))
            {
                continue;
            }

            for (var index = component.Habits.Count - 1; index >= 0; index--)
            {
                var habit = component.Habits[index];
                var reference = habit.LastActivatedAt ?? habit.CreatedAt;
                var inactivity = currentTick.Value - reference.Value;

                if (inactivity <= _inactivityThresholdTicks)
                {
                    continue;
                }

                var eroded = NormalizeStrength(
                    _strengthPolicy.Erode(
                        habit,
                        actor,
                        world,
                        currentTick));

                if (eroded > habit.Force)
                {
                    throw new InvalidOperationException(
                        "Une politique d'érosion d'Habitude ne peut pas augmenter la Force.");
                }

                if (eroded <= 0)
                {
                    component.Habits.RemoveAt(index);
                    continue;
                }

                component.Habits[index] = habit with
                {
                    Force = eroded,
                };
            }
        }
    }

    private static double NormalizeStrength(double value)
    {
        if (!double.IsFinite(value))
        {
            throw new InvalidOperationException(
                "Une politique de Force d'Habitude doit retourner une valeur finie.");
        }

        return Math.Clamp(value, 0d, 100d);
    }
}
