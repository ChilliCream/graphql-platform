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

    /// <summary>
    /// A running unified Nitro agent TUI, bound to the durable human mail
    /// actor as an operator participant rather than to a coding-harness hook. See
    /// <see cref="AgentSessionEndpointKind.DbWatch"/>.
    /// </summary>
    public const string NitroBoard = "nitro-board";
}
