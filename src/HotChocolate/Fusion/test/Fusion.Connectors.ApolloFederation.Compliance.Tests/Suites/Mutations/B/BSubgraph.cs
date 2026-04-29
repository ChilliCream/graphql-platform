using HotChocolate.Fusion.Suites.Mutations.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Suites.Mutations.B;

/// <summary>
/// Builds the <c>b</c> Apollo Federation subgraph for the
/// <c>mutations</c> audit suite. Owns <c>Product.isExpensive</c>,
/// <c>Product.isAvailable</c>, and <c>Category.name</c>, plus the
/// <c>Mutation.delete</c> and shareable <c>Mutation.addCategory</c>.
/// </summary>
public static class BSubgraph
{
    /// <summary>
    /// The source-schema name by which the gateway addresses this subgraph.
    /// </summary>
    public const string Name = "b";

    /// <summary>
    /// Starts the subgraph's <see cref="TestServer"/> with the supplied
    /// shared <see cref="MutationsState"/> and returns a
    /// <see cref="SubgraphHost"/>.
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
            .AddType<CategoryType>();

        var app = builder.Build();
        app.MapGraphQL();

        await app.StartAsync();

        return new SubgraphHost(Name, app);
    }
}
