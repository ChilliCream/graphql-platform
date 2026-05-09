---
title: "Write resolvers"
description: "Add resolver methods to the tutorial server, return library data, use parent values, inject a service, and verify the response in Nitro."
---

In the previous chapter, you defined your schema and added a placeholder `GetBooks()` resolver. The schema now describes `Book`, `Author`, and the root query field, but the resolver still returns only placeholder data. In this chapter, you will update the resolver to provide real library data.

By the end of this chapter, you will:

- Replace the placeholder `books` query resolver
- Return consistent library data
- See how selected fields shape the JSON response
- Add a field resolver that uses its parent `Book`
- Move sample data into a service
- Inject that service into a resolver method

Resolvers are the C# methods and properties that Hot Chocolate uses to produce field values. When a client requests a field, Hot Chocolate resolves it. Public properties can resolve scalar fields, while methods can resolve root query fields, computed fields, or fields that require application logic.

If you want a deeper understanding after this chapter, see [Resolver and data middleware model](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/resolver-and-data-middleware-model/). For now, continue with this chapter as you build the tutorial project.

# Start from your existing schema

Open the `LibraryServer` project you created earlier.

Your project should contain these files:

```text
LibraryServer/
├── Program.cs
└── Types/
    ├── Author.cs
    ├── Book.cs
    └── Query.cs
```

Your code may differ slightly, but you should have a root `Query` type, the library object types, and a placeholder `books` field.

Start the server if it is not already running:

```bash
dotnet run
```

Open Nitro at your local `/graphql` endpoint. For example:

```text
http://localhost:5095/graphql
```

Use the port shown in your terminal.

In Nitro, open the schema view or explorer and check for these:

- `Query` type exists
- `Book` type exists
- `Author` type exists
- The root `books` field returns placeholder data

If your schema does not match, revisit [Define your first types](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/02-define-your-first-types/) or compare your project with [Source code and checkpoints](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/00-source-code-and-checkpoints/).

# Update the root query resolver

GraphQL queries begin at the root `Query` type. Each field on `Query` is an entry point for client read operations.

Update `Types/Query.cs` so the `books` field returns richer library data:

```csharp
namespace LibraryServer.Types;

[QueryType]
public static partial class Query
{
    [GraphQLDescription("Gets the books currently available in the library catalog.")]
    public static IReadOnlyList<Book> GetBooks()
        =>
        [
            new Book
            {
                Id = "book-1",
                Title = "The Left Hand of Darkness",
                Author = new Author
                {
                    Id = "author-1",
                    Name = "Ursula K. Le Guin"
                }
            },
            new Book
            {
                Id = "book-2",
                Title = "Kindred",
                Author = new Author
                {
                    Id = "author-2",
                    Name = "Octavia E. Butler"
                }
            }
        ];
}
```

This method is a resolver. Hot Chocolate uses the `[QueryType]` attribute to add the method to the GraphQL `Query` type and calls it when a client selects the field.

The generated GraphQL field is named `books`. Hot Chocolate removes the `Get` prefix when creating the field name.

If your `Book` and `Author` files do not yet have `Id`, `Title`, `Author`, and `Name` properties, update them now.

`Types/Book.cs`:

```csharp
namespace LibraryServer.Types;

[GraphQLDescription("A book that can be browsed in the library catalog.")]
public sealed class Book
{
    [GraphQLDescription("The stable identifier for the book.")]
    public required string Id { get; init; }

    [GraphQLDescription("The title printed for readers.")]
    public required string Title { get; init; }

    [GraphQLDescription("The person who wrote the book.")]
    public required Author Author { get; init; }
}
```

`Types/Author.cs`:

```csharp
namespace LibraryServer.Types;

[GraphQLDescription("A person who wrote one or more books in the library catalog.")]
public sealed class Author
{
    [GraphQLDescription("The stable identifier for the author.")]
    public required string Id { get; init; }

    [GraphQLDescription("The author's display name.")]
    public required string Name { get; init; }
}
```

Build the project:

```bash
dotnet build
```

Expected build signal:

```text
Build succeeded.
```

Restart the server if it was already running. Refresh Nitro's schema information, then check that `Query` has a `books` field.

Expected schema shape:

```graphql
type Query {
  books: [Book!]!
}
```

Your schema can contain descriptions and other built-in fields. The checkpoint is that `books` appears on `Query` and returns a list of `Book`.

# Return domain data from the resolver

Run this operation in Nitro:

```graphql
query GetBooks {
  books {
    id
    title
    author {
      id
      name
    }
  }
}
```

Expected response:

```json
{
  "data": {
    "books": [
      {
        "id": "book-1",
        "title": "The Left Hand of Darkness",
        "author": {
          "id": "author-1",
          "name": "Ursula K. Le Guin"
        }
      },
      {
        "id": "book-2",
        "title": "Kindred",
        "author": {
          "id": "author-2",
          "name": "Octavia E. Butler"
        }
      }
    ]
  }
}
```

