---
title: "Subscriptions"
---

GraphQL subscriptions allow clients to receive real-time updates from the server. A client opens a persistent connection (over WebSocket or SSE) and asks for specific events. When those events occur, the server pushes the data to the client immediately.

Subscriptions differ from queries and mutations in one key way: the client receives a stream of results rather than a single response. Each result has the same shape as a query response.

**GraphQL schema**

```graphql
type Subscription {
  orderStatusChanged(orderId: ID!): Order!
  bookAdded: Book!
}
```

**Client subscription**

```graphql
subscription {
  bookAdded {
    title
    author
  }
}
```

The client stays connected and receives a new `bookAdded` result each time the server publishes that event.

# Defining a Subscription Type

Mark a class with `[SubscriptionType]` and the source generator registers it as part of the Subscription type. The class must be `partial` so the source generator can add code at build time.

Each subscription field uses two attributes:

- `[Subscribe]` tells Hot Chocolate this field represents a subscription and should be backed by a topic from the pub/sub system.
- `[EventMessage]` marks the parameter that receives the event payload when a message arrives on the topic.

<ExampleTabs>
<Implementation>

```csharp
// Types/BookSubscriptions.cs
[SubscriptionType]
public static partial class BookSubscriptions
{
    [Subscribe]
    public static Book OnBookAdded([EventMessage] Book book)
        => book;
}
```

The source generator wires up the Subscription type automatically. No additional registration call is needed beyond the source generator's `AddTypes`.

</Implementation>
<Code>

```csharp
// Types/BookSubscriptions.cs
public class BookSubscriptions
{
    [Subscribe]
    public Book OnBookAdded([EventMessage] Book book)
        => book;
}
```

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddSubscriptionType<BookSubscriptions>();
```

</Code>
</ExampleTabs>

The method body returns the event payload. Hot Chocolate calls this method each time a message arrives, so you can transform or filter the payload before it reaches the client.

# Publishing Events with ITopicEventSender

To trigger a subscription, you publish an event using `ITopicEventSender`. This abstraction works with any configured subscription provider (in-memory, Redis, NATS, or Postgres), so you can switch providers without changing your publishing code.

You typically publish events from mutations after a successful write. Inject `ITopicEventSender` as a method parameter, the same way you inject any other service.

```csharp
// Types/BookMutations.cs
[MutationType]
public static partial class BookMutations
{
    public static async Task<Book> AddBookAsync(
        string title,
        string author,
        CatalogContext db,
        ITopicEventSender sender,
        CancellationToken ct)
    {
        var book = new Book { Title = title, Author = author };
        db.Books.Add(book);
        await db.SaveChangesAsync(ct);

        await sender.SendAsync(nameof(BookSubscriptions.OnBookAdded), book, ct);

        return book;
    }
}
```

The first argument to `SendAsync` is the topic name. By default, Hot Chocolate maps the topic to the subscription field by method name. Using `nameof` keeps the topic and the subscription field in sync at compile time.

You can also publish events from anywhere you have access to `ITopicEventSender` through dependency injection, not only from mutations.

# Topic Filtering with Dynamic Topics

By default, every subscriber to a field receives every event published to that topic. When you need subscribers to receive events for a specific resource, use the `[Topic]` attribute with argument placeholders to create dynamic topics.

```csharp
// Types/OrderSubscriptions.cs
[SubscriptionType]
public static partial class OrderSubscriptions
{
    [Subscribe]
    [Topic($"{{{nameof(orderId)}}}")]
    public static Order OnOrderStatusChanged(
        [ID] string orderId,
        [EventMessage] Order order)
        => order;
}
```

The `{orderId}` placeholder is replaced with the actual argument value at subscription time. A client subscribing with `orderId: "order-42"` only receives events published to the topic `"order-42"`.

Publish to the matching topic from your mutation:

```csharp
// Types/OrderMutations.cs
[MutationType]
public static partial class OrderMutations
{
    public static async Task<Order> UpdateOrderStatusAsync(
        [ID] string orderId,
        OrderStatus newStatus,
        OrderService orders,
        ITopicEventSender sender,
        CancellationToken ct)
    {
        var order = await orders.UpdateStatusAsync(orderId, newStatus, ct);

        await sender.SendAsync(orderId, order, ct);

        return order;
    }
}
```

You can combine multiple arguments in a single topic pattern. Each placeholder uses the format `{argumentName}`:

```csharp
[Subscribe]
[Topic("OnMessage_{arg1}_{arg2}")]
public static string OnMessage(string arg1, string arg2, [EventMessage] string message)
    => message;
