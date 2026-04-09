# Mocha Transport Patterns Scout Report

## Overview

The Mocha messaging framework follows a well-established pattern for transport implementations. This report documents the key abstractions, base types, and patterns that all transports must implement.

---

## 1. Base Transport Architecture

### MessagingTransport (Abstract Base)
**File**: `/workspaces/hc2/src/Mocha/src/Mocha/Transport/MessagingTransport.cs`

The root abstraction for all transports. Key responsibilities:
- Manages lifecycle of receive and dispatch endpoints
- Provides topology and naming conventions
- Coordinates startup/shutdown across all endpoints

**Key Members**:
- `Name` (string): Logical transport name
- `Schema` (string): URI scheme (e.g., "rabbitmq", "azure-eventhub")
- `Options` (IReadOnlyTransportOptions): Concurrency limits, prefetch settings
- `ReceiveEndpoints` (IReadOnlySet<ReceiveEndpoint>): All inbound endpoints
- `DispatchEndpoints` (IReadOnlySet<DispatchEndpoint>): All outbound endpoints
- `Topology` (MessagingTopology): Abstract topology object
- `Naming` (IBusNamingConventions): Endpoint naming conventions
- `Configuration` (MessagingTransportConfiguration): Endpoint definitions and middleware

**Lifecycle Methods** (lines 179-235):
- `StartAsync()` (line 179): Calls `OnBeforeStartAsync()`, then starts all receive endpoints
- `StopAsync()` (line 203): Calls `OnBeforeStopAsync()`, then stops all receive endpoints
- `OnBeforeStartAsync()` (line 225, virtual): Hook for transport-specific pre-start logic (connection setup, topology declaration)
- `OnBeforeStopAsync()` (line 235, virtual): Hook for transport-specific pre-stop logic (cleanup, resource release)

**Abstract Methods** (lines 339-381):
- `CreateConfiguration(IMessagingSetupContext)` (line 242): Build transport configuration from setup context
- `CreateEndpointConfiguration()` overloads (lines 339-369): Create endpoint configs from routes/addresses
- `CreateReceiveEndpoint()` (line 375): Factory to instantiate receive endpoints
- `CreateDispatchEndpoint()` (line 381): Factory to instantiate dispatch endpoints
- `TryGetDispatchEndpoint(Uri, out DispatchEndpoint)` (line 166): Resolve existing dispatch endpoint by address

**Endpoint Management** (lines 252-326):
- `ConnectRoute(context, OutboundRoute)` (line 252): Connect route → create or reuse dispatch endpoint
- `ConnectRoute(context, InboundRoute)` (line 275): Connect route → create or reuse receive endpoint
- `AddEndpoint(context, DispatchEndpointConfiguration)` (line 296): Create and register dispatch endpoint
- `AddEndpoint(context, ReceiveEndpointConfiguration)` (line 315): Create and register receive endpoint

### MessagingTopology (Abstract Base)
**File**: `/workspaces/hc2/src/Mocha/src/Mocha/Topology/MessagingTopology.cs`

Minimal base class for transport-specific topologies.

**Members**:
- `Address` (Uri): Base address URI (readonly property)
- `Transport` (MessagingTransport): Owner transport (protected)

**Example**: EventHubMessagingTopology (line 7-12 in EventHubMessagingTopology.cs):
```csharp
public sealed class EventHubMessagingTopology(
    EventHubMessagingTransport transport,
    Uri baseAddress,
    EventHubBusDefaults defaults,
    bool autoProvision)
    : MessagingTopology<EventHubMessagingTransport>(transport, baseAddress)
```

---

## 2. Endpoint Base Classes

### ReceiveEndpoint (Abstract Base)
**File**: `/workspaces/hc2/src/Mocha/src/Mocha/Endpoints/ReceiveEndpoint.cs`

Consumes messages from a transport source through a compiled middleware pipeline.

