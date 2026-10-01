namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// Coordinates background mail wakes for one Nitro instance.
/// No background loop runs before <see cref="StartAsync"/> is called.
/// </summary>
internal interface IMailWakeDaemonCoordinator : IAsyncDisposable
{
    /// <summary>
    /// The latest leadership snapshot observed by this coordinator.
    /// </summary>
    MailWakeDaemonStatus Status { get; }

    /// <summary>
    /// Starts the background loop until stopped or cancelled and returns without waiting
    /// for leadership. Throws <see cref="InvalidOperationException"/> if a previous loop
    /// has not been stopped and cleared.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Cancels the run loop and waits up to the shutdown budget, with leadership released
    /// after in-flight work finishes; unfinished work remains subject to lease expiry.
    /// Does nothing when no run loop is present.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken);
}
