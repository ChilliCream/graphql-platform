using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Services.Notify;

internal sealed class PingSessionExecutor(
    IMailStore mailStore,
    ICodexQueueClient queueClient,
    IAgentSessionRegistry sessionRegistry,
    IPingLeaseStore leaseStore,
    TimeProvider timeProvider) : IPingSessionExecutor
{
    public async Task<string> ExecuteCodexThreadAsync(
        string harness,
        string sessionId,
        string actorName,
        string endpointAddr,
        string attemptId,
        int slot,
        DateTimeOffset deadline,
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
                return await WriteResultAsync(harness, sessionId, attemptId, AgentPingResult.Timeout, null);
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
                return await WriteResultAsync(harness, sessionId, attemptId, AgentPingResult.Timeout, null);
            }

            if (digest is null)
            {
                // The unread mail that triggered this ping was already read
                // by the time the attempt actually ran (a benign race, not
                // a failure): nothing left to say, so this is a success
                // with no transport call.
                return await WriteResultAsync(harness, sessionId, attemptId, AgentPingResult.Ok, null);
            }

            bool queued;

            try
            {
                queued = await queueClient.QueueAsync(endpointAddr, digest, linkedSource.Token);
            }
            catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
            {
                return await WriteResultAsync(harness, sessionId, attemptId, AgentPingResult.Timeout, null);
            }

            return await WriteResultAsync(
                harness, sessionId, attemptId, queued ? AgentPingResult.Ok : AgentPingResult.Error, null);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // A failed ping is a non-event (the plan's failure policy): no
            // exception from this method may ever propagate to a caller
            // whose own exit code or output must stay unaffected.
            return await WriteResultAsync(
                harness, sessionId, attemptId, AgentPingResult.Error, Truncate(exception.Message));
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
        var entries = unread.Select(m => (m.Id, m.Sender)).ToList();

        return ClaudeHookDigestFormatter.Format(totalUnread, entries);
    }

    /// <summary>
    /// Always writes on an uncancellable token: the caller's own
    /// cancellation or hard-timeout token must not prevent the outcome it
    /// just decided from actually being recorded.
    /// </summary>
    private async Task<string> WriteResultAsync(
        string harness, string sessionId, string attemptId, string result, string? detail)
    {
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

        return result;
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
}
