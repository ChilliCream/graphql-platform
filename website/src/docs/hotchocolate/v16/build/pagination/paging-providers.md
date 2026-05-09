---
title: Paging providers
---

Paging middleware adds pagination arguments and a GraphQL result shape to a field. A paging provider determines how those arguments are applied to the resolver's result.

Use a provider when your resolver returns a data source that supports efficient paging, such as LINQ, EF Core, MongoDB, or RavenDB. If your resolver already calls a paged service or external API, return a manual paging result rather than creating a provider.

```text
resolver source
    |
[UsePaging] or [UseOffsetPaging]
    |
PagingOptions.ProviderName
    |
CursorPagingProvider or OffsetPagingProvider
    |
CursorPagingHandler or OffsetPagingHandler
    |
backend query with paging applied
```

This page explains how to select and register paging providers. For information on field shapes and client requests, see [Cursor connections](connections-cursor.md) and [Offset pagination](offset.md). For details on page sizes, provider names, null ordering, and global defaults, see [Paging options](paging-options.md).

# Choose a provider

| Resolver source                           | Paging style      | Provider                           | Registration                                                                                         | Notes                                                                                                     |
| ----------------------------------------- | ----------------- | ---------------------------------- | ---------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------- |
| `IQueryable<T>` or common collection      | Cursor            | Built-in queryable cursor provider | None required, or `.AddQueryableCursorPagingProvider(...)` when naming or default ordering is needed | Use a deterministic order for database-backed queries.                                                    |
| `IQueryable<T>` or common collection      | Offset            | Built-in queryable offset provider | None required, or `.AddQueryableOffsetPagingProvider(...)` when naming or default ordering is needed | Applies offset paging through the queryable path when possible.                                           |
| EF Core `IQueryable<T>`                   | Cursor            | EF Core cursor provider            | `.AddDbContextCursorPagingProvider()`                                                                | Requires `HotChocolate.Data.EntityFramework`. Uses native keyset pagination.                              |
| EF Core `IQueryable<T>`                   | Offset            | Built-in queryable offset provider | None required in most schemas                                                                        | v16 does not include an EF-specific offset provider registration helper in this source snapshot.          |
| MongoDB executable or driver query source | Cursor and offset | MongoDB providers                  | `.AddMongoDbPagingProviders()`                                                                       | Requires `HotChocolate.Data.MongoDb`. Supports Mongo executable, aggregate, find, and collection sources. |
| RavenDB query source                      | Cursor and offset | Raven providers                    | `.AddRavenPagingProviders()`                                                                         | Requires `HotChocolate.Data.Raven`. Supports Raven query abstractions.                                    |
| Reusable custom backend query abstraction | Cursor or offset  | Custom provider                    | `.AddCursorPagingProvider<TProvider>()` or `.AddOffsetPagingProvider<TProvider>()`                   | Advanced extension point. Prefer manual results for a single field or external API call.                  |

The default queryable providers are sufficient when your resolver returns `IQueryable<T>`, `IEnumerable<T>`, or a common collection, and Hot Chocolate can apply paging after the resolver returns the source. Add an integration provider if you need to push paging into a specific backend API or if source inference might select the wrong provider.

# How provider selection works

Hot Chocolate resolves cursor and offset providers independently.

1. During schema completion, Hot Chocolate determines the resolver's source type.
2. If `PagingOptions.ProviderName` is set, it looks for a registered provider with that exact name.
3. If `ProviderName` is not set, it selects the first registered provider whose `CanHandle(source)` method returns `true`.
4. If there are registered providers but none match, it uses the first registered provider as a fallback.
5. If no providers are registered for that paging style, it creates the built-in queryable provider.

Provider names are exact and case-sensitive. Named lookup always selects by name first, so only use a name with fields whose source type is supported by that provider.

Application services are checked before schema services. This is the provider scope exposed in v16 during resolution. There is no separate field option for provider scope in the public paging options. Use `ProviderName` on a field when it must select a specific registered provider. A provider registered with `defaultProvider: true` is inserted first, making it the preferred fallback for that paging style.

# Use the built-in queryable providers

Most schemas start with the built-in queryable providers. No provider registration is required.

```csharp
#nullable enable

using HotChocolate;
using HotChocolate.Types;

[QueryType]
public static partial class ProductQueries
{
    [UsePaging]
    public static IQueryable<Product> GetProducts(CatalogContext db)
    {
        return db.Products
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Id);
    }
}
```

Offset paging uses the matching offset middleware and provider path.

```csharp
[QueryType]
public static partial class ProductQueries
{
    [UseOffsetPaging]
    public static IQueryable<Product> GetProductsOffset(CatalogContext db)
    {
        return db.Products
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Id);
    }
}
```

