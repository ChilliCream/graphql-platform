using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Implements the opencode session lifecycle and prompt-context delivery.
/// </summary>
internal sealed class OpencodeHookHandler(
    IFileSystem fileSystem,
    TimeProvider timeProvider,
    IAgentSessionRegistry sessionRegistry,
    ISessionDeliveryLedger ledger,
    IMailStore mailStore,
    IEnvironmentVariableProvider environmentVariableProvider,
    INitroInstanceIdProvider instanceIdProvider,
    IGlobalConfigDirectoryProvider globalConfigDirectoryProvider) : IOpencodeHookHandler
{
    /// <summary>
    /// The largest unread-message batch represented by one delivery nudge.
    /// </summary>
    public const int MaxDigestMessages = 10;

    public async Task<OpencodeHookOutcome> HandleSessionCreatedAsync(
        OpencodeHookPayload payload, bool dryRun, CancellationToken cancellationToken)
    {
        // dryRun has no effect here: unlike ClaudeHookHandler, opencode has
        // no session-file side channel to skip in a dry run. Retained for
        // interface parity with the Claude and Codex hook handlers.
        _ = dryRun;

        var resolved = await ResolveAsync(payload, cancellationToken);

        if (resolved is null)
        {
            return OpencodeHookOutcome.Neutral;
        }

        await sessionRegistry.StartAsync(
            resolved.Generation,
            resolved.Cwd,
            resolved.WorkspaceDirectory,
            AgentSessionEndpointKind.OpencodeServer,
            payload.ServerUrl!,
            payload.ServerPassword,
            envActor: null,
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(payload.HarnessVersion))
        {
            await sessionRegistry.RecordHarnessVersionAsync(
                resolved.Generation, payload.HarnessVersion, cancellationToken);
        }

        // Arms the first-prompt announcement and the idle-push gate so a
        // session that goes idle before its first chat message still pushes
        // once, exactly as a session with prior chat activity would.
        await sessionRegistry.ArmAnnouncementAsync(resolved.Generation, cancellationToken);
        await sessionRegistry.RearmIdlePushAsync(resolved.Generation, cancellationToken);

        return OpencodeHookOutcome.Neutral;
    }

    public async Task<OpencodeHookOutcome> HandleChatMessageAsync(
        OpencodeHookPayload payload, bool dryRun, CancellationToken cancellationToken)
    {
        _ = dryRun; // see HandleSessionCreatedAsync

        var resolved = await ResolveAsync(payload, cancellationToken);

        if (resolved is null)
        {
            return OpencodeHookOutcome.Neutral;
        }

        if (!await sessionRegistry.TouchAsync(resolved.Generation, cancellationToken))
        {
            return OpencodeHookOutcome.Neutral;
        }

        if (payload.NitroPushed)
        {
            // A marked, Nitro-pushed turn never rearms the idle-push gate
            // and never receives the announcement or digest injection this
            // method exists to add: it is Nitro's own delivery, not a
            // genuine prompt from the user.
            return OpencodeHookOutcome.Neutral;
        }

        await sessionRegistry.RearmIdlePushAsync(resolved.Generation, cancellationToken);
        await sessionRegistry.ResetBlockBudgetAsync(resolved.Generation, cancellationToken);

        var row = await sessionRegistry.FindByGenerationAsync(resolved.Generation, cancellationToken);

        if (row is null || row.BindingKind == AgentSessionBindingKind.None || row.AgentName is null)
        {
            return OpencodeHookOutcome.Neutral;
        }

        var parts = new List<string>(2);

        // Claims the durable, atomic first-prompt marker for this session.
        if (await sessionRegistry.ClaimAnnouncementAsync(resolved.Generation, cancellationToken))
        {
            parts.Add(AgentActorContext.Format(row.AgentName, row.Role));
        }

        var digest = await BuildDigestAsync(
            resolved.Generation, row.AgentName, AgentSessionChannel.Digest, cancellationToken);

        if (digest is not null)
        {
            parts.Add(digest);
        }

        return parts.Count == 0 ? OpencodeHookOutcome.Neutral : new OpencodeHookOutcome { Parts = parts };
    }

    public async Task<OpencodeHookOutcome> HandleSessionIdleAsync(
        OpencodeHookPayload payload, bool dryRun, CancellationToken cancellationToken)
    {
        _ = dryRun; // see HandleSessionCreatedAsync

        if (environmentVariableProvider.GetEnvironmentVariable("NITRO_HOOK_SUPPRESS") is "1" or "true")
        {
            return OpencodeHookOutcome.Neutral;
        }

        var resolved = await ResolveAsync(payload, cancellationToken);

        if (resolved is null)
        {
            return OpencodeHookOutcome.Neutral;
        }

        // Heartbeat/presence only: ActorWakeDispatcher is the idle-push
        // gate's sole claimant (see IAgentSessionRegistry.ClaimIdlePushAsync).
        // This event's own response is discarded by the shim (opencode's
        // session-idle hook has no output channel;
        // OpencodeHookExecutor.ToResponse never reads anything beyond
        // Parts), so claiming the gate or reserving a delivery here would
        // only race the dispatcher for the same one-shot push with nothing
        // to show for it - see the hc-10-5n6.2 planner ruling.
        await sessionRegistry.TouchAsync(resolved.Generation, cancellationToken);

        return OpencodeHookOutcome.Neutral;
    }

    public async Task<OpencodeHookOutcome> HandleSessionDeletedAsync(
        OpencodeHookPayload payload, bool dryRun, CancellationToken cancellationToken)
    {
        _ = dryRun; // see HandleSessionCreatedAsync

        var resolved = await ResolveAsync(payload, cancellationToken);

        if (resolved is not null)
        {
            await sessionRegistry.EndAsync(resolved.Generation, cancellationToken);
        }

        return OpencodeHookOutcome.Neutral;
    }

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

        // The reserving INSERT itself is conditioned on the session row
        // still existing (see SessionDeliveryLedger.ReserveAsync), so a
        // session deleted concurrently reserves nothing here rather than
        // raising a foreign-key violation: an empty result reads exactly
        // like every other already-reserved case below.
        var reserved = await ledger.ReserveAsync(
            generation,
            unread.Select(static message => message.Id).ToList(),
            channel,
            timeProvider.GetUtcNow(),
            cancellationToken);

        return reserved.Count == 0
            ? null
            : MailNudgeText.Format(actor, await mailStore.CountUnreadAsync(actor, cancellationToken));
    }

    private async Task<ResolvedGeneration?> ResolveAsync(
        OpencodeHookPayload payload,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(payload.SessionId)
            || string.IsNullOrWhiteSpace(payload.ServerUrl)
            || !EndpointAddress.IsValidOpencodeServerUrl(payload.ServerUrl))
        {
            return null;
        }

        var cwd = string.IsNullOrWhiteSpace(payload.Cwd)
            ? fileSystem.GetCurrentDirectory()
            : payload.Cwd;
        var workspaceDirectory = AgentWorkspace.Find(fileSystem, cwd);
        var processWorkspaceDirectory = AgentWorkspace.Find(fileSystem, fileSystem.GetCurrentDirectory());

        if (workspaceDirectory is null || workspaceDirectory != processWorkspaceDirectory)
        {
            return null;
        }

        var host = await instanceIdProvider.GetIdAsync(
            globalConfigDirectoryProvider.GetDirectory(), cancellationToken);
        var generation = new AgentSessionGeneration(AgentSessionHarness.Opencode, payload.SessionId, host);

        return new ResolvedGeneration(generation, cwd, workspaceDirectory);
    }

    private sealed record ResolvedGeneration(
        AgentSessionGeneration Generation,
        string Cwd,
        string WorkspaceDirectory);
}
