namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Actor presence states, including offline when the actor has no surviving sessions.
/// </summary>
internal static class AgentPresenceState
{
    public const string Offline = "offline";
    public const string Online = AgentSessionState.Online;
    public const string Unreachable = AgentSessionState.Unreachable;
    public const string Unobservable = AgentSessionState.Unobservable;
    public const string Remote = AgentSessionState.Remote;
}
