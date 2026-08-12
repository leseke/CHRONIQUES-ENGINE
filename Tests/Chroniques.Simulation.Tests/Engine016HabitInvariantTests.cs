namespace Chroniques.Simulation.Tests;

using Xunit;
using Chroniques.Simulation.Actions;
using Chroniques.Simulation.Actions.Exemples;
using Chroniques.Simulation.Autonomy;
using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;

public sealed class Engine016HabitInvariantTests
{
    [Fact]
    public void Observer_SansCandidateNiSelection_NAjoutePasComponentVide()
    {
        var world = new World(42);
        var actor = world.Spawn();
        actor.Set(new NeedsComponent { Fatigue = 50 });
        var rule = new FakeRule("test.autre", observedObjective: "autre");
        var registry = new HabitSelectionRegistry();
        var observer = CreateObserver(rule, registry, new DeltaPolicy(10, -5));
        var executor = CreateExecutor(observer);

        executor.Execute(
            new Intent(actor.Id, NeedsIntentSource.RestObjective, 1),
            world);

        Assert.False(actor.Has<HabitComponent>());
    }

    [Fact]
    public void Renforcement_PolitiqueQuiDiminueForce_EstRejeteeMaisActivationResteTracee()
    {
        var world = new World(42);
        var actor = world.Spawn();
        actor.Set(new NeedsComponent { Fatigue = 50 });
        actor.Set(new HabitComponent
        {
            Habits =
            {
                new HabitState(
                    "test.repos",
                    NeedsIntentSource.RestObjective,
                    "ctx",
                    50,
                    null,
                    Tick.Zero),
            },
        });

        var rule = new FakeRule("test.repos", NeedsIntentSource.RestObjective);
        var registry = new HabitSelectionRegistry();
        var observer = CreateObserver(rule, registry, new DeltaPolicy(-10, -5));
        var source = new HabitIntentSource(new[] { rule }, registry);
        var executor = CreateExecutor(observer);
        var intent = source.CreateIntent(actor, world, Tick.Zero)!;

        Assert.Throws<InvalidOperationException>(() => executor.Execute(intent, world));

        Assert.True(actor.TryGet<HabitComponent>(out var component));
        var habit = Assert.Single(component.Habits);
        Assert.Equal(50d, habit.Force);
        Assert.Equal(Tick.Zero, habit.LastActivatedAt!.Value);
    }

    [Fact]
    public void Erosion_PolitiqueQuiAugmenteForce_EstRejeteeSansMutation()
    {
        var world = new World(42);
        var actor = world.Spawn();
        actor.Set(new HabitComponent
        {
            Habits =
            {
                new HabitState(
                    "test.repos",
                    "agir",
                    "ctx",
                    50,
                    new Tick(1),
                    Tick.Zero),
            },
        });

        var system = new HabitEvolutionSystem(
            inactivityThresholdTicks: 0,
            new DeltaPolicy(10, 5));

        Assert.Throws<InvalidOperationException>(
            () => system.Update(world, new Tick(2)));

        Assert.True(actor.TryGet<HabitComponent>(out var component));
        Assert.Equal(50d, Assert.Single(component.Habits).Force);
    }

    private static HabitLearningObserver CreateObserver(
        IHabitRule rule,
        HabitSelectionRegistry registry,
        IHabitStrengthPolicy policy)
        => new(
            new[] { rule },
            new FixedParameters(),
            policy,
            registry);

    private static PipelineAutonomousIntentExecutor CreateExecutor(
        IAutonomousIntentExecutionObserver observer)
    {
        var food = new NoFoodResolver();
        return new PipelineAutonomousIntentExecutor(
            new PipelineRunner(
                new NeedsPlanner(food),
                new NeedsExecutionEngine(food)),
            observer);
    }

    private sealed class FakeRule : IHabitRule
    {
        private readonly string _observedObjective;

        public string HabitTypeId { get; }

        public FakeRule(string habitTypeId, string observedObjective)
        {
            HabitTypeId = habitTypeId;
            _observedObjective = observedObjective;
        }

        public HabitFormationCandidate? ObserveFormation(
            Intent intent,
            Entity actor,
            World world,
            Tick currentTick)
            => string.Equals(intent.Objectif, _observedObjective, StringComparison.Ordinal)
                ? new HabitFormationCandidate(HabitTypeId, intent.Objectif, "ctx")
                : null;

        public bool IsTriggered(
            HabitState habit,
            Entity actor,
            World world,
            Tick currentTick)
            => currentTick.Value % 2 == 0;

        public bool IsIntentTreatable(
            HabitState habit,
            Entity actor,
            World world,
            Tick currentTick)
            => true;
    }

    private sealed class FixedParameters : IHabitFormationParameterResolver
    {
        public HabitFormationParameters Resolve(
            string habitTypeId,
            Entity actor,
            World world,
            Tick currentTick)
            => new(2, 10, 40);
    }

    private sealed class DeltaPolicy : IHabitStrengthPolicy
    {
        private readonly double _reinforcementDelta;
        private readonly double _erosionDelta;

        public DeltaPolicy(double reinforcementDelta, double erosionDelta)
        {
            _reinforcementDelta = reinforcementDelta;
            _erosionDelta = erosionDelta;
        }

        public double Reinforce(
            HabitState habit,
            Entity actor,
            World world,
            Tick currentTick)
            => habit.Force + _reinforcementDelta;

        public double Erode(
            HabitState habit,
            Entity actor,
            World world,
            Tick currentTick)
            => habit.Force + _erosionDelta;
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
