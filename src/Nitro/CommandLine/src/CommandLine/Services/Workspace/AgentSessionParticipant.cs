namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// One live harness session paired with the durable <see cref="AgentRecord"/>
/// its <see cref="AgentSessionRecord.AgentName"/> binds to, or null when the
/// session is unbound. One row per active <c>agent_sessions</c> entry, as
/// <see cref="AgentSessionRegistry.ListParticipantsAsync"/> returns it.
/// No command reads this yet; a later bead wires a caller and DI
/// registration.
/// </summary>
internal sealed record AgentSessionParticipant(AgentSessionRecord Session, AgentRecord? Agent);
