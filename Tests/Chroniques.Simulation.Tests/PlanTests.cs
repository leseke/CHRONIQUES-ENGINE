using Chroniques.Simulation.Actions;
using Chroniques.Simulation.Kernel;
using Xunit;

namespace Chroniques.Simulation.Tests;

/// <summary>
/// Vérifie ACT-002-I : dépendances acycliques entre étapes, et
/// réévaluation d'un Plan (suspendre, adapter, abandonner, reconstruire).
/// </summary>
public class PlanTests
{
    private static ActionDefinition CreerDefinitionMinimale(string verbe = "TestVerbe") => new(
        Verbe: verbe,
        Pattern: "TestPattern",
        Principe: "TestPrincipe",
        Contract: new ActionContract(
            Inputs: Array.Empty<InputSlot>(),
            Preconditions: Array.Empty<Condition>(),
            Constraints: Array.Empty<Condition>(),
            Costs: Array.Empty<CostSlot>(),
            Effects: Array.Empty<ConsequenceTemplate>(),
            Events: Array.Empty<EventTemplate>()));

    private static Intent CreerIntentMinimal() => new(EntityId.New(), "Objectif de test", Priorite: 1);

    [Fact]
    public void Un_plan_a_une_seule_etape_sans_dependance_est_accepte()
    {
        var etape = new PlanStep(CreerDefinitionMinimale(), PlanStepMode.Sequentiel, Array.Empty<PlanStep>());

        var plan = new Plan(CreerIntentMinimal(), new[] { etape });

        Assert.Single(plan.Steps);
    }

    [Fact]
    public void Une_dependance_circulaire_directe_est_refusee()
    {
        // étape A dépend de B, B dépend de A --- construit via une liste
        // mutable le temps de la construction, car les records sont
        // immuables une fois créés.
        var etapeA = new PlanStep(CreerDefinitionMinimale("A"), PlanStepMode.Sequentiel, new List<PlanStep>());
        var etapeB = new PlanStep(CreerDefinitionMinimale("B"), PlanStepMode.Sequentiel, new List<PlanStep> { etapeA });
        ((List<PlanStep>)etapeA.DependsOn).Add(etapeB);

        Assert.Throws<InvalidOperationException>(
            () => new Plan(CreerIntentMinimal(), new[] { etapeA, etapeB }));
    }

    [Fact]
    public void Deux_etapes_independantes_structurellement_identiques_coexistent()
    {
        // Deux étapes distinctes, sans dépendance entre elles, mais avec
        // les mêmes valeurs (même Definition, même Mode, aucune
        // dépendance) --- PlanStep étant un record, ces deux étapes sont
        // structurellement égales. Ce test vérifie qu'un Plan les accepte
        // toutes les deux sans les confondre en une seule.
        var definition = CreerDefinitionMinimale();
        var etape1 = new PlanStep(definition, PlanStepMode.Sequentiel, Array.Empty<PlanStep>());
        var etape2 = new PlanStep(definition, PlanStepMode.Sequentiel, Array.Empty<PlanStep>());

        var plan = new Plan(CreerIntentMinimal(), new[] { etape1, etape2 });

        Assert.Equal(2, plan.Steps.Count);
    }

    [Fact]
    public void Une_chaine_de_dependances_sans_cycle_est_acceptee()
    {
        var etapeA = new PlanStep(CreerDefinitionMinimale("A"), PlanStepMode.Sequentiel, Array.Empty<PlanStep>());
        var etapeB = new PlanStep(CreerDefinitionMinimale("B"), PlanStepMode.Sequentiel, new[] { etapeA });
        var etapeC = new PlanStep(CreerDefinitionMinimale("C"), PlanStepMode.Sequentiel, new[] { etapeB });

        var plan = new Plan(CreerIntentMinimal(), new[] { etapeA, etapeB, etapeC });

        Assert.Equal(3, plan.Steps.Count);
    }

    [Fact]
    public void Suspendre_puis_reprendre_fonctionne()
    {
        var etape = new PlanStep(CreerDefinitionMinimale(), PlanStepMode.Sequentiel, Array.Empty<PlanStep>());
        var plan = new Plan(CreerIntentMinimal(), new[] { etape });

        plan.Suspendre();
        Assert.Equal(PlanStatus.Suspendu, plan.Status);

        plan.Reprendre();
        Assert.Equal(PlanStatus.Actif, plan.Status);
    }

    [Fact]
    public void Abandonner_empeche_toute_reevaluation_ulterieure()
    {
        var etape = new PlanStep(CreerDefinitionMinimale(), PlanStepMode.Sequentiel, Array.Empty<PlanStep>());
        var plan = new Plan(CreerIntentMinimal(), new[] { etape });

        plan.Abandonner();

        Assert.Throws<InvalidOperationException>(() => plan.Suspendre());
        Assert.Throws<InvalidOperationException>(() => plan.Adapter(new[] { etape }));
    }

    [Fact]
    public void Adapter_remplace_les_etapes_sans_changer_lintent()
    {
        var etapeInitiale = new PlanStep(CreerDefinitionMinimale("Initiale"), PlanStepMode.Sequentiel, Array.Empty<PlanStep>());
        var intent = CreerIntentMinimal();
        var plan = new Plan(intent, new[] { etapeInitiale });

        var nouvelleEtape = new PlanStep(CreerDefinitionMinimale("Adaptee"), PlanStepMode.Sequentiel, Array.Empty<PlanStep>());
        plan.Adapter(new[] { nouvelleEtape });

        Assert.Same(intent, plan.Intent);
        Assert.Equal("Adaptee", plan.Steps[0].Definition.Verbe);
    }

    private sealed class PlannerDeTest : IPlanner
    {
        private readonly PlanStep _etape;

        public PlannerDeTest(PlanStep etape) => _etape = etape;

        public Plan CreatePlan(Intent intent, World world) => new(intent, new[] { _etape });
    }

    [Fact]
    public void Reconstruire_adopte_les_etapes_du_nouveau_plan_sans_changer_lintent()
    {
        var etapeInitiale = new PlanStep(CreerDefinitionMinimale("Initiale"), PlanStepMode.Sequentiel, Array.Empty<PlanStep>());
        var intent = CreerIntentMinimal();
        var plan = new Plan(intent, new[] { etapeInitiale });

        var etapeReconstruite = new PlanStep(CreerDefinitionMinimale("Reconstruite"), PlanStepMode.Sequentiel, Array.Empty<PlanStep>());
        var planner = new PlannerDeTest(etapeReconstruite);
        var world = new World(seed: 1);

        plan.Reconstruire(planner, world);

        Assert.Same(intent, plan.Intent);
        Assert.Equal("Reconstruite", plan.Steps[0].Definition.Verbe);
        Assert.Equal(PlanStatus.Actif, plan.Status);
    }
}
