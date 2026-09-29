namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Defines the four workspace-wide ping-lease slots.
/// </summary>
internal static class PingLeaseSchema
{
    public const string Create =
        """
        CREATE TABLE IF NOT EXISTS ping_leases (
            slot INTEGER PRIMARY KEY CHECK (slot BETWEEN 1 AND 4),
            attempt_id TEXT NOT NULL,
            acquired_at TEXT NOT NULL,
            expires_at TEXT NOT NULL
        );
        """;
}
