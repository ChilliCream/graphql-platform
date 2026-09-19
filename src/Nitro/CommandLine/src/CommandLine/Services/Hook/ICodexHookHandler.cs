namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Implements the Codex turn-boundary event state machine: presence upsert on
/// <c>SessionStart</c>, the unread-mail digest on <c>UserPromptSubmit</c>, presence
/// teardown on <c>SessionEnd</c>, and the idle-turn gate on the separate
/// <c>notify</c> mechanism. Every member is fail-open by contract.
/// </summary>
internal interface ICodexHookHandler
{
    /// <summary>
    /// Upserts the session's presence row. <paramref name="dryRun"/> skips every
    /// side effect outside the workspace database.
    /// </summary>
    Task<CodexHookOutcome> HandleSessionStartAsync(
        CodexHookPayload payload, bool dryRun, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the unread-mail digest for messages not yet delivered on the digest
    /// channel, or <see cref="CodexHookOutcome.Neutral"/> when there is nothing new.
    /// </summary>
    Task<CodexHookOutcome> HandleUserPromptSubmitAsync(
        CodexHookPayload payload, bool dryRun, CancellationToken cancellationToken);

    /// <summary>
    /// Conditionally deletes the session's presence row.
    /// </summary>
    Task<CodexHookOutcome> HandleSessionEndAsync(
        CodexHookPayload payload, bool dryRun, CancellationToken cancellationToken);

    /// <summary>
    /// The idle-turn gate: resolves the workspace from <paramref name="payload"/>'s
    /// <c>cwd</c>, matches the session row by thread id, and queues one digest via
    /// <c>codex queue --thread</c> for unread messages not yet claimed on the gate
    /// channel. A message already claimed is not re-queued.
    /// </summary>
    Task<CodexNotifyOutcome> HandleNotifyAsync(
        CodexNotifyPayload payload, bool dryRun, CancellationToken cancellationToken);
}