```

# Static Topics

If you want to decouple the topic name from the method name, use `[Topic]` with a fixed string.

```csharp
// Types/BookSubscriptions.cs
[SubscriptionType]
public static partial class BookSubscriptions
{
    [Subscribe]
    [Topic("NewBookAvailable")]
    public static Book OnBookAdded([EventMessage] Book book)
        => book;
}
```

Publish to the same static topic string:

```csharp
await sender.SendAsync("NewBookAvailable", book, ct);
```

# Custom Subscribe Resolvers

If you need more control over how a subscription connects to the pub/sub system, use `[Subscribe(With = ...)]` to point to a custom subscribe resolver method.

```csharp
// Types/BookSubscriptions.cs
[SubscriptionType]
public static partial class BookSubscriptions
{
    public static ValueTask<ISourceStream<Book>> SubscribeToBooks(
        ITopicEventReceiver receiver)
        => receiver.SubscribeAsync<Book>("CustomBookTopic");

    [Subscribe(With = nameof(SubscribeToBooks))]
    public static Book OnBookAdded([EventMessage] Book book)
        => book;
}
```

The `With` parameter names a method on the same class that returns a `ValueTask<ISourceStream<T>>`. Hot Chocolate calls this method when a client subscribes. This is useful when the topic name depends on runtime logic that goes beyond argument placeholders.

# Transport Mechanisms

Subscriptions require a persistent connection between the client and server. Hot Chocolate supports two transport mechanisms.

## WebSocket (graphql-ws protocol)

WebSocket provides a full-duplex channel over a single TCP connection. Both the client and server can send messages at any time. This is the most widely supported option for GraphQL subscriptions.

Hot Chocolate supports both the modern [graphql-ws](https://github.com/enisdenjo/graphql-ws) protocol and the legacy [subscriptions-transport-ws](https://github.com/apollographql/subscriptions-transport-ws) protocol. Use graphql-ws for new projects.

Add the WebSocket middleware to your request pipeline:

```csharp
// Program.cs
app.UseRouting();

app.UseWebSockets();

app.MapGraphQL();
```

## Server-Sent Events (graphql-sse)

Server-Sent Events (SSE) is a one-way channel where the server pushes updates to the client over HTTP. SSE works well with HTTP/2 and has better firewall compatibility than WebSocket. The trade-off is that SSE only supports server-to-client communication.

Hot Chocolate supports the [graphql-sse](https://github.com/enisdenjo/graphql-sse) protocol. SSE works out of the box when you map the GraphQL endpoint. No additional middleware is needed.

Choose WebSocket when you need bidirectional communication or broad client library support. Choose SSE when you want to leverage HTTP/2 multiplexing and avoid WebSocket-related firewall issues.

# Subscription Providers

A subscription provider is the pub/sub backend that delivers events between your mutation (the publisher) and the subscription (the subscriber). You must register exactly one provider.

## In-Memory (default)

The in-memory provider works without any external infrastructure. It is suitable for single-server deployments and local development.

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddInMemorySubscriptions();
```

Events are lost if the server restarts, and they are not shared across multiple server instances.

## Redis

The Redis provider supports multi-instance deployments. Events published on one server instance are delivered to subscribers connected to any instance.

Install the package:

