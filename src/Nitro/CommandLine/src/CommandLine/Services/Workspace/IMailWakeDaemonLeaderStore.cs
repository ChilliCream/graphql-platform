namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Manages the single shared mail-wake leadership lease for this workspace.
/// </summary>
internal interface IMailWakeDaemonLeaderStore
{
    /// <summary>
    /// Claims the lease when no row exists yet or the existing lease has expired,
    /// stamping <paramref name="token"/> as the new holder. Returns false, claiming
    /// nothing, while any unexpired lease exists, including one held by
    /// <paramref name="token"/> itself.
    /// </summary>
    Task<bool> TryAcquireAsync(
        string token,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken);

    /// <summary>
    /// Extends the lease's expiry for its current holder. Returns false, changing
    /// nothing, when <paramref name="token"/> does not hold an unexpired lease.
    /// </summary>
    Task<bool> TryRenewAsync(
        string token,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken);

    /// <summary>
    /// Expires the lease immediately for its current holder. Returns false, changing
    /// nothing, when <paramref name="token"/> does not hold the lease.
    /// </summary>
    Task<bool> TryReleaseAsync(string token, DateTimeOffset now, CancellationToken cancellationToken);
}
