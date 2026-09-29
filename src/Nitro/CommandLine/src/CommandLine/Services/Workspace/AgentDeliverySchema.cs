namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Defines per-channel delivery reservations keyed by agent name.
/// </summary>
internal static class AgentDeliverySchema
{
    public const string Create =
        """
        CREATE TABLE IF NOT EXISTS agent_deliveries (
            agent TEXT NOT NULL REFERENCES agents (name),
            message_id TEXT NOT NULL,
            channel TEXT NOT NULL CHECK (channel IN ('digest', 'gate', 'ping')),
            delivered_at TEXT NOT NULL,
            PRIMARY KEY (agent, message_id, channel)
        );
        """;
}
