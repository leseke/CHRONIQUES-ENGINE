namespace Chroniques.Simulation.Kernel.EventBus;

/// <summary>
/// Traite un type précis d'événement.
/// </summary>
public interface IEventHandler<in TEvent>
    where TEvent : IEvent
{
    void Handle(TEvent evt);
}
