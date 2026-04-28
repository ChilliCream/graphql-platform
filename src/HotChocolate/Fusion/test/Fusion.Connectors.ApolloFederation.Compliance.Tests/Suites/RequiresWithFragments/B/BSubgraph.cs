using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Suites.RequiresWithFragments.B;

/// <summary>
/// Builds subgraph <c>b</c> for the <c>requires-with-fragments</c> suite.
/// Owns <c>Query.b</c>, <c>Query.bb</c>, <c>Entity.requirer</c>,
/// and <c>Entity.requirer2</c>.
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
            .AddType<EntityType>()
            .AddType<BazType>()
            .AddType<QuxType>()
            .AddType<FooInterfaceType>()
            .AddType<BarInterfaceType>();

        var app = builder.Build();
        app.MapGraphQL();

        await app.StartAsync();

        return new SubgraphHost(Name, app);
    }
}
