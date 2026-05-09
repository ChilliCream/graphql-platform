---
title: "Subscriptions"
---

Subscriptions allow clients to receive future events from your schema. When a client starts a `subscription` operation, it stays connected through a real-time transport and receives a stream of GraphQL execution results as matching events are published.

Use a subscription when a client needs to be notified of changes that occur after the initial state is loaded. First, load the current state with a query. Then, use a mutation or service to change the state, publish an event after the change succeeds, and let the subscription deliver subsequent updates.

```graphql
type Subscription {
  bookAdded: Book!
}

subscription WatchBooks {
  bookAdded {
    id
    title
  }
}
```

| Operation type | Use it for                                              | Result shape                                           |
| -------------- | ------------------------------------------------------- | ------------------------------------------------------ |
| Query          | Read current state once.                                | One execution result.                                  |
| Mutation       | Change state once and return the result of that action. | One execution result.                                  |
| Subscription   | Receive future events that match a field and topic.     | A stream of execution results with the selected shape. |

# Defining the `Subscription` Root Type

Apply `[SubscriptionType]` to contribute fields to the GraphQL `Subscription` root type. In implementation-first v16 code, declare the class as `partial` so the source generator can add schema wiring.

```csharp
#nullable enable

using HotChocolate;
using HotChocolate.Types;

namespace Catalog.Types;

public sealed record Book([property: ID<Book>] int Id, string Title);

[SubscriptionType]
public static partial class BookSubscriptions
{
    [Subscribe]
    public static Book OnBookAdded([EventMessage] Book book)
        => book;
}
```

Register the generated types with your GraphQL setup:

```csharp
builder
    .AddGraphQL()
    .AddTypes()
    .AddInMemorySubscriptions();
```

Expected SDL:

```graphql
type Subscription {
  bookAdded: Book!
}
```

Hot Chocolate uses these conventions:

| C# part                    | Schema or runtime effect                                                                                          |
| -------------------------- | ----------------------------------------------------------------------------------------------------------------- |
| `[SubscriptionType]`       | Adds fields to the schema `Subscription` root type.                                                               |
| `partial` class            | Allows the source generator to contribute the generated type setup.                                               |
| `[Subscribe]`              | Marks the method as a subscription field backed by a topic.                                                       |
| `[EventMessage] Book book` | Marks the event payload Hot Chocolate passes to the resolver for each message.                                    |
| `OnBookAdded`              | Becomes the field `bookAdded`; `On` is stripped and the result is camel-cased. It is also the default topic name. |

The method body runs for each received event. You can return the payload directly or map an internal event message to the public GraphQL shape.

If you use explicit type registration instead of the generated `AddTypes`, register a subscription root type with `.AddSubscriptionType<T>()`:

```csharp
builder
    .AddGraphQL()
    .AddSubscriptionType<BookSubscriptions>()
    .AddInMemorySubscriptions();
```

A GraphQL schema has one `Subscription` root type. You can split subscription fields across multiple `[SubscriptionType]` classes by domain, and Hot Chocolate merges them into the same root type.

# Publishing Events After State Changes

Publish subscription events using `ITopicEventSender`. This sender is provider-independent, so your mutation code remains unchanged if you switch from in-memory subscriptions to Redis, NATS, Postgres, RabbitMQ, or another provider.

Always publish after the write succeeds. If you publish before `SaveChangesAsync` or before your domain service commits the change, subscribers might receive data that was never persisted.

```csharp
#nullable enable

using HotChocolate;
using HotChocolate.Subscriptions;
using HotChocolate.Types;

namespace Catalog.Types;

public sealed record AddBookInput(string Title);

[MutationType]
public static partial class BookMutations
{
    public static async Task<Book> AddBookAsync(
        AddBookInput input,
        CatalogContext db,
        ITopicEventSender sender,
        CancellationToken cancellationToken)
    {
        var book = new Book(0, input.Title);

        db.Books.Add(book);
        await db.SaveChangesAsync(cancellationToken);

        await sender.SendAsync(
            nameof(BookSubscriptions.OnBookAdded),
            book,
            cancellationToken);

        return book;
    }
}
```

