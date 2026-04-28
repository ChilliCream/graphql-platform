using Mocha.Middlewares;
using static System.StringSplitOptions;

namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Resolves the broker entity path (queue or topic name) for a dispatch.
/// </summary>
internal static class AzureServiceBusEntityPathResolver
{
    public static string Resolve(AzureServiceBusDispatchEndpoint endpoint, MessageEnvelope envelope)
    {
        if (endpoint.Kind == DispatchEndpointKind.Reply)
        {
            if (!Uri.TryCreate(envelope.DestinationAddress, UriKind.Absolute, out var destinationAddress))
            {
                throw new InvalidOperationException("Destination address is not a valid URI");
            }

            var path = destinationAddress.AbsolutePath.AsSpan();
            Span<Range> ranges = stackalloc Range[2];
            var segmentCount = path.Split(ranges, '/', RemoveEmptyEntries | TrimEntries);

            if (segmentCount != 2)
            {
                throw new InvalidOperationException(
                    $"Cannot determine topic or queue name from destination address {destinationAddress}");
            }

            var kind = path[ranges[0]];
            var name = path[ranges[1]];

            if (kind is not ("t" or "q"))
            {
                throw new InvalidOperationException(
                    $"Cannot determine topic or queue name from destination address {destinationAddress}");
            }

            return new string(name);
        }

        if (endpoint.Topic is not null)
        {
            return endpoint.Topic.Name;
        }

        if (endpoint.Queue is not null)
        {
            return endpoint.Queue.Name;
        }

        throw new InvalidOperationException("Destination not configured");
    }
}
