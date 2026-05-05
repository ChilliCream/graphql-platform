# Plan: Native Scheduling, Native Dead-Lettering, and `_skipped` Fix

## Scope

One PR, three tightly-related changes:

1. **Core fix — `_skipped` actually receives unmatched-consumer messages** (`ReceiveDeadLetterMiddleware` currently writes them to `_error`, which buries them next to real faults). Cross-transport; one-file fix.
2. **ASB native scheduling** — integrate with Mocha's post-merge scheduling contracts (`SchedulingTransportFeature`, `IScheduledMessageStore`, `ScheduledMessageFeature`) so scheduled sends use `ServiceBusSender.ScheduleMessageAsync` and cancellation flows through `sender.CancelScheduledMessageAsync(sequenceNumber)`.
3. **ASB native dead-lettering** — power-user API (`ctx.AzureServiceBus().DeadLetterAsync(reason, description, props)`), opt-in `UseNativeDeadLetterForwarding()` descriptor sugar, and idempotent settlement in the ack middleware.

## Non-Goals (Deliberately Dropped)

Each of these was explored, reviewed, and removed with a reason:

- **`PoisonMessageException` (as an exception type).** Imperative `ctx.AzureServiceBus().DeadLetterAsync(...)` covers the common case without throw-as-control-flow on the hot path. One way to do it.
- **Core middleware changes for poison signals.** None needed — the ack middleware's existing try/catch plus idempotent settlement handles every path.
- **Cross-transport `IMessageSettlement` / capability flags.** YAGNI until a second transport (e.g. RabbitMQ DLX) asks for the same shape.
- **`FromDeadLetterQueue()` receive endpoint.** MassTransit actively discourages consuming from DLQ ("dead-dead-letter has nowhere to go"). Users who need replay drop to the `ClientManager` escape hatch.
- **Stripping `_error` / `_skipped` topology defaults on ASB.** Breaks cross-transport portability (same `descriptor.Endpoint(…)` would behave differently on ASB vs Postgres). Keep defaults; fix the `_skipped` wiring so both actually work.
- **`IAzureServiceBusScheduler` service.** The framework's `IMessageBus.ScheduleSendAsync` / `SchedulePublishAsync` / `CancelScheduledMessageAsync` already cover everything. An ASB-specific service would be duplicate surface.
- **Reschedule on receive side, `Defer`, programmatic `RenewLock`.** `MaxAutoLockRenewalDuration` covers long-running handlers; Defer needs receive-side plumbing Mocha doesn't have.

---

## Change 1 — Core: `_skipped` Gets Populated

**The bug.** `ReceiveDeadLetterMiddleware` terminates as the safety net when no handler consumed the message. It currently forwards to `ErrorEndpoint` unconditionally. `ReceiveFaultMiddleware` already handles handler-exceptions correctly (fault metadata, response-address NACK, writes to `ErrorEndpoint`, sets `MessageConsumed = true`). So the DeadLetter middleware only fires on the "no consumer matched" path — and it should write to `SkippedEndpoint`, not `ErrorEndpoint`.

Result today: `_skipped` is provisioned on every receive endpoint across all transports and never receives a message. `_error` receives both faults *and* unmatched types, making dashboards noisy.

**The fix.** In `src/Mocha/src/Mocha/Middlewares/Receive/ReceiveDeadLetterMiddleware.cs`:

- `Create()` reads `context.Endpoint.SkippedEndpoint` instead of `ErrorEndpoint`, short-circuits to `next` when null.
- The ctor parameter and the field inside `InvokeAsync` are renamed accordingly.
- The `catch (Exception ex) { logger.ExceptionOccurred(ex); }` block stays as a final safety net in case Fault MW itself throws during error-endpoint dispatch. Rare but cheap.

Body of the middleware is otherwise unchanged: forwards the original envelope to the configured endpoint, sets `MessageConsumed = true`.

**Net behavior after the fix.**