**Constructor** (line 22):
```csharp
public abstract class ReceiveEndpoint(MessagingTransport transport) : IReceiveEndpoint, IFeatureProvider
```

**Key Properties**:
- `Transport` (MessagingTransport, line 36): Owner transport
- `IsInitialized` (bool, line 41): After `Initialize()`
- `IsCompleted` (bool, line 49): After `Complete()` and pipeline compiled
- `IsStarted` (bool, line 54): After `StartAsync()`
- `Name` (string, line 59): Unique endpoint identifier
- `Source` (TopologyResource, line 64): Physical resource this endpoint consumes from
- `Address` (Uri, line 69): Transport-specific address (auto-built if not set)
- `Kind` (ReceiveEndpointKind, line 74): Default/Error/Skipped/Reply
- `ErrorEndpoint` (DispatchEndpoint?, line 83): Forward faulted messages here (if configured)
- `SkippedEndpoint` (DispatchEndpoint?, line 92): Forward unrecognized messages here (if configured)
- `Features` (IFeatureCollection, line 97): Transport-extensibility features
- `Configuration` (ReceiveEndpointConfiguration, line 31, protected): Applied configuration

**Lifecycle Methods**:
1. `Initialize(context, configuration)` (line 163):
   - Apply conventions to configuration
   - Store configuration and endpoint name
   - **Call `OnInitialize(context, configuration)`** (line 173, abstract)
   - Mark initialized

2. `DiscoverTopology(context)` (line 191):
   - Run convention-based topology discovery
   - Resolves `Source` resource

3. `Complete(context)` (line 207):
   - **Call `OnComplete(context, configuration)`** (line 209, virtual, empty by default)
   - Set default address if not already set (line 211)
   - Resolve error/skipped endpoints (lines 213-220)
   - Compile receive middleware pipeline (lines 223-232)
   - Mark completed

4. `StartAsync(context, cancellationToken)` (line 257):
   - Resolve runtime services (logger, pool, service provider)
   - **Call `OnStartAsync(context, cancellationToken)`** (line 277, abstract)
   - Mark started

5. `StopAsync(context, cancellationToken)` (line 292):
   - **Call `OnStopAsync(context, cancellationToken)`** (line 299, abstract)
   - Clear runtime state
   - Mark stopped

**Abstract Methods**:
- `OnInitialize(IMessagingConfigurationContext, ReceiveEndpointConfiguration)` (line 183): Transport-specific init
- `OnStartAsync(IMessagingRuntimeContext, CancellationToken)` (line 311): Open connections, register consumers
- `OnStopAsync(IMessagingRuntimeContext, CancellationToken)` (line 319): Close connections, clean up

**Message Execution**:
- `ExecuteAsync<TState>(Action<ReceiveContext, TState> configure, TState state, CancellationToken)` (line 115):
  - Allocate scoped service provider
  - Get pooled ReceiveContext from pool
  - Call `configure()` to populate with transport-specific message data
  - Execute compiled pipeline
  - Handle exceptions at critical level
  - Return context to pool

**Virtual Method**:
- `OnComplete(context, configuration)` (line 242, virtual, empty): Optional completion hook before pipeline compilation

### DispatchEndpoint (Abstract Base)
**File**: `/workspaces/hc2/src/Mocha/src/Mocha/Endpoints/DispatchEndpoint.cs`

Sends messages through a compiled middleware pipeline to a transport destination.

**Constructor** (line 64):
```csharp
protected DispatchEndpoint(MessagingTransport transport)
```
- Sets up deferred pipeline delegate (awaits TaskCompletionSource)
- Allows dispatch calls before pipeline is compiled

**Key Properties**:
- `Transport` (MessagingTransport, line 78): Owner transport
- `IsInitialized` (bool, line 88): After `Initialize()`
- `IsCompleted` (bool, line 94): After `Complete()` and pipeline compiled
- `Name` (string, line 83): Unique endpoint identifier
- `Destination` (TopologyResource, line 99): Physical resource this endpoint sends to
- `Kind` (DispatchEndpointKind, line 104): Default/Reply
- `Address` (Uri, line 109): Transport-specific address (auto-built if not set)
- `Configuration` (DispatchEndpointConfiguration, line 114, protected): Applied configuration

