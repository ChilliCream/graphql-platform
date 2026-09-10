using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// Claims one actor's outstanding <c>mail_wake_outbox</c> generation as a
/// frozen <c>mail_wake_batches</c> row and dispatches its one materialized
/// coding-session target in the foreground, entirely in-process (no detached worker):
/// <list type="bullet">
/// <item>Nothing outstanding, not yet due, or another owner already holds a
/// live batch for this actor: <see cref="DispatchAsync"/> returns null.</item>
/// <item>The frozen target set is empty (no live claimed session): the
/// batch completes immediately as skipped: that actor takes no push.</item>
/// <item>The actor has no unread mail left by dispatch time: the target
/// settles <see cref="MailWakeTargetStatus.Satisfied"/> without attempting
/// any transport.</item>
/// <item>Otherwise, the target is re-resolved against its exact frozen
/// generation (a session that ended or rebound since the claim disappears
/// as a failure, not a stale write). Nitro board sessions are never targets;
/// the board is an alternate dispatcher for work a sandboxed sender leaves
/// pending. The coding-session target is reserved through
/// <see cref="ISessionGateCoordinator"/> and dispatched through
/// <see cref="IPingSessionExecutor"/>. A Claude access-denied outcome leaves
/// the target pending so a Nitro board daemon can retry the same delivery
/// outside the sender's sandbox.</item>
/// </list>
/// The batch's own lease is renewed periodically while dispatch is in
/// flight; losing that renewal to a fresher claimant cancels the target,
/// and this attempt makes no further claim about its outcome (left
/// <see cref="MailWakeTargetStatus.Pending"/>,
/// never asserted as failed or delivered) and does not touch the batch row
/// again, since a newer owner already holds it. When this attempt still
/// holds the batch at the end, it completes (settles the claimed generation)
/// unless at least one target was left with durable offered work, in which
/// case it releases the batch with a rescheduled retry instead.
/// </summary>
internal sealed class ActorWakeDispatcher(
    IMailWakeBatchStore batchStore,
    IAgentSessionRegistry sessionRegistry,
    ISessionGateCoordinator gateCoordinator,
    IPingSessionExecutor executor,
    IMailStore mailStore,
    INitroInstanceIdProvider instanceIdProvider,
    IGlobalConfigDirectoryProvider globalConfigDirectoryProvider,
    TimeProvider timeProvider) : IActorWakeDispatcher
{
    public async Task<ActorWakeReceipt?> DispatchAsync(
        string actor, DateTimeOffset deadline, CancellationToken cancellationToken)
    {
        var normalizedActor = MailAgentName.Normalize(actor);
        var nitroInstanceId = await instanceIdProvider.GetIdAsync(
            globalConfigDirectoryProvider.GetDirectory(), cancellationToken);

        var candidates = await ResolveCandidatesAsync(normalizedActor, cancellationToken);

        var now = timeProvider.GetUtcNow();
        var ownerId = $"dispatcher-{Guid.NewGuid():N}";
        var batchAttemptId = $"batch-{Guid.NewGuid():N}";

        var claim = await batchStore.TryClaimAsync(
            nitroInstanceId, normalizedActor, ownerId, batchAttemptId, candidates, now,
            WakeDispatchPolicy.BatchLeaseDuration, cancellationToken);

        if (claim is null)
        {
            return null;
        }

        if (claim.Targets.Count == 0)
        {
            await batchStore.TryCompleteAsync(claim.BatchId, ownerId, batchAttemptId, now, cancellationToken);
            return new ActorWakeReceipt(normalizedActor, MailWakeTargetStatus.Skipped, []);
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

    private async Task<IReadOnlyList<AgentSessionGeneration>> ResolveCandidatesAsync(
        string actor, CancellationToken cancellationToken)
    {
        var sessions = await sessionRegistry.FindLiveClaimedByAgentNameAsync(actor, cancellationToken);

        return sessions
            .Where(s => s.Harness != AgentSessionHarness.NitroBoard)
            .OrderByDescending(s => s.LastBeatAt)
            .Take(1)
            .Select(s => new AgentSessionGeneration(s.Harness, s.SessionId, s.Host))
            .ToList();
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
                actor,
                claim.Targets.Single(),
                claim.ClaimedGeneration,
                batchDeadline,
                dispatchSource.Token,
                cancellationToken);
        }
        finally
        {
            // Always drained before the `using` CTSs above dispose, even
            // when Task.WhenAll itself throws (the caller's own token was
            // cancelled): RenewLoopAsync must never observe a disposed
            // CancellationTokenSource through lossSource or stopToken.
            await renewalDoneSource.CancelAsync();

            try
            {
                await renewalTask;
            }
            catch (OperationCanceledException)
            {
                // Expected once renewalDoneSource signals dispatch is over,
                // or the caller's own token was cancelled.
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

        // else: a newer owner already holds the batch (this attempt's
        // renewal was lost); it owns completing or releasing it now, not
        // this attempt.

        return new ActorWakeReceipt(actor, receipt.Status, [receipt]);
    }

    /// <summary>
    /// Periodically renews the batch's own lease while its targets are
    /// still dispatching. A failed renewal (the lease was lost to a fresher
    /// claimant) cancels <paramref name="lossSource"/>, which every
    /// in-flight or not-yet-started target's own dispatch observes and
    /// stops on.
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
            // Dispatch finished normally (stopToken), or the caller's own
            // token was cancelled: either way, nothing more to renew.
        }
        catch (Exception)
        {
            // A renewal whose result is unknown (the store call itself
            // threw, not merely reported false) is treated the same as a
            // lost renewal: in-flight targets are left Pending and the
            // batch row is left for lease-expiry reclaim, so no non-OCE
            // exception ever escapes RenewLoopAsync past this point.
            await lossSource.CancelAsync();
        }
    }

    private async Task<ActorWakeTargetReceipt> DispatchTargetAsync(
        string batchId,
        string ownerId,
        string batchAttemptId,
        string actor,
        AgentSessionGeneration target,
        long claimedGeneration,
        DateTimeOffset batchDeadline,
        CancellationToken dispatchToken,
        CancellationToken callerToken)
    {
        try
        {
            var session = await sessionRegistry.FindByGenerationAsync(target, dispatchToken);

            if (session is null)
            {
                return await RecordFailureAsync(batchId, target, ownerId, batchAttemptId, "session-gone");
            }

            if (session.EndpointKind == AgentSessionEndpointKind.None)
            {
                return await RecordFailureAsync(batchId, target, ownerId, batchAttemptId, "no-endpoint");
            }

            if (session.EndpointKind is AgentSessionEndpointKind.DbWatch
                or AgentSessionEndpointKind.CopilotExtension)
            {
                // Database-watching endpoints observe the committed message
                // themselves. No direct transport is required.
                return await RecordDeliveredAsync(batchId, target, ownerId, batchAttemptId, claimedGeneration);
            }

            if (session.EndpointKind is not AgentSessionEndpointKind.ClaudePeer
                and not AgentSessionEndpointKind.CodexThread)
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

            try
            {
                var attemptDeadline = ClampDeadline(now, batchDeadline);

                var outcome = session.EndpointKind == AgentSessionEndpointKind.ClaudePeer
                    ? await executor.ExecuteClaudePeerAsync(
                        session.Harness, session.SessionId, actor, pingAttemptId, held.Slot,
                        attemptDeadline, dispatchToken)
                    : await executor.ExecuteCodexThreadAsync(
                        session.Harness, session.SessionId, actor, session.EndpointAddr, pingAttemptId, held.Slot,
                        attemptDeadline, dispatchToken);

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
                // Never on dispatchToken: the reservation this attempt holds
                // must be released (or extended into cooldown) even when
                // dispatchToken itself is the reason the transport call just
                // unwound.
                await gateCoordinator.CompleteAsync(held, success, timeProvider.GetUtcNow(), CancellationToken.None);
            }
        }
        catch (OperationCanceledException) when (!callerToken.IsCancellationRequested)
        {
            // The batch's own lease renewal was lost (or dispatchToken was
            // otherwise cancelled for a reason other than the caller's own
            // token): this target was never dispatched, or its dispatch was
            // aborted mid-flight, possibly after a transport call already
            // wrote to the wire. Never asserted delivered or failed; its row
            // stays whatever it already durably was.
            return new ActorWakeTargetReceipt(target, MailWakeTargetStatus.Pending, null, null, null);
        }
    }

    private async Task<ActorWakeTargetReceipt> RecordDeliveredAsync(
        string batchId, AgentSessionGeneration target, string ownerId, string attemptId, long claimedGeneration)
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
        string batchId, AgentSessionGeneration target, string ownerId, string attemptId, string reason)
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
        string batchId, AgentSessionGeneration target, string ownerId, string attemptId,
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
    /// The earlier of (<see cref="WakeDispatchPolicy.HandoffObservationReserve"/>
    /// held back from <paramref name="batchDeadline"/>) and one attempt's
    /// own <see cref="PingPolicy.HardTimeout"/> budget from <paramref name="now"/>,
    /// so a target dispatched late in the batch's window never gets more
    /// than what is left, and no target ever gets more than its own hard
    /// cap regardless of how much budget remains.
    /// </summary>
    private static DateTimeOffset ClampDeadline(DateTimeOffset now, DateTimeOffset batchDeadline)
    {
        var reserved = batchDeadline - WakeDispatchPolicy.HandoffObservationReserve;
        var hardCap = now + PingPolicy.HardTimeout;
        return reserved < hardCap ? reserved : hardCap;
    }
}
