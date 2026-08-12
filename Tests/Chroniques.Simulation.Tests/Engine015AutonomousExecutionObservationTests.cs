namespace Chroniques.Simulation.Tests;

using Xunit;
using Chroniques.Simulation.Actions;
using Chroniques.Simulation.Actions.Exemples;
using Chroniques.Simulation.Autonomy;
using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;
using Chroniques.Simulation.Systems;

public sealed class Engine015AutonomousExecutionObservationTests
{
    [Fact]
    public void Execute_SansObserver_ExecutePipelineEtAppliqueEffects()
    {
        var world = new World(42);
        var actor = CreateActor(world, fatigue: 50);
        var executor = CreateExecutor();

        executor.Execute(
            new Intent(actor.Id, NeedsIntentSource.RestObjective, 1),
            world);

        Assert.True(actor.TryGet<NeedsComponent>(out var needs));
        Assert.Equal(70d, needs.Fatigue);
    }

    [Fact]
    public void Execute_ActeurAbsent_RejeteAvantObservation()
    {
        var world = new World(42);
        var observer = new RecordingObserver();
        var executor = CreateExecutor(observer);

        Assert.Throws<InvalidOperationException>(() => executor.Execute(
            new Intent(EntityId.New(), NeedsIntentSource.RestObjective, 1),
            world));

        Assert.Empty(observer.Events);
    }

    [Fact]
    public void BeforeExecution_VoitWorldAvantEffects()
    {
        var world = new World(42);
        var actor = CreateActor(world, fatigue: 50);
        var observer = new RecordingObserver();
        var executor = CreateExecutor(observer);

        executor.Execute(
            new Intent(actor.Id, NeedsIntentSource.RestObjective, 1),
            world);

        Assert.Equal(50d, observer.FatigueBefore);
    }

    [Fact]
    public void AfterExecution_VoitActionArchiveeEtWorldApresEffects()
    {
        var world = new World(42);
        var actor = CreateActor(world, fatigue: 50);
        var observer = new RecordingObserver();
        var executor = CreateExecutor(observer);

        executor.Execute(
            new Intent(actor.Id, NeedsIntentSource.RestObjective, 1),
            world);

        Assert.NotNull(observer.LastAction);
        Assert.Equal(ActionState.Archived, observer.LastAction!.State);
        Assert.Equal(OutcomeForme.Reussite, observer.LastAction.Outcome?.Forme);
        Assert.Equal(70d, observer.FatigueAfter);
    }

    [Fact]
    public void OutcomeEchec_AppelleAfterExecutionSansAppliquerEffects()
    {
        var world = new World(42);
        var actor = world.Spawn();
        var observer = new RecordingObserver();
        var executor = CreateExecutor(observer);

        executor.Execute(
            new Intent(actor.Id, NeedsIntentSource.RestObjective, 1),
            world);

        Assert.Equal(
            new[] { "before", "after" },
            observer.Events);
        Assert.NotNull(observer.LastAction);
        Assert.Equal(OutcomeForme.Echec, observer.LastAction!.Outcome?.Forme);
        Assert.Equal(ActionState.Archived, observer.LastAction.State);
    }

    [Fact]
    public void PlusieursObservers_RespectentOrdreEnregistrement()
    {
        var world = new World(42);
        var actor = CreateActor(world, fatigue: 50);
        var shared = new List<string>();
        var first = new OrderedObserver("A", shared);
        var second = new OrderedObserver("B", shared);
        var executor = CreateExecutor(first, second);

        executor.Execute(
            new Intent(actor.Id, NeedsIntentSource.RestObjective, 1),
            world);

        Assert.Equal(
            new[] { "A:before", "B:before", "A:after", "B:after" },
            shared);
    }

    [Fact]
    public void ExceptionPipeline_AppelleExecutionAborted()
    {
        var world = new World(42);
        var actor = CreateActor(world, fatigue: 50);
        var observer = new RecordingObserver();
        var executor = CreateExecutor(observer);

        Assert.Throws<NotSupportedException>(() => executor.Execute(
            new Intent(actor.Id, "objectif_inconnu", 1),
            world));

        Assert.Equal(
            new[] { "before", "aborted" },
            observer.Events);
        Assert.IsType<NotSupportedException>(observer.LastError);
    }

