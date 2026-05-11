---
title: Pagination
---

Pagination transforms a collection field into a well-defined contract between your schema, resolver, data source, and client. Instead of retrieving every product, the client requests a specific window of products and receives navigation metadata for subsequent requests.

```graphql
# Unbounded list
products: [Product!]!

# Bounded cursor connection
products(first: 10, after: $cursor): ProductsConnection
```

This page introduces pagination in Hot Chocolate. Here, you will learn how to select a pagination model, enable paging, combine it with data middleware, and navigate to more detailed topics.

```text
resolver source -> paging arguments -> paging provider -> connection or segment -> client navigation
```

# Choosing a Pagination Model

Hot Chocolate supports two paging models:

| Model              | Enable with                                 | Client arguments                   | Schema shape                                                  | Recommended for                                                                                                    |
| ------------------ | ------------------------------------------- | ---------------------------------- | ------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------ |
| Cursor connections | `[UsePaging]` or `.UsePaging()`             | `first`, `after`, `last`, `before` | `ProductsConnection`, `ProductsEdge`, `PageInfo`              | Public APIs, feeds, catalogs, search results, load more, infinite scroll, Relay-style clients, changing data       |
| Offset paging      | `[UseOffsetPaging]` or `.UseOffsetPaging()` | `skip`, `take`                     | `ProductsCollectionSegment`, `items`, `CollectionSegmentInfo` | Small bounded admin lists, compatibility with offset-based data sources, cases where offset semantics are required |

Cursor pagination is ideal for public and evolving APIs. It uses opaque positions and stable ordering, making it suitable for data that may change while users are paging.

Offset paging is familiar for page-number interfaces. However, deep offsets can be slow, and changing data may result in repeated or skipped items. Use offset paging when these trade-offs are acceptable for your use case.

# Adding Cursor Pagination to a Field

Begin with a resolver that returns an ordered collection. For database-backed fields, return `IQueryable<T>` or another provider-supported query type so paging can be translated to the data source.

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

The expected SDL shape is:

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

A client can render nodes and keep the next cursor for navigation:

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

Use `edges` when the client needs item cursors or edge-specific fields:

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

Cursors are opaque. Store and return them as-is; do not attempt to parse them.

For more details, see [Cursor connections](connections-cursor.md) for field setup, connection results, and cursor behavior.

# Adding Offset Pagination to a Field

Use offset paging when the client requires `skip` and `take` arguments.

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

The expected SDL shape is:

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

See [Offset paging](offset.md) for more on `skip`, `take`, segment behavior, and offset-specific considerations.

# Configuring Paging with Fluent Types

Use the fluent API when schema configuration is managed outside the resolver class.

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

For offset paging, use `.UseOffsetPaging()`:

```csharp
descriptor
    .Field(t => t.GetProducts(default!))
    .UseOffsetPaging();
```

# Returning a Provider-Friendly Resolver Shape

Paging providers adapt resolver results to the schema. Choose a result type that allows the provider to slice data before it is materialized.

| Resolver result        | Use when                                                                                 |
| ---------------------- | ---------------------------------------------------------------------------------------- |
| `IQueryable<T>`        | A database provider can translate paging into source queries.                            |
| `IExecutable<T>`       | An integration exposes an executable query, such as MongoDB.                             |
| `IEnumerable<T>`       | The collection is already in memory and bounded.                                         |
| `Connection<T>`        | Your service layer, external API, or custom cursor logic already produced a cursor page. |
| `CollectionSegment<T>` | Your service layer already produced an offset page.                                      |

Avoid returning a large, materialized list from a database query. Let the provider apply paging, filtering, and sorting at the data source whenever possible.

For nested node data, use DataLoader to batch lookups rather than issuing one database call per row. Paging controls how many nodes are included in the field result, while DataLoader manages how related data is loaded for those nodes.

# Ensuring Stable Ordering

