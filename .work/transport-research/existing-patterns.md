# Mocha MessageBus Transport Implementation Patterns

Research findings on existing RabbitMQ, Postgres, and InMemory transports. These patterns should inform the Azure Event Hub transport implementation.

---

## 1. DI Registration Pattern (Builder Extension Method)

All transports follow a consistent builder extension method pattern in `MessageBusBuilderExtensions.cs`:

**RabbitMQ:**
```csharp
public static IMessageBusHostBuilder AddRabbitMQ(
    this IMessageBusHostBuilder busBuilder,
    Action<IRabbitMQMessagingTransportDescriptor> configure)
{
    var transport = new RabbitMQMessagingTransport(x => configure(x.AddDefaults()));
    busBuilder.ConfigureMessageBus(b => b.AddTransport(transport));
    return busBuilder;
}
```

**Postgres:**
```csharp
public static IMessageBusHostBuilder AddPostgres(
    this IMessageBusHostBuilder busBuilder,
    Action<IPostgresMessagingTransportDescriptor> configure)
{
    var transport = new PostgresMessagingTransport(x => configure(x.AddDefaults()));
    busBuilder.ConfigureMessageBus(b => b.AddTransport(transport));
    return busBuilder;
}
```

**Key pattern:**
- Two overloads: one taking a configuration delegate, one parameterless
- Transport constructor receives a delegate that calls `x.AddDefaults()` before user configuration
- `AddDefaults()` registers naming conventions, middleware, and topology conventions
- Call `busBuilder.ConfigureMessageBus(b => b.AddTransport(transport))`

---

## 2. MessagingTransport Subclass

Each transport subclasses `MessagingTransport` with these lifecycle hooks:

### Constructor
```csharp
private readonly Action<IXxxMessagingTransportDescriptor> _configure;

public XxxMessagingTransport(Action<IXxxMessagingTransportDescriptor> configure)
{
    _configure = configure;
}
```

### Key Properties/Fields
```csharp
private XxxMessagingTopology _topology = null!;
public override MessagingTopology Topology => _topology;

// Transport-specific managers (examples from RabbitMQ/Postgres)
public RabbitMQConsumerManager ConsumerManager { get; private set; } = null!;
public RabbitMQDispatcher Dispatcher { get; private set; } = null!;
public IRabbitMQConnectionProvider Connection { get; private set; } = null!;
```

### Lifecycle Hooks

#### `CreateConfiguration(IMessagingSetupContext)`
Called during setup to build the `MessagingTransportConfiguration`. Creates the transport descriptor and invokes the user's configuration delegate:

```csharp
protected override MessagingTransportConfiguration CreateConfiguration(IMessagingSetupContext context)
{
    var descriptor = new RabbitMQMessagingTransportDescriptor(context);
    _configure(descriptor);
    return descriptor.CreateConfiguration();
}
```

#### `OnAfterInitialized(IMessagingSetupContext)`
Called after base initialization. Responsibility:
- Resolve transport connection providers from DI
- Build the topology URI from connection details (host, port, vhost, etc.)
- Create the MessagingTopology instance
- Add any declaratively configured topology resources (exchanges, queues, etc.)
- Create transport-specific managers (ConsumerManager, Dispatcher, etc.)

**RabbitMQ example:**
```csharp
protected override void OnAfterInitialized(IMessagingSetupContext context)
{
    var configuration = (RabbitMQTransportConfiguration)Configuration;

    // Get connection provider from config or DI
    Connection = configuration.ConnectionProvider?.Invoke(context.Services)
        ?? new ConnectionFactoryRabbitMQConnectionProvider(
            context.Services.GetApplicationServices().GetRequiredService<IConnectionFactory>());

    // Build topology base URI
    var builder = new UriBuilder
    {
        Scheme = Schema,
        Host = Connection.Host,
        Port = Connection.Port,
        Path = Connection.VirtualHost
    };

    // Create topology
    _topology = new RabbitMQMessagingTopology(
        this,
        builder.Uri,
        configuration.Defaults,
        configuration.AutoProvision ?? true);

    // Add declared resources
    foreach (var exchange in configuration.Exchanges)
        _topology.AddExchange(exchange);

    foreach (var queue in configuration.Queues)
        _topology.AddQueue(queue);

    foreach (var binding in configuration.Bindings)
        _topology.AddBinding(binding);

    // Create managers
    Dispatcher = CreateDispatcher(context);
    ConsumerManager = CreateConsumerManager(context);
}
```

