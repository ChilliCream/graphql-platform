using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Services.Notify;

internal sealed class SessionGateCoordinator(
    IAgentPingGateStore gateStore, IPingLeaseStore leaseStore) : ISessionGateCoordinator
{
    public async Task<WakeReservationResult> TryReserveAsync(
        string target,
        string attemptId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var gateAcquired = await gateStore.TryAcquireAsync(
            target, attemptId, now, WakeDispatchPolicy.SessionGateLeaseDuration, cancellationToken);

        if (!gateAcquired)
        {
            return WakeReservationResult.Rejected(WakeReservationFailure.GateBusy);
        }

        var slot = await leaseStore.TryAcquireAsync(
            attemptId, now, WakeDispatchPolicy.SessionGateLeaseDuration, cancellationToken);

        if (slot is null)
        {
            // Releases the ping gate when no transport slot is available.
            await gateStore.ReleaseAsync(target, attemptId, cancellationToken);
            return WakeReservationResult.Rejected(WakeReservationFailure.CapacityDropped);
        }

        return WakeReservationResult.Reserved(new WakeGateReservation(target, attemptId, slot.Value));
    }

    public async Task CompleteAsync(
        WakeGateReservation reservation,
        bool success,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await leaseStore.ReleaseAsync(reservation.Slot, reservation.AttemptId, cancellationToken);

        if (success)
        {
            // A successful attempt starts the agent's cooldown.
            await gateStore.TryRenewAsync(
                reservation.Target, reservation.AttemptId, now, PingPolicy.Cooldown, cancellationToken);
        }
        else
        {
            await gateStore.ReleaseAsync(reservation.Target, reservation.AttemptId, cancellationToken);
        }
    }
}
