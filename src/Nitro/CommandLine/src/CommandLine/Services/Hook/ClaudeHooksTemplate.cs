namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// The desired Claude Code <c>settings.json</c> hook entry for each
/// turn-boundary event this CLI adapts, built from a
/// <see cref="LaunchDescriptor"/>. The single source of truth both
/// <c>hooks install</c> (what to write) and <c>hooks status</c> (what
/// "current" means, for drift detection) compare against.
/// </summary>
internal static class ClaudeHooksTemplate
{
    /// <summary>
    /// Every entry this CLI ever writes contains this literal substring in
    /// its command text. An installed entry is recognized as Nitro-owned by
    /// this marker, independent of which machine, install mode, or Nitro
    /// version wrote it, and independent of the sidecar (which records exact
    /// provenance for precise, low-risk removal, see
    /// <c>ClaudeHooksInstallerService</c>).
    /// </summary>
    public const string CommandMarker = "agent hook claude ";

    public const int TimeoutSeconds = 10;

    /// <summary>
    /// Claude Code hook event names this installer manages, in the order
    /// <c>hooks install</c> and <c>hooks status</c> report them.
    /// </summary>
    public static readonly IReadOnlyList<string> Events = ["SessionStart", "UserPromptSubmit", "Stop", "SessionEnd"];

    public static string EventCommand(string claudeEvent) => claudeEvent switch
    {
        "SessionStart" => "session-start",
        "UserPromptSubmit" => "user-prompt-submit",
        "Stop" => "stop",
        "SessionEnd" => "session-end",
        _ => throw new ArgumentOutOfRangeException(
            nameof(claudeEvent), claudeEvent, "Not a hook event this installer manages.")
    };

    /// <summary>
    /// The exact command text <c>hooks install</c> writes (and
    /// <c>hooks status</c> compares against) for <paramref name="claudeEvent"/>
    /// given <paramref name="descriptor"/>.
    /// </summary>
    public static string BuildCommand(LaunchDescriptor descriptor, string claudeEvent)
        => descriptor.BuildCommandLine(["agent", "hook", "claude", EventCommand(claudeEvent)]);
}
