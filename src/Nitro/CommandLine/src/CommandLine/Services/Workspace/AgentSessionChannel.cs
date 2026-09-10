namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// The <c>session_deliveries.channel</c> values, matching the table's CHECK
/// constraint. The delivery ledger reserves at most once per (message,
/// channel) pair, so a digest lost to a crash never suppresses the gate or a
/// ping for the same message.
/// </summary>
internal static class AgentSessionChannel
{
    public const string Digest = "digest";
    public const string Gate = "gate";
    public const string Ping = "ping";
}
