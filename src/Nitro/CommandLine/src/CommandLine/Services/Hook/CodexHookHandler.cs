using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

internal sealed class CodexHookHandler(
    IFileSystem fileSystem,
    TimeProvider timeProvider,
    IAgentStore agentStore,
    IAgentDeliveryLedger ledger,
    IMailStore mailStore,
    ICodexHarnessVersionResolver harnessVersionResolver,
    ICodexQueueClient queueClient) : ICodexHookHandler
{
    public async Task<CodexHookOutcome> HandleSessionStartAsync(
        CodexHookPayload payload, CancellationToken cancellationToken)
    {
        var workspaceDirectory = Resolve(payload);

        if (workspaceDirectory is null)
        {
            return CodexHookOutcome.Neutral;
        }

        var result = await agentStore.StartSessionAsync(
            BuildStartRequest(payload, workspaceDirectory), cancellationToken);

        if (result.Kind == AgentSessionStartKind.Ignored)
        {
            return CodexHookOutcome.Neutral;
        }

        var row = result.Row!;

        return new CodexHookOutcome { AdditionalContext = AgentActorContext.Format(row.Name, row.Role) };
    }

    public async Task<CodexHookOutcome> HandleUserPromptSubmitAsync(
        CodexHookPayload payload, CancellationToken cancellationToken)
    {
        var row = await ResolveOrStartRowAsync(payload, cancellationToken);

        if (row is null)
        {
            return CodexHookOutcome.Neutral;
        }

        var digest = await BuildDigestAsync(row.Name, AgentSessionChannel.Digest, cancellationToken);

        return digest is null
            ? CodexHookOutcome.Neutral
            : new CodexHookOutcome { AdditionalContext = digest.Text };
    }

    public async Task<CodexHookOutcome> HandleSessionEndAsync(
        CodexHookPayload payload, CancellationToken cancellationToken)
    {
        var workspaceDirectory = Resolve(payload);

        if (workspaceDirectory is not null)
        {
            await agentStore.EndSessionAsync(AgentSessionHarness.Codex, payload.SessionId!, cancellationToken);
        }

        return CodexHookOutcome.Neutral;
    }

    public async Task<CodexNotifyOutcome> HandleNotifyAsync(
        CodexNotifyPayload payload, CancellationToken cancellationToken)
    {
        if (payload.Type != CodexNotifyPayload.AgentTurnComplete)
        {
            return CodexNotifyOutcome.Neutral;
        }

        if (string.IsNullOrWhiteSpace(payload.ThreadId) || string.IsNullOrWhiteSpace(payload.Cwd))
        {
            return CodexNotifyOutcome.Neutral;
        }

        var row = await ResolveOrStartRowAsync(
            new CodexHookPayload { SessionId = payload.ThreadId, Cwd = payload.Cwd }, cancellationToken);

        if (row is null)
        {
            return CodexNotifyOutcome.Neutral;
        }

        var digest = await BuildDigestAsync(row.Name, AgentSessionChannel.Gate, cancellationToken);

        if (digest is null)
        {
            return CodexNotifyOutcome.Neutral;
        }

        // Gate reservations remain claimed even if queueing fails.
        var queueResult = await queueClient.QueueAsync(payload.ThreadId, digest.Text, cancellationToken);

        return new CodexNotifyOutcome { Queued = queueResult == CodexQueueResult.Ok };
    }

    /// <summary>
    /// Resolves the current thread's row, minting one exactly like
    /// <see cref="HandleSessionStartAsync"/> without announcing it when none is bound yet.
    /// Returns null when the payload does not resolve, the session belongs to a deleted
    /// agent, or the mint itself is ignored for the same reason.
    /// </summary>
    private async Task<AgentRow?> ResolveOrStartRowAsync(
        CodexHookPayload payload, CancellationToken cancellationToken)
    {
        var workspaceDirectory = Resolve(payload);

        if (workspaceDirectory is null)
        {
            return null;
        }

        var row = await agentStore.FindBySessionAsync(AgentSessionHarness.Codex, payload.SessionId!, cancellationToken);

        if (row is not null)
        {
            if (row.IsDeleted)
            {
                return null;
            }

            await agentStore.TouchSessionAsync(AgentSessionHarness.Codex, payload.SessionId!, cancellationToken);

            return row;
        }

        var result = await agentStore.StartSessionAsync(
            BuildStartRequest(payload, workspaceDirectory), cancellationToken);

        return result.Kind == AgentSessionStartKind.Ignored ? null : result.Row;
    }

    /// <summary>
    /// Returns a digest or unread-count reminder for newly reserved messages in the
    /// current inbox batch, or null when that batch yields no reservations.
    /// </summary>
    private async Task<MailDigestResult?> BuildDigestAsync(
        string actor,
        string channel,
        CancellationToken cancellationToken)
    {
        var unread = await mailStore.QueryInboxAsync(
            new MailInboxFilter { Actor = actor, UnreadOnly = true, Limit = MailDigestPolicy.MaxMessages },
            cancellationToken);

        if (unread.Count == 0)
        {
            return null;
        }

        var messageIds = unread.Select(message => message.Id).ToList();
        var delivered = await ledger.FindDeliveredAsync(actor, messageIds, cancellationToken);
        var reserved = await ledger.ReserveAsync(
            actor, messageIds, channel, timeProvider.GetUtcNow(), cancellationToken);

        if (reserved.Count == 0)
        {
            return null;
        }

        var reservedIds = reserved.ToHashSet(StringComparer.Ordinal);
        var deliveredIds = delivered.ToHashSet(StringComparer.Ordinal);
        var messages = unread
            .Where(message => reservedIds.Contains(message.Id) && !deliveredIds.Contains(message.Id))
            .ToList();
        var unreadTotal = await mailStore.CountUnreadAsync(actor, cancellationToken);

        return new MailDigestResult(MailDigest.Render(actor, messages, unreadTotal));
    }

    /// <summary>
    /// Resolves the session's workspace directory, or null when the session id or cwd is
    /// missing, no workspace is found, or the payload and process workspaces differ.
    /// </summary>
    private string? Resolve(CodexHookPayload payload)
    {
        if (string.IsNullOrWhiteSpace(payload.Cwd) || string.IsNullOrWhiteSpace(payload.SessionId))
        {
            return null;
        }

        var payloadWorkspace = AgentWorkspace.Find(fileSystem, payload.Cwd);
        var processWorkspace = AgentWorkspace.Find(fileSystem, fileSystem.GetCurrentDirectory());

        return payloadWorkspace is null || payloadWorkspace != processWorkspace ? null : payloadWorkspace;
    }

    private AgentSessionStartRequest BuildStartRequest(CodexHookPayload payload, string workspaceDirectory)
    {
        // The Codex endpoint address is the thread id itself, which equals the session id.
        var (endpointKind, endpointAddr) = EndpointAddress.IsValid(payload.SessionId!)
            ? (AgentSessionEndpointKind.CodexThread, payload.SessionId!)
            : (AgentSessionEndpointKind.None, string.Empty);

        return new AgentSessionStartRequest
        {
            Harness = AgentSessionHarness.Codex,
            SessionId = payload.SessionId!,
            HarnessVersion = harnessVersionResolver.Resolve(payload.SessionId!),
            Cwd = payload.Cwd!,
            WorkspacePath = workspaceDirectory,
            EndpointKind = endpointKind,
            EndpointAddr = endpointAddr
        };
    }

    private sealed record MailDigestResult(string Text);
}
