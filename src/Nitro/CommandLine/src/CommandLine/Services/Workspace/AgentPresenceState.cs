namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// The presence states shown to humans for an agent IDENTITY (as opposed to
/// <see cref="AgentSessionState"/>, which is per-session). Adds
/// <c>offline</c> - zero live sessions bound to the agent - to the three
/// states <see cref="IAgentSessionRegistry.ListAsync"/> computes per row.
/// </summary>
internal static class AgentPresenceState
{
    public const string Offline = "offline";
    public const string Online = AgentSessionState.Online;
    public const string Unreachable = AgentSessionState.Unreachable;
    public const string Unobservable = AgentSessionState.Unobservable;
    public const string Remote = AgentSessionState.Remote;
}
