# Kafka Transport Topology Review

**Reviewer:** Kafka Infrastructure Expert
**Date:** 2026-04-09
**Scope:** Topic naming, DLQ patterns, consumer groups, partitioning, reply topics, auto-provisioning, offset management, lifecycle

---

## 1. Topic Naming and Structure

### How names are derived

Topic names flow through `DefaultNamingConventions`:

- **Publish (events):** `GetPublishEndpointName` produces `{namespace-kebab}.{type-name-kebab}` -- e.g., `OrderPlacedEvent` in namespace `KafkaTransport.Contracts.Events` becomes `kafka-transport.contracts.events.order-placed`.
- **Send (commands):** `GetSendEndpointName` produces just `{type-name-kebab}` -- e.g., `ProcessOrderCommand` becomes `process-order`.
- **Subscribe endpoints:** `{service-name-kebab}.{handler-name-kebab}` -- e.g., `order-service.order-placed-notification`.
- **Reply topics:** `response-{guid:N}` -- instance-specific, e.g., `response-a1b2c3d4...`.
- **Error/skipped suffixes:** `_error`, `_skipped` appended to base name.

### Assessment

**GOOD: Kebab-case naming.** Kafka topic names are case-sensitive and the community convention is lowercase. Using kebab-case (`order-placed` not `OrderPlaced`) aligns with Confluent recommendations and avoids case-sensitivity bugs.

**GOOD: Deterministic, human-readable names.** Names derived from types are predictable, which aids debugging and monitoring. `kafka-transport.contracts.events.order-placed` is readable in tooling.

**CONCERN: Dot-separated vs hyphen-separated topic names.** The publish endpoint produces names like `kafka-transport.contracts.events.order-placed`, mixing dots (namespace separators) with hyphens (word separators within segments). This is technically valid, but:
- Dots in topic names interact with `auto.create.topics.enable` and metrics hierarchies in JMX (Kafka exports topic metrics using dots, which can collide with topic names containing dots).
- Confluent's recommended convention is either all-dots or all-hyphens, not mixed. The mixed approach can cause confusion in monitoring dashboards (Grafana, Datadog) that split metrics on dots.
- **Recommendation:** Consider using only hyphens for the full topic name, or provide a Kafka-specific naming override that flattens namespaces into hyphens: `kafka-transport-contracts-events-order-placed`.

**CONCERN: Topic names can be very long.** A deeply nested namespace like `MyCompany.Platform.Ordering.Domain.Events` produces `my-company.platform.ordering.domain.events.order-placed`. Kafka's hard limit is 249 characters, but long names waste space in metadata fetches and are unwieldy in CLI tooling. No length validation exists.
- **Recommendation:** Add a length check in topic configuration or provide a way to customize the publish namespace prefix.

**CONCERN: Topic-per-event-type with full namespace prefix.** The publish convention prefixes the full namespace, creating very specific topic names. This is the correct approach for a messaging framework (each event type gets its own topic, enabling independent consumer groups), but it does mean topic counts scale linearly with event types. For a microservices system with hundreds of event types, this creates hundreds of topics. This is manageable in modern Kafka (tens of thousands of topics are fine with KRaft), but operators should be aware.
- **Recommendation:** Document the expected topic count scaling behavior.

**GOOD: Send topics use short names without namespace.** `process-order` is clean and appropriate for command topics that are point-to-point.

---

## 2. Error / Dead-Letter Topic Design

### How it works

- For every default receive endpoint, `KafkaReceiveEndpointTopologyConvention` creates an `_error` topic (line 59-63).
- For every dispatch endpoint, `KafkaDispatchEndpointTopologyConvention` also creates `_error` topics (with guard against recursive `_error_error`).
- Skipped messages go to `_skipped` topics.
- Error/skipped topics are created with default configuration (no special retention or compaction).

### Assessment

**GOOD: Dedicated error topics per source.** Having `order-placed_error` separate from `payment-processed_error` is the right pattern. It allows targeted investigation and replay per source topic.

**GOOD: Guard against recursive error topic creation.** The check for `_error` suffix in `KafkaDispatchEndpointTopologyConvention` (line 32-33) prevents creating `orders_error_error` chains.

**CONCERN: Error topics have no retention policy.** Error topics are created with the same default configuration as regular topics. In production, this means:
- Error topics use broker-default retention (typically 7 days), which may be too short for investigation.
- Or if broker defaults are long, error messages accumulate indefinitely.
- **Recommendation:** Apply a specific retention policy to error topics. Common practice is 30 days for error topics (long enough for investigation, not forever). Consider `retention.ms=2592000000` (30 days) and `cleanup.policy=delete` for error topics.

