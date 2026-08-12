namespace Chroniques.Simulation.Tests;

using Xunit;
using Chroniques.Simulation.Actions;
using Chroniques.Simulation.Actions.Exemples;
using Chroniques.Simulation.Autonomy;
using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;
using Chroniques.Simulation.Persistence;
using Chroniques.Simulation.Systems;

public sealed class Engine017AmbitionTests
{
    [Fact]
    public void Persistence_PreserveAmbitions()
    {
        var world = new World(42);
        var actor = world.Spawn();
        actor.Set(new AmbitionComponent
        {
            Ambitions =
            {
                new AmbitionState(
                    "test.goal",
                    "goal-1",
                    "{\"target\":42}",
                    "se_reposer",
                    75d,
                    35d,
                    false,
                    new Tick(3)),
                new AmbitionState(
                    "test.goal",
                    "goal-2",
                    "done",
                    "se_reposer",
                    50d,
                    100d,
                    false,
                    new Tick(4)),
            },
        });

        var json = WorldRepository.Save(world);
        var loaded = WorldRepository.Load(json);

        Assert.True(loaded.TryGetEntity(actor.Id, out var restored));
        Assert.True(restored.TryGet<AmbitionComponent>(out var component));
        Assert.Equal(2, component.Ambitions.Count);

        var first = component.Ambitions[0];
        Assert.Equal("test.goal", first.AmbitionTypeId);
        Assert.Equal("goal-1", first.InstanceKey);
        Assert.Equal("{\"target\":42}", first.ObjectivePayload);
        Assert.Equal(75d, first.Intensity);
        Assert.Equal(35d, first.Progress);
        Assert.False(first.IsAbandoned);
        Assert.Equal(new Tick(3), first.CreatedAt);
    }

    [Fact]
    public void Persistence_SansAmbitionComponent_OmetLeChampAmbitions()
    {
        var world = new World(42);
        world.Spawn();

        var json = WorldRepository.Save(world);

        Assert.DoesNotContain("\"Ambitions\":", json);
    }

    [Fact]
    public void Evolution_SansCandidate_NeCreePasComponent()
    {
        var world = new World(42);
        var actor = world.Spawn();
        var rule = new TestAmbitionRule("test.goal");
        var system = new AmbitionEvolutionSystem(new[] { rule });

        system.Update(world, Tick.Zero);

        Assert.False(actor.Has<AmbitionComponent>());
    }

    [Fact]
    public void Evolution_CandidateValide_CreeEtEvalueAuMemeTick()
    {
        var world = new World(42);
        var actor = world.Spawn();
        var rule = new TestAmbitionRule("test.goal")
        {
            Candidates = (_, _, _) => new[]
            {
                Candidate("test.goal", "goal-1", intensity: 70d),
            },
            Evaluation = (_, _, _, _) => new AmbitionEvaluation(42d, false),
        };
        var system = new AmbitionEvolutionSystem(new[] { rule });

        system.Update(world, new Tick(5));

        Assert.True(actor.TryGet<AmbitionComponent>(out var component));
        var ambition = Assert.Single(component.Ambitions);
        Assert.Equal("goal-1", ambition.InstanceKey);
        Assert.Equal(70d, ambition.Intensity);
        Assert.Equal(42d, ambition.Progress);
        Assert.Equal(new Tick(5), ambition.CreatedAt);
        Assert.Equal(1, rule.EvaluationCalls);
    }

    [Fact]
    public void Evolution_InstanceExistante_NeCreePasDoublon()
    {
        var world = new World(42);
        var actor = world.Spawn();
        var rule = new TestAmbitionRule("test.goal")
        {
            Candidates = (_, _, _) => new[]
            {
                Candidate("test.goal", "goal-1", intensity: 70d),
            },
        };
        var system = new AmbitionEvolutionSystem(new[] { rule });

        system.Update(world, Tick.Zero);
        system.Update(world, new Tick(1));

        Assert.True(actor.TryGet<AmbitionComponent>(out var component));
        Assert.Single(component.Ambitions);
    }

