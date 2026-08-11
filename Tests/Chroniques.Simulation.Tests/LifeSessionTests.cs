namespace Chroniques.Simulation.Tests;

using Xunit;
using Chroniques.Simulation.Actions;
using Chroniques.Simulation.Actions.Exemples;
using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;
using Chroniques.Simulation.Session;
using Chroniques.Simulation.Systems;

public sealed class LifeSessionTests
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
    public void Constructeur_PersonnageActifInexistant_RefuseLaSession()
    {
        var world = CreerWorld();
        var scheduler = new Scheduler();
        var absent = new EntityId(Guid.NewGuid());

        Assert.Throws<ArgumentException>(
            () => new LifeSession(world, scheduler, absent));
    }

    [Fact]
    public void AdvanceTime_FaitAvancerExactementUnTick()
    {
        var world = CreerWorld();
        var personnage = world.Spawn();
        var scheduler = new Scheduler();
        var session = new LifeSession(world, scheduler, personnage.Id);

        session.AdvanceTime();

        Assert.Equal(new Tick(1), world.CurrentTick);
    }

    [Fact]
    public void AdvanceTime_PersonnageVivant_ResteActif()
    {
        var world = CreerWorld();
        var personnage = world.Spawn();
        var scheduler = new Scheduler();
        var session = new LifeSession(world, scheduler, personnage.Id);

        session.AdvanceTime();

        Assert.Equal(personnage.Id, session.ActiveCharacterId);
        Assert.Equal(LifeSessionState.Active, session.State);
    }

    [Fact]
    public void AdvanceTime_MortSansSuccesseur_TermineLaSession()
    {
        var world = CreerWorld();
        var personnage = world.Spawn();
        personnage.Set(new RelationComponent());

        var heritageSystem = new HeritageSystem();
        var scheduler = new Scheduler();
        scheduler.Register(heritageSystem);

        TuerEntity(personnage, Tick.Zero);

        var session = new LifeSession(world, scheduler, personnage.Id);

        session.AdvanceTime();

        Assert.Equal(LifeSessionState.EndedWithoutSuccessor, session.State);
        Assert.Equal(personnage.Id, session.ActiveCharacterId);
    }

    [Fact]
    public void AdvanceTime_MortAvecTransmission_BasculeSurHeritier()
    {
        var world = CreerWorld();
        var personnage = world.Spawn();
        var heritier = world.Spawn();

        personnage.Set(new RelationComponent());

        var relationSystem = new RelationSystem();
        relationSystem.EnregistrerInteraction(
            world,
            Tick.Zero,
            personnage.Id,
            heritier.Id,
            TypeRelation.Familiale,
            10.0,
            "famille");

        var heritageSystem = new HeritageSystem();
        var scheduler = new Scheduler();
        scheduler.Register(heritageSystem);

        TuerEntity(personnage, Tick.Zero);

        var session = new LifeSession(world, scheduler, personnage.Id);

        session.AdvanceTime();

        Assert.Equal(LifeSessionState.Active, session.State);
        Assert.Equal(heritier.Id, session.ActiveCharacterId);
    }

    [Fact]
    public void AdvanceTime_NeReutilisePasUneTransmissionAncienne()
    {
        var world = CreerWorld();
        var personnage = world.Spawn();
        var ancienHeritier = world.Spawn();

        world.Publish(
            GameEvent.Create(
                Tick.Zero,
                "heritage.transmission",
                personnage.Id,
                ancienHeritier.Id));

        personnage.Set(new RelationComponent());

        var heritageSystem = new HeritageSystem();
        var scheduler = new Scheduler();
        scheduler.Register(heritageSystem);

        TuerEntity(personnage, Tick.Zero);

        var session = new LifeSession(world, scheduler, personnage.Id);

        session.AdvanceTime();

        Assert.Equal(LifeSessionState.EndedWithoutSuccessor, session.State);
        Assert.Equal(personnage.Id, session.ActiveCharacterId);
    }

    [Fact]
    public void AdvanceTime_TransmissionVersCibleInexistante_NeBasculePasLeControle()
    {
        var world = CreerWorld();
        var personnage = world.Spawn();
        var cibleInexistante = new EntityId(Guid.NewGuid());

        var scheduler = new Scheduler();
        scheduler.Register(
            new PublishTransmissionSystem(
                personnage.Id,
                cibleInexistante));

        scheduler.Register(new KillSystem(personnage.Id));

        var session = new LifeSession(world, scheduler, personnage.Id);

        session.AdvanceTime();

        Assert.Equal(LifeSessionState.EndedWithoutSuccessor, session.State);
        Assert.Equal(personnage.Id, session.ActiveCharacterId);
    }

    [Fact]
    public void AdvanceTime_SessionTerminee_NavancePlusLeWorld()
    {
        var world = CreerWorld();
        var personnage = world.Spawn();
        personnage.Set(new RelationComponent());

        var scheduler = new Scheduler();
        scheduler.Register(new HeritageSystem());

        TuerEntity(personnage, Tick.Zero);

        var session = new LifeSession(world, scheduler, personnage.Id);
        session.AdvanceTime();

        var tickFin = world.CurrentTick;

        session.AdvanceTime();

        Assert.Equal(tickFin, world.CurrentTick);
    }


    [Fact]
    public void ParcoursV03_ActionPuisVieillissementMortEtHeritage_AssureLaContinuite()
    {
        var world = CreerWorld();
        var personnage = world.Spawn();
        var heritier = world.Spawn();

        personnage.Set(new AgeComponent { Annees = 79 });
        personnage.Set(new NeedsComponent { Fatigue = 50 });
        personnage.Set(new RelationComponent());

        var relationSystem = new RelationSystem();
        relationSystem.EnregistrerInteraction(
            world,
            Tick.Zero,
            personnage.Id,
            heritier.Id,
            TypeRelation.Familiale,
            10.0,
            "famille");

        var intent = new Intent(
            personnage.Id,
            "se_reposer",
            Priorite: 1);

        var pipeline = new PipelineRunner(
            new SimplePlanner(),
            new SimpleExecutionEngine());

        var action = pipeline.ExecuterSeReposer(
            intent,
            personnage.Id,
            world);

        Assert.Equal(ActionState.Archived, action.State);
        Assert.Equal(OutcomeForme.Reussite, action.Outcome?.Forme);
        Assert.Contains(
            world.Events,
            evt =>
                evt.Kind == "besoin.fatigue.restauree"
                && evt.Source == personnage.Id);

        var scheduler = new Scheduler();
        scheduler.Register(new AgingSystem(esperanceDeVie: 80));
        scheduler.Register(new HeritageSystem());

        var session = new LifeSession(
            world,
            scheduler,
            personnage.Id);

        for (var i = 0; i < CalendrierSimule.MoisParAn; i++)
        {
            session.AdvanceTime();
        }

        Assert.Equal("mort", personnage.Lifecycle.CurrentState.Name);
        Assert.Contains(
            world.Events,
            evt =>
                evt.Kind == "heritage.transmission"
                && evt.Source == personnage.Id
                && evt.Target == heritier.Id);
        Assert.Equal(LifeSessionState.Active, session.State);
        Assert.Equal(heritier.Id, session.ActiveCharacterId);
    }

    private sealed class KillSystem : ISystem
    {
        private readonly EntityId _entityId;

        public KillSystem(EntityId entityId)
        {
            _entityId = entityId;
        }

        public void Update(World world, Tick currentTick)
        {
            if (!world.TryGetEntity(_entityId, out var entity))
                return;

            TuerEntity(entity, currentTick);
        }
    }

    private sealed class PublishTransmissionSystem : ISystem
    {
        private readonly EntityId _source;
        private readonly EntityId _target;

        public PublishTransmissionSystem(EntityId source, EntityId target)
        {
            _source = source;
            _target = target;
        }

        public void Update(World world, Tick currentTick)
        {
            world.Publish(
                GameEvent.Create(
                    currentTick,
                    "heritage.transmission",
                    _source,
                    _target));
        }
    }
}
