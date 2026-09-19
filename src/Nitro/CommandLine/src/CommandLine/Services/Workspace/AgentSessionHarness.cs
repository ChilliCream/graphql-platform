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
}
