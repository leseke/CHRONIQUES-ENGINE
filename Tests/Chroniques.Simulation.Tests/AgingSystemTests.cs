using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;
using Chroniques.Simulation.Systems;
using Xunit;

namespace Chroniques.Simulation.Tests;

/// <summary>
/// Vérifie GDB-008C : un habitant traverse enfance → adolescence → âge
/// adulte → maturité → vieillesse → mort, et complète le critère de sortie
/// v0.2 de MASTER-005 (« un personnage naît, vit ses besoins année après
/// année, et meurt »).
/// </summary>
public class AgingSystemTests
{
    [Fact]
    public void Douze_ticks_font_avancer_lage_dune_annee()
    {
        var world = new World(seed: 1);
        var habitant = world.Spawn();
        habitant.Set(new AgeComponent());
        var system = new AgingSystem();

        for (var i = 1; i <= CalendrierSimule.MoisParAn; i++)
        {
            system.Update(world, new Tick(i));
        }

        habitant.TryGet<AgeComponent>(out var age);
        Assert.Equal(1, age.Annees);
    }

    [Fact]
    public void Onze_ticks_ne_suffisent_pas_a_faire_avancer_lage()
    {
        // 3 Ticks par saison x 4 saisons par an = 12 Ticks par an. En rester
        // à 11 ne doit donc pas encore compter une année pleine.
        var world = new World(seed: 1);
        var habitant = world.Spawn();
        habitant.Set(new AgeComponent());
        var system = new AgingSystem();

        for (var i = 1; i < CalendrierSimule.MoisParAn; i++)
        {
            system.Update(world, new Tick(i));
        }

        habitant.TryGet<AgeComponent>(out var age);
        Assert.Equal(0, age.Annees);
    }

    [Fact]
    public void Un_habitant_nait_vivant_puis_bascule_en_enfance_des_le_premier_tick()
    {
        var world = new World(seed: 1);
        var habitant = world.Spawn();
        habitant.Set(new AgeComponent());
        Assert.Equal("vivant", habitant.Lifecycle.CurrentState.Name);

        new AgingSystem().Update(world, new Tick(1));

        Assert.Equal("enfance", habitant.Lifecycle.CurrentState.Name);
    }

    [Fact]
    public void Un_habitant_atteint_lage_adulte_au_seuil_configure()
    {
        var world = new World(seed: 1);
        var habitant = world.Spawn();
        habitant.Set(new AgeComponent { Annees = 17 });
        var system = new AgingSystem(seuilAgeAdulte: 18);

        // Un multiple de CalendrierSimule.MoisParAn, seul moment où l'âge
        // s'incrémente réellement (17 → 18).
        system.Update(world, new Tick(CalendrierSimule.MoisParAn));

        Assert.Equal("age_adulte", habitant.Lifecycle.CurrentState.Name);
    }

    [Fact]
    public void Un_habitant_meurt_en_atteignant_lesperance_de_vie_configuree()
    {
        var world = new World(seed: 1);
        var habitant = world.Spawn();
        habitant.Set(new AgeComponent { Annees = 79 });
        var system = new AgingSystem(esperanceDeVie: 80);

        // Un multiple de CalendrierSimule.MoisParAn, seul moment où l'âge
        // s'incrémente réellement (79 → 80).
        system.Update(world, new Tick(CalendrierSimule.MoisParAn));

        Assert.Equal("mort", habitant.Lifecycle.CurrentState.Name);
    }

    [Fact]
    public void La_mort_publie_un_event_vie_mort_observable_sur_le_world()
    {
        var world = new World(seed: 1);
        var habitant = world.Spawn();
        habitant.Set(new AgeComponent { Annees = 79 });
        var system = new AgingSystem(esperanceDeVie: 80);

        var tickDeLaMort = new Tick(2 * CalendrierSimule.MoisParAn);
        system.Update(world, tickDeLaMort);

        var evenement = Assert.Single(world.Events);
        Assert.Equal("vie.mort", evenement.Kind);
        Assert.Equal(habitant.Id, evenement.Source);
        Assert.Equal(tickDeLaMort, evenement.OccurredAt);
    }

    [Fact]
    public void Un_habitant_mort_ne_vieillit_plus()
    {
        var world = new World(seed: 1);
        var habitant = world.Spawn();
        habitant.Set(new AgeComponent { Annees = 80 });
        var system = new AgingSystem(esperanceDeVie: 80);

        system.Update(world, new Tick(1));
        system.Update(world, new Tick(2));
        system.Update(world, new Tick(3));

        habitant.TryGet<AgeComponent>(out var age);
        Assert.Equal(80, age.Annees);
        Assert.Equal("mort", habitant.Lifecycle.CurrentState.Name);
    }

    [Fact]
    public void Un_habitant_deja_mort_ne_republie_pas_devent_de_mort()
    {
        var world = new World(seed: 1);
        var habitant = world.Spawn();
        habitant.Set(new AgeComponent { Annees = 80 });
        var system = new AgingSystem(esperanceDeVie: 80);

        system.Update(world, new Tick(1));
        system.Update(world, new Tick(2));

        Assert.Single(world.Events);
    }

    [Fact]
    public void Une_entity_sans_age_component_est_ignoree_sans_erreur()
    {
        var world = new World(seed: 1);
        world.Spawn();
        var system = new AgingSystem();

        var exception = Record.Exception(() => system.Update(world, new Tick(1)));

        Assert.Null(exception);
    }
}
