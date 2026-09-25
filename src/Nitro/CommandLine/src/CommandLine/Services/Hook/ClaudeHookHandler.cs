using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

internal sealed class ClaudeHookHandler(
    IFileSystem fileSystem,
    TimeProvider timeProvider,
    IAgentStore agentStore,
    IAgentDeliveryLedger ledger,
    IMailStore mailStore,
    IClaudeSessionFileReader sessionFileReader) : IClaudeHookHandler
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
        ClaudeHookPayload payload, bool skipSessionFileLookup, CancellationToken cancellationToken)
    {
        if (IsSubagentSession(payload))
        {
            return ClaudeHookOutcome.Neutral;
        }

        var resolved = Resolve(payload, skipSessionFileLookup);

        if (resolved is null)
        {
            return ClaudeHookOutcome.Neutral;
        }

        var result = await agentStore.StartSessionAsync(BuildStartRequest(payload, resolved), cancellationToken);

        if (result.Kind == AgentSessionStartKind.Ignored)
        {
            return ClaudeHookOutcome.Neutral;
        }

        var row = result.Row!;

        return new ClaudeHookOutcome { AdditionalContext = AgentActorContext.Format(row.Name, row.Role) };
    }

    public async Task<ClaudeHookOutcome> HandleUserPromptSubmitAsync(
        ClaudeHookPayload payload, bool skipSessionFileLookup, CancellationToken cancellationToken)
    {
        if (IsSubagentSession(payload))
        {
            return ClaudeHookOutcome.Neutral;
        }

        var resolved = await ResolveOrStartRowAsync(payload, skipSessionFileLookup, cancellationToken);

        if (resolved is null)
        {
            return ClaudeHookOutcome.Neutral;
        }

        var row = resolved.Row;

        await agentStore.ResetBlockBudgetAsync(row.Name, cancellationToken);

        var digest = await BuildDigestAsync(row.Name, AgentSessionChannel.Digest, cancellationToken);
        var context = ComposeContext(resolved.Minted ? Announce(row) : null, digest?.Text);

        return context is null ? ClaudeHookOutcome.Neutral : new ClaudeHookOutcome { AdditionalContext = context };
    }

    public async Task<ClaudeHookOutcome> HandleStopAsync(
        ClaudeHookPayload payload, bool skipSessionFileLookup, CancellationToken cancellationToken)
    {
        if (IsSubagentSession(payload) || payload.StopHookActive)
        {
            return ClaudeHookOutcome.Neutral;
        }

        var resolved = await ResolveOrStartRowAsync(payload, skipSessionFileLookup, cancellationToken);

        if (resolved is null)
        {
            return ClaudeHookOutcome.Neutral;
        }

        var row = resolved.Row;

        if (row.BlockBudgetUsed >= MaxBlocksPerTurn)
        {
            // The exhausted budget leaves candidates unreserved for a later turn.
            return ClaudeHookOutcome.Neutral;
        }

        var digest = await BuildDigestAsync(row.Name, AgentSessionChannel.Gate, cancellationToken);

        if (digest is null)
        {
            return ClaudeHookOutcome.Neutral;
        }

        var incremented = await agentStore.IncrementBlockBudgetAsync(row.Name, cancellationToken);

        if (incremented == 0)
        {
            // The agent no longer matches (deleted, or otherwise gone) before the budget
            // could be incremented.
            return ClaudeHookOutcome.Neutral;
        }

        return new ClaudeHookOutcome
        {
            Block = true,
            BlockReason = digest.HasMessages
                ? $"{BlockDigestPreamble}\n{digest.Text}"
                : BlockReason(row.Name)
        };
    }

    public async Task<ClaudeHookOutcome> HandleNotificationAsync(
        ClaudeHookPayload payload, bool skipSessionFileLookup, CancellationToken cancellationToken)
    {
        if (IsSubagentSession(payload) || payload.NotificationType != "idle_prompt")
        {
            return ClaudeHookOutcome.Neutral;
        }

        var resolved = await ResolveOrStartRowAsync(payload, skipSessionFileLookup, cancellationToken);

        return resolved is { Minted: true }
            ? new ClaudeHookOutcome { AdditionalContext = Announce(resolved.Row) }
            : ClaudeHookOutcome.Neutral;
    }

    public async Task<ClaudeHookOutcome> HandleSessionEndAsync(
        ClaudeHookPayload payload, bool skipSessionFileLookup, CancellationToken cancellationToken)
    {
        if (IsSubagentSession(payload))
        {
            return ClaudeHookOutcome.Neutral;
        }

        var resolved = Resolve(payload, skipSessionFileLookup);

        if (resolved is not null)
        {
            await agentStore.EndSessionAsync(AgentSessionHarness.ClaudeCode, payload.SessionId!, cancellationToken);
        }

        return ClaudeHookOutcome.Neutral;
    }

    /// <summary>
    /// Resolves the current harness session's row, minting one exactly like
    /// <see cref="HandleSessionStartAsync"/> without announcing it here when none is bound
    /// yet; callers decide whether the mint is announced. Returns null when the payload
    /// does not resolve, the session belongs to a deleted agent, or the mint itself is
    /// ignored for the same reason.
    /// </summary>
    private async Task<ResolvedRow?> ResolveOrStartRowAsync(
        ClaudeHookPayload payload, bool skipSessionFileLookup, CancellationToken cancellationToken)
    {
        var resolved = Resolve(payload, skipSessionFileLookup);

        if (resolved is null)
        {
            return null;
        }

        var row = await agentStore.FindBySessionAsync(
            AgentSessionHarness.ClaudeCode, payload.SessionId!, cancellationToken);

        if (row is not null)
        {
            if (row.IsDeleted)
            {
                return null;
            }

            await agentStore.TouchSessionAsync(AgentSessionHarness.ClaudeCode, payload.SessionId!, cancellationToken);

            return new ResolvedRow(row, Minted: false);
        }

        var result = await agentStore.StartSessionAsync(BuildStartRequest(payload, resolved), cancellationToken);

        return result.Kind == AgentSessionStartKind.Ignored
            ? null
            : new ResolvedRow(result.Row!, result.Kind == AgentSessionStartKind.Minted);
    }

    /// <summary>
    /// A payload carries a subagent marker when either <c>agent_id</c> or <c>agent_type</c>
    /// is set, meaning the hook fired inside a subagent session rather than the top-level one.
    /// </summary>
    private static bool IsSubagentSession(ClaudeHookPayload payload)
        => !string.IsNullOrEmpty(payload.AgentId) || !string.IsNullOrEmpty(payload.AgentType);

    private static string Announce(AgentRow row) => AgentActorContext.Format(row.Name, row.Role);

    /// <summary>
    /// Joins the mint announcement and the mail digest with a blank line when both are
    /// present, or returns whichever one is present, or null when neither is.
    /// </summary>
    private static string? ComposeContext(string? announcement, string? digest) => (announcement, digest) switch
    {
        (null, null) => null,
        ({ } head, null) => head,
        (null, { } tail) => tail,
        ({ } head, { } tail) => $"{head}\n\n{tail}"
    };

    /// <summary>
    /// Returns a digest or unread-count reminder for newly reserved messages in the
    /// current inbox batch, or null when that batch yields no reservations.
    /// </summary>
    private async Task<MailDigestResult?> BuildDigestAsync(
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
        var delivered = await ledger.FindDeliveredAsync(actor, messageIds, cancellationToken);
        var reserved = await ledger.ReserveAsync(
            actor, messageIds, channel, timeProvider.GetUtcNow(), cancellationToken);

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
    /// Resolves the session's workspace and, when available, its peer name and harness
    /// version, or null when the session id or cwd is missing, no workspace is found, or
    /// the payload and process workspaces differ. The session-file lookup is skipped when
    /// <paramref name="skipSessionFileLookup"/> is true.
    /// </summary>
    private ResolvedSession? Resolve(ClaudeHookPayload payload, bool skipSessionFileLookup)
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
        var session = skipSessionFileLookup ? null : sessionFileReader.Find(payload.SessionId);

        return new ResolvedSession(payloadWorkspace, session?.Name, session?.Version ?? string.Empty);
    }

    private static AgentSessionStartRequest BuildStartRequest(ClaudeHookPayload payload, ResolvedSession resolved)
    {
        var (endpointKind, endpointAddr) = resolved.EndpointName is { Length: > 0 } name
            && EndpointAddress.IsValid(name)
                ? (AgentSessionEndpointKind.ClaudePeer, name)
                : (AgentSessionEndpointKind.None, string.Empty);

        return new AgentSessionStartRequest
        {
            Harness = AgentSessionHarness.ClaudeCode,
            SessionId = payload.SessionId!,
            HarnessVersion = resolved.HarnessVersion,
            Cwd = payload.Cwd!,
            WorkspacePath = resolved.WorkspaceDirectory,
            EndpointKind = endpointKind,
            EndpointAddr = endpointAddr
        };
    }

    private sealed record ResolvedSession(
        string WorkspaceDirectory,
        string? EndpointName,
        string HarnessVersion);

    /// <summary>
    /// A resolved harness session's row, and whether resolving it just minted a new agent.
    /// </summary>
    private sealed record ResolvedRow(AgentRow Row, bool Minted);

    private sealed record MailDigestResult(string Text, bool HasMessages);
}
