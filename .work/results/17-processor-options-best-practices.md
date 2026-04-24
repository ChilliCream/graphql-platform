# `ServiceBusProcessorOptions` Best Practices for the Mocha ASB Transport

## 1. Code Under Review

File: `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/AzureServiceBusReceiveEndpoint.cs` (lines 80-87)

```csharp
var options = new ServiceBusProcessorOptions
{
    MaxConcurrentCalls = _maxConcurrentCalls,             // user-configurable, clamped [1, 1000]
    AutoCompleteMessages = false,                          // middleware does ack/nack
    ReceiveMode = ServiceBusReceiveMode.PeekLock,
    PrefetchCount = _prefetchCount,                        // user-configurable, defaults to MaxConcurrency * 2
    MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(5),  // hardcoded
};
```

Public override surface today (`AzureServiceBusReceiveEndpointConfiguration` + `IAzureServiceBusReceiveEndpointDescriptor`):

- `MaxConcurrency(int)` — surfaced
- `PrefetchCount(int?)` — surfaced
- `MaxAutoLockRenewalDuration` — **not** surfaced
- `Identifier`, `ReceiveMode` — **not** surfaced

Defaults pipeline (`AzureServiceBusDefaultEndpointOptions`) only carries `PrefetchCount` and `MaxConcurrency`.

---

## 2. Per-Option Findings (from Microsoft Learn / Azure SDK source)

### `MaxConcurrentCalls`

- SDK default: **1** (`ServiceBusProcessorOptions.MaxConcurrentCalls`).
- Defines the max concurrent invocations of the `ProcessMessageAsync` callback the processor will issue. For a session processor, total concurrency is `MaxConcurrentSessions * MaxConcurrentCallsPerSession` instead.
- Microsoft (Java SDK guidance, equally valid for the .NET dispatch model) recommends capping concurrency per processor instance to **~20-30** and going horizontal beyond that with multiple `ServiceBusClient` instances. The boundedElastic thread-pool guidance is Java-specific, but the underlying message holds in .NET as well: very high values starve the host's thread pool when work is even mildly synchronous.
- The Azure Functions/WebJobs default for the same knob is `16 * GetProcessorCount()` (e.g., 128 on an 8-core box). That target assumes async I/O-bound handlers.
- `PrefetchCount` is a **strict upper bound** on parallel work: if `MaxConcurrentCalls > PrefetchCount`, the extra slots stay idle. Microsoft docs make this explicit:
  > "It's important to note that `PrefetchCount` defines the maximum number of messages that can exist in the local buffer at any time. This also means that it acts as a strict upper limit on how many messages can be processed concurrently."
- Mocha clamp `[1, 1000]` is reasonable, but values >100 should at least raise an internal warning unless the user has explicitly tuned the prefetch count, lock duration, and host thread pool.

### `AutoCompleteMessages = false` — required for middleware ack/nack? **Confirmed.**

- SDK default: **true**. When true, the SDK calls `CompleteMessageAsync` after the user handler returns successfully and `AbandonMessageAsync` if the handler throws.
- Setting `false` is the **correct and required** choice when the application owns settlement (which Mocha middleware does — it can complete, abandon, dead-letter, defer, or schedule a retry). Leaving it `true` would cause double-completion attempts (`MessageLockLost` exceptions) and silently override middleware decisions like dead-letter.
- Important nuance from the docs: even with `AutoCompleteMessages = false`, **if the handler throws and has not settled the message, the SDK still auto-abandons it**. So Mocha middleware MUST catch and convert handler exceptions into explicit settlement actions if it wants to control the abandon-vs-deadletter decision; otherwise the SDK abandons (incrementing `DeliveryCount`) on its own.
- `disableAutoComplete()` is the documented-recommended pattern for "explicit, manual settlement" in both .NET and Java samples. The Mocha choice matches the Azure SDK guidance.

### `ReceiveMode.PeekLock` vs `ReceiveAndDelete`

