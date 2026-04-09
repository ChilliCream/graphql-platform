# Mocha Kafka Transport - Public API Surface

## Overview

The Mocha Kafka transport integrates Apache Kafka as a messaging transport for the Mocha message bus framework. This document comprehensively catalogs all public types, methods, configuration options, and builder APIs that developers interact with.

---

## 1. Registration & Setup

### MessageBusBuilderExtensions

**Namespace:** `Mocha.Transport.Kafka`

Extension methods for registering Kafka transport on the message bus.

#### Methods

| Signature | Returns | Summary |
|-----------|---------|---------|
| `AddKafka(this IMessageBusHostBuilder busBuilder, Action<IKafkaMessagingTransportDescriptor> configure)` | `IMessageBusHostBuilder` | Adds a Kafka transport with the specified configuration delegate; applies default conventions and middleware after registration. |
| `AddKafka(this IMessageBusHostBuilder busBuilder)` | `IMessageBusHostBuilder` | Adds a Kafka transport with default configuration and conventions. |

---

## 2. Transport Configuration Interfaces & Descriptors

### IKafkaMessagingTransportDescriptor

**Namespace:** `Mocha.Transport.Kafka`  
**Inherits:** `IMessagingTransportDescriptor`, `IMessagingDescriptor<KafkaTransportConfiguration>`

Fluent descriptor interface for configuring a Kafka messaging transport. All methods return `this` for chaining.

#### Inherited Methods (returning IKafkaMessagingTransportDescriptor)

| Signature | Summary |
|-----------|---------|
| `ModifyOptions(Action<TransportOptions> configure)` | Override transport-level options. |
| `Schema(string schema)` | Set the URI schema for this transport. |
| `BindHandlersImplicitly()` | Automatically bind all available handlers. |
| `BindHandlersExplicitly()` | Require explicit handler binding. |
| `Name(string name)` | Set the logical name of the transport. |
| `AddConvention(IConvention convention)` | Register a convention to modify transport behavior. |
| `IsDefaultTransport()` | Mark this as the default transport for message routing. |
| `UseDispatch(DispatchMiddlewareConfiguration configuration, string? before = null, string? after = null)` | Register dispatch-side middleware. |
| `UseReceive(ReceiveMiddlewareConfiguration configuration, string? before = null, string? after = null)` | Register receive-side middleware. |

#### Kafka-Specific Methods

| Signature | Summary |
|-----------|---------|
| `BootstrapServers(string bootstrapServers)` | Set Kafka bootstrap servers (comma-separated host:port pairs). **Required.** |
| `ConfigureProducer(Action<ProducerConfig> configure)` | Apply custom settings to the Kafka producer (`Confluent.Kafka.ProducerConfig`). |
| `ConfigureConsumer(Action<ConsumerConfig> configure)` | Apply custom settings to the Kafka consumer (`Confluent.Kafka.ConsumerConfig`). |
| `ConfigureDefaults(Action<KafkaBusDefaults> configure)` | Set bus-level defaults applied to all auto-provisioned topics. |
| `Endpoint(string name)` | Declare a receive endpoint; returns `IKafkaReceiveEndpointDescriptor`. |
| `DispatchEndpoint(string name)` | Declare a dispatch endpoint; returns `IKafkaDispatchEndpointDescriptor`. |
| `DeclareTopic(string name)` | Declare a topic in the transport topology; returns `IKafkaTopicDescriptor`. |
| `AutoProvision(bool autoProvision = true)` | Control whether topology resources (topics) auto-provision during startup. |

---

### KafkaMessagingTransportDescriptor

**Namespace:** `Mocha.Transport.Kafka`  
**Visibility:** Sealed class (internal implementation of `IKafkaMessagingTransportDescriptor`)

#### Constructor

```csharp
public KafkaMessagingTransportDescriptor(IMessagingSetupContext discoveryContext)
```

#### Key Methods

| Signature | Summary |
|-----------|---------|
| `KafkaTransportConfiguration CreateConfiguration()` | Build the final transport configuration from all accumulated descriptor settings, including receive/dispatch endpoints. |
| `static KafkaMessagingTransportDescriptor New(IMessagingSetupContext discoveryContext)` | Factory method to create a new descriptor. |

