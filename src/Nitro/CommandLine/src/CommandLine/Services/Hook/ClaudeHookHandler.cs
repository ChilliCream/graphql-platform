using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

internal sealed class ClaudeHookHandler(
    IFileSystem fileSystem,
    TimeProvider timeProvider,
    IAgentSessionRegistry sessionRegistry,
    ISessionDeliveryLedger ledger,
    IMailStore mailStore,
    IEnvironmentVariableProvider environmentVariables,
    IProcessInfoProvider processInfoProvider,
    IClaudeAncestorSessionResolver ancestorResolver,
    INitroInstanceIdProvider instanceIdProvider,
    IGlobalConfigDirectoryProvider globalConfigDirectoryProvider) : IClaudeHookHandler
{
    /// <summary>
    /// The per-turn Stop gate block budget, reset on <c>UserPromptSubmit</c>
    /// so normal mail volume can never silently disable the gate for the
    /// rest of the conversation.
    /// </summary>
    public const int MaxBlocksPerTurn = 3;

    /// <summary>
    /// The digest's per-call message cap, before the byte ceiling
    /// <see cref="ClaudeHookDigestFormatter"/> applies on top of it.
    /// </summary>
    public const int MaxDigestMessages = 10;

    private const string BlockReason =
        "Unread nitro mail is waiting. Read it with `nitro agent mail inbox` "
        + "before ending this turn, or ignore this once if it is not actionable right now.";

    public async Task<ClaudeHookOutcome> HandleSessionStartAsync(
        ClaudeHookPayload payload, bool dryRun, CancellationToken cancellationToken)
    {
        var resolved = await ResolveAsync(payload, dryRun, cancellationToken);

        if (resolved is null)
        {
            return ClaudeHookOutcome.Neutral;
        }

        var envActor = MailActor.TryResolve(null, environmentVariables);

        var (endpointKind, endpointAddr) = resolved.EndpointName is { Length: > 0 } name
            && EndpointAddress.IsValid(name)
                ? (AgentSessionEndpointKind.ClaudePeer, name)
                : (AgentSessionEndpointKind.None, string.Empty);

        await sessionRegistry.StartAsync(
            resolved.Generation,
            payload.Cwd!,
            resolved.WorkspaceDirectory,
            endpointKind,
            endpointAddr,
            envActor,
            cancellationToken);

        return ClaudeHookOutcome.Neutral;
    }

    public async Task<ClaudeHookOutcome> HandleUserPromptSubmitAsync(
        ClaudeHookPayload payload, bool dryRun, CancellationToken cancellationToken)
    {
        var resolved = await ResolveAsync(payload, dryRun, cancellationToken);

        if (resolved is null)
        {
            return ClaudeHookOutcome.Neutral;
        }

        var row = await sessionRegistry.FindByGenerationAsync(resolved.Generation, cancellationToken);

        if (row is null || row.BindingKind == AgentSessionBindingKind.None || row.AgentName is null)
        {
            return ClaudeHookOutcome.Neutral;
        }

        await sessionRegistry.ResetBlockBudgetAsync(resolved.Generation, cancellationToken);

        var digest = await BuildDigestAsync(resolved.Generation, row.AgentName, cancellationToken);

        return digest is null ? ClaudeHookOutcome.Neutral : new ClaudeHookOutcome { AdditionalContext = digest };
    }

    public async Task<ClaudeHookOutcome> HandleStopAsync(
        ClaudeHookPayload payload, bool dryRun, CancellationToken cancellationToken)
    {
        if (payload.StopHookActive)
        {
            return ClaudeHookOutcome.Neutral;
        }

        var resolved = await ResolveAsync(payload, dryRun, cancellationToken);

        if (resolved is null)
        {
            return ClaudeHookOutcome.Neutral;
        }

        var row = await sessionRegistry.FindByGenerationAsync(resolved.Generation, cancellationToken);

        if (row is null || row.BindingKind == AgentSessionBindingKind.None || row.AgentName is null)
        {
            return ClaudeHookOutcome.Neutral;
        }

        if (row.BlockBudgetUsed >= MaxBlocksPerTurn)
        {
            // Over budget: candidates are left unreserved so a fresh
            // UserPromptSubmit budget reset can still gate them later,
            // instead of permanently marking them delivered on the gate
            // channel while never actually blocking for them.
            return ClaudeHookOutcome.Neutral;
        }

        var unread = await mailStore.QueryInboxAsync(
            new MailInboxFilter { Actor = row.AgentName, UnreadOnly = true },
            cancellationToken);

        if (unread.Count == 0)
        {
            return ClaudeHookOutcome.Neutral;
        }

        var reserved = await ledger.ReserveAsync(
            resolved.Generation.Harness,
            resolved.Generation.SessionId,
            unread.Select(m => m.Id).ToList(),
            AgentSessionChannel.Gate,
            timeProvider.GetUtcNow(),
            cancellationToken);

        if (reserved.Count == 0)
        {
            return ClaudeHookOutcome.Neutral;
        }

        await sessionRegistry.IncrementBlockBudgetAsync(resolved.Generation, cancellationToken);

        return new ClaudeHookOutcome { Block = true, BlockReason = BlockReason };
    }

    public async Task<ClaudeHookOutcome> HandleSessionEndAsync(
        ClaudeHookPayload payload, bool dryRun, CancellationToken cancellationToken)
    {
        var resolved = await ResolveAsync(payload, dryRun, cancellationToken);

        if (resolved is not null)
        {
            await sessionRegistry.EndAsync(resolved.Generation, cancellationToken);
        }

        return ClaudeHookOutcome.Neutral;
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
    /// addresses, or null when any fail-open condition applies: a missing
    /// or unresolvable session id or cwd, no agent workspace at that cwd, no
    /// live process identity (the ancestor walk in real usage, this
    /// process's own identity in <paramref name="dryRun"/>), or this
    /// process's own cwd resolving to a different workspace than the
    /// payload's cwd does (mirrors <c>SelfClaimAsync</c>'s same check).
    /// </summary>
    private async Task<ResolvedGeneration?> ResolveAsync(
        ClaudeHookPayload payload, bool dryRun, CancellationToken cancellationToken)
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
        string? endpointName;

        if (dryRun)
        {
            pid = Environment.ProcessId;
            endpointName = null;
        }
        else
        {
            var ancestor = ancestorResolver.Resolve();

            if (ancestor is null)
            {
                return null;
            }

            pid = ancestor.Pid;
            endpointName = ancestor.Name;
        }

        var procStart = processInfoProvider.GetStartTime(pid);

        if (procStart is null)
        {
            return null;
        }

        var host = await instanceIdProvider.GetIdAsync(
            globalConfigDirectoryProvider.GetDirectory(), cancellationToken);

        var generation = new AgentSessionGeneration(
            AgentSessionHarness.ClaudeCode, payload.SessionId, host, pid, procStart.Value);

        return new ResolvedGeneration(generation, payloadWorkspace, endpointName);
    }

    private sealed record ResolvedGeneration(
        AgentSessionGeneration Generation, string WorkspaceDirectory, string? EndpointName);
}
