using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Services.Notify;

internal sealed class MailNudge(
    IAgentSessionRegistry sessions,
    IMailStore mail,
    IClaudePeerClient claudePeerClient,
    ICodexQueueClient codexQueueClient) : IMailNudge
{
    public async Task NudgeAsync(IReadOnlyList<string> actors, CancellationToken cancellationToken)
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

            var unread = await mail.CountUnreadAsync(actor, cancellationToken);

            if (unread == 0)
            {
                continue;
            }

            var text = MailNudgeText.Format(actor, unread);

            foreach (var target in targets)
            {
                await SendAsync(target.Session, text, cancellationToken);
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
