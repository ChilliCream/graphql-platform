namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Maps a canonical harness id to the display name shown by the CLI list and the board.
/// </summary>
internal static class AgentHarnessDisplay
{
    /// <summary>
    /// Returns the display name for <paramref name="harness"/>, or <c>-</c> for a login-only agent.
    /// </summary>
    public static string Name(string? harness) => harness switch
    {
        "claude-code" => "Claude Code",
        "codex" => "Codex",
        "copilot" => "Copilot",
        "opencode" => "OpenCode",
        _ => "-"
    };
}
