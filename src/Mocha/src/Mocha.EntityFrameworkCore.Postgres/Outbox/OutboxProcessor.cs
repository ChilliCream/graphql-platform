using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Mocha.Middlewares;
using Mocha.Threading;
using Npgsql;

namespace Mocha.Outbox;

/// <summary>
/// Continuously polls the Postgres outbox table for pending messages and dispatches them
/// through the messaging runtime, using exponential backoff for retry scheduling.
/// </summary>
public sealed class PostgresOutboxProcessor
{
    private static readonly TimeSpan s_pauseDelay = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan s_maxPauseDelay = TimeSpan.FromSeconds(10);

    // An idle worker reads the outbox again after this time, which finds a message that nobody signalled.
    private static readonly TimeSpan s_idlePollingInterval = TimeSpan.FromSeconds(30);

    // The pause before a worker looks again for a due message that another worker holds.
    private static readonly TimeSpan s_heldMessagePollingInterval = TimeSpan.FromSeconds(1);

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
    /// Gets or sets the longest time a worker waits before it reads the outbox again.
    /// </summary>
    internal TimeSpan IdlePollingInterval { get; set; } = s_idlePollingInterval;

    /// <summary>
    /// Runs the outbox processing loop, dispatching pending messages and waiting
    /// until the next message is due or a signal is received.
    /// </summary>
    /// <param name="cancellationToken">A token that signals when the processor should stop.</param>
    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        var workers = new Task[_options.MaxConcurrentMessages];
        for (var i = 0; i < workers.Length; i++)
        {
            workers[i] = ProcessWorkerAsync(cancellationToken);
        }

