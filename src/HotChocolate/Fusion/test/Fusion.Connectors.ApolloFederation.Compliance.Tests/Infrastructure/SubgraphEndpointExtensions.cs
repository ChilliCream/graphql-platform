using HotChocolate.AspNetCore;
using Microsoft.AspNetCore.Builder;

namespace HotChocolate.Fusion;

/// <summary>
/// Maps the GraphQL endpoint for a compliance-suite Apollo Federation subgraph.
/// </summary>
internal static class SubgraphEndpointExtensions
{
    /// <summary>
    /// Maps the subgraph's GraphQL endpoint with HTTP batching enabled. A plain
    /// HotChocolate server does not allow batching by default, so the harness turns it
    /// on to match the batching support a Fusion source-schema server exposes. With the
    /// subgraphs accepting the standard batch wire formats, the gateway's uniform default
    /// transport exchanges those formats with every subgraph, no settings declaration
    /// required.
    /// </summary>
    /// <param name="app">The subgraph web application.</param>
    public static void MapSubgraph(this WebApplication app, bool enableBatching = false)
    {
        ArgumentNullException.ThrowIfNull(app);

        var endpoint = app.MapGraphQL();

        if (enableBatching)
        {
            endpoint.WithOptions(o => o.Batching = AllowedBatching.All);
        }
    }
}
