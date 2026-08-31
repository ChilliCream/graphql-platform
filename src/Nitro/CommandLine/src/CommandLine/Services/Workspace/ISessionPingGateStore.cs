namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Per-session-generation mutual exclusion over <c>session_ping_gates</c>: at
/// most one caller may hold the gate for a given
/// <see cref="AgentSessionGeneration"/> at a time. Distinct from
/// <see cref="IPingLeaseStore"/>, which caps total outstanding ping children
/// workspace-wide regardless of target; a caller reserving a transport
/// attempt against one session acquires both.
/// </summary>
internal interface ISessionPingGateStore
{
    /// <summary>
    /// Atomically claims the gate for <paramref name="generation"/>,
    /// stealing it if the current holder's lease has already expired.
    /// Returns false, claiming nothing, when the gate is currently held by
    /// an unexpired attempt.
    /// </summary>
    Task<bool> TryAcquireAsync(
        AgentSessionGeneration generation,
        string attemptId,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken);

    /// <summary>
    /// Extends a held gate's expiry. Returns false, changing nothing, when
    /// <paramref name="attemptId"/> no longer holds the gate or its lease
    /// has already expired (a lost gate can never be renewed back; the
    /// caller must re-acquire).
    /// </summary>
    Task<bool> TryRenewAsync(
        AgentSessionGeneration generation,
        string attemptId,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken);

    /// <summary>
    /// Releases a held gate. A no-op when <paramref name="attemptId"/> no
    /// longer holds it (already stolen as expired, or already released), so
    /// a late or duplicate release can never free a different attempt's
    /// gate.
    /// </summary>
    Task ReleaseAsync(
        AgentSessionGeneration generation,
        string attemptId,
        CancellationToken cancellationToken);
}
