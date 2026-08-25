namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// What a Copilot CLI hook event handler decided to tell Copilot,
/// harness-agnostic in the same spirit as <see cref="ClaudeHookOutcome"/> and
/// <see cref="CodexHookOutcome"/>. This adapter does not use Copilot's
/// <c>agentStop</c> hook for blocking a turn, so this carries only context
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
