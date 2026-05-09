---
title: "Group DataLoader"
---

# Load one-to-many relationships with a group DataLoader

Use a group DataLoader when a parent key maps to a child collection. Common examples are products by brand, reviews by product, and orders by user.

The goal is to let each field resolver ask for the children of its own parent while Hot Chocolate batches all requested parent keys into one data-source call:

```text
Batch by parent key -> query all children -> group by parent key -> return one collection per parent
```

For new Hot Chocolate v16 code, start with a source-generated `[DataLoader]` method that accepts `IReadOnlyList<TKey>` and returns a dictionary whose value is a collection, for example `Dictionary<int, Product[]>`. In source-generator terms, that return shape is an array-valued batch loader. In this page, group DataLoader means the one-to-many lookup pattern.

## When to use this pattern

| You need to                                                           | Use                                                                                                       |
| --------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------- |
| Load child collections by parent key, such as products for each brand | Group DataLoader pattern                                                                                  |
| Load one entity or value by key, such as brand by ID                  | [Batch DataLoader](./batch-dataloader)                                                                    |
| Batch one field without reusable keyed caching                        | [Batch resolver](/docs/hotchocolate/v16/resolvers-and-data/dataloader#batch-resolvers)                    |
| Return large or pageable child collections                            | [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination) with paging-specific DataLoader shapes |

Create one loader per lookup shape. `ProductsByBrandId` and `ReviewsByProductId` should be separate loaders because they batch different keys and return different child types.

## Recognize the child-collection N+1 problem

This query asks for several brands and each brand's products:

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

Avoid putting the product query directly in the child resolver. This code can run one product query per brand:

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

The brand list loads once. Then each `Brand.products` resolver runs its own child query. A group DataLoader keeps the resolver small and moves the batched data access into one reusable lookup.

## Create the source-generated loader

The examples assume an EF Core `CatalogContext` with `Brands` and `Products` sets. `Product.BrandId` is the foreign key back to `Brand.Id`.

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

The source generator strips `Get` and `Async` from the method name. `GetProductsByBrandIdAsync` generates `ProductsByBrandIdDataLoader` and `IProductsByBrandIdDataLoader`.

The generated loader has the shape `IDataLoader<int, Product[]>`. The dictionary key is the parent key. The array value is the child collection for that parent key. This is conceptually a group DataLoader pattern, but the exact `Dictionary<TKey, TValue[]>` signature is not the manual `GroupedDataLoader<TKey, TValue>` API.

## Use the loader from a resolver

Inject the generated interface into the resolver and pass the request cancellation token to `LoadAsync`.

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

Each parent resolver calls `LoadAsync(brand.Id, ct)`. Hot Chocolate collects those keys, dispatches the DataLoader batch, and resumes each resolver with the matching product array.

The `?? []` fallback is part of the field contract. It keeps a brand with no products as an empty list, and it protects non-null list fields when a dictionary result omits a key.

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

## Map results back to parent keys

The batch method does not return one flat child list to every resolver. It returns a keyed map of child collections.

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
| Batch by parent key             | Use the parent IDs from `IReadOnlyList<TKey>` in one data-source query.                                                         |
| Group by the same key           | Group children by the foreign key that points to the parent.                                                                    |
| Align results to requested keys | Let `LoadAsync` return the value for the key each resolver requested. Multi-key `LoadAsync` results follow the input key order. |
| Handle missing keys             | Return `[]` from the resolver when the GraphQL field represents an empty child collection.                                      |
| Define child order              | Add `OrderBy` and a stable tie-breaker before building arrays or lookups.                                                       |

With the dictionary-array source-generated pattern, omitted keys can resolve as `null`. Keep `?? []` at the resolver boundary or add empty arrays for every requested key when your code needs that guarantee inside the loader.

## Order child collections

DataLoader maps child groups to parent keys. It does not choose the business order inside each group.

Add ordering in the data query before you build arrays:

```csharp
var products = await db.Products
    .Where(p => brandIds.Contains(p.BrandId))
    .OrderBy(p => p.Name)
    .ThenBy(p => p.Id)
    .ToListAsync(ct);
```

Use deterministic ordering when UI rendering, tests, or later paging decisions depend on stable child lists.

## Understand batching and request caching

DataLoader instances are scoped to one GraphQL request.

- Duplicate parent keys within one request share the same pending load.
- After a key resolves, the same key can be served from the request cache.
- A new GraphQL request starts with a new DataLoader instance and a new cache.
- Complex execution paths can still produce more than one batch. DataLoader batches work between resolver waves, not across all possible execution paths.

Keep data fetching inside the DataLoader method. The resolver should read the parent key and call `LoadAsync`.

```text
Wave 1:   brands resolver returns [Brand 1, Brand 2, Brand 3]
Wave 2:   each products resolver calls LoadAsync(brand.Id)
Dispatch: one query WHERE BrandId IN (1, 2, 3)
Return:   each brand receives its product array
```

## Pass cancellation and handle errors

Accept a `CancellationToken` in the resolver and pass it to `LoadAsync`. Accept a `CancellationToken` in the DataLoader method and pass it to EF Core, HTTP, or service calls.

If the batch method throws, the pending loads for that batch fail and surface through normal GraphQL error handling. Design partial or per-item error behavior on a focused page, not in a child collection resolver by accident. For GraphQL error behavior, see [Error handling](/docs/hotchocolate/v16/guides/error-handling).

## Register generated loaders

Source-generated DataLoaders are registered by generated code when you call the generated type module extension for your project. Many projects use `AddTypes()`. If your generated extension has a project-specific name, call that method instead.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CatalogContext>();

builder
    .AddGraphQL()
    .AddTypes();
```

Do not register generated or manual DataLoaders with plain `AddScoped` or `AddTransient`. Use generated registration for source-generated loaders. Use GreenDonut `AddDataLoader` registration methods for manual loaders.

Services such as `CatalogContext` can be DataLoader method parameters. The generated loader resolves them from dependency injection. If you need a dedicated service scope for the loader method, use `[DataLoader(ServiceScope = ...)]` and review [Service Injection](../resolvers/service-injection).

Pass a name to `[DataLoader]` when you need a custom generated name:

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

## Use `ILookup<TKey, TValue>` when it fits

`Dictionary<TKey, TValue[]>` is the recommended v16 documentation pattern for unpaged one-to-many fields. A source-generated method that returns `ILookup<TKey, TValue>` is also supported and is useful when your code naturally produces a lookup.

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

This generated loader still exposes `IDataLoader<int, Product[]>`. Missing keys map to empty arrays because lookup indexing returns an empty sequence for keys with no elements.

## Maintain a manual `GroupedDataLoader`

Use manual `GroupedDataLoader<TKey, TValue>` classes for maintenance or advanced cases where source generation does not fit. The manual base class returns `TValue[]` to callers and asks your implementation for an `ILookup<TKey, TValue>`.

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

Register manual loaders with GreenDonut registration methods, for example:

```csharp
builder.Services.AddDataLoader<ProductsByBrandIdDataLoader>();
```

Missing keys become empty arrays through lookup indexing.

## Keep paged child collections separate

Large child collections should usually be paged rather than returned as arrays. A paged one-to-many loader uses a paging-specific shape, for example `Dictionary<TKey, Page<TValue>>`, and helpers such as `ToBatchPageAsync`.

Use the [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination) and connection docs when the field returns a paged collection.

## Troubleshoot group DataLoaders

### The resolver still runs one SQL query per parent

- Move the database query into the DataLoader method.
- Make the resolver call `LoadAsync` instead of querying by parent key directly.
- Verify the loader is registered through generated registration or GreenDonut registration methods.

### Some parents return `null` instead of an empty list

- Keep `?? []` at the resolver boundary when the GraphQL field should return an empty list for no children.
- With dictionary-array loaders, omitted keys can resolve as `null`.
- With generated `ILookup<TKey, TValue>` loaders or manual `GroupedDataLoader<TKey, TValue>` loaders, missing keys map to empty arrays.

### Child items appear in the wrong order

- Add explicit ordering before creating arrays or lookups.
- Include a stable tie-breaker such as `Id`.
- Keep the same ordering rule in tests and UI expectations.

### The generated loader name is not what you expected

- Remember that the generator strips `Get` and `Async`.
- `GetProductsByBrandIdAsync` becomes `IProductsByBrandIdDataLoader`.
- Use `[DataLoader("...")]` when a custom name improves resolver injection.
- Inject the generated interface, not the static method class.

### The loader does not batch

- Avoid plain service registration such as `AddScoped<ProductsByBrandIdDataLoader>()`.
- Do not query children before calling `LoadAsync`.
- Do not store the rented `IReadOnlyList<TKey>` from the DataLoader method. Use it only during that method call.

### The relationship needs paging

- Do not return a large `TValue[]` when clients need page boundaries.
- Use paging-specific loaders, `Page<T>`, and connection fields.
- Start with [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination).

## Go next

- [DataLoader overview](./index)
- [Create a source-generated DataLoader](./dataloader-attribute)
- [Load one entity by ID](./batch-dataloader)
- [Maintain a manual group DataLoader](#maintain-a-manual-groupeddataloader)
- [Use a batch resolver for one field](/docs/hotchocolate/v16/resolvers-and-data/dataloader#batch-resolvers)
- [Access parent values in resolvers](../resolvers/parent-attribute)
- [Inject services into resolvers](../resolvers/service-injection)
- [Page collection fields](/docs/hotchocolate/v16/resolvers-and-data/pagination)
- [Handle GraphQL errors](/docs/hotchocolate/v16/guides/error-handling)
