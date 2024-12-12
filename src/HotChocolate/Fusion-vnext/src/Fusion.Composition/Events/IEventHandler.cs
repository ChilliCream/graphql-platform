namespace HotChocolate.Fusion.Events;

internal interface IEventHandler<in TEvent> where TEvent : IEvent
{
    void Handle(TEvent @event, CompositionContext context);
}
