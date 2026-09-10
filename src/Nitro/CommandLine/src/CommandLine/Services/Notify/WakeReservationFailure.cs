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
