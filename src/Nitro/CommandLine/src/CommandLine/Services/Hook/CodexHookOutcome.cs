namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// The additional context returned by a Codex hook handler.
/// </summary>
internal sealed record CodexHookOutcome
{
    /// <summary>
    /// An outcome with no additional context.
    /// </summary>
    public static readonly CodexHookOutcome Neutral = new();

    /// <summary>
    /// Text to inject as additional context, or null for none.
    /// </summary>
    public string? AdditionalContext { get; init; }
}
