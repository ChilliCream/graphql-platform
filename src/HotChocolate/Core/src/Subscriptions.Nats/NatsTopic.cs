using System.Diagnostics;
using HotChocolate.Subscriptions.Diagnostics;
using NATS.Client.Core;
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
        CancellationToken cancellationToken)
    {
        // We ensure that the processing is not started before the context is fully initialized.
        Debug.Assert(_connection != null, "_connection != null");
        Debug.Assert(_connection != null, "_serializer != null");

        var natsSession = Task.Run(async () =>
        {
            await foreach (var msg in _connection.SubscribeAsync<string?>(
                Name,
                cancellationToken: cancellationToken))
            {
                Console.WriteLine($"Received {msg.Subject}: {msg.Data}\n");//tmp
                DispatchMessage(_serializer, msg.Data);
            }
        }, cancellationToken);

        DiagnosticEvents.ProviderTopicInfo(Name, NatsTopic_ConnectAsync_SubscribedToNats);

        return natsSession;

        //return new Session(Name, natsSession, DiagnosticEvents);
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