**Lifecycle Methods**:
1. `Initialize(context, configuration)` (line 127):
   - Apply conventions to configuration
   - Store configuration and endpoint name
   - **Call `OnInitialize(context, configuration)`** (line 136, abstract)
   - Mark initialized

2. `DiscoverTopology(context)` (line 166):
   - Run convention-based topology discovery
   - Register endpoint in context.Endpoints (line 169)

3. `Complete(context)` (line 185):
   - **Call `OnComplete(context, configuration)`** (line 187, abstract)
   - Compile dispatch middleware pipeline (lines 189-198)
   - Atomically install compiled pipeline via volatile write (line 200)
   - Signal TaskCompletionSource to unblock deferred calls (line 203)
   - Clear configuration (line 202)
   - Set default address if not already set (line 206)
   - Register endpoint in context.Endpoints (line 208)

**Abstract Methods**:
- `OnInitialize(IMessagingConfigurationContext, DispatchEndpointConfiguration)` (line 146): Transport-specific init
- `OnComplete(IMessagingConfigurationContext, DispatchEndpointConfiguration)` (line 217): Transport-specific completion
- `DispatchAsync(IDispatchContext)` (line 244): Terminal pipeline delegate — actual send to transport

**Message Dispatch**:
- `ExecuteAsync(IDispatchContext context)` (line 159):
  - If not yet completed, awaits TaskCompletionSource and defers to installed pipeline
  - Once completed, executes compiled pipeline directly

---

## 3. Endpoint Configuration Types

### ReceiveEndpointConfiguration
**File**: Located in Mocha.Endpoints.Configurations

**Key Members**:
- `Name` (string): Endpoint identifier
- `Kind` (ReceiveEndpointKind): Default/Error/Skipped/Reply
- `ConsumerIdentities` (List<Type>): Registered handler/consumer types
- `MaxConcurrency` (int?): Concurrency limit
- `ErrorEndpoint` (Uri?): Address for faulted messages
- `SkippedEndpoint` (Uri?): Address for unrecognized messages
- `ReceiveMiddlewares` (List<ReceiveMiddlewareConfiguration>): Endpoint-level middleware
- `ReceivePipelineModifiers` (OrderedModifiers): Middleware positioning directives
- `Features` (IFeatureCollection): Extensibility features
- `Defaults` static class: MaxConcurrency, etc.

### DispatchEndpointConfiguration
**File**: Located in Mocha.Endpoints.Configurations

**Key Members**:
- `Name` (string): Endpoint identifier
- `Kind` (DispatchEndpointKind): Default/Reply
- `Routes` (List<(Type MessageType, OutboundRouteKind Kind)>): Configured message routes
- `DispatchMiddlewares` (List<DispatchMiddlewareConfiguration>): Endpoint-level middleware
- `DispatchPipelineModifiers` (OrderedModifiers): Middleware positioning directives

---

## 4. Descriptor Pattern

### Base Descriptor Classes

**ReceiveEndpointDescriptor<T>**
**File**: `/workspaces/hc2/src/Mocha/src/Mocha/Endpoints/Descriptors/ReceiveEndpointDescriptor.cs`

Fluent configuration builder for receive endpoints (lines 1-76).

**Key Methods**:
- `Handler<THandler>()` (line 13): Register handler type
- `Consumer<TConsumer>()` (line 19): Register consumer type
- `Kind(ReceiveEndpointKind)` (line 25): Set endpoint kind
- `MaxConcurrency(int)` (line 31): Set concurrency limit
- `FaultEndpoint(string address)` (line 37): Set error endpoint address
- `SkippedEndpoint(string address)` (line 43): Set skipped endpoint address
- `UseReceive(ReceiveMiddlewareConfiguration, before?, after?)` (line 49): Add receive middleware with positioning