<PackageInstallation packageName="HotChocolate.Subscriptions.Redis" />

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddRedisSubscriptions(
        _ => ConnectionMultiplexer.Connect("localhost:6379"));
```

The Redis provider uses [StackExchange.Redis](https://github.com/StackExchange/StackExchange.Redis) under the hood.

## NATS

The NATS provider is new in Hot Chocolate v16. Like Redis, it supports multi-instance deployments. NATS uses core publish/subscribe. JetStream is not required.

Install the packages:

<PackageInstallation packageName="HotChocolate.Subscriptions.Nats" />

<PackageInstallation packageName="NATS.Extensions.Microsoft.DependencyInjection" external />

```csharp
// Program.cs
using NATS.Extensions.Microsoft.DependencyInjection;

builder.Services
    .AddNatsClient(
        nats => nats.ConfigureOptions(
            options => options.Configure(
                opts => opts.Opts = opts.Opts with
                {
                    Url = "nats://localhost:4222"
                })));

builder.Services
    .AddGraphQLServer()
    .AddSubscriptionType<BookSubscriptions>()
    .AddNatsSubscriptions();
```

If multiple GraphQL servers share the same NATS broker, set a `TopicPrefix` to isolate their topics:

```csharp
// Program.cs
using HotChocolate.Subscriptions;

builder.Services
    .AddGraphQLServer()
    .AddSubscriptionType<BookSubscriptions>()
    .AddNatsSubscriptions(
        new SubscriptionOptions
        {
            TopicPrefix = "orders-service-dev"
        });
```

## Postgres

The Postgres provider uses PostgreSQL's native `LISTEN/NOTIFY` mechanism. This is a good choice when you already run PostgreSQL and want to avoid adding a separate pub/sub service.

Install the package:

<PackageInstallation packageName="HotChocolate.Subscriptions.Postgres" />

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddSubscriptionType<BookSubscriptions>()
    .AddPostgresSubscriptions(options =>
        options.ConnectionFactory = ct => /* create your NpgsqlConnection */);
```

For the connection factory, configure a long-lived connection with pooling disabled:

```csharp
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.ConnectionStringBuilder.Pooling = false;
dataSourceBuilder.ConnectionStringBuilder.KeepAlive = 30;
dataSourceBuilder.ConnectionStringBuilder.Enlist = false;

var dataSource = dataSourceBuilder.Build();
```

# Splitting Across Multiple Classes

GraphQL allows only one Subscription type per schema, but you can split your subscription fields across multiple classes. With the source generator, annotate each class with `[SubscriptionType]`. The source generator merges them into one Subscription type.

```csharp
// Types/BookSubscriptions.cs
[SubscriptionType]
public static partial class BookSubscriptions
{
    [Subscribe]
    public static Book OnBookAdded([EventMessage] Book book)
        => book;
}
```

```csharp
// Types/OrderSubscriptions.cs
[SubscriptionType]
public static partial class OrderSubscriptions
{
    [Subscribe]
    [Topic($"{{{nameof(orderId)}}}")]
    public static Order OnOrderStatusChanged(
        [ID] string orderId,
        [EventMessage] Order order)
        => order;
}
```

This produces a schema with both fields on the Subscription type:

```graphql
type Subscription {
  onBookAdded: Book!
  onOrderStatusChanged(orderId: ID!): Order!
}
```

Group your subscription classes by domain area, the same way you would split queries and mutations.

# Next Steps

- **Need to read data?** See [Queries](/docs/hotchocolate/v16/defining-a-schema/queries).
- **Need to write data?** See [Mutations](/docs/hotchocolate/v16/defining-a-schema/mutations).
- **Need to understand how types map to the schema?** See [Object Types](/docs/hotchocolate/v16/defining-a-schema/object-types).
- **Need to authenticate WebSocket connections?** See the [WebSocket authentication example](https://github.com/ChilliCream/hotchocolate-examples/tree/master/misc/WebsocketAuthentication).
