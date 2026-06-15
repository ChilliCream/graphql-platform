using static System.StringSplitOptions;

namespace Mocha.Transport.InMemory;

/// <summary>
/// Resolves the static destination entity for an in-memory message route. The resolver is the
/// single authority consulted by both the producer path and the consumer conventions, so the two
/// sides converge on one entity and one canonical endpoint name and cannot drift apart.
/// </summary>
internal sealed class InMemoryDestinationResolver
{
    private readonly string _schema;

    /// <summary>
    /// Creates a resolver bound to the transport schema used to form canonical destination URIs.
    /// </summary>
    /// <param name="schema">The URI schema of the owning transport (for example, <c>inmemory</c>).</param>
    public InMemoryDestinationResolver(string schema)
    {
        _schema = schema;
    }

    /// <summary>
    /// Resolves the destination entity for an outbound route, honoring an explicitly configured
    /// destination and otherwise falling back to the convention topic for the message type.
    /// </summary>
    /// <param name="naming">The naming conventions used to derive convention topic names.</param>
    /// <param name="route">The outbound route to resolve. Its destination must not be a reply.</param>
    /// <returns>The resolved destination entity kind, name, and canonical endpoint name.</returns>
    public InMemoryDestinationResolution ResolveDestination(IBusNamingConventions naming, OutboundRoute route)
    {
        if (route.HasExplicitDestination && route.Destination is { } destination
            && TryResolveExplicit(destination, out var explicitResolution))
        {
            return explicitResolution;
        }

        if (route.Kind == OutboundRouteKind.Send)
        {
            var queueName = naming.GetSendEndpointName(route.MessageType.RuntimeType);
            return Queue(queueName);
        }

        var topicName = naming.GetPublishEndpointName(route.MessageType.RuntimeType);
        return Topic(topicName);
    }

    /// <summary>
    /// Resolves the convention topic a consumed message type publishes to, used as the chain entry
    /// the receive convention binds into the endpoint queue.
    /// </summary>
    /// <param name="naming">The naming conventions used to derive convention topic names.</param>
    /// <param name="messageType">The consumed message type.</param>
    /// <returns>The resolved publish topic entity for the type.</returns>
    public InMemoryDestinationResolution ResolvePublishDestination(IBusNamingConventions naming, MessageType messageType)
        => Topic(naming.GetPublishEndpointName(messageType.RuntimeType));

    /// <summary>
    /// Attempts to extract a topic name from a BindFrom source URI so the materialization path
    /// can create the topic entity in the topology.
    /// </summary>
    /// <param name="source">The source URI identifying the topic to bind from.</param>
    /// <param name="topicName">
    /// When this method returns <c>true</c>, contains the resolved topic name.
    /// </param>
    /// <returns><c>true</c> if the URI identifies a known topic scheme; <c>false</c> otherwise.</returns>
    public bool TryResolveSourceTopic(Uri source, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? topicName)
    {
        if (TryResolveExplicit(source, out var resolution) && resolution.Kind == InMemoryDestinationKind.Topic)
        {
            topicName = resolution.Name;
            return true;
        }

        topicName = null;
        return false;
    }

    private bool TryResolveExplicit(Uri destination, out InMemoryDestinationResolution resolution)
    {
        var path = destination.AbsolutePath.AsSpan();
        Span<Range> ranges = stackalloc Range[2];
        var segmentCount = path.Split(ranges, '/', RemoveEmptyEntries | TrimEntries);

        if ((destination.Scheme == _schema || destination.Scheme is "topic" or "queue") && segmentCount == 2)
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

    private static InMemoryDestinationResolution Topic(string name)
        => new(InMemoryDestinationKind.Topic, name, "t/" + name);

    private static InMemoryDestinationResolution Queue(string name)
        => new(InMemoryDestinationKind.Queue, name, "q/" + name);
}

/// <summary>
/// The resolved static destination for an in-memory message route.
/// </summary>
/// <param name="Kind">Whether the destination is a topic or a queue.</param>
/// <param name="Name">The resolved entity name.</param>
/// <param name="EndpointName">
/// The canonical dispatch endpoint name (<c>t/{name}</c> or <c>q/{name}</c>) shared by the producer
/// and consumer paths so both converge on a single endpoint.
/// </param>
internal readonly record struct InMemoryDestinationResolution(
    InMemoryDestinationKind Kind,
    string Name,
    string EndpointName);
