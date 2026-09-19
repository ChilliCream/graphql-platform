namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Implements the Claude Code turn-boundary event state machine: presence upsert on
/// <c>SessionStart</c>, the unread-mail digest on <c>UserPromptSubmit</c>, the Stop
/// gate, and presence teardown on <c>SessionEnd</c>. Every member is fail-open by
/// contract, returning <see cref="ClaudeHookOutcome.Neutral"/> instead of throwing.
/// </summary>
internal interface IClaudeHookHandler
{
    /// <summary>
    /// Upserts the session's presence row. <paramref name="dryRun"/> pins the row's
    /// generation to a fixed sentinel identity instead of walking this process's
    /// ancestors for a live Claude Code parent. Dry-run still writes to the real
    /// workspace database; a caller must not replay it with a live session's
    /// session_id.
    /// </summary>
    Task<ClaudeHookOutcome> HandleSessionStartAsync(
        ClaudeHookPayload payload, bool dryRun, CancellationToken cancellationToken);

    /// <summary>
    /// Resets the Stop gate's per-turn block budget, then returns the unread
    /// mail digest for messages not yet delivered on the digest channel, or
    /// <see cref="ClaudeHookOutcome.Neutral"/> when there is nothing new.
    /// </summary>
    Task<ClaudeHookOutcome> HandleUserPromptSubmitAsync(
        ClaudeHookPayload payload, bool dryRun, CancellationToken cancellationToken);

    /// <summary>
    /// Blocks the turn from ending when unread mail not yet delivered on the
    /// gate channel exists for the session's claimed actor, honoring
    /// <see cref="ClaudeHookPayload.StopHookActive"/> reentrancy and the
    /// per-turn block budget.
    /// </summary>
    Task<ClaudeHookOutcome> HandleStopAsync(
        ClaudeHookPayload payload, bool dryRun, CancellationToken cancellationToken);

    /// <summary>
    /// Conditionally deletes the session's presence row.
    /// </summary>
    Task<ClaudeHookOutcome> HandleSessionEndAsync(
        ClaudeHookPayload payload, bool dryRun, CancellationToken cancellationToken);
}
