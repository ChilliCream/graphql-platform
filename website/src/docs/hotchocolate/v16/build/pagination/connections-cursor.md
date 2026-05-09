---
title: Cursor connections
---

Cursor pagination transforms a collection field into a Relay-style connection. With this approach, the client requests a window of items, receives opaque cursors for navigation, and uses the `pageInfo` object to determine if more data is available.

Cursor connections are ideal for public APIs, feeds, catalogs, search results, load-more interfaces, infinite scroll, and Relay-compatible clients. If your field requires `skip` and `take`, page-number navigation, or must work with an offset-based data source, consider using [offset paging](offset.md) instead.

# Understanding Relay Connections

A connection represents a page from an ordered list.

```text
ordered result set -> requested window -> connection -> client
```

The generated schema includes four key components:

- **Connection**: Wraps the page of results.
- **Edge**: Wraps a single node and its cursor.
- **Node**: The actual item returned by your resolver.
- **PageInfo**: Informs the client how to continue pagination.

Cursors are opaque tokens that represent positions in the list. Clients should store and return these cursors as-is, without attempting to parse or generate them.

# Schema Structure

When you apply `[UsePaging]` to a field such as `products`, the field changes from a list to a connection.

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

The resulting SDL looks like this:

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

By default, the `nodes` field is included. The `totalCount` field is available as an option.

# Adding Paging with the Fluent API

If your schema configuration is defined in an `ObjectType`, use the fluent API to add paging.

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

You can specify a node type, connection name, or `PagingOptions` with `.UsePaging(...)` for field-specific configuration.

# Forward Pagination: `first` and `after`

To fetch the first page, use the `first` argument.

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

A typical response:

```json
{
  "data": {
    "products": {
      "nodes": [
        { "id": 1, "name": "Banana" },
        { "id": 2, "name": "Coffee" }
      ],
      "pageInfo": {
        "hasNextPage": true,
        "endCursor": "Q3Vyc29yOjI="
      }
    }
  }
}
```

To request the next page, use the returned `endCursor` as the `after` argument.

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

With variables:

```json
{
  "after": "Q3Vyc29yOjI="
}
```

A cursor remains valid only for the same field arguments, filters, and sort order that produced it. If you change filters or sorting, request a new first page.

# Backward Pagination: `last` and `before`

Backward pagination uses the `last` and `before` arguments and is enabled by default.

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

To fetch the previous page, use the `startCursor` from the current page as the `before` argument.

If your data source cannot support backward pagination or reverse navigation is too costly, you can disable it:

```csharp
[UsePaging(AllowBackwardPagination = false)]
public static IQueryable<Product> GetProducts(ProductStore store)
{
    return store.Products.OrderBy(p => p.Id);
}
```

The SDL then becomes:

```graphql
type Query {
  products(first: Int, after: String): ProductsConnection
}
```

# Choosing Between Edges and Nodes

Use the `nodes` field when the client only needs the items:

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

Use the `edges` field when the client needs a cursor for each item or requires edge-level fields:

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

Relay-style clients often rely on `edges.cursor` and `pageInfo` for navigation.

# Ensuring Stable Cursor Ordering

Cursor pagination requires deterministic ordering. If two rows can share the same primary sort value, add a unique key as a final sort criterion.

```csharp
public static IQueryable<Product> GetProducts(ProductStore store)
{
    return store.Products
        .OrderBy(p => p.Name)
        .ThenBy(p => p.Id);
}
```

Stable ordering is important for changing datasets. Without it, inserts or deletions can cause repeated or skipped items as the client pages through the list.

For database-backed fields, keep ordering in the `IQueryable<T>` or provider-supported query so the paging provider can translate it to the data source.

# Customizing Field Behavior

Use field-level options when a field requires different limits or schema behavior.

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

The equivalent fluent configuration:

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

See [paging options](paging-options.md) for global defaults and the complete set of options.

# Configuring the `nodes` Field

The `nodes` field is controlled by `PagingOptions.IncludeNodesField` and is enabled by default. The `[UsePaging]` attribute does not expose this option, so use the fluent API or global paging options to configure it.

```csharp
using HotChocolate.Types.Pagination;

descriptor
    .Field(t => t.GetProducts(default!))
    .UsePaging(options: new PagingOptions
    {
        IncludeNodesField = false
    });
```

