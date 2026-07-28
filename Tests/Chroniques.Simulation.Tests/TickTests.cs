using Chroniques.Simulation.Kernel;
using Xunit;

namespace Chroniques.Simulation.Tests;

/// <summary>
/// Vérifie CORE-000-G : « Time fournit un ordre ». Un Tick avance toujours
/// strictement, jamais en arrière ni en place.
/// </summary>
public class TickTests
{
    [Fact]
    public void Next_produit_toujours_un_tick_strictement_superieur()
    {
        var tick = Tick.Zero;

        var suivant = tick.Next();

        Assert.True(suivant > tick);
    }

    [Fact]
    public void Lordre_des_ticks_est_transitif()
    {
        var t0 = Tick.Zero;
        var t1 = t0.Next();
        var t2 = t1.Next();

        Assert.True(t0 < t1);
        Assert.True(t1 < t2);
        Assert.True(t0 < t2);
    }
}
