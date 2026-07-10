---
title: "DataLoader"
metaTitle: "GraphQL DataLoader: Solve the N+1 Problem"
description: "Solve the N+1 problem in Hot Chocolate with DataLoader: source-generated methods that batch, cache, and deduplicate data lookups within a GraphQL request."
---

DataLoaders solve the N+1 problem in GraphQL by batching key-based data lookups. When the execution engine resolves a list of objects and each object needs related data, a naive implementation fires one database query per object. A DataLoader collects the requested keys while resolvers execute and then sends one query for all keys at once, deduplicating and caching results for the rest of the request.

This page covers the source-generated DataLoader (the recommended approach) and manual DataLoader classes. If you are new to GraphQL data fetching, start with [Resolvers](../../resolvers/index.md) first. For the background on why batching matters, see the [Batching overview](./index.md).

# The N+1 Problem

Consider a schema where each product has a brand, and clients can query a list of products with their brands.

**GraphQL schema**

```graphql
type Query {
  products(first: 5): ProductsConnection
}

type Product {
  id: ID!
  name: String!
  brand: Brand!
}

type Brand {
  id: ID!
  name: String!
}
```

**Client query**

```graphql
query {
  products(first: 5) {
    nodes {
      name
      brand {
        name
      }
    }
  }
}
```

Without a DataLoader, the `brand` resolver executes once per product. Five products means five database queries for brands, even if several products share the same brand. With 50 products, that becomes 50 queries. This is the N+1 problem: 1 query for the product list, plus N queries for related data.

A DataLoader batches those N brand lookups into a single query. The resolver asks the DataLoader for each brand by key. The DataLoader collects the keys while the engine executes resolvers and then fires one `WHERE id IN (...)` query for all requested brands.

# Source-Generated DataLoader

The recommended way to define a DataLoader is with the `[DataLoader]` attribute and the source generator. You write a static method that accepts a list of keys and returns a dictionary of results. The source generator creates the DataLoader class and its interface at build time.

## Batch DataLoader (one-to-one)

Use a batch DataLoader when each key maps to at most one result. This is the most common pattern: fetching an entity by ID.

**C# DataLoader**

```csharp
internal static class BrandDataLoaders
{
    [DataLoader]
    public static async Task<Dictionary<int, Brand>> GetBrandByIdAsync(
        IReadOnlyList<int> ids,
        CatalogContext db,
        CancellationToken ct)
        => await db.Brands
            .Where(b => ids.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, ct);
}
```

The source generator produces an `IBrandByIdDataLoader` interface and a `BrandByIdDataLoader` class. The name is derived from the method name: the `Get` prefix and the `Async` suffix are stripped, leaving `BrandById`.

**C# resolver**

```csharp
[ObjectType<Product>]
public static partial class ProductNode
{
    public static async Task<Brand> GetBrandAsync(
        [Parent] Product product,
        IBrandByIdDataLoader brandById,
        CancellationToken ct)
        => await brandById.LoadAsync(product.BrandId, ct);
}
```

The resolver requests a brand by key. The DataLoader collects the keys from all concurrently executing resolvers, then calls `GetBrandByIdAsync` once with the full list. Duplicate keys are collapsed: if five products reference only two distinct brands, the fetch method receives exactly two keys in one call.

## Group DataLoader (one-to-many)

Use a group DataLoader when each key maps to multiple results. This is common for relationships like "all products for a brand" or "all reviews for a product".

**C# DataLoader**

```csharp
internal static class ProductDataLoaders
{
    [DataLoader]
    public static async Task<Dictionary<int, Product[]>> GetProductsByBrandIdAsync(
        IReadOnlyList<int> brandIds,
        CatalogContext db,
        CancellationToken ct)
        => await db.Products
            .Where(p => brandIds.Contains(p.BrandId))
            .GroupBy(p => p.BrandId)
            .Select(g => new { g.Key, Items = g.OrderBy(p => p.Name).ToArray() })
            .ToDictionaryAsync(g => g.Key, g => g.Items, ct);
}
```

The return type is `Dictionary<int, Product[]>`. The generated interface is `IProductsByBrandIdDataLoader`.

**C# resolver**

```csharp
[ObjectType<Brand>]
public static partial class BrandNode
{
    public static async Task<Product[]> GetProductsAsync(
        [Parent] Brand brand,
        IProductsByBrandIdDataLoader productsByBrandId,
        CancellationToken ct)
        => await productsByBrandId.LoadAsync(brand.Id, ct) ?? [];
}
```

