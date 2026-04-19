# Mocha Scheduling Implementation Inventory

## 1. Public API Surface for Scheduling

### SendOptions & PublishOptions
- **SendOptions.ScheduledTime** (property, init-only): `/workspaces/hc5/src/Mocha/src/Mocha/Execution/SendOptions.cs:11`
- **PublishOptions.ScheduledTime** (property, init-only): `/workspaces/hc5/src/Mocha/src/Mocha/Execution/PublishOptions.cs:11`
Both are now wired in DefaultMessageBus and properly set on dispatch contexts.

### MessageEnvelope
- **MessageEnvelope.ScheduledTime**: `/workspaces/hc5/src/Mocha/src/Mocha/Transport/MessageEnvelope.cs:112`
  New init-only property: "The earliest time at which the message should be made available for consumption."
- **MessageEnvelope.Properties.ScheduledTime** constant: `/workspaces/hc5/src/Mocha/src/Mocha/Transport/MessageEnvelope.cs:182`

### IMessageBus Scheduling Methods
All on IMessageBus interface at `/workspaces/hc5/src/Mocha/src/Mocha/IMessageBus.cs`:

- **SchedulePublishAsync<T>** (2 overloads): lines 106-124
  - Returns `ValueTask<SchedulingResult>` with cancellation token and metadata
  - Versions: `(message, scheduledTime, cancellationToken)` and with `PublishOptions`

- **ScheduleSendAsync** (2 overloads): lines 133-150
  - Returns `ValueTask<SchedulingResult>`
  - Versions: `(message, scheduledTime, cancellationToken)` and with `SendOptions`

- **CancelScheduledMessageAsync**: line 159-161
  - Signature: `ValueTask<bool> CancelScheduledMessageAsync(string token, CancellationToken cancellationToken)`
  - Returns true if cancelled, false if already dispatched/not found

### SchedulingResult Type
New record at `/workspaces/hc5/src/Mocha/src/Mocha/SchedulingResult.cs`:
- **Token** (string?): Opaque cancellation token, null if transport doesn't support cancellation
- **ScheduledTime** (DateTimeOffset): The scheduled delivery time
- **IsCancellable** (bool): Whether the message can be cancelled

### ScheduledMessageFeature
New pooled feature at `/workspaces/hc5/src/Mocha/src/Mocha/Features/ScheduledMessageFeature.cs`:
- **Token** property: Carries the opaque scheduling token from store back to message bus

## 2. How Transports Integrate Scheduling

### Architecture Pattern
Transports have **two integration paths**:

**Path A: Opt-in Middleware (Default)**
- Dispatch scheduling middleware intercepts outgoing messages with `ScheduledTime` set
- Middleware checks if `context.ScheduledTime` is not null (DispatchSchedulingMiddleware.cs:28)
- If true, persists envelope to `IScheduledMessageStore` instead of forwarding to transport
- Store returns opaque token; middleware stores it in `ScheduledMessageFeature.Token`
- Transport never sees the message

**Path B: Native Scheduling Support**
- Transport declares `SchedulingTransportFeature.SupportsSchedulingNatively = true`
- Middleware is skipped entirely (DispatchSchedulingMiddleware.cs:70)
- Transport receives the envelope directly and honors `envelope.ScheduledTime`

### Transport Implementation Status

**InMemory** (DefaultMessageBus wires ScheduledTime → context):
- Does NOT set `SupportsSchedulingNatively`
- Uses Path A (middleware-based scheduling)
- Reference: DefaultMessageBus line 68, 122

**RabbitMQ** (DefaultMessageBus wires ScheduledTime → context):
- Does NOT set `SupportsSchedulingNatively`
- Uses Path A (middleware-based scheduling)
- No native scheduled-time handling in DispatchAsync
- Reference: RabbitMQDispatchEndpoint.cs line 128-143 (no ScheduledTime property)

**Postgres** (Path B - Native Support):
- **DOES** set `SupportsSchedulingNatively = true` (PostgresMessagingTransport.cs)
- Passes `scheduledTime` parameter to messageStore.PublishAsync/SendAsync
- Reference: PostgresDispatchEndpoint.cs line 55 reads `envelope.ScheduledTime`
- Reference: PostgresDispatchEndpoint.cs line 75, 81, 92, 96 pass it to store

