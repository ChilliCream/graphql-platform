---
title: Pagination
---

Pagination turns a collection field into a bounded contract between your schema, resolver, data source, and client. Instead of asking for every product, the client asks for one window of products and receives navigation metadata for the next request.

```graphql
# Unbounded list
products: [Product!]!

# Bounded cursor connection
products(first: 10, after: $cursor): ProductsConnection
```

Use this page as the entry point for Hot Chocolate v16 pagination. It helps you choose a model, enable paging, compose it with data middleware, and move to the focused child pages.

```text
resolver source -> paging arguments -> paging provider -> connection or segment -> client navigation
```

# Choose cursor pagination or offset pagination

Hot Chocolate v16 has two paging models.

| Model              | Enable it with                              | Client arguments                   | Schema shape                                                  | Use it for                                                                                                         |
| ------------------ | ------------------------------------------- | ---------------------------------- | ------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------ |
| Cursor connections | `[UsePaging]` or `.UsePaging()`             | `first`, `after`, `last`, `before` | `ProductsConnection`, `ProductsEdge`, `PageInfo`              | Public APIs, feeds, catalogs, search results, load more, infinite scroll, Relay-style clients, changing data       |
| Offset paging      | `[UseOffsetPaging]` or `.UseOffsetPaging()` | `skip`, `take`                     | `ProductsCollectionSegment`, `items`, `CollectionSegmentInfo` | Small bounded admin lists, compatibility with offset-based data sources, cases where offset semantics are required |

Prefer cursor pagination for public and growing APIs. Cursor pagination works with opaque positions and stable ordering, which makes it a better fit for data that can change while a user pages through it.

Offset paging is familiar for page-number UIs, but deep offsets can become slow and changing data can cause repeated or skipped items. Use it when those trade-offs match the field.

# Add cursor pagination to a field

Start with a resolver that returns an ordered collection. For database-backed fields, return `IQueryable<T>` or another provider-supported query shape so paging can translate to the data source.

```csharp
#nullable enable

using HotChocolate;
using HotChocolate.Types;

[QueryType]
public static partial class ProductQueries
{
    [UsePaging]
    public static IQueryable<Product> GetProducts(ProductStore store)
    {
        return store.Products.OrderBy(p => p.Id);
    }
}

public sealed class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;
}

public sealed class ProductStore
{
    private static readonly Product[] s_products =
    [
        new() { Id = 1, Name = "Banana" },
        new() { Id = 2, Name = "Coffee" }
    ];

    public IQueryable<Product> Products => s_products.AsQueryable();
}
```

Expected SDL shape:

```graphql
type Query {
  products(
    first: Int
    after: String
    last: Int
    before: String
  ): ProductsConnection
}

type ProductsConnection {
  pageInfo: PageInfo!
  edges: [ProductsEdge!]
  nodes: [Product!]
}

type ProductsEdge {
  cursor: String!
  node: Product!
}

type PageInfo {
  hasNextPage: Boolean!
  hasPreviousPage: Boolean!
  startCursor: String
  endCursor: String
}
```

A client can render nodes and keep the next cursor:

```graphql
query GetProducts($first: Int!, $after: String) {
  products(first: $first, after: $after) {
    nodes {
      id
      name
    }
    pageInfo {
      hasNextPage
      endCursor
    }
  }
}
```

Use `edges` when the client needs item cursors or edge fields:

```graphql
query GetProductsWithCursors($first: Int!) {
  products(first: $first) {
    edges {
      cursor
      node {
        id
        name
      }
    }
  }
}
```

Cursors are opaque. Store and pass them back, but do not parse them.

Continue with [Cursor connections](connections-cursor.md) for field-level setup, connection results, and cursor behavior.

# Add offset pagination to a field

Use offset paging when the client needs `skip` and `take`.

```csharp
using HotChocolate;
using HotChocolate.Types;

[QueryType]
public static partial class ProductQueries
{
    [UseOffsetPaging]
    public static IQueryable<Product> GetProducts(ProductStore store)
    {
        return store.Products.OrderBy(p => p.Id);
    }
}
```

Expected SDL shape:

```graphql
type Query {
  products(skip: Int, take: Int): ProductsCollectionSegment
}

type ProductsCollectionSegment {
  items: [Product!]
  pageInfo: CollectionSegmentInfo!
}

type CollectionSegmentInfo {
  hasNextPage: Boolean!
  hasPreviousPage: Boolean!
}
```

A client can request a page by offset:

```graphql
query GetProducts {
  products(skip: 20, take: 10) {
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

Continue with [Offset paging](offset.md) for `skip`, `take`, segment behavior, and offset-specific caveats.

# Configure paging with fluent types

Use the fluent API when schema configuration lives outside the resolver class.

```csharp
using HotChocolate.Types;

