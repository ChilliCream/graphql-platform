---
title: "Resolvers"
---

A resolver is a function that produces the value for a field in your GraphQL schema. Every field, whether it maps to a database column, a computed value, or an API call, is backed by a resolver. In Hot Chocolate v16, the source generator is the primary way to define resolvers. You write plain C# methods and the generator handles schema wiring at build time.

# Properties as Resolvers

Any public property with a `get` accessor on a type is automatically treated as a resolver that returns its value.

```csharp
// Models/User.cs
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}
```

When `User` is exposed in the schema, the `id`, `name`, and `email` fields each resolve by returning the corresponding property value. No additional configuration is needed.

# Methods as Resolvers

Public methods on your types become resolvers. The source generator strips the `Get` prefix and `Async` suffix when generating the GraphQL field name.

<ExampleTabs>
<Implementation>

```csharp
// Types/BookQueries.cs
[QueryType]
public static partial class BookQueries
{
    public static Book GetBook()
        => new Book { Title = "C# in depth", Author = "Jon Skeet" };
}
```

This produces a `book` field on the Query type.

</Implementation>
<Code>

```csharp
// Types/BookQueries.cs
public class BookQueries
{
    public Book GetBook()
        => new Book { Title = "C# in depth", Author = "Jon Skeet" };
}

// Types/BookQueriesType.cs
public class BookQueriesType : ObjectType<BookQueries>
{
    protected override void Configure(IObjectTypeDescriptor<BookQueries> descriptor)
    {
        descriptor
            .Field(f => f.GetBook())
            .Type<BookType>();
    }
}
```

```csharp
// Program.cs
builder
    .AddGraphQL()
    .AddQueryType<BookQueriesType>();
```

You can also provide a resolver delegate using the `Resolve` method:

```csharp
descriptor
    .Field("book")
    .Resolve(context =>
    {
        return new Book { Title = "C# in depth", Author = "Jon Skeet" };
    });
```

</Code>
</ExampleTabs>

# Async Resolvers

Most data fetching operations are asynchronous. Mark your resolver methods as `async` or return `Task<T>` and Hot Chocolate handles the rest.

Add a `CancellationToken` parameter to your resolver and Hot Chocolate automatically cancels it if the request is aborted. This prevents wasted work when clients disconnect.

<ExampleTabs>
<Implementation>

```csharp
// Types/BookQueries.cs
[QueryType]
public static partial class BookQueries
{
    public static async Task<Book?> GetBookByIdAsync(
        int id,
        CatalogContext db,
        CancellationToken ct)
        => await db.Books.FindAsync([id], ct);
}
```

</Implementation>
<Code>

```csharp
// Types/BookQueries.cs
public class BookQueries
{
    public async Task<Book?> GetBookByIdAsync(
        int id,
        CatalogContext db,
        CancellationToken ct)
        => await db.Books.FindAsync([id], ct);
}
```

When using a delegate resolver, the `CancellationToken` is passed as the second argument:

```csharp
descriptor
    .Field("book")
    .Resolve(async (context, ct) =>
    {
        var db = context.Service<CatalogContext>();
        var id = context.ArgumentValue<int>("id");
        return await db.Books.FindAsync([id], ct);
    });
```

</Code>
</ExampleTabs>

# Accessing Parent Values

When you define a resolver on a type, you often need the value that was resolved for the parent field. For example, a `friends` resolver on a `User` type needs the user's ID to look up their friends.

<ExampleTabs>
<Implementation>

In the implementation-first approach, you can access parent properties through the `this` keyword when the resolver is defined on the type itself:

```csharp
// Models/User.cs
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }

    public async Task<List<User>> GetFriendsAsync(
        FriendService friendService,
        CancellationToken ct)
    {
        return await friendService.GetFriendsForUserAsync(this.Id, ct);
    }
}
```

When the resolver is defined elsewhere (such as in a type extension), use the `[Parent]` attribute to inject the parent value:

```csharp
// Types/UserExtensions.cs
[ExtendObjectType<User>]
public static partial class UserExtensions
{
    public static async Task<List<User>> GetFriendsAsync(
        [Parent] User user,
        FriendService friendService,
        CancellationToken ct)
        => await friendService.GetFriendsForUserAsync(user.Id, ct);
}
```

</Implementation>
<Code>

