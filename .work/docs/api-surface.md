# Mocha Azure Event Hub Transport - Public API Surface

## Overview

The Azure Event Hub Transport (`Mocha.Transport.AzureEventHub`) is a messaging transport implementation for the Mocha message bus framework. It enables applications to use Azure Event Hubs as the underlying messaging infrastructure, supporting both publish/subscribe and request/reply patterns.

**Target Framework:** .NET 10.0

---

## 1. Entry Point: MessageBusBuilderExtensions

The primary entry point for configuring the Event Hub transport.

**File:** `MessageBusBuilderExtensions.cs`

### Methods

#### `AddEventHub(IMessageBusHostBuilder busBuilder, Action<IEventHubMessagingTransportDescriptor> configure)`
```csharp
public static IMessageBusHostBuilder AddEventHub(
    this IMessageBusHostBuilder busBuilder,
    Action<IEventHubMessagingTransportDescriptor> configure)
```
- Adds an Azure Event Hub messaging transport to the message bus with custom configuration
- Applies the configuration delegate after default conventions and middleware have been registered
- Returns the builder for method chaining

#### `AddEventHub(IMessageBusHostBuilder busBuilder)` (Overload)
```csharp
public static IMessageBusHostBuilder AddEventHub(this IMessageBusHostBuilder busBuilder)
```
- Overload with default configuration (no custom setup required)
- Equivalent to calling `AddEventHub(_ => { })`
- Returns the builder for method chaining

---

## 2. Transport Configuration

### EventHubTransportConfiguration

**File:** `Configurations/EventHubTransportConfiguration.cs`

**Inheritance:** Extends `MessagingTransportConfiguration`

**Default Constants:**
- `DefaultName = "eventhub"`
- `DefaultSchema = "eventhub"`

**Properties:**

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ConnectionProvider` | `Func<IServiceProvider, IEventHubConnectionProvider>?` | `null` | Factory delegate for custom connection provider. When `null`, falls back to `ConnectionString` or `FullyQualifiedNamespace` |
| `ConnectionString` | `string?` | `null` | Event Hub namespace connection string. Mutually exclusive with `FullyQualifiedNamespace`. Creates `ConnectionStringEventHubConnectionProvider` automatically |
| `FullyQualifiedNamespace` | `string?` | `null` | Fully qualified namespace (e.g., "mynamespace.servicebus.windows.net"). Mutually exclusive with `ConnectionString`. Creates `CredentialEventHubConnectionProvider` with `DefaultAzureCredential` |
| `Topics` | `List<EventHubTopicConfiguration>` | `[]` | Explicitly declared Event Hub entities |
| `Subscriptions` | `List<EventHubSubscriptionConfiguration>` | `[]` | Explicitly declared consumer groups |
| `AutoProvision` | `bool?` | `true` (defaults if `null`) | Whether to automatically provision topology resources (hubs, consumer groups). Individual resources can override |
| `Defaults` | `EventHubBusDefaults` | new instance | Bus-level defaults applied to all auto-provisioned topics and subscriptions |
| `CheckpointStoreFactory` | `Func<IServiceProvider, ICheckpointStore>?` | `null` | Factory for checkpoint store. If `null`, uses in-memory checkpoint store |
| `OwnershipStoreFactory` | `Func<IServiceProvider, IPartitionOwnershipStore>?` | `null` | Factory for ownership store. If `null`, uses single-instance mode where local processor claims all partitions |
| `SubscriptionId` | `string?` | `null` | Azure subscription ID (required for ARM-based auto-provisioning) |
| `ResourceGroupName` | `string?` | `null` | Resource group name containing the Event Hubs namespace (required for auto-provisioning) |
| `NamespaceName` | `string?` | `null` | Event Hubs namespace name (required for auto-provisioning) |
| `ReplyHubName` | `string` | `"replies"` | Hub name used for request/reply patterns. Since Event Hubs don't support dynamic creation, a shared hub is used |

---

## 3. Fluent Configuration Interface

### IEventHubMessagingTransportDescriptor

**File:** `Descriptors/IEventHubMessagingTransportDescriptor.cs`

**Inheritance:** `IMessagingTransportDescriptor`, `IMessagingDescriptor<EventHubTransportConfiguration>`

**Base Transport Methods (inherited, now fluent-returning):**
- `ModifyOptions(Action<TransportOptions> configure)` - Configure transport-level options
- `Schema(string schema)` - Set URI schema for addressing
- `BindHandlersImplicitly()` - Use implicit handler binding
- `BindHandlersExplicitly()` - Use explicit handler binding
- `Name(string name)` - Set transport name
- `AddConvention(IConvention convention)` - Add topology convention
- `IsDefaultTransport()` - Mark as default transport
- `UseDispatch(DispatchMiddlewareConfiguration, string? before, string? after)` - Configure dispatch pipeline
- `UseReceive(ReceiveMiddlewareConfiguration, string? before, string? after)` - Configure receive pipeline

**Connection Methods:**

#### `ConnectionString(string connectionString)`
Sets the connection string for the Event Hub namespace
```csharp
descriptor.ConnectionString("Endpoint=sb://namespace.servicebus.windows.net/...")
```

#### `Namespace(string fullyQualifiedNamespace)`
Sets the fully qualified namespace for Azure Identity-based authentication
```csharp
descriptor.Namespace("mynamespace.servicebus.windows.net")
```

#### `ConnectionProvider(Func<IServiceProvider, IEventHubConnectionProvider> connectionProvider)`
Sets a factory for custom connection providers
```csharp
descriptor.ConnectionProvider(sp => new CustomConnectionProvider(...))
```

**Provisioning Methods:**

#### `AutoProvision(bool autoProvision = true)`
Sets whether topology resources should be automatically provisioned

#### `ConfigureDefaults(Action<EventHubBusDefaults> configure)`
Configures bus-level defaults applied to all auto-provisioned topics and subscriptions
```csharp
descriptor.ConfigureDefaults(defaults =>
{
    defaults.Topic.PartitionCount = 4;
    defaults.DefaultBatchMode = EventHubBatchMode.Batch;
})
```

#### `ResourceGroup(string subscriptionId, string resourceGroupName, string namespaceName)`
Configures Azure Resource Manager coordinates for ARM-based auto-provisioning
```csharp
descriptor.ResourceGroup("sub-id", "resource-group", "eventhub-namespace")
```

**Endpoint Methods:**

#### `Endpoint(string name)`
Gets or creates a receive endpoint descriptor with the specified name
```csharp
descriptor.Endpoint("orders-endpoint")
    .Hub("orders")
    .ConsumerGroup("order-service")
    .Handler<OrderHandler>()
