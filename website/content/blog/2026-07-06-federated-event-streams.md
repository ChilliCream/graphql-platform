---
date: "2026-07-06"
title: "Introducing Federated Event Streams for Fusion 16.4"
description: "Federated Event Streams add broker-backed, resumable GraphQL subscriptions to Fusion 16.4, with stateless gateway scaling and client-owned resume cursors."
tags: ["fusion", "graphql", "federation", "subscriptions", "event-streams", "dotnet"]
category: "Release"
featuredImage: "header.png"
author: Michael Staib
authorUrl: https://github.com/michaelstaib
authorImageUrl: https://avatars1.githubusercontent.com/u/9714350?s=100&v=4
---

Queries and mutations are the easy part of GraphQL. A client sends a request, the server returns a response, and the operation is done. You do not have to keep track of connection state, reconnects, transport differences, or delivery guarantees over time.

Subscriptions are different. They are long-lived streams, and that means we need to think about what happens while the connection is open, what happens when it drops, and what a client can safely assume when it comes back. From the GraphQL spec's point of view, those delivery guarantees are not defined for you.

Take an order management screen. A support agent is watching an order move from `placed`, to `paid`, to `packed`, to `shipped`. Each transition is pushed to the UI as it happens. If the agent's laptop switches networks or the browser reconnects after a short outage, the client needs to know whether it missed an update while it was offline.

For some applications, missing a few events is acceptable. A live typing indicator, presence update, or fast-changing dashboard can simply continue with the newest value. For others, every event matters. In an order workflow, audit trail, payment process, or support inbox, the client needs to resume exactly where it left off, without gaps and without replaying events it has already processed.

In a federated graph, these concerns become even more important. Events can originate from different subgraphs, and the gateway has to turn each event into the response shape the client asked for. At the same time, we still want the system to scale like the rest of our GraphQL architecture: gateway instances should stay stateless, subgraphs should scale independently, and reconnects should not depend on sticky sessions, in-memory subscription state, or transferring session data from one gateway replica to another.

Today, we are introducing **Federated Event Streams** in Fusion 16.4: a new way to build broker-backed, resumable subscriptions across a federated graph without making the gateway stateful. A client subscribes once through the Fusion gateway, events come from your broker, and for each event the gateway resolves exactly the fields the client asked for across the federated graph. Resume state stays with the client, so a reconnect can land on any gateway replica without sticky sessions or gateway-owned subscription state.

## Broker-backed GraphQL subscriptions in Fusion

Federated Event Streams starts with the GraphQL subscription the client already knows. The client subscribes through the Fusion gateway and selects the fields it wants back.

```graphql
subscription {
  onReviewCreated {
    review {
      id
      body
    }
  }
}
```

Instead of forwarding that subscription to a subgraph, the gateway subscribes to a broker topic itself. That topic becomes the event stream for the subscription. Whenever the gateway receives an event, it uses the event as the starting point for the subscription query plan. It performs ordinary stateless GraphQL requests against the relevant subgraphs, builds the response the client asked for, sends it to the client, and waits for the next event.

<FusionSubscriptionsDiagram />

This is the important shift. The gateway does not need to keep a stateful subscription connection open to a subgraph, and subgraphs do not need to keep subscription state for the gateway. From a subgraph's point of view, the gateway only sends normal GraphQL query requests. There is no gateway-to-subgraph subscription lifecycle to recover, no subscription state to move between gateway replicas, and no need for participating subgraphs to hold long-lived connection state.

## Declaring the event stream

On the subgraph that exposes the subscription field, you add the `@eventStream` directive. The `message` argument describes the shape of the broker message. It is a selection set over the return type, and it tells the gateway which fields are already present when an event arrives.

```graphql
# Reviews subgraph
type Subscription {
  onReviewCreated: ReviewCreated! @eventStream(message: "review { id }")
}

type ReviewCreated {
  review: Review!
}

type Review @key(fields: "id") {
  id: ID!
}
```

If you are using Hot Chocolate, the same stream can be declared with the `[EventStream]` attribute:

```csharp
[SubscriptionType]
public static partial class ReviewSubscriptions
{
    [EventStream("review { id }")]
    public static ReviewCreated OnReviewCreated()
        => EventStream.Create<ReviewCreated>();
}

public record ReviewCreated(Review Review);
```

That is the whole contract. `review { id }` means the broker delivers a message shaped like `{ "review": { "id": "1" } }`. The gateway uses that key to resolve the `Review` entity and then continues with whatever the client selected.

The message does not have to be only an entity key, though. Because `message` is a selection set over the return type, it can describe any fields that arrive with the event. Some fields can come directly from the broker message, while others can be entity links that the gateway resolves through the graph:

```graphql
onProductPriceChanged(productId: ID!): ProductPriceChangedEvent
  @eventStream(message: "{ oldPrice newPrice product { id } }")

type ProductPriceChangedEvent {
  oldPrice: Float!
  newPrice: Float!
  product: Product!
}
```

Here `oldPrice` and `newPrice` come straight from the broker, while `product` is resolved across your subgraphs from `{ product { id } }`. You stream exactly what the event is about, and let federation fill in the rest only when the client asks for it.

## Resuming without gateway state

A long-lived subscription will be interrupted eventually. A phone goes to sleep, a network blips, or you roll out a new gateway version. The important question is what happens when the client reconnects. Can it continue from the last event it processed without asking the gateway to remember anything?

