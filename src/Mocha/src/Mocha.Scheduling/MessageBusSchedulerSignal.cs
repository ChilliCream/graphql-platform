namespace Mocha.Scheduling;

internal sealed class MessageBusSchedulerSignal(TimeProvider timeProvider)
    : IDisposable
    , ISchedulerSignal
{
    private static readonly TimeSpan s_maxDelay = TimeSpan.FromMinutes(5);

    private readonly object _lock = new();

    private DateTimeOffset _target = DateTimeOffset.MaxValue;
    private CancellationTokenSource? _delayCts;

    /// <inheritdoc />
    public void Notify(DateTimeOffset scheduledTime)
    {
        lock (_lock)
        {
            if (scheduledTime >= _target)
            {
                return;
            }

            _target = scheduledTime;
            _delayCts?.Cancel();
        }
    }

    /// <inheritdoc />
    public async Task WaitUntilAsync(DateTimeOffset wakeTime, CancellationToken cancellationToken)
    {
        CancellationTokenSource delayCts;

        lock (_lock)
        {
            _target = wakeTime;
            _delayCts?.Dispose();
            _delayCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            delayCts = _delayCts;
        }

        var delay = wakeTime - timeProvider.GetUtcNow();

        if (delay <= TimeSpan.Zero)
        {
            return;
        }

        if (delay > s_maxDelay)
        {
            delay = s_maxDelay;
        }

        try
        {
            await Task.Delay(delay, timeProvider, delayCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Woken by Notify - return to dispatcher, which will re-query and re-sleep.
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _delayCts?.Cancel();
        _delayCts?.Dispose();
        _delayCts = null;
    }
}
