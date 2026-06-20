# Queue-as-Flat-Composition-Descriptor: Executable Workflow Plan

`t.Queue(name)` stops returning `I*QueueEndpointDescriptor : I*ReceiveEndpointDescriptor` (infra piled onto a routing type). It becomes its own type `I*QueueBuilder` that COMPOSES a separate infra descriptor (`I*QueueDescriptor` from `DeclareQueue`) and a lazily-created receive endpoint, delegating each flat method to exactly one of them. Infra-only queues (`Queue("audit").Durable()` with no routing method) never materialize an endpoint. All reasoning ran on Opus 4.6 1M.

## Locked Decisions

### D1. Lazy-endpoint mechanism

The builder holds `_transport` (the concrete `*MessagingTransportDescriptor`) and `_name`. Every routing method calls `EnsureEndpoint()` which does `_endpoint ??= _transport.Endpoint(_name)`. Because `Endpoint(name)` is idempotent get-or-create on `_receiveEndpoints`, `Queue("x").Consumer<A>()` then `Endpoint("x").Consumer<B>()` resolve to one endpoint instance. No `Lazy<T>`, descriptors are single-threaded config-time objects.

The builder stores the endpoint as the concrete type (e.g., `RabbitMQReceiveEndpointDescriptor`) internally, not the interface. This is necessary for satellite sugar methods to write directly to `Configuration.ErrorQueue`/`Configuration.SkippedQueue` without going through interface methods that will be deleted.

### D2. Satellite home

Builder sugar: `ErrorQueue(string)`, `DisableErrorQueue()`, `SkippedQueue(string)`, `DisableSkippedQueue()` live on `I*QueueBuilder` (RabbitMQ and Postgres only; InMemory has no satellite config). They call `EnsureEndpoint()` and write directly to `RabbitMQSatelliteConfiguration` fields on the endpoint's `Configuration` object.

**STAYS:** `RabbitMQSatelliteConfiguration`, `PostgresSatelliteConfiguration`, `ErrorQueue`/`SkippedQueue` properties on `*ReceiveEndpointConfiguration`, `*DefaultReceiveEndpointConvention`, `TryGetSatelliteAutoProvision` (RabbitMQ only).

**DELETED from `I*ReceiveEndpointDescriptor`:** `ErrorQueue`, `DisableErrorQueue`, `SkippedQueue`, `DisableSkippedQueue` (moved to builder only).

**DELETED from `RabbitMQReceiveEndpointConfiguration`:** `QueueDurable`, `QueueAutoProvision`, `QueueArguments` (the "second home" queue-shape fields).

### D3. Quorum placement

`Quorum()` lives on `IRabbitMQQueueBuilder` only, not on `IRabbitMQQueueDescriptor`. It delegates to `_queue.WithArgument("x-queue-type", RabbitMQQueueType.Quorum)`.

### D4. QueueName handling

`QueueName` stays on `*ReceiveEndpointConfiguration`, set once in the ctor to the same value as `Name`. After deleting `Queue(string)` from `I*ReceiveEndpointDescriptor`, no descriptor-level code can make `QueueName != Name`. The only `Name != QueueName` case is the reply endpoint (RabbitMQ), built directly at the config layer in `RabbitMQMessagingTransport`. The `QueueName.EqualsOrdinal(name)` drift arm in `Endpoint()` is deleted.

### D5. Builder member lists

**RabbitMQ `IRabbitMQQueueBuilder`:**
- Infra group (delegates to `_queue: IRabbitMQQueueDescriptor`): `Durable(bool)`, `Quorum()`, `WithArgument(string, object)`, `AutoProvision(bool)`.
- Routing group (delegates to lazily-created endpoint via `EnsureEndpoint()`): `Consumer<T>()`, `Consumer(Type)`, `Handler<T>()`, `Handler(Type)`, `Receives<T>()`, `Receives<T>(Action<IReceiveTypeBindDescriptor>)`, `Receives(Type)`, `AutoBind(bool)`, `MaxPrefetch(ushort)`, `Kind(ReceiveEndpointKind)`, `MaxConcurrency(int)`, `UseReceive(...)`, `FaultEndpoint(string)`, `SkippedEndpoint(string)`.
- Satellite sugar (routing group, calls `EnsureEndpoint()`): `ErrorQueue(string)`, `DisableErrorQueue()`, `SkippedQueue(string)`, `DisableSkippedQueue()`.
- BindFrom: see D7.

**Postgres `IPostgresQueueBuilder`:**
- Infra group: `AutoProvision(bool)`, `AutoDelete(bool)`.
- Routing group: same as RabbitMQ except `MaxBatchSize(int)` instead of `MaxPrefetch(ushort)`, no BindFrom.
- Satellite sugar: `ErrorQueue(string)`, `DisableErrorQueue()`, `SkippedQueue(string)`, `DisableSkippedQueue()`.

**InMemory `IInMemoryQueueBuilder`:**
- Infra group: **empty** (no broker-shape properties).
- Routing group: `Consumer`, `Handler`, `Receives`, `AutoBind`, `MaxConcurrency`, `Kind`, `UseReceive`, `FaultEndpoint`, `SkippedEndpoint`. No `MaxPrefetch`, no `MaxBatchSize`.
- Satellite sugar: **none** (InMemory has no satellite config).
- BindFrom: see D7.

### D6. Naming

`IRabbitMQQueueBuilder` / `RabbitMQQueueBuilder` (not `*Descriptor`). Same pattern for Postgres and InMemory. The type is a composition facade, not a descriptor.

### D7. BindFrom classification

