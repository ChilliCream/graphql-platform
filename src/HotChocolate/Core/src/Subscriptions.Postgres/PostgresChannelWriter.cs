using System.Data;
using System.Threading.Channels;
using HotChocolate.Subscriptions.Diagnostics;
using Npgsql;
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
        _task = new ContinuousTask(ct => HandleMessage(connection, ct));

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

        _diagnosticEvents.ProviderInfo(ChannelWriter_Disconnectd);
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

            await using var batch = connection.CreateBatch();

            foreach (var message in messages)
            {
                var command = batch.CreateBatchCommand();

                command.CommandText = "SELECT pg_notify(@channel, @message);";

                var channel = new NpgsqlParameter("channel", DbType.String)
                {
                    Value = _channelName
                };
                var msg = new NpgsqlParameter("message", DbType.String)
                {
                    Value = message.FormattedPayload
                };
                command.Parameters.Add(channel);
                command.Parameters.Add(msg);

                batch.BatchCommands.Add(command);
            }

            await batch.PrepareAsync(ct);
            await batch.ExecuteNonQueryAsync(ct);
        }
        catch (Exception ex)
        {
            var msg = string.Format(ChannelWriter_FailedToSend, messages.Count, ex.Message);
            _diagnosticEvents.ProviderInfo(msg);

            // if we cannot send the message we put it back into the channel
            foreach (var message in messages)
            {
                await _channel.Writer.WriteAsync(message, ct);
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