Cursor pagination requires deterministic ordering. If two items can have the same primary sort value, add a unique final sort key.

```csharp
public static IQueryable<Product> GetProducts(ProductStore store)
{
    return store.Products
        .OrderBy(p => p.Name)
        .ThenBy(p => p.Id);
}
```

Filtering and sorting change the logical list. A cursor is valid only for the same field arguments, filters, and sort order that produced it. When those arguments change, request a new first page.

# Combining Paging with Projections, Filtering, and Sorting

When combining paging with data middleware, use this order:

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

The declaration order may appear reversed. Field middleware is invoked in declaration order, but the resolver result flows back through the middleware in reverse. Filtering and sorting transform the query before paging creates the connection window.

Learn more:

- [Field middleware](/docs/hotchocolate/v16/build/execution-engine/field-middleware)
- [Projections](/docs/hotchocolate/v16/build/filtering-sorting-projections/projection-options)
- [Filtering](/docs/hotchocolate/v16/build/filtering-sorting-projections/filter-types)
- [Sorting](/docs/hotchocolate/v16/build/filtering-sorting-projections/sort-types)

# Setting Paging Limits and Defaults

Hot Chocolate uses conservative defaults for paged fields:

| Option                    | Default | Why it matters                                            |
| ------------------------- | ------- | --------------------------------------------------------- |
| `DefaultPageSize`         | `10`    | Used when the client does not provide a boundary.         |
| `MaxPageSize`             | `50`    | Caps requested page sizes.                                |
| `IncludeTotalCount`       | `false` | Adds `totalCount` when enabled.                           |
| `AllowBackwardPagination` | `true`  | Enables `last` and `before` on cursor fields.             |
| `RequirePagingBoundaries` | `false` | Requires clients to pass a page boundary when enabled.    |
| `IncludeNodesField`       | `true`  | Adds the shortcut `nodes` field on connections.           |
| `EnableRelativeCursors`   | `false` | Enables relative cursor behavior for supported scenarios. |

Set shared defaults using `ModifyPagingOptions`:

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

To set options for a single field:

```csharp
[UsePaging(DefaultPageSize = 25, MaxPageSize = 100, IncludeTotalCount = true)]
public static IQueryable<Product> GetProducts(ProductStore store)
{
    return store.Products.OrderBy(p => p.Id);
}
```

See [Paging options](paging-options.md) for details on options, naming, `IncludeNodesField`, `NullOrdering`, relative cursors, and public API limits.

# Enabling Total Count

By default, `totalCount` is disabled. Enable it when clients need the total size for a workflow.

```csharp
[UsePaging(IncludeTotalCount = true)]
public static IQueryable<Product> GetProducts(ProductStore store)
{
    return store.Products.OrderBy(p => p.Id);
}
```

The expected SDL addition is:

```graphql
type ProductsConnection {
  pageInfo: PageInfo!
  edges: [ProductsEdge!]
  nodes: [Product!]
  totalCount: Int!
}
```

Example client query:

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

Hot Chocolate only computes the count when the client requests `totalCount`. However, counting can add work for the selected provider. If you return `Connection<T>` yourself, provide count support through the connection rather than relying on the provider to infer it.

# Selecting a Paging Provider

Providers connect the schema model to different source types, including `IQueryable<T>`, `IEnumerable<T>`, `IExecutable<T>`, EF Core, MongoDB, Raven, and custom sources.

If `ProviderName` is not set, Hot Chocolate selects a provider based on the source type. If no provider can be inferred, it uses the first registered provider. Set `ProviderName` when multiple providers are registered or when a field should use a specific provider.

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

See [Paging providers](paging-providers.md) for provider registration, provider names, EF Core, MongoDB, Raven, and custom providers.

Related integrations:

- [Entity Framework](/docs/hotchocolate/v16/_leagcy/integrations/entity-framework)
- [MongoDB](/docs/hotchocolate/v16/_leagcy/integrations/mongodb)

