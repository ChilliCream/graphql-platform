namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// The workspace-wide concurrency cap on outstanding ping children, backed
/// by the four fixed slots in <c>ping_leases</c>. A cross-process claim, not
/// in-process state: every ping attempt is its own CLI process, so an
/// in-process semaphore would cap nothing.
/// </summary>
internal interface IPingLeaseStore
{
    /// <summary>
    /// Atomically claims a free slot, stealing one whose lease has already
    /// expired if every slot is otherwise occupied. Returns the claimed slot
    /// number, or null when all four slots hold an unexpired lease (the
    /// capacity-dropped case). <paramref name="leaseDuration"/> bounds how
    /// long the caller may hold the slot before another attempt is allowed
    /// to steal it; the caller's own work must finish well inside it.
    /// </summary>
    Task<int?> TryAcquireAsync(
        string attemptId, DateTimeOffset now, TimeSpan leaseDuration, CancellationToken cancellationToken);

    /// <summary>
    /// Releases a held slot. A no-op when <paramref name="slot"/> no longer
    /// carries <paramref name="attemptId"/> (already stolen as expired, or
    /// already released), so a late or duplicate release can never free a
    /// different attempt's lease.
    /// </summary>
    Task ReleaseAsync(int slot, string attemptId, CancellationToken cancellationToken);
}
