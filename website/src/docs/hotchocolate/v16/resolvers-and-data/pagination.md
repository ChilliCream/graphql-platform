---
title: "Pagination"
---

When a dataset is too large to return in a single response, you need pagination. Hot Chocolate implements cursor-based connection pagination following the [Relay Cursor Connections Specification](https://relay.dev/graphql/connections.htm). Connections give clients a standardized way to traverse pages using opaque cursors, and they translate directly to efficient database queries when backed by `IQueryable`.

# How Connections Work

Instead of returning a flat list, a paginated field returns a Connection. The connection wraps the data with page metadata and cursors for navigation.

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

Apply the `[UsePaging]` attribute to a resolver that returns `IEnumerable<T>` or `IQueryable<T>`. The middleware handles slicing the result, computing cursors, and building the `PageInfo`.

<ExampleTabs>
<Implementation>

```csharp
// Types/UserQueries.cs
[QueryType]
public static partial class UserQueries
{
    [UsePaging]
    public static IQueryable<User> GetUsers(CatalogContext db)
        => db.Users.OrderBy(u => u.Id);
}
```

</Implementation>
<Code>

```csharp
// Types/UserQueriesType.cs
public class UserQueriesType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("users")
            .UsePaging()
            .Resolve(context =>
            {
                var db = context.Service<CatalogContext>();
                return db.Users.OrderBy(u => u.Id);
            });
    }
}
```

</Code>
</ExampleTabs>

When backed by `IQueryable<T>`, the pagination operations translate directly to native database queries. Hot Chocolate does not load the entire dataset into memory.

# The Connection&lt;T&gt; Type

When you need full control over the pagination process, return a `Connection<T>` from your resolver. This is useful when you build cursors from an external API, implement a custom data source, or need to control the page info values.

<ExampleTabs>
<Implementation>

```csharp
// Types/UserQueries.cs
[QueryType]
public static partial class UserQueries
{
    [UsePaging]
    public static async Task<Connection<User>> GetUsersAsync(
        string? after,
        int? first,
        UserService userService,
        CancellationToken ct)
    {
        var result = await userService.GetUsersPageAsync(after, first, ct);

        var edges = result.Items
            .Select(u => new Edge<User>(u, u.Id.ToString()))
            .ToList();

        var pageInfo = new ConnectionPageInfo(
            result.HasNextPage,
            result.HasPreviousPage,
            edges.FirstOrDefault()?.Cursor,
            edges.LastOrDefault()?.Cursor);

        return new Connection<User>(
            edges,
            pageInfo,
            totalCount: _ => ValueTask.FromResult(result.TotalCount));
    }
}
```

</Implementation>
<Code>

```csharp
// Types/UserQueriesType.cs
public class UserQueriesType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("users")
            .UsePaging()
            .Resolve(async context =>
            {
                var after = context.ArgumentValue<string?>("after");
                var first = context.ArgumentValue<int?>("first");
                var userService = context.Service<UserService>();

                var result = await userService.GetUsersPageAsync(after, first);

                var edges = result.Items
                    .Select(u => new Edge<User>(u, u.Id.ToString()))
                    .ToList();

                var pageInfo = new ConnectionPageInfo(
                    result.HasNextPage,
                    result.HasPreviousPage,
                    edges.FirstOrDefault()?.Cursor,
                    edges.LastOrDefault()?.Cursor);

                return new Connection<User>(
                    edges,
                    pageInfo,
                    totalCount: _ => ValueTask.FromResult(result.TotalCount));
            });
    }
}
```

</Code>
</ExampleTabs>

# Pagination Options

You can configure pagination behavior per field or globally.

## Per-Field Options

<ExampleTabs>
<Implementation>

```csharp
// Types/UserQueries.cs
[QueryType]
public static partial class UserQueries
{
    [UsePaging(MaxPageSize = 100, DefaultPageSize = 25, IncludeTotalCount = true)]
    public static IQueryable<User> GetUsers(CatalogContext db)
        => db.Users.OrderBy(u => u.Id);
}
```

</Implementation>
<Code>

```csharp
descriptor
    .Field("users")
    .UsePaging(options: new PagingOptions
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
// Program.cs
builder.Services
    .AddGraphQLServer()
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

The `MaxPageSize` setting works together with [cost analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis) to protect your API. Cost analysis uses the `MaxPageSize` as the assumed list size when calculating the cost of a paginated field. If you increase `MaxPageSize`, the cost of queries against that field increases proportionally.

For public APIs, keep `MaxPageSize` conservative and use `RequirePagingBoundaries = true` to force clients to declare how many items they want.

# Connection Naming

The Connection and Edge type names are inferred from the field name by default. A field called `users` generates `UsersConnection` and `UsersEdge`.

Override the name with `ConnectionName`:

<ExampleTabs>
<Implementation>

```csharp
[UsePaging(ConnectionName = "TeamMembers")]
public static IQueryable<User> GetUsers(CatalogContext db)
    => db.Users.OrderBy(u => u.Id);
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
[UsePaging(IncludeTotalCount = true)]
public static IQueryable<User> GetUsers(CatalogContext db)
    => db.Users.OrderBy(u => u.Id);
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

When your resolver returns `IEnumerable<T>` or `IQueryable<T>`, the total count is computed automatically. When you return a `Connection<T>`, provide the count through the `totalCount` delegate:

```csharp
var connection = new Connection<User>(
    edges,
    pageInfo,
    totalCount: ct => ValueTask.FromResult(totalItems));
```

# Extending Connection and Edge Types

Add fields to a Connection or Edge type using type extensions. This is useful for aggregation fields or metadata.

```csharp
// Types/UsersConnectionExtension.cs
[ExtendObjectType("UsersConnection")]
public class UsersConnectionExtension
{
    public double GetAverageAge([Parent] Connection<User> connection)
    {
        return connection.Edges.Average(e => e.Node.Age);
    }
}
```

```csharp
// Types/UsersEdgeExtension.cs
[ExtendObjectType("UsersEdge")]
public class UsersEdgeExtension
{
    public int GetIndex([Parent] Edge<User> edge)
    {
        // Custom edge field logic
        return int.Parse(edge.Cursor);
    }
}
```

> If you use [projections](/docs/hotchocolate/v16/resolvers-and-data/projections), some properties on your model may not be populated depending on what the client requested.

# Nullable Cursor Keys

When your cursor key field can be `null`, you must tell Hot Chocolate how the database orders null values so that cursor-based pagination produces correct results across pages.

Set `NullOrdering` on `PagingOptions` to match your database:

| Value              | When to use                                                                    |
| ------------------ | ------------------------------------------------------------------------------ |
| `Unspecified`      | Default. The EF Core paging handler auto-detects ordering for known providers. |
| `NativeNullsFirst` | Nulls sort before non-null values (SQL Server, SQLite, in-memory LINQ).        |
| `NativeNullsLast`  | Nulls sort after non-null values (PostgreSQL default).                         |

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .ModifyPagingOptions(opt => opt.NullOrdering = NullOrdering.NativeNullsLast);
```

When `NullOrdering` is `Unspecified` and the EF Core paging handler is used, ordering is detected automatically for PostgreSQL (`NativeNullsLast`) and SQL Server, SQLite, and in-memory (`NativeNullsFirst`). For unrecognized providers, an error is thrown when nullable cursor keys are present. Set `NullOrdering` explicitly to resolve it.

# Pagination Providers

The `UsePaging` middleware provides a unified API that adapts to different data sources through pagination providers. The default provider supports `IEnumerable<T>` and `IQueryable<T>`. Other providers handle specific databases like MongoDB.

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddMongoDbPagingProviders();
```

Name a provider to reference it explicitly on specific fields:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddMongoDbPagingProviders(providerName: "MongoDB");
```

```csharp
[UsePaging(ProviderName = "MongoDB")]
public static IExecutable<User> GetUsers(IMongoCollection<User> collection)
    => collection.AsExecutable();
```

If no `ProviderName` is specified, the correct provider is selected based on the return type. If it cannot be inferred, the first registered provider is used.

[Learn more about database integrations](/docs/hotchocolate/v16/integrations)

# Troubleshooting

## "Cannot determine the element type" error

Ensure your resolver returns `IEnumerable<T>`, `IQueryable<T>`, or `Connection<T>`. If the return type is untyped (like `IEnumerable` without a generic argument), the paging middleware cannot determine the node type.

## Missing `totalCount` field

Enable `IncludeTotalCount` in `PagingOptions` or on the `[UsePaging]` attribute. This field is opt-in because computing the total count requires an additional database query.

## Cursors appear to be wrong after sorting changes

Cursors encode the position of an item based on the sort order at the time of the query. If you change the sort order between pages, cursors from the previous page may point to the wrong position. Ensure the sort order remains consistent across paginated requests.

## "Nullable cursor key requires NullOrdering" error

You have a nullable field as a cursor key and the paging handler cannot detect your database's null ordering behavior. Set `NullOrdering` explicitly in `PagingOptions`.

# Next Steps

- **Need to filter results?** See [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering).
- **Need to sort results?** See [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting).
- **Need to optimize database queries?** See [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections).
- **Need to protect against expensive queries?** See [Cost Analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis).
