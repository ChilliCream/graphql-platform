---
title: "DataLoader"
---

DataLoaders solve the N+1 problem in GraphQL. When the execution engine resolves a list of objects and each object needs related data, a naive implementation fires one database query per object. A DataLoader collects all those individual requests, waits for the execution engine to finish the current batch of resolvers, and then sends one query for all requested keys at once.

This page covers the source-generated DataLoader (the recommended approach), manual DataLoader classes, and batch resolvers (a v16 alternative for simpler cases). If you are new to GraphQL data fetching, start with [Resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers) first.

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

A DataLoader batches those N brand lookups into a single query. The resolver asks the DataLoader for each brand by key. The DataLoader collects all keys, waits for the execution engine to drain the current resolver batch, and then fires one `WHERE id IN (...)` query for all requested brands.

# Source-Generated DataLoader

The recommended way to define a DataLoader is with the `[DataLoader]` attribute and the source generator. You write a static method that accepts a list of keys and returns a dictionary of results. The source generator creates the DataLoader class and its interface at build time.

## Batch DataLoader (one-to-one)

Use a batch DataLoader when each key maps to at most one result. This is the most common pattern: fetching an entity by ID.

**C# DataLoader**

```csharp
// DataLoaders/BrandDataLoaders.cs
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

The source generator produces an `IBrandByIdDataLoader` interface and a `BrandByIdDataLoader` class. The name is derived from the method name: `Get` prefix and `Async` suffix are stripped, leaving `BrandById`.

**C# resolver**

```csharp
// Types/ProductNode.cs
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

The resolver requests a brand by key. The DataLoader collects all keys from all concurrently executing resolvers, then calls `GetBrandByIdAsync` once with the full list.

## Group DataLoader (one-to-many)

Use a group DataLoader when each key maps to multiple results. This is common for relationships like "all products for a brand" or "all reviews for a product".

**C# DataLoader**

```csharp
// DataLoaders/ProductDataLoaders.cs
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

The return type is `Dictionary<int, Product[]>`. The source generator recognizes the array value type and generates a group DataLoader. The generated interface is `IProductsByBrandIdDataLoader`.

**C# resolver**

```csharp
// Types/BrandNode.cs
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

## DataLoader Options

The `[DataLoader]` attribute accepts configuration options.

| Property         | Type                       | Default             | Description                                                                                                                                                                               |
| ---------------- | -------------------------- | ------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Name`           | `string?`                  | Derived from method | Override the generated DataLoader class name.                                                                                                                                             |
| `ServiceScope`   | `DataLoaderServiceScope`   | `Default`           | Controls how injected services are resolved. `DataLoaderScope` creates a dedicated scope. `OriginalScope` uses the request scope.                                                         |
| `AccessModifier` | `DataLoaderAccessModifier` | `Default`           | Controls generated class visibility. `Public` makes both class and interface public. `Internal` makes both internal. `PublicInterface` makes the interface public and the class internal. |

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

Use `DataLoaderServiceScope.DataLoaderScope` when your data access service (like `DbContext`) is registered as scoped and you want the DataLoader to have its own scope, separate from the request scope. This prevents lifetime conflicts when the DataLoader outlives a single resolver.

## How Execution Works

The execution engine resolves fields in waves. Within each wave, resolvers run concurrently. When a resolver calls `LoadAsync` on a DataLoader, the key is queued but no database call happens yet. After all resolvers in the current wave finish, the engine dispatches all pending DataLoader batches. Each DataLoader fires a single call to its data source with the full set of collected keys.

```text
Wave 1: Resolve products(first: 5) → [Product1, Product2, Product3, Product4, Product5]
         ↓
Wave 2: Resolve brand for each product → DataLoader collects keys [1, 2, 1, 3, 2]
         ↓
Dispatch: DataLoader deduplicates → sends one query for brand IDs [1, 2, 3]
         ↓
