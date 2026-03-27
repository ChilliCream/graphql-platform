using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Mocha.Middlewares;
using Mocha.Outbox;
using Npgsql;
using NpgsqlTypes;

namespace Mocha.Scheduling;

/// <summary>
/// Continuously polls the Postgres scheduled messages table for messages that are due and dispatches them
/// through the messaging runtime, using the scheduler signal to sleep efficiently between polls.
/// </summary>
public sealed class ScheduledMessageDispatcher
{
    private readonly ILogger<ScheduledMessageDispatcher> _logger;
    private readonly IServiceProvider _services;
    private readonly IMessagingRuntime _runtime;
    private readonly ISchedulerSignal _signal;
    private readonly ObjectPool<DispatchContext> _contextPool;
    private readonly ScheduledMessageQueries _queries;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduledMessageDispatcher"/> class.
    /// </summary>
    /// <param name="logger">
    /// The logger used to record scheduled message processing diagnostics and errors.
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
    /// The scheduler signal used to sleep efficiently and wake when new messages are scheduled.
    /// </param>
    /// <param name="queries">
    /// The SQL query definitions for Postgres scheduled messages table operations.
    /// </param>
    internal ScheduledMessageDispatcher(
        ILogger<ScheduledMessageDispatcher> logger,
        IServiceProvider services,
        IMessagingRuntime runtime,
        IMessagingPools pools,
        ISchedulerSignal signal,
        ScheduledMessageQueries queries)
    {
        _logger = logger;
        _services = services;
        _runtime = runtime;
        _signal = signal;
        _contextPool = pools.DispatchContext;
        _queries = queries;
    }

    /// <summary>
    /// Runs the scheduled message processing loop, dispatching one message per iteration and sleeping
    /// until the next message is due or a signal is received.
    /// </summary>
    /// <remarks>
    /// The loop continues until <paramref name="cancellationToken"/> is cancelled. Each iteration
    /// locks a single row using <c>FOR UPDATE SKIP LOCKED</c>, dispatches the envelope,
    /// and deletes the row on success. Messages that fail are retried with exponential backoff
    /// up to 10 attempts before being dropped.
    /// </remarks>
    /// <param name="connection">An open Postgres connection to use for scheduled message queries.</param>
    /// <param name="cancellationToken">A token that signals when the dispatcher should stop.</param>
    public async Task ProcessAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var activity = OpenTelemetry.Source.StartActivity(
                "Process Scheduled Message",
                ActivityKind.Consumer,
                new ActivityContext());

