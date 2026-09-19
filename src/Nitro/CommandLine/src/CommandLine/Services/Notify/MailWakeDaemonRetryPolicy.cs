namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// Capped exponential delays for database contention and transient wake offers.
/// </summary>
internal static class MailWakeDaemonRetryPolicy
{
    public static readonly TimeSpan InitialDelay = TimeSpan.FromMilliseconds(250);
    public static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds(60);

    /// <summary>
    /// The maximum number of times <see cref="ComputeDelay"/> doubles the delay.
    /// </summary>
    private const int MaxDoublings = 32;

    /// <summary>
    /// Returns the exponentially increasing delay for a 1-based failure count, capped
    /// at <see cref="MaxDelay"/>. Counts below 1 use <see cref="InitialDelay"/>.
    /// </summary>
    public static TimeSpan ComputeDelay(int consecutiveFailures)
    {
        var delayMs = InitialDelay.TotalMilliseconds;
        var doublings = Math.Min(Math.Max(consecutiveFailures, 1) - 1, MaxDoublings);

        for (var i = 0; i < doublings; i++)
        {
            delayMs *= 2;

            if (delayMs >= MaxDelay.TotalMilliseconds)
            {
                return MaxDelay;
            }
        }

        return TimeSpan.FromMilliseconds(delayMs);
    }

    /// <summary>
    /// True for busy, capacity-dropped, access-denied, or idle-not-armed offers;
    /// false for null or any other value.
    /// </summary>
    public static bool IsTransientOffer(string? lastError) =>
        lastError is "busy" or "capacity-dropped" or "access-denied" or "idle-not-armed";
}
