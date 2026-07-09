namespace Mocha;

/// <summary>
/// Builder for chaining actions after retry configuration.
/// If nothing is chained, retry-only behavior applies and redelivery is skipped.
/// </summary>
public interface IAfterRetryBuilder
{
    /// <summary>
    /// Chains redelivery after retry exhaustion with default settings.
    /// </summary>
    /// <returns>A builder for chaining additional actions after redelivery.</returns>
    IAfterRedeliveryBuilder ThenRedeliver();

    /// <summary>
    /// Chains redelivery after retry exhaustion with the specified attempts and base delay.
    /// </summary>
    /// <param name="attempts">The number of redelivery attempts.</param>
    /// <param name="baseDelay">The base delay for redelivery.</param>
    /// <returns>A builder for chaining additional actions after redelivery.</returns>
    IAfterRedeliveryBuilder ThenRedeliver(int attempts, TimeSpan baseDelay);

    /// <summary>
    /// Chains redelivery after retry exhaustion with explicit intervals.
    /// </summary>
    /// <param name="intervals">The explicit intervals between redeliveries.</param>
    /// <returns>A builder for chaining additional actions after redelivery.</returns>
    IAfterRedeliveryBuilder ThenRedeliver(TimeSpan[] intervals);

    /// <summary>
    /// Routes the message to the error endpoint after retry exhaustion.
    /// </summary>
    void ThenDeadLetter();
}
