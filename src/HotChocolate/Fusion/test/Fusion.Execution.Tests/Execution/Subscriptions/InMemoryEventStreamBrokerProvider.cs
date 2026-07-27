namespace HotChocolate.Fusion.Subscriptions;

public sealed class InMemoryEventStreamBrokerProvider(InMemoryEventStreamBrokerHub hub)
    : IEventStreamBrokerProvider
{
    public IEventStreamBroker Create() => new InMemoryEventStreamBroker(hub);
}
