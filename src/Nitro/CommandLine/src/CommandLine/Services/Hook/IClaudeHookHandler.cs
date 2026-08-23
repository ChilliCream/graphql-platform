namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Implements the Claude Code turn-boundary event state machine: presence
/// upsert on <c>SessionStart</c>, the unread-mail digest on
/// <c>UserPromptSubmit</c>, the Stop gate, and presence teardown on
/// <c>SessionEnd</c>. Every member is fail-open by contract: it never
/// throws for a condition the command layer's caller cannot act on
/// (unresolvable workspace, unclaimed session, contended database), instead
/// returning <see cref="ClaudeHookOutcome.Neutral"/>. The command layer
/// wraps every call in an additional catch-all and timeout regardless, so
/// this type does not have to be exhaustive about it.
/// </summary>
internal interface IClaudeHookHandler
{
    /// <summary>
    /// Upserts the session's presence row. <paramref name="dryRun"/> resolves
    /// the process identity pinning the row's generation from THIS process's
    /// own pid and start time instead of walking this process's ancestors
    /// for a live Claude Code parent, so a fixture-driven test (or a human
    /// replaying a captured payload) can drive the full adapter without a
    /// real Claude Code process above it.
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
