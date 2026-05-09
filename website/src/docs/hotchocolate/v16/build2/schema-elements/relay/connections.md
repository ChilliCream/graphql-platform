---
title: Relay Connections
---

A Relay connection is the schema contract that lets clients traverse a paginated list using opaque cursors. Hot Chocolate generates the full connection shape from a single attribute or descriptor call. This page covers the connection schema contract, how to expose it, and how to extend it.

For cursor slicing mechanics, provider configuration, offset pagination, and full paging option details, see [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination).

# Understand What Relay Clients Expect

A connection field accepts forward-paging arguments (`first`, `after`) and, when backward paging is enabled, reverse-paging arguments (`last`, `before`). It returns a connection type that wraps items with page metadata.

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
  nodes: [Product!] # Hot Chocolate convenience field
  totalCount: Int # optional, disabled by default
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

| Term         | Role                                                                                   |
| ------------ | -------------------------------------------------------------------------------------- |
| `node`       | The item in the list                                                                   |
| `cursor`     | An opaque position token that marks the item's place in the dataset                    |
| `edge`       | One slot in the list: the item plus its cursor and any relationship metadata           |
| `pageInfo`   | Page-level navigation metadata shared across all edges on the current page             |
| `nodes`      | Hot Chocolate shortcut field: items without cursors; controlled by `IncludeNodesField` |
| `totalCount` | Optional count of the whole result set; not part of the Relay core contract            |

The Relay core contract requires `edges`, `pageInfo`, edge `cursor`, and edge `node`. The `nodes` and `totalCount` fields are Hot Chocolate additions to the contract.

Cursors are opaque position tokens. Do not parse, construct, or expose their internal format. Use [global object identification](/docs/hotchocolate/v16/building-a-schema/relay) when clients need to refetch individual items by identity.

# Add a Connection Field with `UsePaging`

Apply `[UsePaging]` to a resolver that returns `IEnumerable<T>` or `IQueryable<T>`. Hot Chocolate adds the connection arguments, builds the connection and edge types, and hands slicing to the configured paging provider.

<ExampleTabs>
<Implementation>

```csharp
using HotChocolate.Types;
using HotChocolate.Types.Pagination;

[QueryType]
public static partial class ProductQueries
{
    [UsePaging]
    public static IQueryable<Product> GetProducts(CatalogContext db)
        => db.Products.OrderBy(p => p.Id);
}
```

</Implementation>
<Code>

```csharp
public class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("products")
            .UsePaging<ObjectType<Product>>()
            .Resolve(context =>
            {
                var db = context.Service<CatalogContext>();
                return db.Products.OrderBy(p => p.Id);
            });
    }
}
```

</Code>
</ExampleTabs>

The field must return items in a deterministic order before paging is applied. Include the primary key as the final sort key to guarantee stable pages across requests. Sorting mechanics are covered in [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting).

The generated SDL for the attribute example above:

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

## Query the First Page

```graphql
query GetProducts($first: Int!, $after: String) {
  products(first: $first, after: $after) {
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

## Query the Next Page

Pass the `endCursor` from the previous response as the `after` argument:

```graphql
query GetNextProducts($first: Int!, $after: String!) {
  products(first: $first, after: $after) {
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

# Choose `edges` or `nodes` in Client Queries

Use `edges` when the client needs per-item cursors or edge-level metadata. Use `nodes` when the client only needs the items themselves and reads page cursors from `pageInfo`.

```graphql
query GetProductNodes($first: Int!, $after: String) {
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

The `nodes` field is a Hot Chocolate convenience field controlled by the `IncludeNodesField` paging option. When `nodes` is absent from your schema, query items through `edges { node { ... } }` or enable the field in your paging options.

# Return a Manual `Connection<T>` When You Already Have a Page

When your resolver receives a page from a service layer or a remote API, return a `Connection<T>` directly instead of a raw collection. This gives you full control over edge cursors, page info flags, and total count.

```csharp
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
        var page = await productService.GetProductsAsync(pagingArguments, cancellationToken);

        var edges = page.Items
            .Select(item => new Edge<Product>(item.Product, item.Cursor))
            .ToList();

        var pageInfo = new ConnectionPageInfo(
            hasNextPage: page.HasNextPage,
            hasPreviousPage: page.HasPreviousPage,
            startCursor: edges.Count > 0 ? edges[0].Cursor : null,
            endCursor: edges.Count > 0 ? edges[^1].Cursor : null);

        return new Connection<Product>(edges, pageInfo, page.TotalCount);
    }
}
```

When your service layer produces a `Page<T>` from GreenDonut, use `.ToConnectionAsync()` to convert it:

```csharp
using GreenDonut.Data;
using HotChocolate.Types.Pagination;