---

### KafkaTransportConfiguration

**Namespace:** `Mocha.Transport.Kafka`  
**Inherits:** `MessagingTransportConfiguration`

Runtime configuration for a Kafka transport instance.

#### Constants

| Name | Value | Summary |
|------|-------|---------|
| `DefaultName` | `"kafka"` | Default transport name. |
| `DefaultSchema` | `"kafka"` | Default URI schema for Kafka addresses. |

#### Properties

| Name | Type | Summary |
|------|------|---------|
| `BootstrapServers` | `string?` | Kafka bootstrap servers (comma-separated host:port pairs). |
| `ProducerConfigOverrides` | `Action<ProducerConfig>?` | Delegate to override producer settings. |
| `ConsumerConfigOverrides` | `Action<ConsumerConfig>?` | Delegate to override consumer settings. |
| `Topics` | `List<KafkaTopicConfiguration>` | List of explicitly declared topics. |
| `AutoProvision` | `bool?` | Whether topology resources auto-provision (default: true). |
| `Defaults` | `KafkaBusDefaults` | Bus-level defaults applied to auto-provisioned topics. |

---

### KafkaBusDefaults

**Namespace:** `Mocha.Transport.Kafka`

Defines bus-level defaults applied to all auto-provisioned topics.

#### Properties

| Name | Type | Summary |
|------|------|---------|
| `Topic` | `KafkaDefaultTopicOptions` | Default topic options applied to all auto-provisioned topics. |

---

### KafkaDefaultTopicOptions

**Namespace:** `Mocha.Transport.Kafka`

Default options for topics created by topology conventions.

#### Properties

| Name | Type | Summary |
|------|------|---------|
| `Partitions` | `int?` | Default number of partitions (null = 1). |
| `ReplicationFactor` | `short?` | Default replication factor (null = 1). |
| `TopicConfigs` | `Dictionary<string, string>?` | Default topic-level configs (e.g., "retention.ms", "cleanup.policy"). |

---

## 3. Receive Endpoint Configuration

### IKafkaReceiveEndpointDescriptor

**Namespace:** `Mocha.Transport.Kafka`  
**Inherits:** `IReceiveEndpointDescriptor<KafkaReceiveEndpointConfiguration>`

Fluent descriptor interface for configuring a Kafka receive endpoint.

#### Inherited Methods (returning IKafkaReceiveEndpointDescriptor)

| Signature | Summary |
|-----------|---------|
| `Handler<THandler>() where THandler : class, IHandler` | Register a specific handler type. |
| `Consumer<TConsumer>() where TConsumer : class, IConsumer` | Register a specific consumer type. |
| `Kind(ReceiveEndpointKind kind)` | Set the endpoint kind (Default, Error, Reply, Skipped). |
| `FaultEndpoint(string name)` | Specify where to route fault messages. |
| `SkippedEndpoint(string name)` | Specify where to route skipped messages. |
| `MaxConcurrency(int maxConcurrency)` | Set max concurrent message processing. |
| `UseReceive(ReceiveMiddlewareConfiguration configuration, string? before = null, string? after = null)` | Register receive-side middleware. |

#### Kafka-Specific Methods

| Signature | Summary |
|-----------|---------|
| `Topic(string name)` | Set the Kafka topic name this endpoint consumes from. |
| `ConsumerGroup(string groupId)` | Set the consumer group ID (defaults to endpoint name). |

---

### KafkaReceiveEndpointConfiguration

**Namespace:** `Mocha.Transport.Kafka`  
**Inherits:** `ReceiveEndpointConfiguration`

Configuration for a Kafka receive endpoint.

#### Properties

| Name | Type | Summary |
|------|------|---------|
| `TopicName` | `string?` | Kafka topic name this endpoint consumes from. |
| `ConsumerGroupId` | `string?` | Consumer group ID for this endpoint. |

---

## 4. Dispatch Endpoint Configuration

### IKafkaDispatchEndpointDescriptor

**Namespace:** `Mocha.Transport.Kafka`  
**Inherits:** `IDispatchEndpointDescriptor<KafkaDispatchEndpointConfiguration>`

