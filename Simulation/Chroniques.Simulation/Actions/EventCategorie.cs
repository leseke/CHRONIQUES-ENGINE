namespace Chroniques.Simulation.Actions;

/// <summary>
/// Catégories stables d'événements qu'une Action peut publier (ACT-010-A,
/// section 3).
/// </summary>
public enum EventCategorie
{
    Transition,
    Fait,
    Notification,
    Narratif
}
