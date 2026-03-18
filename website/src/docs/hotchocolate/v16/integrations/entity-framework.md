---
title: Entity Framework Core
description: Learn how to integrate Entity Framework Core with Hot Chocolate v16, including DbContext injection and factory patterns.
---

[Entity Framework Core](https://docs.microsoft.com/ef/core/) is a powerful object-relational mapping framework that has become a staple when working with SQL-based databases in .NET applications.

# Resolver Injection of a DbContext

When using the [default scope](/docs/hotchocolate/v16/resolvers-and-data/dependency-injection#default-scope) for queries, each resolver that accepts a scoped `DbContext` receives a **separate** instance. This avoids [threading issues](https://learn.microsoft.com/en-gb/ef/core/dbcontext-configuration/#avoiding-dbcontext-threading-issues).

```csharp
public static async Task<Book?> GetBookByIdAsync(
    ApplicationDbContext dbContext) => // ...
```

When using the [default scope](/docs/hotchocolate/v16/resolvers-and-data/dependency-injection#default-scope) for mutations, each mutation resolver that accepts a scoped `DbContext` receives the **same** request-scoped instance, as mutations execute sequentially.

```csharp
public static async Task<Book> AddBookAsync(
    AddBookInput input,
    AppDbContext dbContext) => // ...
```

See the [Dependency Injection](/docs/hotchocolate/v16/resolvers-and-data/dependency-injection) documentation for more details.

> Warning: Changing the default scope for queries will likely result in the error "A second operation started on this context before a previous operation completed", because Entity Framework Core does not support multiple parallel operations on the same `DbContext` instance.

# Using a DbContext Factory

To use a `DbContext` factory, register your `DbContext` with Hot Chocolate. Install the additional package:

<PackageInstallation packageName="HotChocolate.Data.EntityFramework" />

Call the `RegisterDbContextFactory<T>` method on the `IRequestExecutorBuilder`. The Hot Chocolate resolver compiler then takes care of injecting your `DbContext` instance into resolvers.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddDbContextFactory<ApplicationDbContext>(
        options => options.UseSqlServer("YOUR_CONNECTION_STRING"));

// ... or AddPooledDbContextFactory.

builder.Services
    .AddGraphQLServer()
    .RegisterDbContextFactory<ApplicationDbContext>()
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
        ApplicationDbContext dbContext)
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
                .CreateDbContext()
                .Books
                .FindAsync(ctx.ArgumentValue<Guid>("id")));
    }
}
```

</Code>
<Schema>

Take a look at the annotation-based or code-first example.

</Schema>
</ExampleTabs>

> Warning: You still need to add your `DbContextFactory` to the dependency injection container by calling `AddDbContextFactory<T>` or `AddPooledDbContextFactory<T>`. `RegisterDbContextFactory<T>` on its own is not enough.

# Working with a DbContext Factory

When you use a `DbContext` factory, you need to access the `DbContext` differently outside of direct resolver injection.

## DataLoaders

When creating DataLoaders that need access to your `DbContext`, inject the `IDbContextFactory<T>` through the constructor. Create and dispose the `DbContext` within the `LoadBatchAsync` method.

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

> Warning: Dispose the `DbContext` after use. The example above uses the `using` statement for this purpose.

## Services

Services that need a `DbContext` should inject `IDbContextFactory<T>` instead of the `DbContext` directly.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContextFactory<ApplicationDbContext>(
    options => options.UseSqlServer("YOUR_CONNECTION_STRING"));

builder.Services.AddScoped<BookService>();

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

Take a look at the annotation-based or code-first example.

</Schema>
</ExampleTabs>

> Warning: Dispose the `DbContext` when the service is disposed. The example above implements `IAsyncDisposable` and disposes the `DbContext` in `DisposeAsync`.

# Troubleshooting

**"A second operation started on this context before a previous operation completed"**
This error means multiple resolvers share the same `DbContext` instance in parallel. Use the default resolver scope (which creates a separate `DbContext` per resolver for queries) or switch to a `DbContext` factory.

**DbContext is not injected into the resolver**
Verify that you registered the factory with `RegisterDbContextFactory<T>()` on the `IRequestExecutorBuilder` and that the `DbContextFactory` is also registered in the DI container.

**DbContext is disposed before use in DataLoaders**
Create the `DbContext` inside the `LoadBatchAsync` method and dispose it there. Do not create it in the constructor.

# Next Steps

- [Dependency Injection](/docs/hotchocolate/v16/resolvers-and-data/dependency-injection) for DI scope configuration
- [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader) for batching patterns
- [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering) for applying filters to EF Core queries