BindFrom is an **infra-group method** on the builder. It delegates directly to `DeclareExchange` + `DeclareBinding` (RabbitMQ) or `DeclareTopic` + `DeclareSubscription` (Postgres/InMemory) on the transport, WITHOUT calling `EnsureEndpoint()`. This prevents entity-only queues with `BindFrom` from materializing phantom endpoints. The binding intent is lowered to topology entities immediately, matching what `LowerEntityOnlyQueue` did today.

For endpoints that DO have consumers, the receive-endpoint lifecycle's `OnDiscoverTopology` also creates bindings from `QueueBindFroms`. These merge correctly via the topology's idempotent add. But the builder's BindFrom bypasses the endpoint config's `QueueBindFroms` list entirely, writing directly to topology.

### D8. Entity-only validation

If a builder calls `EnsureEndpoint()` but the endpoint ends up with no consumers and no `ReceivedMessageTypes` (e.g., `Queue("x").ErrorQueue("err")` without a consumer), `CreateConfiguration` validates this and throws `SatelliteRequiresConsumingEndpoint`. This preserves the existing guard. The check: iterate builders, if `_endpoint` is not null and `IsEntityOnly(endpoint.Configuration)` and the endpoint has satellite config or ErrorEndpoint/SkippedEndpoint URIs set, throw.

## Phases

The strategy is **vertical-slice per transport**: complete RabbitMQ first (src + tests green), then mirror to Postgres, then InMemory. Within each transport, the builder is added, the transport descriptor is re-pointed, the conflated tier is deleted, and the receive endpoint is slimmed, all atomically. This avoids transient compile breaks.

### Phase 1: RabbitMQ -- Add Builder + Re-point + Delete Conflated Tier + Slim Endpoint

**Goal:** Add `IRabbitMQQueueBuilder`/`RabbitMQQueueBuilder`, change `Queue()` return type, delete `IRabbitMQQueueEndpointDescriptor`/`RabbitMQQueueEndpointDescriptor`, remove `Queue(string)`/`ErrorQueue`/`DisableErrorQueue`/`SkippedQueue`/`DisableSkippedQueue` from `IRabbitMQReceiveEndpointDescriptor`, remove `PinQueueIdentity` machinery, remove `QueueDurable`/`QueueAutoProvision`/`QueueArguments` from config, kill drift arm, rewrite `CreateConfiguration`, delete `IsEntityOnly`/`LowerEntityOnlyQueue`. Migrate all RabbitMQ tests. Build + tests green.

#### Sub-phase 1a: Source edits (sequential, compile-order-sensitive)

**NEW files (can be written in PARALLEL):**

1. `src/Mocha/src/Mocha.Transport.RabbitMQ/Descriptors/IRabbitMQQueueBuilder.cs`
   - Public interface per D5 member list above. All fluent returns are `IRabbitMQQueueBuilder`.

2. `src/Mocha/src/Mocha.Transport.RabbitMQ/Descriptors/RabbitMQQueueBuilder.cs`
   - `internal sealed class RabbitMQQueueBuilder : IRabbitMQQueueBuilder`
   - Fields: `_transport` (`RabbitMQMessagingTransportDescriptor`), `_queue` (`IRabbitMQQueueDescriptor`), `_name` (`string`), `_endpoint` (`RabbitMQReceiveEndpointDescriptor?`).
   - Constructor: `(RabbitMQMessagingTransportDescriptor transport, string name)`. Body: `_transport = transport; _name = name; _queue = transport.DeclareQueue(name);`.
   - `private RabbitMQReceiveEndpointDescriptor EnsureEndpoint() => _endpoint ??= (RabbitMQReceiveEndpointDescriptor)_transport.Endpoint(_name);`
   - Internal accessor: `internal RabbitMQReceiveEndpointDescriptor? Endpoint => _endpoint;`
   - Infra methods delegate to `_queue`.
   - `Quorum()` delegates to `_queue.WithArgument("x-queue-type", RabbitMQQueueType.Quorum)`.
   - BindFrom: calls `_transport.DeclareExchange(exchangeName)` + `_transport.DeclareBinding(exchangeName, _name)` directly (resolves URI to exchange name using the same pattern as `LowerEntityOnlyQueue`). Does NOT call `EnsureEndpoint()`.
   - Routing methods call `EnsureEndpoint()` then delegate.
   - Satellite sugar calls `EnsureEndpoint()` then writes directly to `_endpoint.Configuration.ErrorQueue`/`.SkippedQueue`.

**DELETE files (AFTER the new files exist, BEFORE slimming interfaces):**

3. DELETE `src/Mocha/src/Mocha.Transport.RabbitMQ/Descriptors/IRabbitMQQueueEndpointDescriptor.cs`
4. DELETE `src/Mocha/src/Mocha.Transport.RabbitMQ/Descriptors/RabbitMQQueueEndpointDescriptor.cs`

   Rationale: these must be deleted BEFORE removing members from `IRabbitMQReceiveEndpointDescriptor` because `RabbitMQQueueEndpointDescriptor` has explicit interface implementations (`IRabbitMQReceiveEndpointDescriptor.ErrorQueue`, `.DisableErrorQueue`, `.SkippedQueue`, `.DisableSkippedQueue`, `.Queue`) that would cause CS0539 if the interface members are removed while the impl still exists.

**MODIFY files (sequential after deletes):**

5. `src/Mocha/src/Mocha.Transport.RabbitMQ/Descriptors/IRabbitMQMessagingTransportDescriptor.cs`
   - Line 134: change `IRabbitMQQueueEndpointDescriptor Queue(string name)` to `IRabbitMQQueueBuilder Queue(string name)`.
   - Line 145: change `Action<IRabbitMQQueueEndpointDescriptor>` to `Action<IRabbitMQQueueBuilder>`.
   - Update XML docs (remove "merges onto endpoint" wording, describe builder composition).

