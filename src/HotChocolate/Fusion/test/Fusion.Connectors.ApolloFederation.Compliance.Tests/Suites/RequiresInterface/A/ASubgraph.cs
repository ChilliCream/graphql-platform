using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Suites.RequiresInterface.A;

/// <summary>
/// Builds subgraph <c>a</c> for the <c>requires-interface</c> suite.
/// Owns <c>Query.a</c>, <c>User.city</c> (requires address.id),
/// <c>User.country</c> (requires address on WorkAddress),
/// <c>HomeAddress</c>, and <c>WorkAddress</c>.
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
            .AddType<UserType>()
            .AddType<HomeAddressType>()
            .AddType<WorkAddressType>()
            .AddType<AddressInterfaceType>();

        var app = builder.Build();
        app.MapGraphQL();

        await app.StartAsync();

        return new SubgraphHost(Name, app);
    }
}
