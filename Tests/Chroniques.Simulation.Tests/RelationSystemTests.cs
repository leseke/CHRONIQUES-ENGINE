namespace Chroniques.Simulation.Tests;

using Chroniques.Simulation.Actions;
using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;
using Chroniques.Simulation.Systems;

/// <summary>
/// Tests de RelationSystem (ENGINE-008, section 8).
/// Couvre : érosion, plancher familial, création, disparition à Force 0,
/// Épisode au-dessus du seuil, absence d'Épisode en dessous, éviction
/// du plus ancien, traitement d'un RelationInteractionEffect.
/// </summary>
public sealed class RelationSystemTests
{
    private static World CréerWorld() => new(seed: 42);

    private static RelationSystem CréerSystem(
        double erosion = 1.0,
        double plancherFamilial = 10.0,
        double seuilEpisode = 10.0,
        int capacite = 3,
        double forceInitiale = 50.0)
        => new(erosion, plancherFamilial, seuilEpisode, capacite, forceInitiale);

    // ── Érosion ──────────────────────────────────────────────────────────────

    [Fact]
    public void Erosion_DiminueLaForce_AChaqueTick()
    {
        var world = CréerWorld();
        var system = CréerSystem(erosion: 5.0);
        var a = world.Spawn();
        var b = world.Spawn();
        a.Set(new RelationComponent());
        var rc = new RelationComponent();
        a.Set(rc);

        system.EnregistrerInteraction(world, Tick.Zero, a.Id, b.Id,
            TypeRelation.Amicale, 0, "neutre");

        var force0 = a.TryGet<RelationComponent>(out var comp)
            ? comp.Relations[0].Force : 0;

        system.Update(world, Tick.Zero.Next());

        a.TryGet<RelationComponent>(out var comp2);
        Assert.True(comp2.Relations[0].Force < force0);
    }

    [Fact]
    public void Erosion_RelationFamiliale_NedescendPasSousLePlancher()
    {
        var world = CréerWorld();
        var system = CréerSystem(erosion: 100.0, plancherFamilial: 10.0, forceInitiale: 15.0);
        var a = world.Spawn();
        var b = world.Spawn();
        a.Set(new RelationComponent());

        system.EnregistrerInteraction(world, Tick.Zero, a.Id, b.Id,
            TypeRelation.Familiale, 0, "neutre");

        // Plusieurs Ticks d'érosion intense
        for (var i = 0; i < 10; i++)
            system.Update(world, Tick.Zero.Next());

        a.TryGet<RelationComponent>(out var rc);
        Assert.Single(rc.Relations);
        Assert.True(rc.Relations[0].Force >= 10.0);
    }

    [Fact]
    public void Erosion_RelationFamiliale_PeutDisparaitreParInteractionNegative()
    {
        // ENGINE-008 v1.3 : pas d'immunité absolue, seulement contre l'érosion.
        var world = CréerWorld();
        var system = CréerSystem(plancherFamilial: 10.0, forceInitiale: 5.0);
        var a = world.Spawn();
        var b = world.Spawn();
        a.Set(new RelationComponent());

        system.EnregistrerInteraction(world, Tick.Zero, a.Id, b.Id,
            TypeRelation.Familiale, -100, "rupture totale");

        a.TryGet<RelationComponent>(out var rc);
        Assert.Empty(rc.Relations);
    }

    // ── Création et disparition ───────────────────────────────────────────────

    [Fact]
    public void EnregistrerInteraction_CreeRelation_SiInexistante()
    {
        var world = CréerWorld();
        var system = CréerSystem();
        var a = world.Spawn();
        var b = world.Spawn();
        a.Set(new RelationComponent());

        system.EnregistrerInteraction(world, Tick.Zero, a.Id, b.Id,
            TypeRelation.Amicale, 5, "rencontre");

        a.TryGet<RelationComponent>(out var rc);
        Assert.Single(rc.Relations);
        Assert.Equal(TypeRelation.Amicale, rc.Relations[0].Type);
    }