6. `src/Mocha/src/Mocha.Transport.RabbitMQ/Descriptors/IRabbitMQReceiveEndpointDescriptor.cs`
   - Delete `ErrorQueue(string)` (line 45), `DisableErrorQueue()` (line 52), `SkippedQueue(string)` (line 60), `DisableSkippedQueue()` (line 67), `Queue(string)` (line 84).
   - Keep: `MaxPrefetch` (line 92), `FaultEndpoint` (line 73), `SkippedEndpoint` (line 76), all routing methods.

7. `src/Mocha/src/Mocha.Transport.RabbitMQ/Descriptors/RabbitMQReceiveEndpointDescriptor.cs`
   - Delete `_queueIdentityPinned` field (line 10), `IsQueueIdentityPinned` (line 22), `PinQueueIdentity()` (lines 28-31).
   - Delete `Queue(string)` impl (lines 135-145).
   - Delete `ErrorQueue(string)` impl (lines 156-161), `DisableErrorQueue()` (lines 164-168), `SkippedQueue(string)` (lines 172-177), `DisableSkippedQueue()` (lines 180-185).

8. `src/Mocha/src/Mocha.Transport.RabbitMQ/Configurations/RabbitMQReceiveEndpointConfiguration.cs`
   - Delete `QueueDurable` (line 32), `QueueAutoProvision` (line 37), `QueueArguments` (line 42).
   - KEEP `ErrorQueue` (line 22), `SkippedQueue` (line 27), `QueueName` (line 11), `MaxPrefetch` (line 17).

9. `src/Mocha/src/Mocha.Transport.RabbitMQ/Descriptors/RabbitMQMessagingTransportDescriptor.cs`
   - Lines 15-16: replace `Dictionary<string, RabbitMQQueueEndpointDescriptor> _queueEndpoints` with `Dictionary<string, RabbitMQQueueBuilder> _queueBuilders`.
   - Lines 162-163: delete `|| e.Extend().Configuration.QueueName.EqualsOrdinal(name)` from `Endpoint()`.
   - Lines 221-242: rewrite `Queue(string name)`:
     ```csharp
     public IRabbitMQQueueBuilder Queue(string name)
     {
         if (_queueBuilders.TryGetValue(name, out var existing))
         {
             return existing;
         }
         var builder = new RabbitMQQueueBuilder(this, name);
         _queueBuilders[name] = builder;
         return builder;
     }
     ```
   - Lines 245-250: change param type to `Action<IRabbitMQQueueBuilder>`.
   - Lines 256-298: rewrite `CreateConfiguration()`:
     - Remove entity-only partitioning loop (lines 262-277). Remove `entityOnly` HashSet.
     - Replace with: iterate `_queueBuilders.Values`. For each builder where `builder.Endpoint` is null, skip (infra-only, queue already in `_queues` via DeclareQueue). For each builder where `builder.Endpoint` is not null, check `IsEntityOnly(builder.Endpoint.Configuration)`. If entity-only AND has satellite config set, throw `SatelliteRequiresConsumingEndpoint`. If entity-only without satellite, skip (the endpoint should not enter the lifecycle; remove it from `_receiveEndpoints`). If not entity-only, leave the endpoint in `_receiveEndpoints`.
     - Keep `ValidateOneEndpointPerQueue` on the filtered `_receiveEndpoints`.
   - Lines 300-367: delete `IsEntityOnly` and `LowerEntityOnlyQueue` methods entirely. `IsEntityOnly` logic inlines into the `CreateConfiguration` rewrite above.

10. `src/Mocha/src/Mocha/ThrowHelper.cs`
    - Delete `QueueIdentityPinned` (lines 205-208).
    - KEEP `SatelliteRequiresConsumingEndpoint` (lines 199-203), still used by CreateConfiguration validation.
    - KEEP `TwoReceiveEndpointsShareOneQueue` (lines 193-197).

#### Sub-phase 1b: Test migration (can start in PARALLEL once 1a compiles)

**Migration patterns:**

| Old pattern | New pattern |
|---|---|
| `t.Queue("x").Consumer<A>()` | Same call, return type is now `IRabbitMQQueueBuilder` (API-compatible fluent chain) |
| `t.Endpoint("a").Queue("x")` | `t.Queue("x")` (queue name = endpoint name, drift concept is gone) |
| `t.Endpoint("a").Queue("x").Consumer<A>()` | `t.Queue("x").Consumer<A>()` |
| `t.Queue("x").ErrorQueue("err")` | Same call on builder |
| `t.Queue("x").Durable()` | Same call on builder |
| `t.Queue("x").Quorum()` | Same call on builder |
| `t.Queue("x").AutoProvision()` | Same call on builder |
| `t.Handler<T>().ConfigureEndpoint(e => e.Queue("x"))` | Remove `.Queue("x")` call (QueueName already = Name from ctor); or rewrite to `t.Queue("x").Handler<T>()` if the queue name differs from convention name |
| Variables typed `IRabbitMQQueueEndpointDescriptor` | Change to `IRabbitMQQueueBuilder` or `var` |
| Tests asserting `QueueIdentityPinned` throw | Delete |
| Tests asserting entity-only lowering output | Rewrite or delete |

**RabbitMQ test files (19 files):**

