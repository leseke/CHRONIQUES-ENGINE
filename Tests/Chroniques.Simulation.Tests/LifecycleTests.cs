using Chroniques.Simulation.Kernel;
using Xunit;

namespace Chroniques.Simulation.Tests;

/// <summary>
/// Vérifie CORE-010 : Lifecycle représente une continuité ordonnée
/// d'Events, sans jamais perdre la trace des étapes précédentes.
/// </summary>
public class LifecycleTests
{
    [Fact]
    public void Un_lifecycle_conserve_lordre_de_ses_events()
    {
        var lifecycle = new Lifecycle(Tick.Zero, new State("naissant"));

        var premier = GameEvent.Create(new Tick(1), "vie.naissance");
        var second = GameEvent.Create(new Tick(2), "vie.premier_pas");

        lifecycle.Record(premier);
        lifecycle.Record(second);

        Assert.Equal(new[] { premier, second }, lifecycle.History);
    }

    [Fact]
    public void Enregistrer_un_event_sans_nouvel_etat_conserve_letat_courant()
    {
        var etatInitial = new State("naissant");
        var lifecycle = new Lifecycle(Tick.Zero, etatInitial);

        lifecycle.Record(GameEvent.Create(new Tick(1), "vie.observation"));

        Assert.Same(etatInitial, lifecycle.CurrentState);
    }

    [Fact]
    public void Enregistrer_un_event_avec_nouvel_etat_fait_progresser_le_lifecycle()
    {
        var lifecycle = new Lifecycle(Tick.Zero, new State("naissant"));
        var etatAdulte = new State("adulte");

        lifecycle.Record(GameEvent.Create(new Tick(10), "vie.maturite"), etatAdulte);

        Assert.Same(etatAdulte, lifecycle.CurrentState);
    }
}
