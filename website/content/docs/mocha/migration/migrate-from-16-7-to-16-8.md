---
title: Migrate Mocha from 16.7 to 16.8
description: "Migration guide for Mocha v16.7 to v16.8: RabbitMQ publisher confirms, publish and start timeouts, connection and consumer recovery, Postgres outbox retry rules, and the health check, log, and trace changes that monitoring depends on."
---

Update every `Mocha.*` package in the application to version 16.8 before applying these changes.

Entries that name RabbitMQ, the Postgres outbox, or Azure Service Bus apply to applications that use them. All other entries apply to every transport.

# Behavioral breaking changes

## RequestAsync cancels a dispatch that outlasts the response timeout

The response timeout of `RequestAsync`, two minutes, now also ends the dispatch of the request. When it elapses while the request is still being dispatched, the dispatch is cancelled and `RequestAsync` throws `ResponseTimeoutException`. In 16.7 `RequestAsync` waited for the dispatch to return first. See [How to handle timeouts](../messaging-patterns.md#how-to-handle-timeouts).

## Receive endpoints start and stop in a different order

This entry applies to every transport, including custom transports. 16.7 started and stopped the receive endpoints of a transport one after the other, without regard to their kind.

- On start, a transport starts its reply endpoints first and then its other receive endpoints.
- On stop, a transport stops its other receive endpoints at the same time and then its reply endpoints.
- When the start of a transport fails, the transport stops what it has started and rethrows the failure.

For a custom transport:

- `ReceiveEndpoint.OnStopAsync` runs for several endpoints of the transport at the same time.
- The cancellation token of `ReceiveEndpoint.OnStopAsync` and `MessagingTransport.OnAfterStopAsync` bounds the wait for running handlers. It can already be cancelled when the method is called. Release the resources of the endpoint, including temporary broker resources, without it.
- Override `MessagingTransport.HealthFailureReason` to report the state of the transport to the health check.

## Disposing the runtime without stopping it cancels running handlers

When the messaging runtime is disposed while it is still started, for example because the service provider is disposed without a host stop, it stops with a cancelled token. Running handlers get no time to finish: their cancellation tokens are cancelled at once. In 16.7 the dispose stopped the runtime with a token that was never cancelled.

Cleanup still runs, each step with its own time limit:

| Transport         | Cleanup                                                             | Limit          |
| ----------------- | ------------------------------------------------------------------- | -------------- |
| Postgres          | Releases its leased messages and removes its consumer registration. | 5 seconds each |
| Azure Service Bus | Removes the resources of temporary endpoints.                       | 30 seconds     |

To let handlers finish, stop the host before the service provider is disposed. A host that stops normally does this already.

## An Azure Service Bus stop that is cancelled no longer fails

When the shutdown timeout of the host elapses while an Azure Service Bus receive endpoint waits for its handlers, the endpoint now disposes its processor and completes the stop. In 16.7 the stop failed with an `OperationCanceledException`, and the resources of a temporary endpoint were not removed.

What you will observe:

- For a temporary endpoint, the forwarding subscriptions and the queue are removed after the processor is disposed. When this takes longer than 30 seconds, the log shows the Warning `Deleting the temporary resources of Azure Service Bus queue '<queue>' did not complete within 00:00:30` and the stop continues.
- Disposing the processor waits for running handlers. A handler that ignores its `CancellationToken` delays the stop until it returns.

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
