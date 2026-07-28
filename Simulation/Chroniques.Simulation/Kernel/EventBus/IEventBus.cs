namespace Chroniques.Simulation.Kernel.EventBus;

/// <summary>
/// Bus de communication entre les systèmes.
/// </summary>
public interface IEventBus
{
    void Publish<TEvent>(TEvent evt)
        where TEvent : IEvent;

    void Subscribe<TEvent>(IEventHandler<TEvent> handler)
        where TEvent : IEvent;

    void Unsubscribe<TEvent>(IEventHandler<TEvent> handler)
        where TEvent : IEvent;
}
