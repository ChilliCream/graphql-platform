using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Services.Notify;

internal sealed class SessionGateCoordinator(
    ISessionPingGateStore gateStore, IPingLeaseStore leaseStore) : ISessionGateCoordinator
{
    public async Task<WakeReservationResult> TryReserveAsync(
        AgentSessionGeneration target,
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
            // The gate was reserved for nothing: release it immediately so
            // this rejected attempt never costs the target an undeserved
            // cooldown.
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
            // Extends the gate rather than releasing it: a successful
            // attempt starts this generation's cooldown, so an immediately
            // following wake for the same actor finds it busy instead of
            // re-pinging a session that was just reached.
            await gateStore.TryRenewAsync(
                reservation.Target, reservation.AttemptId, now, PingPolicy.Cooldown, cancellationToken);
        }
        else
        {
            await gateStore.ReleaseAsync(reservation.Target, reservation.AttemptId, cancellationToken);
        }
    }
}
