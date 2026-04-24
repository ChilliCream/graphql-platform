# MessageLockLost handling in `AzureServiceBusAcknowledgementMiddleware`

## 1. Where the code lives

The snippet in the question is **not** in `AzureServiceBusReceiveEndpoint.cs`. It lives in:

- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/Middlewares/Receive/AzureServiceBusAcknowledgementMiddleware.cs`

The receive endpoint (`AzureServiceBusReceiveEndpoint.cs`) sets up the `ServiceBusProcessor` with:

- `ReceiveMode = ServiceBusReceiveMode.PeekLock`
- `AutoCompleteMessages = false` (the middleware owns settlement)
- `MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(5)`
- `MaxConcurrentCalls`, `PrefetchCount` derived from configuration

Note: that endpoint also has an `IsTransientProcessorError` helper for the **`ProcessErrorAsync`** path (logging-only) that classifies `ServiceCommunicationProblem`, `ServiceBusy`, `ServiceTimeout`, `MessageLockLost`, `SessionLockLost` as transient — but that helper is purely for log-level selection, not for settlement decisions. The settlement-time guard we are auditing is the one in the middleware.

The middleware has three settle paths:

1. `CompleteAsync` (success) — guarded against `MessageLockLost`.
2. `AbandonAsync` (failure path inside the outer `catch`) — guarded against `MessageLockLost`.
3. There is **no** `DeadLetterMessageAsync` call in the middleware itself. The comment in `CompleteAsync` says the lost lock can also happen because *user code* dead-lettered via `IAzureServiceBusMessageContext` before middleware reached `CompleteAsync`. (I did not find an in-tree implementation that exposes a `DeadLetterAsync` on `IAzureServiceBusMessageContext`; in the current file set, the only `DeadLetter*` references are topology configuration — `UseNativeDeadLetterForwarding`, `ForwardDeadLetteredMessagesTo` — which are auto-forwarding rules and not user-facing settle calls.)

## 2. Why the swallow exists at all

In `PeekLock` mode the broker holds an **exclusive, time-bounded lock** on the message. Settling (`Complete`/`Abandon`/`DeadLetter`) requires that lock still to be valid on the service side. If the lock is no longer valid, the broker rejects the settle call with `ServiceBusException { Reason = MessageLockLost }`.

When this exception is raised, the message has already left the receiver's hands as far as the broker is concerned — either:

- the lock genuinely expired (broker has already released the message back to the queue for redelivery, or, if `MaxDeliveryCount` was hit, moved it to the DLQ), **or**
- the message was already settled by a previous call (a parallel `DeadLetter` path, or a stale retry).

In **both** cases there is nothing the client can do to recover: it cannot extend an expired lock, and it cannot re-settle an already-settled message. Microsoft's own guidance (Service Bus messaging exceptions doc) is explicit: *"the client must dispose of the message"* — i.e. swallow and move on. Throwing further would only re-bubble a non-actionable error.

## 3. Why specifically `MessageLockLost` and nothing else

The official Azure SDK reference for `ProcessMessageEventArgs.AbandonMessageAsync`, `CompleteMessageAsync`, and `DeadLetterMessageAsync` lists exactly **two** documented `ServiceBusException` reasons:

| Reason | Applies to |
| --- | --- |
| `MessageLockLost` | non-session entities |
| `SessionLockLost` | session-enabled entities |

The current Mocha topology does **not enable sessions** — there is no `RequiresSession = true` path being exercised on the receive side, so `SessionLockLost` is not reachable in the current code paths. `MessageLockLost` is therefore the only documented reason that can come out of these settle calls under normal operation.

Per the official `ServiceBusFailureReason` enum, the other reasons (`MessagingEntityNotFound`, `MessagingEntityDisabled`, `MessageNotFound`, `MessageSizeExceeded`, `QuotaExceeded`, `ServiceBusy`, `ServiceTimeout`, `ServiceCommunicationProblem`, `SessionCannotBeLocked`, `MessagingEntityAlreadyExists`, `GeneralError`) are not documented as outcomes of a settle on a peek-locked message:

- `MessagingEntityNotFound` / `MessagingEntityDisabled` — would have failed at processor start, not on a per-message settle. If they did somehow fire here, swallowing them would mask a misconfigured/deleted queue, which is a real fault we want to surface.
- `MessageNotFound` — applies to `Receive(sequenceNumber)` style operations, not to settling an in-hand locked message.
- `MessageSizeExceeded` — outbound (send) only.
- `QuotaExceeded` — outbound (send) into a full entity.
- `ServiceBusy` / `ServiceTimeout` / `ServiceCommunicationProblem` — these are transient transport-layer faults. The Azure SDK has built-in retry policy that handles them transparently underneath the `Abandon/Complete/DeadLetter` call. By the time one surfaces to user code, the SDK has already exhausted its retries; rethrowing is the correct behaviour. Note also that a transient transport failure that drops the AMQP link **is itself a cause of `MessageLockLost`** on the next attempt — the SDK reconnects, but the lock cannot be carried across links, so the failure ultimately surfaces as `MessageLockLost`, which is exactly what we already swallow.

So: the guard is correctly narrow. Broadening it to `ServiceBusy/Timeout/CommunicationProblem` would silently drop genuine transport failures that the bus already retried internally — those should surface, not be swallowed.

## 4. What happens to the message when the lock is lost

From the Microsoft docs ("Message transfers, locks, and settlement" and "Service Bus messaging exceptions (.NET)"):

- The broker considers the lock released and **re-queues the message for redelivery** to the next receiver that pulls.
- The delivery count is **not** incremented purely because of an SDK-internal AMQP link drop and reconnect (per the docs' note about the 10-min idle timeout / AMQP detach: "the delivery count of the message isn't incremented"). It *is* incremented on a normal lock expiry / explicit abandon path.
- If `MaxDeliveryCount` is exceeded after redelivery, the broker moves the message to the dead-letter sub-queue automatically.

In other words: the message is **never lost** when we swallow `MessageLockLost`. It gets another try, capped by the queue's `MaxDeliveryCount`, with eventual DLQ as the safety net. Swallowing is therefore semantically equivalent to "let the lock expire naturally."

## 5. Comparison of the three settle calls in the file

| Call | Has guard? | Should it? | Notes |
| --- | --- | --- | --- |
| `CompleteAsync` | Yes (`MessageLockLost`) | Yes — correct | Comment correctly notes the "already settled by user-code DeadLetter" case and the "lock expired" case. |
| `AbandonAsync` | Yes (`MessageLockLost`) | Yes — correct | Already inside a try/catch wrapper that also swallows everything else (line 33–40), but the inner guard is still useful to avoid a noisy log of an expected condition. |
| `DeadLetterMessageAsync` | N/A here | N/A | Not invoked from the middleware in this file. If a user-facing `IAzureServiceBusMessageContext.DeadLetterAsync` is added later, it should apply the same guard since the API has the same documented exception contract. |

## 6. One real gap: `SessionLockLost`

The middleware does not set `RequiresSession = true` anywhere in the receive path I can see, so today this is moot. **But** if session support is added later (the `AzureServiceBusDefaultQueueOptions.RequiresSession` knob exists in topology config), the same swallow needs to extend to `SessionLockLost`, because per the SDK docs that is the session-mode equivalent of `MessageLockLost` and is the *only* other documented failure reason for these settle methods.

Suggested forward-compatible filter that still stays narrow:

```csharp
catch (ServiceBusException ex) when (
    ex.Reason == ServiceBusFailureReason.MessageLockLost
    || ex.Reason == ServiceBusFailureReason.SessionLockLost)
{
    // Lock expired or message already settled. Broker will redeliver
    // (or dead-letter when MaxDeliveryCount is exceeded). Nothing else to do.
}
```

This is consistent with how the same file's `AzureServiceBusReceiveEndpoint.IsTransientProcessorError` already handles both reasons together. Aligning the middleware with that classification would be the single small improvement worth making — and it costs nothing in the no-session case because that branch will simply never execute.

## 7. Recommendation

- **Current handling is correct** for the existing non-session topology. Do not broaden it to `ServiceBusy/Timeout/CommunicationProblem` — those are real faults that the SDK has already retried, and silently dropping them would mask outages.
- **Add `SessionLockLost`** to the `when` filter on both `CompleteAsync` and `AbandonAsync` as a cheap, future-proof change so that turning on `RequiresSession` later doesn't break the settlement contract.
- If/when a user-facing `IAzureServiceBusMessageContext.DeadLetterAsync` is exposed, wrap it with the same guard — its documented exception contract is identical to `Abandon`/`Complete`.

## 8. Sources

- ServiceBusFailureReason enum (full list of values): https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.servicebusfailurereason?view=azure-dotnet
- ProcessMessageEventArgs.AbandonMessageAsync (documented exceptions): https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.processmessageeventargs.abandonmessageasync?view=azure-dotnet
- ProcessMessageEventArgs.CompleteMessageAsync (documented exceptions): https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.processmessageeventargs.completemessageasync?view=azure-dotnet
- ProcessMessageEventArgs.DeadLetterMessageAsync (documented exceptions): https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.processmessageeventargs.deadlettermessageasync?view=azure-dotnet
- Service Bus messaging exceptions, "Reason: MessageLockLost": https://learn.microsoft.com/azure/service-bus-messaging/service-bus-messaging-exceptions-latest#reason-messagelocklost
- Message transfers, locks, and settlement: https://learn.microsoft.com/azure/service-bus-messaging/message-transfers-locks-settlement#settling-receive-operations
- Queue/Subscription LockDuration (max 5 min, default 60 s): https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.administration.queueproperties.lockduration?view=azure-dotnet
