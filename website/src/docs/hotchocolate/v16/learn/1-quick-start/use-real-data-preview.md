---
title: "Use real data preview"
description: "Replace the scaffolded in-memory Hot Chocolate resolver with one small real-data read, keep the same query shape, and choose the next data topic."
---

This lesson builds on the scaffolded `GettingStarted` server by changing a single aspect: the `book` field will no longer return a hard-coded `Book` from `Types/Query.cs`. Instead, it will fetch data from a real source.

By the end, you will have:

- Saved the current `book` query as a before state
- Chosen a preview data path
- Registered a data source with ASP.NET Core dependency injection
- Updated the resolver to read from that source
- Run the same GraphQL query and compared the response
- Identified the first production concern before expanding your data access code

This page is a preview. It demonstrates how real data enters the Hot Chocolate resolver model. For more advanced topics, see [Fetching from databases](/docs/hotchocolate/v16/resolvers-and-data/fetching-from-databases), [Entity Framework Core](/docs/hotchocolate/v16/integrations/entity-framework), [MongoDB](/docs/hotchocolate/v16/integrations/mongodb), or [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader).

# Start from the working `book` query

Open your `GettingStarted` project from [Install and scaffold](/docs/hotchocolate/v16/get-started/install-and-scaffold). If the server is not running, start it from the project directory:

```bash
dotnet run
```

Open Nitro at your local `/graphql` endpoint. The port may differ on your machine:

```text
http://localhost:5095/graphql
```

Run the starter query:

```graphql
{
  book {
    title
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
    "book": {
      "title": "C# in depth.",
      "author": {
        "name": "Jon Skeet"
      }
    }
  }
}
```

This is the response shape to preserve. The client requests `book.title` and `book.author.name`. In this lesson, you will change only the source of the `Book` value, not the query shape.

Stop the running server with <kbd>Ctrl</kbd> + <kbd>C</kbd> before making changes.

# Choose a real data path for the preview

Select the path that matches your current setup:

