using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Suites.SimpleEntityCall.Email;

/// <summary>
/// Builds the <c>email</c> Apollo Federation subgraph:
/// <c>type User @key(fields: "id") { id: ID!, email: String! }</c>
/// plus a root <c>Query.user: User</c>. The subgraph runs in-process under
/// <see cref="TestServer"/> so the gateway dispatches real HTTP requests through
/// the full ASP.NET Core / HotChocolate pipeline.
/// </summary>
public static class EmailSubgraph
{
    /// <summary>
    /// The source-schema name by which the gateway addresses this subgraph.
    /// </summary>
    public const string Name = "email";

    /// <summary>
    /// Starts the subgraph's <see cref="TestServer"/> and returns a
    /// <see cref="SubgraphHost"/> that routes <c>/graphql</c> requests to the
    /// in-process pipeline.
    /// </summary>
    public static async Task<SubgraphHost> BuildAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services
            .AddRouting()
            .AddGraphQLServer()
            .AddApolloFederation()
            .AddQueryType<QueryType>()
            .AddType<UserType>();

        var app = builder.Build();
        app.MapGraphQL();

        await app.StartAsync();

        return new SubgraphHost(Name, app);
    }
}
