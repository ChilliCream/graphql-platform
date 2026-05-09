---
title: Relay Connections
---

Relay connections define a schema contract that enables clients to navigate paginated lists using opaque cursors. Hot Chocolate can generate the complete connection structure from a single attribute or descriptor. This page explains the connection schema contract, how to expose connection fields, and how to extend them.

For details on cursor slicing, provider configuration, offset pagination, and all paging options, see [Pagination](/docs/hotchocolate/v16/build/pagination).

# What Relay Clients Expect

A connection field accepts forward-paging arguments (`first`, `after`) and, if backward paging is enabled, reverse-paging arguments (`last`, `before`). It returns a connection type that wraps the items and includes page metadata.

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

The Relay core contract requires `edges`, `pageInfo`, and for each edge, a `cursor` and `node`. The `nodes` and `totalCount` fields are Hot Chocolate extensions to this contract.

Cursors are opaque tokens representing positions. Do not parse, construct, or reveal their internal format. If clients need to refetch items by identity, use [global object identification](/docs/hotchocolate/v16/build/schema-elements/relay).

# Adding a Connection Field with `UsePaging`

Apply `[UsePaging]` to a resolver that returns `IEnumerable<T>` or `IQueryable<T>`. Hot Chocolate will add the connection arguments, generate the connection and edge types, and delegate slicing to the configured paging provider.

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

The resolver must return items in a deterministic order before paging is applied. Always include the primary key as the final sort key to ensure stable pages across requests. For more on sorting, see [Sorting](/docs/hotchocolate/v16/build/filtering-sorting-projections/sort-types).

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

## Querying the First Page

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

## Querying the Next Page

To fetch the next page, pass the `endCursor` from the previous response as the `after` argument:

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

# Choosing Between `edges` and `nodes` in Client Queries

Use `edges` when the client needs per-item cursors or edge-level metadata. Use `nodes` when only the items themselves are needed and page cursors are read from `pageInfo`.

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

The `nodes` field is a Hot Chocolate convenience, controlled by the `IncludeNodesField` paging option. If `nodes` is not present in your schema, query items through `edges { node { ... } }` or enable the field in your paging options.

# Returning a Manual `Connection<T>` When You Already Have a Page

If your resolver receives a page from a service layer or remote API, return a `Connection<T>` directly instead of a raw collection. This approach gives you full control over edge cursors, page info flags, and total count.

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

If your service layer produces a `Page<T>` from GreenDonut, you can convert it using `.ToConnectionAsync()`:

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

The field still requires `[UsePaging]` so the schema exposes connection arguments and generates the connection types. For more on `Page<T>` and `PagingArguments`, see [Pagination](/docs/hotchocolate/v16/build/pagination).

# Adding `totalCount` When Clients Need It

The `totalCount` field is optional and not part of the Relay core contract. You can enable it per field or globally.

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

To enable `totalCount` globally for all paged fields:

```csharp
builder
    .AddGraphQL()
    .ModifyPagingOptions(opt => opt.IncludeTotalCount = true);
```

Enabling `totalCount` adds the field to the SDL. Clients must still request it explicitly, and the database or service layer must compute it when requested.

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

When returning a manual `Connection<T>`, pass the total as the integer parameter:

```csharp
return new Connection<Product>(edges, pageInfo, page.TotalCount);
```

# Naming Connection and Edge Types Intentionally

By default, Hot Chocolate infers connection and edge type names from the field name. For example, a field named `products` generates `ProductsConnection` and `ProductsEdge`. This behavior is controlled by `InferConnectionNameFromField`.

| Field              | Default connection type      | Default edge type      | Example explicit connection name |
| ------------------ | ---------------------------- | ---------------------- | -------------------------------- |
| `products`         | `ProductsConnection`         | `ProductsEdge`         | `CatalogProducts`                |
| `featuredProducts` | `FeaturedProductsConnection` | `FeaturedProductsEdge` | `FeaturedProducts`               |

Set an explicit name if the same node type appears in multiple lists with different meanings, if you plan to extend the connection or edge type, or if you need a stable schema name that will not change if the field is renamed.

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

Hot Chocolate appends `Connection` and `Edge` to the name you provide. Setting `ConnectionName` to `"CatalogProducts"` results in `CatalogProductsConnection` and `CatalogProductsEdge` types.

# Extending a Connection or Edge Type

Add fields to a connection type for collection-level metadata such as aggregates, facets, or summaries. Add fields to an edge type for item-in-list metadata such as rank, relevance score, or relationship properties.

## Extending the Connection Type

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

## Extending the Edge Type

