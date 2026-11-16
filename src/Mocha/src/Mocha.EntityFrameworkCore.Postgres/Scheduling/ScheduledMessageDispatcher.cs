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
    // A Critical entry is logged every time the attempt count of a message reaches a multiple of this value.
    private const int CriticalAttemptInterval = 10;

    // The pause before the dispatcher looks again for a due message that another dispatcher holds.
    private static readonly TimeSpan s_heldMessagePollingInterval = TimeSpan.FromSeconds(1);

    // The pause of the dispatcher after a failed dispatch, which is as long as the retry delay of the message.
    private static readonly TimeSpan s_failurePause = TimeSpan.FromSeconds(ScheduledMessageQueries.RetryDelaySeconds);

    private readonly ILogger<ScheduledMessageDispatcher> _logger;
    private readonly IServiceProvider _services;
    private readonly IMessagingRuntime _runtime;
    private readonly ISchedulerSignal _signal;
    private readonly ObjectPool<DispatchContext> _contextPool;
    private readonly PostgresScheduledMessageOptions _options;
    private readonly TimeProvider _timeProvider;

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
    /// <param name="options">
    /// The scheduled message options containing the SQL queries and connection factory.
    /// </param>
    internal ScheduledMessageDispatcher(
        ILogger<ScheduledMessageDispatcher> logger,
        IServiceProvider services,
        IMessagingRuntime runtime,
        IMessagingPools pools,
        ISchedulerSignal signal,
        PostgresScheduledMessageOptions options)
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
    /// Runs the scheduled message processing loop, dispatching due messages and sleeping
    /// until the next message is due or a signal is received.
    /// </summary>
    /// <param name="cancellationToken">A token that signals when the dispatcher should stop.</param>
    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = await ProcessPendingAsync(cancellationToken);

                if (result.Status is MessageStatus.DispatchFailed)
                {
                    // A signal does not end the pause, so that a transport that is down is not called
                    // for every message that is scheduled meanwhile.
                    await Task.Delay(s_failurePause, _timeProvider, cancellationToken);
                }
                else if (result.NextWakeTime is { } nextWakeTime)
                {
                    _logger.SchedulerSleepingUntil(nextWakeTime);
                    await _signal.WaitUntilAsync(nextWakeTime, cancellationToken);
                }
                else
                {
                    // No scheduled messages - sleep until notified.
                    await _signal.WaitUntilAsync(DateTimeOffset.MaxValue, cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Normal shutdown.
            }
        }
    }

    private async ValueTask<MessageResult> ProcessPendingAsync(CancellationToken cancellationToken)
    {
        await using var scope = _services.CreateAsyncScope();
        await using var connection = _options.CreateConnection(scope.ServiceProvider);
        await connection.OpenAsync(cancellationToken);

        while (true)
        {
            var result = await ProcessMessageAsync(connection, cancellationToken);

            if (result.Status is not MessageStatus.Processed)
            {
                return result;
            }

            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    private async Task<DateTimeOffset?> GetNextWakeTimeAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = _options.Queries.NextWakeInterval;
        await command.PrepareAsync(cancellationToken);

        if (await command.ExecuteScalarAsync(cancellationToken) is not double secondsUntilDue)
        {
            return null;
        }

        var now = _timeProvider.GetUtcNow();

        if (secondsUntilDue <= 0)
        {
            // The claim did not return the message that is due, so another dispatcher holds it.
            return now + s_heldMessagePollingInterval;
        }

        return secondsUntilDue < (DateTimeOffset.MaxValue - now).TotalSeconds
            ? now.AddSeconds(secondsUntilDue)
            : DateTimeOffset.MaxValue;
    }

    private async Task<MessageResult> ProcessMessageAsync(
        NpgsqlConnection connection,
        CancellationToken cancellationToken)
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
            command.CommandText = _options.Queries.ProcessMessage;

            await command.PrepareAsync(cancellationToken);

            try
            {
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);

                if (await reader.ReadAsync(cancellationToken))
                {
                    var id = reader.GetGuid(0);
                    var envelope = Serializer.ReadMessageEnvelopeSafe(reader, 1, _logger);
                    var timesSent = reader.GetInt32(2);

                    // A message this instance cannot read or route stays in the table and is
                    // retried after the retry delay.
                    if (envelope is null)
                    {
                        _logger.CouldNotDeserializeScheduledMessageBody(id);
                        LogIfRepeatedlyFailed(id, null, timesSent);

                        return MessageResult.Processed;
                    }

                    var messageType = GetMessageType(envelope.MessageType);

                    if (messageType is null)
                    {
                        _logger.CouldNotResolveScheduledMessageType(id, envelope.MessageType);
                        LogIfRepeatedlyFailed(id, envelope, timesSent);

                        return MessageResult.Processed;
                    }

                    var endpoint = ResolveDispatchEndpoint(envelope);

                    if (endpoint is null)
                    {
                        _logger.CouldNotDetermineScheduledMessageDestination(id, envelope.DestinationAddress);
                        LogIfRepeatedlyFailed(id, envelope, timesSent);

                        return MessageResult.Processed;
                    }

                    try
                    {
                        await SendAsync(envelope, endpoint, messageType, cancellationToken);

                        await reader.CloseAsync();

                        await DeleteMessageAsync(connection, id, transaction, cancellationToken);
                    }
                    catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                    {
                        _logger.ScheduledMessageDispatchFailed(id, ex);

                        await reader.CloseAsync();

                        await UpdateLastErrorAsync(connection, id, ex, transaction, cancellationToken);

                        LogIfRepeatedlyFailed(id, envelope, timesSent);

                        return MessageResult.DispatchFailed;
                    }

                    return MessageResult.Processed;
                }
                else
                {
                    // The wake time is read in the transaction of the claim, so that both see the same time.
                    await reader.CloseAsync();

                    return new MessageResult(
                        MessageStatus.NoneDue,
                        await GetNextWakeTimeAsync(connection, transaction, cancellationToken));
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
        }
        catch (Exception ex)
        {
            // Log only - no RollbackAsync here (commit already handled in finally)
            _logger.UnexpectedErrorWhileProcessingScheduledMessage(ex);
            throw;
        }
    }

    private void LogIfRepeatedlyFailed(Guid id, MessageEnvelope? envelope, int timesSent)
    {
        if (timesSent % CriticalAttemptInterval == 0)
        {
            _logger.ScheduledMessageFailedRepeatedly(
                id,
                envelope?.MessageId,
                envelope?.MessageType,
                envelope?.DestinationAddress,
                timesSent);
        }
    }

    private readonly record struct MessageResult(MessageStatus Status, DateTimeOffset? NextWakeTime)
    {
        public static MessageResult Processed { get; } = new(MessageStatus.Processed, null);

        public static MessageResult DispatchFailed { get; } = new(MessageStatus.DispatchFailed, null);
    }

    private enum MessageStatus
    {
        // A message was claimed and the dispatcher continues with the next one.
        Processed,

        // The transport did not accept a claimed message and the dispatcher pauses.
        DispatchFailed,

        // No message was due.
        NoneDue
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

                activity?.SetMessageId(envelope.MessageId);

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
        command.CommandText = _options.Queries.DeleteMessage;
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
                exceptionType = exception.GetType().FullName
            });

        await using var command = connection.CreateCommand();
        command.CommandText = _options.Queries.UpdateLastError;
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
        LogLevel.Warning,
        "Could not deserialize message body for scheduled message with ID {Id}. The message will be retried.")]
    public static partial void CouldNotDeserializeScheduledMessageBody(this ILogger logger, Guid id);

    [LoggerMessage(2, LogLevel.Information, "Scheduler sleeping until {WakeTime}.")]
    public static partial void SchedulerSleepingUntil(this ILogger logger, DateTimeOffset wakeTime);

    [LoggerMessage(3, LogLevel.Error, "An unexpected error occurred while processing scheduled message")]
    public static partial void UnexpectedErrorWhileProcessingScheduledMessage(this ILogger logger, Exception exception);

    [LoggerMessage(4, LogLevel.Warning, "Failed to dispatch scheduled message {Id}. Error recorded for retry.")]
    public static partial void ScheduledMessageDispatchFailed(this ILogger logger, Guid id, Exception exception);

    [LoggerMessage(
        5,
        LogLevel.Critical,
        "Scheduled message {Id} (message ID {MessageId}, type {MessageType}) to {DestinationAddress} failed on attempt {Attempt}. The message is retried.")]
    public static partial void ScheduledMessageFailedRepeatedly(
        this ILogger logger,
        Guid id,
        string? messageId,
        string? messageType,
        string? destinationAddress,
        int attempt);

    [LoggerMessage(
        6,
        LogLevel.Warning,
        "Could not resolve the message type {MessageType} of scheduled message with ID {Id}. The message will be retried.")]
    public static partial void CouldNotResolveScheduledMessageType(this ILogger logger, Guid id, string? messageType);

    [LoggerMessage(
        7,
        LogLevel.Warning,
        "Could not resolve a dispatch endpoint for the destination {DestinationAddress} of scheduled message with ID {Id}. The message will be retried.")]
    public static partial void CouldNotDetermineScheduledMessageDestination(
        this ILogger logger,
        Guid id,
        string? destinationAddress);
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
