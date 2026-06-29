using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Suites.RequiresRequires.D;

/// <summary>
/// Builds subgraph <c>d</c> for the <c>requires-requires</c> suite.
/// Owns <c>canAfford</c> (<c>@requires(fields: "isExpensive")</c>) and
/// <c>canAffordWithDiscount</c> (<c>@requires(fields: "isExpensiveWithDiscount")</c>).
/// </summary>
public static class DSubgraph
{
    public const string Name = "d";

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