    [Fact]
    public void Evolution_RegleAbsente_ConserveAmbitionSansEvaluer()
    {
        var world = new World(42);
        var ambition = State("absent", "goal-1", intensity: 60d, progress: 30d);
        var actor = CreateActorWithAmbitions(world, ambition);
        var system = new AmbitionEvolutionSystem(Array.Empty<IAmbitionRule>());

        system.Update(world, new Tick(10));

        Assert.True(actor.TryGet<AmbitionComponent>(out var component));
        Assert.Equal(ambition, Assert.Single(component.Ambitions));
    }

    [Fact]
    public void Evolution_ProgressNegatif_ClampAZero()
    {
        var world = new World(42);
        var actor = CreateActorWithAmbitions(
            world,
            State("test.goal", "goal-1", progress: 30d));
        var rule = new TestAmbitionRule("test.goal")
        {
            Evaluation = (_, _, _, _) => new AmbitionEvaluation(-25d, false),
        };
        var system = new AmbitionEvolutionSystem(new[] { rule });

        system.Update(world, Tick.Zero);

        Assert.True(actor.TryGet<AmbitionComponent>(out var component));
        Assert.Equal(0d, Assert.Single(component.Ambitions).Progress);
    }

    [Fact]
    public void Evolution_ProgressSuperieur100_ClampA100EtAccomplit()
    {
        var world = new World(42);
        var actor = CreateActorWithAmbitions(
            world,
            State("test.goal", "goal-1", progress: 80d));
        var rule = new TestAmbitionRule("test.goal")
        {
            Evaluation = (_, _, _, _) => new AmbitionEvaluation(140d, false),
        };
        var system = new AmbitionEvolutionSystem(new[] { rule });

        system.Update(world, Tick.Zero);

        Assert.True(actor.TryGet<AmbitionComponent>(out var component));
        Assert.Equal(100d, Assert.Single(component.Ambitions).Progress);

        var source = new AmbitionIntentSource(new[] { rule });
        Assert.Null(source.CreateIntent(actor, world, Tick.Zero));
    }

    [Fact]
    public void Evolution_ShouldAbandon_MarqueAmbition()
    {
        var world = new World(42);
        var actor = CreateActorWithAmbitions(
            world,
            State("test.goal", "goal-1", progress: 20d));
        var rule = new TestAmbitionRule("test.goal")
        {
            Evaluation = (_, _, _, _) => new AmbitionEvaluation(25d, true),
        };
        var system = new AmbitionEvolutionSystem(new[] { rule });

        system.Update(world, Tick.Zero);

        Assert.True(actor.TryGet<AmbitionComponent>(out var component));
        var ambition = Assert.Single(component.Ambitions);
        Assert.True(ambition.IsAbandoned);
        Assert.Equal(25d, ambition.Progress);
    }

    [Fact]
    public void Evolution_IntensiteZero_SupprimeAmbition()
    {
        var world = new World(42);
        var actor = CreateActorWithAmbitions(
            world,
            State("test.goal", "goal-1", intensity: 0d));
        var rule = new TestAmbitionRule("test.goal");
        var system = new AmbitionEvolutionSystem(new[] { rule });

        system.Update(world, Tick.Zero);

        Assert.True(actor.TryGet<AmbitionComponent>(out var component));
        Assert.Empty(component.Ambitions);
    }

    [Fact]
    public void Evolution_NavanceJamaisLeTick()
    {
        var world = new World(42);
        world.Spawn();
        var system = new AmbitionEvolutionSystem(Array.Empty<IAmbitionRule>());

        system.Update(world, new Tick(10));

        Assert.Equal(Tick.Zero, world.CurrentTick);
    }

