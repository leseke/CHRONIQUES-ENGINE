using Chroniques.Simulation.Actions;
using Chroniques.Simulation.Actions.Exemples;
using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;
using Xunit;

namespace Chroniques.Simulation.Tests;

/// <summary>
/// Preuve de bout en bout du pipeline (ENGINE-006) : Intent → Planner →
/// Plan → Action Instance → Execution Engine → Outcome → Effects → Events,
/// à travers l'unique Verbe de démonstration « Se reposer »
/// (Actions/Exemples). Ne teste aucune règle nouvelle --- uniquement que
/// l'assemblage complet fonctionne réellement, pas seulement chaque pièce
/// isolément (déjà couvert par ActionInstanceTests et PlanTests).
/// </summary>
public class PipelineIntegrationTests
{
    private static PipelineRunner CreerRunner() => new(new SimplePlanner(), new SimpleExecutionEngine());

    [Fact]
    public void Le_pipeline_complet_restaure_la_fatigue_et_publie_un_event()
    {
        var world = new World(seed: 1);
        var acteur = world.Spawn();
        acteur.Set(new NeedsComponent { Fatigue = 50 });
        var intent = new Intent(acteur.Id, "se_reposer", Priorite: 1);

        var instance = CreerRunner().ExecuterSeReposer(intent, acteur.Id, world);

        Assert.Equal(ActionState.Archived, instance.State);
        Assert.Equal(OutcomeForme.Reussite, instance.Outcome?.Forme);

        world.TryGetEntity(acteur.Id, out var acteurApres);
        acteurApres.TryGet<NeedsComponent>(out var besoins);
        Assert.Equal(70.0, besoins.Fatigue);

        var evenement = Assert.Single(world.Events);
        Assert.Equal("besoin.fatigue.restauree", evenement.Kind);
        Assert.Equal(acteur.Id, evenement.Source);
    }

    [Fact]
    public void La_fatigue_ne_depasse_jamais_100()
    {
        var world = new World(seed: 1);
        var acteur = world.Spawn();
        acteur.Set(new NeedsComponent { Fatigue = 95 });
        var intent = new Intent(acteur.Id, "se_reposer", Priorite: 1);

        CreerRunner().ExecuterSeReposer(intent, acteur.Id, world);

        world.TryGetEntity(acteur.Id, out var acteurApres);
        acteurApres.TryGet<NeedsComponent>(out var besoins);
        Assert.Equal(100.0, besoins.Fatigue);
    }

    [Fact]
    public void Un_acteur_sans_needscomponent_echoue_sans_effet_ni_event()
    {
        var world = new World(seed: 1);
        var acteur = world.Spawn(); // aucun NeedsComponent attaché

        var intent = new Intent(acteur.Id, "se_reposer", Priorite: 1);

        var instance = CreerRunner().ExecuterSeReposer(intent, acteur.Id, world);

        Assert.Equal(ActionState.Archived, instance.State);
        Assert.Equal(OutcomeForme.Echec, instance.Outcome?.Forme);
        Assert.Empty(world.Events);
    }

    [Fact]
    public void Un_intent_non_reconnu_par_le_planner_est_refuse()
    {
        var world = new World(seed: 1);
        var acteur = world.Spawn();
        var intent = new Intent(acteur.Id, "objectif_inconnu", Priorite: 1);

        Assert.Throws<NotSupportedException>(
            () => CreerRunner().ExecuterSeReposer(intent, acteur.Id, world));
    }
}
