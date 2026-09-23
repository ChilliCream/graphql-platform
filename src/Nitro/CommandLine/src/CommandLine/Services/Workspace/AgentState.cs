namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// The presence of an agent, derived from its row at read or render time and never stored.
/// </summary>
internal enum AgentState
{
    Online,
    Unreachable,
    Offline
}
