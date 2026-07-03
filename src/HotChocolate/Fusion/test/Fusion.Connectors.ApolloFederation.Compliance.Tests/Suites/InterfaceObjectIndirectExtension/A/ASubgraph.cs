using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Suites.InterfaceObjectIndirectExtension.A;

/// <summary>
/// Builds the <c>a</c> Apollo Federation subgraph for the
/// <c>interface-object-indirect-extension</c> audit suite. Owns the
/// <c>Media</c> interface and its <c>Video</c> / <c>Article</c> implementations.
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
            .AddType<MediaInterfaceType>()
            .AddType<VideoType>()
            .AddType<ArticleType>();

        var app = builder.Build();
        app.MapSubgraph();

        await app.StartAsync();

        return new SubgraphHost(Name, app);
    }
}
