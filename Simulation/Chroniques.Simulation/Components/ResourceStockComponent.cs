namespace Chroniques.Simulation.Components;

using Chroniques.Simulation.Kernel;

/// <summary>
/// Stock matériel minimal utilisé par ENGINE-013.
///
/// Pure donnée : la consommation est effectuée uniquement par les Effects
/// d'Actions validées. La quantité ne porte ni unité universelle, ni prix,
/// ni propriété implicite.
/// </summary>
public sealed class ResourceStockComponent : IComponent
{
    public double Quantity { get; set; }
}
