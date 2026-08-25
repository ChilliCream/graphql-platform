namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// The desired Codex CLI <c>hooks.json</c> hook entry for each turn-boundary
/// event this CLI adapts, built from a <see cref="LaunchDescriptor"/>. The
/// Codex analog of <see cref="ClaudeHooksTemplate"/>: the same command-line
/// group, command, and timeout structure, with three events instead of
/// four (Codex has no <c>Stop</c>-equivalent hooks.json event - its idle-turn
/// gate is the separate <c>notify</c> mechanism, see
/// <see cref="CodexNotifyTemplate"/>).
/// </summary>
internal static class CodexHooksTemplate
{
    /// <summary>
    /// Every hooks.json entry this CLI ever writes contains this literal
    /// substring in its command text - the Codex analog of
    /// <see cref="ClaudeHooksTemplate.CommandMarker"/>.
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

/// <summary>
/// The desired Codex CLI <c>config.toml</c> <c>notify</c> program this
/// installer writes: an argv array (not a single shell command string like
/// <see cref="CodexHooksTemplate"/> - Codex invokes <c>notify</c> directly,
/// no shell), always exactly this CLI's launch descriptor followed by
/// <c>agent hook codex notify</c>. Wrapping a foreign notify value never
/// changes this array itself: the wrapping happens INSIDE the installed
/// <c>notify</c> command, which execs the foreign program (recorded in the
/// sidecar) after doing its own work.
/// </summary>
internal static class CodexNotifyTemplate
{
    public static IReadOnlyList<string> BuildArgv(LaunchDescriptor descriptor)
    {
        var argv = new List<string>(1 + descriptor.ArgumentPrefix.Count + 3)
        {
            descriptor.Executable
        };

        argv.AddRange(descriptor.ArgumentPrefix);
        argv.AddRange(["agent", "hook", "codex", "notify"]);

        return argv;
    }
}
