using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Suites.ProvidesOnInterface.SubgraphA;

/// <summary>
/// Builds the <c>subgraph-a</c> Apollo Federation subgraph for the
/// <c>provides-on-interface</c> audit suite. Owns the <c>Book</c>
/// entity and the <c>Query.media</c> / <c>Query.book</c> fields.
/// </summary>
public static class SubgraphASubgraph
{
    /// <summary>
    /// The source-schema name by which the gateway addresses this subgraph.
    /// </summary>
    public const string Name = "subgraph-a";

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
            .AddType<BookType>()
            .AddType<DogType>()
            .AddType<CatType>()
            .AddType<MediaInterfaceType>()
            .AddType<AnimalInterfaceType>();

        var app = builder.Build();
        app.MapGraphQL();

        await app.StartAsync();

        return new SubgraphHost(Name, app);
    }
}