Wave 3: Return cached Brand objects to each product resolver
```

DataLoaders also deduplicate keys. If two products share the same brand ID, the DataLoader sends that ID only once and returns the same Brand instance to both resolvers.

## Data Consistency

A DataLoader caches results for the duration of a single GraphQL request. If the same key is requested multiple times within one request, the DataLoader returns the cached result without hitting the data source again. This guarantees that all resolvers within a request see the same data for the same key.

The cache is not shared across requests. Each request gets a fresh DataLoader instance with an empty cache.

# Batch Resolvers

Batch resolvers are an alternative to DataLoaders for cases where you want to resolve a field for multiple parent objects in a single method call without defining a separate DataLoader class. Instead of each resolver running independently and batching through a DataLoader, the execution engine collects all parent objects and calls your resolver once with the full list.

## When to Use Batch Resolvers vs DataLoaders

**Use a DataLoader** when the batched data is reused across multiple fields or resolvers. DataLoaders cache by key, so the same entity fetched in different parts of the query tree is only loaded once.

**Use a batch resolver** when the resolved value is specific to one field and does not benefit from cross-field caching. Common examples: computed values, string formatting, or calling an external service that supports batch requests natively.

## Defining a Batch Resolver

Mark a method with `[BatchResolver]`. The `[Parent]` parameter and all arguments must be list types. The return type must also be a list, with one element per parent.

**C# resolver**

```csharp
// Types/UserNode.cs
[ObjectType<User>]
public static partial class UserNode
{
    [BatchResolver]
    public static List<string> GetDisplayName([Parent] List<User> users)
    {
        return users.Select(u => $"{u.FirstName} {u.LastName}").ToList();
    }
}
```

The execution engine collects all `User` parent objects being resolved in the current wave and calls `GetDisplayName` once with the full list. The returned list must have the same count and order as the input list.

## Batch Resolvers with Services and Arguments

Batch resolvers support dependency injection and field arguments. Arguments that are list types are collected from each parent context.

```csharp
// Types/UserNode.cs
[ObjectType<User>]
public static partial class UserNode
{
    [BatchResolver]
    public static async Task<List<string>> GetGreeting(
        [Parent] List<User> users,
        GreetingService greetingService,
        CancellationToken ct)
    {
        return await greetingService.GetGreetingsAsync(
            users.Select(u => u.Id).ToList(), ct);
    }
}
```

## Handling Errors in Batch Resolvers

Use `ResolverResult` to return per-item errors without failing the entire batch. Each element in the returned list can be either a success or an error.

```csharp
// Types/UserNode.cs
[ObjectType<User>]
public static partial class UserNode
{
    [BatchResolver]
    public static List<ResolverResult> GetVerificationStatus([Parent] List<User> users)
    {
        return users.Select<User, ResolverResult>(user =>
        {
            if (user.Email is null)
                return ResolverResult.Fail(
                    ErrorBuilder.New()
                        .SetMessage("User has no email address.")
                        .Build());

            return ResolverResult.Ok(user.IsVerified ? "verified" : "pending");
        }).ToList();
    }
}
```

## Code-First Batch Resolvers

In the code-first approach, use `ResolveBatch` on the field descriptor.

```csharp
// Types/UserType.cs
public class UserType : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor
            .Field("displayName")
            .Type<StringType>()
            .ResolveBatch(contexts =>
            {
                var results = new ResolverResult[contexts.Count];

                for (var i = 0; i < contexts.Count; i++)
                {
                    var user = contexts[i].Parent<User>();
                    results[i] = ResolverResult.Ok($"{user.FirstName} {user.LastName}");
                }

                return new ValueTask<IReadOnlyList<ResolverResult>>(results);
            });
    }
}
```

You can also point to an external method with `ResolveBatchWith<T>`.

```csharp
descriptor
    .Field("greeting")
    .ResolveBatchWith<UserNode>(t => t.GetGreeting(default!));
```

# Conditional Data Fetching with Field Selection

The `[IsSelected]` attribute lets a resolver check whether specific fields are selected in the query. This is useful for skipping expensive data fetching when the client does not need the result.

```csharp
// Types/ProductNode.cs
[ObjectType<Product>]
public static partial class ProductNode
{
    public static async Task<ProductDetails> GetDetailsAsync(
        [Parent] Product product,
        [IsSelected(nameof(ProductDetails.Inventory))] bool inventorySelected,
        IProductDetailsDataLoader detailsLoader,
        IInventoryService inventoryService,
        CancellationToken ct)
    {
        var details = await detailsLoader.LoadAsync(product.Id, ct);

        if (inventorySelected)
        {
            details.Inventory = await inventoryService.GetStockAsync(product.Id, ct);
        }

        return details;
    }
}
```

If the client query does not select the `inventory` field, `inventorySelected` is `false` and the inventory service call is skipped.

# Manual DataLoader Classes

You can write DataLoader classes by hand when you need full control over the batching logic. This is rarely needed since the source generator covers most cases.

```csharp
// DataLoaders/BrandByIdDataLoader.cs
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

> The `IReadOnlyList<TKey>` passed to `LoadBatchAsync` and source-generated methods is a rented list. Do not store or use it outside the method body.

# Next Steps

- **Need to understand resolver basics?** See [Resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers).
- **Need pagination?** See [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination) for cursor-based connections.
- **Need to filter or sort data?** See [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering) and [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting).
- **Using Entity Framework?** See [Entity Framework](/docs/hotchocolate/v16/integrations/entity-framework) for integration patterns with DataLoaders.