The response follows the operation:

- The operation selects `books`, so the response has `data.books`.
- The operation selects `id`, `title`, and `author`, so each book in the response has those fields.
- The operation selects `id` and `name` under `author`, so each author object has those fields.

Change the operation so it asks for fewer fields:

```graphql
query GetBookTitles {
  books {
    title
  }
}
```

Expected response:

```json
{
  "data": {
    "books": [
      {
        "title": "The Left Hand of Darkness"
      },
      {
        "title": "Kindred"
      }
    ]
  }
}
```

You did not change the resolver method. You changed the selection set. GraphQL returns the fields the client selected.

The resolver in this section is synchronous because it returns fixed in-memory data. Later chapters use asynchronous resolvers when the server waits for database, network, or other I/O work. Hot Chocolate supports both shapes.

# How nested fields are resolved

Let’s walk through the first operation again:

```graphql
query GetBooks {
  books {
    id
    title
    author {
      id
      name
    }
  }
}
```

Hot Chocolate resolves the selected fields as a tree:

```text
Query.books       -> calls Query.GetBooks()
  Book.id         -> reads Book.Id
  Book.title      -> reads Book.Title
  Book.author     -> reads Book.Author
    Author.id     -> reads Author.Id
    Author.name   -> reads Author.Name
```

`Query.GetBooks()` starts the operation by returning a list of `Book` objects. Hot Chocolate then resolves the fields selected on each `Book`.

The `id`, `title`, `author`, and `name` fields come from public properties. A public property with a getter can act as the resolver for that field. You write a resolver method when a field needs logic, a lookup, or a value that is not stored directly as a property.

Checkpoint:

- You can identify `Query.GetBooks()` as the method that produces the root list.
- You can point to `Book.Title` as the property that produces `title`.
- You can explain why `author.name` appears only when the operation selects it.

# Add a field that uses the parent value

Now add a computed field to `Book`. The field will return a citation string built from the current book.

Create `Types/BookResolvers.cs`:

```csharp
namespace LibraryServer.Types;

[ExtendObjectType<Book>]
public static partial class BookResolvers
{
    public static string GetCitation([Parent] Book book)
        => $"{book.Title} by {book.Author.Name}";
}
```

This adds a `citation` field to the `Book` object type.

The `[Parent]` parameter gives the resolver the `Book` object currently being resolved. In this case, Hot Chocolate calls `GetCitation` once for each `Book` selected under `books`. The `book` parameter is the object returned by the parent `books` resolver.

Build and restart the server:

```bash
dotnet build
dotnet run
```

Refresh Nitro's schema information. Check that `citation` appears on `Book`, not on `Query`.

Run this operation:

```graphql
query GetBooksWithCitations {
  books {
    title
    citation
  }
}
```

Expected response:

```json
{
  "data": {
    "books": [
      {
        "title": "The Left Hand of Darkness",
        "citation": "The Left Hand of Darkness by Ursula K. Le Guin"
      },
      {
        "title": "Kindred",
        "citation": "Kindred by Octavia E. Butler"
      }
    ]
  }
}
```

Checkpoint:

- `citation` is selected inside `books`.
- Each `citation` value is calculated from the current parent `Book`.
- `BookResolvers.GetCitation` is the resolver method for the `citation` field.

# Move data access into a service

The `GetBooks` resolver currently creates data itself. That is useful for the first run, but resolvers should not grow into application services.

Move the sample data into a service. The resolver will stay focused on the GraphQL field, and the service will own the data-producing work.

Create a `Services` folder. Add `Services/BookCatalog.cs`:

```csharp
using LibraryServer.Types;

namespace LibraryServer.Services;

public sealed class BookCatalog
{
    private static readonly IReadOnlyList<Book> s_books =
    [
        new Book
        {
            Id = "book-1",
            Title = "The Left Hand of Darkness",
            Author = new Author
            {
                Id = "author-1",
                Name = "Ursula K. Le Guin"
            }
        },
        new Book
        {
            Id = "book-2",
            Title = "Kindred",
            Author = new Author
            {
                Id = "author-2",
                Name = "Octavia E. Butler"
            }
        }
    ];

    public IReadOnlyList<Book> GetBooks()
        => s_books;
}
```

Register the service in `Program.cs` before the app is built:

```csharp
using LibraryServer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<BookCatalog>();

builder.AddGraphQL().AddTypes();

var app = builder.Build();

app.MapGraphQL();

app.RunWithGraphQLCommands(args);
```

`AddSingleton` is fine for this tutorial data because the service returns a fixed in-memory list. Later chapters change the data access boundary when the project connects to real data.

Update `Types/Query.cs` so the resolver receives the service as a parameter:

```csharp
using LibraryServer.Services;

namespace LibraryServer.Types;

[QueryType]
public static partial class Query
{
    [GraphQLDescription("Gets the books currently available in the library catalog.")]
    public static IReadOnlyList<Book> GetBooks(BookCatalog catalog)
        => catalog.GetBooks();
}
```