    [Fact]
    public void Erosion_SupprimeRelation_QuandForceAtteint0()
    {
        var world = CréerWorld();
        var system = CréerSystem(erosion: 100.0, forceInitiale: 5.0);
        var a = world.Spawn();
        var b = world.Spawn();
        a.Set(new RelationComponent());

        system.EnregistrerInteraction(world, Tick.Zero, a.Id, b.Id,
            TypeRelation.Amicale, 0, "neutre");

        system.Update(world, Tick.Zero.Next());

        a.TryGet<RelationComponent>(out var rc);
        Assert.Empty(rc.Relations);
    }

    // ── Épisodes ─────────────────────────────────────────────────────────────

    [Fact]
    public void EnregistrerInteraction_CreeEpisode_SiImpactAuDessusDuSeuil()
    {
        var world = CréerWorld();
        var system = CréerSystem(seuilEpisode: 10.0);
        var a = world.Spawn();
        var b = world.Spawn();
        a.Set(new RelationComponent());

        system.EnregistrerInteraction(world, Tick.Zero, a.Id, b.Id,
            TypeRelation.Amicale, 15, "moment fort");

        a.TryGet<RelationComponent>(out var rc);
        Assert.Single(rc.Relations[0].Episodes);
    }

    [Fact]
    public void EnregistrerInteraction_NeCreesPasEpisode_SiImpactEnDessousDuSeuil()
    {
        var world = CréerWorld();
        var system = CréerSystem(seuilEpisode: 10.0);
        var a = world.Spawn();
        var b = world.Spawn();
        a.Set(new RelationComponent());

        system.EnregistrerInteraction(world, Tick.Zero, a.Id, b.Id,
            TypeRelation.Amicale, 3, "interaction banale");

        a.TryGet<RelationComponent>(out var rc);
        Assert.Empty(rc.Relations[0].Episodes);
    }

    [Fact]
    public void EnregistrerInteraction_EvicteLePlusAncien_QuandCapaciteDepassee()
    {
        var world = CréerWorld();
        var system = CréerSystem(seuilEpisode: 1.0, capacite: 2);
        var a = world.Spawn();
        var b = world.Spawn();
        a.Set(new RelationComponent());

        var tick1 = Tick.Zero;
        var tick2 = tick1.Next();
        var tick3 = tick2.Next();

        system.EnregistrerInteraction(world, tick1, a.Id, b.Id,
            TypeRelation.Amicale, 5, "épisode 1");
        system.EnregistrerInteraction(world, tick2, a.Id, b.Id,
            TypeRelation.Amicale, 5, "épisode 2");
        system.EnregistrerInteraction(world, tick3, a.Id, b.Id,
            TypeRelation.Amicale, 5, "épisode 3");

        a.TryGet<RelationComponent>(out var rc);
        Assert.Equal(2, rc.Relations[0].Episodes.Count);
        // L'épisode le plus ancien (tick1) a été évincé
        Assert.All(rc.Relations[0].Episodes, e => Assert.NotEqual(tick1, e.Tick));
    }

    // ── EffectApplicator ─────────────────────────────────────────────────────

    [Fact]
    public void PopulationEffectApplicator_TraiteRelationInteractionEffect()
    {
        var world = CréerWorld();
        var relSystem = CréerSystem();
        var skillSystem = new SkillSystem();
        var applicator = new PopulationEffectApplicator(relSystem, skillSystem);
        var a = world.Spawn();
        var b = world.Spawn();
        a.Set(new RelationComponent());

        var effect = new RelationInteractionEffect(
            a.Id, b.Id, TypeRelation.Amicale, 20, "test effect");

        applicator.Appliquer(effect, world, Tick.Zero);

        a.TryGet<RelationComponent>(out var rc);
        Assert.Single(rc.Relations);
    }
}
