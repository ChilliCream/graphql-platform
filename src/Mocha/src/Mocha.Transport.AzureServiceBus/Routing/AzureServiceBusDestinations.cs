using System.Diagnostics.CodeAnalysis;
using static System.StringSplitOptions;

namespace Mocha.Transport.AzureServiceBus;

internal static class AzureServiceBusDestinations
{
    public static AzureServiceBusDestination Resolve(
        string schema,
        IBusNamingConventions naming,
        OutboundRoute route)
    {
        if (route.HasExplicitDestination
            && route.Destination is { } destination
            && TryResolveExplicit(schema, destination, out var explicitDestination))
        {
            return explicitDestination;
        }

        return ResolveConvention(naming, route.Kind, route.MessageType);
    }

    public static AzureServiceBusDestination ResolveConvention(
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
            && destination.Kind == AzureServiceBusDestinationKind.Topic)
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
        [NotNullWhen(true)] out AzureServiceBusDestination? resolution)
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

        if (destination.Scheme is "topic" && TryGetNeutralName(destination, path, ranges, segmentCount, out var topic))
        {
            resolution = Topic(topic);
            return true;
        }

        if (destination.Scheme is "queue" && TryGetNeutralName(destination, path, ranges, segmentCount, out var queue))
        {
            resolution = Queue(queue);
            return true;
        }

        resolution = null;
        return false;
    }

    private static bool TryGetNeutralName(
        Uri destination,
        ReadOnlySpan<char> path,
        Span<Range> ranges,
        int segmentCount,
        [NotNullWhen(true)] out string? name)
    {
        if (!string.IsNullOrEmpty(destination.Host) && destination.AbsolutePath is "" or "/")
        {
            name = destination.Host;
            return true;
        }

        if (segmentCount == 1)
        {
            name = new string(path[ranges[0]]);
            return true;
        }

        name = null;
        return false;
    }

    private static AzureServiceBusDestination Topic(string name)
        => new(AzureServiceBusDestinationKind.Topic, name, "t/" + name);

    private static AzureServiceBusDestination Queue(string name)
        => new(AzureServiceBusDestinationKind.Queue, name, "q/" + name);
}

internal sealed record AzureServiceBusDestination(
    AzureServiceBusDestinationKind Kind,
    string Name,
    string EndpointName);

internal enum AzureServiceBusDestinationKind
{
    Topic,
    Queue
}
