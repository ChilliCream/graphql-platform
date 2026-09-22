namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Session presence values: online for a local session with an endpoint, unreachable
/// for a local session without one, and remote for another Nitro instance.
/// Unobservable is retained as a legacy value.
/// </summary>
internal static class AgentSessionState
{
    public const string Online = "online";
    public const string Unreachable = "unreachable";
    public const string Unobservable = "unobservable";
    public const string Remote = "remote";
}
