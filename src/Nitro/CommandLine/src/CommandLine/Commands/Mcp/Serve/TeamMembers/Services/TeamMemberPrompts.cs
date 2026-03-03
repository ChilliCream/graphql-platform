using System.ComponentModel;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.TeamMembers.Models;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.TeamMembers.Services;

[McpServerPromptType]
internal static class TeamMemberPrompts
{
    [McpServerPrompt(Name = "backend_engineer")]
    [Description(
        "A Hot Chocolate backend engineering expert who helps with resolvers, "
            + "types, DataLoaders, middleware, and server configuration")]
    public static IEnumerable<PromptMessage> BackendEngineer([Description("What you need help with")] string task)
        => BuildMessages("backend_engineer", task);

    [McpServerPrompt(Name = "frontend_engineer")]
    [Description(
        "A React/Relay frontend expert who helps with queries, fragments, "
            + "components, code generation, and testing")]
    public static IEnumerable<PromptMessage> FrontendEngineer([Description("What you need help with")] string task)
        => BuildMessages("frontend_engineer", task);

    [McpServerPrompt(Name = "graphql_expert")]
    [Description(
        "A GraphQL schema design authority who helps with naming, nullability, "
            + "Relay compliance, and federation patterns")]
    public static IEnumerable<PromptMessage> GraphqlExpert([Description("What you need help with")] string task)
        => BuildMessages("graphql_expert", task);

    [McpServerPrompt(Name = "schema_reviewer")]
    [Description(
        "A schema change reviewer who analyzes breaking changes, " + "usage impact, and deprecation management")]
    public static IEnumerable<PromptMessage> SchemaReviewer([Description("What you need help with")] string task)
        => BuildMessages("schema_reviewer", task);

    [McpServerPrompt(Name = "performance_engineer")]
    [Description(
        "A GraphQL performance specialist who helps with N+1 detection, "
            + "DataLoader optimization, and query complexity")]
    public static IEnumerable<PromptMessage> PerformanceEngineer([Description("What you need help with")] string task)
        => BuildMessages("performance_engineer", task);

    [McpServerPrompt(Name = "devops_engineer")]
    [Description(
        "A deployment and operations expert who helps with stages, CI/CD, " + "monitoring, and Fusion configuration")]
    public static IEnumerable<PromptMessage> DevopsEngineer([Description("What you need help with")] string task)
        => BuildMessages("devops_engineer", task);

    [McpServerPrompt(Name = "security_engineer")]
    [Description(
        "A GraphQL security expert who helps with authorization, " + "input validation, rate limiting, and CORS")]
    public static IEnumerable<PromptMessage> SecurityEngineer([Description("What you need help with")] string task)
        => BuildMessages("security_engineer", task);

    [McpServerPrompt(Name = "testing_engineer")]
    [Description(
        "A test patterns expert who helps with schema tests, resolver tests, " + "snapshot tests, and CookieCrumble")]
    public static IEnumerable<PromptMessage> TestingEngineer([Description("What you need help with")] string task)
        => BuildMessages("testing_engineer", task);

    [McpServerPrompt(Name = "relay_expert")]
    [Description(
        "A Relay specification expert who helps with global IDs, connections, " + "mutations, and Strawberry Shake")]
    public static IEnumerable<PromptMessage> RelayExpert([Description("What you need help with")] string task)
        => BuildMessages("relay_expert", task);

    private static IEnumerable<PromptMessage> BuildMessages(string memberId, string task)
    {
        var member = TeamMemberProvider.Instance.GetById(memberId);

        if (member is null)
        {
            yield return new PromptMessage
            {
                Role = Role.Assistant,
                Content = new TextContentBlock { Text = $"Unknown team member: {memberId}" }
            };
            yield break;
        }

        yield return new PromptMessage
        {
            Role = Role.User,
            Content = new TextContentBlock { Text = $"I need help from the {member.Title}. Task: {task}" }
        };

        yield return new PromptMessage
        {
            Role = Role.Assistant,
            Content = new TextContentBlock
            {
                Text =
                    member.PersonaText
                    + "\n\n---\n\n"
                    + $"I'm ready to help you with: **{task}**\n\n"
                    + "Let me start by understanding your current setup."
            }
        };
    }
}