The first argument to `SendAsync` is the topic name. Without `[Topic]`, `[Subscribe]` uses the method name as the topic, so `nameof(BookSubscriptions.OnBookAdded)` keeps the publisher aligned with the subscriber.

For each event, clients receive one result with the selection set they asked for:

```json
{
  "data": {
    "bookAdded": {
      "id": "Qm9vazox",
      "title": "GraphQL in Practice"
    }
  }
}
```

`ITopicEventSender` can be injected into mutations, application services, hosted services, or background workers. Use `CompleteAsync(topicName)` when a domain stream has a real end, such as closing a chat or completing a job:

```csharp
await sender.CompleteAsync(chatTopicName);
```

# Route events with topics

Topics decide which subscribers receive a published event. Topic scoping is separate from data filtering. Do not publish all tenant, user, or resource data to one global topic and rely on a field resolver to hide data later.

## Use the default method-name topic

When a subscription field has `[Subscribe]` and no `[Topic]`, the topic is the C# method name.

```csharp
[Subscribe]
public static Book OnBookAdded([EventMessage] Book book)
    => book;
```

Publish with `nameof` so method renames update the topic at compile time:

```csharp
await sender.SendAsync(
    nameof(BookSubscriptions.OnBookAdded),
    book,
    cancellationToken);
```

Use the default when the C# method name is a stable internal topic name.

## Use a static topic name

Use `[Topic("...")]` when the topic name comes from an external event name or when you do not want to couple it to the C# method name.

```csharp
[Subscribe]
[Topic("NewBookAvailable")]
public static Book OnBookAdded([EventMessage] Book book)
    => book;
```

Publish to the same topic string:

```csharp
await sender.SendAsync("NewBookAvailable", book, cancellationToken);
```

## Use dynamic topics with arguments

Use argument placeholders when each subscriber should listen to a resource-specific topic.

```csharp
#nullable enable

using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Relay;

namespace Orders.Types;

public sealed record Order([property: ID<Order>] string Id, string Status);

[SubscriptionType]
public static partial class OrderSubscriptions
{
    [Subscribe]
    [Topic($"{{{nameof(orderId)}}}")]
    public static Order OnOrderStatusChanged(
        [ID<Order>] string orderId,
        [EventMessage] Order order)
        => order;
}
```

A client subscribing with `orderId: "order-42"` listens to topic `"order-42"`:

```graphql
subscription WatchOrder($orderId: ID!) {
  orderStatusChanged(orderId: $orderId) {
    id
    status
  }
}
```

Publish to the matching topic after the status update succeeds:

```csharp
await sender.SendAsync(orderId, order, cancellationToken);
```

The placeholder name must match the GraphQL argument name. Use tenant, user, or resource identifiers in topic names when the data is scoped.

## Combine multiple topic arguments

A topic pattern can include more than one argument:

```csharp
[Subscribe]
[Topic("Order_{tenantId}_{orderId}")]
public static Order OnOrderStatusChanged(
    [ID] string tenantId,
    [ID] string orderId,
    [EventMessage] Order order)
    => order;
```

Publishers must use the same formatting and value normalization:

```csharp
var topic = $"Order_{tenantId}_{orderId}";
await sender.SendAsync(topic, order, cancellationToken);
```

If subscribers receive wrong events or no events, compare the final topic string on both sides.

# Shape event payloads for clients

Every event is executed like a normal GraphQL result for the subscription selection set. Design the event payload as a client-facing schema contract, not as a dump of your internal message bus shape.

A simple entity payload works well when one event means one changed object:

```graphql
type Subscription {
  bookAdded: Book!
}

type Book {
  id: ID!
  title: String!
  author: String!
}
```

When one field represents several related event kinds, use an interface or union so clients can handle each shape explicitly:

