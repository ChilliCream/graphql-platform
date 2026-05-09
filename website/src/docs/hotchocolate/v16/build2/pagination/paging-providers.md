---
title: Paging providers
---

Paging middleware gives a field pagination arguments and a GraphQL result shape. A paging provider decides how those arguments are applied to the resolver result.

Use providers when the resolver result is backed by a data source that can page efficiently, such as LINQ, EF Core, MongoDB, or RavenDB. If your resolver already calls a paged service or external API, return a manual paging result instead of writing a provider.

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

This page covers provider selection and registration. For field shapes and client requests, see [Cursor connections](connections-cursor.md) and [Offset pagination](offset.md). For page sizes, names, null ordering, and global defaults, see [Paging options](paging-options.md).

# Choose a provider

| Resolver source                           | Paging style      | Provider                           | Registration                                                                                         | Notes                                                                                                     |
| ----------------------------------------- | ----------------- | ---------------------------------- | ---------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------- |
| `IQueryable<T>` or common collection      | Cursor            | Built-in queryable cursor provider | None required, or `.AddQueryableCursorPagingProvider(...)` when naming or default ordering is needed | Use a deterministic order for database-backed queries.                                                    |
| `IQueryable<T>` or common collection      | Offset            | Built-in queryable offset provider | None required, or `.AddQueryableOffsetPagingProvider(...)` when naming or default ordering is needed | Applies offset paging through the queryable path when possible.                                           |
| EF Core `IQueryable<T>`                   | Cursor            | EF Core cursor provider            | `.AddDbContextCursorPagingProvider()`                                                                | Requires `HotChocolate.Data.EntityFramework`. Uses native keyset pagination.                              |
| EF Core `IQueryable<T>`                   | Offset            | Built-in queryable offset provider | None required in most schemas                                                                        | v16 has no EF-specific offset provider registration helper in this source snapshot.                       |
| MongoDB executable or driver query source | Cursor and offset | MongoDB providers                  | `.AddMongoDbPagingProviders()`                                                                       | Requires `HotChocolate.Data.MongoDb`. Supports Mongo executable, aggregate, find, and collection sources. |
| RavenDB query source                      | Cursor and offset | Raven providers                    | `.AddRavenPagingProviders()`                                                                         | Requires `HotChocolate.Data.Raven`. Supports Raven query abstractions.                                    |
| Reusable custom backend query abstraction | Cursor or offset  | Custom provider                    | `.AddCursorPagingProvider<TProvider>()` or `.AddOffsetPagingProvider<TProvider>()`                   | Advanced extension point. Prefer manual results for one field or one external API call.                   |

The default queryable providers are enough when the resolver returns `IQueryable<T>`, `IEnumerable<T>`, or a common collection and Hot Chocolate can apply paging after the resolver returns the source. Add an integration provider when it can push paging into a specific backend API or when source inference would otherwise pick the wrong provider.

# How provider selection works

Hot Chocolate resolves cursor and offset providers separately.

1. During schema completion, Hot Chocolate determines the resolver source type.
2. If `PagingOptions.ProviderName` is set, it searches registered providers for that exact name.
3. If `ProviderName` is not set, it picks the first registered provider whose `CanHandle(source)` returns `true`.
4. If registered providers exist but none match, it uses the first registered provider as the fallback.
5. If no providers were registered for that paging style, it creates the built-in queryable provider.

Provider names are exact and case-sensitive. Named lookup selects by name first, so use a name only with fields whose source type is supported by that provider.

Application services are checked before schema services. This is the provider scope that v16 exposes during resolution. There is no separate field option for a provider scope in the public paging options. Use `ProviderName` on the field when a field must choose one registered provider. A provider registered with `defaultProvider: true` is inserted first, which makes it the preferred fallback for that paging style.

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

Install the EF Core data integration package and register the cursor provider when EF Core fields should use native keyset pagination.

```csharp
builder
    .AddGraphQL()
    .AddQueryType()
    .AddSorting()
    .AddDbContextCursorPagingProvider();
```

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

The field still exposes the normal connection shape. The provider changes how paging is applied to the EF query. Keep ordering stable, especially when clients use cursors over changing data. Sorting and cursor behavior are covered in [Cursor connections](connections-cursor.md).

The EF Core helper registers a cursor provider only. For offset fields over EF `IQueryable<T>`, use `[UseOffsetPaging]` and the built-in queryable offset provider unless your schema provides a custom offset provider.

# Register MongoDB paging providers

Install `HotChocolate.Data.MongoDb`, register the providers, and return a MongoDB-backed source.

```csharp
builder
    .AddGraphQL()
    .AddMongoDbPagingProviders();
```

Cursor field:

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

Offset field:

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

`.AddMongoDbPagingProviders()` registers cursor and offset providers with the same optional name and default setting. The providers handle MongoDB executable sources, `IAggregateFluent<T>`, `IFindFluent<TDocument, TProjection>`, and `IMongoCollection<T>`.

If you return `IExecutable<T>`, make sure it is backed by the MongoDB integration. A generic executable from another backend is not a MongoDB provider source.

# Register Raven paging providers

Raven paging support is available through `HotChocolate.Data.Raven`.

