namespace Chroniques.Simulation.Actions;

/// <summary>
/// Une étape d'un <see cref="Plan"/> (ACT-002-I, section 4 --- Composition).
///
/// <paramref name="DependsOn"/> ne peut jamais former de cycle (ACT-002-I,
/// section 6 : « les dépendances circulaires sont interdites ») --- cet
/// invariant n'est pas vérifié par ce type lui-même (un record n'a pas de
/// vue sur le graphe complet), il appartient à <see cref="Plan"/>.
///
/// ENGINE-012 ajoute <see cref="Cibles"/> afin que les Cibles concrètes
/// choisies pendant la Planification soient portées par le Plan lui-même,
/// plutôt que fournies à côté du pipeline. L'Intent reste ainsi indépendant
/// des Cibles conformément à ACT-005-A.
/// </summary>
public sealed record PlanStep(
    ActionDefinition Definition,
    PlanStepMode Mode,
    IReadOnlyList<PlanStep> DependsOn)
{
    /// <summary>
    /// Cibles concrètes prévues pour l'Action Instance issue de cette étape.
    /// Vide par défaut pour préserver les constructions historiques ; tout
    /// chemin d'exécution générique exige ensuite exactement une Cible
    /// principale, invariant déjà contrôlé par <see cref="ActionInstance"/>.
    /// </summary>
    public IReadOnlyList<CibleRef> Cibles { get; init; }
        = Array.Empty<CibleRef>();
}
