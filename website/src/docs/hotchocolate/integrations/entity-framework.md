---
title: Entity Framework Core
---

import { ExampleTabs } from "../../../components/mdx/example-tabs"

Entity Framework Core is a powerful object-relational mapping framework that has become a staple when working with SQL-based Databases in ASP.NET Core applications.

Besides its many benefits there is one shortcoming that makes Entity Framework hard to use with a standard Hot Chocolate GraphQL server:

[**Entity Framework Core doesn't support multiple parallel operations being run on the same context instance.**](https://docs.microsoft.com/ef/core/miscellaneous/async)

When using `services.AddDbContext<T>` to register a `DbContext` as a scoped service, one instance of this `DbContext` is created and used for the entirety of a GraphQL request. This is an issue, since Hot Chocolate executes resolvers in parallel for performance reasons. If two resolvers are executed in parallel and both try to perform an operation using the `DbContext`, we might see one of the following exceptions being thrown:

- `A second operation started on this context before a previous operation completed.`
- `Cannot access a disposed object.`

Fortunately there are a couple of solutions that can be used to avoid the described issue. We will take a closer look at them in the below sections.

# DbContextFactory

The recommended approach to solving the `DbContext` concurreny issues is creating a `DbContext` instance on a per-operation basis using an `IDbContextFactory`.

We can register a [pooled](https://docs.microsoft.com/en-us/ef/core/performance/advanced-performance-topics?tabs=with-constant#dbcontext-pooling) `IDbContextFactory` like the following.

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddPooledDbContextFactory<SomeDbContext>(b =>
            b.UseInMemoryDatabase("Test"));

        // Omitted code for brevity
    }
}

```

> ⚠️ Note: All of the configuration done in the `OnConfiguring` method of the `DbContext` needs to be moved to the configuration action on the `AddPooledDbContextFactory` call.

Using the `IDbContextFactory` changes how we access an instance of our `DbContext`. Previously we would directly inject the scoped `DbContext` instance into our constructors or methods. Now we need to inject the `IDbContextFactory` instead and create an instance of the `DbContext` ourselves.

```csharp
public ExampleConstructor(IDbContextFactory<SomeDbContext> dbContextFactory)
{
    SomeDbContext dbContext = dbContextFactory.CreateDbContext();
}
```

In the following we will look at some usage examples of the `IDbContextFactory` as well as how we eased the integration with resolvers.

## Resolvers

In order to integrate with our Data middleware, such as `UsePagination` or `UseSorting`, we have added a `UseDbContext` middleware. This middleware takes care of retrieving a `DbContext` instance from the pool, as well as disposing said instance, once the resolver and subsequent middleware has finished executing.

The middleware is part of the `HotChocolate.Data.EntityFramework` package.

```bash
dotnet add package HotChocolate.Data.EntityFramework
```

Once installed we can start applying the `UseDbContext` middleware to our resolvers.

<ExampleTabs>
<ExampleTabs.Annotation>

In the Annotation-based approach, we can annotate our resolver using the `UseDbContextAttribute`, specifying the type of our `DbContext` as an argument.

```csharp
public class Query
{
    [UseDbContext(typeof(SomeDbContext))]
    public IQueryable<User> GetUsers([ScopedService] SomeDbContext dbContext)
        => dbContext.Users;
}
```

Please note that the `ScopedServiceAttribute` has nothing to do with service lifetime. It is used to inject the `DbContext` instance the `UseDbContext` middleware retrieved from the pool.

We can make this even simpler, by creating an attribute inheriting from the `UseDbContextAttribute`:

```csharp
public class UseSomeDbContext : UseDbContextAttribute
{
    public UseSomeDbContext([CallerLineNumber] int order = 0)
        : base(typeof(SomeDbContext))
    {
        Order = order;
    }
}

public class Query
{
    [UseSomeDbContext]
    public IQueryable<User> GetUsers([ScopedService] SomeDbContext dbContext)
        => dbContext.Users;
}
```

> Note: Since the [order of attributes is not guaranteed by .NET](https://docs.microsoft.com/dotnet/csharp/language-reference/language-specification/attributes#attribute-specification), Hot Chocolate uses the line number to determine the correct order of the Data middleware. If you are using a custom attribute, you have to forward the line number using the `CallerLineNumberAttribute`.

</ExampleTabs.Annotation>
<ExampleTabs.Code>

In the Code-first approach we can add the `UseDbContext` middlware through the `IObjectFieldDescriptor`, specifying the type of our `DbContext` as the generic type parameter.

```csharp
public class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("users")
            .UseDbContext<SomeDbContext>()
            .Resolver((ctx) =>
            {
                return ctx.DbContext<SomeDbContext>().Users;
            });
    }
}
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