**DispatchEndpointDescriptor<T>**
**File**: `/workspaces/hc2/src/Mocha/src/Mocha/Endpoints/Descriptors/DispatchEndpointDescriptor.cs`

Fluent configuration builder for dispatch endpoints (lines 1-52).

**Key Methods**:
- `Send<TMessage>()` (line 13): Register send route
- `Publish<TMessage>()` (line 19): Register publish route
- `UseDispatch(DispatchMiddlewareConfiguration, before?, after?)` (line 25): Add dispatch middleware with positioning

**MessagingTransportDescriptor<T>**
**File**: `/workspaces/hc2/src/Mocha/src/Mocha/Transport/MessagingTransportDescriptor.cs`

Fluent configuration builder for transports (lines 95-120).

**Key Methods**:
- `ModifyOptions(Action<TransportOptions>)` (line 106): Configure transport-level options
- `BindHandlersImplicitly()` (line 113): Convention-based consumer binding
- `BindHandlersExplicitly()`: Explicit consumer binding (from IMessagingTransportDescriptor)
- `Schema(string)`: Set URI scheme
- `Name(string)`: Set logical name
- `AddConvention(IConvention)`: Register convention
- `IsDefaultTransport()`: Mark as default
- `UseDispatch(DispatchMiddlewareConfiguration, before?, after?)`: Add transport-level dispatch middleware
- `UseReceive(ReceiveMiddlewareConfiguration, before?, after?)`: Add transport-level receive middleware

### Event Hub Descriptors

**EventHubReceiveEndpointDescriptor**
**File**: `/workspaces/hc2/src/Mocha/src/Mocha.Transport.AzureEventHub/Descriptors/EventHubReceiveEndpointDescriptor.cs`

Extends ReceiveEndpointDescriptor with Event Hub-specific configuration.

**Key Methods** (lines 45-63):
- `Hub(string name)` (line 45): Set Event Hub name
- `ConsumerGroup(string consumerGroup)` (line 52): Set consumer group
- `CheckpointInterval(int interval)` (line 59): Set checkpoint interval (events between checkpoints)

**IEventHubReceiveEndpointDescriptor Interface**
**File**: `/workspaces/hc2/src/Mocha/src/Mocha.Transport.AzureEventHub/Descriptors/IEventHubReceiveEndpointDescriptor.cs`

Fluent interface extending IReceiveEndpointDescriptor with Event Hub-specific methods (lines 1-54).

**EventHubDispatchEndpointDescriptor**
**File**: `/workspaces/hc2/src/Mocha/src/Mocha.Transport.AzureEventHub/Descriptors/EventHubDispatchEndpointDescriptor.cs`

Extends DispatchEndpointDescriptor with Event Hub-specific configuration.

**Key Methods** (lines 16-34):
- `ToHub(string name)` (line 16): Set target hub name
- `PartitionId(string partitionId)` (line 23): Set target partition (optional)
- `BatchMode(EventHubBatchMode mode)` (line 30): Set batching behavior

**EventHubMessagingTransportDescriptor**
**File**: `/workspaces/hc2/src/Mocha/src/Mocha.Transport.AzureEventHub/Descriptors/EventHubMessagingTransportDescriptor.cs`

Extends MessagingTransportDescriptor with Event Hub-specific configuration (lines 1-25).

**Key Methods** (lines 97-143):
- `ConnectionString(string connectionString)` (line 97): Set connection string
- `Namespace(string fullyQualifiedNamespace)` (line 104): Set fully qualified namespace
- `ConnectionProvider(Func<IServiceProvider, IEventHubConnectionProvider>)` (line 111): Custom connection provider
- `AutoProvision(bool autoProvision)` (line 119): Enable/disable auto-provisioning of hubs
- `ConfigureDefaults(Action<EventHubBusDefaults>)` (line 126): Configure bus-level defaults
- `ResourceGroup(subscriptionId, resourceGroupName, namespaceName)` (line 133): Set ARM resource group for provisioning

