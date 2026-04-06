namespace Mocha;

/// <summary>
/// Specifies the backoff strategy for delay calculations between retry attempts.
/// </summary>
public enum RetryBackoffType
{
    /// <summary>
    /// Constant delay between retries. Every attempt waits the same base delay.
    /// </summary>
    Constant,

    /// <summary>
    /// Linearly increasing delay. Delay = baseDelay * attempt.
    /// </summary>
    Linear,

    /// <summary>
    /// Exponentially increasing delay. Delay = baseDelay * 2^(attempt-1).
    /// </summary>
    Exponential
}
