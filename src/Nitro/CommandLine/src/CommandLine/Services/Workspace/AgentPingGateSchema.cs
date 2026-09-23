namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Defines the transport lease keyed by agent name.
/// </summary>
internal static class AgentPingGateSchema
{
    public const string Create =
        """
        CREATE TABLE IF NOT EXISTS agent_ping_gates (
            agent TEXT PRIMARY KEY REFERENCES agents (name),
            attempt_id TEXT NOT NULL,
            acquired_at TEXT NOT NULL,
            expires_at TEXT NOT NULL
        );

        CREATE INDEX IF NOT EXISTS idx_agent_ping_gates_expires ON agent_ping_gates (expires_at);
        """;
}
