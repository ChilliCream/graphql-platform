namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// The capped exponential backoff the mail-wake daemon coordinator applies
/// to its own operational retries: a transient SQLite busy/locked error on
/// its admission or leader-lease reads, and a per-actor cooldown after a
/// dispatch attempt leaves durable offered work behind (busy, capacity, or
/// an access-denied handoff). Starts at <see cref="InitialDelay"/> and
/// doubles per consecutive failure, never exceeding <see cref="MaxDelay"/>.
/// This governs only how eagerly the coordinator re-attempts an actor beyond
/// what <c>mail_wake_outbox.due_at</c> already schedules; it can delay a
/// retry further than that, never sooner.
/// </summary>
internal static class MailWakeDaemonRetryPolicy
{
    public static readonly TimeSpan InitialDelay = TimeSpan.FromMilliseconds(250);
    public static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds(60);

    /// <summary>
    /// A hard ceiling on how many times the delay is doubled: past this many
    /// consecutive failures the result is already pinned to
    /// <see cref="MaxDelay"/>, so further doubling would only waste cycles.
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
    /// Whether <paramref name="lastError"/> is one of the safe
    /// transient/scheduling reasons a pending target or an offered actor
    /// generation can be retried for (a busy or cooldown-held session gate,
    /// exhausted shared transport capacity, or an accepted Claude
    /// access-denied handoff). Deterministic protocol, authentication,
    /// malformed-endpoint, and generic terminal transport failures are never
    /// retried by this policy.
    /// </summary>
    public static bool IsTransientOffer(string? lastError) =>
        lastError is "busy" or "capacity-dropped" or "access-denied";
}
