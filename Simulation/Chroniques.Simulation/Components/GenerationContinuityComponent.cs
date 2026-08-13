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
        if (item.Transitions.Any(x => x.SourceEventId == evidenceId)) return;
        if (item.CurrentMemberId != previous) throw new InvalidOperationException("current member mismatch");
        if (!world.TryGetEntity(next, out _)) throw new InvalidOperationException("next member missing");

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

public sealed class GenerationContinuitySynchronizer
{
    private readonly string _continuityId;

    public GenerationContinuitySynchronizer(string continuityId)
    {
        if (string.IsNullOrWhiteSpace(continuityId)) throw new ArgumentException("continuityId");
        _continuityId = continuityId;
    }

    public void Synchronize(World world, EntityId activeMemberId)
    {
        var continuity = GenerationContinuityRegistry.Get(world, _continuityId);
        if (continuity.CurrentMemberId == activeMemberId) return;
        if (!world.TryGetEntity(activeMemberId, out _)) throw new InvalidOperationException("active member missing");

        var previous = continuity.CurrentMemberId;
        var evidence = world.Events.LastOrDefault(item =>
            item.OccurredAt == world.CurrentTick
            && string.Equals(item.Kind, "heritage.transmission", StringComparison.Ordinal)
            && item.Source == previous
            && item.Target == activeMemberId);

        if (evidence is null) throw new InvalidOperationException("missing continuity evidence");

        GenerationContinuityRegistry.Advance(
            world,
            _continuityId,
            previous,
            activeMemberId,
            evidence.OccurredAt,
            evidence.Id);
    }
}