    [Fact]
    public void IntentSource_SansComponent_RetourneNull()
    {
        var world = new World(42);
        var actor = world.Spawn();
        var source = new AmbitionIntentSource(
            new[] { new TestAmbitionRule("test.goal") });

        Assert.Null(source.CreateIntent(actor, world, Tick.Zero));
    }

    [Fact]
    public void IntentSource_RegleAbsente_RetourneNull()
    {
        var world = new World(42);
        var actor = CreateActorWithAmbitions(
            world,
            State("absent", "goal-1"));
        var source = new AmbitionIntentSource(Array.Empty<IAmbitionRule>());

        Assert.Null(source.CreateIntent(actor, world, Tick.Zero));
    }

    [Fact]
    public void IntentSource_IntensiteZero_RetourneNull()
    {
        var world = new World(42);
        var actor = CreateActorWithAmbitions(
            world,
            State("test.goal", "goal-1", intensity: 0d));
        var source = new AmbitionIntentSource(
            new[] { new TestAmbitionRule("test.goal") });

        Assert.Null(source.CreateIntent(actor, world, Tick.Zero));
    }

    [Fact]
    public void IntentSource_Accomplie_RetourneNull()
    {
        var world = new World(42);
        var actor = CreateActorWithAmbitions(
            world,
            State("test.goal", "goal-1", progress: 100d));
        var source = new AmbitionIntentSource(
            new[] { new TestAmbitionRule("test.goal") });

        Assert.Null(source.CreateIntent(actor, world, Tick.Zero));
    }

    [Fact]
    public void IntentSource_Abandonnee_RetourneNull()
    {
        var world = new World(42);
        var actor = CreateActorWithAmbitions(
            world,
            State("test.goal", "goal-1", abandoned: true));
        var source = new AmbitionIntentSource(
            new[] { new TestAmbitionRule("test.goal") });

        Assert.Null(source.CreateIntent(actor, world, Tick.Zero));
    }

    [Fact]
    public void IntentSource_NonTraitable_IgnoreEtChoisitAutreCandidate()
    {
        var world = new World(42);
        var actor = CreateActorWithAmbitions(
            world,
            State("blocked", "goal-a", objective: "bloque", intensity: 90d),
            State("open", "goal-b", objective: "ouvert", intensity: 50d));
        var blocked = new TestAmbitionRule("blocked")
        {
            Treatable = (_, _, _, _) => false,
        };
        var open = new TestAmbitionRule("open");
        var source = new AmbitionIntentSource(new IAmbitionRule[] { blocked, open });

        var intent = source.CreateIntent(actor, world, Tick.Zero);

        Assert.NotNull(intent);
        Assert.Equal("ouvert", intent!.Objectif);
    }

    [Fact]
    public void IntentSource_ChoisitIntensitePlusElevee()
    {
        var world = new World(42);
        var actor = CreateActorWithAmbitions(
            world,
            State("test.goal", "low", objective: "faible", intensity: 20d, progress: 90d),
            State("test.goal", "high", objective: "fort", intensity: 80d, progress: 10d));
        var source = new AmbitionIntentSource(
            new[] { new TestAmbitionRule("test.goal") });

        var intent = source.CreateIntent(actor, world, Tick.Zero);

        Assert.Equal("fort", intent!.Objectif);
    }

    [Fact]
    public void IntentSource_EgaliteIntensite_ChoisitProgresPlusEleve()
    {
        var world = new World(42);
        var actor = CreateActorWithAmbitions(
            world,
            State("test.goal", "far", objective: "loin", intensity: 60d, progress: 20d),
            State("test.goal", "near", objective: "proche", intensity: 60d, progress: 70d));
        var source = new AmbitionIntentSource(
            new[] { new TestAmbitionRule("test.goal") });

        var intent = source.CreateIntent(actor, world, Tick.Zero);

        Assert.Equal("proche", intent!.Objectif);
    }