Fluent descriptor interface for configuring a Kafka dispatch endpoint.

#### Kafka-Specific Methods

| Signature | Summary |
|-----------|---------|
| `ToTopic(string name)` | Set the target topic for message dispatch. |

#### Inherited Methods (returning IKafkaDispatchEndpointDescriptor)

| Signature | Summary |
|-----------|---------|
| `Send<TMessage>()` | Register this endpoint for send (direct) messages of type TMessage. |
| `Publish<TMessage>()` | Register this endpoint for publish (fanout) messages of type TMessage. |
| `UseDispatch(DispatchMiddlewareConfiguration configuration, string? before = null, string? after = null)` | Register dispatch-side middleware. |

---

### KafkaDispatchEndpointConfiguration

**Namespace:** `Mocha.Transport.Kafka`  
**Inherits:** `DispatchEndpointConfiguration`

Configuration for a Kafka dispatch endpoint.

#### Properties

| Name | Type | Summary |
|------|------|---------|
| `TopicName` | `string?` | Target topic name for message dispatch. |

---

## 5. Topic Configuration

### IKafkaTopicDescriptor

**Namespace:** `Mocha.Transport.Kafka`  
**Inherits:** `IMessagingDescriptor<KafkaTopicConfiguration>`

Fluent descriptor interface for configuring a Kafka topic.

#### Methods

| Signature | Summary |
|-----------|---------|
| `Partitions(int partitions)` | Set the number of partitions. |
| `ReplicationFactor(short replicationFactor)` | Set the replication factor. |
| `AutoProvision(bool autoProvision = true)` | Control whether this topic auto-provisions. |
| `WithConfig(string key, string value)` | Add a topic-level config entry (e.g., "retention.ms", "cleanup.policy"). |

---

### KafkaTopicConfiguration

**Namespace:** `Mocha.Transport.Kafka`  
**Inherits:** `TopologyConfiguration<KafkaMessagingTopology>`

Configuration for a Kafka topic.

#### Properties

| Name | Type | Summary |
|------|------|---------|
| `Name` | `string` | Topic name. |
| `Partitions` | `int?` | Number of partitions (null = bus default or 1). |
| `ReplicationFactor` | `short?` | Replication factor (null = bus default or 1). |
| `AutoProvision` | `bool?` | Whether to auto-provision (null = transport default). |
| `TopicConfigs` | `Dictionary<string, string>?` | Topic-level configs (retention.ms, cleanup.policy, etc.). |
| `IsTemporary` | `bool` | Indicates temporary topics (e.g., reply topics) with special retention. |

---

### KafkaTopic

**Namespace:** `Mocha.Transport.Kafka`  
**Inherits:** `TopologyResource`

Represents a Kafka topic entity in the transport topology.

#### Properties

| Name | Type | Access | Summary |
|------|------|--------|---------|
| `Name` | `string` | Public (read-only) | Topic name as declared in Kafka. |
| `Partitions` | `int` | Public (read-only) | Number of partitions (default: 1). |
| `ReplicationFactor` | `short` | Public (read-only) | Replication factor (default: 1). |
| `AutoProvision` | `bool?` | Public (read-only) | Whether to auto-provision (null = transport default). |
| `TopicConfigs` | `Dictionary<string, string>?` | Public (read-only) | Topic-level configuration entries. |
| `IsTemporary` | `bool` | Public (read-only) | Whether this is a temporary topic (e.g., reply topic). |
| `Topology` | `KafkaMessagingTopology` | Public (read-only) | The owning topology. |
| `Address` | `Uri` | Public (read-only) | Topology-relative URI (e.g., `kafka://host:9092/t/topic-name`). |

---

## 6. Transport & Topology

### KafkaMessagingTransport

**Namespace:** `Mocha.Transport.Kafka`  
**Inherits:** `MessagingTransport`

Kafka implementation of the messaging transport, managing connections, topology provisioning, and endpoint lifecycle.

#### Constructor

```csharp
public KafkaMessagingTransport(Action<IKafkaMessagingTransportDescriptor> configure)
```

#### Properties

