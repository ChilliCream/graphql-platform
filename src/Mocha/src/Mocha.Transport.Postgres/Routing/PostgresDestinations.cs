using System.Diagnostics.CodeAnalysis;
using static System.StringSplitOptions;

namespace Mocha.Transport.Postgres;

internal static class PostgresDestinations
{
    public static PostgresDestination Resolve(
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

    public static PostgresDestination ResolveConvention(
        IBusNamingConventions naming,
        OutboundRouteKind kind,
        MessageType messageType)
        => kind switch
        {
            OutboundRouteKind.Send => Topic(naming.GetSendEndpointName(messageType.RuntimeType)),
            OutboundRouteKind.Publish => Topic(naming.GetPublishEndpointName(messageType.RuntimeType)),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };

    public static bool TryResolveSourceTopic(
        string schema,
        Uri source,
        [NotNullWhen(true)] out string? topicName)
    {
        if (TryResolveExplicit(schema, source, out var destination)
            && destination.Kind == PostgresDestinationKind.Topic)
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
        [NotNullWhen(true)] out PostgresDestination? resolution)
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

        resolution = null;
        return false;
    }

    private static PostgresDestination Topic(string name)
        => new(PostgresDestinationKind.Topic, name, "t/" + name);

    private static PostgresDestination Queue(string name)
        => new(PostgresDestinationKind.Queue, name, "q/" + name);
}

internal sealed record PostgresDestination(
    PostgresDestinationKind Kind,
    string Name,
    string EndpointName);

internal enum PostgresDestinationKind
{
    Topic,
    Queue
}
