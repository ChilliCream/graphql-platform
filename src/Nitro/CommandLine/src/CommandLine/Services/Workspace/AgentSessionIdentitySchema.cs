namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

internal static class AgentSessionIdentitySchema
{
    public const string Create =
        """
        CREATE TABLE IF NOT EXISTS agent_session_identities (
            harness TEXT NOT NULL CHECK (harness IN ('claude-code', 'codex', 'copilot')),
            session_id TEXT NOT NULL,
            actor TEXT NOT NULL UNIQUE REFERENCES agents (name),
            role TEXT NOT NULL DEFAULT '',
            actor_revision INTEGER NOT NULL DEFAULT 1 CHECK (actor_revision > 0),
            created_at TEXT NOT NULL,
            last_seen_at TEXT NOT NULL,
            PRIMARY KEY (harness, session_id)
        );

        CREATE INDEX IF NOT EXISTS idx_agent_session_identities_actor
            ON agent_session_identities (actor);
        """;
}
