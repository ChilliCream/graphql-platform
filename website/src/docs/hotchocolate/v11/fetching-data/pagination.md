---
title: "Pagination"
---

Pagination is one of the most common problems that we have to solve when implementing our backend. Often, sets of data are too large to pass them directly to the consumer of our service.

Pagination solves this problem by giving the consumer the ability to fetch a set in chunks.

# Connections

_Connections_ are a standardized way to expose pagination to clients.

Instead of returning a list of entries, we return a _Connection_.

```sdl
type Query {
  users(first: Int after: String last: Int before: String): UserConnection
}

type UserConnection {
  pageInfo: PageInfo!
  edges: [UserEdge!]
  nodes: [User!]
}

type UserEdge {
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

You can learn more about this in the [GraphQL Cursor Connections Specification](https://relay.dev/graphql/connections.htm).

> Note: _Connections_ are often associated with _cursor-based_ pagination, due to the use of a _cursor_. Nonetheless, since the specification describes the _cursor_ as opaque, it can be used to facilitate an _offset_ as well.

## Usage

Adding pagination capabilities to our fields is a breeze. All we have to do is add the `UsePaging` middleware.

<ExampleTabs>
<Implementation>

```csharp
public class Query
{
    [UsePaging]
    public IEnumerable<User> GetUsers([Service] IUserRepository repository)
        => repository.GetUsers();
}
```

If we need to specify the concrete node type of our pagination, we can do so by passing a Type as the constructor argument `[UsePaging(typeof(User))]`.

The `UsePaging` attribute also allows us to configure some other properties, like `DefaultPageSize`, `MaxPageSize` and `IncludeTotalCount`.

```csharp
[UsePaging(MaxPageSize = 50)]
```

</Implementation>
<Code>

```csharp
public class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("users")
            .UsePaging()
            .Resolve(context =>
            {
                var repository = context.Service<IUserRepository>();

                return repository.GetUsers();
            });
    }
}
```

If we need to specify the concrete node type of our pagination, we can do so via the generic argument: `UsePaging<UserType>()`.

We can also configure the `UsePaging` middleware further, by specifying `PagingOptions`.

```csharp
descriptor.UsePaging(options: new PagingOptions
{
    MaxPageSize = 50
});
```

</Code>
<Schema>

⚠️ Schema-first does currently not support pagination!

</Schema>
</ExampleTabs>

For the `UsePaging` middleware to work, our resolver needs to return an `IEnumerable<T>` or an `IQueryable<T>`. The middleware will then apply the pagination arguments to what we have returned. In the case of an `IQueryable<T>` this means that the pagination operations can be directly translated to native database queries, through database drivers like EntityFramework or the MongoDB client.

## Customization

If we need more control over the pagination process we can do so, by returning a `Connection<T>`.

<ExampleTabs>
<Implementation>

```csharp
public class Query
{
    [UsePaging]
    public Connection<User> GetUsers(string? after, int? first, string sortBy)
    {
        // get users using the above arguments
        IEnumerable<User> users = null;

        var edges = users.Select(user => new Edge<User>(user, user.Id))
                            .ToList();
        var pageInfo = new ConnectionPageInfo(false, false, null, null);

        var connection = new Connection<User>(edges, pageInfo,
                            ct => ValueTask.FromResult(0));

        return connection;
    }
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
            .Field("users")
            .UsePaging()
            .Argument("sortBy", a => a.Type<NonNullType<StringType>>())
            .Resolve(context =>
            {
                var after = context.ArgumentValue<string?>("after");
                var first = context.ArgumentValue<int?>("first");
                var sortBy = context.ArgumentValue<string>("sortBy");

                // get users using the above arguments
                IEnumerable<User> users = null;

                var edges = users.Select(user => new Edge<User>(user, user.Id))
                                    .ToList();
                var pageInfo = new ConnectionPageInfo(false, false, null, null);

                var connection = new Connection<User>(edges, pageInfo,
                                    ct => ValueTask.FromResult(0));

                return connection;
            });
    }
}
```

If we need to work on an even lower level, we could also use `descriptor.AddPagingArguments()` and `descriptor.Type<ConnectionType<UserType>>()` to get rid of the `UsePaging` middleware.

</Code>
<Schema>

⚠️ Schema-first does currently not support pagination!

</Schema>
</ExampleTabs>

## Total count

Sometimes we might want to return the total number of pageable entries.

For this to work we need to enable the `IncludeTotalCount` flag on the `UsePaging` middleware.

<ExampleTabs>
<Implementation>

```csharp
[UsePaging(IncludeTotalCount = true)]
```

</Implementation>
<Code>

```csharp
descriptor.UsePaging(options: new PagingOptions
{
    IncludeTotalCount = true
});
```

</Code>
<Schema>

⚠️ Schema-first does currently not support pagination!

</Schema>
</ExampleTabs>

This will add a new field called `totalCount` to our _Connection_.

```sdl
type UserConnection {
  pageInfo: PageInfo!
  edges: [UserEdge!]
  nodes: [User!]
  totalCount: Int!
}
```

If our resolver returns an `IEnumerable<T>` or an `IQueryable<T>` the `totalCount` will be automatically computed, if it has been specified as a subfield in the query.

If we have customized our pagination and our resolver now returns a `Connection<T>`, we have to explicitly declare how the `totalCount` value is computed.

```csharp
var connection = new Connection<User>(
    edges,
    pageInfo,
    getTotalCount: cancellationToken => ValueTask.FromResult(0));
