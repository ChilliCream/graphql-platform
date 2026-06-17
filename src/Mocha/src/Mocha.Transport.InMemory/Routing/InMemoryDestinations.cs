using System.Diagnostics.CodeAnalysis;
using static System.StringSplitOptions;

namespace Mocha.Transport.InMemory;

internal static class InMemoryDestinations
{
    public static (InMemoryDestinationKind Kind, string Name, string EndpointName) Resolve(
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

    public static (InMemoryDestinationKind Kind, string Name, string EndpointName) ResolveConvention(
        IBusNamingConventions naming,
        OutboundRouteKind kind,
        MessageType messageType)
        => kind switch
        {
            OutboundRouteKind.Send => Queue(naming.GetSendEndpointName(messageType.RuntimeType)),
            OutboundRouteKind.Publish => Topic(naming.GetPublishEndpointName(messageType.RuntimeType)),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };

    public static bool TryResolveSourceTopic(
        string schema,
        Uri source,
        [NotNullWhen(true)] out string? topicName)
    {
        if (TryResolveExplicit(schema, source, out var destination)
            && destination.Kind == InMemoryDestinationKind.Topic)
        {
            topicName = destination.Name;
            return true;
        }

        topicName = null;
        return false;
    }

    private static bool TryResolveExplicit(
        string schema,
        Uri destination,
        out (InMemoryDestinationKind Kind, string Name, string EndpointName) resolution)
    {
        var path = destination.AbsolutePath.AsSpan();
        Span<Range> ranges = stackalloc Range[2];
        var segmentCount = path.Split(ranges, '/', RemoveEmptyEntries | TrimEntries);

        if ((destination.Scheme == schema || destination.Scheme is "topic" or "queue")
            && segmentCount == 2)
        {
            var kind = path[ranges[0]];
            var name = new string(path[ranges[1]]);

            if (kind is "t")
            {
                resolution = Topic(name);
                return true;
            }

            if (kind is "q")
            {
                resolution = Queue(name);
                return true;
            }
        }

        if (destination.Scheme is "topic" && segmentCount == 1)
        {
            resolution = Topic(new string(path[ranges[0]]));
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

    private static (InMemoryDestinationKind Kind, string Name, string EndpointName) Topic(string name)
        => (InMemoryDestinationKind.Topic, name, "t/" + name);

    private static (InMemoryDestinationKind Kind, string Name, string EndpointName) Queue(string name)
        => (InMemoryDestinationKind.Queue, name, "q/" + name);
}
