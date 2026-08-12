namespace Chroniques.Simulation.Persistence;

using System.Text.Json.Serialization;
using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;

/// <summary>
/// Représentation sérialisable d'une Entity : son identité, plus chaque
/// Component métier concret qui lui est attaché.
///
/// L'approche reste volontairement explicite tant que le nombre de Components
/// persistés reste faible. Les champs optionnels sont omis lorsqu'ils sont null
/// afin de préserver la forme historique des sauvegardes non concernées.
/// </summary>
public sealed record EntitySnapshot(
    Guid Id,
    long LifecycleCreatedAt,
    string LifecycleState,
    NeedsComponent? Needs,
    AgeComponent? Age,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    FoodProductComponent? FoodProduct = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    ResourceStockComponent? ResourceStock = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    ProductionProvenanceComponent? ProductionProvenance = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    HabitComponent? Habits = null);

/// <summary>
/// Représentation sérialisable d'un <see cref="World"/>.
/// </summary>
public sealed record WorldSnapshot(
    long Seed,
    long CurrentTick,
    IReadOnlyList<EntitySnapshot> Entities,
    IReadOnlyList<GameEvent> Events);
