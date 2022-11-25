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
        TopicBufferFullMode fullMode,
        ISubscriptionDiagnosticEvents diagnosticEvents)
        : base(
            name,
            capacity,
            fullMode,
            diagnosticEvents,
            CreateUnbounded<InMemoryMessageEnvelope<TMessage>>())
    {
    }

    public void TryWrite(TMessage message)
    {
        var envelope = new InMemoryMessageEnvelope<TMessage>(message);
        DiagnosticEvents.Send(Name, envelope);
        Incoming.TryWrite(envelope);
    }

    public void TryComplete()
    {
        DiagnosticEvents.Send(Name, _complete);
        Incoming.TryWrite(_complete);
    }
}
