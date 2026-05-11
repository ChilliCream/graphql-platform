---
title: "Group DataLoader"
---

# Load One-to-Many Relationships with a Group DataLoader

A group DataLoader is ideal when a parent key maps to a collection of children. Typical scenarios include products by brand, reviews by product, or orders by user.

This approach allows each field resolver to request the children for its parent, while Hot Chocolate batches all parent keys into a single data-source call:

```text
Batch by parent key -> query all children -> group by parent key -> return one collection per parent
```

Begin with a source-generated `[DataLoader]` method that takes an `IReadOnlyList<TKey>` and returns a dictionary where each value is a collection, such as `Dictionary<int, Product[]>`. In source generator terms, this is an array-valued batch loader. Throughout this page, "group DataLoader" refers to this one-to-many lookup pattern.

## When to Use This Pattern

| You need to                                                           | Use                                                                                          |
| --------------------------------------------------------------------- | -------------------------------------------------------------------------------------------- |
| Load child collections by parent key, such as products for each brand | Group DataLoader pattern                                                                     |
| Load one entity or value by key, such as brand by ID                  | [Batch DataLoader](./batch-dataloader)                                                       |
| Batch one field without reusable keyed caching                        | [Batch resolver](/docs/hotchocolate/v16/build/dataloader#batch-resolvers)                    |
| Return large or pageable child collections                            | [Pagination](/docs/hotchocolate/v16/build/pagination) with paging-specific DataLoader shapes |

Create a separate loader for each lookup shape. For example, `ProductsByBrandId` and `ReviewsByProductId` should be distinct loaders, as they batch different keys and return different child types.

## Understanding the Child-Collection N+1 Problem

Consider a query that requests several brands and each brand's products:

```graphql
query GetBrands {
  brands(first: 3) {
    nodes {
      id
      name
      products {
        id
        name
      }
    }
  }
}
```

If you place the product query directly in the child resolver, you may end up running one product query per brand:

```csharp
using HotChocolate;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;

namespace Demo.Catalog;

[ObjectType<Brand>]
public static partial class BrandNode
{
    public static async Task<Product[]> GetProductsAsync(
        [Parent] Brand brand,
        CatalogContext db,
        CancellationToken ct)
    {
        return await db.Products
            .Where(p => p.BrandId == brand.Id)
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Id)
            .ToArrayAsync(ct);
    }
}
```

Here, the brand list loads once, but each `Brand.products` resolver executes its own child query. Using a group DataLoader keeps the resolver concise and moves the batched data access into a reusable lookup.

## Creating the Source-Generated Loader

The following examples assume an EF Core `CatalogContext` with `Brands` and `Products` sets, where `Product.BrandId` is a foreign key to `Brand.Id`.

```csharp
using GreenDonut;
using Microsoft.EntityFrameworkCore;

namespace Demo.Catalog;

internal static class ProductDataLoaders
{
    [DataLoader]
    public static async Task<Dictionary<int, Product[]>> GetProductsByBrandIdAsync(
        IReadOnlyList<int> brandIds,
        CatalogContext db,
        CancellationToken ct)
    {
        var products = await db.Products
            .Where(p => brandIds.Contains(p.BrandId))
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Id)
            .ToListAsync(ct);

        return products
            .GroupBy(p => p.BrandId)
            .ToDictionary(group => group.Key, group => group.ToArray());
    }
}
```

The source generator removes `Get` and `Async` from the method name. For example, `GetProductsByBrandIdAsync` generates `ProductsByBrandIdDataLoader` and `IProductsByBrandIdDataLoader`.

The generated loader implements `IDataLoader<int, Product[]>`. The dictionary key is the parent key, and the array value is the child collection for that parent. This follows the group DataLoader pattern, though the signature is not the same as the manual `GroupedDataLoader<TKey, TValue>` API.

## Using the Loader in a Resolver

Inject the generated interface into your resolver and pass the request cancellation token to `LoadAsync`:

```csharp
using HotChocolate;
using HotChocolate.Types;

namespace Demo.Catalog;

[ObjectType<Brand>]
public static partial class BrandNode
{
    public static async Task<Product[]> GetProductsAsync(
        [Parent] Brand brand,
        IProductsByBrandIdDataLoader productsByBrandId,
        CancellationToken ct)
    {
        return await productsByBrandId.LoadAsync(brand.Id, ct) ?? [];
    }
}
```

Each parent resolver calls `LoadAsync(brand.Id, ct)`. Hot Chocolate collects these keys, dispatches the DataLoader batch, and resumes each resolver with the corresponding product array.

The `?? []` fallback is part of the field contract. It ensures a brand with no products returns an empty list and protects non-null list fields if a dictionary result omits a key.

Expected SDL excerpt:

```graphql
type Brand {
  id: Int!
  name: String!
  products: [Product!]!
}

type Product {
  id: Int!
  name: String!
}
```

Example response:

```json
{
  "data": {
    "brands": {
      "nodes": [
        {
          "id": 1,
          "name": "Contoso",
          "products": [
            { "id": 10, "name": "Trail Shoe" },
            { "id": 11, "name": "Rain Jacket" }
          ]
        },
        {
          "id": 2,
          "name": "Northwind",
          "products": []
        }
      ]
    }
  }
}
```

## Mapping Results Back to Parent Keys

The batch method does not return a flat child list to every resolver. Instead, it returns a keyed map of child collections.

```text
Requested brand IDs: [1, 2, 3]
Database rows:       Product A (BrandId 1), Product B (BrandId 1), Product C (BrandId 3)
Grouped result:      1 -> [A, B]
                     2 -> []
                     3 -> [C]
Resolver results:    brand 1 products [A, B], brand 2 products [], brand 3 products [C]
```

| Contract point                  | What to do                                                                                                                      |
| ------------------------------- | ------------------------------------------------------------------------------------------------------------------------------- |
| Batch by parent key             | Use the parent IDs from `IReadOnlyList<TKey>` in a single data-source query.                                                    |
| Group by the same key           | Group children by the foreign key that points to the parent.                                                                    |
| Align results to requested keys | Let `LoadAsync` return the value for the key each resolver requested. Multi-key `LoadAsync` results follow the input key order. |
| Handle missing keys             | Return `[]` from the resolver when the GraphQL field represents an empty child collection.                                      |
| Define child order              | Add `OrderBy` and a stable tie-breaker before building arrays or lookups.                                                       |

With the dictionary-array source-generated pattern, omitted keys can resolve as `null`. Keep `?? []` at the resolver boundary, or add empty arrays for every requested key if your code requires that guarantee inside the loader.

## Ordering Child Collections

DataLoader maps child groups to parent keys, but does not determine the order within each group.

Add ordering in the data query before building arrays:

```csharp
var products = await db.Products
    .Where(p => brandIds.Contains(p.BrandId))
    .OrderBy(p => p.Name)
    .ThenBy(p => p.Id)
    .ToListAsync(ct);
```

Use deterministic ordering when UI rendering, tests, or later paging decisions depend on stable child lists.

## Batching and Request Caching

DataLoader instances are scoped to a single GraphQL request.

- Duplicate parent keys within one request share the same pending load.
- Once a key resolves, the same key can be served from the request cache.
- Each new GraphQL request starts with a new DataLoader instance and cache.
- Complex execution paths may still produce more than one batch. DataLoader batches operate between resolver waves, not across all possible execution paths.

Keep data fetching inside the DataLoader method. The resolver should read the parent key and call `LoadAsync`.

```text
Wave 1:   brands resolver returns [Brand 1, Brand 2, Brand 3]
Wave 2:   each products resolver calls LoadAsync(brand.Id)
Dispatch: one query WHERE BrandId IN (1, 2, 3)
Return:   each brand receives its product array
```

## Passing Cancellation and Handling Errors

Accept a `CancellationToken` in the resolver and pass it to `LoadAsync`. Also accept a `CancellationToken` in the DataLoader method and pass it to EF Core, HTTP, or service calls.

If the batch method throws, the pending loads for that batch fail and are surfaced through standard GraphQL error handling. Design partial or per-item error behavior on a dedicated page, not in a child collection resolver by accident. For more on GraphQL error handling, see [Error handling](/docs/hotchocolate/v16/_leagcy/guides/error-handling).

## Registering Generated Loaders

Source-generated DataLoaders are registered by generated code when you call the generated type module extension for your project. Many projects use `AddTypes()`. If your generated extension has a project-specific name, call that method instead.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CatalogContext>();

builder
    .AddGraphQL()
    .AddTypes();
```

Do not register generated or manual DataLoaders with plain `AddScoped` or `AddTransient`. Use generated registration for source-generated loaders, and GreenDonut `AddDataLoader` registration methods for manual loaders.

Services such as `CatalogContext` can be DataLoader method parameters. The generated loader resolves them from dependency injection. If you need a dedicated service scope for the loader method, use `[DataLoader(ServiceScope = ...)]` and review [Service Injection](../resolvers/service-injection).

To specify a custom generated name, pass a name to `[DataLoader]`:

```csharp
[DataLoader("BrandProducts")]
public static async Task<Dictionary<int, Product[]>> GetProductsByBrandIdAsync(
    IReadOnlyList<int> brandIds,
    CatalogContext db,
    CancellationToken ct)
{
    var products = await db.Products
        .Where(p => brandIds.Contains(p.BrandId))
        .OrderBy(p => p.Name)
        .ThenBy(p => p.Id)
        .ToListAsync(ct);

    return products
        .GroupBy(p => p.BrandId)
        .ToDictionary(group => group.Key, group => group.ToArray());
}
```

This generates `IBrandProductsDataLoader`.

## Using `ILookup<TKey, TValue>` When Appropriate

`Dictionary<TKey, TValue[]>` is the recommended pattern for unpaged one-to-many fields. However, a source-generated method that returns `ILookup<TKey, TValue>` is also supported and is useful when your code naturally produces a lookup.

```csharp
using GreenDonut;
using Microsoft.EntityFrameworkCore;

namespace Demo.Catalog;

internal static class ProductDataLoaders
{
    [DataLoader]
    public static async Task<ILookup<int, Product>> GetProductsByBrandIdAsync(
        IReadOnlyList<int> brandIds,
        CatalogContext db,
        CancellationToken ct)
    {
        var products = await db.Products
            .Where(p => brandIds.Contains(p.BrandId))
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Id)
            .ToListAsync(ct);

        return products.ToLookup(p => p.BrandId);
    }
}
```

This generated loader still exposes `IDataLoader<int, Product[]>`. Missing keys map to empty arrays, as lookup indexing returns an empty sequence for keys with no elements.

## Maintaining a Manual `GroupedDataLoader`

Use manual `GroupedDataLoader<TKey, TValue>` classes for maintenance or advanced scenarios where source generation is not suitable. The manual base class returns `TValue[]` to callers and expects your implementation to provide an `ILookup<TKey, TValue>`.

```csharp
using GreenDonut;
using Microsoft.EntityFrameworkCore;

