---
title: "Batch DataLoader"
---

A batch DataLoader gathers multiple resolver requests for related data and retrieves them in a single set-based operation. Use `BatchDataLoader<TKey, TValue>` when each key maps to zero or one value, such as `BrandId -> Brand`, `CustomerId -> Customer`, or `Sku -> Product`.

This page covers manual batch DataLoader classes. For most new code, the source-generated `[DataLoader]` model is recommended. Manual classes are helpful when you need custom base-class logic, specialized options, migration compatibility, or explicit constructor control.

## Why batch related-object lookups?

A typical GraphQL query requests a list and then related data for each item:

```graphql
query GetProducts {
  products(first: 5) {
    nodes {
      id
      name
      brand {
        id
        name
      }
    }
  }
}
```

The schema might define the relationship as follows:

```graphql
type Product {
  id: ID!
  name: String!
  brand: Brand
}

type Brand {
  id: ID!
  name: String!
}
```

Without a DataLoader, the `Product.brand` resolver may call the database once for every product. For five products, this results in one products query and five brand queries. If several products share the same brand, the resolver can repeat identical work.

With a batch DataLoader, each resolver schedules a brand ID. Hot Chocolate continues executing the current resolver wave, then the DataLoader dispatches a single batch fetch for the unique keys.

```text
Product.brand resolvers
  Product 1 -> LoadAsync(10)
  Product 2 -> LoadAsync(20)
  Product 3 -> LoadAsync(10)
        |
        v
BatchDataLoader collects unique keys: 10, 20
        |
        v
LoadBatchAsync([10, 20]) -> { 10: Brand A, 20: Brand B }
        |
        v
Resolvers receive Brand A, Brand B, Brand A
```

This approach is ideal for one-to-one and many-to-one lookups where each key maps to at most one result. Do not use `BatchDataLoader<TKey, TValue>` for `BrandId -> list of Products`; that scenario requires a grouped or collection DataLoader.

## Creating a manual batch loader

A manual loader inherits from `BatchDataLoader<TKey, TValue>`. The constructor receives an `IBatchScheduler` and a non-null `DataLoaderOptions` from dependency injection, then passes both to the base class.

The batch method receives all keys scheduled for the current batch. It should perform a single set-based backend call and return a dictionary keyed by the same values passed to `LoadAsync`.

```csharp
using GreenDonut;

public sealed record Brand(int Id, string Name);

public interface IBrandRepository
{
    Task<IReadOnlyDictionary<int, Brand>> GetBrandsByIdAsync(
        IReadOnlyList<int> ids,
        CancellationToken cancellationToken);
}

public sealed class BrandByIdDataLoader(
    IBrandRepository repository,
    IBatchScheduler batchScheduler,
    DataLoaderOptions options)
    : BatchDataLoader<int, Brand>(batchScheduler, options)
{
    protected override async Task<IReadOnlyDictionary<int, Brand>> LoadBatchAsync(
        IReadOnlyList<int> keys,
        CancellationToken cancellationToken)
    {
        return await repository.GetBrandsByIdAsync(keys, cancellationToken);
    }
}
```

If your loader interacts directly with Entity Framework Core, use `IDbContextFactory<TContext>` or a dedicated service scope. Create and dispose the context inside `LoadBatchAsync`. Avoid sharing a non-thread-safe scoped `DbContext` across concurrently executing resolver or DataLoader work.

> The key list passed to `LoadBatchAsync` is rented. Do not store `keys` or use it after the method returns.

## Registering the loader

Register manual loaders on `IServiceCollection` with `AddDataLoader<T>()`. Keep this registration separate from GraphQL server configuration.

```csharp
builder.Services.AddScoped<IBrandRepository, BrandRepository>();
builder.Services.AddDataLoader<BrandByIdDataLoader>();

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>();
```

Hot Chocolate resolves the registered loader through the DataLoader scope for the request. That scope provides the scheduler, request cache, diagnostics, and default options. Do not construct loaders manually inside resolvers, as this bypasses batching, cache sharing, diagnostics, and DI-provided options.

## Using the loader in a resolver

Inject the loader as a resolver parameter, accept a `CancellationToken`, and pass that token to `LoadAsync`.

```csharp
using HotChocolate;
using HotChocolate.Types;

public sealed record Product(int Id, string Name, int BrandId);

[ObjectType<Product>]
public static partial class ProductNode
{
    public static async Task<Brand?> GetBrandAsync(
        [Parent] Product product,
        BrandByIdDataLoader brandById,
        CancellationToken cancellationToken)
    {
        return await brandById.LoadAsync(product.BrandId, cancellationToken);
    }
}
```

`LoadAsync` returns `Task<Brand?>` because a requested key may be missing from the batch result. Keep the GraphQL field nullable unless your application guarantees the related row exists and is visible to the current user.

## Mapping keys to results correctly

The dictionary returned from `LoadBatchAsync` must use the requested keys. If the resolver calls `LoadAsync(product.BrandId)`, key the dictionary by brand ID, not by product ID.

For requested keys `[1, 2, 3]`, the backend might return brands `1` and `3`:

```csharp
return new Dictionary<int, Brand>
{
    [1] = new Brand(1, "ChilliCream"),
    [3] = new Brand(3, "Contoso")
};
```

| Batch result                                | Resolver receives                           |
| ------------------------------------------- | ------------------------------------------- |
| Requested key is present in the dictionary  | The matching value                          |
| Requested key is absent from the dictionary | `null`                                      |
| `LoadBatchAsync` throws                     | Loads in that batch fail with the exception |

