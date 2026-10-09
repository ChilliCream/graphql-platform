namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// The <c>agents.endpoint_kind</c> values, matching the table's
/// CHECK constraint.
/// </summary>
internal static class AgentSessionEndpointKind
{
    public const string ClaudePeer = "claude-peer";
    public const string CodexThread = "codex-thread";
    public const string CopilotExtension = "copilot-extension";
    public const string OpencodeServer = "opencode-server";

    /// <summary>
    /// A Nitro board endpoint that observes mail through the shared workspace database.
    /// </summary>
    public const string DbWatch = "db-watch";

    public const string None = "none";
}