```

#### `DispatchEndpoint(string name)`
Gets or creates a dispatch endpoint descriptor with the specified name
```csharp
descriptor.DispatchEndpoint("orders-dispatch")
    .ToHub("orders")
    .Send<OrderCreated>()
```

**Topology Declaration Methods:**

#### `DeclareTopic(string name)`
Declares or retrieves a topic (Event Hub entity) in the transport topology
```csharp
descriptor.DeclareTopic("orders")
    .PartitionCount(4)
    .AutoProvision(true)
```

#### `DeclareSubscription(string topicName, string consumerGroup)`
Declares or retrieves a subscription (consumer group) in the transport topology
```csharp
descriptor.DeclareSubscription("orders", "order-service")
    .AutoProvision(true)
```

**Checkpoint Store Methods:**

#### `CheckpointStore(Func<IServiceProvider, ICheckpointStore> factory)`
Sets a factory for custom checkpoint store implementations
```csharp
descriptor.CheckpointStore(sp => new BlobStorageCheckpointStore(...))
```

#### `BlobCheckpointStore(string connectionString, string containerName)`
Configures Azure Blob Storage as the checkpoint store for persisting partition checkpoints across process restarts
```csharp
descriptor.BlobCheckpointStore(
    "DefaultEndpointsProtocol=https;AccountName=...",
    "event-hub-checkpoints")
```

**Ownership Store Methods:**

#### `OwnershipStore(Func<IServiceProvider, IPartitionOwnershipStore> factory)`
Sets a factory for custom ownership store implementations for distributed partition balancing
```csharp
descriptor.OwnershipStore(sp => new BlobStorageOwnershipStore(...))
```

#### `BlobOwnershipStore(string connectionString, string containerName)`
Configures Azure Blob Storage as the partition ownership store for distributed partition balancing across multiple processor instances
```csharp
descriptor.BlobOwnershipStore(
    "DefaultEndpointsProtocol=https;AccountName=...",
    "event-hub-ownership")
