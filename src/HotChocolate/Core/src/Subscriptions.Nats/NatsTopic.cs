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

    protected override async ValueTask<IAsyncDisposable> OnConnectAsync(
        CancellationToken cancellationToken)
    {
        // We ensure that the processing is not started before the context is fully initialized.
        Debug.Assert(_connection != null, "_connection != null");
        Debug.Assert(_serializer != null, "_serializer != null");

        var sessionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var natsSession = await _connection
            .SubscribeCoreAsync<string?>(Name, cancellationToken: sessionCts.Token)
            .ConfigureAwait(false);
        var processing = ProcessMessagesAsync(natsSession, sessionCts.Token);

        async Task ProcessMessagesAsync(
            INatsSub<string?> natsSubscription,
            CancellationToken ct)
        {
            try
            {
                await foreach (var message in natsSubscription.Msgs.ReadAllAsync(ct).ConfigureAwait(false))
                {
                    DispatchMessage(_serializer, message.Data);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
            }
            catch (ObjectDisposedException) when (ct.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                DiagnosticEvents.MessageProcessingError(Name, ex);
            }
        }

        DiagnosticEvents.ProviderTopicInfo(Name, NatsTopic_ConnectAsync_SubscribedToNats);

        return new Session(Name, natsSession, processing, sessionCts, DiagnosticEvents);
    }

    private sealed class Session : IAsyncDisposable
    {
        private readonly string _name;
        private readonly INatsSub<string?> _natsSession;
        private readonly Task _processing;
        private readonly CancellationTokenSource _sessionCts;
        private readonly ISubscriptionDiagnosticEvents _diagnosticEvents;
        private bool _disposed;

        public Session(
            string name,
            INatsSub<string?> natsSession,
            Task processing,
            CancellationTokenSource sessionCts,
            ISubscriptionDiagnosticEvents diagnosticEvents)
        {
            _name = name;
            _natsSession = natsSession;
            _processing = processing;
            _sessionCts = sessionCts;
            _diagnosticEvents = diagnosticEvents;
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _sessionCts.Cancel();

            try
            {
                await _natsSession.DisposeAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (_sessionCts.IsCancellationRequested)
            {
            }
            catch (ObjectDisposedException) when (_sessionCts.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                _diagnosticEvents.MessageProcessingError(_name, ex);
            }

            try
            {
                await _processing.ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (_sessionCts.IsCancellationRequested)
            {
            }
            catch (ObjectDisposedException) when (_sessionCts.IsCancellationRequested)
            {
            }

            _sessionCts.Dispose();
            _diagnosticEvents.ProviderTopicInfo(_name, Session_Dispose_UnsubscribedFromNats);
        }
    }
}