- SDK default: **PeekLock**. Mocha matches the default, which is correct.
- **PeekLock** (at-least-once delivery): broker holds an exclusive lock; client must complete/abandon/deadletter; required for any of the following: dead-lettering, deferring, transactions, retry on failure, lock renewal. This is the only sane default for a general-purpose framework like Mocha.
- **ReceiveAndDelete** (at-most-once): the broker considers the message settled the moment it is dispatched. If the consumer crashes mid-processing, the message is **lost forever**. Service Bus does not support transactions in this mode. Use only for telemetry-style fire-and-forget data with low individual value.
- Recommendation for Mocha: keep `PeekLock` as the only supported mode for now. If `ReceiveAndDelete` is ever exposed, gate it behind an explicit opt-in on the descriptor (e.g., `.UnsafeAtMostOnce()`) and disable retry, dead-letter, scheduling middleware on those endpoints.

### `PrefetchCount`

- SDK default: **0** (no prefetch — processor pulls one message at a time per concurrency slot, paying a full network round-trip per message).
- Definition: maximum messages cached locally; cache is per-receiver, not shared.
- **Critical interaction with locks**: when prefetched, the broker locks the message immediately. The lock timer starts at prefetch, **not** at handler dispatch. So a message sitting in the prefetch buffer can lose its lock before the handler ever sees it. From the docs:
  > "If the prefetch buffer is large, and processing takes so long that message locks expire while staying in the prefetch buffer or even while the application is processing the message, there might be some confusing events for the application to handle."
- Microsoft formulas (in order of usefulness):
  1. **General throughput rule of thumb**: `PrefetchCount = 20 × max_processing_rate_per_receiver_per_second`. Example from docs: 3 receivers × 10 msg/s each → cap at ~600.
  2. **Lock-safe rule (more conservative, more correct)**: `PrefetchCount ≤ n / 3 × messages_per_second`, where `n` is the lock duration in seconds. The "/3" leaves headroom for renewal failures and processing variance.
  3. **Cheap default when nothing is known**: `~20 × MaxConcurrentCalls` — but only if message processing fits inside `LockDuration` with margin.
- Special-case guidance from Microsoft's "scenarios" page:
  - Single high-throughput consumer: `20 × MaxConcurrentCalls`.
  - **Many consumers competing on one queue**: keep prefetch **small (e.g., 10)** so one greedy receiver doesn't lock messages others could be processing.
  - Low-latency, single client: prefetch a small amount so it's already in cache.
  - Low-latency, multiple clients: set to **0** so dispatch is shared evenly.
- **Mocha's current default `MaxConcurrency * 2` is conservative and safe.** It eliminates the worst case (PrefetchCount=0 → one round-trip per message) without committing to a value that could cause lock-expiry storms. It is meaningfully lower than the throughput-optimal 20×, which is fine for a framework default — users who care about peak throughput will tune it.

### `MaxAutoLockRenewalDuration`

- SDK default: **5 minutes** (`TimeSpan.FromMinutes(5)`). Mocha hardcodes the SDK default.
- Mechanics: a Service Bus queue/subscription has a `LockDuration` set at the entity level (default 1 minute, max 5 minutes). The processor's background task auto-renews the lock for up to `MaxAutoLockRenewalDuration` so handlers can run longer than `LockDuration`. Setting to `TimeSpan.Zero` (or null on Java) disables auto-renewal entirely.
- "What `TimeSpan.FromMinutes(5)` means": the renewer keeps refreshing the lock for **at most 5 minutes of wall-clock time from when the message was dispatched to the handler**. After that, the renewer stops; the next missed renewal causes the broker to release the lock; the next settlement attempt throws `MessageLockLost`.
- When to increase: if handlers can legitimately exceed 5 minutes (long-running batch jobs, external API calls with high tail latency, large file processing). Set to a value greater than the *p99* of handler runtime, not the median.
- When to decrease / set to `Zero`: when the entity-level `LockDuration` is intentionally tight (a few seconds, with retries handling the redelivery), or when you want misbehaving handlers to surface as lock-loss quickly rather than holding messages hostage.
- **`Timeout.InfiniteTimeSpan` is documented as supported.** A handler that hangs forever will hold the lock forever — see anti-patterns.
- Background renewal can continue briefly after settlement, producing transient `MessageLockLostException` warnings — documented behavior, not a bug.