namespace Demo.Catalog;

public sealed class ProductsByBrandIdDataLoader(
    CatalogContext db,
    IBatchScheduler batchScheduler,
    DataLoaderOptions options)
    : GroupedDataLoader<int, Product>(batchScheduler, options)
{
    protected override async Task<ILookup<int, Product>> LoadGroupedBatchAsync(
        IReadOnlyList<int> keys,
        CancellationToken ct)
    {
        var products = await db.Products
            .Where(p => keys.Contains(p.BrandId))
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Id)
            .ToListAsync(ct);

        return products.ToLookup(p => p.BrandId);
    }
}
```

Register manual loaders using GreenDonut registration methods, for example:

```csharp
builder.Services.AddDataLoader<ProductsByBrandIdDataLoader>();
```

Missing keys become empty arrays through lookup indexing.

## Keeping Paged Child Collections Separate

Large child collections should typically be paged rather than returned as arrays. A paged one-to-many loader uses a paging-specific shape, such as `Dictionary<TKey, Page<TValue>>`, and helpers like `ToBatchPageAsync`.

Refer to the [Pagination](/docs/hotchocolate/v16/build/pagination) and connection documentation when the field returns a paged collection.

## Troubleshooting Group DataLoaders

### The Resolver Still Runs One SQL Query per Parent

- Move the database query into the DataLoader method.
- Ensure the resolver calls `LoadAsync` instead of querying by parent key directly.
- Verify the loader is registered through generated registration or GreenDonut registration methods.

### Some Parents Return `null` Instead of an Empty List

- Keep `?? []` at the resolver boundary when the GraphQL field should return an empty list for no children.
- With dictionary-array loaders, omitted keys can resolve as `null`.
- With generated `ILookup<TKey, TValue>` loaders or manual `GroupedDataLoader<TKey, TValue>` loaders, missing keys map to empty arrays.

### Child Items Appear in the Wrong Order

- Add explicit ordering before creating arrays or lookups.
- Include a stable tie-breaker such as `Id`.
- Use the same ordering rule in tests and UI expectations.

### The Generated Loader Name Is Not What You Expected

- The generator removes `Get` and `Async`.
- `GetProductsByBrandIdAsync` becomes `IProductsByBrandIdDataLoader`.
- Use `[DataLoader("...")]` for a custom name if it improves resolver injection.
- Inject the generated interface, not the static method class.

### The Loader Does Not Batch

- Avoid plain service registration such as `AddScoped<ProductsByBrandIdDataLoader>()`.
- Do not query children before calling `LoadAsync`.
- Do not store the rented `IReadOnlyList<TKey>` from the DataLoader method. Use it only during that method call.

### The Relationship Needs Paging

- Do not return a large `TValue[]` when clients require page boundaries.
- Use paging-specific loaders, `Page<T>`, and connection fields.
- Start with [Pagination](/docs/hotchocolate/v16/build/pagination).

## Next Steps

- [DataLoader overview](./index)
- [Create a source-generated DataLoader](./dataloader-attribute)
- [Load one entity by ID](./batch-dataloader)
- [Maintain a manual group DataLoader](#maintain-a-manual-groupeddataloader)
- [Use a batch resolver for one field](/docs/hotchocolate/v16/build/dataloader#batch-resolvers)
- [Access parent values in resolvers](../resolvers/parent-attribute)
- [Inject services into resolvers](../resolvers/service-injection)
- [Page collection fields](/docs/hotchocolate/v16/build/pagination)
- [Handle GraphQL errors](/docs/hotchocolate/v16/_leagcy/guides/error-handling)
