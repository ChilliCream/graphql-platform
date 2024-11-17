---
title: Entity Framework Core
---

[Entity Framework Core](https://docs.microsoft.com/ef/core/) is a powerful object-relational mapping framework that has become a staple when working with SQL-based databases in .NET Core applications.

# Resolver injection of a DbContext

When using the [default scope](/docs/hotchocolate/v15/server/dependency-injection#default-scope) for queries, each execution of a query that accepts a scoped DbContext will receive a **separate** instance, avoiding [threading issues](https://learn.microsoft.com/en-gb/ef/core/dbcontext-configuration/#avoiding-dbcontext-threading-issues).

```csharp
public static async Task<Book?> GetBookByIdAsync(
    ApplicationDbContext dbContext) => // ...
```

When using the [default scope](/docs/hotchocolate/v15/server/dependency-injection#default-scope) for mutations, each execution of a mutation that accepts a scoped DbContext will receive the **same** request-scoped instance, as mutations are executed sequentially.

```csharp
public static async Task<Book> AddBookAsync(
    AddBookInput input,
    AppDbContext dbContext) => // ...
```

See the [Dependency Injection](/docs/hotchocolate/v15/server/dependency-injection) documentation for more details.

> Warning: Changing the default scope for queries will likely result in the error "A second operation started on this context before a previous operation completed", since Entity Framework Core does not support multiple parallel operations being run on the same DbContext instance.

# Using a DbContext factory

In order to use a DbContext factory, you need to register your DbContext with Hot Chocolate. To do so, an additional package needs to be installed:

<PackageInstallation packageName="HotChocolate.Data.EntityFramework" />

Once installed, you can simply call the `RegisterDbContextFactory<T>` method on the `IRequestExecutorBuilder`. The Hot Chocolate Resolver Compiler will then take care of correctly injecting your DbContext instance into your resolvers.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddDbContextFactory<ApplicationDbContext>(
        options => options.UseSqlServer("YOUR_CONNECTION_STRING"));

// ... or AddPooledDbContextFactory.

builder.Services
    .AddGraphQLServer()
    .RegisterDbContextFactory<ApplicationDbContext>() // ⬅️
    .AddTypes();
```

<ExampleTabs>
<Implementation>

```csharp
[QueryType]
public static class Query
{
    public static async Task<Book?> GetBookByIdAsync(
        Guid id,
        ApplicationDbContext dbContext) // ⬅️
    {
        return await dbContext.Books.FindAsync(id);
    }
}
```

</Implementation>
<Code>

```csharp
public sealed class QueryType : ObjectType
{
    protected override void Configure(
        IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("bookById")
            .Argument("id", a => a.Type<NonNullType<UuidType>>())
            .Resolve(async ctx => await ctx
                .Service<IDbContextFactory<ApplicationDbContext>>()
                // ⬆️
                .CreateDbContext()
                .Books
                .FindAsync(ctx.ArgumentValue<Guid>("id")));
    }
}
```

</Code>
<Schema>

Take a look at the implementation-first or code-first example.

</Schema>
</ExampleTabs>

> Warning: As shown above, you still need to add your `DbContextFactory` to the dependency injection container, by calling `AddDbContextFactory<T>` or `AddPooledDbContextFactory<T>`. `RegisterDbContextFactory<T>` on its own is not enough.

# Working with a DbContext factory

When you use a DbContext factory, you need to access your DbContext differently if it is not being directly injected into a resolver. In the following sections we will take a look at some of the changes that you need to make.

## DataLoaders

When creating DataLoaders that need access to your DbContext, you need to inject the `IDbContextFactory<T>` using the constructor.

The DbContext should only be created **and disposed** in the `LoadBatchAsync` method.

```csharp
public sealed class BookByIdDataLoader : BatchDataLoader<Guid, Book>
{
    private readonly IDbContextFactory<AppDbContext>
        _dbContextFactory;

    public BookByIdDataLoader(
        IDbContextFactory<AppDbContext> dbContextFactory,
        IBatchScheduler batchScheduler,
        DataLoaderOptions options)
        : base(batchScheduler, options)
    {
        _dbContextFactory = dbContextFactory;
    }

    protected override async Task<IReadOnlyDictionary<Guid, Book>>
        LoadBatchAsync(
            IReadOnlyList<Guid> keys,
            CancellationToken cancellationToken)
    {
        using AppDbContext dbContext =
            _dbContextFactory.CreateDbContext();

        return await dbContext.Books
            .Where(b => keys.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, cancellationToken);
    }
}
```

> Warning: It is important that you dispose the DbContext. In the example above we use the `using` statement to dispose the DbContext after it is no longer required.

## Services

When creating services, they now need to inject the `IDbContextFactory<T>` instead of the DbContext directly.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContextFactory<ApplicationDbContext>(
    options => options.UseSqlServer("YOUR_CONNECTION_STRING"));

builder.Services.AddScoped<BookService>()

builder.Services
    .AddGraphQLServer()
    .AddTypes();
```

```csharp
public sealed class BookService : IAsyncDisposable
{
    private readonly ApplicationDbContext _dbContext;

    public BookService(
        IDbContextFactory<ApplicationDbContext> dbContextFactory)
    {
        _dbContext = dbContextFactory.CreateDbContext();
    }

    public async Task<Book?> GetBookAsync(Guid id)
    {
        return await _dbContext.Books.FindAsync(id);
    }

    public ValueTask DisposeAsync()
    {
        return _dbContext.DisposeAsync();
    }
}
```

<ExampleTabs>
<Implementation>

```csharp
[QueryType]
public static class Query
{
    public static async Task<Book?> GetBookByIdAsync(
        Guid id,
        BookService bookService)
    {
        return await bookService.GetBookAsync(id);
    }
}
```

</Implementation>
<Code>

```csharp
public sealed class QueryType : ObjectType
{
    protected override void Configure(
        IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("bookById")
            .Argument("id", a => a.Type<NonNullType<UuidType>>())
            .Resolve<Book?>(async ctx => await ctx
                .Service<BookService>()
                .GetBookAsync(ctx.ArgumentValue<Guid>("id")));
    }
}
```

</Code>
<Schema>

Take a look at the implementation-first or code-first example.

</Schema>
</ExampleTabs>

> Warning: It is important that you dispose the DbContext when your service is being disposed. In the example above we are implementing `IAsyncDisposable` and disposing the created DbContext in the `DisposeAsync` method. This method will be invoked by the dependency injection system.
