---
title: "Pagination"
---

When a dataset is too large to return in a single response, you need pagination. Hot Chocolate implements cursor-based connection pagination following the [GraphQL Cursor Connections Specification](https://relay.dev/graphql/connections.htm). Connections give clients a standardized way to traverse pages using opaque cursors.

GraphQL models data as a graph of related entities. When one entity relates to a list of other entities, that relationship is called a _connection_. A `UsersConnection` for instance represents the connection between `Query` and `User`. Each _edge_ in that connection links one `User` to the parent, and carries a cursor that marks the user's position in the list.

This is more than just naming. Traditional offset pagination (`skip: 20, take: 10`) breaks when data changes between pages: inserts and deletes shift items, causing duplicates or gaps. Cursors avoid this because they point to a stable position rather than a numeric offset. The database can seek directly to the cursor position, which also means pagination performance stays constant regardless of how deep into the list the client navigates.

# How Connections Work

Instead of returning a flat list, a paginated field returns a Connection. The connection wraps the data with page metadata, cursors for navigation and optionally aggregations.

```graphql
type Query {
  users(first: Int, after: String, last: Int, before: String): UsersConnection
}

type UsersConnection {
  pageInfo: PageInfo!
  edges: [UsersEdge!]
  nodes: [User!]
}

type UsersEdge {
  cursor: String!
  node: User!
}

type PageInfo {
  hasNextPage: Boolean!
  hasPreviousPage: Boolean!
  startCursor: String
  endCursor: String
}
```

Clients use `first`/`after` to page forward and `last`/`before` to page backward. Each edge carries a cursor that points to its position in the dataset.

# Adding Pagination

<ExampleTabs>
<Implementation>

To use pagination register the paging arguments with the GraphQL builder.

```csharp
builder
    .AddGraphQL()
    .AddPagingArguments();
```

Hot Chocolate by default builds on top of the `Page<T>` which describes a single page in a dataset. A page can be used to construct a `PageConnection<T>`.

```csharp
[QueryType]
public static partial class UserQueries
{
    public static async Task<PageConnection<User>> GetUsersAsync(
        PagingArguments pagingArgs,
        CatalogContext db,
        CancellationToken cancellationToken)
        => await db.Users.OrderBy(u => u.Id).ToPageAsync(pagingArgs, cancellationToken);
}
```

</Implementation>
<Code>

To use connection-based pagination with code-first, use the `ToPageAsync` extension and map the resulting page to a `Connection<T>`.

```csharp
public class UserQueriesType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("users")
            .UsePaging()
            .Resolve(async context =>
            {
                var db = context.Service<CatalogContext>();
                return await db.Users.OrderBy(u => u.Id)
                  .ToPageAsync(pagingArgs, context.RequestAborted)
                  .ToConnectionAsync();
            });
    }
}
```

</Code>
</ExampleTabs>

The `ToPageAsync` extension method is located in one of the following packages:

- GreenDonut.Data.EntityFramework
- GreenDonut.Data.Raven
- GreenDonut.Data.Marten
- GreenDonut.Data.Mongo

# Pagination Options

You can configure pagination behavior per field or globally.

## Per-Field Options

<ExampleTabs>
<Implementation>

```csharp
[QueryType]
public static partial class UserQueries
{
    [UseConnection(MaxPageSize = 100, DefaultPageSize = 25, IncludeTotalCount = true)]
    public static async Task<PageConnection<User>> GetUsersAsync(
        PagingArguments pagingArgs,
        CatalogContext db,
        CancellationToken cancellationToken)
        => await db.Users.OrderBy(u => u.Id).ToPageAsync(pagingArgs, cancellationToken);
}
```

</Implementation>
<Code>

```csharp
descriptor
    .Field("users")
    .UsePaging(new PagingOptions
    {
        MaxPageSize = 100,
        DefaultPageSize = 25,
        IncludeTotalCount = true
    });
```

</Code>
</ExampleTabs>

## Global Defaults

Apply consistent pagination settings across your entire schema:

```csharp
builder
    .AddGraphQL()
    .ModifyPagingOptions(opt =>
    {
        opt.MaxPageSize = 100;
        opt.DefaultPageSize = 25;
        opt.IncludeTotalCount = true;
    });
```

## All PagingOptions

| Property                       | Default       | Description                                                                           |
| ------------------------------ | ------------- | ------------------------------------------------------------------------------------- |
| `MaxPageSize`                  | `50`          | Maximum number of items a client can request via `first` or `last`.                   |
| `DefaultPageSize`              | `10`          | Number of items returned if the client does not specify `first` or `last`.            |
| `IncludeTotalCount`            | `false`       | Adds a `totalCount` field to the Connection.                                          |
| `AllowBackwardPagination`      | `true`        | Includes `before` and `last` arguments on the Connection.                             |
| `RequirePagingBoundaries`      | `false`       | Requires the client to specify `first` or `last`.                                     |
| `InferConnectionNameFromField` | `true`        | Infers the Connection name from the field name instead of the return type.            |
| `ProviderName`                 | `null`        | Name of the pagination provider to use.                                               |
| `NullOrdering`                 | `Unspecified` | Controls how `null` values are ordered when a nullable field is used as a cursor key. |

# MaxPageSize and Cost Analysis

The `MaxPageSize` setting works together with [cost analysis](/docs/hotchocolate/v16/security/cost-analysis) to protect your API. Cost analysis uses the `MaxPageSize` as the assumed list size when calculating the cost of a paginated field. If you increase `MaxPageSize`, the cost of queries against that field increases proportionally.

For public APIs, keep `MaxPageSize` conservative and use `RequirePagingBoundaries = true` to force clients to declare how many items they want.

# Connection Naming

The Connection and Edge type names are inferred from the field name by default. A field called `users` generates `UsersConnection` and `UsersEdge`.

Override the name with `ConnectionName`:

<ExampleTabs>
<Implementation>

```csharp
[QueryType]
public static partial class UserQueries
{
    [UseConnection(ConnectionName = "TeamMembers")]
    public static async Task<PageConnection<User>> GetUsersAsync(
        PagingArguments pagingArgs,
        CatalogContext db,
        CancellationToken cancellationToken)
        => await db.Users.OrderBy(u => u.Id).ToPageAsync(pagingArgs, cancellationToken);
}
```

This produces `TeamMembersConnection` and `TeamMembersEdge`.

</Implementation>
<Code>

```csharp
descriptor
    .Field("users")
    .UsePaging(connectionName: "TeamMembers");
```

</Code>
</ExampleTabs>

# Total Count

Enable the `totalCount` field to let clients request the total number of items in the dataset:

<ExampleTabs>
<Implementation>

```csharp
[QueryType]
public static partial class UserQueries
{
    [UseConnection(IncludeTotalCount = true)]
    public static async Task<PageConnection<User>> GetUsersAsync(
        PagingArguments pagingArgs,
        CatalogContext db,
        CancellationToken cancellationToken)
        => await db.Users.OrderBy(u => u.Id).ToPageAsync(pagingArgs, cancellationToken);
}
```

</Implementation>
<Code>

```csharp
descriptor
    .Field("users")
    .UsePaging(options: new PagingOptions { IncludeTotalCount = true });
```

</Code>
</ExampleTabs>

# Relative Cursors

Cursor-based pagination is great for infinite scrolling, but many applications need a traditional page bar that lets users jump to a specific page (e.g. "1 2 3 ... 10"). Relative cursors bridge this gap. They let you request cursors for surrounding pages so the frontend can render a page bar while still using cursor-based navigation under the hood.

```text
  [1]  2  3  4  5  ...  10
       ↑  ↑  ↑  ↑
       forward cursors
```

When a client requests `forwardCursors` or `backwardCursors` inside `pageInfo`, Hot Chocolate returns a list of `PageCursor` objects, each containing a `page` number and the opaque `cursor` to navigate there. The frontend can render these directly as page links.

Enable relative cursors on a field with `EnableRelativeCursors`:

```csharp
[QueryType]
public static partial class UserQueries
{
    [UseConnection(EnableRelativeCursors = true)]
    public static async Task<PageConnection<User>> GetUsersAsync(
        PagingArguments pagingArgs,
        CatalogContext db,
        CancellationToken cancellationToken)
        => await db.Users.OrderBy(u => u.Id).ToPageAsync(pagingArgs, cancellationToken);
}
```

Clients can then query the relative cursors:

```graphql
query {
  users(first: 10) {
    nodes {
      id
      name
    }
    pageInfo {
      hasNextPage
      hasPreviousPage
      forwardCursors {
        page
        cursor
      }
      backwardCursors {
        page
        cursor
      }
    }
  }
}
```

The response includes cursors for surrounding pages:

```json
{
  "data": {
    "users": {
      "nodes": [ ... ],
      "pageInfo": {
        "hasNextPage": true,
        "hasPreviousPage": false,
        "forwardCursors": [
          { "page": 2, "cursor": "ezB8MXw2fTIz" },
          { "page": 3, "cursor": "ezF8MXw2fTIz" },
          { "page": 4, "cursor": "ezJ8MXw2fTIz" }
        ],
        "backwardCursors": []
      }
    }
  }
}
```

To navigate to page 3, the client sends `users(first: 10, after: "ezF8MXw2fTIz")`. By default, up to 5 cursors are returned per direction.

You can also enable relative cursors globally:

```csharp
builder
    .AddGraphQL()
    .ModifyPagingOptions(opt =>
    {
        opt.EnableRelativeCursors = true;
    });
```

> Relative cursors are only available with the implementation-first approach.

# Custom Connection Types

## Extending PageConnection

The simplest way to add fields to a connection is to inherit from `PageConnection<T>`. Any public property or method you add becomes a GraphQL field on the connection type.

```csharp
public class ProductConnection : PageConnection<Product>
{
    private readonly Page<Product> _page;

    public ProductConnection(Page<Product> page) : base(page)
    {
        _page = page;
    }

    public decimal AveragePrice => _page.Average(p => p.Price);
}
```

Return the custom connection from your resolver instead of `PageConnection<T>`:

```csharp
[QueryType]
public static partial class ProductQueries
{
    [UseConnection(IncludeTotalCount = true)]
    public static async Task<ProductConnection> GetProductsAsync(
        PagingArguments pagingArgs,
        CatalogContext db,
        CancellationToken cancellationToken)
    {
        var page = await db.Products
            .OrderBy(p => p.Id)
            .ToPageAsync(pagingArgs, cancellationToken);

        return new ProductConnection(page);
    }
}
```

## ConnectionBase for Full Control

When you need custom edge types or want to control how edges and page info are constructed, inherit from `ConnectionBase<TNode, TEdge, TPageInfo>` directly.

Start by defining a custom edge. An edge implements `IEdge<T>` and pairs a node with its cursor.

```csharp
public class ProductsEdge(Page<Product> page, PageEntry<Product> entry) : IEdge<Product>
{
    public Product Node => entry.Item;

    object? IEdge.Node => Node;

    public string Cursor => page.CreateCursor(entry);
}
```

Then build the connection around it:

```csharp
public class ProductConnection : ConnectionBase<Product, ProductsEdge, ConnectionPageInfo>
{
    private readonly Page<Product> _page;
    private ConnectionPageInfo? _pageInfo;
    private ProductsEdge[]? _edges;

    public ProductConnection(Page<Product> page)
    {
        _page = page;
    }

    public override IReadOnlyList<ProductsEdge>? Edges
    {
        get
        {
            if (_edges is null)
            {
                var entries = _page.Entries;
                var edges = new ProductsEdge[entries.Length];

                for (var i = 0; i < entries.Length; i++)
                {
                    edges[i] = new ProductsEdge(_page, entries[i]);
                }

                _edges = edges;
            }

            return _edges;
        }
    }

    public IReadOnlyList<Product>? Nodes => _page;

    public override ConnectionPageInfo PageInfo
    {
        get
        {
            if (_pageInfo is null)
            {
                var startCursor = _page.CreateStartCursor();
                var endCursor = _page.CreateEndCursor();

                _pageInfo = new ConnectionPageInfo(
                    _page.HasNextPage, _page.HasPreviousPage,
                    startCursor, endCursor);
            }

            return _pageInfo;
        }
    }

    public int TotalCount => _page.TotalCount ?? 0;
}
```

## Reusable Generic Connection

If multiple entities share the same connection structure, define a generic connection and edge. Use the `[GraphQLName("{0}Connection")]` attribute so Hot Chocolate replaces `{0}` with the entity name (e.g. `CatalogConnection<Brand>` becomes `BrandConnection`).

```csharp
[GraphQLName("{0}Edge")]
public class CatalogEdge<TEntity>(
    Page<TEntity> page,
    PageEntry<TEntity> entry) : IEdge<TEntity>
{
    public TEntity Node => entry.Item;

    object? IEdge.Node => Node;

    public string Cursor => page.CreateCursor(entry);
}
```

```csharp
[GraphQLName("{0}Connection")]
public class CatalogConnection<TEntity>
    : ConnectionBase<TEntity, CatalogEdge<TEntity>, ConnectionPageInfo>
{
    private readonly Page<TEntity> _page;
    private ConnectionPageInfo? _pageInfo;
    private CatalogEdge<TEntity>[]? _edges;

    public CatalogConnection(Page<TEntity> page)
    {
        _page = page;
    }

    public override IReadOnlyList<CatalogEdge<TEntity>> Edges
    {
        get
        {
            if (_edges is null)
            {
                var entries = _page.Entries;
                var edges = new CatalogEdge<TEntity>[entries.Length];

                for (var i = 0; i < entries.Length; i++)
                {
                    edges[i] = new CatalogEdge<TEntity>(_page, entries[i]);
                }

                _edges = edges;
            }

            return _edges;
        }
    }

    public IReadOnlyList<TEntity> Nodes => _page;

    public override ConnectionPageInfo PageInfo
    {
        get
        {
            if (_pageInfo is null)
            {
                var startCursor = _page.CreateStartCursor();
                var endCursor = _page.CreateEndCursor();

                _pageInfo = new ConnectionPageInfo(
                    _page.HasNextPage, _page.HasPreviousPage,
                    startCursor, endCursor);
            }

            return _pageInfo;
        }
    }

    public int TotalCount => _page.TotalCount ?? 0;
}
```

# Nullable Cursor Keys

When your cursor key field can be `null`, you must tell Hot Chocolate how the database orders null values so that cursor-based pagination produces correct results across pages.

Set `NullOrdering` on `PagingOptions` to match your database:

| Value              | When to use                                                                    |
| ------------------ | ------------------------------------------------------------------------------ |
| `Unspecified`      | Default. The EF Core paging handler auto-detects ordering for known providers. |
| `NativeNullsFirst` | Nulls sort before non-null values (SQL Server, SQLite, in-memory LINQ).        |
| `NativeNullsLast`  | Nulls sort after non-null values (PostgreSQL default).                         |

```csharp
builder
    .AddGraphQL()
    .ModifyPagingOptions(opt => opt.NullOrdering = NullOrdering.NativeNullsLast);
```

When `NullOrdering` is `Unspecified` and the EF Core paging handler is used, ordering is detected automatically for PostgreSQL (`NativeNullsLast`) and SQL Server, SQLite, and in-memory (`NativeNullsFirst`). For unrecognized providers, an error is thrown when nullable cursor keys are present. Set `NullOrdering` explicitly to resolve it.

[Learn more about database integrations](/docs/hotchocolate/v16/fetching-data/integrations)

# Next Steps

- **Need to filter results?** See [Filtering](/docs/hotchocolate/v16/fetching-data/filtering).
- **Need to sort results?** See [Sorting](/docs/hotchocolate/v16/fetching-data/sorting).
- **Need to optimize database queries?** See [Projections](/docs/hotchocolate/v16/fetching-data/projections).
- **Need to protect against expensive queries?** See [Cost Analysis](/docs/hotchocolate/v16/security/cost-analysis).
