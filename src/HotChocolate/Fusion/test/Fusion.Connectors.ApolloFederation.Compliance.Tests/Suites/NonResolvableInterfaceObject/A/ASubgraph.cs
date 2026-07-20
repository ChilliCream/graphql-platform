using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Suites.NonResolvableInterfaceObject.A;

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
            .AddType<NodeType>()
            .AddType<NodeImplType>()
            .AddType<ProductType>();

        var app = builder.Build();
        app.MapSubgraph();

        await app.StartAsync();

        return new SubgraphHost(Name, app);
    }
}
