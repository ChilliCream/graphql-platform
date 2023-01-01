using System.Diagnostics;
using System.Threading.Channels;
using AlterNats;
using HotChocolate.Subscriptions.Diagnostics;
using static HotChocolate.Subscriptions.Nats.NatsResources;

namespace HotChocolate.Subscriptions.Nats;

internal sealed class NatsTopic<TMessage> : DefaultTopic<TMessage>
{
    private readonly NatsConnection _connection;
    private readonly IMessageSerializer _serializer;

    public NatsTopic(
        string name,
        NatsConnection connection,
        IMessageSerializer serializer,
        int capacity,
        TopicBufferFullMode fullMode,
        ISubscriptionDiagnosticEvents diagnosticEvents)
        : base(name, capacity, fullMode, diagnosticEvents)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    }

    protected override async ValueTask<IDisposable> OnConnectAsync(
        ChannelWriter<MessageEnvelope<TMessage>> incoming,
        CancellationToken cancellationToken)
    {
        // We ensure that the processing is not started before the context is fully initialized.
        Debug.Assert(_connection != null, "_connection != null");
        Debug.Assert(_connection != null, "_serializer != null");

        var natsSession = await _connection
            .SubscribeAsync(
                Name,
                async (string? m) => await DispatchAsync(incoming, m).ConfigureAwait(false))
            .ConfigureAwait(false);

        DiagnosticEvents.ProviderTopicInfo(Name, NatsTopic_ConnectAsync_SubscribedToNats);

        return new Session(Name, natsSession, DiagnosticEvents);
    }

    private async ValueTask DispatchAsync(
        ChannelWriter<MessageEnvelope<TMessage>> incoming,
        string? serializedMessage)
    {
        // we ensure that if there is noise on the channel we filter it out.
        if (!string.IsNullOrEmpty(serializedMessage))
        {
            DiagnosticEvents.Received(Name, serializedMessage);
            var envelope = _serializer.Deserialize<MessageEnvelope<TMessage>>(serializedMessage);
            await incoming.WriteAsync(envelope).ConfigureAwait(false);
        }
    }

    private sealed class Session : IDisposable
    {
        private readonly string _name;
        private readonly IDisposable _natsSession;
        private readonly ISubscriptionDiagnosticEvents _diagnosticEvents;
        private bool _disposed;

        public Session(
            string name,
            IDisposable natsSession,
            ISubscriptionDiagnosticEvents diagnosticEvents)
        {
            _name = name;
            _natsSession = natsSession;
            _diagnosticEvents = diagnosticEvents;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _natsSession.Dispose();
                _diagnosticEvents.ProviderTopicInfo(_name, Session_Dispose_UnsubscribedFromNats);
                _disposed = true;
            }
        }
    }
}
