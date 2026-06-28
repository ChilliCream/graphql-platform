using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Suites.Fed2ExternalExtends.A;

/// <summary>
/// Builds the <c>a</c> Apollo Federation subgraph for the
/// <c>fed2-external-extends</c> audit suite. Owns the <c>User.rid</c>
/// field, declares the federated <c>User</c> type as an extension via
/// <c>@extends</c>, and exposes <c>randomUser</c> plus
/// <c>providedRandomUser</c>. The subgraph runs in-process under
/// <see cref="TestServer"/>.
/// </summary>
public static class ASubgraph
{
    /// <summary>
    /// The source-schema name by which the gateway addresses this subgraph.
    /// </summary>
    public const string Name = "a";

    /// <summary>
    /// Starts the subgraph's <see cref="TestServer"/> and returns a
    /// <see cref="SubgraphHost"/>.
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
