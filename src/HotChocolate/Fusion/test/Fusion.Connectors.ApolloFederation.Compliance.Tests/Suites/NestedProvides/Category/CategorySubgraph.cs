using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Suites.NestedProvides.Category;

/// <summary>
/// Builds the <c>category</c> Apollo Federation subgraph for the
/// <c>nested-provides</c> audit suite. Owns the <c>Category.name</c>
/// field and provides product categories inline through
/// <c>Query.products</c>.
/// </summary>
public static class CategorySubgraph
{
    /// <summary>
    /// The source-schema name by which the gateway addresses this subgraph.
    /// </summary>
    public const string Name = "category";

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
