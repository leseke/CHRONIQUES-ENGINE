namespace Chroniques.Simulation.Components;

using Chroniques.Simulation.Kernel;

public sealed class GenerationContinuityComponent : IComponent
{
    public string ContinuityId { get; set; } = string.Empty;
    public long GenerationIndex { get; set; }
    public EntityId CurrentMemberId { get; set; }
    public List<GenerationTransitionTrace> Transitions { get; set; } = new();
}

public sealed record GenerationTransitionTrace(
    long GenerationIndex,
    EntityId PreviousMemberId,
    EntityId NewMemberId,
    Tick OccurredAt,
    Guid SourceEventId);
