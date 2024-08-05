---
title: Entity Framework Core
---

[Entity Framework Core](https://docs.microsoft.com/ef/core/) is a powerful object-relational mapping framework that has become a staple when working with SQL-based Databases in .NET Core applications.

When working with Entity Framework Core's [DbContext](https://docs.microsoft.com/dotnet/api/system.data.entity.dbcontext), it is most commonly registered as a scoped service.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(
    options => options.UseSqlServer("YOUR_CONNECTION_STRING"));
```

If you have read our [guidance on dependency injection](/docs/hotchocolate/v13/server/dependency-injection#resolver-injection) you might be inclined to simply inject your `DbContext` using the `HotChocolate.ServiceAttribute`.

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

[Learn more about `ServiceKind.Synchronized`](/docs/hotchocolate/v13/server/dependency-injection#servicekindsynchronized)

Since this is a lot of code to write, just to inject a `DbContext`, you can use [`RegisterDbContext<T>`](#registerdbcontext) to simplify the injection.

# RegisterDbContext

In order to simplify the injection of a `DbContext` we have introduced a method called `RegisterDbContext<T>`, similar to the [`RegisterService<T>`](/docs/hotchocolate/v13/server/dependency-injection#registerservice) method for regular services. This method is part of the `HotChocolate.Data.EntityFramework` package, which you'll have to install.

<PackageInstallation packageName="HotChocolate.Data.EntityFramework" />

Once installed you can simply call the `RegisterDbContext<T>` method on the `IRequestExecutorBuilder`. The Hot Chocolate Resolver Compiler will then take care of correctly injecting your scoped `DbContext` instance into your resolvers and also ensuring that the resolvers using it are never run in parallel.

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

> Warning: You still have to register your `DbContext` in the actual dependency injection container, by calling `services.AddDbContext<T>`. `RegisterDbContext<T>` on its own is not enough.

You can also specify a [DbContextKind](#dbcontextkind) as argument to the `RegisterDbContext<T>` method, to change how the `DbContext` should be injected.

```csharp
builder.Services
    .AddGraphQLServer()
    .RegisterDbContext<ApplicationDbContext>(DbContextKind.Pooled)
```

# DbContextKind

When registering a `DbContext` you can specify a `DbContextKind` to instruct Hot Chocolate to use a certain strategy when injecting the `DbContext`. For the most part the `DbContextKind` is really similar to the [ServiceKind](/docs/hotchocolate/v13/server/dependency-injection#servicekind), with the exception of the [DbContextKind.Pooled](#dbcontextkindpooled).

## DbContextKind.Synchronized

This injection mechanism ensures that resolvers injecting the specified `DbContext` are never run in parallel. This allows you to use the same scoped `DbContext` instance throughout a request, without the risk of running into concurrency exceptions as mentioned above. It behaves in the same fashion as [ServiceKind.Synchronized](/docs/hotchocolate/v13/server/dependency-injection#servicekindsynchronized) does for regular services.

## DbContextKind.Resolver

This injection mechanism will resolve the scoped `DbContext` from a resolver-scoped [`IServiceScope`](https://docs.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.iservicescope). It behaves in the same fashion as [ServiceKind.Resolver](/docs/hotchocolate/v13/server/dependency-injection#servicekindresolver) does for regular services. Since a different `DbContext` instance is resolved for each resolver invocation, Hot Chocolate can parallelize the execution of resolvers using this `DbContext`.

## DbContextKind.Pooled

This injection mechanism will require your `DbContext` to be registered as a [pooled](https://docs.microsoft.com/ef/core/performance/advanced-performance-topics?tabs=with-constant#dbcontext-pooling) `IDbContextFactory<T>`.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPooledDbContextFactory<ApplicationDbContext>(
    options => options.UseSqlServer("YOUR_CONNECTION_STRING"));

builder.Services
    .AddGraphQLServer()
    .RegisterDbContext<ApplicationDbContext>(DbContextKind.Pooled)
    .AddQueryType<Query>();

public class Query
{
    public Foo GetFoo(ApplicationDbContext dbContext)
        => // Omitted code for brevity
}
```

When injecting a `DbContext` using the `DbContextKind.Pool`, Hot Chocolate will retrieve one `DbContext` instance from the pool for each invocation of a resolver. Once the resolver has finished executing, the instance will be returned to the pool.

Since each resolver invocation is therefore working with a "transient" `DbContext` instance, Hot Chocolate can parallelize the execution of resolvers using this `DbContext`.

# Working with a pooled DbContext

If you have registered your `DbContext` using [DbContextKind.Pooled](#dbcontextkindpooled) you are on your way to squeeze the most performance out of your GraphQL server, but unfortunately it also changes how you have to use the `DbContext`.

For example you need to move all of the configuration from the `OnConfiguring` method inside your `DbContext` into the configuration action on the `AddPooledDbContextFactory` call.

You also need to access your `DbContext` differently. In the following chapters we will take a look at some of the changes you have to make.

## DataLoaders

When creating DataLoaders that need access to your `DbContext`, you need to inject the `IDbContextFactory<T>` using the constructor.

The `DbContext` should only be created **and disposed** in the `LoadBatchAsync` method.

```csharp
public class FooByIdDataLoader : BatchDataLoader<string, Foo>
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

    public FooByIdDataLoader(
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        IBatchScheduler batchScheduler, DataLoaderOptions options)
        : base(batchScheduler, options)
    {
        _dbContextFactory = dbContextFactory;
    }

    protected override async Task<IReadOnlyDictionary<string, Foo>>
        LoadBatchAsync(IReadOnlyList<string> keys, CancellationToken ct)
    {
        await using ApplicationDbContext dbContext =
            _dbContextFactory.CreateDbContext();

        return await dbContext.Foos
            .Where(s => keys.Contains(s.Id))
            .ToDictionaryAsync(t => t.Id, ct);
    }
}
```

> Warning: It is important that you dispose the `DbContext` to return it to the pool. In the above example we are using `await using` to dispose the `DbContext` after it is no longer required.

## Services

When creating services, they now need to inject the `IDbContextFactory<T>` instead of the `DbContext` directly. Your services also need be of a transient lifetime. Otherwise you could be faced with the `DbContext` concurrency issue again, if the same `DbContext` instance is accessed by two resolvers through our service in parallel.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPooledDbContextFactory<ApplicationDbContext>(
    options => options.UseSqlServer("YOUR_CONNECTION_STRING"));

builder.Services.AddTransient<FooService>()

builder.Services
    .AddGraphQLServer()
    .RegisterService<FooService>()
    .AddQueryType<Query>();

public class Query
{
    public Foo GetFoo(FooService FooService)
        => // Omitted code for brevity
}

public class FooService : IAsyncDisposable
{
    private readonly ApplicationDbContext _dbContext;

    public FooService(IDbContextFactory<ApplicationDbContext> dbContextFactory)
    {
        _dbContext = dbContextFactory.CreateDbContext();
    }

    public Foo GetFoo()
        => _dbContext.Foos.FirstOrDefault();

    public ValueTask DisposeAsync()
    {
        return _dbContext.DisposeAsync();
    }
}
```

> Warning: It is important that you dispose the `DbContext` to return it to the pool, once your transient service is being disposed. In the above example we are implementing `IAsyncDisposable` and disposing the created `DbContext` in the `DisposeAsync` method. This method will be invoked by the dependency injection system.
