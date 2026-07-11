namespace Mocha.Scheduling;

/// <summary>
/// Represents a signal that can be used to notify a waiting thread that a scheduled message has been persisted.
/// </summary>
public interface ISchedulerSignal
{
    /// <summary>
    /// Called by the scheduling middleware after persisting a message.
    /// The scheduler wakes only if <paramref name="scheduledTime"/> is earlier than its current wake target.
    /// </summary>
    /// <param name="scheduledTime">The time the persisted message is scheduled for delivery.</param>
    void Notify(DateTimeOffset scheduledTime);

    /// <summary>
    /// Sleeps until <paramref name="wakeTime"/> arrives, or a <see cref="Notify"/> call with a time earlier than
    /// <paramref name="wakeTime"/> is received, or the <paramref name="cancellationToken"/> is cancelled.
    /// </summary>
    /// <param name="wakeTime">The time to wake up at if no earlier notification arrives.</param>
    /// <param name="cancellationToken">A token to cancel the wait.</param>
    /// <returns>A task that completes when the signal wakes.</returns>
    Task WaitUntilAsync(DateTimeOffset wakeTime, CancellationToken cancellationToken);
}
