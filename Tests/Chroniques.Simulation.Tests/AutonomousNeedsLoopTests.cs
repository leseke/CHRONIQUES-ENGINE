namespace Chroniques.Simulation.Tests;

using System.Linq;
using Xunit;
using Chroniques.Simulation.Actions;
using Chroniques.Simulation.Actions.Exemples;
using Chroniques.Simulation.Autonomy;
using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;
using Chroniques.Simulation.Systems;

public sealed class AutonomousNeedsLoopTests
{
    [Fact]
    public void Scheduler_MultiTick_DecayPuisAutonomie_DeclencheReposSeulementSousSeuil()
    {
        var world = new World(seed: 42);
        var habitant = world.Spawn();
        habitant.Set(
            new NeedsComponent
            {
                Faim = 100,
                Fatigue = 62,
                Sante = 100,
                Moral = 100
            });

        var source = new NeedsIntentSource(
            fatigueActivationThreshold: 60);
        var executor = new PipelineRunnerIntentExecutor(
            new PipelineRunner(
                new SimplePlanner(),
                new SimpleExecutionEngine()));

        var autonomy = new AutonomousActionSystem(
            source,
            executor);
        autonomy.RegisterActor(habitant.Id);

        var scheduler = new Scheduler();
        scheduler.Register(
            new NeedsDecaySystem(
                faimDeclinParTick: 0,
                fatigueDeclinParTick: 1,
                moralDeclinEnDetresse: 0,
                seuilDetresse: 0));
        scheduler.Register(autonomy);

        scheduler.Tick(world);

        Assert.Equal(new Tick(1), world.CurrentTick);
        Assert.True(
            habitant.TryGet<NeedsComponent>(out var needsApresTick1));
        Assert.Equal(61d, needsApresTick1.Fatigue);
        Assert.Equal(
            0,
            world.Events.Count(
                evt => evt.Kind == "besoin.fatigue.restauree"));

        scheduler.Tick(world);

        Assert.Equal(new Tick(2), world.CurrentTick);
        Assert.True(
            habitant.TryGet<NeedsComponent>(out var needsApresTick2));
        Assert.Equal(60d, needsApresTick2.Fatigue);
        Assert.Equal(
            0,
            world.Events.Count(
                evt => evt.Kind == "besoin.fatigue.restauree"));

        scheduler.Tick(world);

        Assert.Equal(new Tick(3), world.CurrentTick);
        Assert.True(
            habitant.TryGet<NeedsComponent>(out var needsApresTick3));
        Assert.Equal(79d, needsApresTick3.Fatigue);
        Assert.Equal(
            1,
            world.Events.Count(
                evt => evt.Kind == "besoin.fatigue.restauree"));
    }

    [Fact]
    public void Scheduler_VingtTicks_SansEntreeJoueur_ReguleLaFatigueDeManiereAutonome()
    {
        var world = new World(seed: 42);
        var habitant = world.Spawn();
        habitant.Set(
            new NeedsComponent
            {
                Faim = 100,
                Fatigue = 80,
                Sante = 100,
                Moral = 100
            });

        var source = new NeedsIntentSource(
            fatigueActivationThreshold: 60);
        var executor = new PipelineRunnerIntentExecutor(
            new PipelineRunner(
                new SimplePlanner(),
                new SimpleExecutionEngine()));

        var autonomy = new AutonomousActionSystem(
            source,
            executor);
        autonomy.RegisterActor(habitant.Id);

        var scheduler = new Scheduler();
        scheduler.Register(
            new NeedsDecaySystem(
                faimDeclinParTick: 0,
                fatigueDeclinParTick: 5,
                moralDeclinEnDetresse: 0,
                seuilDetresse: 0));
        scheduler.Register(autonomy);

        for (var i = 0; i < 20; i++)
        {
            scheduler.Tick(world);
        }

        Assert.Equal(new Tick(20), world.CurrentTick);
        Assert.True(
            habitant.TryGet<NeedsComponent>(out var needs));
        Assert.Equal(60d, needs.Fatigue);
        Assert.Equal(
            4,
            world.Events.Count(
                evt =>
                    evt.Kind == "besoin.fatigue.restauree"
                    && evt.Source == habitant.Id));
    }

    private sealed class PipelineRunnerIntentExecutor
        : IAutonomousIntentExecutor
    {
        private readonly PipelineRunner _pipeline;

        public PipelineRunnerIntentExecutor(
            PipelineRunner pipeline)
        {
            _pipeline = pipeline;
        }

        public void Execute(Intent intent, World world)
        {
            if (!string.Equals(
                    intent.Objectif,
                    NeedsIntentSource.RestObjective,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "L'adaptateur de test ne supporte que l'Intent se_reposer.");
            }

            _pipeline.ExecuterSeReposer(
                intent,
                intent.Acteur,
                world);
        }
    }
}