---

## 5. Transport Implementation Pattern

### RabbitMQ Transport (Reference Implementation)

**RabbitMQReceiveEndpoint**
**File**: `/workspaces/hc2/src/Mocha/src/Mocha.Transport.RabbitMQ/RabbitMQReceiveEndpoint.cs`

Implementation pattern (lines 1-95):
- Constructor takes transport (line 10)
- `OnInitialize()` (line 21): Extract queue name and concurrency from config
- `OnComplete()` (line 38): Resolve RabbitMQ queue from topology, set `Source = Queue`
- `OnStartAsync()` (line 58): Register consumer with ConsumerManager, passing message handler callback

**RabbitMQDispatchEndpoint**
**File**: `/workspaces/hc2/src/Mocha/src/Mocha.Transport.RabbitMQ/RabbitMQDispatchEndpoint.cs`

Implementation pattern (lines 1-44):
- Properties for target queue/exchange (lines 17-23)
- `DispatchAsync()` (line 25): Core implementation
  - Rent channel from pool (line 34)
  - Ensure topology provisioned (line 37)
  - Send via channel (line 38)
  - Return channel to pool (line 42)
- Caches provisioning state with `_isProvisioned` flag (line 140)

### Event Hub Transport (Current Implementation)

**EventHubReceiveEndpoint**
**File**: `/workspaces/hc2/src/Mocha/src/Mocha.Transport.AzureEventHub/EventHubReceiveEndpoint.cs`

Implementation pattern (lines 1-145):
- Constructor takes transport (line 13)
- `OnInitialize()` (line 37): Extract hub name and consumer group from config
- `OnComplete()` (line 51): Resolve EventHubTopic from topology, set `Source = Topic`
- `OnStartAsync()` (line 64): Create MochaEventProcessor with message handler, start processing

**EventHubDispatchEndpoint**
**File**: `/workspaces/hc2/src/Mocha/src/Mocha.Transport.AzureEventHub/EventHubDispatchEndpoint.cs`

Implementation pattern (lines 1-150+):
- Property for target topic (line 25)
- `DispatchAsync()` (line 28): Core implementation
  - Resolve target hub name (lines 37-65)
  - Get or create producer (line 67)
  - Validate message size (lines 73-78)
  - Build EventData from envelope (line 82)
  - Map envelope to AMQP properties (lines 84-150)
  - Dispatch via producer (end of method, not shown in excerpt)
- Uses EventData constructor for zero-copy body handling (line 82)
- Maps envelope fields to AMQP structured properties (lines 88-111)
- Overflow headers go to ApplicationProperties dictionary (line 115)

---

## 6. Message Envelope

### MessageEnvelope
**File**: `/workspaces/hc2/src/Mocha/src/Mocha/Transport/MessageEnvelope.cs`

Immutable wire-format wrapper for messages (lines 1-120).

**Key Properties**:
- `MessageId` (string?): Unique message identifier
- `CorrelationId` (string?): Correlate related messages
- `ConversationId` (string?): Larger conversation flow
- `CausationId` (string?): Parent message that triggered this
- `SourceAddress` (string?): Originating endpoint address
- `DestinationAddress` (string?): Target endpoint address
- `ResponseAddress` (string?): Reply-to address for request/response
- `FaultAddress` (string?): Error queue address (future use)
- `ContentType` (string?): MIME type (e.g., "application/json")
- `MessageType` (string?): URN of message type
- `SentAt` (DateTimeOffset?): UTC timestamp of creation
- `DeliverBy` (DateTimeOffset?): TTL expiration time
- `DeliveryCount` (int?): Retry counter
- `Headers` (IHeaders?): Custom headers
- `Body` (ReadOnlyMemory<byte>): Serialized message
- `EnclosedMessageTypes` (string[]?): Contained message type URNs
- `Host` (string?): Originating host identifier

