---
title: Entity Framework Core
---

import { ExampleTabs, Annotation, Code, Schema } from "../../../components/mdx/example-tabs"

[Entity Framework Core](https://docs.microsoft.com/ef/core/) is a powerful object-relational mapping framework that has become a staple when working with SQL-based Databases in .NET Core applications.

When working with Entity Framework Core's [DbContext](https://docs.microsoft.com/dotnet/api/system.data.entity.dbcontext), it is most commonly registered as a scoped service.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(
    options => options.UseSqlServer("YOUR_CONNECTION_STRING"));
```

If you have read our [guidance on dependency injection](/docs/hotchocolate/server/dependency-injection#resolver-injection) you might be inclined to simply inject your `DbContext` using the `HotChocolate.ServiceAttribute`.

```csharp
public Foo GetFoo([Service] ApplicationDbContext dbContext)
    => // Omitted code for brevity
```

While this is usually the correct way to inject services and it may appear to work initially, it has a fatal flaw: Entity Framework Core doesn't support [multiple parallel operations being run on the same context instance](https://docs.microsoft.com/ef/core/miscellaneous/async).

Lets take a look at an example to understand why this can lead to issues. Both the `foo` and `bar` field in the below query are backed by a resolver that injects a scoped `DbContext` instance and performs a database query using it.

```graphql
{
  foo
  bar
}
```

Since Hot Chocolate parallelizes the execution of query fields, and both of the resolvers will receive the same scoped `DbContext` instance, two database queries are likely to be ran through this scoped `DbContext` instance in parallel. This will then lead to one of the following exceptions being thrown:

- `A second operation started on this context before a previous operation completed.`
- `Cannot access a disposed object.`

# Resolver injection of a DbContext

In order to ensure that resolvers do not access the same scoped `DbContext` instance in parallel, you can inject it using the `ServiceKind.Synchronized`.

```csharp
public Foo GetFoo(
    [Service(ServiceKind.Synchronized)] ApplicationDbContext dbContext)
    => // Omitted code for brevity
```

[Learn more about `ServiceKind.Synchronized`](/docs/hotchocolate/server/dependency-injection#servicekindsynchronized)

Since this is a lot of code to write, just to inject a `DbContext`, you can use [`RegisterDbContext<T>`](#registerdbcontext) to simplify the injection.

# RegisterDbContext

If you want to omit the attribute, you can simply call `RegisterDbContext<T>` on the `IRequestExecutorBuilder`. The Hot Chocolate Resolver Compiler will then take care of correctly injecting your scoped `DbContext` instance and also ensuring that the resolvers using it are never run in parallel.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(
    options => options.UseSqlServer("YOUR_CONNECTION_STRING"));

builder.Services
    .AddGraphQLServer()
    .RegisterDbContext<ApplicationDbContext>()
    .AddQueryType<Query>();

public class Query
{
    public Foo GetFoo(ApplicationDbContext dbContext)
        => // Omitted code for brevity
}
```

> ⚠️ Note: You still have to register your `DbContext` in the actual dependency injection container, by calling `services.AddDbContext<T>`. `RegisterDbContext<T>` on its own is not enough.

You can also specify a [DbContextKind](#dbcontextkind) as argument to the `RegisterDbContext<T>` method, to change how the `DbContext` should be injected.

```csharp
builder.Services
    .AddGraphQLServer()
    .RegisterDbContext<ApplicationDbContext>(DbContextKind.Pooled)
```

# DbContextKind

For the most part the `DbContextKind` is really similar to the [ServiceKind](/docs/hotchocolate/server/dependency-injection#servicekind), with the exception of the [DbContextKind.Pooled](#dbcontextkindpooled).

## DbContextKind.Synchronized

This injection mechanism ensures that resolvers injecting the specified `DbContext` are never run in parallel. It is the default for the [`RegisterDbContext<T>`](#registerdbcontext) method and behaves in the same fashion as [ServiceKind.Synchronized](/docs/hotchocolate/server/dependency-injection#servicekindsynchronized) does for regular services.

## DbContextKind.Pooled

## DbContextKind.Resolver

<!--
# DbContextFactory

The recommended approach to solving the `DbContext` concurreny issues is creating a `DbContext` instance on a per-operation basis using an `IDbContextFactory`.

We can register a [pooled](https://docs.microsoft.com/ef/core/performance/advanced-performance-topics?tabs=with-constant#dbcontext-pooling) `IDbContextFactory` like the following.

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

> ⚠️ Note: All `HotChocolate.*` packages need to have the same version.

Once installed we can start applying the `UseDbContext` middleware to our resolvers.

<ExampleTabs>
<Annotation>

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

</Annotation>
<Code>

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

</Code>
<Schema>

⚠️ Schema-first does currently not support DbContext integration!

</Schema>
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
<Annotation>

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

</Annotation>
<Code>

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

</Code>
<Schema>

Take a look at the Annotation-based or Code-first example.

</Schema>
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

> ⚠️ Note: This incurs the biggest performance hit as it causes all non-pure resolvers to be executed serially. -->
