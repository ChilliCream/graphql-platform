namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Builds the Codex hook commands for the managed events from a <see cref="LaunchDescriptor"/>.
/// </summary>
internal static class CodexHooksTemplate
{
    /// <summary>
    /// Every hooks.json entry this CLI ever writes contains this literal substring
    /// in its command text.
    /// </summary>
    public const string CommandMarker = "agent hook codex ";

    public const int TimeoutSeconds = 10;

    /// <summary>
    /// Codex hooks.json event names this installer manages, in the order
    /// <c>hooks codex install</c> and <c>hooks codex status</c> report them.
    /// </summary>
    public static readonly IReadOnlyList<string> Events = ["SessionStart", "UserPromptSubmit", "SessionEnd"];

    public static string EventCommand(string codexEvent) => codexEvent switch
    {
        "SessionStart" => "session-start",
        "UserPromptSubmit" => "user-prompt-submit",
        "SessionEnd" => "session-end",
        _ => throw new ArgumentOutOfRangeException(
            nameof(codexEvent), codexEvent, "Not a hooks.json event this installer manages.")
    };

    /// <summary>
    /// The exact command text <c>hooks codex install</c> writes (and
    /// <c>hooks codex status</c> compares against) for
    /// <paramref name="codexEvent"/> given <paramref name="descriptor"/>.
    /// </summary>
    public static string BuildCommand(LaunchDescriptor descriptor, string codexEvent)
        => descriptor.BuildCommandLine(["agent", "hook", "codex", EventCommand(codexEvent)]);
}
