using HotChocolate.Fusion.Suites.Mutations.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Suites.Mutations.A;

/// <summary>
/// Builds the <c>a</c> Apollo Federation subgraph for the
/// <c>mutations</c> audit suite. Owns <c>Product.name</c>, <c>Product.price</c>,
/// the shareable <c>Mutation.addCategory</c>, and the <c>Mutation.multiply</c>
/// counter operation.
/// </summary>
public static class ASubgraph
{
    /// <summary>
    /// The source-schema name by which the gateway addresses this subgraph.
    /// </summary>
    public const string Name = "a";

    /// <summary>
    /// Starts the subgraph's <see cref="TestServer"/> with the supplied
    /// shared <see cref="MutationsState"/> and returns a
    /// <see cref="SubgraphHost"/> that routes <c>/graphql</c> requests to the
    /// in-process pipeline.
    /// </summary>
    public static async Task<SubgraphHost> BuildAsync(MutationsState state)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddSingleton(state);

        builder.Services
            .AddRouting()
            .AddGraphQLServer()
            .AddApolloFederation()
            .AddQueryType<QueryType>()
            .AddMutationType<MutationType>()
            .AddType<ProductType>()
            .AddType<CategoryType>()
            .AddType<AddProductInputType>();

        var app = builder.Build();
        app.MapGraphQL();

        await app.StartAsync();

        return new SubgraphHost(Name, app);
    }
}
