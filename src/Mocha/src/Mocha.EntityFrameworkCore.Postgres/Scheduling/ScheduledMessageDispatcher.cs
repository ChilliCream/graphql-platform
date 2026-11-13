using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Mocha.Middlewares;
using Mocha.Outbox;
using Mocha.Threading;
using Npgsql;
using NpgsqlTypes;

namespace Mocha.Scheduling;

/// <summary>
/// Continuously polls the Postgres scheduled messages table for messages that are due and dispatches them
/// through the messaging runtime, using the scheduler signal to sleep efficiently between polls.
/// </summary>
public sealed class ScheduledMessageDispatcher
{
    private static readonly TimeSpan s_pauseDelay = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan s_maxPauseDelay = TimeSpan.FromSeconds(10);

    // The pause before the dispatcher looks again for a due message that another dispatcher holds.
    private static readonly TimeSpan s_heldMessagePollingInterval = TimeSpan.FromSeconds(1);

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
        // The pause before a released message is claimed again, which grows until a message is sent.
        var pause = new ExponentialBackoff(int.MaxValue, s_pauseDelay, s_maxPauseDelay);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var pass = await ProcessPendingAsync(pause, cancellationToken);

                if (pass.IsPaused)
                {
                    // The released message is claimed again after the pause, with or without a signal.
                    await pause.WaitAsync(cancellationToken);
                }
                else if (pass.SecondsUntilDue is not { } secondsUntilDue)
                {
                    // No scheduled messages - sleep until notified.
                    await _signal.WaitUntilAsync(DateTimeOffset.MaxValue, cancellationToken);
                }
                else if (secondsUntilDue > 0)
                {
                    var wakeTime = GetWakeTime(secondsUntilDue);
                    _logger.SchedulerSleepingUntil(wakeTime);
                    await _signal.WaitUntilAsync(wakeTime, cancellationToken);
                }
                else
                {
                    // The claim did not return the message that is due, so another dispatcher holds it.
                    await _signal.WaitUntilAsync(
                        _timeProvider.GetUtcNow() + s_heldMessagePollingInterval,
                        cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Normal shutdown.
            }
        }
    }

    private DateTimeOffset GetWakeTime(double secondsUntilDue)
    {
        var now = _timeProvider.GetUtcNow();

        return secondsUntilDue < (DateTimeOffset.MaxValue - now).TotalSeconds
            ? now.AddSeconds(secondsUntilDue)
            : DateTimeOffset.MaxValue;
    }

    private async ValueTask<PassResult> ProcessPendingAsync(
        ExponentialBackoff pause,
        CancellationToken cancellationToken)
    {
        await using var scope = _services.CreateAsyncScope();
        await using var connection = _options.CreateConnection(scope.ServiceProvider);
        await connection.OpenAsync(cancellationToken);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // An exception before the commit rolls the claim back when the transaction is disposed.
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            switch (await ProcessMessageAsync(connection, transaction, pause, cancellationToken))
            {
                case ClaimResult.Empty:
                    // The time is read in the transaction of the claim, so that both see the same time.
                    return new PassResult(
                        IsPaused: false,
                        await GetSecondsUntilDueAsync(connection, transaction, cancellationToken));

                case ClaimResult.Released:
                    return new PassResult(IsPaused: true, SecondsUntilDue: null);
            }
        }
    }

    private async Task<double?> GetSecondsUntilDueAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = _options.Queries.NextWakeInterval;
        await command.PrepareAsync(cancellationToken);

        return await command.ExecuteScalarAsync(cancellationToken) as double?;
    }

    private async Task<ClaimResult> ProcessMessageAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        ExponentialBackoff pause,
        CancellationToken cancellationToken)
    {
        using var activity = OpenTelemetry.Source.StartActivity(
            "Process Scheduled Message Event",
            ActivityKind.Producer,
            new ActivityContext());

        try
        {
            Guid id;
            int timesSent;
            int maxAttempts;
            var outcome = DispatchOutcome.Failed;
            Exception? dispatchError = null;

            await using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = _options.Queries.ProcessMessage;

                await command.PrepareAsync(cancellationToken);

                await using var reader = await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return ClaimResult.Empty;
                }

                id = reader.GetGuid(0);
                var envelope = Serializer.ReadMessageEnvelopeSafe(reader, 1, _logger);
                timesSent = reader.GetInt32(2);
                maxAttempts = reader.GetInt32(3);

                if (TryResolve(id, envelope, out var messageType, out var endpoint))
                {
                    try
                    {
                        await SendAsync(envelope, endpoint, messageType, cancellationToken);

                        outcome = DispatchOutcome.Sent;
                    }
                    catch (Exception ex)
                    {
                        outcome = OnDispatchFailed(ex, id, envelope, cancellationToken);
                        dispatchError = ex;
                    }
                }
            }

            switch (outcome)
            {
                case DispatchOutcome.Sent:
                    // The transport has the message, so its row is removed whatever the token says.
                    await DeleteMessageAsync(connection, id, transaction, CancellationToken.None);
                    await transaction.CommitAsync(CancellationToken.None);
                    pause.Reset();
                    return ClaimResult.Processed;

                case DispatchOutcome.Released:
                    // The rollback restores the attempt count and the due time of the row.
                    await transaction.RollbackAsync(CancellationToken.None);
                    return ClaimResult.Released;

                default:
                    if (dispatchError is not null)
                    {
                        await UpdateLastErrorAsync(connection, id, dispatchError, transaction, cancellationToken);
                    }

                    // The commit stores the incremented attempt count and the backoff of the row.
                    await transaction.CommitAsync(cancellationToken);
                    LogIfExhausted(id, timesSent, maxAttempts);
                    return ClaimResult.Processed;
            }
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.UnexpectedErrorWhileProcessingScheduledMessage(ex);
            throw;
        }
    }

    /// <summary>
    /// Resolves what a dispatch of the message needs, and logs why when the message cannot be dispatched.
    /// </summary>
    private bool TryResolve(
        Guid id,
        [NotNullWhen(true)] MessageEnvelope? envelope,
        [NotNullWhen(true)] out MessageType? messageType,
        [NotNullWhen(true)] out DispatchEndpoint? endpoint)
    {
        messageType = null;
        endpoint = null;

        // A message this instance cannot read or route stays in the table and is
        // retried after its backoff.
        if (envelope is null)
        {
            _logger.CouldNotDeserializeScheduledMessageBody(id);
            return false;
        }

        messageType = GetMessageType(envelope.MessageType);

        if (messageType is null)
        {
            _logger.CouldNotResolveScheduledMessageType(id, envelope.MessageType);
            return false;
        }

        endpoint = ResolveDispatchEndpoint(envelope);

        if (endpoint is null)
        {
            _logger.CouldNotDetermineScheduledMessageDestination(id, envelope.DestinationAddress);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Logs why the dispatch did not complete and returns whether the attempt counts.
    /// </summary>
    private DispatchOutcome OnDispatchFailed(
        Exception exception,
        Guid id,
        MessageEnvelope envelope,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            _logger.ScheduledMessageDispatchCancelled(
                exception,
                id,
                envelope.MessageId,
                envelope.MessageType,
                envelope.DestinationAddress);

            return DispatchOutcome.Released;
        }

        if (exception is TransportUnavailableException)
        {
            _logger.ScheduledMessageTransportUnavailable(
                exception,
                id,
                envelope.MessageId,
                envelope.MessageType,
                envelope.DestinationAddress);

            return DispatchOutcome.Released;
        }

        _logger.ScheduledMessageDispatchFailed(id, exception);

        return DispatchOutcome.Failed;
    }

    private void LogIfExhausted(Guid id, int timesSent, int maxAttempts)
    {
        if (timesSent >= maxAttempts)
        {
            _logger.ScheduledMessageExhausted(id, maxAttempts);
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

    private enum ClaimResult
    {
        // No message was due.
        Empty,

        // A message was claimed and its outcome was stored.
        Processed,

        // A message was claimed and handed back unchanged.
        Released
    }

    private enum DispatchOutcome
    {
        // The transport accepted the message.
        Sent,

        // The attempt failed and counts.
        Failed,

        // The attempt was cut short and does not count.
        Released
    }

    // What the dispatcher does after a pass: it pauses after a released claim and waits for a signal otherwise.
    private readonly record struct PassResult(bool IsPaused, double? SecondsUntilDue);
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
        "Scheduled message {Id} exhausted all {MaxAttempts} retry attempts and will not be retried.")]
    public static partial void ScheduledMessageExhausted(this ILogger logger, Guid id, int maxAttempts);

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

    [LoggerMessage(
        8,
        LogLevel.Warning,
        "The dispatch of scheduled message {Id} (message ID {MessageId}, type {MessageType}) to {DestinationAddress} failed because the transport is unavailable. The attempt is not counted and the message is dispatched again.")]
    public static partial void ScheduledMessageTransportUnavailable(
        this ILogger logger,
        Exception exception,
        Guid id,
        string? messageId,
        string? messageType,
        string? destinationAddress);

    [LoggerMessage(
        9,
        LogLevel.Debug,
        "The dispatch of scheduled message {Id} (message ID {MessageId}, type {MessageType}) to {DestinationAddress} was cancelled because the host is stopping. The attempt is not counted.")]
    public static partial void ScheduledMessageDispatchCancelled(
        this ILogger logger,
        Exception exception,
        Guid id,
        string? messageId,
        string? messageType,
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
