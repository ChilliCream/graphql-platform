namespace HotChocolate.Fusion.Execution.Brokers;

public sealed class InMemoryEventStreamBrokerProvider(InMemoryEventStreamBrokerHub hub)
    : IEventStreamBrokerProvider
{
    public IEventStreamBroker Create() => new InMemoryEventStreamBroker(hub);
}
