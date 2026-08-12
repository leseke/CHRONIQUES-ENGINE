namespace Chroniques.Simulation.Tests;

using Xunit;
using Chroniques.Simulation.Actions;
using Chroniques.Simulation.Actions.Exemples;
using Chroniques.Simulation.Autonomy;
using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;
using Chroniques.Simulation.Persistence;
using Chroniques.Simulation.Systems;

public sealed class Engine016HabitTests
{
    [Fact]
    public void Persistence_PreserveHabitudesEtTracesDeFormation()
    {
        var world = new World(42);
        var actor = world.Spawn();
        actor.Set(new HabitComponent
        {
            Habits =
            {
                new HabitState(
                    "test.repos",
                    NeedsIntentSource.RestObjective,
                    "soir",
                    55,
                    new Tick(7),
                    new Tick(3)),
            },
            FormationTraces =
            {
                new HabitFormationTrace
                {
                    HabitTypeId = "test.travail",
                    IntentObjective = "produire_denree",
                    FormationSignature = "atelier",
                    ObservedAt = new List<Tick> { new(5), new(6) },
                },
            },
        });

        var json = WorldRepository.Save(world);
        var loaded = WorldRepository.Load(json);

        Assert.True(loaded.TryGetEntity(actor.Id, out var restored));
        Assert.True(restored.TryGet<HabitComponent>(out var habits));

        var habit = Assert.Single(habits.Habits);
        Assert.Equal("test.repos", habit.HabitTypeId);
        Assert.Equal(55, habit.Force);
        Assert.Equal(new Tick(7), habit.LastActivatedAt!.Value);

        var trace = Assert.Single(habits.FormationTraces);
        Assert.Equal("atelier", trace.FormationSignature);
        Assert.Equal(new[] { new Tick(5), new Tick(6) }, trace.ObservedAt);
    }

    [Fact]
    public void Persistence_SansHabitComponent_OmetLeChampHabits()
    {
        var world = new World(42);
        world.Spawn();

        var json = WorldRepository.Save(world);

        Assert.DoesNotContain("\"Habits\":", json);
    }

    [Fact]
    public void HabitIntentSource_SansComponent_RetourneNull()
    {
        var world = new World(42);
        var actor = world.Spawn();
        var registry = new HabitSelectionRegistry();
        var source = new HabitIntentSource(
            new[] { new TestHabitRule("test.repos") },
            registry);

        var intent = source.CreateIntent(actor, world, Tick.Zero);

        Assert.Null(intent);
    }

    [Fact]
    public void HabitIntentSource_RegleAbsente_RetourneNull()
    {
        var world = new World(42);
        var actor = CreateActorWithHabit(
            world,
            new HabitState("absent", "agir", "ctx", 50, null, Tick.Zero));
        var source = new HabitIntentSource(
            Array.Empty<IHabitRule>(),
            new HabitSelectionRegistry());

        Assert.Null(source.CreateIntent(actor, world, Tick.Zero));
    }

    [Fact]
    public void HabitIntentSource_DeclencheurFaux_RetourneNull()
    {
        var world = new World(42);
        var actor = CreateActorWithHabit(
            world,
            new HabitState("test.repos", "agir", "ctx", 50, null, Tick.Zero));
        var rule = new TestHabitRule("test.repos")
        {
            Trigger = (_, _, _, _) => false,
        };
        var source = new HabitIntentSource(
            new[] { rule },
            new HabitSelectionRegistry());

        Assert.Null(source.CreateIntent(actor, world, Tick.Zero));
    }

    [Fact]
    public void HabitIntentSource_IntentNonTraitable_RetourneNull()
    {
        var world = new World(42);
        var actor = CreateActorWithHabit(
            world,
            new HabitState("test.repos", "agir", "ctx", 50, null, Tick.Zero));
        var rule = new TestHabitRule("test.repos")
        {
            Treatable = (_, _, _, _) => false,
        };
        var source = new HabitIntentSource(
            new[] { rule },
            new HabitSelectionRegistry());

        Assert.Null(source.CreateIntent(actor, world, Tick.Zero));
    }