**CONCERN: No compaction option for error topics.** Some teams prefer `cleanup.policy=compact` on error topics so that the latest error per key is retained indefinitely while older duplicates are removed. This is particularly useful when error topics are used for dead-letter replay.
- **Recommendation:** Make error topic retention configurable via `KafkaBusDefaults` or a dedicated `KafkaDefaultErrorTopicOptions`.

**SUGGESTION: Skipped topic distinction.** Having both `_error` and `_skipped` is a good separation of concerns (processing failures vs. unroutable messages). This follows MassTransit's pattern and is well-understood in the .NET ecosystem.

**GOOD: DLQ naming convention.** The `_error` suffix is a widely recognized convention (MassTransit uses it, Spring Kafka uses `.DLT`). It is clear and unambiguous.

---

## 3. Consumer Group Strategy

### How it works

- Consumer group IDs default to the endpoint name, which defaults to the topic name (`KafkaDefaultReceiveEndpointConvention`, line 16).
- For subscribe (pub/sub) endpoints: `{service-name}.{handler-name}` -- e.g., `shipping-service.order-placed-event`.
- For send (command) endpoints: same as topic name -- e.g., `process-order`.
- For reply endpoints: `response-{guid:N}` (same as topic name).
- Each receive endpoint gets its own consumer instance via `ConnectionManager.CreateConsumer(groupId)`.

### Assessment

**GOOD: Consumer group per handler for subscribe endpoints.** This is correct for pub/sub. When `OrderService.OrderPlacedEventHandler` and `ShippingService.OrderPlacedEventHandler` both subscribe to the same event, they get different consumer group IDs (because service name is included), so both receive all messages independently. This is textbook Kafka fan-out via consumer groups.

**GOOD: Shared consumer group for send/command endpoints.** When multiple instances of the same service consume from a command topic like `process-order`, they use the same consumer group ID, achieving competing-consumer load distribution.

**CONCERN: Consumer group ID collision risk.** If two different services have the same service name (or no service name configured -- note the `host.ServiceName is not null` check on line 33 of `DefaultNamingConventions`), and register handlers with the same name, they would get the same consumer group ID and compete for messages instead of each getting a copy.
- When `host.ServiceName` is null, subscribe endpoint names become just the handler name without service prefix. Two services with `OrderPlacedEventHandler` and no service name would collide.
- **Recommendation:** Require `ServiceName` for subscribe endpoints, or validate uniqueness. At minimum, log a warning when service name is not set.

**GOOD: One consumer per receive endpoint.** This is the simplest correct model. Each endpoint gets a dedicated `IConsumer<byte[], byte[]>`, avoiding the complexity of shared consumers reading from multiple topics.

**CONCERN: No configurable partition assignment strategy.** The consumer config doesn't set `PartitionAssignmentStrategy`. It defaults to `Range` in librdkafka, which can cause uneven distribution when the number of partitions isn't divisible by the number of consumers. `CooperativeSticky` (available since Kafka 2.4+) is now the recommended default for most workloads because:
- It avoids stop-the-world rebalances.
- It maintains partition stickiness for better cache locality.
- It supports incremental rebalancing.
- **Recommendation:** Default to `CooperativeSticky` partition assignment. This is a single config line: `config.PartitionAssignmentStrategy = PartitionAssignmentStrategy.CooperativeSticky`.

---

## 4. Partition and Replication Defaults

### How it works

- `KafkaTopic.Partitions` defaults to `1` (line 16 of KafkaTopic.cs).
- `KafkaTopic.ReplicationFactor` defaults to `1` (line 21 of KafkaTopic.cs).
- Both can be overridden via `KafkaBusDefaults.Topic` or per-topic configuration.
- The `KafkaDefaultTopicOptions.ApplyTo` method fills in unset values from defaults.

### Assessment

**CONCERN: Default partition count of 1.** While 1 partition is safe as a default (no parallelism issues), it limits throughput for any topic to a single consumer instance. For most production workloads, 3-6 partitions is a better starting point.
- However, for a framework, defaulting to 1 is arguably correct -- it's the safest option and avoids ordering surprises. Users who need throughput can override.
- **Recommendation:** This is acceptable as a default but should be prominently documented. Consider whether the framework should suggest a minimum based on expected consumer count.

