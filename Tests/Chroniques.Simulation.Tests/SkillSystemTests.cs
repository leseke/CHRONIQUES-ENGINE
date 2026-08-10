namespace Chroniques.Simulation.Tests;

using Xunit;
using Chroniques.Simulation.Actions;
using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;
using Chroniques.Simulation.Systems;

/// <summary>
/// Tests de SkillSystem (ENGINE-008, section 8).
/// Couvre : gain décroissant en approchant 100, absence de déclin avant le
/// seuil d'inactivité, déclin après, traitement d'un SkillPracticeEffect.
/// </summary>
public sealed class SkillSystemTests
{
    private static World CréerWorld() => new(seed: 42);

    private static SkillSystem CréerSystem(
        double facteurGain = 10.0,
        int seuilInactivite = 5,
        double declin = 1.0)
        => new(facteurGain, seuilInactivite, declin);

    // ── Gain décroissant ─────────────────────────────────────────────────────

    [Fact]
    public void Pratiquer_GainMaximal_QuandNiveauEst0()
    {
        var world = CréerWorld();
        var system = CréerSystem(facteurGain: 10.0);
        var entity = world.Spawn();

        entity.Set(new SkillComponent());

        system.Pratiquer(
            world,
            Tick.Zero,
            entity.Id,
            "cuisine");

        entity.TryGet<SkillComponent>(out var sc);

        Assert.Equal(
            10.0,
            sc.Competences["cuisine"].Niveau,
            precision: 5);
    }

    [Fact]
    public void Pratiquer_GainDecroissant_AvecLeNiveau()
    {
        var world = CréerWorld();
        var system = CréerSystem(facteurGain: 10.0);
        var entity = world.Spawn();

        entity.Set(new SkillComponent());

        // Premier gain depuis le niveau 0.
        system.Pratiquer(
            world,
            Tick.Zero,
            entity.Id,
            "cuisine");

        entity.TryGet<SkillComponent>(out var sc);

        var niveauApresPremierePratique =
            sc.Competences["cuisine"].Niveau;

        var gain1 = niveauApresPremierePratique;

        // Deuxième pratique depuis un niveau supérieur à 0.
        system.Pratiquer(
            world,
            Tick.Zero,
            entity.Id,
            "cuisine");

        var niveauApresDeuxiemePratique =
            sc.Competences["cuisine"].Niveau;

        var gain2 =
            niveauApresDeuxiemePratique -
            niveauApresPremierePratique;

        Assert.True(
            gain2 < gain1,
            "Le gain doit être strictement décroissant avec le Niveau.");
    }

    [Fact]
    public void Pratiquer_GainDevientNul_EnApprochantNiveau100()
    {
        var world = CréerWorld();
        var system = CréerSystem(facteurGain: 10.0);
        var entity = world.Spawn();

        entity.Set(new SkillComponent());

        /*
         * On n'accède volontairement pas à SkillComponent.ObtenirOuCreer(),
         * qui est une méthode interne.
         *
         * Le niveau est amené naturellement vers 100 uniquement en utilisant
         * l'API publique SkillSystem.Pratiquer().
         */
        for (var i = 0; i < 1000; i++)
        {
            system.Pratiquer(
                world,
                Tick.Zero,
                entity.Id,
                "cuisine");
        }

        entity.TryGet<SkillComponent>(out var sc);

        var niveauAvant =
            sc.Competences["cuisine"].Niveau;

        Assert.Equal(
            100.0,
            niveauAvant,
            precision: 5);

        // Une pratique supplémentaire ne doit plus produire
        // d'augmentation significative.
        system.Pratiquer(
            world,
            Tick.Zero,
            entity.Id,
            "cuisine");

        var niveauApres =
            sc.Competences["cuisine"].Niveau;

        Assert.Equal(
            niveauAvant,
            niveauApres,
            precision: 10);

        Assert.InRange(
            niveauApres,
            0.0,
            100.0);
    }

    // ── Déclin par inactivité ────────────────────────────────────────────────

    [Fact]
    public void Update_PasDeDeclin_AvantLeSeuilDinactivite()
    {
        var world = CréerWorld();
        var system = CréerSystem(
            seuilInactivite: 10,
            declin: 1.0);

        var entity = world.Spawn();

        entity.Set(new SkillComponent());

        system.Pratiquer(
            world,
            Tick.Zero,
            entity.Id,
            "cuisine");

        entity.TryGet<SkillComponent>(out var sc);

        var niveauApresPratique =
            sc.Competences["cuisine"].Niveau;

        // 5 Ticks inactifs, seuil = 10 :
        // aucun déclin ne doit être appliqué.
        var tick5 = Tick.Zero;

        for (var i = 0; i < 5; i++)
        {
            tick5 = tick5.Next();
        }

        system.Update(world, tick5);

        Assert.Equal(
            niveauApresPratique,
            sc.Competences["cuisine"].Niveau,
            precision: 5);
    }

    [Fact]
    public void Update_DeclinApplique_ApresLeSeuilDinactivite()
    {
        var world = CréerWorld();
        var system = CréerSystem(
            seuilInactivite: 3,
            declin: 1.0);

        var entity = world.Spawn();

        entity.Set(new SkillComponent());

        system.Pratiquer(
            world,
            Tick.Zero,
            entity.Id,
            "cuisine");

        entity.TryGet<SkillComponent>(out var sc);

        var niveauApresPratique =
            sc.Competences["cuisine"].Niveau;

        // 5 Ticks inactifs, seuil = 3 :
        // le déclin doit être appliqué.
        var tick5 = Tick.Zero;

        for (var i = 0; i < 5; i++)
        {
            tick5 = tick5.Next();
        }

        system.Update(world, tick5);

        Assert.True(
            sc.Competences["cuisine"].Niveau <
            niveauApresPratique);
    }

    // ── EffectApplicator ─────────────────────────────────────────────────────

    [Fact]
    public void PopulationEffectApplicator_TraiteSkillPracticeEffect()
    {
        var world = CréerWorld();

        var relationSystem =
            new RelationSystem();

        var skillSystem =
            CréerSystem();

        var applicator =
            new PopulationEffectApplicator(
                relationSystem,
                skillSystem);

        var entity = world.Spawn();

        entity.Set(new SkillComponent());

        var effect =
            new SkillPracticeEffect(
                entity.Id,
                "sculpture");

        applicator.Appliquer(
            effect,
            world,
            Tick.Zero);

        entity.TryGet<SkillComponent>(out var sc);

        Assert.True(
            sc.Competences.ContainsKey("sculpture"));

        Assert.True(
            sc.Competences["sculpture"].Niveau > 0);
    }
}
