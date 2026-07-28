namespace Chroniques.Simulation.Components;

using Chroniques.Simulation.Kernel;

/// <summary>
/// Implémente GDB-004B (Les Besoins des Habitants), restreint au minimum
/// retenu pour v0.2 par MASTER-005 : faim, fatigue, santé, moral. Les
/// autres catégories citées par GDB-004B (logement, sécurité, relations,
/// travail, apprentissage, loisirs, transmission) appartiennent aux phases
/// suivantes --- elles ne sont pas oubliées, seulement pas encore
/// implémentées (MASTER-006 : pas d'anticipation sans motif réel).
///
/// Pure donnée, conformément à CORE-003-C : aucune logique n'est exécutée
/// ici. C'est <see cref="Chroniques.Simulation.Systems.NeedsDecaySystem"/>
/// qui la fait évoluer à chaque Tick.
///
/// Chaque valeur va de 0 (critique) à 100 (pleinement satisfait).
/// </summary>
public sealed class NeedsComponent : IComponent
{
    public double Faim { get; set; } = 100;
    public double Fatigue { get; set; } = 100;
    public double Sante { get; set; } = 100;
    public double Moral { get; set; } = 100;
}