        await Task.WhenAll(workers);
    }

    private async Task ProcessWorkerAsync(CancellationToken cancellationToken)
    {
        using var worker = new WorkerState(_options.DispatchTimeout, cancellationToken);
        Task? signaled = null;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                signaled ??= _signal.WaitAsync(cancellationToken);

                var pass = await ProcessPendingAsync(worker, cancellationToken);

                if (pass.IsPaused)
                {
                    // The released message is claimed again after the pause, with or without a signal.
                    await worker.Backoff.WaitAsync(cancellationToken);
                    continue;
                }

                _logger.OutboxProcessorSleeping(pass.PollingInterval);

                try
                {
                    await signaled.WaitAsync(pass.PollingInterval, cancellationToken);
                }
                catch (TimeoutException)
                {
                    continue;
                }

                signaled = null;
            }
            catch (Exception) when (cancellationToken.IsCancellationRequested)
            {
                // The host is stopping.
            }
            catch (OperationCanceledException) when (signaled is { IsCanceled: true })
            {
                signaled = null;
            }
            catch (Exception ex)
            {
                // A failure outside of a dispatch, such as a lost database connection, pauses this worker only.
                if (signaled is { IsFaulted: true })
                {
                    signaled = null;
                }

                _logger.UnexpectedErrorWhileProcessingOutboxEvent(ex);
                await worker.Backoff.WaitAsync(cancellationToken);
            }
        }
    }

    private async ValueTask<PassResult> ProcessPendingAsync(WorkerState worker, CancellationToken cancellationToken)
    {
        await using var scope = _services.CreateAsyncScope();
        await using var connection = _options.CreateConnection(scope.ServiceProvider);
        await connection.OpenAsync(cancellationToken);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var activity = OpenTelemetry.Source.StartActivity(
                "Process Message Outbox",
                ActivityKind.Consumer,
                new ActivityContext());

            // An exception before the commit rolls the claim back when the transaction is disposed.
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            switch (await ProcessEventAsync(connection, transaction, worker, cancellationToken))
            {
                case ClaimResult.Empty:
                    // The interval is read in the transaction of the claim, so that both see the same time.
                    return new PassResult(
                        IsPaused: false,
                        await GetNextPollingIntervalAsync(connection, transaction, cancellationToken));

                case ClaimResult.Released:
                    return new PassResult(IsPaused: true, PollingInterval: TimeSpan.Zero);
            }
        }
    }

    private async Task<TimeSpan> GetNextPollingIntervalAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = _options.Queries.NextPollingInterval;
        await command.PrepareAsync(cancellationToken);

        if (await command.ExecuteScalarAsync(cancellationToken) is not double secondsUntilDue)
        {
            return IdlePollingInterval;
        }

        if (secondsUntilDue <= 0)
        {
            // The claim did not return the message that is due, so another worker holds it.
            return s_heldMessagePollingInterval;
        }

        return secondsUntilDue < IdlePollingInterval.TotalSeconds
            ? TimeSpan.FromSeconds(secondsUntilDue)
            : IdlePollingInterval;
    }

    private async Task<ClaimResult> ProcessEventAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        WorkerState worker,
        CancellationToken cancellationToken)
    {
        using var activity = OpenTelemetry.Source.StartActivity(
            "Process Message Outbox Event",
            ActivityKind.Producer,
            new ActivityContext());

        Guid id;
        MessageEnvelope? envelope;
        int timesSent;
        DispatchOutcome outcome;

        // Lock an event for processing and increment TimesSent in case of failure
        await using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = _options.Queries.ProcessEvent;

            await command.PrepareAsync(cancellationToken);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return ClaimResult.Empty;
            }

            id = reader.GetGuid(0);
            envelope = Serializer.ReadMessageEnvelopeSafe(reader, 1, _logger);
            timesSent = reader.GetInt32(2);

            if (TryResolve(id, envelope, out var messageType, out var endpoint, out outcome))
            {
                var dispatchToken = worker.StartDispatch();

                try
                {
                    await SendAsync(envelope, endpoint, messageType, dispatchToken);

                    outcome = DispatchOutcome.Sent;
                }
                catch (Exception ex)
                {
                    outcome = OnDispatchFailed(ex, id, envelope, timesSent, dispatchToken, cancellationToken);
                }
                finally
                {
                    worker.EndDispatch();
                }
            }
        }

        switch (outcome)
        {
            case DispatchOutcome.Sent:
                try
                {
                    // The transport has the message, so its row is removed whatever the token says.
                    await DeleteEventAsync(connection, id, transaction, CancellationToken.None);
                    await transaction.CommitAsync(CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.OutboxMessageNotRemoved(
                        ex,
                        id,
                        envelope?.MessageId,
                        envelope?.MessageType,
                        envelope?.DestinationAddress);

                    // The worker pauses before it claims the message again.
                    return ClaimResult.Released;
                }

                worker.Backoff.Reset();
                return ClaimResult.Processed;

            case DispatchOutcome.Expired:
                await DeleteEventAsync(connection, id, transaction, cancellationToken);
                await transaction.CommitAsync(CancellationToken.None);
                _logger.OutboxMessageExpired(id);
                return ClaimResult.Processed;

            case DispatchOutcome.Released:
                // The rollback restores the attempt count and the position of the row.
                await transaction.RollbackAsync(CancellationToken.None);
                return ClaimResult.Released;

            default:
                // The commit stores the incremented attempt count and the backoff of the row.
                await transaction.CommitAsync(CancellationToken.None);
                LogIfExhausted(id, envelope, timesSent);
                return ClaimResult.Processed;
        }
    }

    /// <summary>
    /// Resolves what a dispatch of the message needs. When the message cannot be dispatched,
    /// <paramref name="outcome"/> says how its claim ends.
    /// </summary>
    private bool TryResolve(
        Guid id,
        [NotNullWhen(true)] MessageEnvelope? envelope,
        [NotNullWhen(true)] out MessageType? messageType,
        [NotNullWhen(true)] out DispatchEndpoint? endpoint,
        out DispatchOutcome outcome)
    {
        messageType = null;
        endpoint = null;
        outcome = DispatchOutcome.Failed;

        // A message this instance cannot read or route stays in the outbox and is
        // retried after its backoff.
        if (envelope is null)
        {
            _logger.CouldNotDeserializeMessageBody(id);
            return false;
        }

        if (envelope.DeliverBy is { } deadline && deadline <= _timeProvider.GetUtcNow())
        {
            outcome = DispatchOutcome.Expired;
            return false;
        }

        messageType = GetMessageType(envelope.MessageType);

        if (messageType is null)
        {
            _logger.CouldNotResolveMessageType(id, envelope.MessageType);
            return false;
        }

        endpoint = ResolveDispatchEndpoint(envelope);

        if (endpoint is null)
        {
            _logger.CouldNotDetermineDestination(id, envelope.DestinationAddress);
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
        int timesSent,
        CancellationToken dispatchToken,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            _logger.OutboxDispatchCancelled(
                exception,
                id,
                envelope.MessageId,
                envelope.MessageType,
                envelope.DestinationAddress);

            return DispatchOutcome.Released;
        }

        if (dispatchToken.IsCancellationRequested)
        {
            _logger.OutboxDispatchTimedOut(
                exception,
                id,
                envelope.MessageId,
                envelope.MessageType,
                envelope.DestinationAddress,
                _options.DispatchTimeout);

            return DispatchOutcome.Released;
        }

        if (exception is TransportUnavailableException)
        {
            _logger.OutboxTransportUnavailable(
                exception,
                id,
                envelope.MessageId,
                envelope.MessageType,
                envelope.DestinationAddress);

            return DispatchOutcome.Released;
        }

        _logger.OutboxDispatchFailed(
            exception,
            id,
            envelope.MessageId,
            envelope.MessageType,
            envelope.DestinationAddress,
            timesSent,
            _options.MaxAttempts);

        return DispatchOutcome.Failed;
    }

    private void LogIfExhausted(Guid id, MessageEnvelope? envelope, int timesSent)
    {
        if (timesSent >= _options.MaxAttempts)
        {
            // An envelope that could not be read leaves only the id of the row.
            _logger.OutboxMessageExhausted(
                id,
                envelope?.MessageId,
                envelope?.MessageType,
                envelope?.DestinationAddress,
                _options.MaxAttempts);
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

    private enum ClaimResult
    {
        // No message was eligible.
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

        // The delivery deadline of the message has passed.
        Expired,

        // The attempt failed and counts.
        Failed,

        // The attempt was cut short and does not count.
        Released
    }

    // What a worker does after a pass: it pauses after a released claim and waits for a signal otherwise.
    private readonly record struct PassResult(bool IsPaused, TimeSpan PollingInterval);

    // The state one worker keeps across the messages it dispatches.
    private sealed class WorkerState(TimeSpan dispatchTimeout, CancellationToken stopToken) : IDisposable
    {
        private CancellationTokenSource? _dispatchCancellation;

        // The pause between two passes after a released claim or a failure of the worker.
        public ExponentialBackoff Backoff { get; } = new(int.MaxValue, s_pauseDelay, s_maxPauseDelay);

        // Returns the token of the next dispatch, which the dispatch timeout and a host stop cancel.
        public CancellationToken StartDispatch()
        {
            if (dispatchTimeout == Timeout.InfiniteTimeSpan)
            {
                return stopToken;
            }

            _dispatchCancellation ??= CancellationTokenSource.CreateLinkedTokenSource(stopToken);
            _dispatchCancellation.CancelAfter(dispatchTimeout);

            return _dispatchCancellation.Token;
        }

        public void EndDispatch()
        {
            // A source that was cancelled cannot be reset, so the next dispatch gets a new one.
            if (_dispatchCancellation is { } cancellation && !cancellation.TryReset())
            {
                cancellation.Dispose();
                _dispatchCancellation = null;
            }
        }

        public void Dispose() => _dispatchCancellation?.Dispose();
    }
}

internal static partial class Logs
{
    [LoggerMessage(
        1,
        LogLevel.Warning,
        "Could not deserialize message body for message with ID {Id}. The message will be retried.")]
    public static partial void CouldNotDeserializeMessageBody(this ILogger logger, Guid id);

    [LoggerMessage(
        3,
        LogLevel.Warning,
        "Could not resolve a dispatch endpoint for the destination {DestinationAddress} of message with ID {Id}. The message will be retried.")]
    public static partial void CouldNotDetermineDestination(this ILogger logger, Guid id, string? destinationAddress);

    [LoggerMessage(4, LogLevel.Debug, "Outbox processor is sleeping for {NextPollingInterval}.")]
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
        "Outbox message {Id} (message ID {MessageId}, type {MessageType}) to {DestinationAddress} exhausted all {MaxAttempts} attempts and will not be retried.")]
    public static partial void OutboxMessageExhausted(
        this ILogger logger,
        Guid id,
        string? messageId,
        string? messageType,
        string? destinationAddress,
        int maxAttempts);

    [LoggerMessage(8, LogLevel.Information, "Expired outbox message with ID {Id} was discarded.")]
    public static partial void OutboxMessageExpired(this ILogger logger, Guid id);

    [LoggerMessage(
        9,
        LogLevel.Warning,
        "The dispatch of outbox message {Id} (message ID {MessageId}, type {MessageType}) to {DestinationAddress} failed because the transport is unavailable. The attempt is not counted and the message is dispatched again.")]
    public static partial void OutboxTransportUnavailable(
        this ILogger logger,
        Exception exception,
        Guid id,
        string? messageId,
        string? messageType,
        string? destinationAddress);

    [LoggerMessage(
        10,
        LogLevel.Warning,
        "The dispatch of outbox message {Id} (message ID {MessageId}, type {MessageType}) to {DestinationAddress} did not complete within the dispatch timeout of {DispatchTimeout}. The attempt is not counted and the message is dispatched again.")]
    public static partial void OutboxDispatchTimedOut(
        this ILogger logger,
        Exception exception,
        Guid id,
        string? messageId,
        string? messageType,
        string? destinationAddress,
        TimeSpan dispatchTimeout);

    [LoggerMessage(
        11,
        LogLevel.Debug,
        "The dispatch of outbox message {Id} (message ID {MessageId}, type {MessageType}) to {DestinationAddress} was cancelled because the host is stopping. The attempt is not counted.")]
    public static partial void OutboxDispatchCancelled(
        this ILogger logger,
        Exception exception,
        Guid id,
        string? messageId,
        string? messageType,
        string? destinationAddress);

    [LoggerMessage(
        12,
        LogLevel.Error,
        "The dispatch of outbox message {Id} (message ID {MessageId}, type {MessageType}) to {DestinationAddress} failed on attempt {Attempt} of {MaxAttempts}.")]
    public static partial void OutboxDispatchFailed(
        this ILogger logger,
        Exception exception,
        Guid id,
        string? messageId,
        string? messageType,
        string? destinationAddress,
        int attempt,
        int maxAttempts);

    [LoggerMessage(
        13,
        LogLevel.Error,
        "Outbox message {Id} (message ID {MessageId}, type {MessageType}) to {DestinationAddress} was accepted by the transport, but removing its outbox row failed. The message is dispatched again unless the removal took effect.")]
    public static partial void OutboxMessageNotRemoved(
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