| Name | Type | Access | Summary |
|------|------|--------|---------|
| `Topology` | `MessagingTopology` | Public (read-only, override) | Returns the Kafka messaging topology. |
| `ConnectionManager` | `KafkaConnectionManager` | Public (read-only) | Connection manager for producer, consumer, and admin clients. |

#### Key Methods

| Signature | Summary |
|-----------|---------|
| `protected override MessagingTransportConfiguration CreateConfiguration(IMessagingSetupContext context)` | Creates the final transport configuration. |
| `protected override ReceiveEndpoint CreateReceiveEndpoint()` | Factory method for creating receive endpoints. |
| `protected override DispatchEndpoint CreateDispatchEndpoint()` | Factory method for creating dispatch endpoints. |
| `DispatchEndpointConfiguration? CreateEndpointConfiguration(IMessagingConfigurationContext context, OutboundRoute route)` | Resolves dispatch endpoints from outbound routes. |
| `DispatchEndpointConfiguration? CreateEndpointConfiguration(IMessagingConfigurationContext context, Uri address)` | Resolves dispatch endpoints from URIs (supports `kafka://`, `kafka:///`, `topic://`, `topic:`). |
| `ReceiveEndpointConfiguration CreateEndpointConfiguration(IMessagingConfigurationContext context, InboundRoute route)` | Resolves receive endpoints from inbound routes. |
| `bool TryGetDispatchEndpoint(Uri address, out DispatchEndpoint? endpoint)` | Looks up a dispatch endpoint by address. |
| `public override async ValueTask DisposeAsync()` | Disposes the transport and cleans up all resources. |

#### Protected Lifecycle Methods

| Signature | Summary |
|-----------|---------|
| `protected override void OnAfterInitialized(IMessagingSetupContext context)` | Called after initialization; creates the topology and connection manager. |
| `protected override async ValueTask OnBeforeStartAsync(IMessagingConfigurationContext context, CancellationToken cancellationToken)` | Called before startup; creates the producer and provisions topology if enabled. |

---

### KafkaMessagingTopology

**Namespace:** `Mocha.Transport.Kafka`  
**Inherits:** `MessagingTopology<KafkaMessagingTransport>`

Manages the Kafka topology model (topics and consumer groups) for a transport instance.

#### Constructor

```csharp
public KafkaMessagingTopology(
    KafkaMessagingTransport transport,
    Uri baseAddress,
    KafkaBusDefaults defaults,
    bool autoProvision)
```

#### Properties

| Name | Type | Summary |
|------|------|---------|
| `AutoProvision` | `bool` | Whether topology resources auto-provision by default. |
| `Topics` | `IReadOnlyList<KafkaTopic>` | List of registered topics. |
| `Defaults` | `KafkaBusDefaults` | Bus-level defaults applied to auto-provisioned topics. |
| `Address` | `Uri` | Base URI for topology resources (inherited). |

#### Methods

| Signature | Summary |
|-----------|---------|
| `KafkaTopic AddTopic(KafkaTopicConfiguration configuration)` | Adds a new topic to the topology from configuration. |

---

## 7. Endpoints

### KafkaReceiveEndpoint

**Namespace:** `Mocha.Transport.Kafka`  
**Inherits:** `ReceiveEndpoint<KafkaReceiveEndpointConfiguration>`

Kafka receive endpoint that consumes messages from a specific topic using a dedicated consumer.

#### Constructor

```csharp
public KafkaReceiveEndpoint(KafkaMessagingTransport transport)
```

#### Properties

| Name | Type | Access | Summary |
|------|------|--------|---------|
| `Topic` | `KafkaTopic` | Public (read-only) | The Kafka topic this endpoint consumes from. |
| `ConsumerGroupId` | `string` | Public (read-only) | The consumer group ID for this endpoint. |

#### Protected Lifecycle Methods

| Signature | Summary |
|-----------|---------|
| `protected override void OnInitialize(IMessagingConfigurationContext context, KafkaReceiveEndpointConfiguration configuration)` | Initializes the endpoint; validates topic name. |
| `protected override void OnComplete(IMessagingConfigurationContext context, KafkaReceiveEndpointConfiguration configuration)` | Completes initialization; resolves topic from topology. |
| `protected override ValueTask OnStartAsync(IMessagingRuntimeContext context, CancellationToken cancellationToken)` | Starts the consumer loop. |
| `protected override async ValueTask OnStopAsync(IMessagingRuntimeContext context, CancellationToken cancellationToken)` | Stops the consumer loop and cleans up. |

