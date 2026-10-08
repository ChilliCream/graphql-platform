namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Notification channels with independent delivery reservations for each session and message.
/// </summary>
internal static class AgentSessionChannel
{
    public const string Digest = "digest";
    public const string Gate = "gate";
    public const string Ping = "ping";
}
