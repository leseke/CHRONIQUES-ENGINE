using Chroniques.Simulation.Actions;
using Chroniques.Simulation.Kernel;
using Xunit;

namespace Chroniques.Simulation.Tests;

/// <summary>
/// Vérifie ACT-001-F (machine à états officielle), ACT-005-A section 6
/// (exactement une Cible principale) et ACT-009-A (composition d'Actions).
/// </summary>
public class ActionInstanceTests
{
    private static ActionDefinition CreerDefinitionMinimale() => new(
        Verbe: "TestVerbe",
        Pattern: "TestPattern",
        Principe: "TestPrincipe",
        Contract: new ActionContract(
            Inputs: Array.Empty<InputSlot>(),
            Preconditions: Array.Empty<Condition>(),
            Constraints: Array.Empty<Condition>(),
            Costs: Array.Empty<CostSlot>(),
            Effects: Array.Empty<ConsequenceTemplate>(),
            Events: Array.Empty<EventTemplate>()));

    private static ActionInstance CreerInstance(
        IReadOnlyList<CibleRef>? cibles = null,
        IReadOnlyList<SubActionRef>? sousActions = null,
        EntityId? acteur = null)
    {
        var acteurReel = acteur ?? EntityId.New();
        var ciblesReelles = cibles ?? new[] { new CibleRef(EntityId.New(), RoleCible.Principale) };

        return new ActionInstance(
            new Tick(1),
            CreerDefinitionMinimale(),
            acteurReel,
            ciblesReelles,
            sousActions);
    }

    // --- Cible principale (ACT-005-A, section 6) ---

    [Fact]
    public void Une_instance_sans_cible_principale_est_refusee()
    {
        var cibles = new[] { new CibleRef(EntityId.New(), RoleCible.Secondaire) };

        Assert.Throws<ArgumentException>(() => CreerInstance(cibles: cibles));
    }

    [Fact]
    public void Une_instance_avec_deux_cibles_principales_est_refusee()
    {
        var cibles = new[]
        {
            new CibleRef(EntityId.New(), RoleCible.Principale),
            new CibleRef(EntityId.New(), RoleCible.Principale)
        };

        Assert.Throws<ArgumentException>(() => CreerInstance(cibles: cibles));
    }

    [Fact]
    public void Une_instance_avec_une_principale_et_des_secondaires_est_acceptee()
    {
        var cibles = new[]
        {
            new CibleRef(EntityId.New(), RoleCible.Principale),
            new CibleRef(EntityId.New(), RoleCible.Secondaire),
            new CibleRef(EntityId.New(), RoleCible.Secondaire)
        };

        var instance = CreerInstance(cibles: cibles);

        Assert.Equal(3, instance.Cibles.Count);
    }

    [Fact]
    public void Retirer_une_cible_secondaire_ne_change_pas_letat()
    {
        var secondaire = EntityId.New();
        var cibles = new[]
        {
            new CibleRef(EntityId.New(), RoleCible.Principale),
            new CibleRef(secondaire, RoleCible.Secondaire)
        };
        var instance = CreerInstance(cibles: cibles);

        instance.RetirerCibleSecondaire(secondaire);

        Assert.Single(instance.Cibles);
        Assert.Equal(ActionState.Created, instance.State);
    }

    // --- Machine à états (ACT-001-F) ---

    [Fact]
    public void Le_chemin_nominal_complet_est_autorise()
    {
        var instance = CreerInstance();

        instance.Transition(ActionState.Validated);
        instance.Transition(ActionState.Planned);
        instance.Transition(ActionState.Prepared);
        instance.Transition(ActionState.Running);
        instance.Transition(ActionState.Succeeded);
        instance.Transition(ActionState.Resolved);
        instance.Transition(ActionState.Committed);
        instance.Transition(ActionState.Archived);

        Assert.Equal(ActionState.Archived, instance.State);
    }

    [Fact]
    public void Sauter_un_etat_est_interdit()
    {
        var instance = CreerInstance();

        Assert.Throws<InvalidOperationException>(() => instance.Transition(ActionState.Running));
    }

    [Fact]
    public void Une_invalidite_interne_transite_directement_vers_failed()
    {
        var instance = CreerInstance();

        instance.Transition(ActionState.Validated);
        instance.Transition(ActionState.Failed);

        Assert.Equal(ActionState.Failed, instance.State);
    }

    [Fact]
    public void Suspendre_puis_reprendre_fonctionne()
    {
        var instance = CreerInstance();
        instance.Transition(ActionState.Validated);
        instance.Transition(ActionState.Planned);
        instance.Transition(ActionState.Prepared);
        instance.Transition(ActionState.Running);

        instance.Transition(ActionState.Suspended);
        instance.Transition(ActionState.Running);

        Assert.Equal(ActionState.Running, instance.State);
    }

    [Fact]
    public void Interrompre_puis_echouer_fonctionne()
    {
        var instance = CreerInstance();
        instance.Transition(ActionState.Validated);
        instance.Transition(ActionState.Planned);
        instance.Transition(ActionState.Prepared);
        instance.Transition(ActionState.Running);

        instance.Transition(ActionState.Interrupted);
        instance.Transition(ActionState.Failed);

        Assert.Equal(ActionState.Failed, instance.State);
    }

