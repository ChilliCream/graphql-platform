namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// What a Codex <c>hooks.json</c> event handler decided to tell Codex,
/// harness-agnostic in the same spirit as <see cref="ClaudeHookOutcome"/>.
/// </summary>
internal sealed record CodexHookOutcome
{
    /// <summary>
    /// The neutral outcome every fail-open path and every event with nothing
    /// to say returns: no context to inject.
    /// </summary>
    public static readonly CodexHookOutcome Neutral = new();

    /// <summary>
    /// Text to inject as additional context, or null for none.
    /// </summary>
    public string? AdditionalContext { get; init; }
}
