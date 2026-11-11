---
title: Migrate Mocha from 16.7 to 16.8
description: "Migration guide for Mocha v16.7 to v16.8: RabbitMQ publisher confirms, publish and start timeouts, connection and consumer recovery, Postgres outbox retry rules, and the health check, log, and trace changes that monitoring depends on."
---

Update every `Mocha.*` package in the application to version 16.8 before applying these changes.

Entries that name RabbitMQ, the Postgres outbox, or Azure Service Bus apply to applications that use them. All other entries apply to every transport.

# API breaking changes

## RabbitMQ connection types are internal

`RabbitMQDispatcher`, `RabbitMQConsumerManager`, and `RabbitMQConnectionManagerBase` are no longer public. The `Dispatcher` and `ConsumerManager` properties of `RabbitMQMessagingTransport` are no longer public either. Code that rented a channel from the dispatcher or registered a consumer through the consumer manager does not compile.

`IRabbitMQConnectionProvider` and the `Connection` property of `RabbitMQMessagingTransport` stay public. Code that needs a channel of its own creates a connection with `IRabbitMQConnectionProvider.CreateAsync` or with the application's `IConnectionFactory`, and keeps that connection for the lifetime of the application.

## RabbitMQ topology resources are provisioned with their connection

`IRabbitMQResource.ProvisionAsync` and the `ProvisionAsync` methods of `RabbitMQQueue`, `RabbitMQExchange`, `RabbitMQBinding`, `RabbitMQQueueBinding`, and `RabbitMQExchangeBinding` take the connection that owns the channel as a new second parameter:

```diff
-await queue.ProvisionAsync(channel, cancellationToken);
+await queue.ProvisionAsync(channel, connection, cancellationToken);
```

Assemblies that call or implement these methods must be recompiled against 16.8.

## The RabbitMQ transport requires RabbitMQ.Client 7.2.2

`Mocha.Transport.RabbitMQ` references `RabbitMQ.Client` 7.2.2. Version 16.7 referenced 7.2.0. An application that pins `RabbitMQ.Client` to a lower version gets a package downgrade error (`NU1605`) at restore. Raise the pinned version to 7.2.2 or later.

# Behavioral breaking changes

## RabbitMQ sends and publishes wait for the broker's confirmation

A send or publish through the RabbitMQ transport completes when RabbitMQ has confirmed the message. In 16.7 it completed when the message was written to the connection, and a message that RabbitMQ did not accept was lost without an error. The change covers `SendAsync`, `PublishAsync`, `RequestAsync`, replies, faults, and the dispatches of the outbox and the scheduler.

What you will observe:

- A call that returns means that RabbitMQ has the message. A message for an exchange reached every queue that the exchange routes to. When no binding matches, RabbitMQ drops the message and the call still completes, as in 16.7. A message for a queue address reached that queue.
- Every call takes at least one round trip to the broker. A loop that awaits each call sends one message per round trip. To send faster, start several calls and await them together.
- While RabbitMQ cannot be reached, or blocks the connection during a memory or disk alarm, a call waits. It fails when `PublishTimeout` elapses, after two minutes by default.
- A handler that sends or publishes keeps its concurrency slot while the call waits.

