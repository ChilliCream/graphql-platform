using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Suites.OverrideWithRequires.B;

/// <summary>
/// Builds subgraph <c>b</c> for the <c>override-with-requires</c> suite.
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
            .AddType<UserType>();

        var app = builder.Build();
        app.MapGraphQL();

        await app.StartAsync();

        return new SubgraphHost(Name, app);
    }
}
