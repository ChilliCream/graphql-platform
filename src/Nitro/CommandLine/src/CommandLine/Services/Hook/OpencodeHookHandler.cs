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
    IAgentStore agentStore,
    IAgentDeliveryLedger ledger,
    IMailStore mailStore,
    IEnvironmentVariableProvider environmentVariableProvider) : IOpencodeHookHandler
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

        var resolved = Resolve(payload);

        if (resolved is null)
        {
            return OpencodeHookOutcome.Neutral;
        }

        var result = await agentStore.StartSessionAsync(BuildStartRequest(payload, resolved), cancellationToken);

        if (result.Kind == AgentSessionStartKind.Ignored)
        {
            return OpencodeHookOutcome.Neutral;
        }

        var row = result.Row!;

        await agentStore.ArmAnnouncementAsync(row.Name, cancellationToken);

        if (row.EndpointKind == AgentSessionEndpointKind.OpencodeServer)
        {
            await agentStore.RearmIdlePushAsync(row.Name, cancellationToken);
        }

        return OpencodeHookOutcome.Neutral;
    }

    public async Task<OpencodeHookOutcome> HandleChatMessageAsync(
        OpencodeHookPayload payload, bool dryRun, CancellationToken cancellationToken)
    {
        _ = dryRun; // see HandleSessionCreatedAsync

        var resolved = Resolve(payload);

        if (resolved is null)
        {
            return OpencodeHookOutcome.Neutral;
        }

        var row = await agentStore.FindBySessionAsync(AgentSessionHarness.Opencode, payload.SessionId!, cancellationToken);

        if (row is null || row.IsDeleted)
        {
            return OpencodeHookOutcome.Neutral;
        }

        await agentStore.TouchSessionAsync(AgentSessionHarness.Opencode, payload.SessionId!, cancellationToken);

        // An unconfirmed previous delivery rearms the announcement and releases digest
        // reservations for the current unread batch.
        if (payload.Delivered == false)
        {
            await agentStore.ArmAnnouncementAsync(row.Name, cancellationToken);

            var stillUnread = await mailStore.QueryInboxAsync(
                new MailInboxFilter { Actor = row.Name, UnreadOnly = true, Limit = MaxDigestMessages },
                cancellationToken);

            if (stillUnread.Count > 0)
            {
                await ledger.ReleaseAsync(
                    row.Name,
                    stillUnread.Select(static message => message.Id).ToList(),
                    AgentSessionChannel.Digest,
                    cancellationToken);
            }
        }

        if (payload.NitroPushed)
        {
            // Nitro-pushed turns receive no additional context and do not rearm idle push.
            return OpencodeHookOutcome.Neutral;
        }

        if (row.EndpointKind == AgentSessionEndpointKind.OpencodeServer)
        {
            await agentStore.RearmIdlePushAsync(row.Name, cancellationToken);
        }

        await agentStore.ResetBlockBudgetAsync(row.Name, cancellationToken);

        var digest = await BuildDigestAsync(row.Name, AgentSessionChannel.Digest, cancellationToken);
        bool announce;

        try
        {
            announce = await agentStore.ClaimAnnouncementAsync(row.Name, cancellationToken);
        }
        catch
        {
            // Compensation releases only the message ids reserved by this call.
            await ReleaseCompensatingReservationAsync(row.Name, digest.ReservedIds, AgentSessionChannel.Digest);

            throw;
        }

        var parts = new List<string>(2);

        if (announce)
        {
            parts.Add(AgentActorContext.Format(row.Name, row.Role));
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

        var resolved = Resolve(payload);

        if (resolved is null)
        {
            return OpencodeHookOutcome.Neutral;
        }

        // Idle events refresh presence without claiming the push gate or reserving messages.
        await agentStore.TouchSessionAsync(AgentSessionHarness.Opencode, payload.SessionId!, cancellationToken);

        return OpencodeHookOutcome.Neutral;
    }

    public async Task<OpencodeHookOutcome> HandleSessionDeletedAsync(
        OpencodeHookPayload payload, bool dryRun, CancellationToken cancellationToken)
    {
        _ = dryRun; // see HandleSessionCreatedAsync

        var resolved = Resolve(payload);

        if (resolved is not null)
        {
            await agentStore.EndSessionAsync(AgentSessionHarness.Opencode, payload.SessionId!, cancellationToken);
        }

        return OpencodeHookOutcome.Neutral;
    }

    /// <summary>
    /// Returns a digest or unread-count reminder for newly reserved messages in the
    /// current inbox batch, or null when that batch yields no reservations.
    /// </summary>
    private async Task<DigestBuildResult> BuildDigestAsync(
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

        // A deleted agent yields no reservations.
        var reserved = await ledger.ReserveAsync(
            actor,
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
            await ReleaseCompensatingReservationAsync(actor, reserved, channel);
            throw;
        }
    }

    /// <summary>
    /// Attempts to release the supplied reservations without cancellation.
    /// Release failures are ignored.
    /// </summary>
    private async Task ReleaseCompensatingReservationAsync(
        string actor, IReadOnlyList<string> messageIds, string channel)
    {
        try
        {
            await ledger.ReleaseAsync(actor, messageIds, channel, CancellationToken.None);
        }
        catch
        {
        }
    }

    private sealed record DigestBuildResult(string? Text, IReadOnlyList<string> ReservedIds)
    {
        public static readonly DigestBuildResult Empty = new(null, []);
    }

    /// <summary>
    /// Resolves the session's workspace and cwd, or null when the session id or server URL
    /// is missing or invalid, no workspace is found, or the payload and process workspaces differ.
    /// </summary>
    private ResolvedSession? Resolve(OpencodeHookPayload payload)
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

        return new ResolvedSession(cwd, workspaceDirectory);
    }

    private static AgentSessionStartRequest BuildStartRequest(OpencodeHookPayload payload, ResolvedSession resolved)
    {
        // Only a valid endpoint reported as bound is registered for push delivery.
        var trusted = EndpointAddress.IsTrustedOpencodeServerUrl(payload.ServerUrl!, payload.ServerBound);
        var (endpointKind, endpointAddr, endpointSecret) = trusted
            ? (AgentSessionEndpointKind.OpencodeServer, payload.ServerUrl!, payload.ServerPassword)
            : (AgentSessionEndpointKind.None, string.Empty, null);

        return new AgentSessionStartRequest
        {
            Harness = AgentSessionHarness.Opencode,
            SessionId = payload.SessionId!,
            HarnessVersion = payload.HarnessVersion ?? string.Empty,
            Cwd = resolved.Cwd,
            WorkspacePath = resolved.WorkspaceDirectory,
            EndpointKind = endpointKind,
            EndpointAddr = endpointAddr,
            EndpointSecret = endpointSecret
        };
    }

    private sealed record ResolvedSession(string Cwd, string WorkspaceDirectory);
}
