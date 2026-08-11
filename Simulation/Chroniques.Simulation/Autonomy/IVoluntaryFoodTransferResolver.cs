namespace Chroniques.Simulation.Autonomy;

using Chroniques.Simulation.Actions;
using Chroniques.Simulation.Kernel;

/// <summary>
/// Frontière ENGINE-014 permettant au moteur de demander quel transfert de
/// denrée est réellement volontaire et exécutable pour un Acteur.
///
/// Cette interface ne constitue ni un marché, ni une négociation, ni un
/// système général de propriété.
/// </summary>
public interface IVoluntaryFoodTransferResolver
{
    FoodTransferOpportunity? FindExecutableTransfer(
        Entity actor,
        World world,
        Tick currentTick);
}
