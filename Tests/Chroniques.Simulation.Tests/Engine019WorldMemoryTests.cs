namespace Chroniques.Simulation.Tests;

using Xunit;
using Chroniques.Simulation.Autonomy;
using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;
using Chroniques.Simulation.Persistence;
using Chroniques.Simulation.Systems;

public sealed class Engine019WorldMemoryTests
{
    [Fact]
    public void Persistence_PreserveWorldMemory()
    {
        var world = new World(42);
        var entity = world.Spawn();
        entity.Set(new WorldMemoryComponent
        {
            MemoryTypeId = "test.memory",
            MemoryKey = "m-1",
            Payload = "payload",
            Tier = WorldMemoryTier.Legend,
            SourceRefs = { "event:1", "event:2" },
            CreatedGeneration = 1,
            LastEvaluatedGeneration = 3,
            Transitions =
            {
                new WorldMemoryTransitionTrace(2, WorldMemoryTier.Anecdote, WorldMemoryTier.Memory, false, new[] { "event:2" }),
                new WorldMemoryTransitionTrace(3, WorldMemoryTier.Memory, WorldMemoryTier.Legend, false, new[] { "event:2" }),
            },
        });

        var loaded = WorldRepository.Load(WorldRepository.Save(world));
        var memoryEntity = Assert.Single(loaded.Entities.Where(item => item.Has<WorldMemoryComponent>()));
        Assert.True(memoryEntity.TryGet<WorldMemoryComponent>(out var memory));
        Assert.Equal("test.memory", memory.MemoryTypeId);
        Assert.Equal("m-1", memory.MemoryKey);
        Assert.Equal("payload", memory.Payload);
        Assert.Equal(WorldMemoryTier.Legend, memory.Tier);
        Assert.Equal(2, memory.SourceRefs.Count);
        Assert.Equal(2, memory.Transitions.Count);
    }

    [Fact]
    public void Persistence_OmitsWorldMemory_WhenAbsent()
    {
        var world = new World(42);
        world.Spawn();
        var json = WorldRepository.Save(world);
        Assert.DoesNotContain("WorldMemory", json, StringComparison.Ordinal);
    }

    [Fact]
    public void NoRule_DoesNotCreateMemory()
    {
        var world = new World(42);
        var system = new WorldMemoryEvolutionSystem(Array.Empty<IWorldMemoryRule>(), new MutableGenerationResolver(0));
        system.Update(world, world.CurrentTick);
        Assert.Empty(world.Entities.Where(entity => entity.Has<WorldMemoryComponent>()));
    }

    [Fact]
    public void Candidate_CreatesAnecdote()
    {
        var world = new World(42);
        var rule = TestRule.Creating("test.memory", "m-1", "source:1");
        var system = new WorldMemoryEvolutionSystem(new[] { rule }, new MutableGenerationResolver(4));

        system.Update(world, world.CurrentTick);

        var memory = GetSingleMemory(world);
        Assert.Equal(WorldMemoryTier.Anecdote, memory.Tier);
        Assert.False(memory.IsForgotten);
        Assert.Equal(4, memory.CreatedGeneration);
        Assert.Equal(4, memory.LastEvaluatedGeneration);
    }

    [Fact]
    public void DuplicateCandidate_IsNotDuplicated()
    {
        var world = new World(42);
        var rule = TestRule.Creating("test.memory", "m-1", "source:1");
        var system = new WorldMemoryEvolutionSystem(new[] { rule }, new MutableGenerationResolver(0));

        system.Update(world, world.CurrentTick);
        system.Update(world, world.CurrentTick);

        Assert.Single(world.Entities.Where(entity => entity.Has<WorldMemoryComponent>()));
    }

    [Fact]
    public void ExistingForgottenIdentity_IsNotRecreated()
    {
        var world = new World(42);
        var entity = world.Spawn();
        entity.Set(new WorldMemoryComponent
        {
            MemoryTypeId = "test.memory",
            MemoryKey = "m-1",
            Payload = "old",
            Tier = WorldMemoryTier.Anecdote,
            IsForgotten = true,
            SourceRefs = { "source:old" },
            CreatedGeneration = 0,
            LastEvaluatedGeneration = 1,
            Transitions =
            {
                new WorldMemoryTransitionTrace(1, WorldMemoryTier.Anecdote, WorldMemoryTier.Anecdote, true, Array.Empty<string>()),
            },
        });

        var rule = TestRule.Creating("test.memory", "m-1", "source:new");
        var system = new WorldMemoryEvolutionSystem(new[] { rule }, new MutableGenerationResolver(1));
        system.Update(world, world.CurrentTick);

        Assert.Single(world.Entities.Where(item => item.Has<WorldMemoryComponent>()));
        Assert.Equal("old", GetSingleMemory(world).Payload);
    }

