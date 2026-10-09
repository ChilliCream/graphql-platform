using HotChocolate.Execution.Internal;
using Microsoft.Extensions.Options;

namespace HotChocolate.AspNetCore;

/// <summary>
/// Derives the transport capabilities declared in an exported schema settings file
/// from the <see cref="GraphQLServerOptions"/> of the schema.
/// </summary>
internal sealed class TransportCapabilitiesProvider(
    IOptionsMonitor<GraphQLServerOptions> serverOptions)
    : ITransportCapabilitiesProvider
{
    public TransportCapabilities GetCapabilities(string schemaName)
    {
        var batching = serverOptions.Get(schemaName).Batching;

        return new TransportCapabilities(
            VariableBatching: batching.HasFlag(AllowedBatching.VariableBatching),
            RequestBatching: batching.HasFlag(AllowedBatching.RequestBatching));
    }
}
