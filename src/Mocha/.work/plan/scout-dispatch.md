# Scout Report: Kafka Dispatch Pipeline and Topology System

## Executive Summary

This document details the exact code locations and mechanisms involved in the 6 key findings requiring fixes:
- #2: ReplicationFactor default
- #5: TaskCompletionSource per dispatch
- #7: Header byte[] allocations on dispatch
- #10: Error topic retention
- #11: Consumer group collision
- #12: Reply topic cleanup

---

## Finding #2: ReplicationFactor Default

**Issue**: Hardcoded default of 1 for all topics; no way to configure globally.

### Locations

#### 1. KafkaTopic.cs:21
```csharp
public short ReplicationFactor { get; private set; } = 1;
```
Default set at property level. This is applied during initialization:

#### 2. KafkaTopic.cs:40-46 (OnInitialize)
```csharp
protected override void OnInitialize(TopologyConfiguration configuration)
{
    var config = (KafkaTopicConfiguration)configuration;
    
    Name = config.Name;
    Partitions = config.Partitions ?? 1;
    ReplicationFactor = config.ReplicationFactor ?? 1;  // <-- Hardcoded default
    AutoProvision = config.AutoProvision;
    TopicConfigs = config.TopicConfigs;
    IsTemporary = config.IsTemporary;
    
    Topology = config.Topology!;
    Address = new Uri(Topology.Address, $"t/{Name}");
}
```

#### 3. KafkaMessagingTopology.cs:43-61 (AddTopic method)
```csharp
public KafkaTopic AddTopic(KafkaTopicConfiguration configuration)
{
    lock (_lock)
    {
        var topic = _topics.FirstOrDefault(t => t.Name == configuration.Name);
        if (topic is not null)
        {
            throw new InvalidOperationException($"Topic '{configuration.Name}' already exists");
        }

        topic = new KafkaTopic();
        configuration.Topology = this;
        defaults.Topic.ApplyTo(configuration);  // <-- Applies KafkaBusDefaults
        topic.Initialize(configuration);
        _topics.Add(topic);
        topic.Complete();
        return topic;
    }
}
```

#### 4. KafkaDefaultTopicOptions.cs:27-36 (ApplyTo method)
```csharp
internal void ApplyTo(KafkaTopicConfiguration configuration)
{
    configuration.Partitions ??= Partitions;
    configuration.ReplicationFactor ??= ReplicationFactor;  // <-- Only applies if not null

    if (TopicConfigs is not null && configuration.TopicConfigs is null)
    {
        configuration.TopicConfigs = new Dictionary<string, string>(TopicConfigs);
    }
}
```
**Problem**: If KafkaBusDefaults.Topic.ReplicationFactor is null (default), no override happens, and the hardcoded `?? 1` applies.

#### 5. KafkaBusDefaults.cs:7-14
```csharp
public sealed class KafkaBusDefaults
{
    /// <summary>
    /// Gets or sets the default topic configuration that is applied to all auto-provisioned topics.
    /// Individual topic settings will override these defaults.
    /// </summary>
    public KafkaDefaultTopicOptions Topic { get; set; } = new();
}
```
**Problem**: Topic is initialized as `new()`, with all fields null by default.

#### 6. KafkaDefaultTopicOptions.cs:6-21
```csharp
public sealed class KafkaDefaultTopicOptions
{
    /// <summary>
    /// Gets or sets the default number of partitions for auto-provisioned topics.
    /// </summary>
    public int? Partitions { get; set; }

    /// <summary>
    /// Gets or sets the default replication factor for auto-provisioned topics.
    /// </summary>
    public short? ReplicationFactor { get; set; }

    /// <summary>
    /// Gets or sets the default topic-level configs (e.g., retention.ms, cleanup.policy).
    /// </summary>
    public Dictionary<string, string>? TopicConfigs { get; set; }
    // ...
}
```
**All fields default to null**, so no way to set a global default without explicitly calling ConfigureDefaults.

#### 7. KafkaMessagingTransportDescriptor.cs:140-145 (ConfigureDefaults method)
```csharp
public IKafkaMessagingTransportDescriptor ConfigureDefaults(Action<KafkaBusDefaults> configure)
{
    configure(Configuration.Defaults);

    return this;
}
```
**Users must call this to override defaults** — there is no built-in sensible default.

---

## Finding #5: TaskCompletionSource Allocated Per Dispatch

