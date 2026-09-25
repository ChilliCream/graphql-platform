namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

internal static class AgentRegistrySchema
{
    /// <summary>
    /// Creates the agent registry table if it does not exist; existing tables are unchanged.
    /// </summary>
    public const string Create =
        """
        CREATE TABLE IF NOT EXISTS agents (
            name TEXT PRIMARY KEY,
            registered_at TEXT NOT NULL,
            last_seen_at TEXT NOT NULL,
            role TEXT NOT NULL DEFAULT '',
            implicit INTEGER NOT NULL DEFAULT 0 CHECK (implicit IN (0, 1)),
            client TEXT NOT NULL DEFAULT ''
        );
        """;
}