    [Fact]
    public void IntentSource_EgaliteIntensiteEtProgres_ChoisitPlusAncienne()
    {
        var world = new World(42);
        var actor = CreateActorWithAmbitions(
            world,
            State("test.goal", "recent", objective: "recente", createdAt: new Tick(8)),
            State("test.goal", "old", objective: "ancienne", createdAt: new Tick(2)));
        var source = new AmbitionIntentSource(
            new[] { new TestAmbitionRule("test.goal") });

        var intent = source.CreateIntent(actor, world, Tick.Zero);

        Assert.Equal("ancienne", intent!.Objectif);
    }

    [Fact]
    public void IntentSource_EgaliteTotale_RespecteOrdrePersistant()
    {
        var world = new World(42);
        var actor = CreateActorWithAmbitions(
            world,
            State("test.goal", "first", objective: "premiere"),
            State("test.goal", "second", objective: "seconde"));
        var source = new AmbitionIntentSource(
            new[] { new TestAmbitionRule("test.goal") });

        var intent = source.CreateIntent(actor, world, Tick.Zero);

        Assert.Equal("premiere", intent!.Objectif);
    }

    [Fact]
    public void IntentSource_NeMuteNiWorldNiAmbition()
    {
        var world = new World(42);
        var state = State("test.goal", "goal-1", objective: "agir", intensity: 70d, progress: 40d);
        var actor = CreateActorWithAmbitions(world, state);
        var source = new AmbitionIntentSource(
            new[] { new TestAmbitionRule("test.goal") });
        var tickBefore = world.CurrentTick;
        var eventsBefore = world.Events.Count;

        var intent = source.CreateIntent(actor, world, Tick.Zero);

        Assert.NotNull(intent);
        Assert.Equal(tickBefore, world.CurrentTick);
        Assert.Equal(eventsBefore, world.Events.Count);
        Assert.True(actor.TryGet<AmbitionComponent>(out var component));
        Assert.Equal(state, Assert.Single(component.Ambitions));
    }

    [Fact]
    public void Composite_SourcePlusHautePreempteAmbition()
    {
        var world = new World(42);
        var actor = CreateActorWithAmbitions(
            world,
            State("test.goal", "goal-1", objective: "ambition"));
        var ambition = new AmbitionIntentSource(
            new[] { new TestAmbitionRule("test.goal") });
        var composite = new CompositeAutonomousIntentSource(
            new FixedIntentSource("prioritaire"),
            ambition);

        var intent = composite.CreateIntent(actor, world, Tick.Zero);

        Assert.Equal("prioritaire", intent!.Objectif);
    }

    [Fact]
    public void Composite_SourcePrecedenteNull_TombeSurAmbition()
    {
        var world = new World(42);
        var actor = CreateActorWithAmbitions(
            world,
            State("test.goal", "goal-1", objective: "ambition"));
        var ambition = new AmbitionIntentSource(
            new[] { new TestAmbitionRule("test.goal") });
        var composite = new CompositeAutonomousIntentSource(
            new NullIntentSource(),
            ambition);

        var intent = composite.CreateIntent(actor, world, Tick.Zero);

        Assert.Equal("ambition", intent!.Objectif);
    }

