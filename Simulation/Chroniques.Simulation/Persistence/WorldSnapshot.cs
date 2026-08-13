namespace Chroniques.Simulation.Persistence;

using System.Text.Json.Serialization;
using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;

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
    HabitComponent? Habits = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    AmbitionComponent? Ambitions = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    PersonalityComponent? Personality = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    WorldMemoryComponent? WorldMemory = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    GenerationContinuityComponent? GenerationContinuity = null);

public sealed record WorldSnapshot(
    long Seed,
    long CurrentTick,
    IReadOnlyList<EntitySnapshot> Entities,
    IReadOnlyList<GameEvent> Events);
