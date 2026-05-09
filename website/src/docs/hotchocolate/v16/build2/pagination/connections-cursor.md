---
title: Cursor connections
---

Cursor pagination turns a collection field into a Relay-style connection. The client asks for a window of items, receives opaque cursors for navigation, and uses `pageInfo` to decide whether another request is available.

Use cursor connections for public APIs, feeds, catalogs, search results, load-more UIs, infinite scroll, and Relay-compatible clients. Use [offset paging](offset.md) when the field needs `skip` and `take`, page-number semantics, or compatibility with an offset-based data source.

# Relay connection mental model

A connection is a page of an ordered list.

```text
ordered result set -> requested window -> connection -> client
```

The generated schema has four important parts:

- Connection: wraps the page.
- Edge: wraps one node and the cursor for that node.
- Node: the item your resolver returned.
- PageInfo: tells the client how to continue.

Cursors are opaque position tokens. Clients should store and pass them back. They should not parse or create cursor values.

# Generated schema shape

Given a field named `products`, `[UsePaging]` changes the field from a list into a connection.

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
        return store.Products
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Id);
    }
}

public sealed class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;

    public decimal Price { get; set; }
}

public sealed class ProductStore
{
    private static readonly Product[] s_products =
    [
        new() { Id = 1, Name = "Banana", Price = 1.99m },
        new() { Id = 2, Name = "Coffee", Price = 9.99m },
        new() { Id = 3, Name = "Tea", Price = 4.99m }
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

The `nodes` field is included by default. The `totalCount` field is opt-in.

# Add paging with fluent configuration

Use the fluent API when schema configuration lives in an `ObjectType`.

```csharp
using HotChocolate.Types;

public sealed class Query
{
    public IQueryable<Product> GetProducts(ProductStore store)
    {
        return store.Products
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Id);
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

You can pass a node type, connection name, or `PagingOptions` through `.UsePaging(...)` when the field needs local configuration.

# Page forward with first and after

Use `first` for the first page.

```graphql
query GetProducts {
  products(first: 2) {
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

Representative response:

```json
{
  "data": {
    "products": {
      "nodes": [
        {
          "id": 1,
          "name": "Banana"
        },
        {
          "id": 2,
          "name": "Coffee"
        }
      ],
      "pageInfo": {
        "hasNextPage": true,
        "endCursor": "Q3Vyc29yOjI="
      }
    }
  }
}
```

Use the returned `endCursor` as `after` for the next page.

```graphql
query GetNextProducts($after: String!) {
  products(first: 2, after: $after) {
    nodes {
      id
      name
    }
    pageInfo {
      hasNextPage
      endCursor
      startCursor
    }
  }
}
```

Variables:

```json
{
  "after": "Q3Vyc29yOjI="
}
```

A cursor is valid for the same field arguments, filters, and sort order that produced it. Ask for a new first page after changing filters or sorting.

# Page backward with last and before

Backward pagination uses `last` and `before`. It is enabled by default.

```graphql
query GetPreviousProducts($before: String!) {
  products(last: 2, before: $before) {
    nodes {
      id
      name
    }
    pageInfo {
      hasPreviousPage
      startCursor
    }
  }
}
```

Use `startCursor` from the current page as `before` when requesting the previous page.

Disable backward pagination when a source cannot support it or when reverse navigation is too expensive.

```csharp
[UsePaging(AllowBackwardPagination = false)]
public static IQueryable<Product> GetProducts(ProductStore store)
{
    return store.Products.OrderBy(p => p.Id);
}
```

Expected SDL shape after disabling backward pagination:

```graphql
type Query {
  products(first: Int, after: String): ProductsConnection
}
```

# Edges versus nodes

Use `nodes` when the client only needs the items.

```graphql
query GetProductNodes {
  products(first: 10) {
    nodes {
      id
      name
    }
  }
}
```

Use `edges` when the client needs a cursor per item or edge-level fields.

```graphql
query GetProductEdges {
  products(first: 10) {
    edges {
      cursor
      node {
        id
        name
      }
    }
    pageInfo {
      hasNextPage
      endCursor
    }
  }
}
```

Relay-style clients commonly rely on `edges.cursor` and `pageInfo`.

# Keep cursor ordering stable

Cursor pagination depends on deterministic ordering. If two rows can have the same primary sort value, add a unique final key.

```csharp
public static IQueryable<Product> GetProducts(ProductStore store)
{
    return store.Products
        .OrderBy(p => p.Name)
        .ThenBy(p => p.Id);
}
```

This matters for changing datasets. Without stable ordering, inserts or deletes can cause repeated or skipped items while a client pages through the list.

For database-backed fields, keep ordering in the `IQueryable<T>` or provider-supported query so the paging provider can translate it to the data source.

# Configure field behavior

Use field-level options when one field needs different limits or schema behavior.

```csharp
[UsePaging(
    DefaultPageSize = 25,
    MaxPageSize = 100,
    RequirePagingBoundaries = true,
    IncludeTotalCount = true,
    ConnectionName = "CatalogProducts")]
public static IQueryable<Product> GetProducts(ProductStore store)
{
    return store.Products
        .OrderBy(p => p.Name)
        .ThenBy(p => p.Id);
}
```

Equivalent fluent configuration:

```csharp
using HotChocolate.Types;
using HotChocolate.Types.Pagination;

descriptor
    .Field(t => t.GetProducts(default!))
    .UsePaging(
        connectionName: "CatalogProducts",
        options: new PagingOptions
        {
            DefaultPageSize = 25,
            MaxPageSize = 100,
            RequirePagingBoundaries = true,
            IncludeTotalCount = true
        });
```

| Option                    | Default                 | Effect                                                |
| ------------------------- | ----------------------- | ----------------------------------------------------- |
| `DefaultPageSize`         | `10`                    | Used when the client omits `first` and `last`.        |
| `MaxPageSize`             | `50`                    | Caps accepted values for `first` and `last`.          |
| `RequirePagingBoundaries` | `false`                 | Requires the client to send `first` or `last`.        |
| `AllowBackwardPagination` | `true`                  | Adds `last` and `before` to the field.                |
| `IncludeTotalCount`       | `false`                 | Adds `totalCount` to the connection type.             |
| `ConnectionName`          | inferred from the field | Sets the base name for the connection and edge types. |
| `ProviderName`            | `null`                  | Selects a named paging provider.                      |

See [paging options](paging-options.md) for global defaults and the full option set.

# Configure the nodes field

`nodes` is controlled by `PagingOptions.IncludeNodesField`. It is enabled by default. The `[UsePaging]` attribute does not expose this option, so configure it with the fluent API or global paging options.

```csharp
using HotChocolate.Types.Pagination;

descriptor
    .Field(t => t.GetProducts(default!))
    .UsePaging(options: new PagingOptions
    {
        IncludeNodesField = false
    });
```

With `IncludeNodesField = false`, clients use `edges { node { ... } }`.

```graphql
type ProductsConnection {
  pageInfo: PageInfo!
  edges: [ProductsEdge!]
}
```

# Add totalCount

`totalCount` is not generated by default. Enable it when clients need the total number of matching items.

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
query GetProductsWithCount {
  products(first: 10) {
    totalCount
    nodes {
      id
      name
    }
  }
}
```

For `IQueryable<T>` and `IEnumerable<T>` sources, the paging provider supplies the count when `totalCount` is selected. Counts can add data-source work, so enable the field when the client workflow needs it.

# Return Connection<T> manually

Return `Connection<T>` when a service layer or external API already performed slicing, cursor creation, and count calculation. The `Connection<T>` constructor takes an integer `totalCount`.

```csharp
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;

[QueryType]
public static partial class ProductQueries
{
    [UsePaging(IncludeTotalCount = true)]
    public static async Task<Connection<Product>> GetFeaturedProductsAsync(
        string? after,
        int? first,
        ProductApi api,
        CancellationToken cancellationToken)
    {
        var pageSize = first ?? 10;
        var page = await api.GetFeaturedProductsAsync(after, pageSize, cancellationToken);

        var edges = page.Items
            .Select(item => new Edge<Product>(item.Product, item.Cursor))
            .ToArray();

        var pageInfo = new ConnectionPageInfo(
            page.HasNextPage,
            page.HasPreviousPage,
            edges.FirstOrDefault()?.Cursor,
            edges.LastOrDefault()?.Cursor);

        return new Connection<Product>(
            edges,
            pageInfo,
            totalCount: page.TotalCount);
    }
}

public sealed record ProductApiPage(
    IReadOnlyList<ProductApiItem> Items,
    bool HasNextPage,
    bool HasPreviousPage,
    int TotalCount);

public sealed record ProductApiItem(Product Product, string Cursor);
```

Use this pattern when the GraphQL resolver should expose a connection while another layer owns the paging rules. If you use `GreenDonut.Data.Page<T>`, convert it with `.ToConnection()` or `.ToConnectionAsync()` after registering paging argument binding with `.AddPagingArguments()`.

```csharp
using GreenDonut.Data;
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;

[QueryType]
public static partial class ProductQueries
{
    [UsePaging]
    public static async Task<Connection<Product>> GetProductsAsync(
        PagingArguments pagingArguments,
        ProductService productService,
        CancellationToken cancellationToken)
    {
        return await productService
            .GetProductsAsync(pagingArguments, cancellationToken)
            .ToConnectionAsync();
    }
}
```

# Add connection and edge fields

Extend generated connection and edge types when clients need aggregate data or edge metadata.

```csharp
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;

[ExtendObjectType("ProductsConnection")]
public sealed class ProductsConnectionExtensions
{
    public decimal GetPageTotal([Parent] Connection<Product> connection)
    {
        return connection.Edges.Sum(edge => edge.Node.Price);
    }
}
```

```csharp
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;

[ExtendObjectType("ProductsEdge")]
public sealed class ProductsEdgeExtensions
{
    public string GetDisplayLabel([Parent] Edge<Product> edge)
    {
        return $"{edge.Node.Name} ({edge.Node.Id})";
    }
}
```

Expected SDL shape:

```graphql
type ProductsConnection {
  pageInfo: PageInfo!
  edges: [ProductsEdge!]
  nodes: [Product!]
  pageTotal: Decimal!
}

type ProductsEdge {
  cursor: String!
  node: Product!
  displayLabel: String!
}
```

If you combine these fields with projections, make sure the selected model data is available for the extension field.

# Compose with projections, filtering, and sorting

Use this middleware order:

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

Fluent configuration uses the same order:

```csharp
descriptor
    .Field(t => t.GetProducts(default!))
    .UsePaging()
    .UseProjection()
    .UseFiltering()
    .UseSorting();
```

Filtering and sorting change the logical result set. Do not reuse a cursor after changing those arguments.

# Database and DataLoader guidance

Return a provider-friendly shape from resolvers:

| Resolver result  | Use it when                                                 |
| ---------------- | ----------------------------------------------------------- |
| `IQueryable<T>`  | A database provider can translate paging to source queries. |
| `IExecutable<T>` | An integration exposes an executable query shape.           |
| `IEnumerable<T>` | The collection is already in memory and bounded.            |
| `Connection<T>`  | Another layer already produced the cursor page.             |

Avoid materializing a large database query before paging middleware runs. Keep filtering, sorting, ordering, and paging in the query shape that the provider receives.

Use DataLoader for related data loaded from the paged nodes. Paging limits how many nodes enter the result. DataLoader batches follow-up lookups for those nodes and avoids one database call per item.

# Nullable cursor keys

When an order key can be `null`, the paging provider needs to know how the database orders null values.

```csharp
using GreenDonut.Data;
using HotChocolate.Types.Pagination;

builder
    .AddGraphQL()
    .ModifyPagingOptions(options =>
    {
        options.NullOrdering = NullOrdering.NativeNullsLast;
    });
```

Set `NullOrdering` to match the database when provider inference is not enough.

# When offset paging is a better fit

Use offset paging when the client or data source needs offset semantics:

- Admin grids with bounded, stable data.
- Page-number navigation.
- Existing APIs that expose `skip` and `take`.
- Reports where a deep offset is an accepted tradeoff.
- Compatibility layers where cursor semantics would not match the source.

Cursor pagination is the better default for large or changing lists because it navigates by opaque positions in a stable order.

# Troubleshooting

| Symptom                                                 | What to check                                                                                                                        |
| ------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------ |
| Pages repeat or skip items.                             | Add deterministic ordering with a unique final key, and keep filters and sorting stable while using a cursor.                        |
| `totalCount` is missing.                                | Set `IncludeTotalCount = true` and select `totalCount` in the query. For `Connection<T>`, pass the integer count to the constructor. |
| `last` or `before` is missing.                          | Check `AllowBackwardPagination`. If it is `false`, the schema only exposes forward paging arguments.                                 |
| A query without `first` or `last` fails.                | `RequirePagingBoundaries` is enabled. Send a page size.                                                                              |
| A page-size query fails.                                | The requested value is negative or greater than `MaxPageSize`.                                                                       |
| `nodes` is missing.                                     | `IncludeNodesField` is `false`. Query through `edges` or enable `IncludeNodesField`.                                                 |
| Cursors stop working after filter or sort changes.      | Request a new first page. Cursors belong to the field arguments that produced them.                                                  |
| Nullable cursor keys behave incorrectly.                | Configure `NullOrdering` to match the data source.                                                                                   |
| Filtering, sorting, or projections behave unexpectedly. | Check middleware order: paging, projection, filtering, sorting.                                                                      |
| Paging happens in memory for a large table.             | Return `IQueryable<T>` or another provider-supported query shape, and avoid materializing before middleware runs.                    |
| The wrong provider handles the field.                   | Register the integration provider and set `ProviderName` where the field needs it.                                                   |

# API quick reference

| Item                  | Names                                                                   |
| --------------------- | ----------------------------------------------------------------------- |
| Forward arguments     | `first`, `after`                                                        |
| Backward arguments    | `last`, `before`                                                        |
| Connection fields     | `pageInfo`, `edges`, `nodes`, `totalCount`                              |
| Edge fields           | `cursor`, `node`                                                        |
| PageInfo fields       | `hasNextPage`, `hasPreviousPage`, `startCursor`, `endCursor`            |
| Attribute             | `[UsePaging]`                                                           |
| Fluent API            | `.UsePaging()`                                                          |
| Manual result types   | `Connection<T>`, `Edge<T>`, `ConnectionPageInfo`                        |
| Service-layer helpers | `PagingArguments`, `Page<T>`, `.ToConnection()`, `.ToConnectionAsync()` |

# Next steps

- [Offset paging](offset.md) for `skip`, `take`, and collection segments.
- [Paging options](paging-options.md) for defaults, limits, `IncludeNodesField`, `NullOrdering`, and relative cursor settings.
- [Paging providers](paging-providers.md) for provider registration and named providers.
- [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections), [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering), and [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting) for data middleware.
- [Relay support](/docs/hotchocolate/v16/building-a-schema/relay) for Relay conventions beyond connections.
- [Cost analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis) for query cost limits on paged fields.
