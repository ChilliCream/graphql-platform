using System.Diagnostics;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// Dispatches one actor's outstanding wake work to the actor's own agent row.
/// Returns null when no batch can be claimed and leaves unresolved work pending for retry.
/// </summary>
internal sealed class ActorWakeDispatcher(
    IMailWakeBatchStore batchStore,
    IAgentStore agentStore,
    ISessionGateCoordinator gateCoordinator,
    IPingSessionExecutor executor,
    IMailStore mailStore,
    TimeProvider timeProvider) : IActorWakeDispatcher
{
    public async Task<ActorWakeReceipt?> DispatchAsync(
        string actor, DateTimeOffset deadline, CancellationToken cancellationToken)
    {
        var normalizedActor = MailAgentName.Normalize(actor);

        var now = timeProvider.GetUtcNow();
        var ownerId = $"dispatcher-{Guid.NewGuid():N}";
        var batchAttemptId = $"batch-{Guid.NewGuid():N}";

        var claim = await batchStore.TryClaimAsync(
            normalizedActor, ownerId, batchAttemptId, [normalizedActor], now,
            WakeDispatchPolicy.BatchLeaseDuration, cancellationToken);

        if (claim is null)
        {
            return null;
        }

        var unread = await mailStore.CountUnreadAsync(normalizedActor, cancellationToken);

        if (unread == 0)
        {
            return await CompleteAlreadyReadAsync(
                normalizedActor, claim, ownerId, batchAttemptId, cancellationToken);
        }

        return await DispatchTargetsAsync(
            normalizedActor, claim, ownerId, batchAttemptId, deadline, cancellationToken);
    }

    private async Task<ActorWakeReceipt> CompleteAlreadyReadAsync(
        string actor,
        MailWakeBatchClaim claim,
        string ownerId,
        string batchAttemptId,
        CancellationToken cancellationToken)
    {
        var target = claim.Targets.Single();
        var recordedAt = timeProvider.GetUtcNow();
        var recorded = await batchStore.TryRecordTargetOutcomeAsync(
            claim.BatchId, target, ownerId, batchAttemptId, MailWakeTargetStatus.Satisfied,
            offeredGeneration: null, acceptedGeneration: claim.ClaimedGeneration,
            lastError: "mail-already-read", recordedAt, cancellationToken);
        var receipt = recorded
            ? new ActorWakeTargetReceipt(
                target, MailWakeTargetStatus.Satisfied, null, claim.ClaimedGeneration, "mail-already-read")
            : new ActorWakeTargetReceipt(target, MailWakeTargetStatus.Pending, null, null, null);

        await batchStore.TryCompleteAsync(
            claim.BatchId, ownerId, batchAttemptId, timeProvider.GetUtcNow(), cancellationToken);

        return new ActorWakeReceipt(actor, receipt.Status, [receipt]);
    }

    private async Task<ActorWakeReceipt> DispatchTargetsAsync(
        string actor,
        MailWakeBatchClaim claim,
        string ownerId,
        string batchAttemptId,
        DateTimeOffset batchDeadline,
        CancellationToken cancellationToken)
    {
        using var renewalDoneSource = new CancellationTokenSource();
        using var renewalLossSource = new CancellationTokenSource();
        using var dispatchSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, renewalLossSource.Token);

        var renewalTask = RenewLoopAsync(
            claim.BatchId, ownerId, batchAttemptId, renewalLossSource, renewalDoneSource.Token, cancellationToken);

        ActorWakeTargetReceipt receipt;

        try
        {
            receipt = await DispatchTargetAsync(
                claim.BatchId,
                ownerId,
                batchAttemptId,
                claim.Targets.Single(),
                claim.ClaimedGeneration,
                batchDeadline,
                dispatchSource.Token,
                cancellationToken);
        }
        finally
        {
            // Stops and awaits lease renewal before disposing its cancellation sources.
            await renewalDoneSource.CancelAsync();

            try
            {
                await renewalTask;
            }
            catch (OperationCanceledException)
            {
                // Renewal stops when dispatch completes or the caller cancels.
            }
        }

        if (!renewalLossSource.IsCancellationRequested)
        {
            var finalNow = timeProvider.GetUtcNow();
            var hasOffered = receipt.Status == MailWakeTargetStatus.Pending;

            if (hasOffered)
            {
                await batchStore.TryReleaseAsync(
                    claim.BatchId, ownerId, batchAttemptId, finalNow,
                    finalNow + WakeDispatchPolicy.OfferedRetryDelay, "offered", cancellationToken);
            }
            else
            {
                await batchStore.TryCompleteAsync(claim.BatchId, ownerId, batchAttemptId, finalNow, cancellationToken);
            }
        }

        // A lost renewal leaves batch completion or release to a later owner.

        return new ActorWakeReceipt(actor, receipt.Status, [receipt]);
    }

    /// <summary>
    /// Renews the batch lease until dispatch stops or the caller cancels.
    /// A rejected or failed renewal cancels <paramref name="lossSource"/>.
    /// </summary>
    private async Task RenewLoopAsync(
        string batchId,
        string ownerId,
        string attemptId,
        CancellationTokenSource lossSource,
        CancellationToken stopToken,
        CancellationToken callerToken)
    {
        using var loopSource = CancellationTokenSource.CreateLinkedTokenSource(stopToken, callerToken);

        try
        {
            while (true)
            {
                await Task.Delay(WakeDispatchPolicy.BatchRenewInterval, timeProvider, loopSource.Token);

                var now = timeProvider.GetUtcNow();
                var renewed = await batchStore.TryRenewAsync(
                    batchId, ownerId, attemptId, now, WakeDispatchPolicy.BatchLeaseDuration, callerToken);

                if (!renewed)
                {
                    await lossSource.CancelAsync();
                    return;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Dispatch completion or caller cancellation ends renewal.
        }
        catch (Exception)
        {
            // An unsuccessful renewal cancels dispatch and leaves the batch for expiry.
            await lossSource.CancelAsync();
        }
    }

    private async Task<ActorWakeTargetReceipt> DispatchTargetAsync(
        string batchId,
        string ownerId,
        string batchAttemptId,
        string target,
        long claimedGeneration,
        DateTimeOffset batchDeadline,
        CancellationToken dispatchToken,
        CancellationToken callerToken)
    {
        try
        {
            var row = await agentStore.FindAsync(target, dispatchToken);

            if (row is null || AgentStateResolver.Resolve(row, timeProvider.GetUtcNow()) != AgentState.Online)
            {
                return await RecordOfferedAsync(batchId, target, ownerId, batchAttemptId, claimedGeneration, "offline");
            }

            if (row.EndpointKind is AgentSessionEndpointKind.DbWatch or AgentSessionEndpointKind.CopilotExtension)
            {
                // Database-watching endpoints require no direct transport.
                return await RecordDeliveredAsync(batchId, target, ownerId, batchAttemptId, claimedGeneration);
            }

            if (row.EndpointKind is not AgentSessionEndpointKind.ClaudePeer
                and not AgentSessionEndpointKind.CodexThread
                and not AgentSessionEndpointKind.OpencodeServer)
            {
                return await RecordFailureAsync(batchId, target, ownerId, batchAttemptId, "unsupported");
            }

            var now = timeProvider.GetUtcNow();
            var pingAttemptId = MemoryId.New(now);
            var reservation = await gateCoordinator.TryReserveAsync(target, pingAttemptId, now, dispatchToken);

            if (reservation.Reservation is null)
            {
                var reason = reservation.Failure == WakeReservationFailure.GateBusy ? "busy" : "capacity-dropped";
                return await RecordOfferedAsync(batchId, target, ownerId, batchAttemptId, claimedGeneration, reason);
            }

            var held = reservation.Reservation;
            var success = false;
            var idlePushClaimed = false;
            var idlePushSettled = false;

            try
            {
                if (row.EndpointKind == AgentSessionEndpointKind.OpencodeServer
                    && !await agentStore.ClaimIdlePushAsync(target, dispatchToken))
                {
                    // An unarmed agent remains pending for a later attempt.
                    return await RecordOfferedAsync(
                        batchId, target, ownerId, batchAttemptId, claimedGeneration, "idle-not-armed");
                }

                if (row.EndpointKind == AgentSessionEndpointKind.OpencodeServer)
                {
                    // This attempt holds the agent's idle-push claim.
                    idlePushClaimed = true;
                }

                // Claims the ping attempt; no transport runs if the claim fails.
                var stamped = await agentStore.TryClaimPingCooldownAsync(
                    target, TimeSpan.Zero, pingAttemptId, dispatchToken);

                if (!stamped)
                {
                    return await RecordFailureAsync(batchId, target, ownerId, batchAttemptId, "session-gone");
                }

                var attemptDeadline = ClampDeadline(now, batchDeadline);

                var outcome = row.EndpointKind switch
                {
                    AgentSessionEndpointKind.ClaudePeer => await executor.ExecuteClaudePeerAsync(
                        target, row.SessionId!, pingAttemptId, held.Slot, attemptDeadline, dispatchToken),
                    AgentSessionEndpointKind.CodexThread => await executor.ExecuteCodexThreadAsync(
                        target, row.EndpointAddr, pingAttemptId, held.Slot, attemptDeadline, dispatchToken),
                    AgentSessionEndpointKind.OpencodeServer => await executor.ExecuteOpencodeServerAsync(
                        target, row.SessionId!, row.EndpointAddr, row.EndpointSecret, pingAttemptId, held.Slot,
                        attemptDeadline, dispatchToken),
                    _ => throw new UnreachableException(
                        $"Endpoint kind '{row.EndpointKind}' passed the earlier supported-kind guard.")
                };

                if (row.EndpointKind == AgentSessionEndpointKind.OpencodeServer
                    && (outcome.Reason != PingAttemptReason.Ok
                        || outcome.Detail == PingSessionExecutor.HealthOnlyDetail))
                {
                    // An unsuccessful push or health-only check restores the idle-push claim.
                    await agentStore.RearmIdlePushAsync(target, CancellationToken.None);
                }

                idlePushSettled = true;

                if (outcome.Reason == PingAttemptReason.AccessDenied)
                {
                    return await RecordOfferedAsync(
                        batchId, target, ownerId, batchAttemptId, claimedGeneration, "access-denied");
                }

                success = outcome.Reason == PingAttemptReason.Ok;
                var status = success ? MailWakeTargetStatus.Delivered : MailWakeTargetStatus.Failed;
                var lastError = success ? null : outcome.Reason.ToString();

                var recorded = await batchStore.TryRecordTargetOutcomeAsync(
                    batchId, target, ownerId, batchAttemptId, status,
                    offeredGeneration: null, acceptedGeneration: success ? claimedGeneration : null,
                    lastError, timeProvider.GetUtcNow(), CancellationToken.None);

                return recorded
                    ? new ActorWakeTargetReceipt(target, status, null, success ? claimedGeneration : null, lastError)
                    : new ActorWakeTargetReceipt(target, MailWakeTargetStatus.Pending, null, null, null);
            }
            finally
            {
                if (idlePushClaimed && !idlePushSettled)
                {
                    // An interrupted attempt restores the idle-push claim.
                    await agentStore.RearmIdlePushAsync(target, CancellationToken.None);
                }

                // Reservation cleanup continues after dispatch cancellation.
                await gateCoordinator.CompleteAsync(held, success, timeProvider.GetUtcNow(), CancellationToken.None);
            }
        }
        catch (OperationCanceledException) when (!callerToken.IsCancellationRequested)
        {
            // A cancelled dispatch leaves the target pending without asserting a transport outcome.
            return new ActorWakeTargetReceipt(target, MailWakeTargetStatus.Pending, null, null, null);
        }
    }

    private async Task<ActorWakeTargetReceipt> RecordDeliveredAsync(
        string batchId, string target, string ownerId, string attemptId, long claimedGeneration)
    {
        var recorded = await batchStore.TryRecordTargetOutcomeAsync(
            batchId, target, ownerId, attemptId, MailWakeTargetStatus.Delivered,
            offeredGeneration: null, acceptedGeneration: claimedGeneration, lastError: null,
            timeProvider.GetUtcNow(), CancellationToken.None);

        return recorded
            ? new ActorWakeTargetReceipt(target, MailWakeTargetStatus.Delivered, null, claimedGeneration, null)
            : new ActorWakeTargetReceipt(target, MailWakeTargetStatus.Pending, null, null, null);
    }

    private async Task<ActorWakeTargetReceipt> RecordFailureAsync(
        string batchId, string target, string ownerId, string attemptId, string reason)
    {
        var recorded = await batchStore.TryRecordTargetOutcomeAsync(
            batchId, target, ownerId, attemptId, MailWakeTargetStatus.Failed,
            offeredGeneration: null, acceptedGeneration: null, lastError: reason,
            timeProvider.GetUtcNow(), CancellationToken.None);

        return recorded
            ? new ActorWakeTargetReceipt(target, MailWakeTargetStatus.Failed, null, null, reason)
            : new ActorWakeTargetReceipt(target, MailWakeTargetStatus.Pending, null, null, null);
    }

    private async Task<ActorWakeTargetReceipt> RecordOfferedAsync(
        string batchId, string target, string ownerId, string attemptId,
        long claimedGeneration, string reason)
    {
        var recorded = await batchStore.TryRecordTargetOutcomeAsync(
            batchId, target, ownerId, attemptId, MailWakeTargetStatus.Pending,
            offeredGeneration: claimedGeneration, acceptedGeneration: null, lastError: reason,
            timeProvider.GetUtcNow(), CancellationToken.None);

        return new ActorWakeTargetReceipt(
            target, MailWakeTargetStatus.Pending, recorded ? claimedGeneration : null, null, recorded ? reason : null);
    }

    /// <summary>
    /// The earlier of <paramref name="batchDeadline"/> less
    /// <see cref="WakeDispatchPolicy.HandoffObservationReserve"/>, and
    /// <paramref name="now"/> plus <see cref="PingPolicy.HardTimeout"/>.
    /// </summary>
    private static DateTimeOffset ClampDeadline(DateTimeOffset now, DateTimeOffset batchDeadline)
    {
        var reserved = batchDeadline - WakeDispatchPolicy.HandoffObservationReserve;
        var hardCap = now + PingPolicy.HardTimeout;
        return reserved < hardCap ? reserved : hardCap;
    }
}
