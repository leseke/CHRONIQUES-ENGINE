namespace Chroniques.Simulation.Tests;

using Xunit;
using Chroniques.Simulation.Autonomy;
using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;

public sealed class Engine019WorldMemoryInvariantTests
{
    [Fact]
    public void Constructor_DuplicateRuleType_IsRejected()
    {
        Assert.Throws<ArgumentException>(() => new WorldMemoryEvolutionSystem(
            new IWorldMemoryRule[] { new TestRule("dup"), new TestRule("dup") },
            new FixedGenerationResolver(0)));
    }

    [Fact]
    public void Constructor_EmptyRuleType_IsRejected()
    {
        Assert.Throws<ArgumentException>(() => new WorldMemoryEvolutionSystem(
            new IWorldMemoryRule[] { new TestRule(" ") },
            new FixedGenerationResolver(0)));
    }

    [Fact]
    public void NegativeGeneration_IsRejected()
    {
        var world = new World(42);
        var system = new WorldMemoryEvolutionSystem(Array.Empty<IWorldMemoryRule>(), new FixedGenerationResolver(-1));
        Assert.Throws<InvalidOperationException>(() => system.Update(world, world.CurrentTick));
    }

    [Fact]
    public void GenerationRegression_IsRejected()
    {
        var world = new World(42);
        AddMemory(world, memory =>
        {
            memory.CreatedGeneration = 2;
            memory.LastEvaluatedGeneration = 3;
        });

        var system = new WorldMemoryEvolutionSystem(Array.Empty<IWorldMemoryRule>(), new FixedGenerationResolver(2));
        Assert.Throws<InvalidOperationException>(() => system.Update(world, world.CurrentTick));
    }

    [Fact]
    public void PersistedEmptyIdentity_IsRejected()
    {
        var world = new World(42);
        AddMemory(world, memory => memory.MemoryKey = " ");
        AssertInvalidExisting(world);
    }

    [Fact]
    public void PersistedEmptySources_AreRejected()
    {
        var world = new World(42);
        AddMemory(world, memory => memory.SourceRefs.Clear());
        AssertInvalidExisting(world);
    }

    [Fact]
    public void PersistedDuplicateSources_AreRejected()
    {
        var world = new World(42);
        AddMemory(world, memory => memory.SourceRefs.Add("source:1"));
        AssertInvalidExisting(world);
    }

    [Fact]
    public void PersistedBothCountersPositive_IsRejected()
    {
        var world = new World(42);
        AddMemory(world, memory =>
        {
            memory.Tier = WorldMemoryTier.Memory;
            memory.ConsecutiveReferencedGenerations = 1;
            memory.ConsecutiveUnreferencedGenerations = 1;
        });
        AssertInvalidExisting(world);
    }

    [Fact]
    public void CountersOutsideMemoryTier_AreRejected()
    {
        var world = new World(42);
        AddMemory(world, memory => memory.ConsecutiveReferencedGenerations = 1);
        AssertInvalidExisting(world);
    }

    [Fact]
    public void CandidateWithWrongType_IsRejected()
    {
        var world = new World(42);
        var rule = new TestRule("expected")
        {
            Candidates = new[]
            {
                new WorldMemoryCreationCandidate("other", "m-1", "payload", new[] { "source:1" }),
            },
        };

        var system = new WorldMemoryEvolutionSystem(new[] { rule }, new FixedGenerationResolver(0));
        Assert.Throws<InvalidOperationException>(() => system.Update(world, world.CurrentTick));
    }

    [Fact]
    public void CandidateWithoutSource_IsRejected()
    {
        var world = new World(42);
        var rule = new TestRule("test")
        {
            Candidates = new[]
            {
                new WorldMemoryCreationCandidate("test", "m-1", "payload", Array.Empty<string>()),
            },
        };

        var system = new WorldMemoryEvolutionSystem(new[] { rule }, new FixedGenerationResolver(0));
        Assert.Throws<InvalidOperationException>(() => system.Update(world, world.CurrentTick));
    }

