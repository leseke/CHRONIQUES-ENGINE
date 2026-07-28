namespace Chroniques.Simulation.Kernel.EventBus;

public sealed class EventBus : IEventBus
{
    public void Publish<TEvent>(TEvent evt)
        where TEvent : IEvent
    {
        throw new NotImplementedException();
    }

    public void Subscribe<TEvent>(IEventHandler<TEvent> handler)
        where TEvent : IEvent
    {
        throw new NotImplementedException();
    }

    public void Unsubscribe<TEvent>(IEventHandler<TEvent> handler)
        where TEvent : IEvent
    {
        throw new NotImplementedException();
    }
}
