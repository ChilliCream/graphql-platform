---
title: "Connect to real data"
description: "Replace tutorial sample objects with a local SQLite database, seed library data with EF Core, update resolvers, and verify the same GraphQL API reads stored data."
---

In previous chapters, you built a library API using C# types and resolver methods, but the data was still coming from in-memory sample objects.

This chapter transitions those GraphQL fields to use a real data source. You'll use EF Core with a local SQLite file, so you don't need an external database server.

By the end of this chapter, you will have:

- Defined EF Core entity classes for books and authors
- Added a `LibraryDbContext`
- Seeded a small library catalog
- Registered EF Core with ASP.NET Core dependency injection
- Updated query resolvers to fetch data from the database
- Kept the GraphQL field names stable for clients
- Verified list, by-id, and filtered queries in Nitro

This guide follows a single provider path. If you want to compare EF Core with MongoDB, REST, or other services, see [Connecting to real data](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/connecting-to-real-data/) after completing this tutorial step.

# What changes in this chapter

Previously, the `books` field returned objects from `BookCatalog`.

After this chapter, the field will return rows through EF Core:

```text
GraphQL request -> resolver method -> LibraryDbContext -> SQLite database -> GraphQL response
```

The client API remains familiar:

| Before | After |
| --- | --- |
| `books` returns sample objects from a service. | `books` returns `Book` entities from SQLite. |
| `bookById` or the by-id resolver reads from the sample list. | `bookById` reads from SQLite by identifier. |
| Filtering runs over tutorial data. | Filtering runs over an EF Core query shape. |
| `Book.author` is already present in the schema. | `Book.author` is resolved from the stored `AuthorId`. |

You'll work with these files:

```text
LibraryServer/
├── Program.cs
├── Data/
│   └── LibraryDbContext.cs
└── Types/
    ├── Author.cs
    ├── Book.cs
    ├── BookResolvers.cs
    └── Query.cs
```

If your project contains extra files from earlier chapters, keep them unless this chapter instructs you to replace them.

# Add the catalog model

EF Core requires entity types it can create, track, and map to tables. Replace the tutorial-only object shape with settable entity classes.

Replace `Types/Author.cs` with:

```csharp
namespace LibraryServer.Types;

[GraphQLDescription("A person who wrote one or more books in the library catalog.")]
public sealed class Author
{
    [GraphQLDescription("The stable identifier for the author.")]
    public int Id { get; set; }

    [GraphQLDescription("The author's display name.")]
    public string Name { get; set; } = string.Empty;
}
```

Replace `Types/Book.cs` with:

```csharp
namespace LibraryServer.Types;

[GraphQLDescription("A book that can be browsed in the library catalog.")]
public sealed class Book
{
    [GraphQLDescription("The stable identifier for the book.")]
    public int Id { get; set; }

    [GraphQLDescription("The title printed for readers.")]
    public string Title { get; set; } = string.Empty;

    public int AuthorId { get; set; }
}
```

The `AuthorId` property is used internally for storage. You'll use it in a resolver to expose the GraphQL `author` field, but clients won't query `authorId` directly.

Save these files. If your `BookResolvers.cs` from the resolver chapter still references `book.Author.Name`, the project will not build until you update that resolver later in this chapter.

If you don't have a `BookResolvers.cs` file yet, you can build now:

```bash
dotnet build
```

You should see:

```text
Build succeeded.
```

If the build fails because `Book` no longer has an `Author` property, continue to the `BookResolvers.cs` update below before building again.

# Add a database context and seed data

First, install the SQLite provider for EF Core:

```bash
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
```

If you added filtering in the previous chapter, your project should already reference `HotChocolate.Data`. If `.AddFiltering()` or `[UseFiltering]` is not recognized later, add the package:

```bash
dotnet add package HotChocolate.Data
```

Create a `Data` folder and add `Data/LibraryDbContext.cs`:

```csharp
using LibraryServer.Types;
using Microsoft.EntityFrameworkCore;

namespace LibraryServer.Data;

public sealed class LibraryDbContext(DbContextOptions<LibraryDbContext> options)
    : DbContext(options)
{
    public DbSet<Book> Books => Set<Book>();

    public DbSet<Author> Authors => Set<Author>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>()
            .HasOne<Author>()
            .WithMany()
            .HasForeignKey(b => b.AuthorId)
            .IsRequired();

        modelBuilder.Entity<Author>().HasData(
            new Author { Id = 1, Name = "Ursula K. Le Guin" },
            new Author { Id = 2, Name = "Octavia E. Butler" },
            new Author { Id = 3, Name = "Martha Wells" }
        );

        modelBuilder.Entity<Book>().HasData(
            new Book { Id = 1, Title = "The Left Hand of Darkness", AuthorId = 1 },
            new Book { Id = 2, Title = "A Wizard of Earthsea", AuthorId = 1 },
            new Book { Id = 3, Title = "Kindred", AuthorId = 2 },
            new Book { Id = 4, Title = "Parable of the Sower", AuthorId = 2 },
            new Book { Id = 5, Title = "Network Effect", AuthorId = 3 }
        );
    }
}
```