---

### KafkaDispatchEndpoint

**Namespace:** `Mocha.Transport.Kafka`  
**Inherits:** `DispatchEndpoint<KafkaDispatchEndpointConfiguration>`

Kafka dispatch endpoint that publishes outbound messages to a target topic using the transport's shared producer.

#### Constructor

```csharp
public KafkaDispatchEndpoint(KafkaMessagingTransport transport)
```

#### Properties

| Name | Type | Access | Summary |
|------|------|--------|---------|
| `Topic` | `KafkaTopic?` | Public (read-only) | The target Kafka topic, or null if not yet resolved. |

#### Protected Lifecycle Methods

| Signature | Summary |
|-----------|---------|
| `protected override void OnInitialize(IMessagingConfigurationContext context, KafkaDispatchEndpointConfiguration configuration)` | Initializes the endpoint; validates topic name. |
| `protected override void OnComplete(IMessagingConfigurationContext context, KafkaDispatchEndpointConfiguration configuration)` | Completes initialization; resolves topic from topology. |
| `protected override async ValueTask DispatchAsync(IDispatchContext context)` | Dispatches a message to the target topic using the producer. |

---

## 8. Connection Management

### KafkaConnectionManager

**Namespace:** `Mocha.Transport.Kafka.Connection`  
**Implements:** `IAsyncDisposable`

Owns the lifecycle of the shared Kafka producer, per-endpoint consumers, and the admin client used for topology provisioning.

#### Constructor

```csharp
public KafkaConnectionManager(
    ILogger<KafkaConnectionManager> logger,
    string bootstrapServers,
    Action<ProducerConfig>? producerConfigOverrides,
    Action<ConsumerConfig>? consumerConfigOverrides)
```

#### Properties

| Name | Type | Summary |
|------|------|---------|
| `Producer` | `IProducer<byte[], byte[]>` | Gets the shared producer instance (throws if not created). |

#### Methods

| Signature | Summary |
|-----------|---------|
| `void EnsureProducerCreated()` | Creates the shared producer if it doesn't exist; uses double-checked locking. |
| `IConsumer<byte[], byte[]> CreateConsumer(string groupId, ILogger logger)` | Creates a new consumer for a specific consumer group. |
| `IAdminClient GetOrCreateAdminClient()` | Gets or creates the shared admin client for topology provisioning. |
| `async Task ProvisionTopologyAsync(IEnumerable<KafkaTopic> topics, CancellationToken cancellationToken)` | Provisions topics on the cluster; ignores "already exists" errors. |
| `void TrackInflight(TaskCompletionSource tcs)` | Tracks an in-flight dispatch for graceful shutdown. |
| `void UntrackInflight(TaskCompletionSource tcs)` | Untracks a completed/cancelled in-flight dispatch. |
| `async ValueTask DisposeAsync()` | Flushes pending messages, cancels in-flight dispatches, and disposes clients. |

#### Producer Configuration

Default producer settings:
- `BootstrapServers`: from constructor argument
- `LingerMs`: 5 (batch messages for up to 5ms)
- `BatchNumMessages`: 10000
- `Acks`: All (await replica confirmation)
- `EnableIdempotence`: true
- `EnableDeliveryReports`: true

Custom overrides applied after defaults.

#### Consumer Configuration

Default consumer settings:
- `BootstrapServers`: from constructor argument
- `GroupId`: from method argument
- `EnableAutoCommit`: false (explicit offset management)
- `AutoOffsetReset`: Earliest
- `EnablePartitionEof`: false
- `MaxPollIntervalMs`: 600,000 (10 minutes)

Custom overrides applied after defaults.

---

## 9. Conventions

### IKafkaReceiveEndpointConfigurationConvention

**Namespace:** `Mocha.Transport.Kafka`  
**Inherits:** `IEndpointConfigurationConvention<ReceiveEndpointConfiguration>`

