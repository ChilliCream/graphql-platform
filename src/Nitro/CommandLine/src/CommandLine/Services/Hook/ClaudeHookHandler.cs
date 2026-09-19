using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

internal sealed class ClaudeHookHandler(
    IFileSystem fileSystem,
    TimeProvider timeProvider,
    IAgentSessionRegistry sessionRegistry,
    IAgentRegistry agentRegistry,
    ISessionDeliveryLedger ledger,
    IMailStore mailStore,
    IClaudeSessionFileReader sessionFileReader,
    INitroInstanceIdProvider instanceIdProvider,
    IGlobalConfigDirectoryProvider globalConfigDirectoryProvider) : IClaudeHookHandler
{
    /// <summary>
    /// The maximum number of Stop blocks per turn, reset on <c>UserPromptSubmit</c>.
    /// </summary>
    public const int MaxBlocksPerTurn = 3;

    private static string BlockReason(string actor)
        => $"Unread nitro mail is waiting. Read it with `nitro agent mail inbox --actor {actor}` "
            + "before ending this turn, or ignore this once if it is not actionable right now.";

    private const string BlockDigestPreamble =
        "Unread nitro mail is waiting; handle it before ending this turn, or ignore this once if it is not actionable right now.";

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

        if (resolved.HarnessVersion.Length > 0)
        {
            await sessionRegistry.RecordHarnessVersionAsync(
                resolved.Generation, resolved.HarnessVersion, cancellationToken);
        }

        var role = await AgentEffectiveRole.ResolveAsync(
            session.Role, session.AgentName!, agentRegistry, cancellationToken);

        return new ClaudeHookOutcome
        {
            AdditionalContext = AgentActorContext.Format(session.AgentName!, role)
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

        var digest = await BuildDigestAsync(
            resolved.Generation, row.AgentName, AgentSessionChannel.Digest, cancellationToken);

        return digest is null
            ? ClaudeHookOutcome.Neutral
            : new ClaudeHookOutcome { AdditionalContext = digest.Text };
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
            // The exhausted budget leaves candidates unreserved for a later turn.
            return ClaudeHookOutcome.Neutral;
        }

        var digest = await BuildDigestAsync(
            resolved.Generation, row.AgentName, AgentSessionChannel.Gate, cancellationToken);

        if (digest is null)
        {
            return ClaudeHookOutcome.Neutral;
        }

        var incremented = await sessionRegistry.IncrementBlockBudgetAsync(resolved.Generation, cancellationToken);

        if (incremented is null)
        {
            // The session ended before the budget could be incremented.
            return ClaudeHookOutcome.Neutral;
        }

        return new ClaudeHookOutcome
        {
            Block = true,
            BlockReason = digest.HasMessages
                ? $"{BlockDigestPreamble}\n{digest.Text}"
                : BlockReason(row.AgentName)
        };
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
    /// Returns a digest or unread-count reminder for newly reserved messages in the
    /// current inbox batch, or null when that batch yields no reservations.
    /// </summary>
    private async Task<MailDigestResult?> BuildDigestAsync(
        AgentSessionGeneration generation,
        string actor,
        string channel,
        CancellationToken cancellationToken)
    {
        var unread = await mailStore.QueryInboxAsync(
            new MailInboxFilter { Actor = actor, UnreadOnly = true, Limit = MailDigestPolicy.MaxMessages },
            cancellationToken);

        if (unread.Count == 0)
        {
            return null;
        }

        var messageIds = unread.Select(message => message.Id).ToList();
        var delivered = await ledger.FindDeliveredAsync(generation, messageIds, cancellationToken);
        var reserved = await ledger.ReserveAsync(
            generation.Harness,
            generation.SessionId,
            messageIds,
            channel,
            timeProvider.GetUtcNow(),
            cancellationToken);

        if (reserved.Count == 0)
        {
            return null;
        }

        var reservedIds = reserved.ToHashSet(StringComparer.Ordinal);
        var deliveredIds = delivered.ToHashSet(StringComparer.Ordinal);
        var messages = unread
            .Where(message => reservedIds.Contains(message.Id) && !deliveredIds.Contains(message.Id))
            .ToList();
        var unreadTotal = await mailStore.CountUnreadAsync(actor, cancellationToken);

        return new MailDigestResult(
            MailDigest.Render(actor, messages, unreadTotal),
            messages.Count > 0);
    }

    /// <summary>
    /// Resolves the session identity and workspace, or null when the session id or cwd
    /// is missing, no workspace is found, or the payload and process workspaces differ.
    /// A dry run skips reading the session file.
    /// </summary>
    private async Task<ResolvedGeneration?> ResolveAsync(
        ClaudeHookPayload payload, bool dryRun, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(payload.Cwd) || string.IsNullOrWhiteSpace(payload.SessionId))
        {
            return null;
        }

        var payloadWorkspace = AgentWorkspace.Find(fileSystem, payload.Cwd);
        var processWorkspace = AgentWorkspace.Find(fileSystem, fileSystem.GetCurrentDirectory());

        if (payloadWorkspace is null || payloadWorkspace != processWorkspace)
        {
            return null;
        }

        // A session with no file still resolves; the file only supplies the peer
        // address and the harness version.
        var session = dryRun ? null : sessionFileReader.Find(payload.SessionId);

        var host = await instanceIdProvider.GetIdAsync(
            globalConfigDirectoryProvider.GetDirectory(), cancellationToken);

        var generation = new AgentSessionGeneration(
            AgentSessionHarness.ClaudeCode, payload.SessionId, host);

        return new ResolvedGeneration(
            generation, payloadWorkspace, session?.Name, session?.Version ?? string.Empty);
    }

    private sealed record ResolvedGeneration(
        AgentSessionGeneration Generation,
        string WorkspaceDirectory,
        string? EndpointName,
        string HarnessVersion);

    private sealed record MailDigestResult(string Text, bool HasMessages);
}