```graphql
type Subscription {
  chatMessageChanged(chatId: ID!): ChatMessageEvent!
}

union ChatMessageEvent = ChatMessageCreated | ChatMessageUpdated

type ChatMessageCreated {
  messageId: ID!
  role: ChatMessageRole!
}

type ChatMessageUpdated {
  messageId: ID!
  role: ChatMessageRole!
}
```

Client operation:

```graphql
subscription WatchChat($chatId: ID!) {
  chatMessageChanged(chatId: $chatId) {
    ... on ChatMessageCreated {
      messageId
      role
    }
    ... on ChatMessageUpdated {
      messageId
      role
    }
  }
}
```

Use these payload rules for subscription fields:

- Include stable IDs so normalized clients can deduplicate mutation responses and subscription events.
- Include enough data for the client to update UI state without refetching after every event.
- Pair subscriptions with initial queries, because subscriptions only deliver future events.
- Avoid overloading one field with unrelated event kinds unless an interface or union makes the contract clear.

# Use custom subscribe resolvers

Use `[Subscribe(With = nameof(...))]` when the default topic binding is not enough. Common reasons include service-backed streams, resource ownership checks before the stream opens, advanced topic selection, or custom `ITopicEventReceiver.SubscribeAsync<T>` buffer options.

The custom subscribe resolver runs when a client starts the subscription. The field resolver runs for each event.

```csharp
#nullable enable

using HotChocolate;
using HotChocolate.Subscriptions;
using HotChocolate.Types;

namespace Catalog.Types;

[SubscriptionType]
public static partial class BookSubscriptions
{
    public static ValueTask<ISourceStream<Book>> SubscribeToBooks(
        ITopicEventReceiver receiver,
        CancellationToken cancellationToken)
        => receiver.SubscribeAsync<Book>(
            "CustomBookTopic",
            cancellationToken);

    [Subscribe(With = nameof(SubscribeToBooks))]
    public static Book OnBookAdded([EventMessage] Book book)
        => book;
}
```

You can pass buffer options for this subscription when the field needs a different policy from the provider default:

```csharp
public static ValueTask<ISourceStream<Book>> SubscribeToBooks(
    ITopicEventReceiver receiver,
    CancellationToken cancellationToken)
    => receiver.SubscribeAsync<Book>(
        "CustomBookTopic",
        bufferCapacity: 256,
        bufferFullMode: TopicBufferFullMode.DropWrite,
        cancellationToken);
```

A custom resolver can also return an `IAsyncEnumerable<T>` from an application service:

```csharp
#nullable enable

using System.Runtime.CompilerServices;
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Relay;

namespace Orders.Types;

[SubscriptionType]
public static partial class OrderSubscriptions
{
    public static async IAsyncEnumerable<OrderStatusChanged> SubscribeToOrderStatus(
        [ID<Order>] string orderId,
        OrderService orders,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var item in orders.WatchStatusAsync(orderId, cancellationToken))
        {
            yield return item;
        }
    }

    [Subscribe(With = nameof(SubscribeToOrderStatus))]
    public static OrderStatusChanged OnOrderStatusChanged(
        [EventMessage] OrderStatusChanged change,
        [ID<Order>] string orderId)
        => change;
}
```

Pass cancellation tokens into stream APIs so abandoned subscriptions stop work. Prefer services that return source streams or async enumerables. Keep data loading for the event result resolver or nested object fields.

# Authorize and isolate subscriptions

Protect subscription fields that expose user-specific or restricted data. Use `HotChocolate.Authorization.AuthorizeAttribute`, not the ASP.NET Core MVC attribute.

```csharp
#nullable enable

using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Types;
using HotChocolate.Types.Relay;

namespace Orders.Types;

[SubscriptionType]
public static partial class OrderSubscriptions
{
    [Subscribe]
    [Authorize]
    [Topic($"Order_{{{nameof(orderId)}}}")]
    public static Order OnOrderStatusChanged(
        [ID<Order>] string orderId,
        [EventMessage] Order order)
        => order;
}
```