```

---

## 4. Bus-Level Defaults

### EventHubBusDefaults

**File:** `Configurations/EventHubBusDefaults.cs`

Defines defaults applied to all auto-provisioned topics and subscriptions.

**Properties:**

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Topic` | `EventHubDefaultTopicOptions` | new instance | Default topic configuration |
| `Subscription` | `EventHubDefaultSubscriptionOptions` | new instance | Default subscription configuration |
| `DefaultBatchMode` | `EventHubBatchMode` | `Single` | Default batch mode for dispatch endpoints |

### EventHubDefaultTopicOptions

**File:** `Configurations/EventHubDefaultTopicOptions.cs`

**Properties:**

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `PartitionCount` | `int?` | `null` | Default partition count for auto-provisioned topics. When `null`, Azure uses namespace default |

### EventHubDefaultSubscriptionOptions

**File:** `Configurations/EventHubDefaultSubscriptionOptions.cs`

Currently a placeholder for future subscription-level defaults (e.g., default checkpoint interval, consumer group naming conventions).

---

## 5. Connection Providers

### IEventHubConnectionProvider

**File:** `Configurations/IEventHubConnectionProvider.cs`

Provides connection details and the ability to create Event Hub producer clients.

**Properties:**

| Property | Type | Description |
|----------|------|-------------|
| `FullyQualifiedNamespace` | `string` | Fully qualified namespace (e.g., "mynamespace.servicebus.windows.net") |
| `ConnectionString` | `string?` | Connection string for this provider, or `null` if using token credentials. Used by `MochaEventProcessor` |
| `Credential` | `TokenCredential?` | Token credential for this provider, or `null` if using connection string |

**Methods:**

#### `CreateProducer(string eventHubName)`
```csharp
EventHubProducerClient CreateProducer(string eventHubName)
```
Creates an `EventHubProducerClient` for the specified hub.

### ConnectionStringEventHubConnectionProvider

**File:** `Configurations/ConnectionStringEventHubConnectionProvider.cs`

**Inheritance:** Implements `IEventHubConnectionProvider`

Connection provider for connection-string-based authentication.

**Constructor:**
```csharp
public ConnectionStringEventHubConnectionProvider(string connectionString)
```

**Behavior:**
- Parses the connection string to extract the fully qualified namespace
- Returns the connection string via `ConnectionString` property
- `Credential` property returns `null`

### CredentialEventHubConnectionProvider

**File:** `Configurations/CredentialEventHubConnectionProvider.cs`

**Inheritance:** Implements `IEventHubConnectionProvider`

Connection provider for Azure Identity token credential-based authentication.

**Constructor:**
```csharp
public CredentialEventHubConnectionProvider(
    string fullyQualifiedNamespace,
    TokenCredential credential)
```

**Behavior:**
- Uses the provided token credential for authentication
- Returns the credential via `Credential` property
- `ConnectionString` property returns `null`

---

## 6. Checkpoint & Ownership Stores

### ICheckpointStore

**File:** `Connection/ICheckpointStore.cs`

Pluggable checkpoint store for tracking the last processed sequence number per partition.

**Methods:**

#### `GetCheckpointAsync(...)`
```csharp
ValueTask<long?> GetCheckpointAsync(
    string fullyQualifiedNamespace,
    string eventHubName,
    string consumerGroup,
    string partitionId,
    CancellationToken cancellationToken)
```
Gets the checkpoint (last processed sequence number) for a partition. Returns `null` if no checkpoint exists.

#### `SetCheckpointAsync(...)`
```csharp
ValueTask SetCheckpointAsync(
    string fullyQualifiedNamespace,
    string eventHubName,
    string consumerGroup,
    string partitionId,
    long sequenceNumber,
    CancellationToken cancellationToken)
```
Updates the checkpoint for a partition.

**Built-in Implementations:**
- `InMemoryCheckpointStore` - In-memory storage (default for Phase 1)
- `BlobStorageCheckpointStore` - Azure Blob Storage persistence

### IPartitionOwnershipStore

**File:** `Connection/IPartitionOwnershipStore.cs`

Pluggable store for coordinating partition ownership across multiple processor instances.

**Methods:**

#### `ListOwnershipAsync(...)`
```csharp
ValueTask<IEnumerable<EventProcessorPartitionOwnership>> ListOwnershipAsync(
    string fullyQualifiedNamespace,
    string eventHubName,
    string consumerGroup,
    CancellationToken cancellationToken)
```
Lists all current partition ownership records for the specified Event Hub and consumer group.

