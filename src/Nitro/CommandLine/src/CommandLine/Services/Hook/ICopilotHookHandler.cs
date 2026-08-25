namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Implements the Copilot turn-boundary event state machine: presence upsert
/// and the unread-mail digest on <c>sessionStart</c>, a presence-only touch
/// on <c>userPromptSubmitted</c> because Copilot drops that event's response
/// body, and
/// presence teardown on <c>sessionEnd</c>. Every member is fail-open by
/// contract, same as <see cref="IClaudeHookHandler"/>/<see cref="ICodexHookHandler"/>.
/// </summary>
internal interface ICopilotHookHandler
{
    /// <summary>
    /// Upserts the session's presence row, then returns the unread-mail digest for
    /// messages not yet delivered on the digest channel, if the row is
    /// claimed and any exist. <paramref name="dryRun"/> pins the row's
    /// generation to the same fixed sentinel identity
    /// <see cref="IClaudeHookHandler.HandleSessionStartAsync"/> uses (pid 1,
    /// epoch proc_start).
    /// </summary>
    Task<CopilotHookOutcome> HandleSessionStartAsync(
        CopilotHookPayload payload, bool dryRun, CancellationToken cancellationToken);

    /// <summary>
    /// Always returns <see cref="CopilotHookOutcome.Neutral"/> because
    /// <c>userPromptSubmitted</c>'s response body is silently dropped by Copilot, unlike the same
    /// field on <c>sessionStart</c> or on Claude/Codex's own
    /// <c>UserPromptSubmit</c>/<c>user_prompt_submit</c> hook. Building and
    /// ledger-reserving a digest here would mark it delivered on the digest
    /// channel without ever showing it to the user, so this handler does not
    /// attempt it.
    /// </summary>
    Task<CopilotHookOutcome> HandleUserPromptSubmitAsync(
        CopilotHookPayload payload, bool dryRun, CancellationToken cancellationToken);

    /// <summary>
    /// Conditionally deletes the session's presence row.
    /// </summary>
    Task<CopilotHookOutcome> HandleSessionEndAsync(
        CopilotHookPayload payload, bool dryRun, CancellationToken cancellationToken);
}
