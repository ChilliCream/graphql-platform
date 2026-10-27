namespace Mocha.Scheduling;

internal sealed class MessageBusSchedulerSignal(TimeProvider timeProvider)
    : IDisposable
    , ISchedulerSignal
{
    private static readonly TimeSpan s_maxDelay = TimeSpan.FromMinutes(5);

    private readonly object _lock = new();

    private DateTimeOffset _target = DateTimeOffset.MaxValue;
    private CancellationTokenSource? _delayCts;
    private bool _notified;
    private bool _isWaiting;

    /// <inheritdoc />
    public void Notify(DateTimeOffset scheduledTime)
    {
        lock (_lock)
        {
            // An active wait wakes no later than its target, so a notify for an equal or later time
            // is already covered. When no wait is active the target is stale, so any notify must be
            // recorded to force the next wait to re-evaluate rather than sleep.
            if (_isWaiting && scheduledTime >= _target)
            {
                return;
            }

            _notified = true;
            _delayCts?.Cancel();
        }
    }

    /// <inheritdoc />
    public async Task WaitUntilAsync(DateTimeOffset wakeTime, CancellationToken cancellationToken)
    {
        CancellationTokenSource delayCts;

        lock (_lock)
        {
            if (_notified)
            {
                // A notify arrived before this wait began. Consume it and return at once so the
                // caller re-evaluates its next due time instead of sleeping.
                _notified = false;

                return;
            }

            _target = wakeTime;
            _isWaiting = true;
            _delayCts?.Dispose();
            _delayCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            delayCts = _delayCts;
        }

        try
        {
            var delay = wakeTime - timeProvider.GetUtcNow();

            if (delay > TimeSpan.Zero)
            {
                if (delay > s_maxDelay)
                {
                    delay = s_maxDelay;
                }

                await Task.Delay(delay, timeProvider, delayCts.Token);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Woken by Notify - return to dispatcher, which will re-query and re-sleep.
        }
        finally
        {
            lock (_lock)
            {
                _isWaiting = false;
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_lock)
        {
            _delayCts?.Cancel();
            _delayCts?.Dispose();
            _delayCts = null;
        }
    }
}