This seed data provides a repeatable local catalog:

| Book id | Title | Author |
| --- | --- | --- |
| `1` | `The Left Hand of Darkness` | `Ursula K. Le Guin` |
| `2` | `A Wizard of Earthsea` | `Ursula K. Le Guin` |
| `3` | `Kindred` | `Octavia E. Butler` |
| `4` | `Parable of the Sower` | `Octavia E. Butler` |
| `5` | `Network Effect` | `Martha Wells` |

Authors appear more than once on purpose. The next chapter will use this integer-keyed relationship to demonstrate N+1 query issues. Keep `Author.Id` and `Book.AuthorId` as `int` keys for DataLoader integration later.

# Register EF Core and GraphQL services

Open `Program.cs` and replace its contents with:

```csharp
using LibraryServer.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<LibraryDbContext>(
    options => options.UseSqlite("Data Source=library.db"));

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

This setup does three things:

1. Registers the EF Core context with ASP.NET Core dependency injection using `AddDbContext<LibraryDbContext>`
2. Configures EF Core to use a local SQLite file with `UseSqlite("Data Source=library.db")`
3. Ensures the development database is created and seeded when the app starts for the first time using `EnsureCreatedAsync`

`EnsureCreatedAsync` is suitable for tutorial and development purposes. For production, use the EF Core migration workflow described in the [Microsoft EF Core migrations documentation](https://learn.microsoft.com/ef/core/managing-schemas/migrations/).

Build the project:

```bash
dotnet build
```

You should see:

```text
Build succeeded.
```

If you see an error about `AddFiltering` missing, check that the `HotChocolate.Data` package is installed and that its version matches your other Hot Chocolate packages.

For more on package installation, see [Packages](/docs/hotchocolate/v16/learn/4-installation-and-setup/packages/).

# Replace sample resolvers with database resolvers

Now, update the root query fields to use `LibraryDbContext` instead of `BookCatalog`.

Replace `Types/Query.cs` with:

```csharp
using LibraryServer.Data;
using Microsoft.EntityFrameworkCore;

namespace LibraryServer.Types;

[QueryType]
public static partial class Query
{
    [GraphQLDescription("Gets the books currently available in the library catalog.")]
    [UseFiltering]
    public static IQueryable<Book> GetBooks(LibraryDbContext db)
        => db.Books
            .AsNoTracking()
            .OrderBy(b => b.Id);

