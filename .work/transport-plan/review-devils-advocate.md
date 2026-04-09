# Devil's Advocate Review: Azure Event Hub Transport Implementation Plan

**Reviewer role**: Senior distributed systems engineer, adversarial reviewer
**Date**: 2026-03-27

---

## Risks Identified

### HIGH RISK

#### 1. EventPosition.Latest means silent message loss on restart

The plan uses `EventPosition.Latest` in `ReadPartitionAsync`. This means:
- When the consumer starts, it only sees **new** messages arriving after the connection is established.
- If the consumer crashes, restarts, or is deployed, **every message published between shutdown and restart is permanently lost**.
- There is no checkpoint store, no offset tracking, and no way to recover these messages.

The plan explicitly defers checkpointing to Phase 5 ("Future"). This is not a future concern -- it is a **correctness issue from day one**. Any transport that silently drops messages during restarts is not production-viable, and even for testing it creates flaky, non-deterministic behavior.

**Impact**: Messages lost during deployments, crashes, scaling events, or any consumer restart.

**Recommendation**: At minimum, start from `EventPosition.Earliest` with a configurable option, and move checkpoint support to Phase 1 or Phase 2. Without checkpoints, this transport cannot provide at-least-once delivery guarantees.

#### 2. No reconnection strategy for partition readers

The `ReadPartitionAsync` method has a `catch (Exception ex)` block with a `// TODO: implement reconnection/backoff`. If an AMQP connection drops in a way the SDK considers unrecoverable (network partition, token expiry, service-side throttling beyond retry limits), the partition reader task **exits silently** after logging. There is no retry loop, no backoff, no re-creation of the consumer client.

This means a transient infrastructure event can permanently disable consumption on one or more partitions with no recovery path except full application restart.

**Impact**: Silent partition read failure in production. Partial message consumption with no alerting mechanism beyond log inspection.

**Recommendation**: Implement a retry loop with exponential backoff around the entire `ReadPartitionAsync` method. On unrecoverable failure, dispose the consumer, create a new one from the connection provider, and restart partition reads. This belongs in Phase 1, not deferred.

#### 3. EventHubConsumerClient per partition is not production-grade

The SDK documentation explicitly labels `EventHubConsumerClient` as suited for "simple scenarios" and "dev/test". The plan uses `ReadEventsFromPartitionAsync` which:
- Has no built-in reconnection or rebalancing
- Requires the application to manage all failure recovery
- Has no distributed coordination between instances

The SDK research document (section 10) explicitly recommends `EventProcessorClient` for production workloads. The plan dismisses it to avoid the Blob Storage dependency, but this trades a well-tested production mechanism for a hand-rolled consumer loop with known gaps (no reconnection, no checkpointing, no rebalancing).

**Impact**: The transport will behave differently in production than what the Azure SDK team has tested and validated.

**Recommendation**: Either use `EventProcessorClient` (accepting the Blob Storage dependency) or subclass `EventProcessor<TPartition>` (from `Azure.Messaging.EventHubs.Primitives`) with a custom checkpoint store. The latter avoids Blob Storage while getting the SDK's built-in reconnection, rebalancing, and partition management. If `EventHubConsumerClient` is the final choice, the reconnection and checkpoint gaps must be closed in Phase 1.

#### 4. Error/skipped routing to separate Event Hubs is expensive and potentially unbounded

The convention routes errors to `error-{hubname}` and skipped messages to `skipped-{hubname}` as separate Event Hubs. But:
- Event Hubs are **not free** -- each one consumes namespace capacity and has a cost
- Auto-provisioning is deferred, so these error/skipped hubs must be **manually pre-created** for every receive endpoint
- If a receive endpoint handles N message types, you need the main hub + error hub + skipped hub = 3 hubs minimum per endpoint
- Event Hubs have no dead-letter queue, so "error routing" means publishing to another hub, which can itself fail -- creating a cascade

