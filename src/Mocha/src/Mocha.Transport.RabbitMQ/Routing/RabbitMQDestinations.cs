using static System.StringSplitOptions;

namespace Mocha.Transport.RabbitMQ;

internal static class RabbitMQDestinations
{
    public static (RabbitMQDestinationKind Kind, string Name, string EndpointName) Resolve(
        string schema,
        IBusNamingConventions naming,
        OutboundRoute route)
    {
        if (route.HasExplicitDestination && route.Destination is { } destination
            && TryResolveExplicit(schema, destination, out var explicitDestination))
        {
            return explicitDestination;
        }

        return ResolveConvention(naming, route.Kind, route.MessageType);
    }

    public static (RabbitMQDestinationKind Kind, string Name, string EndpointName) ResolveConvention(
        IBusNamingConventions naming,
        OutboundRouteKind kind,
        MessageType messageType)
        => kind switch
        {
            OutboundRouteKind.Send => Exchange(naming.GetSendEndpointName(messageType.RuntimeType)),
            OutboundRouteKind.Publish => Exchange(naming.GetPublishEndpointName(messageType.RuntimeType)),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };

    public static bool TryResolveSourceExchange(
        string schema,
        Uri source,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? exchangeName)
    {
        if (TryResolveExplicit(schema, source, out var destination)
            && destination.Kind == RabbitMQDestinationKind.Exchange)
        {
            exchangeName = destination.Name;
            return true;
        }

        exchangeName = null;
        return false;
    }

    private static bool TryResolveExplicit(
        string schema,
        Uri destination,
        out (RabbitMQDestinationKind Kind, string Name, string EndpointName) resolution)
    {
        var path = destination.AbsolutePath.AsSpan();
        Span<Range> ranges = stackalloc Range[2];
        var segmentCount = path.Split(ranges, '/', RemoveEmptyEntries | TrimEntries);

        if ((destination.Scheme == schema || destination.Scheme is "exchange" or "queue")
            && segmentCount == 2)
        {
            var kind = path[ranges[0]];
            var name = new string(path[ranges[1]]);

            if (kind is "e")
            {
                resolution = Exchange(name);
                return true;
            }

            if (kind is "q")
            {
                resolution = Queue(name);
                return true;
            }
        }

        if (destination.Scheme is "exchange" && segmentCount == 1)
        {
            resolution = Exchange(new string(path[ranges[0]]));
            return true;
        }

        if (destination.Scheme is "queue" && segmentCount == 1)
        {
            resolution = Queue(new string(path[ranges[0]]));
            return true;
        }

        resolution = default;
        return false;
    }

    private static (RabbitMQDestinationKind Kind, string Name, string EndpointName) Exchange(string name)
        => (RabbitMQDestinationKind.Exchange, name, "e/" + name);

    private static (RabbitMQDestinationKind Kind, string Name, string EndpointName) Queue(string name)
        => (RabbitMQDestinationKind.Queue, name, "q/" + name);
}
