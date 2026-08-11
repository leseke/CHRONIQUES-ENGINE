namespace Chroniques.Simulation.Tests;

using Xunit;
using Chroniques.Simulation.Actions;
using Chroniques.Simulation.Actions.Exemples;
using Chroniques.Simulation.Autonomy;
using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;
using Chroniques.Simulation.Systems;

public sealed class AutonomousActionSystemTests
{
    private static World CreerWorld()
        => new(seed: 42);

    private static void TuerEntity(Entity entity, Tick tick)
    {
        entity.Lifecycle.Record(
            GameEvent.Create(
                tick,
                "vie.mort",
                entity.Id),
            new State("mort"));
    }

    [Fact]
    public void Update_ActeurEnregistreVivant_InterrogeLaSource()
    {
        var world = CreerWorld();
        var acteur = world.Spawn();
        var source = new DelegateIntentSource((_, _, _) => null);
        var executor = new RecordingIntentExecutor();
        var system = new AutonomousActionSystem(source, executor);
        system.RegisterActor(acteur.Id);

        system.Update(world, Tick.Zero);

        Assert.Single(source.Calls);
        Assert.Equal(acteur.Id, source.Calls[0]);
    }

    [Fact]
    public void Update_ActeurNonEnregistre_NestJamaisInterroge()
    {
        var world = CreerWorld();
        world.Spawn();
        var source = new DelegateIntentSource((_, _, _) => null);
        var executor = new RecordingIntentExecutor();
        var system = new AutonomousActionSystem(source, executor);

        system.Update(world, Tick.Zero);

        Assert.Empty(source.Calls);
        Assert.Empty(executor.Intents);
    }

    [Fact]
    public void Update_ActeurAbsentDuWorld_EstIgnore()
    {
        var world = CreerWorld();
        var absent = new EntityId(Guid.NewGuid());
        var source = new DelegateIntentSource((_, _, _) => null);
        var executor = new RecordingIntentExecutor();
        var system = new AutonomousActionSystem(source, executor);
        system.RegisterActor(absent);

        system.Update(world, Tick.Zero);

        Assert.Empty(source.Calls);
        Assert.Empty(executor.Intents);
    }

    [Fact]
    public void Update_ActeurMort_EstIgnore()
    {
        var world = CreerWorld();
        var acteur = world.Spawn();
        TuerEntity(acteur, Tick.Zero);

        var source = new DelegateIntentSource(
            (entity, _, _) => new Intent(entity.Id, "test", Priorite: 1));
        var executor = new RecordingIntentExecutor();
        var system = new AutonomousActionSystem(source, executor);
        system.RegisterActor(acteur.Id);

        system.Update(world, Tick.Zero);

        Assert.Empty(source.Calls);
        Assert.Empty(executor.Intents);
    }

    [Fact]
    public void Update_SourceRetourneNull_NexecuteRien()
    {
        var world = CreerWorld();
        var acteur = world.Spawn();
        var source = new DelegateIntentSource((_, _, _) => null);
        var executor = new RecordingIntentExecutor();
        var system = new AutonomousActionSystem(source, executor);
        system.RegisterActor(acteur.Id);

        system.Update(world, Tick.Zero);

        Assert.Single(source.Calls);
        Assert.Empty(executor.Intents);
    }

    [Fact]
    public void Update_IntentValide_EstExecuteExactementUneFois()
    {
        var world = CreerWorld();
        var acteur = world.Spawn();
        var source = new DelegateIntentSource(
            (entity, _, _) => new Intent(entity.Id, "objectif_test", Priorite: 1));
        var executor = new RecordingIntentExecutor();
        var system = new AutonomousActionSystem(source, executor);
        system.RegisterActor(acteur.Id);

        system.Update(world, Tick.Zero);

        var intent = Assert.Single(executor.Intents);
        Assert.Equal(acteur.Id, intent.Acteur);
        Assert.Equal("objectif_test", intent.Objectif);
    }

    [Fact]
    public void Update_IntentAttribueAUnAutreActeur_EstRejete()
    {
        var world = CreerWorld();
        var acteur = world.Spawn();
        var autre = world.Spawn();
        var source = new DelegateIntentSource(
            (_, _, _) => new Intent(autre.Id, "objectif_invalide", Priorite: 1));
        var executor = new RecordingIntentExecutor();
        var system = new AutonomousActionSystem(source, executor);
        system.RegisterActor(acteur.Id);

        Assert.Throws<InvalidOperationException>(
            () => system.Update(world, Tick.Zero));

        Assert.Empty(executor.Intents);
    }

    [Fact]
    public void Update_PlusieursActeurs_RespecteOrdreEnregistrement()
    {
        var world = CreerWorld();
        var premier = world.Spawn();
        var second = world.Spawn();
        var troisieme = world.Spawn();

        var source = new DelegateIntentSource(
            (entity, _, _) => new Intent(entity.Id, "agir", Priorite: 1));
        var executor = new RecordingIntentExecutor();
        var system = new AutonomousActionSystem(source, executor);

        system.RegisterActor(second.Id);
        system.RegisterActor(premier.Id);
        system.RegisterActor(troisieme.Id);

        system.Update(world, Tick.Zero);

        Assert.Equal(
            new[] { second.Id, premier.Id, troisieme.Id },
            source.Calls);
        Assert.Equal(
            new[] { second.Id, premier.Id, troisieme.Id },
            executor.Intents.Select(intent => intent.Acteur).ToArray());
    }

