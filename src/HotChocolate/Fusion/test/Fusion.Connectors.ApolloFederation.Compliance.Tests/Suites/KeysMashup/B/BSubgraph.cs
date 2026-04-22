using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Suites.KeysMashup.B;

/// <summary>
/// Builds the <c>b</c> Apollo Federation subgraph for the
/// <c>keys-mashup</c> audit suite. Owns the <c>B</c> entity, the
/// <c>A.nameInB</c> field via <c>@requires(name)</c>, and a separate
/// projection of <c>A</c> with four keys.
/// </summary>
public static class BSubgraph
{
    /// <summary>
    /// The source-schema name by which the gateway addresses this subgraph.
    /// </summary>
    public const string Name = "b";

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
            .AddType<AType>()
            .AddType<BType>()
            .AddType<CompositeIDType>();

        var app = builder.Build();
        app.MapGraphQL();

        await app.StartAsync();

        return new SubgraphHost(Name, app);
    }
}
