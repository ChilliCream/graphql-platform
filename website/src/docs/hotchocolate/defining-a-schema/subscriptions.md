---
title: "Subscriptions"
---

import { ExampleTabs, Annotation, Code, Schema } from "../../../components/mdx/example-tabs"

The subscription type in GraphQL is used to add real-time capabilities to our applications. Clients can subscribe to events and receive the event data in real-time, as soon as the server publishes it.

```sdl
type Subscription {
  bookAdded: Book!
  bookPublished(author: String!): Book!
}
```

Subscribing to an event is like writing a standard query. The only difference is the operation keyword and that we are only allowed to have one root field.

```graphql
subscription {
  bookAdded {
    title
  }
}
```

Hot Chocolate implements subscriptions via WebSockets and uses the pub/sub approach of [Apollo](https://www.apollographql.com/docs/apollo-server/data/subscriptions/#the-pubsub-class) for triggering subscriptions.

# Usage

A subscription type can be defined like the following.

<ExampleTabs>
<Annotation>

```csharp
public class Subscription
{
    [Subscribe]
    public Book BookAdded([EventMessage] Book book) => book;
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddSubscriptionType<Subscription>();
    }

    // Omitted code for brevity
}
```

</Annotation>
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
                    await receiver.SubscribeAsync<string, Book>("bookAdded");

                return stream;
            });
    }
}


public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddSubscriptionType<SubscriptionType>();
    }

    // Omitted code for brevity
}
```

</Code>
<Schema>

```csharp
public class Subscription
{
    [Subscribe]
    public Book BookAdded([EventMessage] Book book) => book;
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
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
    }

    // Omitted code for brevity
}
```

</Schema>
</ExampleTabs>

> ⚠️ Note: Only **one** subscription type can be registered using `AddSubscriptionType()`. If we want to split up our subscription type into multiple classes, we can do so using type extensions.
>
> [Learn more about extending types](/docs/hotchocolate/defining-a-schema/extending-types)

A subscription type is just a regular object type, so everything that applies to an object type also applies to the subscription type (this is true for all all root types).

[Learn more about object types](/docs/hotchocolate/defining-a-schema/object-types)

# Transport

After defining the subscription type, we need to add the WebSockets middleware to our request pipeline.

```csharp
public class Startup
{
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();

        app.UseWebSockets();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGraphQL();
        });
    }

    // Omitted code for brevity
}
```

To make pub/sub work, we also have to register a subscription provider. A subscription provider represents a pub/sub implementation used to handle events. Out of the box we support two subscription providers.

## In-Memory Provider

The In-Memory subscription provider does not need any configuration and is easily setup.

```csharp
services.AddInMemorySubscriptions();
```

## Redis Provider

**InMemory provider distributes events only in a single process.**
If we have multiple instances of our Hot Chocolate GraphQL server, users subscribed to other instances than the one which published the event won't receive it.
Redis and other tools address this by being a separate hub to which all instances publish their events and which then redistributes them back.
Thus Redis subscription provider enables us to run multiple instances and handle subscription events reliably.

In order to use the Redis provider we have to add the `HotChocolate.Subscriptions.Redis` package.

```bash
dotnet add package HotChocolate.Subscriptions.Redis
```

> ⚠️ Note: All `HotChocolate.*` packages need to have the same version.

After adding the package we can setup the Redis subscription provider.

```csharp
services.AddRedisSubscriptions((sp) =>
    ConnectionMultiplexer.Connect("host:port"));
```

Our Redis subscription provider uses the [StackExchange.Redis](https://github.com/StackExchange/StackExchange.Redis) Redis client underneath.

## RabbitMQ Provider

Where Redis is a database that may be used as a message broker, RabbitMQ is the message broker first and foremost.
RabbitMQ provides advanced concept and use cases such as message persistance.

In order to use the RabbitMQ provider we have to add the `HotChocolate.Subscriptions.RabbitMQ` package.

```bash
dotnet add package HotChocolate.Subscriptions.RabbitMQ
```

After adding the package we can setup the RabbitMQ subscription provider.

```csharp
services.AddRabbitMQSubscriptions((sp) => {
    var factory = new ConnectionFactory() {
        HostName = "localhost",
    };
    return factory.CreateConnection();
});
```

The way how exchanges and queues are declared and bound together can be overridden in configuration callback.

```csharp
services.AddRabbitMQSubscriptions(..., opts => {
    // Now exchanges will be declared as durable
    opts.DeclareExchange = (channel, name) => channel.ExchangeDeclare(name, ExchangeType.Direct, durable: true);
    // All queues created by this HotChocolate GraphQL server instance will include this identifier
    opts.InstanceName = "My test instance";
});
```

Naming convention and serialization can be override by implementing `ISerializer`, `IExchangeNameFactory` and `IQueueNameFactory`.

Our RabbitMQ subscription provider uses the [RabbitMQ.Client](https://www.rabbitmq.com/dotnet.html) underneath.

# Publishing Events

To publish events and trigger subscriptions, we can use the `ITopicEventSender`. The `ITopicEventSender` is an abstraction for the registered event publishing provider. Using this abstraction allows us to seamlessly switch between subscription providers, when necessary.

Most of the time we will be publishing events for successful mutations. Therefore we can simply inject the `ITopicEventSender` into our mutations like we would with every other `Service`. Of course we can not only publish events from mutations, but everywhere we have access to the `ITopicEventSender` through the DI Container.

```csharp
public class Mutation
{
    public async Book AddBook(Book book, [Service] ITopicEventSender sender)
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

public async Book AddBook(Book book, [Service] ITopicEventSender sender)
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
    public Book BookPublished([Topic] string author, [EventMessage] Book book)
        => book;
}

public async Book PublishBook(Book book, [Service] ITopicEventSender sender)
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
    [SubscribeAndResolve]
    public ValueTask<ISourceStream<Book>> BookPublished(string author,
        [Service] ITopicEventReceiver receiver)
    {
        var topic = $"{author}_PublishedBook";

        return receiver.SubscribeAsync<string, Book>(topic);
    }
}

public async Book PublishBook(Book book, [Service] ITopicEventSender sender)
{
    await sender.SendAsync($"{book.Author}_PublishedBook", book);

    // Omitted code for brevity
}
```

If we do not want to mix the subscription logic with our resolver, we can also use the `With` argument on the `Subscribe` attribute to specify a seperate method that handles the event subscription.

```csharp
public class Subscription
{
    public ValueTask<ISourceStream<Book>> SubscribeToBooks(
        [Service] ITopicEventReceiver receiver)
        => receiver.SubscribeAsync<string, Book>("ExampleTopic");

    [Subscribe(With = nameof(SubscribeToBooks))]
    public Book BookAdded([EventMessage] Book book)
        => book;
}
```
