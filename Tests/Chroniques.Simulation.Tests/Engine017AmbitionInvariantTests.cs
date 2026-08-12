namespace Chroniques.Simulation.Tests;

using Xunit;
using Chroniques.Simulation.Autonomy;
using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;

public sealed class Engine017AmbitionInvariantTests
{
    [Fact]
    public void EvolutionSystem_TypeDuplique_EstRejete()
    {
        Assert.Throws<ArgumentException>(() => new AmbitionEvolutionSystem(
            new IAmbitionRule[]
            {
                new TestRule("dup"),
                new TestRule("dup"),
            }));
    }

    [Fact]
    public void IntentSource_TypeDuplique_EstRejete()
    {
        Assert.Throws<ArgumentException>(() => new AmbitionIntentSource(
            new IAmbitionRule[]
            {
                new TestRule("dup"),
                new TestRule("dup"),
            }));
    }

    [Fact]
    public void Creation_TypeDifferentDeLaRegle_EstRejete()
    {
        var world = new World(42);
        world.Spawn();
        var rule = new TestRule("type-a")
        {
            Candidates = (_, _, _) => new[]
            {
                new AmbitionCreationCandidate(
                    "type-b",
                    "goal-1",
                    "payload",
                    "agir",
                    50d),
            },
        };
        var system = new AmbitionEvolutionSystem(new[] { rule });

        Assert.Throws<InvalidOperationException>(
            () => system.Update(world, Tick.Zero));
    }

    [Fact]
    public void Creation_IntensiteInitialeInvalide_EstRejetee()
    {
        var world = new World(42);
        world.Spawn();
        var rule = new TestRule("type-a")
        {
            Candidates = (_, _, _) => new[]
            {
                new AmbitionCreationCandidate(
                    "type-a",
                    "goal-1",
                    "payload",
                    "agir",
                    0d),
            },
        };
        var system = new AmbitionEvolutionSystem(new[] { rule });

        Assert.Throws<InvalidOperationException>(
            () => system.Update(world, Tick.Zero));
    }

    [Fact]
    public void Evaluation_ProgressNonFini_EstRejete()
    {
        var world = new World(42);
        var actor = world.Spawn();
        actor.Set(new AmbitionComponent
        {
            Ambitions =
            {
                new AmbitionState(
                    "type-a",
                    "goal-1",
                    "payload",
                    "agir",
                    50d,
                    20d,
                    false,
                    Tick.Zero),
            },
        });
        var rule = new TestRule("type-a")
        {
            Evaluation = (_, _, _, _) => new AmbitionEvaluation(double.NaN, false),
        };
        var system = new AmbitionEvolutionSystem(new[] { rule });

        Assert.Throws<InvalidOperationException>(
            () => system.Update(world, Tick.Zero));
    }

    [Fact]
    public void IntentSource_EtatPersistantHorsBornes_EstRejete()
    {
        var world = new World(42);
        var actor = world.Spawn();
        actor.Set(new AmbitionComponent
        {
            Ambitions =
            {
                new AmbitionState(
                    "type-a",
                    "goal-1",
                    "payload",
                    "agir",
                    50d,
                    120d,
                    false,
                    Tick.Zero),
            },
        });
        var source = new AmbitionIntentSource(new[] { new TestRule("type-a") });

        Assert.Throws<InvalidOperationException>(
            () => source.CreateIntent(actor, world, Tick.Zero));
    }

    private sealed class TestRule : IAmbitionRule
    {
        public string AmbitionTypeId { get; }

        public Func<Entity, World, Tick, IReadOnlyList<AmbitionCreationCandidate>> Candidates { get; set; }
            = (_, _, _) => Array.Empty<AmbitionCreationCandidate>();

        public Func<AmbitionState, Entity, World, Tick, AmbitionEvaluation> Evaluation { get; set; }
            = (ambition, _, _, _) => new AmbitionEvaluation(ambition.Progress, false);

        public TestRule(string ambitionTypeId)
        {
            AmbitionTypeId = ambitionTypeId;
        }

        public IReadOnlyList<AmbitionCreationCandidate> FindCreationCandidates(
            Entity actor,
            World world,
            Tick currentTick)
            => Candidates(actor, world, currentTick);

        public AmbitionEvaluation Evaluate(
            AmbitionState ambition,
            Entity actor,
            World world,
            Tick currentTick)
            => Evaluation(ambition, actor, world, currentTick);

        public bool IsIntentTreatable(
            AmbitionState ambition,
            Entity actor,
            World world,
            Tick currentTick)
            => true;
    }
}
