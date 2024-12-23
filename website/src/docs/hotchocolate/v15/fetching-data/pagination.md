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
  users(first: Int after: String last: Int before: String): UsersConnection
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

You can learn more about this in the [GraphQL Cursor Connections Specification](https://relay.dev/graphql/connections.htm).

> Note: _Connections_ are often associated with _cursor-based_ pagination, due to the use of a _cursor_. Nonetheless, since the specification describes the _cursor_ as opaque, it can be used to facilitate an _offset_ as well.

## Definition

Adding pagination capabilities to our fields is a breeze. All we have to do is add the `UsePaging` middleware.

<ExampleTabs>
<Implementation>

```csharp
public class Query
{
    [UsePaging]
    public IEnumerable<User> GetUsers(IUserRepository repository)
        => repository.GetUsers();
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
            .Resolve(context =>
            {
                var repository = context.Service<IUserRepository>();

                return repository.GetUsers();
            });
    }
}
```

</Code>
<Schema>

In the schema-first approach we define the resolver in the same way we would in the implementation-first approach.

To make our life easier, we do not have to write out the _Connection_ types in our schema, we can simply return a list of our type, e.g. `[User]`. If the resolver for this field is annotated to use pagination, Hot Chocolate will automatically rewrite the field to return a proper _Connection_ type.

```csharp
public class Query
{
    [UsePaging]
    public IEnumerable<User> GetUsers(IUserRepository repository)
        => repository.GetUsers();
}
```

```csharp
builder.Services
    .AddGraphQLServer()
    .AddDocumentFromString(@"
        type Query {
            users : [User!]!
        }
    ")
    .AddResolver<Query>();
```

</Schema>
</ExampleTabs>

For the `UsePaging` middleware to work, our resolver needs to return an `IEnumerable<T>` or an `IQueryable<T>`. The middleware will then apply the pagination arguments to what we have returned. In the case of an `IQueryable<T>` this means that the pagination operations can be directly translated to native database queries.

We also offer pagination integrations for some database technologies that do not use `IQueryable`.

[Learn more about pagination providers](#providers)

## Naming

The name of the _Connection_ and Edge type is automatically inferred from the field name. If our field is called `users`, a `UsersConnection` and `UsersEdge` type is automatically generated.

We can also specify a custom name for our _Connection_ like the following.

<ExampleTabs>
<Implementation>

```csharp
public class Query
{
    [UsePaging(ConnectionName = "CustomUsers")]
    public IEnumerable<User> GetUsers(IUserRepository repository)
    {
        // Omitted code for brevity
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
            .UsePaging(connectionName: "CustomUsers")
            .Resolve(context =>
            {
                // Omitted code for brevity
            });
    }
}
```

</Code>
<Schema>

Take a look at the implementation-first or code-first example.

</Schema>
</ExampleTabs>

The strings `Connection` and `Edge` are automatically appended to this user specified value to form the names of the _Connection_ and Edge types.

## Options

We can define a number of options on a per-field basis.

<ExampleTabs>
<Implementation>

In the implementation-first approach we can define these options using properties on the `[UsePaging]` attribute.

```csharp
[UsePaging(MaxPageSize = 100)]
```

</Implementation>
<Code>

In the code-first approach we can pass an instance of `PagingOptions` to the `UsePaging` middleware.

```csharp
descriptor.Field("users").UsePaging(options: new PagingOptions
{
    MaxPageSize = 100
});
```

</Code>
<Schema>

Take a look at the implementation-first or code-first example.

</Schema>
</ExampleTabs>

[Learn more about the possible PagingOptions](#pagingoptions)

## Changing the node type

Lets say we are returning a collection of `string` from our pagination resolver, but we want these `string` to be represented in the schema using the `ID` scalar.

For this we can specifically tell the `UsePaging` middleware, which type to use in the schema for representation of the returned CLR type.

<ExampleTabs>
<Implementation>

```csharp
public class Query
{
    [UsePaging(typeof(IdType))]
    public IEnumerable<string> GetIds()
    {
        // Omitted code for brevity
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
            .Field("ids")
            .UsePaging<IdType>()
            .Resolve(context =>
            {
                // Omitted code for brevity
            });
    }
}
```

</Code>
<Schema>

Take a look at the implementation-first or code-first example..

</Schema>
</ExampleTabs>

The same applies of course, if we are returning a collection of `User` from our pagination resolver, but we want to use the `UserType` for representation in the schema.

## Custom pagination logic

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

</Code>
<Schema>

Take a look at the implementation-first or code-first example.

</Schema>
</ExampleTabs>

## Adding fields to an Edge

We can add new fields to an Edge type, by creating a type extension that targets the Edge type by its name.

If our Edge is named `UsersEdge`, we can add a new field to it like the following.

```csharp
[ExtendObjectType("UsersEdge")]
public class UsersEdge
{
    public string NewField([Parent] Edge<User> edge)
    {
        var cursor = edge.Cursor;
        var user = edge.Node;

        // Omitted code for brevity
    }
}
```

[Learn more about extending types](/docs/hotchocolate/v15/defining-a-schema/extending-types)

## Adding fields to a Connection

We can add new fields to a _Connection_ type, by creating a type extension that targets the _Connection_ type by its name.

If our _Connection_ is named `UsersConnection`, we can add a new field to it like the following.

```csharp
[ExtendObjectType("UsersConnection")]
public class UsersConnectionExtension
{
    public string NewField()
    {
        // Omitted code for brevity
    }
}
```

[Learn more about extending types](/docs/hotchocolate/v15/defining-a-schema/extending-types)

These additional fields are great to perform aggregations either on the entire dataset, by for example issuing a second database call, or on top of the paginated result.

We can access the pagination result like the following:

```csharp
[ExtendObjectType("UsersConnection")]
public class UsersConnectionExtension
{
    public string NewField([Parent] Connection<User> connection)
    {
        var result = connection.Edges.Sum(e => e.Node.SomeField);

        // Omitted code for brevity
    }
}
```

> Note: If you are using [Projections](/docs/hotchocolate/v15/fetching-data/projections), be aware that some properties on your model might not be set, depending on what the user queried for.

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

Take a look at the implementation-first or code-first example.

</Schema>
</ExampleTabs>

This will add a new field called `totalCount` to our _Connection_.

```sdl
type UsersConnection {
  pageInfo: PageInfo!
  edges: [UsersEdge!]
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

## Definition

To add _offset-based_ pagination capabilities to our fields we have to add the `UseOffsetPaging` middleware.

<ExampleTabs>
<Implementation>

```csharp
public class Query
{
    [UseOffsetPaging]
    public IEnumerable<User> GetUsers(IUserRepository repository)
        => repository.GetUsers();
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
            .Resolve(context =>
            {
                var repository = context.Service<IUserRepository>();

                return repository.GetUsers();
            });
    }
}
```

</Code>
<Schema>

Take a look at the implementation-first or code-first example.

</Schema>
</ExampleTabs>

For the `UseOffsetPaging` middleware to work, our resolver needs to return an `IEnumerable<T>` or an `IQueryable<T>`. The middleware will then apply the pagination arguments to what we have returned. In the case of an `IQueryable<T>` this means that the pagination operations can be directly translated to native database queries.

We also offer pagination integrations for some database technologies that do not use `IQueryable`.

[Learn more about pagination providers](#providers)

## Naming

The name of the CollectionSegment type is inferred from the item type name. If our field returns a collection of `UserType` and the name of this type is `User`, the CollectionSegment will be called `UserCollectionSegment`.

## Options

We can define a number of options on a per-field basis.

<ExampleTabs>
<Implementation>

In the implementation-first approach we can define these options using properties on the `[UseOffsetPaging]` attribute.

```csharp
[UseOffsetPaging(MaxPageSize = 100)]
```

</Implementation>
<Code>

In the code-first approach we can pass an instance of `PagingOptions` to the `UseOffsetPaging` middleware.

```csharp
descriptor.Field("users").UseOffsetPaging(options: new PagingOptions
{
    MaxPageSize = 100
});
```

</Code>
<Schema>

Take a look at the implementation-first or code-first example.

</Schema>
</ExampleTabs>

[Learn more about the possible PagingOptions](#pagingoptions)

## Changing the item type

Lets say we are returning a collection of `string` from our pagination resolver, but we want these `string` to be represented in the schema using the `ID` scalar.

For this we can specifically tell the `UseOffsetPaging` middleware, which type to use in the schema for representation of the returned CLR type.

<ExampleTabs>
<Implementation>

```csharp
public class Query
{
    [UseOffsetPaging(typeof(IdType))]
    public IEnumerable<string> GetIds()
    {
        // Omitted code for brevity
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
            .Field("ids")
            .UseOffsetPaging<IdType>()
            .Resolve(context =>
            {
                // Omitted code for brevity
            });
    }
}
```

</Code>
<Schema>

Take a look at the implementation-first or code-first example..

</Schema>
</ExampleTabs>

The same applies of course, if we are returning a collection of `User` from our pagination resolver, but we want to use the `UserType` for representation in the schema.

## Custom pagination logic

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

</Code>
<Schema>

Take a look at the implementation-first or code-first example..

</Schema>
</ExampleTabs>

## Adding fields to a CollectionSegment

We can add new fields to a CollectionSegment type, by creating a type extension that targets the CollectionSegment by its name.

If our CollectionSegment is named `UserCollectionSegment`, we can add a new field to it like the following.

```csharp
[ExtendObjectType("UserCollectionSegment")]
public class UserCollectionSegmentExtension
{
    public string NewField()
    {
        // Omitted code for brevity
    }
}
```

[Learn more about extending types](/docs/hotchocolate/v15/defining-a-schema/extending-types)

These additional fields are great to perform aggregations either on the entire dataset, by for example issuing a second database call, or on top of the paginated result.

We can access the pagination result like the following:

```csharp
[ExtendObjectType("UserCollectionSegment")]
public class UserCollectionSegmentExtension
{
    public string NewField([Parent] CollectionSegment<User> collectionSegment)
    {
        var result = collectionSegment.Items.Sum(i => i.SomeField);

        // Omitted code for brevity
    }
}
```

> Note: If you are using [Projections](/docs/hotchocolate/v15/fetching-data/projections), be aware that some properties on your model might not be set, depending on what the user queried for.

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

Take a look at the implementation-first or code-first example.

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

# Providers

The `UsePaging` and `UseOffsetPaging` middleware provide a unified way of applying pagination to our resolvers. Depending on the data source used within the resolver the pagination mechanism needs to be different though. Hot Chocolate includes so called paging providers that allow us to use the same API, e.g. `UsePaging`, but for different data sources, e.g. MongoDB and SQL.

Paging providers can be registered using various methods on the `IRequestExecutorBuilder`. For example the MongoDB paging provider can be registered like the following.

```csharp
builder.Services
    .AddGraphQLServer()
    .AddMongoDbPagingProviders();
```

[Consult the specific integration documentation for more details](/docs/hotchocolate/v15/integrations)

When registering paging providers we can name them to be able to explicitly reference them.

```csharp
builder.Services
    .AddGraphQLServer()
    .AddMongoDbPagingProviders(providerName: "MongoDB");
```

They can then be referenced like the following.

<ExampleTabs>
<Implementation>

```csharp
[UsePaging(ProviderName = "MongoDB")]
public IEnumerable<User> GetUsers()
```

</Implementation>
<Code>

```csharp
descriptor
    .Field("users")
    .UsePaging(options: new PagingOptions
    {
        ProviderName = "MongoDB"
    })
```

</Code>
<Schema>

Take a look at the implementation-first or code-first example.

</Schema>
</ExampleTabs>

If no `ProviderName` is specified, the correct provider is selected based on the return type of the resolver. If the provider to use can't be inferred from the return type, the first (default) provider is used automatically. If needed we can mark a paging provider as the explicit default.

```csharp
builder.Services
    .AddGraphQLServer()
    .AddMongoDbPagingProviders(defaultProvider: true);
```

If no paging providers have been registered, a default paging provider capable of handling `IEnumerable<T>` and `IQueryable<T>` is used.

# PagingOptions

`PagingOptions` can either be defined on a per-field basis or [globally](#pagination-defaults).

The following options can be configured.

| Property                       | Default | Description                                                                         |
| ------------------------------ | ------- | ----------------------------------------------------------------------------------- |
| `MaxPageSize`                  | `50`    | Maximum number of items a client can request via `first`, `last` or `take`.         |
| `DefaultPageSize`              | `10`    | The default number of items, if a client does not specify`first`, `last` or `take`. |
| `IncludeTotalCount`            | `false` | Add a `totalCount` field for clients to request the total number of items.          |
| `AllowBackwardPagination`      | `true`  | Include `before` and `last` arguments on the _Connection_.                          |
| `RequirePagingBoundaries`      | `false` | Clients need to specify either `first`, `last` or `take`.                           |
| `InferConnectionNameFromField` | `true`  | Infer the name of the _Connection_ from the field name rather than its type.        |
| `ProviderName`                 | `null`  | The name of the pagination provider to use.                                         |

# Pagination defaults

If we want to enforce consistent pagination defaults throughout our app, we can do so by modifying the global `PagingOptions`.

```csharp
builder.Services
    .AddGraphQLServer()
    .ModifyPagingOptions(opt => opt.MaxPageSize = 100);
```

[Learn more about possible PagingOptions](#pagingoptions)

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