When the DataLoader returns `null` for a key (the brand has no products), use the null-coalescing operator to return an empty array.

## How the Generator Classifies Your Method

The DataLoader kind is derived from the method signature:

| Method shape                                                | Kind  | Behavior                                                              |
| ----------------------------------------------------------- | ----- | --------------------------------------------------------------------- |
| `Task<Dictionary<TKey, TValue>>` with `IReadOnlyList<TKey>` | Batch | One fetch call per batch; missing keys resolve to `null` / `default`. |
| `Task<ILookup<TKey, TValue>>` with `IReadOnlyList<TKey>`    | Group | One fetch call per batch; missing keys resolve to an empty array.     |
| `Task<TValue>` with a single `TKey` parameter               | Cache | Fetches each key individually, but caches results per request.        |

`IReadOnlyDictionary<TKey, TValue>` and other `IDictionary<TKey, TValue>` implementations work as batch return types too. A `Dictionary<TKey, TValue[]>` (like the group example above) is technically a batch loader whose value is an array, which is why a missing key yields `null` there. If you return `ILookup<TKey, TValue>` instead, the generated loader returns an empty array for missing keys.

Beyond the key parameter, a DataLoader method can declare additional parameters: a `CancellationToken`, services from dependency injection (like the `CatalogContext` above), `[DataLoaderState]` parameters for passing context data, and data-integration parameters such as `PagingArguments`, `QueryContext<T>`, or `ISelectorBuilder` (see [Entity Framework](../integrations/entity-framework.md) and [Pagination](../pagination.md)).

## Handling Missing Keys

A batch DataLoader returns `null` for keys that are absent from the returned dictionary. If that `null` flows into a non-nullable GraphQL field, the execution engine reports a standard non-null violation at that field's path (error code `HC0018`) and applies regular null propagation:

**Response**

```json
{
  "errors": [
    {
      "message": "Cannot return null for non-nullable field.",
      "path": ["products", 0, "brand"],
      "extensions": { "code": "HC0018" }
    }
  ],
  "data": { "products": [null] }
}
```

If a missing key is a valid state, make the field nullable or handle the `null` in the resolver. If a missing key indicates broken data, use `LoadRequiredAsync` instead of `LoadAsync`: it throws a `KeyNotFoundException` naming the missing key, which surfaces as a clearer error than a generic non-null violation.

## Registration

Declare a DataLoader module for your assembly. The source generator then emits a registration extension method named after the module:

```csharp
[assembly: DataLoaderModule("CatalogDataLoaders")]
```

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCatalogDataLoaders();

builder.Services
    .AddGraphQLServer()
    .AddTypes();
```

`AddCatalogDataLoaders()` registers every generated DataLoader with the dependency injection container. Resolvers do not need any further wiring: declaring the generated interface as a resolver parameter (as in the examples above) is enough, and Hot Chocolate injects the request's DataLoader instance automatically.

## DataLoader Options

The `[DataLoader]` attribute accepts configuration options.

| Option           | Type                       | Default                      | Description                                                                                                                                                                                                                                                                       |
| ---------------- | -------------------------- | ---------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Name`           | `string`                   | Derived from the method name | First constructor argument: `[DataLoader("...")]`. Overrides the generated DataLoader name. Used verbatim, with `DataLoader` appended, so `[DataLoader("BrandLookup")]` generates `BrandLookupDataLoader`.                                                                        |
| `MaxBatchSize`   | `int`                      | `1024`                       | Maximum number of keys per fetch call. When more keys are pending, they are split into multiple batches. `0` disables splitting.                                                                                                                                                  |
| `ServiceScope`   | `DataLoaderServiceScope`   | `Default`                    | Controls how injected services are resolved. `DataLoaderScope` creates a dedicated scope per fetch. `OriginalScope` resolves services from the request scope. `Default` defers to the assembly-level `[DataLoaderDefaults]` attribute; without one, a dedicated scope is created. |
| `AccessModifier` | `DataLoaderAccessModifier` | `Default`                    | Controls generated code visibility. `Public` makes both class and interface public. `Internal` makes both internal. `PublicInterface` makes the interface public and the class internal. `Default` defers to `[DataLoaderDefaults]`; without one, both are public.                |
| `Lookups`        | `string[]`                 | None                         | Names of methods on the same class that derive additional cache keys from loaded values, so an entity fetched by one key can be resolved from the cache by another.                                                                                                               |

> [!NOTE]
> `Name` is a positional constructor argument, not a settable property. `[DataLoader(Name = "X")]` does not compile; use `[DataLoader("X")]`.