**Issue**: New TaskCompletionSource created for every dispatch; TODO comment acknowledges pooling opportunity.

### Locations

#### 1. KafkaDispatchEndpoint.cs:46-122 (DispatchAsync method)
```csharp
protected override async ValueTask DispatchAsync(IDispatchContext context)
{
    if (context.Envelope is not { } envelope)
    {
        throw new InvalidOperationException("Envelope is not set");
    }

    // ... setup code ...
    
    // Use Produce() with callback for performance (avoids Task allocation from ProduceAsync)
    // TODO: consider IValueTaskSource pooling for high-throughput scenarios
    var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);  // <-- Allocation per dispatch
    connectionManager.TrackInflight(tcs);

    // Link caller's cancellation token to TCS so cancellation unblocks the await
    await using var ctr = cancellationToken.Register(static state =>
    {
        var t = (TaskCompletionSource)state!;
        t.TrySetCanceled();
    }, tcs);

    try
    {
        producer.Produce(topicName, message, report =>
        {
            // This callback runs on librdkafka's delivery-report thread.
            // Do NOT access context or features here -- context may be pooled/recycled.
            if (report.Error.IsError)
            {
                tcs.TrySetException(new KafkaException(report.Error));
            }
            else
            {
                tcs.TrySetResult();
            }

            connectionManager.UntrackInflight(tcs);
        });
    }
    catch (ProduceException<byte[], byte[]> ex)
    {
        tcs.TrySetException(ex);
        connectionManager.UntrackInflight(tcs);
    }

    await tcs.Task;  // <-- Allocates Task from TCS
}
```

#### 2. KafkaConnectionManager.cs:22 (Inflight tracking)
```csharp
private readonly ConcurrentDictionary<TaskCompletionSource, byte> _inflightDispatches = new();
```

#### 3. KafkaConnectionManager.cs:193-199 (Tracking methods)
```csharp
/// <summary>
/// Tracks an in-flight dispatch operation for graceful shutdown.
/// </summary>
/// <param name="tcs">The task completion source representing the in-flight dispatch.</param>
public void TrackInflight(TaskCompletionSource tcs) => _inflightDispatches.TryAdd(tcs, 0);

/// <summary>
/// Untracks an in-flight dispatch operation after delivery report or cancellation.
/// </summary>
/// <param name="tcs">The task completion source to untrack.</param>
public void UntrackInflight(TaskCompletionSource tcs) => _inflightDispatches.TryRemove(tcs, out _);
```

#### 4. KafkaConnectionManager.cs:217-224 (Disposal with remaining TCS)
```csharp
// Cancel any remaining in-flight dispatch TCS instances.
// After Flush(10s), any TCS still pending means the delivery
// report never arrived -- cancel to unblock callers.
foreach (var tcs in _inflightDispatches.Keys)
{
    tcs.TrySetCanceled();
}

_inflightDispatches.Clear();
```

**Allocation Pattern**:
- **Per-dispatch allocation**: 1 TaskCompletionSource + 1 Task from `.Task` property = 2 allocations per message
- **Tracking overhead**: ConcurrentDictionary entry per inflight message
- **Cancellation registration**: Per-dispatch CancellationTokenRegistration

**Note**: Comment on line 86 explicitly mentions "TODO: consider IValueTaskSource pooling for high-throughput scenarios"

---

## Finding #7: Header byte[] Allocations on Dispatch

**Issue**: Encoding.UTF8.GetBytes called for every string header field, even for small strings.

### Locations

