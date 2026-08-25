namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// The timing and concurrency constants one <see cref="MailWakeDaemonCoordinator"/>
/// instance fixes for its own leader lease, heartbeat, and polling loops.
/// Injected rather than a static class (unlike <see cref="WakeDispatchPolicy"/>
/// and <see cref="PingPolicy"/>) so tests can substitute a fast policy and
/// observe leader election and takeover against real wall-clock time instead
/// of racing a <see cref="TimeProvider"/> fake against background loops.
/// <see cref="LeaderLeaseDuration"/> is strictly longer than
/// <see cref="HeartbeatInterval"/>, so a live leader always renews with
/// margin before its own lease could expire.
/// </summary>
internal sealed record MailWakeDaemonPolicy(
    TimeSpan LeaderLeaseDuration,
    TimeSpan HeartbeatInterval,
    TimeSpan AdmissionPollInterval,
    TimeSpan StandbyPollInterval,
    int MaxConcurrentActorExecutions,
    TimeSpan ShutdownWait)
{
    /// <summary>
    /// A 5-second leader heartbeat, 15-second leader expiry, and 100-250 ms
    /// ready-leader admission polling, matching the mail-wake daemon design's
    /// starting values. A non-leader instance observes the lease at a
    /// coarser 1-second cadence with a plain read, only attempting to
    /// acquire once it observes the row as expired, so a healthy leader
    /// never sees write pressure from an idle standby.
    /// </summary>
    public static readonly MailWakeDaemonPolicy Default = new(
        LeaderLeaseDuration: TimeSpan.FromSeconds(15),
        HeartbeatInterval: TimeSpan.FromSeconds(5),
        AdmissionPollInterval: TimeSpan.FromMilliseconds(200),
        StandbyPollInterval: TimeSpan.FromSeconds(1),
        MaxConcurrentActorExecutions: 4,
        ShutdownWait: TimeSpan.FromSeconds(5));
}
