using HotChocolate.Subscriptions.Diagnostics;
using static System.Threading.Channels.Channel;

namespace HotChocolate.Subscriptions.InMemory;

/// <summary>
/// The in-memory pub/sub topic representation.
/// </summary>
/// <typeparam name="TMessage">
/// The message type used with this topic.
/// </typeparam>
internal sealed class InMemoryTopic<TMessage> : DefaultTopic<TMessage>, IInMemoryTopic
{
    private static readonly MessageEnvelope<TMessage> _complete = new(kind: MessageKind.Completed);

    public InMemoryTopic(
        string name,
        int capacity,
        TopicBufferFullMode fullMode,
        ISubscriptionDiagnosticEvents diagnosticEvents)
        : base(
            name,
            capacity,
            fullMode,
            diagnosticEvents,
            CreateUnbounded<MessageEnvelope<TMessage>>())
    {
    }

    public void TryWrite(MessageEnvelope<TMessage> message)
        => Incoming.TryWrite(message);

    public void TryComplete()
        => Incoming.TryWrite(_complete);
}
