namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// Owns one Nitro instance's persistent mail-wake daemon leadership and its
/// admission/execution loops. Registered as an inert singleton: nothing
/// happens until a caller (the dashboard launcher) invokes
/// <see cref="StartAsync"/>. See <see cref="MailWakeDaemonCoordinator"/> for
/// the full leader-election, admission, and degradation contract.
/// </summary>
internal interface IMailWakeDaemonCoordinator : IAsyncDisposable
{
    /// <summary>
    /// This instance's current, in-memory leadership snapshot. Never blocks
    /// or reads the database; reflects only what this instance itself last
    /// observed or wrote.
    /// </summary>
    MailWakeDaemonStatus Status { get; }

    /// <summary>
    /// Starts the background run loop for <paramref name="nitroInstanceId"/>:
    /// standby lease observation, leader election, heartbeat renewal,
    /// admission, and execution, until <see cref="StopAsync"/> is called or
    /// <paramref name="cancellationToken"/> fires. Returns once the loop has
    /// been launched, not once leadership is acquired. Throws
    /// <see cref="InvalidOperationException"/> if already started.
    /// </summary>
    Task StartAsync(string nitroInstanceId, CancellationToken cancellationToken);

    /// <summary>
    /// Signals the run loop to stop, releasing leadership immediately if
    /// currently held, and waits up to the coordinator's own shutdown budget
    /// for it to wind down. A no-op if never started or already stopped.
    /// Never throws for a noncooperative in-flight task that outlives the
    /// budget; it is simply left to lose its lease/claims to expiry.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken);
}