Field authorization checks whether the subscriber can execute the field. Resource-specific access should be checked before opening the stream, because the topic decides which events the subscriber can receive.

```csharp
public static async ValueTask<ISourceStream<Order>> SubscribeToOrderStatusAsync(
    [ID<Order>] string orderId,
    OrderService orders,
    ITopicEventReceiver receiver,
    CancellationToken cancellationToken)
{
    if (!await orders.CanViewOrderAsync(orderId, cancellationToken))
    {
        throw new GraphQLException("You are not allowed to subscribe to this order.");
    }

    return await receiver.SubscribeAsync<Order>(
        $"Order_{orderId}",
        cancellationToken);
}
```

Combine authorization with topic isolation:

- Add `[Authorize]` to the subscription type or field for authentication, roles, and policies.
- Check tenant, user, or resource access before returning the stream.
- Include tenant, user, or resource identifiers in topic names for scoped data.
- Do not treat connection authentication as permission for every resource-specific subscription.

Connection-level WebSocket authentication and interceptors are transport concerns. See [Interceptors](/docs/hotchocolate/v16/build/server-configuration/interceptors) and [HTTP Transport](/docs/hotchocolate/v16/build/server-configuration/http-transport) for endpoint behavior.

# Choose a subscription provider

A server must register one subscription provider for topic fanout. The schema defines what clients can subscribe to. The provider moves events between publishers and subscribers.

| Provider  | Package                               | Registration method              | Use when                                                              | Watch for                                             |
| --------- | ------------------------------------- | -------------------------------- | --------------------------------------------------------------------- | ----------------------------------------------------- |
| In-memory | Built in                              | `.AddInMemorySubscriptions(...)` | Local development or single-instance apps.                            | No cross-instance fanout. Events are lost on restart. |
| Redis     | `HotChocolate.Subscriptions.Redis`    | `.AddRedisSubscriptions(...)`    | You already operate Redis and need multi-instance fanout.             | Requires `StackExchange.Redis` connection setup.      |
| NATS      | `HotChocolate.Subscriptions.Nats`     | `.AddNatsSubscriptions(...)`     | You use NATS core publish/subscribe for multi-instance fanout.        | Configure a NATS client in dependency injection.      |
| Postgres  | `HotChocolate.Subscriptions.Postgres` | `.AddPostgresSubscriptions(...)` | You already operate PostgreSQL and want `LISTEN/NOTIFY` based fanout. | Follow provider connection guidance.                  |
| RabbitMQ  | `HotChocolate.Subscriptions.RabbitMQ` | `.AddRabbitMQSubscriptions(...)` | You use RabbitMQ as broker infrastructure.                            | Follow provider queue and connection guidance.        |

For local development, the minimal setup looks like this:

```csharp
builder
    .AddGraphQL()
    .AddTypes()
    .AddInMemorySubscriptions();
```

All providers accept `SubscriptionOptions`:

```csharp
using HotChocolate.Subscriptions;

builder
    .AddGraphQL()
    .AddTypes()
    .AddInMemorySubscriptions(new SubscriptionOptions
    {
        TopicPrefix = "catalog-dev",
        TopicBufferCapacity = 128,
        TopicBufferFullMode = TopicBufferFullMode.DropOldest,
    });
```

| Option                | Default      | Use it for                                                                                   |
| --------------------- | ------------ | -------------------------------------------------------------------------------------------- |
| `TopicPrefix`         | `null`       | Isolate apps, tenants, or environments that share a broker.                                  |
| `TopicBufferCapacity` | `64`         | Set the per-topic message buffer size.                                                       |
| `TopicBufferFullMode` | `DropOldest` | Choose what happens when a topic buffer is full: `DropOldest`, `DropNewest`, or `DropWrite`. |

Subscription providers are not durable event stores unless the provider documentation says so. If clients must recover missed events after reconnecting, model that with queries, cursors, or an application event store.

# Enable a real-time transport