#### 1. KafkaDispatchEndpoint.cs:171-250 (BuildKafkaHeaders method)
```csharp
private static Confluent.Kafka.Headers BuildKafkaHeaders(MessageEnvelope envelope)
{
    var headers = new Confluent.Kafka.Headers();

    // Map well-known envelope fields to Kafka headers
    if (envelope.MessageId is not null)
    {
        headers.Add(KafkaMessageHeaders.MessageId, Encoding.UTF8.GetBytes(envelope.MessageId));
    }

    if (envelope.CorrelationId is not null)
    {
        headers.Add(KafkaMessageHeaders.CorrelationId, Encoding.UTF8.GetBytes(envelope.CorrelationId));
    }

    if (envelope.ConversationId is not null)
    {
        headers.Add(KafkaMessageHeaders.ConversationId, Encoding.UTF8.GetBytes(envelope.ConversationId));
    }

    if (envelope.CausationId is not null)
    {
        headers.Add(KafkaMessageHeaders.CausationId, Encoding.UTF8.GetBytes(envelope.CausationId));
    }

    if (envelope.SourceAddress is not null)
    {
        headers.Add(KafkaMessageHeaders.SourceAddress, Encoding.UTF8.GetBytes(envelope.SourceAddress));
    }

    if (envelope.DestinationAddress is not null)
    {
        headers.Add(KafkaMessageHeaders.DestinationAddress, Encoding.UTF8.GetBytes(envelope.DestinationAddress));
    }

    if (envelope.ResponseAddress is not null)
    {
        headers.Add(KafkaMessageHeaders.ResponseAddress, Encoding.UTF8.GetBytes(envelope.ResponseAddress));
    }

    if (envelope.FaultAddress is not null)
    {
        headers.Add(KafkaMessageHeaders.FaultAddress, Encoding.UTF8.GetBytes(envelope.FaultAddress));
    }

    if (envelope.ContentType is not null)
    {
        headers.Add(KafkaMessageHeaders.ContentType, Encoding.UTF8.GetBytes(envelope.ContentType));
    }

    if (envelope.MessageType is not null)
    {
        headers.Add(KafkaMessageHeaders.MessageType, Encoding.UTF8.GetBytes(envelope.MessageType));
    }

    if (envelope.SentAt is not null)
    {
        headers.Add(KafkaMessageHeaders.SentAt, Encoding.UTF8.GetBytes(envelope.SentAt.Value.ToString("O")));
    }

    if (envelope.EnclosedMessageTypes is { Length: > 0 } enclosed)
    {
        headers.Add(KafkaMessageHeaders.EnclosedMessageTypes,
            Encoding.UTF8.GetBytes(string.Join(",", enclosed)));
    }

    // Map custom headers
    if (envelope.Headers is not null)
    {
        foreach (var header in envelope.Headers)
        {
            if (header.Value is not null)
            {
                headers.Add(header.Key, Encoding.UTF8.GetBytes(header.Value.ToString()!));  // <-- Per custom header
            }
        }
    }

    return headers;
}
```

**Allocation count**:
- Fixed fields: 12 Encoding.UTF8.GetBytes calls (MessageId, CorrelationId, ConversationId, CausationId, SourceAddress, DestinationAddress, ResponseAddress, FaultAddress, ContentType, MessageType, SentAt, EnclosedMessageTypes)
- Variable fields: N custom headers × Encoding.UTF8.GetBytes
- **Total per dispatch**: 12+ allocations from encoding alone

#### 2. KafkaDispatchEndpoint.cs:165-169 (SelectKey method)
```csharp
private static byte[]? SelectKey(MessageEnvelope envelope)
{
    var keySource = envelope.CorrelationId ?? envelope.MessageId;
    return keySource is not null ? Encoding.UTF8.GetBytes(keySource) : null;  // <-- Key encoding allocation
}
```

**Total allocation impact per dispatch**:
- Key: 1 × Encoding.UTF8.GetBytes
- Headers: 12+ × Encoding.UTF8.GetBytes (more with custom headers)
- **Typical message: 13+ byte[] allocations**

**Note**: No pooling mechanism exists for header serialization. Compare to receive side:
- KafkaMessageEnvelopeParser.cs (lines 46-59) decodes headers on read path but doesn't have equivalent allocation pressure because it reuses existing bytes from Kafka.

---

## Finding #10: Error Topic Retention Not Set

**Issue**: Error topics created without any retention config; can grow unbounded.

### Locations

#### 1. KafkaDispatchEndpointTopologyConvention.cs:28-40
```csharp
// Ensure error topics exist for dispatch endpoints that route to named topics.
// Skip endpoints whose topic is already an error or skipped topic to avoid
// recursive topic creation (e.g. "orders_error_error").
if (configuration.TopicName is { } topicName
    && !topicName.EndsWith("_error", StringComparison.Ordinal)
    && !topicName.EndsWith("_skipped", StringComparison.Ordinal))
{
    var errorTopicName = topicName + "_error";
    if (topology.Topics.FirstOrDefault(t => t.Name == errorTopicName) is null)
    {
        topology.AddTopic(new KafkaTopicConfiguration { Name = errorTopicName });  // <-- No TopicConfigs set
    }
}
```

