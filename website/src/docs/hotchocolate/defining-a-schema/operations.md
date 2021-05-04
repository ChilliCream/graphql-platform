---
title: "GraphQL Operations"
---

> We are still working on the documentation for Hot Chocolate 11.1 so help us by finding typos, missing things or write some additional docs with us.

import { ExampleTabs } from "../../../components/mdx/example-tabs"

In GraphQL, there are three root types from which only the Query type has to be defined. Root types provide the entry points that lets us fetch data, mutate data, or subscribe to events. Root types themselves are object types and are commonly referred to as operations.

# Query

The query type is how we can read data. It is described as a way to access read-only data in a side-effect free way. This means that the GraphQL engine is allowed to parallelize data fetching.

```graphql
query {
  book(id: 1) {
    title
    author
  }
}
```

## Defining a query

A query type can be represented like the following:

<ExampleTabs>
<ExampleTabs.Annotation>

```csharp
public class Query
{
    public Book GetBook() => new Book { Title  = "C# in depth", Author = "Jon Skeet" };
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddQueryType<Query>();
    }

    // Omitted code for brevity
}
```

</ExampleTabs.Annotation>
<ExampleTabs.Code>

```csharp
public class Query
{
    public Book GetBook() => new Book { Title  = "C# in depth", Author = "Jon Skeet" };
}

public class QueryType : ObjectType<Query>
{
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor
            .Field(f => f.GetBook())
            .Type<BookType>();
    }
}

public class BookType : ObjectType<Book>
{
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor
            .Field(f => f.Title)
            .Type<StringType>();

        descriptor
            .Field(f => f.Author)
            .Type<StringType>();
    }
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddQueryType<QueryType>();
    }

    // Omitted code for brevity
}
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

```csharp
public class Query
{
    public Book GetBook() => new Book { Title  = "C# in depth", Author = "Jon Skeet" };
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddDocumentFromString(@"
                type Query {
                  book: Book
                }

                type Book {
                  title: String
                  author: String
                }
            ")
            .BindComplexType<Query>()
            .BindComplexType<Book>();
    }

    // Omitted code for brevity
}
```

</ExampleTabs.Schema>
</ExampleTabs>

# Mutation

The mutation type in GraphQL is used to mutate/change data. This means that when we are doing mutations, we are causing side-effects to the system.

GraphQL defines mutations as top-level fields on the mutation type. Meaning only the fields on the mutation root type itself are mutations. Everything that is returned from a mutation field represents the changed state of the server.

```graphql
{
  mutation {
    # changeBookTitle is a mutation and is allowed to cause side-effects.
    changeBookTitle(input: { id: 1, title: "C# in depth" }) {
      # everything in this selection set and below is a query.
      # We essentially allow the user to query the effect that the mutation had
      # on our system.
      # In this case we are querying the changed book.
      book {
        title
      }
    }
  }
}
```

In one GraphQL request we can execute multiple mutations. Each of these mutations are executed serially one by one whereas their child selection sets are executed possibly in parallel since only the top-level mutations fields are allowed to have side-effects in GraphQL.

```graphql
{
  mutation {
    addBook(input: { title: "C# in depth", author: "Jon Skeet" }) {
      book {
        title
      }
    }
    publishBook(input: { id: 1 }) {
      book {
        isPublished
      }
    }
  }
}
```

## Defining a Mutation

A mutation type can be represented like the following:

<ExampleTabs>
<ExampleTabs.Annotation>

```csharp
public class Mutation
{
    public async Task<Book> AddBook(Book book)
    {
        // Omitted code for brevity
    }
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            // ...
            .AddMutationType<Mutation>();
    }

    // Omitted code for brevity
}
```

</ExampleTabs.Annotation>
<ExampleTabs.Code>

```csharp
public class Mutation
{
    public async Task<Book> AddBook(Book book)
    {
        // Omitted code for brevity
    }
}

