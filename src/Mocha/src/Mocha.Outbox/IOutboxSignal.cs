namespace Mocha.Outbox;

/// <summary>
/// Notifies outbox processors that messages may be available for processing.
/// </summary>
public interface IOutboxSignal
{
    /// <summary>
    /// Sets the signal and releases all current waiters.
    /// </summary>
    void Set();

    /// <summary>
    /// Waits for the signal to be set.
    /// </summary>
    Task WaitAsync(CancellationToken cancellationToken);
}
