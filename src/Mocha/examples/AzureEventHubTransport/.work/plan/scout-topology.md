# Scout Report: Azure Event Hub Transport Topology & Lifecycle

## Overview

This report documents the Azure Event Hub transport's architecture across endpoint lifecycle, topology provisioning, batching, and checkpointing. The foundation for understanding the 7 review issues identified.

---

## 1. Endpoint Lifecycle

### Base Classes & Lifecycle Flow

**Base Type: `ReceiveEndpoint<TConfiguration>` (Mocha.Endpoints.ReceiveEndpoint.cs:14-246)**

The endpoint lifecycle follows a strict state machine:
1. **Initialize** (line 163) — applies conventions, stores configuration, calls `OnInitialize()`
2. **DiscoverTopology** (line 191) — runs topology discovery conventions  
3. **Complete** (line 207) — resolves Source, compiles receive pipeline, calls `OnComplete()`
4. **StartAsync** (line 257) — initializes runtime services, calls `OnStartAsync()`
5. **StopAsync** — gracefully stops processing

**Critical properties:**
- `Source` (line 64) — the topology resource (topic/hub) being consumed
- `Configuration` (line 31) — stored during Initialize
- `IsInitialized`, `IsCompleted`, `IsStarted` (lines 41-54) — state flags
- `_pipeline` (line 24) — compiled receive middleware, installed during Complete

### EventHubReceiveEndpoint Implementation

**File:** `/workspaces/hc2/src/Mocha/src/Mocha.Transport.AzureEventHub/EventHubReceiveEndpoint.cs`

**Properties:**
- `Topic` (line 21) — the EventHubTopic being consumed (resolved in OnComplete)
- `Subscription` (line 26) — **ISSUE #5**: Never populated. Declared but always null.
- `_consumerGroup` (line 16) — consumer group name (default "$Default", configurable)
- `_checkpointInterval` (line 28) — events between checkpoints (default 100)
- `_processor` (line 29) — the running MochaEventProcessor instance

**Lifecycle Implementation:**

```
OnInitialize (line 37):
  - Validates HubName is set
  - Stores consumerGroup from configuration
  - Stores checkpointInterval from configuration

OnComplete (line 51):
  - Looks up Topic in topology.Topics by HubName (line 57)
  - Throws if not found
  - Sets Source = Topic
  - ** MISSING: Does NOT populate Subscription property **

OnStartAsync (line 64):
  - Resolves checkpoint store (memory vs blob storage)
  - Resolves ownership store (for distributed mode)
  - Creates MochaEventProcessor with:
    - Consumer group (_consumerGroup, line 100)
    - Message handler (ExecuteAsync through pipeline, line 80)
    - Checkpoint store (line 104)
    - Ownership store (line 105)
    - Checkpoint interval (line 106)
  - Starts processor (line 129)

OnStopAsync (line 133):
  - Gracefully stops processor (line 141)
  - ** ISSUE #4: No final checkpoint flush before stop **
```

### EventHubDispatchEndpoint Implementation

**File:** `/workspaces/hc2/src/Mocha/src/Mocha.Transport.AzureEventHub/EventHubDispatchEndpoint.cs`

**Properties:**
- `Topic` (line 25) — the EventHubTopic being published to
- `_batchDispatcher` (line 19) — optional EventHubBatchDispatcher (created if batch mode enabled)
- `_partitionId` (line 20) — static partition targeting (if configured)

**Lifecycle:**

```
OnInitialize (line 207):
  - Validates HubName is set

OnComplete (line 218):
  - Looks up Topic in topology.Topics (line 228)
  - Sets Destination = Topic
  - Creates batch dispatcher if batch mode enabled (lines 238-242)

DispatchAsync (line 28):
  - Partition routing precedence:
    1. x-partition-id header (explicit per-message, line 175)
    2. Configuration PartitionId (static, line 180)
    3. x-partition-key header (for ordering, line 184)
    4. Round-robin (no targeting, line 188)
  - Routes to batch dispatcher or direct send (lines 192-202)
```

