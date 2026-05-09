---
title: Offset pagination
---

Offset pagination allows clients to request a specific numeric window within a list. The client specifies how many items to skip and how many to take. Hot Chocolate then returns a collection segment containing the current items and page information.

```graphql
query GetUsersPage {
  users(skip: 40, take: 20) {
    items {
      id
      name
    }
    pageInfo {
      hasNextPage
      hasPreviousPage
    }
  }
}
```

Offset pagination is a good fit when your client needs page numbers, admin grids, or must work with an offset-based data source. For public APIs, infinite scroll, deep traversal, or data that changes frequently while users page through it, prefer [cursor connections](connections-cursor.md).

# How offset pagination works

Offset paging uses two nullable `Int` arguments:

| Argument | Meaning                                           | Default behavior                                                     |
| -------- | ------------------------------------------------- | -------------------------------------------------------------------- |
| `skip`   | Number of results to skip before the page starts. | Omitted values are treated as `0`.                                   |
| `take`   | Number of results requested for the page.         | Uses `DefaultPageSize` when omitted, unless boundaries are required. |

For page-number navigation, calculate the offset in the client:

```text
skip = (page - 1) * pageSize
take = pageSize
```

Hot Chocolate applies `skip`, then requests `take + 1` items from the provider when possible. The extra item tells Hot Chocolate whether `hasNextPage` should be true. `hasPreviousPage` is true when `skip` is greater than `0`.

```text
client: skip=40, take=20
        |
        v
resolver returns ordered IQueryable<User>
        |
        v
provider applies Skip(40).Take(21)
        |
        v
Hot Chocolate returns 20 items and derives hasNextPage from the extra item
```

# Add offset pagination to a resolver

Start with an ordered collection or query. For database-backed fields, return `IQueryable<T>` or another provider-supported source so the provider can apply the offset at the data source.

```csharp
#nullable enable

using HotChocolate;
using HotChocolate.Types;

[QueryType]
public static partial class UserQueries
{
    [UseOffsetPaging]
    public static IQueryable<User> GetUsers(CatalogContext db)
    {
        return db.Users
            .OrderBy(u => u.Name)
            .ThenBy(u => u.Id);
    }
}

public sealed class User
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;
}

public sealed class CatalogContext
{
    private static readonly User[] s_users =
    [
        new() { Id = 1, Name = "Ada" },
        new() { Id = 2, Name = "Grace" }
    ];

    public IQueryable<User> Users => s_users.AsQueryable();
}
```

Expected SDL shape:

```graphql
type Query {
  users(skip: Int, take: Int): UsersCollectionSegment
}

type UsersCollectionSegment {
  items: [User!]
  pageInfo: CollectionSegmentInfo!
}

type CollectionSegmentInfo {
  hasNextPage: Boolean!
  hasPreviousPage: Boolean!
}
```

The segment type name uses the field name by default. A field named `users` becomes `UsersCollectionSegment`, and a field named `userOrders` becomes `UserOrdersCollectionSegment`.

Use the fluent API when your schema configuration lives in an object type:

```csharp
using HotChocolate.Types;

public sealed class Query
{
    public IQueryable<User> GetUsers(CatalogContext db)
    {
        return db.Users
            .OrderBy(u => u.Name)
            .ThenBy(u => u.Id);
    }
}

public sealed class QueryType : ObjectType<Query>
{
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor
            .Field(t => t.GetUsers(default!))
            .UseOffsetPaging();
    }
}
```

# Query pages with skip and take

Request the first page with `skip: 0` and the page size in `take`:

```graphql
query GetFirstUsersPage {
  users(skip: 0, take: 20) {
    items {
      id
      name
    }
    pageInfo {
      hasNextPage
      hasPreviousPage
    }
  }
}
```

Request the next page by increasing `skip` by the page size:

```graphql
query GetSecondUsersPage {
  users(skip: 20, take: 20) {
    items {
      id
      name
    }
    pageInfo {
      hasNextPage
      hasPreviousPage
    }
  }
}
```

A collection segment is not a cursor connection. It has no edges, nodes shortcut, cursors, start cursor, or end cursor. Use the returned `items` and the two page flags for offset navigation.

# Enable total count when the client needs it

`totalCount` is not part of the segment by default. Enable it per field when the client needs the full result size for a workflow such as page-count display.

```csharp
[UseOffsetPaging(IncludeTotalCount = true)]
public static IQueryable<User> GetUsers(CatalogContext db)
{
    return db.Users
        .OrderBy(u => u.Name)
        .ThenBy(u => u.Id);
}
```

