namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Manages four shared lease slots for concurrent ping attempts in a workspace.
/// </summary>
internal interface IPingLeaseStore
{
    /// <summary>
    /// Atomically claims the lowest available slot after removing expired leases.
    /// Returns null when all four slots have unexpired leases.
    /// </summary>
    Task<int?> TryAcquireAsync(
        string attemptId, DateTimeOffset now, TimeSpan leaseDuration, CancellationToken cancellationToken);

    /// <summary>
    /// Releases the slot only when it still belongs to <paramref name="attemptId"/>;
    /// a missing or reassigned slot is unchanged.
    /// </summary>
    Task ReleaseAsync(int slot, string attemptId, CancellationToken cancellationToken);
}
