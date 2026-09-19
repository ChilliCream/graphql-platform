namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Manages shared mail-wake leadership leases, one per Nitro instance.
/// </summary>
internal interface IMailWakeDaemonLeaderStore
{
    /// <summary>
    /// Claims absent or expired leadership and returns its new epoch, starting at 1.
    /// Returns null while any unexpired lease exists, including one held by the same owner.
    /// </summary>
    Task<long?> TryAcquireAsync(
        string nitroInstanceId,
        string ownerId,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken);

    /// <summary>
    /// Sets the matching leader lease's expiry and last error; null clears the error.
    /// Returns false when the owner or epoch differs or the lease has expired.
    /// </summary>
    Task<bool> TryRenewAsync(
        string nitroInstanceId,
        string ownerId,
        long epoch,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        string? lastError,
        CancellationToken cancellationToken);

    /// <summary>
    /// Sets the matching owner and epoch's lease expiry to <paramref name="now"/>.
    /// Returns false when no matching leadership row exists.
    /// </summary>
    Task<bool> TryReleaseAsync(
        string nitroInstanceId,
        string ownerId,
        long epoch,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}
