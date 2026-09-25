namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Handles Claude session registration, unread-mail context, Stop decisions, and session
/// end, returning a neutral outcome without writing anything when the payload carries
/// <see cref="ClaudeHookPayload.AgentId"/> or <see cref="ClaudeHookPayload.AgentType"/>,
/// which marks a subagent session. Exceptions propagate to the hook executor.
/// </summary>
internal interface IClaudeHookHandler
{
    /// <summary>
    /// Registers the session and returns its actor context; <paramref name="skipSessionFileLookup"/>
    /// skips the session-file lookup while retaining workspace database writes.
    /// </summary>
    Task<ClaudeHookOutcome> HandleSessionStartAsync(
        ClaudeHookPayload payload, bool skipSessionFileLookup, CancellationToken cancellationToken);

    /// <summary>
    /// Resets the Stop block budget and returns an unread-mail digest or count reminder for
    /// newly reserved messages in the current inbox batch, or a neutral outcome when
    /// no context is available.
    /// </summary>
    Task<ClaudeHookOutcome> HandleUserPromptSubmitAsync(
        ClaudeHookPayload payload, bool skipSessionFileLookup, CancellationToken cancellationToken);

    /// <summary>
    /// Blocks the turn from ending when unread mail not yet delivered on the
    /// gate channel exists for the session's claimed actor, honoring
    /// <see cref="ClaudeHookPayload.StopHookActive"/> reentrancy and the
    /// per-turn block budget.
    /// </summary>
    Task<ClaudeHookOutcome> HandleStopAsync(
        ClaudeHookPayload payload, bool skipSessionFileLookup, CancellationToken cancellationToken);

    /// <summary>
    /// Touches the session's last-seen time, minting the row first when it is not yet known,
    /// only when <see cref="ClaudeHookPayload.NotificationType"/> is <c>idle_prompt</c>; any
    /// other type, or a payload that does not resolve, leaves the store untouched.
    /// </summary>
    Task<ClaudeHookOutcome> HandleNotificationAsync(
        ClaudeHookPayload payload, bool skipSessionFileLookup, CancellationToken cancellationToken);

    /// <summary>
    /// Stamps the session's agent row as ended, keeping it in place.
    /// </summary>
    Task<ClaudeHookOutcome> HandleSessionEndAsync(
        ClaudeHookPayload payload, bool skipSessionFileLookup, CancellationToken cancellationToken);
}