### Options Mocha currently does NOT set

- **`Identifier`** — `string`, default null/empty (SDK assigns a random GUID-like id). Showing up in `ProcessErrorEventArgs.Identifier`, exception messages, and AMQP traces. Mocha should set this to something descriptive like `$"mocha:{transport.Name}:{queueName}"` for diagnostics — multiple Mocha endpoints in one process get distinguishable log lines for free.
- **`SessionIdleTimeout`** — session-processor only. Default falls back to `ServiceBusRetryOptions.TryTimeout` (60 seconds out of the box). Time the processor waits for the next message in the active session before releasing it. Not relevant until Mocha supports session-enabled queues.
- **`MaxConcurrentSessions`** — session-processor only. Default **8**. Number of sessions held concurrently per processor. Total concurrency is `MaxConcurrentSessions × MaxConcurrentCallsPerSession`.
- **`MaxConcurrentCallsPerSession`** — session-processor only. Default **1**. Increasing this only makes sense for sessions where the user explicitly opts out of in-session ordering, which defeats the main point of sessions; default 1 is correct.

---

## 3. Anti-Patterns (Concrete and Documented)

### Anti-pattern 1: `PrefetchCount` set far above what `LockDuration` can absorb (lock-expiry storm)

**Symptom**: messages arrive at the handler with `LockedUntilUtc` already in the past or seconds away; settlement throws `MessageLockLost`; `DeliveryCount` climbs; messages eventually go to the DLQ via max-delivery-count without ever being processed.

**Why**: prefetch locks the message at fetch time, not at dispatch time. If `(prefetched_messages_ahead_of_current × per_message_processing_time) > LockDuration`, the messages waiting in the buffer time out before they are reached.

**Mocha guard**: clamp the default at `MaxConcurrency × 2`. Surface but do not auto-shrink user-supplied values. Document the formula `PrefetchCount ≤ LockDuration_seconds / 3 × processing_rate_per_second` in the descriptor's XML doc.

### Anti-pattern 2: `MaxAutoLockRenewalDuration = TimeSpan.MaxValue` (or `InfiniteTimeSpan`)

**Symptom**: a hung handler (deadlock, infinite loop, blocked on a never-completing remote call) holds its lock forever. The message is invisible to other consumers; the queue depth balloons; restarting the host re-locks the same message; no path to recovery without manual intervention.

**Why**: infinite renewal means the broker never gets a chance to redeliver, and `MaxDeliveryCount` never trips so the message can't even reach the DLQ.

**Mocha guard**: validate user-supplied values; reject `Timeout.InfiniteTimeSpan` outright (or require a separate `.UnsafeInfiniteLockRenewal()` opt-in). Enforce a practical upper bound — e.g., 1 hour — and require explicit opt-in past that.

### Anti-pattern 3: High `MaxConcurrentCalls` + synchronous (blocking) handlers

**Symptom**: thread-pool starvation. The .NET `ThreadPool` injects new threads slowly (~1 per 500ms after the initial burst). With `MaxConcurrentCalls = 100` and synchronous handlers blocking on I/O, the SDK's own AMQP receive loop, lock renewer, and error path can all be starved, producing cascading `MessageLockLost`, missed heartbeats, and connection re-establishment storms.

**Why**: `ServiceBusProcessor` dispatches via `Task.Run`-style continuations on the thread pool; blocking calls don't free threads back. The Java SDK boundedElastic guidance ("20-30 threads per CPU core") translates directly: keep concurrent blocking work below `Environment.ProcessorCount × ~25`.

**Mocha guard**: cannot detect at runtime, but clamp default to a safe value (1) and document in the descriptor that high `MaxConcurrency` requires fully async handlers.

### Anti-pattern 4: Using `ReceiveAndDelete` with retry/DLQ middleware (silent message loss)