1. `Descriptors/RabbitMQUnifiedQueueTests.cs` -- heavy rewrite; references `IRabbitMQQueueEndpointDescriptor`, tests PinQueueIdentity throw
2. `Descriptors/RabbitMQQueueFrontDoorTests.cs` -- delete or rewrite; tests QueueName drift arm
3. `Descriptors/RabbitMQQueueFrontDoorLoweringTests.cs` -- delete or rewrite; tests entity-only lowering
4. `Descriptors/RabbitMQDescriptorTests.cs` -- update ErrorQueue/SkippedQueue tests to use builder
5. `Descriptors/RabbitMQHandlerBindingTests.cs` -- update Queue front door usage
6. `RabbitMQReceiveEndpointTests.cs` -- remove `.Queue("name")` chains on endpoints
7. `RabbitMQSatelliteTests.cs` -- update `.Queue("name")` chains to use builder
8. `RabbitMQEndpointQueueOwnershipTests.cs` -- update `.Queue("name")` chains
9. `RabbitMQReceiveEndpointBindFromTests.cs` -- update `.Queue("name")` chains
10. `Topology/RabbitMQExplicitTopologyTests.cs` -- update `.Queue("name")` chains; note test at ~line 150 uses `DeclareQueue + Endpoint.Queue` pattern, merge into `Queue("x").AutoProvision(true).Consumer<T>()`
11. `Topology/RabbitMQMessageTypeExtensionTests.cs` -- update `.Queue("name")` at line 88
12. `Behaviors/ExplicitTopologyTests.cs` -- update `.Queue("name")` chains
13. `Behaviors/UnifiedQueueBehaviorTests.cs` -- update `IRabbitMQQueueEndpointDescriptor` references
14. `Behaviors/AutoProvisionIntegrationTests.cs` -- update `.Queue()` chains (3 calls)
15. `Behaviors/BusDefaultsIntegrationTests.cs` -- update `.Queue()` chains (2 calls)
16. `Behaviors/ErrorQueueTests.cs` -- update `.Queue()` chains (3 calls)
17. `Behaviors/RoutingKeyTests.cs` -- update `.Queue()` chains (7 calls)
18. `Behaviors/TransportMiddlewareTests.cs` -- update `.Queue()` chains (3 calls)
19. `Routing/RabbitMQReceiveTopologyConventionTests.cs` -- update `.Queue()` chains

**Example file:**
- `src/Mocha/src/Examples/Transports/RabbitMQ/RabbitMQ.cs:38` -- `.Queue("orders.processing")` chain compiles unchanged (return type change is transparent for fluent chains)

#### Sub-phase 1c: Snapshot handling

- Delete snapshots for deleted tests (QueueFrontDoor drift, PinQueueIdentity).
- Run tests, copy `__mismatch__/` to `__snapshots__/` after verifying correctness.
- Snapshots affected by entity-only path change: `RabbitMQQueueFrontDoorLoweringTests.*` (delete if test deleted).
- Satellite snapshots: should be byte-identical (satellite mechanism unchanged).

#### Compile gate
```bash
dotnet build src/Mocha/src/Mocha.Transport.RabbitMQ
dotnet build src/Mocha/test/Mocha.Transport.RabbitMQ.Tests
```

#### Test gate
```bash
dotnet test src/Mocha/test/Mocha.Transport.RabbitMQ.Tests
```

#### Orchestration
- Agent 1: Write IRabbitMQQueueBuilder.cs + RabbitMQQueueBuilder.cs (parallel new files).
- Agent 2: Delete conflated tier files, modify IRabbitMQMessagingTransportDescriptor.cs + RabbitMQMessagingTransportDescriptor.cs + IRabbitMQReceiveEndpointDescriptor.cs + RabbitMQReceiveEndpointDescriptor.cs + RabbitMQReceiveEndpointConfiguration.cs + ThrowHelper.cs (sequential, depends on Agent 1).
- Agent 3-5: Test migration batches (parallel, depends on Agent 2): Descriptors group, Behaviors group, root+Topology+Routing group.
- Agent 6: Snapshot cleanup (sequential after tests run).

---

### Phase 2: Postgres -- Mirror of Phase 1

**Goal:** Replicate the exact pattern for Postgres. Add `IPostgresQueueBuilder`/`PostgresQueueBuilder`, delete `IPostgresQueueEndpointDescriptor`/`PostgresQueueEndpointDescriptor`, slim `IPostgresReceiveEndpointDescriptor`, migrate tests. Build + tests green.

#### Source edits

**NEW files (PARALLEL):**
- `src/Mocha/src/Mocha.Transport.Postgres/Descriptors/IPostgresQueueBuilder.cs` -- per D5 Postgres member list.
- `src/Mocha/src/Mocha.Transport.Postgres/Descriptors/PostgresQueueBuilder.cs` -- same lazy pattern; constructor calls `_transport.DeclareQueue(name)`. BindFrom calls `_transport.DeclareTopic(topicName)` + `_transport.DeclareSubscription(topicName, _name)` directly (resolves URI to topic name).

**DELETE files:**
- `src/Mocha/src/Mocha.Transport.Postgres/Descriptors/IPostgresQueueEndpointDescriptor.cs`
- `src/Mocha/src/Mocha.Transport.Postgres/Descriptors/PostgresQueueEndpointDescriptor.cs`

**MODIFY files:**
- `IPostgresMessagingTransportDescriptor.cs`: change Queue() return/param types (lines 124, 135).
- `PostgresMessagingTransportDescriptor.cs`: replace `_queueEndpoints` (line 20-21) with `_queueBuilders`, rewrite `Queue()` (lines 237-266), delete QueueName drift arm in `Endpoint()` (line 168), rewrite `CreateConfiguration()` (lines 273-315), delete `IsEntityOnly`/`LowerEntityOnlyQueue`/`ValidateOneEndpointPerQueue` -- same structural changes as RabbitMQ.
- `IPostgresReceiveEndpointDescriptor.cs`: delete `Queue(string)` (line 83), `ErrorQueue` (line 45), `DisableErrorQueue` (line 52), `SkippedQueue` (line 60), `DisableSkippedQueue` (line 67).
- `PostgresReceiveEndpointDescriptor.cs`: delete `PinQueueIdentity` machinery, `Queue(string)`, satellite methods.
- `PostgresReceiveEndpointConfiguration.cs`: NO `QueueDurable`/`QueueAutoProvision`/`QueueArguments` to delete (Postgres never had these). KEEP `ErrorQueue`, `SkippedQueue`.

