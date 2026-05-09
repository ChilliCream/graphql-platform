---
title: "Fix N+1 with DataLoader"
description: "Find the first repeated relationship lookup in the tutorial server, add a source-generated DataLoader, use it from a field resolver, and verify batched author loading."
---

In the previous chapter, you connected your server to a real data source. The GraphQL schema remained the same: clients can still query books and their nested authors.

Now, you will examine how many data calls are made to produce that nested shape.

By the end of this chapter, you will:

- Identify the N+1 query pattern in a nested `books { author { ... } }` query
- Add temporary logging to observe the server's data access behavior
- Create a source-generated DataLoader for authors
- Update the author field resolver to use the DataLoader
- Confirm that the response remains unchanged while author loading is batched

DataLoader is Hot Chocolate's approach for batching key-based reads within a single GraphQL request. Hot Chocolate uses [GreenDonut](https://github.com/ChilliCream/graphql-platform/tree/main/src/GreenDonut) as its DataLoader implementation.

# Identifying the N+1 Query Pattern

Consider a query that requests a list of books and, for each book, its author:

```graphql
query GetBooksWithAuthors {
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

A correct response includes each book with its author:

```json
{
  "data": {
    "books": [
      {
        "id": "1",
        "title": "The Left Hand of Darkness",
        "author": {
          "id": "1",
          "name": "Ursula K. Le Guin"
        }
      },
      {
        "id": "2",
        "title": "A Wizard of Earthsea",
        "author": {
          "id": "1",
          "name": "Ursula K. Le Guin"
        }
      },
      {
        "id": "3",
        "title": "Kindred",
        "author": {
          "id": "2",
          "name": "Octavia E. Butler"
        }
      },
      {
        "id": "4",
        "title": "Parable of the Sower",
        "author": {
          "id": "2",
          "name": "Octavia E. Butler"
        }
      },
      {
        "id": "5",
        "title": "Network Effect",
        "author": {
          "id": "3",
          "name": "Martha Wells"
        }
      }
    ]
  }
}
```

The structure of the JSON is not the issue. The concern is the number of data calls required to build it.

If the `books` resolver fetches the list of books and the `author` field resolver fetches the author for each book, the server will make:

```text
1 data call  -> load the book list
5 data calls -> load one author for each returned book
```

With 5 books, this results in 6 calls. With 50 books, it could be 51 calls. This is the N+1 pattern: one call for the parent list, plus one for each item in the list.

While the resolver is functionally correct, it does not scale as the number of parent objects increases.

# Add Logging to Observe the Problem

Logging helps you measure the server's data access behavior.

If you already see EF Core SQL commands in your terminal, run the query above and look for repeated author lookups. These typically appear as repeated `SELECT` statements against the `Authors` table, each with a different parameter.

If you do not see EF Core command logging, add the following development-only logging to your `LibraryDbContext` registration in `Program.cs`:

```csharp
using LibraryServer.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<LibraryDbContext>(
    options => options
        .UseSqlite("Data Source=library.db")
        .LogTo(Console.WriteLine, LogLevel.Information));

builder.AddGraphQL()
    .AddFiltering()
    .AddTypes();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.MapGraphQL();

