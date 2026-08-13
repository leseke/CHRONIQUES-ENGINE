namespace Chroniques.Simulation.Tests;

using Xunit;
using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;
using Chroniques.Simulation.Persistence;
using LineageWorldMemoryGenerationResolver = Chroniques.Simulation.Autonomy.LineageWorldMemoryGenerationResolver;

public sealed class Engine020Tests
{
    [Fact]
    public void Create_InitializesContinuity()
    {
        var world = new World(42); var a = world.Spawn();
        GenerationContinuityRegistry.Create(world, "main", a.Id);
        var c = GenerationContinuityRegistry.Get(world, "main");
        Assert.Equal(0, c.GenerationIndex); Assert.Equal(a.Id, c.CurrentMemberId);
    }

    [Fact]
    public void Create_MissingMember_IsRejected()
    {
        var world = new World(42);
        Assert.Throws<ArgumentException>(() => GenerationContinuityRegistry.Create(world, "main", EntityId.New()));
    }

    [Fact]
    public void Create_NegativeIndex_IsRejected()
    {
        var world = new World(42); var a = world.Spawn();
        Assert.Throws<ArgumentOutOfRangeException>(() => GenerationContinuityRegistry.Create(world, "main", a.Id, -1));
    }

    [Fact]
    public void Create_DuplicateId_IsRejected()
    {
        var world = new World(42); var a = world.Spawn();
        GenerationContinuityRegistry.Create(world, "main", a.Id);
        Assert.Throws<InvalidOperationException>(() => GenerationContinuityRegistry.Create(world, "main", a.Id));
    }

    [Fact]
    public void MultipleContinuities_Coexist()
    {
        var world = new World(42); var a = world.Spawn(); var b = world.Spawn();
        GenerationContinuityRegistry.Create(world, "a", a.Id, 2);
        GenerationContinuityRegistry.Create(world, "b", b.Id, 7);
        Assert.Equal(2, GenerationContinuityRegistry.Get(world, "a").GenerationIndex);
        Assert.Equal(7, GenerationContinuityRegistry.Get(world, "b").GenerationIndex);
    }

    [Fact]
    public void Advance_IncrementsAndTraces()
    {
        var world = new World(42); var a = world.Spawn(); var b = world.Spawn(); var id = Guid.NewGuid();
        GenerationContinuityRegistry.Create(world, "main", a.Id);
        GenerationContinuityRegistry.Advance(world, "main", a.Id, b.Id, Tick.Zero, id);
        var c = GenerationContinuityRegistry.Get(world, "main");
        Assert.Equal(1, c.GenerationIndex); Assert.Equal(b.Id, c.CurrentMemberId); Assert.Equal(id, Assert.Single(c.Transitions).SourceEventId);
    }

    [Fact]
    public void Advance_SameEvidence_IsIdempotent()
    {
        var world = new World(42); var a = world.Spawn(); var b = world.Spawn(); var id = Guid.NewGuid();
        GenerationContinuityRegistry.Create(world, "main", a.Id);
        GenerationContinuityRegistry.Advance(world, "main", a.Id, b.Id, Tick.Zero, id);
        GenerationContinuityRegistry.Advance(world, "main", a.Id, b.Id, Tick.Zero, id);
        Assert.Equal(1, GenerationContinuityRegistry.Get(world, "main").GenerationIndex);
    }

    [Fact]
    public void Synchronize_CurrentEvidence_Advances()
    {
        var world = new World(42); var a = world.Spawn(); var b = world.Spawn();
        GenerationContinuityRegistry.Create(world, "main", a.Id);
        world.Publish(GameEvent.Create(Tick.Zero, "heritage.transmission", a.Id, b.Id));
        new GenerationContinuitySynchronizer("main").Synchronize(world, b.Id);
        Assert.Equal(1, GenerationContinuityRegistry.Get(world, "main").GenerationIndex);
    }

    [Fact]
    public void Resolver_ReadsWithoutMutation()
    {
        var world = new World(42); var a = world.Spawn();
        GenerationContinuityRegistry.Create(world, "main", a.Id, 4); var tick = world.CurrentTick;
        Assert.Equal(4, new LineageWorldMemoryGenerationResolver("main").ResolveGeneration(world, tick));
        Assert.Equal(tick, world.CurrentTick);
    }

    [Fact]
    public void Persistence_RoundTrip_PreservesContinuity()
    {
        var world = new World(42); var a = world.Spawn(); var b = world.Spawn();
        GenerationContinuityRegistry.Create(world, "main", a.Id);
        GenerationContinuityRegistry.Advance(world, "main", a.Id, b.Id, Tick.Zero, Guid.NewGuid());
        var c = GenerationContinuityRegistry.Get(WorldRepository.Load(WorldRepository.Save(world)), "main");
        Assert.Equal(1, c.GenerationIndex); Assert.Equal(b.Id, c.CurrentMemberId); Assert.Single(c.Transitions);
    }
}
