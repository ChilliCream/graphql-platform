using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

internal sealed class CodexHookHandler(
    IFileSystem fileSystem,
    TimeProvider timeProvider,
    IAgentSessionRegistry sessionRegistry,
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
            ledger,
            mailStore,
            harnessVersionResolver,
            instanceIdProvider,
            globalConfigDirectoryProvider,
            queueClient)
    {
        ArgumentNullException.ThrowIfNull(environmentVariableProvider);
    }

    /// <summary>
    /// How many unread messages one nudge accounts for.
    /// </summary>
    public const int MaxDigestMessages = 10;

    public async Task<CodexHookOutcome> HandleSessionStartAsync(
        CodexHookPayload payload, bool dryRun, CancellationToken cancellationToken)
    {
        var resolved = await ResolveAsync(payload, cancellationToken);

        if (resolved is null)
        {
            return CodexHookOutcome.Neutral;
        }

        // The Codex endpoint address is the thread id itself, which equals
        // the session id. Unlike Claude's ancestor-derived peer name, Codex has no
        // live per-pid registry to read a "name" from.
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

        var harnessVersion = harnessVersionResolver.Resolve(resolved.Generation.SessionId, resolved.Generation.Pid);

        if (harnessVersion.Length > 0)
        {
            await sessionRegistry.RecordHarnessVersionAsync(resolved.Generation, harnessVersion, cancellationToken);
        }

        return new CodexHookOutcome
        {
            AdditionalContext = AgentActorContext.Format(session.AgentName!, session.Role)
        };
    }

    public async Task<CodexHookOutcome> HandleUserPromptSubmitAsync(
        CodexHookPayload payload, bool dryRun, CancellationToken cancellationToken)
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

        var actorContext = AgentActorContext.Format(row.AgentName, row.Role);
        var digest = await BuildDigestAsync(
            resolved.Generation, row.AgentName, AgentSessionChannel.Digest, cancellationToken);

        return new CodexHookOutcome
        {
            AdditionalContext = AgentActorContext.Combine(actorContext, digest)
        };
    }

    public async Task<CodexHookOutcome> HandleSessionEndAsync(
        CodexHookPayload payload, bool dryRun, CancellationToken cancellationToken)
    {
        var resolved = await ResolveAsync(payload, cancellationToken);

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

        // Reserve-then-emit (the plan's documented crash policy): the ledger
        // claim above already stands regardless of whether this call
        // actually succeeds, so a `codex queue` failure here suppresses this
        // digest on the gate channel from then on rather than retrying or
        // duplicating it - the message stays visible to a direct inbox read
        // and to the digest channel either way.
        var queueResult = await queueClient.QueueAsync(payload.ThreadId, digest, cancellationToken);

        return new CodexNotifyOutcome { Queued = queueResult == CodexQueueResult.Ok };
    }

    /// <summary>
    /// The unread-mail nudge for this session on <paramref name="channel"/>,
    /// or null when nothing is unread or every unread message was already
    /// announced there. It names the command that reads the mail; the mail
    /// itself stays in the inbox.
    /// </summary>
    private async Task<string?> BuildDigestAsync(
        AgentSessionGeneration generation,
        string actor,
        string channel,
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
            channel,
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
    /// addresses, or null when any fail-open condition applies: a missing or
    /// unresolvable cwd, no agent workspace at that cwd, no live Codex
    /// ancestor process (the ancestor walk in real usage; <paramref
    /// name="payload"/> names no session, or this
    /// process's own cwd resolving to a different workspace than the
    /// payload's cwd does. A missing session/thread id does not fail open by
    /// itself: with a resolvable process identity, the deterministic
    /// provisional session id for it (see
    /// <see cref="AgentSessionProvisionalSessionId"/>) is used instead.
    /// Mirrors <c>ClaudeHookHandler.ResolveAsync</c>.
    /// <para>
    /// For <see cref="HandleNotifyAsync"/> specifically, the Codex process
    /// may no longer be resolvable as a live
    /// ancestor by the time <c>notify</c> runs for a one-shot <c>codex exec</c>
    /// invocation. If
    /// it is not, this resolves to null and the gate fails open for that
    /// turn - an accepted, documented drop under the plan's guarantee
    /// statement ("notification is best effort... fail-open errors... can
    /// drop an individual attempt"), not a crash or an incorrect action.
    /// </para>
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

        // The event names its own session and delivery addresses the thread
        // id, so no process is involved. The schema still requires a
        // positive pid, and (harness, session_id) is what identifies a row,
        // so a fixed sentinel is enough until those columns are dropped.
        const int pid = 1;
        const string procStart = "0";

        var host = await instanceIdProvider.GetIdAsync(
            globalConfigDirectoryProvider.GetDirectory(), cancellationToken);

        if (string.IsNullOrWhiteSpace(payload.SessionId))
        {
            return null;
        }

        var generation = new AgentSessionGeneration(
            AgentSessionHarness.Codex, payload.SessionId, host, pid, procStart);

        return new ResolvedGeneration(generation, payloadWorkspace);
    }

    private sealed record ResolvedGeneration(AgentSessionGeneration Generation, string WorkspaceDirectory);
}
