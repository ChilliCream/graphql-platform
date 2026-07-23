using Mocha.Middlewares;
using static System.StringSplitOptions;

namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Resolves the queue or topic entity path targeted by a dispatch endpoint.
/// </summary>
internal static class AzureServiceBusEntityPathResolver
{
    public static string Resolve(
        AzureServiceBusDispatchEndpoint endpoint,
        MessageEnvelope envelope)
    {
        if (endpoint.Kind == DispatchEndpointKind.Reply)
        {
            if (!Uri.TryCreate(envelope.DestinationAddress, UriKind.Absolute, out var destinationAddress))
            {
                throw ThrowHelper.DispatchEndpointDestinationAddressInvalidUri();
            }

            var path = destinationAddress.AbsolutePath.AsSpan();
            Span<Range> ranges = stackalloc Range[2];
            var segmentCount = path.Split(ranges, '/', RemoveEmptyEntries | TrimEntries);

            if (segmentCount != 2)
            {
                throw ThrowHelper.DispatchEndpointCannotDetermineDestinationName(destinationAddress.ToString());
            }

            var kind = path[ranges[0]];
            if (kind is not ("t" or "q"))
            {
                throw ThrowHelper.DispatchEndpointCannotDetermineDestinationName(destinationAddress.ToString());
            }

            return new string(path[ranges[1]]);
        }

        if (endpoint.Topic is not null)
        {
            return endpoint.Topic.Name;
        }

        if (endpoint.Queue is not null)
        {
            return endpoint.Queue.Name;
        }

        throw ThrowHelper.DispatchEndpointDestinationNotConfigured();
    }
}
