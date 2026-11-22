namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// A signal that wakes every waiter each time a state changes.
/// </summary>
internal sealed class AsyncCondition
{
    private TaskCompletionSource _nextPulse = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Wakes every waiter, so that each of them checks its condition again.
    /// </summary>
    public void PulseAll()
        => Interlocked
            .Exchange(ref _nextPulse, new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously))
            .TrySetResult();

    /// <summary>
    /// Waits until <paramref name="condition"/> is <c>true</c>. The condition is checked now and after each
    /// pulse.
    /// </summary>
    public async ValueTask WaitUntilAsync(Func<bool> condition, CancellationToken cancellationToken)
    {
        while (true)
        {
            // Read before the condition, so a pulse after the check completes this task.
            var next = Volatile.Read(ref _nextPulse).Task;
            if (condition())
            {
                return;
            }

            await next.WaitAsync(cancellationToken);
        }
    }
}
