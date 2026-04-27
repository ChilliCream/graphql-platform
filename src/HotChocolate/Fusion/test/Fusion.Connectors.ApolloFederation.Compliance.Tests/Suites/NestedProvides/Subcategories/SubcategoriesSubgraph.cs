using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Suites.NestedProvides.Subcategories;

/// <summary>
/// Builds the <c>subcategories</c> Apollo Federation subgraph for the
/// <c>nested-provides</c> audit suite. Contributes shareable
/// <c>categories</c> and <c>subCategories</c> fields to the
/// <c>Product</c> and <c>Category</c> entities.
/// </summary>
public static class SubcategoriesSubgraph
{
    /// <summary>
    /// The source-schema name by which the gateway addresses this subgraph.
    /// </summary>
    public const string Name = "subcategories";

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
            .AddType<ProductType>()
            .AddType<CategoryEntityType>();

        var app = builder.Build();
        app.MapGraphQL();

        await app.StartAsync();

        return new SubgraphHost(Name, app);
    }
}
