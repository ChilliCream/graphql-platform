using System.Collections.Immutable;

namespace Mocha;

/// <summary>
/// Maintains the routing table that maps message types to inbound and outbound routes, and provides
/// endpoint resolution.
/// </summary>
public interface IMessageRouter
{
    /// <summary>
    /// Gets all registered inbound routes.
    /// </summary>
    IReadOnlyList<InboundRoute> InboundRoutes { get; }

    /// <summary>
    /// Gets all registered outbound routes.
    /// </summary>
    IReadOnlyList<OutboundRoute> OutboundRoutes { get; }

    /// <summary>
    /// Gets the inbound routes that handle the specified message type.
    /// </summary>
    /// <param name="messageType">The message type to look up.</param>
    /// <returns>The set of matching inbound routes.</returns>
    ImmutableHashSet<InboundRoute> GetInboundByMessageType(MessageType messageType);

    /// <summary>
    /// Gets the inbound routes bound to the specified consumer.
    /// </summary>
    /// <param name="consumer">The consumer to look up.</param>
    /// <returns>The set of matching inbound routes.</returns>
    ImmutableHashSet<InboundRoute> GetInboundByConsumer(Consumer consumer);

    /// <summary>
    /// Gets the inbound routes connected to the specified receive endpoint.
    /// </summary>
    /// <param name="endpoint">The receive endpoint to look up.</param>
    /// <returns>The set of matching inbound routes.</returns>
    ImmutableHashSet<InboundRoute> GetInboundByEndpoint(ReceiveEndpoint endpoint);

    /// <summary>
    /// Gets the outbound routes registered for the specified message type.
    /// </summary>
    /// <param name="messageType">The message type to look up.</param>
    /// <returns>The set of matching outbound routes.</returns>
    ImmutableHashSet<OutboundRoute> GetOutboundByMessageType(MessageType messageType);

    /// <summary>
    /// Gets or creates the dispatch endpoint for the specified message type and route kind, connecting it to a transport if needed.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="messageType">The message type to route.</param>
    /// <param name="kind">The outbound route kind (send or publish).</param>
    /// <returns>The dispatch endpoint for the message type.</returns>
    DispatchEndpoint GetEndpoint(
        IMessagingConfigurationContext context,
        MessageType messageType,
        OutboundRouteKind kind);

    /// <summary>
    /// Adds or updates an inbound route in the routing table.
    /// </summary>
    /// <param name="route">The inbound route to add or update.</param>
    void AddOrUpdate(InboundRoute route);

    /// <summary>
    /// Adds or updates an outbound route in the routing table.
    /// </summary>
    /// <param name="route">The outbound route to add or update.</param>
    void AddOrUpdate(OutboundRoute route);
}

/// <summary>
/// Thread-safe implementation of <see cref="IMessageRouter"/> that maintains indexed routing tables
/// for inbound and outbound routes.
/// </summary>
public sealed class MessageRouter : IMessageRouter
{
#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif

    // Inbound storage and indexes
    private readonly Dictionary<InboundRoute, InboundTrackedState> _inboundRoutes = [];
    private readonly Dictionary<MessageType, ImmutableHashSet<InboundRoute>> _inboundByType = [];
    private readonly Dictionary<Consumer, ImmutableHashSet<InboundRoute>> _inboundByConsumer = [];

    private readonly Dictionary<ReceiveEndpoint, ImmutableHashSet<InboundRoute>> _inboundByEndpoint = [];

    // Outbound storage and indexes
    private readonly Dictionary<OutboundRoute, OutboundTrackedState> _outboundRoutes = [];
    private readonly Dictionary<MessageType, ImmutableHashSet<OutboundRoute>> _outboundByType = [];

    public IReadOnlyList<InboundRoute> InboundRoutes
    {
        get
        {
            lock (_lock)
            {
                return [.. _inboundRoutes.Keys];
            }
        }
    }

    public IReadOnlyList<OutboundRoute> OutboundRoutes
    {
        get
        {
            lock (_lock)
            {
                return [.. _outboundRoutes.Keys];
            }
        }
    }

    public ImmutableHashSet<InboundRoute> GetInboundByMessageType(MessageType messageType)
    {
        lock (_lock)
        {
            return _inboundByType.TryGetValue(messageType, out var set) ? set : [];
        }
    }

    public ImmutableHashSet<InboundRoute> GetInboundByConsumer(Consumer consumer)
    {
        lock (_lock)
        {
            return _inboundByConsumer.TryGetValue(consumer, out var set) ? set : [];
        }
    }

    public ImmutableHashSet<InboundRoute> GetInboundByEndpoint(ReceiveEndpoint endpoint)
    {
        lock (_lock)
        {
            return _inboundByEndpoint.TryGetValue(endpoint, out var set) ? set : [];
        }
    }

    public ImmutableHashSet<OutboundRoute> GetOutboundByMessageType(MessageType messageType)
    {
        lock (_lock)
        {
            return _outboundByType.TryGetValue(messageType, out var set) ? set : [];
        }
    }

