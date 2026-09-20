using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

internal sealed class CodexHookHandler(
    IFileSystem fileSystem,
    TimeProvider timeProvider,
    IAgentSessionRegistry sessionRegistry,
    IAgentRegistry agentRegistry,
    ISessionDeliveryLedger ledger,
    IMailStore mailStore,
    ICodexHarnessVersionResolver harnessVersionResolver,
    INitroInstanceIdProvider instanceIdProvider,
    IGlobalConfigDirectoryProvider globalConfigDirectoryProvider,
    ICodexQueueClient queueClient) : ICodexHookHandler
{
    public CodexHookHandler(
        IFileSystem fileSystem,
        TimeProvider timeProvider,
        IAgentSessionRegistry sessionRegistry,
        IAgentRegistry agentRegistry,
        ISessionDeliveryLedger ledger,
        IMailStore mailStore,
        IEnvironmentVariableProvider environmentVariableProvider,
        ICodexHarnessVersionResolver harnessVersionResolver,
        INitroInstanceIdProvider instanceIdProvider,
        IGlobalConfigDirectoryProvider globalConfigDirectoryProvider,
        ICodexQueueClient queueClient)
        : this(
            fileSystem,
            timeProvider,
            sessionRegistry,
            agentRegistry,
            ledger,
            mailStore,
            harnessVersionResolver,
            instanceIdProvider,
            globalConfigDirectoryProvider,
            queueClient)
    {
        ArgumentNullException.ThrowIfNull(environmentVariableProvider);
    }

    public async Task<CodexHookOutcome> HandleSessionStartAsync(
        CodexHookPayload payload, CancellationToken cancellationToken)
    {
        var resolved = await ResolveAsync(payload, cancellationToken);

        if (resolved is null)
        {
            return CodexHookOutcome.Neutral;
        }

        // The Codex endpoint address is the thread id itself, which equals the
        // session id.
        var (endpointKind, endpointAddr) = EndpointAddress.IsValid(resolved.Generation.SessionId)
            ? (AgentSessionEndpointKind.CodexThread, resolved.Generation.SessionId)
            : (AgentSessionEndpointKind.None, string.Empty);

        var session = await sessionRegistry.StartAsync(
            resolved.Generation,
            payload.Cwd!,
            resolved.WorkspaceDirectory,
            endpointKind,
            endpointAddr,
            envActor: null,
            cancellationToken);

        var harnessVersion = harnessVersionResolver.Resolve(resolved.Generation.SessionId);

        if (harnessVersion.Length > 0)
        {
            await sessionRegistry.RecordHarnessVersionAsync(resolved.Generation, harnessVersion, cancellationToken);
        }

        var role = await AgentEffectiveRole.ResolveAsync(
            session.Role, session.AgentName!, agentRegistry, cancellationToken);

        return new CodexHookOutcome
        {
            AdditionalContext = AgentActorContext.Format(session.AgentName!, role)
        };
    }

    public async Task<CodexHookOutcome> HandleUserPromptSubmitAsync(
        CodexHookPayload payload, CancellationToken cancellationToken)
    {
        var resolved = await ResolveAsync(payload, cancellationToken);

        if (resolved is null)
        {
            return CodexHookOutcome.Neutral;
        }

        var row = await sessionRegistry.FindByGenerationAsync(resolved.Generation, cancellationToken);

        if (row is null)
        {
            var (endpointKind, endpointAddr) = EndpointAddress.IsValid(resolved.Generation.SessionId)
                ? (AgentSessionEndpointKind.CodexThread, resolved.Generation.SessionId)
                : (AgentSessionEndpointKind.None, string.Empty);
            row = await sessionRegistry.StartAsync(
                resolved.Generation,
                payload.Cwd!,
                resolved.WorkspaceDirectory,
                endpointKind,
                endpointAddr,
                envActor: null,
                cancellationToken);
        }
        else
        {
            await sessionRegistry.TouchAsync(resolved.Generation, cancellationToken);
        }

        if (row.BindingKind == AgentSessionBindingKind.None || row.AgentName is null)
        {
            return CodexHookOutcome.Neutral;
        }

        var digest = await BuildDigestAsync(
            resolved.Generation, row.AgentName, AgentSessionChannel.Digest, cancellationToken);

        return digest is null
            ? CodexHookOutcome.Neutral
            : new CodexHookOutcome { AdditionalContext = digest.Text };
    }

    public async Task<CodexHookOutcome> HandleSessionEndAsync(
        CodexHookPayload payload, CancellationToken cancellationToken)
    {
        var resolved = await ResolveAsync(payload, cancellationToken);

        if (resolved is not null)
        {
            await sessionRegistry.EndAsync(resolved.Generation, cancellationToken);
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

        var resolved = await ResolveAsync(
            new CodexHookPayload { SessionId = payload.ThreadId, Cwd = payload.Cwd }, cancellationToken);

        if (resolved is null)
        {
            return CodexNotifyOutcome.Neutral;
        }

        await sessionRegistry.TouchAsync(resolved.Generation, cancellationToken);

        var row = await sessionRegistry.FindByGenerationAsync(resolved.Generation, cancellationToken);

        if (row is null || row.BindingKind == AgentSessionBindingKind.None || row.AgentName is null)
        {
            return CodexNotifyOutcome.Neutral;
        }

        var digest = await BuildDigestAsync(
            resolved.Generation, row.AgentName, AgentSessionChannel.Gate, cancellationToken);

        if (digest is null)
        {
            return CodexNotifyOutcome.Neutral;
        }

        // Gate reservations remain claimed even if queueing fails.
        var queueResult = await queueClient.QueueAsync(payload.ThreadId, digest.Text, cancellationToken);

        return new CodexNotifyOutcome { Queued = queueResult == CodexQueueResult.Ok };
    }

    /// <summary>
    /// Returns a digest or unread-count reminder for newly reserved messages in the
    /// current inbox batch, or null when that batch yields no reservations.
    /// </summary>
    private async Task<MailDigestResult?> BuildDigestAsync(
        AgentSessionGeneration generation,
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
        var delivered = await ledger.FindDeliveredAsync(generation, messageIds, cancellationToken);
        var reserved = await ledger.ReserveAsync(
            generation.Harness,
            generation.SessionId,
            messageIds,
            channel,
            timeProvider.GetUtcNow(),
            cancellationToken);

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

        return new MailDigestResult(
            MailDigest.Render(actor, messages, unreadTotal));
    }

    /// <summary>
    /// Resolves the session identity and workspace, or null when the session id or cwd
    /// is missing, no workspace is found, or the payload and process workspaces differ.
    /// </summary>
    private async Task<ResolvedGeneration?> ResolveAsync(
        CodexHookPayload payload, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(payload.Cwd))
        {
            return null;
        }

        var payloadWorkspace = AgentWorkspace.Find(fileSystem, payload.Cwd);
        var processWorkspace = AgentWorkspace.Find(fileSystem, fileSystem.GetCurrentDirectory());

        if (payloadWorkspace is null || payloadWorkspace != processWorkspace)
        {
            return null;
        }

        var host = await instanceIdProvider.GetIdAsync(
            globalConfigDirectoryProvider.GetDirectory(), cancellationToken);

        if (string.IsNullOrWhiteSpace(payload.SessionId))
        {
            return null;
        }

        var generation = new AgentSessionGeneration(
            AgentSessionHarness.Codex, payload.SessionId, host);

        return new ResolvedGeneration(generation, payloadWorkspace);
    }

    private sealed record ResolvedGeneration(AgentSessionGeneration Generation, string WorkspaceDirectory);

    private sealed record MailDigestResult(string Text);
}