app.RunWithGraphQLCommands(args);
```

Keep any other registrations your project needs, such as seed data or additional services. The key addition is `.LogTo(Console.WriteLine, LogLevel.Information)` on the EF Core options.

Start the server:

```bash
dotnet run
```

Run the `GetBooksWithAuthors` query in Nitro.

Before making any changes, you should see output like:

```text
Executed DbCommand ... SELECT ... FROM "Books" ...
Executed DbCommand ... SELECT ... FROM "Authors" ... WHERE "Id" = @__book_AuthorId_0
Executed DbCommand ... SELECT ... FROM "Authors" ... WHERE "Id" = @__book_AuthorId_0
Executed DbCommand ... SELECT ... FROM "Authors" ... WHERE "Id" = @__book_AuthorId_0
Executed DbCommand ... SELECT ... FROM "Authors" ... WHERE "Id" = @__book_AuthorId_0
Executed DbCommand ... SELECT ... FROM "Authors" ... WHERE "Id" = @__book_AuthorId_0
```

Your SQL and parameter names may differ. The important pattern is:

- One query for the list of books
- Multiple queries for authors, one per book

Record this behavior so you can compare it after introducing DataLoader.

# Create a Source-Generated DataLoader for Authors

A DataLoader batches multiple key lookups into a single operation.

Create a `DataLoaders` folder and add a file named `DataLoaders/AuthorDataLoaders.cs`:

```csharp
using GreenDonut;
using LibraryServer.Data;
using LibraryServer.Types;
using Microsoft.EntityFrameworkCore;

namespace LibraryServer.DataLoaders;

internal static class AuthorDataLoaders
{
    [DataLoader]
    public static async Task<Dictionary<int, Author>> GetAuthorByIdAsync(
        IReadOnlyList<int> ids,
        LibraryDbContext db,
        CancellationToken cancellationToken)
        => await db.Authors
            .AsNoTracking()
            .Where(author => ids.Contains(author.Id))
            .ToDictionaryAsync(author => author.Id, cancellationToken);
}
```

The `[DataLoader]` attribute instructs the source generator to create the DataLoader type at build time.

This method receives all requested author IDs for the current batch and returns a dictionary keyed by those IDs. Hot Chocolate uses this dictionary to map each `LoadAsync` call to the correct `Author`.

The generated DataLoader name is based on the method name:

| Method name           | Generated interface         | Generated class         |
|----------------------|----------------------------|------------------------|
| `GetAuthorByIdAsync` | `IAuthorByIdDataLoader`    | `AuthorByIdDataLoader` |

The `Get` prefix and `Async` suffix are removed. The remaining name, `AuthorById`, becomes the DataLoader name.

Build the project:

```bash
dotnet build
```

You should see:

```text
Build succeeded.
```

If your editor still cannot find `IAuthorByIdDataLoader` after a successful build, restart the C# language service or reopen the project. The source generator runs during build.

# Update the Field Resolver to Use the DataLoader

Replace the direct per-book author lookup with a DataLoader call.

Your current author resolver might look like this:

```csharp
using LibraryServer.Data;
using Microsoft.EntityFrameworkCore;

namespace LibraryServer.Types;

[ExtendObjectType<Book>]
public static partial class BookResolvers
{
    [BindMember(nameof(Book.AuthorId))]
    [GraphQLDescription("The person who wrote the book.")]
    public static async Task<Author> GetAuthorAsync(
        [Parent] Book book,
        LibraryDbContext db,
        CancellationToken cancellationToken)
    {
        return await db.Authors
            .AsNoTracking()
            .FirstAsync(
                author => author.Id == book.AuthorId,
                cancellationToken);
    }

    [GraphQLDescription("A short citation for the book.")]
    public static async Task<string> GetCitationAsync(
        [Parent] Book book,
        LibraryDbContext db,
        CancellationToken cancellationToken)
    {
        var author = await db.Authors
            .AsNoTracking()
            .FirstAsync(
                author => author.Id == book.AuthorId,
                cancellationToken);

        return $"{book.Title} by {author.Name}";
    }
}
```

This resolver receives a single `Book` and fetches its `Author` from the database. Hot Chocolate invokes it once for each selected book.

Update `Types/BookResolvers.cs` so the resolver uses the generated DataLoader interface. If your file contains other resolver methods, such as `GetCitation`, keep them and update only the author resolver.

```csharp
using LibraryServer.Data;
using LibraryServer.DataLoaders;
using Microsoft.EntityFrameworkCore;

namespace LibraryServer.Types;