Defining a `Subscription` field does not enable a transport. Enabling WebSocket or SSE does not create subscription fields.

Use this boundary when you troubleshoot:

| Layer     | Responsibility                                                       |
| --------- | -------------------------------------------------------------------- |
| Schema    | Defines subscription fields, arguments, payloads, and authorization. |
| Provider  | Moves topic messages between publishers and active subscribers.      |
| Transport | Delivers GraphQL results between the server and client connection.   |

For WebSocket subscriptions, add WebSocket middleware before mapping GraphQL:

```csharp
app.UseWebSockets();
app.MapGraphQL();
```

Keep transport configuration in the server docs. See [HTTP Transport](/docs/hotchocolate/v16/build/server-configuration/http-transport) for HTTP, WebSocket, and SSE behavior, and [Interceptors](/docs/hotchocolate/v16/build/server-configuration/interceptors) for connection and operation interception.

# Troubleshoot subscription fields

| Symptom                                                    | Likely cause                                                                                                                                  | Fix or link                                                                                                                                                                |
| ---------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Subscription field is missing from the schema.             | The class lacks `[SubscriptionType]`, is not `partial`, generated `.AddTypes()` is not registered, or `.AddSubscriptionType<T>()` is missing. | Add the root type attribute or explicit registration, then inspect the generated SDL.                                                                                      |
| Schema build fails for the subscription message type.      | The field has no `[EventMessage]` parameter and `[Subscribe]` cannot infer the message type.                                                  | Add `[EventMessage]` to the payload parameter or use an explicit subscribe configuration.                                                                                  |
| Client connects but receives no events.                    | The publisher sends to a different topic than the subscription listens to.                                                                    | Compare the topic used by `[Topic]`, the method-name default, and `SendAsync`.                                                                                             |
| Dynamic topic receives wrong events.                       | Placeholder name, argument value, or publisher formatting does not match.                                                                     | Log or inspect the final topic string and normalize IDs consistently.                                                                                                      |
| It works locally but not across multiple server instances. | The in-memory provider is registered.                                                                                                         | Use Redis, NATS, Postgres, RabbitMQ, or another external provider.                                                                                                         |
| Unauthorized subscribers can open a stream.                | Missing `[Authorize]`, missing resource checks, or overly broad topic names.                                                                  | Add field authorization, check resource access in a custom subscribe resolver, and scope topics.                                                                           |
| Events appear duplicated in the UI.                        | The mutation response and subscription event both update the same client list.                                                                | Deduplicate by stable IDs and decide whether the mutation response or subscription event owns the optimistic update.                                                       |
| A stream never ends for a completed resource.              | The publisher never calls `CompleteAsync(topicName)` for a domain stream that has ended.                                                      | Call `CompleteAsync` when the resource lifecycle ends.                                                                                                                     |
| WebSocket or SSE connection fails.                         | Transport middleware, protocol, authentication, or client configuration is incorrect.                                                         | See [HTTP Transport](/docs/hotchocolate/v16/build/server-configuration/http-transport) and [Interceptors](/docs/hotchocolate/v16/build/server-configuration/interceptors). |

# Next steps

- Load initial state with [Queries](./operations-queries).
- Publish events after writes from [Mutations](./operations-mutations).
- Design payload shapes with [Object Types](./object-types), [Interfaces](./interfaces), and [Unions](./unions).
- Model subscription input with [Arguments](./arguments) and [Lists and Non-Null](./lists-and-non-null).
- Use services and cancellation tokens with [Dependency Injection](/docs/hotchocolate/v16/build/resolvers/service-injection).
- Protect fields with [Authorization](/docs/hotchocolate/v16/build/security/authorization).
- Configure endpoint behavior with [HTTP Transport](/docs/hotchocolate/v16/build/server-configuration/http-transport) and [Interceptors](/docs/hotchocolate/v16/build/server-configuration/interceptors).
- Tune buffers with [Subscription Options](/docs/hotchocolate/v16/build/server-configuration/schema-options#subscription-options).
