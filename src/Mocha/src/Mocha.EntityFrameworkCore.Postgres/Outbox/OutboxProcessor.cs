using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Mocha.Middlewares;
using Npgsql;

namespace Mocha.Outbox;

/// <summary>
/// Continuously polls the Postgres outbox table for pending messages and dispatches them
/// through the messaging runtime, using exponential backoff for retry scheduling.
/// </summary>
public sealed class PostgresOutboxProcessor
{
    private static readonly TimeSpan s_minPollingInterval = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan s_maxPollingInterval = TimeSpan.FromSeconds(1);
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

                var delay = nextPollingInterval is { } interval && interval < s_maxPollingInterval
                    ? interval
                    : s_maxPollingInterval;
                if (delay < s_minPollingInterval)
                {
                    delay = s_minPollingInterval;
                }

                _logger.OutboxProcessorSleeping(delay);

                await Task.WhenAny(Task.Delay(delay, cancellationToken), signaled);
                joinedCts.TryCancel();
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

        return result is not null and not DBNull ? ((DateTime)result - DateTimeOffset.UtcNow) : null;
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
            // Lock an event for processing and increment TimesSent in case of failure
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = _options.Queries.ProcessEvent;

            await command.PrepareAsync(cancellationToken);

            try
            {
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);

                if (await reader.ReadAsync(cancellationToken))
                {
                    var id = reader.GetGuid(0);
                    var envelope = Serializer.ReadMessageEnvelopeSafe(reader, 1, _logger);
                    var timesSent = reader.GetInt32(2);

                    // A message this instance cannot read or route stays in the outbox and is
                    // retried after its backoff.
                    if (envelope is null)
                    {
                        _logger.CouldNotDeserializeMessageBody(id);
                        LogIfExhausted(id, timesSent);

                        return true;
                    }

                    if (envelope.DeliverBy is { } deadline && deadline <= _timeProvider.GetUtcNow())
                    {
                        await reader.CloseAsync();
                        await DeleteEventAsync(connection, id, transaction, cancellationToken);
                        _logger.OutboxMessageExpired(id);
                        return true;
                    }

                    var messageType = GetMessageType(envelope.MessageType);

                    if (messageType is null)
                    {
                        _logger.CouldNotResolveMessageType(id, envelope.MessageType);
                        LogIfExhausted(id, timesSent);

                        return true;
                    }

                    var endpoint = ResolveDispatchEndpoint(envelope);

                    if (endpoint is null)
                    {
                        _logger.CouldNotDetermineDestination(id, envelope.DestinationAddress);
                        LogIfExhausted(id, timesSent);

                        return true;
                    }

                    await SendAsync(envelope, endpoint, messageType, cancellationToken);

                    await reader.CloseAsync();

                    await DeleteEventAsync(connection, id, transaction, cancellationToken);

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
                    // A failed commit can have an unknown outcome.
                    try
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                    }
                    catch
                    {
                        // Preserve the commit failure.
                    }

                    throw;
                }
            }
        }
        catch (Exception ex)
        {
            // Log only - no RollbackAsync here (commit already handled in finally)
            _logger.UnexpectedErrorWhileProcessingOutboxEvent(ex);
            throw;
        }
    }

    private void LogIfExhausted(Guid id, int timesSent)
    {
        if (timesSent >= PostgresMessageOutboxQueries.MaxAttempts)
        {
            _logger.OutboxMessageExhausted(id, PostgresMessageOutboxQueries.MaxAttempts);
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

            context.MessageId = envelope.MessageId;
            context.CorrelationId = envelope.CorrelationId;
            context.ConversationId = envelope.ConversationId;
            context.CausationId = envelope.CausationId;
            context.SourceAddress = ParseAddress(envelope.SourceAddress);
            context.DestinationAddress = ParseAddress(envelope.DestinationAddress);
            context.ResponseAddress = ParseAddress(envelope.ResponseAddress);
            context.FaultAddress = ParseAddress(envelope.FaultAddress);
            context.ContentType = MessageContentType.Parse(envelope.ContentType);
            context.SentAt = envelope.SentAt ?? context.SentAt;
            context.DeliverBy = envelope.DeliverBy;
            context.ScheduledTime = envelope.ScheduledTime;
            context.Host = envelope.Host ?? context.Host;
            if (envelope.Headers is { } headers)
            {
                context.Headers.AddRange(headers);
            }

            context.Envelope = envelope;

            await endpoint.DeliverAsync(context);
        }
        finally
        {
            _contextPool.Return(context);
            activity?.Dispose();
        }
    }

    private static Uri? ParseAddress(string? address)
        => Uri.TryCreate(address, UriKind.Absolute, out var parsed) ? parsed : null;

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
        "Message with ID {Id} exhausted all {MaxAttempts} attempts and will not be retried.")]
    public static partial void OutboxMessageExhausted(this ILogger logger, Guid id, int maxAttempts);

    [LoggerMessage(8, LogLevel.Information, "Expired outbox message with ID {Id} was discarded.")]
    public static partial void OutboxMessageExpired(this ILogger logger, Guid id);
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