Convention interface for applying Kafka-specific configuration to receive endpoints.

#### Methods

| Signature | Summary |
|-----------|---------|
| `void Configure(IMessagingConfigurationContext context, KafkaMessagingTransport transport, KafkaReceiveEndpointConfiguration configuration)` | Applies Kafka-specific configuration to a receive endpoint. |

---

### IKafkaReceiveEndpointTopologyConvention

**Namespace:** `Mocha.Transport.Kafka`  
**Inherits:** `IReceiveEndpointTopologyConvention<KafkaReceiveEndpoint, KafkaReceiveEndpointConfiguration>`

Convention interface for discovering and provisioning Kafka topology resources required by a receive endpoint.

---

### IKafkaDispatchEndpointTopologyConvention

**Namespace:** `Mocha.Transport.Kafka`  
**Inherits:** `IDispatchEndpointTopologyConvention<KafkaDispatchEndpoint, KafkaDispatchEndpointConfiguration>`

Convention interface for discovering and provisioning Kafka topology resources required by a dispatch endpoint.

---

### KafkaDefaultReceiveEndpointConvention

**Namespace:** `Mocha.Transport.Kafka`  
**Implements:** `IKafkaReceiveEndpointConfigurationConvention`

Default convention that assigns topic names, error endpoints, and skipped endpoints to receive endpoint configurations.

#### Behavior

- Sets `TopicName` to endpoint name if not already set
- Sets `ConsumerGroupId` to endpoint name if not already set
- For default-kind endpoints, creates error endpoint URI (`kafka:///t/{topicName}_error`)
- For default-kind endpoints, creates skipped endpoint URI (`kafka:///t/{topicName}_skipped`)

---

### KafkaReceiveEndpointTopologyConvention

**Namespace:** `Mocha.Transport.Kafka`  
**Implements:** `IKafkaReceiveEndpointTopologyConvention`

Convention that auto-provisions topics in the topology for receive endpoints.

#### Behavior

- Creates the main topic if it doesn't exist
- For reply-kind endpoints: sets retention to 1 hour and cleanup policy to "delete"
- For default-kind endpoints: creates an error topic (`{topicName}_error`)
- Discovers inbound routes and creates topics for each message type's publish/send endpoints

---

### KafkaDispatchEndpointTopologyConvention

**Namespace:** `Mocha.Transport.Kafka`  
**Implements:** `IKafkaDispatchEndpointTopologyConvention`

Convention that auto-provisions topics in the topology for dispatch endpoints.

#### Behavior

- Creates the target topic if it doesn't exist
- Creates error topics for non-error/non-skipped endpoints (avoids recursive `_error_error` topics)

---

## 10. Features

### KafkaReceiveFeature

**Namespace:** `Mocha.Transport.Kafka.Features`  
**Implements:** `IPooledFeature`

Pooled feature that carries the Kafka consume result and consumer reference through the receive middleware pipeline, enabling acknowledgement and message parsing middleware to access the raw delivery context.

#### Properties

| Name | Type | Summary |
|------|------|---------|
| `ConsumeResult` | `ConsumeResult<byte[], byte[]>` | Kafka consume result containing message body, headers, and offset metadata. |
| `Consumer` | `IConsumer<byte[], byte[]>` | The Kafka consumer that received the message (for manual offset commits). |
| `Topic` | `string` | Topic from which the message was consumed (read-only). |
| `Partition` | `int` | Partition from which the message was consumed (read-only). |
| `Offset` | `long` | Offset of the consumed message within the partition (read-only). |

#### Methods

| Signature | Summary |
|-----------|---------|
| `void Initialize(object state)` | Pool lifecycle method; resets to null. |
| `void Reset()` | Pool lifecycle method; resets to null. |

---

## 11. Message Parsing & Headers

### KafkaMessageEnvelopeParser

**Namespace:** `Mocha.Transport.Kafka`

Converts a raw Kafka `ConsumeResult` into a normalized `MessageEnvelope`, extracting standard message properties, custom headers, and the message body.

#### Methods

