---
title: "Messages"
description: "Understand the message envelope, naming conventions, correlation, and message type identity in Mocha, and learn how to define message types."
---

A message is any C# class, record, or struct. No base class, no marker interface, no framework attributes. You define a type that holds your business data, and Mocha handles everything else - routing, serialization, correlation, and delivery. Records are used throughout the examples because they naturally fit message semantics, but they are not required. Mocha serializes message bodies as JSON by default.

# Why envelopes exist

When a message crosses a process boundary, it carries more than your business payload. The receiving service needs to know what type the message is, where it came from, when it was sent, and how it relates to other messages in a workflow. None of this belongs in your domain model.

Mocha solves this with the **envelope pattern**: every message is wrapped in an envelope containing headers and the serialized body. Your POCO contains business data; the envelope contains infrastructure metadata. Keep them separate.

This separation means your `OrderPlaced` record stays a clean contract with business properties. The envelope wraps it with everything the infrastructure needs: correlation identifiers, addressing, timestamps, and custom headers. The bus builds the envelope automatically when you publish or send - most of the time you never interact with it directly.

# Naming conventions

> **Convention:** Name commands in imperative verb-noun form (`PlaceOrder`, `ProcessPayment`). Name events in past-tense noun-verb form (`OrderPlaced`, `PaymentProcessed`). The name communicates intent - commands request action, events announce what happened.

Following this convention makes the intent of each message type clear at a glance and keeps your codebase consistent with the broader .NET messaging ecosystem.

# Define and use messages

This section walks through defining a message, attaching custom headers when publishing, and reading envelope metadata inside a handler.

## Define a message

Messages can be any class, record, or struct. Records with `{ get; init; }` properties are a natural fit:

```csharp filename="OrderPlaced.cs"
namespace MyApp;

public sealed record OrderPlaced
{
    public required Guid OrderId { get; init; }
    public required string CustomerId { get; init; }
    public required decimal TotalAmount { get; init; }
}
```

The record holds business data only. No framework types, no base classes, no marker interfaces.

## Publish with custom headers

To attach custom metadata, pass `PublishOptions` with a `Headers` dictionary:

```csharp
await bus.PublishAsync(
    new OrderPlaced
    {
        OrderId = Guid.NewGuid(),
        CustomerId = "CUST-001",
        TotalAmount = 99.95m
    },
    new PublishOptions
    {
        Headers = new() { ["x-tenant"] = "acme", ["x-trace-id"] = "abc-123" }
    },
    CancellationToken.None);
```

For commands, use `SendOptions` with the same `Headers` property:

```csharp
await bus.SendAsync(
    new ProcessPayment { OrderId = "ORD-1", Amount = 99.95m },
    new SendOptions
    {
        Headers = new() { ["x-tenant"] = "acme" }
    },
    CancellationToken.None);
```

The bus merges your headers into the envelope's header collection before dispatching.

## What a header value can hold

A header value is weakly typed, but the set of types Mocha carries is closed: `null`, `bool`,
`string`, `char`, the numeric types, `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`,
`TimeSpan`, `Guid`, `Uri`, an enum, a byte payload (`byte[]`, `ArraySegment<byte>`,
`ReadOnlyMemory<byte>`, `Memory<byte>`), a JSON value (`JsonElement`, `JsonDocument`, `JsonNode`),
nested headers, a dictionary with string keys, or a sequence of any of these.

The envelope serializer rejects any other type with an error naming the header. Setting a value always
succeeds; a transport that encodes headers itself applies its own rules, described at the end of this
section.

A value keeps its representation across a hop but not always its CLR type, because neither JSON nor an
AMQP field table carries a .NET type tag. Through the envelope serializer, which the outbox and the
Entity Framework Core scheduled store use:

