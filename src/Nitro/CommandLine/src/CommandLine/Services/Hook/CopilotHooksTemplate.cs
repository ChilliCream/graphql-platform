namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// The desired Copilot CLI hooks-dir entry for each turn-boundary event this
/// CLI adapts, built from a <see cref="LaunchDescriptor"/>. The Copilot
/// analog of <see cref="ClaudeHooksTemplate"/>/<see cref="CodexHooksTemplate"/>:
/// registers only the canonical camelCase event keys spike S5 (redo,
/// perles-net-k3j.4) confirmed live (not the Claude-Code-compat PascalCase
/// alias keys, which yield a different, snake_case payload shape this
/// adapter does not parse).
/// </summary>
internal static class CopilotHooksTemplate
{
    /// <summary>
    /// Every hooks-dir entry this CLI ever writes contains this literal
    /// substring in its command text - the Copilot analog of
    /// <see cref="ClaudeHooksTemplate.CommandMarker"/>.
    /// </summary>
    public const string CommandMarker = "agent hook copilot ";

    public const int TimeoutSeconds = 10;

    /// <summary>
    /// Copilot hooks-dir event names this installer manages, in the order
    /// <c>hooks copilot install</c> and <c>hooks copilot status</c> report
    /// them.
    /// </summary>
    public static readonly IReadOnlyList<string> Events = ["sessionStart", "userPromptSubmitted", "sessionEnd"];

    public static string EventCommand(string copilotEvent) => copilotEvent switch
    {
        "sessionStart" => "session-start",
        "userPromptSubmitted" => "user-prompt-submit",
        "sessionEnd" => "session-end",
        _ => throw new ArgumentOutOfRangeException(
            nameof(copilotEvent), copilotEvent, "Not a hooks-dir event this installer manages.")
    };

    /// <summary>
    /// The exact command text <c>hooks copilot install</c> writes (and
    /// <c>hooks copilot status</c> compares against) for
    /// <paramref name="copilotEvent"/> given <paramref name="descriptor"/>.
    /// </summary>
    public static string BuildCommand(LaunchDescriptor descriptor, string copilotEvent)
        => descriptor.BuildCommandLine(["agent", "hook", "copilot", EventCommand(copilotEvent)]);
}