Use the `[Parent]` attribute on a parameter, or access the parent through `IResolverContext`:

```csharp
// Types/UserType.cs
public class UserType : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor
            .Field("friends")
            .Resolve(async context =>
            {
                var user = context.Parent<User>();
                var friendService = context.Service<FriendService>();
                return await friendService.GetFriendsForUserAsync(user.Id);
            });
    }
}
```

</Code>
</ExampleTabs>

# Dependency Injection

In Hot Chocolate v16, registered services are automatically recognized as service parameters without needing the `[Service]` attribute. If a parameter type is registered in the DI container, Hot Chocolate injects it.

```csharp
// Types/BookQueries.cs
[QueryType]
public static partial class BookQueries
{
    public static async Task<List<Book>> GetBooksAsync(
        CatalogService catalogService,
        CancellationToken ct)
        => await catalogService.GetAllBooksAsync(ct);
}
```

```csharp
// Program.cs
builder.Services.AddScoped<CatalogService>();

builder
    .AddGraphQL()
    .AddQueryType<BookQueries>();
```

Hot Chocolate resolves `CatalogService` from the DI container at execution time. This works for scoped, transient, and singleton services.

[Learn more about dependency injection](/docs/hotchocolate/v16/resolvers-and-data/dependency-injection)

## Accessing the HttpContext

Use `IHttpContextAccessor` when you need access to HTTP-specific details like headers or cookies:

```csharp
// Program.cs
builder.Services.AddHttpContextAccessor();
```

```csharp
// Types/BookQueries.cs
[QueryType]
public static partial class BookQueries
{
    public static string GetCorrelationId(IHttpContextAccessor httpContextAccessor)
    {
        return httpContextAccessor.HttpContext?.Request.Headers["X-Correlation-ID"]
            .FirstOrDefault() ?? "unknown";
    }
}
```

# Batch Resolvers

Batch resolvers allow you to resolve a field for multiple parent objects in a single call. This is useful when you need to load related data efficiently without a separate DataLoader class.

## The [BatchResolver] Attribute

Mark a static method with `[BatchResolver]` to define an inline batch resolver. The method receives all parent objects at once and returns results keyed by parent.

```csharp
// Types/UserExtensions.cs
[ExtendObjectType<User>]
public static partial class UserExtensions
{
    [BatchResolver]
    public static async Task<Dictionary<int, Address>> GetAddressAsync(
        IReadOnlyList<User> users,
        AddressService addressService,
        CancellationToken ct)
    {
        var userIds = users.Select(u => u.Id).ToList();
        var addresses = await addressService.GetByUserIdsAsync(userIds, ct);
        return addresses.ToDictionary(a => a.UserId);
    }
}
```

## ResolveBatch and ResolverResult

For more control, use `ResolveBatch()` in code-first to define a batch resolver inline. Use `ResolverResult<T>` when your batch resolver needs to return partial results or errors:

```csharp
// Types/UserType.cs
public class UserType : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor
            .Field("address")
            .ResolveBatch(async (users, ct) =>
            {
                var service = users.First().GetService<AddressService>();
                var ids = users.Select(u => u.Parent<User>().Id).ToList();
                var addresses = await service.GetByUserIdsAsync(ids, ct);
                return addresses.ToDictionary(a => a.UserId);
            });
    }
}
```

# Conditional Data Fetching with [IsSelected]

The `[IsSelected]` attribute lets you check whether a particular field was requested in the query. Use this to skip expensive data loading when the client does not need certain fields.

```csharp
// Types/UserQueries.cs
[QueryType]
public static partial class UserQueries
{
    public static async Task<User> GetUserByIdAsync(
        int id,
        [IsSelected("address")] bool includeAddress,
        UserService userService,
        CancellationToken ct)
    {
        if (includeAddress)
        {
            return await userService.GetUserWithAddressAsync(id, ct);
        }

        return await userService.GetUserAsync(id, ct);
    }
}
```

When the client query includes the `address` field, `includeAddress` is `true` and the resolver loads the address eagerly. Otherwise, it skips the additional work.

# Next Steps

- **Need to batch data access?** See [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader).
- **Need to page through results?** See [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination).
- **Need to filter or sort?** See [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering) and [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting).
- **Need to understand type extensions?** See [Extending Types](/docs/hotchocolate/v16/building-a-schema/extending-types).
