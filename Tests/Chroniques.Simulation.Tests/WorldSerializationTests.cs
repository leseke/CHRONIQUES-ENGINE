using Chroniques.Simulation.Kernel;
using Chroniques.Simulation.Kernel.Persistence;
using Xunit;

namespace Chroniques.Simulation.Tests;

/// <summary>
/// Vérifie le critère de sortie v0.1 de MASTER-005 : « un World vide se
/// sauvegarde et se recharge à l'identique. »
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
}
