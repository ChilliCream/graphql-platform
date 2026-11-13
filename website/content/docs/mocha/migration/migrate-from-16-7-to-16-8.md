---
title: Migrate Mocha from 16.7 to 16.8
description: "Migration guide for Mocha v16.7 to v16.8: RabbitMQ publisher confirms, publish and start timeouts, connection and consumer recovery, Postgres outbox retry rules, and the health check, log, and trace changes that monitoring depends on."
---

Update every `Mocha.*` package in the application to version 16.8 before applying these changes.

Entries that name RabbitMQ, the Postgres outbox, or Azure Service Bus apply to applications that use them. All other entries apply to every transport.

# Health check, log, and trace changes

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