| Written as                                                                                       | Read back as                                                      |
| ------------------------------------------------------------------------------------------------ | ----------------------------------------------------------------- |
| `null`, `bool`, `string`                                                                         | unchanged                                                         |
| any integer type                                                                                 | `int`, `long` or `ulong`, the smallest of those holding the value |
| `double`, `float`, `decimal`                                                                     | the same integer types when the value is whole, else `double`     |
| `DateTime`, `DateTimeOffset`, `Guid`, `Uri`, `TimeSpan`, `DateOnly`, `TimeOnly`, `char`, an enum | the text that was written                                         |
| a byte payload                                                                                   | base64 text                                                       |
| a JSON object, nested headers, a dictionary                                                      | `Dictionary<string, object?>`                                     |
| a JSON array, any sequence                                                                       | `object?[]`                                                       |

A number is read by value, not by the type that wrote it, so `5L` and `2.0` both come back as `int`,
and a `uint` above `int.MaxValue` as `long`. Two cases lose data: a whole number beyond
`ulong.MaxValue` is read as a `double` and rewritten in scientific notation, and a `decimal` carrying
more precision than a `double` holds loses the difference.

A date-shaped string stays a string. Parse explicitly when you need a typed value:

```csharp
using System.Globalization;

if (context.Headers.TryGetValue("x-processed-at", out var value)
    && DateTimeOffset.TryParse(value as string, CultureInfo.InvariantCulture, out var processedAt))
{
    // ...
}
```

A transport that defines its own header encoding differs in the details.

**RabbitMQ** maps a header onto an AMQP field table. A date arrives back as a `DateTimeOffset`
truncated to whole seconds, and a `DateTime` carrying no time zone is read as UTC. A byte payload
arrives back as bytes, carried by the table's own byte array type, and a JSON value arrives in the
shapes listed above. The table has no unsigned 64-bit type and only a 32-bit decimal mantissa, so a
number above `long.MaxValue` and a longer `decimal` travel as text. A long string from another
publisher is decoded as text when it holds valid UTF-8.

**Postgres** reads every number back as a `double`, and drops a header whose value is a dictionary, a
sequence, or `null`. Messages scheduled on this transport are stored with the Postgres encoding rather
than through the envelope serializer, so the table above does not describe them.

Those typed arrivals hold for a message taken straight off the broker. One that passes through the
outbox, or that the scheduled store redelivers, is rehydrated by the envelope serializer on the way,
so the same date arrives as text and the same byte payload as base64.

> Keep header values simple. Text and booleans behave the same everywhere, and whole numbers behave
> the same anywhere except Postgres; everything else is worth a round-trip test on the transport you
> actually deploy.

## Access envelope metadata in a handler

`IEventHandler<T>` receives the deserialized message and a cancellation token - that is all. To read message IDs, correlation IDs, timestamps, or custom headers, implement `IConsumer<T>` instead. The `IConsumeContext<T>` parameter gives you both the deserialized message and all envelope fields:

```csharp filename="OrderAuditConsumer.cs"
using Mocha;

namespace MyApp;

public class OrderAuditConsumer(ILogger<OrderAuditConsumer> logger)
    : IConsumer<OrderPlaced>
{
    public ValueTask ConsumeAsync(
        IConsumeContext<OrderPlaced> context,
        CancellationToken cancellationToken)
    {
        // The deserialized message
        var order = context.Message;

        // Envelope metadata - generated by the bus automatically
        logger.LogInformation(
            "MessageId={MessageId} CorrelationId={CorrelationId} " +
            "ConversationId={ConversationId} SentAt={SentAt}",
            context.MessageId,
            context.CorrelationId,
            context.ConversationId,
            context.SentAt);

        // Custom headers you attached when publishing
        if (context.Headers.TryGetValue("x-tenant", out var tenant))
        {
            logger.LogInformation("Tenant: {Tenant}", tenant);
        }

        return default;
    }
}
```

Register a consumer with `.AddConsumer<T>()`:

```csharp
builder.Services
    .AddMessageBus()
    .AddConsumer<OrderAuditConsumer>()
    .AddInMemory();
```

