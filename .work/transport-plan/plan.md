# Azure Event Hub Transport Implementation Plan

## 1. Project Structure

### NuGet Package Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `Azure.Messaging.EventHubs` | 5.12.2 | Core client library: `EventHubProducerClient`, `EventData`, `EventProcessor<TPartition>` (from `Azure.Messaging.EventHubs.Primitives`) |
| `Azure.Identity` | 1.13.2 | `DefaultAzureCredential` and other token-based auth for production deployments |

**Not** using `Azure.Messaging.EventHubs.Processor` -- avoids the Blob Storage dependency. Instead, we subclass `EventProcessor<EventProcessorPartition>` from `Azure.Messaging.EventHubs.Primitives` with a pluggable checkpoint store. This gives us the SDK's built-in reconnection, partition load balancing, and failure recovery without requiring Blob Storage.

### Project File (.csproj)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <AssemblyName>Mocha.Transport.AzureEventHub</AssemblyName>
    <RootNamespace>Mocha.Transport.AzureEventHub</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Mocha.Abstractions\Mocha.Abstractions.csproj" />
    <ProjectReference Include="..\Mocha\Mocha.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Azure.Messaging.EventHubs" VersionOverride="5.12.2" />
    <PackageReference Include="Azure.Identity" VersionOverride="1.13.2" />
  </ItemGroup>
</Project>
```

Location: `src/Mocha/src/Mocha.Transport.AzureEventHub/Mocha.Transport.AzureEventHub.csproj`

### Complete File Listing

```
src/Mocha/src/Mocha.Transport.AzureEventHub/
├── Assembly.cs                                          # Assembly-level attributes
├── Mocha.Transport.AzureEventHub.csproj                 # Project file
├── MessageBusBuilderExtensions.cs                       # AddEventHub() DI registration
├── EventHubMessagingTransport.cs                        # Main transport class
├── EventHubDispatchEndpoint.cs                          # Dispatch endpoint (send to Event Hub)
├── EventHubReceiveEndpoint.cs                           # Receive endpoint (consume from Event Hub)
├── EventHubMessageEnvelopeParser.cs                     # EventData -> MessageEnvelope conversion
├── EventHubMessageHeaders.cs                            # Header key constants
├── Configurations/
│   ├── EventHubTransportConfiguration.cs                # Transport-level config + connection provider interface
│   ├── EventHubReceiveEndpointConfiguration.cs          # Receive endpoint config (consumer group, checkpoint interval)
│   ├── EventHubDispatchEndpointConfiguration.cs         # Dispatch endpoint config (hub name, partition key strategy)
│   └── EventHubBusDefaults.cs                           # Bus-level defaults (hub options, consumer group options)
├── Connection/
│   ├── EventHubConnectionManager.cs                     # Manages producer lifecycle (singleton per hub)
│   ├── ICheckpointStore.cs                              # Pluggable checkpoint store interface
│   ├── InMemoryCheckpointStore.cs                       # In-memory checkpoint store (Phase 1)
│   └── MochaEventProcessor.cs                           # Custom EventProcessor<EventProcessorPartition> subclass
├── Conventions/
│   ├── IEventHubReceiveEndpointTopologyConvention.cs    # Convention interface for receive topology
│   ├── IEventHubDispatchEndpointTopologyConvention.cs   # Convention interface for dispatch topology
│   ├── IEventHubReceiveEndpointConfigurationConvention.cs # Convention interface for receive config
│   ├── EventHubDefaultReceiveEndpointConvention.cs      # Default error/skipped naming
│   ├── EventHubReceiveEndpointTopologyConvention.cs     # Auto-provision hubs for receive
│   └── EventHubDispatchEndpointTopologyConvention.cs    # Auto-provision hubs for dispatch
├── Descriptors/
│   ├── IEventHubMessagingTransportDescriptor.cs          # Fluent API interface
│   ├── EventHubMessagingTransportDescriptor.cs           # Fluent API implementation
│   ├── IEventHubReceiveEndpointDescriptor.cs             # Receive endpoint fluent API
│   ├── EventHubReceiveEndpointDescriptor.cs              # Receive endpoint fluent API impl
│   ├── IEventHubDispatchEndpointDescriptor.cs            # Dispatch endpoint fluent API
│   └── EventHubDispatchEndpointDescriptor.cs             # Dispatch endpoint fluent API impl
├── Features/
│   ├── EventHubReceiveFeature.cs                         # Pooled feature: carries EventData through receive pipeline
│   └── EventHubDispatchFeature.cs                        # (Optional) carries send options through dispatch pipeline
├── Middlewares/
│   └── Receive/
│       ├── EventHubParsingMiddleware.cs                  # EventData -> MessageEnvelope parsing
│       ├── EventHubAcknowledgementMiddleware.cs          # Checkpoint-based acknowledgement
│       └── EventHubReceiveMiddlewares.cs                 # Static middleware configuration instances
├── Topology/
│   ├── EventHubMessagingTopology.cs                      # Topology container (hubs, subscriptions)
│   ├── EventHubTopic.cs                                  # TopologyResource: represents an Event Hub entity
│   ├── EventHubSubscription.cs                           # TopologyResource: represents a consumer group
│   ├── Configurations/
│   │   ├── EventHubTopicConfiguration.cs                 # Config for hub entity
│   │   └── EventHubSubscriptionConfiguration.cs          # Config for consumer group
│   ├── Descriptors/
│   │   ├── IEventHubTopicDescriptor.cs                   # Fluent API for hub declaration
│   │   ├── EventHubTopicDescriptor.cs                    # Implementation
│   │   ├── IEventHubSubscriptionDescriptor.cs            # Fluent API for consumer group
│   │   └── EventHubSubscriptionDescriptor.cs             # Implementation
│   └── Extensions/
│       ├── EventHubTransportDescriptorExtensions.cs      # AddDefaults() extension
│       └── EventHubMessageTypeDescriptorExtensions.cs    # Message type -> hub name mapping
```

---

## 2. Transport Class: EventHubMessagingTransport

### Constructor and Fields

```csharp
public sealed class EventHubMessagingTransport : MessagingTransport
{
    private readonly Action<IEventHubMessagingTransportDescriptor> _configure;
    private EventHubMessagingTopology _topology = null!;

    // Singleton clients -- thread-safe, long-lived
    public EventHubConnectionManager ConnectionManager { get; private set; } = null!;

    public override MessagingTopology Topology => _topology;

