namespace Chroniques.Simulation.Tests;

using Xunit;
using Chroniques.Simulation.Actions;
using Chroniques.Simulation.Actions.Exemples;
using Chroniques.Simulation.Autonomy;
using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;
using Chroniques.Simulation.Systems;

public sealed class NeedsIntentSourceTests
{
    private static World CreerWorld()
        => new(seed: 42);

    [Fact]
    public void Constructeur_SeuilInferieurAZero_EstRejete()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new NeedsIntentSource(-0.1));
    }

    [Fact]
    public void Constructeur_SeuilSuperieurACent_EstRejete()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new NeedsIntentSource(100.1));
    }

    [Fact]
    public void CreateIntent_SansNeedsComponent_RetourneNull()
    {
        var world = CreerWorld();
        var acteur = world.Spawn();
        var source = new NeedsIntentSource(60);

        var intent = source.CreateIntent(
            acteur,
            world,
            world.CurrentTick);

        Assert.Null(intent);
    }

    [Fact]
    public void CreateIntent_FatigueSuperieureAuSeuil_RetourneNull()
    {
        var world = CreerWorld();
        var acteur = world.Spawn();
        acteur.Set(new NeedsComponent { Fatigue = 61 });
        var source = new NeedsIntentSource(60);

        var intent = source.CreateIntent(
            acteur,
            world,
            world.CurrentTick);

        Assert.Null(intent);
    }

    [Fact]
    public void CreateIntent_FatigueEgaleAuSeuil_RetourneNull()
    {
        var world = CreerWorld();
        var acteur = world.Spawn();
        acteur.Set(new NeedsComponent { Fatigue = 60 });
        var source = new NeedsIntentSource(60);

        var intent = source.CreateIntent(
            acteur,
            world,
            world.CurrentTick);

        Assert.Null(intent);
    }

    [Fact]
    public void CreateIntent_FatigueInferieureAuSeuil_ProduitIntentSeReposer()
    {
        var world = CreerWorld();
        var acteur = world.Spawn();
        acteur.Set(new NeedsComponent { Fatigue = 59 });
        var source = new NeedsIntentSource(60);

        var intent = source.CreateIntent(
            acteur,
            world,
            world.CurrentTick);

        Assert.NotNull(intent);
        Assert.Equal(acteur.Id, intent!.Acteur);
        Assert.Equal(NeedsIntentSource.RestObjective, intent.Objectif);
        Assert.Equal(1, intent.Priorite);
    }

    [Fact]
    public void CreateIntent_FaimCritiqueSansFatigueActionnable_NinventePasDIntent()
    {
        var world = CreerWorld();
        var acteur = world.Spawn();
        acteur.Set(
            new NeedsComponent
            {
                Faim = 0,
                Fatigue = 80,
                Sante = 100,
                Moral = 100
            });
        var source = new NeedsIntentSource(60);

        var intent = source.CreateIntent(
            acteur,
            world,
            world.CurrentTick);

        Assert.Null(intent);
    }

    [Fact]
    public void CreateIntent_NeModifieJamaisNeedsComponent()
    {
        var world = CreerWorld();
        var acteur = world.Spawn();
        var needs = new NeedsComponent
        {
            Faim = 31,
            Fatigue = 42,
            Sante = 53,
            Moral = 64
        };
        acteur.Set(needs);

        var avant = (
            needs.Faim,
            needs.Fatigue,
            needs.Sante,
            needs.Moral);

        var source = new NeedsIntentSource(60);

        _ = source.CreateIntent(
            acteur,
            world,
            world.CurrentTick);

        var apres = (
            needs.Faim,
            needs.Fatigue,
            needs.Sante,
            needs.Moral);

        Assert.Equal(avant, apres);
    }

    [Fact]
    public void CreateIntent_MemesEntrees_ProduitMemeDecision()
    {
        var world = CreerWorld();
        var acteur = world.Spawn();
        acteur.Set(new NeedsComponent { Fatigue = 40 });
        var source = new NeedsIntentSource(60);

        var premier = source.CreateIntent(
            acteur,
            world,
            new Tick(5));
        var second = source.CreateIntent(
            acteur,
            world,
            new Tick(5));

        Assert.Equal(premier, second);
    }

    [Fact]
    public void SchedulerTick_NeedsIntentSource_TraverseAutonomieEtPipeline()
    {
        var world = CreerWorld();
        var habitant = world.Spawn();
        habitant.Set(new NeedsComponent { Fatigue = 50 });

        var source = new NeedsIntentSource(60);
        var pipeline = new PipelineRunner(
            new SimplePlanner(),
            new SimpleExecutionEngine());
        var executor = new PipelineRunnerIntentExecutor(pipeline);

        var autonomy = new AutonomousActionSystem(
            source,
            executor);
        autonomy.RegisterActor(habitant.Id);

        var scheduler = new Scheduler();
        scheduler.Register(autonomy);

        scheduler.Tick(world);

        Assert.Equal(new Tick(1), world.CurrentTick);
        Assert.NotNull(executor.LastAction);
        Assert.Equal(
            ActionState.Archived,
            executor.LastAction!.State);
        Assert.Equal(
            OutcomeForme.Reussite,
            executor.LastAction.Outcome?.Forme);

        Assert.True(
            habitant.TryGet<NeedsComponent>(out var needs));
        Assert.True(needs.Fatigue > 50);

        Assert.Contains(
            world.Events,
            evt =>
                evt.Kind == "besoin.fatigue.restauree"
                && evt.Source == habitant.Id);
    }

    private sealed class PipelineRunnerIntentExecutor
        : IAutonomousIntentExecutor
    {
        private readonly PipelineRunner _pipeline;

        public ActionInstance? LastAction { get; private set; }

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

            LastAction = _pipeline.ExecuterSeReposer(
                intent,
                intent.Acteur,
                world);
        }
    }
}