```csharp
builder
    .AddGraphQL()
    .AddRavenPagingProviders();
```

The helper registers both Raven cursor and offset providers and registers the document store integration. The provider source checks cover `IRavenQueryable<T>` and `IAsyncDocumentQuery<T>`.

Use this provider when your resolver returns Raven-backed query objects. If your Raven access layer already returns a sliced result, return a manual `Connection<T>` or `CollectionSegment<T>` instead.

# Select a provider by name

Name providers when multiple providers can handle similar source shapes, when a field must use a specific integration, or when migration code keeps old and new providers side by side.

```csharp
builder
    .AddGraphQL()
    .AddMongoDbPagingProviders(providerName: "MongoDB");
```

Select the named provider with attributes:

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

Names must match the registration exactly, including casing. Named selection does not replace provider package registration.

# Set the default provider

`defaultProvider: true` puts the provider at the start of the registered provider list for that paging style.

```csharp
builder
    .AddGraphQL()
    .AddMongoDbPagingProviders(defaultProvider: true);
```

For custom providers, set the cursor and offset defaults independently.

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

Use explicit `ProviderName` on important fields in multi-database schemas. A default is useful as a fallback, but a named field documents the intended provider.

# Return manual paging results when the data is already paged

You do not need a provider when the resolver or service already applies paging and can return Hot Chocolate paging result types.

Return `Connection<T>` for cursor fields when the service returns items, cursors, page flags, and an optional total count.

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

Return `CollectionSegment<T>` for offset fields when your service uses `skip` and `take`.

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

For services that use GreenDonut DataLoader, `Page<T>`, `PagingArguments`, and `.ToConnectionAsync()` from the `GreenDonut.Data` namespace can turn a service-layer page into a `Connection<T>`. `PageConnection<T>` in `HotChocolate.Types.Pagination` wraps a GreenDonut `Page<T>` when that fits your resolver model.

# DataLoader and database performance

Paging works best when the provider can apply the requested window in the backend query.

- Return provider-supported query shapes from resolvers. Avoid materializing the full result set before paging.
- Add a deterministic order before paging. Use a unique tie breaker such as `Id` when sorting by non-unique fields.
- Register filtering, sorting, and projection middleware for the data source instead of doing those operations after paging in memory.
- Use DataLoader for child object lookups from page nodes. Do not use one paged parent field to trigger N database queries for node details.
- Keep page-size limits in [Paging options](paging-options.md) aligned with database indexes and query plans.
- For MongoDB and RavenDB, return integration-backed query sources so the provider can translate paging into the database operation.

# Custom providers are advanced

Write a provider when you have a reusable backend query abstraction that appears on many fields and can push paging into that backend.

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

Keep `CanHandle` narrow so a custom provider does not capture unrelated fields. The handler should translate paging arguments to backend operations. It should not load the whole dataset and slice it in memory.

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

Registration extension methods are in the `Microsoft.Extensions.DependencyInjection` namespace, so normal GraphQL builder chains can call them after the package is referenced.

# Troubleshooting

| Symptom                                                                                 | Likely cause                                                                    | Fix                                                                                                                                                          |
| --------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| A field uses the default queryable provider instead of a database integration provider. | The integration provider was not registered, or another provider matched first. | Register the integration provider and use `ProviderName` or `defaultProvider: true` where needed.                                                            |
| A named provider is not found.                                                          | The field option and registration name differ.                                  | Use the exact same provider name and casing.                                                                                                                 |
| A named provider is selected but the source type fails later.                           | Named lookup selects by name and does not use `CanHandle` for compatibility.    | Return a supported source type or choose the provider that owns the source.                                                                                  |
| Cursor registration does not affect an offset field.                                    | Cursor and offset providers are separate.                                       | Register the matching offset provider or change the field to `[UsePaging]`.                                                                                  |
| MongoDB paging runs in memory or cannot handle the source.                              | The resolver returned a non-MongoDB source or the provider was not registered.  | Register `.AddMongoDbPagingProviders()` and return `AsExecutable()`, `IAggregateFluent<T>`, `IFindFluent<TDocument, TProjection>`, or `IMongoCollection<T>`. |
| EF Core cursor paging does not use native keyset pagination.                            | The EF provider was not registered or another default provider was chosen.      | Register `.AddDbContextCursorPagingProvider()` and use a named provider when multiple cursor providers can match.                                            |
| Nullable cursor keys fail for a database provider.                                      | `NullOrdering` is unspecified for a provider that cannot be inferred.           | Configure `NullOrdering` in [Paging options](paging-options.md).                                                                                             |

# Next steps

- [Cursor connections](connections-cursor.md): connection shape, stable ordering, cursors, `totalCount`, and nullable cursor keys.
- [Offset pagination](offset.md): `skip`, `take`, `CollectionSegment<T>`, and offset trade-offs.
- [Paging options](paging-options.md): global defaults, field overrides, provider names, and `NullOrdering`.
- [Pagination](index.md): entry point for choosing cursor or offset pagination.
- [MongoDB integration](/docs/hotchocolate/v16/integrations/mongodb): MongoDB `AsExecutable()`, filtering, sorting, and projections.