Hot Chocolate sees that `BookCatalog` is registered in ASP.NET Core dependency injection and provides it when the resolver runs. You do not need a constructor on the `Query` type. For resolvers, method-parameter injection keeps the dependency close to the field that uses it.

Build and restart the server:

```bash
dotnet build
dotnet run
```

Run the same operation as before:

```graphql
query GetBooksWithCitations {
  books {
    id
    title
    citation
    author {
      name
    }
  }
}
```

Expected response:

```json
{
  "data": {
    "books": [
      {
        "id": "book-1",
        "title": "The Left Hand of Darkness",
        "citation": "The Left Hand of Darkness by Ursula K. Le Guin",
        "author": {
          "name": "Ursula K. Le Guin"
        }
      },
      {
        "id": "book-2",
        "title": "Kindred",
        "citation": "Kindred by Octavia E. Butler",
        "author": {
          "name": "Octavia E. Butler"
        }
      }
    ]
  }
}
```

The response should stay the same. The implementation changed from inline data to service-backed data.

Checkpoint:

- `Program.cs` registers `BookCatalog`.
- `Query.GetBooks` accepts `BookCatalog catalog`.
- The query result still returns the two books.
- `BookResolvers.GetCitation` still uses the parent `Book`.

For more service injection details, read [Dependency Injection](/docs/hotchocolate/v16/resolvers-and-data/dependency-injection/).

# Use async when the resolver waits for work

Keep the final tutorial code synchronous for this chapter. The data is already in memory, so there is nothing to wait for.

When a resolver calls an asynchronous service later, return `Task<T>` or mark the resolver `async`. The GraphQL field shape does not change.

For example, an asynchronous version of the same resolver shape would look like this:

```csharp
public static async Task<IReadOnlyList<Book>> GetBooksAsync(
    BookCatalog catalog,
    CancellationToken cancellationToken)
{
    return await catalog.GetBooksAsync(cancellationToken);
}
```

In that shape:

- `GetBooksAsync` still becomes the `books` field.
- `BookCatalog` still comes from dependency injection.
- `CancellationToken` is provided by Hot Chocolate.
- The client still queries `books`.

Do not add this example to the project unless your service has an asynchronous `GetBooksAsync` method. You will use this pattern when later chapters introduce data access that waits for external work.

# Checkpoint: run the complete query

Run this final operation in Nitro:

```graphql
query ResolverCheckpoint {
  books {
    id
    title
    citation
    author {
      id
      name
    }
  }
}
```

Expected response:

```json
{
  "data": {
    "books": [
      {
        "id": "book-1",
        "title": "The Left Hand of Darkness",
        "citation": "The Left Hand of Darkness by Ursula K. Le Guin",
        "author": {
          "id": "author-1",
          "name": "Ursula K. Le Guin"
        }
      },
      {
        "id": "book-2",
        "title": "Kindred",
        "citation": "Kindred by Octavia E. Butler",
        "author": {
          "id": "author-2",
          "name": "Octavia E. Butler"
        }
      }
    ]
  }
}
```

You are ready for the next chapter when all of these are true:

- `dotnet build` reports `Build succeeded`.
- Nitro shows `Query.books`.
- Nitro shows `Book.citation`.
- The checkpoint operation returns a top-level `data` object with no `errors`.
- `books` returns two books.
- Each book includes `id`, `title`, `citation`, and `author` when selected.

You should also be able to answer these questions:

| Question | Answer |
| --- | --- |
| Which resolver starts the operation? | `Query.GetBooks` starts the operation by resolving `Query.books`. |
| Which fields come from object properties? | `Book.Id`, `Book.Title`, `Book.Author`, `Author.Id`, and `Author.Name`. |
| Which field uses the parent value? | `Book.citation`, resolved by `BookResolvers.GetCitation([Parent] Book book)`. |
| Which resolver receives a service from dependency injection? | `Query.GetBooks(BookCatalog catalog)`. |

If the root field or `citation` field does not appear, rebuild the project, restart the server, and refresh Nitro's schema information. If the response omits a field, confirm that the field is selected in the operation. If service injection fails, confirm that `BookCatalog` is registered in `Program.cs` before `builder.Build()` and that the resolver parameter type is also `BookCatalog`.

# What to learn next

Continue to [Add arguments and filters](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/04-add-arguments-and-filters/) when the checkpoint query matches the expected response.

Use these pages when you want more background:

- [Resolver and data middleware model](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/resolver-and-data-middleware-model/) for the resolver tree and where data middleware fits.
- [How GraphQL executes](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/how-graphql-executes/) for a broader request-to-response explanation.
- [Operations](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/operations/) for selection sets, operation names, variables, and response shape.
- [Resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers/) for resolver syntax beyond this tutorial path.
