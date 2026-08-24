using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

internal sealed class CopilotHookHandler(
    IFileSystem fileSystem,
    TimeProvider timeProvider,
    IAgentSessionRegistry sessionRegistry,
    ISessionDeliveryLedger ledger,
    IMailStore mailStore,
    IEnvironmentVariableProvider environmentVariables,
    IProcessInfoProvider processInfoProvider,
    ICopilotAncestorSessionResolver ancestorResolver,
    ICopilotHarnessVersionResolver harnessVersionResolver,
    INitroInstanceIdProvider instanceIdProvider,
    IGlobalConfigDirectoryProvider globalConfigDirectoryProvider) : ICopilotHookHandler
{
    /// <summary>
    /// The digest's per-call message cap, before the byte ceiling
    /// <see cref="ClaudeHookDigestFormatter"/> applies on top of it. Shared
    /// with Claude's and Codex's cap deliberately: the digest envelope, cap,
    /// and byte ceiling are harness-neutral by design.
    /// </summary>
    public const int MaxDigestMessages = 10;

    public async Task<CopilotHookOutcome> HandleSessionStartAsync(
        CopilotHookPayload payload, bool dryRun, CancellationToken cancellationToken)
    {
        var resolved = await ResolveAsync(payload, dryRun, cancellationToken);

        if (resolved is null)
        {
            return CopilotHookOutcome.Neutral;
        }

        var envActor = MailActor.TryResolve(null, environmentVariables);

        var record = await sessionRegistry.StartAsync(
            resolved.Generation,
            payload.Cwd!,
            resolved.WorkspaceDirectory,
            AgentSessionEndpointKind.None,
            string.Empty,
            envActor,
            cancellationToken);

        var harnessVersion = harnessVersionResolver.Resolve(resolved.Generation.SessionId, resolved.Generation.Pid);

        if (harnessVersion.Length > 0)
        {
            await sessionRegistry.RecordHarnessVersionAsync(resolved.Generation, harnessVersion, cancellationToken);
        }

        if (record.BindingKind == AgentSessionBindingKind.None || record.AgentName is null)
        {
            return CopilotHookOutcome.Neutral;
        }

        // sessionStart is the one Copilot hook event S5 (redo) live-verified
        // as able to carry additionalContext into the model's context, so
        // the initial unread-mail digest rides here rather than on the
        // turn-boundary event Claude/Codex use for the same content (see
        // HandleUserPromptSubmitAsync).
        var digest = await BuildDigestAsync(resolved.Generation, record.AgentName, cancellationToken);

        return digest is null ? CopilotHookOutcome.Neutral : new CopilotHookOutcome { AdditionalContext = digest };
    }

    public async Task<CopilotHookOutcome> HandleUserPromptSubmitAsync(
        CopilotHookPayload payload, bool dryRun, CancellationToken cancellationToken)
    {
        // No digest capability rides here (S5's live-verified finding: only
        // sessionStart can carry additionalContext for Copilot), but a
        // resolved generation is still a heartbeat-eligible lifecycle event.
        var resolved = await ResolveAsync(payload, dryRun, cancellationToken);

        if (resolved is not null)
        {
            await sessionRegistry.TouchAsync(resolved.Generation, cancellationToken);
        }

        return CopilotHookOutcome.Neutral;
    }

    public async Task<CopilotHookOutcome> HandleSessionEndAsync(
        CopilotHookPayload payload, bool dryRun, CancellationToken cancellationToken)
    {
        var resolved = await ResolveAsync(payload, dryRun, cancellationToken);

        if (resolved is not null)
        {
            await sessionRegistry.EndAsync(resolved.Generation, cancellationToken);
        }

        return CopilotHookOutcome.Neutral;
    }

    private async Task<string?> BuildDigestAsync(
        AgentSessionGeneration generation, string actor, CancellationToken cancellationToken)
    {
        var unread = await mailStore.QueryInboxAsync(
            new MailInboxFilter { Actor = actor, UnreadOnly = true, Limit = MaxDigestMessages },
            cancellationToken);

        if (unread.Count == 0)
        {
            return null;
        }

        var reserved = await ledger.ReserveAsync(
            generation.Harness,
            generation.SessionId,
            unread.Select(m => m.Id).ToList(),
            AgentSessionChannel.Digest,
            timeProvider.GetUtcNow(),
            cancellationToken);

        if (reserved.Count == 0)
        {
            return null;
        }

        var reservedIds = reserved.ToHashSet(StringComparer.Ordinal);

        // `unread` is already newest-first (IMailStore.QueryInboxAsync);
        // filtering preserves that order.
        var newEntries = unread
            .Where(m => reservedIds.Contains(m.Id))
            .Select(m => (m.Id, m.Sender))
            .ToList();

        var totalUnread = await mailStore.CountUnreadAsync(actor, cancellationToken);

        return ClaudeHookDigestFormatter.Format(totalUnread, newEntries);
    }

    /// <summary>
    /// Resolves the generation identity and workspace an event's payload
    /// addresses, or null when any fail-open condition applies: a missing or
    /// unresolvable session id or cwd, no agent workspace at that cwd, no
    /// live Copilot ancestor process (the ancestor walk in real usage;
    /// <paramref name="dryRun"/> pins a fixed sentinel identity instead), or
    /// this process's own cwd resolving to a different workspace than the
    /// payload's cwd does. Mirrors <c>CodexHookHandler.ResolveAsync</c>.
    /// </summary>
    private async Task<ResolvedGeneration?> ResolveAsync(
        CopilotHookPayload payload, bool dryRun, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(payload.SessionId) || string.IsNullOrWhiteSpace(payload.Cwd))
        {
            return null;
        }

        var payloadWorkspace = AgentWorkspace.Find(fileSystem, payload.Cwd);
        var processWorkspace = AgentWorkspace.Find(fileSystem, fileSystem.GetCurrentDirectory());

        if (payloadWorkspace is null || payloadWorkspace != processWorkspace)
        {
            return null;
        }

        int pid;
        DateTimeOffset procStart;

        if (dryRun)
        {
            // Pid 1, not 0: the agent_sessions schema's `pid > 0` CHECK
            // rejects zero. Any fixed positive pid is exactly as safe a
            // sentinel here, since pairing it with the epoch proc_start below
            // is what actually makes collision with a real session's
            // generation impossible.
            pid = 1;
            procStart = DateTimeOffset.UnixEpoch;
        }
        else
        {
            var ancestor = ancestorResolver.Resolve();

            if (ancestor is null)
            {
                return null;
            }

            var resolvedStart = processInfoProvider.GetStartTime(ancestor.Pid);

            if (resolvedStart is null)
            {
                return null;
            }

            pid = ancestor.Pid;
            procStart = resolvedStart.Value;
        }

        var host = await instanceIdProvider.GetIdAsync(
            globalConfigDirectoryProvider.GetDirectory(), cancellationToken);

        var generation = new AgentSessionGeneration(
            AgentSessionHarness.Copilot, payload.SessionId, host, pid, procStart);

        return new ResolvedGeneration(generation, payloadWorkspace);
    }

    private sealed record ResolvedGeneration(AgentSessionGeneration Generation, string WorkspaceDirectory);
}
