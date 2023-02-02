using System.Diagnostics;
using System.Threading.Channels;
using HotChocolate.Subscriptions.Diagnostics;
using StackExchange.Redis;
using static HotChocolate.Subscriptions.Redis.Properties.Resources;

namespace HotChocolate.Subscriptions.Redis;

internal sealed class RedisTopic<TMessage> : DefaultTopic<TMessage>
{
    private readonly IConnectionMultiplexer _connection;
    private readonly IMessageSerializer _serializer;

    public RedisTopic(
        string name,
        IConnectionMultiplexer connection,
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

        var subscriber = _connection.GetSubscriber();
        var messageQueue = await subscriber.SubscribeAsync(Name).ConfigureAwait(false);
        DiagnosticEvents.ProviderTopicInfo(Name, RedisTopic_SubscribedToRedis);

        messageQueue.OnMessage(
            async redisMessage =>
            {
                await DispatchAsync(incoming, redisMessage.Message.ToString())
                    .ConfigureAwait(false);
            });

        return new Session(Name, _connection, DiagnosticEvents);
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
        private readonly IConnectionMultiplexer _connection;
        private readonly ISubscriptionDiagnosticEvents _diagnosticEvents;
        private bool _disposed;

        public Session(
            string name,
            IConnectionMultiplexer connection,
            ISubscriptionDiagnosticEvents diagnosticEvents)
        {
            _name = name;
            _connection = connection;
            _diagnosticEvents = diagnosticEvents;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _connection.GetSubscriber().Unsubscribe(_name);
                _diagnosticEvents.ProviderTopicInfo(_name, RedisTopic_UnsubscribedFromRedis);
                _disposed = true;
            }
        }
    }
}