#### `OnBeforeStartAsync(IMessagingConfigurationContext, CancellationToken)`
Called before endpoints start processing. Responsibilities:
- Ensure connections are established and healthy
- Provision topology resources (exchanges, queues, topics)
- Register consumers with the broker
- Start background services (listeners, heartbeat tasks, etc.)

**RabbitMQ (simple):**
```csharp
protected override async ValueTask OnBeforeStartAsync(
    IMessagingConfigurationContext context,
    CancellationToken cancellationToken)
{
    await Task.WhenAll(
        ConsumerManager.EnsureConnectedAsync(cancellationToken),
        Dispatcher.EnsureConnectedAsync(cancellationToken));
}
```

**Postgres (complex):**
```csharp
protected override async ValueTask OnBeforeStartAsync(
    IMessagingConfigurationContext context,
    CancellationToken cancellationToken)
{
    await ConnectionManager.EnsureMigratedAsync(cancellationToken);
    await ConsumerManager.RegisterAsync(cancellationToken);
    await NotificationListener.StartAsync(cancellationToken);

    // Provision topology
    var autoProvision = _topology.AutoProvision;
    foreach (var topic in _topology.Topics)
        if (topic.AutoProvision ?? autoProvision)
            await topic.ProvisionAsync(ConnectionManager, _schemaOptions, cancellationToken);
    // ... etc
}
```

### Endpoint Creation Methods

#### `CreateReceiveEndpoint()`
Returns a new `ReceiveEndpoint` instance bound to this transport. Should be a simple factory:

```csharp
protected override ReceiveEndpoint CreateReceiveEndpoint()
{
    return new RabbitMQReceiveEndpoint(this);
}
```

#### `CreateDispatchEndpoint()`
Returns a new `DispatchEndpoint` instance bound to this transport:

```csharp
protected override DispatchEndpoint CreateDispatchEndpoint()
{
    return new RabbitMQDispatchEndpoint(this);
}
```

### Endpoint Configuration Creation (3 overloads)

These are called by the framework to auto-generate configurations for discovered routes:

#### 1. From OutboundRoute (Send/Publish)
```csharp
public override DispatchEndpointConfiguration? CreateEndpointConfiguration(
    IMessagingConfigurationContext context,
    OutboundRoute route)
{
    if (route.Kind == OutboundRouteKind.Send)
    {
        var exchangeName = context.Naming.GetSendEndpointName(route.MessageType.RuntimeType);
        return new RabbitMQDispatchEndpointConfiguration
        {
            ExchangeName = exchangeName,
            Name = "e/" + exchangeName
        };
    }
    // Similar for Publish
    return null;
}
```

#### 2. From Uri Address
URI resolution pattern. Expects addresses like:
- `rabbitmq://empty/e/exchange-name` (exchange)
- `rabbitmq://empty/q/queue-name` (queue)
- `queue://queue-name` (shorthand)
- `exchange://exchange-name` (shorthand)
- Base topology URI + resource path

```csharp
public override DispatchEndpointConfiguration? CreateEndpointConfiguration(
    IMessagingConfigurationContext context,
    Uri address)
{
    var path = address.AbsolutePath.AsSpan();
    Span<Range> ranges = stackalloc Range[2];
    var segmentCount = path.Split(ranges, '/', RemoveEmptyEntries | TrimEntries);

    // Handle "rabbitmq://empty/e/name" and "rabbitmq://empty/q/name"
    if (address.Scheme == Schema && address.Host is "")
    {
        if (segmentCount == 2)
        {
            var kind = path[ranges[0]];  // "e" or "q"
            var name = path[ranges[1]];
            // ... create configuration
        }
    }

    // Handle shorthand "exchange://name" and "queue://name"
    if (address is { Scheme: "exchange" } && segmentCount == 1)
    {
        // ...
    }

    return null;  // No match
}
```

