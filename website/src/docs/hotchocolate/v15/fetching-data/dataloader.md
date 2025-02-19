---
title: "DataLoader"
---

> If you want to read more about data loaders in general, you can head over to Facebook's [GitHub repository](https://github.com/facebook/dataloader).

Every data fetching technology suffers the _n+1_ problem.

The difference between GraphQL and, for example, REST is that the _n+1_ problem occurs on the server rather than on the client.
The clear benefit is that we only have to deal with this problem once on the server rather than on every client.

To illustrate the issue that data loaders solve in this context, let’s assume we have the following schema:

```sdl
type Query {
  productById(id: ID): Product
}

type Product {
  id: ID
  name: String
  relatedProducts: [Product]
}
```

The above schema allows to fetch a product by its internal identifier and each product has a list of related products that is represented by a list of products.

A query against the above schema could look like the following:

```graphql
{
  a: productById(id: "a") {
    name
  }

  b: productById(id: "b") {
    name
  }
}
```

The above request fetches two products in one go without the need to call the backend twice. The problem with the GraphQL backend is that field resolvers are atomic and do not have any knowledge about the query as a whole. So, a field resolver does not know that it will be called multiple times in parallel to fetch similar or equal data from the same data source.

The idea of a DataLoader is to batch these two requests into one call to the database.

Let's look at some code to understand what data loaders are doing. First, let's have a look at how we would write our field resolver without data loaders:

```csharp
public async Task<Product?> GetProductByIdAsync(
    string id,
    CatalogContext dbContext,
    CancellationToken cancellationToken)
    => await dbContext.Products.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
```

The above example would result in two calls to the product repository that would then fetch the product one by one from our data source.

If you think that through you see that each GraphQL request would cause multiple requests to our data source resulting in sluggish performance and unnecessary round-trips to our data source.

This means that we reduced the round-trips from our client to our server with GraphQL but still have the round-trips between the data sources and the service layer.

With data loaders we can now centralize the data fetching and reduce the number of round trips to our data source.

Instead of fetching the data from the repository directly, we fetch the data from the data loader.
The data loader batches all the requests together into one request to the database.

```csharp
// This is using the source-generated data loader.
internal static class ProductDataLoader
{
    [DataLoader]
    public static async Task<Dictionary<int, Product>> GetProductByIdAsync(
        IReadOnlyList<int> productIds,
        CatalogContext context,
        CancellationToken cancellationToken)
        => await context.Products
            .Where(t => productIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, cancellationToken);
}

public class Query
{
    public async Task<Product?> GetProductByIdAsync(
        string id,
        IProductByIdDataLoader productById,
        CancellationToken cancellationToken)
        => await productById.LoadAsync(id, cancellationToken);
}
```

Alternatively, you can write a DataLoader by hand without our source generator:

```csharp
public class ProductByIdDataLoader : BatchDataLoader<int, Product>
{
    private readonly IServiceProvider _services;

    public ProductDataLoader1(
        IServiceProvider services,
        IBatchScheduler batchScheduler,
        DataLoaderOptions options)
        : base(batchScheduler, options)
    {
        _services = services;
    }

    protected override async Task<IReadOnlyDictionary<int, Product>> LoadBatchAsync(
        IReadOnlyList<int> keys,
        CancellationToken cancellationToken)
    {
        await using var scope = _services.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<CatalogContext>();

        return await context.Products
            .Where(t => keys.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, cancellationToken);
    }
}
```

# Execution

With a data loader, you can fetch entities with a key.
These are the two generics you have in the class data loaders:

```csharp
public class BatchDataLoader<TId, TEntity>
```

`TId` is used as an identifier of `TEntity`. `TId` is the type of the values you put into `LoadAsync`.

The execution engine of Hot Chocolate tries to batch as much as possible.
It executes resolvers until the queue is empty and then triggers the data loader to resolve the data for the waiting resolvers.

# Data Consistency

DataLoader do not only batch calls to the database, they also cache the database response.
A data loader guarantees data consistency in a single request.
If you load an entity with a data loader in your request more than once, it is given that these two entities are equivalent.

Data loaders do not fetch an entity if there is already an entity with the requested key in the cache.

# Types of DataLoader

In Hot Chocolate you can declare data loaders in two different ways.
You can separate the data loading concern into separate classes or you can use a delegate in the resolver to define data loaders on the fly.
Below you will find the different types of data loaders with examples for class and delegate definition.

## Batch DataLoader (1:1)

> One - To - One, usually used for fields like `productById` or one to one relations

The batch data loader collects requests for entities and sends them as a batch request to the data source. Moreover, the data loader caches the retrieved entries within a request.

The batch data loader gets the keys as `IReadOnlyList<TKey>` and returns an `Dictionary<TKey, TValue>`.

> The `IReadOnlyList<TKey>` representing the key is a rented list and must not be stored or used outside of the `LoadAsync` method.

