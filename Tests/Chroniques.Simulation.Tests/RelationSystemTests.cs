namespace Chroniques.Simulation.Tests;

using Xunit;
using Chroniques.Simulation.Actions;
using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;
using Chroniques.Simulation.Systems;

/// <summary>
/// Tests de RelationSystem (ENGINE-008, section 8).
///
/// Couvre :
/// - érosion naturelle ;
/// - plancher familial ;
/// - absence de remontée artificielle sous le plancher ;
/// - rupture d'une relation Familiale par interaction négative ;
/// - création et disparition des relations ;
/// - création et éviction des Épisodes ;
/// - traitement d'un RelationInteractionEffect.
/// </summary>
public sealed class RelationSystemTests
{
    private static World CréerWorld()
        => new(seed: 42);

    private static RelationSystem CréerSystem(
        double erosion = 1.0,
        double plancherFamilial = 10.0,
        double seuilEpisode = 10.0,
        int capacite = 3,
        double forceInitiale = 50.0)
        => new(
            erosion,
            plancherFamilial,
            seuilEpisode,
            capacite,
            forceInitiale);

    // ── Érosion ──────────────────────────────────────────────────────────────

    [Fact]
    public void Erosion_DiminueLaForce_AChaqueTick()
    {
        var world = CréerWorld();
        var system = CréerSystem(
            erosion: 5.0);

        var a = world.Spawn();
        var b = world.Spawn();

        a.Set(new RelationComponent());

        system.EnregistrerInteraction(
            world,
            Tick.Zero,
            a.Id,
            b.Id,
            TypeRelation.Amicale,
            0,
            "neutre");

        a.TryGet<RelationComponent>(out var component);

        var forceAvant =
            component.Relations[0].Force;

        system.Update(
            world,
            Tick.Zero.Next());

        var forceApres =
            component.Relations[0].Force;

        Assert.True(
            forceApres < forceAvant);
    }

    [Fact]
    public void Erosion_RelationFamiliale_NeDescendPasSousLePlancher()
    {
        var world = CréerWorld();

        var system = CréerSystem(
            erosion: 100.0,
            plancherFamilial: 10.0,
            forceInitiale: 15.0);

        var a = world.Spawn();
        var b = world.Spawn();

        a.Set(new RelationComponent());

        system.EnregistrerInteraction(
            world,
            Tick.Zero,
            a.Id,
            b.Id,
            TypeRelation.Familiale,
            0,
            "neutre");

        // Plusieurs Ticks d'érosion intense.
        for (var i = 0; i < 10; i++)
        {
            system.Update(
                world,
                Tick.Zero.Next());
        }

        a.TryGet<RelationComponent>(out var rc);

        Assert.Single(rc.Relations);

        Assert.Equal(
            10.0,
            rc.Relations[0].Force,
            precision: 5);
    }

    [Fact]
    public void Erosion_RelationFamiliale_AuDessusDuPlancher_DescendJusquAuPlancher()
    {
        var world = CréerWorld();

        var system = CréerSystem(
            erosion: 5.0,
            plancherFamilial: 10.0,
            forceInitiale: 12.0);

        var a = world.Spawn();
        var b = world.Spawn();

        a.Set(new RelationComponent());

        system.EnregistrerInteraction(
            world,
            Tick.Zero,
            a.Id,
            b.Id,
            TypeRelation.Familiale,
            0,
            "création");

        a.TryGet<RelationComponent>(out var rc);

        var relation =
            rc.Relations.Single();

        Assert.Equal(
            12.0,
            relation.Force,
            precision: 5);

        system.Update(
            world,
            Tick.Zero.Next());

        Assert.Equal(
            10.0,
            relation.Force,
            precision: 5);
    }

    [Fact]
    public void Erosion_RelationFamiliale_DejaSousPlancher_NeRemontePas()
    {
        var world = CréerWorld();

        var system = CréerSystem(
            erosion: 1.0,
            plancherFamilial: 10.0,
            forceInitiale: 40.0);

        var a = world.Spawn();
        var b = world.Spawn();

        a.Set(new RelationComponent());

        // Création à Force 40.
        system.EnregistrerInteraction(
            world,
            Tick.Zero,
            a.Id,
            b.Id,
            TypeRelation.Familiale,
            0.0,
            "création relation familiale");

        a.TryGet<RelationComponent>(out var rc);

        var relation =
            rc.Relations.Single();

        Assert.Equal(
            40.0,
            relation.Force,
            precision: 5);

        // Interaction volontaire :
        // 40 - 35 = 5.
        // La relation passe donc sous le plancher de 10.
        system.EnregistrerInteraction(
            world,
            Tick.Zero,
            a.Id,
            b.Id,
            TypeRelation.Familiale,
            -35.0,
            "conflit grave");

        Assert.Equal(
            5.0,
            relation.Force,
            precision: 5);

        // Au Tick suivant, l'érosion naturelle ne doit
        // ni diminuer ni remonter une relation Familiale
        // déjà sous son plancher.
        system.Update(
            world,
            Tick.Zero.Next());

        Assert.Equal(
            5.0,
            relation.Force,
            precision: 5);
    }

