using Chroniques.Simulation.Kernel;

namespace Chroniques.Simulation.Actions;

/// <summary>
/// Représente une exécution réelle d'une <see cref="ActionDefinition"/>
/// (ACT-002-D, section 4). N'exécute aucune logique elle-même --- toute
/// transformation passe par un <see cref="IExecutionEngine"/> externe
/// (ACT-002-F, section 2).
///
/// Porte la machine à états officielle (ACT-001-F), les Cibles avec leur
/// rôle (ACT-005-A, section 6), et --- si elle est composite --- ses
/// sous-Actions (ACT-009-A).
/// </summary>
public sealed class ActionInstance
{
    /// <summary>
    /// Transitions autorisées (ACT-001-F, section 6 ; ACT-002-F, section
    /// 8bis pour les transitions directes vers <see cref="ActionState.Failed"/>
    /// depuis un état antérieur à Running --- « invalidité interne »).
    /// Aucune transition absente de cette table n'est autorisée
    /// (ACT-001-F, section 7 : « Toute transition non documentée est
    /// interdite »).
    /// </summary>
    private static readonly IReadOnlyDictionary<ActionState, IReadOnlyCollection<ActionState>> TransitionsAutorisees =
        new Dictionary<ActionState, IReadOnlyCollection<ActionState>>
        {
            [ActionState.Created] = new[] { ActionState.Validated, ActionState.Failed },
            [ActionState.Validated] = new[] { ActionState.Planned, ActionState.Failed },
            [ActionState.Planned] = new[] { ActionState.Prepared, ActionState.Failed },
            [ActionState.Prepared] = new[] { ActionState.Running, ActionState.Failed },
            [ActionState.Running] = new[]
            {
                ActionState.Succeeded, ActionState.Failed,
                ActionState.Suspended, ActionState.Interrupted
            },
            [ActionState.Suspended] = new[] { ActionState.Running },
            [ActionState.Interrupted] = new[] { ActionState.Running, ActionState.Failed },
            [ActionState.Succeeded] = new[] { ActionState.Resolved },
            [ActionState.Failed] = new[] { ActionState.Resolved },
            [ActionState.Resolved] = new[] { ActionState.Committed },
            [ActionState.Committed] = new[] { ActionState.Archived },
            [ActionState.Archived] = Array.Empty<ActionState>()
        };

    public EntityId Id { get; }
    public Tick CreatedAt { get; }
    public ActionDefinition Definition { get; }
    public ActionState State { get; private set; } = ActionState.Created;
    public EntityId Acteur { get; }
    public IReadOnlyList<CibleRef> Cibles { get; private set; }
    public Outcome? Outcome { get; private set; }
    public IReadOnlyList<SubActionRef> SousActions { get; }

    public ActionInstance(
        Tick createdAt,
        ActionDefinition definition,
        EntityId acteur,
        IReadOnlyList<CibleRef> cibles,
        IReadOnlyList<SubActionRef>? sousActions = null)
    {
        var ciblesPrincipales = cibles.Count(c => c.Role == RoleCible.Principale);
        if (ciblesPrincipales != 1)
        {
            throw new ArgumentException(
                "Une Action Instance possède toujours exactement une Cible principale (ACT-005-A, section 6).",
                nameof(cibles));
        }

        SousActions = sousActions ?? Array.Empty<SubActionRef>();

        foreach (var sousAction in SousActions)
        {
            if (!sousAction.Instance.Acteur.Equals(acteur))
            {
                throw new ArgumentException(
                    "Une sous-Action ne peut jamais avoir un Acteur différent de l'Action composite qui la contient (ACT-009-A, section 5).",
                    nameof(sousActions));
            }
        }

        Id = EntityId.New();
        CreatedAt = createdAt;
        Definition = definition;
        Acteur = acteur;
        Cibles = cibles;
    }

    /// <summary>
    /// Fait transiter l'Action Instance vers un nouvel état, en respectant
    /// strictement <see cref="TransitionsAutorisees"/> (ACT-001-F, sections
    /// 6 et 7).
    /// </summary>
    public void Transition(ActionState nouvelEtat)
    {
        if (!TransitionsAutorisees[State].Contains(nouvelEtat))
        {
            throw new InvalidOperationException(
                $"Transition interdite : {State} → {nouvelEtat} (ACT-001-F, section 7).");
        }

        State = nouvelEtat;
    }

    /// <summary>
    /// Retire une Cible secondaire devenue invalide sans interrompre
    /// l'Action (ACT-005-A, section 8) --- une Cible principale invalide
    /// suit exclusivement <see cref="Transition"/> vers
    /// <see cref="ActionState.Failed"/>, jamais cette méthode.
    /// </summary>
    public void RetirerCibleSecondaire(EntityId cible)
    {
        var cibleRef = Cibles.FirstOrDefault(c => c.Cible.Equals(cible) && c.Role == RoleCible.Secondaire);
        if (cibleRef is null)
        {
            return;
        }

        Cibles = Cibles.Where(c => c != cibleRef).ToList();
    }

    /// <summary>
    /// Attache l'Outcome produit par un <see cref="IExecutionEngine"/> ---
    /// uniquement possible une fois <see cref="ActionState.Succeeded"/> ou
    /// <see cref="ActionState.Failed"/> atteint (ACT-002-G : l'Outcome
    /// précède les Effects, jamais l'inverse ; il ne peut donc être connu
    /// qu'une fois la Résolution amorcée).
    /// </summary>
    public void DefinirOutcome(Outcome outcome)
    {
        if (State != ActionState.Succeeded && State != ActionState.Failed)
        {
            throw new InvalidOperationException(
                "Un Outcome ne peut être défini qu'après Succeeded ou Failed (ACT-002-G).");
        }

        Outcome = outcome;
    }

    /// <summary>
    /// Calcule la forme d'Outcome d'une Action composite selon l'état de
    /// ses sous-Actions (ACT-009-A, section 6). Ne s'applique qu'aux
    /// instances composites --- une instance sans sous-Action retourne
    /// toujours <see cref="OutcomeForme.Reussite"/> par cette méthode, son
    /// Outcome réel étant décidé par l'Execution Engine, pas par elle.
    /// </summary>
    public OutcomeForme CalculerOutcomeComposite()
    {
        if (State == ActionState.Interrupted)
        {
            return OutcomeForme.Interruption;
        }

        if (SousActions.Any(sa => sa.Essentielle && sa.Instance.State == ActionState.Failed))
        {
            return OutcomeForme.Echec;
        }

        if (SousActions.Any(sa => !sa.Essentielle && sa.Instance.State == ActionState.Failed))
        {
            return OutcomeForme.ReussitePartielle;
        }

        return OutcomeForme.Reussite;
    }

    /// <summary>
    /// Fait transiter cette Action composite vers Failed si au moins une
    /// sous-Action essentielle a déjà échoué (ACT-009-A, section 4) ---
    /// sans effet si aucune sous-Action essentielle n'a échoué, ou si
    /// l'instance est déjà dans un état terminal.
    /// </summary>
    public void PropagerEchecSiNecessaire()
    {
        var doitEchouer = SousActions.Any(sa => sa.Essentielle && sa.Instance.State == ActionState.Failed);
        if (doitEchouer && TransitionsAutorisees[State].Contains(ActionState.Failed))
        {
            Transition(ActionState.Failed);
        }
    }
}