**Azure Service Bus** (No scheduling support yet):
- Does NOT set `SupportsSchedulingNatively`
- Does NOT read `envelope.ScheduledTime` in DispatchAsync
- Reference: AzureServiceBusDispatchEndpoint.cs line 99-184
- **This is the integration hook:** Wire ScheduledTime into `ServiceBusMessage` properties

## 3. Cancellation Support

**Full Support Implemented:**
- `IMessageBus.CancelScheduledMessageAsync(token)` returns `ValueTask<bool>`
- `SchedulingResult.Token` provides the opaque cancellation handle
- `SchedulingResult.IsCancellable` signals whether cancellation is available
- `IScheduledMessageStore.CancelAsync(token)` contract for implementations
- Reference: DefaultMessageBus.cs lines 414-427

**Dispatch Context Integration:**
- `IDispatchContext.ScheduledTime` property (DispatchContext.cs:157)
- DefaultMessageBus correctly sets it from options before pipeline execution

## 4. ASB Transport Status

**Current State: Unwired**
- AzureServiceBusDispatchEndpoint.DispatchAsync does NOT handle scheduling
- No `envelope.ScheduledTime` read or passed to ServiceBusMessage
- No `SupportsSchedulingNatively` flag set
- Reference: AzureServiceBusDispatchEndpoint.cs line 39-97

**Integration Hooks Available:**
1. Set `SupportsSchedulingNatively = true` in transport initialization (Path B), OR
2. Leave at false and let middleware-based scheduling handle it (Path A, current for InMemory/RabbitMQ)

**For native support (Path B):**
- Read `envelope.ScheduledTime` in DispatchAsync (like Postgres does)
- Set `ServiceBusMessage.ScheduledEnqueueTimeUtc` property
- ASB SDK supports this natively

## 5. Tests Added

**Core Scheduling Tests** (Mocha.Tests/Scheduling/):
- **SchedulingMiddlewareIntegrationTests.cs** (168+ lines): Integration tests with actual store
  - Tests: scheduled message dispatch, cancellation success/failure scenarios
  - Line 168: `CancelScheduledMessageAsync_Should_RemoveFromStore_When_ValidToken`
  - Line 199: `CancelScheduledMessageAsync_Should_ReturnFalse_When_AlreadyCancelled`

- **MessageBusSchedulingExtensionsTests.cs**: Unit tests for ScheduleSendAsync/SchedulePublishAsync
  - Tests: message recording, SchedulingResult metadata (ScheduledTime, Token, IsCancellable)
  - Lines 6-41: ScheduleSendAsync and SchedulePublishAsync test cases

- **DispatchSchedulingMiddlewareTests.cs**: Middleware behavior tests
  - Tests: middleware skipping conditions (no ScheduledTime, SkipScheduler flag, native support)

- **CancelScheduledMessageTests.cs**: Cancellation edge cases
  - Lines 11-67: Null token, empty token, no store, valid store scenarios

**Transport-Specific Tests:**
- **RabbitMQ.Tests/SchedulingTests.cs**: RabbitMQ scheduling behavior
- **Postgres.Tests/PostgresSchedulingIntegrationTests.cs**: Postgres native scheduling
- **SagaSchedulingTests.cs**: Saga/choreography scheduling scenarios

## Summary: ASB Integration Checklist

1. **Minimal Integration (Path A - Current Default)**
   - Already works if IScheduledMessageStore is registered
   - Middleware intercepts, stores, transports never see scheduled messages
   - No code changes needed in AzureServiceBusDispatchEndpoint

2. **Native Support (Path B - Optional for efficiency)**
   - Set `SupportsSchedulingNatively = true` in AzureServiceBusMessagingTransport
   - Read `envelope.ScheduledTime` in AzureServiceBusDispatchEndpoint.DispatchAsync
   - Set `message.ScheduledEnqueueTimeUtc` on ServiceBusMessage
   - Reference example: PostgresDispatchEndpoint.cs lines 55, 75, 81, 92, 96
