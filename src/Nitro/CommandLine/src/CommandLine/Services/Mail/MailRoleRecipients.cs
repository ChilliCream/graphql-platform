using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Services.Mail;

/// <summary>
/// Resolves role-targeted recipients from the agents table.
/// </summary>
internal static class MailRoleRecipients
{
    /// <summary>
    /// Returns distinct, non-deleted agent names whose role matches the normalized role,
    /// excluding <paramref name="excludingActor"/>.
    /// </summary>
    public static async Task<IReadOnlyList<string>> ResolveAsync(
        IAgentStore agents,
        string role,
        string excludingActor,
        CancellationToken cancellationToken)
    {
        var normalizedRole = AgentRole.Normalize(role);
        var rows = await agents.ListAsync(cancellationToken);

        return rows
            .Where(agent => agent.Role == normalizedRole)
            .Select(agent => agent.Name)
            .Where(name => name != excludingActor)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }
}