    [Fact]
    public void HabitIntentSource_ForceNulle_RetourneNull()
    {
        var world = new World(42);
        var actor = CreateActorWithHabit(
            world,
            new HabitState("test.repos", "agir", "ctx", 0, null, Tick.Zero));
        var source = new HabitIntentSource(
            new[] { new TestHabitRule("test.repos") },
            new HabitSelectionRegistry());

        Assert.Null(source.CreateIntent(actor, world, Tick.Zero));
    }

    [Fact]
    public void HabitIntentSource_ChoisitForceLaPlusElevee()
    {
        var world = new World(42);
        var actor = world.Spawn();
        actor.Set(new HabitComponent
        {
            Habits =
            {
                new HabitState("test.repos", "faible", "a", 20, null, Tick.Zero),
                new HabitState("test.repos", "fort", "b", 80, null, new Tick(1)),
            },
        });
        var registry = new HabitSelectionRegistry();
        var source = new HabitIntentSource(
            new[] { new TestHabitRule("test.repos") },
            registry);

        var intent = source.CreateIntent(actor, world, Tick.Zero);

        Assert.NotNull(intent);
        Assert.Equal("fort", intent!.Objectif);
        Assert.True(registry.TryGet(actor.Id, out var selected));
        Assert.Equal("b", selected.FormationSignature);
    }

    [Fact]
    public void HabitIntentSource_EgaliteForce_ChoisitPlusAncienne()
    {
        var world = new World(42);
        var actor = world.Spawn();
        actor.Set(new HabitComponent
        {
            Habits =
            {
                new HabitState("test.repos", "recente", "a", 50, null, new Tick(5)),
                new HabitState("test.repos", "ancienne", "b", 50, null, new Tick(2)),
            },
        });
        var source = new HabitIntentSource(
            new[] { new TestHabitRule("test.repos") },
            new HabitSelectionRegistry());

        var intent = source.CreateIntent(actor, world, Tick.Zero);

        Assert.Equal("ancienne", intent!.Objectif);
    }

    [Fact]
    public void HabitIntentSource_NeMutePasDerniereActivation()
    {
        var world = new World(42);
        var state = new HabitState(
            "test.repos",
            "agir",
            "ctx",
            50,
            null,
            Tick.Zero);
        var actor = CreateActorWithHabit(world, state);
        var source = new HabitIntentSource(
            new[] { new TestHabitRule("test.repos") },
            new HabitSelectionRegistry());

        var intent = source.CreateIntent(actor, world, Tick.Zero);

        Assert.NotNull(intent);
        Assert.True(actor.TryGet<HabitComponent>(out var component));
        Assert.Null(Assert.Single(component.Habits).LastActivatedAt);
    }

    [Fact]
    public void Formation_RepetitionsSuffisantes_FormeHabitude()
    {
        var world = new World(42);
        var actor = CreateActor(world, fatigue: 10);
        var rule = new TestHabitRule("test.repos");
        var observer = CreateObserver(
            rule,
            new HabitFormationParameters(2, 10, 40));
        var executor = CreateExecutor(observer);

        ExecuteRest(executor, actor, world);
        world.Advance();
        ExecuteRest(executor, actor, world);

        Assert.True(actor.TryGet<HabitComponent>(out var component));
        var habit = Assert.Single(component.Habits);
        Assert.Equal("test.repos", habit.HabitTypeId);
        Assert.Equal(NeedsIntentSource.RestObjective, habit.IntentObjective);
        Assert.Equal("ctx", habit.FormationSignature);
        Assert.Equal(40, habit.Force);
        Assert.Null(habit.LastActivatedAt);
        Assert.Equal(new Tick(1), habit.CreatedAt);
        Assert.Empty(component.FormationTraces);
    }

