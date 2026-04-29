using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Suites.SimpleEntityCall.Nickname;

/// <summary>
/// Builds the <c>nickname</c> Apollo Federation subgraph:
/// <c>extend type User @key(fields: "email") { email: String! @external, nickname: String! }</c>.
/// The subgraph has no user-facing root query fields; Apollo Federation still
/// exposes <c>_service { sdl }</c> and <c>_entities</c> automatically. The
/// subgraph runs in-process under <see cref="TestServer"/> so the gateway
/// dispatches real HTTP requests through the full ASP.NET Core / HotChocolate
/// pipeline.
/// </summary>
public static class NicknameSubgraph
{
    /// <summary>
    /// The source-schema name by which the gateway addresses this subgraph.
    /// </summary>
    public const string Name = "nickname";

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
