using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Suites.ParentEntityCallComplex.A;

/// <summary>
/// Builds the <c>a</c> Apollo Federation subgraph for the
/// <c>parent-entity-call-complex</c> audit suite. Owns the shareable
/// <c>Product.category</c> field and produces the <c>Category.details</c>
/// payload from the <c>__resolveReference</c> entity call.
/// </summary>
public static class ASubgraph
{
    /// <summary>
    /// The source-schema name by which the gateway addresses this subgraph.
    /// </summary>
    public const string Name = "a";

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
            .AddType<ProductType>()
            .AddType<CategoryType>();

        var app = builder.Build();
        app.MapGraphQL();

        await app.StartAsync();

        return new SubgraphHost(Name, app);
    }
}
