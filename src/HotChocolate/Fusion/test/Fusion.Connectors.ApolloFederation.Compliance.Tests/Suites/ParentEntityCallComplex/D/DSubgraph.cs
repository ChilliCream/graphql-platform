using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Suites.ParentEntityCallComplex.D;

/// <summary>
/// Builds the <c>d</c> Apollo Federation subgraph for the
/// <c>parent-entity-call-complex</c> audit suite. Exposes the
/// <c>Query.productFromD(id: ID!)</c> root field and owns the
/// <c>Product @key("id")</c> entity (the <c>name</c> field).
/// </summary>
public static class DSubgraph
{
    /// <summary>
    /// The source-schema name by which the gateway addresses this subgraph.
    /// </summary>
    public const string Name = "d";

    /// <summary>
    /// Starts the subgraph's <see cref="TestServer"/> and returns a
    /// <see cref="SubgraphHost"/> that routes <c>/graphql</c> requests to
    /// the in-process pipeline.
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
            .AddType<ProductType>();

        var app = builder.Build();
        app.MapGraphQL();

        await app.StartAsync();

        return new SubgraphHost(Name, app);
    }
}