    [Fact]
    public void ExceptionPipeline_EstRelanceeEtAfterNestPasAppele()
    {
        var world = new World(42);
        var actor = CreateActor(world, fatigue: 50);
        var observer = new RecordingObserver();
        var executor = CreateExecutor(observer);

        var error = Assert.Throws<NotSupportedException>(() => executor.Execute(
            new Intent(actor.Id, "objectif_inconnu", 1),
            world));

        Assert.Contains("objectif_inconnu", error.Message);
        Assert.DoesNotContain("after", observer.Events);
        Assert.Null(observer.LastAction);
    }

    [Fact]
    public void Scheduler_AvecAdaptateur_ExecuteUneFoisSansTickSupplementaire()
    {
        var world = new World(42);
        var actor = CreateActor(world, fatigue: 50);
        var observer = new RecordingObserver();
        var executor = CreateExecutor(observer);
        var source = new FixedIntentSource();
        var autonomy = new AutonomousActionSystem(source, executor);
        autonomy.RegisterActor(actor.Id);

        var scheduler = new Scheduler();
        scheduler.Register(autonomy);

        scheduler.Tick(world);

        Assert.Equal(new Tick(1), world.CurrentTick);
        Assert.Equal(1, source.Calls);
        Assert.Equal(new[] { "before", "after" }, observer.Events);
        Assert.Equal(new Tick(1), observer.RequestedAt);
    }

    private static Entity CreateActor(World world, double fatigue)
    {
        var actor = world.Spawn();
        actor.Set(new NeedsComponent
        {
            Fatigue = fatigue,
            Faim = 100,
            Sante = 100,
            Moral = 100
        });
        return actor;
    }

    private static PipelineAutonomousIntentExecutor CreateExecutor(
        params IAutonomousIntentExecutionObserver[] observers)
    {
        var foodResolver = new EmptyFoodResolver();
        var pipeline = new PipelineRunner(
            new NeedsPlanner(foodResolver),
            new NeedsExecutionEngine(foodResolver));

        return new PipelineAutonomousIntentExecutor(
            pipeline,
            observers);
    }

    private sealed class EmptyFoodResolver : IAccessibleFoodResolver
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

    private sealed class RecordingObserver : IAutonomousIntentExecutionObserver
    {
        public List<string> Events { get; } = new();
        public double? FatigueBefore { get; private set; }
        public double? FatigueAfter { get; private set; }
        public ActionInstance? LastAction { get; private set; }
        public Exception? LastError { get; private set; }
        public Tick? RequestedAt { get; private set; }

        public void BeforeExecution(
            Intent intent,
            Entity actor,
            World world,
            Tick currentTick)
        {
            Events.Add("before");
            RequestedAt = currentTick;

            if (actor.TryGet<NeedsComponent>(out var needs))
            {
                FatigueBefore = needs.Fatigue;
            }
        }

        public void AfterExecution(
            Intent intent,
            Entity actor,
            ActionInstance action,
            World world,
            Tick requestedAt)
        {
            Events.Add("after");
            LastAction = action;
            RequestedAt = requestedAt;

            if (actor.TryGet<NeedsComponent>(out var needs))
            {
                FatigueAfter = needs.Fatigue;
            }
        }

        public void ExecutionAborted(
            Intent intent,
            Entity actor,
            World world,
            Tick requestedAt,
            Exception error)
        {
            Events.Add("aborted");
            LastError = error;
            RequestedAt = requestedAt;
        }
    }

    private sealed class OrderedObserver : IAutonomousIntentExecutionObserver
    {
        private readonly string _name;
        private readonly List<string> _events;

        public OrderedObserver(string name, List<string> events)
        {
            _name = name;
            _events = events;
        }

        public void BeforeExecution(
            Intent intent,
            Entity actor,
            World world,
            Tick currentTick)
            => _events.Add($"{_name}:before");

        public void AfterExecution(
            Intent intent,
            Entity actor,
            ActionInstance action,
            World world,
            Tick requestedAt)
            => _events.Add($"{_name}:after");

        public void ExecutionAborted(
            Intent intent,
            Entity actor,
            World world,
            Tick requestedAt,
            Exception error)
            => _events.Add($"{_name}:aborted");
    }

    private sealed class FixedIntentSource : IAutonomousIntentSource
    {
        public int Calls { get; private set; }

        public Intent? CreateIntent(
            Entity actor,
            World world,
            Tick currentTick)
        {
            Calls += 1;
            return new Intent(
                actor.Id,
                NeedsIntentSource.RestObjective,
                1);
        }
    }
}