    [GraphQLDescription("Gets one book from the library catalog by its identifier.")]
    public static async Task<Book?> GetBookByIdAsync(
        int id,
        LibraryDbContext db,
        CancellationToken cancellationToken)
    {
        return await db.Books
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }
}
```

`GetBooks` returns `IQueryable<Book>`, which allows Hot Chocolate filtering to operate on the EF Core query before data is loaded.

`GetBookByIdAsync` returns `Book?` because a client might request an id that doesn't exist. The `CancellationToken` is provided by Hot Chocolate and passed to EF Core.

Next, update the fields on `Book`.

Replace `Types/BookResolvers.cs` with:

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
            .FirstAsync(a => a.Id == book.AuthorId, cancellationToken);
    }

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

The `[BindMember(nameof(Book.AuthorId))]` attribute tells Hot Chocolate to map this resolver to the `author` field, replacing the storage property. Clients continue to select `author`, not `authorId`.

Both the `author` and `citation` resolvers use direct lookups for now. When a query requests several books and their authors, this approach can result in repeated work. The next chapter will introduce DataLoader to optimize these lookups. The `citation` resolver will continue to perform its own author lookup.

If you created `Services/BookCatalog.cs` in an earlier chapter, you can leave it in the project or delete it. It is no longer registered in `Program.cs`, and no resolver should depend on it after this update.

Build the project again:

```bash
dotnet build
```

You should see:

```text
Build succeeded.
```

# Verify the database is being used

Start the server:

```bash
dotnet run
```

Open Nitro at your local `/graphql` endpoint. Use the port shown in your terminal, for example:

```text
http://localhost:5095/graphql
```

If Nitro still shows the old in-memory schema, refresh its schema information.

Try the following list query:

```graphql
query GetBooksFromDatabase {
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

You should receive a response like:

```json
{
  "data": {
    "books": [
      {
        "id": 1,
        "title": "The Left Hand of Darkness",
        "citation": "The Left Hand of Darkness by Ursula K. Le Guin",
        "author": {
          "id": 1,
          "name": "Ursula K. Le Guin"
        }
      },
      {
        "id": 2,
        "title": "A Wizard of Earthsea",
        "citation": "A Wizard of Earthsea by Ursula K. Le Guin",
        "author": {
          "id": 1,
          "name": "Ursula K. Le Guin"
        }
      },
      {
        "id": 3,
        "title": "Kindred",
        "citation": "Kindred by Octavia E. Butler",
        "author": {
          "id": 2,
          "name": "Octavia E. Butler"
        }
      },
      {
        "id": 4,
        "title": "Parable of the Sower",
        "citation": "Parable of the Sower by Octavia E. Butler",
        "author": {
          "id": 2,
          "name": "Octavia E. Butler"
        }
      },
      {
        "id": 5,
        "title": "Network Effect",
        "citation": "Network Effect by Martha Wells",
        "author": {
          "id": 3,
          "name": "Martha Wells"
        }
      }
    ]
  }
}
```

Identifiers appear as numbers in JSON because the model uses `int` properties, and Hot Chocolate v16 infers GraphQL `Int` fields from them.

Next, run a by-id query with variables:

```graphql
query GetBookById($id: Int!) {
  bookById(id: $id) {
    id
    title
    author {
      name
    }
  }
}
```

Use this variables JSON in Nitro:

```json
{
  "id": 3
}
```

Expected response:

```json
{
  "data": {
    "bookById": {
      "id": 3,
      "title": "Kindred",
      "author": {
        "name": "Octavia E. Butler"
      }
    }
  }
}
```

Try a filtered query:

```graphql
query FilterBooksFromDatabase {
  books(where: { title: { contains: "Earthsea" } }) {
    id
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
    "books": [
      {
        "id": 2,
        "title": "A Wizard of Earthsea",
        "author": {
          "name": "Ursula K. Le Guin"
        }
      }
    ]
  }
}
```

These results confirm:

- The app created and seeded `library.db`
- The root resolver reads through `LibraryDbContext`
- The `where` filter works after moving the field to EF Core

If you still see old sample data, stop the server, rebuild, restart, and refresh Nitro's schema. If you changed the seed after the database file was created, stop the server and delete `library.db` from the project directory so `EnsureCreatedAsync` can recreate it.

# Understand the provider boundary

Hot Chocolate manages the GraphQL schema and resolver pipeline. EF Core translates supported LINQ query shapes to database operations. SQLite stores the local database file.

Keep these boundaries in mind:

| Layer | Owns |
| --- | --- |
| GraphQL schema | Field names, arguments, return types, nullability, and descriptions |
| Resolver methods | Which application or data service produces a field value |
| Hot Chocolate data middleware | Standard client shaping such as filtering, paging, projections, and sorting |
| EF Core provider | Translating supported LINQ expressions to database operations |
| SQLite | Storing the local tutorial data |

Returning `IQueryable<Book>` from `GetBooks` allows filtering to work with a provider-backed query. Not every C# expression can be translated to SQL, though. If EF Core cannot translate a LINQ expression, the error is at the EF Core provider boundary. Refer to the [EF Core querying documentation](https://learn.microsoft.com/ef/core/querying/) for provider behavior and the Hot Chocolate [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering/) page for GraphQL filter configuration.

The schema does not need to mirror the database. The database stores `Book.AuthorId`, while the GraphQL schema exposes `Book.author`. The resolver bridges the storage shape and the domain field used by clients.

# Checkpoint: real data powers the tutorial API

You are ready to continue when all of these are true:

- `dotnet build` reports `Build succeeded`.
- `dotnet run` starts the server.
- The project directory contains a local `library.db` file after the server starts.
- Nitro connects to your local `/graphql` endpoint.
- `Query.books` still appears in the schema.
- `Query.bookById(id:)` appears in the schema.
- `Book.author` appears in the schema.
- The list query returns five seeded books.
- The by-id query with `{ "id": 3 }` returns `Kindred`.
- The filtered query with `contains: "Earthsea"` returns only `A Wizard of Earthsea`.

This chapter updated these project areas:

| Area | Expected state |
| --- | --- |
| `Types/Author.cs` | EF-compatible `Author` entity with `Id` and `Name`. |
| `Types/Book.cs` | EF-compatible `Book` entity with `Id`, `Title`, and `AuthorId`. |
| `Data/LibraryDbContext.cs` | EF Core context with `Books`, `Authors`, a relationship, and seed data. |
| `Program.cs` | Registers `LibraryDbContext`, keeps GraphQL filtering, creates the local database, and maps `/graphql`. |
| `Types/Query.cs` | Reads `books` and `bookById` through `LibraryDbContext`. |
| `Types/BookResolvers.cs` | Resolves `author` and `citation` from the parent book's `AuthorId`. |

If you run into issues, use [Source code and checkpoints](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/00-source-code-and-checkpoints/) to compare your files. For more on EF Core integration, see [Entity Framework Core](/docs/hotchocolate/v16/integrations/entity-framework/) and [Fetching from databases](/docs/hotchocolate/v16/resolvers-and-data/fetching-from-databases/).

Continue to [Fix N+1 with DataLoader](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/06-fix-n-plus-1-with-dataloader/). You now have a working relationship field, and the next chapter will show how to make that lookup more efficient.