Federated Event Streams supports this with an opaque cursor that lives with the client. On the subscription field, you annotate one argument with the `@eventCursor` directive. On the payload type, you annotate one field with `@eventCursor` as well. That field carries the position of the event within the stream.

```graphql
type Subscription {
  onReviewCreated(after: String @eventCursor): ReviewCreated!
    @eventStream(message: "review { id }")
}

type ReviewCreated {
  review: Review!
  cursor: String @eventCursor
}

type Review @key(fields: "id") {
  id: ID!
}
```

The event cursor is inserted by the gateway, so there is no need to add it to the message format. If you are using Hot Chocolate, the same schema looks like this:

```csharp
[SubscriptionType]
public static partial class ReviewSubscriptions
{
    [EventStream("review { id }")]
    public static ReviewCreated OnReviewCreated([EventCursor] string? after)
        => EventStream.Create<ReviewCreated>(after);
}

public record ReviewCreated(
    Review Review,
    [property: EventCursor] string Cursor);
```

The client stores the latest cursor after it has processed the event. If the connection drops, the client opens the same subscription again and passes that cursor back as `after`.

```graphql
subscription {
  onReviewCreated(after: "Mw==") {
    review {
      id
      body
    }
    cursor
  }
}
```

The gateway resumes the stream after that cursor. A first-time subscriber simply omits the argument.

This is the part that matters for scale. The gateway stores no resume position. There is no per-subscriber cursor table, no position store, and no subscription state to move between gateway replicas. The resume position travels with the client, so a reconnect can land on any gateway instance behind your load balancer.

The cursor itself is a black box. It is a base64 token whose meaning belongs to the broker. Clients never parse it. They only store it and send it back when they need to resume. If you have used cursor-based paging in GraphQL, the pattern should feel familiar.

Because resume is modeled in the GraphQL schema rather than broker-specific client code, clients can write their resume logic once. You can change the broker behind a stream without changing how clients resume.

If you want clients to handle cursors generically, expose a shared interface for resumable payloads:

```graphql
type Subscription {
  onReviewCreated(after: String @eventCursor): ReviewCreated!
    @eventStream(message: "review { id }")
}

type ReviewCreated implements Resumable {
  review: Review!
  cursor: String @eventCursor
}

interface Resumable {
  cursor: String
}

type Review @key(fields: "id") {
  id: ID!
}
```

## Pick the broker that fits your world

Broker infrastructure stays out of your schema. The schema describes the shape of the event, while the gateway decides which broker implementation to use. Your services can keep publishing with their normal broker clients, and your GraphQL contract stays focused on the API.

Fusion ships five broker integrations that you can plug in and configure:

- **NATS** (core and JetStream)
- **Apache Kafka**
- **Azure Event Hubs**
- **Amazon SQS** (with optional SNS fan-out)
- **Redis**

```csharp
builder
    .AddGraphQLGateway()
    .AddNatsEventStreamBroker(options =>
    {
        options.Url = "nats://localhost:4222";
        options.JetStream = new NatsJetStreamOptions
        {
            Stream = "reviews"
        };
    });
```

Want Kafka instead? Swap `AddNatsEventStreamBroker` for `AddKafkaEventStreamBroker`. The event shape in your schema stays the same, while the broker configuration lives in host DI. Connection strings, partitions, SASL/SSL, JetStream stream and consumer names all live in your application code. None of that plumbing leaks into the published API contract.

If you use a broker we do not ship, or if you want to wrap an existing broker with your own authorization, filtering, or transformation logic, implement `IEventStreamBroker`:

```csharp
public interface IEventStreamBroker : IAsyncDisposable
{
    IAsyncEnumerable<EventMessage> SubscribeAsync(
        ISubscriptionFieldContext context,
        string[] topics,
        string? cursor,
        CancellationToken cancellationToken);
}
```

Fusion gives your broker the subscription field context, the topics to consume, and the optional resume cursor supplied by the client. Your broker returns `EventMessage` values with the raw JSON body and, when supported by your implementation, the next opaque cursor.

## Publishing stays in your application

How does a message get into the stream? This is where your application code connects. Event-driven architecture belongs in your domain, and the gateway is simply one more consumer of those events.

```csharp
await nats.PublishAsync(
    "onReviewCreated",
    JsonSerializer.SerializeToUtf8Bytes(new { review = new { id } }),
    cancellationToken: cancellationToken);
```

Whether you publish directly through your NATS client, use [Mocha](../docs/mocha/messaging-patterns.md), or hide the broker behind a small wrapper is up to you. Federated Event Streams takes the complex parts out of the subscription path and lets your application publish events from your domain without bleeding GraphQL execution details into the rest of your system.

## Try Fusion 16.4

Federated Event Streams is the main feature in Fusion 16.4, but it is not the only one. This release also continues our work on the GraphQL-Federation spec (aka Composite Schema spec). `@tag` can now be applied to directive definitions, directive definitions support deprecation and the `DIRECTIVE_DEFINITION` location in introspection, and Fusion adds opt-in feature support with `@requiresOptIn`.

The full Federated Event Streams reference, including every broker and the `@eventStream` / `@eventCursor` SDL, lives in the [subscriptions docs](../docs/fusion/subscriptions.md).

Give Fusion 16.4 a try, and tell us what works, what is missing, and where you want the feature to go next. Join us on [Slack](https://slack.chillicream.com).
