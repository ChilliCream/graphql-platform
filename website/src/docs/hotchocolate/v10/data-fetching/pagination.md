---
title: Pagination
---

Pagination is one of the most common problems that you have to solve when implementing your backend. Often, sets of data are too large to pass them directly to the consumer of your service.

Pagination solves this problem by giving the consumer the capability to fetch a set in chunks.

There are various ways to implement pagination in your server and you can basically do what ever feels best for you.

However, there are two models that you see in most GraphQL server implementations and we have some specific helpers for the later one.

# Offset-based

Offset-based pagination — also called numbered pages — is a very common pattern.

Offset-based pagination is found in many server implementation whether the backend is implemented in _SOAP_, _REST_ or _GraphQL_.

Most databases enable you to simply skip and take records. The simplest way to provide such a capability is to add an argument _skip_ and an argument _take_ like in the following example.

```csharp
public class Query
{
    private readonly _strings = new List<string> { "a", "b", "c", "d", "e", "f", "g" };

    public IEnumerable<string> GetStrings(int? skip, int? take)
    {
        IEnumerable<string> strings = _strings;

        if(skip.HasValue)
        {
            strings = strings.Skip(skip.Value);
        }

        if(take.HasValue)
        {
            strings = strings.Take(take.Value);
        }

        return strings;
    }
}
```

# Relay-style cursor pagination

In cursor-based pagination, a cursor is used to keep track of where in the data set the next items should be fetched from. The cursor can contain various information like the index of the record within the set and properties that the server can use to recreate the set.

Relay’s support for pagination relies on the GraphQL server exposing connections in a standardized way. In the query, the connection model provides a standard mechanism for slicing and paginating the result set.

Hot Chocolate provides many helpers to make implementing a relay-style cursor pagination a simple task.

## Pagination support through `IQueryable<T>`

Let us start with something simple and then drill deeper into more complex solutions. For our first example let us assume we have an in-memory list of strings that we do want to expose as paginated list.

```csharp
public class Query
{
    public ICollection<string> Strings { get; } =
        new List<string> { "a", "b", "c", "d", "e", "f", "g" };
}
```

In order to tell Hot Chocolate that an `IEnumerable<T>` or an `IQueryable<T>` shall be exposed as page-able list in our schema we have to declare that in a schema type.

```csharp
public class QueryType
    : ObjectType<Query>
{
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor.Field(t => t.Strings).UsePaging<StringType>();
    }
}
```

`UsePaging` adds the relay-style cursor pagination arguments defined by the spec, defines the return type of the field to be `ConnectionType<StringType>` and adds a paging field middleware to the field resolver pipeline.

The middleware can handle `IQueryable<T>` and `IEnumerable<T>`. This means that you can apply the middleware also to database drivers like entity framework or the Mongo db client.

The database drivers will translate the queryable actions into native database queries.

If you now want to support filtering and/or sorting on a page-able list you have to feed the sorting properties to the paging middleware so that the middleware can include them into the cursors. The cursors can then be used to recreate the data set in fetch more queries.

Let's enhance our example and add the capability to sort our list in descending order.

We will do that by adding another argument `descending` to our field. If the argument is set to `true` than the list is sorted by descending order otherwise the set is sorted in ascending order.

```csharp
public class QueryType
    : ObjectType<Query>
{
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor.Field(t => t.Strings)
            .Argument("descending", a => a.Type<BooleanType>())
            .UsePaging<StringType>()
            .Resolver(ctx =>
            {
                IDictionary<string, object> cursorProperties =
                    ctx.GetCursorProperties();

                // get the sort order from the sorting argument or from a cursor that was passed in.
                bool descending = cursorProperties.TryGetValue("descending", out object d)
                    ? (bool)d
                    : ctx.Argument<bool>("descending");

                // set the cursor sorting property.
                cursorProperties["descending"] = descending;

                IEnumerable<string> strings = ctx.Parent<Query>().Strings;

                // return the sorted string dataset with the cursor properties.
                return descending
                    ? new PageableData<string>(strings.OrderByDescending(t => t), cursorProperties)
                    : new PageableData<string>(strings.OrderBy(t => t), cursorProperties);
            });
    }
}
```

The previous example shows how we can access the cursor sorting properties and how we can pass the cursor sorting properties to the middleware.

Our default solution makes it very easy to provide paging capabilities, but a custom optimized paging could yield better performance.

For this you can extend our `QueryableConnectionResolver` implementation or opt in to implement `IConnection` by yourself.

Let us first have a look how you can pass in an extended queryable resolver to our paging middleware.

```csharp
public class QueryType
    : ObjectType<Query>
{
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor.Field(t => t.Strings)
            .Argument("descending", a => a.Type<BooleanType>())
            .UsePaging<StringType>((source, pagingDetails) =>
                new QueryableConnectionResolver<T>(
                    source, pagingDetails))
            .Resolver(ctx =>
            {
                IDictionary<string, object> cursorProperties =
                    ctx.GetCursorProperties();

                // get the sort order from the sorting argument or from a cursor that was passed in.
                bool descending = cursorProperties.TryGetValue("descending", out object d)
                    ? (bool)d
                    : ctx.Argument<bool>("descending");

                // set the curosr sorting property.
                cursorProperties["descending"] = descending;

                IEnumerable<string> strings = ctx.Parent<Query>().Strings;

                // return the sorted string dataset with the cursor properties.
                return descending
                    ? new PageableData<string>(strings.OrderByDescending(t => t), cursorProperties)
                    : new PageableData<string>(strings.OrderBy(t => t), cursorProperties);
            });
    }
}
```

The `UsePaging` extension provides an overload in which you can pass in a factory that creates a connection resolver.

## Pagination support for stored procedures and other sources

In case you want to provide pagination support for stored procedures or other data sources Hot Chocolate allows you to do that as well.

Our generic connection type expects the executed page to be of the type `IConnection`. So, basically the field resolver just has to return a class implementing that interface or using our default implementation `Connection<T>`.

```csharp
public class QueryType
    : ObjectType<Query>
{
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor.Field("strings")
            .AddPagingArguments()
            .Type<ConnectionType<StringType>>()
            .Resolver(ctx =>
            {
                // resolver logic that returns IConnection data.
            });
    }
}
```

You can implement your data resolver logic as resolver or if it is generalized enough you could implement it as a field middleware.

A field middleware can be declared on the field or on the schema depending on what you want to do.

Let's say you want to write a middleware to provide pagination support specifically for SQL server, then you could provide that as a middleware like we did for `IQueryable<T>`.

If you need help implementing a pagination solution just reach out to us. We are happy to help you.

[Relay Cursor Connections Specification](https://relay.dev/graphql/connections.htm)
