using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// Reserves and releases the two things one endpoint transport attempt must
/// hold together: the target's <c>session_ping_gates</c> mutual-exclusion
/// gate (see <see cref="ISessionPingGateStore"/>) and one of the four shared
/// <c>ping_leases</c> slots (see <see cref="IPingLeaseStore"/>). A success
/// extends the gate into a cooldown window; a failure releases the gate
/// immediately.
/// </summary>
internal interface ISessionGateCoordinator
{
    Task<WakeReservationResult> TryReserveAsync(
        AgentSessionGeneration target,
        string attemptId,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    /// <summary>
    /// Releases the lease slot unconditionally, and the session gate either
    /// into a cooldown (<paramref name="success"/> true) or immediately
    /// (<paramref name="success"/> false). Safe to call even when the
    /// reservation has already been lost to expiry: both underlying releases
    /// are no-ops in that case.
    /// </summary>
    Task CompleteAsync(
        WakeGateReservation reservation,
        bool success,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}
