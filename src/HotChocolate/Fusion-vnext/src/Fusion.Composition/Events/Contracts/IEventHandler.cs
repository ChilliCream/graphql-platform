namespace HotChocolate.Fusion.Events.Contracts;

internal interface IEventHandler<in TEvent> where TEvent : IEvent
{
    void Handle(TEvent @event, CompositionContext context);
}