    [Fact]
    public void Erosion_RelationFamiliale_PeutDisparaitreParInteractionNegative()
    {
        // ENGINE-008 v1.3 :
        // le plancher familial protège contre l'érosion,
        // mais pas contre une rupture causée par une interaction.
        var world = CréerWorld();

        var system = CréerSystem(
            plancherFamilial: 10.0,
            forceInitiale: 5.0);

        var a = world.Spawn();
        var b = world.Spawn();

        a.Set(new RelationComponent());

        system.EnregistrerInteraction(
            world,
            Tick.Zero,
            a.Id,
            b.Id,
            TypeRelation.Familiale,
            -100.0,
            "rupture totale");

        a.TryGet<RelationComponent>(out var rc);

        Assert.Empty(rc.Relations);
    }

    // ── Création et disparition ──────────────────────────────────────────────

    [Fact]
    public void EnregistrerInteraction_CreeRelation_SiInexistante()
    {
        var world = CréerWorld();
        var system = CréerSystem();

        var a = world.Spawn();
        var b = world.Spawn();

        a.Set(new RelationComponent());

        system.EnregistrerInteraction(
            world,
            Tick.Zero,
            a.Id,
            b.Id,
            TypeRelation.Amicale,
            5.0,
            "rencontre");

        a.TryGet<RelationComponent>(out var rc);

        Assert.Single(rc.Relations);

        Assert.Equal(
            TypeRelation.Amicale,
            rc.Relations[0].Type);
    }

    [Fact]
    public void Erosion_SupprimeRelation_QuandForceAtteint0()
    {
        var world = CréerWorld();

        var system = CréerSystem(
            erosion: 100.0,
            forceInitiale: 5.0);

        var a = world.Spawn();
        var b = world.Spawn();

        a.Set(new RelationComponent());

        system.EnregistrerInteraction(
            world,
            Tick.Zero,
            a.Id,
            b.Id,
            TypeRelation.Amicale,
            0.0,
            "neutre");

        system.Update(
            world,
            Tick.Zero.Next());

        a.TryGet<RelationComponent>(out var rc);

        Assert.Empty(rc.Relations);
    }

    // ── Épisodes ─────────────────────────────────────────────────────────────

    [Fact]
    public void EnregistrerInteraction_CreeEpisode_SiImpactAuDessusDuSeuil()
    {
        var world = CréerWorld();

        var system = CréerSystem(
            seuilEpisode: 10.0);

        var a = world.Spawn();
        var b = world.Spawn();

        a.Set(new RelationComponent());

        system.EnregistrerInteraction(
            world,
            Tick.Zero,
            a.Id,
            b.Id,
            TypeRelation.Amicale,
            15.0,
            "moment fort");

        a.TryGet<RelationComponent>(out var rc);

        Assert.Single(
            rc.Relations[0].Episodes);
    }

    [Fact]
    public void EnregistrerInteraction_NeCreePasEpisode_SiImpactEnDessousDuSeuil()
    {
        var world = CréerWorld();

        var system = CréerSystem(
            seuilEpisode: 10.0);

        var a = world.Spawn();
        var b = world.Spawn();

        a.Set(new RelationComponent());

        system.EnregistrerInteraction(
            world,
            Tick.Zero,
            a.Id,
            b.Id,
            TypeRelation.Amicale,
            3.0,
            "interaction banale");

        a.TryGet<RelationComponent>(out var rc);

        Assert.Empty(
            rc.Relations[0].Episodes);
    }

    [Fact]
    public void EnregistrerInteraction_EvicteLePlusAncien_QuandCapaciteDepassee()
    {
        var world = CréerWorld();

        var system = CréerSystem(
            seuilEpisode: 1.0,
            capacite: 2);

        var a = world.Spawn();
        var b = world.Spawn();

        a.Set(new RelationComponent());

        var tick1 = Tick.Zero;
        var tick2 = tick1.Next();
        var tick3 = tick2.Next();

        system.EnregistrerInteraction(
            world,
            tick1,
            a.Id,
            b.Id,
            TypeRelation.Amicale,
            5.0,
            "épisode 1");

        system.EnregistrerInteraction(
            world,
            tick2,
            a.Id,
            b.Id,
            TypeRelation.Amicale,
            5.0,
            "épisode 2");

        system.EnregistrerInteraction(
            world,
            tick3,
            a.Id,
            b.Id,
            TypeRelation.Amicale,
            5.0,
            "épisode 3");

        a.TryGet<RelationComponent>(out var rc);

        Assert.Equal(
            2,
            rc.Relations[0].Episodes.Count);

        // L'épisode le plus ancien, tick1, doit avoir été évincé.
        Assert.All(
            rc.Relations[0].Episodes,
            episode =>
                Assert.NotEqual(
                    tick1,
                    episode.Tick));
    }

    // ── EffectApplicator ─────────────────────────────────────────────────────

    [Fact]
    public void PopulationEffectApplicator_TraiteRelationInteractionEffect()
    {
        var world = CréerWorld();

        var relationSystem =
            CréerSystem();

        var skillSystem =
            new SkillSystem();

        var applicator =
            new PopulationEffectApplicator(
                relationSystem,
                skillSystem);

        var a = world.Spawn();
        var b = world.Spawn();

        a.Set(new RelationComponent());

        var effect =
            new RelationInteractionEffect(
                a.Id,
                b.Id,
                TypeRelation.Amicale,
                20.0,
                "test effect");

        applicator.Appliquer(
            effect,
            world,
            Tick.Zero);

        a.TryGet<RelationComponent>(out var rc);

        Assert.Single(rc.Relations);
    }
}