When the handler processes a published `OrderPlaced` event, you see output like:

```text
MessageId=3f2a... CorrelationId=7b1c... ConversationId=9d4e... SentAt=2026-02-25T10:30:00Z
Tenant: acme
```

The bus generates `MessageId`, `CorrelationId`, and `ConversationId` automatically. Your custom headers appear alongside them.

> [!TIP]
> Use `IEventHandler<T>` when you only need the message payload. Switch to `IConsumer<T>` when you need envelope metadata or custom headers. Both can coexist for the same message type - Mocha routes to all registered handlers and consumers.

# How correlation works

Mocha uses three identifiers to track relationships between messages:

```text
ConversationId ─── groups all messages in a logical conversation
  │
  ├── CorrelationId ─── chains messages in a specific workflow
  │     │
  │     ├── CausationId ─── links parent → child
  │     │     │
  │     │     └── CausationId ─── links parent → child
  │     │
  │     └── CausationId ─── links parent → child
  │
  └── CorrelationId ─── a different workflow in the same conversation
```

**ConversationId** is the broadest scope. When you publish the first message in a flow, the bus generates a `ConversationId`. Every subsequent message in that conversation - across services, across handler chains - inherits the same `ConversationId`. Use it to find all messages that belong to a single business transaction.

**CorrelationId** is narrower. It groups messages within a specific workflow or saga instance. A single conversation may contain multiple correlation scopes - for example, an order saga and a payment saga both triggered by the same initial event.

**CausationId** traces direct causality. When a handler publishes or sends a new message in response to a received message, the bus sets the new message's `CausationId` to the `MessageId` of the received message. This creates a parent-child chain you can follow to reconstruct the exact sequence of events.

Together, these three identifiers give you full traceability without adding any fields to your message records.

# How message type resolution works

When you register a message type - explicitly with `AddMessage<T>()` or implicitly by adding a handler - Mocha assigns it a URN-based identity:

```text
urn:message:<namespace>:<type-name>
```

For example, `MyApp.Contracts.OrderPlaced` becomes:

```text
urn:message:my-app.contracts:order-placed
```

The bus stores this identity in the `MessageType` field of the envelope. On the receiving side, the message type selection middleware matches the incoming URN against the registered types to find the correct CLR type for deserialization.

Use `AddMessage<T>()` to configure a message type explicitly - for example, to pin its URN when refactoring CLR namespaces, or to configure a send route:

```csharp
builder.Services
    .AddMessageBus()
    .AddMessage<OrderPlaced>(d =>
    {
        d.Send(route => route.ToQueue("orders-queue"));
    })
    .AddInMemory();
```

**Polymorphic messages.** If a message class implements interfaces or extends base classes that are also registered as message types, the envelope carries all of those identities in the `EnclosedMessageTypes` array. This allows a handler registered for an interface to receive messages that implement it.

# Message versioning

Evolving message contracts requires care because producers and consumers may deploy independently. Adding a new `init` property with a default value is backward-compatible - existing consumers that do not know about the property ignore it during deserialization. Renaming or removing a required property is breaking and requires a coordinated deployment or a versioning strategy.

When you need to refactor a message type's CLR namespace without changing its wire identity, use `AddMessage<T>()` to pin the URN explicitly. This decouples the type's wire identity from its CLR location, so consumers continue to receive the message under the old URN even after you move or rename the class.

# Next steps

Now that you understand message structure, learn the three messaging patterns.

- [**Messaging Patterns**](./messaging-patterns.md) - Pub/sub events, point-to-point commands, and request/reply.

> **Full demo:** [Demo.Contracts](https://github.com/ChilliCream/graphql-platform/tree/main/src/Mocha/examples/Demo/Demo.Contracts) contains a complete set of message contracts for an e-commerce system - events (`OrderPlacedEvent`, `PaymentCompletedEvent`), send messages (`ProcessRefundCommand`, `ReserveInventoryCommand`), and request/reply pairs used by sagas.