# Protecting Public APIs

Pagination gives every collection field a cost boundary. For public APIs, keep `MaxPageSize` conservative, consider enabling `RequirePagingBoundaries`, and review how query cost is calculated for paged fields.

Learn more:

- [Cost analysis](/docs/hotchocolate/v16/build/security/cost-analysis)
- [Public API guide](/docs/hotchocolate/v16/_leagcy/guides/public-api)

# Using Advanced Paging When You Need Control

Most fields should return `IQueryable<T>`, `IExecutable<T>`, or a bounded in-memory collection. Use advanced shapes when another layer already manages the page.

- Return `Connection<T>` when a service or external API already produced nodes, cursors, page info, and optional count.
- Use service-layer paging with `PagingArguments`, `Page<T>`, `.ToConnectionAsync()`, and `.AddPagingArguments()` when paging is handled below the GraphQL resolver.
- Use `[UseConnection]`, `PageConnection<T>`, and `EnableRelativeCursors` for page-bar or relative cursor scenarios.
- Extend connection and edge types when clients need aggregate fields or edge metadata.

See [Cursor connections](connections-cursor.md) and [Paging options](paging-options.md) for more on these scenarios. If your client follows Relay conventions, see [Relay support](/docs/hotchocolate/v16/build/schema-elements/relay).

# Troubleshooting Common Paging Issues

| Symptom                                                   | Solution                                                                                                         | Read next                                   |
| --------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------- | ------------------------------------------- |
| `totalCount` is missing.                                  | Enable `IncludeTotalCount`. If you return `Connection<T>`, provide count support in that connection.             | [Paging options](paging-options.md)         |
| Pages repeat or skip items.                               | Add deterministic ordering and a unique final sort key such as `Id`.                                             | [Cursor connections](connections-cursor.md) |
| `last` or `before` is unavailable.                        | Check `AllowBackwardPagination`.                                                                                 | [Paging options](paging-options.md)         |
| A query without `first`, `last`, `skip`, or `take` fails. | Check `RequirePagingBoundaries`.                                                                                 | [Paging options](paging-options.md)         |
| Filtering or sorting changes page results.                | Treat cursors as scoped to the same filters and sort arguments. Request a new first page after argument changes. | [Cursor connections](connections-cursor.md) |
| The wrong provider handles the field.                     | Register the provider and set `ProviderName` on the field if inference is insufficient.                          | [Paging providers](paging-providers.md)     |
| Paging happens in memory for a large database table.      | Return `IQueryable<T>` or `IExecutable<T>` and avoid materializing the list before middleware runs.              | [Paging providers](paging-providers.md)     |
| Offset pages are slow or inconsistent.                    | Prefer cursor pagination for large or changing data.                                                             | [Offset paging](offset.md)                  |
| Nullable cursor keys fail with a provider error.          | Set `NullOrdering` in `PagingOptions` to match the database.                                                     | [Paging options](paging-options.md)         |

# Next Steps

- Build a cursor-paginated field: [Cursor connections](connections-cursor.md)
- Use `skip` and `take`: [Offset paging](offset.md)
- Configure limits, defaults, total count, nodes, null ordering, or relative cursors: [Paging options](paging-options.md)
- Register or select a provider: [Paging providers](paging-providers.md)
- Combine with data middleware: [Projections](/docs/hotchocolate/v16/build/filtering-sorting-projections/projection-options), [Filtering](/docs/hotchocolate/v16/build/filtering-sorting-projections/filter-types), and [Sorting](/docs/hotchocolate/v16/build/filtering-sorting-projections/sort-types)
- Protect public schemas: [Cost analysis](/docs/hotchocolate/v16/build/security/cost-analysis) and the [Public API guide](/docs/hotchocolate/v16/_leagcy/guides/public-api)
- Understand Relay conventions: [Relay support](/docs/hotchocolate/v16/build/schema-elements/relay)