**Structural differences from RabbitMQ:**
- No `QueueDurable`/`QueueArguments`/`QueueAutoProvision` fields on config (Postgres never had them).
- `PostgresSatelliteConfiguration` has only `QueueName` and `IsDisabled` (no `AutoProvision`).
- No `TryGetSatelliteAutoProvision` in Postgres transport.
- Builder infra group: `AutoProvision(bool)`, `AutoDelete(bool)` only.
- `MaxBatchSize(int)` replaces `MaxPrefetch(ushort)`.

#### Test migration (17 files)

1. `Descriptors/PostgresUnifiedQueueTests.cs` -- heavy rewrite; references `IPostgresQueueEndpointDescriptor`
2. `Descriptors/PostgresDescriptorTests.cs` -- update ErrorQueue/SkippedQueue tests
3. `Descriptors/PostgresHandlerBindingTests.cs` -- update Queue front door usage
4. `Descriptors/PostgresTopologyDescriptorTests.cs` -- update `.Queue()` chains
5. `PostgresReceiveEndpointTests.cs` -- remove `.Queue("name")` chains
6. `PostgresSatelliteTests.cs` -- update satellite tests to use builder
7. `PostgresReceiveEndpointBindFromTests.cs` -- update `.Queue("name")` chains
8. `PostgresHandlerClaimTests.cs` -- line 33: `ConfigureEndpoint(e => e.Queue("custom-handler-queue"))` must migrate. Remove `.Queue()` call or rewrite to `t.Queue("custom-handler-queue").Handler<T>()`
9. `PostgresBusDefaultsTests.cs` -- line 283: `.Queue("custom-q")` chain
10. `Conventions/PostgresDefaultConventionTests.cs` -- line 96: `.Queue("my-q")` chain
11. `Conventions/PostgresReceiveEndpointTopologyConventionTests.cs` -- update `.Queue()` chains
12. `Behaviors/AutoProvisionIntegrationTests.cs` -- update `.Queue()` chains
13. `Behaviors/BusDefaultsIntegrationTests.cs` -- update `.Queue()` chains
14. `Behaviors/EndpointMiddlewareTests.cs` -- update `.Queue()` chains
15. `Behaviors/ErrorQueueTests.cs` -- update `.Queue()` chains
16. `Behaviors/ExplicitTopologyTests.cs` -- update `.Queue()` chains
17. `Behaviors/TransportMiddlewareTests.cs` -- update `.Queue()` chains

**Benchmark file:**
- `benchmarks/PostgresBenchmark/PostgresBenchmark.cs:78` -- change `t.Endpoint("bench-cmd-ep").Queue("bench-cmd").Handler<BenchmarkCommandHandler>()` to `t.Queue("bench-cmd").Handler<BenchmarkCommandHandler>()`

#### Compile gate
```bash
dotnet build src/Mocha/src/Mocha.Transport.Postgres
dotnet build src/Mocha/test/Mocha.Transport.Postgres.Tests
```

#### Test gate
```bash
dotnet test src/Mocha/test/Mocha.Transport.Postgres.Tests
```

#### Orchestration
Same structure as Phase 1: agent for new files, agent for src modifications, agents for test batches.

---

### Phase 3: InMemory -- Mirror of Phase 1

**Goal:** Replicate for InMemory. Simplest transport: empty infra group, no satellite sugar.

#### Source edits

**NEW files (PARALLEL):**
- `src/Mocha/src/Mocha.Transport.InMemory/Descriptors/IInMemoryQueueBuilder.cs` -- per D5 InMemory member list.
- `src/Mocha/src/Mocha.Transport.InMemory/Descriptors/InMemoryQueueBuilder.cs` -- same lazy pattern; empty infra group; BindFrom calls `_transport.DeclareTopic(topicName)` + `_transport.DeclareBinding(topicName, _name)` directly.

**DELETE files:**
- `src/Mocha/src/Mocha.Transport.InMemory/Descriptors/IInMemoryQueueEndpointDescriptor.cs`
- `src/Mocha/src/Mocha.Transport.InMemory/Descriptors/InMemoryQueueEndpointDescriptor.cs`

**MODIFY files:**
- `IInMemoryMessagingTransportDescriptor.cs`: change Queue() return/param types (lines 107, 118).
- `InMemoryMessagingTransportDescriptor.cs`: replace `_queueEndpoints` (line 21) with `_queueBuilders`, rewrite `Queue()` (lines 142-171), delete QueueName drift arm in `Endpoint()` (line 177), rewrite `CreateConfiguration()` (lines 247-289), delete `IsEntityOnly`/`LowerEntityOnlyQueue`/`ValidateOneEndpointPerQueue`.
- `IInMemoryReceiveEndpointDescriptor.cs`: delete `Queue(string)` (line 53) only. InMemory has NO ErrorQueue/SkippedQueue methods to delete.
- `InMemoryReceiveEndpointDescriptor.cs`: delete `PinQueueIdentity` machinery, `Queue(string)` impl.

**Structural differences from RabbitMQ:**
- No satellite config class, no satellite sugar on builder.
- No infra methods at all on the builder (empty infra group).
- No `AutoProvision` concept.
- `LowerEntityOnlyQueue` satellite check uses `configuration.ErrorEndpoint is not null` (base class URI), not satellite config objects.
- Convention has no `MaterializeSatellite` (only `QueueName ??= Name` fallback).

