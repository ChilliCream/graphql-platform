---
title: "Fetching from Databases"
---

Hot Chocolate is not bound to a specific database, pattern, or architecture. You can call any data source from your resolvers. This page shows a practical example of fetching data from a database and exposing it through a GraphQL API.

[Hot Chocolate provides integrations](/docs/hotchocolate/v16/integrations) for Entity Framework Core, MongoDB, and other databases. These integrations add convenience on top of the core resolver model but are not required.

# Fetching from MongoDB

In this example, you inject a MongoDB collection into a resolver and query it directly.

<ExampleTabs>
<Implementation>

```csharp
// Models/Book.cs
public class Book
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Author { get; set; }
}

// Types/BookQueries.cs
[QueryType]
public static partial class BookQueries
{
    public static async Task<Book?> GetBookByIdAsync(
        Guid id,
        IMongoCollection<Book> collection,
        CancellationToken ct)
        => await collection.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
}
```

```csharp
// Program.cs
builder
    .AddGraphQL()
    .AddTypes();
```

</Implementation>
<Code>

```csharp
// Models/Book.cs
public class Book
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Author { get; set; }
}

// Types/BookQueries.cs
public class BookQueries
{
    public async Task<Book?> GetBookByIdAsync(
        Guid id,
        IMongoCollection<Book> collection,
        CancellationToken ct)
        => await collection.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
}

// Types/BookQueriesType.cs
public class BookQueriesType : ObjectType<BookQueries>
{
    protected override void Configure(IObjectTypeDescriptor<BookQueries> descriptor)
    {
        descriptor
            .Field(f => f.GetBookByIdAsync(default, default!, default))
            .Type<BookType>();
    }
}
```

```csharp
// Program.cs
builder
    .AddGraphQL()
    .AddQueryType<BookQueriesType>();
```

</Code>
</ExampleTabs>

# Fetching from Entity Framework Core

When using EF Core, inject your `DbContext` directly into resolvers. Hot Chocolate's EF Core integration registers the context correctly for concurrent resolver execution.

```csharp
// Types/BookQueries.cs
[QueryType]
public static partial class BookQueries
{
    public static async Task<Book?> GetBookByIdAsync(
        int id,
        CatalogContext db,
        CancellationToken ct)
        => await db.Books.FindAsync([id], ct);

    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Book> GetBooks(CatalogContext db)
        => db.Books;
}
```

When you return `IQueryable<T>`, the pagination, projection, filtering, and sorting middleware translate to native SQL queries. The database handles the heavy lifting.

[Learn more about the Entity Framework integration](/docs/hotchocolate/v16/integrations/entity-framework)

# Next Steps

- **Need to batch database calls?** See [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader).
- **Need to optimize SQL queries?** See [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections).
- **Need to integrate with MongoDB?** See [MongoDB Integration](/docs/hotchocolate/v16/integrations/mongodb).
- **Need to integrate with EF Core?** See [Entity Framework Integration](/docs/hotchocolate/v16/integrations/entity-framework).
