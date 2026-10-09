namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// The result of <see cref="ISessionGateCoordinator.TryReserveAsync"/>:
/// exactly one of <see cref="Reservation"/> or <see cref="Failure"/> is set.
/// </summary>
internal sealed record WakeReservationResult(WakeGateReservation? Reservation, WakeReservationFailure? Failure)
{
    public static WakeReservationResult Reserved(WakeGateReservation reservation) => new(reservation, null);

    public static WakeReservationResult Rejected(WakeReservationFailure failure) => new(null, failure);
}