#### 3. From InboundRoute (Receive)
```csharp
public override ReceiveEndpointConfiguration CreateEndpointConfiguration(
    IMessagingConfigurationContext context,
    InboundRoute route)
{
    if (route.Kind == InboundRouteKind.Reply)
    {
        var instanceEndpointName = context.Naming.GetInstanceEndpoint(context.Host.InstanceId);
        return new RabbitMQReceiveEndpointConfiguration
        {
            Name = "Replies",
            QueueName = instanceEndpointName,
            IsTemporary = true,
            Kind = ReceiveEndpointKind.Reply,
            AutoProvision = true,
            ReceiveMiddlewares = [ReplyReceiveMiddleware.Create()]
        };
    }

    var queueName = context.Naming.GetReceiveEndpointName(route, ReceiveEndpointKind.Default);
    return new RabbitMQReceiveEndpointConfiguration { Name = queueName, QueueName = queueName };
}
```

### TryGetDispatchEndpoint(Uri) Method
Called at runtime to resolve a URI to an already-configured dispatch endpoint:

```csharp
public override bool TryGetDispatchEndpoint(Uri address, [NotNullWhen(true)] out DispatchEndpoint? endpoint)
{
    // Try exact match by address
    if (address.Scheme == Schema)
    {
        foreach (var candidate in DispatchEndpoints)
        {
            if (candidate.Address == address)
            {
                endpoint = candidate;
                return true;
            }
        }
    }

    // Try match by base URI + relative path
    if (Topology.Address.IsBaseOf(address))
    {
        foreach (var candidate in DispatchEndpoints)
        {
            if (candidate.Destination.Address == address)
            {
                endpoint = candidate;
                return true;
            }
        }
    }

    // Try shorthand resolution
    if (address is { Scheme: "queue", Segments: [var queueName] })
    {
        foreach (var candidate in DispatchEndpoints)
        {
            if (candidate.Destination is RabbitMQQueue queue && queue.Name == queueName)
            {
                endpoint = candidate;
                return true;
            }
        }
    }

    endpoint = null;
    return false;
}
```

### Describe() Method
Returns a `TransportDescription` with all configured endpoints and topology entities. Used for diagnostics/UI:

```csharp
public override TransportDescription Describe()
{
    var receiveEndpoints = ReceiveEndpoints.Select(e => e.Describe()).ToList();
    var dispatchEndpoints = DispatchEndpoints.Select(e => e.Describe()).ToList();

    var entities = new List<TopologyEntityDescription>();
    var links = new List<TopologyLinkDescription>();

    // Add exchanges, queues, bindings to entities/links with metadata
    foreach (var exchange in _topology.Exchanges)
    {
        entities.Add(new TopologyEntityDescription(
            "exchange", exchange.Name, exchange.Address?.ToString(),
            "inbound", new Dictionary<string, object?> { /* metadata */ }));
    }

    // ...

    return new TransportDescription(
        _topology.Address.ToString(),
        Name,
        Schema,
        nameof(RabbitMQMessagingTransport),
        receiveEndpoints,
        dispatchEndpoints,
        new TopologyDescription(_topology.Address.ToString(), entities, links));
}
```

### DisposeAsync()
Clean up transport resources:

```csharp
public override async ValueTask DisposeAsync()
{
    if (ConsumerManager is not null)
        await ConsumerManager.DisposeAsync();
    // ... other cleanup
}
```

---

## 3. Configuration Descriptor

The descriptor collects configuration from the fluent API and builds the final `MessagingTransportConfiguration`.

