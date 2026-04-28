using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Suites.RequiresInterface.B;

/// <summary>
/// Builds subgraph <c>b</c> for the <c>requires-interface</c> suite.
/// Owns <c>Query.b</c>, <c>User.address</c> (shareable),
/// <c>HomeAddress</c>, <c>WorkAddress</c>, and <c>SecondAddress</c>.
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
            .AddType<UserType>()
            .AddType<HomeAddressType>()
            .AddType<WorkAddressType>()
            .AddType<SecondAddressType>()
            .AddType<AddressInterfaceType>();

        var app = builder.Build();
        app.MapGraphQL();

        await app.StartAsync();

        return new SubgraphHost(Name, app);
    }
}