| Starting point | Preview path | Go deeper |
| --- | --- | --- |
| You already have relational data or a `DbContext`. | Read through EF Core from a resolver. | [Entity Framework Core](/docs/hotchocolate/v16/integrations/entity-framework), [EF Core documentation](https://learn.microsoft.com/ef/core/) |
| You already have MongoDB, a database name, and a collection. | Read one document through the MongoDB .NET driver from a resolver. | [MongoDB integration](/docs/hotchocolate/v16/integrations/mongodb), [MongoDB C# driver documentation](https://www.mongodb.com/docs/drivers/csharp/current/) |
| Your application already owns data access behind a service. | Inject that application service into the resolver. | [Dependency injection](/docs/hotchocolate/v16/resolvers-and-data/dependency-injection), [Resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers) |
| You want a complete guided server. | Leave the quick start and follow the full tutorial. | [Build your first GraphQL server](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server) |

The rest of this page uses EF Core with a local SQLite file for a concrete example. If you prefer MongoDB or an application service, follow the same resolver pattern and refer to the alternatives later in this guide.

# Register an EF Core data source

Install the SQLite EF Core provider:

```bash
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
```

Add a `Data` folder and create `Data/BookDbContext.cs`:

```csharp
using Microsoft.EntityFrameworkCore;

namespace GettingStarted.Data;

public sealed class BookDbContext(DbContextOptions<BookDbContext> options)
    : DbContext(options)
{
    public DbSet<BookRow> Books => Set<BookRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BookRow>().HasData(new BookRow
        {
            Id = 1,
            Title = "C# in depth.",
            AuthorName = "Jon Skeet"
        });
    }
}
```

Create `Data/BookRow.cs`:

```csharp
namespace GettingStarted.Data;

public sealed class BookRow
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string AuthorName { get; set; } = string.Empty;
}
```

Update `Program.cs` so the ASP.NET Core service container can create `BookDbContext` for resolvers:

```csharp
using GettingStarted.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<BookDbContext>(
    options => options.UseSqlite("Data Source=books.db"));

builder.AddGraphQL().AddTypes();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BookDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.MapGraphQL();

app.RunWithGraphQLCommands(args);
```

This preview uses `EnsureCreatedAsync` to create a local development database and seed it with one row using `HasData`. For production, use your application's established database setup and migration process.

Build the project:

```bash
dotnet build
```

Expected output:

```text
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

The key flow is:

```text
Program.cs registers BookDbContext -> resolver requests BookDbContext -> Hot Chocolate injects the service into the resolver
```

Hot Chocolate uses the ASP.NET Core service container for resolver parameters. If a resolver parameter type is registered as a service, it is injected automatically, not exposed as a GraphQL argument.

# Update the resolver to use the data source

Open `Types/Query.cs`. The starter resolver returns a `Book` directly:

```csharp
namespace GettingStarted.Types;

[QueryType]
public static partial class Query
{
    public static Book GetBook()
        => new Book("C# in depth.", new Author("Jon Skeet"));
}
```

Replace it with an async resolver that reads the seeded EF Core row:

```csharp
using GettingStarted.Data;
using Microsoft.EntityFrameworkCore;

namespace GettingStarted.Types;

[QueryType]
public static partial class Query
{
    public static async Task<Book> GetBookAsync(
        BookDbContext db,
        CancellationToken ct)
    {
        var row = await db.Books
            .AsNoTracking()
            .OrderBy(b => b.Id)
            .FirstAsync(ct);

        return new Book(row.Title, new Author(row.AuthorName));
    }
}
```

`GetBookAsync` still creates the GraphQL field named `book`. Hot Chocolate removes the `Get` prefix and `Async` suffix when generating the field name.

The field continues to return the same GraphQL object shape:

```text
BookRow from EF Core -> Book record returned by resolver -> book selection in GraphQL -> data.book JSON object
```

Restart the server:

```bash
dotnet run
```

Open the `/graphql` endpoint again. If Nitro displays outdated schema information, refresh Nitro's schema or reload the browser.

Run the same query:

```graphql
{
  book {
    title
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
    "book": {
      "title": "C# in depth.",
      "author": {
        "name": "Jon Skeet"
      }
    }
  }
}
```

The JSON response matches the original because the seeded row contains the same values as the starter sample. The difference is in the C# code: the resolver now reads `BookRow` through `BookDbContext` instead of returning a hard-coded `Book`.

# Keep the GraphQL API stable

Use this checklist to confirm what changed:

| Part | Before | After |
| --- | --- | --- |
| Client operation | Selects `book { title author { name } }`. | Same operation. |
| GraphQL field name | `book` | `book` |
| Resolver implementation | Returns `new Book(...)` directly. | Reads `BookRow` from EF Core and maps it to `Book`. |
| Data source | C# literal values in `Types/Query.cs`. | Local SQLite database through `BookDbContext`. |
| Response shape | `data.book.title` and `data.book.author.name`. | Same response shape. |

This separation is important: the GraphQL schema defines the client contract, while the resolver determines how to produce the field's value.

A database table, document, REST response, or application service result does not need to match the GraphQL type exactly. In this EF Core preview, `BookRow` stores `AuthorName` as a string, but the GraphQL response still exposes an `author` object with a `name` field.

# Use the same pattern with MongoDB or an application service

If MongoDB is your data source, register the MongoDB collection with ASP.NET Core dependency injection and inject it into the resolver.

```bash
dotnet add package MongoDB.Driver
```

```csharp
using GettingStarted.Data;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IMongoClient>(
    _ => new MongoClient(
        builder.Configuration.GetConnectionString("MongoDb")));

builder.Services.AddScoped(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    var database = client.GetDatabase("catalog");
    return database.GetCollection<BookDocument>("books");
});

builder.AddGraphQL().AddTypes();
```

```csharp
using MongoDB.Bson;

namespace GettingStarted.Data;

public sealed class BookDocument
{
    public ObjectId Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
}
```

```csharp
using GettingStarted.Data;
using MongoDB.Driver;

namespace GettingStarted.Types;

[QueryType]
public static partial class Query
{
    public static async Task<Book> GetBookAsync(
        IMongoCollection<BookDocument> books,
        CancellationToken ct)
    {
        var document = await books
            .Find(_ => true)
            .FirstAsync(ct);

        return new Book(document.Title, new Author(document.AuthorName));
    }
}
```

This MongoDB example assumes you have a connection string named `MongoDb`, a database called `catalog`, and a `books` collection with at least one document. Adjust the configuration to match your environment.

If your application already has a data service that returns `Book`, the resolver can depend on that service instead:

```csharp
namespace GettingStarted.Types;

[QueryType]
public static partial class Query
{
    public static async Task<Book> GetBookAsync(
        BookCatalog catalog,
        CancellationToken ct)
        => await catalog.GetFeaturedBookAsync(ct);
}
```

Register the service in `Program.cs` before `builder.AddGraphQL().AddTypes()`:

```csharp
builder.Services.AddScoped<BookCatalog>();
```

The resolver model is consistent across all three approaches: use GraphQL arguments as resolver inputs, request registered services as resolver parameters, await I/O operations, and pass `CancellationToken` to APIs that support it.

# Troubleshooting: when the real data query fails

If the checkpoint query does not return the expected response, use this table to diagnose and resolve issues:

| Symptom | Likely cause | Fix | Verify |
| --- | --- | --- | --- |
| The app fails during schema creation and says a service cannot be resolved. | The resolver has a `BookDbContext`, `IMongoCollection<T>`, or service parameter that is not registered in `Program.cs`. | Register the data source or service before `builder.AddGraphQL().AddTypes()`. | Restart the app and run the `book` query. |
| The project does not compile after adding EF Core. | The package is missing, a `using` directive is missing, or the namespace does not match your project name. | Add `Microsoft.EntityFrameworkCore.Sqlite`, keep your `GettingStarted` namespace, and add the `using` directives shown in the snippets. | `dotnet build` reports `Build succeeded.` |
| The response contains `errors` and `data.book` is `null`. | The resolver threw while reading the data source, or a non-null field received a null value. | Read the first error message. Confirm the local database was created and seeded, or return `Book?` for a field that can be missing. | The response either returns the known book or a documented null result. |
| Nitro says `Cannot query field "book"`. | The running server does not include the resolver type, or Nitro has stale schema information. | Rebuild, restart, and refresh Nitro's schema information. Confirm `builder.AddGraphQL().AddTypes()` is still present. | Nitro accepts the original `book` query. |
| EF Core reports that a second operation started on the same context. | The app uses an unsafe `DbContext` lifetime or changed the default resolver service scope. | Refer to the v16 [Entity Framework Core](/docs/hotchocolate/v16/integrations/entity-framework) and [Dependency injection](/docs/hotchocolate/v16/resolvers-and-data/dependency-injection) guidance. | Re-running the query completes without the context error. |
| MongoDB returns no document. | The connection string, database name, collection name, serializer mapping, or stored id shape differs from the example. | Check the document in MongoDB and query a value that exists. | The resolver returns the expected document mapped to `Book`. |

# Notice the first production data concern

This preview resolver is safe for learning because it resolves a single root field.

Be cautious when a resolver runs for each parent object in a list. For example, a `books` field might return 50 books, and an `author` resolver could load the author separately for every book:

```graphql
{
  books {
    title
    author {
      name
    }
  }
}
```

Without batching, this pattern can result in one query for the list and one author query per book, known as the N+1 problem.

Hot Chocolate addresses this with [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader). DataLoader batches related lookups and caches by key for each request. Learn about DataLoader before adding nested database resolvers to lists.

Another optimization is [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections). Projections allow supported providers to reduce selected columns or document fields based on the GraphQL selection set. This requires provider-specific setup and is separate from the first resolver preview.

Consider these as later production topics:

- [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination) for large lists
- [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering) and [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting) for client-shaped lists
- [MongoDB data middleware](/docs/hotchocolate/v16/integrations/mongodb) for native MongoDB filtering, sorting, projections, or paging
- Authorization, transactions, observability, and deployment configuration

# Continue with the full data path

You have changed the data source behind one resolver while keeping the client operation stable.

Choose your next step based on your goal:

| Next goal | Continue here |
| --- | --- |
| Build a complete app with cumulative checkpoints. | [Build your first GraphQL server](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server) |
| Understand resolver parameters, async resolvers, and parent values. | [Resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers) |
| Learn the service injection rules used by this page. | [Dependency injection](/docs/hotchocolate/v16/resolvers-and-data/dependency-injection) |
| Choose a data strategy before adding more sources. | [Connecting to real data](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/connecting-to-real-data) |
| Connect deeper to relational data. | [Fetching from databases](/docs/hotchocolate/v16/resolvers-and-data/fetching-from-databases), [Entity Framework Core](/docs/hotchocolate/v16/integrations/entity-framework) |
| Connect deeper to MongoDB. | [MongoDB integration](/docs/hotchocolate/v16/integrations/mongodb) |
| Fix repeated nested data calls. | [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader) |
| Reduce selected provider data later. | [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections) |
| Return to smaller lessons. | [Quick Start](/docs/hotchocolate/v16/learn/1-quick-start) |

Final checkpoint: the same `book` query shape now reads through a registered data source, and you know which data topic to explore before expanding your production data access.
