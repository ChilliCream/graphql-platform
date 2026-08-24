using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

internal sealed class CodexHookHandler(
    IFileSystem fileSystem,
    TimeProvider timeProvider,
    IAgentSessionRegistry sessionRegistry,
    ISessionDeliveryLedger ledger,
    IMailStore mailStore,
    IEnvironmentVariableProvider environmentVariables,
    IProcessInfoProvider processInfoProvider,
    ICodexAncestorSessionResolver ancestorResolver,
    ICodexHarnessVersionResolver harnessVersionResolver,
    INitroInstanceIdProvider instanceIdProvider,
    IGlobalConfigDirectoryProvider globalConfigDirectoryProvider,
    ICodexQueueClient queueClient) : ICodexHookHandler
{
    /// <summary>
    /// The digest's per-call message cap, before the byte ceiling
    /// <see cref="ClaudeHookDigestFormatter"/> applies on top of it. Shared
    /// with Claude's cap deliberately: the digest envelope, cap, and byte
    /// ceiling are harness-neutral by design (Layer A's "Digest content"
    /// section makes no per-harness distinction), so this reuses
    /// <see cref="ClaudeHookDigestFormatter"/> directly rather than
    /// duplicating its byte-budgeting logic.
    /// </summary>
    public const int MaxDigestMessages = 10;

    public async Task<CodexHookOutcome> HandleSessionStartAsync(
        CodexHookPayload payload, bool dryRun, CancellationToken cancellationToken)
    {
        var resolved = await ResolveAsync(payload, dryRun, cancellationToken);

        if (resolved is null)
        {
            return CodexHookOutcome.Neutral;
        }

        // No explicit actor (NITRO_MAIL_ACTOR/NITRO_TASK_ACTOR) configured: a
        // deterministic, harness-namespaced actor generated from the full
        // session id keeps this live session mail-addressable rather than
        // settling as an unbound presence row.
        var envActor = MailActor.TryResolve(null, environmentVariables)
            ?? AgentSessionActorNaming.Generate(resolved.Generation.Harness, resolved.Generation.SessionId);

        // The Codex endpoint address is the thread id itself (== session id,
        // spike S4), unlike Claude's ancestor-derived peer name: Codex has no
        // live per-pid registry to read a "name" from.
        var (endpointKind, endpointAddr) = EndpointAddress.IsValid(resolved.Generation.SessionId)
            ? (AgentSessionEndpointKind.CodexThread, resolved.Generation.SessionId)
            : (AgentSessionEndpointKind.None, string.Empty);

        await sessionRegistry.StartAsync(
            resolved.Generation,
            payload.Cwd!,
            resolved.WorkspaceDirectory,
            endpointKind,
            endpointAddr,
            envActor,
            cancellationToken);

        var harnessVersion = harnessVersionResolver.Resolve(resolved.Generation.SessionId, resolved.Generation.Pid);

        if (harnessVersion.Length > 0)
        {
            await sessionRegistry.RecordHarnessVersionAsync(resolved.Generation, harnessVersion, cancellationToken);
        }

        return CodexHookOutcome.Neutral;
    }

    public async Task<CodexHookOutcome> HandleUserPromptSubmitAsync(
        CodexHookPayload payload, bool dryRun, CancellationToken cancellationToken)
    {
        var resolved = await ResolveAsync(payload, dryRun, cancellationToken);

        if (resolved is null)
        {
            return CodexHookOutcome.Neutral;
        }

        await sessionRegistry.TouchAsync(resolved.Generation, cancellationToken);

        var row = await sessionRegistry.FindByGenerationAsync(resolved.Generation, cancellationToken);

        if (row is null || row.BindingKind == AgentSessionBindingKind.None || row.AgentName is null)
        {
            return CodexHookOutcome.Neutral;
        }

        var digest = await BuildDigestAsync(
            resolved.Generation, row.AgentName, AgentSessionChannel.Digest, cancellationToken);

        return digest is null ? CodexHookOutcome.Neutral : new CodexHookOutcome { AdditionalContext = digest };
    }

    public async Task<CodexHookOutcome> HandleSessionEndAsync(
        CodexHookPayload payload, bool dryRun, CancellationToken cancellationToken)
    {
        var resolved = await ResolveAsync(payload, dryRun, cancellationToken);

        if (resolved is not null)
        {
            await sessionRegistry.EndAsync(resolved.Generation, cancellationToken);
        }

        return CodexHookOutcome.Neutral;
    }

    public async Task<CodexNotifyOutcome> HandleNotifyAsync(
        CodexNotifyPayload payload, bool dryRun, CancellationToken cancellationToken)
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
            new CodexHookPayload { SessionId = payload.ThreadId, Cwd = payload.Cwd }, dryRun, cancellationToken);

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

        // Reserve-then-emit (the plan's documented crash policy): the ledger
        // claim above already stands regardless of whether this call
        // actually succeeds, so a `codex queue` failure here suppresses this
        // digest on the gate channel from then on rather than retrying or
        // duplicating it - the message stays visible to a direct inbox read
        // and to the digest channel either way.
        var queueResult = await queueClient.QueueAsync(payload.ThreadId, digest, cancellationToken);

        return new CodexNotifyOutcome { Queued = queueResult == CodexQueueResult.Ok };
    }

    private async Task<string?> BuildDigestAsync(
        AgentSessionGeneration generation, string actor, string channel, CancellationToken cancellationToken)
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
            channel,
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
    /// unresolvable session/thread id or cwd, no agent workspace at that cwd,
    /// no live Codex ancestor process (the ancestor walk in real usage;
    /// <paramref name="dryRun"/> pins a fixed sentinel identity instead), or
    /// this process's own cwd resolving to a different workspace than the
    /// payload's cwd does. Mirrors <c>ClaudeHookHandler.ResolveAsync</c>.
    /// <para>
    /// For <see cref="HandleNotifyAsync"/> specifically: spike S2 could not
    /// determine whether the Codex process is still resolvable as a live
    /// ancestor by the time <c>notify</c> runs for a one-shot <c>codex exec</c>
    /// invocation (see perles-net-k3j.2's "not determined" list). If
    /// it is not, this resolves to null and the gate fails open for that
    /// turn - an accepted, documented drop under the plan's guarantee
    /// statement ("notification is best effort... fail-open errors... can
    /// drop an individual attempt"), not a crash or an incorrect action.
    /// </para>
    /// </summary>
    private async Task<ResolvedGeneration?> ResolveAsync(
        CodexHookPayload payload, bool dryRun, CancellationToken cancellationToken)
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
        string procStart;

        if (dryRun)
        {
            // Pid 1, not 0: the agent_sessions schema's `pid > 0` CHECK
            // rejects zero. Any fixed positive pid is exactly as safe a
            // sentinel here, since pairing it with the "0" proc_start below
            // (no live process ever reports 0 start ticks) is what actually
            // makes collision with a real session's generation impossible.
            pid = 1;
            procStart = "0";
        }
        else
        {
            var ancestor = ancestorResolver.Resolve();

            if (ancestor is null)
            {
                return null;
            }

            var resolvedStart = processInfoProvider.GetStartTicks(ancestor.Pid);

            if (resolvedStart is null)
            {
                return null;
            }

            pid = ancestor.Pid;
            procStart = resolvedStart;
        }

        var host = await instanceIdProvider.GetIdAsync(
            globalConfigDirectoryProvider.GetDirectory(), cancellationToken);

        var generation = new AgentSessionGeneration(AgentSessionHarness.Codex, payload.SessionId, host, pid, procStart);

        return new ResolvedGeneration(generation, payloadWorkspace);
    }

    private sealed record ResolvedGeneration(AgentSessionGeneration Generation, string WorkspaceDirectory);
}