public class MutationType : ObjectType<Mutation>
{
    protected override void Configure(IObjectTypeDescriptor<Mutation> descriptor)
    {
        descriptor.Field(f => f.AddBook(default));
    }
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            // ...
            .AddMutationType<MutationType>();
    }

    // Omitted code for brevity
}
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

```csharp
public class Mutation
{
    public async Task<Book> AddBook(Book book)
    {
        // Omitted code for brevity
    }
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddDocumentFromString(@"
                # ...

                type Mutation {
                  addBook(input: BookInput): Book
                }

                type BookInput {
                  title: String
                  author: String
                }

                type Book {
                  title: String
                  author: String
                }
            ")
            // ...
            .BindComplexType<Mutation>();
    }

    // Omitted code for brevity
}
```

</ExampleTabs.Schema>
</ExampleTabs>

## Mutation Transactions

With multiple mutations executed serially in one request it sometimes would be great to put these into a transactions scope that we can control.

Hot Chocolate provides for this the `ITransactionScopeHandler` which is used by the operation execution middleware to create transaction scopes for mutation requests.

Hot Chocolate provides a default implementation based on the `System.Transactions.TransactionScope` which works with Microsoft ADO.NET data provider and hence can be used in combination with Entity Framework.

The default transaction scope handler can be added like the following:

```csharp
services
    .AddGraphQLServer()
    // ...
    .AddDefaultTransactionScopeHandler();
```

This is how the default implementation looks like:

```csharp
/// <summary>
/// Represents the default mutation transaction scope handler implementation.
/// </summary>
public class DefaultTransactionScopeHandler : ITransactionScopeHandler
{
    /// <summary>
    /// Creates a new transaction scope for the current
    /// request represented by the <see cref="IRequestContext"/>.
    /// </summary>
    /// <param name="context">
    /// The GraphQL request context.
    /// </param>
    /// <returns>
    /// Returns a new <see cref="ITransactionScope"/>.
    /// </returns>
    public virtual ITransactionScope Create(IRequestContext context)
    {
        return new DefaultTransactionScope(
            context,
            new TransactionScope(
                TransactionScopeOption.Required,
                new TransactionOptions
                {
                    IsolationLevel = IsolationLevel.ReadCommitted
                }));
    }
}
```

If we implement a custom transaction scope handler or if we choose to extend upon the default transaction scope handler, we can add it like the following.

```csharp
services
    .AddGraphQLServer()
    // ...
    .AddTransactionScopeHandler<CustomTransactionScopeHandler>();
```

# Subscription

The subscription type in GraphQL is used to add real-time capabilities to our applications. Clients can subscribe to events and receive the event data in real-time, as soon as the server publishes it.

Subscribing to an event is like writing a standard query. The only difference is the operation keyword and that we are only allowed to have one root field.

```graphql
subscription {
  onBookAdded(author: "Jon Skeet") {
    title
  }
}
```

