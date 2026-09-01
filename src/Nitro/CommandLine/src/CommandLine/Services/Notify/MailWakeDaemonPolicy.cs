namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// The timing and concurrency constants for one
/// <see cref="MailWakeDaemonCoordinator"/> instance. <see cref="LeaderLeaseDuration"/>
/// must exceed <see cref="HeartbeatInterval"/>.
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
    /// A 15-second leader lease renewed every 5 seconds, 200 ms admission
    /// polling while leader, a 1-second standby poll, up to 4 concurrent
    /// actor executions, and a 5-second shutdown wait.
    /// </summary>
    public static readonly MailWakeDaemonPolicy Default = new(
        LeaderLeaseDuration: TimeSpan.FromSeconds(15),
        HeartbeatInterval: TimeSpan.FromSeconds(5),
        AdmissionPollInterval: TimeSpan.FromMilliseconds(200),
        StandbyPollInterval: TimeSpan.FromSeconds(1),
        MaxConcurrentActorExecutions: 4,
        ShutdownWait: TimeSpan.FromSeconds(5));
}
