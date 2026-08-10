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

        system.Pratiquer(world, Tick.Zero, entity.Id, "cuisine");

        entity.TryGet<SkillComponent>(out var sc);
        Assert.Equal(10.0, sc.Competences["cuisine"].Niveau, precision: 5);
    }

    [Fact]
    public void Pratiquer_GainDecroissant_AVecLeNiveau()
    {
        var world = CréerWorld();
        var system = CréerSystem(facteurGain: 10.0);
        var entity = world.Spawn();
        entity.Set(new SkillComponent());

        // Premier gain depuis 0
        system.Pratiquer(world, Tick.Zero, entity.Id, "cuisine");
        entity.TryGet<SkillComponent>(out var sc);
        var gain1 = sc.Competences["cuisine"].Niveau;

        // Second gain depuis ~10
        system.Pratiquer(world, Tick.Zero, entity.Id, "cuisine");
        var gain2 = sc.Competences["cuisine"].Niveau - gain1;

        Assert.True(gain2 < gain1,
            "Le gain doit être strictement décroissant avec le Niveau.");
    }

    [Fact]
    public void Pratiquer_GainNul_QuandNiveauEst100()
    {
        var world = CréerWorld();
        var system = CréerSystem(facteurGain: 10.0);
        var entity = world.Spawn();
        entity.Set(new SkillComponent());
        entity.TryGet<SkillComponent>(out var sc);

        // Forcer le niveau à 100 directement
        var comp = sc.ObtenirOuCreer("cuisine", Tick.Zero);
        comp.Niveau = 100;

        system.Pratiquer(world, Tick.Zero, entity.Id, "cuisine");

        Assert.Equal(100.0, sc.Competences["cuisine"].Niveau, precision: 5);
    }

    // ── Déclin par inactivité ─────────────────────────────────────────────────

    [Fact]
    public void Update_PasDeDéclin_AvantLeSeuilDinactivite()
    {
        var world = CréerWorld();
        var system = CréerSystem(seuilInactivite: 10, declin: 1.0);
        var entity = world.Spawn();
        entity.Set(new SkillComponent());

        system.Pratiquer(world, Tick.Zero, entity.Id, "cuisine");
        entity.TryGet<SkillComponent>(out var sc);
        var niveauApratique = sc.Competences["cuisine"].Niveau;

        // 5 Ticks inactif, seuil est 10 : pas de déclin
        var tick5 = Tick.Zero;
        for (var i = 0; i < 5; i++) tick5 = tick5.Next();
        system.Update(world, tick5);

        Assert.Equal(niveauApratique, sc.Competences["cuisine"].Niveau, precision: 5);
    }

    [Fact]
    public void Update_DeclinAppliqué_ApresLeSeuilDinactivite()
    {
        var world = CréerWorld();
        var system = CréerSystem(seuilInactivite: 3, declin: 1.0);
        var entity = world.Spawn();
        entity.Set(new SkillComponent());

        system.Pratiquer(world, Tick.Zero, entity.Id, "cuisine");
        entity.TryGet<SkillComponent>(out var sc);
        var niveauApratique = sc.Competences["cuisine"].Niveau;

        // 5 Ticks inactif, seuil est 3 : déclin
        var tick5 = Tick.Zero;
        for (var i = 0; i < 5; i++) tick5 = tick5.Next();
        system.Update(world, tick5);

        Assert.True(sc.Competences["cuisine"].Niveau < niveauApratique);
    }

    // ── EffectApplicator ─────────────────────────────────────────────────────

    [Fact]
    public void PopulationEffectApplicator_TraiteSkillPracticeEffect()
    {
        var world = CréerWorld();
        var relSystem = new RelationSystem();
        var skillSystem = CréerSystem();
        var applicator = new PopulationEffectApplicator(relSystem, skillSystem);
        var entity = world.Spawn();
        entity.Set(new SkillComponent());

        var effect = new SkillPracticeEffect(entity.Id, "sculpture");
        applicator.Appliquer(effect, world, Tick.Zero);

        entity.TryGet<SkillComponent>(out var sc);
        Assert.True(sc.Competences.ContainsKey("sculpture"));
        Assert.True(sc.Competences["sculpture"].Niveau > 0);
    }
}