#### Test migration (9 files)

1. `Descriptors/InMemoryUnifiedQueueTests.cs` -- rewrite with builder API
2. `InMemoryBuilderApiTests.cs` -- update `.Queue()` chains (~7 calls)
3. `ReceivesTests.cs` -- update `.Queue()` chains (~30 calls)
4. `InMemoryReceiveEndpointBindFromTests.cs` -- update `.Queue()` chains
5. `InMemoryHandlerClaimTests.cs` -- update `.Queue()` chains
6. `Behaviors/ErrorQueueTests.cs` -- update `.Queue()` chains
7. `Behaviors/PublishOptionsEndpointTests.cs` -- update `.Queue()` chains
8. `Behaviors/CustomHeaderTests.cs` -- update `.Queue()` chains
9. `Topology/InMemoryTopologyConventionTests.cs` -- update `.Queue()` chains

#### Compile gate
```bash
dotnet build src/Mocha/src/Mocha.Transport.InMemory
dotnet build src/Mocha/test/Mocha.Transport.InMemory.Tests
```

#### Test gate
```bash
dotnet test src/Mocha/test/Mocha.Transport.InMemory.Tests
```

---

### Phase 4: Cross-cutting Cleanup

**Goal:** Verify no stale references remain. Clean up ThrowHelper. Update docs.

#### Edits

