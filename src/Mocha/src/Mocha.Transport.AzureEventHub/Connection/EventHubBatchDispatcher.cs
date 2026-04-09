using System.Threading.Channels;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.Logging;

namespace Mocha.Transport.AzureEventHub;

/// <summary>
/// Batches outbound <see cref="EventData"/> messages into <see cref="EventDataBatch"/> instances
/// for efficient dispatch via a single <see cref="EventHubProducerClient"/>.
/// </summary>
/// <remarks>
/// Thread-safe for concurrent <see cref="EnqueueAsync"/> calls. A background loop drains the
/// internal channel, accumulating events into the current batch. The batch is flushed when it
/// is full (i.e. <see cref="EventDataBatch.TryAdd"/> returns <c>false</c>) or when the
/// max wait time elapses since the last flush.
/// </remarks>
internal sealed class EventHubBatchDispatcher : IAsyncDisposable
{
    private readonly EventHubProducerClient _producer;
    private readonly ILogger _logger;
    private readonly TimeSpan _maxWaitTime;
    private readonly Channel<PendingEvent> _channel;
    private readonly Task _processLoop;
    private readonly CancellationTokenSource _cts = new();
    private readonly List<PendingEvent> _pending = [];

    /// <summary>
    /// Creates a new batch dispatcher for the specified producer.
    /// </summary>
    /// <param name="producer">The Event Hub producer client to send batches through.</param>
    /// <param name="logger">Logger for batch lifecycle events.</param>
    /// <param name="maxWaitTime">
    /// Maximum time to wait before flushing a partially full batch.
    /// Defaults to 100ms if not specified.
    /// </param>
    public EventHubBatchDispatcher(
        EventHubProducerClient producer,
        ILogger logger,
        TimeSpan? maxWaitTime = null)
    {
        _producer = producer;
        _logger = logger;
        _maxWaitTime = maxWaitTime ?? TimeSpan.FromMilliseconds(100);
        _channel = Channel.CreateUnbounded<PendingEvent>(
            new UnboundedChannelOptions { SingleReader = true });
        _processLoop = Task.Run(ProcessLoopAsync);
    }

