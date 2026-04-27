using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Suites.NestedProvides.AllProducts;

/// <summary>
/// Builds the <c>all-products</c> Apollo Federation subgraph for the
/// <c>nested-provides</c> audit suite. Owns the <c>Product</c> entity
/// key and acts as a pure entity provider.
/// </summary>
public static class AllProductsSubgraph
{
    /// <summary>
    /// The source-schema name by which the gateway addresses this subgraph.
    /// </summary>
    public const string Name = "all-products";

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
            .AddType<ProductType>();

        var app = builder.Build();
        app.MapGraphQL();

        await app.StartAsync();

        return new SubgraphHost(Name, app);
    }
}
