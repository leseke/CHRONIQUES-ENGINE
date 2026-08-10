namespace Chroniques.Simulation.Tests;

using Xunit;
using Chroniques.Simulation.Actions;
using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;
using Chroniques.Simulation.Systems;

/// <summary>
/// Tests de HeritageSystem (ENGINE-008, section 8).
///
/// Couvre :
/// - priorité des relations Familiales ;
/// - tie-break déterministe par ancienneté ;
/// - absence de successeur ;
/// - absence de RelationComponent ;
/// - transmission observable ;
/// - non-retraitement d'une Entity morte ;
/// - refus d'héritage traité par HeritageSystem ;
/// - dispatch de HeritageRefusalEffect vers HeritageSystem.
/// </summary>
public sealed class HeritageSystemTests
{
    private static World CréerWorld()
        => new(seed: 42);

    private static void TuerEntity(
        Entity entity)
    {
        entity.Lifecycle.Record(
            GameEvent.Create(
                Tick.Zero,
                "vie.mort",
                entity.Id),
            new State("mort"));
    }

    // ── Désignation ──────────────────────────────────────────────────────────

    [Fact]
    public void DesignationHeritier_PrioriteFamiliale_MemeAvecForcePlusFaible()
    {
        var world = CréerWorld();

        var relationSystem =
            new RelationSystem(
                forceInitiale: 50.0);

        var heritageSystem =
            new HeritageSystem();

        var defunt =
            world.Spawn();

        var amiFort =
            world.Spawn();

        var familleModeree =
            world.Spawn();

        defunt.Set(
            new RelationComponent());

        // Relation amicale très forte.
        relationSystem.EnregistrerInteraction(
            world,
            Tick.Zero,
            defunt.Id,
            amiFort.Id,
            TypeRelation.Amicale,
            40.0,
            "grande amitié");

        // Relation familiale moins forte.
        relationSystem.EnregistrerInteraction(
            world,
            Tick.Zero,
            defunt.Id,
            familleModeree.Id,
            TypeRelation.Familiale,
            10.0,
            "lien familial");

        TuerEntity(defunt);

        heritageSystem.Update(
            world,
            Tick.Zero.Next());

        var evt =
            world.Events
                .FirstOrDefault(
                    e =>
                        e.Kind == "heritage.transmission");

        Assert.NotNull(evt);

        Assert.Equal(
            familleModeree.Id,
            evt!.Target);
    }

    [Fact]
    public void DesignationHeritier_TieBreakParAnciennete()
    {
        var world = CréerWorld();

        var relationSystem =
            new RelationSystem(
                forceInitiale: 50.0);

        var heritageSystem =
            new HeritageSystem();

        var defunt =
            world.Spawn();

        var familleA =
            world.Spawn();

        var familleB =
            world.Spawn();

        defunt.Set(
            new RelationComponent());

        var tick1 =
            Tick.Zero;

        var tick2 =
            tick1.Next();

        relationSystem.EnregistrerInteraction(
            world,
            tick1,
            defunt.Id,
            familleA.Id,
            TypeRelation.Familiale,
            0.0,
            "aîné");

        relationSystem.EnregistrerInteraction(
            world,
            tick2,
            defunt.Id,
            familleB.Id,
            TypeRelation.Familiale,
            0.0,
            "cadet");

        TuerEntity(defunt);

        heritageSystem.Update(
            world,
            tick2.Next());

        var evt =
            world.Events
                .FirstOrDefault(
                    e =>
                        e.Kind == "heritage.transmission");

        Assert.NotNull(evt);

        // La relation la plus ancienne doit gagner le tie-break.
        Assert.Equal(
            familleA.Id,
            evt!.Target);
    }

    // ── Transmission réussie ────────────────────────────────────────────────

    [Fact]
    public void TransmissionAvecHeritier_PublieEvenementTransmission()
    {
        var world =
            CréerWorld();

        var relationSystem =
            new RelationSystem();

        var heritageSystem =
            new HeritageSystem();

        var defunt =
            world.Spawn();

        var heritier =
            world.Spawn();

        defunt.Set(
            new RelationComponent());

        relationSystem.EnregistrerInteraction(
            world,
            Tick.Zero,
            defunt.Id,
            heritier.Id,
            TypeRelation.Familiale,
            10.0,
            "famille");

        TuerEntity(defunt);

        heritageSystem.Update(
            world,
            Tick.Zero.Next());

        Assert.Contains(
            world.Events,
            e =>
                e.Kind == "heritage.transmission"
                && e.Source == defunt.Id
                && e.Target == heritier.Id);
    }

    // ── Cas d'échec : absence de successeur ─────────────────────────────────

