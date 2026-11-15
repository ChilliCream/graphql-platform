using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Mocha.Middlewares;
using Npgsql;

namespace Mocha.Outbox;

/// <summary>
/// Continuously polls the Postgres outbox table and dispatches pending messages through the messaging
/// runtime, one transaction at a time and in the order they were written.
/// </summary>
public sealed class PostgresOutboxProcessor
{
    private static readonly TimeSpan s_contendedPollingInterval = TimeSpan.FromSeconds(1);

    private readonly ILogger<PostgresOutboxProcessor> _logger;
    private readonly IServiceProvider _services;
    private readonly IMessagingRuntime _runtime;
    private readonly IOutboxSignal _signal;
    private readonly ObjectPool<DispatchContext> _contextPool;
    private readonly PostgresMessageOutboxOptions _options;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresOutboxProcessor"/> class.
    /// </summary>
    /// <param name="logger">
    /// The logger used to record outbox processing diagnostics and errors.
    /// </param>
    /// <param name="services">
    /// The service provider used to create scoped services for each dispatched message.
    /// </param>
    /// <param name="runtime">
    /// The messaging runtime used to resolve message types and dispatch endpoints.
    /// </param>
    /// <param name="pools">
    /// The pool provider supplying reusable <see cref="DispatchContext"/> instances to reduce allocations.
    /// </param>
    /// <param name="signal">
    /// The signal used to wake the processor when new outbox messages are enqueued.
    /// </param>
    /// <param name="options">
    /// The outbox options containing the SQL queries and connection factory.
    /// </param>
    internal PostgresOutboxProcessor(
        ILogger<PostgresOutboxProcessor> logger,
        IServiceProvider services,
        IMessagingRuntime runtime,
        IMessagingPools pools,
        IOutboxSignal signal,
        PostgresMessageOutboxOptions options)
    {
        _logger = logger;
        _services = services;
        _runtime = runtime;
        _signal = signal;
        _contextPool = pools.DispatchContext;
        _options = options;
        _timeProvider = services.GetService<TimeProvider>() ?? TimeProvider.System;
    }