Expected SDL addition:

```graphql
type UsersCollectionSegment {
  items: [User!]
  pageInfo: CollectionSegmentInfo!
  totalCount: Int!
}
```

Query `totalCount` only when you need it:

```graphql
query GetUsersWithCount {
  users(skip: 0, take: 20) {
    totalCount
    items {
      id
      name
    }
  }
}
```

For supported sources, Hot Chocolate computes the count only when the client selects `totalCount`. The count can still be expensive for large or heavily filtered data.

# Configure field-level offset options

Set offset options on the field when one list needs different limits or behavior.

```csharp
[UseOffsetPaging(
    DefaultPageSize = 25,
    MaxPageSize = 100,
    RequirePagingBoundaries = true,
    IncludeTotalCount = true)]
public static IQueryable<User> GetUsers(CatalogContext db)
{
    return db.Users
        .OrderBy(u => u.Name)
        .ThenBy(u => u.Id);
}
```

Use `PagingOptions` with the fluent API:

```csharp
using HotChocolate.Types;
using HotChocolate.Types.Pagination;

descriptor
    .Field(t => t.GetUsers(default!))
    .UseOffsetPaging(
        options: new PagingOptions
        {
            DefaultPageSize = 25,
            MaxPageSize = 100,
            RequirePagingBoundaries = true,
            IncludeTotalCount = true
        });
```

| Option                                | Default                       | Use it for                                                                         |
| ------------------------------------- | ----------------------------- | ---------------------------------------------------------------------------------- |
| `Type`                                | Inferred                      | Override the item GraphQL type when inference is not enough.                       |
| `CollectionSegmentName`               | Generated from the field name | Set an explicit segment type name for this field.                                  |
| `DefaultPageSize`                     | `10`                          | Choose the page size used when `take` is omitted.                                  |
| `MaxPageSize`                         | `50`                          | Cap the largest accepted `take` value.                                             |
| `IncludeTotalCount`                   | `false`                       | Add `totalCount: Int!` to the segment.                                             |
| `RequirePagingBoundaries`             | `false`                       | Require clients to pass `take`.                                                    |
| `ProviderName`                        | Inferred provider             | Select a registered named provider.                                                |
| `InferCollectionSegmentNameFromField` | `true`                        | Keep v16 field-name segment naming. Set `false` only for legacy type-based naming. |

Use [Paging options](paging-options.md) for global defaults and shared paging configuration.

# Use provider-friendly database sources

Return a source shape that lets the paging provider slice before data is materialized.

| Resolver result        | Use it when                                                         |
| ---------------------- | ------------------------------------------------------------------- |
| `IQueryable<T>`        | A LINQ provider can translate offset operations to the data source. |
| `IExecutable<T>`       | An integration exposes an executable query, for example MongoDB.    |
| `IEnumerable<T>`       | The collection is already in memory and bounded.                    |
| `CollectionSegment<T>` | Another service already produced an offset page.                    |

For Entity Framework or another LINQ provider, keep the query ordered and let the provider translate the offset:

```csharp
[UseOffsetPaging(IncludeTotalCount = true)]
public static IQueryable<User> GetUsers(CatalogContext db)
{
    return db.Users
        .AsNoTracking()
        .OrderBy(u => u.Name)
        .ThenBy(u => u.Id);
}
```

Add indexes that match your filters and sort order when you expect offset pages over database tables. Deep offsets can be slow because many databases still scan skipped rows.

Select a named provider when provider inference is not enough:

```csharp
[UseOffsetPaging(ProviderName = "Products")]
public static IQueryable<Product> GetProducts(ProductContext db)
{
    return db.Products
        .OrderBy(p => p.Name)
        .ThenBy(p => p.Id);
}
```

Use [Paging providers](paging-providers.md) for provider registration and provider-specific setup.

# Keep ordering stable

Offset paging relies on the order staying deterministic between requests. If two items can have the same primary sort value, add a unique final sort key.

```csharp
public static IQueryable<User> GetUsers(CatalogContext db)
{
    return db.Users
        .OrderBy(u => u.Name)
        .ThenBy(u => u.Id);
}
```

Changing data can still repeat or skip rows between pages. For example, a new row inserted before the current offset can move all later rows. If the UI must traverse a growing or high-write list, use [cursor connections](connections-cursor.md).

# Combine offset paging with data middleware