**Structure:**
```csharp
public sealed class RabbitMQMessagingTransportDescriptor
    : MessagingTransportDescriptor<RabbitMQTransportConfiguration>
    , IRabbitMQMessagingTransportDescriptor
{
    private readonly List<RabbitMQReceiveEndpointDescriptor> _receiveEndpoints = [];
    private readonly List<RabbitMQDispatchEndpointDescriptor> _dispatchEndpoints = [];
    private readonly List<RabbitMQExchangeDescriptor> _exchanges = [];
    private readonly List<RabbitMQQueueDescriptor> _queues = [];
    private readonly List<RabbitMQBindingDescriptor> _bindings = [];

    public RabbitMQMessagingTransportDescriptor(IMessagingSetupContext discoveryContext)
        : base(discoveryContext)
    {
        Configuration = new RabbitMQTransportConfiguration();
    }

    // Fluent API methods (delegate to base, return this for chaining)
    public new IRabbitMQMessagingTransportDescriptor ModifyOptions(Action<TransportOptions> configure) { ... }
    public new IRabbitMQMessagingTransportDescriptor Name(string name) { ... }
    // ... etc

    // Transport-specific configuration methods
    public IRabbitMQMessagingTransportDescriptor AutoProvision(bool autoProvision = true) { ... }
    public IRabbitMQMessagingTransportDescriptor ConnectionProvider(...) { ... }
    public IRabbitMQMessagingTransportDescriptor ConfigureDefaults(Action<RabbitMQBusDefaults> configure) { ... }

    // Topology declaration methods (return specific descriptor types)
    public IRabbitMQReceiveEndpointDescriptor Endpoint(string name) { ... }
    public IRabbitMQDispatchEndpointDescriptor DispatchEndpoint(string name) { ... }
    public IRabbitMQExchangeDescriptor DeclareExchange(string name) { ... }
    public IRabbitMQQueueDescriptor DeclareQueue(string name) { ... }
    public IRabbitMQBindingDescriptor DeclareBinding(string exchange, string queue) { ... }

    // Final build step
    public RabbitMQTransportConfiguration CreateConfiguration()
    {
        Configuration.ReceiveEndpoints = _receiveEndpoints
            .Select(e => e.CreateConfiguration()).ToList();
        Configuration.DispatchEndpoints = _dispatchEndpoints
            .Select(e => e.CreateConfiguration()).ToList();
        Configuration.Exchanges = _exchanges
            .Select(e => e.CreateConfiguration()).ToList();
        // ... etc
        return Configuration;
    }
}
```

**Key pattern:**
- Extends `MessagingTransportDescriptor<TConfig>` (base provides common fluent methods)
- Stores collected descriptors in lists
- Lazy deduplication in methods like `Endpoint()` (check if exists before adding)
- `CreateConfiguration()` converts all sub-descriptors to configuration objects

**TransportConfiguration base class:**
```csharp
public class RabbitMQTransportConfiguration : MessagingTransportConfiguration
{
    public const string DefaultName = "rabbitmq";
    public const string DefaultSchema = "rabbitmq";

    public RabbitMQTransportConfiguration()
    {
        Name = DefaultName;
        Schema = DefaultSchema;
    }

    // RabbitMQ-specific properties
    public Func<IServiceProvider, IRabbitMQConnectionProvider>? ConnectionProvider { get; set; }
    public List<RabbitMQExchangeConfiguration> Exchanges { get; set; } = [];
    public List<RabbitMQQueueConfiguration> Queues { get; set; } = [];
    public List<RabbitMQBindingConfiguration> Bindings { get; set; } = [];
    public bool? AutoProvision { get; set; }
    public RabbitMQBusDefaults Defaults { get; set; } = new();
}
```

---

## 4. Connection Management

### Connection Provider Abstraction

RabbitMQ defines an interface so custom providers can be injected:

```csharp
public interface IRabbitMQConnectionProvider
{
    string Host { get; }
    string VirtualHost { get; }
    int Port { get; }
    ValueTask<IConnection> CreateAsync(CancellationToken cancellationToken);
}
```

Default implementation wraps the underlying client library:

```csharp
public sealed class ConnectionFactoryRabbitMQConnectionProvider(IConnectionFactory factory)
    : IRabbitMQConnectionProvider
{
    public string Host => factory.Uri.Host;
    public string VirtualHost => factory.VirtualHost;
    public int Port => factory.Uri.Port;

    public async ValueTask<IConnection> CreateAsync(CancellationToken cancellationToken)
    {
        return await factory.CreateConnectionAsync(cancellationToken);
    }
}
```

**Key pattern:**
- Connection provider is resolved in `OnAfterInitialized()`
- Can be provided via config delegate or resolved from DI
- Used to build the topology base URI
- Passed to connection managers for creating new connections

### Connection Manager Base Class

RabbitMQ has `RabbitMQConnectionManagerBase` that handles:
- Connection pooling/lifecycle
- Reconnection logic with exponential backoff
- Disposal of connections and channels