```

# Offset Pagination

> Note: While we support _offset-based_ pagination, we highly encourage the use of [_Connections_](#connections) instead. _Connections_ provide an abstraction which makes it easier to switch to another pagination mechanism later on.

Besides _Connections_ we can also expose a more traditional _offset-based_ pagination.

```sdl
type Query {
  users(skip: Int take: Int): UserCollectionSegment
}

type UserCollectionSegment {
  items: [User!]
  pageInfo: CollectionSegmentInfo!
}

type CollectionSegmentInfo {
  hasNextPage: Boolean!
  hasPreviousPage: Boolean!
}
```

## Usage

To add _offset-based_ pagination capabilities to our fields we have to add the `UseOffsetPaging` middleware.

<ExampleTabs>
<Implementation>

```csharp
public class Query
{
    [UseOffsetPaging]
    public IEnumerable<User> GetUsers([Service] IUserRepository repository)
        => repository.GetUsers();
}
```

If we need to specify the concrete node type of our pagination, we can do so by passing a Type as the constructor argument `[UseOffsetPaging(typeof(User))]`.

The `UseOffsetPaging` attribute also allows us to configure some other properties, like `DefaultPageSize`, `MaxPageSize` and `IncludeTotalCount`.

```csharp
[UseOffsetPaging(MaxPageSize = 50)]
```

</Implementation>
<Code>

```csharp
public class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("users")
            .UseOffsetPaging()
            .Resolve(context =>
            {
                var repository = context.Service<IUserRepository>();

                return repository.GetUsers();
            });
    }
}
```

If we need to specify the concrete node type of our pagination, we can do so via the generic argument: `UseOffsetPaging<UserType>()`.

We can also configure the `UseOffsetPaging` middleware further, by specifying `PagingOptions`.

```csharp
descriptor.UseOffsetPaging(options: new PagingOptions
{
    MaxPageSize = 50
});
```

</Code>
<Schema>

⚠️ Schema-first does currently not support pagination!

</Schema>
</ExampleTabs>

For the `UseOffsetPaging` middleware to work, our resolver needs to return an `IEnumerable<T>` or an `IQueryable<T>`. The middleware will then apply the pagination arguments to what we have returned. In the case of an `IQueryable<T>` this means that the pagination operations can be directly translated to native database queries, through database drivers like EntityFramework or the MongoDB client.

## Customization

If we need more control over the pagination process we can do so, by returning a `CollectionSegment<T>`.

<ExampleTabs>
<Implementation>

```csharp
public class Query
{
    [UseOffsetPaging]
    public CollectionSegment<User> GetUsers(int? skip, int? take, string sortBy)
    {
        /// get users using the above arguments
        IEnumerable<User> users = null;

        var pageInfo = new CollectionSegmentInfo(false, false);

        var collectionSegment = new CollectionSegment<User>(
            users,
            pageInfo,
            ct => ValueTask.FromResult(0));

        return collectionSegment;
    }
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
            .Field("users")
            .UseOffsetPaging()
            .Argument("sortBy", a => a.Type<NonNullType<StringType>>())
            .Resolve(context =>
            {
                var skip = context.ArgumentValue<int?>("skip");
                var take = context.ArgumentValue<int?>("take");
                var sortBy = context.ArgumentValue<string>("sortBy");

                // get users using the above arguments
                IEnumerable<User> users = null;

                var pageInfo = new CollectionSegmentInfo(false, false);

                var collectionSegment = new CollectionSegment<User>(
                    users,
                    pageInfo,
                    ct => ValueTask.FromResult(0));

                return collectionSegment;
            });
    }
}
```

If we need to work on an even lower level, we could also use `descriptor.AddOffsetPagingArguments()` and `descriptor.Type<CollectionSegmentType<UserType>>()` to get rid of the `UseOffsetPaging` middleware.

</Code>
<Schema>

⚠️ Schema-first does currently not support pagination!

</Schema>
</ExampleTabs>

## Total count

Sometimes we might want to return the total number of pageable entries.

For this to work we need to enable the `IncludeTotalCount` flag on the `UseOffsetPaging` middleware.

<ExampleTabs>
<Implementation>

```csharp
[UseOffsetPaging(IncludeTotalCount = true)]
```

</Implementation>
<Code>

```csharp
descriptor.UseOffsetPaging(options: new PagingOptions
{
    IncludeTotalCount = true
});
```

</Code>
<Schema>

⚠️ Schema-first does currently not support pagination!

</Schema>
</ExampleTabs>

This will add a new field called `totalCount` to our _CollectionSegment_.

```sdl
type UserCollectionSegment {
  pageInfo: CollectionSegmentInfo!
  items: [User!]
  totalCount: Int!
}
```

If our resolver returns an `IEnumerable<T>` or an `IQueryable<T>` the `totalCount` will be automatically computed, if it has been specified as a subfield in the query.

If we have customized our pagination and our resolver now returns a `CollectionSegment<T>`, we have to explicitly declare how the `totalCount` value is computed.

```csharp
var collectionSegment = new CollectionSegment<User>(
    items,
    pageInfo,
    getTotalCount: cancellationToken => ValueTask.FromResult(0));
