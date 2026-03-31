namespace Mocha;

/// <summary>
/// Specifies the backoff strategy for delay calculations between retry attempts.
/// </summary>
public enum RetryBackoffType
{
    /// <summary>
    /// Constant delay between retries. Every attempt waits the same <see cref="RetryOptions.Delay"/>.
    /// </summary>
    Constant,

    /// <summary>
    /// Linearly increasing delay. Delay = <see cref="RetryOptions.Delay"/> * (attempt + 1).
    /// </summary>
    Linear,

    /// <summary>
    /// Exponentially increasing delay. Delay = <see cref="RetryOptions.Delay"/> * 2^attempt.
    /// </summary>
    Exponential
}