public sealed class Query
{
    public IQueryable<Product> GetProducts(ProductStore store)
    {
        return store.Products.OrderBy(p => p.Id);
    }
}

public sealed class QueryType : ObjectType<Query>
{
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor
            .Field(t => t.GetProducts(default!))
            .UsePaging();
    }
}
```

Use `.UseOffsetPaging()` for an offset field:

```csharp
descriptor
    .Field(t => t.GetProducts(default!))
    .UseOffsetPaging();
```

# Return a provider-friendly resolver shape

Paging providers adapt resolver results to the schema shape. Choose a result type that lets the provider slice data before it is materialized.

| Resolver result        | Use it when                                                                              |
| ---------------------- | ---------------------------------------------------------------------------------------- |
| `IQueryable<T>`        | A database provider can translate paging into source queries.                            |
| `IExecutable<T>`       | An integration exposes an executable query, for example MongoDB.                         |
| `IEnumerable<T>`       | The collection is already in memory and bounded.                                         |
| `Connection<T>`        | Your service layer, external API, or custom cursor logic already produced a cursor page. |
| `CollectionSegment<T>` | Your service layer already produced an offset page.                                      |

Avoid returning a large materialized list from a database query. Let the provider apply paging, filtering, and sorting at the data source when possible.

For nested node data, use DataLoader to batch lookups instead of issuing one database call per row. Paging controls how many nodes enter the field result. DataLoader controls how related data is loaded for those nodes.

# Keep ordering stable

Cursor pagination needs deterministic ordering. If two items can have the same primary sort value, add a unique final sort key.

```csharp
public static IQueryable<Product> GetProducts(ProductStore store)
{
    return store.Products
        .OrderBy(p => p.Name)
        .ThenBy(p => p.Id);
}
```

Filtering and sorting change the logical list. A cursor is valid for the same field arguments, filters, and sort order that produced it. When those arguments change, ask for a fresh first page.

# Compose paging with projections, filtering, and sorting

Use this order when you combine paging with the data middleware:

```csharp
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;

[QueryType]
public static partial class ProductQueries
{
    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Product> GetProducts(ProductStore store)
    {
        return store.Products.OrderBy(p => p.Id);
    }
}
```

The declaration order can look reversed. Field middleware is invoked in declaration order, then the resolver result flows back through the middleware in reverse order. Filtering and sorting transform the query before paging creates the connection window.

Read more:

- [Field middleware](/docs/hotchocolate/v16/execution-engine/field-middleware)
- [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections)
- [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering)
- [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting)

# Set paging limits and defaults

Hot Chocolate uses conservative defaults for paged fields.

| Option                    | Default | Why it matters                                            |
| ------------------------- | ------- | --------------------------------------------------------- |
| `DefaultPageSize`         | `10`    | Used when the client does not provide a boundary.         |
| `MaxPageSize`             | `50`    | Caps requested page sizes.                                |
| `IncludeTotalCount`       | `false` | Adds `totalCount` when enabled.                           |
| `AllowBackwardPagination` | `true`  | Enables `last` and `before` on cursor fields.             |
| `RequirePagingBoundaries` | `false` | Requires clients to pass a page boundary when enabled.    |
| `IncludeNodesField`       | `true`  | Adds the shortcut `nodes` field on connections.           |
| `EnableRelativeCursors`   | `false` | Enables relative cursor behavior for supported scenarios. |

Set shared defaults with `ModifyPagingOptions`:

```csharp
builder
    .AddGraphQL()
    .ModifyPagingOptions(options =>
    {
        options.DefaultPageSize = 25;
        options.MaxPageSize = 100;
        options.RequirePagingBoundaries = true;
    });
```

Use local options for one field:

```csharp
[UsePaging(DefaultPageSize = 25, MaxPageSize = 100, IncludeTotalCount = true)]
public static IQueryable<Product> GetProducts(ProductStore store)
{
    return store.Products.OrderBy(p => p.Id);
}
```

Continue with [Paging options](paging-options.md) for option details, naming, `IncludeNodesField`, `NullOrdering`, relative cursors, and public API limits.

# Enable total count intentionally

`totalCount` is disabled by default. Enable it when clients need the total size for a workflow.

```csharp
[UsePaging(IncludeTotalCount = true)]
public static IQueryable<Product> GetProducts(ProductStore store)
{
    return store.Products.OrderBy(p => p.Id);
}
```

Expected SDL addition:

```graphql
type ProductsConnection {
  pageInfo: PageInfo!
  edges: [ProductsEdge!]
  nodes: [Product!]
  totalCount: Int!
}
```

Client query:

```graphql
query GetProducts {
  products(first: 10) {
    totalCount
    nodes {
      id
      name
    }
  }
}
```

Hot Chocolate only computes the count when the client requests `totalCount`, but the count can add work for the selected provider. When you return `Connection<T>` yourself, provide count support through the connection rather than expecting the provider to infer it.

# Select a paging provider

Providers connect the same schema model to different source shapes, including `IQueryable<T>`, `IEnumerable<T>`, `IExecutable<T>`, EF Core, MongoDB, Raven, and custom sources.

When `ProviderName` is not set, Hot Chocolate selects a provider from the source type. If no provider can be inferred, it uses the first registered provider. Set `ProviderName` when multiple providers are registered or when the field should use a specific provider.

```csharp
builder
    .AddGraphQL()
    .AddMongoDbPagingProviders(providerName: "MongoDB");
