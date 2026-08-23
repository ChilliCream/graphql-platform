namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// The timing constants Layer C's notifier contract fixes: the per-session
/// cooldown, the lease duration a slot is held for, and the hard timeout a
/// ping attempt's own transport call is bounded by. The hard timeout is
/// strictly shorter than the lease duration, so an expired lease can never
/// be stolen while its child still runs.
/// </summary>
internal static class PingPolicy
{
    public static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(60);
    public static readonly TimeSpan LeaseDuration = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan HardTimeout = TimeSpan.FromSeconds(20);

    /// <summary>
    /// Shared with the Claude and Codex turn-boundary digests
    /// (<c>CodexHookHandler.MaxDigestMessages</c>): the digest envelope, its
    /// per-call message cap, and its byte ceiling are harness-neutral by
    /// design.
    /// </summary>
    public const int MaxDigestMessages = 10;
}
