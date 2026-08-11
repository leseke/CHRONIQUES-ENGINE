using Chroniques.Simulation.Actions;

namespace Chroniques.Simulation.Actions.Exemples;

/// <summary>
/// Action Definition conforme à PAT-004 / VERB-004 : « Donner une denrée »
/// transfère des portions existantes entre deux stocks alimentaires
/// compatibles sans créer de contrepartie économique.
/// </summary>
public static class DonnerDenreeDefinition
{
    public static readonly ActionDefinition Definition = new(
        Verbe: "DonnerDenree",
        Pattern: "Transfert",
        Principe: "Échange",
        Contract: new ActionContract(
            Inputs: new[]
            {
                new InputSlot("Acteur", "EntityId"),
                new InputSlot("Destinataire", "EntityId"),
                new InputSlot("ProduitSource", "EntityId"),
                new InputSlot("ProduitDestination", "EntityId")
            },
            Preconditions: Array.Empty<Condition>(),
            Constraints: Array.Empty<Condition>(),
            Costs: Array.Empty<CostSlot>(),
            Effects: new[]
            {
                new ConsequenceTemplate(
                    Description: "Transfère des portions du stock source vers le stock destination compatible",
                    Categorie: ConsequenceCategorie.Materielle,
                    Temporalite: ConsequenceTemporalite.Immediate,
                    Reversibilite: ConsequenceReversibilite.Reversible,
                    Portee: ConsequencePortee.Locale,
                    CibleVisee: RoleCible.Principale)
            },
            Events: new[]
            {
                new EventTemplate("produit.alimentaire.transfere", EventCategorie.Fait)
            }));
}