#### 2. KafkaReceiveEndpointTopologyConvention.cs:58-63
```csharp
// Ensure error topic exists for default endpoints.
var errorTopicName = configuration.TopicName + "_error";
if (topology.Topics.FirstOrDefault(t => t.Name == errorTopicName) is null)
{
    topology.AddTopic(new KafkaTopicConfiguration { Name = errorTopicName });  // <-- No TopicConfigs set
}
```

**Problem**: 
- Error topics are created with empty KafkaTopicConfiguration
- No retention settings (retention.ms) are applied
- No cleanup.policy specified
- Topics inherit defaults from KafkaDefaultTopicOptions, which are typically null
- This leads to unbounded growth of error topics in production

**Contrast with Reply topics** (KafkaReceiveEndpointTopologyConvention.cs:40-48):
```csharp
// Reply topics get short retention for self-cleanup.
if (endpoint.Kind == ReceiveEndpointKind.Reply)
{
    topicConfig.TopicConfigs = new Dictionary<string, string>
    {
        ["retention.ms"] = "3600000",      // 1 hour
        ["cleanup.policy"] = "delete"
    };
}
```

---

## Finding #11: Consumer Group Collision

**Issue**: Reply endpoints and regular endpoints both use endpoint name as consumer group ID; potential collision if service name varies.

### Locations

#### 1. DefaultNamingConventions.cs:22-40 (GetReceiveEndpointName for routes)
```csharp
public string GetReceiveEndpointName(InboundRoute route, ReceiveEndpointKind kind)
{
    ArgumentNullException.ThrowIfNull(route);

    if (!route.IsInitialized)
    {
        throw ThrowHelper.RouteNotInitialized();
    }

    return route.Kind switch
    {
        InboundRouteKind.Subscribe => (host.ServiceName is not null ? ToKebabCase(host.ServiceName) + "." : "")
            + GetReceiveEndpointName(route.Consumer.Name, kind),
        InboundRouteKind.Send => GetSendEndpointName(route.MessageType!.RuntimeType),
        InboundRouteKind.Request => GetSendEndpointName(route.MessageType!.RuntimeType),
        InboundRouteKind.Reply => "reply-endpoint",  // <-- Fixed name for reply endpoints
        _ => throw new ArgumentException("Invalid inbound route kind.", nameof(route))
    };
}
```

#### 2. DefaultNamingConventions.cs:91-100 (GetInstanceEndpoint for replies)
```csharp
public string GetInstanceEndpoint(Guid consumerId)
{
    if (consumerId == Guid.Empty)
    {
        throw new ArgumentException("Consumer ID cannot be empty.", nameof(consumerId));
    }

    // Use N format (no hyphens) for shorter queue names
    return $"response-{consumerId:N}";  // <-- Per-instance unique name
}
```

#### 3. KafkaMessagingTransport.cs:220-232 (CreateEndpointConfiguration for Reply routes)
```csharp
if (route.Kind == InboundRouteKind.Reply)
{
    var instanceTopicName = context.Naming.GetInstanceEndpoint(context.Host.InstanceId);  // <-- Uses instance GUID
    return new KafkaReceiveEndpointConfiguration
    {
        Name = "Replies",
        TopicName = instanceTopicName,
        ConsumerGroupId = instanceTopicName,  // <-- Uses same as topic name
        IsTemporary = true,
        Kind = ReceiveEndpointKind.Reply,
        AutoProvision = true,
        ReceiveMiddlewares = [ReplyReceiveMiddleware.Create()]
    };
}

var endpointName = context.Naming.GetReceiveEndpointName(route, ReceiveEndpointKind.Default);
return new KafkaReceiveEndpointConfiguration
{
    Name = endpointName,
    TopicName = endpointName,
    ConsumerGroupId = endpointName  // <-- Uses endpoint name as group ID
};
```

#### 4. KafkaReceiveEndpoint.cs (receives messages)
Consumer is created in KafkaReceiveEndpoint; uses ConsumerGroupId from configuration:

Located at: /workspaces/hc3/src/Mocha/src/Mocha.Transport.Kafka/KafkaReceiveEndpoint.cs
(Not fully shown in prior reads, but from KafkaConnectionManager.cs:94-124, CreateConsumer uses `groupId` parameter directly)

**Collision Mechanism**:
- Regular endpoints: consumer group = endpoint name (e.g., "order-handler")
- Reply endpoints: consumer group = "response-{instanceGuid}" (unique per instance)
- **BUT**: If two instances run with the same ServiceName and handlers, they create the same endpoint names
  - The reply endpoint uses the instance GUID, so it's safe
  - Regular endpoints could collide if ServiceName is used inconsistently
  - **Actual risk**: Consumer group name derivation in line 240 uses just `endpointName`, which comes from handler type or explicit name
  - If two instances have same ServiceName + same handler, they'll attempt to join the same consumer group
  - The partition assignment strategy will cause message loss (competing consumers on same topic)