    [Fact]
    public void AbsenceDeSuccesseur_PublieEvenementDedie()
    {
        var world =
            CréerWorld();

        var heritageSystem =
            new HeritageSystem();

        var defunt =
            world.Spawn();

        defunt.Set(
            new RelationComponent());

        TuerEntity(defunt);

        heritageSystem.Update(
            world,
            Tick.Zero.Next());

        Assert.Contains(
            world.Events,
            e =>
                e.Kind == "heritage.absence-successeur"
                && e.Source == defunt.Id);
    }

    [Fact]
    public void AbsenceDeRelationComponent_PublieAbsenceDeSuccesseur()
    {
        var world =
            CréerWorld();

        var heritageSystem =
            new HeritageSystem();

        var defunt =
            world.Spawn();

        // Aucun RelationComponent.

        TuerEntity(defunt);

        heritageSystem.Update(
            world,
            Tick.Zero.Next());

        Assert.Contains(
            world.Events,
            e =>
                e.Kind == "heritage.absence-successeur"
                && e.Source == defunt.Id);
    }

    // ── Non-retraitement ─────────────────────────────────────────────────────

    [Fact]
    public void EntityDejaTraitee_NestPasRetraitee()
    {
        var world =
            CréerWorld();

        var heritageSystem =
            new HeritageSystem();

        var defunt =
            world.Spawn();

        defunt.Set(
            new RelationComponent());

        TuerEntity(defunt);

        heritageSystem.Update(
            world,
            Tick.Zero.Next());

        heritageSystem.Update(
            world,
            Tick.Zero.Next().Next());

        var evenementsHeritage =
            world.Events
                .Where(
                    e =>
                        e.Kind.StartsWith(
                            "heritage.",
                            StringComparison.Ordinal))
                .ToList();

        Assert.Single(
            evenementsHeritage);
    }

    // ── Refus : HeritageSystem source de vérité ──────────────────────────────

    [Fact]
    public void RefuserHeritage_PublieEvenementRefus()
    {
        var world =
            CréerWorld();

        var heritageSystem =
            new HeritageSystem();

        var heritier =
            world.Spawn();

        var defunt =
            world.Spawn();

        heritageSystem.RefuserHeritage(
            world,
            Tick.Zero,
            heritier.Id,
            defunt.Id);

        Assert.Contains(
            world.Events,
            e =>
                e.Kind == "heritage.refus"
                && e.Source == heritier.Id
                && e.Target == defunt.Id);
    }

    [Fact]
    public void RefuserHeritage_EntityHeritierInexistante_NePublieRien()
    {
        var world =
            CréerWorld();

        var heritageSystem =
            new HeritageSystem();

        var defunt =
            world.Spawn();

        var heritierInexistant =
            new EntityId(Guid.NewGuid());

        var nombreEvenementsAvant =
            world.Events.Count;

        heritageSystem.RefuserHeritage(
            world,
            Tick.Zero,
            heritierInexistant,
            defunt.Id);

        Assert.Equal(
            nombreEvenementsAvant,
            world.Events.Count);
    }

    [Fact]
    public void RefuserHeritage_EntityDefuntInexistante_NePublieRien()
    {
        var world =
            CréerWorld();

        var heritageSystem =
            new HeritageSystem();

        var heritier =
            world.Spawn();

        var defuntInexistant =
            new EntityId(Guid.NewGuid());

        var nombreEvenementsAvant =
            world.Events.Count;

        heritageSystem.RefuserHeritage(
            world,
            Tick.Zero,
            heritier.Id,
            defuntInexistant);

        Assert.Equal(
            nombreEvenementsAvant,
            world.Events.Count);
    }

    // ── Refus via EffectApplicator ────────────────────────────────────────────

    [Fact]
    public void PopulationEffectApplicator_DispatcheHeritageRefusalEffect_VersHeritageSystem()
    {
        var world =
            CréerWorld();

        var relationSystem =
            new RelationSystem();

        var skillSystem =
            new SkillSystem();

        var heritageSystem =
            new HeritageSystem();

        var applicator =
            new PopulationEffectApplicator(
                relationSystem,
                skillSystem,
                heritageSystem);

        var heritier =
            world.Spawn();

        var defunt =
            world.Spawn();

        var effect =
            new HeritageRefusalEffect(
                heritier.Id,
                defunt.Id);

        applicator.Appliquer(
            effect,
            world,
            Tick.Zero);

        Assert.Contains(
            world.Events,
            e =>
                e.Kind == "heritage.refus"
                && e.Source == heritier.Id
                && e.Target == defunt.Id);
    }
}
