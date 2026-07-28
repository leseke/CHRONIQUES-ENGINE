using Chroniques.Simulation.Kernel;
using Xunit;

namespace Chroniques.Simulation.Tests;

/// <summary>
/// Vérifie CORE-000-G : « les Events sont immuables ». Un GameEvent ne peut
/// être modifié après création ; toute évolution produit un nouvel Event.
/// </summary>
public class GameEventTests
{
    [Fact]
    public void Un_event_conserve_ses_valeurs_dorigine()
    {
        var source = EntityId.New();
        var occurredEvent = GameEvent.Create(new Tick(5), "vie.naissance", source: source);

        Assert.Equal(new Tick(5), occurredEvent.OccurredAt);
        Assert.Equal("vie.naissance", occurredEvent.Kind);
        Assert.Equal(source, occurredEvent.Source);
    }

    [Fact]
    public void Modifier_un_event_produit_une_nouvelle_instance_distincte()
    {
        var original = GameEvent.Create(Tick.Zero, "vie.naissance");

        var modifie = original with { Kind = "vie.mort" };

        Assert.NotEqual(original, modifie);
        Assert.Equal("vie.naissance", original.Kind);
        Assert.Equal("vie.mort", modifie.Kind);
    }

    [Fact]
    public void Deux_events_avec_le_meme_id_sont_egaux_par_valeur()
    {
        var id = Guid.NewGuid();
        var tick = new Tick(1);

        var a = new GameEvent(id, tick, "test", null, null);
        var b = new GameEvent(id, tick, "test", null, null);

        Assert.Equal(a, b);
    }
}
