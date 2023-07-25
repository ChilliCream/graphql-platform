using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace HotChocolate.Subscriptions.Postgres;

internal sealed class PostgresChannel : IAsyncDisposable
{
    private readonly string _channelName;
    private readonly ResilientNpgsqlConnection _connection;
    private readonly List<PostgresChannelObserver> _observers = new();
    private readonly PostgresChannelWriter _writer;
    private bool _initialized;
    private ContinuousTask? _waitOnNotificationTask;
    private ChannelSubscription? _subscription;

    public PostgresChannel(PostgresSubscriptionOptions options)
    {
        _channelName = options.ChannelName;
        _connection = new ResilientNpgsqlConnection(options.ConnectionFactory, Connect, Disconnect);
        _writer = new PostgresChannelWriter(options);
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

        return new Unsubscriber(this, observer);
    }

    private void Unsubscribe(PostgresChannelObserver observer)
        => _observers.Remove(observer);

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

    private async ValueTask Connect(CancellationToken cancellationToken = default)
    {
        var connection = _connection.Connection ??
            throw new InvalidOperationException("Connection was not yet initialized.");

        _subscription = new ChannelSubscription(_channelName, connection, OnNotification);
        await _subscription.ConnectAsync(cancellationToken);

        _waitOnNotificationTask = new ContinuousTask(ct => connection.WaitAsync(ct));
    }

    private async ValueTask Disconnect(CancellationToken cancellationToken = default)
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
    }

    private void OnNotification(object sender, NpgsqlNotificationEventArgs eventArgs)
    {
        if (eventArgs.Channel != _channelName)
        {
            return;
        }

        var deserialized = PostgresMessageEnvelope.Parse(eventArgs.Payload);
        if (deserialized is not null)
        {
            foreach (var observer in _observers)
            {
                var value = deserialized.Value;
                observer.OnNext(ref value);
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
            command.CommandText = $"LISTEN {_channelName}";
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                _connection.Notification -= _handler;

                await using var command = _connection.CreateCommand();
                command.CommandText = $"UNLISTEN {_channelName}";
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception)
            {
                // we swallow any exception because we dont care about the connection state
            }
        }
    }
}