### Source-Generated

```csharp
internal static class ProductDataLoader
{
    [DataLoader]
    public static async Task<Dictionary<int, Product>> GetProductByIdAsync(
        IReadOnlyList<int> productIds,
        CatalogContext context,
        CancellationToken cancellationToken)
        => await context.Products
            .Where(t => productIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, cancellationToken);
}

public class Query
{
    public async Task<Product?> GetProductByIdAsync(
        string id,
        IProductByIdDataLoader productById,
        CancellationToken cancellationToken)
        => await productById.LoadAsync(id, cancellationToken);
}
```

### Class

```csharp
public class ProductByIdDataLoader : BatchDataLoader<int, Product>
{
    private readonly IServiceProvider _services;

    public ProductDataLoader1(
        IServiceProvider services,
        IBatchScheduler batchScheduler,
        DataLoaderOptions options)
        : base(batchScheduler, options)
    {
        _services = services;
    }

    protected override async Task<IReadOnlyDictionary<int, Product>> LoadBatchAsync(
        IReadOnlyList<int> keys,
        CancellationToken cancellationToken)
    {
        await using var scope = _services.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<CatalogContext>();

        return await context.Products
            .Where(t => keys.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, cancellationToken);
    }
}

public class Query
{
    public async Task<Product?> GetProductByIdAsync(
        string id,
        ProductByIdDataLoader productById,
        CancellationToken cancellationToken)
        => await productById.LoadAsync(id, cancellationToken);
}
```

## Group DataLoader (1:n)

> One - To - Many, usually used for fields like `Brand.products` or one to many relations

The batch data loader can also be used to fetch 1:n relations, where you get many items for a single key. In this case we simple group our data and return an array or a list of entities.

### Source-Generated

```csharp
internal static class ProductDataLoader
{
    [DataLoader]
    public static async Task<Dictionary<int, Product[]>> GetProductsByBrandIdAsync(
        IReadOnlyList<int> brandIds,
        CatalogContext context,
        CancellationToken cancellationToken)
        => await context.Products
            .Where(t => brandIds.Contains(t.BrandId))
            .GroupBy(t => t.BrandId)
            .Select(t => new { t.Key, Items = t.OrderBy(p => p.Name).ToArray() })
            .ToDictionaryAsync(t => t.Key, t => t.Items, cancellationToken);
}

[ObjectType<Brand>]
public static partial class BrandNode
{
    public static async Task<Product[]> GetProductsAsync(
        [Parent] Brand brand,
        IProductsByBrandIdDataLoader productById,
        CancellationToken cancellationToken)
        => await productById.LoadAsync(brand.Id, cancellationToken) ?? [];
}
```

### Class

```csharp
public class ProductByBrandIdDataLoader : BatchDataLoader<int, Product[]>
{
    private readonly IServiceProvider _services;

    public ProductByBrandIdDataLoader(
        IServiceProvider services,
        IBatchScheduler batchScheduler,
        DataLoaderOptions options)
        : base(batchScheduler, options)
    {
        _services = services;
    }

    protected override async Task<IReadOnlyDictionary<int, Product[]>> LoadBatchAsync(
        IReadOnlyList<int> keys,
        CancellationToken cancellationToken)
    {
        await using var scope = _services.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<CatalogContext>();

        return await context.Products
            .Where(t => keys.Contains(t.BrandId))
            .GroupBy(t => t.BrandId)
            .Select(t => new { t.Key, Items = t.OrderBy(p => p.Name).ToArray() })
            .ToDictionaryAsync(t => t.Key, t => t.Items, cancellationToken);
    }
}

[ObjectType<Brand>]
public static partial class BrandNode
{
    public static async Task<Product[]> GetProductsAsync(
        [Parent] Brand brand,
        ProductsByBrandIdDataLoader productById,
        CancellationToken cancellationToken)
        => await productById.LoadAsync(brand.Id, cancellationToken) ?? [];
}
```

## Cache DataLoader

> No batching, just caching. This data loader is used rarely. You most likely want to use the batch data loader.

The cache data loader is the easiest to implement since there is no batching involved. You can just use the initial `GetProductByIdAsync` method. We do not get the benefits of batching with this one, but if in a query the same entity is resolved more than once we will load it only once from the data source.

```csharp
public class ProductCacheDataLoader : CacheDataLoader<int, Product?>
{
    private readonly IServiceProvider _services;

    public ProductDataLoader1(
        IServiceProvider services,
        DataLoaderOptions options)
        : base(options)
    {
        _services = services;
    }

    protected override async Task<Product?> LoadSingleAsync(int key, CancellationToken cancellationToken)
    {
        await using var scope = _services.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<CatalogContext>();
        return await context.Products.FirstOrDefaultAsync(t => t.Id == key, cancellationToken);
    }
}
```