    public DispatchEndpoint GetEndpoint(
        IMessagingConfigurationContext context,
        MessageType messageType,
        OutboundRouteKind kind)
    {
        lock (_lock)
        {
            OutboundRoute route;
            // AddMessage(...).Send/Publish registers a route before an endpoint exists.
            // Runtime lookup must materialize that same route instead of returning a null endpoint.
            if (_outboundByType.TryGetValue(messageType, out var set)
                && set.FirstOrDefault(r => r.Kind == kind) is { } existingRoute)
            {
                route = existingRoute;
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (route.Endpoint is not null)
                {
                    return route.Endpoint;
                }
            }
            else
            {
                route = new OutboundRoute();
                var configuration = new OutboundRouteConfiguration { MessageType = messageType, Kind = kind };
                route.Initialize(context, configuration);
            }

            // TODO not sure about this. What is the "default" transport?
            foreach (var transport in context.Transports)
            {
                var endpoint = transport.ConnectRoute(context, route);

                if (!endpoint.IsCompleted)
                {
                    endpoint.DiscoverTopology(context);
                    endpoint.Complete(context);
                    // Connect after completion so a route without an explicit destination gets
                    // the final endpoint address, while explicit destinations remain unchanged.
                    route.ConnectEndpoint(context, endpoint);
                }

                if (!route.IsCompleted)
                {
                    route.Complete(context);
                }

                return endpoint;
            }

            throw ThrowHelper.NoTransportForMessageType(messageType);
        }
    }

    public void AddOrUpdate(InboundRoute route)
    {
        ArgumentNullException.ThrowIfNull(route);

        if (!route.IsInitialized)
        {
            throw ThrowHelper.RouteMustBeInitialized();
        }

        lock (_lock)
        {
            if (_inboundRoutes.TryGetValue(route, out var oldState))
            {
                if (oldState.MessageType == route.MessageType
                    && oldState.Consumer == route.Consumer
                    && oldState.Endpoint == route.Endpoint)
                {
                    return; // No changes
                }

                // Update indexes where values changed
                UpdateIndex(_inboundByType, oldState.MessageType, route.MessageType, route);
                UpdateIndex(_inboundByConsumer, oldState.Consumer, route.Consumer, route);
                UpdateIndex(_inboundByEndpoint, oldState.Endpoint, route.Endpoint, route);

                oldState.MessageType = route.MessageType;
                oldState.Consumer = route.Consumer;
                oldState.Endpoint = route.Endpoint;
            }
            else
            {
                // New route
                _inboundRoutes[route] = new InboundTrackedState
                {
                    MessageType = route.MessageType,
                    Consumer = route.Consumer,
                    Endpoint = route.Endpoint
                };
                if (route.MessageType != null)
                {
                    AddToIndex(_inboundByType, route.MessageType, route);
                }
                AddToIndex(_inboundByConsumer, route.Consumer, route);
                if (route.Endpoint != null)
                {
                    AddToIndex(_inboundByEndpoint, route.Endpoint, route);
                }
            }
        }
    }

    public void AddOrUpdate(OutboundRoute route)
    {
        ArgumentNullException.ThrowIfNull(route);
        if (!route.IsInitialized)
        {
            throw ThrowHelper.RouteMustBeInitialized();
        }

        lock (_lock)
        {
            if (_outboundRoutes.TryGetValue(route, out var oldState))
            {
                if (oldState.MessageType == route.MessageType)
                {
                    return; // No changes
                }

                // Update indexes where values changed
                UpdateIndex(_outboundByType, oldState.MessageType, route.MessageType, route);

                oldState.MessageType = route.MessageType;
            }
            else
            {
                // New route
                _outboundRoutes[route] = new OutboundTrackedState { MessageType = route.MessageType };
                AddToIndex(_outboundByType, route.MessageType, route);
            }
        }
    }

    private static void UpdateIndex<TKey, TValue>(
        Dictionary<TKey, ImmutableHashSet<TValue>> index,
        TKey? oldKey,
        TKey? newKey,
        TValue value)
        where TKey : notnull
    {
        if (EqualityComparer<TKey>.Default.Equals(oldKey, newKey))
        {
            return;
        }

        if (oldKey != null)
        {
            RemoveFromIndex(index, oldKey, value);
        }

        if (newKey != null)
        {
            AddToIndex(index, newKey, value);
        }
    }

    private static void AddToIndex<TKey, TValue>(
        Dictionary<TKey, ImmutableHashSet<TValue>> dict,
        TKey key,
        TValue value)
        where TKey : notnull
    {
        if (dict.TryGetValue(key, out var set))
        {
            dict[key] = set.Add(value);
        }
        else
        {
            dict[key] = [value];
        }
    }

    private static void RemoveFromIndex<TKey, TValue>(
        Dictionary<TKey, ImmutableHashSet<TValue>> dict,
        TKey key,
        TValue value)
        where TKey : notnull
    {
        if (dict.TryGetValue(key, out var set))
        {
            set = set.Remove(value);
            if (set.IsEmpty)
            {
                dict.Remove(key);
            }
            else
            {
                dict[key] = set;
            }
        }
    }

    private class InboundTrackedState
    {
        public required MessageType? MessageType { get; set; }
        public required Consumer Consumer { get; set; }
        public required ReceiveEndpoint? Endpoint { get; set; }
    }

    private class OutboundTrackedState
    {
        public required MessageType MessageType { get; set; }
    }
}