See [Publisher confirms](../transports/rabbitmq.md#publisher-confirms).

## RabbitMQ sends and publishes are repeated after a connection loss

When the connection or the channel is lost before the confirmation arrives, Mocha waits for the next connection and sends the message again. The delay between attempts is about 250 milliseconds to 5 seconds, and the attempts end when `PublishTimeout` elapses. 16.7 sent every message once.

RabbitMQ may already have the message of the interrupted attempt, so a consumer can receive the same message twice, with the same message ID. Make handlers safe to run more than once for the same message, or configure the [transactional inbox](../reliability.md#deduplicate-messages-with-the-transactional-inbox) in the consuming service.

The log entry `PublishRetryScheduled` names the message and the reason. It is written at Information for the first retry of a publish and at Debug for further retries. See [A message is handled twice](../transports/rabbitmq.md#a-message-is-handled-twice).

## RabbitMQ sends and publishes fail when the broker does not accept the message

In 16.7 a send or publish completed whether or not RabbitMQ accepted the message. In 16.8 the answer of the broker decides how the call ends.

RabbitMQ answers with a negative acknowledgement when a queue rejects the message. This can happen when the queue is at its length limit and uses `x-overflow: reject-publish`. The outcome depends on whether the message was sent or published. A sent message, such as a command, a request, a reply, or a fault, has one destination queue. A published event is copied into every queue that is bound to its exchange, and RabbitMQ rejects it when one of those queues rejects it, while the other queues keep their copy.

| Answer of the broker                                                                                                                                                                        | Outcome                                                                                                                                                                                                         |
| ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| RabbitMQ rejects a sent message with a negative acknowledgement.                                                                                                                            | Mocha sends the message again until the queue accepts it. When `PublishTimeout` elapses first, the call throws `TransportUnavailableException`.                                                                 |
| RabbitMQ rejects a published event with a negative acknowledgement.                                                                                                                         | The call throws `InvalidOperationException` at once. Mocha does not publish the event again, because the queues that accepted it have it.                                                                       |
| The broker closes the channel with a reply code, for example `406 PRECONDITION_FAILED` for a message above the size limit, `404 NOT_FOUND` for a missing exchange, or `403 ACCESS_REFUSED`. | The call throws the exception of the RabbitMQ client (`AlreadyClosedException` or `OperationInterruptedException`) with the reply code and text of the broker. The message is not delivered and not sent again. |
| The headers of the message do not fit into one frame of the connection.                                                                                                                     | The call throws `InvalidOperationException` before the message is sent.                                                                                                                                         |

What to do:

- Handle these exceptions where the application sends or publishes.
- After the `InvalidOperationException` of a rejected event, publishing the event again delivers it once more to every queue that accepted it.
- For a subscriber queue that must not miss events, use `x-overflow: drop-head` or a dead-letter exchange instead of `reject-publish`. A queue with `reject-publish` suits a queue that receives sent messages, where the sender waits until the queue has room.
- Through the Postgres outbox, a sent message that its queue keeps rejecting is released without counting an attempt. The other three outcomes count as attempts. For a rejected event, every attempt delivers the event once more to the queues that accepted it. See [The Postgres outbox no longer counts an unavailable transport as an attempt](#the-postgres-outbox-no-longer-counts-an-unavailable-transport-as-an-attempt).

## A message for a RabbitMQ queue that does not exist is returned

This entry applies to messages whose destination is a queue address, such as `queue:orders`, and to messages that Mocha moves to an error or skipped queue. RabbitMQ now returns such a message when the queue does not exist. In 16.7 RabbitMQ dropped the message and the call completed.

- With auto-provisioning enabled for the queue, Mocha declares the queue and its bindings again and sends the message once more. The log shows `MessageReturned` (Warning) and `QueueRedeclared` (Information). A queue that is deleted while a sending application runs is created again by the next send.
- With auto-provisioning disabled for the queue, for an exclusive queue, or when the queue is still missing after it was declared, the call throws `InvalidOperationException` with the text `The broker returned the message because no queue named '<queue>' exists.`

Messages for an exchange and replies are not returned. RabbitMQ drops a message for an exchange that no binding matches, and a reply whose requester has stopped, as in 16.7.

See [Missing queues and bindings](../transports/rabbitmq.md#missing-queues-and-bindings).

## A failed RabbitMQ message is acknowledged only after it was moved

When a handler fails, Mocha sends a fault to the requester or moves the message to the error queue of its endpoint. This send now waits for the broker's confirmation. In 16.7 the received message was acknowledged as soon as the fault or the moved message was written to the connection.

When the send fails:

1. The log shows `FaultDeliveryFailed` at Error with the exception.
2. Mocha moves the received message to the skipped queue of its endpoint, and the log shows `MessageSkipped` (Warning).
3. When that send fails as well, the received message returns to its source queue and RabbitMQ delivers it again.

Each of these sends can wait until `PublishTimeout` elapses, and the message keeps its concurrency slot for that time.

## RabbitMQ receive endpoints attach again on their own

In 16.7 a receive endpoint whose channel was closed while the connection stayed open did not consume again until the connection was replaced or the application restarted. This can happen when a handler exceeds the broker's [delivery acknowledgement timeout](https://www.rabbitmq.com/docs/consumers#acknowledgement-timeout) or when the queue is deleted.

In 16.8 every receive endpoint attaches again on its own, with delays of about 1 to 30 seconds between attempts. With auto-provisioning enabled it declares its queue and bindings again first. Mocha also reconnects a lost connection itself and keeps trying until the application stops, whatever the `AutomaticRecoveryEnabled` setting of the connection factory is.

What you will observe:

- The log shows `ConsumerChannelLost` or `ConsumerCancelled` (Warning) and then `ConsumerAttached` (Information). For a lost connection it shows `ConnectionLost` (Warning) and then `ConnectionReady` (Information).
- A queue that is deleted while its consuming application runs is created again when auto-provisioning is enabled.
- The health check reports an endpoint that is not attached. See [The message bus health check reports transport health](#the-message-bus-health-check-reports-transport-health).
- When the broker refuses the connection, a declaration, or the consumer the same way on every attempt, the attempts continue and are logged at Error (`ConnectionAttemptFailed`, `ConsumerAttachFailed`). This can happen when credentials changed or when the configuration of a queue differs from the existing queue. Sends and publishes wait while the dispatcher connection is not ready.
- A network path that fails without closing the connection is detected through heartbeats, after two to three minutes with the client's default interval of 60 seconds. Sends and publishes wait during that time. See [Heartbeats](../transports/rabbitmq.md#heartbeats) for a shorter interval.

See [Connection recovery](../transports/rabbitmq.md#connection-recovery).

## RabbitMQ handlers are cancelled when their connection or channel is lost

When a connection or channel is lost, Mocha cancels the `CancellationToken` of every handler that runs for a message of that channel. RabbitMQ delivers these messages again. This includes a message whose handler had finished its work but whose acknowledgement could no longer be sent. In 16.7 the token was cancelled only when the endpoint stopped.

What you will observe:

- A handler that passes its token on ends with an `OperationCanceledException` when the channel is lost, and runs again for the redelivered message. Make handlers safe to run more than once for the same message, or configure the [transactional inbox](../reliability.md#deduplicate-messages-with-the-transactional-inbox).
- A handler that ignores its token keeps its `MaxConcurrency` slot until it returns. When such handlers hold every slot, the endpoint cannot attach again. The health check reports `The consumer waits for a handler of a lost channel to return.` for its queue, and after 30 seconds the log shows `ConsumerBlockedByHandlers` at Error. Pass the token to database queries, HTTP requests, and other asynchronous operations.

See [Message handling after a connection loss](../transports/rabbitmq.md#message-handling-after-a-connection-loss).

## The Postgres outbox no longer counts an unavailable transport as an attempt

In 16.7 every dispatch that threw an exception counted as one of the 10 attempts of an outbox message, so a broker outage could use up all attempts. In 16.8 the way a dispatch ends decides whether it counts:

| The dispatch ends with                                                                               | Counted | What the processor does                                                                                                                                                |
| ---------------------------------------------------------------------------------------------------- | ------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `TransportUnavailableException`, a dispatch that takes longer than `DispatchTimeout`, or a host stop | No      | Releases the message unchanged, pauses, and dispatches the message again. The pause is about 100 milliseconds at first and at most 10 seconds after repeated releases. |
| Any other exception, for example for a message that the broker refuses                               | Yes     | Dispatches the message again after 1, 2, 4, 8 seconds and so on, up to `MaxRetryDelay`.                                                                                |
| A message that cannot be read, has an unknown message type, or matches no endpoint                   | Yes     | Dispatches the message again after the same delays.                                                                                                                    |

Four new options on `PostgresOutboxOptions` control the processor:

| Option                  | Default    | Description                                                                                                                              |
| ----------------------- | ---------- | ---------------------------------------------------------------------------------------------------------------------------------------- |
| `MaxConcurrentMessages` | 1          | Number of messages that one outbox processor dispatches at the same time. A value above 1 can change the order in which messages arrive. |
| `DispatchTimeout`       | 30 seconds | Longest time one dispatch may take before it is cancelled and the message is released.                                                   |
| `MaxAttempts`           | 10         | Number of counted attempts after which a message is no longer dispatched.                                                                |
| `MaxRetryDelay`         | 10 minutes | Upper bound of the delay after a counted attempt.                                                                                        |

What you will observe:

- During a RabbitMQ outage the attempt count (`TimesSent`) of the waiting messages does not change. With the default settings `DispatchTimeout` elapses before the `PublishTimeout` of the transport, so the log shows a Warning that ends with `did not complete within the dispatch timeout of 00:00:30. The attempt is not counted and the message is dispatched again.` for every dispatch.
- A released message keeps its position in the outbox. With `MaxConcurrentMessages` set to 1, the messages behind it wait until it is dispatched, also messages for other destinations. This can last when a queue keeps rejecting a sent message at its length limit. A higher `MaxConcurrentMessages` lets other messages pass. In 16.7 a failed message was delayed and later messages passed it.
- When a message reaches `MaxAttempts`, the processor writes a Critical entry with the text `Outbox message <id> (message ID <message id>, type <type>) to <destination> exhausted all 10 attempts and will not be retried.` In 16.7 the text was `Message with ID <id> exhausted all 10 attempts and will not be retried.` The row stays in the outbox table and is not dispatched again. 16.7 wrote this entry only for a message that it could not read or route. Alert on this entry. [Configure outbox retries](../reliability.md#configure-outbox-retries) describes how to dispatch the message again.
- An idle processor looks for due messages at least every 30 seconds.

Only the RabbitMQ transport throws `TransportUnavailableException`. With the Azure Service Bus and Postgres transports, a dispatch that fails during an outage still counts as an attempt, unless it takes longer than `DispatchTimeout`.

## A Postgres outbox dispatch keeps its transaction open until the broker confirms

An outbox worker claims a message in a database transaction, dispatches it, and deletes the row in the same transaction. With RabbitMQ a dispatch now waits for the broker's confirmation, so the transaction, its row lock, and its database connection are held for the whole dispatch, up to `DispatchTimeout` per worker. In 16.7 a dispatch to RabbitMQ completed when the message was written to the connection.

What this means for the database:

- Each concurrent dispatch needs a database connection. Leave `MaxConcurrentMessages` connections per outbox processor and application instance in the connection pool. A connection pooler in transaction mode keeps a server connection assigned for the same time.
- The session is idle in transaction while the dispatch runs. When `idle_in_transaction_session_timeout` in PostgreSQL, or the idle transaction timeout of a connection pooler, is shorter than a dispatch, the database ends the session during the dispatch. If the broker has accepted the message by then, the row cannot be deleted and the message is dispatched again. Keep these timeouts above `DispatchTimeout`.

A message can also arrive more than once when a dispatch is cancelled after `DispatchTimeout` and RabbitMQ accepts the message of the cancelled dispatch later. This can happen during a memory or disk alarm of the broker, or when a confirmation takes longer than `DispatchTimeout`. Make handlers safe to run more than once for the same message, or configure the [transactional inbox](../reliability.md#deduplicate-messages-with-the-transactional-inbox).

See [Configure outbox dispatch concurrency](../reliability.md#configure-outbox-dispatch-concurrency).

## Scheduled messages and redeliveries keep their attempts during a RabbitMQ outage

This entry applies to the Postgres-backed scheduled message store that `UsePostgresScheduling()` registers. With RabbitMQ it stores scheduled messages and [redeliveries](../reliability.md#redeliver-failed-messages).

A dispatch that fails with `TransportUnavailableException`, or that a host stop interrupts, no longer counts towards the 10 attempts of a scheduled message. In 16.7 every dispatch that failed with an exception counted, so a broker outage could use up all attempts of the scheduled messages and redeliveries that came due. Every other failure still counts and is recorded in the `last_error` column.

Only the RabbitMQ transport throws `TransportUnavailableException`. See [Retry behavior](../scheduling.md#retry-behavior).

## A RabbitMQ send or publish fails after the publish timeout

A send or publish waits for at most `PublishTimeout`, two minutes by default. The time includes the wait for a connection, the wait for a free channel, the wait of a request for its reply consumer, and every retry. After that the call throws `TransportUnavailableException` (namespace `Mocha`). In 16.7 a call that found no connection threw the exception of the RabbitMQ client after five connection attempts.

The text of the exception says what is known about the message:

- When the message was written at least once, the text says that the broker may or may not have received it. Sending the message again can deliver it twice.
- When the message was never written, because there was no connection or no free channel, the text says that the message was not sent.

What to do:

- Catch `TransportUnavailableException` where the application reacts to an unreachable broker. Exceptions of the RabbitMQ client no longer report it.
- A caller that must not wait for two minutes, such as an HTTP request, passes a cancellation token or uses a shorter timeout:

  ```csharp
  builder.Services
      .AddMessageBus()
      .AddRabbitMQ(transport => transport.PublishTimeout(TimeSpan.FromSeconds(30)));
  ```

- `Timeout.InfiniteTimeSpan` waits until the caller cancels. A cancelled call throws `OperationCanceledException`, and the broker may or may not have the message.

## At most 100 RabbitMQ sends and publishes are in flight per transport

Each send or publish uses a channel of its own on the dispatcher connection until RabbitMQ has answered. One transport instance runs at most 100 sends and publishes at the same time, or fewer when the broker allows fewer channels per connection. Further calls wait for a free channel, and the wait counts towards `PublishTimeout`. 16.7 had no such limit and kept at most 10 idle channels.

What you will observe:

- The dispatcher connection shows up to 100 channels in the RabbitMQ management UI. The channels stay open for the next sends.
- Code that starts many calls at once, for example with `Task.WhenAll` over a large batch, has at most 100 of them in flight. A call that waits for a free channel for longer than `PublishTimeout` fails without having been sent. Limit the number of concurrent calls in the application, or raise `PublishTimeout`.
- In measurements against a single RabbitMQ node, throughput rose up to about 32 calls in flight and not beyond.

## RequestAsync cancels a dispatch that outlasts the response timeout

The response timeout of `RequestAsync`, two minutes, now also ends the dispatch of the request. When it elapses while the request is still being dispatched, the dispatch is cancelled and `RequestAsync` throws `ResponseTimeoutException`. In 16.7 `RequestAsync` waited for the dispatch to return first. See [How to handle timeouts](../messaging-patterns.md#how-to-handle-timeouts).

## RabbitMQ startup retries for up to two minutes

When RabbitMQ cannot be reached at startup, the transport retries with delays of about 1 to 10 seconds until `StartTimeout` elapses, two minutes by default. Then the start fails. In 16.7 the start failed after five attempts, which took about 15 seconds plus the time of the attempts.

The start fails at once, without further attempts, when the broker refuses the connection or the topology: refused credentials, a failed TLS handshake, denied access to the virtual host, or a queue or exchange declaration that the broker rejects. A virtual host that has reached its connection limit refuses the connection the same way, which can happen during a rolling update. 16.7 made five attempts in these cases as well.

What you will observe:

- The host does not finish starting while the transport retries. Check that startup probes and deployment timeouts allow for `StartTimeout`, or lower it:

  ```csharp
  builder.Services
      .AddMessageBus()
      .AddRabbitMQ(transport => transport.StartTimeout(TimeSpan.FromSeconds(30)));
  ```

- `TimeSpan.Zero` makes one attempt. `Timeout.InfiniteTimeSpan` retries until the host cancels the start.
- Each failed attempt is logged as `ConnectionAttemptFailed` (Warning). A failed start is logged as `ConnectionFailed` (Error) with the exception.

`StartTimeout` applies to startup only. After startup Mocha reconnects until the application stops. See [Connection recovery](../transports/rabbitmq.md#connection-recovery).

## The generated RabbitMQ reply queue expires instead of being auto-delete

The reply queue that the transport generates per service instance (`response-<instance id>`) is declared differently:

| Property                                        | 16.7                                    | 16.8                                                                           |
| ----------------------------------------------- | --------------------------------------- | ------------------------------------------------------------------------------ |
| Auto-delete                                     | Yes                                     | No                                                                             |
| Expiry without consumers (`x-expires`)          | 30 minutes                              | One minute                                                                     |
| Queue type when the transport default is quorum | Chosen by the broker                    | Quorum, with `x-quorum-initial-group-size` if configured                       |
| Removed                                         | By RabbitMQ when its consumer goes away | By Mocha when the reply endpoint stops, otherwise by RabbitMQ after the expiry |

What you will observe:

- The queue stays during a short disconnection, and replies wait in it until Mocha has reconnected. In 16.7 RabbitMQ deleted the queue when the connection was lost.
- After a process that ended without stopping the bus, the queue remains on the broker for one minute.
- With auto-provisioning enabled, Mocha deletes the queue when the reply endpoint stops, also when the runtime is disposed without a stop. It waits at most 5 seconds for the deletion.
- Queue policies, dashboards, and alerts that select auto-delete queues no longer match the reply queue.

See [Temporary receive endpoints](../transports/rabbitmq.md#temporary-receive-endpoints).

## RabbitMQ receive endpoints wait for running handlers on shutdown

When the host stops, every receive endpoint stops taking new messages and waits for its running handlers until the shutdown timeout of the host elapses. Only then does Mocha cancel the `CancellationToken` of the handlers that still run, and RabbitMQ delivers their messages again. In 16.7 the token of every handler was cancelled as soon as its endpoint stopped.

What you will observe:

- A shutdown takes as long as the longest running handler, up to the shutdown timeout (`HostOptions.ShutdownTimeout`, 30 seconds by default). Closing channels and deleting the reply queue can take a few seconds longer.
- A handler that relied on its token being cancelled at the start of a shutdown now runs until it finishes or the shutdown timeout elapses.
- Set `HostOptions.ShutdownTimeout` to the time handlers may take, and keep the termination grace period of the platform, such as `terminationGracePeriodSeconds` in Kubernetes, above it.
- The log shows `ConsumerDrainStarted` (Information), `ConsumerDrainTimedOut` (Warning, when the timeout elapsed first), and `ConsumerStopped` (Information) for each endpoint.

See [Graceful shutdown](../transports/rabbitmq.md#graceful-shutdown).

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
| RabbitMQ          | Deletes the generated reply queue.                                  | 5 seconds      |
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

| Reason                                                                           | Reported                                                                                                                 |
| -------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------ |
| `The transport is not started.`                                                  | Before the runtime has started and after it has stopped.                                                                 |
| `Receive endpoint '<name>' is not started.`                                      | While a receive endpoint of a started transport is not started.                                                          |
| `The dispatcher connection is unavailable.`                                      | RabbitMQ: while the dispatcher connection is lost.                                                                       |
| `The consumer connection is unavailable.`                                        | RabbitMQ: while the consumer connection is lost.                                                                         |
| `The broker blocks the dispatcher connection.`                                   | RabbitMQ: during a memory or disk alarm of the broker.                                                                   |
| `Queue '<queue>': The consumer is not attached.`                                 | RabbitMQ: from the loss of a channel until the endpoint consumes again. This includes the short time of every re-attach. |
| `Queue '<queue>': The consumer waits for a handler of a lost channel to return.` | RabbitMQ: while handlers of a lost channel hold every concurrency slot of the endpoint.                                  |

What to do:

- A readiness probe with a failure threshold of 1 fails during every re-attach of a consumer. Use a failure threshold of 2 or more.
- Change alerts that match the 16.7 description `Message Bus is unhealthy.` to the new descriptions.
- The Postgres and Azure Service Bus transports report only whether they and their receive endpoints are started. An outage of their broker or database shows as an unanswered health request.

See [Add the health check to ASP.NET Core](../hosting.md#add-the-health-check-to-aspnet-core).

## RabbitMQ connections have names

The two connections of a RabbitMQ transport are named `<service> <transport> consumer` and `<service> <transport> dispatcher`. The service is the service name of the host, or the process name when there is none. The transport is the name of the transport, `rabbitmq` by default. The names appear in the RabbitMQ management UI and in the broker's log.

- When the connection factory sets `ClientProvidedName`, both connections keep that name.
- With a custom `IRabbitMQConnectionProvider`, the provider names the connections.
- Change dashboards, alerts, and scripts that select the connections of the application by name.

See [Two connections per broker transport](../transports/rabbitmq.md#two-connections-per-broker-transport).

## RabbitMQ log categories, event names, and texts changed

The RabbitMQ transport writes its log entries under two new categories, and every entry has a new event name and a new text:

| 16.7 category                                      | 16.8 category                         |
| -------------------------------------------------- | ------------------------------------- |
| `Mocha.Transport.RabbitMQ.RabbitMQConsumerManager` | `Mocha.Transport.RabbitMQ.Consumer`   |
| `Mocha.Transport.RabbitMQ.RabbitMQDispatcher`      | `Mocha.Transport.RabbitMQ.Dispatcher` |

What to change:

- Log level settings and filters that name a 16.7 category. A filter on the prefix `Mocha.Transport.RabbitMQ` keeps matching.
- Alert rules that match a 16.7 text. Match the event name instead:

  | 16.7 text                                                                                                                                                            | 16.8 event name                                                                                                    |
  | -------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------ |
  | `Connection shutdown detected. ...` (Warning)                                                                                                                        | `ConnectionLost` (Warning)                                                                                         |
  | `Failed to create connection (attempt ...). Retrying in ...` (Warning), `Reconnection attempt failed` (Error), `Connection recovery failed. Will retry. ...` (Error) | `ConnectionAttemptFailed` (Warning, or Error when the broker refuses the connection the same way on every attempt) |
  | `Failed to create connection after 5 attempts` (Error)                                                                                                               | `ConnectionFailed` (Error), written only when the start fails                                                      |
  | `Failed to reconnect consumer for queue ...` (Error)                                                                                                                 | `ConsumerAttachFailed` (Warning, or Error when the broker refuses the consumer the same way on every attempt)      |
  | `Connection blocked: ...` (Warning)                                                                                                                                  | `ConnectionBlocked` (Warning)                                                                                      |

- After startup Mocha does not give up on a connection or a consumer, so no entry reports that. To alert on a long outage, use the `Elapsed` property of `ConnectionAttemptFailed` and `ConsumerAttachFailed`. See [What to alert on](../transports/rabbitmq.md#what-to-alert-on).

Every entry carries properties that identify the service, the connection, and the broker in logger scopes. Keep `IncludeScopes = true` in the OpenTelemetry logging configuration to export them.

[Recovery logs](../transports/rabbitmq.md#recovery-logs) lists every entry with its level, text, and event name.

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

## Postgres outbox log entries changed

The category `Mocha.Outbox.PostgresOutboxProcessor` is unchanged. These entries changed:

- A failed dispatch is logged at Error with the text `The dispatch of outbox message <id> (message ID <message id>, type <type>) to <destination> failed on attempt <n> of <max>.` The entry carries the properties `Id`, `MessageId`, `MessageType`, and `DestinationAddress`. In 16.7 a failed dispatch was logged as `An unexpected error occurred while processing outbox event`, without these properties. That text now reports a failure outside a dispatch, such as a lost database connection.
- A released dispatch is logged at Warning, or at Debug when the host stops.
- `Outbox processor is sleeping for ...` moved from Information to Debug.
- The Critical entry for an exhausted message is now also written for a message whose dispatch kept failing. Its text now starts with `Outbox message <id>` and names the message ID, the type, and the destination. Change alert rules that match the 16.7 text `Message with ID <id> exhausted all`.
- When the row of a message that the transport accepted cannot be removed, for example because the database ended the session, the log shows an Error entry that ends with `was accepted by the transport, but removing its outbox row failed. The message is dispatched again unless the removal took effect.`

See [Configure outbox retries](../reliability.md#configure-outbox-retries).

## The receive span carries the conversation ID

The tag `messaging.message.conversation_id` of the receive span now holds the conversation ID of the message. In 16.7 it held the correlation ID. Change trace queries that look up receive spans by correlation ID through this tag. See [How correlation works](../messages.md#how-correlation-works).
