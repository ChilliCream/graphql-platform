namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// What <see cref="ICodexHookHandler.HandleNotifyAsync"/> decided: whether a
/// digest was actually queued via <c>codex queue</c>. Purely observational
/// (the notify command's stdout/stderr and its own exit code are not part of
/// Codex's notify contract the way <see cref="CodexHookOutcome"/> is part of
/// the hooks.json contract) - this exists so tests, and the command layer's
/// own diagnostics, can tell whether the reserve-then-emit path actually ran.
/// </summary>
internal sealed record CodexNotifyOutcome
{
    public static readonly CodexNotifyOutcome Neutral = new();

    public bool Queued { get; init; }
}

/// <summary>
/// Implements the Codex turn-boundary event state machine: presence upsert
/// on <c>SessionStart</c>, the unread-mail digest on <c>UserPromptSubmit</c>
/// (both via <c>hooks.json</c>'s <c>additionalContext</c>), presence teardown on
/// <c>SessionEnd</c>, and the idle-turn gate on the separate <c>notify</c>
/// mechanism. Unlike Claude's <c>Stop</c> hook, Codex has no way
/// to block a turn from ending, so the gate instead queues the digest into
/// the thread's next turn via <c>codex queue</c>, with the delivery ledger's
/// message-id-keyed reservation as the loop guard. Every member is fail-open by
/// contract, same as <see cref="Hook.IClaudeHookHandler"/>.
/// </summary>
internal interface ICodexHookHandler
{
    /// <summary>
    /// Upserts the session's presence row. <paramref name="dryRun"/> pins the
    /// row's generation to the same fixed sentinel identity
    /// <see cref="Hook.IClaudeHookHandler.HandleSessionStartAsync"/> uses
    /// (pid 1, epoch proc_start), for the same reason: fixture-driven tests
    /// and captured-payload replays without a live Codex ancestor process.
    /// </summary>
    Task<CodexHookOutcome> HandleSessionStartAsync(
        CodexHookPayload payload, bool dryRun, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the unread-mail digest for messages not yet delivered on the
    /// digest channel, or <see cref="CodexHookOutcome.Neutral"/> when there
    /// is nothing new. Codex has no per-turn block budget to reset (that
    /// concept only exists for Claude's <c>Stop</c> gate).
    /// </summary>
    Task<CodexHookOutcome> HandleUserPromptSubmitAsync(
        CodexHookPayload payload, bool dryRun, CancellationToken cancellationToken);

    /// <summary>
    /// Conditionally deletes the session's presence row.
    /// </summary>
    Task<CodexHookOutcome> HandleSessionEndAsync(
        CodexHookPayload payload, bool dryRun, CancellationToken cancellationToken);

    /// <summary>
    /// The idle-turn gate: resolves the workspace from
    /// <paramref name="payload"/>'s <c>cwd</c>, matches the session row by
    /// thread id (Codex's <c>thread-id</c> and <c>session_id</c> are the same
    /// identifier), and queues one digest via <c>codex queue --thread</c>
    /// for unread messages not yet claimed on the gate channel.
    /// A message that IS already claimed (the queued digest's own delivery
    /// turn re-firing notify) is not re-queued - this is the S2-verified
    /// notify/queue loop guard.
    /// </summary>
    Task<CodexNotifyOutcome> HandleNotifyAsync(
        CodexNotifyPayload payload, bool dryRun, CancellationToken cancellationToken);
}
