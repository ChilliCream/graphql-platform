---
title: "Subscriptions"
---

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
<Implementation>

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
            .BindComplexType<Subscription>();
    }

    // Omitted code for brevity
}
```

</Schema>
</ExampleTabs>

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

The Redis subscription provider enables us to run multiple instances of our Hot Chocolate GraphQL server and handle subscription events reliably.

In order to use the Redis provider we have to add the `HotChocolate.Subscriptions.Redis` package.

<PackageInstallation packageName="HotChocolate.Subscriptions.Redis" />

After we have added the package we can setup the Redis subscription provider.

```csharp
services.AddRedisSubscriptions((sp) =>
    ConnectionMultiplexer.Connect("host:port"));
```

Our Redis subscription provider uses the [StackExchange.Redis](https://github.com/StackExchange/StackExchange.Redis) Redis client underneath.

# Publishing Events

To publish events and trigger subscriptions, we can use the `ITopicEventSender`. The `ITopicEventSender` is an abstraction for the registered event publishing provider. Using this abstraction allows us to seamlessly switch between subscription providers, when necessary.

Most of the time we will be publishing events for successful mutations. Therefor we can simply inject the `ITopicEventSender` into our mutations like we would with every other `Service`. Of course we can not only publish events from mutations, but everywhere we have access to the `ITopicEventSender` through the DI Container.

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
        string topic = $"{author}_PublishedBook";
        ISourceStream<Book> stream =
            receiver.SubscribeAsync<string, Book>(topic);

        return stream;
    }
}

public async Book PublishBook(Book book, [Service] ITopicEventSender sender)
{
    await sender.SendAsync($"{book.Author}_PublishedBook", book);

    // Omitted code for brevity
}
```

If we do not want to mix the subscription logic with our resolver, we can also use the `With` argument on the `Subscribe` attribute to specify a separate method that handles the event subscription.

```csharp
public class Subscription
{
    public ValueTask<ISourceStream<Book>> SubscribeToBooks(
        [Service] ITopicEventReceiver receiver)
        => receiver.SubscribeAsync<string, Book>("ExampleTopic");

    [Subscribe(With = nameof(SubscribeToBooks))]
    public ValueTask<ISourceStream<Book>> BookAdded([EventMessage] Book book)
        => book;
}
```
