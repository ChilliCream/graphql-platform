using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Suites.ProvidesOnlyRequestedFields.SubgraphA;

public static class SubgraphASubgraph
{
    public const string Name = "a";

    public static async Task<SubgraphHost> BuildAsync(bool punishForPoorPlans)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services
            .AddRouting()
            .AddGraphQLServer()
            .AddApolloFederation()
            .AddQueryType<QueryType>()
            .AddType(new EntityType(punishForPoorPlans));

        var app = builder.Build();
        app.MapSubgraph();

        await app.StartAsync();

        return new SubgraphHost(Name, app);
    }
}
