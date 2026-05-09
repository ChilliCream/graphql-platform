---
title: "Add subscriptions"
description: "Add one realtime subscription to the tutorial server, publish from the addBook mutation, and watch the event arrive in Nitro."
---

In the previous chapter, you added a mutation to create a book. Your server can now both read and write data.

This chapter will make those writes observable in real time. A subscription is a long-lived GraphQL operation: instead of returning a single response and closing, it stays open and delivers new results whenever the server publishes a matching event.

By the end of this chapter, you will:

- Register a local subscription provider
- Enable WebSockets for the local GraphQL endpoint
- Add an `onBookAdded` subscription field
- Publish an event after `addBook` succeeds
- Watch the event arrive in Nitro while the subscription remains active
- Understand the local realtime boundary

This tutorial uses the in-memory subscription provider, which requires no external infrastructure. It is ideal for learning on a single local server. For production scenarios and scaling out, see [Subscriptions](/docs/hotchocolate/v16/building-a-schema/subscriptions/) and [Subscriptions and realtime](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/subscriptions-and-realtime/) after completing this chapter.

# Add realtime to the next write

The workflow you will build consists of four parts:

| Part        | Role in this chapter                                                      |
|-------------|---------------------------------------------------------------------------|
| Publisher   | The `addBook` mutation publishes an event after saving a new book.         |
| Topic       | A topic name connects publishers and subscribers.                          |
| Subscriber  | A client runs the `onBookAdded` subscription and stays connected.          |
| Payload     | The published `Book` is the data selected by the subscription operation.   |

To test this, you will use two Nitro tabs or panels:

1. In one tab, run a subscription and let it wait for events.
2. In another tab, run the `addBook` mutation.
3. The subscription tab will receive a result for the newly created book.

The mutation continues to return its normal payload. Publishing an event provides a second path for active subscribers.

# Enable local subscription support

Open your project folder containing the `.csproj` file.

If `AddInMemorySubscriptions` is not available, add the in-memory subscription package:

```bash
dotnet add package HotChocolate.Subscriptions.InMemory
```

Next, open `Program.cs`.

Locate the GraphQL registration, which should already include `AddFiltering()`, mutation conventions, and `AddTypes()`. Add the in-memory subscription provider to this builder chain:

```csharp
builder
    .AddGraphQL()
    .AddFiltering()
    .AddMutationConventions(applyToAllMutations: true)
    .AddInMemorySubscriptions()
    .AddTypes();
```

If your `Program.cs` has these calls in a different order, keep your configuration and insert `.AddInMemorySubscriptions()` before `.AddTypes()`.

The in-memory provider acts as a local message bus between the mutation and the subscription. It does not persist data and events remain within the running server process.

Now, enable WebSockets before mapping the GraphQL endpoint:

```csharp
app.UseWebSockets();

app.MapGraphQL();
```

Near the end of `Program.cs`, your pipeline should look like this:

```csharp
app.UseWebSockets();

app.MapGraphQL();

app.RunWithGraphQLCommands(args);
```

Build the project:

```bash
dotnet build
```

You should see:

```text
Build succeeded.
```

If the build fails due to a missing `AddInMemorySubscriptions`, check that the package version matches your other Hot Chocolate packages. All package versions should be aligned.

# Add a subscription field for the event

Create a new file at `Types/BookSubscriptions.cs`:

```csharp
namespace LibraryServer.Types;

[SubscriptionType]
public static partial class BookSubscriptions
{
    [GraphQLDescription("Subscribes to books when they are added to the library catalog.")]
    [Subscribe]
    public static Book OnBookAdded([EventMessage] Book book)
        => book;
}
```

The `[SubscriptionType]` attribute marks this class as contributing fields to the root `Subscription` type. Declaring the class as `partial` allows the source generator to register it through `AddTypes()`.

The method name becomes the subscription field name in the GraphQL schema:

```graphql
type Subscription {
  onBookAdded: Book!
}
```

The `[Subscribe]` attribute connects this field to the subscription provider, and `[EventMessage]` marks the value received from the topic.

The method returns the event payload, which in this case is the `Book` saved by the mutation.

Restart the server if it is running:

```bash
dotnet run
```

Open Nitro at your local GraphQL endpoint, for example:

```text
http://localhost:5095/graphql
```

Refresh Nitro's schema information and locate the root `Subscription` type. It should now include `onBookAdded`.

If the `Subscription` type does not appear, rebuild and restart the server. Make sure `BookSubscriptions` is in your project, has `[SubscriptionType]`, is `partial`, and is in the same assembly as your other generated types.

# Publish the event after the mutation succeeds

Now, connect the existing `addBook` mutation to the subscription.

Open the mutation file from the previous chapter, likely `Types/Mutation.cs` or `Types/BookMutations.cs`.

Add this using directive if it is not already present:

```csharp
using HotChocolate.Subscriptions;
```

Add `ITopicEventSender sender` to the `AddBookAsync` resolver parameters. Then, after saving to the database, publish the event:

