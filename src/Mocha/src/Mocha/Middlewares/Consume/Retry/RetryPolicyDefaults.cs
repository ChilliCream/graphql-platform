namespace Mocha;

/// <summary>
/// Provides the built-in default values for retry policy configuration.
/// </summary>
internal static class RetryPolicyDefaults
{
    /// <summary>
    /// The default number of retry attempts. Value: 3.
    /// </summary>
    public const int Attempts = 3;

    /// <summary>
    /// The default base delay between retries. Value: 200ms.
    /// </summary>
    public static readonly TimeSpan Delay = TimeSpan.FromMilliseconds(200);

    /// <summary>
    /// The default backoff strategy. Value: <see cref="RetryBackoffType.Exponential"/>.
    /// </summary>
    public const RetryBackoffType Backoff = RetryBackoffType.Exponential;

    /// <summary>
    /// The default jitter setting. Value: <c>true</c>.
    /// </summary>
    public const bool UseJitter = true;

    /// <summary>
    /// The default maximum delay cap. Value: 30 seconds.
    /// </summary>
    public static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds(30);
}