**ISSUE: Default replication factor of 1.** This means no data redundancy. If a broker goes down, messages on that broker are lost. For a messaging framework that aims for at-least-once semantics, a replication factor of 1 undermines that guarantee.
- In any production Kafka cluster, RF=1 is never acceptable for important data.
- Combined with `Acks=All` on the producer (which is correctly set), RF=1 means `Acks=All` provides no additional safety -- there's only one replica to ack.
- **Recommendation:** Change the hardcoded default to `-1` (which tells the broker to use its `default.replication.factor` setting), or default to `3` for production safety. At minimum, log a warning when RF=1 is in use.

**GOOD: User can override via `ConfigureDefaults`.** The `KafkaBusDefaults` system allows users to set cluster-wide defaults, and per-topic configuration overrides those defaults. This is the right layering.

---

## 5. Reply Topic Design

### How it works

- Reply topics are instance-specific: `response-{guid:N}` (32-char hex, no hyphens).
- Created with `IsTemporary = true`, `retention.ms = 3600000` (1 hour), `cleanup.policy = delete`.
- Consumer group ID matches the topic name (one consumer per instance).
- Auto-provisioned with `AutoProvision = true`.

### Assessment

**GOOD: Instance-specific reply topics.** This is the standard pattern for request-reply over Kafka. Each service instance listens on its own reply topic, correlated by `ResponseAddress` in the message headers. This avoids the complexity of shared reply topics with correlation filters.

**GOOD: Short retention (1 hour).** Reply messages are ephemeral -- once consumed, they're not needed. 1 hour is generous enough to handle slow consumers or brief outages.

**CONCERN: Reply topics are not cleaned up on shutdown.** When an instance stops, its reply topic remains in Kafka with 1-hour retention. Over time, with frequent deployments or scaling events, orphaned reply topics accumulate. Each topic consumes broker metadata and partition resources.
- The `response-{guid:N}` format means every new instance creates a new topic (GUID changes per instance).
- **Recommendation:** Consider one or more of:
  1. Delete the reply topic on graceful shutdown via the admin client.
  2. Use a stable instance identifier (e.g., hostname + port, or a configured instance ID) so restarts reuse the same topic.
  3. Set a very short retention (5-10 minutes) since replies should be consumed nearly immediately.
  4. Document that operators should periodically clean up `response-*` topics, or provide a utility for this.

**CONCERN: Scaling creates topic proliferation.** If a service auto-scales from 3 to 10 instances and back, 7 orphaned reply topics are created. At scale (e.g., Kubernetes HPA with frequent scaling), this creates hundreds of orphaned topics.
- **Recommendation:** Use stable instance identifiers derived from the pod/host name rather than random GUIDs. This way, a restarted pod reuses the same reply topic.

**SUGGESTION: Consider shared reply topic with correlation.** An alternative design uses a single reply topic per service (e.g., `order-service-replies`) with correlation IDs to route responses. This eliminates topic proliferation entirely. The tradeoff is more complex routing logic and all instances sharing a single consumer group with partition-based filtering. For a framework, the current instance-specific approach is simpler and correct.

---

## 6. Auto-Provisioning

### How it works

- `KafkaMessagingTransport.OnBeforeStartAsync` calls `ConnectionManager.ProvisionTopologyAsync` for all topics with `AutoProvision == true`.
- `ProvisionTopologyAsync` collects all topic specs and calls `adminClient.CreateTopicsAsync(specs)` in a single batch.
- If `CreateTopicsException` is thrown, it iterates `ex.Results` and only throws if a result has an error code other than `TopicAlreadyExists` or `NoError`.
- Dispatch endpoints lazily provision via `EnsureProvisionedAsync` on first dispatch.

### Assessment

**GOOD: Single batch admin call.** Creating all topics in one `CreateTopicsAsync` call is efficient. This minimizes admin client round-trips and is the recommended approach.

**GOOD: TopicAlreadyExists handling.** The fix for handling `TopicAlreadyExists` errors is correct. The original Kafka AdminClient API throws a single exception with results for each topic, and some may succeed while others already exist. The code correctly handles this by checking each result individually.

**CONCERN: No configuration mismatch detection.** When a topic already exists but with different configuration (e.g., fewer partitions than requested, different retention), the current code silently ignores this. This can lead to subtle production issues where the framework expects 6 partitions but the topic has 3.
- **Recommendation:** After provisioning, optionally describe existing topics and warn if their configuration differs from what was requested. This could be behind a `ValidateTopology` flag.

**CONCERN: No authorization error handling.** If the service account doesn't have `CreateTopics` permission, the exception handling might not surface this clearly. The code only checks for `TopicAlreadyExists` and `NoError`, but authorization failures would have a different error code and should be surfaced with a clear message.
- **Recommendation:** Improve error reporting to distinguish "topic exists" (benign) from "access denied" (configuration error) from other failures.

