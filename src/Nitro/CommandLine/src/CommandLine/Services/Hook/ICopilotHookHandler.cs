namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Implements the Copilot turn-boundary event state machine adapted to spike
/// S5's (perles-net-k3j.4, redo against the actually-running 1.0.80 binary)
/// live-verified findings: presence upsert AND the unread-mail digest on
/// <c>sessionStart</c> (the one event S5 live-confirmed can carry
/// <c>additionalContext</c> into the model's context), a presence-only touch
/// on <c>userPromptSubmitted</c> (S5 live-confirmed this event's response
/// body is a no-op on 1.0.80, so no digest is attempted there - see
/// <see cref="CopilotHookHandler.HandleUserPromptSubmitAsync"/>), and
/// presence teardown on <c>sessionEnd</c>. Every member is fail-open by
/// contract, same as <see cref="IClaudeHookHandler"/>/<see cref="ICodexHookHandler"/>.
/// <para>
/// Deliberately out of this ticket's scope, per perles-net-k3j.15's own Fix
/// direction and non-goals: no idle-turn gate. S5 also proved
/// <c>agentStop</c>/<c>Stop</c> is a real, live-verified blocking gate on
/// Copilot (contradicting the plan's original "no turn-end/stop event"
/// premise for this harness), but wiring it is left to a future ticket; the
/// discrepancy is recorded as a task comment, not silently resolved here.
/// </para>
/// </summary>
internal interface ICopilotHookHandler
{
    /// <summary>
    /// Upserts the session's presence row (<c>endpoint_kind = 'none'</c>
    /// always in this ticket's scope: the Copilot extension that would give a
    /// session a reachable <c>copilot-extension</c> endpoint is a sibling
    /// task, perles-net-k3j.16), then returns the unread-mail digest for
    /// messages not yet delivered on the digest channel, if the row is
    /// claimed and any exist. <paramref name="dryRun"/> pins the row's
    /// generation to the same fixed sentinel identity
    /// <see cref="IClaudeHookHandler.HandleSessionStartAsync"/> uses (pid 1,
    /// epoch proc_start).
    /// </summary>
    Task<CopilotHookOutcome> HandleSessionStartAsync(
        CopilotHookPayload payload, bool dryRun, CancellationToken cancellationToken);

    /// <summary>
    /// Always returns <see cref="CopilotHookOutcome.Neutral"/>: spike S5
    /// (redo, perles-net-k3j.4) live-verified that <c>userPromptSubmitted</c>'s
    /// response body is silently dropped by Copilot 1.0.80, unlike the same
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
