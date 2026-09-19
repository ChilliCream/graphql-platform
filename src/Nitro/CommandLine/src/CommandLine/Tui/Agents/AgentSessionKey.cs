using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tui.Agents;

/// <summary>
/// The harness and session id identifying a participant across list refreshes.
/// </summary>
internal readonly record struct AgentSessionKey(string Harness, string SessionId)
{
    public static AgentSessionKey From(AgentSessionRecord session) => new(session.Harness, session.SessionId);
}
