namespace Chroniques.Simulation.Persistence;

using System.Text.Json.Serialization;
using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;

/// <summary>
/// Représentation sérialisable d'une Entity : son identité, plus chaque
/// Component métier concret qui lui est attaché.
///
/// Approche volontairement explicite plutôt que polymorphe : System.Text.Json
/// ne sérialise pas nativement un <c>Dictionary&lt;Type, IComponent&gt;</c>
/// sans convertisseur dédié. Tant qu'un petit nombre de Components existe,
/// un champ nullable par type reste plus simple et plus lisible qu'un
/// mécanisme générique.
///
/// ENGINE-012 ajoute <see cref="FoodProduct"/> afin qu'un produit alimentaire
/// conserve sa valeur de restauration et ses portions disponibles après
/// sauvegarde/rechargement. Le champ est omis lorsqu'il est null afin de ne
/// pas modifier la forme JSON historique des Entities non alimentaires.
/// </summary>
public sealed record EntitySnapshot(
    Guid Id,
    long LifecycleCreatedAt,
    string LifecycleState,
    NeedsComponent? Needs,
    AgeComponent? Age,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    FoodProductComponent? FoodProduct = null);

/// <summary>
/// Représentation sérialisable d'un <see cref="World"/>.
/// </summary>
public sealed record WorldSnapshot(
    long Seed,
    long CurrentTick,
    IReadOnlyList<EntitySnapshot> Entities,
    IReadOnlyList<GameEvent> Events);