Register the built-in providers explicitly only when you need a name, a fallback order, or cursor total-count behavior.

```csharp
builder
    .AddGraphQL()
    .AddQueryableCursorPagingProvider(
        providerName: "Queryable",
        inlineTotalCount: true);

builder
    .AddGraphQL()
    .AddQueryableOffsetPagingProvider(providerName: "QueryableOffset");
```

`inlineTotalCount` applies to the queryable cursor provider. It asks the provider to inline the total-count query into the sliced query. Some database providers may not support that query shape.

# Register the EF Core cursor provider

To use native keyset pagination with EF Core fields, install the EF Core data integration package and register the cursor provider:

```csharp
builder
    .AddGraphQL()
    .AddQueryType()
    .AddSorting()
    .AddDbContextCursorPagingProvider();
```

Example usage:

```csharp
[QueryType]
public static partial class ProductQueries
{
    [UsePaging]
    [UseSorting]
    public static IQueryable<Product> GetProducts(CatalogContext db)
    {
        return db.Products;
    }
}
```

The field still exposes the standard connection shape, but the provider changes how paging is applied to the EF query. Always keep ordering stable, especially when clients use cursors with changing data. For more on sorting and cursor behavior, see [Cursor connections](connections-cursor.md).

The EF Core helper registers only a cursor provider. For offset fields over EF `IQueryable<T>`, use `[UseOffsetPaging]` and the built-in queryable offset provider unless your schema supplies a custom offset provider.

# Register MongoDB paging providers

To use MongoDB paging, install `HotChocolate.Data.MongoDb`, register the providers, and return a MongoDB-backed source:

```csharp
builder
    .AddGraphQL()
    .AddMongoDbPagingProviders();
```

Example for a cursor field:

```csharp
using HotChocolate;
using HotChocolate.Execution;
using MongoDB.Driver;

[QueryType]
public static partial class ProductQueries
{
    [UsePaging]
    public static IExecutable<Product> GetProducts(IMongoCollection<Product> collection)
    {
        return collection.AsExecutable();
    }
}
```

Example for an offset field:

```csharp
[QueryType]
public static partial class ProductQueries
{
    [UseOffsetPaging]
    public static IExecutable<Product> GetProductsOffset(IMongoCollection<Product> collection)
    {
        return collection.AsExecutable();
    }
}
```

`.AddMongoDbPagingProviders()` registers both cursor and offset providers, with optional name and default settings. These providers support MongoDB executable sources, `IAggregateFluent<T>`, `IFindFluent<TDocument, TProjection>`, and `IMongoCollection<T>`.

If you return `IExecutable<T>`, ensure it is backed by the MongoDB integration. A generic executable from another backend is not recognized as a MongoDB provider source.

# Register Raven paging providers

RavenDB paging is supported through `HotChocolate.Data.Raven`.

```csharp
builder
    .AddGraphQL()
    .AddRavenPagingProviders();
```

This helper registers both Raven cursor and offset providers, as well as the document store integration. The provider source checks support `IRavenQueryable<T>` and `IAsyncDocumentQuery<T>`.

Use this provider when your resolver returns Raven-backed query objects. If your Raven access layer already returns a sliced result, return a manual `Connection<T>` or `CollectionSegment<T>` instead.

# Select a provider by name

Assign names to providers when multiple providers can handle similar source types, when a field must use a specific integration, or when migration code keeps old and new providers side by side.

```csharp
builder
    .AddGraphQL()
    .AddMongoDbPagingProviders(providerName: "MongoDB");
```

You can select the named provider using attributes:

```csharp
[QueryType]
public static partial class ProductQueries
{
    [UsePaging(ProviderName = "MongoDB")]
    public static IExecutable<Product> GetProducts(IMongoCollection<Product> collection)
    {
        return collection.AsExecutable();
    }

    [UseOffsetPaging(ProviderName = "MongoDB")]
    public static IExecutable<Product> GetProductsOffset(IMongoCollection<Product> collection)
    {
        return collection.AsExecutable();
    }
}
```

Or select it with the fluent API:

```csharp
using HotChocolate.Types;
using HotChocolate.Types.Pagination;

public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("products")
            .UsePaging(options: new PagingOptions { ProviderName = "MongoDB" });

        descriptor
            .Field("productsOffset")
            .UseOffsetPaging(options: new PagingOptions { ProviderName = "MongoDB" });
    }
}
```

Provider names must match the registration exactly, including casing. Selecting by name does not replace the need to register the provider package.

# Set the default provider

Setting `defaultProvider: true` places the provider at the beginning of the registered provider list for that paging style.

```csharp
builder
    .AddGraphQL()
    .AddMongoDbPagingProviders(defaultProvider: true);
```