[UsePaging]
public static async Task<Connection<Product>> GetProductsAsync(
    PagingArguments pagingArguments,
    ProductService productService,
    CancellationToken cancellationToken)
    => await productService
        .GetProductsAsync(pagingArguments, cancellationToken)
        .ToConnectionAsync();
```

The field still needs `[UsePaging]` so the schema exposes connection arguments and generates the connection types. For full `Page<T>` and `PagingArguments` usage, see [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination).

# Add `totalCount` When Clients Need It

`totalCount` is optional and not part of the Relay core contract. Enable it per field or globally.

<ExampleTabs>
<Implementation>

```csharp
[UsePaging(IncludeTotalCount = true)]
public static IQueryable<Product> GetProducts(CatalogContext db)
    => db.Products.OrderBy(p => p.Id);
```

</Implementation>
<Code>

```csharp
descriptor
    .Field("products")
    .UsePaging<ObjectType<Product>>(options: new PagingOptions
    {
        IncludeTotalCount = true
    });
```

</Code>
</ExampleTabs>

To enable it globally for all paged fields:

```csharp
builder
    .AddGraphQL()
    .ModifyPagingOptions(opt => opt.IncludeTotalCount = true);
```

Enabling `totalCount` adds the field to the SDL. Clients must still request it explicitly, and the database or service layer must compute it when it is requested.

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

When you return a manual `Connection<T>`, pass the total as the integer parameter:

```csharp
return new Connection<Product>(edges, pageInfo, page.TotalCount);
```

# Name Connection and Edge Types Intentionally

By default, Hot Chocolate infers the connection and edge type names from the field name. A field named `products` generates `ProductsConnection` and `ProductsEdge`. This behavior is controlled by `InferConnectionNameFromField`.

| Field              | Default connection type      | Default edge type      | Example explicit connection name |
| ------------------ | ---------------------------- | ---------------------- | -------------------------------- |
| `products`         | `ProductsConnection`         | `ProductsEdge`         | `CatalogProducts`                |
| `featuredProducts` | `FeaturedProductsConnection` | `FeaturedProductsEdge` | `FeaturedProducts`               |

Set an explicit name when the same node type appears in multiple lists with different semantics, when you plan to extend the connection or edge type, or when you need a stable schema name that will not change if the field is renamed.

<ExampleTabs>
<Implementation>

```csharp
[UsePaging(ConnectionName = "CatalogProducts")]
public static IQueryable<Product> GetProducts(CatalogContext db)
    => db.Products.OrderBy(p => p.Id);
```

This produces `CatalogProductsConnection` and `CatalogProductsEdge` in the schema.

</Implementation>
<Code>

```csharp
descriptor
    .Field("products")
    .UsePaging<ObjectType<Product>>(connectionName: "CatalogProducts");
```

</Code>
</ExampleTabs>

Hot Chocolate appends `Connection` and `Edge` to the name you provide. Set `ConnectionName` to `"CatalogProducts"` and the types become `CatalogProductsConnection` and `CatalogProductsEdge`.

# Extend a Connection or Edge Type

Add fields to a connection type for collection-level metadata such as aggregates, facets, or summaries. Add fields to an edge type for item-in-list metadata such as rank, relevance score, or relationship properties.

## Extend the Connection Type

```csharp
[ExtendObjectType("ProductsConnection")]
public sealed class ProductsConnectionExtensions
{
    public decimal AveragePrice([Parent] Connection<Product> connection)
        => connection.Edges.Count > 0
            ? connection.Edges.Average(e => e.Node.Price)
            : 0m;
}
```

Register the extension alongside your other types:

```csharp
builder
    .AddGraphQL()
    .AddType<ProductsConnectionExtensions>();
