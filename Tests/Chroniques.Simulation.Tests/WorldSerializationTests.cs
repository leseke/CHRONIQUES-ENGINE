using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;
using Chroniques.Simulation.Persistence;
using Xunit;

namespace Chroniques.Simulation.Tests;

/// <summary>
/// Vérifie le critère de sortie v0.1 de MASTER-005 : « un World vide se
/// sauvegarde et se recharge à l'identique », étendu en v0.2 aux Components
/// métier (NeedsComponent).
/// </summary>
public class WorldSerializationTests
{
    [Fact]
    public void Un_world_vide_se_recharge_a_lidentique()
    {
        var original = new World(seed: 1234);

        var json = WorldRepository.Save(original);
        var recharge = WorldRepository.Load(json);

        Assert.Equal(original.Seed, recharge.Seed);
        Assert.Equal(original.CurrentTick, recharge.CurrentTick);
        Assert.Empty(recharge.Entities);
        Assert.Empty(recharge.Events);
    }

    [Fact]
    public void Un_world_avec_des_entities_et_des_events_conserve_leurs_identites()
    {
        var original = new World(seed: 7);
        var premiere = original.Spawn();
        var seconde = original.Spawn();
        original.Advance();
        original.Publish(GameEvent.Create(original.CurrentTick, "vie.naissance", source: premiere.Id));

        var json = WorldRepository.Save(original);
        var recharge = WorldRepository.Load(json);

        Assert.Equal(original.CurrentTick, recharge.CurrentTick);
        Assert.Equal(2, recharge.Entities.Count);
        Assert.True(recharge.TryGetEntity(premiere.Id, out _));
        Assert.True(recharge.TryGetEntity(seconde.Id, out _));

        var evenementRecharge = Assert.Single(recharge.Events);
        Assert.Equal("vie.naissance", evenementRecharge.Kind);
        Assert.Equal(premiere.Id, evenementRecharge.Source);
    }

    [Fact]
    public void Le_needs_component_dune_entity_survit_au_rechargement()
    {
        var original = new World(seed: 42);
        var habitant = original.Spawn();
        habitant.Set(new NeedsComponent { Faim = 63, Fatigue = 40, Sante = 90, Moral = 55 });

        var json = WorldRepository.Save(original);
        var recharge = WorldRepository.Load(json);

        Assert.True(recharge.TryGetEntity(habitant.Id, out var habitantRecharge));
        Assert.True(habitantRecharge.TryGet<NeedsComponent>(out var needsRecharge));
        Assert.Equal(63, needsRecharge.Faim);
        Assert.Equal(40, needsRecharge.Fatigue);
        Assert.Equal(90, needsRecharge.Sante);
        Assert.Equal(55, needsRecharge.Moral);
    }

    [Fact]
    public void Une_entity_sans_needs_component_recharge_sans_en_avoir_un()
    {
        var original = new World(seed: 5);
        original.Spawn();

        var json = WorldRepository.Save(original);
        var recharge = WorldRepository.Load(json);

        var entity = Assert.Single(recharge.Entities);
        Assert.False(entity.Has<NeedsComponent>());
    }
}
