using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocha.Features;
using Mocha.Threading;
using Mocha.Transport.Postgres.Features;

namespace Mocha.Transport.Postgres;

/// <summary>
/// A receive endpoint that consumes messages from a PostgreSQL queue by polling the database
/// and processing each message through the receive middleware pipeline.
/// </summary>
/// <remarks>
/// Message processing uses an <see cref="AsyncAutoResetEvent"/> to coordinate between
/// LISTEN/NOTIFY signals and a polling loop. When a notification arrives for the queue name,
/// the signal is set to trigger an immediate read. Messages are read in batches and each
/// message is processed individually via <see cref="ReceiveEndpoint.ExecuteAsync"/>.
/// Successfully processed messages are deleted; faulted messages are released back to the queue.
/// </remarks>
public sealed class PostgresReceiveEndpoint(PostgresMessagingTransport transport)
    : ReceiveEndpoint<PostgresReceiveEndpointConfiguration>(transport)
{
    private int _maxBatchSize;
    private int _maxConcurrency;
    private readonly Guid _consumerId = Guid.NewGuid();

    /// <summary>
    /// Gets the PostgreSQL queue this endpoint is consuming from.
    /// </summary>
    public PostgresQueue Queue { get; private set; } = null!;

    private CancellationTokenSource? _cts;
    private Task? _pollingTask;
    private IDisposable? _notificationSubscription;
    private AsyncAutoResetEvent? _signal;
    private PostgresDelayedTrigger? _delayedTrigger;

    protected override void OnInitialize(
        IMessagingConfigurationContext context,
        PostgresReceiveEndpointConfiguration configuration)
    {
        if (configuration.QueueName is null)
        {
            throw new InvalidOperationException("Queue name is required");
        }

        _maxBatchSize = configuration.MaxBatchSize ?? PostgresReceiveEndpointConfiguration.Defaults.MaxBatchSize;
        _maxConcurrency = configuration.MaxConcurrency ?? ReceiveEndpointConfiguration.Defaults.MaxConcurrency;
    }

    protected override void OnComplete(
        IMessagingConfigurationContext context,
        PostgresReceiveEndpointConfiguration configuration)
    {
        if (configuration.QueueName is null)
        {
            throw new InvalidOperationException("Queue name is required");
        }

        var topology = (PostgresMessagingTopology)Transport.Topology;

        Queue =
            topology.Queues.FirstOrDefault(q => q.Name == configuration.QueueName)
            ?? throw new InvalidOperationException("Queue not found");

        Source = Queue;
    }

    protected override ValueTask OnStartAsync(IMessagingRuntimeContext context, CancellationToken cancellationToken)
    {
        var logger = context.Services.GetRequiredService<ILogger<PostgresReceiveEndpoint>>();

        _signal = new AsyncAutoResetEvent();
        _cts = new CancellationTokenSource();

        // Subscribe to LISTEN/NOTIFY for this queue name
        _notificationSubscription = transport.NotificationListener.Subscribe(queueName =>
        {
            // Match on queue name, or empty payload from reconnection recovery
            if (string.IsNullOrEmpty(queueName)
                || string.Equals(queueName, Queue.Name, StringComparison.Ordinal))
            {
                _signal.Set();
            }
        });

        _pollingTask = PollMessagesAsync(logger, _cts.Token);

        return ValueTask.CompletedTask;
    }

    private async Task PollMessagesAsync(ILogger logger, CancellationToken cancellationToken)
    {
        // Initial signal to process any pending messages
        _signal!.Set();

        var consecutiveFailures = 0;
        const int maxBackoffSeconds = 30;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _signal.WaitAsync(cancellationToken);

                var hasMore = true;
                while (hasMore && !cancellationToken.IsCancellationRequested)
                {
                    using var batch = await transport.MessageStore.ReadMessagesAsync(
                        _maxBatchSize,
                        Queue.Name,
                        _consumerId,
                        cancellationToken);

                    if (batch.Count == 0)
                    {
                        hasMore = false;
                        continue;
                    }

                    await Parallel.ForEachAsync(
                        batch.Messages,
                        new ParallelOptions
                        {
                            MaxDegreeOfParallelism = _maxConcurrency,
                            CancellationToken = cancellationToken
                        },
                        (message, ct) => new ValueTask(ProcessMessageAsync(message, logger, ct)));

                    // If we got a full batch, there may be more messages
                    hasMore = batch.Count >= _maxBatchSize;
                }

                consecutiveFailures = 0;

                // After draining all messages, check for future scheduled messages
                await UpdateScheduledTriggerAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Graceful shutdown
                break;
            }
            catch (Exception ex)
            {
                consecutiveFailures++;
                var backoffSeconds = Math.Min((int)Math.Pow(2, Math.Min(consecutiveFailures, 5)), maxBackoffSeconds);

                if (consecutiveFailures >= 10)
                {
                    logger.PersistentPollingError(ex, Queue.Name, consecutiveFailures);
                }
                else
                {
                    logger.PollingError(ex, Queue.Name, backoffSeconds);
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(backoffSeconds), cancellationToken);

                    // Wait for the database to become reachable before resuming the polling loop.
                    while (!await transport.ConnectionManager.IsHealthyAsync())
                    {
                        logger.WaitingForDatabase(Queue.Name);
                        await Task.Delay(TimeSpan.FromSeconds(maxBackoffSeconds), cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    private async Task ProcessMessageAsync(
        PostgresMessageItem message,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            await ExecuteAsync(
                static (context, state) =>
                {
                    var feature = context.Features.GetOrSet<PostgresReceiveFeature>();
                    feature.MessageItem = state;
                    feature.TransportMessageId = state.TransportMessageId;
                },
                message,
                cancellationToken);

            await transport.MessageStore.DeleteMessageAsync(
                message.TransportMessageId,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.MessageProcessingFailed(ex, message.TransportMessageId);

            try
            {
                var errorInfo = ErrorInfo.From(ex);
                await transport.MessageStore.ReleaseMessageAsync(
                    message.TransportMessageId,
                    errorInfo,
                    cancellationToken);
            }
            catch (Exception releaseEx)
            {
                logger.MessageReleaseFailed(releaseEx, message.TransportMessageId);
            }
        }
    }

    private async Task UpdateScheduledTriggerAsync(CancellationToken cancellationToken)
    {
        var scheduledAt = await transport.MessageStore.GetNextScheduledTimeAsync(
            Queue.Name,
            cancellationToken);

        if (scheduledAt is null)
        {
            return;
        }

        if (scheduledAt <= DateTimeOffset.UtcNow)
        {
            _signal!.Set();
            return;
        }

        if (_delayedTrigger is not null)
        {
            if (_delayedTrigger.ScheduledAt <= scheduledAt && !_delayedTrigger.IsSet)
            {
                return;
            }

            await _delayedTrigger.DisposeAsync();
        }

        _delayedTrigger = new PostgresDelayedTrigger(scheduledAt.Value, _signal!);
    }

    protected override async ValueTask OnStopAsync(
        IMessagingRuntimeContext context,
        CancellationToken cancellationToken)
    {
        _notificationSubscription?.Dispose();
        _notificationSubscription = null;

        if (_cts is not null)
        {
            await _cts.CancelAsync();
        }

        if (_pollingTask is not null)
        {
            try
            {
                await _pollingTask.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
            }
            catch (TimeoutException)
            {
                // Proceed with cleanup
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
            }

            _pollingTask = null;
        }

        if (_delayedTrigger is not null)
        {
            await _delayedTrigger.DisposeAsync();
            _delayedTrigger = null;
        }

        _cts?.Dispose();
        _cts = null;

        _signal?.Dispose();
        _signal = null;
    }
}

internal static partial class Logs
{
    [LoggerMessage(LogLevel.Error, "Persistent error in polling loop for queue {QueueName} ({Failures} consecutive failures).")]
    public static partial void PersistentPollingError(this ILogger logger, Exception exception, string queueName, int failures);

    [LoggerMessage(LogLevel.Warning, "Error in polling loop for queue {QueueName}, retrying in {Backoff}s.")]
    public static partial void PollingError(this ILogger logger, Exception exception, string queueName, int backoff);

    [LoggerMessage(LogLevel.Error, "Error processing message {TransportMessageId}.")]
    public static partial void MessageProcessingFailed(this ILogger logger, Exception exception, Guid transportMessageId);

    [LoggerMessage(LogLevel.Error, "Error releasing message {TransportMessageId}.")]
    public static partial void MessageReleaseFailed(this ILogger logger, Exception exception, Guid transportMessageId);

    [LoggerMessage(LogLevel.Warning, "Database is unreachable for queue {QueueName}, waiting for connectivity to resume.")]
    public static partial void WaitingForDatabase(this ILogger logger, string queueName);
}
