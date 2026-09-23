namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// The <c>agent_sessions.harness</c> values, matching the table's CHECK
/// constraint.
/// </summary>
internal static class AgentSessionHarness
{
    public const string ClaudeCode = "claude-code";
    public const string Codex = "codex";
    public const string Copilot = "copilot";
    public const string Opencode = "opencode";

    /// <summary>
    /// A Nitro agent board session.
    /// </summary>
    public const string NitroBoard = "nitro-board";

    /// <summary>
    /// Returns <c>true</c> when <paramref name="harness"/> is one of the coding agent harnesses
    /// (<see cref="ClaudeCode"/>, <see cref="Codex"/>, <see cref="Copilot"/> or <see cref="Opencode"/>).
    /// </summary>
    public static bool IsAgentHarness(string harness)
        => harness is ClaudeCode or Codex or Copilot or Opencode;
}
