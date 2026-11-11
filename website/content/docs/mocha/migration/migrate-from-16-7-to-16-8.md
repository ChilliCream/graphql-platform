---
title: Migrate Mocha from 16.7 to 16.8
description: "Migration guide for Mocha v16.7 to v16.8: RabbitMQ publisher confirms, publish and start timeouts, connection and consumer recovery, Postgres outbox retry rules, and the health check, log, and trace changes that monitoring depends on."
---

Update every `Mocha.*` package in the application to version 16.8 before applying these changes.

Entries that name RabbitMQ, the Postgres outbox, or Azure Service Bus apply to applications that use them. All other entries apply to every transport.

# Behavioral breaking changes

## RequestAsync cancels a dispatch that outlasts the response timeout

The response timeout of `RequestAsync`, two minutes, now also ends the dispatch of the request. When it elapses while the request is still being dispatched, the dispatch is cancelled and `RequestAsync` throws `ResponseTimeoutException`. In 16.7 `RequestAsync` waited for the dispatch to return first. See [How to handle timeouts](../messaging-patterns.md#how-to-handle-timeouts).

# Health check, log, and trace changes

## The message bus health check is no longer tagged live

`AddMessageBus()` registers the health check with the tag `ready`. In 16.7 it registered the tags `ready` and `live`.

- A health endpoint that filters on the `live` tag no longer runs the message bus check. With no other check tagged `live`, it answers Healthy while the application can serve the request.
- A health endpoint without a filter still runs the check, so it fails during a broker outage and during every re-attach of a consumer. Do not use it as a liveness probe.
- To keep the tags of 16.7, pass them:

  ```diff
   builder.Services
       .AddHealthChecks()
  -    .AddMessageBus();
  +    .AddMessageBus(tags: ["ready", "live"]);
  ```

See [Readiness and liveness](../hosting.md#readiness-and-liveness).

## The message bus health check times out after 10 seconds

The check gives the bus 10 seconds to answer the health request. In 16.7 the check had no timeout of its own: an unanswered health request ended after the response timeout of two minutes with a `ResponseTimeoutException`.

What you will observe:

- A health request that is not answered in time fails the check with the description `Message Bus did not answer the health request within 00:00:10.`
- The health request travels through the broker and waits in a queue of the service like every other request. This can fail the check when the receive endpoint that handles the health request has a backlog of more than 10 seconds. Raise the timeout, or [target a specific endpoint](../hosting.md#target-a-specific-endpoint):

  ```csharp
  builder.Services
      .AddHealthChecks()
      .AddMessageBus(timeout: TimeSpan.FromSeconds(30));
  ```

- A health request that was not handled within the timeout expires and is dropped when it is received.
- A failed check reports the failure status of its registration. `AddMessageBus()` has the new optional parameters `name`, `failureStatus`, `tags`, and `timeout`. The default failure status is `Unhealthy`, as in 16.7.

See [Configure the health check](../hosting.md#configure-the-health-check).

## The message bus health check reports transport health

Before the check sends the health request, it asks every local transport for its state. When a transport is unhealthy, the check fails at once with the description `Transport '<name>' is unhealthy. <reason>`, and the `Data` of the entry lists each unhealthy transport with its reason. In 16.7 the check only sent the health request.

| Reason                                      | Reported                                                        |
| ------------------------------------------- | --------------------------------------------------------------- |
| `The transport is not started.`             | Before the runtime has started and after it has stopped.        |
| `Receive endpoint '<name>' is not started.` | While a receive endpoint of a started transport is not started. |

What to do:

- Change alerts that match the 16.7 description `Message Bus is unhealthy.` to the new descriptions.
- The Postgres and Azure Service Bus transports report only whether they and their receive endpoints are started. An outage of their broker or database shows as an unanswered health request.

See [Add the health check to ASP.NET Core](../hosting.md#add-the-health-check-to-aspnet-core).

## The receive path logs faults, retries, and redeliveries

The receive pipeline of every transport writes log entries that 16.7 did not write:

| Event name            | Level       | Category                                                             | Written                                                                                  |
| --------------------- | ----------- | -------------------------------------------------------------------- | ---------------------------------------------------------------------------------------- |
| `MessageFaulted`      | Error       | `Mocha.Middlewares.ReceiveFaultMiddleware`                           | Once for every message whose processing failed, with the exception.                      |
| `FaultDelivered`      | Information | `Mocha.Middlewares.ReceiveFaultMiddleware`                           | When the fault was sent to the requester or the message was moved to the error endpoint. |
| `FaultDeliveryFailed` | Error       | `Mocha.Middlewares.ReceiveFaultMiddleware`                           | When that send failed.                                                                   |
| `RetryingMessage`     | Information | `Mocha.ConsumerRetryMiddleware`                                      | For every retry of a handler, with the exception.                                        |
| `RedeliveringMessage` | Warning     | `Mocha.ReceiveRedeliveryMiddleware`                                  | When a redelivery is prepared, with the exception.                                       |
| `MessageRedelivered`  | Information | `Mocha.ReceiveRedeliveryMiddleware`                                  | When the redelivery was dispatched.                                                      |
| `MessageDiscarded`    | Warning     | `Mocha.ConsumerRetryMiddleware`, `Mocha.ReceiveRedeliveryMiddleware` | When an exception policy discards a message.                                             |
| `MessageSkipped`      | Warning     | `Mocha.Middlewares.ReceiveDeadLetterMiddleware`                      | When a message that was not consumed is moved to its skipped or error endpoint.          |

What you will observe:

- The log volume rises with the failure rate. Every faulted message adds one Error entry with its exception.
- Alerts that count Error entries of the application now count faulted messages. Exclude the event name `MessageFaulted`, or set the log level of its category, where that is not wanted.
- Every entry carries `MessageId`, `MessageType`, `CorrelationId`, `ConversationId`, `Endpoint`, and `Transport` in a logger scope.
- Every other entry written while a message is received, including the entries of your handlers, carries the same logger scope.

## Circuit breakers log their state changes

Both circuit breakers of the receive pipeline now log when they open (`CircuitOpen`, Warning, with the last exception), test recovery (`CircuitHalfOpen`, Information), and close (`CircuitClosed`, Information). In 16.7 they logged nothing.

| Circuit breaker | Category                                  | Text starts with                 |
| --------------- | ----------------------------------------- | -------------------------------- |
| Receive-level   | `Mocha.ReceiveCircuitBreakerMiddleware`   | `Consumer circuit for endpoint`  |
| Transport-level | `Mocha.TransportCircuitBreakerMiddleware` | `Transport circuit for endpoint` |

An open circuit pauses the processing of its endpoint. Alert on `CircuitOpen`. See [Configure the circuit breaker](../reliability.md#configure-the-circuit-breaker).

## The receive span carries the conversation ID

The tag `messaging.message.conversation_id` of the receive span now holds the conversation ID of the message. In 16.7 it held the correlation ID. Change trace queries that look up receive spans by correlation ID through this tag. See [How correlation works](../messages.md#how-correlation-works).