1. `src/Mocha/src/Mocha/ThrowHelper.cs`: verify `QueueIdentityPinned` was deleted in Phase 1. Verify `SatelliteRequiresConsumingEndpoint` is still used (by builder's CreateConfiguration validation). If not, delete.

2. Grep for any stale references:
   ```bash
   grep -rn "IRabbitMQQueueEndpointDescriptor\|IPostgresQueueEndpointDescriptor\|IInMemoryQueueEndpointDescriptor\|QueueIdentityPinned\|PinQueueIdentity\|IsQueueIdentityPinned\|QueueDurable\b\|QueueAutoProvision\b\|QueueArguments\b\|LowerEntityOnlyQueue\|IsEntityOnly" src/Mocha/ --include="*.cs" -l
   ```

3. Update `routing-topology-proposal.md` section 5.1 wording (if it exists).

#### Compile gate
```bash
dotnet build src/All.slnx
```

#### Test gate
```bash
dotnet test src/Mocha/test/Mocha.Tests
```

---

## Blast-Radius Appendix

### Deleted Symbols

| Symbol | Files | Migration |
|---|---|---|
| `IRabbitMQQueueEndpointDescriptor` | Defined: `IRabbitMQQueueEndpointDescriptor.cs`. Refs: `RabbitMQQueueEndpointDescriptor.cs:8`, `IRabbitMQMessagingTransportDescriptor.cs:134,145`, `RabbitMQMessagingTransportDescriptor.cs:15,221,245`. Tests: `RabbitMQUnifiedQueueTests.cs:174`. | Behavioral: replaced by `IRabbitMQQueueBuilder` |
| `RabbitMQQueueEndpointDescriptor` | Defined: `RabbitMQQueueEndpointDescriptor.cs`. Refs: `RabbitMQMessagingTransportDescriptor.cs:15,240`. | Behavioral: replaced by `RabbitMQQueueBuilder` |
| `IPostgresQueueEndpointDescriptor` | Defined: `IPostgresQueueEndpointDescriptor.cs`. Refs: `PostgresQueueEndpointDescriptor.cs:8`, `IPostgresMessagingTransportDescriptor.cs:124,135`, `PostgresMessagingTransportDescriptor.cs:20,237,261`. Tests: `PostgresUnifiedQueueTests.cs:184`. | Behavioral |
| `PostgresQueueEndpointDescriptor` | Defined: `PostgresQueueEndpointDescriptor.cs`. Refs: `PostgresMessagingTransportDescriptor.cs:20,255`. | Behavioral |
| `IInMemoryQueueEndpointDescriptor` | Defined: `IInMemoryQueueEndpointDescriptor.cs`. Refs: `InMemoryQueueEndpointDescriptor.cs:8`, `IInMemoryMessagingTransportDescriptor.cs:107,118`, `InMemoryMessagingTransportDescriptor.cs:21,142,166`. | Behavioral |
| `InMemoryQueueEndpointDescriptor` | Defined: `InMemoryQueueEndpointDescriptor.cs`. Refs: `InMemoryMessagingTransportDescriptor.cs:21,160`. | Behavioral |
| `_queueEndpoints` dict | All 3 transport descriptors (RMQ:15, PG:20, IM:21). | Replaced by `_queueBuilders` |
| `PinQueueIdentity` / `IsQueueIdentityPinned` / `_queueIdentityPinned` | All 3 receive endpoint descriptors + all 3 queue endpoint descriptors (ctor calls). | Mechanical delete |
| `ThrowHelper.QueueIdentityPinned` | Defined: `ThrowHelper.cs:205`. Called by: all 3 receive endpoint `Queue(string)` impls + all 3 queue endpoint descriptor `Queue(string)` impls. | Mechanical delete |
| `I*ReceiveEndpointDescriptor.Queue(string)` | RMQ:84, PG:83, IM:53 (interfaces). Impls: RMQ:135, PG:135, IM:107. | Behavioral: removed from endpoint, not moved to builder (QueueName always = Name) |
| `I*ReceiveEndpointDescriptor.ErrorQueue/DisableErrorQueue/SkippedQueue/DisableSkippedQueue` | RMQ:45-67, PG:45-67 (interfaces). Impls: RMQ:156-185, PG:156-185. InMemory: N/A (never had these). | Behavioral: moved to builder |
| `RabbitMQReceiveEndpointConfiguration.QueueDurable/QueueAutoProvision/QueueArguments` | Config: lines 32, 37, 42. Written by: `RabbitMQQueueEndpointDescriptor:181,193-194,202`. Read by: `LowerEntityOnlyQueue:332-334`. | Behavioral: "second home" eliminated |
| `IsEntityOnly` / `LowerEntityOnlyQueue` | All 3 transport descriptors (RMQ:300-367, PG:317-389, IM:291-355). | Behavioral: replaced by lazy endpoint + builder BindFrom |
| `SatelliteRequiresConsumingEndpoint` | `ThrowHelper.cs:199`. Called by: `LowerEntityOnlyQueue` in all 3 transports. | KEPT: moved to CreateConfiguration validation |
| `TwoReceiveEndpointsShareOneQueue` | `ThrowHelper.cs:193`. Called by: `ValidateOneEndpointPerQueue` in all 3 transports. | KEPT |
| QueueName drift arm in `Endpoint()` | RMQ:163, PG:168, IM:177. | Mechanical delete: dead code once `Queue(string)` is removed |
| Queue-side QueueName matching | RMQ:230-231, PG:246-247, IM:151-152. | Mechanical delete: replaced by builder's `_queueBuilders` lookup |

### Snapshot Files at Risk

| Snapshot | Action |
|---|---|
| `RabbitMQQueueFrontDoorTests.Queue_Should_MergeOntoRenamedEndpoint_When_EndpointQueueNameMatches.snap` | DELETE (test deleted, QueueName drift gone) |
| `RabbitMQQueueFrontDoorTests.Queue_Should_ResolveSameEndpoint_When_EndpointSharesQueueName.snap` | DELETE (test deleted) |
| `RabbitMQQueueFrontDoorLoweringTests.Describe_Should_ShowEntityOnlyQueue_When_QueueWithoutConsumersOrReceives.snap` | DELETE or rewrite (entity-only lowering deleted) |
| `RabbitMQQueueFrontDoorLoweringTests.Describe_Should_StayByteIdentical_When_ConfigurationUsesNoQueueFrontDoor.snap` | DELETE or rewrite |
| `RabbitMQUnifiedQueueTests.QueueEndpointDeclareQueue_Should_ConvergeToOneEntity_When_SameName.snap` | Rewrite (convergence test uses deleted type) |
| `RabbitMQUnifiedQueueTests.SagaEndpoint_Should_Describe_When_ConfiguredViaUnifiedQueue.snap` | Rewrite (type changes) |
| `RabbitMQHandlerBindingTests.BindHandlersExplicitly_Should_DescribeViaUnifiedQueue_When_HandlerAttachedToQueue.snap` | May change (queue front door path change) |
| `RabbitMQSatelliteTests.*.snap` (5 files) | Likely byte-identical (satellite mechanism unchanged) |
| `RabbitMQExplicitTopologyTests.Describe_Should_InheritParentQueueAutoProvisionOnInfraQueues_When_ParentDeclared.md` | Rewrite (DeclareQueue+Endpoint.Queue merge into single Queue() call) |
| `RabbitMQExplicitTopologyTests.Queue_Endpoint_DeclareQueue_Should_ConvergeToOneEntity_When_SameName.md` | Rewrite (convergence semantics change) |
| Postgres satellite snapshots (3 files) | Likely byte-identical |
| Postgres unified queue snapshots | Rewrite (same as RabbitMQ) |

## Snapshot and Test-Rewrite Strategy

1. **Delete-first:** Any snapshot whose test is deleted gets its `.snap`/`.md` file deleted in the same commit.
2. **Run-then-copy:** After source edits in a phase, run tests. Tests with changed output produce `__mismatch__/` files. Review each mismatch for correctness, then copy to `__snapshots__/`.
3. **Ordering discipline:** Within a phase, snapshot files are only manipulated AFTER the test run produces mismatches. Never pre-edit a snapshot.
4. **New tests for builder:** Add tests that verify:
   - `Queue("x").Durable()` with no routing method does NOT produce a receive endpoint (only topology entity).
   - `Queue("x").Consumer<A>()` then `Endpoint("x").Consumer<B>()` converge on one endpoint with two consumers.
   - `Queue("x").ErrorQueue("err").Consumer<A>()` produces correct satellite config.
   - `Queue("x").Quorum().WithArgument("x-max-length", 1000).Consumer<A>()` produces correct queue arguments.
   - `Queue("x").BindFrom(uri)` without a consumer produces topology entities (exchange + binding) but no endpoint.
5. **Entity-only lowering snapshots:** With the builder, entity-only queues are simpler: `DeclareQueue` eagerly creates the queue entity (Provenance=Declared), and no endpoint materializes. The net topology output should be the same as before for the queue entity, but the "endpoint" section of the snapshot will be absent.

## Cross-Transport Mirror

| Aspect | RabbitMQ | Postgres | InMemory |
|---|---|---|---|
| Builder infra group | Durable, Quorum, WithArgument, AutoProvision | AutoProvision, AutoDelete | **empty** |
| Builder QoS | MaxPrefetch(ushort) | MaxBatchSize(int) | None (only MaxConcurrency from base) |
| Builder BindFrom | Yes (DeclareExchange + DeclareBinding) | Yes (DeclareTopic + DeclareSubscription) | Yes (DeclareTopic + DeclareBinding) |
| Satellite sugar | ErrorQueue, DisableErrorQueue, SkippedQueue, DisableSkippedQueue | Same 4 methods | **none** |
| Satellite config class | RabbitMQSatelliteConfiguration (QueueName, IsDisabled, AutoProvision) | PostgresSatelliteConfiguration (QueueName, IsDisabled) | None |
| Satellite config on recv config | ErrorQueue, SkippedQueue (KEEP) | ErrorQueue, SkippedQueue (KEEP) | None |
| Queue-shape second home to delete | QueueDurable, QueueAutoProvision, QueueArguments | None (never had them) | None |
| TryGetSatelliteAutoProvision | Yes (KEEP) | No | No |
| Convention satellite materialization | Yes (KEEP) | Yes (KEEP) | No |
| Entity-only satellite check | configuration.ErrorQueue/SkippedQueue fields | Same | configuration.ErrorEndpoint/SkippedEndpoint URIs |
| Files to DELETE | 2 (conflated tier) | 2 | 2 |
| Files to MODIFY (src) | ~7 | ~6 | ~5 |
| Test files to migrate | 19 | 17 | 9 |

## Final Verification

```bash
# Full solution build
dotnet build src/All.slnx

# All transport test projects
dotnet test src/Mocha/test/Mocha.Transport.RabbitMQ.Tests
dotnet test src/Mocha/test/Mocha.Transport.Postgres.Tests
dotnet test src/Mocha/test/Mocha.Transport.InMemory.Tests

# Core Mocha tests (ThrowHelper changes)
dotnet test src/Mocha/test/Mocha.Tests

# Grep for any stale references
grep -rn "IRabbitMQQueueEndpointDescriptor\|IPostgresQueueEndpointDescriptor\|IInMemoryQueueEndpointDescriptor\|QueueIdentityPinned\|PinQueueIdentity\|IsQueueIdentityPinned\|LowerEntityOnlyQueue\|IsEntityOnly" src/Mocha/ --include="*.cs" -l
```

## Risks and Open Questions

### Risk 1: BindFrom as infra-group method (D7) changes behavior for consuming endpoints

Today, a consuming endpoint that has BindFrom stores the intent in `QueueBindFroms` on the endpoint config, and the receive-endpoint lifecycle's `OnDiscoverTopology` creates the exchange and binding. With the builder's BindFrom writing directly to the topology (DeclareExchange + DeclareBinding), the binding is created at descriptor time instead. This should produce the same net topology because the topology's `AddExchange`/`AddQueue` idempotently merge. However, the Provenance may differ (Declared from the builder vs Endpoint from the lifecycle). Since MergeFrom never downgrades Declared, this is safe. But snapshot output may change because the provenance field value changes for these entities.

**Mitigation:** The builder's BindFrom should also add to `QueueBindFroms` on the endpoint config (via `EnsureEndpoint()`) so the lifecycle path still fires, producing Provenance=Endpoint which merges with the Declared entry. Actually this would double-create. Better approach: the builder's BindFrom ONLY writes to topology (no endpoint involved). The endpoint's `OnDiscoverTopology` would not see these bindings (since they are not in `QueueBindFroms`). The net result is the same: the binding exists in the topology. Review `OnDiscoverTopology` to confirm it handles the case where the exchange/binding already exist (it should, via idempotent add).

**Alternative mitigation:** Keep BindFrom in the routing group (calls `EnsureEndpoint()`), but in `CreateConfiguration`, when an endpoint has `QueueBindFroms` but no consumers, lower the bindings to topology entities and remove the endpoint from `_receiveEndpoints`. This is closer to the existing `LowerEntityOnlyQueue` pattern. This approach preserves the existing `QueueBindFroms` flow exactly. The risk is that this partially reintroduces the entity-only pruning logic. Given this is a judgment call, the implementer should evaluate both approaches.

### Risk 2: ConfigureEndpoint lambda type

`t.Handler<T>().ConfigureEndpoint(e => e.Queue("x"))` passes a lambda typed `Action<IRabbitMQReceiveEndpointDescriptor>`. After `Queue(string)` is deleted from `IRabbitMQReceiveEndpointDescriptor`, this call becomes a compile error. The fix is to remove the `.Queue("x")` call from the lambda (since QueueName already = Name from the ctor). But if the test's intent is to consume from a different queue name, the test must be rewritten to use `t.Queue("x").Handler<T>()` instead. This affects `PostgresHandlerClaimTests.cs:33` and potentially other test files.

**Mitigation:** Grep for `ConfigureEndpoint.*\.Queue\(` across all test projects to find all affected call sites before starting migration.

### Risk 3: ValidateOneEndpointPerQueue with builder + Endpoint convergence

If `Queue("x").Consumer<A>()` and `Endpoint("x").Consumer<B>()` both run, they converge on one endpoint (via `Endpoint(name)` idempotency). `ValidateOneEndpointPerQueue` keys by `Configuration.QueueName` and iterates `_receiveEndpoints`. Since both paths produce the same endpoint instance, the validation sees one endpoint, one queue. This is correct. But if someone calls `Queue("x").Consumer<A>()` and then separately `Queue("y").Consumer<B>()` where both `x` and `y` map to the same broker queue (impossible after this refactor since the builder uses the queue name as identity), there is no risk.

**Mitigation:** No special handling needed. The idempotent `Endpoint(name)` mechanism is the safety net.

### Risk 4: Phantom endpoint from satellite-only builder usage

`Queue("x").ErrorQueue("err")` calls `EnsureEndpoint()` (materializing an endpoint) but adds no consumer. This creates an endpoint that enters the lifecycle with no handlers. The satellite validation in `CreateConfiguration` (preserved from `LowerEntityOnlyQueue`) catches this and throws. But this depends on the implementer correctly porting the validation from `LowerEntityOnlyQueue` into the new `CreateConfiguration` flow.

**Mitigation:** Write a test that asserts `Queue("x").ErrorQueue("err")` without a consumer throws `SatelliteRequiresConsumingEndpoint`.