Key interface:
```csharp
public sealed class RabbitMQDispatcher(
    ILogger<RabbitMQDispatcher> logger,
    Func<CancellationToken, ValueTask<IConnection>> connectionFactory,
    Func<IConnection, CancellationToken, Task> onConnectionEstablished)
    : RabbitMQConnectionManagerBase(logger, connectionFactory)
{
    // ...
}
```

**Pattern:**
- Managers receive a connection factory function: `Func<CancellationToken, ValueTask<IConnection>>`
- They receive an `onConnectionEstablished` callback to run topology provisioning
- Pool channels for reuse (RabbitMQ max 10 pooled channels)
- Expose `RentChannelAsync()` / `ReturnChannelAsync()` for endpoint dispatch

---

## 5. Receive Endpoint Pattern

### ReceiveEndpoint Subclass Structure

```csharp
public sealed class RabbitMQReceiveEndpoint(RabbitMQMessagingTransport transport)
    : ReceiveEndpoint<RabbitMQReceiveEndpointConfiguration>(transport)
{
    private ushort _maxPrefetch = 100;
    private ushort _consumerDispatchConcurrency = 1;

    public RabbitMQQueue Queue { get; private set; } = null!;

    protected override void OnInitialize(
        IMessagingConfigurationContext context,
        RabbitMQReceiveEndpointConfiguration configuration)
    {
        // Validate configuration
        if (configuration.QueueName is null)
            throw new InvalidOperationException("Queue name is required");

        _maxPrefetch = configuration.MaxPrefetch;
        _consumerDispatchConcurrency = (ushort)Math.Clamp(
            configuration.MaxConcurrency ?? ReceiveEndpointConfiguration.Defaults.MaxConcurrency,
            1,
            ushort.MaxValue);
    }

    protected override void OnComplete(
        IMessagingConfigurationContext context,
        RabbitMQReceiveEndpointConfiguration configuration)
    {
        // Resolve resource from topology
        var topology = (RabbitMQMessagingTopology)Transport.Topology;
        Queue = topology.Queues.FirstOrDefault(q => q.Name == configuration.QueueName)
            ?? throw new InvalidOperationException("Queue not found");
        Source = Queue;  // Set the IMessageSource
    }

    protected override async ValueTask OnStartAsync(
        IMessagingRuntimeContext context,
        CancellationToken cancellationToken)
    {
        // Register consumer with broker
        _consumer = await transport.ConsumerManager.RegisterConsumerAsync(
            Queue.Name,
            (channel, eventArgs, ct) =>
                ExecuteAsync(
                    static (context, state) =>
                    {
                        // Set transport-specific feature with raw message data
                        var feature = context.Features.GetOrSet<RabbitMQReceiveFeature>();
                        feature.Channel = state.channel;
                        feature.EventArgs = state.eventArgs;
                    },
                    (channel, eventArgs),
                    ct),
            _maxPrefetch,
            _consumerDispatchConcurrency,
            cancellationToken);
    }

    protected override async ValueTask OnStopAsync(
        IMessagingRuntimeContext context,
        CancellationToken cancellationToken)
    {
        if (_consumer is not null)
            await _consumer.DisposeAsync();
        _consumer = null;
    }
}
```

**Key patterns:**
- Subclass `ReceiveEndpoint<TConfiguration>`
- `OnInitialize()`: Validate configuration, parse settings
- `OnComplete()`: Resolve the Source (queue/topic) from topology
- `OnStartAsync()`: Register consumer with broker via ConsumerManager
  - Set up the transport-specific feature (e.g., RabbitMQReceiveFeature)
  - Store handle for cleanup in `OnStopAsync()`

### ConsumerManager Contract

The ConsumerManager handles actual message consumption:

```csharp
public sealed class RabbitMQConsumerManager(
    ILogger<RabbitMQConsumerManager> logger,
    Func<CancellationToken, ValueTask<IConnection>> connectionFactory)
    : RabbitMQConnectionManagerBase(logger, connectionFactory)
{
    public async ValueTask<IAsyncDisposable> RegisterConsumerAsync(
        string queueName,
        Func<IChannel, BasicDeliverEventArgs, CancellationToken, ValueTask> onMessageAsync,
        ushort prefetchSize,
        ushort consumerDispatchConcurrency,
        CancellationToken cancellationToken)
    {
        // Create channel, set QoS, attach handler
        // Return disposable token for cleanup
    }
}
```

