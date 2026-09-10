using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tui.Agents;

/// <summary>
/// The identity a live participant row is selected and diffed by across a
/// refresh: the harness plus its session id, never the bound actor name,
/// since two sessions can share one actor and a session can be unbound.
/// </summary>
internal readonly record struct AgentSessionKey(string Harness, string SessionId)
{
    public static AgentSessionKey From(AgentSessionRecord session) => new(session.Harness, session.SessionId);
}
