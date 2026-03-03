using System.ComponentModel;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.TeamMembers.Models;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.TeamMembers.Services;
using ModelContextProtocol.Server;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.TeamMembers.Tools;

[McpServerToolType]
internal static class GetTeamMemberTool
{
    [McpServerTool(Name = "get_team_member")]
    [Description(
        "Returns the full persona definition for a Nitro team member. "
            + "Call this at the start of a session to adopt the perspective of a domain expert. "
            + "The returned text instructs you how to behave, what to focus on, "
            + "and which other tools to call.")]
    public static string Get([Description("The team member persona to load.")] TeamMemberRole member)
    {
        var provider = TeamMemberProvider.Instance;

        var teamMember = provider.GetById(member.ToString()) ?? throw TeamMembersThrowHelper.UnknownTeamMember(member);

        return teamMember.PersonaText;
    }
}
