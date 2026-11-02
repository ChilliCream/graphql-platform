namespace Mocha.Outbox;

/// <summary>
/// Represents a signal that can be used to notify a waiting thread that an event has occurred.
/// </summary>
public interface IOutboxSignal
{
    /// <summary>
    /// Sets the signal.
    /// </summary>
    void Set();

    /// <summary>
    /// Waits for the signal to be set.
    /// </summary>
    Task WaitAsync(CancellationToken cancellationToken);
}