| Signature | Summary |
|-----------|---------|
| `MessageEnvelope Parse(ConsumeResult<byte[], byte[]> consumeResult)` | Converts a Kafka consume result into a MessageEnvelope by mapping Kafka headers to envelope fields. |

#### Well-Known Kafka Headers

| Header Key | Envelope Field | Type |
|------------|----------------|------|
| `mocha-message-id` | `MessageId` | string |
| `mocha-correlation-id` | `CorrelationId` | string |
| `mocha-conversation-id` | `ConversationId` | string |
| `mocha-causation-id` | `CausationId` | string |
| `mocha-source-address` | `SourceAddress` | string (URI) |
| `mocha-destination-address` | `DestinationAddress` | string (URI) |
| `mocha-response-address` | `ResponseAddress` | string (URI) |
| `mocha-fault-address` | `FaultAddress` | string (URI) |
| `mocha-content-type` | `ContentType` | string |
| `mocha-message-type` | `MessageType` | string |
| `mocha-sent-at` | `SentAt` | ISO 8601 DateTimeOffset |
| `mocha-enclosed-message-types` | `EnclosedMessageTypes` | comma-separated string array |

#### Singleton Access

```csharp
public static readonly KafkaMessageEnvelopeParser Instance
```

---

## 12. Consumer Groups

### KafkaConsumerGroup

**Namespace:** `Mocha.Transport.Kafka`  
**Inherits:** `TopologyResource`

Represents a Kafka consumer group, modeling the group identity as a topology resource for endpoint source and address resolution.

#### Properties

| Name | Type | Access | Summary |
|------|------|--------|---------|
| `GroupId` | `string` | Public (read-only) | Consumer group identifier. |
| `Topic` | `KafkaTopic` | Public (read-only) | Topic this consumer group subscribes to. |

#### Methods

| Signature | Summary |
|-----------|---------|
| `void Initialize(string groupId, KafkaTopic topic, KafkaMessagingTopology topology)` | Initializes the consumer group with group ID, topic, and topology. |

---

### KafkaConsumerGroupConfiguration

**Namespace:** `Mocha.Transport.Kafka`  
**Inherits:** `TopologyConfiguration<KafkaMessagingTopology>`

Configuration for a Kafka consumer group.

#### Properties

| Name | Type | Summary |
|------|------|---------|
| `GroupId` | `string` | Consumer group identifier. |
| `Topic` | `KafkaTopic?` | Topic this consumer group subscribes to. |

---

## 13. Middleware

### KafkaReceiveMiddlewares

**Namespace:** `Mocha.Transport.Kafka.Middlewares`

Provides pre-configured Kafka-specific receive middleware configurations.

#### Static Properties

| Name | Type | Summary |
|------|------|---------|
| `Commit` | `ReceiveMiddlewareConfiguration` | Commits Kafka offset after successful processing. |
| `Parsing` | `ReceiveMiddlewareConfiguration` | Parses raw Kafka consume result into MessageEnvelope. |

---

## 14. Address Schemes & URI Resolution

The Kafka transport supports multiple URI schemes for addressing topics and endpoints:

| Scheme | Format | Example | Use |
|--------|--------|---------|-----|
| `kafka` | `kafka://host:port/t/topic_name` | `kafka://localhost:9092/t/orders` | Fully qualified topic reference |
| `kafka:///` | `kafka:///t/topic_name` | `kafka:///t/orders` | Topology-relative topic reference |
| `kafka:///` | `kafka:///replies` | `kafka:///replies` | Reply endpoint (instance-specific topic) |
| `topic` | `topic://topic_name` | `topic://orders` | Shorthand topic reference |
| `topic` | `topic:topic_name` | `topic:orders` | Shorthand topic reference (colon form) |

---

## 15. Default Middleware Registration

When `AddKafka()` is called, the following conventions and middleware are automatically registered:

### Conventions

1. `KafkaDefaultReceiveEndpointConvention` — assigns default topic and consumer group names
2. `KafkaReceiveEndpointTopologyConvention` — auto-provisions receive-side topics
3. `KafkaDispatchEndpointTopologyConvention` — auto-provisions dispatch-side topics

### Middleware