```csharp
[ExtendObjectType("ProductsEdge")]
public sealed class ProductsEdgeExtensions
{
    public int Rank([Parent] Edge<Product> edge, [Service] RankingService ranking)
        => ranking.GetRank(edge.Node.Id);
}
```

If your connection or edge type name is difficult to predict, set `ConnectionName` explicitly before adding extensions. Type extensions that use a generated name will break if the field is renamed.

When projections are active, some model properties may not be populated depending on what the client selected. Avoid reading properties in extension resolvers that are not guaranteed to be projected.

# Connections and Global Object Identification

A connection can expose any object type as `node`. Relay-style clients benefit when those node types also implement global object identification, allowing clients to refetch individual items by their stable global ID.

Global object identification is not required for connections to function, but it is recommended if you want Relay client stores to cache and refetch items by identity.

See [Relay and Global Object Identification](/docs/hotchocolate/v16/build/schema-elements/relay) for setup instructions.

# Connection Options Reference

The following table lists the options most relevant to the connection schema contract. For a complete reference, including `MaxPageSize`, `DefaultPageSize`, `RequirePagingBoundaries`, and provider configuration, see [Pagination](/docs/hotchocolate/v16/build/pagination).

| Option                         | Where to set it                  | Effect on schema                                       |
| ------------------------------ | -------------------------------- | ------------------------------------------------------ |
| `ConnectionName`               | `[UsePaging]` or `.UsePaging()`  | Sets the connection and edge type names                |
| `InferConnectionNameFromField` | Global or per field              | Controls whether names are derived from the field name |
| `IncludeTotalCount`            | `[UsePaging]` or `.UsePaging()`  | Adds `totalCount` to the connection type               |
| `IncludeNodesField`            | Global via `ModifyPagingOptions` | Adds `nodes` shortcut field to the connection type     |
| `AllowBackwardPagination`      | `[UsePaging]` or `.UsePaging()`  | Adds `last` and `before` arguments to the field        |

# Troubleshooting

**`nodes` is missing from the schema.**
Check if `IncludeNodesField` is disabled in your paging options. If it is, query items through `edges { node { ... } }`.

**`totalCount` is missing from the schema.**
Enable `IncludeTotalCount` on the field or globally. The field only appears in the SDL when this option is enabled.

**`totalCount` returns zero for a manual `Connection<T>`.**
Pass the known total as the integer argument to the `Connection<T>` constructor. The default value is zero.

**`totalCount` is null or expensive to resolve.**
Check if the paging provider can compute counts for the returned source. Only enable `IncludeTotalCount` on fields where clients need it, and review [Pagination](/docs/hotchocolate/v16/build/pagination) before adding counts to large datasets.

**Type extension fields do not appear on the connection or edge.**
Verify that the generated type names match the strings in `[ExtendObjectType(...)]`. Set `ConnectionName` explicitly to lock the names before writing extensions.

**A page accepts unbounded requests.**
Set `MaxPageSize`, `DefaultPageSize`, or `RequirePagingBoundaries` in the field or global paging options. Pair schema limits with cost analysis for public APIs.

**Pages skip or repeat items between requests.**
Ensure the resolver returns items in a deterministic order. The primary key must be the final sort key. See [Pagination](/docs/hotchocolate/v16/build/pagination) for cursor mechanics.

**Backward paging arguments (`last`, `before`) are missing.**
Check `AllowBackwardPagination`. This option defaults to `true` but may be disabled globally.

**Clients treat cursors as identifiers.**
Cursors are opaque position tokens, not global IDs. Use global object identification and the `node` root field for stable object identity.

# Next Steps

- **Set up global object identification:** [Relay and Global Object Identification](/docs/hotchocolate/v16/build/schema-elements/relay)
- **Configure paging boundaries, providers, and cursor behavior:** [Pagination](/docs/hotchocolate/v16/build/pagination)
- **Filter results on a paged field:** [Filtering](/docs/hotchocolate/v16/build/filtering-sorting-projections/filter-types)
- **Sort results on a paged field:** [Sorting](/docs/hotchocolate/v16/build/filtering-sorting-projections/sort-types)
- **Optimize database queries with projections:** [Projections](/docs/hotchocolate/v16/build/filtering-sorting-projections/projection-options)
- **Batch data fetching efficiently:** [DataLoader](/docs/hotchocolate/v16/build/dataloader)
- **Extend connection or edge types:** [Extending Types](/docs/hotchocolate/v16/build/schema-elements/extending-types)
- **Protect against expensive queries:** [Cost Analysis](/docs/hotchocolate/v16/build/security/cost-analysis)
