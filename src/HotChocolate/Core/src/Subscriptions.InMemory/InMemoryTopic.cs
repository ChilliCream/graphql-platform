using static System.Threading.Channels.Channel;

namespace HotChocolate.Subscriptions.InMemory;

internal sealed class InMemoryTopic<TMessage> : DefaultTopic<TMessage>, IInMemoryTopic
{
    private static readonly MessageEnvelope<TMessage> _complete = new();

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
        // we need to start the processing the minute the complete context is
        // fully initialized.
        BeginProcessing();
    }

    public void TryWrite(MessageEnvelope<TMessage> message)
        => Incoming.TryWrite(message);

    public void TryComplete()
        => Incoming.TryWrite(_complete);
}
