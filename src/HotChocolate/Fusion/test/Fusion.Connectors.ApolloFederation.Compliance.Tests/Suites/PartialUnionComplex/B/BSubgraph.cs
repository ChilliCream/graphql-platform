using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Suites.PartialUnionComplex.B;

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
            .AddType<ContainerType>()
            .AddType<WrapperType>()
            .AddType<ActionUnionType>()
            .AddType<CommonType>()
            .AddType<OnlyBType>();

        var app = builder.Build();
        app.MapSubgraph();

        await app.StartAsync();

        return new SubgraphHost(Name, app);
    }
}
