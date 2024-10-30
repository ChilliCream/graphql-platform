---
title: "Subscriptions"
---

<Video videoId="wHC9gOk__y0" />

GraphQL subscriptions provide real-time functionality to applications by allowing clients to subscribe to specific events. When these events trigger, the server immediately sends updates to the subscribed clients.

# Transport Mechanisms for GraphQL Subscriptions

The method of how these updates are delivered is determined by the transport mechanism. In this section, we will discuss two popular transport mechanisms: GraphQL over WebSockets and GraphQL over Server-Sent Events (SSE).

## GraphQL over WebSockets

WebSockets provide a full-duplex communication channel over a single TCP connection. This means data can be sent and received simultaneously. With GraphQL, this means both queries/mutations and subscription operations can be sent over the same connection.

WebSockets are widely supported in browsers and have been the de facto standard for real-time data transport in GraphQL. There are two popular protocols for GraphQL over WebSockets: [graphql-ws](https://github.com/enisdenjo/graphql-ws) and [subscription-transport-ws](https://github.com/apollographql/subscriptions-transport-ws).
Hot Chocolate, supports both protocols.

In terms of specific protocols, the recommendation is to use graphql-ws or graphql-sse over the legacy subscription-transport-ws.

**Key Features:**

- Full-duplex: Both the client and server can initiate communication, allowing real-time bidirectional communication.
- Persistent connection: The connection between client and server remains open, allowing for real-time data transfer.
- Well-supported: There are several libraries available for managing WebSocket connections and GraphQL subscriptions.

## GraphQL over Server-Sent Events (SSE)

Server-Sent Events (SSE) is a standard that allows a server to push real-time updates to clients over HTTP. Unlike WebSockets, SSE is a half-duplex communication channel, which means the server can send messages to the client, but not the other way around. This makes it a good fit for one-way real-time data like updates or notifications.

With GraphQL, you can send regular queries and mutations over HTTP/2 and subscription updates over SSE. This combination leverages the strengths of both HTTP/2 (efficient for request-response communication) and SSE (efficient for server-to-client streaming).

Another advantage of SSE is its better compatibility with firewalls compared to WebSockets. However, if you're using HTTP/1, keep in mind that SSE inherits its limitations, such as supporting no more than 7 parallel requests in the browser.

[graphql-sse](https://github.com/enisdenjo/graphql-sse) is a popular library for GraphQL over SSE.

**Key Features:**

- Efficient for one-way real-time data: The server can push updates to the client as soon as they occur.
- Built on HTTP: SSE is built on HTTP, simplifying handling and compatibility. It benefits from HTTP features such as automatic reconnection, HTTP/2 multiplexing, and headers/cookies support.
- Less Complex: SSE is less complex than WebSockets as it only allows for one-way communication.
- Better Firewall Compatibility: SSE generally encounters fewer issues with firewalls.

Choosing between GraphQL over WebSockets and GraphQL over SSE depends on the specific needs of your application. If you need full-duplex, real-time communication, WebSockets may be the best choice. If you only need server-to-client real-time communication and want to take advantage of existing HTTP infrastructure, SSE could be a better option.

Special thanks to Denis Badurina, @enisdenjo on [Twitter](https://twitter.com/enisdenjo) and [GitHub](https://github.com/enisdenjo). He is the creator of [graphql-http](https://github.com/enisdenjo/graphql-http), [graphql-ws](https://github.com/enisdenjo/graphql-ws) and [graphql-sse](https://github.com/enisdenjo/graphql-sse).

# Usage

Subscribing to an event is like writing a standard query. The only difference is the operation keyword and that we are only allowed to have one root field.

```sdl
type Subscription {
  bookAdded: Book!
  bookPublished(author: String!): Book!
}
```

```graphql
subscription {
  bookAdded {
    title
  }
}
```

A subscription type can be defined like the following.

<ExampleTabs>
<Implementation>

```csharp
public class Subscription
{
    [Subscribe]
    public Book BookAdded([EventMessage] Book book) => book;
}
```

```csharp
builder.Services
    .AddGraphQLServer()
    .AddSubscriptionType<Subscription>();
```

</Implementation>
<Code>

```csharp
public class SubscriptionType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("bookAdded")
            .Type<BookType>()
            .Resolve(context => context.GetEventMessage<Book>())
            .Subscribe(async context =>
            {
                var receiver = context.Service<ITopicEventReceiver>();

                ISourceStream stream =
                    await receiver.SubscribeAsync<Book>("bookAdded");

                return stream;
            });
    }
}
```

```csharp
builder.Services
    .AddGraphQLServer()
    .AddSubscriptionType<SubscriptionType>();
```

</Code>
<Schema>

```csharp
public class Subscription
{
    [Subscribe]
    public Book BookAdded([EventMessage] Book book) => book;
}
```

```csharp
builder.Services
    .AddGraphQLServer()
    .AddDocumentFromString(@"
        type Subscription {
          bookAdded: Book!
        }

        type Book {
          title: String
          author: String
        }
    ")
    .BindRuntimeType<Subscription>();
```

</Schema>
</ExampleTabs>

> Warning: Only **one** subscription type can be registered using `AddSubscriptionType()`. If we want to split up our subscription type into multiple classes, we can do so using type extensions.
>
> [Learn more about extending types](/docs/hotchocolate/v14/defining-a-schema/extending-types)

A subscription type is just a regular object type, so everything that applies to an object type also applies to the subscription type (this is true for all all root types).

[Learn more about object types](/docs/hotchocolate/v14/defining-a-schema/object-types)

# Transport

After defining the subscription type, we need to add the WebSockets middleware to our request pipeline.

```csharp
app.UseRouting();

app.UseWebSockets();

app.UseEndpoints(endpoints =>
{
    endpoints.MapGraphQL();
});
```

To make pub/sub work, we also have to register a subscription provider. A subscription provider represents a pub/sub implementation used to handle events. Out of the box we support two subscription providers.

## In-Memory Provider

The In-Memory subscription provider does not need any configuration and is easily setup.

```csharp
builder.Services
    .AddGraphQLServer()
    .AddInMemorySubscriptions();
```

## Redis Provider

The Redis subscription provider enables us to run multiple instances of our Hot Chocolate GraphQL server and handle subscription events reliably.

In order to use the Redis provider we have to add the `HotChocolate.Subscriptions.Redis` package.

<PackageInstallation packageName="HotChocolate.Subscriptions.Redis" />

After we have added the package we can setup the Redis subscription provider.

```csharp
builder.Services
    .AddGraphQLServer()
    .AddRedisSubscriptions((sp) => ConnectionMultiplexer.Connect("host:port"));
```

Our Redis subscription provider uses the [StackExchange.Redis](https://github.com/StackExchange/StackExchange.Redis) Redis client underneath.

## Postgres Provider

The PostgreSQL Subscription Provider enables your GraphQL server to provide real-time updates to your clients using PostgreSQL's native `LISTEN/NOTIFY` mechanism. This provider is ideal for applications that already use PostgreSQL and want to avoid the overhead of running a separate pub/sub service.

In order to use the PostgreSQL provider we have to add the `HotChocolate.Subscriptions.Postgres` package.

```bash
dotnet add package HotChocolate.Subscriptions.Postgres
```

To enable Postgres subscriptions with your HotChocolate server, add `AddPostgresSubscriptions` to your GraphQL server configuration:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>() // every GraphQL server needs a query
    .AddSubscriptionType<Subscriptions>()
    .AddPostgresSubscriptions((sp, options) => options.ConnectionFactory = ct => /*create you connection*/);
```

### Options

`PostgresSubscriptionOptions` encapsulates options for configuring the Postgres subscription provider. The properties included in this class are:

1. `ConnectionFactory`: A function used to create a new, long-lived connection. The connection should have the following configuration to work optimally:

   - `KeepAlive=30`: Sets a keep alive interval to keep the connection alive
   - `Pooling=false`: Disables pooling as it is not needed
   - `Enlist=false`: Ensures subscriptions run in the background and are not enlisted into any transaction

2. `ChannelName`: Specifies the name of the Postgres channel used to send/receive messages. The default value is "hotchocolate_subscriptions".
3. `MaxSendBatchSize`: Sets the maximum number of messages sent in one batch. The default value is 256.
4. `MaxSendQueueSize`: Determines the maximum number of messages that can be queued for sending. If the queue is full, the subscription will wait until there is available space. The default value is 2048.
5. `SubscriptionOptions`: Options used to configure the subscriptions.

Here's an example of creating a connection factory suitable for long-lived connections:

```csharp
var builder = new NpgsqlDataSourceBuilder(connectionString);

// we do not need pooling for long running connections
builder.ConnectionStringBuilder.Pooling = false;
// we set the keep alive to 30 seconds
builder.ConnectionStringBuilder.KeepAlive = 30;
// as these tasks often run in the background we do not want to enlist them so they do not
// interfere with the main transaction
builder.ConnectionStringBuilder.Enlist = false;

var dataSource = builder.Build();
```

# Publishing Events

To publish events and trigger subscriptions, we can use the `ITopicEventSender`. The `ITopicEventSender` is an abstraction for the registered event publishing provider. Using this abstraction allows us to seamlessly switch between subscription providers, when necessary.

Most of the time we will be publishing events for successful mutations. Therefore we can simply inject the `ITopicEventSender` into our mutations like we would with every other `Service`. Of course we can not only publish events from mutations, but everywhere we have access to the `ITopicEventSender` through the DI Container.

```csharp
public class Mutation
{
    public async Book AddBook(Book book, ITopicEventSender sender)
    {
        await sender.SendAsync("BookAdded", book);

        // Omitted code for brevity
    }
}
```

In the example the `"BookAdded"` is the topic we want to publish to, and `book` is our payload. Even though we have used a string as the topic, we do not have to. Any other type works just fine.

But where is the connection between `"BookAdded"` as a topic and the subscription type? By default, Hot Chocolate will try to map the topic to a field of the subscription type. If we want to make this binding less error-prone, we could do the following.

```csharp
await sender.SendAsync(nameof(Subscription.BookAdded), book);
```

If we do not want to use the method name, we could use the `Topic` attribute.

```csharp
public class Subscription
{
    [Subscribe]
    [Topic("ExampleTopic")]
    public Book BookAdded([EventMessage] Book book) => book;
}

public async Book AddBook(Book book, ITopicEventSender sender)
{
    await sender.SendAsync("ExampleTopic", book);

    // Omitted code for brevity
}
```

## Dynamic Topics

We can even use the `Topic` attribute on dynamic arguments of the subscription field.

```csharp
public class Subscription
{
    [Subscribe]
    // The topic argument must be in the format "{argument}"
    // Using string interpolation and nameof is a good way to reference the argument name properly
    [Topic($"{{{nameof(author)}}}")]
    public Book BookPublished(string author, [EventMessage] Book book)
        => book;
}

public async Book PublishBook(Book book, ITopicEventSender sender)
{
    await sender.SendAsync(book.Author, book);

    // Omitted code for brevity
}
```

## ITopicEventReceiver

If more complex topics are required, we can use the `ITopicEventReceiver`.

```csharp
public class Subscription
{
    public ValueTask<ISourceStream<Book>> SubscribeToBooks(ITopicEventReceiver receiver)
        => receiver.SubscribeAsync<Book>("ExampleTopic");

    [Subscribe(With = nameof(SubscribeToBooks))]
    public Book BookAdded([EventMessage] Book book)
        => book;
}
```
