using System.Threading.Channels;
using HotChocolate.Subscriptions.Diagnostics;
using Npgsql;
using NpgsqlTypes;
using static HotChocolate.Subscriptions.Postgres.PostgresResources;

namespace HotChocolate.Subscriptions.Postgres;

internal sealed class PostgresChannelWriter : IAsyncDisposable
{
    private readonly ISubscriptionDiagnosticEvents _diagnosticEvents;
    private readonly Channel<PostgresMessageEnvelope> _channel;
    private readonly ResilientNpgsqlConnection _connection;
    private readonly string _channelName;
    private readonly int _maxSendBatchSize;
    private ContinuousTask? _task;

    public PostgresChannelWriter(
        ISubscriptionDiagnosticEvents diagnosticEvents,
        PostgresSubscriptionOptions options)
    {
        _diagnosticEvents = diagnosticEvents;
        _maxSendBatchSize = options.MaxSendBatchSize;
        _channelName = options.ChannelName;
        _connection = new ResilientNpgsqlConnection(
            _diagnosticEvents,
            options.ConnectionFactory,
            OnConnect,
            Disconnect);
        _channel = Channel.CreateBounded<PostgresMessageEnvelope>(options.MaxSendQueueSize);
    }

    public async ValueTask Initialize(CancellationToken cancellationToken)
    {
        await _connection.Initialize(cancellationToken);
    }

    public async Task SendAsync(PostgresMessageEnvelope message, CancellationToken ct)
    {
        await _channel.Writer.WriteAsync(message, ct);
    }

    private ValueTask OnConnect(CancellationToken cancellationToken = default)
    {
        var connection = _connection.Connection ??
            throw new InvalidOperationException("Connection was not yet initialized.");

        // on connection we start a task that will read from the channel and send the messages
        _task = new ContinuousTask(ct => HandleMessage(connection, ct), TimeProvider.System);

        _diagnosticEvents.ProviderInfo(ChannelWriter_ConnectionEstablished);

        return ValueTask.CompletedTask;
    }

    private async ValueTask Disconnect(CancellationToken cancellationToken = default)
    {
        if (_task is not null)
        {
            // we stop the task that reads from the channel and sends the messages on disconnect
            await _task.DisposeAsync();
        }

        _task = null;

        _diagnosticEvents.ProviderInfo(ChannelWriter_Disconnected);
    }

    private async Task HandleMessage(NpgsqlConnection connection, CancellationToken ct)
    {
        PostgresMessageEnvelope firstItem;

        while (!_channel.Reader.TryRead(out firstItem))
        {
            if (ct.IsCancellationRequested)
            {
                return;
            }

            await _channel.Reader.WaitToReadAsync(ct);
        }

        var messages = new List<PostgresMessageEnvelope> { firstItem };
        while (!ct.IsCancellationRequested &&
               _maxSendBatchSize > messages.Count &&
               _channel.Reader.TryRead(out var item))
        {
            messages.Add(item);
        }

        try
        {
            // we throw instead of checking the cancellation token because we want to requeue the
            // firstMessage that was already read from the channel
            ct.ThrowIfCancellationRequested();

            var payloads = new string[messages.Count];

            for (var i = 0; i < messages.Count; i++)
            {
                payloads[i] = messages[i].FormattedPayload;
            }

            const string sql =
                """
                SELECT pg_notify(t.channel, t.message)
                FROM (SELECT @channel AS channel, unnest(@messages) AS message ) AS t;
                """;

            await using var command = connection.CreateCommand();

            command.CommandText = sql;

            command.Parameters.Add(
                new NpgsqlParameter("channel", NpgsqlDbType.Text)
                {
                    Value = _channelName
                });

            command.Parameters.Add(
                new NpgsqlParameter("messages", NpgsqlDbType.Array | NpgsqlDbType.Varchar)
                {
                    Value = payloads
                });

            await command.PrepareAsync(ct);
            await command.ExecuteNonQueryAsync(ct);
        }
        catch (Exception ex)
        {
            var msg = string.Format(ChannelWriter_FailedToSend, messages.Count, ex.Message);
            _diagnosticEvents.ProviderInfo(msg);

            // if we cannot send the message we put it back into the channel
            // however as the channel is bounded, we might not able to requeue the message and will
            // be forced to drop them if they can't be written
            var failedCount = 0;

            foreach (var message in messages)
            {
                if (!_channel.Writer.TryWrite(message))
                {
                    failedCount++;
                }
            }

            if (failedCount > 0)
            {
                _diagnosticEvents.ProviderInfo(
                    string.Format(ChannelWriter_FailedToRequeueMessage, failedCount));
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_task is not null)
        {
            await _task.DisposeAsync();
        }

        _channel.Writer.Complete();
        await _connection.DisposeAsync();
    }
}
