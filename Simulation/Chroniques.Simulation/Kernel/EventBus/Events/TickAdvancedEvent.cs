using Chroniques.Simulation.Kernel.EventBus;

namespace Chroniques.Simulation.Kernel.EventBus.Events;

/// <summary>
/// Événement publié à chaque avancement d'un Tick.
/// </summary>
public sealed record TickAdvancedEvent(long Tick)
    : IEvent;
