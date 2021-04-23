---
title: "Pagination"
---

import { ExampleTabs } from "../../../components/mdx/example-tabs"

<!-- todo: reword and update link, once final -->

> This document starts by covering different pagination approaches. If you just want to learn how to implement pagination, head over [here](/docs/hotchocolate/fetching-data/pagination/#connections).

Pagination is one of the most common problems that we have to solve when implementing our backend. Often, sets of data are too large to pass them directly to the consumer of our service.

Pagination solves this problem by giving the consumer the ability to fetch a set in chunks.

There are various ways we could implement pagination in our server, but there are mainly two concepts we find in most GraphQL servers: _offset-based_ and _cursor-based_ pagination.

# Types of pagination

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

Luckily we can solve these issues pretty easily by switching from an _offset_ to a _cursor_. Continue reading to learn more.

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
WHERE Id >= %cursorId AND Birthday >= %cursorBirthday
ORDER BY Birthday, Id
LIMIT %limit
```

### Problems

Even though _cursor-based_ pagination is more performant than _offset-based_ pagination, it comes with a big downside.

Since we now only know of the next entry, there is no more concept of pages. If we have a feed or only _Next_ and _Previous_ buttons, this works great, but if we depend on page numbers, we are in a tight spot.

# Connections

_Connections_ are a standardized way to expose pagination to clients.

Instead of returning a list, we now return a _Connection_.

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

> Note: _Connections_ are often associated with _cursor-based_ pagination, due to the use of a _cursor_. Since the specification describes the _cursor_ as opague though, it can be used to faciliate an _offset_ as well.

## Usage

Adding pagination capabilties to our fields is a breeze. All we have to do is add the `UsePaging` middleware.

<ExampleTabs>
<ExampleTabs.Annotation>

```csharp
public class Query
{
    [UsePaging]
    public IEnumerable<User> GetUsers([Service] IUserRespository repository)
        => repository.GetUsers();
}
```

If we need to specify the concrete node type of our pagination, we can do so by passing a Type as the constructor argument `[UsePaging(typeof(User))]`.

The `UsePaging` attribute also allows us to configure some other properties, like `DefaultPageSize`, `MaxPageSize` and `IncludeTotalCount`. Example:

```csharp
[UsePaging(MaxPageSize = 50)]
```

</ExampleTabs.Annotation>
<ExampleTabs.Code>

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
                var repository = context.Service<IUserRespository>();

                return repository.GetUsers();
            });
    }
}
```

If we need to specify the concrete node type of our pagination, we can do so via the generic argument: `UsePaging<UserType>()`.

We can also configure the `UsePaging` middleware further, by specifying `PagingOptions`.

```csharp
.UsePaging(options: new PagingOptions
{
    MaxPageSize = 50
});
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

TODO

</ExampleTabs.Schema>
</ExampleTabs>

For the `UsePaging` middleware to work, our resolver needs to return an `IEnumerable<T>` or an `IQueryable<T>`. The middleware will then apply the pagination arguments to what we have returned. In the case of an `IQueryable<T>` this means that the pagination operations can be directly translated to native database queries, through database drivers like EntityFramework or the MongoDB client.

## Customization

If we need more control over the pagination process we can do so, by returning a `Connection<T>`.

<ExampleTabs>
<ExampleTabs.Annotation>

```csharp
public class Query
{
    [UsePaging]
    public Connection<User> GetUsers(string? after, int? first, string sortBy)
    {
        IEnumerable<User> users = null; // get users using the above arguments

        var edges = users.Select(user => new Edge<User>(user, user.Id)).ToList();
        var pageInfo = new ConnectionPageInfo(false, false, null, null);

        var connection = new Connection<User>(edges, pageInfo,
                            ct => ValueTask.FromResult(0));

        return connection;
    }
}
```

</ExampleTabs.Annotation>
<ExampleTabs.Code>

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
                var sortBy = context.ArgumentValue<string?>("sortBy");

                IEnumerable<User> users = null; // get users using the above arguments

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

If we need to work on an even lower level, we could also use `descriptor.AddPagingArguments()` and `descriptor.Type<ConnectionType<TType>>()` to get rid of the `UsePaging` middleware.

</ExampleTabs.Code>
<ExampleTabs.Schema>

TODO

</ExampleTabs.Schema>
</ExampleTabs>

## Total count

TODO

## Custom Edges

TODO

# Offset Pagination

> Note: While we support _offset_ pagination, we highly encourage the use of [_Connections_](/docs/hotchocolate/fetching-data/pagination/#connections) instead. _Connections_ provide an abstraction which makes it easier to switch to another pagination mechanism later on.

## Usage

TODO

## Customization

TODO

## Total count

TODO