---

## 6. Dispatch Endpoint Pattern

### DispatchEndpoint Subclass Structure

```csharp
public sealed class RabbitMQDispatchEndpoint(RabbitMQMessagingTransport transport)
    : DispatchEndpoint<RabbitMQDispatchEndpointConfiguration>(transport)
{
    public RabbitMQQueue? Queue { get; private set; }
    public RabbitMQExchange? Exchange { get; private set; }

    protected override async ValueTask DispatchAsync(IDispatchContext context)
    {
        if (context.Envelope is not { } envelope)
            throw new InvalidOperationException("Envelope is not set");

        var dispatcher = transport.Dispatcher;
        var channel = await dispatcher.RentChannelAsync(context.CancellationToken);
        try
        {
            await EnsureProvisionedAsync(channel, context.CancellationToken);
            await DispatchAsync(channel, envelope, context.CancellationToken);
        }
        finally
        {
            await dispatcher.ReturnChannelAsync(channel);
        }
    }

    private async ValueTask DispatchAsync(
        IChannel channel,
        MessageEnvelope envelope,
        CancellationToken cancellationToken)
    {
        // Build headers from envelope
        var headers = envelope.BuildHeaders();
        var messageType = envelope.MessageType ?? headers.Get(RabbitMQMessageHeaders.MessageType);

        // Create AMQP properties
        var properties = new BasicProperties
        {
            MessageId = envelope.MessageId,
            CorrelationId = envelope.CorrelationId,
            Type = messageType,
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
            ReplyTo = envelope.ResponseAddress,
            Headers = headers,
            ContentType = envelope.ContentType,
            DeliveryMode = DeliveryModes.Persistent
        };

        // Resolve target (exchange or queue)
        var exchangeName = CachedString.Empty;
        var routingKey = CachedString.Empty;
        if (Kind == DispatchEndpointKind.Reply)
        {
            // Dynamic address resolution from envelope.DestinationAddress
        }
        else
        {
            if (Exchange is not null)
                exchangeName = Exchange.CachedName;
            else if (Queue is not null)
                routingKey = Queue.CachedName;
        }

        // Publish
        await channel.BasicPublishAsync(exchangeName, routingKey, true, properties, envelope.Body, cancellationToken);
    }

    private async ValueTask EnsureProvisionedAsync(IChannel channel, CancellationToken cancellationToken)
    {
        if (_isProvisioned)
            return;

        var autoProvision = ((RabbitMQMessagingTopology)transport.Topology).AutoProvision;
        if (Queue is not null && (Queue.AutoProvision ?? autoProvision))
            await Queue.ProvisionAsync(channel, cancellationToken);
        if (Exchange is not null && (Exchange.AutoProvision ?? autoProvision))
            await Exchange.ProvisionAsync(channel, cancellationToken);

        _isProvisioned = true;
    }

    protected override void OnInitialize(
        IMessagingConfigurationContext context,
        RabbitMQDispatchEndpointConfiguration configuration)
    {
        if (configuration.ExchangeName is null && configuration.QueueName is null)
            throw new InvalidOperationException("Exchange name or queue name is required");
    }

    protected override void OnComplete(
        IMessagingConfigurationContext context,
        RabbitMQDispatchEndpointConfiguration configuration)
    {
        var topology = (RabbitMQMessagingTopology)Transport.Topology;
        if (configuration.ExchangeName is not null)
        {
            Exchange = topology.Exchanges.FirstOrDefault(e => e.Name == configuration.ExchangeName)
                ?? throw new InvalidOperationException("Exchange not found");
        }
        else if (configuration.QueueName is not null)
        {
            Queue = topology.Queues.FirstOrDefault(q => q.Name == configuration.QueueName)
                ?? throw new InvalidOperationException("Queue not found");
        }

        Destination = Exchange as TopologyResource
            ?? Queue as TopologyResource
            ?? throw new InvalidOperationException("Destination is not set");
    }
}
```