    [Fact]
    public void Formation_SignaturesDifferentes_NeSeCombinentPas()
    {
        var world = new World(42);
        var actor = CreateActor(world, fatigue: 10);
        var rule = new TestHabitRule("test.repos")
        {
            Signature = (_, _, _, tick) => tick.Value == 0 ? "matin" : "soir",
        };
        var observer = CreateObserver(
            rule,
            new HabitFormationParameters(2, 10, 40));
        var executor = CreateExecutor(observer);

        ExecuteRest(executor, actor, world);
        world.Advance();
        ExecuteRest(executor, actor, world);

        Assert.True(actor.TryGet<HabitComponent>(out var component));
        Assert.Empty(component.Habits);
        Assert.Equal(2, component.FormationTraces.Count);
        Assert.All(component.FormationTraces, trace => Assert.Single(trace.ObservedAt));
    }

    [Fact]
    public void Formation_HorsFenetre_EcarteAncienneObservation()
    {
        var world = new World(42);
        var actor = CreateActor(world, fatigue: 10);
        var rule = new TestHabitRule("test.repos");
        var observer = CreateObserver(
            rule,
            new HabitFormationParameters(2, 2, 40));
        var executor = CreateExecutor(observer);

        ExecuteRest(executor, actor, world);
        world.Advance();
        world.Advance();
        ExecuteRest(executor, actor, world);

        Assert.True(actor.TryGet<HabitComponent>(out var component));
        Assert.Empty(component.Habits);
        var trace = Assert.Single(component.FormationTraces);
        Assert.Equal(new[] { new Tick(2) }, trace.ObservedAt);
    }

    [Fact]
    public void Formation_HabitudeExistante_NeCreePasDoublon()
    {
        var world = new World(42);
        var actor = CreateActor(world, fatigue: 10);
        actor.Set(new HabitComponent
        {
            Habits =
            {
                new HabitState(
                    "test.repos",
                    NeedsIntentSource.RestObjective,
                    "ctx",
                    30,
                    null,
                    Tick.Zero),
            },
        });
        var rule = new TestHabitRule("test.repos");
        var observer = CreateObserver(
            rule,
            new HabitFormationParameters(1, 10, 40));
        var executor = CreateExecutor(observer);

        ExecuteRest(executor, actor, world);

        Assert.True(actor.TryGet<HabitComponent>(out var component));
        Assert.Single(component.Habits);
        Assert.Empty(component.FormationTraces);
    }

    [Fact]
    public void Formation_OutcomeEchec_CompteCommeObservationTerminee()
    {
        var world = new World(42);
        var actor = world.Spawn();
        var rule = new TestHabitRule("test.repos");
        var observer = CreateObserver(
            rule,
            new HabitFormationParameters(1, 10, 40));
        var executor = CreateExecutor(observer);

        ExecuteRest(executor, actor, world);

        Assert.True(actor.TryGet<HabitComponent>(out var component));
        Assert.Single(component.Habits);
    }

    [Fact]
    public void Formation_ExecutionAborted_NeComptePasObservation()
    {
        var world = new World(42);
        var actor = CreateActor(world, fatigue: 10);
        var rule = new TestHabitRule("test.inconnu")
        {
            ObservedObjective = "objectif_inconnu",
        };
        var observer = CreateObserver(
            rule,
            new HabitFormationParameters(1, 10, 40));
        var executor = CreateExecutor(observer);

        Assert.Throws<NotSupportedException>(() => executor.Execute(
            new Intent(actor.Id, "objectif_inconnu", 1),
            world));

        Assert.False(actor.Has<HabitComponent>());
    }

