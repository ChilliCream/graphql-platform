namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// The states one <see cref="IMailWakeDaemonCoordinator"/> instance moves
/// through against the shared <c>mail_wake_daemons</c> leader row:
/// <list type="bullet">
/// <item><see cref="Standby"/>: not the current leader. Either no attempt has
/// been made yet, a live lease is held by someone else, or leadership was
/// just lost or voluntarily released.</item>
/// <item><see cref="Ready"/>: holds the current unexpired lease and runs the
/// admission and execution loops.</item>
/// <item><see cref="Degraded"/>: this instance itself hit a Claude
/// access-denial while dispatching as leader. It released leadership
/// immediately and will not attempt to reacquire it until its own denial
/// backoff elapses, so a differently privileged standby can take over.</item>
/// <item><see cref="Stopping"/>: <see cref="IMailWakeDaemonCoordinator.StopAsync"/>
/// was called; the run loop is winding down.</item>
/// </list>
/// </summary>
internal enum MailWakeDaemonState
{
    Standby,
    Ready,
    Degraded,
    Stopping
}
