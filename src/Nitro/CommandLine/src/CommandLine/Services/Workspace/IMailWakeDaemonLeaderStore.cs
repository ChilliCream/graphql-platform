namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Leader election over the one persistent <c>mail_wake_daemons</c> row per
/// Nitro instance. A cross-process claim, not in-process state: the
/// dashboard mail-wake daemon is its own process, so an in-process lock
/// would elect nothing.
/// </summary>
internal interface IMailWakeDaemonLeaderStore
{
    /// <summary>
    /// Atomically claims leadership for <paramref name="nitroInstanceId"/>,
    /// creating its row on first use. Fails, returning null, when a current
    /// lease is already held by someone else (checked by
    /// <c>expires_at &gt; now</c>, not by owner identity, so a would-be
    /// leader waiting out a live lease from its own prior attempt also
    /// fails, not just a different owner). Succeeding increments
    /// <c>mail_wake_daemons.epoch</c> from whatever it was before
    /// (1 on first creation), so every successive owner, including the same
    /// one reclaiming after its own expiry, gets a fresh epoch a stale write
    /// under the old one can no longer fence past.
    /// </summary>
    Task<long?> TryAcquireAsync(
        string nitroInstanceId,
        string ownerId,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken);

    /// <summary>
    /// Extends the current leader's lease and optionally records
    /// <paramref name="lastError"/>. Returns false, changing nothing, when
    /// <paramref name="ownerId"/> or <paramref name="epoch"/> no longer
    /// matches the live row, or the lease has already expired (a lost
    /// leadership can never be renewed back; the caller must re-acquire and
    /// treat that as a fresh epoch).
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
    /// Voluntarily releases leadership by expiring the lease immediately, so
    /// the next <see cref="TryAcquireAsync"/> call from any owner succeeds
    /// without waiting out the remaining lease duration. A no-op, returning
    /// false, when <paramref name="ownerId"/> or <paramref name="epoch"/> no
    /// longer matches the live row.
    /// </summary>
    Task<bool> TryReleaseAsync(
        string nitroInstanceId,
        string ownerId,
        long epoch,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}