For custom providers, you can set the cursor and offset defaults independently:

```csharp
builder
    .AddGraphQL()
    .AddCursorPagingProvider<SearchCursorPagingProvider>(
        providerName: "Search",
        defaultProvider: true)
    .AddOffsetPagingProvider<SearchOffsetPagingProvider>(
        providerName: "Search",
        defaultProvider: true);
```

In multi-database schemas, use an explicit `ProviderName` on important fields. A default provider is helpful as a fallback, but naming a field clarifies which provider is intended.

# Return manual paging results when the data is already paged

You do not need a provider if your resolver or service already applies paging and can return Hot Chocolate paging result types.

For cursor fields, return `Connection<T>` when your service provides items, cursors, page flags, and an optional total count:

```csharp
using HotChocolate.Types.Pagination;

[QueryType]
public static partial class ProductQueries
{
    [UsePaging]
    public static async Task<Connection<Product>> GetProductsAsync(
        string? after,
        int? first,
        ProductSearchClient search,
        CancellationToken cancellationToken)
    {
        var page = await search.SearchAsync(after, first, cancellationToken);
        var edges = page.Items
            .Select(item => new Edge<Product>(item, item.Cursor))
            .ToArray();

        return new Connection<Product>(
            edges,
            new ConnectionPageInfo(
                page.HasNextPage,
                page.HasPreviousPage,
                edges.FirstOrDefault()?.Cursor,
                edges.LastOrDefault()?.Cursor),
            page.TotalCount);
    }
}
```

For offset fields, return `CollectionSegment<T>` when your service uses `skip` and `take`:

```csharp
using HotChocolate.Types.Pagination;

[QueryType]
public static partial class ProductQueries
{
    [UseOffsetPaging]
    public static async Task<CollectionSegment<Product>> GetProductsOffsetAsync(
        int? skip,
        int? take,
        ProductSearchClient search,
        CancellationToken cancellationToken)
    {
        var page = await search.SearchOffsetAsync(skip ?? 0, take ?? 10, cancellationToken);

        return new CollectionSegment<Product>(
            page.Items,
            new CollectionSegmentInfo(page.HasNextPage, page.HasPreviousPage),
            page.TotalCount);
    }
}
```

If your service uses GreenDonut DataLoader, you can use `Page<T>`, `PagingArguments`, and `.ToConnectionAsync()` from the `GreenDonut.Data` namespace to convert a service-layer page into a `Connection<T>`. The `PageConnection<T>` type in `HotChocolate.Types.Pagination` wraps a GreenDonut `Page<T>` when that fits your resolver model.

# DataLoader and database performance

Paging is most efficient when the provider can apply the requested window directly in the backend query.

- Return query shapes that the provider supports from your resolvers. Avoid materializing the entire result set before paging.
- Always add a deterministic order before paging. Use a unique tie breaker, such as `Id`, when sorting by non-unique fields.
- Register filtering, sorting, and projection middleware for the data source, rather than performing these operations in memory after paging.
- Use DataLoader for child object lookups from page nodes. Avoid triggering N database queries for node details from a single paged parent field.
- Set page-size limits in [Paging options](paging-options.md) that align with your database indexes and query plans.
- For MongoDB and RavenDB, return integration-backed query sources so the provider can translate paging into the database operation.

# Custom providers are advanced

Create a custom provider when you have a reusable backend query abstraction that appears on many fields and can push paging into that backend.

A cursor provider derives from `CursorPagingProvider`:

```csharp
using HotChocolate.Internal;
using HotChocolate.Types.Pagination;

public sealed class SearchCursorPagingProvider : CursorPagingProvider
{
    public override bool CanHandle(IExtendedType source)
    {
        return source.Source.IsGenericType
            && source.Source.GetGenericTypeDefinition() == typeof(SearchQuery<>);
    }

    protected override CursorPagingHandler CreateHandler(
        IExtendedType source,
        PagingOptions options)
    {
        return SearchCursorPagingHandler.Create(source, options);
    }
}
```

An offset provider derives from `OffsetPagingProvider`:

```csharp
using HotChocolate.Internal;
using HotChocolate.Types.Pagination;

public sealed class SearchOffsetPagingProvider : OffsetPagingProvider
{
    public override bool CanHandle(IExtendedType source)
    {
        return source.Source.IsGenericType
            && source.Source.GetGenericTypeDefinition() == typeof(SearchQuery<>);
    }

    protected override OffsetPagingHandler CreateHandler(
        IExtendedType source,
        PagingOptions options)
    {
        return SearchOffsetPagingHandler.Create(source, options);
    }
}
```

Register and select the provider:

```csharp
builder
    .AddGraphQL()
    .AddCursorPagingProvider<SearchCursorPagingProvider>(providerName: "Search")
    .AddOffsetPagingProvider<SearchOffsetPagingProvider>(providerName: "Search");
```

```csharp
[UsePaging(ProviderName = "Search")]
public static SearchQuery<Product> GetProducts(ProductSearchClient search)
{
    return search.Products();
}
```

Keep `CanHandle` focused so your custom provider does not capture unrelated fields. The handler should translate paging arguments into backend operations, not load the entire dataset and slice it in memory.

# Provider API reference

| API                                                                                 | Style             | Package                             | Registers                           | Key notes                                                             |
| ----------------------------------------------------------------------------------- | ----------------- | ----------------------------------- | ----------------------------------- | --------------------------------------------------------------------- |
| `AddQueryableCursorPagingProvider(providerName, defaultProvider, inlineTotalCount)` | Cursor            | Core Hot Chocolate types            | `QueryableCursorPagingProvider`     | Optional explicit registration for the default cursor path.           |
| `AddQueryableOffsetPagingProvider(providerName, defaultProvider)`                   | Offset            | Core Hot Chocolate types            | `QueryableOffsetPagingProvider`     | Optional explicit registration for the default offset path.           |
| `AddCursorPagingProvider<TProvider>(...)`                                           | Cursor            | Core Hot Chocolate types            | Custom `CursorPagingProvider`       | `TProvider` derives from `CursorPagingProvider`.                      |
| `AddOffsetPagingProvider<TProvider>(...)`                                           | Offset            | Core Hot Chocolate types            | Custom `OffsetPagingProvider`       | `TProvider` derives from `OffsetPagingProvider`.                      |
| `AddDbContextCursorPagingProvider(providerName, defaultProvider)`                   | Cursor            | `HotChocolate.Data.EntityFramework` | EF cursor provider                  | Cursor only, native keyset pagination.                                |
| `AddMongoDbPagingProviders(providerName, defaultProvider)`                          | Cursor and offset | `HotChocolate.Data.MongoDb`         | MongoDB cursor and offset providers | Supports MongoDB executable, aggregate, find, and collection sources. |
| `AddRavenPagingProviders(providerName, defaultProvider)`                            | Cursor and offset | `HotChocolate.Data.Raven`           | Raven cursor and offset providers   | Default provider name is the Raven provider name constant.            |

All registration extension methods are in the `Microsoft.Extensions.DependencyInjection` namespace, so you can use them in your GraphQL builder chain after referencing the package.

# Troubleshooting

| Symptom                                                                                 | Likely cause                                                                    | Fix                                                                                                                                                          |
| --------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| A field uses the default queryable provider instead of a database integration provider. | The integration provider was not registered, or another provider matched first. | Register the integration provider and use `ProviderName` or `defaultProvider: true` where needed.                                                            |
| A named provider is not found.                                                          | The field option and registration name differ.                                  | Use the exact same provider name and casing.                                                                                                                 |
| A named provider is selected but the source type fails later.                           | Named lookup selects by name and does not use `CanHandle` for compatibility.    | Return a supported source type or choose the provider that owns the source.                                                                                  |
| Cursor registration does not affect an offset field.                                    | Cursor and offset providers are separate.                                       | Register the matching offset provider or change the field to `[UsePaging]`.                                                                                  |
| MongoDB paging runs in memory or cannot handle the source.                              | The resolver returned a non-MongoDB source or the provider was not registered.  | Register `.AddMongoDbPagingProviders()` and return `AsExecutable()`, `IAggregateFluent<T>`, `IFindFluent<TDocument, TProjection>`, or `IMongoCollection<T>`. |
| EF Core cursor paging does not use native keyset pagination.                            | The EF provider was not registered or another default provider was chosen.      | Register `.AddDbContextCursorPagingProvider()` and use a named provider when multiple cursor providers can match.                                            |
| Nullable cursor keys fail for a database provider.                                      | `NullOrdering` is unspecified for a provider that cannot be inferred.           | Set `NullOrdering` in [Paging options](paging-options.md).                                                                                                   |

# Next steps

- [Cursor connections](connections-cursor.md): Learn about connection shape, stable ordering, cursors, `totalCount`, and nullable cursor keys.
- [Offset pagination](offset.md): Explore `skip`, `take`, `CollectionSegment<T>`, and offset trade-offs.
- [Paging options](paging-options.md): Review global defaults, field overrides, provider names, and `NullOrdering`.
- [Pagination](index.md): Start here to choose between cursor and offset pagination.
- [MongoDB integration](/docs/hotchocolate/v16/_leagcy/integrations/mongodb): See how to use MongoDB `AsExecutable()`, filtering, sorting, and projections.
