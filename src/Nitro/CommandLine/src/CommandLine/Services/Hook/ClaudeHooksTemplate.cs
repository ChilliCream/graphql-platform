using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// The desired <c>settings.json</c> hook entry for each turn-boundary event this CLI
/// adapts for a harness speaking Claude Code's hook contract, built from a
/// <see cref="LaunchDescriptor"/>.
/// </summary>
internal sealed class ClaudeHooksTemplate
{
    /// <summary>
    /// The entries <c>nitro agent hooks claude</c> manages in Claude Code settings.
    /// </summary>
    public static ClaudeHooksTemplate Claude { get; } =
        new(AgentSessionHarness.ClaudeCode, "claude", null, hasHookSwitches: false, runsWindowsHooksInPowerShell: false);

    /// <summary>
    /// The entries <c>nitro agent hooks venti</c> manages in Venti settings, which must not
    /// share a settings file with <see cref="Claude"/> entries.
    /// </summary>
    public static ClaudeHooksTemplate Venti { get; } =
        new(AgentSessionHarness.Venti, "venti", Claude, hasHookSwitches: true, runsWindowsHooksInPowerShell: true);

    public const int TimeoutSeconds = 10;

    /// <summary>
    /// Hook event names this installer manages, in the order
    /// <c>hooks install</c> and <c>hooks status</c> report them.
    /// </summary>
    public static readonly IReadOnlyList<string> Events =
        ["SessionStart", "UserPromptSubmit", "Stop", "Notification", "SessionEnd"];

    private ClaudeHooksTemplate(
        string harness,
        string commandName,
        ClaudeHooksTemplate? conflicting,
        bool hasHookSwitches,
        bool runsWindowsHooksInPowerShell)
    {
        Harness = harness;
        CommandName = commandName;
        Conflicting = conflicting;
        HasHookSwitches = hasHookSwitches;
        RunsWindowsHooksInPowerShell = runsWindowsHooksInPowerShell;
    }

    /// <summary>
    /// The <c>agents.harness</c> value of the sessions these entries register.
    /// </summary>
    public string Harness { get; }

    /// <summary>
    /// The <c>nitro agent hook</c> subcommand these entries invoke.
    /// </summary>
    public string CommandName { get; }

    /// <summary>
    /// Every entry this template writes contains this literal substring in its command text.
    /// </summary>
    public string CommandMarker => $"agent hook {CommandName} ";

    /// <summary>
    /// The template whose entries must not share a settings file with this template's,
    /// or null when none conflicts.
    /// </summary>
    public ClaudeHooksTemplate? Conflicting { get; }

    /// <summary>
    /// Whether the harness skips a handler whose <c>enabled</c> flag is <c>false</c>, and every
    /// handler while its user settings file sets <c>disableAllHooks</c> to <c>true</c>.
    /// </summary>
    public bool HasHookSwitches { get; }

    /// <summary>
    /// Whether the harness runs hook commands through PowerShell on Windows instead of a POSIX shell.
    /// </summary>
    public bool RunsWindowsHooksInPowerShell { get; }

    public static string EventCommand(string hookEvent) => hookEvent switch
    {
        "SessionStart" => "session-start",
        "UserPromptSubmit" => "user-prompt-submit",
        "Stop" => "stop",
        "Notification" => "notification",
        "SessionEnd" => "session-end",
        _ => throw new ArgumentOutOfRangeException(
            nameof(hookEvent), hookEvent, "Not a hook event this installer manages.")
    };

    /// <summary>
    /// The exact command text <c>hooks install</c> writes (and
    /// <c>hooks status</c> compares against) for <paramref name="hookEvent"/>
    /// given <paramref name="descriptor"/>.
    /// </summary>
    public string BuildCommand(LaunchDescriptor descriptor, string hookEvent)
    {
        string[] argv = ["agent", "hook", CommandName, EventCommand(hookEvent)];

        return RunsWindowsHooksInPowerShell && OperatingSystem.IsWindows()
            ? descriptor.BuildPowerShellCommandLine(argv)
            : descriptor.BuildCommandLine(argv);
    }
}