#### `ClaimOwnershipAsync(...)`
```csharp
ValueTask<IEnumerable<EventProcessorPartitionOwnership>> ClaimOwnershipAsync(
    IEnumerable<EventProcessorPartitionOwnership> desiredOwnership,
    CancellationToken cancellationToken)
```
Attempts to claim ownership of specified partitions using optimistic concurrency. Only successfully claimed partitions are included in the result.

**Built-in Implementations:**
- Single-instance mode (claim all, empty list) - default
- `BlobStorageOwnershipStore` - Azure Blob Storage-based distributed balancing

---

## 7. Receive Endpoint Configuration

### IEventHubReceiveEndpointDescriptor

**File:** `Descriptors/IEventHubReceiveEndpointDescriptor.cs`

**Inheritance:** `IReceiveEndpointDescriptor<EventHubReceiveEndpointConfiguration>`

Fluent interface for configuring Event Hub receive endpoints.

**Base Methods (inherited, now fluent-returning):**
- `Handler<THandler>()` - Register handler
- `Consumer<TConsumer>()` - Register consumer
- `Kind(ReceiveEndpointKind kind)` - Set endpoint kind
- `FaultEndpoint(string name)` - Configure fault endpoint
- `SkippedEndpoint(string name)` - Configure skipped endpoint
- `MaxConcurrency(int maxConcurrency)` - Set max concurrent message processing
- `UseReceive(ReceiveMiddlewareConfiguration, string? before, string? after)` - Configure receive pipeline

**Event Hub-Specific Methods:**

#### `Hub(string name)`
Sets the Event Hub name that this endpoint will consume from
```csharp
descriptor.Hub("orders-hub")
```

#### `ConsumerGroup(string consumerGroup)`
Sets the consumer group used by this endpoint
```csharp
descriptor.ConsumerGroup("order-service-group")
```

#### `CheckpointInterval(int interval)`
Sets the number of events processed between checkpoints
- Default: `100`
```csharp
descriptor.CheckpointInterval(50)
```

---

## 8. Dispatch Endpoint Configuration

### IEventHubDispatchEndpointDescriptor

**File:** `Descriptors/IEventHubDispatchEndpointDescriptor.cs`

**Inheritance:** `IDispatchEndpointDescriptor<EventHubDispatchEndpointConfiguration>`

Fluent interface for configuring Event Hub dispatch endpoints.

**Base Methods (inherited, now fluent-returning):**
- `Send<TMessage>()` - Register message to be sent
- `Publish<TMessage>()` - Register message to be published
- `UseDispatch(DispatchMiddlewareConfiguration, string? before, string? after)` - Configure dispatch pipeline

**Event Hub-Specific Methods:**

#### `ToHub(string name)`
Sets the target Event Hub name for outbound message dispatch
```csharp
descriptor.ToHub("orders-hub")
```

#### `PartitionId(string partitionId)`
Sets the static partition ID for outbound message dispatch. When set, all messages from this endpoint are sent to the specified partition
```csharp
descriptor.PartitionId("0")
```

#### `BatchMode(EventHubBatchMode mode)`
Sets the batch mode for this dispatch endpoint
```csharp
descriptor.BatchMode(EventHubBatchMode.Batch)
```

---

## 9. Topic & Subscription Configuration

### IEventHubTopicDescriptor

**File:** `Topology/Descriptors/IEventHubTopicDescriptor.cs`

**Inheritance:** `IMessagingDescriptor<EventHubTopicConfiguration>`

Fluent interface for configuring Event Hub topics (entities).

**Methods:**

#### `PartitionCount(int partitionCount)`
Sets the number of partitions for the Event Hub
```csharp
descriptor.DeclareTopic("orders").PartitionCount(4)
```

#### `AutoProvision(bool autoProvision = true)`
Sets whether the topic should be automatically provisioned
```csharp
descriptor.DeclareTopic("orders").AutoProvision(true)
```

### IEventHubSubscriptionDescriptor

**File:** `Topology/Descriptors/IEventHubSubscriptionDescriptor.cs`

**Inheritance:** `IMessagingDescriptor<EventHubSubscriptionConfiguration>`

Fluent interface for configuring Event Hub subscriptions (consumer groups).

**Methods:**

#### `AutoProvision(bool autoProvision = true)`
Sets whether the consumer group should be automatically provisioned
```csharp
descriptor.DeclareSubscription("orders", "order-service")
    .AutoProvision(true)
```

