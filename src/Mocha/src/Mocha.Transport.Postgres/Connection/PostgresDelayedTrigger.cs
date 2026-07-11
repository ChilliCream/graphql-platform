using Mocha.Threading;

namespace Mocha.Transport.Postgres;

/// <summary>
/// A trigger that schedules a wake-up signal at a specific time, used to avoid busy-waiting
/// on scheduled messages. When the scheduled time arrives, the trigger sets the
/// <see cref="AsyncAutoResetEvent"/> to wake up the receive endpoint's polling loop.
/// </summary>
internal sealed class PostgresDelayedTrigger : IAsyncDisposable
{
    private readonly TimeSpan _delay;
    private readonly CancellationTokenSource _cts;
    private readonly AsyncAutoResetEvent _signal;
    private readonly Task _delayTask;
    private bool _isSet;

    public PostgresDelayedTrigger(DateTimeOffset scheduledAt, AsyncAutoResetEvent receiveMessageSignal)
    {
        ScheduledAt = scheduledAt;
        _delay = scheduledAt - DateTimeOffset.UtcNow;
        _signal = receiveMessageSignal;
        _cts = new CancellationTokenSource();
        _delayTask = DelayedTriggerAsync(_cts.Token);
    }

    /// <summary>
    /// Gets the time at which this trigger is scheduled to fire.
    /// </summary>
    public DateTimeOffset ScheduledAt { get; }

    /// <summary>
    /// Gets whether this trigger has already fired.
    /// </summary>
    public bool IsSet => _isSet;

    private async Task DelayedTriggerAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_delay > TimeSpan.Zero)
            {
                await Task.Delay(_delay, cancellationToken);
            }

            _isSet = true;
            _signal.Set();
        }
        catch (OperationCanceledException)
        {
            // Expected when disposed before scheduled time
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        await _delayTask;
        _cts.Dispose();
    }
}