```

## Extend the Edge Type

```csharp
[ExtendObjectType("ProductsEdge")]
public sealed class ProductsEdgeExtensions
{
    public int Rank([Parent] Edge<Product> edge, [Service] RankingService ranking)
        => ranking.GetRank(edge.Node.Id);
}
```

When your connection or edge type name is hard to predict, set `ConnectionName` explicitly before adding extensions. Type extensions that use a generated name break when the field is renamed.

When projections are active, some model properties may not be populated depending on what the client selected. Avoid reading properties in extension resolvers that are not guaranteed to be projected.

# How Connections Relate to Global Object Identification

A connection can expose any object type as `node`. Relay-style clients benefit when those node types also implement global object identification, because clients can then refetch individual items by their stable global ID.

Global object identification is not required for connections to work. It is recommended when you want Relay client stores to cache and refetch items by identity.

See [Relay and Global Object Identification](/docs/hotchocolate/v16/building-a-schema/relay) for setup instructions.

# Connection Options Reference

This table covers the options most relevant to the connection schema contract. For the full options reference including `MaxPageSize`, `DefaultPageSize`, `RequirePagingBoundaries`, and provider configuration, see [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination).

| Option                         | Where to set it                  | Effect on schema                                       |
| ------------------------------ | -------------------------------- | ------------------------------------------------------ |
| `ConnectionName`               | `[UsePaging]` or `.UsePaging()`  | Sets the connection and edge type names                |
| `InferConnectionNameFromField` | Global or per field              | Controls whether names are derived from the field name |
| `IncludeTotalCount`            | `[UsePaging]` or `.UsePaging()`  | Adds `totalCount` to the connection type               |
| `IncludeNodesField`            | Global via `ModifyPagingOptions` | Adds `nodes` shortcut field to the connection type     |
| `AllowBackwardPagination`      | `[UsePaging]` or `.UsePaging()`  | Adds `last` and `before` arguments to the field        |

# Troubleshooting

**`nodes` is missing from the schema.**
Check whether `IncludeNodesField` is disabled in your paging options. When it is disabled, query items through `edges { node { ... } }`.

**`totalCount` is missing from the schema.**
Enable `IncludeTotalCount` on the field or globally. The field only appears in the SDL when the option is enabled.

**`totalCount` returns zero for a manual `Connection<T>`.**
Pass the known total as the integer argument to the `Connection<T>` constructor. The default value is zero.

**`totalCount` is null or expensive to resolve.**
Check whether the paging provider can compute counts for the returned source. Only enable `IncludeTotalCount` on fields where clients need it, and review [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination) before adding counts to large datasets.

**Type extension fields do not appear on the connection or edge.**
Verify the generated type names match the strings in `[ExtendObjectType(...)]`. Set `ConnectionName` explicitly to lock the names before writing extensions.

**A page accepts unbounded requests.**
Set `MaxPageSize`, `DefaultPageSize`, or `RequirePagingBoundaries` in the field or global paging options. Pair schema limits with cost analysis for public APIs.

**Pages skip or repeat items between requests.**
Ensure the resolver returns items in a deterministic order. The primary key must be the final sort key. See [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination) for cursor mechanics.

**Backward paging arguments (`last`, `before`) are missing.**
Check `AllowBackwardPagination`. The option defaults to `true` but may be disabled globally.

**Clients treat cursors as identifiers.**
Cursors are opaque position tokens, not global IDs. Use global object identification and the `node` root field for stable object identity.

# Next Steps

- **Set up global object identification:** [Relay and Global Object Identification](/docs/hotchocolate/v16/building-a-schema/relay)
- **Configure paging boundaries, providers, and cursor behavior:** [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination)
- **Filter results on a paged field:** [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering)
- **Sort results on a paged field:** [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting)
- **Optimize database queries with projections:** [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections)
- **Batch data fetching efficiently:** [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader)
- **Extend connection or edge types:** [Extending Types](/docs/hotchocolate/v16/defining-a-schema/extending-types)
- **Protect against expensive queries:** [Cost Analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis)
