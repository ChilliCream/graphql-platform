using Microsoft.Extensions.Logging;
using Mocha.Middlewares;

namespace Mocha.Transport.InMemory;

internal sealed class InMemoryScheduler : IAsyncDisposable
{
#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif
    private readonly PriorityQueue<ScheduledEntry, DateTimeOffset> _queue = new();
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<InMemoryScheduler> _logger;
    private readonly CancellationTokenSource _shutdownCts = new();
    private CancellationTokenSource? _delayCts;
    private DateTimeOffset _nextWakeTime = DateTimeOffset.MaxValue;
    private Task? _loopTask;
    private bool _disposed;

    public InMemoryScheduler(TimeProvider timeProvider, ILogger<InMemoryScheduler> logger)
    {
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public void Start()
    {
        _loopTask = Task.Run(() => ProcessLoopAsync(_shutdownCts.Token));
    }

    public void Schedule(MessageEnvelope envelope, IInMemoryResource resource, DateTimeOffset scheduledTime)
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            var bodyCopy = envelope.Body.ToArray();
            var envelopeCopy = new MessageEnvelope(envelope) { Body = bodyCopy };

            _queue.Enqueue(new ScheduledEntry(envelopeCopy, resource), scheduledTime);

            _logger.MessageScheduled(envelopeCopy.MessageId, scheduledTime, resource.ToString());

            if (scheduledTime < _nextWakeTime)
            {
                _delayCts?.Cancel();
            }
        }
    }

    private async Task ProcessLoopAsync(CancellationToken shutdownToken)
    {
        _logger.SchedulerStarted();

        try
        {
            while (!shutdownToken.IsCancellationRequested)
            {
                // Phase 1: drain all due messages
                while (true)
                {
                    ScheduledEntry entry;

                    lock (_lock)
                    {
                        if (!_queue.TryPeek(out _, out var nextTime) || nextTime > _timeProvider.GetUtcNow())
                        {
                            break;
                        }

                        entry = _queue.Dequeue();
                    }

                    try
                    {
                        await entry.Resource.SendAsync(entry.Envelope, shutdownToken);
                        _logger.MessageDispatched(entry.Envelope.MessageId);
                    }
                    catch (OperationCanceledException) when (shutdownToken.IsCancellationRequested)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        _logger.DispatchFailed(ex, entry.Envelope.MessageId, entry.Resource.ToString());
                    }
                }

                // Phase 2: compute delay and sleep
                TimeSpan delay;

                lock (_lock)
                {
                    if (_queue.TryPeek(out _, out var nextTime))
                    {
                        _nextWakeTime = nextTime;
                        delay = nextTime - _timeProvider.GetUtcNow();

                        if (delay <= TimeSpan.Zero)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        _nextWakeTime = DateTimeOffset.MaxValue;
                        delay = Timeout.InfiniteTimeSpan;
                    }
                }

                var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(shutdownToken);

                lock (_lock)
                {
                    _delayCts = linkedCts;
                }

                try
                {
                    await Task.Delay(delay, _timeProvider, linkedCts.Token);
                }
                catch (OperationCanceledException) when (!shutdownToken.IsCancellationRequested)
                {
                    // Woken by Schedule() — loop again
                }
                finally
                {
                    lock (_lock)
                    {
                        _delayCts = null;
                    }

                    linkedCts.Dispose();
                }
            }
        }
        finally
        {
            int pendingCount;

            lock (_lock)
            {
                pendingCount = _queue.Count;
            }

            _logger.SchedulerStopped(pendingCount);
        }
    }

    public async ValueTask DisposeAsync()
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }

        await _shutdownCts.CancelAsync();

        lock (_lock)
        {
            _delayCts?.Cancel();
        }

        if (_loopTask is not null)
        {
            await _loopTask;
        }

        _shutdownCts.Dispose();

        lock (_lock)
        {
            _queue.Clear();
        }
    }

    private readonly record struct ScheduledEntry(MessageEnvelope Envelope, IInMemoryResource Resource);
}

internal static partial class InMemorySchedulerLogs
{
    [LoggerMessage(LogLevel.Debug, "InMemory scheduler started.")]
    public static partial void SchedulerStarted(this ILogger logger);

    [LoggerMessage(LogLevel.Debug, "InMemory scheduler stopped. Pending messages: {PendingCount}.")]
    public static partial void SchedulerStopped(this ILogger logger, int pendingCount);

    [LoggerMessage(LogLevel.Debug, "Message {MessageId} scheduled for {ScheduledTime} on {ResourceName}.")]
    public static partial void MessageScheduled(
        this ILogger logger,
        string? messageId,
        DateTimeOffset scheduledTime,
        string? resourceName);

    [LoggerMessage(LogLevel.Debug, "Scheduled message {MessageId} dispatched.")]
    public static partial void MessageDispatched(this ILogger logger, string? messageId);

    [LoggerMessage(LogLevel.Error, "Failed to dispatch scheduled message {MessageId} to {ResourceName}.")]
    public static partial void DispatchFailed(
        this ILogger logger,
        Exception exception,
        string? messageId,
        string? resourceName);
}
