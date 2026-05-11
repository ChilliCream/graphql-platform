---
title: "Cache DataLoaders"
---

A cache DataLoader prevents repeated single-key work within a single GraphQL request. This is useful when multiple resolvers, nested fields, or services might request the same value by the same key, but the underlying source does not offer a batch API.

For each loader cache type and key, Hot Chocolate stores a single task in the request DataLoader cache. Subsequent `LoadAsync` calls for the same key return the cached task, avoiding duplicate data source calls.

This mechanism coordinates requests within a single operation. It is not an application cache, distributed cache, HTTP cache, or a persisted operation cache.

## When to Use a Cache DataLoader

Consider a cache DataLoader when:

- The data source only accepts one key at a time, such as `GET /products/{sku}`.
- The same key might be accessed through different field paths in a single operation.
- Batching is not possible, not beneficial, or already handled by a lower-level client.
- You want to use the standard DataLoader `LoadAsync` API and benefit from request-scoped deduplication.

Choose a different pattern if the source supports more advanced operations:

| Requirement                                                     | Recommended Pattern                                        |
| --------------------------------------------------------------- | ---------------------------------------------------------- |
| Fetch multiple database rows by ID in a single query            | Batch DataLoader                                           |
| Fetch many child rows for each parent key                       | Grouped DataLoader                                         |
| Reuse values across requests with expiration or eviction policy | Application or distributed cache                           |
| Control response caching semantics                              | HTTP cache headers or Hot Chocolate cache-control features |

`CacheDataLoader` does not batch database queries. Manual cache loaders set their maximum batch size to `1` and call `LoadSingleAsync` for each cache miss.

## Comparing DataLoader Types

| Loader Type              | Input Method Shape                      | Data Source Calls       | Request Cache | Best Use                     |
| ------------------------ | --------------------------------------- | ----------------------- | ------------- | ---------------------------- |
| Source-generated batch   | `IReadOnlyList<TKey>` to dictionary     | One call for many keys  | Yes           | Entity by ID from a database |
| Source-generated group   | `IReadOnlyList<TKey>` to grouped values | One call for many keys  | Yes           | One-to-many relationships    |
| Source-generated cache   | One `TKey` to one value                 | One call per cache miss | Yes           | External one-key source      |
| Manual `BatchDataLoader` | Override `LoadBatchAsync`               | One call for many keys  | Yes           | Custom batching control      |
| Manual `CacheDataLoader` | Override `LoadSingleAsync`              | One call per cache miss | Yes           | Custom cache-only control    |

Prefer source generation when the method shape matches your needs. Use a manual `CacheDataLoader<TKey, TValue>` if you require a custom class, constructor, or explicit loader implementation.

## Understanding the Request Cache Lifecycle

Hot Chocolate creates the DataLoader cache for each GraphQL request. The cache is discarded when the request scope ends.

```text
Resolver A: LoadAsync("ABC") -> miss -> LoadSingleAsync("ABC")
Resolver B: LoadAsync("ABC") -> same cached task
Resolver C: LoadAsync("XYZ") -> miss -> LoadSingleAsync("XYZ")
End of request -> request cache discarded
```

`LoadAsync(keys)` checks the cache for each key individually. Cached keys reuse their stored tasks, while missing keys are fetched by the loader.

The internal cache entry combines the loader cache type and your key value:

```text
PromiseCacheKey(Type = "ProductBySkuDataLoader", Key = "ABC")
PromiseCacheKey(Type = "ProductByIdDataLoader",  Key = 123)
```

Two loaders can use the same user key value without conflict, since the loader cache type is part of the cache key.

## Caching a Repeated Single-Key Lookup

The following example demonstrates caching product lookups by SKU within a single GraphQL request.

Start with a service or client that retrieves a single key:

```csharp
using System.Net;
using System.Net.Http.Json;

public sealed record Product(int Id, string Sku, string Name);

public sealed class ProductApi
{
    public async Task<Product?> GetBySkuAsync(
        string sku,
        CancellationToken cancellationToken)
    {
        using var response = await _http.GetAsync(
            $"products/{Uri.EscapeDataString(sku)}",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Product>(
            cancellationToken);
    }

    private readonly HttpClient _http;

    public ProductApi(HttpClient http)
    {
        _http = http;
    }
}
```

Next, create a manual cache loader by inheriting from `CacheDataLoader<TKey, TValue>`:

```csharp
using GreenDonut;

public sealed class ProductBySkuDataLoader
    : CacheDataLoader<string, Product?>
{
    private readonly ProductApi _api;

    public ProductBySkuDataLoader(
        ProductApi api,
        DataLoaderOptions options)
        : base(options)
    {
        _api = api;
    }

    protected override Task<Product?> LoadSingleAsync(
        string sku,
        CancellationToken cancellationToken)
    {
        return _api.GetBySkuAsync(sku, cancellationToken);
    }
}
```

