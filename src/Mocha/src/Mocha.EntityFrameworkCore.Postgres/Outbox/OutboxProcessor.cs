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
    private readonly ILogger<PostgresOutboxProcessor> _logger;
    private readonly IServiceProvider _services;
    private readonly IMessagingRuntime _runtime;
    private readonly IOutboxSignal _signal;
    private readonly ObjectPool<DispatchContext> _contextPool;
    private readonly PostgresMessageOutboxQueries _queries;

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
    /// <param name="queries">
    /// The SQL query definitions for Postgres outbox table operations.
    /// </param>
    internal PostgresOutboxProcessor(
        ILogger<PostgresOutboxProcessor> logger,
        IServiceProvider services,
        IMessagingRuntime runtime,
        IMessagingPools pools,
        IOutboxSignal signal,
        PostgresMessageOutboxQueries queries)
    {
        _logger = logger;
        _services = services;
        _runtime = runtime;
        _signal = signal;
        _contextPool = pools.DispatchContext;
        _queries = queries;
    }

    /// <summary>
    /// Runs the outbox processing loop, dispatching one message per iteration and sleeping
    /// until the next message is due or a signal is received.
    /// </summary>
    /// <remarks>
    /// The loop continues until <paramref name="cancellationToken"/> is cancelled. Each iteration
    /// locks a single outbox row using <c>FOR UPDATE SKIP LOCKED</c>, dispatches the envelope,
    /// and deletes the row on success. Messages that fail are retried with exponential backoff
    /// up to 10 attempts before being dropped.
    /// </remarks>
    /// <param name="connection">An open Postgres connection to use for outbox queries.</param>
    /// <param name="cancellationToken">A token that signals when the processor should stop.</param>
    public async Task ProcessAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var activity = OpenTelemetry.Source.StartActivity(
                "Process Message Outbox",
                ActivityKind.Consumer,
                new ActivityContext());

            using var joinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            try
            {
                var signaled = _signal.WaitAsync(joinedCts.Token);

                var result = await ProcessEventAsync(connection, cancellationToken);

                if (!result)
                {
                    var nextPollingInterval = await GetNextPollingIntervalAsync(connection, cancellationToken);

                    activity?.Dispose();

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
                else
                {
                    activity?.Dispose();
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

    private async Task<TimeSpan?> GetNextPollingIntervalAsync(
        NpgsqlConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = _queries.NextPollingInterval;
        await command.PrepareAsync(cancellationToken);

        var result = await command.ExecuteScalarAsync(cancellationToken);

        return result is not null and not DBNull ? ((DateTime)result - DateTimeOffset.UtcNow).Duration() : null;
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
            command.CommandText = _queries.ProcessEvent;

            await command.PrepareAsync(cancellationToken);

            try
            {
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);

                if (await reader.ReadAsync(cancellationToken))
                {
                    var id = reader.GetGuid(0);
                    var envelope = Serializer.ReadMessageEnvelopeSafe(reader, 1);
                    var messageType = GetMessageType(envelope?.MessageType);
                    var isReply = envelope?.Headers?.IsReply() ?? false;
                    var endpoint = isReply
                        ? GetReplyDispatchEndpoint(envelope?.DestinationAddress)
                        : GetDispatchEndpoint(envelope?.DestinationAddress);

                    if (envelope is null || messageType is null || endpoint is null)
                    {
                        _logger.CouldNotDeserializeMessageBody(id);

                        await reader.CloseAsync();

                        await DeleteEventAsync(connection, id, transaction, cancellationToken);

                        // we skipped this message yet, still have to check for the next ones
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
                await transaction.CommitAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.UnexpectedErrorWhileProcessingOutboxEvent(ex);
            await transaction.RollbackAsync(cancellationToken);
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
                    $"outbox send {envelope.MessageId}",
                    ActivityKind.Client,
                    parentContext);

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
        command.CommandText = _queries.DeleteEvent;
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
        LogLevel.Critical,
        "Could not deserialize message body for message with ID {Id}. Message Dropped.")]
    public static partial void CouldNotDeserializeMessageBody(this ILogger logger, Guid id);

    [LoggerMessage(2, LogLevel.Error, "Message with ID {Id} was not an event request.")]
    public static partial void SendMessageWasNotAnEventRequest(this ILogger logger, Guid id);

    [LoggerMessage(3, LogLevel.Error, "Could not determine destination for message with ID {Id}. Message Discarded.")]
    public static partial void CouldNotDetermineDestination(this ILogger logger, Guid id);

    [LoggerMessage(4, LogLevel.Information, "Outbox processor is sleeping for {NextPollingInterval}.")]
    public static partial void OutboxProcessorSleeping(this ILogger logger, TimeSpan nextPollingInterval);

    [LoggerMessage(5, LogLevel.Error, "An unexpected error occurred while processing outbox event")]
    public static partial void UnexpectedErrorWhileProcessingOutboxEvent(this ILogger logger, Exception exception);
}

file static class Serializer
{
    public static MessageEnvelope? ReadMessageEnvelopeSafe(NpgsqlDataReader reader, int ordinal)
    {
        try
        {
            var envelope = reader.GetFieldValue<ReadOnlyMemory<byte>>(ordinal);
            return MessageEnvelopeReader.Parse(envelope);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading message envelope: {ex.Message}");
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
