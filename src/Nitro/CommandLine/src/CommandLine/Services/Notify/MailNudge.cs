using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Services.Notify;

internal sealed class MailNudge(
    IAgentSessionRegistry sessions,
    IMailStore mail,
    ISessionDeliveryLedger ledger,
    IClaudePeerClient claudePeerClient,
    ICodexQueueClient codexQueueClient,
    TimeProvider timeProvider) : IMailNudge
{
    public async Task NudgeAsync(IReadOnlyList<string> actors, CancellationToken cancellationToken)
    {
        if (actors.Count == 0)
        {
            return;
        }

        foreach (var actor in actors.Distinct(StringComparer.Ordinal))
        {
            var target = await ResolveTargetAsync(actor, cancellationToken);

            if (target is null)
            {
                continue;
            }

            var unread = await mail.QueryInboxAsync(
                new MailInboxFilter { Actor = actor, UnreadOnly = true, Limit = PingPolicy.MaxDigestMessages },
                cancellationToken);

            if (unread.Count == 0)
            {
                continue;
            }

            // Reserve-then-emit on the shared ping channel: the wake daemon
            // dispatches the same nudge for the same message, and only one of
            // the two may claim it.
            var reserved = await ledger.ReserveAsync(
                target.Harness,
                target.SessionId,
                unread.Select(message => message.Id).ToList(),
                AgentSessionChannel.Ping,
                timeProvider.GetUtcNow(),
                cancellationToken);

            if (reserved.Count == 0)
            {
                continue;
            }

            await SendAsync(
                target,
                MailNudgeText.Format(actor, await mail.CountUnreadAsync(actor, cancellationToken)),
                cancellationToken);
        }
    }

    /// <summary>
    /// The single session this nudge fires at: the most recently seen live
    /// coding session bound to <paramref name="actor"/> that advertises a
    /// push endpoint, or null when the actor has none.
    /// </summary>
    private async Task<AgentSessionRecord?> ResolveTargetAsync(
        string actor,
        CancellationToken cancellationToken)
    {
        var live = await sessions.FindLiveClaimedByAgentNameAsync(actor, cancellationToken);

        return live
            .Where(session => session.Harness != AgentSessionHarness.NitroBoard)
            .Where(session => session.EndpointKind
                is AgentSessionEndpointKind.ClaudePeer or AgentSessionEndpointKind.CodexThread)
            .MaxBy(session => session.LastBeatAt);
    }

    /// <summary>
    /// Delivers the nudge over whichever transport the session advertises.
    /// A session with no reachable endpoint, and any transport failure, is
    /// ignored.
    /// </summary>
    private async Task SendAsync(
        AgentSessionRecord session,
        string text,
        CancellationToken cancellationToken)
    {
        try
        {
            switch (session.EndpointKind)
            {
                case AgentSessionEndpointKind.ClaudePeer:
                    await claudePeerClient.SendAsync(session.SessionId, text, cancellationToken);
                    break;

                case AgentSessionEndpointKind.CodexThread:
                    await codexQueueClient.QueueAsync(session.EndpointAddr, text, cancellationToken);
                    break;
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Best effort: the recipient's next turn reports the unread mail.
        }
    }
}
