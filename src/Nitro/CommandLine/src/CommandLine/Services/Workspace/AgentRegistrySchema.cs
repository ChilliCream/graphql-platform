namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

internal static class AgentRegistrySchema
{
    /// <summary>
    /// The complete schema for a freshly created database. Statements are
    /// idempotent so applying them to an existing database is
    /// non-destructive; upgrading an existing v2 database's agents table
    /// (created before role and implicit existed) is a separate, explicit
    /// step in <see cref="AgentDatabase"/>.
    /// </summary>
    public const string Create =
        """
        CREATE TABLE IF NOT EXISTS agents (
            name TEXT PRIMARY KEY,
            registered_at TEXT NOT NULL,
            last_seen_at TEXT NOT NULL,
            role TEXT NOT NULL DEFAULT '',
            implicit INTEGER NOT NULL DEFAULT 0 CHECK (implicit IN (0, 1))
        );
        """;
}