    [Fact]
    public void Activation_OutcomeEchec_MetAJourTickSansRenforcer()
    {
        var world = new World(42);
        var actor = CreateActorWithHabit(
            world,
            new HabitState(
                "test.repos",
                NeedsIntentSource.RestObjective,
                "ctx",
                50,
                null,
                Tick.Zero));
        var rule = new TestHabitRule("test.repos");
        var registry = new HabitSelectionRegistry();
        var observer = CreateObserver(
            rule,
            new HabitFormationParameters(2, 10, 40),
            registry,
            new FixedStrengthPolicy(reinforceDelta: 10, erodeDelta: 5));
        var source = new HabitIntentSource(new[] { rule }, registry);
        var executor = CreateExecutor(observer);

        var intent = source.CreateIntent(actor, world, Tick.Zero);
        executor.Execute(intent!, world);

        Assert.True(actor.TryGet<HabitComponent>(out var component));
        var habit = Assert.Single(component.Habits);
        Assert.Equal(50, habit.Force);
        Assert.Equal(Tick.Zero, habit.LastActivatedAt!.Value);
    }

    [Fact]
    public void Activation_Reussite_RenforceEtClampA100()
    {
        var world = new World(42);
        var actor = CreateActor(world, fatigue: 10);
        actor.Set(new HabitComponent
        {
            Habits =
            {
                new HabitState(
                    "test.repos",
                    NeedsIntentSource.RestObjective,
                    "ctx",
                    95,
                    null,
                    Tick.Zero),
            },
        });
        var rule = new TestHabitRule("test.repos");
        var registry = new HabitSelectionRegistry();
        var observer = CreateObserver(
            rule,
            new HabitFormationParameters(2, 10, 40),
            registry,
            new FixedStrengthPolicy(reinforceDelta: 10, erodeDelta: 5));
        var source = new HabitIntentSource(new[] { rule }, registry);
        var executor = CreateExecutor(observer);

        executor.Execute(source.CreateIntent(actor, world, Tick.Zero)!, world);

        Assert.True(actor.TryGet<HabitComponent>(out var component));
        var habit = Assert.Single(component.Habits);
        Assert.Equal(100, habit.Force);
        Assert.Equal(Tick.Zero, habit.LastActivatedAt!.Value);
    }

    [Fact]
    public void Activation_ExecutionAborted_ConserveActivationSansRenforcement()
    {
        var world = new World(42);
        var actor = CreateActor(world, fatigue: 10);
        actor.Set(new HabitComponent
        {
            Habits =
            {
                new HabitState(
                    "test.inconnu",
                    "objectif_inconnu",
                    "ctx",
                    50,
                    null,
                    Tick.Zero),
            },
        });
        var rule = new TestHabitRule("test.inconnu")
        {
            ObservedObjective = "objectif_inconnu",
        };
        var registry = new HabitSelectionRegistry();
        var observer = CreateObserver(
            rule,
            new HabitFormationParameters(2, 10, 40),
            registry,
            new FixedStrengthPolicy(reinforceDelta: 10, erodeDelta: 5));
        var source = new HabitIntentSource(new[] { rule }, registry);
        var executor = CreateExecutor(observer);

        var intent = source.CreateIntent(actor, world, Tick.Zero)!;
        Assert.Throws<NotSupportedException>(() => executor.Execute(intent, world));

        Assert.True(actor.TryGet<HabitComponent>(out var component));
        var habit = Assert.Single(component.Habits);
        Assert.Equal(50, habit.Force);
        Assert.Equal(Tick.Zero, habit.LastActivatedAt!.Value);
    }

    [Fact]
    public void Evolution_AvantSeuil_NerodePas()
    {
        var world = new World(42);
        var actor = CreateActorWithHabit(
            world,
            new HabitState("test.repos", "agir", "ctx", 50, new Tick(1), Tick.Zero));
        var system = new HabitEvolutionSystem(
            inactivityThresholdTicks: 2,
            new FixedStrengthPolicy(reinforceDelta: 10, erodeDelta: 5));

        system.Update(world, new Tick(3));

        Assert.True(actor.TryGet<HabitComponent>(out var component));
        Assert.Equal(50, Assert.Single(component.Habits).Force);
    }

