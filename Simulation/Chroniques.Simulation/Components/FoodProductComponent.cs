namespace Chroniques.Simulation.Components;

using System.Text.Json.Serialization;
using Chroniques.Simulation.Kernel;

/// <summary>
/// Représente le minimum exécutable d'un produit alimentaire.
///
/// ENGINE-014 ajoute <see cref="ProductKindId"/> afin que deux stocks
/// distincts puissent être comparés avant un transfert. Cette identité est
/// optionnelle pour les usages historiques de Manger/ProduireDenree, mais un
/// transfert entre stocks exige deux identités non vides et identiques.
///
/// Pure donnée : aucune consommation, production ou circulation n'est
/// exécutée dans le Component lui-même.
/// </summary>
public sealed class FoodProductComponent : IComponent
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ProductKindId { get; set; }

    public double FaimRestauree { get; set; }
    public int PortionsDisponibles { get; set; }
}