    [Fact]
    public void SameGeneration_DoesNotEvaluate()
    {
        var world = new World(42);
        AddMemory(world, WorldMemoryTier.Anecdote, lastGeneration: 2);
        var rule = new TestRule("test.memory");
        var system = new WorldMemoryEvolutionSystem(new[] { rule }, new MutableGenerationResolver(2));

        system.Update(world, world.CurrentTick);

        Assert.Empty(rule.EvaluatedGenerations);
    }

    [Fact]
    public void Anecdote_WithLink_BecomesMemory()
    {
        var world = new World(42);
        AddMemory(world, WorldMemoryTier.Anecdote);
        var rule = new TestRule("test.memory")
        {
            Evidence = (_, _, _, _) => Evidence(link: true, source: "link:1"),
        };

        new WorldMemoryEvolutionSystem(new[] { rule }, new MutableGenerationResolver(1)).Update(world, world.CurrentTick);

        var memory = GetSingleMemory(world);
        Assert.Equal(WorldMemoryTier.Memory, memory.Tier);
        Assert.Single(memory.Transitions);
    }

    [Fact]
    public void Anecdote_WithoutLink_BecomesForgotten()
    {
        var world = new World(42);
        AddMemory(world, WorldMemoryTier.Anecdote);
        var rule = new TestRule("test.memory");

        new WorldMemoryEvolutionSystem(new[] { rule }, new MutableGenerationResolver(1)).Update(world, world.CurrentTick);

        var memory = GetSingleMemory(world);
        Assert.True(memory.IsForgotten);
        Assert.True(memory.Transitions.Single().BecameForgotten);
    }

    [Fact]
    public void Memory_FirstReference_IncrementsCounter()
    {
        var world = new World(42);
        AddMemory(world, WorldMemoryTier.Memory);
        var rule = new TestRule("test.memory")
        {
            Evidence = (_, _, _, _) => Evidence(reference: true, source: "ref:1"),
        };

        new WorldMemoryEvolutionSystem(new[] { rule }, new MutableGenerationResolver(1)).Update(world, world.CurrentTick);

        var memory = GetSingleMemory(world);
        Assert.Equal(WorldMemoryTier.Memory, memory.Tier);
        Assert.Equal(1, memory.ConsecutiveReferencedGenerations);
        Assert.Equal(0, memory.ConsecutiveUnreferencedGenerations);
    }

    [Fact]
    public void Memory_SecondConsecutiveReference_BecomesLegend()
    {
        var world = new World(42);
        var memory = AddMemory(world, WorldMemoryTier.Memory);
        memory.ConsecutiveReferencedGenerations = 1;
        var rule = new TestRule("test.memory")
        {
            Evidence = (_, _, _, _) => Evidence(reference: true, source: "ref:2"),
        };

        new WorldMemoryEvolutionSystem(new[] { rule }, new MutableGenerationResolver(1)).Update(world, world.CurrentTick);

        Assert.Equal(WorldMemoryTier.Legend, memory.Tier);
        Assert.Equal(0, memory.ConsecutiveReferencedGenerations);
    }

    [Fact]
    public void Memory_Absence_ResetsReferencedCounter()
    {
        var world = new World(42);
        var memory = AddMemory(world, WorldMemoryTier.Memory);
        memory.ConsecutiveReferencedGenerations = 1;
        var rule = new TestRule("test.memory");

        new WorldMemoryEvolutionSystem(new[] { rule }, new MutableGenerationResolver(1)).Update(world, world.CurrentTick);

        Assert.Equal(0, memory.ConsecutiveReferencedGenerations);
        Assert.Equal(1, memory.ConsecutiveUnreferencedGenerations);
    }

    [Fact]
    public void Memory_TwoConsecutiveAbsences_BecomesForgotten()
    {
        var world = new World(42);
        var memory = AddMemory(world, WorldMemoryTier.Memory);
        memory.ConsecutiveUnreferencedGenerations = 1;
        var rule = new TestRule("test.memory");

        new WorldMemoryEvolutionSystem(new[] { rule }, new MutableGenerationResolver(1)).Update(world, world.CurrentTick);

        Assert.True(memory.IsForgotten);
        Assert.Equal(WorldMemoryTier.Memory, memory.Tier);
    }

