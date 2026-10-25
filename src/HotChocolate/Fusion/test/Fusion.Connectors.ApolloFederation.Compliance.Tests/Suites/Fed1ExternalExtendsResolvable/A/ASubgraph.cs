using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Suites.Fed1ExternalExtendsResolvable.A;

/// <summary>
/// Builds the <c>a</c> Apollo Federation subgraph for the
/// <c>fed1-external-extends-resolvable</c> audit suite: exposes
/// <c>Query.productInA</c> and owns the <c>Product</c> entity under
/// <c>@key(fields: "id")</c>.
/// </summary>
public static class ASubgraph
{
    /// <summary>
    /// The source-schema name by which the gateway addresses this subgraph.
    /// </summary>
    public const string Name = "a";

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
        app.MapSubgraph();

        await app.StartAsync();

        return new SubgraphHost(Name, app);
    }
}
