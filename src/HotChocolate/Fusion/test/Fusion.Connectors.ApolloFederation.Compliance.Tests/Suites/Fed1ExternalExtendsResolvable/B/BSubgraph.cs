using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Suites.Fed1ExternalExtendsResolvable.B;

/// <summary>
/// Builds the <c>b</c> Apollo Federation subgraph for the
/// <c>fed1-external-extends-resolvable</c> audit suite: exposes
/// <c>Query.productInB</c> and extends the federated <c>Product</c> entity with
/// the local <c>price</c> field under the <c>@key(fields: "id name")</c> and
/// <c>@key(fields: "upc")</c> keys.
/// </summary>
public static class BSubgraph
{
    /// <summary>
    /// The source-schema name by which the gateway addresses this subgraph.
    /// </summary>
    public const string Name = "b";

    /// <summary>
    /// Starts the subgraph's <see cref="TestServer"/> and returns a
    /// <see cref="SubgraphHost"/> that routes <c>/graphql</c> requests to the
    /// in-process pipeline.
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
            .AddType<ProductType>();

        var app = builder.Build();
        app.MapGraphQL();

        await app.StartAsync();

        return new SubgraphHost(Name, app);
    }
}
