using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Services.Mail;

/// <summary>
/// Resolves role-targeted mail recipients from live orchestration
/// participants, so a role broadcast only reaches an actor with a session
/// currently claiming that role.
/// </summary>
internal static class MailRoleRecipients
{
    /// <summary>
    /// Returns the distinct normalized names of every durable, non-implicit
    /// actor bound to a live session whose own role equals
    /// <paramref name="role"/> (normalized), excluding
    /// <paramref name="excludingActor"/>. When the session's own role is
    /// blank, the durable identity's role is matched instead: a session
    /// bound before role-aware registration never had its own role written.
    /// An identity with no live session, and a live session that is unbound,
    /// implicit, or matches neither role, are never returned.
    /// </summary>
    public static async Task<IReadOnlyList<string>> ResolveAsync(
        IAgentSessionRegistry sessions,
        string role,
        string excludingActor,
        CancellationToken cancellationToken)
    {
        var normalizedRole = AgentRole.Normalize(role);
        var participants = await sessions.ListParticipantsAsync(cancellationToken);

        return participants
            .Where(participant => participant.Agent is { Implicit: false } agent
                && MatchesRole(participant.Session, agent, normalizedRole))
            .Select(participant => participant.Agent!.Name)
            .Where(name => name != excludingActor)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static bool MatchesRole(AgentSessionRecord session, AgentRecord agent, string normalizedRole)
        => session.Role == normalizedRole
            || (session.Role.Length is 0 && agent.Role == normalizedRole);
}