**GOOD: Lazy provisioning for dispatch endpoints.** The `EnsureProvisionedAsync` in `KafkaDispatchEndpoint` handles dynamically-created dispatch endpoints (e.g., for reply routing). This prevents failures when a dispatch endpoint is created after initial startup.

**SUGGESTION: Idempotent provisioning flag.** Consider exposing a `CreateIfNotExists` vs `EnsureConfiguration` mode. The former (current behavior) is fast and safe. The latter would use `CreateTopicsAsync` + `AlterConfigsAsync` to ensure configuration matches, which is useful in CI/CD pipelines.

---

## 7. Offset Management

### How it works

- `EnableAutoCommit = false` in `ConsumerConfig` (line 102 of `KafkaConnectionManager.cs`).
- `KafkaCommitMiddleware` commits after successful pipeline processing: `feature.Consumer.Commit(feature.ConsumeResult)`.
- On failure, offset is not committed, so the message will be redelivered.
- `AutoOffsetReset = AutoOffsetReset.Earliest` -- new consumer groups start from the beginning.
- Sequential processing: one message at a time per consumer (the consume loop awaits `ExecuteAsync`).

### Assessment

**GOOD: Manual commit after processing.** This is the correct approach for at-least-once delivery. Auto-commit risks acknowledging messages before processing completes, leading to message loss on crash.

**GOOD: Synchronous commit.** `Consumer.Commit()` (synchronous) ensures the offset is persisted before moving to the next message. This is the safest option for at-least-once semantics. The performance cost is acceptable given sequential processing.

**GOOD: Earliest offset reset.** Starting from the earliest offset ensures no messages are missed when a new consumer group is created. This is the correct default for a messaging framework.

**CONCERN: No commit batching.** The current implementation commits after every single message. For high-throughput scenarios, this creates significant overhead (each commit is a round-trip to the Kafka coordinator). The common optimization is to commit every N messages or every T milliseconds.
- However, given the sequential processing model, this is a correctness-over-performance tradeoff. Batched commits would mean re-processing N messages on crash instead of 1.
- **Recommendation:** This is acceptable for now. For a future optimization, consider periodic commit (e.g., every 100ms) with a "last processed offset" tracker, accepting re-delivery of a few messages on crash.

**CONCERN: Rebalance handling is minimal.** The `PartitionsRevokedHandler` does nothing beyond logging. For at-least-once semantics with manual commit, best practice is to commit current offsets for revoked partitions in the revoked handler. However, since processing is sequential (no in-flight messages when the handler fires, as the comment correctly notes), this is actually safe.
- **Recommendation:** The current approach is correct for sequential processing. If concurrent processing is added later, this must be revisited.

**GOOD: MaxPollIntervalMs = 600000 (10 minutes).** This is generous and prevents spurious rebalances for slow message processing. The default of 300000 (5 minutes) is often too aggressive.

---

## 8. Topic Cleanup and Lifecycle

### How it works

- Reply topics get `retention.ms=3600000` (1 hour) and `cleanup.policy=delete`.
- No active cleanup of temporary topics on shutdown.
- Error/skipped topics have no special lifecycle management.

### Assessment

**CONCERN: No active reply topic cleanup.** As discussed in section 5, orphaned reply topics accumulate. The 1-hour retention means data is cleaned up, but the topic metadata persists until manually deleted.
- **Recommendation:** Add topic deletion to the shutdown path for temporary topics, or use stable instance IDs.

**CONCERN: Error topics accumulate indefinitely.** If an error topic has the broker default retention (e.g., 7 days with `delete` policy), old error messages are cleaned up. But the topics themselves are never deleted, even if the corresponding handler is removed.
- **Recommendation:** This is acceptable -- topic deletion should be an operational concern, not automated. But document the expected topic lifecycle.

**SUGGESTION: Consider `delete.retention.ms` for error topics.** For teams using error topics as a replay source, setting `delete.retention.ms` to a longer value (e.g., 30 days) on error topics specifically would be valuable.

---

## 9. Producer Configuration

### Assessment

**GOOD: `Acks=All` and `EnableIdempotence=true`.** This is the gold standard for reliable message production. Every message is acknowledged by all in-sync replicas, and idempotence prevents duplicate messages from producer retries.

**GOOD: `LingerMs=5` and `BatchNumMessages=10000`.** These settings enable efficient batching without adding significant latency. 5ms linger is a good balance between throughput and latency.

**GOOD: Callback-based produce with TCS.** Using `Produce()` with a delivery report callback instead of `ProduceAsync()` avoids an unnecessary `Task` allocation per message. This shows attention to framework-level performance.

