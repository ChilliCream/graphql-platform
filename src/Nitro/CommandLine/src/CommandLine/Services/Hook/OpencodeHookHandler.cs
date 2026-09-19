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
        // dryRun has no effect here: opencode has no session-file side channel to skip.
        _ = dryRun;

        var resolved = await ResolveAsync(payload, cancellationToken);

        if (resolved is null)
        {
            return OpencodeHookOutcome.Neutral;
        }

        // Register the endpoint only when EndpointAddress.IsTrustedOpencodeServerUrl
        // confirms this process actually bound a server. An unproven serverUrl is
        // treated as unregistered instead of trusted.
        var trusted = EndpointAddress.IsTrustedOpencodeServerUrl(payload.ServerUrl!, payload.ServerBound);
        var (endpointKind, endpointAddr, endpointSecret) = trusted
            ? (AgentSessionEndpointKind.OpencodeServer, payload.ServerUrl!, payload.ServerPassword)
            : (AgentSessionEndpointKind.None, string.Empty, null);

        await sessionRegistry.StartAsync(
            resolved.Generation,
            resolved.Cwd,
            resolved.WorkspaceDirectory,
            endpointKind,
            endpointAddr,
            endpointSecret,
            envActor: null,
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(payload.HarnessVersion))
        {
            await sessionRegistry.RecordHarnessVersionAsync(
                resolved.Generation, payload.HarnessVersion, cancellationToken);
        }

        // Arms the first-prompt announcement so a session that goes idle before its
        // first chat message still announces once.
        await sessionRegistry.ArmAnnouncementAsync(resolved.Generation, cancellationToken);

        if (trusted)
        {
            // The idle-push gate is armed only for an endpoint proven to belong to
            // this process.
            await sessionRegistry.RearmIdlePushAsync(resolved.Generation, cancellationToken);
        }

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

        // payload.Delivered == false means the previous turn's response never reached
        // output.parts: re-arm the announcement and release that turn's still-unread
        // digest reservations so the same mail is re-offered.
        if (payload.Delivered == false)
        {
            await sessionRegistry.ArmAnnouncementAsync(resolved.Generation, cancellationToken);

            if (row?.AgentName is { } releaseActor)
            {
                // Re-queries what is still unread, since the previous turn's exact
                // reservation list is not available here.
                var stillUnread = await mailStore.QueryInboxAsync(
                    new MailInboxFilter { Actor = releaseActor, UnreadOnly = true, Limit = MaxDigestMessages },
                    cancellationToken);

                if (stillUnread.Count > 0)
                {
                    await ledger.ReleaseAsync(
                        resolved.Generation,
                        stillUnread.Select(static message => message.Id).ToList(),
                        AgentSessionChannel.Digest,
                        cancellationToken);
                }
            }
        }

        if (payload.NitroPushed)
        {
            // A Nitro-pushed turn is Nitro's own delivery, not a genuine prompt from
            // the user, so it never rearms the idle-push gate or receives an
            // announcement or digest.
            return OpencodeHookOutcome.Neutral;
        }

        if (row?.EndpointKind == AgentSessionEndpointKind.OpencodeServer)
        {
            // The idle-push gate is rearmed only for a session whose endpoint was
            // proven to belong to this process.
            await sessionRegistry.RearmIdlePushAsync(resolved.Generation, cancellationToken);
        }

        await sessionRegistry.ResetBlockBudgetAsync(resolved.Generation, cancellationToken);

        if (row is null || row.BindingKind == AgentSessionBindingKind.None || row.AgentName is null)
        {
            return OpencodeHookOutcome.Neutral;
        }

        // Build the digest before claiming the announcement, so a failure here never
        // burns an announcement claim for a response this turn never returns.
        var digest = await BuildDigestAsync(
            resolved.Generation, row.AgentName, AgentSessionChannel.Digest, cancellationToken);
        bool announce;

        try
        {
            announce = await sessionRegistry.ClaimAnnouncementAsync(resolved.Generation, cancellationToken);
        }
        catch
        {
            // Releases exactly the ids this turn reserved, never a re-query of the
            // whole unread inbox, so an id another consumer holds stays held.
            await ReleaseCompensatingReservationAsync(
                resolved.Generation, digest.ReservedIds, AgentSessionChannel.Digest);

            throw;
        }

        var parts = new List<string>(2);

        if (announce)
        {
            parts.Add(AgentActorContext.Format(row.AgentName, row.Role));
        }

        if (digest.Text is not null)
        {
            parts.Add(digest.Text);
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

        // Heartbeat/presence only: this event's response carries no output channel,
        // so claiming the idle-push gate or reserving a delivery here would only
        // race ActorWakeDispatcher for the same one-shot push.
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

    private async Task<DigestBuildResult> BuildDigestAsync(
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
            return DigestBuildResult.Empty;
        }

        // A session deleted concurrently reserves nothing here, rather than raising a
        // foreign-key violation, so an empty result reads like any other
        // already-reserved case below.
        var reserved = await ledger.ReserveAsync(
            generation,
            unread.Select(static message => message.Id).ToList(),
            channel,
            timeProvider.GetUtcNow(),
            cancellationToken);

        if (reserved.Count == 0)
        {
            return DigestBuildResult.Empty;
        }

        try
        {
            var text = MailNudgeText.Format(actor, await mailStore.CountUnreadAsync(actor, cancellationToken));

            return new DigestBuildResult(text, reserved);
        }
        catch
        {
            // Releases the reservation so the next chat message reserves and delivers
            // the same messages again instead of losing them.
            await ReleaseCompensatingReservationAsync(generation, reserved, channel);
            throw;
        }
    }

    /// <summary>
    /// Releases a reservation this turn made but can no longer use, as compensation
    /// for an exception the caller is about to rethrow.
    /// </summary>
    private async Task ReleaseCompensatingReservationAsync(
        AgentSessionGeneration generation, IReadOnlyList<string> messageIds, string channel)
    {
        try
        {
            await ledger.ReleaseAsync(generation, messageIds, channel, CancellationToken.None);
        }
        catch
        {
            // A failure here is swallowed so it cannot replace the original exception
            // the caller is propagating.
        }
    }

    private sealed record DigestBuildResult(string? Text, IReadOnlyList<string> ReservedIds)
    {
        public static readonly DigestBuildResult Empty = new(null, []);
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
