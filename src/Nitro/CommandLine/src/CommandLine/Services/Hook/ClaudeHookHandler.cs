using System.Text;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

internal sealed class ClaudeHookHandler(
    IFileSystem fileSystem,
    TimeProvider timeProvider,
    IAgentSessionRegistry sessionRegistry,
    ISessionDeliveryLedger ledger,
    IMailStore mailStore,
    IProcessInfoProvider processInfoProvider,
    IClaudeSessionFileReader sessionFileReader,
    IClaudeHarnessVersionResolver harnessVersionResolver,
    INitroInstanceIdProvider instanceIdProvider,
    IGlobalConfigDirectoryProvider globalConfigDirectoryProvider) : IClaudeHookHandler
{
    public ClaudeHookHandler(
        IFileSystem fileSystem,
        TimeProvider timeProvider,
        IAgentSessionRegistry sessionRegistry,
        ISessionDeliveryLedger ledger,
        IMailStore mailStore,
        IEnvironmentVariableProvider environmentVariableProvider,
        IProcessInfoProvider processInfoProvider,
        IClaudeSessionFileReader sessionFileReader,
        IClaudeHarnessVersionResolver harnessVersionResolver,
        INitroInstanceIdProvider instanceIdProvider,
        IGlobalConfigDirectoryProvider globalConfigDirectoryProvider)
        : this(
            fileSystem,
            timeProvider,
            sessionRegistry,
            ledger,
            mailStore,
            processInfoProvider,
            sessionFileReader,
            harnessVersionResolver,
            instanceIdProvider,
            globalConfigDirectoryProvider)
    {
        ArgumentNullException.ThrowIfNull(environmentVariableProvider);
    }

    /// <summary>
    /// The per-turn Stop gate block budget, reset on <c>UserPromptSubmit</c>
    /// so normal mail volume can never silently disable the gate for the
    /// rest of the conversation.
    /// </summary>
    public const int MaxBlocksPerTurn = 3;

    /// <summary>
    /// How many unread messages one nudge accounts for.
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

        var (endpointKind, endpointAddr) = resolved.EndpointName is { Length: > 0 } name
            && EndpointAddress.IsValid(name)
                ? (AgentSessionEndpointKind.ClaudePeer, name)
                : (AgentSessionEndpointKind.None, string.Empty);

        var session = await sessionRegistry.StartAsync(
            resolved.Generation,
            payload.Cwd!,
            resolved.WorkspaceDirectory,
            endpointKind,
            endpointAddr,
            envActor: null,
            cancellationToken);

        var harnessVersion = harnessVersionResolver.Resolve(resolved.Generation.Pid);

        if (harnessVersion.Length > 0)
        {
            await sessionRegistry.RecordHarnessVersionAsync(resolved.Generation, harnessVersion, cancellationToken);
        }

        return new ClaudeHookOutcome
        {
            AdditionalContext = AgentActorContext.Format(session.AgentName!, session.Role)
        };
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

        if (row is null)
        {
            var (endpointKind, endpointAddr) = resolved.EndpointName is { Length: > 0 } name
                && EndpointAddress.IsValid(name)
                    ? (AgentSessionEndpointKind.ClaudePeer, name)
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
            return ClaudeHookOutcome.Neutral;
        }

        await sessionRegistry.ResetBlockBudgetAsync(resolved.Generation, cancellationToken);

        var actorContext = AgentActorContext.Format(row.AgentName, row.Role);
        var digest = await BuildDigestAsync(resolved.Generation, row.AgentName, cancellationToken);

        return new ClaudeHookOutcome
        {
            AdditionalContext = AgentActorContext.Combine(actorContext, digest)
        };
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

        await sessionRegistry.TouchAsync(resolved.Generation, cancellationToken);

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
            new MailInboxFilter { Actor = row.AgentName, UnreadOnly = true, Limit = MaxDigestMessages },
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

        var incremented = await sessionRegistry.IncrementBlockBudgetAsync(resolved.Generation, cancellationToken);

        if (incremented is null)
        {
            // The row was deleted (SessionEnd) between the FindByGenerationAsync
            // above and this increment: nothing left to gate on behalf of.
            return ClaudeHookOutcome.Neutral;
        }

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

    /// <summary>
    /// The unread-mail nudge for this session, or null when nothing is
    /// unread or every unread message was already announced to it. It names
    /// the command that reads the mail; the mail itself stays in the inbox.
    /// </summary>
    private async Task<string?> BuildDigestAsync(
        AgentSessionGeneration generation,
        string actor,
        CancellationToken cancellationToken)
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

        return MailNudgeText.Format(actor, await mailStore.CountUnreadAsync(actor, cancellationToken));
    }

    /// <summary>
    /// Resolves the generation identity and workspace an event's payload
    /// addresses, or null when any fail-open condition applies: a missing
    /// or unresolvable cwd, no agent workspace at that cwd, no live process
    /// identity (the ancestor walk in real usage; <paramref name="dryRun"/>
    /// pins a fixed sentinel identity instead), or this process's own cwd
    /// resolving to a different workspace than the payload's cwd does
    /// (mirrors <c>SelfClaimAsync</c>'s same check). A missing
    /// <see cref="ClaudeHookPayload.SessionId"/> does not fail open by
    /// itself: with a resolvable process identity, the deterministic
    /// provisional session id for it (see
    /// <see cref="AgentSessionProvisionalSessionId"/>) is used instead.
    /// </summary>
    private async Task<ResolvedGeneration?> ResolveAsync(
        ClaudeHookPayload payload, bool dryRun, CancellationToken cancellationToken)
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

        int pid;
        string procStart;
        string? endpointName;

        if (dryRun)
        {
            // Pid 1, not 0: the agent_sessions schema's `pid > 0` CHECK
            // rejects zero. Any fixed positive pid is exactly as safe a
            // sentinel here, since pairing it with the "0" proc_start
            // below (no live process ever reports 0 start ticks) is what
            // actually makes collision with a real session's generation
            // impossible.
            pid = 1;
            procStart = "0";
            endpointName = null;
        }
        else
        {
            // The event names its own session, so the session file that
            // carries that id identifies the process exactly. Nothing is
            // inferred from the process tree.
            var session = sessionFileReader.Find(payload.SessionId ?? string.Empty);

            if (session is null)
            {
                return null;
            }

            var resolvedStart = processInfoProvider.GetStartTicks(session.Pid);

            if (resolvedStart is null)
            {
                return null;
            }

            pid = session.Pid;
            procStart = resolvedStart;
            endpointName = session.Name;
        }

        var host = await instanceIdProvider.GetIdAsync(
            globalConfigDirectoryProvider.GetDirectory(), cancellationToken);

        if (string.IsNullOrWhiteSpace(payload.SessionId))
        {
            return null;
        }

        var generation = new AgentSessionGeneration(
            AgentSessionHarness.ClaudeCode, payload.SessionId, host, pid, procStart);

        return new ResolvedGeneration(generation, payloadWorkspace, endpointName);
    }

    private sealed record ResolvedGeneration(
        AgentSessionGeneration Generation, string WorkspaceDirectory, string? EndpointName);
}