### MaxBatchSize

Use `MaxBatchSize` to protect downstream systems from unbounded batch sizes (SQL parameter limits, HTTP request size limits):

```csharp
[DataLoader(MaxBatchSize = 2)]
public static Task<IReadOnlyDictionary<int, string>> GetValueByKeyAsync(
    IReadOnlyList<int> keys,
    CancellationToken cancellationToken)
{
    IReadOnlyDictionary<int, string> result =
        keys.ToDictionary(k => k, k => k.ToString());
    return Task.FromResult(result);
}
```

Loading five keys through this DataLoader produces three fetch calls with 2, 2, and 1 keys, and the results are still assembled transparently for all callers. Split batches are dispatched concurrently, not one after another.

### ServiceScope

```csharp
[DataLoader(ServiceScope = DataLoaderServiceScope.DataLoaderScope)]
public static async Task<Dictionary<int, Brand>> GetBrandByIdAsync(
    IReadOnlyList<int> ids,
    CatalogContext db,
    CancellationToken ct)
    => await db.Brands
        .Where(b => ids.Contains(b.Id))
        .ToDictionaryAsync(b => b.Id, ct);
```

Use `DataLoaderServiceScope.DataLoaderScope` when your data access service (like a `DbContext`) is registered as scoped and you want the DataLoader to have its own scope, separate from the request scope. This prevents lifetime conflicts when the DataLoader outlives a single resolver. When `ServiceScope` is not set and no `[DataLoaderDefaults]` assembly attribute overrides it, the generator already creates a dedicated scope, so setting `DataLoaderScope` makes that choice explicit. Use `OriginalScope` to resolve services from the request scope instead.

## How Execution Works

When a resolver calls `LoadAsync`, no data source call happens yet: the key is added to the DataLoader's current batch and the resolver receives a task. The execution engine keeps running other resolvers, and once no more resolver work is immediately ready, each pending batch is dispatched as a single call to its fetch method with all collected keys.

```text
Resolve products(first: 5)  → [Product1 .. Product5]
brand resolvers call LoadAsync → keys accumulate: [1, 2, 1, 3, 2]
no more resolver work ready    → dispatch: one fetch call with keys [1, 2, 3]
brand resolvers resume         → each receives its Brand from the result
```

Sibling resolvers that use the same DataLoader within one execution pass are combined into a single batch. The dispatch trigger is "no more ready work", not a fixed schedule and not one dispatch per level of the query.

DataLoaders also deduplicate keys. If two products share the same brand ID, the DataLoader sends that ID once and returns the same result to both resolvers. In the example above, five products referencing three distinct brands produce exactly one fetch call with three keys.

## Data Consistency

A DataLoader caches results for the duration of a single GraphQL request. If the same key is requested multiple times within one request, the DataLoader returns the cached result without hitting the data source again, even across different fields and depths of the query. This guarantees that all resolvers within a request see the same data for the same key.

Both the DataLoader instance and its cache live for exactly one request (or one subscription event). Nothing is cached across requests, and no data can leak between requests or users.

# Manual DataLoader Classes

You can write DataLoader classes by hand when you need full control over the batching logic. This is rarely needed since the source generator covers most cases.

```csharp
public class BrandByIdDataLoader : BatchDataLoader<int, Brand>
{
    private readonly IServiceProvider _services;

    public BrandByIdDataLoader(
        IServiceProvider services,
        IBatchScheduler batchScheduler,
        DataLoaderOptions options)
        : base(batchScheduler, options)
    {
        _services = services;
    }

    protected override async Task<IReadOnlyDictionary<int, Brand>> LoadBatchAsync(
        IReadOnlyList<int> keys,
        CancellationToken ct)
    {
        await using var scope = _services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogContext>();

        return await db.Brands
            .Where(b => keys.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, ct);
    }
}
```

> [!WARNING]
> The `IReadOnlyList<TKey>` passed to `LoadBatchAsync` and to source-generated methods is only valid for the duration of the fetch call. Do not store a reference to it or use it after the method completes; copy the keys if you need them longer.

# Next Steps

- **Need to batch a single field for many parents?** See [Batch Resolvers](./batch-resolver.md).
- **Need to understand resolver basics?** See [Resolvers](../../resolvers/index.md).
- **Need pagination?** See [Pagination](../pagination.md) for cursor-based connections.
- **Using Entity Framework?** See [Entity Framework](../integrations/entity-framework.md) for integration patterns with DataLoaders.