Register the API client and the DataLoader:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<ProductApi>(client =>
{
    client.BaseAddress = new Uri("https://catalog.example/");
});

builder
    .AddGraphQL()
    .AddTypes()
    .AddDataLoader<ProductBySkuDataLoader>();
```

Use the loader in a resolver:

```csharp
using HotChocolate;
using HotChocolate.Types;

public sealed record LineItem(int Quantity, string Sku);

[ObjectType<LineItem>]
public static partial class LineItemNode
{
    public static async Task<Product?> GetProductAsync(
        [Parent] LineItem item,
        ProductBySkuDataLoader productBySku,
        CancellationToken cancellationToken)
    {
        return await productBySku.LoadAsync(item.Sku, cancellationToken);
    }
}
```

A client may access the same SKU through multiple paths:

```graphql
type Query {
  orderById(id: ID!): Order
}

type Order {
  featuredProduct: Product
  lineItems: [LineItem!]!
}

type LineItem {
  quantity: Int!
  product: Product
}

type Product {
  sku: String!
  name: String!
}
```

```graphql
query GetOrder($id: ID!) {
  orderById(id: $id) {
    featuredProduct {
      sku
      name
    }
    lineItems {
      quantity
      product {
        sku
        name
      }
    }
  }
}
```

If both `featuredProduct` and a line item use SKU `ABC`, the request calls `ProductApi.GetBySkuAsync("ABC", ct)` only once for `ProductBySkuDataLoader`. The next GraphQL request starts with a new, empty DataLoader cache.

## Using a Source-Generated Cache Loader

A source-generated cache loader uses a single-key method that returns one value. This differs from a batch loader, which accepts `IReadOnlyList<TKey>` and returns a dictionary or lookup.

```csharp
using GreenDonut;

internal static class ProductDataLoaders
{
    [DataLoader]
    public static Task<Product?> GetProductBySkuAsync(
        string sku,
        ProductApi api,
        CancellationToken cancellationToken)
    {
        return api.GetBySkuAsync(sku, cancellationToken);
    }
}
```

The generator creates `ProductBySkuDataLoader` and `IProductBySkuDataLoader` from the method name by removing `Get` and `Async`. The generated loader implements `IDataLoader<string, Product?>`, iterates over cache misses, and calls your single-key method for each missing key.

Inject the generated interface into resolvers or services:

```csharp
using HotChocolate;
using HotChocolate.Types;

[ObjectType<LineItem>]
public static partial class LineItemNode
{
    public static Task<Product?> GetProductAsync(
        [Parent] LineItem item,
        IProductBySkuDataLoader productBySku,
        CancellationToken cancellationToken)
    {
        return productBySku.LoadAsync(item.Sku, cancellationToken);
    }
}
```

For details on source-generator setup, batch signatures, and generated naming, see [DataLoader](./index).

## Designing Stable Cache Keys

The user key is used for equality checks. Choose key types whose equality matches the lookup performed by the source.

| Key Shape                     | Guidance                                                    |
| ----------------------------- | ----------------------------------------------------------- |
| Database ID, SKU, external ID | Prefer stable scalar keys when possible.                    |
| Case-insensitive strings      | Normalize before `LoadAsync`, such as using uppercase SKUs. |
| Composite key                 | Use an immutable value type with value equality.            |
| Mutable reference type        | Avoid unless object identity is the intended key.           |

A composite key can be a `readonly record struct`:

```csharp
public readonly record struct ProductPriceKey(int ProductId, string Currency);

public static ProductPriceKey CreatePriceKey(int productId, string currency)
{
    return new ProductPriceKey(
        productId,
        currency.ToUpperInvariant());
}
```

Maintain one DataLoader per lookup shape. Do not mix product ID, SKU, and name in a single string key space. Separate loaders make cache behavior clear and prevent accidental key collisions.

## Adding, Removing, and Clearing Cache Entries

Use cache entry APIs when your application already knows a value or when a mutation changes a value during the same request.

| API                         | Effect                                               |
| --------------------------- | ---------------------------------------------------- |
| `SetCacheEntry(key, value)` | Adds a value if the entry is absent.                 |
| `SetCacheEntry(key, task)`  | Adds a task if the entry is absent.                  |
| `RemoveCacheEntry(key)`     | Removes one entry for the loader and key.            |
| `ClearCache()`              | Clears the underlying cache available to the loader. |

`SetCacheEntry` does not replace an existing cached task. Remove the entry first if you need to replace it.

```csharp
public sealed record FeaturedProductDto(string Sku, Product? Product);

