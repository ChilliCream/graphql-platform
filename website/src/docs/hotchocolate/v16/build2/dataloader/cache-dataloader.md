---
title: "Cache DataLoaders"
---

A cache DataLoader prevents repeated one-key work during one GraphQL request. Use it when several resolvers, nested fields, or services can ask for the same value by the same key, but the backing source does not support a useful batch API.

For the same loader cache type and the same key, Hot Chocolate stores one task in the request DataLoader cache. Later `LoadAsync` calls return that cached task instead of calling the data source again.

This is request coordination. It is not an application cache, distributed cache, HTTP cache, or persisted operation cache.

## When a cache DataLoader helps

Use a cache DataLoader when:

- The source accepts one key at a time, for example `GET /products/{sku}`.
- The same key can be reached through more than one field path in a single operation.
- Batching is impossible, not useful, or already handled by a lower-level client.
- You still want the normal DataLoader `LoadAsync` API and request-scoped deduplication.

Use another pattern when the source can do more:

| You need to                                                     | Use                                                        |
| --------------------------------------------------------------- | ---------------------------------------------------------- |
| Fetch many database rows by ID in one query                     | Batch DataLoader                                           |
| Fetch many child rows for each parent key                       | Grouped DataLoader                                         |
| Reuse values across requests with expiration or eviction policy | Application or distributed cache                           |
| Control response caching semantics                              | HTTP cache headers or Hot Chocolate cache-control features |

`CacheDataLoader` does not batch database queries. Manual cache loaders set their maximum batch size to `1` and call `LoadSingleAsync` once per cache miss.

## Compare DataLoader shapes

| Loader shape             | Input method shape                      | Data source calls       | Request cache | Best use                     |
| ------------------------ | --------------------------------------- | ----------------------- | ------------- | ---------------------------- |
| Source-generated batch   | `IReadOnlyList<TKey>` to dictionary     | One call for many keys  | Yes           | Entity by ID from a database |
| Source-generated group   | `IReadOnlyList<TKey>` to grouped values | One call for many keys  | Yes           | One-to-many relationships    |
| Source-generated cache   | One `TKey` to one value                 | One call per cache miss | Yes           | External one-key source      |
| Manual `BatchDataLoader` | Override `LoadBatchAsync`               | One call for many keys  | Yes           | Custom batching control      |
| Manual `CacheDataLoader` | Override `LoadSingleAsync`              | One call per cache miss | Yes           | Custom cache-only control    |

Prefer source generation when its method shape fits. Use manual `CacheDataLoader<TKey, TValue>` when you need a custom class, custom constructor, or explicit loader implementation.

## Understand the request cache lifecycle

Hot Chocolate creates the DataLoader cache for the GraphQL request. The cache is discarded when the request scope ends.

```text
Resolver A: LoadAsync("ABC") -> miss -> LoadSingleAsync("ABC")
Resolver B: LoadAsync("ABC") -> same cached task
Resolver C: LoadAsync("XYZ") -> miss -> LoadSingleAsync("XYZ")
End of request -> request cache discarded
```

`LoadAsync(keys)` checks the cache for each key independently. Cached keys reuse their stored tasks. Missing keys are fetched by the loader.

The internal cache entry combines the loader cache type and your key value:

```text
PromiseCacheKey(Type = "ProductBySkuDataLoader", Key = "ABC")
PromiseCacheKey(Type = "ProductByIdDataLoader",  Key = 123)
```

Two loaders can use the same user key value without colliding because the loader cache type is part of the cache key.

## Cache a repeated one-key lookup

The following example caches product lookups by SKU for one GraphQL request.

Start with a one-key service or client:

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

Create a manual cache loader by deriving from `CacheDataLoader<TKey, TValue>`:

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

Use the loader from a resolver:

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

A client can reach the same SKU through more than one path:

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

If `featuredProduct` and one line item use SKU `ABC`, the request calls `ProductApi.GetBySkuAsync("ABC", ct)` once for `ProductBySkuDataLoader`. The next GraphQL request starts with an empty DataLoader cache.

## Use a source-generated cache loader

A source-generated cache loader uses a single-key method that returns one value. This is different from a batch loader method, which accepts `IReadOnlyList<TKey>` and returns a dictionary or lookup.

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