---

## 10. Batch Mode

### EventHubBatchMode

**File:** `EventHubBatchMode.cs`

Enum specifying dispatch batching strategy for dispatch endpoints.

**Values:**

| Value | Default | Description |
|-------|---------|-------------|
| `Single` | Yes | Each message sent individually via `SendAsync`. Default behavior |
| `Batch` | No | Messages accumulated into `EventDataBatch` instances and sent as batches for higher throughput |

---

## 11. Health Check

### EventHubHealthCheck

**File:** `EventHubHealthCheck.cs`

**Inheritance:** Implements `IHealthCheck`

Health check that verifies the Event Hub transport's receive endpoints have running processors.

**Constructor:**
```csharp
public EventHubHealthCheck(EventHubMessagingTransport transport)
```

**Health Status:**
- **Healthy:** All configured processors are running
- **Degraded:** Some processors are running, but some are stopped
- **Unhealthy:** No processors are running or no endpoints configured

**Data (when degraded/unhealthy):**
```csharp
new Dictionary<string, object>
{
    ["running"] = runningCount,
    ["stopped"] = stoppedCount,
    ["stoppedEndpoints"] = List<string> // names of stopped endpoints
}
```

### EventHubHealthCheckExtensions

**File:** `EventHubHealthCheckExtensions.cs`

Extension methods for registering the health check.

#### `AddEventHub(IHealthChecksBuilder builder, EventHubMessagingTransport transport, params string[] tags)`
```csharp
healthChecks
    .AddEventHub(transport, "ready", "startup")
```
- Adds a health check monitoring Event Hub processors
- Default tag: `"ready"` (used if no tags provided)
- Failure status: `HealthStatus.Unhealthy`

---

## 12. Auto-Provisioning

### EventHubProvisioner

**File:** `Provisioning/EventHubProvisioner.cs`

**Scope:** Internal - used during startup when auto-provisioning is enabled

Provisions Event Hub entities and consumer groups via Azure Resource Manager.

**Constructor:**
```csharp
public EventHubProvisioner(
    EventHubsNamespaceResource namespaceResource,
    ILogger logger)
```

**Methods:**

#### `ProvisionTopicAsync(...)`
```csharp
public async ValueTask ProvisionTopicAsync(
    string eventHubName,
    int? partitionCount,
    CancellationToken cancellationToken)
```
Ensures an Event Hub entity exists. If the hub already exists, the operation is a no-op. When `partitionCount` is null or 0, the ARM API uses namespace default.

#### `ProvisionSubscriptionAsync(...)`
```csharp
public async ValueTask ProvisionSubscriptionAsync(
    string eventHubName,
    string consumerGroupName,
    CancellationToken cancellationToken)
```
Ensures a consumer group exists on the specified Event Hub. The default `$Default` consumer group is skipped since it always exists. Operation is idempotent.

#### `Create(EventHubTransportConfiguration configuration, IEventHubConnectionProvider connectionProvider, ILogger logger)` (Static)
```csharp
public static EventHubProvisioner Create(
    EventHubTransportConfiguration configuration,
    IEventHubConnectionProvider connectionProvider,
    ILogger logger)
```
Creates a provisioner from transport configuration using the connection provider's credential for ARM authentication.

**Requirements for Auto-Provisioning:**
- `SubscriptionId` configured
- `ResourceGroupName` configured
- `NamespaceName` configured
- Connection provider must supply a `TokenCredential` (connection string auth cannot provision resources)

**Throws:** `InvalidOperationException` if requirements not met

---

## 13. NuGet Dependencies

**Primary Dependencies:**
- `Azure.Messaging.EventHubs` (5.12.2) - Core Event Hubs SDK
- `Azure.Identity` (1.13.2) - Azure authentication via `DefaultAzureCredential`
- `Azure.Storage.Blobs` - For Blob Storage-based checkpoint and ownership stores
- `Azure.ResourceManager.EventHubs` - For ARM-based auto-provisioning
- `Microsoft.Extensions.Diagnostics.HealthChecks` - Health check framework
- `Mocha.Abstractions` - Base interfaces and abstractions
- `Mocha` - Core message bus framework

---

## 14. Aspire Integration

The transport is designed to work seamlessly with .NET Aspire for cloud-native application orchestration.

**Example AppHost Setup:**

