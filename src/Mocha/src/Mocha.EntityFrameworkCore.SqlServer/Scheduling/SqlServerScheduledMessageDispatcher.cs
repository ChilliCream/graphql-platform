using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Mocha.Middlewares;
using Mocha.Outbox;

namespace Mocha.Scheduling;

/// <summary>
/// Continuously polls the SQL Server scheduled messages table for messages that are due and dispatches them
/// through the messaging runtime, using the scheduler signal to sleep efficiently between polls.
/// </summary>
public sealed class SqlServerScheduledMessageDispatcher
{
    private readonly ILogger<SqlServerScheduledMessageDispatcher> _logger;
    private readonly IServiceProvider _services;
    private readonly IMessagingRuntime _runtime;
    private readonly ISchedulerSignal _signal;
    private readonly ObjectPool<DispatchContext> _contextPool;
    private readonly SqlServerScheduledMessageQueries _queries;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerScheduledMessageDispatcher"/> class.
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
    /// The T-SQL query definitions for SQL Server scheduled messages table operations.
    /// </param>
    internal SqlServerScheduledMessageDispatcher(
        ILogger<SqlServerScheduledMessageDispatcher> logger,
        IServiceProvider services,
        IMessagingRuntime runtime,
        IMessagingPools pools,
        ISchedulerSignal signal,
        SqlServerScheduledMessageQueries queries)
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
    /// locks a single row using <c>UPDLOCK, ROWLOCK, READPAST</c>, dispatches the envelope,
    /// and deletes the row on success. Messages that fail are retried with exponential backoff
    /// up to 10 attempts before being dropped.
    /// </remarks>
    /// <param name="connection">An open SQL Server connection to use for scheduled message queries.</param>
    /// <param name="cancellationToken">A token that signals when the dispatcher should stop.</param>
    public async Task ProcessAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = await ProcessMessageAsync(connection, cancellationToken);

                if (!result)
                {
                    var nextWakeTime = await GetNextWakeTimeAsync(connection, cancellationToken);

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
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Normal shutdown.
            }
        }
    }

    private async Task<DateTimeOffset?> GetNextWakeTimeAsync(
        SqlConnection connection,
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

    private async Task<bool> ProcessMessageAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        using var activity = OpenTelemetry.Source.StartActivity(
            "Process Scheduled Message Event",
            ActivityKind.Producer,
            new ActivityContext());

        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = _queries.ProcessMessage;

            try
            {
                await command.PrepareAsync(cancellationToken);
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);

                if (await reader.ReadAsync(cancellationToken))
                {
                    var id = reader.GetGuid(0);
                    var envelope = Serializer.ReadMessageEnvelopeSafe(reader, 1, _logger);
                    var timesSent = reader.GetInt32(2);
                    var maxAttempts = reader.GetInt32(3);
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

                        if (timesSent >= maxAttempts)
                        {
                            _logger.ScheduledMessageExhausted(id, maxAttempts);
                        }
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
        SqlConnection connection,
        Guid eventId,
        SqlTransaction transaction,
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
        SqlConnection connection,
        Guid id,
        Exception exception,
        SqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        var errorJson = JsonSerializer.Serialize(
            new
            {
                message = exception.Message,
                exceptionType = exception.GetType().FullName
            });

        await using var command = connection.CreateCommand();
        command.CommandText = _queries.UpdateLastError;
        command.Connection = connection;
        command.Transaction = transaction;
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@last_error", errorJson);

        await command.PrepareAsync(cancellationToken);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}

internal static partial class SqlServerSchedulerLogs
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

    [LoggerMessage(5, LogLevel.Warning,
        "Scheduled message {Id} exhausted all {MaxAttempts} retry attempts and will not be retried.")]
    public static partial void ScheduledMessageExhausted(this ILogger logger, Guid id, int maxAttempts);
}

file static class Serializer
{
    public static MessageEnvelope? ReadMessageEnvelopeSafe(SqlDataReader reader, int ordinal, ILogger logger)
    {
        try
        {
            var envelopeString = reader.GetString(ordinal);
            var envelope = Encoding.UTF8.GetBytes(envelopeString);
            return MessageEnvelopeReader.Parse(envelope);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reading message envelope");
            return null;
        }
    }
}
