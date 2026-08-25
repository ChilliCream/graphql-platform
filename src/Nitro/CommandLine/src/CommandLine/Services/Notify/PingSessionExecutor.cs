using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Services.Notify;

internal sealed class PingSessionExecutor(
    IMailStore mailStore,
    ICodexQueueClient queueClient,
    IClaudePeerClient claudePeerClient,
    IAgentSessionRegistry sessionRegistry,
    IPingLeaseStore leaseStore,
    TimeProvider timeProvider) : IPingSessionExecutor
{
    public Task<PingAttemptOutcome> ExecuteCodexThreadAsync(
        string harness,
        string sessionId,
        string actorName,
        string endpointAddr,
        string attemptId,
        int slot,
        DateTimeOffset deadline,
        CancellationToken cancellationToken)
        => ExecuteAsync(
            harness,
            sessionId,
            actorName,
            attemptId,
            slot,
            deadline,
            async (digest, token) => MapQueueResult(await queueClient.QueueAsync(endpointAddr, digest, token)),
            cancellationToken);

    public Task<PingAttemptOutcome> ExecuteClaudePeerAsync(
        string harness,
        string sessionId,
        string actorName,
        int pid,
        string attemptId,
        int slot,
        DateTimeOffset deadline,
        CancellationToken cancellationToken)
        => ExecuteAsync(
            harness,
            sessionId,
            actorName,
            attemptId,
            slot,
            deadline,
            async (digest, token) => MapClaudePeerResult(
                await claudePeerClient.SendAsync(pid, sessionId, digest, token)),
            cancellationToken);

    private async Task<PingAttemptOutcome> ExecuteAsync(
        string harness,
        string sessionId,
        string actorName,
        string attemptId,
        int slot,
        DateTimeOffset deadline,
        Func<string, CancellationToken, Task<TransportOutcome>> sendAsync,
        CancellationToken cancellationToken)
    {
        var remaining = ClampRemaining(deadline);

        if (remaining <= TimeSpan.Zero)
        {
            // The deadline was already behind us by the time this attempt
            // started running (startup latency across the process
            // boundary counted against it): no digest or transport work
            // may start.
            try
            {
                return await WriteResultAsync(
                    harness, sessionId, attemptId, PingAttemptReason.Timeout, null);
            }
            finally
            {
                await ReleaseLeaseAsync(slot, attemptId);
            }
        }

        using var timeoutSource = new CancellationTokenSource(remaining);
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, timeoutSource.Token);

        try
        {
            string? digest;

            try
            {
                digest = await BuildDigestAsync(actorName, linkedSource.Token);
            }
            catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
            {
                return await WriteResultAsync(
                    harness, sessionId, attemptId, PingAttemptReason.Timeout, null);
            }

            if (digest is null)
            {
                // The unread mail that triggered this ping was already read
                // by the time the attempt actually ran (a benign race, not
                // a failure): nothing left to say, so this is a success
                // with no transport call.
                return await WriteResultAsync(harness, sessionId, attemptId, PingAttemptReason.Ok, null);
            }

            TransportOutcome transportOutcome;

            try
            {
                transportOutcome = await sendAsync(digest, linkedSource.Token);
            }
            catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
            {
                return await WriteResultAsync(
                    harness, sessionId, attemptId, PingAttemptReason.Timeout, null);
            }

            return await WriteResultAsync(
                harness, sessionId, attemptId, transportOutcome.Reason, Truncate(transportOutcome.Detail));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // A failed ping is a non-event (the plan's failure policy): no
            // exception from this method may ever propagate to a caller
            // whose own exit code or output must stay unaffected.
            return await WriteResultAsync(
                harness, sessionId, attemptId, PingAttemptReason.TransportError, Truncate(exception.Message));
        }
        finally
        {
            await ReleaseLeaseAsync(slot, attemptId);
        }
    }

    private async Task<string?> BuildDigestAsync(string actorName, CancellationToken cancellationToken)
    {
        var unread = await mailStore.QueryInboxAsync(
            new MailInboxFilter { Actor = actorName, UnreadOnly = true, Limit = PingPolicy.MaxDigestMessages },
            cancellationToken);

        if (unread.Count == 0)
        {
            return null;
        }

        var totalUnread = await mailStore.CountUnreadAsync(actorName, cancellationToken);
        var entries = unread.Select(m => (m.Id, m.Sender, m.Subject, m.Body)).ToList();

        return ClaudeHookDigestFormatter.Format(totalUnread, entries);
    }

    /// <summary>
    /// Always writes on an uncancellable token: the caller's own
    /// cancellation or hard-timeout token must not prevent the outcome it
    /// just decided from actually being recorded.
    /// </summary>
    private async Task<PingAttemptOutcome> WriteResultAsync(
        string harness, string sessionId, string attemptId, PingAttemptReason reason, string? detail)
    {
        var result = ToResult(reason);

        try
        {
            await sessionRegistry.WritePingResultAsync(
                harness, sessionId, attemptId, result, detail, CancellationToken.None);
        }
        catch
        {
            // Recording the outcome is itself best effort; a failed write
            // is a non-event like every other ping failure.
        }

        return new PingAttemptOutcome(
            result,
            reason,
            IsRetryable(reason),
            detail,
            harness,
            sessionId,
            attemptId,
            timeProvider.GetUtcNow());
    }

    private async Task ReleaseLeaseAsync(int slot, string attemptId)
    {
        try
        {
            // A best-effort release on an unrelated, uncancellable token: a
            // caller's own cancellation must not leak the slot until it
            // expires and gets stolen.
            await leaseStore.ReleaseAsync(slot, attemptId, CancellationToken.None);
        }
        catch
        {
            // The slot self-heals via expiry either way; a failed release
            // is a non-event like every other ping failure.
        }
    }

    private static string? Truncate(string? value)
        => value is { Length: > 200 } ? value[..200] : value;

    /// <summary>
    /// The coarse, CHECK-compatible <c>agent_sessions.last_ping_result</c>
    /// value for <paramref name="reason"/>. Every reason without its own
    /// column value collapses to <see cref="AgentPingResult.Error"/>; rich
    /// per-reason state lives only in the returned <see cref="PingAttemptOutcome"/>.
    /// </summary>
    private static string ToResult(PingAttemptReason reason) => reason switch
    {
        PingAttemptReason.Ok => AgentPingResult.Ok,
        PingAttemptReason.Unsupported => AgentPingResult.Unsupported,
        PingAttemptReason.EndpointGone => AgentPingResult.EndpointGone,
        PingAttemptReason.Timeout => AgentPingResult.Timeout,
        PingAttemptReason.CapacityDropped => AgentPingResult.CapacityDropped,
        _ => AgentPingResult.Error
    };

    private static bool IsRetryable(PingAttemptReason reason) => reason switch
    {
        PingAttemptReason.Timeout => true,
        PingAttemptReason.CapacityDropped => true,
        PingAttemptReason.TransportError => true,
        _ => false
    };

    private static TransportOutcome MapQueueResult(CodexQueueResult result) => result switch
    {
        CodexQueueResult.Ok => new TransportOutcome(PingAttemptReason.Ok, null),
        CodexQueueResult.EndpointGone => new TransportOutcome(PingAttemptReason.EndpointGone, null),
        // CodexQueueResult.Error covers a spawn failure, a timeout, or any
        // other nonzero exit; the subprocess's raw stderr never reaches
        // this layer, so there is no detail to attach.
        _ => new TransportOutcome(PingAttemptReason.TransportError, null)
    };

    private static TransportOutcome MapClaudePeerResult(ClaudePeerSendOutcome outcome) => new(
        outcome.Reason switch
        {
            ClaudePeerSendReason.Ok => PingAttemptReason.Ok,
            ClaudePeerSendReason.Unsupported => PingAttemptReason.Unsupported,
            ClaudePeerSendReason.EndpointGone => PingAttemptReason.EndpointGone,
            ClaudePeerSendReason.InvalidAuth => PingAttemptReason.InvalidAuth,
            ClaudePeerSendReason.AccessDenied => PingAttemptReason.AccessDenied,
            _ => PingAttemptReason.TransportError
        },
        outcome.Detail);

    /// <summary>
    /// The time left until <paramref name="deadline"/>, clamped to
    /// <c>[TimeSpan.Zero, PingPolicy.HardTimeout]</c> so a caller's own
    /// clock skew or an unexpectedly distant deadline can never grant an
    /// attempt more than its policy budget.
    /// </summary>
    private TimeSpan ClampRemaining(DateTimeOffset deadline)
    {
        var remaining = deadline - timeProvider.GetUtcNow();

        if (remaining < TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        return remaining > PingPolicy.HardTimeout ? PingPolicy.HardTimeout : remaining;
    }

    private readonly record struct TransportOutcome(PingAttemptReason Reason, string? Detail);
}
