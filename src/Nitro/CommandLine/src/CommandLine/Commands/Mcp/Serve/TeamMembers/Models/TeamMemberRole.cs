using System.ComponentModel;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.TeamMembers.Models;

internal enum TeamMemberRole
{
    [Description("Hot Chocolate expert — resolvers, DataLoaders, types, middleware")]
    backend_engineer,

    [Description("React/Relay expert — queries, fragments, components, codegen")]
    frontend_engineer,

    [Description("Schema design authority — naming, nullability, Relay compliance")]
    graphql_expert,

    [Description("Reviews schema changes — uses statistics tool, checks usage")]
    schema_reviewer,

    [Description("Optimization — N+1 detection, DataLoader patterns, complexity")]
    performance_engineer,

    [Description("Deployment — stages, CI/CD, monitoring, fusion")]
    devops_engineer,

    [Description("Security — authorization, input validation, rate limiting")]
    security_engineer,

    [Description("Test patterns — CookieCrumble snapshots, integration tests")]
    testing_engineer,

    [Description("Relay-specific — global IDs, connections, mutations, fragments")]
    relay_expert
}
