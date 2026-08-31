namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// One live harness session paired with the durable <see cref="AgentRecord"/>
/// its <see cref="AgentSessionRecord.AgentName"/> binds to, or null when the
/// session is unbound, and its computed <see cref="AgentSessionState"/>. One
/// row per active <c>agent_sessions</c> entry, as
/// <see cref="IAgentSessionRegistry.ListParticipantsAsync"/> returns it.
/// </summary>
internal sealed record AgentSessionParticipant(AgentSessionRecord Session, AgentRecord? Agent, string State)
{
    /// <summary>
    /// True when this participant is bound and its role, or its durable
    /// identity's role when the session's own role is blank, equals
    /// <paramref name="normalizedRole"/>. A session bound before role-aware
    /// registration never had its own role written, so the durable identity's
    /// role is matched instead. An unbound participant never matches.
    /// </summary>
    public bool MatchesRole(string normalizedRole)
        => Agent is not null
            && (Session.Role == normalizedRole || (Session.Role.Length is 0 && Agent.Role == normalizedRole));
}
