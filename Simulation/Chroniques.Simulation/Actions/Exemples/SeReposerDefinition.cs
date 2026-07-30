using Chroniques.Simulation.Actions;

namespace Chroniques.Simulation.Actions.Exemples;

/// <summary>
/// Action Definition de démonstration : « Se reposer » restaure la
/// Fatigue de son Acteur.
///
/// Ceci n'est jamais un Verbe officiel --- ACT-008-A, section 1, réserve
/// l'énumération des Verbes concrets à la bibliothèque VERBS, pilotée par
/// GDB. Ce dossier <c>Exemples</c> ne sert qu'à prouver que l'architecture
/// d'ENGINE-006 fonctionne réellement de bout en bout, avec un unique
/// Verbe volontairement minimal.
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
