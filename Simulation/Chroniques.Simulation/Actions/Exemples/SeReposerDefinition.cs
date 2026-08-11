using Chroniques.Simulation.Actions;

namespace Chroniques.Simulation.Actions.Exemples;

/// <summary>
/// Action Definition actuellement utilisée pour le Verbe canonique
/// <c>VERB-001 — Se reposer</c>, spécialisé depuis <c>PAT-001 — Repos</c>.
///
/// La classe reste pour l'instant dans le namespace historique
/// <c>Actions.Exemples</c> parce que le moteur ne possède pas encore de registre
/// général des Verbes ni de second Verbe réel justifiant une réorganisation.
/// Ce placement technique ne retire pas le statut canonique du Verbe ACT.
///
/// La valeur <see cref="FatigueRestauree"/> reste une valeur de tuning de
/// l'implémentation courante ; VERB-001 ne la définit pas comme constante
/// universelle de Game Design.
/// </summary>
public static class SeReposerDefinition
{
    public const double FatigueRestauree = 20.0;

    public static readonly ActionDefinition Definition = new(
        Verbe: "SeReposer",
        Pattern: "Repos",
        Principe: "Entretien",
        Contract: new ActionContract(
            Inputs: new[] { new InputSlot("Acteur", "EntityId") },
            Preconditions: Array.Empty<Condition>(),
            Constraints: Array.Empty<Condition>(),
            Costs: Array.Empty<CostSlot>(),
            Effects: new[]
            {
                new ConsequenceTemplate(
                    Description: "Restaure la Fatigue de l'Acteur",
                    Categorie: ConsequenceCategorie.Materielle,
                    Temporalite: ConsequenceTemporalite.Immediate,
                    Reversibilite: ConsequenceReversibilite.Reversible,
                    Portee: ConsequencePortee.Locale,
                    CibleVisee: RoleCible.Principale)
            },
            Events: new[]
            {
                new EventTemplate("besoin.fatigue.restauree", EventCategorie.Fait)
            }));
}