⚠️ Schema-first does currently not support DbContext integration!

</ExampleTabs.Schema>
</ExampleTabs>

> ⚠️ Note: When using multiple middleware, keep in mind that the order of middleware matters. The correct order is:
>
> UseDbContext > UsePaging > UseProjections > UseFiltering > UseSorting

## Services

When creating services they can no longer be of a scoped lifetime, i.e. injecting a `DbContext` instance using the constructor. Instead we can create transient services and inject the `IDbContextFactory` using the constructor.

```csharp
public class UserRepository : IAsyncDisposable
{
    private readonly SomeDbContext _dbContext;

    public UserRepository(IDbContextFactory<SomeDbContext> dbContextFactory)
    {
        _dbContext = dbContextFactory.CreateDbContext();
    }

    public async Task<User> GetUserAsync(string id)
    {
        var user = await _dbContext.Users.FirstOrDefault(u => u.Id == id);

        return user;
    }

    public ValueTask DisposeAsync()
    {
        return _dbContext.DisposeAsync();
    }
}

public class Query
{
    public async Task<User> GetUserAsync(string id,
        [Service] UserRepository repository)
    {
        return await repository.GetUserAsync(id);
    }
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<UserRepository>();

        // Omitted code for brevity
    }
}
```

> ⚠️ Note: It is important to dispose the `DbContext` we created. Notice how our service implements `IAsyncDisposable` and the `DbContext` we created is disposed using the `DisposeAsync` method.

## DataLoaders

With DataLoaders we inject the `IDbContextFactory` using the constructor and create **and dispose** our `DbContext` within the `Load...` method.

```csharp
public class UserByIdDataLoader : BatchDataLoader<string, User>
{
    private readonly IDbContextFactory<SomeDbContext> _dbContextFactory;

    public UserByIdDataLoader(
        IDbContextFactory<SomeDbContext> dbContextFactory,
        IBatchScheduler batchScheduler, DataLoaderOptions options)
        : base(batchScheduler, options)
    {
        _dbContextFactory = dbContextFactory;
    }

    protected override async Task<IReadOnlyDictionary<string, User>>
        LoadBatchAsync(IReadOnlyList<string> keys, CancellationToken ct)
    {
        await using SomeDbContext dbContext =
            _dbContextFactory.CreateDbContext();

        return await dbContext.Users
            .Where(s => keys.Contains(s.Id))
            .ToDictionaryAsync(t => t.Id, ct);
    }
}
```

> ⚠️ Note: It is important to dispose the `DbContext` we created. Notice how we used the `IAsyncDisposable` functionality using `await using` in the above example.

# Serial execution

If we depend on using a scoped `DbContext` or if we are using a Unit of Work style pattern, using the [DbContextFactory](#dbcontextfactory) might not be an option for us.

Hot Chocolate gives us the option to execute certain resolvers serially, meaning that no other non-pure resolvers are run in parallel to this resolver. If we execute all resolvers using our `DbContext` serially, we avoid the possibility of two resolvers trying to run an operation on our scoped `DbContext` in parallel.

> ⚠️ Note: Executing a resolver serially incurs a performance penalty. While a resolver is running serially, no other non-pure resolver can run in parallel. This also includes non-pure resolvers that do not use the `DbContext`, such as a resolver executing a REST call.
>
> If we want to get the most performance out of our GraphQL server, we definitely need to use the [DbContextFactory](#dbcontextfactory) described above.

We can execute a single resolver serially, by marking it as `Serial`.

<ExampleTabs>
<ExampleTabs.Annotation>

```csharp
public class Query
{
    [Serial]
    public async Task<List<User>> GetUsers([Service] SomeDbContext dbContext)
    {
        return await dbContext.Users.ToListAsync();
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
            .Serial()
            .Resolver((ctx) =>
            {
                var dbContext = ctx.Service<SomeDbContext>();

                return dbContext.Users.ToListAsync();
            });
    }
}
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

Take a look at the Annotation-based or Code-first example.

</ExampleTabs.Schema>
</ExampleTabs>

We can also make `Serial` the default execution strategy.

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .ModifyOptions(options =>
            {
                options.DefaultResolverStrategy = ExecutionStrategy.Serial;
            });
    }
}
```

> ⚠️ Note: This incurs the biggest performance hit as it causes all non-pure resolvers to be executed serially.