    public EventHubMessagingTransport(Action<IEventHubMessagingTransportDescriptor> configure)
    {
        _configure = configure;
    }
}
```

### CreateConfiguration

```csharp
protected override MessagingTransportConfiguration CreateConfiguration(IMessagingSetupContext context)
{
    var descriptor = new EventHubMessagingTransportDescriptor(context);
    _configure(descriptor);
    return descriptor.CreateConfiguration();
}
```

### OnAfterInitialized

```csharp
protected override void OnAfterInitialized(IMessagingSetupContext context)
{
    var configuration = (EventHubTransportConfiguration)Configuration;

    // Resolve connection provider
    var connectionProvider = configuration.ConnectionProvider?.Invoke(context.Services)
        ?? ResolveDefaultConnectionProvider(context, configuration);

    // Build topology base URI: eventhub://{namespace}
    var builder = new UriBuilder
    {
        Scheme = Schema,
        Host = connectionProvider.FullyQualifiedNamespace,
    };

    _topology = new EventHubMessagingTopology(
        this,
        builder.Uri,
        configuration.Defaults,
        configuration.AutoProvision ?? true);

    // Add declared topics and subscriptions from config
    foreach (var topic in configuration.Topics)
    {
        _topology.AddTopic(topic);
    }

    foreach (var subscription in configuration.Subscriptions)
    {
        _topology.AddSubscription(subscription);
    }

    // Create connection manager (producers only)
    ConnectionManager = new EventHubConnectionManager(
        context.Services.GetRequiredService<ILogger<EventHubConnectionManager>>(),
        connectionProvider);
}
```

### OnBeforeStartAsync

```csharp
protected override async ValueTask OnBeforeStartAsync(
    IMessagingConfigurationContext context,
    CancellationToken cancellationToken)
{
    // Provision topology if auto-provision is enabled
    // (Note: provisioning Event Hubs requires management credentials;
    //  for now we skip auto-provisioning -- hubs must exist)
}
```

### OnBeforeStopAsync

No override. Each receive endpoint's `OnStopAsync` manages its own processor lifecycle. This is consistent with RabbitMQ, which has no `OnBeforeStopAsync` and relies on per-endpoint cleanup.

### CreateEndpointConfiguration -- OutboundRoute

```csharp
public override DispatchEndpointConfiguration? CreateEndpointConfiguration(
    IMessagingConfigurationContext context,
    OutboundRoute route)
{
    EventHubDispatchEndpointConfiguration? configuration = null;

    if (route.Kind == OutboundRouteKind.Send)
    {
        var hubName = context.Naming.GetSendEndpointName(route.MessageType.RuntimeType);
        configuration = new EventHubDispatchEndpointConfiguration
        {
            HubName = hubName,
            Name = "h/" + hubName
        };
    }
    else if (route.Kind == OutboundRouteKind.Publish)
    {
        var hubName = context.Naming.GetPublishEndpointName(route.MessageType.RuntimeType);
        configuration = new EventHubDispatchEndpointConfiguration
        {
            HubName = hubName,
            Name = "h/" + hubName
        };
    }

    return configuration;
}
```

### CreateEndpointConfiguration -- Uri

```csharp
public override DispatchEndpointConfiguration? CreateEndpointConfiguration(
    IMessagingConfigurationContext context,
    Uri address)
{
    EventHubDispatchEndpointConfiguration? configuration = null;

    var path = address.AbsolutePath.AsSpan();
    Span<Range> ranges = stackalloc Range[2];
    var segmentCount = path.Split(ranges, '/', RemoveEmptyEntries | TrimEntries);

    // eventhub:///replies
    if (address.Scheme == Schema && address.Host is "")
    {
        if (segmentCount == 1 && path[ranges[0]] is "replies")
        {
            var instanceEndpointName = context.Naming.GetInstanceEndpoint(context.Host.InstanceId);
            configuration = new EventHubDispatchEndpointConfiguration
            {
                Kind = DispatchEndpointKind.Reply,
                HubName = instanceEndpointName,
                Name = "Replies"
            };
        }

        // eventhub:///h/{hub-name}
        if (segmentCount == 2)
        {
            var kind = path[ranges[0]];
            var name = path[ranges[1]];

            if (kind is "h")
            {
                configuration = new EventHubDispatchEndpointConfiguration
                {
                    HubName = new string(name),
                    Name = "h/" + new string(name)
                };
            }
        }
    }

    // eventhub://{namespace}/h/{hub-name}
    if (configuration is null && _topology.Address.IsBaseOf(address) && segmentCount == 2)
    {
        var kind = path[ranges[0]];
        var name = path[ranges[1]];

        if (kind is "h")
        {
            configuration = new EventHubDispatchEndpointConfiguration
            {
                HubName = new string(name),
                Name = "h/" + new string(name)
            };
        }
    }

    // hub://hub-name (shorthand)
    if (configuration is null && address is { Scheme: "hub" } && segmentCount == 1)
    {
        var name = path[ranges[0]];
        configuration = new EventHubDispatchEndpointConfiguration
        {
            HubName = new string(name),
            Name = "h/" + new string(name)
        };
    }

    return configuration;
}
```

### CreateEndpointConfiguration -- InboundRoute

```csharp
public override ReceiveEndpointConfiguration CreateEndpointConfiguration(
    IMessagingConfigurationContext context,
    InboundRoute route)
{
    EventHubReceiveEndpointConfiguration configuration;

    if (route.Kind == InboundRouteKind.Reply)
    {
        var instanceEndpointName = context.Naming.GetInstanceEndpoint(context.Host.InstanceId);
        configuration = new EventHubReceiveEndpointConfiguration
        {
            Name = "Replies",
            HubName = instanceEndpointName,
            ConsumerGroup = "$Default",
            IsTemporary = true,
            Kind = ReceiveEndpointKind.Reply,
            AutoProvision = true,
            ReceiveMiddlewares = [ReplyReceiveMiddleware.Create()]
        };
    }
    else
    {
        var hubName = context.Naming.GetReceiveEndpointName(route, ReceiveEndpointKind.Default);
        configuration = new EventHubReceiveEndpointConfiguration
        {
            Name = hubName,
            HubName = hubName,
            ConsumerGroup = "$Default"
        };
    }

    return configuration;
}
```

### TryGetDispatchEndpoint

```csharp
public override bool TryGetDispatchEndpoint(
    Uri address,
    [NotNullWhen(true)] out DispatchEndpoint? endpoint)
{
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

    if (address is { Scheme: "hub", Segments: [var hubName] })
    {
        foreach (var candidate in DispatchEndpoints)
        {
            if (candidate.Destination is EventHubTopic topic && topic.Name == hubName)
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

### Schema and Addressing

- **Schema**: `"eventhub"`
- **Addressing convention**: `eventhub://{namespace}/h/{hub-name}`
- **Shorthand**: `hub://{hub-name}`
- **Path prefix**: `h/` for hub (analogous to `e/` for exchange and `q/` for queue in RabbitMQ)

### Describe() Method

```csharp
public override TransportDescription Describe()
{
    var receiveEndpoints = ReceiveEndpoints.Select(e => e.Describe()).ToList();
    var dispatchEndpoints = DispatchEndpoints.Select(e => e.Describe()).ToList();

    var entities = new List<TopologyEntityDescription>();
    var links = new List<TopologyLinkDescription>();
    var autoProvision = _topology.AutoProvision;

    foreach (var topic in _topology.Topics)
    {
        entities.Add(
            new TopologyEntityDescription(
                "hub",
                topic.Name,
                topic.Address?.ToString(),
                "inbound",
                new Dictionary<string, object?>
                {
                    ["partitionCount"] = topic.PartitionCount,
                    ["autoProvision"] = topic.AutoProvision ?? autoProvision
                }));
    }

    foreach (var subscription in _topology.Subscriptions)
    {
        links.Add(
            new TopologyLinkDescription(
                "consumer-group",
                subscription.Address?.ToString(),
                _topology.Topics
                    .FirstOrDefault(t => t.Name == subscription.TopicName)
                    ?.Address?.ToString(),
                null,
                "subscribe",
                new Dictionary<string, object?>
                {
                    ["consumerGroup"] = subscription.ConsumerGroup,
                    ["autoProvision"] = subscription.AutoProvision ?? autoProvision
                }));
    }

    var topology = new TopologyDescription(_topology.Address.ToString(), entities, links);

    return new TransportDescription(
        _topology.Address.ToString(),
        Name,
        Schema,
        nameof(EventHubMessagingTransport),
        receiveEndpoints,
        dispatchEndpoints,
        topology);
}
```

### DisposeAsync

```csharp
public override async ValueTask DisposeAsync()
{
    if (ConnectionManager is not null)
    {
        await ConnectionManager.DisposeAsync();
    }
}
```

---

## 3. Connection Management

### Connection Provider Interface

```csharp
public interface IEventHubConnectionProvider
{
    /// <summary>
    /// Fully qualified namespace (e.g., "mynamespace.servicebus.windows.net").
    /// </summary>
    string FullyQualifiedNamespace { get; }

    /// <summary>
    /// Creates an EventHubProducerClient for the specified hub.
    /// </summary>
    EventHubProducerClient CreateProducer(string eventHubName);

    /// <summary>
    /// The connection string for this provider, or null if using token credentials.
    /// Used by MochaEventProcessor for connection creation.
    /// </summary>
    string? ConnectionString { get; }

    /// <summary>
    /// The token credential for this provider, or null if using connection string.
    /// </summary>
    TokenCredential? Credential { get; }
}
```

Two implementations:

```csharp
/// <summary>
/// Connection-string-based provider.
/// </summary>
public sealed class ConnectionStringEventHubConnectionProvider : IEventHubConnectionProvider
{
    private readonly string _connectionString;

    public ConnectionStringEventHubConnectionProvider(string connectionString)
    {
        _connectionString = connectionString;
        // Parse namespace from connection string
        var props = EventHubsConnectionStringProperties.Parse(connectionString);
        FullyQualifiedNamespace = props.FullyQualifiedNamespace;
    }

    public string FullyQualifiedNamespace { get; }
    public string? ConnectionString => _connectionString;
    public TokenCredential? Credential => null;

    public EventHubProducerClient CreateProducer(string eventHubName)
        => new(_connectionString, eventHubName);
}

/// <summary>
/// Azure Identity credential-based provider.
/// </summary>
public sealed class CredentialEventHubConnectionProvider : IEventHubConnectionProvider
{
    private readonly string _fullyQualifiedNamespace;
    private readonly TokenCredential _credential;

    public CredentialEventHubConnectionProvider(string fullyQualifiedNamespace, TokenCredential credential)
    {
        _fullyQualifiedNamespace = fullyQualifiedNamespace;
        _credential = credential;
    }

    public string FullyQualifiedNamespace => _fullyQualifiedNamespace;
    public string? ConnectionString => null;
    public TokenCredential? Credential => _credential;

    public EventHubProducerClient CreateProducer(string eventHubName)
        => new(_fullyQualifiedNamespace, eventHubName, _credential);
}
```

### EventHubConnectionManager

Manages singleton `EventHubProducerClient` instances per hub name. Producers are thread-safe and long-lived.

```csharp
public sealed class EventHubConnectionManager : IAsyncDisposable
{
    private readonly ILogger<EventHubConnectionManager> _logger;
    private readonly IEventHubConnectionProvider _connectionProvider;
    private readonly ConcurrentDictionary<string, EventHubProducerClient> _producers = new();

    public EventHubConnectionManager(
        ILogger<EventHubConnectionManager> logger,
        IEventHubConnectionProvider connectionProvider)
    {
        _logger = logger;
        _connectionProvider = connectionProvider;
    }

    public IEventHubConnectionProvider ConnectionProvider => _connectionProvider;

    public EventHubProducerClient GetOrCreateProducer(string eventHubName)
    {
        return _producers.GetOrAdd(eventHubName, static (name, state) =>
        {
            state._logger.CreatingProducerForHub(name);
            return state._connectionProvider.CreateProducer(name);
        }, this);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var (_, producer) in _producers)
        {
            await producer.DisposeAsync();
        }
        _producers.Clear();
    }
}
```

Note: `GetOrAdd` uses the `static` lambda with state tuple overload to avoid closure allocation on cache hits.

### ICheckpointStore

Pluggable checkpoint store interface for future extensibility. Phase 1 uses in-memory; future phases can implement Blob Storage, database, etc.

```csharp
public interface ICheckpointStore
{
    /// <summary>
    /// Gets the checkpoint (last processed sequence number) for a partition.
    /// Returns null if no checkpoint exists.
    /// </summary>
    ValueTask<long?> GetCheckpointAsync(
        string fullyQualifiedNamespace,
        string eventHubName,
        string consumerGroup,
        string partitionId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates the checkpoint for a partition.
    /// </summary>
    ValueTask SetCheckpointAsync(
        string fullyQualifiedNamespace,
        string eventHubName,
        string consumerGroup,
        string partitionId,
        long sequenceNumber,
        CancellationToken cancellationToken);
}
```

### InMemoryCheckpointStore

Tracks last processed sequence number per partition in memory. Provides at-least-once delivery within a process lifetime. Checkpoints are lost on restart.

```csharp
public sealed class InMemoryCheckpointStore : ICheckpointStore
{
    private readonly ConcurrentDictionary<string, long> _checkpoints = new();

    public ValueTask<long?> GetCheckpointAsync(
        string fullyQualifiedNamespace,
        string eventHubName,
        string consumerGroup,
        string partitionId,
        CancellationToken cancellationToken)
    {
        var key = BuildKey(fullyQualifiedNamespace, eventHubName, consumerGroup, partitionId);
        return _checkpoints.TryGetValue(key, out var seq)
            ? new ValueTask<long?>(seq)
            : new ValueTask<long?>((long?)null);
    }

    public ValueTask SetCheckpointAsync(
        string fullyQualifiedNamespace,
        string eventHubName,
        string consumerGroup,
        string partitionId,
        long sequenceNumber,
        CancellationToken cancellationToken)
    {
        var key = BuildKey(fullyQualifiedNamespace, eventHubName, consumerGroup, partitionId);
        _checkpoints.AddOrUpdate(key, sequenceNumber, static (_, newSeq) => newSeq);
        return default;
    }

    private static string BuildKey(string ns, string hub, string cg, string pid)
        => string.Concat(ns, "/", hub, "/", cg, "/", pid);
}
```

### MochaEventProcessor

Custom `EventProcessor<EventProcessorPartition>` subclass that provides:
- Built-in reconnection and failure recovery (from the SDK base class)
- Automatic partition load balancing across instances
- Pluggable checkpoint store (no Blob Storage required)
- Integration with the Mocha receive pipeline via a message handler delegate

```csharp
internal sealed class MochaEventProcessor : EventProcessor<EventProcessorPartition>
{
    private readonly ILogger _logger;
    private readonly Func<EventData, string, CancellationToken, ValueTask> _messageHandler;
    private readonly ICheckpointStore _checkpointStore;
    private readonly string _fullyQualifiedNamespace;
    private readonly string _eventHubName;
    private readonly string _consumerGroup;

    public MochaEventProcessor(
        ILogger logger,
        string consumerGroup,
        string fullyQualifiedNamespace,
        string eventHubName,
        TokenCredential credential,
        Func<EventData, string, CancellationToken, ValueTask> messageHandler,
        ICheckpointStore checkpointStore,
        EventProcessorOptions? options = null)
        : base(
            checkpointRetriever: default!, // overridden below
            eventBatchMaximumCount: options?.MaxBatchSize ?? 1,
            consumerGroup: consumerGroup,
            fullyQualifiedNamespace: fullyQualifiedNamespace,
            eventHubName: eventHubName,
            credential: credential,
            options: options)
    {
        _logger = logger;
        _messageHandler = messageHandler;
        _checkpointStore = checkpointStore;
        _fullyQualifiedNamespace = fullyQualifiedNamespace;
        _eventHubName = eventHubName;
        _consumerGroup = consumerGroup;
    }

    // Constructor overload for connection string
    public MochaEventProcessor(
        ILogger logger,
        string consumerGroup,
        string connectionString,
        string eventHubName,
        Func<EventData, string, CancellationToken, ValueTask> messageHandler,
        ICheckpointStore checkpointStore,
        EventProcessorOptions? options = null)
        : base(
            checkpointRetriever: default!,
            eventBatchMaximumCount: options?.MaxBatchSize ?? 1,
            consumerGroup: consumerGroup,
            connectionString: connectionString,
            eventHubName: eventHubName,
            options: options)
    {
        _logger = logger;
        _messageHandler = messageHandler;
        _checkpointStore = checkpointStore;
        // Parse from connection string
        var props = EventHubsConnectionStringProperties.Parse(connectionString);
        _fullyQualifiedNamespace = props.FullyQualifiedNamespace;
        _eventHubName = eventHubName;
        _consumerGroup = consumerGroup;
    }

    protected override async Task OnProcessingEventBatchAsync(
        IEnumerable<EventData> events,
        EventProcessorPartition partition,
        CancellationToken cancellationToken)
    {
        foreach (var eventData in events)
        {
            await _messageHandler(eventData, partition.PartitionId, cancellationToken);

            // Update in-memory checkpoint after successful processing
            await _checkpointStore.SetCheckpointAsync(
                _fullyQualifiedNamespace,
                _eventHubName,
                _consumerGroup,
                partition.PartitionId,
                eventData.SequenceNumber,
                cancellationToken);
        }
    }

    protected override Task OnProcessingErrorAsync(
        Exception exception,
        EventProcessorPartition? partition,
        string operationDescription,
        CancellationToken cancellationToken)
    {
        _logger.ErrorProcessingPartition(
            exception,
            _eventHubName,
            partition?.PartitionId ?? "unknown",
            operationDescription);

        // The base class handles reconnection and recovery automatically.
        // We just log the error.
        return Task.CompletedTask;
    }

    protected override async Task<EventProcessorCheckpoint> GetCheckpointAsync(
        string partitionId,
        CancellationToken cancellationToken)
    {
        var sequenceNumber = await _checkpointStore.GetCheckpointAsync(
            _fullyQualifiedNamespace,
            _eventHubName,
            _consumerGroup,
            partitionId,
            cancellationToken);

        if (sequenceNumber.HasValue)
        {
            return new EventProcessorCheckpoint
            {
                FullyQualifiedNamespace = _fullyQualifiedNamespace,
                EventHubName = _eventHubName,
                ConsumerGroup = _consumerGroup,
                PartitionId = partitionId,
                StartingPosition = EventPosition.FromSequenceNumber(sequenceNumber.Value, isInclusive: false)
            };
        }

        // No checkpoint -- start from latest (configurable via options)
        return new EventProcessorCheckpoint
        {
            FullyQualifiedNamespace = _fullyQualifiedNamespace,
            EventHubName = _eventHubName,
            ConsumerGroup = _consumerGroup,
            PartitionId = partitionId,
            StartingPosition = EventPosition.Latest
        };
    }

    protected override Task<IEnumerable<EventProcessorPartitionOwnership>> ClaimOwnershipAsync(
        IEnumerable<EventProcessorPartitionOwnership> desiredOwnership,
        CancellationToken cancellationToken)
    {
        // For Phase 1: accept all claimed partitions (single-instance mode).
        // The SDK calls this to coordinate partition ownership across instances.
        // With in-memory store, each instance claims all partitions it requests.
        return Task.FromResult(desiredOwnership);
    }

    protected override Task<IEnumerable<EventProcessorPartitionOwnership>> ListOwnershipAsync(
        CancellationToken cancellationToken)
    {
        // For Phase 1: return empty -- no distributed coordination.
        // Each instance processes all partitions independently.
        return Task.FromResult(Enumerable.Empty<EventProcessorPartitionOwnership>());
    }

    protected override Task<IEnumerable<EventProcessorCheckpoint>> ListCheckpointsAsync(
        CancellationToken cancellationToken)
    {
        // The base class calls this during startup to get all checkpoints.
        // We return empty and use GetCheckpointAsync per-partition instead.
        return Task.FromResult(Enumerable.Empty<EventProcessorCheckpoint>());
    }
}
```

### Thread Safety Model

- `EventHubProducerClient` is singleton per hub, thread-safe -- no pooling needed (unlike RabbitMQ channels)
- `MochaEventProcessor` manages its own CancellationTokenSource internally via `StartProcessingAsync`/`StopProcessingAsync`
- `ConcurrentDictionary` for producer cache
- Each partition's event processing is invoked sequentially by the SDK (one batch at a time per partition)

### Reconnection Strategy

The `EventProcessor<TPartition>` base class provides built-in reconnection:
- Automatically reconnects on transient AMQP failures
- Re-establishes partition readers after connection loss
- Rebalances partitions when instances join or leave
- Configurable retry via `EventProcessorOptions.RetryOptions`
- Errors surface via `OnProcessingErrorAsync` for logging

Producer failures: the Azure SDK handles transient failures internally per `EventHubsRetryOptions`. Unrecoverable failures surface as exceptions on `SendAsync`.

---

## 4. Dispatch Endpoint: EventHubDispatchEndpoint

```csharp
public sealed class EventHubDispatchEndpoint(EventHubMessagingTransport transport)
    : DispatchEndpoint<EventHubDispatchEndpointConfiguration>(transport)
{
    public EventHubTopic? Topic { get; private set; }

    protected override async ValueTask DispatchAsync(IDispatchContext context)
    {
        if (context.Envelope is not { } envelope)
        {
            throw new InvalidOperationException("Envelope is not set");
        }

        var cancellationToken = context.CancellationToken;

        // Resolve target hub name
        string hubName;
        if (Kind == DispatchEndpointKind.Reply)
        {
            // Dynamic destination from envelope
            if (!Uri.TryCreate(envelope.DestinationAddress, UriKind.Absolute, out var destinationAddress))
            {
                throw new InvalidOperationException("Destination address is not a valid URI");
            }

            var path = destinationAddress.AbsolutePath.AsSpan();
            Span<Range> ranges = stackalloc Range[2];
            var segmentCount = path.Split(ranges, '/', RemoveEmptyEntries | TrimEntries);

            if (segmentCount >= 1)
            {
                var lastSegment = path[ranges[segmentCount - 1]];
                hubName = new string(lastSegment);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Cannot determine hub name from destination address {destinationAddress}");
            }
        }
        else
        {
            hubName = Topic?.Name
                ?? throw new InvalidOperationException("Topic is not set on dispatch endpoint");
        }

        // Get or create producer for this hub
        var producer = transport.ConnectionManager.GetOrCreateProducer(hubName);

        // Build EventData from envelope
        var eventData = new EventData(envelope.Body);

        // Size validation: Event Hubs has a 1MB message size limit
        if (envelope.Body.Length > 1_048_576)
        {
            throw new InvalidOperationException(
                $"Message body size ({envelope.Body.Length} bytes) exceeds the Event Hubs " +
                $"maximum message size of 1MB. Consider splitting the message or using a " +
                $"claim-check pattern.");
        }

        // Map envelope headers -> AMQP structured properties (zero dictionary allocation path)
        var amqp = eventData.GetRawAmqpMessage();
        var props = amqp.Properties;

        if (envelope.MessageId is not null)
        {
            props.MessageId = new AmqpMessageId(envelope.MessageId);
        }

        if (envelope.CorrelationId is not null)
        {
            props.CorrelationId = new AmqpMessageId(envelope.CorrelationId);
        }

        if (envelope.ContentType is not null)
        {
            props.ContentType = envelope.ContentType;
        }

        // Use Subject for MessageType (structured AMQP property, no dict allocation)
        if (envelope.MessageType is not null)
        {
            props.Subject = envelope.MessageType;
        }

        if (envelope.ResponseAddress is not null)
        {
            props.ReplyTo = new AmqpAddress(envelope.ResponseAddress);
        }

        // Overflow headers go to ApplicationProperties
        var appProps = amqp.ApplicationProperties;

        if (envelope.ConversationId is not null)
        {
            appProps[EventHubMessageHeaders.ConversationId] = envelope.ConversationId;
        }

        if (envelope.CausationId is not null)
        {
            appProps[EventHubMessageHeaders.CausationId] = envelope.CausationId;
        }

        if (envelope.SourceAddress is not null)
        {
            appProps[EventHubMessageHeaders.SourceAddress] = envelope.SourceAddress;
        }

        if (envelope.DestinationAddress is not null)
        {
            appProps[EventHubMessageHeaders.DestinationAddress] = envelope.DestinationAddress;
        }

        if (envelope.FaultAddress is not null)
        {
            appProps[EventHubMessageHeaders.FaultAddress] = envelope.FaultAddress;
        }

        if (envelope.EnclosedMessageTypes is { Length: > 0 } types)
        {
            appProps[EventHubMessageHeaders.EnclosedMessageTypes] = string.Join(";", types);
        }

        if (envelope.SentAt is not null)
        {
            appProps[EventHubMessageHeaders.SentAt] = envelope.SentAt.Value.ToUnixTimeMilliseconds();
        }

        // Custom headers
        if (envelope.Headers is not null)
        {
            foreach (var header in envelope.Headers)
            {
                if (header.Value is not null)
                {
                    appProps[header.Key] = header.Value switch
                    {
                        DateTimeOffset dto => dto.ToUnixTimeMilliseconds(),
                        DateTime dt => new DateTimeOffset(dt).ToUnixTimeMilliseconds(),
                        _ => header.Value
                    };
                }
            }
        }

        // Partition key strategy: no partition key by default (round-robin distribution).
        // Explicit partition key via x-partition-key header for ordering guarantees.
        SendEventOptions? sendOptions = null;
        if (envelope.Headers?.TryGet("x-partition-key", out string? partitionKey) == true
            && partitionKey is not null)
        {
            sendOptions = new SendEventOptions { PartitionKey = partitionKey };
        }

        // NOTE: [eventData] allocates a single-element array on every dispatch.
        // This is unavoidable -- the SDK has no single-EventData SendAsync overload.
        // Motivation for Phase 5 EventDataBatch support.
        if (sendOptions is not null)
        {
            await producer.SendAsync([eventData], sendOptions, cancellationToken);
        }
        else
        {
            await producer.SendAsync([eventData], cancellationToken);
        }
    }

    protected override void OnInitialize(
        IMessagingConfigurationContext context,
        EventHubDispatchEndpointConfiguration configuration)
    {
        if (configuration.HubName is null)
        {
            throw new InvalidOperationException("Hub name is required");
        }
    }

    protected override void OnComplete(
        IMessagingConfigurationContext context,
        EventHubDispatchEndpointConfiguration configuration)
    {
        var topology = (EventHubMessagingTopology)Transport.Topology;

        if (configuration.HubName is not null)
        {
            Topic = topology.Topics.FirstOrDefault(t => t.Name == configuration.HubName)
                ?? throw new InvalidOperationException($"Topic '{configuration.HubName}' not found");
        }

        Destination = Topic
            ?? throw new InvalidOperationException("Destination is not set");
    }
}
```

### Key Design Decisions

1. **`EventData(ReadOnlyMemory<byte>)`** constructor wraps `envelope.Body` without copying -- zero-copy on send path
2. **AMQP structured properties** via `GetRawAmqpMessage()` for MessageId, CorrelationId, ContentType, Subject (MessageType), ReplyTo -- avoids `Properties` dictionary allocation
3. **ApplicationProperties** for overflow headers (ConversationId, CausationId, addresses, custom headers)
4. **Partition key**: no partition key by default (round-robin). Opt-in via `x-partition-key` header for ordering guarantees. See "Partition Key Strategy" section below.
5. **Direct `SendAsync(IEnumerable<EventData>)`** for single-message dispatch; batch optimization can be added later via `EventDataBatch`
6. **SendEventOptions** only allocated when partition key is present -- avoids per-dispatch allocation on the common path
7. **Size validation**: checks body size against 1MB limit before sending, with a clear error message

### Partition Key Strategy

**Default: no partition key (round-robin)**. When no partition key is set, Event Hubs distributes events across partitions using round-robin, providing even load distribution.

**Opt-in: explicit partition key** via the `x-partition-key` header. When set, all events with the same key are routed to the same partition, guaranteeing ordering for related messages.

**Why not CorrelationId by default**: Using CorrelationId as partition key risks hot partitions in saga-style workflows where many messages share a long-lived correlation ID. Round-robin is safer as a default. Users who need ordering guarantees for correlated messages should explicitly set `x-partition-key` to their CorrelationId.

---

## 5. Receive Endpoint: EventHubReceiveEndpoint

```csharp
public sealed class EventHubReceiveEndpoint(EventHubMessagingTransport transport)
    : ReceiveEndpoint<EventHubReceiveEndpointConfiguration>(transport)
{
    private string _consumerGroup = "$Default";
    public EventHubTopic Topic { get; private set; } = null!;
    public EventHubSubscription? Subscription { get; private set; }

    private MochaEventProcessor? _processor;

    protected override void OnInitialize(
        IMessagingConfigurationContext context,
        EventHubReceiveEndpointConfiguration configuration)
    {
        if (configuration.HubName is null)
        {
            throw new InvalidOperationException("Hub name is required");
        }

        _consumerGroup = configuration.ConsumerGroup ?? "$Default";
    }

    protected override void OnComplete(
        IMessagingConfigurationContext context,
        EventHubReceiveEndpointConfiguration configuration)
    {
        var topology = (EventHubMessagingTopology)Transport.Topology;

        Topic = topology.Topics.FirstOrDefault(t => t.Name == configuration.HubName)
            ?? throw new InvalidOperationException($"Topic '{configuration.HubName}' not found");

        Source = Topic;
    }

    protected override async ValueTask OnStartAsync(
        IMessagingRuntimeContext context,
        CancellationToken cancellationToken)
    {
        if (Transport is not EventHubMessagingTransport ehTransport)
        {
            throw new InvalidOperationException("Transport is not EventHubMessagingTransport");
        }

        var connectionProvider = ehTransport.ConnectionManager.ConnectionProvider;
        var checkpointStore = new InMemoryCheckpointStore();

        Func<EventData, string, CancellationToken, ValueTask> messageHandler =
            (eventData, partitionId, ct) =>
                ExecuteAsync(
                    static (context, state) =>
                    {
                        var feature = context.Features.GetOrSet<EventHubReceiveFeature>();
                        feature.EventData = state.eventData;
                        feature.PartitionId = state.partitionId;
                    },
                    (eventData, partitionId),
                    ct);

        var logger = context.Services.GetRequiredService<ILogger<MochaEventProcessor>>();

        if (connectionProvider.ConnectionString is not null)
        {
            _processor = new MochaEventProcessor(
                logger,
                _consumerGroup,
                connectionProvider.ConnectionString,
                Topic.Name,
                messageHandler,
                checkpointStore);
        }
        else if (connectionProvider.Credential is not null)
        {
            _processor = new MochaEventProcessor(
                logger,
                _consumerGroup,
                connectionProvider.FullyQualifiedNamespace,
                Topic.Name,
                connectionProvider.Credential,
                messageHandler,
                checkpointStore);
        }
        else
        {
            throw new InvalidOperationException(
                "Connection provider must supply either a connection string or token credential");
        }

        // StartProcessingAsync creates its own internal CancellationTokenSource.
        // The cancellationToken here is only used for the startup operation itself.
        await _processor.StartProcessingAsync(cancellationToken);
    }

    protected override async ValueTask OnStopAsync(
        IMessagingRuntimeContext context,
        CancellationToken cancellationToken)
    {
        if (_processor is not null)
        {
            // StopProcessingAsync gracefully cancels the internal processing loop
            // and waits for all in-flight event processing to complete.
            await _processor.StopProcessingAsync(cancellationToken);
            _processor = null;
        }
    }
}
```

### Consumer Strategy

Using `EventProcessor<EventProcessorPartition>` from `Azure.Messaging.EventHubs.Primitives`:
- **Built-in reconnection**: The SDK base class handles transient AMQP failures, reconnection, and partition reader recovery automatically
- **Partition load balancing**: When multiple instances run, the SDK automatically distributes partitions across instances (requires a shared ownership store for production -- Phase 1 uses in-memory which means each instance reads all partitions)
- **Pluggable checkpoint store**: `ICheckpointStore` interface allows swapping in-memory for Blob Storage, database, etc. without changing the processor
- **Configurable EventPosition**: `GetCheckpointAsync` returns the last processed position per partition; defaults to `EventPosition.Latest` when no checkpoint exists
- **No CancellationToken lifetime issues**: The processor manages its own internal CTS via `StartProcessingAsync`/`StopProcessingAsync`

### Concurrency Model

- **One processing call per partition at a time** -- the SDK enforces sequential processing per partition
- Cross-partition parallelism is inherent (the processor runs partition handlers concurrently)
- The Mocha concurrency limiter middleware handles additional concurrency control

### Delivery Semantics

Explicitly stated:

| Checkpoint Store | Delivery Guarantee |
|---|---|
| None (no checkpoints) | At-most-once: messages received but unprocessed on crash are lost |
| In-memory (Phase 1) | At-least-once within a process lifetime; at-most-once across restarts (checkpoints lost) |
| Persistent (Blob/DB, future) | At-least-once: survives restarts, reprocesses from last checkpoint |

Phase 1 uses in-memory checkpoints. This means:
- Within a running process, if the processor restarts a partition reader (e.g., after transient failure), it resumes from the last processed sequence number
- On full process restart, checkpoints are lost and the processor starts from `EventPosition.Latest`
- Messages published while the process is down are lost (at-most-once across restarts)

This is acceptable for Phase 1. Persistent checkpoint support is planned for Phase 2.

---

## 6. Topology

### EventHubMessagingTopology

```csharp
public sealed class EventHubMessagingTopology(
    EventHubMessagingTransport transport,
    Uri baseAddress,
    EventHubBusDefaults defaults,
    bool autoProvision)
    : MessagingTopology<EventHubMessagingTransport>(transport, baseAddress)
{
#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif
    private readonly List<EventHubTopic> _topics = [];
    private readonly List<EventHubSubscription> _subscriptions = [];

    public bool AutoProvision => autoProvision;
    public IReadOnlyList<EventHubTopic> Topics => _topics;
    public IReadOnlyList<EventHubSubscription> Subscriptions => _subscriptions;
    public EventHubBusDefaults Defaults => defaults;

    public EventHubTopic AddTopic(EventHubTopicConfiguration configuration)
    {
        lock (_lock)
        {
            var topic = _topics.FirstOrDefault(t => t.Name == configuration.Name);
            if (topic is not null)
            {
                throw new InvalidOperationException($"Topic '{configuration.Name}' already exists");
            }

            topic = new EventHubTopic();
            configuration.Topology = this;
            defaults.Topic.ApplyTo(configuration);
            topic.Initialize(configuration);
            _topics.Add(topic);
            topic.Complete();

            return topic;
        }
    }

    public EventHubSubscription AddSubscription(EventHubSubscriptionConfiguration configuration)
    {
        lock (_lock)
        {
            var sub = new EventHubSubscription();
            configuration.Topology = this;
            sub.Initialize(configuration);
            _subscriptions.Add(sub);
            sub.Complete();
            return sub;
        }
    }
}
```

### EventHubTopic (Topology Resource)

Maps to an Event Hub entity (the publish/subscribe target).

```csharp
public sealed class EventHubTopic : TopologyResource<EventHubTopicConfiguration>
{
    public string Name { get; private set; } = null!;
    public int? PartitionCount { get; private set; }
    public bool? AutoProvision { get; private set; }

    protected override void OnInitialize(EventHubTopicConfiguration configuration)
    {
        Name = configuration.Name;
        PartitionCount = configuration.PartitionCount;
        AutoProvision = configuration.AutoProvision;
    }

    protected override void OnComplete(EventHubTopicConfiguration configuration)
    {
        var address = new UriBuilder(Topology.Address);
        // Use explicit "/" separators -- not Path.Combine (OS-specific separators)
        var basePath = address.Path.TrimEnd('/');
        address.Path = basePath + "/h/" + configuration.Name;
        Address = address.Uri;
    }
}
```

### EventHubSubscription (Topology Resource)

Maps to a consumer group on an Event Hub.

```csharp
public sealed class EventHubSubscription : TopologyResource<EventHubSubscriptionConfiguration>
{
    public string TopicName { get; private set; } = null!;
    public string ConsumerGroup { get; private set; } = null!;
    public bool? AutoProvision { get; private set; }

    protected override void OnInitialize(EventHubSubscriptionConfiguration configuration)
    {
        TopicName = configuration.TopicName;
        ConsumerGroup = configuration.ConsumerGroup;
        AutoProvision = configuration.AutoProvision;
    }

    protected override void OnComplete(EventHubSubscriptionConfiguration configuration)
    {
        var address = new UriBuilder(Topology.Address);
        var basePath = address.Path.TrimEnd('/');
        address.Path = basePath + "/h/" + configuration.TopicName + "/cg/" + configuration.ConsumerGroup;
        Address = address.Uri;
    }
}
```

### Addressing Convention

```
eventhub://{namespace}/h/{hub-name}                           # Topic (hub)
eventhub://{namespace}/h/{hub-name}/cg/{consumer-group}       # Subscription (consumer group)
hub://{hub-name}                                              # Shorthand for topic
```

### Provisioning

Auto-provisioning Event Hubs requires management-level permissions (typically via Azure Resource Manager or the management API). For the initial implementation:
- **No auto-provisioning** -- Event Hubs and consumer groups must be pre-created
- The `AutoProvision` flag is on the topology model for future implementation
- When implemented, would use `Azure.ResourceManager.EventHubs` to create/manage hub entities

---

## 7. Configuration & Descriptor

### EventHubTransportConfiguration

```csharp
public class EventHubTransportConfiguration : MessagingTransportConfiguration
{
    public const string DefaultName = "eventhub";
    public const string DefaultSchema = "eventhub";

    public EventHubTransportConfiguration()
    {
        Name = DefaultName;
        Schema = DefaultSchema;
    }

    public Func<IServiceProvider, IEventHubConnectionProvider>? ConnectionProvider { get; set; }
    public string? ConnectionString { get; set; }
    public string? FullyQualifiedNamespace { get; set; }
    public List<EventHubTopicConfiguration> Topics { get; set; } = [];
    public List<EventHubSubscriptionConfiguration> Subscriptions { get; set; } = [];
    public bool? AutoProvision { get; set; }
    public EventHubBusDefaults Defaults { get; set; } = new();
}
```

### IEventHubMessagingTransportDescriptor

```csharp
public interface IEventHubMessagingTransportDescriptor
    : IMessagingTransportDescriptor
    , IMessagingDescriptor<EventHubTransportConfiguration>
{
    new IEventHubMessagingTransportDescriptor ModifyOptions(Action<TransportOptions> configure);
    new IEventHubMessagingTransportDescriptor Schema(string schema);
    new IEventHubMessagingTransportDescriptor BindHandlersImplicitly();
    new IEventHubMessagingTransportDescriptor BindHandlersExplicitly();
    new IEventHubMessagingTransportDescriptor Name(string name);
    new IEventHubMessagingTransportDescriptor AddConvention(IConvention convention);
    new IEventHubMessagingTransportDescriptor IsDefaultTransport();
    new IEventHubMessagingTransportDescriptor UseDispatch(
        DispatchMiddlewareConfiguration configuration, string? before = null, string? after = null);
    new IEventHubMessagingTransportDescriptor UseReceive(
        ReceiveMiddlewareConfiguration configuration, string? before = null, string? after = null);

    // Event Hub-specific
    IEventHubMessagingTransportDescriptor ConnectionString(string connectionString);
    IEventHubMessagingTransportDescriptor Namespace(string fullyQualifiedNamespace);
    IEventHubMessagingTransportDescriptor ConnectionProvider(
        Func<IServiceProvider, IEventHubConnectionProvider> connectionProvider);
    IEventHubMessagingTransportDescriptor AutoProvision(bool autoProvision = true);
    IEventHubMessagingTransportDescriptor ConfigureDefaults(Action<EventHubBusDefaults> configure);

    IEventHubReceiveEndpointDescriptor Endpoint(string name);
    IEventHubDispatchEndpointDescriptor DispatchEndpoint(string name);
    IEventHubTopicDescriptor DeclareTopic(string name);
    IEventHubSubscriptionDescriptor DeclareSubscription(string topicName, string consumerGroup);
}
```

### Builder Extension Method

```csharp
public static class MessageBusBuilderExtensions
{
    public static IMessageBusHostBuilder AddEventHub(
        this IMessageBusHostBuilder busBuilder,
        Action<IEventHubMessagingTransportDescriptor> configure)
    {
        var transport = new EventHubMessagingTransport(x => configure(x.AddDefaults()));
        busBuilder.ConfigureMessageBus(b => b.AddTransport(transport));
        return busBuilder;
    }

    public static IMessageBusHostBuilder AddEventHub(this IMessageBusHostBuilder busBuilder)
    {
        return busBuilder.AddEventHub(static _ => { });
    }
}
```

---

## 8. Features

### EventHubReceiveFeature

```csharp
public sealed class EventHubReceiveFeature : IPooledFeature
{
    public EventData EventData { get; set; } = null!;
    public string PartitionId { get; set; } = null!;

    public void Initialize(object state)
    {
        EventData = null!;
        PartitionId = null!;
    }

    public void Reset()
    {
        EventData = null!;
        PartitionId = null!;
    }
}
```

### EventHubDispatchFeature (Optional)

Only needed if dispatch middleware needs to carry `SendEventOptions` or `EventDataBatch` context:

```csharp
public sealed class EventHubDispatchFeature : IPooledFeature
{
    public SendEventOptions? SendOptions { get; set; }

    public void Initialize(object state) => SendOptions = null;
    public void Reset() => SendOptions = null;
}
```

---

## 9. Message Envelope Parser

```csharp
internal sealed class EventHubMessageEnvelopeParser
{
    public MessageEnvelope Parse(EventData eventData)
    {
        var amqp = eventData.GetRawAmqpMessage();
        var hasAppProps = amqp.HasSection(AmqpMessageSection.ApplicationProperties);
        IDictionary<string, object>? appProps = hasAppProps ? amqp.ApplicationProperties : null;

        var envelope = new MessageEnvelope
        {
            // Body: zero-copy from EventBody
            Body = eventData.EventBody.ToMemory(),

            // Structured AMQP properties (no dictionary)
            MessageId = amqp.Properties.MessageId?.ToString(),
            CorrelationId = amqp.Properties.CorrelationId?.ToString(),
            ContentType = amqp.Properties.ContentType,
            MessageType = amqp.Properties.Subject,
            ResponseAddress = amqp.Properties.ReplyTo?.ToString(),

            // ApplicationProperties (overflow headers)
            ConversationId = appProps?.GetStringOrNull(EventHubMessageHeaders.ConversationId),
            CausationId = appProps?.GetStringOrNull(EventHubMessageHeaders.CausationId),
            SourceAddress = appProps?.GetStringOrNull(EventHubMessageHeaders.SourceAddress),
            DestinationAddress = appProps?.GetStringOrNull(EventHubMessageHeaders.DestinationAddress),
            FaultAddress = appProps?.GetStringOrNull(EventHubMessageHeaders.FaultAddress),

            // SentAt from ApplicationProperties
            SentAt = appProps?.GetDateTimeOffsetOrNull(EventHubMessageHeaders.SentAt),

            // EnclosedMessageTypes
            EnclosedMessageTypes = ParseEnclosedMessageTypes(appProps),

            // DeliveryCount: Event Hubs doesn't track per-message delivery,
            // so always 0 for first delivery
            DeliveryCount = 0,

            // Build custom headers from remaining ApplicationProperties
            Headers = BuildHeaders(appProps),
        };

        return envelope;
    }

    private static ImmutableArray<string> ParseEnclosedMessageTypes(
        IDictionary<string, object>? appProps)
    {
        if (appProps is null)
        {
            return [];
        }

        if (appProps.TryGetValue(EventHubMessageHeaders.EnclosedMessageTypes, out var value)
            && value is string typesStr
            && !string.IsNullOrEmpty(typesStr))
        {
            return [.. typesStr.Split(';', StringSplitOptions.RemoveEmptyEntries)];
        }

        return [];
    }

    private static Headers BuildHeaders(IDictionary<string, object>? appProps)
    {
        if (appProps is null || appProps.Count == 0)
        {
            return Headers.Empty();
        }

        // Short-circuit: if all app properties are well-known transport headers,
        // return empty without allocating a Headers instance.
        var hasCustomHeaders = false;
        foreach (var (key, _) in appProps)
        {
            if (!EventHubMessageHeaders.IsWellKnown(key))
            {
                hasCustomHeaders = true;
                break;
            }
        }

        if (!hasCustomHeaders)
        {
            return Headers.Empty();
        }

        var result = new Headers(appProps.Count);
        foreach (var (key, value) in appProps)
        {
            if (EventHubMessageHeaders.IsWellKnown(key))
            {
                continue;
            }

            result.Set(key, value);
        }

        return result;
    }

    public static readonly EventHubMessageEnvelopeParser Instance = new();
}
```

### EventHubMessageHeaders

```csharp
internal static class EventHubMessageHeaders
{
    public const string ConversationId = "x-conversation-id";
    public const string CausationId = "x-causation-id";
    public const string SourceAddress = "x-source-address";
    public const string DestinationAddress = "x-destination-address";
    public const string FaultAddress = "x-fault-address";
    public const string EnclosedMessageTypes = "x-enclosed-message-types";
    public const string SentAt = "x-sent-at";

    private static readonly HashSet<string> s_wellKnown =
    [
        ConversationId,
        CausationId,
        SourceAddress,
        DestinationAddress,
        FaultAddress,
        EnclosedMessageTypes,
        SentAt,
    ];

    public static bool IsWellKnown(string key) => s_wellKnown.Contains(key);
}
```

---

## 10. Performance Design

### Zero-Copy Body

- **Send**: `new EventData(envelope.Body)` wraps `ReadOnlyMemory<byte>` without copying
- **Receive**: `eventData.EventBody.ToMemory()` returns `ReadOnlyMemory<byte>` without copying

### Structured AMQP Properties

Map these envelope fields to structured AMQP properties (no dictionary allocation):

| Envelope Field | AMQP Property |
|---|---|
| `MessageId` | `AmqpMessageProperties.MessageId` |
| `CorrelationId` | `AmqpMessageProperties.CorrelationId` |
| `ContentType` | `AmqpMessageProperties.ContentType` |
| `MessageType` | `AmqpMessageProperties.Subject` |
| `ResponseAddress` | `AmqpMessageProperties.ReplyTo` |

### HasSection() Guard

```csharp
// On receive, check before accessing ApplicationProperties to avoid lazy dict allocation
if (amqp.HasSection(AmqpMessageSection.ApplicationProperties))
{
    // only then access amqp.ApplicationProperties
}
```

### URI Parsing

Use `stackalloc Range[]` pattern from RabbitMQ:
```csharp
var path = address.AbsolutePath.AsSpan();
Span<Range> ranges = stackalloc Range[2];
var segmentCount = path.Split(ranges, '/', RemoveEmptyEntries | TrimEntries);
```

### Producer Singleton

`EventHubProducerClient` is thread-safe and manages its own AMQP connection pool internally. One singleton per hub name, cached in `ConcurrentDictionary`. No channel pooling needed (unlike RabbitMQ).

### SendEventOptions Conditional Allocation

`SendEventOptions` is only allocated when a partition key is explicitly provided via the `x-partition-key` header. When no partition key is needed (the common case with round-robin default), the parameterless `SendAsync` overload is used, avoiding the allocation entirely.

### BuildHeaders Short-Circuit

`BuildHeaders` checks whether any non-well-known headers exist before allocating a `Headers` instance. In the common case where all `ApplicationProperties` are well-known transport headers, it returns `Headers.Empty()` without allocation.

### Known Unavoidable Allocations

| Allocation | Location | Reason |
|---|---|---|
| `[eventData]` single-element array | `DispatchAsync` | SDK has no single-EventData `SendAsync` overload. Motivation for Phase 5 `EventDataBatch` support. |
| `string.Join(";", types)` | `DispatchAsync` (EnclosedMessageTypes) | Bounded by message type count (typically 1-3). Acceptable. |
| `new string(lastSegment)` | Reply dispatch hub name resolution | Needed for `GetOrCreateProducer(string)` key. Consistent with RabbitMQ pattern. |

### Batch Optimization (Phase 5)

For high-throughput scenarios, `EventDataBatch` can be added later:
```csharp
using var batch = await producer.CreateBatchAsync(cancellationToken);
if (!batch.TryAdd(eventData))
{
    // Event too large for batch -- send individually
}
await producer.SendAsync(batch, cancellationToken);
```

This is a future optimization, not needed for the initial implementation.

### Hot Path Rules

- No LINQ on message dispatch/receive hot paths
- No string concatenation on hot paths -- use pre-built URIs from topology
- Avoid `Properties` dictionary on `EventData` -- use `GetRawAmqpMessage()` for structured access
- `EventHubMessageHeaders` constants are `const string` (no allocation)
- `SendEventOptions` only allocated when partition key is present

---

## 11. Middlewares

### EventHubParsingMiddleware

```csharp
internal sealed class EventHubParsingMiddleware
{
    public async ValueTask InvokeAsync(IReceiveContext context, ReceiveDelegate next)
    {
        var feature = context.Features.GetOrSet<EventHubReceiveFeature>();
        var eventData = feature.EventData;

        var envelope = EventHubMessageEnvelopeParser.Instance.Parse(eventData);
        context.SetEnvelope(envelope);

        await next(context);
    }

    private static readonly EventHubParsingMiddleware s_instance = new();

    public static ReceiveMiddlewareConfiguration Create()
        => new(static (_, next) => ctx => s_instance.InvokeAsync(ctx, next), "EventHubParsing");
}
```

### EventHubAcknowledgementMiddleware

Event Hubs has no per-message ack. This middleware is a no-op pass-through for now. The `MochaEventProcessor` tracks processed sequence numbers via the checkpoint store after successful processing.

```csharp
internal sealed class EventHubAcknowledgementMiddleware
{
    public async ValueTask InvokeAsync(IReceiveContext context, ReceiveDelegate next)
    {
        // Event Hubs does not have per-message ack.
        // Process the message. If it throws, the exception propagates.
        // Checkpoint-based progress tracking is handled by the MochaEventProcessor.
        await next(context);
    }

    private static readonly EventHubAcknowledgementMiddleware s_instance = new();

    public static ReceiveMiddlewareConfiguration Create()
        => new(
            static (_, next) => ctx => s_instance.InvokeAsync(ctx, next),
            "EventHubAcknowledgement");
}
```

### EventHubReceiveMiddlewares

```csharp
public static class EventHubReceiveMiddlewares
{
    public static readonly ReceiveMiddlewareConfiguration Acknowledgement =
        EventHubAcknowledgementMiddleware.Create();

    public static readonly ReceiveMiddlewareConfiguration Parsing =
        EventHubParsingMiddleware.Create();
}
```

### AddDefaults Extension

```csharp
internal static IEventHubMessagingTransportDescriptor AddDefaults(
    this IEventHubMessagingTransportDescriptor descriptor)
{
    descriptor.AddConvention(new EventHubDefaultReceiveEndpointConvention());
    descriptor.AddConvention(new EventHubReceiveEndpointTopologyConvention());
    descriptor.AddConvention(new EventHubDispatchEndpointTopologyConvention());

    descriptor.UseReceive(
        EventHubReceiveMiddlewares.Acknowledgement,
        after: ReceiveMiddlewares.ConcurrencyLimiter.Key);
    descriptor.UseReceive(
        EventHubReceiveMiddlewares.Parsing,
        after: EventHubReceiveMiddlewares.Acknowledgement.Key);

    return descriptor;
}
```

---

## 12. Conventions

### EventHubDefaultReceiveEndpointConvention

Uses a single shared error hub and a single shared skipped hub per transport, rather than per-endpoint. This reduces the number of hubs that must be pre-created.

```csharp
public sealed class EventHubDefaultReceiveEndpointConvention : IEventHubReceiveEndpointConfigurationConvention
{
    public void Configure(
        IMessagingConfigurationContext context,
        EventHubMessagingTransport transport,
        EventHubReceiveEndpointConfiguration configuration)
    {
        configuration.HubName ??= configuration.Name;
        configuration.ConsumerGroup ??= "$Default";

        if (configuration is { Kind: ReceiveEndpointKind.Default })
        {
            if (configuration.ErrorEndpoint is null)
            {
                // Shared error hub for the entire transport
                configuration.ErrorEndpoint = new Uri($"{transport.Schema}:h/error");
            }

            if (configuration.SkippedEndpoint is null)
            {
                // Shared skipped hub for the entire transport
                configuration.SkippedEndpoint = new Uri($"{transport.Schema}:h/skipped");
            }
        }
    }
}
```

**Design decision**: A single shared `error` hub and a single shared `skipped` hub per transport instead of per-endpoint (`error-{hubname}`, `skipped-{hubname}`). Rationale:
- Event Hubs are not free -- each hub consumes namespace capacity
- Without auto-provisioning, fewer hubs need to be pre-created
- The original message's source endpoint is preserved in the `x-source-address` header, so the error hub consumer can still identify which endpoint the failed message came from
- This matches common Event Hub patterns where dead-letter processing is centralized

### EventHubReceiveEndpointTopologyConvention

```csharp
public sealed class EventHubReceiveEndpointTopologyConvention : IEventHubReceiveEndpointTopologyConvention
{
    public void DiscoverTopology(
        IMessagingConfigurationContext context,
        EventHubReceiveEndpoint endpoint,
        EventHubReceiveEndpointConfiguration configuration)
    {
        if (configuration.HubName is null)
        {
            throw new InvalidOperationException("Hub name is required");
        }

        var topology = (EventHubMessagingTopology)endpoint.Transport.Topology;

        // Ensure topic exists in topology model
        if (topology.Topics.FirstOrDefault(t => t.Name == configuration.HubName) is null)
        {
            topology.AddTopic(new EventHubTopicConfiguration
            {
                Name = configuration.HubName,
                AutoProvision = configuration.AutoProvision
            });
        }

        if (endpoint.Kind is ReceiveEndpointKind.Reply or ReceiveEndpointKind.Error
            or ReceiveEndpointKind.Skipped)
        {
            return;
        }

        // For each inbound route, ensure a topic exists for that message type
        var routes = context.Router.GetInboundByEndpoint(endpoint);
        foreach (var route in routes)
        {
            if (route.MessageType is null)
            {
                continue;
            }

            var publishHubName = context.Naming.GetPublishEndpointName(route.MessageType.RuntimeType);
            if (topology.Topics.FirstOrDefault(t => t.Name == publishHubName) is null)
            {
                topology.AddTopic(new EventHubTopicConfiguration { Name = publishHubName });
            }

            var sendHubName = context.Naming.GetSendEndpointName(route.MessageType.RuntimeType);
            if (sendHubName != publishHubName
                && topology.Topics.FirstOrDefault(t => t.Name == sendHubName) is null)
            {
                topology.AddTopic(new EventHubTopicConfiguration { Name = sendHubName });
            }
        }
    }
}
```

### EventHubDispatchEndpointTopologyConvention

```csharp
public sealed class EventHubDispatchEndpointTopologyConvention : IEventHubDispatchEndpointTopologyConvention
{
    public void DiscoverTopology(
        IMessagingConfigurationContext context,
        EventHubDispatchEndpoint endpoint,
        EventHubDispatchEndpointConfiguration configuration)
    {
        var topology = (EventHubMessagingTopology)endpoint.Transport.Topology;

        if (configuration.HubName is not null
            && topology.Topics.FirstOrDefault(t => t.Name == configuration.HubName) is null)
        {
            topology.AddTopic(new EventHubTopicConfiguration { Name = configuration.HubName });
        }
    }
}
```

---

## 13. Error/Skipped Hub Strategy

### Shared Hubs

The transport uses a single shared `error` hub and a single shared `skipped` hub per transport instance:

| Hub | URI | Purpose |
|---|---|---|
| `error` | `eventhub:h/error` | All failed messages from any receive endpoint |
| `skipped` | `eventhub:h/skipped` | All skipped messages from any receive endpoint |

### Operational Requirements

These two hubs must be pre-created in the Event Hubs namespace (since auto-provisioning is deferred):
1. `error` -- receives messages that failed processing
2. `skipped` -- receives messages that were skipped (no matching handler)

The source endpoint is identifiable from the `x-source-address` header on each failed/skipped message.

### Error Routing Failure

If the error hub does not exist when error routing is attempted, the `SendAsync` call will throw. This surfaces as a logged error. The original message is lost in this case (no retry, no dead-letter). Users must ensure the error/skipped hubs exist before starting the transport.

---

## 14. Reply Endpoint Strategy

The per-instance reply hub requires auto-provisioning, which is deferred. Options for Phase 1:

### Option A: Shared Reply Hub with Partition Key Routing (Recommended)

Use a shared reply hub (e.g., `replies`) with partition key routing based on instance ID:
- All instances share a single pre-created `replies` hub
- Reply messages are sent with `PartitionKey = instanceId`
- Each instance's `MochaEventProcessor` reads all partitions but the reply middleware filters by instance ID
- Avoids the need for per-instance hub creation

```csharp
// In CreateEndpointConfiguration (InboundRoute, Reply kind):
configuration = new EventHubReceiveEndpointConfiguration
{
    Name = "Replies",
    HubName = "replies",  // shared hub
    ConsumerGroup = "$Default",
    IsTemporary = false,   // not temporary -- shared
    Kind = ReceiveEndpointKind.Reply,
    ReceiveMiddlewares = [ReplyReceiveMiddleware.Create()]
};
```

### Option B: Pre-Created Reply Hubs

Require reply hubs to follow a well-known naming pattern (e.g., `reply-{instanceId}`). Users must pre-create hubs for each expected instance. Not recommended for dynamic scaling.

### Option C: Require Auto-Provisioning for Request-Reply

Accept that request-reply requires management permissions and auto-provisioning support. Not available in Phase 1.

**Phase 1 decision**: Use Option A (shared reply hub). The `replies` hub must be pre-created. The reply dispatch endpoint sets `PartitionKey = targetInstanceId` so replies cluster on the same partition for a given target. The reply receive middleware matches by instance ID.

---

## 15. Test Plan

### Fixture Design

The Azure Event Hub Emulator requires a `Config.json` file that pre-declares Event Hub names. Dynamic hub creation at runtime is not supported without management API access. The test strategy uses the emulator with pre-configured hubs and isolates tests via unique consumer groups.

**Option A: Azure Event Hubs Emulator (Docker)**
- Use the official emulator image: `mcr.microsoft.com/azure-messaging/eventhubs-emulator`
- Pre-declare a fixed set of Event Hubs in `Config.json` (e.g., `test-hub-1` through `test-hub-10`, `error`, `skipped`, `replies`)
- Isolate tests via unique consumer groups (not unique hubs)

**Option B: Real Azure Event Hubs (CI/CD)**
- Use a dedicated test namespace with pre-created hubs
- Clean up consumer groups after tests

Recommend **Option A** for local development, **Option B** for CI.

### Emulator Config.json

```json
{
  "UserConfig": {
    "NamespaceConfig": [
      {
        "Type": "EventHub",
        "Name": "test-namespace",
        "Entities": [
          { "Name": "test-hub-send", "PartitionCount": "4", "ConsumerGroups": [{ "Name": "$Default" }] },
          { "Name": "test-hub-pubsub", "PartitionCount": "4", "ConsumerGroups": [{ "Name": "$Default" }] },
          { "Name": "test-hub-reqreply", "PartitionCount": "4", "ConsumerGroups": [{ "Name": "$Default" }] },
          { "Name": "test-hub-batch", "PartitionCount": "4", "ConsumerGroups": [{ "Name": "$Default" }] },
          { "Name": "test-hub-fault", "PartitionCount": "4", "ConsumerGroups": [{ "Name": "$Default" }] },
          { "Name": "test-hub-concurrency", "PartitionCount": "4", "ConsumerGroups": [{ "Name": "$Default" }] },
          { "Name": "test-hub-headers", "PartitionCount": "4", "ConsumerGroups": [{ "Name": "$Default" }] },
          { "Name": "test-hub-middleware", "PartitionCount": "4", "ConsumerGroups": [{ "Name": "$Default" }] },
          { "Name": "test-hub-partition", "PartitionCount": "4", "ConsumerGroups": [{ "Name": "$Default" }, { "Name": "group-a" }, { "Name": "group-b" }] },
          { "Name": "error", "PartitionCount": "2", "ConsumerGroups": [{ "Name": "$Default" }] },
          { "Name": "skipped", "PartitionCount": "2", "ConsumerGroups": [{ "Name": "$Default" }] },
          { "Name": "replies", "PartitionCount": "4", "ConsumerGroups": [{ "Name": "$Default" }] }
        ]
      }
    ]
  }
}
```

### Isolation Strategy

- **Fixed set of Event Hubs** pre-declared in emulator Config.json
- **Unique consumer groups per test** for isolation (consumer groups are lightweight and can be created via the SDK if the emulator supports it, or pre-declared)
- All tests share the same namespace/emulator
- Collection fixture: `[Collection("EventHub")]`

### Fixture Structure

```csharp
public sealed class EventHubFixture : IAsyncLifetime
{
    // Docker container management for emulator
    // OR connection string for real Azure Event Hubs
    public string ConnectionString { get; private set; } = null!;

    public string GetHubForTest(string testCategory)
    {
        // Returns the pre-configured hub name for the test category
        return testCategory switch
        {
            "send" => "test-hub-send",
            "pubsub" => "test-hub-pubsub",
            "reqreply" => "test-hub-reqreply",
            "batch" => "test-hub-batch",
            "fault" => "test-hub-fault",
            "concurrency" => "test-hub-concurrency",
            "headers" => "test-hub-headers",
            "middleware" => "test-hub-middleware",
            "partition" => "test-hub-partition",
            _ => throw new ArgumentException($"Unknown test category: {testCategory}")
        };
    }

    public string GetUniqueConsumerGroup()
    {
        // Generate a unique consumer group for test isolation
        return $"test-{Guid.NewGuid():N}";
    }

    public async Task InitializeAsync()
    {
        // Start emulator container, set connection string
    }

    public async Task DisposeAsync()
    {
        // Stop emulator container
    }
}
```

### Behavior Tests to Adapt

Copy from RabbitMQ tests, changing fixture and transport registration:

1. **SendTests** -- send to hub, handler receives
2. **PublishSubscribeTests** -- publish to hub, multiple subscribers receive
3. **RequestReplyTests** -- request-reply pattern (uses shared replies hub)
4. **BatchingTests** -- message batching
5. **FaultHandlingTests** -- error handling (errors go to shared error hub)
6. **ConcurrencyLimiterTests** -- concurrency constraints
7. **ConcurrencyTests** -- concurrent message processing
8. **ErrorQueueTests** -- failed message routing to error hub
9. **CustomHeaderTests** -- custom headers propagation via ApplicationProperties
10. **BusDefaultsIntegrationTests** -- default behavior
11. **AutoProvisionIntegrationTests** -- hub/consumer group creation (when implemented)
12. **TransportMiddlewareTests** -- middleware pipeline
13. **EndpointMiddlewareTests** -- endpoint middleware

### Transport-Specific Tests

1. **PartitionRoutingTests** -- verify partition key routing (same key = same partition)
2. **ConsumerGroupIsolationTests** -- two consumer groups see all events independently
3. **EventHubTopologyTests** -- topology model building
4. **EventHubMessageEnvelopeParserTests** -- round-trip header serialization
5. **UriResolutionTests** -- test all URI forms including `hub://` shorthand

### Test Registration Pattern

```csharp
var hubName = _fixture.GetHubForTest("send");
var consumerGroup = _fixture.GetUniqueConsumerGroup();

await using var bus = await new ServiceCollection()
    .AddSingleton(recorder)
    .AddMessageBus()
    .AddRequestHandler<ProcessPaymentHandler>()
    .AddEventHub(t => t
        .ConnectionString(_fixture.ConnectionString)
        .Endpoint(hubName, e => e.ConsumerGroup(consumerGroup)))
    .BuildTestBusAsync();
```

---

## 16. Implementation Order

### Phase 1: Minimum Viable Transport (Send + Receive)

1. **Project setup**: Create `.csproj`, `Assembly.cs`, add package references
2. **Configuration classes**: `EventHubTransportConfiguration`, `EventHubReceiveEndpointConfiguration`, `EventHubDispatchEndpointConfiguration`, `EventHubBusDefaults`
3. **Connection provider**: `IEventHubConnectionProvider`, `ConnectionStringEventHubConnectionProvider`
4. **Checkpoint store**: `ICheckpointStore`, `InMemoryCheckpointStore`
5. **Connection manager**: `EventHubConnectionManager` (producer management)
6. **Event processor**: `MochaEventProcessor` (custom `EventProcessor<EventProcessorPartition>` with in-memory checkpoints, built-in reconnection)
7. **Message headers**: `EventHubMessageHeaders`
8. **Envelope parser**: `EventHubMessageEnvelopeParser`
9. **Features**: `EventHubReceiveFeature`
10. **Middlewares**: `EventHubParsingMiddleware`, `EventHubAcknowledgementMiddleware`, `EventHubReceiveMiddlewares`
11. **Topology**: `EventHubMessagingTopology`, `EventHubTopic`, `EventHubSubscription`, configurations
12. **Transport class**: `EventHubMessagingTransport` (all lifecycle hooks, endpoint creation, URI resolution, `Describe()`)
13. **Dispatch endpoint**: `EventHubDispatchEndpoint` (with size validation, conditional SendEventOptions)
14. **Receive endpoint**: `EventHubReceiveEndpoint` (using `MochaEventProcessor`)
15. **Conventions**: `EventHubDefaultReceiveEndpointConvention` (shared error/skipped), `EventHubReceiveEndpointTopologyConvention`, `EventHubDispatchEndpointTopologyConvention`
16. **Descriptor**: `IEventHubMessagingTransportDescriptor`, `EventHubMessagingTransportDescriptor`, `AddDefaults()`
17. **Builder extension**: `MessageBusBuilderExtensions.AddEventHub()`

**Verification**: Build compiles, basic send/receive test passes.

### Phase 2: Endpoint Descriptors + Topology Descriptors

18. **Endpoint descriptors**: `IEventHubReceiveEndpointDescriptor`, `EventHubReceiveEndpointDescriptor`, `IEventHubDispatchEndpointDescriptor`, `EventHubDispatchEndpointDescriptor`
19. **Topology descriptors**: `IEventHubTopicDescriptor`, `EventHubTopicDescriptor`, `IEventHubSubscriptionDescriptor`, `EventHubSubscriptionDescriptor`
20. **Convention interfaces**: `IEventHubReceiveEndpointTopologyConvention`, `IEventHubDispatchEndpointTopologyConvention`, `IEventHubReceiveEndpointConfigurationConvention`

**Verification**: Explicit topology configuration works, descriptors build correctly.

### Phase 3: Test Infrastructure

21. **Test project setup**: Create test project with fixture
22. **EventHubFixture**: Docker-based emulator with pre-configured `Config.json`
23. **TestBus extension**: `BuildTestBusAsync()` for Event Hub
24. **SendTests**: First behavior test
25. **PublishSubscribeTests**: Multi-subscriber test
26. **UriResolutionTests**: All URI forms including `hub://` shorthand

**Verification**: Core messaging patterns work end-to-end.

### Phase 4: Full Feature Parity

27. **RequestReplyTests**: Request-reply flows (shared replies hub)
28. **Error handling**: Error hub routing, fault handling tests
29. **Custom headers**: Round-trip header tests
30. **Concurrency**: Concurrency limiter and concurrent processing tests
31. **Credential provider**: `CredentialEventHubConnectionProvider` with Azure Identity
32. **Consumer restart test**: Verify behavior of messages sent during consumer downtime

**Verification**: All adapted behavior tests pass.

### Phase 5: Advanced Features (Future)

33. **Persistent checkpoint store**: Blob Storage or database-backed `ICheckpointStore` for at-least-once delivery across restarts
34. **Distributed partition ownership**: Shared ownership store for `ClaimOwnershipAsync`/`ListOwnershipAsync` to enable proper partition balancing across instances
35. **Batch dispatch**: `EventDataBatch` support for high-throughput
36. **Auto-provisioning**: Create Event Hubs and consumer groups via management API
37. **Partition-aware routing**: Explicit partition targeting
38. **Health monitoring**: Health check that verifies the processor is running and partitions are being read