    [Fact]
    public void Memory_RegionalInfluence_BecomesLegend()
    {
        var world = new World(42);
        AddMemory(world, WorldMemoryTier.Memory);
        var rule = new TestRule("test.memory")
        {
            Evidence = (_, _, _, _) => Evidence(regional: true, source: "region:1"),
        };

        new WorldMemoryEvolutionSystem(new[] { rule }, new MutableGenerationResolver(1)).Update(world, world.CurrentTick);

        Assert.Equal(WorldMemoryTier.Legend, GetSingleMemory(world).Tier);
    }

    [Fact]
    public void Legend_Practice_BecomesTradition()
    {
        var world = new World(42);
        AddMemory(world, WorldMemoryTier.Legend);
        var rule = new TestRule("test.memory")
        {
            Evidence = (_, _, _, _) => Evidence(practice: true, source: "practice:1"),
        };

        new WorldMemoryEvolutionSystem(new[] { rule }, new MutableGenerationResolver(1)).Update(world, world.CurrentTick);

        Assert.Equal(WorldMemoryTier.Tradition, GetSingleMemory(world).Tier);
    }

    [Fact]
    public void Legend_Contradiction_BecomesMemory()
    {
        var world = new World(42);
        AddMemory(world, WorldMemoryTier.Legend);
        var rule = new TestRule("test.memory")
        {
            Evidence = (_, _, _, _) => Evidence(contradiction: true, source: "contradiction:1"),
        };

        new WorldMemoryEvolutionSystem(new[] { rule }, new MutableGenerationResolver(1)).Update(world, world.CurrentTick);

        Assert.Equal(WorldMemoryTier.Memory, GetSingleMemory(world).Tier);
    }

    [Fact]
    public void Legend_WithoutEvidence_RemainsLegend()
    {
        var world = new World(42);
        AddMemory(world, WorldMemoryTier.Legend);
        var rule = new TestRule("test.memory");

        new WorldMemoryEvolutionSystem(new[] { rule }, new MutableGenerationResolver(1)).Update(world, world.CurrentTick);

        var memory = GetSingleMemory(world);
        Assert.Equal(WorldMemoryTier.Legend, memory.Tier);
        Assert.Empty(memory.Transitions);
    }

    [Fact]
    public void Tradition_WithPractice_RemainsTradition()
    {
        var world = new World(42);
        AddMemory(world, WorldMemoryTier.Tradition);
        var rule = new TestRule("test.memory")
        {
            Evidence = (_, _, _, _) => Evidence(practice: true, source: "practice:1"),
        };

        new WorldMemoryEvolutionSystem(new[] { rule }, new MutableGenerationResolver(1)).Update(world, world.CurrentTick);

        Assert.Equal(WorldMemoryTier.Tradition, GetSingleMemory(world).Tier);
    }

    [Fact]
    public void Tradition_WithoutPractice_BecomesLegend()
    {
        var world = new World(42);
        AddMemory(world, WorldMemoryTier.Tradition);
        var rule = new TestRule("test.memory");

        new WorldMemoryEvolutionSystem(new[] { rule }, new MutableGenerationResolver(1)).Update(world, world.CurrentTick);

        Assert.Equal(WorldMemoryTier.Legend, GetSingleMemory(world).Tier);
    }

    [Fact]
    public void SkippedGenerations_AreReplayedSequentially()
    {
        var world = new World(42);
        AddMemory(world, WorldMemoryTier.Anecdote);
        var rule = new TestRule("test.memory")
        {
            Evidence = (_, _, _, generation) => generation switch
            {
                1 => Evidence(link: true, source: "link:1"),
                2 => Evidence(reference: true, source: "ref:2"),
                3 => Evidence(reference: true, source: "ref:3"),
                _ => Evidence(),
            },
        };

        new WorldMemoryEvolutionSystem(new[] { rule }, new MutableGenerationResolver(3)).Update(world, world.CurrentTick);

        Assert.Equal(new long[] { 1, 2, 3 }, rule.EvaluatedGenerations);
        Assert.Equal(WorldMemoryTier.Legend, GetSingleMemory(world).Tier);
    }

    [Fact]
    public void MissingRule_KeepsPersistedMemoryUnchanged()
    {
        var world = new World(42);
        var memory = AddMemory(world, WorldMemoryTier.Memory);
        var system = new WorldMemoryEvolutionSystem(Array.Empty<IWorldMemoryRule>(), new MutableGenerationResolver(3));

        system.Update(world, world.CurrentTick);

        Assert.Equal(WorldMemoryTier.Memory, memory.Tier);
        Assert.Equal(0, memory.LastEvaluatedGeneration);
    }