**Key insight from line 33-34**:
```csharp
InboundRouteKind.Subscribe => (host.ServiceName is not null ? ToKebabCase(host.ServiceName) + "." : "")
    + GetReceiveEndpointName(route.Consumer.Name, kind),
```
- If ServiceName is not set, the prefix is omitted
- Two instances with different ServiceNames won't collide
- Two instances with null/empty ServiceName will collide
- **This is the risk**: ServiceName must be set and consistent per logical service

---

## Finding #12: Reply Topic Cleanup

**Issue**: Reply topics created dynamically but no mechanism to clean them up on shutdown.

### Locations

#### 1. DefaultNamingConventions.cs:91-100 (Topic naming)
```csharp
public string GetInstanceEndpoint(Guid consumerId)
{
    if (consumerId == Guid.Empty)
    {
        throw new ArgumentException("Consumer ID cannot be empty.", nameof(consumerId));
    }

    // Use N format (no hyphens) for shorter queue names
    return $"response-{consumerId:N}";  // Format: response-32characterhexguid
}
```

#### 2. KafkaReceiveEndpointTopologyConvention.cs:30-51 (Reply topic provisioning)
```csharp
// Ensure the main topic exists.
if (topology.Topics.FirstOrDefault(t => t.Name == configuration.TopicName) is null)
{
    var topicConfig = new KafkaTopicConfiguration
    {
        Name = configuration.TopicName,
        IsTemporary = endpoint.Kind == ReceiveEndpointKind.Reply,  // <-- Marked as temporary
        AutoProvision = configuration.AutoProvision
    };

    // Reply topics get short retention for self-cleanup.
    if (endpoint.Kind == ReceiveEndpointKind.Reply)
    {
        topicConfig.TopicConfigs = new Dictionary<string, string>
        {
            ["retention.ms"] = "3600000",    // 1 hour
            ["cleanup.policy"] = "delete"
        };
    }

    topology.AddTopic(topicConfig);
}
```

**What exists**:
- Reply topics are marked as `IsTemporary = true`
- Retention is set to 1 hour (3600000 ms)
- Cleanup policy is "delete"

#### 3. KafkaMessagingTransport.cs:300-307 (Shutdown)
```csharp
public override async ValueTask DisposeAsync()
{
    // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
    if (ConnectionManager is not null)
    {
        await ConnectionManager.DisposeAsync();
    }
}
```

**What is missing**:
- No code to delete reply topics on shutdown
- No code to delete stale reply topics (older than 1 hour)
- `IsTemporary` flag is set but never used to drive cleanup logic
- KafkaConnectionManager.DisposeAsync() (lines 202-229) does NOT iterate through topics to delete them

**Shutdown sequence** (from KafkaConnectionManager.cs:202-229):
```csharp
public async ValueTask DisposeAsync()
{
    if (_isDisposed)
    {
        return;
    }

    _isDisposed = true;

    if (_producer is not null)
    {
        // Flush pending messages before disposing
        _producer.Flush(TimeSpan.FromSeconds(10));
        _producer.Dispose();
    }

    // Cancel any remaining in-flight dispatch TCS instances.
    // After Flush(10s), any TCS still pending means the delivery
    // report never arrived -- cancel to unblock callers.
    foreach (var tcs in _inflightDispatches.Keys)
    {
        tcs.TrySetCanceled();
    }

    _inflightDispatches.Clear();

    _adminClient?.Dispose();
}
```

**Problem**:
- AdminClient is disposed but no topics are deleted
- Reply topics accumulate in Kafka cluster across service restarts
- Cleanup relies on 1-hour retention, but:
  - Requires waiting 1 hour for cleanup
  - Does not guarantee immediate removal
  - Only works if retention is enabled (operator could disable it)
  - Leaves orphaned topics for long-lived services

**Gaps**:
1. No `DeleteTopicsAsync` call in shutdown path
2. No filter for `IsTemporary` topics in topology
3. No utility method in KafkaConnectionManager to delete topics

---

## Supporting Infrastructure

### Pooling Patterns Already in Codebase

