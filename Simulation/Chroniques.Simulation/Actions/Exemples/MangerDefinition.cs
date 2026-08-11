using Chroniques.Simulation.Actions;

namespace Chroniques.Simulation.Actions.Exemples;

/// <summary>
/// Action Definition conforme à PAT-002 / VERB-002 : « Manger » consomme
/// une portion d'un produit alimentaire accessible et restaure la Faim de
/// l'Acteur (GDB-004B v1.2, GDB-005E v1.1, ENGINE-012).
///
/// Les valeurs concrètes de restauration vivent sur le
/// FoodProductComponent de la Cible, jamais dans cette définition.
/// </summary>
public static class MangerDefinition
{
    public static readonly ActionDefinition Definition = new(
        Verbe: "Manger",
        Pattern: "Alimentation",
        Principe: "Entretien",
        Contract: new ActionContract(
            Inputs: new[]
            {
                new InputSlot("Acteur", "EntityId"),
                new InputSlot("ProduitAlimentaire", "EntityId")
            },
            Preconditions: Array.Empty<Condition>(),
            Constraints: Array.Empty<Condition>(),
            Costs: Array.Empty<CostSlot>(),
            Effects: new[]
            {
                new ConsequenceTemplate(
                    Description: "Consomme une portion du produit alimentaire",
                    Categorie: ConsequenceCategorie.Materielle,
                    Temporalite: ConsequenceTemporalite.Immediate,
                    Reversibilite: ConsequenceReversibilite.Irreversible,
                    Portee: ConsequencePortee.Locale,
                    CibleVisee: RoleCible.Principale),
                new ConsequenceTemplate(
                    Description: "Restaure la Faim de l'Acteur",
                    Categorie: ConsequenceCategorie.Materielle,
                    Temporalite: ConsequenceTemporalite.Immediate,
                    Reversibilite: ConsequenceReversibilite.Reversible,
                    Portee: ConsequencePortee.Locale,
                    CibleVisee: RoleCible.Secondaire)
            },
            Events: new[]
            {
                new EventTemplate("produit.alimentaire.consomme", EventCategorie.Fait),
                new EventTemplate("besoin.faim.restauree", EventCategorie.Fait)
            }));
}