When `IncludeNodesField = false`, clients should use `edges { node { ... } }`.

```graphql
type ProductsConnection {
  pageInfo: PageInfo!
  edges: [ProductsEdge!]
}
```

# Adding `totalCount`

The `totalCount` field is not included by default. Enable it when clients need the total number of matching items.

```csharp
[UsePaging(IncludeTotalCount = true)]
public static IQueryable<Product> GetProducts(ProductStore store)
{
    return store.Products.OrderBy(p => p.Id);
}
```

The SDL will then include:

```graphql
type ProductsConnection {
  pageInfo: PageInfo!
  edges: [ProductsEdge!]
  nodes: [Product!]
  totalCount: Int!
}
```

A client query might look like:

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

For `IQueryable<T>` and `IEnumerable<T>` sources, the paging provider supplies the count when `totalCount` is selected. Counting can add work for the data source, so enable this field only when the client workflow requires it.

# Returning `Connection<T>` Manually

Return a `Connection<T>` when a service layer or external API has already performed slicing, cursor creation, and count calculation. The `Connection<T>` constructor accepts an integer `totalCount`.

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

Use this pattern when the GraphQL resolver should expose a connection but another layer manages paging. If you use `GreenDonut.Data.Page<T>`, convert it with `.ToConnection()` or `.ToConnectionAsync()` after registering paging argument binding with `.AddPagingArguments()`.

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

# Extending Connection and Edge Types

You can extend generated connection and edge types to provide aggregate data or edge metadata for clients.

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

The SDL will reflect these extensions:

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

If you use these fields with projections, ensure the selected model data is available for the extension field.

# Combining with Projections, Filtering, and Sorting

Apply middleware in this order:

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

The fluent API uses the same order:

```csharp
descriptor
    .Field(t => t.GetProducts(default!))
    .UsePaging()
    .UseProjection()
    .UseFiltering()
    .UseSorting();
```

Filtering and sorting change the logical result set. Do not reuse a cursor after changing these arguments.

# Database and DataLoader Recommendations

Resolvers should return a shape that is friendly to the paging provider:

| Resolver result  | Use it when                                                 |
| ---------------- | ----------------------------------------------------------- |
| `IQueryable<T>`  | A database provider can translate paging to source queries. |
| `IExecutable<T>` | An integration exposes an executable query shape.           |
| `IEnumerable<T>` | The collection is already in memory and bounded.            |
| `Connection<T>`  | Another layer already produced the cursor page.             |

Avoid materializing large database queries before paging middleware runs. Keep filtering, sorting, ordering, and paging in the query shape that the provider receives.

Use DataLoader for related data loaded from paged nodes. Paging limits how many nodes are in the result, and DataLoader batches follow-up lookups to avoid one database call per item.

# Handling Nullable Cursor Keys

If an order key can be `null`, the paging provider must know how the database orders null values.

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

Set `NullOrdering` to match your database if provider inference is insufficient.

# When to Use Offset Paging

Offset paging is a better fit when the client or data source requires offset semantics:

- Admin grids with bounded, stable data
- Page-number navigation
- Existing APIs that use `skip` and `take`
- Reports where deep offsets are acceptable
- Compatibility layers where cursor semantics do not match the source

Cursor pagination is usually preferred for large or changing lists, as it navigates by opaque positions in a stable order.

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

# API Quick Reference

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

# Next Steps

- [Offset paging](offset.md) for `skip`, `take`, and collection segments
- [Paging options](paging-options.md) for defaults, limits, `IncludeNodesField`, `NullOrdering`, and relative cursor settings
- [Paging providers](paging-providers.md) for provider registration and named providers
- [Projections](/docs/hotchocolate/v16/build/filtering-sorting-projections/projection-options), [Filtering](/docs/hotchocolate/v16/build/filtering-sorting-projections/filter-types), and [Sorting](/docs/hotchocolate/v16/build/filtering-sorting-projections/sort-types) for data middleware
- [Relay support](/docs/hotchocolate/v16/build/schema-elements/relay) for Relay conventions beyond connections
- [Cost analysis](/docs/hotchocolate/v16/build/security/cost-analysis) for query cost limits on paged fields
