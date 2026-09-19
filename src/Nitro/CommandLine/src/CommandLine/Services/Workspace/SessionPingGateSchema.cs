namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Defines transport leases keyed by harness, session id, and host.
/// Deleting session presence does not remove these leases.
/// </summary>
internal static class SessionPingGateSchema
{
    /// <summary>
    /// The columns and constraints of the session transport lease table.
    /// </summary>
    private const string SessionPingGatesColumns =
        """
            harness TEXT NOT NULL CHECK (harness IN ('claude-code', 'codex', 'copilot', 'opencode', 'nitro-board')),
            session_id TEXT NOT NULL,
            host TEXT NOT NULL,
            attempt_id TEXT NOT NULL,
            acquired_at TEXT NOT NULL,
            expires_at TEXT NOT NULL,
            PRIMARY KEY (harness, session_id, host)
        """;

    public const string Create =
        """
        CREATE TABLE IF NOT EXISTS session_ping_gates (
        """
        + SessionPingGatesColumns
        + """

        );

        CREATE INDEX IF NOT EXISTS idx_session_ping_gates_expires ON session_ping_gates (expires_at);
        """;

    /// <summary>
    /// Returns SQL to create session transport leases under <paramref name="tableName"/>.
    /// </summary>
    public static string CreateSessionPingGatesTable(string tableName) =>
        $"""
        CREATE TABLE "{tableName}" (
        {SessionPingGatesColumns}
        );
        """;
}
