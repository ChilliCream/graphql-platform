using ModelContextProtocol;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.TeamMembers;

internal static class TeamMembersThrowHelper
{
    public static InvalidOperationException EmbeddedResourceNotFound(string resourceName)
    {
        return new InvalidOperationException($"Embedded resource '{resourceName}' not found.");
    }

    public static McpException UnknownTeamMember(object member)
    {
        return new McpException($"Unknown team member: '{member}'.");
    }
}