    [Fact]
    public void Archived_ne_permet_plus_aucune_transition()
    {
        var instance = CreerInstance();
        instance.Transition(ActionState.Validated);
        instance.Transition(ActionState.Planned);
        instance.Transition(ActionState.Prepared);
        instance.Transition(ActionState.Running);
        instance.Transition(ActionState.Succeeded);
        instance.Transition(ActionState.Resolved);
        instance.Transition(ActionState.Committed);
        instance.Transition(ActionState.Archived);

        Assert.Throws<InvalidOperationException>(() => instance.Transition(ActionState.Running));
    }

    [Fact]
    public void Aucune_transition_ne_revient_en_arriere()
    {
        var instance = CreerInstance();
        instance.Transition(ActionState.Validated);

        Assert.Throws<InvalidOperationException>(() => instance.Transition(ActionState.Created));
    }

    // --- Outcome (ACT-002-G) ---

    [Fact]
    public void Definir_loutcome_avant_succeeded_ou_failed_est_refuse()
    {
        var instance = CreerInstance();

        Assert.Throws<InvalidOperationException>(
            () => instance.DefinirOutcome(new Outcome(OutcomeForme.Reussite)));
    }

    [Fact]
    public void Definir_loutcome_apres_succeeded_fonctionne()
    {
        var instance = CreerInstance();
        instance.Transition(ActionState.Validated);
        instance.Transition(ActionState.Planned);
        instance.Transition(ActionState.Prepared);
        instance.Transition(ActionState.Running);
        instance.Transition(ActionState.Succeeded);

        instance.DefinirOutcome(new Outcome(OutcomeForme.Reussite));

        Assert.Equal(OutcomeForme.Reussite, instance.Outcome?.Forme);
    }

    // --- Composition (ACT-009-A) ---

    [Fact]
    public void Une_sous_action_avec_un_acteur_different_est_refusee()
    {
        var acteurComposite = EntityId.New();
        var sousInstance = CreerInstance(acteur: EntityId.New());
        var sousActions = new[] { new SubActionRef(sousInstance, Essentielle: true) };

        Assert.Throws<ArgumentException>(
            () => CreerInstance(acteur: acteurComposite, sousActions: sousActions));
    }

    [Fact]
    public void Toutes_les_sous_actions_essentielles_reussies_donne_reussite()
    {
        var acteur = EntityId.New();
        var sous1 = CreerInstance(acteur: acteur);
        sous1.Transition(ActionState.Validated);
        sous1.Transition(ActionState.Planned);
        sous1.Transition(ActionState.Prepared);
        sous1.Transition(ActionState.Running);
        sous1.Transition(ActionState.Succeeded);

        var composite = CreerInstance(
            acteur: acteur,
            sousActions: new[] { new SubActionRef(sous1, Essentielle: true) });

        Assert.Equal(OutcomeForme.Reussite, composite.CalculerOutcomeComposite());
    }

    [Fact]
    public void Echec_dune_sous_action_non_essentielle_donne_reussite_partielle()
    {
        var acteur = EntityId.New();
        var sousEchouee = CreerInstance(acteur: acteur);
        sousEchouee.Transition(ActionState.Validated);
        sousEchouee.Transition(ActionState.Failed);

        var composite = CreerInstance(
            acteur: acteur,
            sousActions: new[] { new SubActionRef(sousEchouee, Essentielle: false) });

        Assert.Equal(OutcomeForme.ReussitePartielle, composite.CalculerOutcomeComposite());
    }

    [Fact]
    public void Echec_dune_sous_action_essentielle_donne_echec()
    {
        var acteur = EntityId.New();
        var sousEchouee = CreerInstance(acteur: acteur);
        sousEchouee.Transition(ActionState.Validated);
        sousEchouee.Transition(ActionState.Failed);

        var composite = CreerInstance(
            acteur: acteur,
            sousActions: new[] { new SubActionRef(sousEchouee, Essentielle: true) });

        Assert.Equal(OutcomeForme.Echec, composite.CalculerOutcomeComposite());
    }

    [Fact]
    public void Propager_echec_transite_le_composite_si_sous_action_essentielle_echoue()
    {
        var acteur = EntityId.New();
        var sousEchouee = CreerInstance(acteur: acteur);
        sousEchouee.Transition(ActionState.Validated);
        sousEchouee.Transition(ActionState.Failed);

        var composite = CreerInstance(
            acteur: acteur,
            sousActions: new[] { new SubActionRef(sousEchouee, Essentielle: true) });
        composite.Transition(ActionState.Validated);

        composite.PropagerEchecSiNecessaire();

        Assert.Equal(ActionState.Failed, composite.State);
    }

    [Fact]
    public void Propager_echec_ne_fait_rien_si_seule_une_sous_action_non_essentielle_echoue()
    {
        var acteur = EntityId.New();
        var sousEchouee = CreerInstance(acteur: acteur);
        sousEchouee.Transition(ActionState.Validated);
        sousEchouee.Transition(ActionState.Failed);

        var composite = CreerInstance(
            acteur: acteur,
            sousActions: new[] { new SubActionRef(sousEchouee, Essentielle: false) });
        composite.Transition(ActionState.Validated);

        composite.PropagerEchecSiNecessaire();

        Assert.Equal(ActionState.Validated, composite.State);
    }
}
