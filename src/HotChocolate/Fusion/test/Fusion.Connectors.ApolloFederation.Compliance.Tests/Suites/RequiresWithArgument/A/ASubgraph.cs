using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Suites.RequiresWithArgument.A;

/// <summary>
/// Builds the <c>a</c> Apollo Federation subgraph for the
/// <c>requires-with-argument</c> audit suite. Extends <c>Product</c>
/// with <c>shippingEstimate</c> and <c>isExpensiveCategory</c>, both
/// of which use <c>@requires</c> with field arguments.
/// </summary>
public static class ASubgraph
{
    public const string Name = "a";

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
