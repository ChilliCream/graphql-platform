namespace Mocha;

/// <summary>
/// Fluent builder for configuring per-exception retry/redelivery behavior.
/// </summary>
/// <typeparam name="TException">The exception type to configure behavior for.</typeparam>
public interface IExceptionPolicyBuilder<TException> where TException : Exception
{
    /// <summary>
    /// Discards the message when this exception occurs. The exception is swallowed.
    /// </summary>
    void Discard();

    /// <summary>
    /// Routes the message to the error endpoint when this exception occurs.
    /// Retry and redelivery are skipped.
    /// </summary>
    void DeadLetter();

    /// <summary>
    /// Retries the handler invocation with default settings.
    /// Redelivery is disabled unless chained with <see cref="IAfterRetryBuilder.ThenRedeliver()"/>.
    /// </summary>
    /// <returns>A builder for chaining additional actions after retry.</returns>
    IAfterRetryBuilder Retry();

    /// <summary>
    /// Retries the handler invocation with the specified number of attempts.
    /// Redelivery is disabled unless chained with <see cref="IAfterRetryBuilder.ThenRedeliver()"/>.
    /// </summary>
    /// <param name="attempts">The number of retry attempts.</param>
    /// <returns>A builder for chaining additional actions after retry.</returns>
    IAfterRetryBuilder Retry(int attempts);

    /// <summary>
    /// Retries the handler invocation with full configuration.
    /// Redelivery is disabled unless chained with <see cref="IAfterRetryBuilder.ThenRedeliver()"/>.
    /// </summary>
    /// <param name="attempts">The number of retry attempts.</param>
    /// <param name="delay">The base delay between retries.</param>
    /// <param name="backoff">The backoff strategy.</param>
    /// <returns>A builder for chaining additional actions after retry.</returns>
    IAfterRetryBuilder Retry(int attempts, TimeSpan delay, RetryBackoffType backoff = RetryBackoffType.Exponential);

    /// <summary>
    /// Retries the handler invocation with explicit intervals.
    /// Redelivery is disabled unless chained with <see cref="IAfterRetryBuilder.ThenRedeliver()"/>.
    /// </summary>
    /// <param name="intervals">The explicit intervals between retries.</param>
    /// <returns>A builder for chaining additional actions after retry.</returns>
    IAfterRetryBuilder Retry(TimeSpan[] intervals);

    /// <summary>
    /// Redelivers the message with default settings, skipping retry.
    /// </summary>
    /// <returns>A builder for chaining additional actions after redelivery.</returns>
    IAfterRedeliveryBuilder Redeliver();

    /// <summary>
    /// Redelivers the message with the specified attempts and base delay, skipping retry.
    /// </summary>
    /// <param name="attempts">The number of redelivery attempts.</param>
    /// <param name="baseDelay">The base delay for redelivery.</param>
    /// <returns>A builder for chaining additional actions after redelivery.</returns>
    IAfterRedeliveryBuilder Redeliver(int attempts, TimeSpan baseDelay);

    /// <summary>
    /// Redelivers the message with explicit intervals, skipping retry.
    /// </summary>
    /// <param name="intervals">The explicit intervals between redeliveries.</param>
    /// <returns>A builder for chaining additional actions after redelivery.</returns>
    IAfterRedeliveryBuilder Redeliver(TimeSpan[] intervals);
}
