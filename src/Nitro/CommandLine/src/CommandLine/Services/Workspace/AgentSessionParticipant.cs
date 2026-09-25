namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// A session, its associated agent identity when available, and its computed presence state.
/// </summary>
internal sealed record AgentSessionParticipant(AgentSessionRecord Session, AgentRecord? Agent, string State)
{
    /// <summary>
    /// True when an associated agent exists and the session role matches
    /// <paramref name="normalizedRole"/>, or the session role is empty and the agent role matches.
    /// </summary>
    public bool MatchesRole(string normalizedRole)
        => Agent is not null
            && (Session.Role == normalizedRole || (Session.Role.Length is 0 && Agent.Role == normalizedRole));
}
