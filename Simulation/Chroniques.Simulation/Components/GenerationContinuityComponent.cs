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

public static class GenerationContinuityRegistry
{
    public static Entity Create(World world, string id, EntityId memberId, long index = 0)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("id");
        if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
        if (!world.TryGetEntity(memberId, out _)) throw new ArgumentException("memberId");
        if (Find(world, id).Count != 0) throw new InvalidOperationException("duplicate continuity");

        var entity = world.Spawn();
        entity.Set(new GenerationContinuityComponent
        {
            ContinuityId = id,
            GenerationIndex = index,
            CurrentMemberId = memberId,
        });
        return entity;
    }

    public static GenerationContinuityComponent Get(World world, string id)
    {
        var matches = Find(world, id);
        if (matches.Count != 1) throw new InvalidOperationException("continuity must exist exactly once");
        return matches[0];
    }

    public static void Advance(World world, string id, EntityId previous, EntityId next, Tick tick, Guid evidenceId)
    {
        var item = Get(world, id);
        if (item.CurrentMemberId != previous) throw new InvalidOperationException("current member mismatch");
        if (!world.TryGetEntity(next, out _)) throw new InvalidOperationException("next member missing");
        if (item.Transitions.Any(x => x.SourceEventId == evidenceId)) return;

        checked { item.GenerationIndex++; }
        item.CurrentMemberId = next;
        item.Transitions.Add(new GenerationTransitionTrace(item.GenerationIndex, previous, next, tick, evidenceId));
    }

    private static List<GenerationContinuityComponent> Find(World world, string id)
        => world.Entities.Select(entity =>
        {
            entity.TryGet<GenerationContinuityComponent>(out var value);
            return value;
        }).Where(value => value is not null && value.ContinuityId == id).Select(value => value!).ToList();
}