| Condition | Middleware | Destination |
|---|---|---|
| Handler threw | `ReceiveFaultMiddleware` | `_error` (unchanged) |
| No handler matched | `ReceiveDeadLetterMiddleware` | `_skipped` (was incorrectly `_error`) |
| Both endpoints null | (both short-circuit) | Transport-specific: ASB → MaxDeliveryCount → `$DeadLetterQueue` |

**Migration call-out.** Users with dashboards on `_error` will see the "no matching consumer" traffic move to `_skipped`. This matches the documented data-model intent and MassTransit's long-standing convention. Surface prominently in the PR description.

---

## Change 2 — ASB Native Scheduling

Mocha core (post-merge) ships:
- `SendOptions.ScheduledTime`, `PublishOptions.ScheduledTime` wired through `DefaultMessageBus`.
- `MessageEnvelope.ScheduledTime` carries it across the dispatch pipeline.
- `IMessageBus.ScheduleSendAsync`, `SchedulePublishAsync`, `CancelScheduledMessageAsync(token)`.
- `DispatchSchedulingMiddleware` intercepts scheduled messages and persists to an `IScheduledMessageStore` by default. Skipped entirely if the transport sets `SchedulingTransportFeature.SupportsSchedulingNatively = true`.
- `ScheduledMessageFeature.Token` — the channel for a native transport to publish the opaque cancellation token back to the bus.

**ASB integration tasks:**

1. **Declare native support.** `AzureServiceBusMessagingTransport` sets `SchedulingTransportFeature { SupportsSchedulingNatively = true }` on its feature collection at construction. `DispatchSchedulingMiddleware` is then skipped for ASB dispatches; the envelope reaches `AzureServiceBusDispatchEndpoint` directly with `ScheduledTime` set.

2. **Dispatch endpoint honors the envelope.** In `AzureServiceBusDispatchEndpoint.DispatchAsync` (currently always calls `sender.SendMessageAsync`): when `envelope.ScheduledTime is { } when`, call `sender.ScheduleMessageAsync(message, when, cancellationToken)` instead, capture the returned `long sequenceNumber`, and write `context.Features.Configure<ScheduledMessageFeature>(f => f.Token = $"asb:{entityPath}:{sequenceNumber}")`. When `ScheduledTime` is null, behavior unchanged. Do NOT also set `message.ScheduledEnqueueTime` in this branch — `ScheduleMessageAsync` owns that field.

3. **Scheduled-message store for cancellation.** New `Scheduling/AzureServiceBusScheduledMessageStore.cs` implements `IScheduledMessageStore`:
   - `PersistAsync` throws `InvalidOperationException("AzureServiceBusScheduledMessageStore should never be called via middleware path; SupportsSchedulingNatively is true.")`. Documents the invariant — the dispatch path handles persistence by virtue of `ScheduleMessageAsync` itself.
   - `CancelAsync(token)` parses `"asb:{entityPath}:{sequenceNumber}"` (strict; errors return false), resolves the sender via `ClientManager.GetSender(entityPath)`, and calls `sender.CancelScheduledMessageAsync(seq, cancellationToken)`. Maps `ServiceBusException { Reason: MessageNotFound }` → `false`. All other exceptions propagate.

