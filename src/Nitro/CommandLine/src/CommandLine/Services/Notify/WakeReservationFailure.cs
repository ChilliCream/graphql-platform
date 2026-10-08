namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// Why <see cref="ISessionGateCoordinator.TryReserveAsync"/> could not
/// reserve a target.
/// </summary>
internal enum WakeReservationFailure
{
    /// <summary>
    /// The session gate has an unexpired reservation or cooldown.
    /// </summary>
    GateBusy,

    /// <summary>
    /// All shared transport slots are occupied by unexpired reservations.
    /// </summary>
    CapacityDropped
}
