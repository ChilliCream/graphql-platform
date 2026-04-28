using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Suites.RequiresRequires.C;

/// <summary>
/// Builds subgraph <c>c</c> for the <c>requires-requires</c> suite.
/// Owns <c>isExpensive</c> (<c>@requires(fields: "price")</c>) and
/// <c>isExpensiveWithDiscount</c> (<c>@requires(fields: "hasDiscount")</c>).
/// </summary>
public static class CSubgraph
{
    public const string Name = "c";

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
