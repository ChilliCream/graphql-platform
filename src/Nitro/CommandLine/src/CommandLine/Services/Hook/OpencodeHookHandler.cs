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

        var row = await sessionRegistry.FindByGenerationAsync(resolved.Generation, cancellationToken);

        // The announcement claims on emission, not on the attempt: it clears
        // the instant this turn actually shows it, but re-arms whenever the
        // shim reports (payload.Delivered - see
        // OpencodeHooksTemplate.appendParts/appendOutcomes) that the PREVIOUS
        // turn's response never made it onto output.parts. false means
        // exactly that: the marker is re-armed (idempotent if it was never
        // cleared) and that turn's still-unread digest-channel reservations
        // are released so the same mail is re-offered rather than lost. This
        // can arrive on a Nitro-pushed payload too (the shim reports the
        // outcome of the last genuine turn regardless of what pushed the
        // next one), so it is handled here, above the NitroPushed early
        // return below, rather than being dropped with it. true, or an
        // absent field from a shim too old to report at all, needs no
        // action: an absent field still degrades an older shim to the same
        // at-most-once behaviour it has today, since nothing here re-arms on
        // it and the marker stays wherever the last claim left it.
        if (payload.Delivered == false)
        {
            await sessionRegistry.ArmAnnouncementAsync(resolved.Generation, cancellationToken);

            if (row?.AgentName is { } releaseActor)
            {
                await ReleaseUnreadDigestReservationsAsync(resolved.Generation, releaseActor, cancellationToken);
            }
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

        if (row is null || row.BindingKind == AgentSessionBindingKind.None || row.AgentName is null)
        {
            return OpencodeHookOutcome.Neutral;
        }

        // Build the digest before claiming the announcement: BuildDigestAsync
        // is the fallible half of this turn (mail-store or ledger failures
        // that OpencodeHookExecutor's fail-open catch-all turns into a
        // neutral response with nothing delivered), so a failure here never
        // burns a claim for a response this turn never returns. The claim
        // itself can still fail after the digest reservation already
        // committed, so it is wrapped rather than left as the assumed last
        // write: a failure here would otherwise burn the reservation for a
        // response this turn also never returns.
        var digest = await BuildDigestAsync(
            resolved.Generation, row.AgentName, AgentSessionChannel.Digest, cancellationToken);
        bool announce;

        try
        {
            announce = await sessionRegistry.ClaimAnnouncementAsync(resolved.Generation, cancellationToken);
        }
        catch
        {
            if (digest is not null)
            {
                await ReleaseUnreadDigestReservationsAsync(resolved.Generation, row.AgentName, cancellationToken);
            }

            throw;
        }

        var parts = new List<string>(2);

        if (announce)
        {
            parts.Add(AgentActorContext.Format(row.AgentName, row.Role));
        }

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

        if (reserved.Count == 0)
        {
            return null;
        }

        try
        {
            return MailNudgeText.Format(actor, await mailStore.CountUnreadAsync(actor, cancellationToken));
        }
        catch
        {
            // The reservation above already committed even though the
            // count that would have turned it into a delivered nudge never
            // did: release it rather than leave it spent for a digest this
            // response never returns, so the next chat message reserves and
            // delivers the same messages again instead of losing them.
            await ledger.ReleaseAsync(generation, reserved, channel, cancellationToken);
            throw;
        }
    }

    private async Task ReleaseUnreadDigestReservationsAsync(
        AgentSessionGeneration generation, string actor, CancellationToken cancellationToken)
    {
        var unread = await mailStore.QueryInboxAsync(
            new MailInboxFilter { Actor = actor, UnreadOnly = true, Limit = MaxDigestMessages },
            cancellationToken);

        if (unread.Count == 0)
        {
            return;
        }

        await ledger.ReleaseAsync(
            generation,
            unread.Select(static message => message.Id).ToList(),
            AgentSessionChannel.Digest,
            cancellationToken);
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