    [Fact]
    public void EndToEnd_TickCreeAmbitionPuisProduitIntentEtAction()
    {
        var world = new World(42);
        var actor = world.Spawn();
        actor.Set(new NeedsComponent { Fatigue = 50d });

        var rule = new TestAmbitionRule("test.rest")
        {
            Candidates = (_, _, tick) => tick.Value == 1
                ? new[]
                {
                    new AmbitionCreationCandidate(
                        "test.rest",
                        "rest-1",
                        "",
                        NeedsIntentSource.RestObjective,
                        80d),
                }
                : Array.Empty<AmbitionCreationCandidate>(),
            Evaluation = (_, _, _, _) => new AmbitionEvaluation(25d, false),
        };

        var evolution = new AmbitionEvolutionSystem(new[] { rule });
        var ambitionSource = new AmbitionIntentSource(new[] { rule });
        var source = new CompositeAutonomousIntentSource(
            new NullIntentSource(),
            ambitionSource);

        var foodResolver = new NoFoodResolver();
        var pipeline = new PipelineRunner(
            new NeedsPlanner(foodResolver),
            new NeedsExecutionEngine(foodResolver));
        var executor = new PipelineAutonomousIntentExecutor(pipeline);
        var autonomy = new AutonomousActionSystem(source, executor);
        autonomy.RegisterActor(actor.Id);

        var scheduler = new Scheduler();
        scheduler.Register(evolution);
        scheduler.Register(autonomy);

        scheduler.Tick(world);

        Assert.Equal(new Tick(1), world.CurrentTick);
        Assert.True(actor.TryGet<AmbitionComponent>(out var ambitions));
        var ambition = Assert.Single(ambitions.Ambitions);
        Assert.Equal(25d, ambition.Progress);
        Assert.True(actor.TryGet<NeedsComponent>(out var needs));
        Assert.Equal(70d, needs.Fatigue);
    }

    private static AmbitionCreationCandidate Candidate(
        string typeId,
        string instanceKey,
        double intensity = 60d)
        => new(
            typeId,
            instanceKey,
            "payload",
            "se_reposer",
            intensity);

    private static AmbitionState State(
        string typeId,
        string instanceKey,
        string objective = "se_reposer",
        double intensity = 60d,
        double progress = 20d,
        bool abandoned = false,
        Tick? createdAt = null)
        => new(
            typeId,
            instanceKey,
            "payload",
            objective,
            intensity,
            progress,
            abandoned,
            createdAt ?? Tick.Zero);

    private static Entity CreateActorWithAmbitions(
        World world,
        params AmbitionState[] ambitions)
    {
        var actor = world.Spawn();
        actor.Set(new AmbitionComponent
        {
            Ambitions = ambitions.ToList(),
        });
        return actor;
    }

    private sealed class TestAmbitionRule : IAmbitionRule
    {
        public string AmbitionTypeId { get; }

        public Func<Entity, World, Tick, IReadOnlyList<AmbitionCreationCandidate>> Candidates { get; set; }
            = (_, _, _) => Array.Empty<AmbitionCreationCandidate>();

        public Func<AmbitionState, Entity, World, Tick, AmbitionEvaluation> Evaluation { get; set; }
            = (ambition, _, _, _) => new AmbitionEvaluation(ambition.Progress, false);

        public Func<AmbitionState, Entity, World, Tick, bool> Treatable { get; set; }
            = (_, _, _, _) => true;

        public int EvaluationCalls { get; private set; }

        public TestAmbitionRule(string ambitionTypeId)
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
        {
            EvaluationCalls++;
            return Evaluation(ambition, actor, world, currentTick);
        }

        public bool IsIntentTreatable(
            AmbitionState ambition,
            Entity actor,
            World world,
            Tick currentTick)
            => Treatable(ambition, actor, world, currentTick);
    }

    private sealed class FixedIntentSource : IAutonomousIntentSource
    {
        private readonly string _objective;

        public FixedIntentSource(string objective)
        {
            _objective = objective;
        }

        public Intent? CreateIntent(Entity actor, World world, Tick currentTick)
            => new Intent(actor.Id, _objective, 1);
    }

    private sealed class NullIntentSource : IAutonomousIntentSource
    {
        public Intent? CreateIntent(Entity actor, World world, Tick currentTick)
            => null;
    }

    private sealed class NoFoodResolver : IAccessibleFoodResolver
    {
        public EntityId? FindAccessibleFood(
            Entity actor,
            World world,
            Tick currentTick)
            => null;

        public bool IsAccessible(
            Entity actor,
            EntityId foodId,
            World world,
            Tick currentTick)
            => false;
    }
}