A missing key can indicate the record does not exist, the current user is not authorized to see it, or the backend filtered it out. Model this outcome deliberately with nullable fields, domain-specific results, or field errors according to your schema design.

## Batching many-to-one relationships

Many parent objects can reference the same related object. Products often share a brand, orders often share a customer, and line items often share a product.

`BatchDataLoader<int, Brand>` remains the correct shape for `Product -> Brand` because each `BrandId` maps to one `Brand`. When several products request `BrandId = 10`, the DataLoader deduplicates that key within the request and returns the same cached result to each waiting resolver.

```text
Products:          P1(BrandId 10), P2(BrandId 20), P3(BrandId 10)
Scheduled keys:    10, 20, 10
Fetched keys:      10, 20
Resolver results:  Brand 10, Brand 20, Brand 10
```

## Loading multiple keys when you already have them

If a resolver already has a set of keys, use the multi-key overload instead of awaiting each key individually.

```csharp
public static async Task<IReadOnlyList<Brand?>> GetBrandsAsync(
    IReadOnlyList<int> brandIds,
    BrandByIdDataLoader brandById,
    CancellationToken cancellationToken)
{
    return await brandById.LoadAsync(brandIds, cancellationToken);
}
```

The returned list follows the input key order. If `brandIds` is `[20, 10, 20]`, the result positions correspond to brand `20`, brand `10`, and brand `20`.

## Understanding caching and request lifetime

With the default DI registration, a DataLoader cache is scoped to the GraphQL request and DataLoader scope.

- The same key requested while a batch is pending shares the same pending work.
- The same key requested after the batch resolves reuses the cached result within that request.
- A new GraphQL request starts with a new cache.
- The request cache is not a distributed cache or application-wide cache.

`DataLoaderOptions.Cache` is available for custom or shared request-cache scenarios, but keep cross-request caching in your application data layer.

## Configuring batch size and options

`DataLoaderOptions` controls loader behavior. The options object injected by DI includes the request cache, diagnostic events, and a default maximum batch size.

| Option             | Default                        | Use when                                                                                                                    |
| ------------------ | ------------------------------ | --------------------------------------------------------------------------------------------------------------------------- |
| `MaxBatchSize`     | `1024`                         | You need to respect SQL parameter limits, API payload limits, URL length, or memory pressure. `0` disables batch splitting. |
| `Cache`            | Request-scoped cache from DI   | You need a custom request cache or cache sharing between loaders.                                                           |
| `DiagnosticEvents` | Registered diagnostics service | You need instrumentation for DataLoader events.                                                                             |

To configure one loader, use the factory overload and copy the DI-provided options before changing them:

```csharp
builder.Services.AddDataLoader<BrandByIdDataLoader>(sp =>
{
    var options = sp.GetRequiredService<DataLoaderOptions>().Copy();
    options.MaxBatchSize = 500;

    return new BrandByIdDataLoader(
        sp.GetRequiredService<IBrandRepository>(),
        sp.GetRequiredService<IBatchScheduler>(),
        options);
});
```

Adjust `MaxBatchSize` according to the backend limit you need to enforce. Setting it to `0` removes splitting, so it is not a safe cap.

## Prefer source-generated loaders when possible

Source-generated loaders are the default for convenience. You write a static method with `[DataLoader]`, and the generator creates the DataLoader class and interface.

```csharp
internal static class BrandDataLoaders
{
    [DataLoader]
    public static async Task<Dictionary<int, Brand>> GetBrandByIdAsync(
        IReadOnlyList<int> ids,
        CatalogContext db,
        CancellationToken cancellationToken)
    {
        return await db.Brands
            .Where(brand => ids.Contains(brand.Id))
            .ToDictionaryAsync(brand => brand.Id, cancellationToken);
    }
}
```

Use the generated approach when your loader is a batch method without custom logic. Use a manual `BatchDataLoader<TKey, TValue>` when you need explicit class-based control.

## Troubleshooting

### The loader still makes N database calls

`LoadBatchAsync` might be looping over keys and calling the database or API once per key. Replace that loop with a single set-based query, stored procedure, repository method, or API request that accepts all keys for the batch.

### Some related objects are always null

The returned dictionary is often keyed by the wrong value, such as parent ID instead of related entity ID. Key the dictionary by the exact key passed to `LoadAsync`.

### Cancellation is ignored

Accept `CancellationToken` in the resolver and pass it to `LoadAsync`. In `LoadBatchAsync`, pass the batch token to EF Core, Dapper, HTTP, or repository calls. Do not swallow `OperationCanceledException`.

### Batches are too large for the backend

Configure `DataLoaderOptions.MaxBatchSize` for the loader. Remember that `0` disables splitting and can make batches larger, not smaller.

### A one-to-many relationship returns only one item

`BatchDataLoader<TKey, TValue>` returns one value per key. Use a grouped or collection DataLoader when each key must return multiple values.

### A scoped dependency behaves inconsistently under load

A non-thread-safe scoped dependency might be shared across parallel resolver or DataLoader work. For EF Core, inject `IDbContextFactory<TContext>` into the loader and create a context inside `LoadBatchAsync`, or use the documented service-scope options with generated loaders.

## Next steps

- Review the [DataLoader overview](./) for batching concepts and implementation choices.
- Review [resolver signatures](../resolvers/resolver-signature) for service parameters, `[Parent]`, and cancellation.
- Review [service injection](../resolvers/service-injection) before changing lifetimes or scopes.
- Use the current [DataLoader guide](/docs/hotchocolate/v16/build/dataloader) for source-generated loaders, grouped loaders, and batch resolvers while the build child pages are being completed.
