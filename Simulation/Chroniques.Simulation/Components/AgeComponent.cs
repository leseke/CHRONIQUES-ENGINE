namespace Chroniques.Simulation.Components;

using Chroniques.Simulation.Kernel;

/// <summary>
/// Implémente GDB-008C (Les Cycles de Vie) : l'âge d'un habitant, exprimé en
/// années simulées.
///
/// Pure donnée, conformément à CORE-003-C : aucune logique n'est exécutée
/// ici. C'est <see cref="Chroniques.Simulation.Systems.AgingSystem"/> qui la
/// fait progresser à chaque Tick et qui en déduit l'étape de vie courante,
/// reflétée par le <see cref="Kernel.Lifecycle"/> de l'Entity --- jamais
/// stockée une seconde fois ici, pour respecter le principe de
/// responsabilité unique (MASTER, README « Principe de responsabilité
/// unique »).
/// </summary>
public sealed class AgeComponent : IComponent
{
    public int Annees { get; set; } = 0;
}