[ExtendObjectType<Book>]
public static partial class BookResolvers
{
    [BindMember(nameof(Book.AuthorId))]
    [GraphQLDescription("The person who wrote the book.")]
    public static async Task<Author> GetAuthorAsync(
        [Parent] Book book,
        IAuthorByIdDataLoader authorById,
        CancellationToken cancellationToken)
        => await authorById.LoadAsync(book.AuthorId, cancellationToken);

    [GraphQLDescription("A short citation for the book.")]
    public static async Task<string> GetCitationAsync(
        [Parent] Book book,
        LibraryDbContext db,
        CancellationToken cancellationToken)
    {
        var author = await db.Authors
            .AsNoTracking()
            .FirstAsync(a => a.Id == book.AuthorId, cancellationToken);

        return $"{book.Title} by {author.Name}";
    }
}
```

Each resolver still requests the author it needs:

```csharp
await authorById.LoadAsync(book.AuthorId, cancellationToken);
```

The difference is that the DataLoader coordinates these requests across all sibling `author` resolvers. It collects the keys, removes duplicates, and calls `GetAuthorByIdAsync` once for the batch.

Build the project:

```bash
dotnet build
```

You should see:

```text
Build succeeded.
```

Restart the server if it is running:

```bash
dotnet run
```

Refresh Nitro's schema information. The `Book.author` field should still be present with the same GraphQL shape.

# Run the Query Again and Compare Data Calls

Run the same query as before:

```graphql
query GetBooksWithAuthors {
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

The response should match the previous output. Clients will not see a different field name or JSON structure.

Expected response:

```json
{
  "data": {
    "books": [
      {
        "id": "1",
        "title": "The Left Hand of Darkness",
        "author": {
          "id": "1",
          "name": "Ursula K. Le Guin"
        }
      },
      {
        "id": "2",
        "title": "A Wizard of Earthsea",
        "author": {
          "id": "1",
          "name": "Ursula K. Le Guin"
        }
      },
      {
        "id": "3",
        "title": "Kindred",
        "author": {
          "id": "2",
          "name": "Octavia E. Butler"
        }
      },
      {
        "id": "4",
        "title": "Parable of the Sower",
        "author": {
          "id": "2",
          "name": "Octavia E. Butler"
        }
      },
      {
        "id": "5",
        "title": "Network Effect",
        "author": {
          "id": "3",
          "name": "Martha Wells"
        }
      }
    ]
  }
}
```

Now, check the terminal output.

After introducing DataLoader, you should see:

```text
Executed DbCommand ... SELECT ... FROM "Books" ...
Executed DbCommand ... SELECT ... FROM "Authors" ... WHERE "Id" IN (...)
```

Your database provider may print a different SQL format, such as `WHERE "Id" IN (@__ids_0, @__ids_1, @__ids_2)`. The key point is that author loading is now batched, not repeated for each book.

If multiple books share the same `AuthorId`, the DataLoader sends that key only once for the batch. Both field resolvers receive the author for that key.

DataLoader also caches by key for the duration of the current GraphQL request. If the same author is requested again later in the same request, the DataLoader returns the cached result. Each new HTTP request starts with a fresh DataLoader cache.

# What DataLoader Handles

Keep this process in mind:

```text
Query.books returns parent books
  ↓
Book.author resolver calls LoadAsync(book.AuthorId)
  ↓
DataLoader collects all requested author IDs for the current request batch
  ↓
GetAuthorByIdAsync receives the collected IDs
  ↓
The batch method returns a dictionary keyed by author ID
  ↓
Hot Chocolate maps each author back to the field that requested it
```

DataLoader is responsible for:

- Batching key-based reads within a single GraphQL request
- Deduplicating repeated keys in that request
- Caching loaded values for the request
- Mapping each requested key to its result

DataLoader does not replace:

- Pagination
- Filtering
- Sorting
- Projections
- Database indexes
- Authorization
- Global or cross-request caching

The most important implementation detail is that the batch method must return results keyed by the requested IDs:

```csharp
ToDictionaryAsync(author => author.Id, cancellationToken)
```

If the dictionary is keyed by the wrong property, Hot Chocolate cannot match the loaded author to the original `LoadAsync(book.AuthorId, ...)` call.

# Checkpoint: Your Server Avoids N+1 for Authors

Do not proceed until all of these are true:

- `DataLoaders/AuthorDataLoaders.cs` exists
- `AuthorDataLoaders.GetAuthorByIdAsync` is marked with `[DataLoader]`
- The batch method accepts `IReadOnlyList<int> ids`, `LibraryDbContext db`, and `CancellationToken cancellationToken`
- The batch method returns a dictionary keyed by `Author.Id`
- `Types/BookResolvers.cs` injects `IAuthorByIdDataLoader`
- `GetAuthorAsync` calls `authorById.LoadAsync(book.AuthorId, cancellationToken)`
- `dotnet build` succeeds
- `dotnet run` starts the server
- Nitro still shows `author` on `Book`
- The nested `books { author { id name } }` query works
- The response data matches previous behavior
- The logs show batched author loading instead of one lookup per book

This prepares your project for pagination. As clients request more books, avoiding repeated relationship lookups becomes even more important.

# Troubleshooting DataLoader Issues

Use this table to resolve common problems:

| Symptom | Likely cause | Fix | Verify |
|---------|--------------|-----|--------|
| `IAuthorByIdDataLoader` cannot be found | The project has not built since adding `[DataLoader]`, the method name is different, or the namespace is missing | Run `dotnet build`, confirm the method is named `GetAuthorByIdAsync`, and add `using LibraryServer.DataLoaders;` to `BookResolvers.cs` | The build succeeds and the resolver parameter type resolves |
| `[DataLoader]` cannot be found | The `GreenDonut` namespace is missing or package versions are not aligned | Add `using GreenDonut;` and ensure your Hot Chocolate package versions match | `AuthorDataLoaders.cs` builds |
| The nested query still performs one author lookup per book | The resolver still calls `LibraryDbContext` directly or a different author field is being resolved | Replace the direct lookup with `authorById.LoadAsync(book.AuthorId, cancellationToken)` | Logs show one batched author lookup for the selected books |
| Some `author` fields are `null` | A `Book.AuthorId` value does not exist in seeded authors, or the dictionary is keyed by the wrong property | Check the seed data and key the dictionary with `author.Id` | Query a book with a known author ID and confirm `author.id` is returned |
| EF Core throws during the batch method | The key type does not match the entity ID type, or the query uses a property that does not exist | Match `IReadOnlyList<int>` to `Author.Id` and `Book.AuthorId`, or use the key type from your model | `dotnet build` succeeds and the query returns data |
| The IDE shows generated type errors but the command line build succeeds | The editor has stale source generator state | Restart the C# language service or reopen the project | The editor recognizes `IAuthorByIdDataLoader` |

For more on DataLoader, see the [DataLoader reference](/docs/hotchocolate/v16/resolvers-and-data/dataloader/). For details on resolver parameters like `[Parent]`, services, and `CancellationToken`, see [Resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers/) and [Dependency Injection](/docs/hotchocolate/v16/resolvers-and-data/dependency-injection/).

# Next Steps

Continue to [Add pagination](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/07-add-pagination/). Pagination and DataLoader work together: pagination keeps list results manageable, and DataLoader reduces repeated relationship work within each request.

Optional further reading:

- [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader/) for group loaders, options, manual DataLoader classes, and batch resolvers
- [Fetching from databases](/docs/hotchocolate/v16/resolvers-and-data/fetching-from-databases/) for data access patterns
- [Entity Framework Core integration](/docs/hotchocolate/v16/integrations/entity-framework/) for EF Core-specific guidance
- [Resolver and data middleware model](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/resolver-and-data-middleware-model/) for the execution flow behind resolvers and data middleware
