using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Suites.RequiresWithArgument.B;

/// <summary>
/// Builds the <c>b</c> Apollo Federation subgraph for the
/// <c>requires-with-argument</c> audit suite. Owns the <c>Product</c>
/// entity with <c>name</c>, <c>price(currency)</c>, <c>weight</c>,
/// and <c>category</c>.
/// </summary>
public static class BSubgraph
{
    public const string Name = "b";

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
