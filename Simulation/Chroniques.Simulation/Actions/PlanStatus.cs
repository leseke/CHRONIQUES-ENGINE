namespace Chroniques.Simulation.Actions;

/// <summary>
/// État persistant d'un <see cref="Plan"/> (ACT-002-I, section 7).
///
/// « Adapté » et « Reconstruit » ne sont pas des états mais des opérations
/// qui laissent le Plan <see cref="Actif"/> avec de nouvelles étapes --- ce
/// type ne couvre donc que les deux états qui persistent réellement dans le
/// temps.
/// </summary>
public enum PlanStatus
{
    Actif,
    Suspendu,
    Abandonne
}
