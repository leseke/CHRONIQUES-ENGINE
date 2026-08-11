namespace Chroniques.Simulation.Actions;

using Chroniques.Simulation.Kernel;

/// <summary>
/// Frontière ENGINE-012 entre la mécanique « Manger » et la future
/// représentation de l'accès aux produits alimentaires.
///
/// Cette interface n'est pas un inventaire. Elle répond uniquement à la
/// question GDB-005E : quelle nourriture est réellement accessible à cet
/// Acteur dans ce World et ce Tick ?
/// </summary>
public interface IAccessibleFoodResolver
{
    EntityId? FindAccessibleFood(
        Entity actor,
        World world,
        Tick currentTick);

    bool IsAccessible(
        Entity actor,
        EntityId foodId,
        World world,
        Tick currentTick);
}
