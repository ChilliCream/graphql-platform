using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Suites.Typename.A;

/// <summary>
/// Builds the <c>a</c> Apollo Federation subgraph for the
/// <c>typename</c> audit suite. Exposes the <c>Query.union</c> and
/// <c>Query.interface</c> root fields, the <c>Product</c> union, the
/// <c>Node</c> interface, and the <c>User @key("id")</c> interface with
/// the <c>Admin</c> concrete entity.
/// </summary>
public static class ASubgraph
{
    /// <summary>
    /// The source-schema name by which the gateway addresses this subgraph.
    /// </summary>
    public const string Name = "a";

    /// <summary>
    /// Starts the subgraph's <see cref="TestServer"/> and returns a
    /// <see cref="SubgraphHost"/>.
    /// </summary>
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
            .AddType<OvenType>()
            .AddType<ToasterType>()
            .AddType<ProductUnionType>()
            .AddType<UserInterfaceType>()
            .AddType<AdminType>();

        var app = builder.Build();
        app.MapGraphQL();

        await app.StartAsync();

        return new SubgraphHost(Name, app);
    }
}
