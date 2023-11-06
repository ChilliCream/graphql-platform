using HotChocolate.Subscriptions.Diagnostics;
using static System.Threading.Channels.Channel;

namespace HotChocolate.Subscriptions.InMemory;

/// <summary>
/// The in-memory pub/sub topic representation.
/// </summary>
/// <typeparam name="TMessage">
/// The message type used with this topic.
/// </typeparam>
internal sealed class InMemoryTopic<TMessage> : DefaultTopic<TMessage>
{
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
            CreateUnbounded<TMessage>())
    {
    }
}