**Key patterns:**
- Subclass `DispatchEndpoint<TConfiguration>`
- `OnComplete()`: Resolve target resources (Exchange/Queue) from topology
- `DispatchAsync()` implementation:
  1. Rent a channel/connection from the dispatcher pool
  2. Ensure topology is provisioned (lazy provisioning)
  3. Build message headers from the envelope
  4. For reply endpoints: resolve destination dynamically from `envelope.DestinationAddress`
  5. For normal endpoints: use `Exchange` or `Queue` set in `OnComplete()`
  6. Publish the message
  7. Return channel to pool

---

## 7. Messaging Topology

### MessagingTopology Subclass

```csharp
public sealed class RabbitMQMessagingTopology(
    RabbitMQMessagingTransport transport,
    Uri baseAddress,
    RabbitMQBusDefaults defaults,
    bool autoProvision)
    : MessagingTopology<RabbitMQMessagingTransport>(transport, baseAddress)
{
#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif
    private readonly List<RabbitMQExchange> _exchanges = [];
    private readonly List<RabbitMQQueue> _queues = [];
    private readonly List<RabbitMQBinding> _bindings = [];

    public bool AutoProvision => autoProvision;
    public IReadOnlyList<RabbitMQExchange> Exchanges => _exchanges;
    public IReadOnlyList<RabbitMQQueue> Queues => _queues;
    public IReadOnlyList<RabbitMQBinding> Bindings => _bindings;
    public RabbitMQBusDefaults Defaults => defaults;

    public RabbitMQExchange AddExchange(RabbitMQExchangeConfiguration configuration)
    {
        lock (_lock)
        {
            var exchange = _exchanges.FirstOrDefault(e => e.Name == configuration.Name);
            if (exchange is not null)
                throw new InvalidOperationException($"Exchange '{configuration.Name}' already exists");

            exchange = new RabbitMQExchange();
            configuration.Topology = this;
            defaults.Exchange.ApplyTo(configuration);
            exchange.Initialize(configuration);
            _exchanges.Add(exchange);
            exchange.Complete();

            return exchange;
        }
    }

    // Similar for AddQueue, AddBinding
}
```

**Key patterns:**
- Extends `MessagingTopology<TTransport>`
- Thread-safe with lock (or `Lock` on NET9+)
- Stores topology resources in lists
- `Add*()` methods:
  1. Check for duplicates under lock
  2. Create new resource instance
  3. Apply bus defaults to configuration
  4. Initialize and complete the resource
  5. Add to list

### Topology Resource Base Classes

Resources inherit from `TopologyResource` (abstract):

```csharp
public sealed class RabbitMQExchange : TopologyResource
{
    // Properties set during initialization
    public string Name { get; private set; } = null!;
    public RabbitMQExchangeType Type { get; private set; }
    public bool Durable { get; private set; }
    public bool AutoDelete { get; private set; }
    public bool? AutoProvision { get; set; }

    // Cached for hot-path access
    public CachedString CachedName { get; private set; }

    public void Initialize(RabbitMQExchangeConfiguration configuration)
    {
        Name = configuration.Name;
        Type = configuration.Type;
        Durable = configuration.Durable;
        AutoDelete = configuration.AutoDelete;
        AutoProvision = configuration.AutoProvision;
        CachedName = new CachedString(Name);

        var builder = new UriBuilder(topology.Address)
        {
            Path = topology.Address.AbsolutePath.TrimEnd('/') + "/e/" + Name
        };
        Address = builder.Uri;
    }

    public async ValueTask ProvisionAsync(IChannel channel, CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(
            Name,
            Type.ToString().ToLowerInvariant(),
            Durable,
            AutoDelete,
            arguments: null,  // TODO arguments
            cancellationToken: cancellationToken);
    }
}
```

**Key patterns:**
- Properties are set during `Initialize()`
- `CachedString` for hot-path names (avoids allocations)
- `Address` property derived from topology base URI + resource kind + name
- `ProvisionAsync()` creates the resource on the broker/database

---

## 8. Feature Pattern (Transport-Specific State)

Transport-specific state is passed through the receive middleware pipeline via features:

```csharp
public sealed class RabbitMQReceiveFeature : IPooledFeature
{
    public IChannel Channel { get; set; } = null!;
    public BasicDeliverEventArgs EventArgs { get; set; } = null!;

    public void Initialize(object state) { }
    public void Reset()
    {
        Channel = null!;
        EventArgs = null!;
    }
}
```