HotChocolate implements Subscriptions via WebSockets and uses the pub/sub approach of [Apollo](https://www.apollographql.com/docs/apollo-server/data/subscriptions/#the-pubsub-class) for triggering subscriptions.

## Defining a Subscription

A subscription type can be represented like the following:

<ExampleTabs>
<ExampleTabs.Annotation>

```csharp
public class Subscription
{
    [Subscribe]
    public Book OnBookAdded([EventMessage] Book book) => book;
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            // ...
            .AddSubscriptionType<Subscription>();
    }

    // Omitted code for brevity
}
```

</ExampleTabs.Annotation>
<ExampleTabs.Code>

```csharp
public class SubscriptionType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("onBookAdded")
            .Type<BookType>()
            .Resolve(context => context.GetEventMessage<Book>())
            .Subscribe(async context =>
            {
                var receiver = context.Service<ITopicEventReceiver>();

                ISourceStream stream =
                    await receiver.SubscribeAsync<string, Book>("OnBookAdded");

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
            // ...
            .AddSubscriptionType<SubscriptionType>();
    }

    // Omitted code for brevity
}
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

```csharp
public class Subscription
{
    [Subscribe]
    public Book OnBookAdded([EventMessage] Book book) => book;
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddDocumentFromString(@"
                # ...

                type Subscription {
                  onBookAdded: Book
                }

                type Book {
                  title: String
                  author: String
                }
            ")
            // ...
            .BindComplexType<Subscription>();
    }

    // Omitted code for brevity
}
```

</ExampleTabs.Schema>
</ExampleTabs>

## Transport

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

### In-Memory Provider

The In-Memory subscription provider does not need any configuration and is easily setup:

```csharp
services.AddInMemorySubscriptions();
```

### Redis Provider

The Redis subscription provider enables us to run multiple instances of our HotChocolate GraphQL server and handle subscription events reliably.

In order to use the Redis provider add the following package: `HotChocolate.Subscriptions.Redis`

After we have added the package we can setup the Redis subscription provider:

```csharp
services.AddRedisSubscriptions((sp) =>
    ConnectionMultiplexer.Connect("host:port"));
```

Our Redis subscription provider uses the [StackExchange.Redis](https://github.com/StackExchange/StackExchange.Redis) Redis client underneath.

## Publishing Events

To publish events and trigger subscriptions, we can use the `ITopicEventSender`. The `ITopicEventSender` is an abstraction for the registered event publishing provider. Using this abstraction allows us to seamlessly switch between subscription providers, when necessary.

Most of the time we will be publishing events for successful mutations. Therefor we can simply inject the `ITopicEventSender` into our mutations like we would with every other `Service`. Of course we can not only publish events from mutations, but everywhere we have access to the `ITopicEventSender` through the DI Container.

```csharp
public class Mutation
{
    public async Book AddBook(Book book, [Service] ITopicEventSender sender)
    {
        await sender.SendAsync("OnBookAdded", book);

        // Omitted code for brevity
    }
}
```

In the example the `"OnBookAdded"` is the topic we want to publish to, and `book` is our payload. Even though we have used a string as the topic, we do not have to. Any other type works just fine.

But where is the connection between `"OnBookAdded"` as a topic and the subscription type? Per default HotChocolate will try to map the topic to a field of the subscription type. If we want to make this binding less error-prone, we could do the following:

```csharp
await sender.SendAsync(nameof(Subscription.OnBookAdded), book);
```

If we do not want to use the method name, we could use the `Topic` attribute.

```csharp
public class Subscription
{
    [Subscribe]
    [Topic("ExampleTopic")]
    public Book OnBookAdded([EventMessage] Book book) => book;
}

public async Book AddBook(Book book, [Service] ITopicEventSender sender)
{
    await sender.SendAsync("ExampleTopic", book);

    // Omitted code for brevity
}
```

We can even use the `Topic` attribute on dynamic arguments of the subscription field.

```csharp
public class Subscription
{
    [Subscribe]
    public Book OnBookAdded([Topic] string author, [EventMessage] Book book)
        => book;
}

public async Book AddBook(Book book, [Service] ITopicEventSender sender)
{
    await sender.SendAsync(book.Author, book);

    // Omitted code for brevity
}
```

We can also use the `ITopicEventReceiver` to work with more complex topics.

```csharp
public class Subscription
{
    [SubscribeAndResolve]
    public ValueTask<ISourceStream<Book>> OnBookAdded(string author,
        [Service] ITopicEventReceiver receiver)
    {
        string topic = $"{author}_AddedBook";
        Book book = receiver.SubscribeAsync<string, Book>(topic);

        return book;
    }
}

public async Book AddBook(Book book, [Service] ITopicEventSender sender)
{
    await sender.SendAsync($"{book.Author}_AddedBook", book);

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
    public ValueTask<ISourceStream<Book>> OnBookAdded([EventMessage] Book book)
        => book;
}
```
