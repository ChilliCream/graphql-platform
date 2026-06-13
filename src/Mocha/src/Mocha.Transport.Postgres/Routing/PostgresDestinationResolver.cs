using static System.StringSplitOptions;

namespace Mocha.Transport.Postgres;

/// <summary>
/// Resolves the static destination entity for a PostgreSQL message route. The resolver is the
/// single authority consulted by both the producer path and the consumer conventions, so the two
/// sides converge on one entity and one canonical endpoint name and cannot drift apart.
/// </summary>
/// <remarks>
/// The resolver answers only the static half of routing: the topic or queue an explicit destination
/// names, or the convention topic a type otherwise routes to. PostgreSQL has no routing keys, so
/// every consume binding is key-less and always derivable. Reply routes are address-routed and must
/// never be passed to the resolver.
/// </remarks>
internal sealed class PostgresDestinationResolver
{
    private readonly string _schema;

    /// <summary>
    /// Creates a resolver bound to the transport schema used to form canonical destination URIs.
    /// </summary>
    /// <param name="schema">The URI schema of the owning transport (for example, <c>postgres</c>).</param>
    public PostgresDestinationResolver(string schema)
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
    public PostgresDestinationResolution ResolveDestination(IBusNamingConventions naming, OutboundRoute route)
    {
        if (route.HasExplicitDestination && route.Destination is { } destination
            && TryResolveExplicit(destination, out var explicitResolution))
        {
            return explicitResolution;
        }

        var topicName = route.Kind == OutboundRouteKind.Send
            ? naming.GetSendEndpointName(route.MessageType.RuntimeType)
            : naming.GetPublishEndpointName(route.MessageType.RuntimeType);

        return Topic(topicName);
    }

    /// <summary>
    /// Resolves the convention topic a consumed message type publishes to, used as the chain entry
    /// the receive convention subscribes from into the endpoint queue.
    /// </summary>
    /// <param name="naming">The naming conventions used to derive convention topic names.</param>
    /// <param name="messageType">The consumed message type.</param>
    /// <returns>The resolved publish topic entity for the type.</returns>
    public PostgresDestinationResolution ResolvePublishDestination(IBusNamingConventions naming, MessageType messageType)
        => Topic(naming.GetPublishEndpointName(messageType.RuntimeType));

    /// <summary>
    /// Attempts to extract a topic name from a BindFrom source URI so the materialization path
    /// can create the topic entity in the topology.
    /// </summary>
    /// <param name="source">The source URI from a <see cref="BindFromIntent"/>.</param>
    /// <param name="topicName">
    /// When this method returns <c>true</c>, contains the resolved topic name.
    /// </param>
    /// <returns><c>true</c> if the URI identifies a known topic scheme; <c>false</c> otherwise.</returns>
    public bool TryResolveSourceTopic(Uri source, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? topicName)
    {
        if (TryResolveExplicit(source, out var resolution) && resolution.Kind == PostgresDestinationKind.Topic)
        {
            topicName = resolution.Name;
            return true;
        }

        topicName = null;
        return false;
    }

    private bool TryResolveExplicit(Uri destination, out PostgresDestinationResolution resolution)
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

    private static PostgresDestinationResolution Topic(string name)
        => new(PostgresDestinationKind.Topic, name, "t/" + name);

    private static PostgresDestinationResolution Queue(string name)
        => new(PostgresDestinationKind.Queue, name, "q/" + name);
}

/// <summary>
/// The resolved static destination for a PostgreSQL message route.
/// </summary>
/// <param name="Kind">Whether the destination is a topic or a queue.</param>
/// <param name="Name">The resolved entity name.</param>
/// <param name="EndpointName">
/// The canonical dispatch endpoint name (<c>t/{name}</c> or <c>q/{name}</c>) shared by the producer
/// and consumer paths so both converge on a single endpoint.
/// </param>
internal readonly record struct PostgresDestinationResolution(
    PostgresDestinationKind Kind,
    string Name,
    string EndpointName);

/// <summary>
/// Classifies the kind of entity a PostgreSQL destination resolves to.
/// </summary>
internal enum PostgresDestinationKind
{
    /// <summary>
    /// The destination is a topic (the standard publish target for PostgreSQL).
    /// </summary>
    Topic,

    /// <summary>
    /// The destination is a queue (used for direct send routing).
    /// </summary>
    Queue
}
