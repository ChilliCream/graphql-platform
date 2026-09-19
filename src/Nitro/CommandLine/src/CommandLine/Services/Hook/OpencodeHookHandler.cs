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
    /// The maximum number of unread messages considered for delivery reservations in one call.
    /// </summary>
    public const int MaxDigestMessages = 10;

    public async Task<OpencodeHookOutcome> HandleSessionCreatedAsync(
        OpencodeHookPayload payload, bool dryRun, CancellationToken cancellationToken)
    {
        // dryRun does not change opencode handler behavior.
        _ = dryRun;

        var resolved = await ResolveAsync(payload, cancellationToken);

        if (resolved is null)
        {
            return OpencodeHookOutcome.Neutral;
        }

        // Only a valid endpoint reported as bound is registered for push delivery.
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

        await sessionRegistry.ArmAnnouncementAsync(resolved.Generation, cancellationToken);

        if (trusted)
        {
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

        // An unconfirmed previous delivery rearms the announcement and releases digest
        // reservations for the current unread batch.
        if (payload.Delivered == false)
        {
            await sessionRegistry.ArmAnnouncementAsync(resolved.Generation, cancellationToken);

            if (row?.AgentName is { } releaseActor)
            {
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
            // Nitro-pushed turns receive no additional context and do not rearm idle push.
            return OpencodeHookOutcome.Neutral;
        }

        if (row?.EndpointKind == AgentSessionEndpointKind.OpencodeServer)
        {
            await sessionRegistry.RearmIdlePushAsync(resolved.Generation, cancellationToken);
        }

        await sessionRegistry.ResetBlockBudgetAsync(resolved.Generation, cancellationToken);

        if (row is null || row.BindingKind == AgentSessionBindingKind.None || row.AgentName is null)
        {
            return OpencodeHookOutcome.Neutral;
        }

        var digest = await BuildDigestAsync(
            resolved.Generation, row.AgentName, AgentSessionChannel.Digest, cancellationToken);
        bool announce;

        try
        {
            announce = await sessionRegistry.ClaimAnnouncementAsync(resolved.Generation, cancellationToken);
        }
        catch
        {
            // Compensation releases only the message ids reserved by this call.
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

        // Idle events refresh presence without claiming the push gate or reserving messages.
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

        // A missing session or a session owned by another host yields no reservations.
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
            await ReleaseCompensatingReservationAsync(generation, reserved, channel);
            throw;
        }
    }

    /// <summary>
    /// Attempts to release the supplied reservations without cancellation.
    /// Release failures are ignored.
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
