namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// The <c>agent_sessions.endpoint_kind</c> values, matching the table's
/// CHECK constraint.
/// </summary>
internal static class AgentSessionEndpointKind
{
    public const string ClaudePeer = "claude-peer";
    public const string CodexThread = "codex-thread";
    public const string CopilotExtension = "copilot-extension";

    /// <summary>
    /// A Nitro board session's endpoint: the shared workspace SQLite file
    /// itself. A message addressed to this endpoint's actor is already
    /// delivered the moment it commits - the board's own db-file watcher
    /// observes the change and refreshes - so it carries no routable peer
    /// or thread id and no transport ever fires against it.
    /// </summary>
    public const string DbWatch = "db-watch";

    public const string None = "none";
}
