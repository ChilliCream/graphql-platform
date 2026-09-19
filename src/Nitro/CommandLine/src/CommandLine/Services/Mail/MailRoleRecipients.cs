using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Services.Mail;

/// <summary>
/// Resolves role-targeted recipients from registered session participants.
/// </summary>
internal static class MailRoleRecipients
{
    /// <summary>
    /// Returns distinct non-implicit actor names whose session role matches the normalized
    /// role, excluding <paramref name="excludingActor"/>. An empty session role falls
    /// back to the actor's role; presence state is not filtered.
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
            .Where(participant => participant.Agent is { Implicit: false } && participant.MatchesRole(normalizedRole))
            .Select(participant => participant.Agent!.Name)
            .Where(name => name != excludingActor)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }
}