```

# Pagination defaults

If we want to enforce consistent pagination defaults throughout our app, we can do so, by setting the global `PagingOptions`.

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            // ...
            .SetPagingOptions(new PagingOptions
            {
                MaxPageSize = 50
            });
    }
}
```

# Types of pagination

In this section we will look at the most common pagination approaches and their downsides. There are mainly two concepts we find today: _offset-based_ and _cursor-based_ pagination.

> Note: This section is intended as a brief overview and should not be treated as a definitive guide or recommendation.

## Offset Pagination

_Offset-based_ pagination is found in many server implementations whether the backend is implemented in SOAP, REST or GraphQL.

It is so common, since it is the simplest form of pagination we can implement. All it requires is an `offset` (start index) and a `limit` (number of entries) argument.

```sql
SELECT * FROM Users
ORDER BY Id
LIMIT %limit OFFSET %offset
```

### Problems

But whilst _offset-based_ pagination is simple to implement and works relatively well, there are also some problems:

- Using `OFFSET` on the database-side does not scale well for large datasets. Most databases work with an index instead of numbered rows. This means the database always has to count _offset + limit_ rows, before discarding the _offset_ and only returning the requested number of rows.

- If new entries are written to or removed from our database at high frequency, the _offset_ becomes unreliable, potentially skipping or returning duplicate entries.

## Cursor Pagination

Contrary to the _offset-based_ pagination, where we identify the position of an entry using an _offset_, _cursor-based_ pagination works by returning the pointer to the next entry in our pagination.

To understand this concept better, let's look at an example: We want to paginate over the users in our application.

First we execute the following to receive our first page:

```sql
SELECT * FROM Users
ORDER BY Id
LIMIT %limit
```

`%limit` is actually `limit + 1`. We are doing this to know wether there are more entries in our dataset and to receive the _cursor_ of the next entry (in this case its `Id`). This additional entry will not be returned to the consumer of our pagination.

To now receive the second page, we execute:

```sql
SELECT * FROM Users
WHERE Id >= %cursor
ORDER BY Id
LIMIT %limit
```

Using `WHERE` instead of `OFFSET` is great, since now we can leverage the index of the `Id` field and the database does not have to compute an _offset_.

For this to work though, our _cursor_ needs to be **unique** and **sequential**. Most of the time the _Id_ field will be the best fit.

But what if we need to sort by a field that does not have the aforementioned properties? We can simply combine the field with another field, which has the needed properties (like `Id`), to form a _cursor_.

Let's look at another example: We want to paginate over the users sorted by their birthday.

After receiving the first page, we create a combined _cursor_, like `"1435+2020-12-31"` (`Id` + `Birthday`), of the next entry. To receive the second page, we convert the _cursor_ to its original values (`Id` + `Birthday`) and use them in our query:

```sql
SELECT * FROM Users
WHERE (Birthday >= %cursorBirthday
OR (Birthday = %cursorBirthday AND Id >= %cursorId))
ORDER BY Birthday, Id
LIMIT %limit
```

### Problems

Even though _cursor-based_ pagination can be more performant than _offset-based_ pagination, it comes with some downsides as well:

- When using `WHERE` and `ORDER BY` on a field without an index, it can be slower than using `ORDER BY` with `OFFSET`.

- Since we now only know of the next entry, there is no more concept of pages. If we have a feed or only _Next_ and _Previous_ buttons, this works great, but if we depend on page numbers, we are in a tight spot.