    /// <summary>
    /// Runs the outbox processing loop, dispatching pending messages and waiting
    /// until the next message is due or a signal is received.
    /// </summary>
    /// <param name="cancellationToken">A token that signals when the processor should stop.</param>
    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            using var joinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            try
            {
                var signaled = _signal.WaitAsync(joinedCts.Token);

                var nextPollingInterval = await ProcessPendingAsync(cancellationToken);

                if (nextPollingInterval is not null)
                {
                    _logger.OutboxProcessorSleeping(nextPollingInterval.Value);

                    await Task.WhenAny(Task.Delay(nextPollingInterval.Value, cancellationToken), signaled);
                }
                else
                {
                    await signaled;
                }
            }
            catch (OperationCanceledException)
            {
                joinedCts.TryCancel();
            }
            catch
            {
                joinedCts.TryCancel();
                throw;
            }
        }
    }

    private async ValueTask<TimeSpan?> ProcessPendingAsync(CancellationToken cancellationToken)
    {
        await using var scope = _services.CreateAsyncScope();
        await using var connection = _options.CreateConnection(scope.ServiceProvider);
        await connection.OpenAsync(cancellationToken);

        while (true)
        {
            using var activity = OpenTelemetry.Source.StartActivity(
                "Process Message Outbox",
                ActivityKind.Consumer,
                new ActivityContext());

            if (!await ProcessEventAsync(connection, cancellationToken))
            {
                return await GetNextPollingIntervalAsync(connection, cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    private async Task<TimeSpan?> GetNextPollingIntervalAsync(
        NpgsqlConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = _options.Queries.NextPollingInterval;
        await command.PrepareAsync(cancellationToken);

        var result = await command.ExecuteScalarAsync(cancellationToken);

        if (result is not double seconds)
        {
            return null;
        }

        return seconds > 0 ? TimeSpan.FromSeconds(seconds) : s_contendedPollingInterval;
    }

    private async Task<bool> ProcessEventAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        using var activity = OpenTelemetry.Source.StartActivity(
            "Process Message Outbox Event",
            ActivityKind.Producer,
            new ActivityContext());

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            Guid? transactionId = null;
            var lastHandledId = Guid.Empty;
            var handled = 0;
            var rows = 0;
            var failedId = Guid.Empty;
            var failedAttempt = 0;
            var failed = false;

            // Lock the first message of a transaction, count an attempt for it and load the batch
            await using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = _options.Queries.ProcessEvent;

                await command.PrepareAsync(cancellationToken);

                await using var reader = await command.ExecuteReaderAsync(cancellationToken);

                try
                {
                    while (!cancellationToken.IsCancellationRequested && await reader.ReadAsync(cancellationToken))
                    {
                        rows++;

                        var id = reader.GetGuid(0);

                        if (rows == 1)
                        {
                            transactionId = reader.IsDBNull(2) ? null : reader.GetGuid(2);
                        }

                        var attempt = reader.GetInt32(3);
                        var envelope = Serializer.ReadMessageEnvelopeSafe(reader, 1, _logger);

                        if (await TryDispatchAsync(id, envelope, cancellationToken))
                        {
                            lastHandledId = id;
                            handled++;
                        }
                        else
                        {
                            failed = true;
                            failedId = id;
                            failedAttempt = attempt;
                            break;
                        }
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // Shutdown stops the batch, the completion below keeps the progress.
                }
            }

            if (!failed && handled == 0 && cancellationToken.IsCancellationRequested)
            {
                // The attempt counted for the first message is released.
                await transaction.RollbackAsync(CancellationToken.None);
                return true;
            }

            if (rows == 0)
            {
                await CommitAsync(transaction);
                return false;
            }

            // Progress is kept on shutdown, so the completion statements ignore the cancellation token.
            if (handled > 0)
            {
                if (transactionId is { } handledTransactionId)
                {
                    await DeleteTransactionEventsAsync(
                        connection,
                        handledTransactionId,
                        lastHandledId,
                        transaction,
                        CancellationToken.None);
                }
                else
                {
                    await DeleteEventAsync(connection, lastHandledId, transaction, CancellationToken.None);
                }
            }

            if (failed)
            {
                // The claim already counted the attempt of the first message.
                if (handled > 0)
                {
                    await RescheduleEventAsync(connection, failedId, transaction, CancellationToken.None);
                }

                LogIfExhausted(failedId, transactionId ?? failedId, failedAttempt);
            }

            await CommitAsync(transaction);

            return true;
        }
        catch (Exception ex)
        {
            // Log only - the transaction is rolled back when it is disposed
            _logger.UnexpectedErrorWhileProcessingOutboxEvent(ex);
            throw;
        }
    }

    private static async Task CommitAsync(NpgsqlTransaction transaction)
    {
        try
        {
            await transaction.CommitAsync(CancellationToken.None);
        }
        catch
        {
            // Commit failed (e.g., connection lost). Attempt rollback.
            // If commit actually succeeded server-side, the sent messages are deleted and the
            // rest stays with times_sent incremented - safe, just causes a retry.
            try
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }
            catch
            {
                /* swallow */
            }
        }
    }

    // Returns true when the message can be deleted (sent or expired) and false when it must be retried.
    private async ValueTask<bool> TryDispatchAsync(
        Guid id,
        MessageEnvelope? envelope,
        CancellationToken cancellationToken)
    {
        // A message this instance cannot read or route stays in the outbox and is
        // retried after its backoff.
        if (envelope is null)
        {
            _logger.CouldNotDeserializeMessageBody(id);
            return false;
        }

        if (envelope.DeliverBy is { } deadline && deadline <= _timeProvider.GetUtcNow())
        {
            _logger.OutboxMessageExpired(id);
            return true;
        }

        var messageType = GetMessageType(envelope.MessageType);

        if (messageType is null)
        {
            _logger.CouldNotResolveMessageType(id, envelope.MessageType);
            return false;
        }

        var endpoint = ResolveDispatchEndpoint(envelope);

        if (endpoint is null)
        {
            _logger.CouldNotDetermineDestination(id, envelope.DestinationAddress);
            return false;
        }

        try
        {
            await SendAsync(envelope, endpoint, messageType, cancellationToken);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.OutboxMessageDispatchFailed(ex, id);
            return false;
        }
    }

    private void LogIfExhausted(Guid id, Guid transactionId, int timesSent)
    {
        if (timesSent >= PostgresMessageOutboxQueries.MaxAttempts)
        {
            _logger.OutboxMessageExhausted(id, transactionId, PostgresMessageOutboxQueries.MaxAttempts);
        }
    }

    private MessageType? GetMessageType(string? messageType)
    {
        try
        {
            if (messageType is null)
            {
                return null;
            }

            return _runtime.Messages.GetMessageType(messageType);
        }
        catch
        {
            return null;
        }
    }

    private DispatchEndpoint? ResolveDispatchEndpoint(MessageEnvelope envelope)
    {
        return envelope.Headers?.IsReplyOrFault() ?? false
            ? GetReplyDispatchEndpoint(envelope.DestinationAddress)
            : GetDispatchEndpoint(envelope.DestinationAddress);
    }

    private DispatchEndpoint? GetReplyDispatchEndpoint(string? destinationAddress)
    {
        try
        {
            if (!Uri.TryCreate(destinationAddress, UriKind.Absolute, out var uri))
            {
                return null;
            }

            return _runtime.GetTransport(uri)?.ReplyDispatchEndpoint;
        }
        catch
        {
            return null;
        }
    }

    private DispatchEndpoint? GetDispatchEndpoint(string? destinationAddress)
    {
        try
        {
            if (destinationAddress is null
                || !Uri.TryCreate(destinationAddress, UriKind.Absolute, out var uri))
            {
                return null;
            }

            return _runtime.GetDispatchEndpoint(uri);
        }
        catch
        {
            return null;
        }
    }

    private async ValueTask SendAsync(
        MessageEnvelope envelope,
        DispatchEndpoint endpoint,
        MessageType messageType,
        CancellationToken cancellationToken)
    {
        Activity? activity = null;
        var traceparent = envelope.Headers?.Get(MessageHeaders.Traceparent);

        if (!string.IsNullOrEmpty(traceparent))
        {
            var tracestate = envelope.Headers?.Get(MessageHeaders.Tracestate);
            if (ActivityContext.TryParse(traceparent, tracestate, out var parentContext))
            {
                activity = OpenTelemetry.Source.CreateActivity(
                    "outbox send",
                    ActivityKind.Client,
                    parentContext);

                activity?.SetMessageId(envelope.MessageId);

                activity?.Start();
            }
        }

        var context = _contextPool.Get();
        try
        {
            await using var scope = _services.CreateAsyncScope();

            context.Initialize(scope.ServiceProvider, endpoint, _runtime, messageType, cancellationToken);

            context.SkipOutbox();

            context.Envelope = envelope;

            await endpoint.ExecuteAsync(context);
        }
        finally
        {
            _contextPool.Return(context);
            activity?.Dispose();
        }
    }

    private async Task DeleteEventAsync(
        NpgsqlConnection connection,
        Guid eventId,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = _options.Queries.DeleteEvent;
        command.Connection = connection;
        command.Transaction = transaction;
        command.Parameters.AddWithValue("@EventId", eventId);

        await command.PrepareAsync(cancellationToken);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task DeleteTransactionEventsAsync(
        NpgsqlConnection connection,
        Guid transactionId,
        Guid lastEventId,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = _options.Queries.DeleteTransactionEvents;
        command.Connection = connection;
        command.Transaction = transaction;
        command.Parameters.AddWithValue("@TransactionId", transactionId);
        command.Parameters.AddWithValue("@LastEventId", lastEventId);

        await command.PrepareAsync(cancellationToken);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task RescheduleEventAsync(
        NpgsqlConnection connection,
        Guid eventId,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = _options.Queries.RescheduleEvent;
        command.Connection = connection;
        command.Transaction = transaction;
        command.Parameters.AddWithValue("@EventId", eventId);

        await command.PrepareAsync(cancellationToken);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}

internal static partial class Logs
{
    [LoggerMessage(
        1,
        LogLevel.Warning,
        "Could not deserialize message body for message with ID {Id}. The message will be retried.")]
    public static partial void CouldNotDeserializeMessageBody(this ILogger logger, Guid id);

    [LoggerMessage(2, LogLevel.Error, "Message with ID {Id} was not an event request.")]
    public static partial void SendMessageWasNotAnEventRequest(this ILogger logger, Guid id);

    [LoggerMessage(
        3,
        LogLevel.Warning,
        "Could not resolve a dispatch endpoint for the destination {DestinationAddress} of message with ID {Id}. The message will be retried.")]
    public static partial void CouldNotDetermineDestination(this ILogger logger, Guid id, string? destinationAddress);

    [LoggerMessage(4, LogLevel.Information, "Outbox processor is sleeping for {NextPollingInterval}.")]
    public static partial void OutboxProcessorSleeping(this ILogger logger, TimeSpan nextPollingInterval);

    [LoggerMessage(5, LogLevel.Error, "An unexpected error occurred while processing outbox event")]
    public static partial void UnexpectedErrorWhileProcessingOutboxEvent(this ILogger logger, Exception exception);

    [LoggerMessage(
        6,
        LogLevel.Warning,
        "Could not resolve the message type {MessageType} of message with ID {Id}. The message will be retried.")]
    public static partial void CouldNotResolveMessageType(this ILogger logger, Guid id, string? messageType);

    [LoggerMessage(
        7,
        LogLevel.Critical,
        "Message with ID {Id} of outbox transaction {TransactionId} exhausted all {MaxAttempts} attempts and will not be retried. Later messages of the same transaction stay in the outbox.")]
    public static partial void OutboxMessageExhausted(
        this ILogger logger,
        Guid id,
        Guid transactionId,
        int maxAttempts);

    [LoggerMessage(8, LogLevel.Information, "Expired outbox message with ID {Id} was discarded.")]
    public static partial void OutboxMessageExpired(this ILogger logger, Guid id);

    [LoggerMessage(
        9,
        LogLevel.Error,
        "Dispatching message with ID {Id} failed. The message and the later messages of its outbox transaction will be retried.")]
    public static partial void OutboxMessageDispatchFailed(this ILogger logger, Exception exception, Guid id);
}

file static class Serializer
{
    public static MessageEnvelope? ReadMessageEnvelopeSafe(NpgsqlDataReader reader, int ordinal, ILogger logger)
    {
        try
        {
            var envelope = reader.GetFieldValue<ReadOnlyMemory<byte>>(ordinal);
            return MessageEnvelopeReader.Parse(envelope);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reading message envelope");
            return null;
        }
    }

    public static void TryCancel(this CancellationTokenSource cts)
    {
        try
        {
            cts.Cancel();
        }
        catch
        {
            // ignore
        }
    }
}