```

```csharp
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using MongoDB.Driver;

[QueryType]
public static partial class ProductQueries
{
    [UsePaging(ProviderName = "MongoDB")]
    public static IExecutable<Product> GetProducts(IMongoCollection<Product> products)
    {
        return products.AsExecutable();
    }
}
```

Continue with [Paging providers](paging-providers.md) for provider registration, provider names, EF Core, MongoDB, Raven, and custom providers.

Related integrations:

- [Entity Framework](/docs/hotchocolate/v16/integrations/entity-framework)
- [MongoDB](/docs/hotchocolate/v16/integrations/mongodb)

# Protect public APIs

Pagination gives every collection field a cost boundary. For public APIs, keep `MaxPageSize` conservative, consider `RequirePagingBoundaries`, and review how query cost is calculated for paged fields.

Read more:

- [Cost analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis)
- [Public API guide](/docs/hotchocolate/v16/guides/public-api)

# Use advanced paging only when you need control

Most fields should return `IQueryable<T>`, `IExecutable<T>`, or a bounded in-memory collection. Use advanced shapes when another layer already owns the page.

- Return `Connection<T>` when a service or external API already produced nodes, cursors, page info, and optional count.
- Use service-layer paging with `PagingArguments`, `Page<T>`, `.ToConnectionAsync()`, and `.AddPagingArguments()` when paging belongs below the GraphQL resolver.
- Use `[UseConnection]`, `PageConnection<T>`, and `EnableRelativeCursors` for page-bar or relative cursor scenarios.
- Extend connection and edge types when clients need aggregate fields or edge metadata.

See [Cursor connections](connections-cursor.md) and [Paging options](paging-options.md) for the paths that lead into these scenarios. If your client follows Relay conventions, see [Relay support](/docs/hotchocolate/v16/building-a-schema/relay).

# Troubleshoot common paging issues

| Symptom                                                   | Fix                                                                                                              | Read next                                   |
| --------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------- | ------------------------------------------- |
| `totalCount` is missing.                                  | Enable `IncludeTotalCount`. If you return `Connection<T>`, provide count support in that connection.             | [Paging options](paging-options.md)         |
| Pages repeat or skip items.                               | Add deterministic ordering and a unique final sort key such as `Id`.                                             | [Cursor connections](connections-cursor.md) |
| `last` or `before` is unavailable.                        | Check `AllowBackwardPagination`.                                                                                 | [Paging options](paging-options.md)         |
| A query without `first`, `last`, `skip`, or `take` fails. | Check `RequirePagingBoundaries`.                                                                                 | [Paging options](paging-options.md)         |
| Filtering or sorting changes page results.                | Treat cursors as scoped to the same filters and sort arguments. Ask for a new first page after argument changes. | [Cursor connections](connections-cursor.md) |
| The wrong provider handles the field.                     | Register the provider and set `ProviderName` on the field when inference is not enough.                          | [Paging providers](paging-providers.md)     |
| Paging happens in memory for a large database table.      | Return `IQueryable<T>` or `IExecutable<T>` and avoid materializing the list before middleware runs.              | [Paging providers](paging-providers.md)     |
| Offset pages are slow or inconsistent.                    | Prefer cursor pagination for large or changing data.                                                             | [Offset paging](offset.md)                  |
| Nullable cursor keys fail with a provider error.          | Set `NullOrdering` in `PagingOptions` to match the database.                                                     | [Paging options](paging-options.md)         |

# Where to go next

- Build a cursor-paginated field: [Cursor connections](connections-cursor.md).
- Use `skip` and `take`: [Offset paging](offset.md).
- Configure limits, defaults, total count, nodes, null ordering, or relative cursors: [Paging options](paging-options.md).
- Register or select a provider: [Paging providers](paging-providers.md).
- Compose with data middleware: [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections), [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering), and [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting).
- Protect public schemas: [Cost analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis) and the [Public API guide](/docs/hotchocolate/v16/guides/public-api).
- Understand Relay conventions: [Relay support](/docs/hotchocolate/v16/building-a-schema/relay).
