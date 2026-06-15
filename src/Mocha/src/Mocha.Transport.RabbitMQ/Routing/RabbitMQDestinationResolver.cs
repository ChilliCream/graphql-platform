using static System.StringSplitOptions;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Resolves the static destination entity for a RabbitMQ message route and classifies whether a
/// consume binding for the type can be derived. The resolver is the single authority consulted by
/// both the producer path and the consumer conventions, so the two sides converge on one entity and
/// one canonical endpoint name and cannot drift apart.
/// </summary>
internal sealed class RabbitMQDestinationResolver
{
    private readonly string _schema;

    /// <summary>
    /// Creates a resolver bound to the transport schema used to form canonical destination URIs.
    /// </summary>
    /// <param name="schema">The URI schema of the owning transport (for example, <c>rabbitmq</c>).</param>
    public RabbitMQDestinationResolver(string schema)
    {
        _schema = schema;
    }

    /// <summary>
    /// Resolves the destination entity for an outbound route, honoring an explicitly configured
    /// destination and otherwise falling back to the convention exchange for the message type.
    /// </summary>
    /// <param name="naming">The naming conventions used to derive convention exchange names.</param>
    /// <param name="route">The outbound route to resolve. Its destination must not be a reply.</param>
    /// <returns>The resolved destination entity kind, name, and canonical endpoint name.</returns>
    public RabbitMQDestinationResolution ResolveDestination(IBusNamingConventions naming, OutboundRoute route)
    {
        if (route.HasExplicitDestination && route.Destination is { } destination
            && TryResolveExplicit(destination, out var explicitResolution))
        {
            return explicitResolution;
        }

        var exchangeName = route.Kind == OutboundRouteKind.Send
            ? naming.GetSendEndpointName(route.MessageType.RuntimeType)
            : naming.GetPublishEndpointName(route.MessageType.RuntimeType);

        return Exchange(exchangeName);
    }

    /// <summary>
    /// Resolves the convention exchange a consumed message type publishes to, used as the chain entry
    /// the receive convention binds into the endpoint queue.
    /// </summary>
    /// <param name="naming">The naming conventions used to derive convention exchange names.</param>
    /// <param name="messageType">The consumed message type.</param>
    /// <returns>The resolved publish exchange entity for the type.</returns>
    public RabbitMQDestinationResolution ResolvePublishDestination(IBusNamingConventions naming, MessageType messageType)
        => Exchange(naming.GetPublishEndpointName(messageType.RuntimeType));

    /// <summary>
    /// Classifies whether a consume binding for the message type can be derived from a statically
    /// known routing key.
    /// </summary>
    /// <param name="messageType">The consumed message type.</param>
    /// <returns>The bind-key classification for the type.</returns>
    public RabbitMQBindKeyResolution ResolveBindKey(MessageType messageType)
    {
        if (messageType.Features.TryGet<RabbitMQRoutingKeyExtractor>(out _))
        {
            return RabbitMQBindKeyResolution.Underivable;
        }

        return RabbitMQBindKeyResolution.None;
    }

    /// <summary>
    /// Attempts to extract an exchange name from a BindFrom source URI so the materialization path
    /// can create the exchange entity in the topology.
    /// </summary>
    /// <param name="source">The source URI identifying the exchange to bind from.</param>
    /// <param name="exchangeName">
    /// When this method returns <c>true</c>, contains the resolved exchange name.
    /// </param>
    /// <returns><c>true</c> if the URI identifies a known exchange scheme; <c>false</c> otherwise.</returns>
    public bool TryResolveSourceExchange(Uri source, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? exchangeName)
    {
        if (TryResolveExplicit(source, out var resolution) && resolution.Kind == RabbitMQDestinationKind.Exchange)
        {
            exchangeName = resolution.Name;
            return true;
        }

        exchangeName = null;
        return false;
    }

    private bool TryResolveExplicit(Uri destination, out RabbitMQDestinationResolution resolution)
    {
        var path = destination.AbsolutePath.AsSpan();
        Span<Range> ranges = stackalloc Range[2];
        var segmentCount = path.Split(ranges, '/', RemoveEmptyEntries | TrimEntries);

        if ((destination.Scheme == _schema || destination.Scheme is "exchange" or "queue") && segmentCount == 2)
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

    private static RabbitMQDestinationResolution Exchange(string name)
        => new(RabbitMQDestinationKind.Exchange, name, "e/" + name);

    private static RabbitMQDestinationResolution Queue(string name)
        => new(RabbitMQDestinationKind.Queue, name, "q/" + name);
}

/// <summary>
/// The resolved static destination for a RabbitMQ message route.
/// </summary>
/// <param name="Kind">Whether the destination is an exchange or a queue.</param>
/// <param name="Name">The resolved entity name.</param>
/// <param name="EndpointName">
/// The canonical dispatch endpoint name (<c>e/{name}</c> or <c>q/{name}</c>) shared by the producer
/// and consumer paths so both converge on a single endpoint.
/// </param>
internal readonly record struct RabbitMQDestinationResolution(
    RabbitMQDestinationKind Kind,
    string Name,
    string EndpointName);

/// <summary>
/// Classifies whether a consume binding for a message type can be derived at configuration time.
/// </summary>
internal enum RabbitMQBindKeyKind
{
    /// <summary>
    /// The type has no routing key, so the binding is created key-less.
    /// </summary>
    None,

    /// <summary>
    /// The type has a statically known constant routing key that the binding can use directly.
    /// </summary>
    Static,

    /// <summary>
    /// The type's routing key is computed per message, so a consume binding cannot be derived without
    /// guessing a key that may silently fail to match.
    /// </summary>
    Underivable
}

/// <summary>
/// The bind-key classification for a consumed message type.
/// </summary>
/// <param name="Kind">The bind-key classification.</param>
/// <param name="Key">
/// The statically known routing key when <see cref="Kind"/> is <see cref="RabbitMQBindKeyKind.Static"/>;
/// otherwise <c>null</c>.
/// </param>
internal readonly record struct RabbitMQBindKeyResolution(RabbitMQBindKeyKind Kind, string? Key)
{
    /// <summary>
    /// A resolution for a type that has no routing key.
    /// </summary>
    public static RabbitMQBindKeyResolution None { get; } = new(RabbitMQBindKeyKind.None, null);

    /// <summary>
    /// A resolution for a type whose consume binding cannot be derived.
    /// </summary>
    public static RabbitMQBindKeyResolution Underivable { get; } = new(RabbitMQBindKeyKind.Underivable, null);

    /// <summary>
    /// Creates a resolution for a type with a statically known constant routing key.
    /// </summary>
    /// <param name="key">The constant routing key.</param>
    /// <returns>A static bind-key resolution carrying the key.</returns>
    public static RabbitMQBindKeyResolution FromStatic(string key) => new(RabbitMQBindKeyKind.Static, key);
}
