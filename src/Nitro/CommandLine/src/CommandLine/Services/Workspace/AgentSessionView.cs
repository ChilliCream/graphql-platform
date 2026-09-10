namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// An <see cref="AgentSessionRecord"/> paired with its computed
/// <see cref="AgentSessionState"/>, as returned by
/// <see cref="IAgentSessionRegistry.ListAsync"/>.
/// </summary>
internal sealed record AgentSessionView(AgentSessionRecord Session, string State);
