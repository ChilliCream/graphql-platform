namespace Mocha;

/// <summary>
/// Per-exception retry configuration.
/// </summary>
public sealed class RetryPolicyConfig
{
    /// <summary>
    /// Gets whether retry is enabled for this exception. Defaults to <c>true</c>.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Gets the number of retry attempts.
    /// </summary>
    public int? Attempts { get; init; }

    /// <summary>
    /// Gets the base delay between retries.
    /// </summary>
    public TimeSpan? Delay { get; init; }

    /// <summary>
    /// Gets the backoff strategy.
    /// </summary>
    public RetryBackoffType? Backoff { get; init; }

    /// <summary>
    /// Gets the maximum delay cap.
    /// </summary>
    public TimeSpan? MaxDelay { get; init; }

    /// <summary>
    /// Gets whether jitter is enabled.
    /// </summary>
    public bool? UseJitter { get; init; }

    /// <summary>
    /// Gets the explicit retry intervals.
    /// </summary>
    public TimeSpan[]? Intervals { get; init; }
}
