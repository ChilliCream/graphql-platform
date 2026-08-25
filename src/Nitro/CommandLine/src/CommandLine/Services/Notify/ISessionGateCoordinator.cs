using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// Why <see cref="ISessionGateCoordinator.TryReserveAsync"/> could not
/// reserve a target.
/// </summary>
internal enum WakeReservationFailure
{
    /// <summary>
    /// The target's <c>session_ping_gates</c> row is held by another
    /// unexpired attempt: either genuinely busy, or still within the
    /// cooldown a prior success on this exact generation extended it to.
    /// </summary>
    GateBusy,

    /// <summary>
    /// The gate was acquired, but every one of the four shared
    /// <c>ping_leases</c> slots was already held by an unexpired attempt.
    /// </summary>
    CapacityDropped
}

/// <summary>
/// One target's held reservation: the session gate and the shared lease
/// slot a dispatch attempt claimed together, and the attempt id that fences
/// both.
/// </summary>
internal sealed record WakeGateReservation(AgentSessionGeneration Target, string AttemptId, int Slot);

/// <summary>
/// The result of <see cref="ISessionGateCoordinator.TryReserveAsync"/>:
/// exactly one of <see cref="Reservation"/> or <see cref="Failure"/> is set.
/// </summary>
internal sealed record WakeReservationResult(WakeGateReservation? Reservation, WakeReservationFailure? Failure)
{
    public static WakeReservationResult Reserved(WakeGateReservation reservation) => new(reservation, null);

    public static WakeReservationResult Rejected(WakeReservationFailure failure) => new(null, failure);
}

/// <summary>
/// Reserves and releases the two things one endpoint transport attempt must
/// hold together: the target's <c>session_ping_gates</c> mutual-exclusion
/// gate (see <see cref="ISessionPingGateStore"/>) and one of the four shared
/// <c>ping_leases</c> slots (see <see cref="IPingLeaseStore"/>). A success
/// extends the gate into a cooldown window so a subsequent reservation
/// attempt against the same generation finds it busy; a failure releases the
/// gate immediately, so the next attempt can retry without waiting out a
/// cooldown it never earned.
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
    /// (<paramref name="success"/> false, so a failed attempt can be retried
    /// right away). Safe to call even when the reservation has already been
    /// lost to expiry: both underlying releases are no-ops in that case.
    /// </summary>
    Task CompleteAsync(
        WakeGateReservation reservation,
        bool success,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}