The generator derives `ProductBySkuDataLoader` and `IProductBySkuDataLoader` from the method name by removing `Get` and `Async`. The generated loader implements `IDataLoader<string, Product?>`, loops over cache misses, and invokes your single-key method once per missing key.

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

For source-generator setup, batch signatures, and generated naming details, see [DataLoader](./index).

## Design stable cache keys

The user key participates in equality. Choose key types whose equality matches the lookup performed by the source.

| Key shape                     | Guidance                                                            |
| ----------------------------- | ------------------------------------------------------------------- |
| Database ID, SKU, external ID | Prefer stable scalar keys when possible.                            |
| Case-insensitive strings      | Normalize before `LoadAsync`, for example uppercase invariant SKUs. |
| Composite key                 | Use an immutable value type with value equality.                    |
| Mutable reference type        | Avoid unless object identity is the intended key.                   |

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

Keep one DataLoader per lookup shape. Do not mix product ID, SKU, and name in one string key space. Separate loaders make cache behavior visible and avoid accidental collisions in your own key design.

## Add, remove, and clear cache entries

Use cache entry APIs when application code already knows a value or when a mutation changes a value during the same request.

| API                         | Effect                                               |
| --------------------------- | ---------------------------------------------------- |
| `SetCacheEntry(key, value)` | Adds a value if the entry is absent.                 |
| `SetCacheEntry(key, task)`  | Adds a task if the entry is absent.                  |
| `RemoveCacheEntry(key)`     | Removes one entry for the loader and key.            |
| `ClearCache()`              | Clears the underlying cache available to the loader. |

`SetCacheEntry` does not replace an existing cached task. Remove first when you need replacement.

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

Prefer `RemoveCacheEntry(key)` for targeted changes. Use `ClearCache()` with care because it clears the cache instance used by the loader, which can affect unrelated keys in the same request.

## Keep mutations consistent within one request

A value loaded before a mutation can remain cached for the rest of the request. This matters when a mutation payload or child field reads through the same DataLoader after the write.

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

The next GraphQL request receives a fresh DataLoader cache. This section is about within-request consistency.

## Handle nulls, errors, and cancellation

- `LoadSingleAsync` receives the request cancellation token. Pass it to HTTP, database, and service calls.
- `LoadAsync(null)` throws before `LoadSingleAsync` runs. Normalize optional arguments before loading.
- Exceptions thrown by `LoadSingleAsync` are captured for the corresponding key and surface through that key's task.
- Because the cached entry is a task, a later same-key load during the request can observe the same failure.
- Model missing values with nullable value types and matching GraphQL field nullability, or convert missing data into a GraphQL error in the resolver.

## Avoid common cache misunderstandings

| Misunderstanding                                      | Correction                                                                                         |
| ----------------------------------------------------- | -------------------------------------------------------------------------------------------------- |
| `CacheDataLoader` batches database queries.           | It calls the source once per cache miss. Use a batch DataLoader when the source accepts many keys. |
| The DataLoader cache is an application cache.         | It is scoped to one GraphQL request.                                                               |
| HTTP cache-control and DataLoader cache are the same. | Cache-control affects response semantics. DataLoader cache coordinates resolver key loads.         |
| Any object works as a key.                            | Key equality must match lookup semantics.                                                          |
| `SetCacheEntry` replaces existing values.             | It adds when absent. Remove first to replace.                                                      |
| `ClearCache` is a harmless reset.                     | It is broader than key removal. Prefer `RemoveCacheEntry` for one changed value.                   |

## Use stateful cache loaders for advanced scenarios

Most cache loaders only need the key and cancellation token. If a manual loader needs `DataLoaderFetchContext<TValue>` while fetching a single key, derive from `StatefulCacheDataLoader<TKey, TValue>` instead.

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

Keep this for cases where the normal generated or manual cache loader cannot express the required state.

## Next steps

- Learn the DataLoader overview in [DataLoader](./index).
- Use resolver injection patterns from [Service Injection](../resolvers/service-injection).
- Review resolver parameter basics in [Resolver Signatures](../resolvers/resolver-signature).
- Use [Mutations](../schema-elements/operations-mutations) when write flows interact with cached reads.
- Review broader execution and batching guidance in [Performance](/docs/hotchocolate/v16/guides/performance).
- Distinguish response caching with [Cache Control](/docs/hotchocolate/v16/server/cache-control).
