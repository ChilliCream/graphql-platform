using HotChocolate.Subscriptions.Diagnostics;
using Npgsql;
using static HotChocolate.Subscriptions.Postgres.PostgresResources;

namespace HotChocolate.Subscriptions.Postgres;

internal sealed class PostgresChannel : IAsyncDisposable
{
    private readonly ISubscriptionDiagnosticEvents _diagnosticEvents;
    private readonly string _channelName;
    private readonly ResilientNpgsqlConnection _connection;
    private readonly CopyOnWriteList<PostgresChannelObserver> _observers = new();
    private readonly PostgresChannelWriter _writer;
    private bool _initialized;
    private ContinuousTask? _waitOnNotificationTask;
    private ChannelSubscription? _subscription;

    public PostgresChannel(
        ISubscriptionDiagnosticEvents diagnosticEvents,
        PostgresSubscriptionOptions options)
    {
        _diagnosticEvents = diagnosticEvents;
        _channelName = options.ChannelName;
        _connection = new ResilientNpgsqlConnection(
            diagnosticEvents,
            options.ConnectionFactory,
            OnConnect,
            OnDisconnect);
        _writer = new PostgresChannelWriter(diagnosticEvents, options);
    }

    public async ValueTask EnsureInitialized(CancellationToken cancellationToken)
    {
        if (!_initialized)
        {
            await _connection.Initialize(cancellationToken);
            await _writer.Initialize(cancellationToken);
            _initialized = true;
        }
    }

    public IDisposable Subscribe(PostgresChannelObserver observer)
    {
        _observers.Add(observer);

        _diagnosticEvents
            .ProviderTopicInfo(observer.Topic, PostgresTopic_ConnectAsync_SubscribedToPostgres);

        return new Unsubscriber(this, observer);
    }

    private void Unsubscribe(PostgresChannelObserver observer)
    {
        _observers.Remove(observer);

        _diagnosticEvents.ProviderTopicInfo(observer.Topic, Subscription_UnsubscribedFromPostgres);
    }

    public async Task SendAsync(
        PostgresMessageEnvelope message,
        CancellationToken cancellationToken)
    {
        await EnsureInitialized(cancellationToken);

        await _writer.SendAsync(message, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    private async ValueTask OnConnect(CancellationToken cancellationToken = default)
    {
        var connection = _connection.Connection ??
            throw new InvalidOperationException("Connection was not yet initialized.");

        _subscription = new ChannelSubscription(_channelName, connection, OnNotification);
        await _subscription.ConnectAsync(cancellationToken);

        _waitOnNotificationTask = new ContinuousTask(
            ct => connection.WaitAsync(ct),
            TimeProvider.System);

        _diagnosticEvents.ProviderInfo(PostgresChannel_ConnectionEstablished);
    }

    private async ValueTask OnDisconnect(CancellationToken cancellationToken = default)
    {
        // we first stop the wait task so that the connection is free to execute the unlisten
        // command
        if (_waitOnNotificationTask is not null)
        {
            await _waitOnNotificationTask.DisposeAsync();
        }

        if (_subscription is not null)
        {
            await _subscription.DisposeAsync();
        }

        _diagnosticEvents.ProviderInfo(PostgresChannel_Disconnected);
    }

    private void OnNotification(object sender, NpgsqlNotificationEventArgs eventArgs)
    {
        if (eventArgs.Channel != _channelName)
        {
            return;
        }

        // as we only really need the topic and the payload we just output them directly instead
        // of creating a full message envelope
        if (PostgresMessageEnvelope.TryParse(eventArgs.Payload, out var topic, out var payload))
        {
            var observers = _observers.Items;
            for (var i = 0; i < observers.Length; i++)
            {
                if (observers[i].Topic == topic)
                {
                    observers[i].OnNext(payload);
                }
            }
        }
    }

    private sealed class Unsubscriber : IDisposable
    {
        private readonly PostgresChannel _channel;
        private readonly PostgresChannelObserver _observer;

        public Unsubscriber(PostgresChannel channel, PostgresChannelObserver observer)
        {
            _channel = channel;
            _observer = observer;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _channel.Unsubscribe(_observer);
        }
    }

    private sealed class ChannelSubscription : IAsyncDisposable
    {
        private readonly string _channelName;
        private readonly NpgsqlConnection _connection;
        private readonly NotificationEventHandler _handler;

        public ChannelSubscription(
            string channelName,
            NpgsqlConnection connection,
            NotificationEventHandler handler)
        {
            _connection = connection;
            _handler = handler;
            _channelName = channelName;
        }

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            _connection.Notification += _handler;

            await using var command = _connection.CreateCommand();
            command.CommandText = $"""LISTEN "{_channelName}" """;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                _connection.Notification -= _handler;

                await using var command = _connection.CreateCommand();
                command.CommandText = $"""UNLISTEN "{_channelName}" """;
                await command.ExecuteNonQueryAsync();
            }
            catch
            {
                // we swallow any exception because we don't care about the connection state
            }
        }
    }
}
