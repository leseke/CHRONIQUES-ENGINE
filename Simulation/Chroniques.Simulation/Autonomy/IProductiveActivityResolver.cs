namespace Chroniques.Simulation.Autonomy;

using Chroniques.Simulation.Actions;
using Chroniques.Simulation.Kernel;

/// <summary>
/// Frontière ENGINE-013 permettant au moteur de demander quelle opération
/// productive est réellement exécutable pour un Acteur dans son contexte.
///
/// Cette interface ne constitue ni un métier, ni un employeur, ni un marché.
/// </summary>
public interface IProductiveActivityResolver
{
    ProductionOperation? FindExecutableOperation(
        Entity actor,
        World world,
        Tick currentTick);
}
