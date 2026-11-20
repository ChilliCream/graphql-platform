---
title: Migrate Mocha from 16.7 to 16.8
description: "Migration guide for Mocha v16.7 to v16.8: handle saga events that fault on a missing saga instance."
---

Update every `Mocha.*` package in the application to version 16.8 before applying these changes.

# Behavioral breaking changes

## Saga events for a missing instance fault

An event correlated through `ICorrelatable` now faults with a `SagaInstanceNotFoundException` when no saga instance exists for its `CorrelationId`. In 16.7, the message was acknowledged without being applied and without a log entry.

A faulted event follows your [exception policies](../exception-policies.md). Without a policy, it is handled like any other failed message, see [Handle faults](../reliability.md#handle-faults). `SagaTimedOutEvent` and messages correlated only by the `saga-id` header are still acknowledged.

To keep the 16.7 behavior for a saga that expects late events after completion, opt out:

```diff
 protected override void Configure(ISagaDescriptor<OrderState> descriptor)
 {
+    descriptor.OnMissingInstance(SagaMissingInstanceBehavior.Ignore);
+
     descriptor.Initially()
         .OnEvent<OrderPlacedEvent>()
```

An event that starts a saga and carries a non-null `CorrelationId` never created an instance. It now faults instead of disappearing, so leave `CorrelationId` `null` on that event.

See [Sagas](../sagas.md#events-for-a-missing-saga-instance) for details.