    [Fact]
    public void PositiveEvidenceSources_AreMergedWithoutDuplicate()
    {
        var world = new World(42);
        var memory = AddMemory(world, WorldMemoryTier.Memory);
        memory.SourceRefs.Add("shared");
        var rule = new TestRule("test.memory")
        {
            Evidence = (_, _, _, _) => Evidence(reference: true, source: "shared"),
        };

        new WorldMemoryEvolutionSystem(new[] { rule }, new MutableGenerationResolver(1)).Update(world, world.CurrentTick);

        Assert.Single(memory.SourceRefs.Where(source => source == "shared"));
    }

    [Fact]
    public void Update_DoesNotAdvanceTickOrPublishEvents()
    {
        var world = new World(42);
        var tick = world.CurrentTick;
        var eventCount = world.Events.Count;
        var rule = TestRule.Creating("test.memory", "m-1", "source:1");

        new WorldMemoryEvolutionSystem(new[] { rule }, new MutableGenerationResolver(0)).Update(world, world.CurrentTick);

        Assert.Equal(tick, world.CurrentTick);
        Assert.Equal(eventCount, world.Events.Count);
    }

    [Fact]
    public void Scheduler_Integration_CreatesMemoryAtResolvedGeneration()
    {
        var world = new World(42);
        var resolver = new MutableGenerationResolver(7);
        var rule = TestRule.Creating("test.memory", "m-1", "source:1");
        var scheduler = new Scheduler();
        scheduler.Register(new WorldMemoryEvolutionSystem(new[] { rule }, resolver));

        scheduler.Tick(world);

        Assert.Equal(new Tick(1), world.CurrentTick);
        Assert.Equal(7, GetSingleMemory(world).CreatedGeneration);
    }

    private static WorldMemoryComponent AddMemory(
        World world,
        WorldMemoryTier tier,
        long lastGeneration = 0)
    {
        var entity = world.Spawn();
        var memory = new WorldMemoryComponent
        {
            MemoryTypeId = "test.memory",
            MemoryKey = "m-1",
            Payload = "payload",
            Tier = tier,
            SourceRefs = { "source:initial" },
            CreatedGeneration = 0,
            LastEvaluatedGeneration = lastGeneration,
        };
        entity.Set(memory);
        return memory;
    }

    private static WorldMemoryComponent GetSingleMemory(World world)
    {
        var entity = Assert.Single(world.Entities.Where(item => item.Has<WorldMemoryComponent>()));
        Assert.True(entity.TryGet<WorldMemoryComponent>(out var memory));
        return memory;
    }

    private static WorldMemoryGenerationEvidence Evidence(
        bool link = false,
        bool reference = false,
        bool regional = false,
        bool contradiction = false,
        bool practice = false,
        string? source = null)
        => new(
            link,
            reference,
            regional,
            contradiction,
            practice,
            source is null ? Array.Empty<string>() : new[] { source });

    private sealed class MutableGenerationResolver : IWorldMemoryGenerationResolver
    {
        public MutableGenerationResolver(long generation) => Generation = generation;
        public long Generation { get; set; }
        public long ResolveGeneration(World world, Tick currentTick) => Generation;
    }

    private sealed class TestRule : IWorldMemoryRule
    {
        public TestRule(string memoryTypeId) => MemoryTypeId = memoryTypeId;

        public string MemoryTypeId { get; }
        public Func<World, Tick, long, IReadOnlyList<WorldMemoryCreationCandidate>> Candidates { get; set; }
            = (_, _, _) => Array.Empty<WorldMemoryCreationCandidate>();
        public Func<Entity, WorldMemoryComponent, World, long, WorldMemoryGenerationEvidence> Evidence { get; set; }
            = (_, _, _, _) => Engine019WorldMemoryTests.Evidence();
        public List<long> EvaluatedGenerations { get; } = new();

        public IReadOnlyList<WorldMemoryCreationCandidate> FindCreationCandidates(
            World world,
            Tick currentTick,
            long currentGeneration)
            => Candidates(world, currentTick, currentGeneration);

        public WorldMemoryGenerationEvidence EvaluateGeneration(
            Entity memoryEntity,
            WorldMemoryComponent memory,
            World world,
            long generation)
        {
            EvaluatedGenerations.Add(generation);
            return Evidence(memoryEntity, memory, world, generation);
        }

        public static TestRule Creating(string type, string key, string source)
            => new(type)
            {
                Candidates = (_, _, _) => new[]
                {
                    new WorldMemoryCreationCandidate(type, key, "payload", new[] { source }),
                },
            };
    }
}
