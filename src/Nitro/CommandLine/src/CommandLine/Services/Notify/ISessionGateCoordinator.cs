namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// Reserves an agent's ping gate and one shared transport slot for a ping attempt.
/// Completion releases the slot and either starts a cooldown on success or releases the gate on failure.
/// </summary>
internal interface ISessionGateCoordinator
{
    Task<WakeReservationResult> TryReserveAsync(
        string target,
        string attemptId,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    /// <summary>
    /// Releases the attempt's transport slot and either renews its unexpired ping gate
    /// for a successful attempt's cooldown or releases it after failure.
    /// Reservations now owned by a different attempt are unchanged.
    /// </summary>
    Task CompleteAsync(
        WakeGateReservation reservation,
        bool success,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}
