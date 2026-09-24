using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Services.Notify;

internal sealed class MailNudge(
    IAgentStore agentStore,
    IMailStore mail,
    IAgentDeliveryLedger ledger,
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
            // Notification failure leaves the mail unread.
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

        foreach (var actor in actors.Distinct(StringComparer.Ordinal))
        {
            try
            {
                var row = await agentStore.FindAsync(actor, cancellationToken);

                if (row is null || AgentStateResolver.Resolve(row, timeProvider.GetUtcNow()) != AgentState.Online)
                {
                    // Nudging only reaches an actor the wake system could itself target.
                    continue;
                }

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

                var messageIds = unread.Select(message => message.Id).ToList();
                var delivered = await ledger.FindDeliveredAsync(actor, messageIds, cancellationToken);
                var reserved = await ledger.ReserveAsync(
                    actor, messageIds, AgentSessionChannel.Ping, timeProvider.GetUtcNow(), cancellationToken);
                var reservedIds = reserved.ToHashSet(StringComparer.Ordinal);
                var deliveredIds = delivered.ToHashSet(StringComparer.Ordinal);
                var messages = unread
                    .Where(message => reservedIds.Contains(message.Id) && !deliveredIds.Contains(message.Id))
                    .ToList();
                var unreadTotal = await mail.CountUnreadAsync(actor, cancellationToken);
                var text = MailDigest.Render(actor, messages, unreadTotal);

                await SendAsync(row, text, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // Notification failure leaves the mail unread.
            }
        }
    }

    /// <summary>
    /// Sends through a Claude peer or Codex thread endpoint, skipping other endpoint kinds.
    /// Cancellation propagates; other transport failures are ignored.
    /// </summary>
    private async Task SendAsync(
        AgentRow row,
        string text,
        CancellationToken cancellationToken)
    {
        try
        {
            switch (row.EndpointKind)
            {
                case AgentSessionEndpointKind.ClaudePeer:
                    if (row.SessionId is null)
                    {
                        break;
                    }

                    await claudePeerClient.SendAsync(row.SessionId, text, cancellationToken);
                    break;

                case AgentSessionEndpointKind.CodexThread:
                    await codexQueueClient.QueueAsync(row.EndpointAddr, text, cancellationToken);
                    break;
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Notification failure leaves the mail unread.
        }
    }
}
