namespace Chroniques.Simulation.Tests;
using Xunit;
using Chroniques.Simulation.Actions;
using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;
using Chroniques.Simulation.Systems;

/// <summary>
/// Tests de HeritageSystem (ENGINE-008, section 8).
/// Couvre : désignation priorisant Familiale, tie-break par ancienneté,
/// absence de successeur, transmission réussie, non-retraitement d'une
/// Entity déjà traitée. Le refus est testé via PopulationEffectApplicator.
/// </summary>
public sealed class HeritageSystemTests
{
    private static World CréerWorld() => new(seed: 42);

    private static void TuerEntity(Entity entity)
    {
        entity.Lifecycle.Record(
            GameEvent.Create(Tick.Zero, "vie.mort", entity.Id),
            new State("mort"));
    }

    // ── Désignation ──────────────────────────────────────────────────────────

    [Fact]
    public void DesignationHeritier_PrioriteFamiliale_MemeAvecForcePlusFaible()
    {
        var world = CréerWorld();
        var relSystem = new RelationSystem(forceInitiale: 50.0);
        var heritageSystem = new HeritageSystem();

        var defunt = world.Spawn();
        var amiFort = world.Spawn();
        var familleModere = world.Spawn();

        defunt.Set(new RelationComponent());

        // Ami avec Force très haute
        relSystem.EnregistrerInteraction(world, Tick.Zero, defunt.Id, amiFort.Id,
            TypeRelation.Amicale, 40, "grande amitié");

        // Famille avec Force modérée
        relSystem.EnregistrerInteraction(world, Tick.Zero, defunt.Id, familleModere.Id,
            TypeRelation.Familiale, 10, "lien familial");

        TuerEntity(defunt);
        heritageSystem.Update(world, Tick.Zero.Next());

        var evt = world.Events
            .FirstOrDefault(e => e.Kind == "heritage.transmission");

        Assert.NotNull(evt);
        Assert.Equal(familleModere.Id, evt!.Target);
    }

    [Fact]
    public void DesignationHeritier_TieBreakParAnciennete()
    {
        var world = CréerWorld();
        var relSystem = new RelationSystem(forceInitiale: 50.0);
        var heritageSystem = new HeritageSystem();

        var defunt = world.Spawn();
        var familleA = world.Spawn();
        var familleB = world.Spawn();

        defunt.Set(new RelationComponent());

        var tick1 = Tick.Zero;
        var tick2 = tick1.Next();

        relSystem.EnregistrerInteraction(world, tick1, defunt.Id, familleA.Id,
            TypeRelation.Familiale, 0, "aîné");
        relSystem.EnregistrerInteraction(world, tick2, defunt.Id, familleB.Id,
            TypeRelation.Familiale, 0, "cadet");

        TuerEntity(defunt);
        heritageSystem.Update(world, tick2.Next());

        var evt = world.Events
            .FirstOrDefault(e => e.Kind == "heritage.transmission");

        Assert.NotNull(evt);
        Assert.Equal(familleA.Id, evt!.Target); // La plus ancienne (tick1)
    }

    // ── Cas d'échec ───────────────────────────────────────────────────────────

    [Fact]
    public void AbsenceDeSuccesseur_PublieEvenementDedié()
    {
        var world = CréerWorld();
        var heritageSystem = new HeritageSystem();

        var defunt = world.Spawn();
        defunt.Set(new RelationComponent()); // aucune relation

        TuerEntity(defunt);
        heritageSystem.Update(world, Tick.Zero.Next());

        Assert.Contains(world.Events,
            e => e.Kind == "heritage.absence-successeur" && e.Source == defunt.Id);
    }

    [Fact]
    public void AbsenceDeRelationComponent_PasDEvenementHeritage()
    {
        var world = CréerWorld();
        var heritageSystem = new HeritageSystem();

        var defunt = world.Spawn();
        // Pas de RelationComponent

        TuerEntity(defunt);
        heritageSystem.Update(world, Tick.Zero.Next());

        Assert.Contains(world.Events,
            e => e.Kind == "heritage.absence-successeur");
    }

    // ── Non-retraitement ─────────────────────────────────────────────────────

    [Fact]
    public void EntityDejaTraitee_NestPasRetraitee()
    {
        var world = CréerWorld();
        var heritageSystem = new HeritageSystem();

        var defunt = world.Spawn();
        defunt.Set(new RelationComponent());

        TuerEntity(defunt);

        heritageSystem.Update(world, Tick.Zero.Next());
        heritageSystem.Update(world, Tick.Zero.Next().Next());

        // Un seul événement heritage, pas deux
        var evts = world.Events
            .Where(e => e.Kind.StartsWith("heritage."))
            .ToList();

        Assert.Single(evts);
    }

    // ── Refus via EffectApplicator ────────────────────────────────────────────

    [Fact]
    public void HeritageRefusalEffect_PublieEvenementRefus()
    {
        var world = CréerWorld();
        var applicator = new PopulationEffectApplicator(
            new RelationSystem(),
            new SkillSystem());

        var heritier = world.Spawn();
        var defunt = world.Spawn();

        var effect = new HeritageRefusalEffect(heritier.Id, defunt.Id);
        applicator.Appliquer(effect, world, Tick.Zero);

        Assert.Contains(world.Events,
            e => e.Kind == "heritage.refus"
              && e.Source == heritier.Id
              && e.Target == defunt.Id);
    }
}