    [Fact]
    public void Evolution_ApresSeuil_Erode()
    {
        var world = new World(42);
        var actor = CreateActorWithHabit(
            world,
            new HabitState("test.repos", "agir", "ctx", 50, new Tick(1), Tick.Zero));
        var system = new HabitEvolutionSystem(
            inactivityThresholdTicks: 2,
            new FixedStrengthPolicy(reinforceDelta: 10, erodeDelta: 5));

        system.Update(world, new Tick(4));

        Assert.True(actor.TryGet<HabitComponent>(out var component));
        Assert.Equal(45, Assert.Single(component.Habits).Force);
    }

    [Fact]
    public void Evolution_ForceAtteintZero_SupprimeHabitude()
    {
        var world = new World(42);
        var actor = CreateActorWithHabit(
            world,
            new HabitState("test.repos", "agir", "ctx", 4, new Tick(1), Tick.Zero));
        var system = new HabitEvolutionSystem(
            inactivityThresholdTicks: 1,
            new FixedStrengthPolicy(reinforceDelta: 10, erodeDelta: 10));

        system.Update(world, new Tick(3));

        Assert.True(actor.TryGet<HabitComponent>(out var component));
        Assert.Empty(component.Habits);
    }

    [Fact]
    public void Evolution_JamaisActivee_UtiliseCreatedAt()
    {
        var world = new World(42);
        var actor = CreateActorWithHabit(
            world,
            new HabitState("test.repos", "agir", "ctx", 50, null, new Tick(2)));
        var system = new HabitEvolutionSystem(
            inactivityThresholdTicks: 2,
            new FixedStrengthPolicy(reinforceDelta: 10, erodeDelta: 5));

        system.Update(world, new Tick(5));

        Assert.True(actor.TryGet<HabitComponent>(out var component));
        Assert.Equal(45, Assert.Single(component.Habits).Force);
    }

    [Fact]
    public void EndToEnd_RepetitionFormeHabitude_PuisHabitudeProduitIntentEtAction()
    {
        var world = new World(42);
        var actor = CreateActor(world, fatigue: 10);
        var rule = new TestHabitRule("test.repos")
        {
            Trigger = (_, _, _, tick) => tick.Value == 3,
        };
        var registry = new HabitSelectionRegistry();
        var observer = CreateObserver(
            rule,
            new HabitFormationParameters(2, 10, 40),
            registry,
            new FixedStrengthPolicy(reinforceDelta: 10, erodeDelta: 5));
        var executor = CreateExecutor(observer);

        ExecuteRest(executor, actor, world);
        world.Advance();
        ExecuteRest(executor, actor, world);

        Assert.True(actor.TryGet<HabitComponent>(out var formed));
        Assert.Single(formed.Habits);
        Assert.Equal(50, actor.TryGet<NeedsComponent>(out var before) ? before.Fatigue : -1);

        world.Advance();

        var habitSource = new HabitIntentSource(new[] { rule }, registry);
        var autonomy = new AutonomousActionSystem(habitSource, executor);
        autonomy.RegisterActor(actor.Id);
        var scheduler = new Scheduler();
        scheduler.Register(autonomy);

        scheduler.Tick(world);

        Assert.Equal(new Tick(3), world.CurrentTick);
        Assert.True(actor.TryGet<NeedsComponent>(out var after));
        Assert.Equal(70, after.Fatigue);
        Assert.True(actor.TryGet<HabitComponent>(out var finalHabits));
        var habit = Assert.Single(finalHabits.Habits);
        Assert.Equal(50, habit.Force);
        Assert.Equal(new Tick(3), habit.LastActivatedAt!.Value);
    }

    private static Entity CreateActor(World world, double fatigue)
    {
        var actor = world.Spawn();
        actor.Set(new NeedsComponent { Fatigue = fatigue });
        return actor;
    }

