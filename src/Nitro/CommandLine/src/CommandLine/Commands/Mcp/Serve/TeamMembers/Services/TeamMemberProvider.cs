using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.TeamMembers.Models;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.TeamMembers.Services;

internal sealed class TeamMemberProvider
{
    private static readonly Lazy<TeamMemberProvider> _instance = new(() => new TeamMemberProvider());

    public static TeamMemberProvider Instance => _instance.Value;

    private readonly IReadOnlyDictionary<string, TeamMember> _members;

    private TeamMemberProvider()
    {
        _members = BuildMembers();
    }

    public IEnumerable<TeamMember> GetAll() => _members.Values;

    public TeamMember? GetById(string id) => _members.GetValueOrDefault(id);

    private static Dictionary<string, TeamMember> BuildMembers()
    {
        return new Dictionary<string, TeamMember>(StringComparer.Ordinal)
        {
            ["backend_engineer"] = new(
                "backend_engineer",
                "Backend Engineer",
                "A Hot Chocolate backend engineering expert who helps with resolvers, "
                    + "types, DataLoaders, middleware, and server configuration",
                ReadResource("backend_engineer.md")),

            ["frontend_engineer"] = new(
                "frontend_engineer",
                "Frontend Engineer",
                "A React/Relay frontend expert who helps with queries, fragments, "
                    + "components, code generation, and testing",
                ReadResource("frontend_engineer.md")),

            ["graphql_expert"] = new(
                "graphql_expert",
                "GraphQL Expert",
                "A GraphQL schema design authority who helps with naming, nullability, "
                    + "Relay compliance, and federation patterns",
                ReadResource("graphql_expert.md")),

            ["schema_reviewer"] = new(
                "schema_reviewer",
                "Schema Reviewer",
                "A schema change reviewer who analyzes breaking changes, usage impact, " + "and deprecation management",
                ReadResource("schema_reviewer.md")),

            ["performance_engineer"] = new(
                "performance_engineer",
                "Performance Engineer",
                "A GraphQL performance specialist who helps with N+1 detection, "
                    + "DataLoader optimization, and query complexity",
                ReadResource("performance_engineer.md")),

            ["devops_engineer"] = new(
                "devops_engineer",
                "DevOps Engineer",
                "A deployment and operations expert who helps with stages, CI/CD, "
                    + "monitoring, and Fusion configuration",
                ReadResource("devops_engineer.md")),

            ["security_engineer"] = new(
                "security_engineer",
                "Security Engineer",
                "A GraphQL security expert who helps with authorization, input validation, "
                    + "rate limiting, and CORS",
                ReadResource("security_engineer.md")),

            ["testing_engineer"] = new(
                "testing_engineer",
                "Testing Engineer",
                "A test patterns expert who helps with schema tests, resolver tests, "
                    + "snapshot tests, and CookieCrumble",
                ReadResource("testing_engineer.md")),

            ["relay_expert"] = new(
                "relay_expert",
                "Relay Expert",
                "A Relay specification expert who helps with global IDs, connections, "
                    + "mutations, and Strawberry Shake",
                ReadResource("relay_expert.md"))
        };
    }

    private static string ReadResource(string fileName)
    {
        var assembly = typeof(TeamMemberProvider).Assembly;
        var resourceName = $"TeamMembers.{fileName}";

        using var stream =
            assembly.GetManifestResourceStream(resourceName)
            ?? throw TeamMembersThrowHelper.EmbeddedResourceNotFound(resourceName);

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
