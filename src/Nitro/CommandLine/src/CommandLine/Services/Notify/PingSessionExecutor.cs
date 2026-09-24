using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Services.Notify;

internal sealed class PingSessionExecutor(
    IMailStore mailStore,
    IAgentDeliveryLedger ledger,
    ICodexQueueClient queueClient,
    IClaudePeerClient claudePeerClient,
    IAgentStore agentStore,
    IPingLeaseStore leaseStore,
    TimeProvider timeProvider,
    IOpencodeServerClient opencodeServerClient) : IPingSessionExecutor
{
    /// <summary>
    /// The diagnostic marking an opencode health check performed without a mail digest.
    /// </summary>
    internal const string HealthOnlyDetail = "health-only";

    public Task<PingAttemptOutcome> ExecuteCodexThreadAsync(
        string actorName,
        string endpointAddr,
        string attemptId,
        int slot,
        DateTimeOffset deadline,
        CancellationToken cancellationToken)
        => ExecuteAsync(
            actorName,
            attemptId,
            slot,
            deadline,
            async (digest, token) => digest is null
                ? new TransportOutcome(PingAttemptReason.Ok, null)
                : MapQueueResult(await queueClient.QueueAsync(endpointAddr, digest, token)),
            cancellationToken);

    public Task<PingAttemptOutcome> ExecuteClaudePeerAsync(
        string actorName,
        string sessionId,
        string attemptId,
        int slot,
        DateTimeOffset deadline,
        CancellationToken cancellationToken)
        => ExecuteAsync(
            actorName,
            attemptId,
            slot,
            deadline,
            async (digest, token) => digest is null
                ? new TransportOutcome(PingAttemptReason.Ok, null)
                : MapClaudePeerResult(await claudePeerClient.SendAsync(sessionId, digest, token)),
            cancellationToken);

    public Task<PingAttemptOutcome> ExecuteOpencodeServerAsync(
        string actorName,
        string sessionId,
        string endpointAddr,
        string? endpointSecret,
        string attemptId,
        int slot,
        DateTimeOffset deadline,
        CancellationToken cancellationToken)
        => ExecuteAsync(
            actorName,
            attemptId,
            slot,
            deadline,
            async (digest, token) =>
            {
                if (digest is not null)
                {
                    return MapOpencodeResult(
                        await opencodeServerClient.PushMessageAsync(
                            endpointAddr,
                            sessionId,
                            OpencodeHookProtocol.PushedPromptPrefix + digest,
                            endpointSecret,
                            token));
                }

                var pingOutcome = MapOpencodeResult(
                    await opencodeServerClient.PingAsync(endpointAddr, sessionId, endpointSecret, token));

                return pingOutcome with { Detail = HealthOnlyDetail };
            },
            cancellationToken);

    /// <summary>
    /// Builds a digest and invokes <paramref name="sendAsync"/> within the attempt budget,
    /// passing null when no unread mail remains. Outcome recording and lease release
    /// are best effort.
    /// </summary>
    private async Task<PingAttemptOutcome> ExecuteAsync(
        string actorName,
        string attemptId,
        int slot,
        DateTimeOffset deadline,
        Func<string?, CancellationToken, Task<TransportOutcome>> sendAsync,
        CancellationToken cancellationToken)
    {
        var remaining = ClampRemaining(deadline);

        if (remaining <= TimeSpan.Zero)
        {
            // An expired deadline produces a timeout without digest or transport work.
            try
            {
                return await WriteResultAsync(actorName, attemptId, PingAttemptReason.Timeout, null);
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
                return await WriteResultAsync(actorName, attemptId, PingAttemptReason.Timeout, null);
            }

            TransportOutcome transportOutcome;

            try
            {
                transportOutcome = await sendAsync(digest, linkedSource.Token);
            }
            catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
            {
                return await WriteResultAsync(actorName, attemptId, PingAttemptReason.Timeout, null);
            }

            return await WriteResultAsync(
                actorName, attemptId, transportOutcome.Reason, Truncate(transportOutcome.Detail));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Non-cancellation failures produce a transport-error outcome.
            return await WriteResultAsync(
                actorName, attemptId, PingAttemptReason.TransportError, Truncate(exception.Message));
        }
        finally
        {
            await ReleaseLeaseAsync(slot, attemptId);
        }
    }

    private async Task<string?> BuildDigestAsync(string actorName, CancellationToken cancellationToken)
    {
        var unread = await mailStore.QueryInboxAsync(
            new MailInboxFilter { Actor = actorName, UnreadOnly = true, Limit = MailDigestPolicy.MaxMessages },
            cancellationToken);

        if (unread.Count == 0)
        {
            return null;
        }

        var messageIds = unread.Select(message => message.Id).ToList();
        var delivered = await ledger.FindDeliveredAsync(actorName, messageIds, cancellationToken);
        var reserved = await ledger.ReserveAsync(
            actorName, messageIds, AgentSessionChannel.Ping, timeProvider.GetUtcNow(), cancellationToken);
        var reservedIds = reserved.ToHashSet(StringComparer.Ordinal);
        var deliveredIds = delivered.ToHashSet(StringComparer.Ordinal);
        var messages = unread
            .Where(message => reservedIds.Contains(message.Id) && !deliveredIds.Contains(message.Id))
            .ToList();
        var unreadTotal = await mailStore.CountUnreadAsync(actorName, cancellationToken);

        return MailDigest.Render(actorName, messages, unreadTotal);
    }

    /// <summary>
    /// Attempts to record the outcome independently of caller cancellation and returns it
    /// even if recording fails.
    /// </summary>
    private async Task<PingAttemptOutcome> WriteResultAsync(
        string actorName, string attemptId, PingAttemptReason reason, string? detail)
    {
        var result = ToResult(reason);

        try
        {
            await agentStore.WritePingResultAsync(actorName, attemptId, result, detail, CancellationToken.None);
        }
        catch
        {
            // A storage failure does not prevent returning the outcome.
        }

        return new PingAttemptOutcome(
            result,
            reason,
            IsRetryable(reason),
            detail,
            actorName,
            attemptId,
            timeProvider.GetUtcNow());
    }

    private async Task ReleaseLeaseAsync(int slot, string attemptId)
    {
        try
        {
            // Attempts lease release independently of caller cancellation.
            await leaseStore.ReleaseAsync(slot, attemptId, CancellationToken.None);
        }
        catch
        {
            // A failed release leaves the slot reserved until expiry or another release.
        }
    }

    private static string? Truncate(string? value)
        => value is { Length: > 200 } ? value[..200] : value;

    /// <summary>
    /// Maps an attempt reason to its persisted ping result, using
    /// <see cref="AgentPingResult.Error"/> when no dedicated result exists.
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
        // Other queue failures have no transport detail.
        _ => new TransportOutcome(PingAttemptReason.TransportError, null)
    };

    /// <summary>
    /// Maps opencode success and timeout results directly; all other values map to
    /// <see cref="PingAttemptReason.EndpointGone"/>.
    /// </summary>
    private static TransportOutcome MapOpencodeResult(string result) => result switch
    {
        AgentPingResult.Ok => new TransportOutcome(PingAttemptReason.Ok, null),
        AgentPingResult.Timeout => new TransportOutcome(PingAttemptReason.Timeout, null),
        _ => new TransportOutcome(PingAttemptReason.EndpointGone, null)
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
    /// <c>[TimeSpan.Zero, PingPolicy.HardTimeout]</c>.
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
