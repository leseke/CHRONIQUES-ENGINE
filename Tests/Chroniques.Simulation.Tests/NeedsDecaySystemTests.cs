using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;
using Chroniques.Simulation.Systems;
using Xunit;

namespace Chroniques.Simulation.Tests;

/// <summary>
/// Vérifie GDB-004B : les besoins évoluent avec le temps, restent bornés, et
/// s'influencent mutuellement en cas de détresse.
/// </summary>
public class NeedsDecaySystemTests
{
    [Fact]
    public void Un_tick_reduit_la_faim_et_la_fatigue()
    {
        var world = new World(seed: 1);
        var habitant = world.Spawn();
        habitant.Set(new NeedsComponent());
        var system = new NeedsDecaySystem(faimDeclinParTick: 5, fatigueDeclinParTick: 3);

        system.Update(world, new Tick(1));

        habitant.TryGet<NeedsComponent>(out var needs);
        Assert.Equal(95, needs.Faim);
        Assert.Equal(97, needs.Fatigue);
    }

    [Fact]
    public void Les_besoins_ne_descendent_jamais_sous_zero()
    {
        var world = new World(seed: 1);
        var habitant = world.Spawn();
        habitant.Set(new NeedsComponent { Faim = 2 });
        var system = new NeedsDecaySystem(faimDeclinParTick: 50);

        system.Update(world, new Tick(1));

        habitant.TryGet<NeedsComponent>(out var needs);
        Assert.Equal(0, needs.Faim);
    }

    [Fact]
    public void Une_detresse_de_faim_reduit_le_moral()
    {
        var world = new World(seed: 1);
        var habitant = world.Spawn();
        habitant.Set(new NeedsComponent { Faim = 15, Moral = 100 });
        var system = new NeedsDecaySystem(faimDeclinParTick: 0, seuilDetresse: 20, moralDeclinEnDetresse: 10);

        system.Update(world, new Tick(1));

        habitant.TryGet<NeedsComponent>(out var needs);
        Assert.Equal(90, needs.Moral);
    }

    [Fact]
    public void Aucune_detresse_ne_laisse_le_moral_intact()
    {
        var world = new World(seed: 1);
        var habitant = world.Spawn();
        habitant.Set(new NeedsComponent { Faim = 80, Fatigue = 80, Moral = 100 });
        var system = new NeedsDecaySystem(faimDeclinParTick: 1, fatigueDeclinParTick: 1, seuilDetresse: 20);

        system.Update(world, new Tick(1));

        habitant.TryGet<NeedsComponent>(out var needs);
        Assert.Equal(100, needs.Moral);
    }

    [Fact]
    public void La_sante_nest_jamais_affectee_par_le_simple_ecoulement_du_temps()
    {
        // GDB-022 (maladies, blessures) n'existe pas encore : seule
        // l'implémentation future de ces systèmes doit pouvoir faire
        // varier Sante, jamais le simple passage d'un Tick.
        var world = new World(seed: 1);
        var habitant = world.Spawn();
        habitant.Set(new NeedsComponent { Sante = 100 });
        var system = new NeedsDecaySystem();

        for (var i = 0; i < 50; i++)
        {
            system.Update(world, new Tick(i));
        }

        habitant.TryGet<NeedsComponent>(out var needs);
        Assert.Equal(100, needs.Sante);
    }

    [Fact]
    public void Une_entity_sans_needs_component_est_ignoree_sans_erreur()
    {
        var world = new World(seed: 1);
        world.Spawn();
        var system = new NeedsDecaySystem();

        var exception = Record.Exception(() => system.Update(world, new Tick(1)));

        Assert.Null(exception);
    }
}
