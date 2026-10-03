using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocha.Threading;
using Mocha.Transport.Postgres.Features;

namespace Mocha.Transport.Postgres;

/// <summary>
/// A receive endpoint that consumes messages from a PostgreSQL queue by polling the database
/// and processing each message through the receive middleware pipeline.
/// </summary>
/// <remarks>
/// Messages are settled only while leased by this endpoint. Stopping returns unstarted messages
/// without counting a delivery attempt and releases cancelled messages as counted delivery attempts.
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

    private ILogger _logger = null!;
    private CancellationTokenSource? _receiveCts;
    private Task? _pollingTask;
    private IDisposable? _notificationSubscription;
    private AsyncAutoResetEvent _signal = null!;
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
        _logger = context.Services.GetRequiredService<ILogger<PostgresReceiveEndpoint>>();
        _receiveCts = new CancellationTokenSource();

        _signal = new AsyncAutoResetEvent();

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

        _pollingTask = PollMessagesAsync(_logger, _receiveCts.Token);

        return ValueTask.CompletedTask;
    }

    private async Task PollMessagesAsync(ILogger logger, CancellationToken receiveToken)
    {
        var receiveStopped = Task.Delay(Timeout.InfiniteTimeSpan, receiveToken);
        var processingToken = ProcessingToken;

        // Initial signal to process any pending messages
        _signal.Set();

        var consecutiveFailures = 0;
        const int maxBackoffSeconds = 30;

        try
        {
            while (!receiveToken.IsCancellationRequested)
            {
                try
                {
                    await _signal.WaitAsync(receiveToken);

                    var hasMore = true;
                    while (hasMore && !receiveToken.IsCancellationRequested)
                    {
                        using var batch = await transport.MessageStore.ReadMessagesAsync(
                            _maxBatchSize,
                            Queue.Name,
                            _consumerId,
                            receiveToken);

                        if (batch.Count == 0)
                        {
                            hasMore = false;
                            continue;
                        }

                        await ProcessBatchAsync(batch, logger, receiveStopped, processingToken);

                        // If we got a full batch, there may be more messages
                        hasMore = batch.Count >= _maxBatchSize;
                    }

                    consecutiveFailures = 0;

                    if (receiveToken.IsCancellationRequested)
                    {
                        break;
                    }

                    // After draining all messages, check for future scheduled messages
                    await UpdateScheduledTriggerAsync(receiveToken);
                }
                catch (OperationCanceledException) when (receiveToken.IsCancellationRequested)
                {
                    // Graceful shutdown
                    break;
                }
                catch (Exception ex)
                {
                    consecutiveFailures++;
                    var backoffSeconds = Math.Min(
                        (int)Math.Pow(2, Math.Min(consecutiveFailures, 5)),
                        maxBackoffSeconds);

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
                        await Task.Delay(TimeSpan.FromSeconds(backoffSeconds), receiveToken);

                        // Wait for the database to become reachable before resuming the polling loop.
                        while (!await transport.ConnectionManager.IsHealthyAsync())
                        {
                            logger.WaitingForDatabase(Queue.Name);
                            await Task.Delay(TimeSpan.FromSeconds(maxBackoffSeconds), receiveToken);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
        }
        finally
        {
            if (_delayedTrigger is not null)
            {
                await _delayedTrigger.DisposeAsync();
            }

            _signal.Dispose();
        }
    }

    private async Task ProcessBatchAsync(
        PostgresMessageBatch batch,
        ILogger logger,
        Task receiveStopped,
        CancellationToken processingToken)
    {
        var messages = batch.Messages;
        var lastClaimed = -1;
        var processing = Task.CompletedTask;

        if (!receiveStopped.IsCompleted)
        {
            var workers = new Task[Math.Min(_maxConcurrency, messages.Count)];
            for (var i = 0; i < workers.Length; i++)
            {
                workers[i] = Task.Run(ProcessNextAsync, CancellationToken.None);
            }

            processing = Task.WhenAll(workers);
            await Task.WhenAny(processing, receiveStopped);
        }

        // Once receiving stops, messages that no worker has claimed go back to the queue right away.
        var claimed = Interlocked.Exchange(ref lastClaimed, messages.Count);
        if (claimed + 1 < messages.Count)
        {
            var unprocessed = new Guid[messages.Count - claimed - 1];
            for (var i = 0; i < unprocessed.Length; i++)
            {
                unprocessed[i] = messages[claimed + 1 + i].TransportMessageId;
            }

            try
            {
                using var cleanupCts = new CancellationTokenSource(Transport.Options.Shutdown.CleanupTimeout);
                await transport.MessageStore.ReturnMessagesAsync(unprocessed, _consumerId, cleanupCts.Token);
            }
            catch (Exception ex)
            {
                logger.MessageReturnFailed(ex, unprocessed.Length, Queue.Name);
            }
        }

        await processing;

        if (processingToken.IsCancellationRequested)
        {
            await ReleaseLeasedMessagesAsync(logger);
        }

        async Task ProcessNextAsync()
        {
            int index;
            while ((index = Interlocked.Increment(ref lastClaimed)) < messages.Count)
            {
                await ProcessMessageAsync(messages[index], logger, processingToken);
            }
        }
    }

    private async Task ProcessMessageAsync(
        PostgresMessageItem message,
        ILogger logger,
        CancellationToken processingToken)
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
                processingToken);

            // Settlement is not cancelled with the receive, because it only applies while the lease is held.
            await transport.MessageStore.DeleteMessageAsync(
                message.TransportMessageId,
                _consumerId,
                CancellationToken.None);
        }
        catch (Exception) when (processingToken.IsCancellationRequested)
        {
            // The message stays leased until the endpoint releases it.
        }
        catch (Exception ex)
        {
            logger.MessageProcessingFailed(ex, message.TransportMessageId);

            try
            {
                var errorInfo = ErrorInfo.From(ex);
                await transport.MessageStore.ReleaseMessageAsync(
                    message.TransportMessageId,
                    _consumerId,
                    errorInfo,
                    CancellationToken.None);
            }
            catch (Exception releaseEx)
            {
                logger.MessageReleaseFailed(releaseEx, message.TransportMessageId);
            }
        }
    }

    private async Task ReleaseLeasedMessagesAsync(ILogger logger)
    {
        try
        {
            using var cleanupCts = new CancellationTokenSource(Transport.Options.Shutdown.CleanupTimeout);
            await transport.MessageStore.ReleaseMessagesAsync(_consumerId, cleanupCts.Token);
        }
        catch (Exception ex)
        {
            logger.LeasedMessageReleaseFailed(ex, Queue.Name);
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
            _signal.Set();
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

        _delayedTrigger = new PostgresDelayedTrigger(scheduledAt.Value, _signal);
    }

    protected override async ValueTask OnStopAsync(
        IMessagingRuntimeContext context,
        CancellationToken cancellationToken)
    {
        _notificationSubscription?.Dispose();
        _notificationSubscription = null;

        if (_receiveCts is not null)
        {
            await _receiveCts.CancelAsync();
        }

        if (_pollingTask is { } pollingTask)
        {
            _pollingTask = null;

            if (!await WaitForProcessingAsync(pollingTask, cancellationToken))
            {
                // Handlers that outlast the stop can no longer settle their messages once another
                // consumer leases them.
                await ReleaseLeasedMessagesAsync(_logger);
            }
        }

        _receiveCts?.Dispose();
        _receiveCts = null;
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

    [LoggerMessage(LogLevel.Warning, "Error returning {Count} unprocessed message(s) to queue {QueueName}.")]
    public static partial void MessageReturnFailed(this ILogger logger, Exception exception, int count, string queueName);

    [LoggerMessage(LogLevel.Warning, "Error releasing leased messages to queue {QueueName}.")]
    public static partial void LeasedMessageReleaseFailed(this ILogger logger, Exception exception, string queueName);

    [LoggerMessage(LogLevel.Warning, "Database is unreachable for queue {QueueName}, waiting for connectivity to resume.")]
    public static partial void WaitingForDatabase(this ILogger logger, string queueName);
}