---

## 2. Topology Flow: Discovery, Configuration, and Provisioning

### Topology Model

**File:** `/workspaces/hc2/src/Mocha/src/Mocha.Transport.AzureEventHub/Topology/EventHubMessagingTopology.cs`

Thread-safe topology manager with two resource collections:

```csharp
public sealed class EventHubMessagingTopology : MessagingTopology
{
    private readonly List<EventHubTopic> _topics = [];          // Hub entities
    private readonly List<EventHubSubscription> _subscriptions = []; // Consumer groups
    public bool AutoProvision { get; }                          // Default provisioning flag
}
```

**Methods:**
- `AddTopic(EventHubTopicConfiguration)` (line 49) — creates/initializes Topic
  - Calls `defaults.Topic.ApplyTo(configuration)` (line 61) — applies bus defaults
  - Calls `topic.Initialize()` then `topic.Complete()` (lines 62-64)
- `AddSubscription(EventHubSubscriptionConfiguration)` (line 75) — creates/initializes Subscription
  - No defaults applied to subscriptions (gap identified for **ISSUE #1**)

**Topology Resources:**

`EventHubTopic` (line 6 of EventHubTopic.cs):
- `Name` — hub entity name
- `PartitionCount` — partition count (null = Azure default)
- `AutoProvision` — nullable, falls back to transport default
- `ProvisionAsync()` (line 37) — calls provisioner.ProvisionTopicAsync()

`EventHubSubscription` (line 6 of EventHubSubscription.cs):
- `TopicName` — hub this consumer group belongs to
- `ConsumerGroup` — group name
- `AutoProvision` — nullable, falls back to transport default
- `ProvisionAsync()` (line 37) — calls provisioner.ProvisionSubscriptionAsync()

### Configuration Objects

**EventHubTopicConfiguration** (line 6 of EventHubTopicConfiguration.cs):
```csharp
public sealed class EventHubTopicConfiguration : TopologyConfiguration
{
    public string Name { get; set; }
    public int? PartitionCount { get; set; }
    public bool? AutoProvision { get; set; }
}
```

**EventHubSubscriptionConfiguration** (line 6 of EventHubSubscriptionConfiguration.cs):
```csharp
public sealed class EventHubSubscriptionConfiguration : TopologyConfiguration
{
    public string TopicName { get; set; }
    public string ConsumerGroup { get; set; } = "$Default";
    public bool? AutoProvision { get; set; }
}
```

**EventHubReceiveEndpointConfiguration** (line 6 of EventHubReceiveEndpointConfiguration.cs):
```csharp
public sealed class EventHubReceiveEndpointConfiguration : ReceiveEndpointConfiguration
{
    public string? HubName { get; set; }
    public string ConsumerGroup { get; set; } = "$Default";
    public int CheckpointInterval { get; set; } = 100;
}
```

### Topology Discovery Conventions

**EventHubReceiveEndpointTopologyConvention** (line 7 of EventHubReceiveEndpointTopologyConvention.cs):

```csharp
public void DiscoverTopology(...)
{
    // 1. Ensure endpoint's hub exists in topology.Topics (lines 23-30)
    if (topology.Topics.FirstOrDefault(t => t.Name == configuration.HubName) is null)
    {
        topology.AddTopic(new EventHubTopicConfiguration { ... });
    }
    
    // 2. For each inbound route, ensure topic exists (lines 38-58)
    var routes = context.Router.GetInboundByEndpoint(endpoint);
    foreach (var route in routes)
    {
        // Create topics for route.MessageType's publish & send hubs
    }
}
```

**CRITICAL GAP (#1):** No consumer group is created for dynamically discovered receive endpoints.
- The convention creates Topic entries (line 25)
- But never creates EventHubSubscription entries
- Endpoints that use non-$Default consumer groups will fail during provisioning

**EventHubDispatchEndpointTopologyConvention** (line 7 of EventHubDispatchEndpointTopologyConvention.cs):

```csharp
public void DiscoverTopology(...)
{
    // Ensure hub exists (lines 17-21)
    if (topology.Topics.FirstOrDefault(t => t.Name == configuration.HubName) is null)
    {
        topology.AddTopic(new EventHubTopicConfiguration { ... });
    }
}
```

### Provisioning Flow

**Entry point:** `EventHubMessagingTransport.OnBeforeStartAsync()` (line 111)

```csharp
protected override async ValueTask OnBeforeStartAsync(...)
{
    if (!autoProvision) return;
    
    // Skip if no ARM coordinates (e.g., using connection string with emulator)
    if (SubscriptionId is null || ResourceGroupName is null || NamespaceName is null)
        return;
    
    // Provision all transport-level topics and subscriptions
    foreach (var topic in _topology.Topics)
    {
        if (topic.AutoProvision ?? autoProvision)
            await topic.ProvisionAsync(provisioner, cancellationToken); // line 139
    }
    
    foreach (var subscription in _topology.Subscriptions)
    {
        if (subscription.AutoProvision ?? autoProvision)
            await subscription.ProvisionAsync(provisioner, cancellationToken); // line 147
    }
}
```

**EventHubProvisioner** (line 13 of EventHubProvisioner.cs):

```csharp
public async ValueTask ProvisionTopicAsync(string eventHubName, int? partitionCount, ...)
{
    // CreateOrUpdateAsync is idempotent (line 47)
    var data = new EventHubData();
    if (partitionCount is > 0)
        data.PartitionCount = partitionCount.Value;
    await collection.CreateOrUpdateAsync(WaitUntil.Completed, eventHubName, data, ...);
}

public async ValueTask ProvisionSubscriptionAsync(string eventHubName, string consumerGroupName, ...)
{
    // Skip $Default (it always exists, line 80)
    if (string.Equals(consumerGroupName, "$Default", ...))
        return;
    
    // CreateOrUpdate is idempotent (line 96)
    var eventHubResponse = await _namespaceResource.GetEventHubAsync(eventHubName, ...);
    await eventHubResponse.Value.GetEventHubsConsumerGroups()
        .CreateOrUpdateAsync(WaitUntil.Completed, consumerGroupName, new(), ...);
}
```

---

## 3. Batch Dispatcher: Partition Key Ordering Issue

**File:** `/workspaces/hc2/src/Mocha/src/Mocha.Transport.AzureEventHub/Connection/EventHubBatchDispatcher.cs`

### Architecture

- **Thread-safe channel** (line 45) for enqueueing events
- **Background loop** (line 47) drains channel and accumulates into batches
- **Time-based flush** (line 129) — max 100ms wait before sending partial batch
- **Partition targeting** (lines 160-161) — respects PartitionKey and PartitionId

### Partition Routing Logic

```csharp
private async Task DrainAndSendAsync(...)
{
    EventDataBatch? batch = null;
    string? currentPartitionKey = null;
    string? currentPartitionId = null;
    
    while (true)
    {
        PendingEvent item = /* read from channel */;
        
        var itemPartitionKey = item.SendOptions?.PartitionKey;
        var itemPartitionId = item.SendOptions?.PartitionId;
        
        // ISSUE #2: Partition key ordering violation
        // If targeting differs from current batch, FLUSH and start new batch (lines 164-178)
        if (batch is not null
            && (itemPartitionKey != currentPartitionKey || itemPartitionId != currentPartitionId))
        {
            await SendBatchAsync(batch, _pending, cancellationToken);
            batch = null;
            _pending.Clear();
        }
        
        // Update current targeting
        currentPartitionKey = itemPartitionKey;
        currentPartitionId = itemPartitionId;
        
        // Try to add to batch (line 185)
        if (!batch.TryAdd(item.EventData))
        {
            // Batch full — send immediately (lines 188-191)
            await SendBatchAsync(batch, _pending, cancellationToken);
            batch = null;
        }
        
        _pending.Add(item);
    }
}
```

### The Ordering Problem

The flush-on-partition-change logic at lines 164-178 **breaks ordering guarantees**:

**Scenario:**
1. Event A with partition-key="user-1" arrives
2. Batch created with PartitionKey="user-1"
3. Event B with partition-key="user-2" arrives
4. Dispatcher detects partition change (line 165)
5. **Flushes batch immediately** (line 169)
6. Event C with partition-key="user-1" arrives later
7. **New batch created** for user-1

**Result:** Events A and C (both user-1) are sent in separate batches. If B takes longer to send (network hiccup), C may arrive first at the broker — **violating order within user-1**.

**Root cause:** Batching on partition keys sacrifices ordering for throughput. The dispatcher was not designed to maintain per-partition ordering across batch boundaries.

---

## 4. Checkpointing & Processor Lifecycle

### MochaEventProcessor

**File:** `/workspaces/hc2/src/Mocha/src/Mocha.Transport.AzureEventHub/Connection/MochaEventProcessor.cs`

Extends `EventProcessor<EventProcessorPartition>` (Azure SDK base class).

**Constructor Parameters:**
- `checkpointInterval` — events to process before checkpointing (default 100, line 40)
- `_checkpointStore` — pluggable checkpoint backend (in-memory or blob storage)
- `_ownershipStore` — partition ownership (for distributed mode, line 20)

### Checkpointing Strategy (ISSUE #3 & #4)

**OnProcessingEventBatchAsync** (line 92):

```csharp
protected override async Task OnProcessingEventBatchAsync(
    IEnumerable<EventData> events,
    EventProcessorPartition partition,
    CancellationToken cancellationToken)
{
    long lastSuccessfulSequence = -1;
    var eventList = events as IList<EventData> ?? events.ToList();
    var lastIndex = eventList.Count - 1;
    var index = 0;
    
    foreach (var eventData in eventList)
    {
        try
        {
            await _messageHandler(eventData, partition.PartitionId, cancellationToken);
            lastSuccessfulSequence = eventData.SequenceNumber;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ErrorProcessingEvent(...);
            index++;
            continue; // Skip failed event, continue
        }
        
        var counter = _partitionCounters.AddOrUpdate(
            partition.PartitionId, 1, static (_, c) => c + 1);
        
        // Checkpoint when interval reached OR on last event in batch (line 120)
        if (counter >= _checkpointInterval || index == lastIndex)
        {
            await _checkpointStore.SetCheckpointAsync(
                _fullyQualifiedNamespace,
                _eventHubName,
                _consumerGroup,
                partition.PartitionId,
                lastSuccessfulSequence,
                cancellationToken);
            
            _partitionCounters[partition.PartitionId] = 0;
        }
        
        index++;
    }
}
```

**ISSUE #3: Count-only checkpointing**
- Checkpoints every 100 events (line 120)
- No time-based trigger (apart from batch boundary)
- If processing is slow, could go hours without checkpointing
- On restart, might reprocess thousands of old events

**ISSUE #4: No graceful shutdown flush**
- When `StopProcessingAsync()` is called (EventHubReceiveEndpoint.OnStopAsync, line 141)
- The processor stops consuming new events
- But any in-flight batches that haven't reached checkpoint interval are lost
- Events processed but not checkpointed will be reprocessed on next start

### Checkpoint Store Interface

**ICheckpointStore** (line 7 of ICheckpointStore.cs):

```csharp
public interface ICheckpointStore
{
    ValueTask<long?> GetCheckpointAsync(
        string fullyQualifiedNamespace,
        string eventHubName,
        string consumerGroup,
        string partitionId,
        CancellationToken cancellationToken);
    
    ValueTask SetCheckpointAsync(
        string fullyQualifiedNamespace,
        string eventHubName,
        string consumerGroup,
        string partitionId,
        long sequenceNumber,
        CancellationToken cancellationToken);
}
```

**InMemoryCheckpointStore** (line 9 of InMemoryCheckpointStore.cs):
- Uses `ConcurrentDictionary<(ns, hub, group, partition), long>`
- Checkpoints lost on process restart
- Used by default if no CheckpointStoreFactory provided

**BlobStorageCheckpointStore** (line 11 of BlobStorageCheckpointStore.cs):
- Stores checkpoint in Azure Blob Storage
- Key format: `{namespace}/{hub}/{group}/checkpoint/{partition}`
- Single blob per partition, overwritten on each checkpoint
- Survives process restart

### Multi-instance Mode (ISSUE #6)

**Ownership Store** (line 38, not provided in read):
- Partition ownership managed by `IPartitionOwnershipStore`
- When set, partitions are claimed competitively (EventProcessor base class)
- When null, single-instance mode: all partitions claimed locally (line 198)

**Problem:** No safety checks for multi-instance deployments.
- If two instances run simultaneously without OwnershipStore configured:
  - Both claim all partitions
  - Both process same events
  - Both write to same checkpoint store
  - **Race condition on SetCheckpointAsync**

---

## 5. RabbitMQ Transport: Comparison Patterns

**File:** `/workspaces/hc2/src/Mocha/src/Mocha.Transport.RabbitMQ/RabbitMQReceiveEndpoint.cs`

**Key Differences:**

| Aspect | Event Hub | RabbitMQ |
|--------|-----------|----------|
| **Source Resolution** | Topology.Topics lookup (line 57) | Topology.Queues lookup (line 50) |
| **Consumer Setup** | MochaEventProcessor (distributed, partition-based) | ConsumerManager (local queue binding) |
| **Checkpointing** | Explicit SetCheckpointAsync per interval | Implicit RabbitMQ acknowledgments |
| **Partition Ordering** | Per-partition, but batch dispatcher breaks it | Per-queue FIFO guaranteed |
| **Error Handling** | Exception logging, continue (line 109) | Explicit nack/retry logic |

RabbitMQ's simpler model avoids partition key batching — each queue is handled serially.

---

## 6. Configuration & Defaults

**EventHubTransportConfiguration** (line 7 of EventHubTransportConfiguration.cs):

```csharp
public class EventHubTransportConfiguration : MessagingTransportConfiguration
{
    public Func<IServiceProvider, IEventHubConnectionProvider>? ConnectionProvider { get; set; }
    public string? ConnectionString { get; set; }
    public string? FullyQualifiedNamespace { get; set; }
    
    public List<EventHubTopicConfiguration> Topics { get; set; } = [];
    public List<EventHubSubscriptionConfiguration> Subscriptions { get; set; } = [];
    
    public bool? AutoProvision { get; set; } // null = default true
    public EventHubBusDefaults Defaults { get; set; } = new();
    
    public Func<IServiceProvider, ICheckpointStore>? CheckpointStoreFactory { get; set; }
    public Func<IServiceProvider, IPartitionOwnershipStore>? OwnershipStoreFactory { get; set; }
    
    // ARM resource coordinates
    public string? SubscriptionId { get; set; }
    public string? ResourceGroupName { get; set; }
    public string? NamespaceName { get; set; }
}
```

**EventHubBusDefaults** (not fully read, but referenced at line 76):
- Applied to all auto-provisioned topics (line 61 of EventHubMessagingTopology.cs)
- **NOT applied to subscriptions** (discovered gap for ISSUE #1)

---

## 7. String Allocation Optimizations (ISSUE #7)

**Hot paths identified:**

1. **EventHubDispatchEndpoint.DispatchAsync** (line 28):
   - Line 53: `new string(lastSegment)` — allocates destination hub name from span
   - Line 222: `new string(name)` — allocates for dynamic dispatch endpoint creation
   
2. **EventHubBatchDispatcher.CreateBatchForOptionsAsync** (line 231):
   - Creates `CreateBatchOptions` with string PartitionKey/PartitionId
   - These strings are already in `SendEventOptions` — could reuse
   
3. **Partition key comparison** (line 160):
   - `itemPartitionKey != currentPartitionKey` — string equality checks
   - Could use intern pool or span-based comparison

---

## Call Graph: Endpoint Initialization → Start

```
MessagingTransport.Initialize()
├─ CreateReceiveEndpoint() → EventHubReceiveEndpoint
├─ endpoint.Initialize(context, configuration)
│  ├─ Transport.Conventions.Configure() [apply conventions]
│  └─ OnInitialize() → validates HubName, stores consumer group & checkpoint interval
├─ endpoint.DiscoverTopology(context)
│  └─ Transport.Conventions.DiscoverTopology() 
│     └─ EventHubReceiveEndpointTopologyConvention.DiscoverTopology()
│        ├─ Ensure Topic exists in topology.Topics [gap: no Subscription created]
│        └─ For each inbound route, ensure Topic exists
├─ endpoint.Complete(context)
│  └─ OnComplete()
│     ├─ Lookup Topic in topology.Topics (line 57)
│     ├─ Set Source = Topic
│     └─ [gap: Subscription property never populated]
└─ transport.StartAsync(context)
   ├─ OnBeforeStartAsync() [provision topology]
   │  ├─ Provision all Topics with ProvisionAsync() (line 139)
   │  └─ Provision all Subscriptions with ProvisionAsync() (line 147)
   │     [gap: dynamic subscriptions not created here]
   └─ endpoint.StartAsync(context)
      ├─ OnStartAsync()
      │  ├─ Resolve CheckpointStore (memory vs blob)
      │  ├─ Create MochaEventProcessor
      │  └─ Start processor (line 129)
      │     [gap: no final checkpoint on stop]
      └─ Processor.StartProcessingAsync()
         └─ Begin consuming from partition with checkpoint recovery
```

---

## Summary of Issues and Locations

| # | Issue | Location | Severity |
|---|-------|----------|----------|
| 1 | No consumer group auto-provisioning for dynamic endpoints | EventHubReceiveEndpointTopologyConvention.DiscoverTopology (line 23) | Critical |
| 2 | Batch dispatcher partition key ordering violation | EventHubBatchDispatcher.DrainAndSendAsync (lines 164-178) | High |
| 3 | No time-based checkpoint flushing | MochaEventProcessor.OnProcessingEventBatchAsync (line 120) | Medium |
| 4 | No graceful shutdown checkpoint flushing | EventHubReceiveEndpoint.OnStopAsync (line 141) | Medium |
| 5 | Subscription property never populated | EventHubReceiveEndpoint.OnComplete (line 51) | Low |
| 6 | No multi-instance deployment safety checks | MochaEventProcessor (line 15) | High |
| 7 | String allocations on hot paths | Multiple (EventHubDispatchEndpoint, BatchDispatcher) | Low |

---

## Key Insights for Fixes

1. **Consumer group provisioning:** Must extend EventHubReceiveEndpointTopologyConvention to create EventHubSubscription entries dynamically, with proper handling of non-$Default groups.

2. **Partition ordering:** Either disable batching for partition-keyed messages or redesign the dispatcher to maintain per-key ordering across batch boundaries (complex).

3. **Checkpointing:** Add time-based timer alongside count-based logic, and implement graceful flush during shutdown.

4. **Multi-instance safety:** Validate OwnershipStore is configured when topology is multi-instance capable.

5. **Subscription property:** Populate during OnComplete after Topic is resolved.

6. **String allocations:** Use ReadOnlyMemory<char> or string interning where possible.