**Copy Constructor** (line 25): Deep copy of envelope with headers cloned, body shared

---

## 7. Feature System

### IFeatureCollection & IFeatureProvider
**File**: `/workspaces/hc2/src/Mocha/src/Mocha.Abstractions/IFeatureCollection.cs`

Generic extensibility mechanism (lines 1-57).

**Key Members**:
- `IsReadOnly` (bool): Collection mutability
- `IsEmpty` (bool): Collection size
- `Revision` (int): Incremented per modification (cache verification)
- `this[Type key]` (getter/setter): Get/set by type key
- `Get<TFeature>()`: Retrieve feature or null
- `TryGet<TFeature>(out feature)`: Try retrieve with null check
- `Set<TFeature>(instance)`: Store feature (null removes it)

**IFeatureProvider Interface** (lines 6-12):
- `Features` property: Access to IFeatureCollection

**Transport Integration**:
- ReceiveEndpoint exposes `Features` (line 97 in ReceiveEndpoint.cs)
- DispatchEndpoint does not directly expose features (configured via Configuration.Features)
- MessagingTransport exposes `Features` (line 63 in MessagingTransport.cs)

**Event Hub Feature Classes**:
- `EventHubReceiveFeature`: Holds EventData, PartitionId, SequenceNumber, EnqueuedTime (lines 84-86 in EventHubReceiveEndpoint.cs)
- Similar patterns for RabbitMQ, Postgres

---

## 8. Topology and Resources

### Topology Resource Concept

TopologyResource is the base abstraction for physical resources (queues, exchanges, topics, subscriptions).

**EventHubTopic** (implicit from EventHubReceiveEndpoint line 21):
- Name property
- Represents an Event Hub entity

**EventHubSubscription** (implicit from EventHubReceiveEndpoint line 26):
- Consumer group association
- Checkpoint state

### EventHubMessagingTopology
**File**: `/workspaces/hc2/src/Mocha/src/Mocha.Transport.AzureEventHub/Topology/EventHubMessagingTopology.cs`

Topology management (lines 1-85).

**Constructor** (lines 7-11):
```csharp
public sealed class EventHubMessagingTopology(
    EventHubMessagingTransport transport,
    Uri baseAddress,
    EventHubBusDefaults defaults,
    bool autoProvision)
```

**Key Members**:
- `AutoProvision` (bool): Auto-create resources (line 26)
- `Topics` (IReadOnlyList<EventHubTopic>): Registered topics (line 31)
- `Subscriptions` (IReadOnlyList<EventHubSubscription>): Registered subscriptions (line 36)
- `Defaults` (EventHubBusDefaults): Bus-level defaults (line 41)

**Key Methods**:
- `AddTopic(EventHubTopicConfiguration)` (line 49): Create and register topic
- `AddSubscription(EventHubSubscriptionConfiguration)` (line 75): Create and register subscription

---

## 9. Key Patterns Summary

### Lifecycle Pattern

All endpoints follow: **Initialize → DiscoverTopology → Complete → StartAsync → StopAsync**

1. **Initialize Phase**: Configure from descriptor, apply conventions, call `OnInitialize()`
2. **DiscoverTopology Phase**: Run topology discovery conventions
3. **Complete Phase**: Resolve topology resources, compile pipelines, call `OnComplete()`, ready to execute
4. **StartAsync Phase**: Resolve runtime services, open connections, call `OnStartAsync()`
5. **StopAsync Phase**: Close connections, call `OnStopAsync()`

### Transport Factory Methods

