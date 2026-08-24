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
    /// Returns the distinct normalized names of every durable actor bound to
    /// a live session whose role equals <paramref name="role"/> (normalized),
    /// excluding <paramref name="excludingActor"/>. An identity with no live
    /// session, and a live session that is unbound or claims a different
    /// role, are never returned.
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
            .Where(participant => participant.Agent is not null && participant.Session.Role == normalizedRole)
            .Select(participant => participant.Agent!.Name)
            .Where(name => name != excludingActor)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }
}
