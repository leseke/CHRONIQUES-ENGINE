namespace Chroniques.Simulation.Components;

using Chroniques.Simulation.Kernel;

/// <summary>
/// Représente le minimum exécutable d'un produit alimentaire (GDB-005E v1.1,
/// ENGINE-012).
///
/// Pure donnée : aucune consommation ni restauration de Faim n'est exécutée
/// dans le Component lui-même.
/// </summary>
public sealed class FoodProductComponent : IComponent
{
    public double FaimRestauree { get; set; }
    public int PortionsDisponibles { get; set; }
}