1. **Receive side:**
   - `KafkaReceiveMiddlewares.Commit` — registered after concurrency limiter
   - `KafkaReceiveMiddlewares.Parsing` — registered after commit middleware

---

## 16. Serialization

Messages are serialized to binary format by the Mocha framework before dispatch. The Kafka transport handles:

- **Message Body:** Raw byte array
- **Message Key:** UTF-8 encoded correlation ID (or message ID if correlation ID not present)
- **Headers:** Kafka headers containing envelope metadata and custom headers

Messages are deserialized by the framework's messaging layer after the KafkaReceiveFeature is populated.

---

## 17. Auto-Provisioning & Topology

### Topic Auto-Provisioning

- Transport-level: `AutoProvision(bool)` on descriptor controls default behavior
- Topic-level: `AutoProvision(bool)` on topic descriptor overrides transport default
- Admin client: Created on first use; retained for topology provisioning

### Error & Skipped Topics

- **Error topics:** Created for each default-kind receive endpoint (`{endpoint_name}_error`)
- **Skipped topics:** Created for each default-kind receive endpoint (`{endpoint_name}_skipped`)
- **Reply topics:** Created for each reply endpoint with 1-hour retention and delete cleanup policy

### Default Topic Settings

Bus-level defaults via `ConfigureDefaults()`:
- `Partitions`: Number of partitions (default: 1)
- `ReplicationFactor`: Replication factor (default: 1)
- `TopicConfigs`: Custom topic-level configs (e.g., retention.ms, cleanup.policy)

---

## 18. Error Handling & Resilience

### Consumer Error Handling

- **Transient errors:** Logged at error level; consumer loop continues
- **Partition assignment:** Logged at info level
- **Partition revocation:** Logged at info level; no action needed (sequential processing)
- **Processing errors:** Logged at critical level; can be routed to error endpoint

### Producer Reliability

- **Idempotent producer:** Enabled by default (prevent duplicate delivery)
- **All replicas:** Await acknowledgement from all in-sync replicas
- **In-flight tracking:** Dispatch operations tracked for graceful shutdown

### Graceful Shutdown

- Producer flush: 10-second timeout for pending messages
- In-flight dispatch cancellation: Remaining TCS instances cancelled after flush
- Consumer close: Explicit close on shutdown; no auto-commit

---

## 19. Logging

The transport logs through `ILogger<T>` for each component:

| Logger | Level | Events |
|--------|-------|--------|
| `KafkaConnectionManager` | Debug/Error | Producer/consumer lifecycle |
| `KafkaReceiveEndpoint` | Error/Critical | Consumer errors, processing failures |
| `KafkaMessagingTransport` | (inherited) | Transport lifecycle |

---

## 20. Memory & Performance

### Zero-Allocation Optimizations

- **Byte array re-use:** Avoids `ToArray()` when envelope body is already backed by byte[]
- **Stack-allocated span:** Partition selection uses stack-allocated ranges
- **Pooled features:** `KafkaReceiveFeature` implements `IPooledFeature` for allocation pooling

### Concurrency Model

- Producer: Shared across all dispatch endpoints; thread-safe
- Consumers: One per receive endpoint; not shared
- Topology: Thread-safe mutation with lock
- In-flight dispatch tracking: ConcurrentDictionary (lock-free)

---

## Summary Table: Top-Level Entry Points

| API Element | Location | Purpose |
|-------------|----------|---------|
| `AddKafka()` | `MessageBusBuilderExtensions` | Register Kafka transport |
| `IKafkaMessagingTransportDescriptor` | Transport configuration | Configure Kafka connection, endpoints, topics |
| `Endpoint()` | Transport descriptor | Create receive endpoint |
| `DispatchEndpoint()` | Transport descriptor | Create dispatch endpoint |
| `DeclareTopic()` | Transport descriptor | Explicitly declare topic |
| `BootstrapServers()` | Transport descriptor | **Required:** Set Kafka servers |
| `KafkaConnectionManager` | Connection management | Manage producer/consumer lifecycle |
| `KafkaMessageEnvelopeParser.Instance` | Message parsing | Parse Kafka messages to envelopes |
| `KafkaReceiveFeature` | Feature context | Access raw Kafka consume result |