**Symptom**: handler crashes; no retry happens; message is gone; nothing in the DLQ.

**Why**: in `ReceiveAndDelete`, the message is settled the moment the broker dispatches it. There is nothing for middleware to abandon, dead-letter, or retry against. Service Bus rejects deferral and transactions in this mode.

**Mocha guard**: keep `ReceiveAndDelete` unexposed for now. If exposed later, it must disable scheduling, retry, and dead-letter middleware for that endpoint at configuration time and surface a warning log on startup.

### Anti-pattern 5: `AutoCompleteMessages = true` while middleware also settles

**Symptom**: `MessageLockLost` exceptions on every successful processing, because the SDK completes the message after middleware already did. Or — worse — middleware dead-letters a message and the SDK then tries to complete it, also throwing.

**Why**: with `AutoCompleteMessages = true`, the SDK calls `CompleteMessageAsync` after the handler returns regardless of what middleware did. The two settlements race.

**Mocha guard**: keep hardcoded `AutoCompleteMessages = false`. Don't surface this knob — there is no scenario where Mocha middleware should not own settlement.

---

## 4. Recommended Defaults — Table

The "Override how" column reflects the descriptor surface Mocha should ship; items marked **(NEW)** require widening the descriptor.

| Option                          | Recommended Default                                    | Reasoning                                                                                                                                                                                  | Override How                                                          | Auto-tune?                                                                                |
| ------------------------------- | ------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | --------------------------------------------------------------------- | ----------------------------------------------------------------------------------------- |
| `MaxConcurrentCalls`            | Inherit from `ReceiveEndpointConfiguration.Defaults.MaxConcurrency` (currently 1, clamped [1, 1000]) | Keeps Mocha's transport-agnostic concurrency contract. SDK default is 1; framework defaults should match — users who want parallelism opt in. | `endpoint.MaxConcurrency(int)` (existing)                             | No. Auto-tuning concurrency requires runtime feedback the transport doesn't have today.   |
| `AutoCompleteMessages`          | **`false` (hardcoded)**                                                          | Required because middleware owns settlement (complete / abandon / dead-letter / defer / schedule). True would cause races and override middleware decisions.                              | Not user-configurable. Document the invariant.                        | N/A.                                                                                      |
| `ReceiveMode`                   | **`PeekLock` (hardcoded for now)**                                               | Only mode that supports retry, dead-letter, deferral, transactions, and lock renewal — all of which Mocha relies on.                                                                        | Not exposed today. If exposed later, gate `ReceiveAndDelete` behind `.UnsafeAtMostOnce()`. | N/A.                                                                                      |
| `PrefetchCount`                 | `MaxConcurrency * 2` when not set                                                | Beats the 0-default round-trip-per-message floor, stays well below the `LockDuration / processing_time` cliff. Conservative — users who profile their workload can raise it.            | `endpoint.PrefetchCount(int?)` (existing)                             | Not yet. Could be derived from a measured `LockDuration` once the topology query exposes it. |
| `MaxAutoLockRenewalDuration`    | `TimeSpan.FromMinutes(5)` (matches SDK default)                                  | 5 min covers the vast majority of long-running handlers without the unbounded-handler trap. Same as SDK default, so behaviour is predictable.                                              | **(NEW)** add `endpoint.MaxAutoLockRenewalDuration(TimeSpan)` and reject `InfiniteTimeSpan` / `MaxValue`. | Auto-tune is unsafe; this controls failure semantics, not throughput.                     |
| `Identifier`                    | `$"mocha:{transport.Name}:{Queue.Name}"` **(NEW default)**                       | The SDK assigns a random GUID-suffix when null. A stable, descriptive identifier makes `ProcessErrorEventArgs`, AMQP traces, and Service Bus diagnostics readable across endpoints.        | **(NEW)** allow override via `endpoint.Identifier(string)` for advanced users. | N/A — derived from existing names.                                                        |
| `SessionIdleTimeout`            | unset (use SDK default = `RetryOptions.TryTimeout`, 60s) — only relevant once sessions are supported | Not applicable to non-session processors today. When sessions are added, default to ~30-60s so idle sessions release promptly and let the processor pick up active sessions.            | **(NEW, future)** `sessionEndpoint.SessionIdleTimeout(TimeSpan)`.     | No.                                                                                       |
| `MaxConcurrentSessions`         | unset until sessions are supported; SDK default is **8**                                              | Default 8 is reasonable when sessions exist; many session-aware workloads need to tune this.                                                                                                | **(NEW, future)** `sessionEndpoint.MaxConcurrentSessions(int)`.       | No.                                                                                       |
| `MaxConcurrentCallsPerSession`  | unset; SDK default **1**                                                                              | Increasing >1 destroys in-session ordering, the entire point of sessions. Default 1 is correct.                                                                                             | **(NEW, future)** advanced override only.                             | No.                                                                                       |

