namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// What a Copilot CLI hook event handler decided to tell Copilot,
/// harness-agnostic in the same spirit as <see cref="ClaudeHookOutcome"/> and
/// <see cref="CodexHookOutcome"/>. Copilot has no hook this adapter uses for
/// blocking a turn (spike S5 redo live-verified that <c>agentStop</c> is a
/// real blocking gate, but wiring it is out of this ticket's scope, see
/// <see cref="ICopilotHookHandler"/>), so this carries only context
/// injection.
/// </summary>
internal sealed record CopilotHookOutcome
{
    /// <summary>
    /// The neutral outcome every fail-open path and every event with nothing
    /// to say returns: no context to inject.
    /// </summary>
    public static readonly CopilotHookOutcome Neutral = new();

    /// <summary>
    /// Text to inject as additional context, or null for none.
    /// </summary>
    public string? AdditionalContext { get; init; }
}