**Impact**: Operational burden of pre-creating error/skipped hubs. If error publishing fails (e.g., error hub doesn't exist), the error handling itself fails silently or throws.

**Recommendation**: Document the operational requirement clearly. Consider whether errors should go to a single shared error hub rather than per-endpoint error hubs. Add error handling around error routing (what happens if the error hub doesn't exist?).

### MEDIUM RISK

#### 5. Max 5 readers per partition per consumer group -- no guard

The Azure Event Hubs service enforces a hard limit of 5 concurrent readers per partition per consumer group. The plan does not:
- Track how many readers exist per partition
- Guard against exceeding this limit
- Handle the `QuotaExceededException` that the SDK throws when the limit is hit

If multiple instances of the same service start (horizontal scaling, blue-green deployment), each instance creates readers for ALL partitions. With >5 instances, readers will fail.

**Impact**: Scaling beyond 5 instances per consumer group causes reader failures.

**Recommendation**: Add documentation about this limit. Consider partition assignment (only read partitions you "own") or at minimum handle `QuotaExceededException` gracefully. This is where `EventProcessorClient`'s built-in partition balancing solves the problem automatically.

#### 6. Single-message SendAsync is inefficient

The dispatch endpoint calls `producer.SendAsync([eventData], sendOptions, cancellationToken)` which sends a single event per call. Each call is a full AMQP round-trip. For high-throughput scenarios this is significantly slower than batching.

The plan acknowledges this and defers batching to Phase 5, but the `SendAsync(IEnumerable<EventData>)` overload with a single item still validates batch size constraints on every call.

**Impact**: Higher latency and lower throughput than necessary on the send path.

**Recommendation**: Acceptable for Phase 1 but should be flagged as a known performance limitation. The batch optimization should be Phase 2, not Phase 5.

#### 7. CancellationToken passed to StartAsync is used for entire consumer lifetime

In `RegisteredConsumer.StartAsync`:
```csharp
_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
```

The `cancellationToken` here comes from the startup pipeline. If this token has a timeout (common in hosted service startup), the linked CTS will cancel all partition readers when the startup timeout expires, not just when shutdown is requested.

**Impact**: Consumers may unexpectedly stop reading if the startup cancellation token has a limited lifetime.

**Recommendation**: Use a separate CTS for the partition read loops. Link only to a shutdown token, not the startup token.

#### 8. Test fixture requires management permissions

The test plan says "each test creates uniquely-named hubs (e.g., `test-{guid}`)". Creating Event Hubs requires management-level permissions (`Azure.ResourceManager.EventHubs` or the management plane). The `Azure.Messaging.EventHubs` package used in the plan has **no management API** -- it only supports data plane operations (send/receive).

For the emulator (Option A): the emulator requires a `Config.json` that pre-declares Event Hub names. You cannot dynamically create hubs at runtime without management API access.

**Impact**: The test isolation strategy as described may not work without adding the `Azure.ResourceManager.EventHubs` package or pre-configuring the emulator.

**Recommendation**: Investigate the emulator's management capabilities. If dynamic hub creation is not possible, use a fixed set of pre-configured hubs and isolate tests via unique consumer groups or partition keys instead.

#### 9. Reply endpoint uses a per-instance Event Hub

For request-reply, the plan creates a per-instance hub (`context.Naming.GetInstanceEndpoint(context.Host.InstanceId)`) marked as temporary. But:
- Event Hubs cannot be dynamically created without management permissions
- There is no cleanup mechanism for these temporary hubs
- Each service instance creates its own hub -- with 100 instances, that's 100 reply hubs

**Impact**: Reply pattern may not work without auto-provisioning. Orphaned reply hubs accumulate.

**Recommendation**: Consider using a shared reply hub with partition key routing based on instance ID, rather than per-instance hubs. Or accept that request-reply requires management permissions and auto-provisioning.

### LOW RISK

#### 10. No health check or liveness probe

The plan has no mechanism for the transport to report its health. If all partition readers silently fail (see Risk #2), there is no way for a health check endpoint to detect the problem.

**Recommendation**: Add a basic health check that verifies at least one partition reader is active per receive endpoint.

#### 11. Path.Combine for URI construction

In `EventHubTopic.OnComplete`:
```csharp
address.Path = Path.Combine(address.Path, "h", configuration.Name);
```

`Path.Combine` uses OS-specific path separators. On Windows, this produces backslashes in URIs. Should use string concatenation or `Uri` builder methods.

**Recommendation**: Replace `Path.Combine` with explicit `/` concatenation.

#### 12. HashSet for well-known headers is overkill

`EventHubMessageHeaders` uses a `HashSet<string>` with 7 entries. For such a small set, a series of string equality checks or a `switch` expression would be faster and allocation-free.

**Recommendation**: Minor optimization opportunity. Not blocking.

#### 13. Partition key defaulting to CorrelationId may cause hot partitions

The dispatch endpoint defaults to using `CorrelationId` as the partition key. If many messages share the same correlation ID (e.g., saga-style workflows with long-lived correlation), they all land on the same partition, creating a hot partition.

**Recommendation**: Document this behavior. Consider whether round-robin (no partition key) should be the default, with CorrelationId-based routing as an opt-in.

---

## Challenged Assumptions

| # | Assumption in the Plan | Challenge |
|---|---|---|
| 1 | "Simpler than EventProcessorClient -- no Blob Storage dependency" | Simpler to implement, but pushes complexity into hand-rolled reconnection, checkpointing, and partition management that the SDK already solves. Net complexity may be higher. |
| 2 | "EventPosition.Latest for new consumers (no replay of old events by default)" | This is not a sensible default for a messaging transport. It means any message sent before the consumer starts is lost. Messaging transports typically guarantee delivery of messages sent after the transport is configured, regardless of consumer start timing. |
| 3 | "Each partition gets its own async read loop" with one `EventHubConsumerClient` | The SDK docs note that `EventHubConsumerClient` opens AMQP links per partition internally, but uses a single connection. Starting N `ReadEventsFromPartitionAsync` calls on one client is fine, but if the underlying connection fails, ALL partition reads fail simultaneously. |
| 4 | "Auto-provisioning deferred" | Without auto-provisioning, every hub (main, error, skipped, reply) must be pre-created manually. For a framework that aims to be developer-friendly, this is a significant operational burden. At minimum, the error should clearly state what's missing and how to fix it. |
| 5 | "No per-message ack -- acknowledgement middleware is a no-op" | A no-op acknowledgement middleware gives the false impression that messages are acknowledged. If processing fails, the message is simply gone -- there's no retry, no dead-letter, no redelivery. This should be explicitly documented as "at-most-once" delivery semantics. |

---

## Missing Failure Scenarios

1. **Token/credential expiry**: The `TokenCredential` provider's tokens expire. The SDK handles refresh, but if refresh fails (e.g., managed identity endpoint down), all operations fail. No handling described.

2. **Event Hub deletion while consumer is running**: What happens if an Event Hub is deleted (by another process or admin) while partition readers are active? The readers will fail -- how is this surfaced?

3. **Namespace throttling**: Event Hubs namespaces have throughput limits (TUs/PUs). When the limit is hit, sends are throttled. The SDK retries, but if the retry budget is exhausted, `SendAsync` throws. The dispatch endpoint does not catch or handle this.

4. **Oversized messages**: The plan sends a single `EventData` via `SendAsync`. If the message body exceeds 1MB (256KB on Basic tier), `SendAsync` throws `EventHubsException`. No size validation or error message to guide the user.

5. **Consumer group doesn't exist**: If the configured consumer group hasn't been created, `ReadEventsFromPartitionAsync` will fail. The error should clearly indicate "consumer group X does not exist" rather than a generic AMQP error.

6. **Concurrent dispose and send**: If `DisposeAsync` is called on the transport while `DispatchAsync` is in-flight, the producer may be disposed mid-send. The `ConcurrentDictionary.Clear()` in `ConnectionManager.DisposeAsync` doesn't wait for in-flight operations.

7. **Network partition during send**: `SendAsync` returns when the service acknowledges. If the network drops after the service receives but before the ack returns, the SDK will retry (potentially duplicating the message). The transport provides no deduplication mechanism.

---

## Suggested Additions to the Plan

1. **Move reconnection strategy from TODO to Phase 1**: A consumer that can't recover from failures is not shippable.

2. **Move basic checkpoint/offset support to Phase 2**: At minimum, allow configuring `EventPosition.Earliest` vs `EventPosition.Latest`, and track the last processed sequence number in-memory so restarts within the same process don't reprocess.

3. **Add graceful shutdown sequencing**: Stop accepting new dispatches before stopping consumers. The current plan stops consumers first, which could leave in-flight responses undeliverable.

4. **Add integration test for consumer restart**: Verify that messages sent during consumer downtime are (or are not) received after restart. This tests the most critical behavior gap.

5. **Add size validation on dispatch**: Check message size before sending and provide a clear error message.

6. **Document delivery semantics explicitly**: State that this transport provides at-most-once delivery without checkpointing, and at-least-once with checkpointing (future).

7. **Consider EventProcessor<TPartition> base class**: The `Azure.Messaging.EventHubs.Primitives` namespace provides `EventProcessor<TPartition>` which can be subclassed with a custom checkpoint store. This gets the SDK's reconnection and partition management without the Blob Storage dependency.

---

## Verdict: REVISE

The plan is well-structured and demonstrates strong understanding of both the Mocha framework patterns and the Event Hub SDK. The addressing model, topology design, envelope mapping (especially the AMQP structured properties optimization), and phased implementation order are solid.

However, three issues must be addressed before implementation:

1. **The consumer reliability story has critical gaps**: No reconnection, no checkpointing, `EventPosition.Latest` -- these combine to create a transport that silently loses messages and cannot recover from infrastructure failures. The TODO in `ReadPartitionAsync` is a production incident waiting to happen.

2. **The 5-reader-per-partition limit is unaddressed**: Without partition assignment or `EventProcessorClient`, horizontal scaling will hit a hard wall at 5 instances.

3. **The test strategy depends on capabilities that may not exist**: Dynamic hub creation in tests requires management permissions that the chosen SDK package doesn't provide.

These are not Phase 5 concerns -- they are Phase 1 correctness requirements. The plan should be revised to either adopt `EventProcessor<TPartition>` as the consumer base (recommended) or close the reconnection and offset tracking gaps in Phase 1.