---

## 5. Concrete Suggested Changes for Mocha

1. **Stop hardcoding `MaxAutoLockRenewalDuration`.** Pull it from `AzureServiceBusReceiveEndpointConfiguration.MaxAutoLockRenewalDuration` (new nullable property) with `TimeSpan.FromMinutes(5)` as the fallback. Validate input: reject negative values and reject `InfiniteTimeSpan` / `TimeSpan.MaxValue` unless an explicit opt-in flag is set.
2. **Set `Identifier`** in `OnStartAsync` to `$"mocha:{Queue.Name}"` (or include the transport instance name if multiple namespaces are configured). Costs nothing and makes diagnostics dramatically easier.
3. **Document in XML doc on `PrefetchCount(int?)`:**
   - Default formula and what `null` means.
   - The lock-expiry storm anti-pattern.
   - The recommended ceiling: `(LockDuration_seconds / 3) × messages_per_second`.
4. **Document the `MaxConcurrency × synchronous-handler` interaction** in XML doc on `MaxConcurrency(int)` — point users at fully-async handlers when raising it.
5. **Do not expose `AutoCompleteMessages` or `ReceiveMode`** for now. They are correctness invariants for the middleware pipeline, not tuning knobs.
6. **Optional follow-up**: when `AzureServiceBusMessagingTopology` knows the configured `LockDuration` of each queue, validate `MaxAutoLockRenewalDuration > LockDuration` at configuration time (`OnComplete`) and warn (or fail) if not — Microsoft explicitly states this requirement and it is a common misconfiguration.

## 6. Sources

- `ServiceBusProcessorOptions.MaxConcurrentCalls` (default 1): https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.servicebusprocessoroptions.maxconcurrentcalls
- `ServiceBusProcessorOptions.AutoCompleteMessages` (default true; auto-abandon on exception): https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.servicebusprocessoroptions.autocompletemessages
- `ServiceBusProcessorOptions.MaxAutoLockRenewalDuration` (default 5 min, supports `InfiniteTimeSpan`): https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.servicebusprocessoroptions.maxautolockrenewalduration
- `ServiceBusProcessorOptions.Identifier`: https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.servicebusprocessoroptions.identifier
- `ServiceBusSessionProcessorOptions.MaxConcurrentSessions` (default 8): https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.servicebussessionprocessoroptions.maxconcurrentsessions
- Performance best practices (`20 × max processing rate`, prefetch-vs-lock interaction, ReceiveMode comparison): https://learn.microsoft.com/azure/service-bus-messaging/service-bus-performance-improvements
- Prefetch deep-dive (lock-expiry storm, FIFO breakage, prefetch buffer caveats): https://learn.microsoft.com/azure/service-bus-messaging/service-bus-prefetch
- Message transfers/locks/settlement (PeekLock vs ReceiveAndDelete, lock duration limits): https://learn.microsoft.com/azure/service-bus-messaging/message-transfers-locks-settlement
- Java SDK concurrency guidance (per-processor cap of ~20-30, scale horizontally): https://learn.microsoft.com/azure/developer/java/sdk/troubleshooting-messaging-service-bus-overview
