using static System.Threading.Channels.Channel;

namespace HotChocolate.Subscriptions.InMemory;

internal sealed class InMemoryTopic<TMessage>
    : DefaultTopic<InMemoryMessageEnvelope<TMessage>, TMessage>
    , IInMemoryTopic
{
    private static readonly InMemoryMessageEnvelope<TMessage> _complete = new();

    public InMemoryTopic(
        string name,
        int capacity,
        TopicBufferFullMode fullMode)
        : base(name, capacity, fullMode, CreateUnbounded<InMemoryMessageEnvelope<TMessage>>())
    {
    }

    public void TryWrite(TMessage message)
        => Incoming.TryWrite(new InMemoryMessageEnvelope<TMessage>(message));

    public void TryComplete()
        => Incoming.TryWrite(_complete);
}
