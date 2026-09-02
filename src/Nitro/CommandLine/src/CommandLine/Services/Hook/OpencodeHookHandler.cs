using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Microsoft.Data.Sqlite;

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

    // Reserved internal marker for an actor announcement, never a Nitro mail id.
    private const string FirstPromptAnnouncementId = "__nitro_internal:opencode:first-prompt-announcement";

    // Reserved internal marker for the current idle period, never a Nitro mail id.
    private const string IdleTransitionId = "__nitro_internal:opencode:idle-transition";

    public async Task<OpencodeHookOutcome> HandleSessionCreatedAsync(
        OpencodeHookPayload payload, bool dryRun, CancellationToken cancellationToken)
    {
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

        return OpencodeHookOutcome.Neutral;
    }

    public async Task<OpencodeHookOutcome> HandleChatMessageAsync(
        OpencodeHookPayload payload, bool dryRun, CancellationToken cancellationToken)
    {
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
            return OpencodeHookOutcome.Neutral;
        }

        await ReleaseAsync(resolved.Generation, IdleTransitionId, AgentSessionChannel.Gate, cancellationToken);
        await sessionRegistry.ResetBlockBudgetAsync(resolved.Generation, cancellationToken);

        var row = await sessionRegistry.FindByGenerationAsync(resolved.Generation, cancellationToken);

        if (row is null || row.BindingKind == AgentSessionBindingKind.None || row.AgentName is null)
        {
            return OpencodeHookOutcome.Neutral;
        }

        var parts = new List<string>(2);

        try
        {
            // Reserves the durable, atomic first-prompt marker for this session.
            var announcement = await ReserveAsync(
                resolved.Generation,
                [FirstPromptAnnouncementId],
                AgentSessionChannel.Digest,
                cancellationToken);

            if (announcement.Count > 0)
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
        catch (SessionRemovedDuringDeliveryException)
        {
            return OpencodeHookOutcome.Neutral;
        }
    }

    public async Task<OpencodeHookOutcome> HandleSessionIdleAsync(
        OpencodeHookPayload payload, bool dryRun, CancellationToken cancellationToken)
    {
        if (environmentVariableProvider.GetEnvironmentVariable("NITRO_HOOK_SUPPRESS") is "1" or "true")
        {
            return OpencodeHookOutcome.Neutral;
        }

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

        if (row is null || row.BindingKind == AgentSessionBindingKind.None || row.AgentName is null)
        {
            return OpencodeHookOutcome.Neutral;
        }

        try
        {
            var unread = await mailStore.QueryInboxAsync(
                new MailInboxFilter { Actor = row.AgentName, UnreadOnly = true, Limit = MaxDigestMessages },
                cancellationToken);

            if (unread.Count == 0)
            {
                return OpencodeHookOutcome.Neutral;
            }

            var transition = await ReserveAsync(
                resolved.Generation,
                [IdleTransitionId],
                AgentSessionChannel.Gate,
                cancellationToken);

            if (transition.Count == 0)
            {
                return OpencodeHookOutcome.Neutral;
            }

            var digest = await BuildDigestAsync(
                resolved.Generation, row.AgentName, AgentSessionChannel.Gate, cancellationToken);

            if (digest is not null)
            {
                return new OpencodeHookOutcome { IdleDelivery = digest };
            }

            await ReleaseAsync(resolved.Generation, IdleTransitionId, AgentSessionChannel.Gate, cancellationToken);
            return OpencodeHookOutcome.Neutral;
        }
        catch (SessionRemovedDuringDeliveryException)
        {
            return OpencodeHookOutcome.Neutral;
        }
    }

    public async Task<OpencodeHookOutcome> HandleSessionDeletedAsync(
        OpencodeHookPayload payload, bool dryRun, CancellationToken cancellationToken)
    {
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

        var reserved = await ReserveAsync(
            generation,
            unread.Select(static message => message.Id).ToList(),
            channel,
            cancellationToken);

        return reserved.Count == 0
            ? null
            : MailNudgeText.Format(actor, await mailStore.CountUnreadAsync(actor, cancellationToken));
    }

    private async Task<IReadOnlyList<string>> ReserveAsync(
        AgentSessionGeneration generation,
        IReadOnlyList<string> messageIds,
        string channel,
        CancellationToken cancellationToken)
    {
        try
        {
            return await ledger.ReserveAsync(
                generation,
                messageIds,
                channel,
                timeProvider.GetUtcNow(),
                cancellationToken);
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19 && ex.SqliteExtendedErrorCode == 787)
        {
            if (await sessionRegistry.FindByGenerationAsync(generation, cancellationToken) is null)
            {
                throw new SessionRemovedDuringDeliveryException();
            }

            throw;
        }
    }

    private Task ReleaseAsync(
        AgentSessionGeneration generation,
        string messageId,
        string channel,
        CancellationToken cancellationToken)
        => ledger.ReleaseAsync(
            generation,
            messageId,
            channel,
            cancellationToken);

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

    private sealed class SessionRemovedDuringDeliveryException : Exception;
}