**GOOD: In-flight tracking with graceful shutdown.** The `TrackInflight`/`UntrackInflight` pattern with `Flush(10s)` on dispose ensures messages are delivered before shutdown, with a timeout to prevent hanging.

---

## 10. Consumer Configuration

### Assessment

**GOOD: `EnablePartitionEof = false`.** This prevents EOF events from cluttering the consume loop.

**GOOD: Error and log handlers configured.** Both producer and consumer have structured logging handlers, which is essential for production observability.

**CONCERN: No `SessionTimeoutMs` configuration.** The default `session.timeout.ms` is 45000ms (45 seconds). This means a crashed consumer takes 45 seconds to be detected and trigger a rebalance. Some teams prefer a lower value (e.g., 10-15 seconds) for faster failover.
- **Recommendation:** Consider making this configurable or documenting the default.

**CONCERN: No `HeartbeatIntervalMs` configuration.** Related to session timeout -- the heartbeat interval should be roughly 1/3 of the session timeout. Relying on defaults is fine but should be documented.

---

## 11. Message Key Strategy

### Assessment

**GOOD: Using CorrelationId (fallback MessageId) as key.** This ensures that messages related to the same entity/saga end up in the same partition, which is important for ordering guarantees within a saga or entity lifecycle.

**CONCERN: Key can be null.** If neither `CorrelationId` nor `MessageId` is set, the key is null. Null-keyed messages are round-robin distributed across partitions, which breaks any ordering assumptions.
- For events (which should always have a correlation ID), this is likely fine.
- For commands and requests that might not have a correlation ID set, this could lead to unexpected ordering.
- **Recommendation:** Consider generating a default key (e.g., from the message body hash or a random UUID) to ensure consistent partitioning. Or at minimum, log a warning when key is null.

---

## Summary Table

| # | Area | Finding | Category |
|---|------|---------|----------|
| 1 | Topic naming | Kebab-case, deterministic, human-readable | **GOOD** |
| 2 | Topic naming | Dots in topic names interact with JMX metrics | **CONCERN** |
| 3 | Topic naming | No length validation (249 char limit) | **CONCERN** |
| 4 | Error topics | Dedicated per-source, recursive guard | **GOOD** |
| 5 | Error topics | No retention policy on error topics | **CONCERN** |
| 6 | Consumer groups | Correct fan-out for pub/sub, competing for commands | **GOOD** |
| 7 | Consumer groups | Collision risk when ServiceName is null | **CONCERN** |
| 8 | Consumer groups | No CooperativeSticky assignment | **CONCERN** |
| 9 | Partitions | Default partition count of 1 is safe but limiting | **CONCERN** |
| 10 | Replication | Default RF=1 undermines at-least-once guarantees | **ISSUE** |
| 11 | Reply topics | Instance-specific with short retention | **GOOD** |
| 12 | Reply topics | No cleanup on shutdown, orphan accumulation | **CONCERN** |
| 13 | Reply topics | Scaling creates topic proliferation (GUID per instance) | **CONCERN** |
| 14 | Auto-provisioning | Single batch admin call, correct TopicAlreadyExists handling | **GOOD** |
| 15 | Auto-provisioning | No configuration mismatch detection | **CONCERN** |
| 16 | Offset management | Manual commit, at-least-once, sequential | **GOOD** |
| 17 | Offset management | No commit batching (perf concern for high throughput) | **CONCERN** |
| 18 | Producer | Acks=All, idempotent, callback-based produce | **GOOD** |
| 19 | Producer | Graceful shutdown with inflight tracking | **GOOD** |
| 20 | Consumer | No CooperativeSticky partition assignment | **CONCERN** |
| 21 | Message keys | CorrelationId/MessageId as key, can be null | **CONCERN** |

---

## Priority Recommendations

### Must Fix (before production use)

1. **Default replication factor should not be 1.** Change to `-1` (broker default) or `3`. RF=1 with `Acks=All` is a false sense of safety.

### Should Fix (before GA)

2. **Default to CooperativeSticky partition assignment.** One line change, significant improvement to rebalance behavior.
3. **Add retention policy to error topics.** Suggest `retention.ms=2592000000` (30 days).
4. **Warn or require ServiceName for subscribe endpoints.** Prevents silent consumer group collisions.
5. **Clean up reply topics on shutdown** or use stable instance identifiers to prevent topic proliferation.

### Nice to Have

6. Replace dots with hyphens in publish topic names (or make configurable).
7. Add topic name length validation.
8. Add topology validation mode that warns on configuration mismatches.
9. Consider commit batching for high-throughput scenarios.
10. Document expected topic count scaling behavior.
