namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// The capped exponential backoff for the mail-wake daemon coordinator's
/// SQLite busy/locked retries and per-actor transient-offer cooldown.
/// Starts at <see cref="InitialDelay"/> and doubles per consecutive failure,
/// never exceeding <see cref="MaxDelay"/>.
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
    /// The delay before the <paramref name="consecutiveFailures"/>-th retry
    /// (1-based; values below 1 are treated as 1, so the very first retry
    /// always waits <see cref="InitialDelay"/>).
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
    /// Whether <paramref name="lastError"/> is a busy session gate, exhausted
    /// transport capacity, or an accepted Claude access-denied handoff.
    /// </summary>
    public static bool IsTransientOffer(string? lastError) =>
        lastError is "busy" or "capacity-dropped" or "access-denied";
}