Use this order when you combine offset paging with projections, filtering, and sorting:

```csharp
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;

[QueryType]
public static partial class UserQueries
{
    [UseOffsetPaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<User> GetUsers(CatalogContext db)
    {
        return db.Users;
    }
}
```

The order is `UseOffsetPaging`, then `UseProjection`, then `UseFiltering`, then `UseSorting`. Field middleware is invoked in declaration order, then the resolver result flows back through middleware in reverse order. Filtering and sorting shape the query before offset paging creates the segment.

Read more:

- [Projections](/docs/hotchocolate/v16/build/filtering-sorting-projections/projection-options)
- [Filtering](/docs/hotchocolate/v16/build/filtering-sorting-projections/filter-types)
- [Sorting](/docs/hotchocolate/v16/build/filtering-sorting-projections/sort-types)

# Return a custom CollectionSegment

Return `CollectionSegment<T>` when an external API or service already applies offset paging.

```csharp
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;

[QueryType]
public static partial class UserQueries
{
    [UseOffsetPaging(IncludeTotalCount = true)]
    public static async Task<CollectionSegment<User>> GetUsersAsync(
        int? skip,
        int? take,
        UserService service,
        CancellationToken cancellationToken)
    {
        var effectiveSkip = skip ?? 0;
        var effectiveTake = take ?? 10;

        var page = await service.GetUsersAsync(
            effectiveSkip,
            effectiveTake,
            cancellationToken);

        return new CollectionSegment<User>(
            page.Items,
            new CollectionSegmentInfo(page.HasNextPage, effectiveSkip > 0),
            page.TotalCount);
    }
}
```

The v16 constructor is `new CollectionSegment<T>(items, info, totalCount)`. Match resolver parameters to the GraphQL argument names `skip` and `take`. If you delegate to another service, apply the same defaults and maximum page size that your GraphQL field exposes.

For advanced custom fields, `OffsetPagingArguments` represents the nullable runtime `Skip` and `Take` values, `ApplyOffsetPaginationAsync` can apply offset pagination from an `IResolverContext`, and `AddOffsetPagingArguments()` adds the `skip` and `take` arguments during manual field setup.

# Troubleshoot offset paging

| Symptom                                    | Cause                                                                   | Fix                                                                                    |
| ------------------------------------------ | ----------------------------------------------------------------------- | -------------------------------------------------------------------------------------- |
| A query without `take` fails.              | `RequirePagingBoundaries = true`.                                       | Pass `take` or change the field option.                                                |
| A query fails when `take` is large.        | `take` is greater than `MaxPageSize`.                                   | Lower `take` or raise `MaxPageSize` intentionally.                                     |
| `totalCount` is missing from the schema.   | `IncludeTotalCount` is `false`.                                         | Enable `IncludeTotalCount` on the field.                                               |
| `totalCount` is slow.                      | The provider runs a count over large or filtered data.                  | Enable counts only where needed and select `totalCount` only when the client needs it. |
| Items repeat or disappear between pages.   | The order is nondeterministic or data changed between requests.         | Add a stable sort with a unique tie-breaker, or use cursor pagination.                 |
| Deep pages are slow.                       | Large `skip` values can force the data source to scan skipped rows.     | Cap page depth, add matching indexes, or use cursor pagination.                        |
| The wrong provider handles the field.      | Inference chose a different provider or the source type is unsupported. | Return a supported source or set `ProviderName`.                                       |
| Filtering or sorting changes page results. | The logical list changed before offset paging sliced it.                | Reset to the first page when filter or sort arguments change.                          |

# Choose cursor connections when offsets do not fit

Use [cursor connections](connections-cursor.md) instead of offset paging when clients need stable traversal over large or changing data. Cursor pagination fits public APIs, feeds, catalogs, search results, load more, infinite scroll, and Relay-style clients. Offset paging fits small bounded lists, page-number UIs, and data sources that already use numeric offsets.

# Where to go next

- Configure shared limits and defaults: [Paging options](paging-options.md).
- Register or select providers: [Paging providers](paging-providers.md).
- Build cursor-based traversal: [Cursor connections](connections-cursor.md).
- Compose the field with [projections](/docs/hotchocolate/v16/build/filtering-sorting-projections/projection-options), [filtering](/docs/hotchocolate/v16/build/filtering-sorting-projections/filter-types), and [sorting](/docs/hotchocolate/v16/build/filtering-sorting-projections/sort-types).
