# Correctness Review: Azure Event Hub Transport Implementation Plan

**Reviewer**: Correctness Reviewer (Senior Framework Engineer)
**Date**: 2026-03-27

---

## 1. Abstract Members of MessagingTransport

### Status: PASS (with notes)

All abstract members are covered:

| Abstract Member | Plan Coverage | Notes |
|---|---|---|
| `Topology` (abstract property) | Yes -- `EventHubMessagingTopology _topology` returned via override | Correct |
| `CreateConfiguration(IMessagingSetupContext)` | Yes -- delegates to descriptor pattern | Matches RabbitMQ |
| `TryGetDispatchEndpoint(Uri, out DispatchEndpoint?)` | Yes -- three resolution paths (scheme, topology base, shorthand) | Correct |
| `CreateEndpointConfiguration(context, OutboundRoute)` | Yes -- handles Send and Publish kinds | Correct |
| `CreateEndpointConfiguration(context, Uri)` | Yes -- handles replies, `h/` paths, topology-relative, shorthand | Correct |
| `CreateEndpointConfiguration(context, InboundRoute)` | Yes -- handles Reply and Default kinds | Correct |
| `CreateReceiveEndpoint()` | Yes -- returns `new EventHubReceiveEndpoint(this)` | Correct |
| `CreateDispatchEndpoint()` | Yes -- returns `new EventHubDispatchEndpoint(this)` | Correct |

Virtual overrides:
| Virtual Member | Plan Coverage |
|---|---|
| `OnAfterInitialized(IMessagingSetupContext)` | Yes -- builds topology, creates managers |
| `OnBeforeStartAsync(context, ct)` | Yes -- ensures producer ready |
| `OnBeforeStopAsync(ct)` | Yes -- stops all consumers |
| `DisposeAsync()` | Yes -- disposes consumer + connection managers |
| `Describe()` | Mentioned but not fully detailed (says "follow RabbitMQ pattern") |

---

## 2. CreateEndpointConfiguration Overloads

### Status: PASS

All 3 overloads are present and correct:

1. **OutboundRoute overload**: Handles `Send` and `Publish` route kinds. Uses `context.Naming.GetSendEndpointName` / `GetPublishEndpointName`. Names prefixed with `h/`. Matches RabbitMQ `e/` pattern.

2. **Uri overload**: Handles 4 URI forms:
   - `eventhub:///replies` -- reply dispatch endpoint
   - `eventhub:///h/{hub-name}` -- scheme-relative
   - `eventhub://{namespace}/h/{hub-name}` -- topology-relative
   - `hub://{hub-name}` -- shorthand

3. **InboundRoute overload**: Handles `Reply` (with `IsTemporary = true`, `Kind = Reply`, `ReplyReceiveMiddleware`) and Default routes.

---

## 3. Lifecycle Correctness

### Status: PASS (with one blocking issue)

The transport lifecycle is `Initialize -> DiscoverEndpoints -> Complete -> Start`.

- **Initialize**: `CreateConfiguration` is called by the base class, `OnAfterInitialized` builds topology and connection managers. Correct.
- **DiscoverEndpoints**: Handled by base class. The plan correctly implements `CreateEndpointConfiguration` overloads that the base class calls during discovery.
- **Complete**: Handled by base class, calls `OnComplete` on each endpoint. Plan implements `OnComplete` on both endpoint types.
- **Start**: `OnBeforeStartAsync` ensures producer ready. Receive endpoints' `OnStartAsync` registers consumers. Correct.
- **Stop**: `OnBeforeStopAsync` stops all consumers. Receive endpoints' `OnStopAsync` disposes individual consumers. Correct.

### BLOCKING ISSUE: Double consumer stop

The plan has `OnBeforeStopAsync` calling `ConsumerManager.StopAllAsync()` AND each `EventHubReceiveEndpoint.OnStopAsync` calling `_consumer.DisposeAsync()`. Looking at the base `MessagingTransport.StopAsync`:

```csharp
await OnBeforeStopAsync(cancellationToken);
foreach (var endpoint in ReceiveEndpoints)
{
    await endpoint.StopAsync(context, cancellationToken);
}
```

This means `StopAllAsync()` is called first (which stops all partition readers in all registered consumers), and then each endpoint's `OnStopAsync` disposes the same consumer again. The `RegisteredConsumer.DisposeAsync()` calls `StopAsync()` internally, so this is a double-stop on every consumer. While it may not crash (the cancellation token source handles it), it's wasteful and could cause `Task.WhenAll` to throw on already-completed tasks.

**Fix**: Remove `ConsumerManager.StopAllAsync()` from `OnBeforeStopAsync`. Each endpoint's `OnStopAsync` already handles its own consumer cleanup. Alternatively, keep `StopAllAsync` and make `OnStopAsync` a no-op (but the per-endpoint pattern is more consistent with RabbitMQ).

---

## 4. Receive Endpoint ExecuteAsync Pattern

### Status: PASS

The receive endpoint correctly uses `ExecuteAsync` with a static configure callback:

```csharp
ExecuteAsync(
    static (context, state) =>
    {
        var feature = context.Features.GetOrSet<EventHubReceiveFeature>();
        feature.EventData = state.eventData;
        feature.PartitionId = state.partitionId;
    },
    (eventData, partitionId),
    ct)
```

This exactly matches the RabbitMQ pattern. The `static` lambda avoids closure allocation. State is passed as a value tuple. Feature is set before the pipeline runs.

---

## 5. Dispatch Endpoint DispatchAsync Override

### Status: PASS (with non-blocking notes)

The dispatch endpoint correctly overrides `DispatchAsync(IDispatchContext context)`:
- Extracts the envelope
- Resolves hub name (dynamic for Reply, static from Topic for Default)
- Gets producer from ConnectionManager
- Builds EventData with zero-copy body
- Maps AMQP structured properties (no dictionary allocation)
- Maps overflow headers to ApplicationProperties
- Sends with partition key strategy

**Non-blocking note**: The RabbitMQ dispatch endpoint has `EnsureProvisionedAsync` for lazy provisioning. The plan doesn't have this since auto-provisioning is deferred. This is fine for now but should be noted as a future consideration.

---

## 6. Topology Model Completeness

### Status: PASS

Two entity types:
1. **EventHubTopic** (`TopologyResource<EventHubTopicConfiguration>`) -- represents an Event Hub entity. Has Name, PartitionCount, AutoProvision. Address: `eventhub://{namespace}/h/{hub-name}`.
2. **EventHubSubscription** (`TopologyResource<EventHubSubscriptionConfiguration>`) -- represents a consumer group. Has TopicName, ConsumerGroup, AutoProvision. Address: `eventhub://{namespace}/h/{hub-name}/cg/{consumer-group}`.

This correctly models Event Hub's two-tier resource model. RabbitMQ has 3 (Exchange, Queue, Binding); Event Hub has 2 (Topic/Hub, ConsumerGroup). The subscription model is simpler because Event Hub doesn't have explicit bindings between topics.

The `EventHubMessagingTopology` extends `MessagingTopology<EventHubMessagingTransport>`. Correct.

---

## 7. DI Registration Pattern

### Status: PASS

```csharp
public static IMessageBusHostBuilder AddEventHub(
    this IMessageBusHostBuilder busBuilder,
    Action<IEventHubMessagingTransportDescriptor> configure)
{
    var transport = new EventHubMessagingTransport(x => configure(x.AddDefaults()));
    busBuilder.ConfigureMessageBus(b => b.AddTransport(transport));
    return busBuilder;
}
```

Exactly matches the RabbitMQ `AddRabbitMQ` pattern:
- `x.AddDefaults()` wraps the user's configure delegate to inject default conventions and middleware
- Transport is added via `ConfigureMessageBus(b => b.AddTransport(transport))`
- Parameterless overload delegates to the configured one with empty action

---

## 8. Naming Conventions

### Status: PASS

