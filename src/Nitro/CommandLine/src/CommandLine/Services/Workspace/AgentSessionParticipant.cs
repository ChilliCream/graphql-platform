namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// One live harness session paired with the durable <see cref="AgentRecord"/>
/// its <see cref="AgentSessionRecord.AgentName"/> binds to, or null when the
/// session is unbound, and its computed <see cref="AgentSessionState"/>. One
/// row per active <c>agent_sessions</c> entry, as
/// <see cref="IAgentSessionRegistry.ListParticipantsAsync"/> returns it.
/// </summary>
internal sealed record AgentSessionParticipant(AgentSessionRecord Session, AgentRecord? Agent, string State);