4. **Register the store.** In `MessageBusBuilderExtensions.AddAzureServiceBus`, `services.TryAddScoped<IScheduledMessageStore>(…)` (matches Postgres's registration shape).

5. **Document single-store-per-bus.** The framework resolves a single `IScheduledMessageStore` at `CancelScheduledMessageAsync` time. Registering ASB and Postgres in the same DI scope means only the first-registered store handles cancellation. Add an XML-doc note on the descriptor; cross-bus composition via token-prefix routing is a separate future RFC.

**Token format.** `"asb:{entityPath}:{sequenceNumber}"`. Entity names cannot contain `:` in ASB (confirmed against SDK validation rules), so the delimiter is unambiguous. Three-part split by `:` is strict — malformed tokens return `false` from `CancelAsync` without throwing.

**Flow:**

```
user bus.ScheduleSendAsync(msg, when)
  → DefaultMessageBus sets envelope.ScheduledTime = when; runs dispatch pipeline
  → DispatchSchedulingMiddleware sees SupportsSchedulingNatively → skipped
  → AzureServiceBusDispatchEndpoint.DispatchAsync:
      if envelope.ScheduledTime: seq = sender.ScheduleMessageAsync(msg, when)
                                 feature.Token = $"asb:{entityPath}:{seq}"
      else:                      sender.SendMessageAsync(msg)
  → SchedulingResult { Token, ScheduledTime, IsCancellable = true }

user bus.CancelScheduledMessageAsync(token)
  → DefaultMessageBus resolves IScheduledMessageStore → AzureServiceBusScheduledMessageStore
  → parses token "asb:{entity}:{seq}"
  → ClientManager.GetSender(entity).CancelScheduledMessageAsync(seq) → bool
```

---

## Change 3 — ASB Native Dead-Lettering

Three pieces:

### 3a. Idempotent settlement in `AzureServiceBusAcknowledgementMiddleware`

Today: `try { next; await CompleteMessageAsync } catch { await AbandonMessageAsync; throw; }`. Problem: if a handler (Ring 3 below) already called `args.DeadLetterMessageAsync(...)`, the outer `CompleteMessageAsync` will hit `MessageLockLost`. Issue #1920 in MassTransit documents this same class of bug.

Fix: wrap each settlement call — both `CompleteMessageAsync` and `AbandonMessageAsync` — with `catch (ServiceBusException { Reason: ServiceBusFailureReason.MessageLockLost })` that logs debug and swallows. Any other SDK exception still surfaces. Hardens every settlement path, not just the new handler-initiated DLQ path.

### 3b. Power-user imperative API

New file `src/Mocha/src/Mocha.Transport.AzureServiceBus/Features/IAzureServiceBusMessageContext.cs`:

```csharp
namespace Mocha.Transport.AzureServiceBus;

public interface IAzureServiceBusMessageContext
{
    ServiceBusReceivedMessage Message { get; }
    string EntityPath { get; }
    int DeliveryCount { get; }
    DateTimeOffset LockedUntil { get; }

    Task DeadLetterAsync(
        string reason,
        string? description = null,
        IDictionary<string, object>? properties = null,
        CancellationToken cancellationToken = default);

    Task AbandonAsync(
        IDictionary<string, object>? propertiesToModify = null,
        CancellationToken cancellationToken = default);
}

public static class AzureServiceBusContextExtensions
{
    public static IAzureServiceBusMessageContext AzureServiceBus(this IMessageContext ctx);
    public static bool TryGetAzureServiceBus(this IMessageContext ctx, out IAzureServiceBusMessageContext asb);
}
```

Facade implemented on the existing `AzureServiceBusReceiveFeature` (pooled — no extra allocation). Methods call `ProcessMessageEventArgs.DeadLetterMessageAsync(...)` / `AbandonMessageAsync(...)` directly. Handler returns normally afterward; the ack middleware then tries to Complete and the idempotent `MessageLockLost` catch handles it cleanly.

NOT public: `ProcessMessageEventArgs`, `Defer`, programmatic `RenewLock`, `SuppressAcknowledgement`.

**Usage:**

```csharp
public async ValueTask HandleAsync(ProcessInvoice msg, IMessageContext ctx)
{
    if (!msg.IsValid())
    {
        await ctx.AzureServiceBus().DeadLetterAsync(
            reason: "InvalidPayload",
            description: $"Missing customer id for invoice {msg.Id}");
        return;
    }
    // ...
}
```

### 3c. Opt-in native DLQ forwarding

New method on `IAzureServiceBusReceiveEndpointDescriptor`:

```csharp
IAzureServiceBusReceiveEndpointDescriptor UseNativeDeadLetterForwarding();
```

When called, the topology provisioning sets `queue.ForwardDeadLetteredMessagesTo = "{queueName}_error"`. Result: messages that ASB auto-DLQs (`MaxDeliveryCountExceeded`, `TTLExpiredException`) get forwarded into the Mocha-managed `_error` queue, consolidating ops surface. Off by default — users who want native DLQ as terminal destination don't set this.

Hard error at provisioning if both `UseNativeDeadLetterForwarding()` is set AND the user explicitly configured `queue.ForwardDeadLetteredMessagesTo` to some other destination. Mutual conflict.

---

## Files Touched

**Core:**
- `src/Mocha/src/Mocha/Middlewares/Receive/ReceiveDeadLetterMiddleware.cs` — switch `ErrorEndpoint` → `SkippedEndpoint`.

**ASB transport:**
- `AzureServiceBusMessagingTransport.cs` — set `SchedulingTransportFeature.SupportsSchedulingNatively = true`.
- `AzureServiceBusDispatchEndpoint.cs` — branch on `envelope.ScheduledTime`; populate `ScheduledMessageFeature.Token`.
- `Scheduling/AzureServiceBusScheduledMessageStore.cs` (new) — `IScheduledMessageStore` impl.
- `MessageBusBuilderExtensions.cs` — `TryAddScoped<IScheduledMessageStore>`.
- `Middlewares/Receive/AzureServiceBusAcknowledgementMiddleware.cs` — idempotent settlement.
- `Features/IAzureServiceBusMessageContext.cs` (new) — interface + accessor extensions.
- `Features/AzureServiceBusReceiveFeature.cs` — implement the interface on the pooled feature.
- `Descriptors/IAzureServiceBusReceiveEndpointDescriptor.cs` + `AzureServiceBusReceiveEndpointDescriptor.cs` — `UseNativeDeadLetterForwarding()`.
- `Topology/AzureServiceBusQueue.cs` — honor the flag during provisioning; conflict detection.

**Examples / tests:**
- `src/Mocha/examples/AzureServiceBusTransport/**` — one scheduled message (e.g. delayed reminder), one programmatic dead-letter demonstration.
- New integration tests against the Aspire-hosted ASB emulator for: scheduled send lands after T; cancellation before T removes the message; `DeadLetterAsync` writes reason + description + custom properties into `$DeadLetterQueue`; idempotent settlement after `DeadLetterAsync` succeeds without `MessageLockLost` escaping.
- New core unit test: unmatched-consumer message lands in `_skipped`, not `_error`.

## Phases

1. **Core `_skipped` fix.** One-file behavior change, cross-transport. Unit test: register a receive endpoint with no handler for a message type, publish it, assert it lands in `_skipped` not `_error`.
2. **ASB scheduling.** Transport feature flag + dispatch endpoint branch + scheduled-message store + DI registration. Integration tests: send-with-schedule arrives after T; cancel before T succeeds and prevents delivery.
3. **ASB idempotent settlement.** Small, mergeable alone. Hardens existing code independent of the handler-initiated DLQ path.
4. **ASB power-user API + opt-in forwarding.** `IAzureServiceBusMessageContext`, accessor, `UseNativeDeadLetterForwarding()`, topology validation. Integration tests for reason codes in `$DeadLetterQueue` and for forwarding to `_error`.
5. **Examples + PR description.** Demonstrate both features; call out the `_skipped` migration clearly.

## Backward Compatibility Summary

| Surface | Before | After |
|---|---|---|
| Handler throws on ASB | → `_error` via Fault MW | → `_error` via Fault MW (unchanged) |
| No matching consumer (all transports) | → `_error` via DeadLetter MW | → `_skipped` via DeadLetter MW **(migration call-out)** |
| `bus.ScheduleSendAsync(…)` on ASB | No-op at broker (middleware store path; only works if user registered one) | Native `ScheduleMessageAsync`; cancellation supported |
| `bus.CancelScheduledMessageAsync(…)` on ASB | Returns `false` | Calls `sender.CancelScheduledMessageAsync(seq)` |
| Handler wants broker DLQ with custom reason | Drop to `ClientManager` SDK | `ctx.AzureServiceBus().DeadLetterAsync(reason, …)` |
| `MaxDeliveryCountExceeded` messages | Land in `$DeadLetterQueue`, ops must check two places | Same, or forwarded to `_error` with `UseNativeDeadLetterForwarding()` |
| `_error` topology default | Auto-provisioned | Auto-provisioned (unchanged) |
| `_skipped` topology default | Auto-provisioned (but never written to) | Auto-provisioned + now actually populated |
