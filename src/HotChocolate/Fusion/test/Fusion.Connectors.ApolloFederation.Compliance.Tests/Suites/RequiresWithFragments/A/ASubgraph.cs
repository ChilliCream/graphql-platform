using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Suites.RequiresWithFragments.A;

/// <summary>
/// Builds subgraph <c>a</c> for the <c>requires-with-fragments</c> suite.
/// Owns <c>Query.a</c>, <c>Entity.data</c>, and the Baz/Qux types.
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