    private static Entity CreateActorWithHabit(World world, HabitState habit)
    {
        var actor = world.Spawn();
        actor.Set(new HabitComponent
        {
            Habits = { habit },
        });
        return actor;
    }

    private static HabitLearningObserver CreateObserver(
        TestHabitRule rule,
        HabitFormationParameters parameters,
        HabitSelectionRegistry? registry = null,
        IHabitStrengthPolicy? strengthPolicy = null)
        => new(
            new[] { rule },
            new FixedFormationParameterResolver(parameters),
            strengthPolicy ?? new FixedStrengthPolicy(10, 5),
            registry ?? new HabitSelectionRegistry());

    private static PipelineAutonomousIntentExecutor CreateExecutor(
        IAutonomousIntentExecutionObserver observer)
    {
        var foodResolver = new NoFoodResolver();
        var pipeline = new PipelineRunner(
            new NeedsPlanner(foodResolver),
            new NeedsExecutionEngine(foodResolver));

        return new PipelineAutonomousIntentExecutor(pipeline, observer);
    }

    private static void ExecuteRest(
        PipelineAutonomousIntentExecutor executor,
        Entity actor,
        World world)
        => executor.Execute(
            new Intent(actor.Id, NeedsIntentSource.RestObjective, 1),
            world);

    private sealed class TestHabitRule : IHabitRule
    {
        public string HabitTypeId { get; }
        public string ObservedObjective { get; set; } = NeedsIntentSource.RestObjective;

        public Func<Intent, Entity, World, Tick, string?> Signature { get; set; }
            = (_, _, _, _) => "ctx";

        public Func<HabitState, Entity, World, Tick, bool> Trigger { get; set; }
            = (_, _, _, tick) => tick.Value % 2 == 0;

        public Func<HabitState, Entity, World, Tick, bool> Treatable { get; set; }
            = (_, _, _, _) => true;

        public TestHabitRule(string habitTypeId)
        {
            HabitTypeId = habitTypeId;
        }

        public HabitFormationCandidate? ObserveFormation(
            Intent intent,
            Entity actor,
            World world,
            Tick currentTick)
        {
            if (!string.Equals(intent.Objectif, ObservedObjective, StringComparison.Ordinal))
            {
                return null;
            }

            var signature = Signature(intent, actor, world, currentTick);
            return string.IsNullOrWhiteSpace(signature)
                ? null
                : new HabitFormationCandidate(
                    HabitTypeId,
                    intent.Objectif,
                    signature);
        }

        public bool IsTriggered(
            HabitState habit,
            Entity actor,
            World world,
            Tick currentTick)
            => Trigger(habit, actor, world, currentTick);

        public bool IsIntentTreatable(
            HabitState habit,
            Entity actor,
            World world,
            Tick currentTick)
            => Treatable(habit, actor, world, currentTick);
    }

    private sealed class FixedFormationParameterResolver : IHabitFormationParameterResolver
    {
        private readonly HabitFormationParameters _parameters;

        public FixedFormationParameterResolver(HabitFormationParameters parameters)
        {
            _parameters = parameters;
        }

        public HabitFormationParameters Resolve(
            string habitTypeId,
            Entity actor,
            World world,
            Tick currentTick)
            => _parameters;
    }

    private sealed class FixedStrengthPolicy : IHabitStrengthPolicy
    {
        private readonly double _reinforceDelta;
        private readonly double _erodeDelta;

        public FixedStrengthPolicy(double reinforceDelta, double erodeDelta)
        {
            _reinforceDelta = reinforceDelta;
            _erodeDelta = erodeDelta;
        }

        public double Reinforce(
            HabitState habit,
            Entity actor,
            World world,
            Tick currentTick)
            => habit.Force + _reinforceDelta;

        public double Erode(
            HabitState habit,
            Entity actor,
            World world,
            Tick currentTick)
            => habit.Force - _erodeDelta;
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
