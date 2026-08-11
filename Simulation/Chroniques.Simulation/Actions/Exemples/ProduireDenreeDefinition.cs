using Chroniques.Simulation.Actions;

namespace Chroniques.Simulation.Actions.Exemples;

/// <summary>
/// Action Definition conforme à PAT-003 / VERB-003.
/// </summary>
public static class ProduireDenreeDefinition
{
    public static readonly ActionDefinition Definition = new(
        Verbe: "ProduireDenree",
        Pattern: "Production",
        Principe: "Transformation",
        Contract: new ActionContract(
            Inputs: new[]
            {
                new InputSlot("Acteur", "EntityId"),
                new InputSlot("EntreeRessource", "EntityId"),
                new InputSlot("SortieAlimentaire", "EntityId")
            },
            Preconditions: Array.Empty<Condition>(),
            Constraints: Array.Empty<Condition>(),
            Costs: Array.Empty<CostSlot>(),
            Effects: new[]
            {
                new ConsequenceTemplate(
                    Description: "Consomme la quantité configurée de ressource d'entrée",
                    Categorie: ConsequenceCategorie.Materielle,
                    Temporalite: ConsequenceTemporalite.Immediate,
                    Reversibilite: ConsequenceReversibilite.Irreversible,
                    Portee: ConsequencePortee.Locale,
                    CibleVisee: RoleCible.Secondaire),
                new ConsequenceTemplate(
                    Description: "Produit les portions alimentaires configurées",
                    Categorie: ConsequenceCategorie.Materielle,
                    Temporalite: ConsequenceTemporalite.Immediate,
                    Reversibilite: ConsequenceReversibilite.Reversible,
                    Portee: ConsequencePortee.Locale,
                    CibleVisee: RoleCible.Principale)
            },
            Events: new[]
            {
                new EventTemplate("production.entree.consommee", EventCategorie.Fait),
                new EventTemplate("production.denree.creee", EventCategorie.Fait)
            }));
}
