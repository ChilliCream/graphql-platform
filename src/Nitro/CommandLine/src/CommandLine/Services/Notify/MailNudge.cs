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
        try
        {
            await NudgeCoreAsync(actors, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Best effort: the recipients' next turns report the unread mail.
        }
    }

    private async Task NudgeCoreAsync(
        IReadOnlyList<string> actors,
        CancellationToken cancellationToken)
    {
        if (actors.Count == 0)
        {
            return;
        }

        var participants = await sessions.ListParticipantsAsync(cancellationToken);

        foreach (var actor in actors.Distinct(StringComparer.Ordinal))
        {
            // No liveness check: the nudge is best effort, so trying and
            // failing costs the same as asking first and is never stale.
            var targets = participants
                .Where(participant => participant.Session.AgentName == actor)
                .ToArray();

            if (targets.Length == 0)
            {
                continue;
            }

            foreach (var target in targets)
            {
                try
                {
                    var unread = await mail.QueryInboxAsync(
                        new MailInboxFilter
                        {
                            Actor = actor,
                            UnreadOnly = true,
                            Limit = MailDigestPolicy.MaxMessages
                        },
                        cancellationToken);

                    if (unread.Count == 0)
                    {
                        continue;
                    }

                    var generation = new AgentSessionGeneration(
                        target.Session.Harness,
                        target.Session.SessionId,
                        target.Session.Host);
                    var messageIds = unread.Select(message => message.Id).ToList();
                    var delivered = await ledger.FindDeliveredAsync(
                        generation, messageIds, cancellationToken);
                    var reserved = await ledger.ReserveAsync(
                        generation.Harness,
                        generation.SessionId,
                        messageIds,
                        AgentSessionChannel.Ping,
                        timeProvider.GetUtcNow(),
                        cancellationToken);
                    var reservedIds = reserved.ToHashSet(StringComparer.Ordinal);
                    var deliveredIds = delivered.ToHashSet(StringComparer.Ordinal);
                    var messages = unread
                        .Where(message =>
                            reservedIds.Contains(message.Id) && !deliveredIds.Contains(message.Id))
                        .ToList();
                    var unreadTotal = await mail.CountUnreadAsync(actor, cancellationToken);
                    var text = MailDigest.Render(actor, messages, unreadTotal);

                    await SendAsync(target.Session, text, cancellationToken);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    // Best effort: the recipient's next turn reports the unread mail.
                }
            }
        }
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
