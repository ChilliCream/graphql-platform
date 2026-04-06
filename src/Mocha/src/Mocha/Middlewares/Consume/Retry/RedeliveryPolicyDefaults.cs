namespace Mocha;

/// <summary>
/// Provides the built-in default values for redelivery policy configuration.
/// </summary>
internal static class RedeliveryPolicyDefaults
{
    /// <summary>
    /// The default redelivery intervals. Value: 5min, 15min, 30min.
    /// </summary>
    public static readonly TimeSpan[] Intervals =
    [
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(15),
        TimeSpan.FromMinutes(30)
    ];

    /// <summary>
    /// The default jitter setting. Value: <c>true</c>.
    /// </summary>
    public const bool UseJitter = true;

    /// <summary>
    /// The default maximum delay cap. Value: 1 hour.
    /// </summary>
    public static readonly TimeSpan MaxDelay = TimeSpan.FromHours(1);
}