```csharp
// AzureEventHubTransport.AppHost/Program.cs
var builder = DistributedApplication.CreateBuilder(args);

// Services register Event Hub transport
var app = builder
    .AddContainer("eventhub", "mcr.microsoft.com/azure-storage/azurite")
    .AddProject<Projects.OrderService>("order-service")
    .AddProject<Projects.ShippingService>("shipping-service")
    .AddProject<Projects.NotificationService>("notification-service")
    .Build();

await app.RunAsync();
```

**Service Registration:**

```csharp
var eventHubConnectionString = builder.Configuration.GetConnectionString("eventhubs")
    ?? "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true";

builder.Services
    .AddMessageBus()
    .AddInstrumentation()
    .AddEventHub(t => t.ConnectionString(eventHubConnectionString))
    .MapMessageBusDeveloperTopology();
```

**Aspire Packages Used:**
- `Aspire.Hosting.AppHost` (13.1.0)
- `Aspire.Hosting.Azure.EventHubs` (13.1.0) - For cloud deployment templates

---

## 15. Configuration Examples

### Basic Configuration with Connection String

```csharp
builder.AddEventHub(t => t
    .ConnectionString("Endpoint=sb://namespace.servicebus.windows.net/..."));
```

### Configuration with Azure Identity

```csharp
builder.AddEventHub(t => t
    .Namespace("mynamespace.servicebus.windows.net"));
    // Uses DefaultAzureCredential automatically
```

### Configuration with Auto-Provisioning

```csharp
builder.AddEventHub(t => t
    .Namespace("mynamespace.servicebus.windows.net")
    .ResourceGroup("sub-id", "resource-group", "eventhub-namespace")
    .AutoProvision(true)
    .ConfigureDefaults(defaults =>
    {
        defaults.Topic.PartitionCount = 4;
        defaults.DefaultBatchMode = EventHubBatchMode.Batch;
    }));
```

### Configuration with Blob Storage Checkpointing

```csharp
builder.AddEventHub(t => t
    .ConnectionString("Endpoint=sb://namespace.servicebus.windows.net/...")
    .BlobCheckpointStore(
        "DefaultEndpointsProtocol=https;AccountName=storageaccount;...",
        "event-hub-checkpoints"));
```

### Configuration with Distributed Ownership

```csharp
builder.AddEventHub(t => t
    .ConnectionString("Endpoint=sb://namespace.servicebus.windows.net/...")
    .BlobCheckpointStore(
        "DefaultEndpointsProtocol=https;AccountName=storageaccount;...",
        "event-hub-checkpoints")
    .BlobOwnershipStore(
        "DefaultEndpointsProtocol=https;AccountName=storageaccount;...",
        "event-hub-ownership"));
```

### Endpoint Configuration

```csharp
builder.AddEventHub(t => t
    .ConnectionString(eventHubConnectionString)
    .Endpoint("orders")
        .Hub("orders-hub")
        .ConsumerGroup("order-service")
        .CheckpointInterval(50)
        .Handler<OrderHandler>()
    .DispatchEndpoint("orders-dispatch")
        .ToHub("orders-hub")
        .BatchMode(EventHubBatchMode.Batch)
        .Send<OrderCreated>()
        .Publish<OrderShipped>());
```

### Topology Declaration

```csharp
builder.AddEventHub(t => t
    .ConnectionString(eventHubConnectionString)
    .DeclareTopic("orders")
        .PartitionCount(4)
        .AutoProvision(true)
    .DeclareSubscription("orders", "order-service")
        .AutoProvision(true));
```

---

## 16. Public Message Headers

### EventHubMessageHeaders

**File:** `EventHubMessageHeaders.cs`

Contains constants for Event Hub-specific message headers set on dispatched messages.

---

## Summary

The Azure Event Hub Transport provides a comprehensive, fluent API for:
1. **Connectivity** - Both connection string and Azure Identity authentication
2. **Topology Management** - Declarative topic and subscription configuration with auto-provisioning via ARM
3. **Distributed Processing** - Checkpoint and ownership stores for reliable, distributed message processing
4. **Flexible Endpoints** - Both receive and dispatch endpoints with batch mode support
5. **Health Monitoring** - Built-in health checks for processor status
6. **Aspire Integration** - First-class support for cloud-native application hosting

All configuration is performed through a fluent builder pattern starting from `AddEventHub()` on `IMessageBusHostBuilder`.