Usage in `OnStartAsync()` of ReceiveEndpoint:

```csharp
_consumer = await transport.ConsumerManager.RegisterConsumerAsync(
    Queue.Name,
    (channel, eventArgs, ct) =>
        ExecuteAsync(
            static (context, state) =>
            {
                var feature = context.Features.GetOrSet<RabbitMQReceiveFeature>();
                feature.Channel = state.channel;
                feature.EventArgs = state.eventArgs;
            },
            (channel, eventArgs),
            ct),
    // ... other args
);
```

**Key pattern:**
- Implements `IPooledFeature` (can be pooled/reused)
- Set in a static lambda to avoid closures
- Retrieved later in middleware: `context.Features.GetOrSet<RabbitMQReceiveFeature>()`

---

## 9. Header Serialization (Postgres Pattern)

Postgres uses `Utf8JsonWriter` for efficient header serialization via a pooled feature:

```csharp
private static ReadOnlyMemory<byte> WriteHeadersJson(JsonHeadersFeature feature, MessageEnvelope envelope)
{
    using var writer = new Utf8JsonWriter(feature.Writer);
    writer.WriteStartObject();

    if (envelope.MessageId is not null)
        writer.WriteString(PostgresMessageHeaders.MessageId, envelope.MessageId);

    if (envelope.CorrelationId is not null)
        writer.WriteString(PostgresMessageHeaders.CorrelationId, envelope.CorrelationId);

    // ... etc

    writer.WriteEndObject();
    return feature.Writer.WrittenMemory;
}
```

Usage in dispatch:

```csharp
var feature = context.Features.GetOrSet<JsonHeadersFeature>();
var headers = WriteHeadersJson(feature, envelope);
```

**Key pattern:**
- Use `Utf8JsonWriter` with a pooled `IBufferWriter<byte>`
- Retrieve pooled feature from context
- Zero allocations on hot path (reuses buffers)

---

## 10. Naming Conventions

Transports use `IMessageTypeNamingConvention` from `context.Naming` to derive resource names:

```csharp
// In CreateEndpointConfiguration(OutboundRoute)
var exchangeName = context.Naming.GetSendEndpointName(route.MessageType.RuntimeType);
var exchangeName = context.Naming.GetPublishEndpointName(route.MessageType.RuntimeType);

// In CreateEndpointConfiguration(InboundRoute)
var queueName = context.Naming.GetReceiveEndpointName(route, ReceiveEndpointKind.Default);

// For reply endpoints
var instanceEndpointName = context.Naming.GetInstanceEndpoint(context.Host.InstanceId);
```

**Pattern:**
- Use conventions from the context, not magic strings
- Conventions are registered in `AddDefaults()` or via fluent API
- Different methods for send vs. publish vs. receive

---

## 11. Schema and Naming Conventions

Each transport defines a URI schema constant:

```csharp
public class RabbitMQTransportConfiguration : MessagingTransportConfiguration
{
    public const string DefaultName = "rabbitmq";
    public const string DefaultSchema = "rabbitmq";
}
```

And a default naming convention that applies to all addresses.

---

## Summary: Key Takeaways for Azure Event Hub Transport

1. **DI Pattern**: `AddAzureEventHub()` builder extension, transport constructor takes `Action<Descriptor>`
2. **Descriptor**: Fluent API that collects endpoint/topology config and builds `TransportConfiguration`
3. **Lifecycle**: `CreateConfiguration()` → `OnAfterInitialized()` → `OnBeforeStartAsync()`
4. **Connection**: Abstraction interface (`IAzureEventHubConnectionProvider`) resolved from DI or config
5. **Endpoints**: Subclass `ReceiveEndpoint<>` and `DispatchEndpoint<>`, implement lifecycle hooks
6. **Topology**: `MessagingTopology` subclass manages (hub, consumer groups, partition assignments, etc.)
7. **Features**: Transport-specific state via `IPooledFeature` (e.g., EventData properties)
8. **Headers**: Use `Utf8JsonWriter` + pooled feature for efficient serialization
9. **URI Resolution**: Parse `eventhub://hub-name` and `consumergroup://group-name` schemes
10. **Naming**: Use `context.Naming` conventions for auto-discovered endpoint names
