namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// The observable presence states <see cref="IAgentSessionRegistry.ListAsync"/>
/// computes for a surviving row: <c>online</c> (current instance, an
/// endpoint is registered), <c>unreachable</c> (current instance, no
/// endpoint), <c>unobservable</c> (current instance, but this reader cannot
/// tell whether its process is alive, typically a different PID namespace
/// than the row's writer recorded), or <c>remote</c> (recorded by a
/// different Nitro instance id, never reaped or pinged from here). A
/// current-instance row this reader can prove dead never reaches this
/// projection: it is reaped on read instead of being reported as offline.
/// </summary>
internal static class AgentSessionState
{
    public const string Online = "online";
    public const string Unreachable = "unreachable";
    public const string Unobservable = "unobservable";
    public const string Remote = "remote";
}