    [Fact]
    public void PositiveEvidenceWithoutSource_IsRejected()
    {
        var world = new World(42);
        AddMemory(world);
        var rule = new TestRule("test.memory")
        {
            Evidence = new WorldMemoryGenerationEvidence(true, false, false, false, false, Array.Empty<string>()),
        };

        AssertInvalidEvidence(world, rule);
    }

    [Fact]
    public void AnecdoteWithIncompatibleEvidence_IsRejected()
    {
        var world = new World(42);
        AddMemory(world);
        var rule = new TestRule("test.memory")
        {
            Evidence = new WorldMemoryGenerationEvidence(false, true, false, false, false, new[] { "ref:1" }),
        };

        AssertInvalidEvidence(world, rule);
    }

    [Fact]
    public void LegendPracticeAndContradiction_IsRejected()
    {
        var world = new World(42);
        AddMemory(world, memory => memory.Tier = WorldMemoryTier.Legend);
        var rule = new TestRule("test.memory")
        {
            Evidence = new WorldMemoryGenerationEvidence(false, false, false, true, true, new[] { "evidence:1" }),
        };

        AssertInvalidEvidence(world, rule);
    }

    [Fact]
    public void InvalidPersistedTransitionShape_IsRejected()
    {
        var world = new World(42);
        AddMemory(world, memory =>
        {
            memory.LastEvaluatedGeneration = 1;
            memory.Transitions.Add(new WorldMemoryTransitionTrace(
                1,
                WorldMemoryTier.Anecdote,
                WorldMemoryTier.Legend,
                false,
                new[] { "source:2" }));
        });

        AssertInvalidExisting(world, generation: 1);
    }

    [Fact]
    public void ForgottenStateWithoutMatchingTrace_IsRejected()
    {
        var world = new World(42);
        AddMemory(world, memory => memory.IsForgotten = true);
        AssertInvalidExisting(world);
    }

    private static void AssertInvalidExisting(World world, long generation = 0)
    {
        var system = new WorldMemoryEvolutionSystem(Array.Empty<IWorldMemoryRule>(), new FixedGenerationResolver(generation));
        Assert.Throws<InvalidOperationException>(() => system.Update(world, world.CurrentTick));
    }

    private static void AssertInvalidEvidence(World world, TestRule rule)
    {
        var system = new WorldMemoryEvolutionSystem(new[] { rule }, new FixedGenerationResolver(1));
        Assert.Throws<InvalidOperationException>(() => system.Update(world, world.CurrentTick));
    }

    private static void AddMemory(World world, Action<WorldMemoryComponent>? configure = null)
    {
        var entity = world.Spawn();
        var memory = new WorldMemoryComponent
        {
            MemoryTypeId = "test.memory",
            MemoryKey = "m-1",
            Payload = "payload",
            Tier = WorldMemoryTier.Anecdote,
            SourceRefs = { "source:1" },
            CreatedGeneration = 0,
            LastEvaluatedGeneration = 0,
        };
        configure?.Invoke(memory);
        entity.Set(memory);
    }

    private sealed class FixedGenerationResolver : IWorldMemoryGenerationResolver
    {
        private readonly long _generation;
        public FixedGenerationResolver(long generation) => _generation = generation;
        public long ResolveGeneration(World world, Tick currentTick) => _generation;
    }

    private sealed class TestRule : IWorldMemoryRule
    {
        public TestRule(string type) => MemoryTypeId = type;
        public string MemoryTypeId { get; }
        public IReadOnlyList<WorldMemoryCreationCandidate> Candidates { get; set; } = Array.Empty<WorldMemoryCreationCandidate>();
        public WorldMemoryGenerationEvidence Evidence { get; set; }
            = new(false, false, false, false, false, Array.Empty<string>());

        public IReadOnlyList<WorldMemoryCreationCandidate> FindCreationCandidates(
            World world,
            Tick currentTick,
            long currentGeneration)
            => Candidates;

        public WorldMemoryGenerationEvidence EvaluateGeneration(
            Entity memoryEntity,
            WorldMemoryComponent memory,
            World world,
            long generation)
            => Evidence;
    }
}