public static async Task<Product?> GetFeaturedProductAsync(
    FeaturedProductDto dto,
    ProductBySkuDataLoader productBySku,
    CancellationToken cancellationToken)
{
    if (dto.Product is not null)
    {
        productBySku.SetCacheEntry(dto.Product.Sku, dto.Product);
        return dto.Product;
    }

    return await productBySku.LoadAsync(dto.Sku, cancellationToken);
}
```

Prefer `RemoveCacheEntry(key)` for targeted changes. Use `ClearCache()` with caution, as it clears the cache instance for the loader and may affect unrelated keys in the same request.

## Keeping Mutations Consistent Within a Request

A value loaded before a mutation can remain cached for the rest of the request. This is important when a mutation payload or child field reads through the same DataLoader after a write.

```csharp
using HotChocolate.Types;

public sealed class ProductService
{
    public Task<Product?> RenameBySkuAsync(
        string sku,
        string name,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<Product?>(new Product(1, sku, name));
    }
}

[MutationType]
public static partial class ProductMutations
{
    public static async Task<Product?> RenameProductAsync(
        string sku,
        string name,
        ProductService products,
        ProductBySkuDataLoader productBySku,
        CancellationToken cancellationToken)
    {
        var updated = await products.RenameBySkuAsync(
            sku,
            name,
            cancellationToken);

        productBySku.RemoveCacheEntry(sku);
        productBySku.SetCacheEntry(sku, updated);

        return updated;
    }
}
```

If you do not have the updated value, remove the entry before reloading:

```csharp
productBySku.RemoveCacheEntry(sku);
var reloaded = await productBySku.LoadAsync(sku, cancellationToken);
```

Each new GraphQL request receives a fresh DataLoader cache. This section focuses on consistency within a single request.

## Handling Nulls, Errors, and Cancellation

- `LoadSingleAsync` receives the request cancellation token. Pass it to HTTP, database, and service calls.
- `LoadAsync(null)` throws before `LoadSingleAsync` runs. Normalize optional arguments before loading.
- Exceptions thrown by `LoadSingleAsync` are captured for the corresponding key and surface through that key's task.
- Since the cached entry is a task, a later same-key load during the request can observe the same failure.
- Model missing values with nullable value types and matching GraphQL field nullability, or convert missing data into a GraphQL error in the resolver.

## Common Cache Misunderstandings

| Misunderstanding                                      | Correction                                                                                         |
| ----------------------------------------------------- | -------------------------------------------------------------------------------------------------- |
| `CacheDataLoader` batches database queries.           | It calls the source once per cache miss. Use a batch DataLoader when the source accepts many keys. |
| The DataLoader cache is an application cache.         | It is scoped to one GraphQL request.                                                               |
| HTTP cache-control and DataLoader cache are the same. | Cache-control affects response semantics. DataLoader cache coordinates resolver key loads.         |
| Any object works as a key.                            | Key equality must match lookup semantics.                                                          |
| `SetCacheEntry` replaces existing values.             | It adds when absent. Remove first to replace.                                                      |
| `ClearCache` is a harmless reset.                     | It is broader than key removal. Prefer `RemoveCacheEntry` for one changed value.                   |

## Using Stateful Cache Loaders for Advanced Scenarios

Most cache loaders only require the key and cancellation token. If a manual loader needs `DataLoaderFetchContext<TValue>` while fetching a single key, inherit from `StatefulCacheDataLoader<TKey, TValue>` instead.

```csharp
using GreenDonut;

public sealed class ProductBySkuDataLoader
    : StatefulCacheDataLoader<string, Product?>
{
    private readonly ProductApi _api;

    public ProductBySkuDataLoader(
        ProductApi api,
        DataLoaderOptions options)
        : base(options)
    {
        _api = api;
    }

    protected override Task<Product?> LoadSingleAsync(
        string sku,
        DataLoaderFetchContext<Product?> context,
        CancellationToken cancellationToken)
    {
        // Read context data only when the loader design requires it.
        return _api.GetBySkuAsync(sku, cancellationToken);
    }
}
```

Use this approach only when the standard generated or manual cache loader cannot express the required state.

## Next Steps

- Learn more in the [DataLoader overview](./index).
- Explore resolver injection patterns in [Service Injection](../resolvers/service-injection).
- Review resolver parameter basics in [Resolver Signatures](../resolvers/resolver-signature).
- Use [Mutations](../type-system/operations-mutations) when write operations interact with cached reads.
- See broader execution and batching guidance in [Performance](/docs/hotchocolate/v16/_leagcy/guides/performance).
- Understand response caching with [Cache Control](/docs/hotchocolate/v16/build/performance/cache-control).
