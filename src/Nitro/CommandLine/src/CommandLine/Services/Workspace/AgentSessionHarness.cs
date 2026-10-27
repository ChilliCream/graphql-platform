namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// The <c>agents.harness</c> values, matching the table's CHECK
/// constraint.
/// </summary>
internal static class AgentSessionHarness
{
    public const string ClaudeCode = "claude-code";
    public const string Codex = "codex";
    public const string Copilot = "copilot";
    public const string Opencode = "opencode";
    public const string Venti = "venti";

    /// <summary>
    /// Returns <c>true</c> when <paramref name="harness"/> is one of the coding agent harnesses
    /// (<see cref="ClaudeCode"/>, <see cref="Codex"/>, <see cref="Copilot"/>, <see cref="Opencode"/> or
    /// <see cref="Venti"/>).
    /// </summary>
    public static bool IsAgentHarness(string harness)
        => harness is ClaudeCode or Codex or Copilot or Opencode or Venti;
}