- Transport name: `"eventhub"` (constant `DefaultName`)
- Schema: `"eventhub"` (constant `DefaultSchema`)
- Path prefix: `h/` for hub (analogous to `e/` for exchange, `q/` for queue in RabbitMQ)
- Endpoint names: `"h/" + hubName` for dispatch, hub name for receive
- Reply endpoint: `"Replies"` (matches RabbitMQ)
- Shorthand scheme: `hub://` (analogous to `queue://` and `exchange://`)
- Error/skipped URIs: `{schema}:h/{name}` (matches RabbitMQ's `{schema}:q/{name}`)

---

## 9. Error Handling

### Status: PASS (with non-blocking concern)

**Connection failures**:
- Azure SDK handles transient AMQP failures internally via `EventHubsRetryOptions`
- Producer is singleton per hub, thread-safe
- Consumer partition reader has catch block for non-cancellation exceptions with logging

**Message processing failures**:
- `ExecuteAsync` in the base `ReceiveEndpoint` catches all exceptions at the top level
- Error/skipped endpoints are configured via default convention
- Acknowledgement middleware is a pass-through (correct for Event Hubs -- no per-message ack)

**Non-blocking concern**: The `ReadPartitionAsync` catch block has a TODO for reconnection/backoff. In production, an unrecoverable consumer failure (e.g., consumer group revoked, hub deleted) will silently stop processing for that partition. A reconnection strategy should be implemented in Phase 5 at minimum, but this is noted in the plan.

---

## 10. Test Plan Coverage

### Status: PASS (adequate)

Behavior tests adapted from RabbitMQ (13 test categories):
- Send, PublishSubscribe, RequestReply, Batching, FaultHandling, ConcurrencyLimiter, Concurrency, ErrorQueue, CustomHeaders, BusDefaults, AutoProvision, TransportMiddleware, EndpointMiddleware

Transport-specific tests (4 categories):
- PartitionRouting, ConsumerGroupIsolation, TopologyModel, EnvelopeParser round-trip

Fixture design with Docker emulator (Option A) and real Azure (Option B).

**Non-blocking note**: Missing explicit test for the `hub://` shorthand URI resolution in `CreateEndpointConfiguration`. Consider adding a URI resolution unit test.

---

## 11. Configuration Classes, Descriptor Interfaces, Convention Interfaces

### Status: PASS

**Configuration classes**:
- `EventHubTransportConfiguration` extends `MessagingTransportConfiguration` -- has ConnectionProvider, ConnectionString, FullyQualifiedNamespace, Topics, Subscriptions, AutoProvision, Defaults
- `EventHubReceiveEndpointConfiguration` -- has HubName, ConsumerGroup, AutoProvision
- `EventHubDispatchEndpointConfiguration` -- has HubName
- `EventHubBusDefaults` -- bus-level defaults

**Descriptor interfaces**:
- `IEventHubMessagingTransportDescriptor` extends `IMessagingTransportDescriptor, IMessagingDescriptor<EventHubTransportConfiguration>` -- includes all `new` overloads from base + EventHub-specific methods (ConnectionString, Namespace, ConnectionProvider, AutoProvision, ConfigureDefaults, Endpoint, DispatchEndpoint, DeclareTopic, DeclareSubscription)
- `IEventHubReceiveEndpointDescriptor` / `IEventHubDispatchEndpointDescriptor` -- endpoint-level descriptors
- Topology descriptors: `IEventHubTopicDescriptor`, `IEventHubSubscriptionDescriptor`

**Convention interfaces**:
- `IEventHubReceiveEndpointConfigurationConvention` -- mirrors `IRabbitMQReceiveEndpointConfigurationConvention` pattern (filters on config/transport type, delegates to typed Configure)
- `IEventHubReceiveEndpointTopologyConvention` -- mirrors `IRabbitMQReceiveEndpointTopologyConvention`
- `IEventHubDispatchEndpointTopologyConvention` -- mirrors `IRabbitMQDispatchEndpointTopologyConvention`

All accounted for.

---

## 12. Middleware Ordering

### Status: PASS

```csharp
descriptor.UseReceive(
    EventHubReceiveMiddlewares.Acknowledgement,
    after: ReceiveMiddlewares.ConcurrencyLimiter.Key);
descriptor.UseReceive(
    EventHubReceiveMiddlewares.Parsing,
    after: EventHubReceiveMiddlewares.Acknowledgement.Key);
```

Order: `ConcurrencyLimiter -> Acknowledgement -> Parsing -> [pipeline]`

This matches the RabbitMQ ordering conceptually: acknowledgement wraps parsing so that if parsing or downstream processing fails, the acknowledgement middleware can handle it. For Event Hubs the acknowledgement is a no-op pass-through (correct -- no per-message ack), but the ordering is still correct for when checkpoint support is added later.

---

## Issues Summary

### Blocking Issues

1. **Double consumer stop in shutdown path**: `OnBeforeStopAsync` calls `ConsumerManager.StopAllAsync()` and then each endpoint's `OnStopAsync` disposes the same consumer. Remove one of the two stop paths. Recommended: remove `StopAllAsync()` from `OnBeforeStopAsync` and let each endpoint manage its own consumer, consistent with RabbitMQ.

### Non-Blocking Issues

1. **`Describe()` not fully specified**: The plan says "follow RabbitMQ pattern" but doesn't show the implementation. Should show the full override to avoid ambiguity during implementation.

2. **Partition reader reconnection**: `ReadPartitionAsync` has a TODO for reconnection/backoff. Acceptable for Phase 1, but should be tracked.

3. **Missing `hub://` URI test**: No explicit unit test for the shorthand URI resolution.

4. **`EventHubConnectionManager.EnsureProducerReadyAsync` is a stub**: The method body just has a comment. Should either be implemented (e.g., `GetEventHubPropertiesAsync`) or explicitly documented as no-op for Phase 1.

5. **`RegisteredConsumer.StartAsync` does not await the read tasks**: The method creates tasks and assigns them to `_readTasks` but does not await them. This is intentional (fire-and-forget read loops) but the `CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)` links to the start-time token which may be cancelled after startup. Should use a standalone CTS that is cancelled only during `StopAsync`.

---

## Suggestions

1. Consider making `EventHubConnectionManager` resolve producers lazily in `DispatchAsync` rather than pre-creating in `OnBeforeStartAsync`. The `GetOrCreateProducer` already supports this. `EnsureProducerReadyAsync` adds little value.

2. The `EventHubDispatchEndpointTopologyConvention` is simpler than RabbitMQ's because Event Hubs doesn't have bindings. Consider whether the dispatch topology convention needs to handle any route-to-topic mapping for custom dispatch endpoints (similar to how RabbitMQ binds custom exchanges to convention exchanges). The current implementation only ensures the topic exists, which may be sufficient.

3. For the `EventHubReceiveEndpointTopologyConvention`: the plan creates topics for both `publishHubName` and `sendHubName` per route. In Event Hubs without bindings, messages sent to a "publish" hub won't automatically arrive at a "send" hub. Consider whether the Event Hub topology actually needs separate publish/send hubs or whether they should be the same hub. This is a design question that may need clarification from the domain owner.

---

## Verdict: REVISE

Fix the blocking issue (double consumer stop) before proceeding to implementation. The non-blocking issues can be addressed during implementation.