    /// <summary>
    /// Enqueues an event for batched dispatch. The returned task completes when the event
    /// has been sent as part of a batch.
    /// </summary>
    /// <param name="eventData">The event data to enqueue.</param>
    /// <param name="sendOptions">Optional send options (e.g. partition key).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async ValueTask EnqueueAsync(
        EventData eventData,
        SendEventOptions? sendOptions,
        CancellationToken cancellationToken)
    {
        var pending = new PendingEvent(eventData, sendOptions);

        await _channel.Writer.WriteAsync(pending, cancellationToken);

        await pending.Completion.Task.WaitAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _channel.Writer.TryComplete();
        await _cts.CancelAsync();

        try
        {
            await _processLoop;
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown.
        }

        _cts.Dispose();
    }

    private async Task ProcessLoopAsync()
    {
        var reader = _channel.Reader;
        var cancellationToken = _cts.Token;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Wait for the first event to arrive.
                if (!await reader.WaitToReadAsync(cancellationToken))
                {
                    break;
                }

                await DrainAndSendAsync(reader, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.BatchDispatchError(ex);
            }
        }

        // Drain remaining events on shutdown.
        await DrainAndSendAsync(reader, CancellationToken.None);
    }

    private async Task DrainAndSendAsync(
        ChannelReader<PendingEvent> reader,
        CancellationToken cancellationToken)
    {
        EventDataBatch? batch = null;
        string? currentPartitionKey = null;
        string? currentPartitionId = null;
        _pending.Clear();

        try
        {
            using var timer = new CancellationTokenSource(_maxWaitTime);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(
                timer.Token, cancellationToken);

            while (true)
            {
                PendingEvent item;

                if (reader.TryRead(out item!))
                {
                    // Got an item immediately.
                }
                else
                {
                    try
                    {
                        if (!await reader.WaitToReadAsync(linked.Token))
                        {
                            break;
                        }

                        continue;
                    }
                    catch (OperationCanceledException) when (timer.IsCancellationRequested)
                    {
                        // Timer expired — flush what we have.
                        break;
                    }
                }

                // Determine partition targeting for this event.
                var itemPartitionKey = item.SendOptions?.PartitionKey;
                var itemPartitionId = item.SendOptions?.PartitionId;

                // If targeting differs from the current batch, flush and start a new batch.
                if (batch is not null
                    && (itemPartitionKey != currentPartitionKey || itemPartitionId != currentPartitionId))
                {
                    if (batch.Count > 0)
                    {
                        await SendBatchAsync(batch, _pending, cancellationToken);
                    }
                    else
                    {
                        batch.Dispose();
                    }

                    batch = null;
                    _pending.Clear();
                }

                currentPartitionKey = itemPartitionKey;
                currentPartitionId = itemPartitionId;

                batch ??= await CreateBatchForOptionsAsync(item.SendOptions, cancellationToken);

                if (!batch.TryAdd(item.EventData))
                {
                    // Current batch is full — send it and start a new one.
                    if (batch.Count > 0)
                    {
                        await SendBatchAsync(batch, _pending, cancellationToken);
                        batch = null;
                        _pending.Clear();
                    }

                    batch = await CreateBatchForOptionsAsync(item.SendOptions, cancellationToken);

                    if (!batch.TryAdd(item.EventData))
                    {
                        // Single event exceeds max batch size — fail it.
                        item.Completion.TrySetException(
                            new InvalidOperationException(
                                $"Event data exceeds the maximum batch size of {batch.MaximumSizeInBytes} bytes."));
                        batch.Dispose();
                        batch = null;
                        continue;
                    }
                }

                _pending.Add(item);
            }

            // Flush remaining batch.
            if (batch is { Count: > 0 })
            {
                await SendBatchAsync(batch, _pending, cancellationToken);
                batch = null;
            }
        }
        catch (Exception ex)
        {
            // Fail all pending events.
            foreach (var p in _pending)
            {
                p.Completion.TrySetException(ex);
            }

            batch?.Dispose();
        }
    }

    private ValueTask<EventDataBatch> CreateBatchForOptionsAsync(
        SendEventOptions? sendOptions,
        CancellationToken cancellationToken)
    {
        if (sendOptions?.PartitionId is not null)
        {
            return _producer.CreateBatchAsync(
                new CreateBatchOptions { PartitionId = sendOptions.PartitionId },
                cancellationToken);
        }

        if (sendOptions?.PartitionKey is not null)
        {
            return _producer.CreateBatchAsync(
                new CreateBatchOptions { PartitionKey = sendOptions.PartitionKey },
                cancellationToken);
        }

        return _producer.CreateBatchAsync(cancellationToken);
    }

    private async Task SendBatchAsync(
        EventDataBatch batch,
        List<PendingEvent> pending,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.SendingBatch(batch.Count, batch.SizeInBytes);
            await _producer.SendAsync(batch, cancellationToken);

            foreach (var p in pending)
            {
                p.Completion.TrySetResult();
            }
        }
        catch (Exception ex)
        {
            foreach (var p in pending)
            {
                p.Completion.TrySetException(ex);
            }
        }
        finally
        {
            batch.Dispose();
        }
    }

    private sealed record PendingEvent(EventData EventData, SendEventOptions? SendOptions)
    {
        public TaskCompletionSource Completion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}

internal static partial class EventHubBatchDispatcherLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Sending batch of {Count} events ({SizeInBytes} bytes)")]
    public static partial void SendingBatch(this ILogger logger, int count, long sizeInBytes);

    [LoggerMessage(LogLevel.Error, "Error in batch dispatch loop")]
    public static partial void BatchDispatchError(this ILogger logger, Exception exception);
}