Transport must implement:
```csharp
protected abstract ReceiveEndpoint CreateReceiveEndpoint();
protected abstract DispatchEndpoint CreateDispatchEndpoint();
protected abstract MessagingTransportConfiguration CreateConfiguration(IMessagingSetupContext);
protected abstract DispatchEndpointConfiguration? CreateEndpointConfiguration(context, OutboundRoute);
protected abstract DispatchEndpointConfiguration? CreateEndpointConfiguration(context, Uri);
protected abstract ReceiveEndpointConfiguration? CreateEndpointConfiguration(context, InboundRoute);
public abstract bool TryGetDispatchEndpoint(Uri address, out DispatchEndpoint? endpoint);
```

### Deferred Dispatch Pattern

DispatchEndpoint uses TaskCompletionSource to defer dispatch calls until pipeline is compiled:
- Constructor sets up deferred pipeline (lines 67-71 in DispatchEndpoint.cs)
- Early callers await `_completed.Task` (line 69)
- `Complete()` signals completion (line 203) and installs real pipeline via volatile write (line 200)
- No blocking of early callers — they asynchronously wait for completion

### Topology Provisioning Pattern

RabbitMQ caches provisioning state:
- `_isProvisioned` flag (line 140 in RabbitMQDispatchEndpoint.cs)
- `EnsureProvisionedAsync()` checks flag, provisions once, sets flag

Event Hub follows similar pattern but deferred to connection manager.

### Zero-Copy Message Handling

Event Hub dispatch uses ReadOnlyMemory<byte> (line 82 in EventHubDispatchEndpoint.cs):
- Envelope.Body is ReadOnlyMemory<byte>
- EventData constructor accepts it directly (no copy)
- AMQP properties use structured types (AmqpMessageId, AmqpAddress) to avoid allocations

---

## 10. Configuration Context Interfaces

### IMessagingConfigurationContext

Provides access during configuration phase:
- Services (IServiceProvider): DI container
- Endpoints: Endpoint collection for lookups
- Conventions registry

### IMessagingRuntimeContext

Provides access during runtime:
- Services (IServiceProvider): DI container with runtime services
- Logger, pool, service provider accessor, lazy runtime

---

## Implementation Checklist for New Transport

1. **Create Transport Class**:
   - Extend MessagingTransport
   - Implement CreateConfiguration(), CreateEndpointConfiguration() (3 overloads), CreateReceiveEndpoint(), CreateDispatchEndpoint(), TryGetDispatchEndpoint()
   - Implement abstract Topology property
   - Optional: Override OnBeforeStartAsync(), OnBeforeStopAsync()

2. **Create Receive Endpoint**:
   - Extend ReceiveEndpoint<TransportConfiguration>
   - Implement OnInitialize(), OnComplete(), OnStartAsync(), OnStopAsync()
   - In OnComplete(): Set Source property to topology resource
   - In OnStartAsync(): Register message handler and call ExecuteAsync()

3. **Create Dispatch Endpoint**:
   - Extend DispatchEndpoint<TransportConfiguration>
   - Implement OnInitialize(), OnComplete(), DispatchAsync()
   - Implement DispatchAsync() as terminal pipeline delegate

4. **Create Topology**:
   - Extend MessagingTopology or MessagingTopology<T>
   - Manage transport-specific resources (queues, exchanges, etc.)

5. **Create Configuration Types**:
   - Extend ReceiveEndpointConfiguration
   - Extend DispatchEndpointConfiguration
   - Extend MessagingTransportConfiguration

6. **Create Descriptors**:
   - EventHubReceiveEndpointDescriptor extends ReceiveEndpointDescriptor<Config>
   - EventHubDispatchEndpointDescriptor extends DispatchEndpointDescriptor<Config>
   - EventHubMessagingTransportDescriptor extends MessagingTransportDescriptor<Config>
   - Implement IEventHub*Descriptor interfaces

7. **Create Feature Classes**:
   - For transport-specific data on endpoints (EventHubReceiveFeature, etc.)
   - Accessible via context.Features.GetOrSet<T>()

8. **Implement Conventions**:
   - IConvention for naming/topology discovery
   - Applied during Initialize and DiscoverTopology phases
