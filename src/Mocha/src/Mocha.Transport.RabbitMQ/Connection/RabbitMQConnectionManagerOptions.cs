namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// The timeouts and the retry delay of a <see cref="RabbitMQConnectionManager"/>.
/// </summary>
internal sealed record RabbitMQConnectionManagerOptions
{
    /// <summary>
    /// Gets how long a start keeps retrying while the broker cannot be reached.
    /// <see cref="Timeout.InfiniteTimeSpan"/> keeps retrying until the caller cancels.
    /// </summary>
    public TimeSpan StartTimeout { get; init; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Gets how long one connection attempt may take before it is abandoned and retried.
    /// </summary>
    public TimeSpan AttemptTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets the delay after the first failed connection attempt.
    /// </summary>
    public TimeSpan InitialRetryDelay { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets the longest delay between two connection attempts.
    /// </summary>
    public TimeSpan MaxRetryDelay { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets the longest wait for the broker while a connection or a channel is closed.
    /// </summary>
    public TimeSpan CloseTimeout { get; init; } = TimeSpan.FromSeconds(5);
}