#### 1. PooledArrayWriter (for byte[] buffers)
Location: `/workspaces/hc3/src/Mocha/src/Mocha.Utilities/Buffers/PooledArrayWriter.cs`
- Already used in DispatchContext.cs (line 19) and PostgresSagaStore.cs
- Used for body serialization but **not for header encoding**

#### 2. DispatchContext Pooling
Location: `/workspaces/hc3/src/Mocha/src/Mocha/Middlewares/DispatchContext.cs`
- Reused via object pooling
- Includes internal PooledArrayWriter field
- **Could be extended** to include pre-allocated header buffer

#### 3. No IValueTaskSource pooling yet
- TODO comment in KafkaDispatchEndpoint.cs:86 suggests this
- No existing pooled implementation of ManualResetValueTaskSourceCore<T> in codebase
- Would require new pooled type or adoption of external library

### Consumer Configuration

#### Defaults in KafkaConnectionManager.cs:98-106
```csharp
var config = new ConsumerConfig
{
    BootstrapServers = _bootstrapServers,
    GroupId = groupId,
    EnableAutoCommit = false,
    AutoOffsetReset = AutoOffsetReset.Earliest,
    EnablePartitionEof = false,
    MaxPollIntervalMs = 600_000 // 10 minutes
};
```

**Points of interest**:
- GroupId is set at consumer creation time (immutable per consumer)
- Each receive endpoint creates its own consumer (see KafkaReceiveEndpoint implementation)
- No shared consumer groups across endpoints

---

## Topology Provisioning Flow

```
KafkaReceiveEndpoint created
  → KafkaReceiveEndpointTopologyConvention.DiscoverTopology()
    → Creates main topic (with reply retention if Kind == Reply)
    → Creates error topic (with NO retention)
    → Creates message type topics for inbound routes

KafkaDispatchEndpoint created
  → KafkaDispatchEndpointTopologyConvention.DiscoverTopology()
    → Creates target topic
    → Creates error topic (with NO retention)

On transport startup:
  → KafkaMessagingTransport.OnBeforeStartAsync()
    → KafkaConnectionManager.ProvisionTopologyAsync()
      → Calls adminClient.CreateTopicsAsync(TopicSpecification)
      → Ignores TopicAlreadyExists errors
```

---

## Message Dispatch Flow

```
DispatchContext.DispatchAsync()
  → KafkaDispatchEndpoint.DispatchAsync()
    → SelectKey(envelope)  // Encoding.UTF8.GetBytes
    → BuildKafkaHeaders(envelope)  // 12+ Encoding.UTF8.GetBytes calls
    → new TaskCompletionSource()  // Allocation #1
    → producer.Produce(message, callback)
    → await tcs.Task  // Allocation #2
    → callback on delivery-report thread
      → tcs.TrySetResult() / TrySetException()
```

---

## Summary Table: Code Locations by Finding

| Finding | File | Line(s) | Issue |
|---------|------|---------|-------|
| #2 ReplicationFactor | KafkaTopic.cs | 21, 46 | Hardcoded `?? 1` fallback |
| #2 ReplicationFactor | KafkaDefaultTopicOptions.cs | 16, 29 | All fields nullable, no default |
| #5 TCS Allocation | KafkaDispatchEndpoint.cs | 87, 121 | `new TaskCompletionSource()` per dispatch |
| #5 TCS Allocation | KafkaConnectionManager.cs | 22, 193-199 | Tracking dict without pooling |
| #7 Header Allocations | KafkaDispatchEndpoint.cs | 178-244 | 12+ `Encoding.UTF8.GetBytes()` calls per dispatch |
| #7 Header Allocations | KafkaDispatchEndpoint.cs | 168 | Key encoding allocation |
| #10 Error Retention | KafkaDispatchEndpointTopologyConvention.cs | 35-39 | Error topic created with empty TopicConfigs |
| #10 Error Retention | KafkaReceiveEndpointTopologyConvention.cs | 59-63 | Error topic created with empty TopicConfigs |
| #11 Group Collision | DefaultNamingConventions.cs | 33-34, 37 | ServiceName used inconsistently |
| #11 Group Collision | KafkaMessagingTransport.cs | 240 | Consumer group ID = endpoint name (not instance-scoped) |
| #12 Reply Cleanup | KafkaMessagingTransport.cs | 300-307 | No deletion on shutdown |
| #12 Reply Cleanup | KafkaConnectionManager.cs | 202-229 | DisposeAsync doesn't delete topics |