            try
            {
                var result = await ProcessMessageAsync(connection, cancellationToken);

                if (!result)
                {
                    var nextWakeTime = await GetNextWakeTimeAsync(connection, cancellationToken);

                    activity?.Dispose();

                    if (nextWakeTime is not null)
                    {
                        _logger.SchedulerSleepingUntil(nextWakeTime.Value);
                        await _signal.WaitUntilAsync(nextWakeTime.Value, cancellationToken);
                    }
                    else
                    {
                        // No scheduled messages - sleep until notified.
                        await _signal.WaitUntilAsync(DateTimeOffset.MaxValue, cancellationToken);
                    }
                }
                else
                {
                    activity?.Dispose();
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Normal shutdown.
            }
            catch (Exception ex)
            {
                _logger.UnexpectedErrorWhileProcessingScheduledMessage(ex);
                activity?.Dispose();

                // Back off briefly to avoid a tight failure loop.
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
    }

    private async Task<DateTimeOffset?> GetNextWakeTimeAsync(
        NpgsqlConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = _queries.NextWakeTime;
        await command.PrepareAsync(cancellationToken);

        var result = await command.ExecuteScalarAsync(cancellationToken);

        return result is not null and not DBNull
            ? new DateTimeOffset((DateTime)result, TimeSpan.Zero)
            : null;
    }

    private async Task<bool> ProcessMessageAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        using var activity = OpenTelemetry.Source.StartActivity(
            "Process Scheduled Message Event",
            ActivityKind.Producer,
            new ActivityContext());

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = _queries.ProcessMessage;

            await command.PrepareAsync(cancellationToken);

            try
            {
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);

                if (await reader.ReadAsync(cancellationToken))
                {
                    var id = reader.GetGuid(0);
                    var envelope = Serializer.ReadMessageEnvelopeSafe(reader, 1, _logger);
                    var messageType = GetMessageType(envelope?.MessageType);
                    var isReply = envelope?.Headers?.IsReply() ?? false;
                    var endpoint = isReply
                        ? GetReplyDispatchEndpoint(envelope?.DestinationAddress)
                        : GetDispatchEndpoint(envelope?.DestinationAddress);

                    if (envelope is null || messageType is null || endpoint is null)
                    {
                        _logger.CouldNotDeserializeScheduledMessageBody(id);

                        await reader.CloseAsync();

                        await DeleteMessageAsync(connection, id, transaction, cancellationToken);

                        // we skipped this message, still have to check for the next ones
                        return true;
                    }

                    try
                    {
                        await SendAsync(envelope, endpoint, messageType, cancellationToken);

                        await reader.CloseAsync();

                        await DeleteMessageAsync(connection, id, transaction, cancellationToken);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.ScheduledMessageDispatchFailed(id, ex);

                        await reader.CloseAsync();

                        await UpdateLastErrorAsync(connection, id, ex, transaction, cancellationToken);
                    }

                    return true;
                }
                else
                {
                    return false;
                }
            }
            finally
            {
                try
                {
                    await transaction.CommitAsync(cancellationToken);
                }
                catch
                {
                    // Commit failed (e.g., connection lost). Attempt rollback.
                    // If commit actually succeeded server-side, the message stays
                    // with times_sent incremented - safe, just causes a retry.
                    try { await transaction.RollbackAsync(CancellationToken.None); }
                    catch
                    {
                        /* swallow */
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Log only - no RollbackAsync here (commit already handled in finally)
            _logger.UnexpectedErrorWhileProcessingScheduledMessage(ex);
            throw;
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
            if (destinationAddress is null || !Uri.TryCreate(destinationAddress, UriKind.Absolute, out var uri))
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
                    "scheduler send",
                    ActivityKind.Client,
                    parentContext);
                activity?.SetTag("messaging.message_id", envelope.MessageId);
                activity?.Start();
            }
        }

        var context = _contextPool.Get();
        try
        {
            await using var scope = _services.CreateAsyncScope();

            context.Initialize(scope.ServiceProvider, endpoint, _runtime, messageType, cancellationToken);

            context.SkipScheduler();
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

    private async Task DeleteMessageAsync(
        NpgsqlConnection connection,
        Guid eventId,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = _queries.DeleteMessage;
        command.Connection = connection;
        command.Transaction = transaction;
        command.Parameters.AddWithValue("@id", eventId);

        await command.PrepareAsync(cancellationToken);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task UpdateLastErrorAsync(
        NpgsqlConnection connection,
        Guid id,
        Exception exception,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        using var errorJson = JsonSerializer.SerializeToDocument(
            new
            {
                message = exception.Message,
                exceptionType = exception.GetType().FullName,
                stackTrace = exception.StackTrace
            });

        await using var command = connection.CreateCommand();
        command.CommandText = _queries.UpdateLastError;
        command.Connection = connection;
        command.Transaction = transaction;
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@last_error", NpgsqlDbType.Jsonb, errorJson);

        await command.PrepareAsync(cancellationToken);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}

internal static partial class SchedulerLogs
{
    [LoggerMessage(
        1,
        LogLevel.Critical,
        "Could not deserialize message body for scheduled message with ID {Id}. Message Dropped.")]
    public static partial void CouldNotDeserializeScheduledMessageBody(this ILogger logger, Guid id);

    [LoggerMessage(2, LogLevel.Information, "Scheduler sleeping until {WakeTime}.")]
    public static partial void SchedulerSleepingUntil(this ILogger logger, DateTimeOffset wakeTime);

    [LoggerMessage(3, LogLevel.Error, "An unexpected error occurred while processing scheduled message")]
    public static partial void UnexpectedErrorWhileProcessingScheduledMessage(this ILogger logger, Exception exception);

    [LoggerMessage(4, LogLevel.Warning, "Failed to dispatch scheduled message {Id}. Error recorded for retry.")]
    public static partial void ScheduledMessageDispatchFailed(this ILogger logger, Guid id, Exception exception);
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
}