```csharp
using HotChocolate.Subscriptions;
using LibraryServer.Data;
using Microsoft.EntityFrameworkCore;

namespace LibraryServer.Types;

[MutationType]
public static partial class Mutation
{
    [GraphQLDescription("Adds a book to the library catalog.")]
    [Error(typeof(BookTitleAlreadyExistsException))]
    public static async Task<Book> AddBookAsync(
        string title,
        int authorId,
        LibraryDbContext db,
        ITopicEventSender sender,
        CancellationToken cancellationToken)
    {
        var normalizedTitle = title.Trim();

        var titleExists = await db.Books
            .AnyAsync(b => b.Title == normalizedTitle, cancellationToken);

        if (titleExists)
        {
            throw new BookTitleAlreadyExistsException(normalizedTitle);
        }

        var book = new Book
        {
            Title = normalizedTitle,
            AuthorId = authorId
        };

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

The key additions are the `ITopicEventSender` parameter and the `SendAsync` call after `SaveChangesAsync`. The duplicate-title check and `[Error]` attribute remain from the previous chapter.

The first argument to `SendAsync` is the topic name. The subscription field uses the default topic for the method, so `nameof(BookSubscriptions.OnBookAdded)` keeps the publisher and subscriber aligned at compile time.

Publish the event only after a successful save. Subscribers should not receive a book event if the mutation did not commit.

Build the project again:

```bash
dotnet build
```

You should see:

```text
Build succeeded.
```

The mutation continues to work even if no subscription is running. The sender publishes to active subscribers, but does not affect the mutation response.

# Subscribe in Nitro and trigger the event

Start the server:

```bash
dotnet run
```

Open Nitro at your local `/graphql` endpoint.

Create a subscription operation:

```graphql
subscription WatchNewBooks {
  onBookAdded {
    id
    title
    author {
      id
      name
    }
  }
}
```

Run the subscription and leave it active. Nitro should indicate that the operation is running and waiting for results.

Open a second Nitro tab or panel. Run the mutation from the previous chapter with a unique title:

```graphql
mutation AddBook($input: AddBookInput!) {
  addBook(input: $input) {
    book {
      id
      title
      author {
        id
        name
      }
    }
    errors {
      __typename
      ... on Error {
        message
      }
    }
  }
}
```

Use variables like these:

```json
{
  "input": {
    "title": "Always Coming Home",
    "authorId": 1
  }
}
```

If you have already created a book with that title, change the title before running the mutation.

The mutation response should look like this:

```json
{
  "data": {
    "addBook": {
      "book": {
        "id": "6",
        "title": "Always Coming Home",
        "author": {
          "id": "1",
          "name": "Ursula K. Le Guin"
        }
      },
      "errors": null
    }
  }
}
```

Your `id` value may differ.

Return to the subscription tab. It should receive a result without needing to refresh:

```json
{
  "data": {
    "onBookAdded": {
      "id": "6",
      "title": "Always Coming Home",
      "author": {
        "id": "1",
        "name": "Ursula K. Le Guin"
      }
    }
  }
}
```

The subscription result shape matches the selection set. If you select only `id` and `title`, the event result will include only those fields.

# Understand the realtime boundary you built

The working flow is:

```text
Client starts subscription -> server keeps the operation open
Mutation saves a book -> mutation publishes to a topic
Provider fans out the event -> matching subscribers receive it
Subscription resolver returns the payload -> client receives a GraphQL result
```

WebSockets provide the transport for the running subscription operation. Nitro connects to the same `/graphql` endpoint, but the subscription remains open instead of closing after a single response.

The subscription provider delivers events between publishers and subscribers. The in-memory provider does this within a single running server process.

This boundary is important:

- If no subscriber is connected, the mutation still succeeds.
- If the server restarts, active subscription connections end.
- If you run multiple server instances, in-memory events are not shared between them.
- If you host behind a proxy, the proxy must allow WebSocket connections.

For more on provider selection, Redis, NATS, Postgres, SSE, reconnect behavior, authentication, and proxy setup, continue with [Subscriptions](/docs/hotchocolate/v16/building-a-schema/subscriptions/) and [Subscriptions and realtime](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/subscriptions-and-realtime/).

# Checkpoint: your server now streams one event

You changed:

| File                  | Change                                                        |
|-----------------------|---------------------------------------------------------------|
| `Program.cs`          | Registered the in-memory subscription provider and enabled WebSockets. |
| `Types/BookSubscriptions.cs` | Added the `onBookAdded` subscription field.                  |
| Mutation file         | Published an event after `addBook` saves a book.              |

You are ready for the next chapter when:

- `dotnet build` reports `Build succeeded.`
- Nitro shows a `Subscription` root with `onBookAdded`
- A running `onBookAdded` subscription receives an event after `addBook` succeeds
- `addBook` still returns its normal mutation payload
- `addBook` still succeeds when no subscription is running

If your project does not match these checkpoints, compare your files with the tutorial checkpoint guidance in [Source code and checkpoints](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/00-source-code-and-checkpoints/) or see [Stuck?](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/stuck/).

In the next chapter, you will test the server to make schema and execution changes safer.
