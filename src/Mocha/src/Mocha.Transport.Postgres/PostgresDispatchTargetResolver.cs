using Mocha.Middlewares;
using static System.StringSplitOptions;

namespace Mocha.Transport.Postgres;

internal static class PostgresDispatchTargetResolver
{
    public static PostgresDispatchTarget Resolve(PostgresDispatchEndpoint endpoint, MessageEnvelope envelope)
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

            if (segmentCount == 2)
            {
                var kind = path[ranges[0]];
                var name = path[ranges[1]];

                if (kind is "t")
                {
                    return new PostgresDispatchTarget(IsTopic: true, new string(name));
                }

                if (kind is "q")
                {
                    return new PostgresDispatchTarget(IsTopic: false, new string(name));
                }
            }

            throw new InvalidOperationException(
                $"Cannot determine topic or queue name from destination address {destinationAddress}");
        }

        if (endpoint.Topic is not null)
        {
            return new PostgresDispatchTarget(IsTopic: true, endpoint.Topic.Name);
        }

        if (endpoint.Queue is not null)
        {
            return new PostgresDispatchTarget(IsTopic: false, endpoint.Queue.Name);
        }

        throw new InvalidOperationException("Resource not found");
    }
}

internal readonly record struct PostgresDispatchTarget(bool IsTopic, string Name);
