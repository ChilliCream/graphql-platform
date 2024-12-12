using HotChocolate.Fusion.Collections;

namespace HotChocolate.Fusion.Events;

internal sealed class EventAggregator
{
    private readonly MultiValueDictionary<Type, Delegate> _subscribers = new();

    public void Subscribe<TEvent>(Action<TEvent> handler)
    {
        _subscribers.Add(typeof(TEvent), handler);
    }

    public void Unsubscribe<TEvent>(Action<TEvent> handler)
    {
        _subscribers.Remove(typeof(TEvent), handler);
    }

    public void Publish<TEvent>(TEvent @event)
    {
        if (_subscribers.ContainsKey(typeof(TEvent)))
        {
            foreach (var handler in _subscribers[typeof(TEvent)])
            {
                ((Action<TEvent>)handler)(@event);
            }
        }
    }
}