    [Fact]
    public void RegisterActor_MemeActeurDeuxFois_NestTraiteQuUneFois()
    {
        var world = CreerWorld();
        var acteur = world.Spawn();
        var source = new DelegateIntentSource(
            (entity, _, _) => new Intent(entity.Id, "agir", Priorite: 1));
        var executor = new RecordingIntentExecutor();
        var system = new AutonomousActionSystem(source, executor);

        system.RegisterActor(acteur.Id);
        system.RegisterActor(acteur.Id);
        system.Update(world, Tick.Zero);

        Assert.Single(system.Actors);
        Assert.Single(source.Calls);
        Assert.Single(executor.Intents);
    }

    [Fact]
    public void Update_MemeEtatEtEntrees_ProduitMemeSequenceDIntents()
    {
        var world = CreerWorld();
        var premier = world.Spawn();
        var second = world.Spawn();
        var source = new DelegateIntentSource(
            (entity, _, _) => new Intent(entity.Id, "agir", Priorite: 1));
        var executor = new RecordingIntentExecutor();
        var system = new AutonomousActionSystem(source, executor);
        system.RegisterActor(premier.Id);
        system.RegisterActor(second.Id);

        system.Update(world, Tick.Zero);
        var premiereSequence = executor.Intents
            .Select(intent => intent.Acteur)
            .ToArray();

        executor.Clear();
        source.Clear();

        system.Update(world, Tick.Zero);
        var secondeSequence = executor.Intents
            .Select(intent => intent.Acteur)
            .ToArray();

        Assert.Equal(premiereSequence, secondeSequence);
    }

    [Fact]
    public void Update_NavanceJamaisLeTickLuiMeme()
    {
        var world = CreerWorld();
        var acteur = world.Spawn();
        var source = new DelegateIntentSource((_, _, _) => null);
        var executor = new RecordingIntentExecutor();
        var system = new AutonomousActionSystem(source, executor);
        system.RegisterActor(acteur.Id);

        system.Update(world, Tick.Zero);

        Assert.Equal(Tick.Zero, world.CurrentTick);
    }

    [Fact]
    public void SchedulerTick_IntentAutonomeTraversePipelineRunner_EtModifieLeWorld()
    {
        var world = CreerWorld();
        var habitant = world.Spawn();
        habitant.Set(new NeedsComponent { Fatigue = 50 });

        var source = new DelegateIntentSource(
            (entity, _, _) => new Intent(
                entity.Id,
                "se_reposer",
                Priorite: 1));

        var pipeline = new PipelineRunner(
            new SimplePlanner(),
            new SimpleExecutionEngine());

        var executor = new PipelineRunnerIntentExecutor(pipeline);
        var autonomy = new AutonomousActionSystem(source, executor);
        autonomy.RegisterActor(habitant.Id);

        var scheduler = new Scheduler();
        scheduler.Register(autonomy);

        scheduler.Tick(world);

        Assert.Equal(new Tick(1), world.CurrentTick);
        Assert.NotNull(executor.LastAction);
        Assert.Equal(ActionState.Archived, executor.LastAction!.State);
        Assert.Equal(OutcomeForme.Reussite, executor.LastAction.Outcome?.Forme);

        Assert.True(habitant.TryGet<NeedsComponent>(out var besoins));
        Assert.True(besoins.Fatigue > 50);

        Assert.Contains(
            world.Events,
            evt =>
                evt.Kind == "besoin.fatigue.restauree"
                && evt.Source == habitant.Id);
    }

    private sealed class DelegateIntentSource : IAutonomousIntentSource
    {
        private readonly Func<Entity, World, Tick, Intent?> _factory;

        public List<EntityId> Calls { get; } = new();

        public DelegateIntentSource(
            Func<Entity, World, Tick, Intent?> factory)
        {
            _factory = factory;
        }

        public Intent? CreateIntent(
            Entity actor,
            World world,
            Tick currentTick)
        {
            Calls.Add(actor.Id);
            return _factory(actor, world, currentTick);
        }

        public void Clear() => Calls.Clear();
    }

    private sealed class RecordingIntentExecutor : IAutonomousIntentExecutor
    {
        public List<Intent> Intents { get; } = new();

        public void Execute(Intent intent, World world)
        {
            Intents.Add(intent);
        }

        public void Clear() => Intents.Clear();
    }

    private sealed class PipelineRunnerIntentExecutor : IAutonomousIntentExecutor
    {
        private readonly PipelineRunner _pipeline;

        public ActionInstance? LastAction { get; private set; }

        public PipelineRunnerIntentExecutor(PipelineRunner pipeline)
        {
            _pipeline = pipeline;
        }

        public void Execute(Intent intent, World world)
        {
            if (!string.Equals(
                    intent.Objectif,
                    "se_reposer",
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
