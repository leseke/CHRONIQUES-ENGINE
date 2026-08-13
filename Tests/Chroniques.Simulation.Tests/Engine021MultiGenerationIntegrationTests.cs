namespace Chroniques.Simulation.Tests;

using Xunit;
using Chroniques.Simulation.Actions;
using Chroniques.Simulation.Autonomy;
using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;
using Chroniques.Simulation.Session;
using Chroniques.Simulation.Systems;
using GCR = Chroniques.Simulation.Components.GenerationContinuityRegistry;
using GCS = Chroniques.Simulation.Components.GenerationContinuitySynchronizer;

public sealed class Engine021MultiGenerationIntegrationTests
{
    [Fact]
    public void TwoGenerations_KeepAutonomyAndWorldMemoryAlive()
    {
        var r = Run();
        Assert.Equal(2, r.Continuity.GenerationIndex);
        Assert.Equal(r.C.Id, r.Continuity.CurrentMemberId);
        Assert.Equal(WorldMemoryTier.Legend, r.Memory.Tier);
        Assert.Equal(2, r.Memory.LastEvaluatedGeneration);
        Assert.Equal(new long[] { 1, 2 }, r.Continuity.Transitions.Select(x => x.GenerationIndex).ToArray());
        Assert.Equal(new long[] { 1, 2 }, r.Memory.Transitions.Select(x => x.Generation).ToArray());

        var deathA = Assert.Single(r.World.Events.Where(x => x.Kind == "vie.mort" && x.Source == r.A.Id)).OccurredAt.Value;
        var deathB = Assert.Single(r.World.Events.Where(x => x.Kind == "vie.mort" && x.Source == r.B.Id)).OccurredAt.Value;
        Assert.Contains(r.World.Events, x => x.Kind == "besoin.fatigue.restauree" && x.Source == r.B.Id && x.OccurredAt.Value > deathA);
        Assert.Contains(r.World.Events, x => x.Kind == "besoin.fatigue.restauree" && x.Source == r.C.Id && x.OccurredAt.Value > deathB);
    }

    [Fact]
    public void SameInitialState_ProducesSameMultiGenerationTrace()
    {
        var a = Run();
        var b = Run();
        Assert.Equal(a.Continuity.Transitions.Select(x => (x.GenerationIndex, x.OccurredAt.Value)).ToArray(), b.Continuity.Transitions.Select(x => (x.GenerationIndex, x.OccurredAt.Value)).ToArray());
        Assert.Equal(a.Memory.Transitions.Select(x => (x.Generation, x.NewTier)).ToArray(), b.Memory.Transitions.Select(x => (x.Generation, x.NewTier)).ToArray());
    }

    private static Result Run()
    {
        var world = new World(42);
        var a = Actor(world, 79); var b = Actor(world, 78); var c = Actor(world, 77);
        var rel = new RelationSystem();
        rel.EnregistrerInteraction(world, Tick.Zero, a.Id, b.Id, TypeRelation.Familiale, 10, "famille");
        rel.EnregistrerInteraction(world, Tick.Zero, b.Id, c.Id, TypeRelation.Familiale, 10, "famille");
        GCR.Create(world, "main", a.Id);
        var sync = new GCS("main");
        var resolver = new LineageWorldMemoryGenerationResolver("main");

        var memory = new WorldMemoryComponent { MemoryTypeId = "test.multi", MemoryKey = "chronique", Payload = "trace", Tier = WorldMemoryTier.Anecdote, CreatedGeneration = 0, LastEvaluatedGeneration = 0 };
        memory.SourceRefs.Add("g0"); world.Spawn().Set(memory);

        var food = new NoFood();
        var executor = new PipelineAutonomousIntentExecutor(new PipelineRunner(new NeedsPlanner(food), new NeedsExecutionEngine(food)));
        var autonomy = new AutonomousActionSystem(new NeedsIntentSource(100), executor);
        autonomy.RegisterActor(a.Id); autonomy.RegisterActor(b.Id); autonomy.RegisterActor(c.Id);

        var scheduler = new Scheduler();
        scheduler.Register(new NeedsDecaySystem(0, 25, 0));
        scheduler.Register(autonomy);
        scheduler.Register(new AgingSystem(esperanceDeVie: 80));
        scheduler.Register(new HeritageSystem());
        scheduler.Register(new WorldMemoryEvolutionSystem(new[] { new MemoryRule() }, resolver));
        var session = new LifeSession(world, scheduler, a.Id);

        while (GCR.Get(world, "main").GenerationIndex < 2)
        {
            session.AdvanceTime();
            Assert.Equal(LifeSessionState.Active, session.State);
            sync.Synchronize(world, session.ActiveCharacterId);
        }
        session.AdvanceTime(); sync.Synchronize(world, session.ActiveCharacterId);
        return new Result(world, a, b, c, GCR.Get(world, "main"), memory);
    }

    private static Entity Actor(World world, int age)
    {
        var e = world.Spawn(); e.Set(new AgeComponent { Annees = age });
        e.Set(new NeedsComponent { Fatigue = 50, Faim = 100, Sante = 100, Moral = 100 }); e.Set(new RelationComponent()); return e;
    }

    private sealed class NoFood : IAccessibleFoodResolver
    {
        public EntityId? FindAccessibleFood(Entity actor, World world, Tick tick) => null;
        public bool IsAccessible(Entity actor, EntityId food, World world, Tick tick) => false;
    }

    private sealed class MemoryRule : IWorldMemoryRule
    {
        public string MemoryTypeId => "test.multi";
        public IReadOnlyList<WorldMemoryCreationCandidate> FindCreationCandidates(World world, Tick tick, long generation) => Array.Empty<WorldMemoryCreationCandidate>();
        public WorldMemoryGenerationEvidence EvaluateGeneration(Entity entity, WorldMemoryComponent memory, World world, long generation) => generation switch
        {
            1 => new(true, false, false, false, false, new[] { "g1" }),
            2 => new(false, false, true, false, false, new[] { "g2" }),
            _ => new(false, false, false, false, false, Array.Empty<string>())
        };
    }

    private sealed record Result(World World, Entity A, Entity B, Entity C, GenerationContinuityComponent Continuity, WorldMemoryComponent Memory);
}
