---
title: "DataLoader"
---

DataLoader is Hot Chocolate’s solution for batching and request-scoped caching of resolver data access. Use it when multiple resolvers need to fetch related data by key within the same GraphQL request.

This page will help you identify N+1 query patterns, understand how Hot Chocolate batches keys between resolver waves, and select the right DataLoader implementation. If you are new to resolvers, begin with [Resolvers](/docs/hotchocolate/v16/build/resolvers).

## Identifying N+1 Query Patterns

The N+1 problem often arises when a root resolver returns a list and a child resolver loads related data for each item in that list.

```graphql
type Query {
  products(first: Int): ProductsConnection
}

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

A client might request products and their brands in a single query:

```graphql
query GetProducts {
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

Without a DataLoader, the `brand` resolver could trigger a separate database query for each product:

```csharp
[ObjectType<Product>]
public static partial class ProductNode
{
    public static async Task<Brand?> GetBrandAsync(
        [Parent] Product product,
        CatalogContext db,
        CancellationToken ct)
    {
        return await db.Brands
            .FirstOrDefaultAsync(b => b.Id == product.BrandId, ct);
    }
}
```

The root field loads products once, but each product then requests its brand. For five products, this can result in five brand queries, and duplicate brand IDs may cause repeated work.

## How DataLoader Batches Keys

A DataLoader allows each resolver to request a key. Hot Chocolate collects these keys, dispatches them in batches, and reuses duplicate work within the same request.

```text
Wave 1:   Resolve products(first: 5)
          -> Product brand IDs: [1, 2, 1, 3, 2]

Wave 2:   Resolve Product.brand for each product
          -> brandById.LoadAsync(1)
          -> brandById.LoadAsync(2)
          -> brandById.LoadAsync(1)
          -> brandById.LoadAsync(3)
          -> brandById.LoadAsync(2)

Dispatch: DataLoader shares duplicate key work
          -> Fetch brand IDs: [1, 2, 3]

Resume:   Waiting Product.brand resolvers receive their Brand result
```

The DataLoader cache is scoped to a single GraphQL request. Duplicate `LoadAsync` calls for the same key share the same pending work and resolved value during that request. Each new request starts with a fresh cache.

Complex execution paths may still result in more than one batch. DataLoader reduces repeated per-object calls, but does not guarantee a single database call for the entire operation.

## Selecting the Right Optimization

DataLoader works alongside database-side query shaping. It cannot make an inefficient root query efficient. If your root resolver returns an unbounded list, loads unnecessary columns, or lacks proper indexing, address those issues at the data source.

| You need to                                      | Use                                                                                                                                           | Read next                                 |
| ------------------------------------------------ | --------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------- |
| Reduce rows in a collection                      | [Pagination](/docs/hotchocolate/v16/build/pagination) or [Filtering](/docs/hotchocolate/v16/build/filtering-sorting-projections/filter-types) | Shape the root query at the data source   |
| Order rows                                       | [Sorting](/docs/hotchocolate/v16/build/filtering-sorting-projections/sort-types)                                                              | Push ordering to the data source          |
| Reduce selected columns or selected subtrees     | [Projections](/docs/hotchocolate/v16/build/filtering-sorting-projections/projection-options) or `QueryContext<T>`                             | Project selected fields from `IQueryable` |
| Load related data for many parent objects by key | DataLoader                                                                                                                                    | Batch and cache key-based resolver loads  |
| Batch one field without a reusable key cache     | Field-local batching pattern                                                                                                                  | Batch a field-local computation           |

For more comprehensive data access strategies, see [Fetching from databases](/docs/hotchocolate/v16/_leagcy/resolvers-and-data/fetching-from-databases) and [Entity Framework](/docs/hotchocolate/v16/_leagcy/integrations/entity-framework).

## Getting Started with Source-Generated DataLoaders

For most v16 code, define a static method, annotate it with `[DataLoader]`, inject the generated interface into your resolvers, and call `LoadAsync`.

The source generator creates names based on your method. For example, `GetBrandByIdAsync` becomes `BrandByIdDataLoader` and `IBrandByIdDataLoader`, with `Get` and `Async` removed.

```csharp
internal static class BrandDataLoaders
{
    [DataLoader]
    public static async Task<Dictionary<int, Brand>> GetBrandByIdAsync(
        IReadOnlyList<int> ids,
        CatalogContext db,
        CancellationToken ct)
    {
        return await db.Brands
            .Where(b => ids.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, ct);
    }
}
```

```csharp
[ObjectType<Product>]
public static partial class ProductNode
{
    public static async Task<Brand?> GetBrandAsync(
        [Parent] Product product,
        IBrandByIdDataLoader brandById,
        CancellationToken ct)
    {
        return await brandById.LoadAsync(product.BrandId, ct);
    }
}
```

This approach produces a `brand` field that can be selected from `Product`:

```graphql
type Product {
  id: ID!
  name: String!
  brand: Brand
}
```

Registered services can be included as method parameters. In the example above, `CatalogContext db` is resolved from dependency injection, and the `CancellationToken` is passed to Entity Framework.

> The key list passed to a DataLoader method is rented. Do not store the `IReadOnlyList<TKey>` or use it after the method returns.

To learn more about the source-generator model, see [DataLoader attribute](./dataloader-attribute). For one-to-one key-value loading, see [Batch DataLoaders](./batch-dataloader).

## Loading One-to-Many Data by Key

Use a grouped DataLoader when a parent key maps to multiple child objects, such as products grouped by brand ID.

```graphql
query GetBrands {
  brands(first: 3) {
    nodes {
      name
      products {
        name
      }
    }
  }
}
```

```csharp
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
            .ToListAsync(ct);

        return products
            .GroupBy(p => p.BrandId)
            .ToDictionary(g => g.Key, g => g.ToArray());
    }
}
```

```csharp
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

If a parent has no children, return an empty collection from the GraphQL field. For more on return shapes and one-to-many loading, see [Grouped DataLoaders](./group-dataloader).

## Choosing a Loading Shape

| Your data-access task                                         | Start with                                      | Return shape                                                 | Read next                                        |
| ------------------------------------------------------------- | ----------------------------------------------- | ------------------------------------------------------------ | ------------------------------------------------ |
| Entity by ID                                                  | Source-generated batch DataLoader               | Dictionary keyed by ID                                       | [Batch DataLoaders](./batch-dataloader)          |
| Parent ID to child collection                                 | Source-generated grouped DataLoader             | Lookup or dictionary values with collections                 | [Grouped DataLoaders](./group-dataloader)        |
| Existing cache or single-key data source                      | Cache DataLoader or generated single-key loader | One value per key                                            | [Cache behavior](./cache-dataloader)             |
| Unsupported signature, custom scheduling, or advanced options | Manual DataLoader                               | `BatchDataLoader`, `GroupedDataLoader`, or `CacheDataLoader` | Use the focused DataLoader pages as API examples |
| One field, no reusable cache                                  | Field-local batching pattern                    | One result per parent                                        | Keep the batching logic close to the field       |

Details about `[DataLoader]` attributes, lookups, `DataLoaderGroup`, `DataLoaderState`, manual class options, cache settings, and advanced generator features are covered on the child pages.

## Planning Services, Lifetimes, and Cancellation

Use method parameters to inject registered services, and always pass the cancellation token to database, HTTP, or service calls.

Scoped services in query resolvers and DataLoaders use resolver or DataLoader scope by default. Mutations use the request scope. For generated loaders, `[DataLoader(ServiceScope = ...)]` supports `DataLoaderServiceScope.Default`, `DataLoaderServiceScope.DataLoaderScope`, and `DataLoaderServiceScope.OriginalScope`.

Review [Dependency Injection](/docs/hotchocolate/v16/build/resolvers/service-injection) and [DataLoader attribute](./dataloader-attribute) before changing service scope. Avoid sharing non-thread-safe services across concurrently executing query resolvers.

## Handling Errors, Nulls, and Missing Keys

Make nullability and error behavior explicit in your schema and resolver signatures.

| Situation                                               | What to expect                                                                      |
| ------------------------------------------------------- | ----------------------------------------------------------------------------------- |
| One-to-one DataLoader does not return a value for a key | Missing entries can resolve to `null`; align GraphQL nullability with that behavior |
| One-to-many DataLoader has no children for a key        | Return an empty collection from the field                                           |
| Batch fetch throws                                      | Awaiting loads for that batch observe the exception                                 |
| Batch resolver needs partial errors                     | Use a GraphQL error or a schema result shape that clients can handle intentionally  |

Error handling for each key depends on the loader shape. Refer to the focused child pages before designing partial-error behavior.

## Diagnosing Common Issues

| Symptom                                   | Check first                                                                                                              | More help                                                                                    |
| ----------------------------------------- | ------------------------------------------------------------------------------------------------------------------------ | -------------------------------------------------------------------------------------------- |
| You still see N+1 queries                 | Ensure child resolvers use the same DataLoader interface and await `LoadAsync` instead of querying the database directly | Review the loading-shape guidance above                                                      |
| Root query cost remains high              | Use projection, paging, filtering, or sorting at the data source                                                         | [Projections](/docs/hotchocolate/v16/build/filtering-sorting-projections/projection-options) |
| Child rows are missing                    | Make sure returned dictionary or lookup keys match the requested keys                                                    | [Grouped DataLoaders](./group-dataloader)                                                    |
| Null values surprise the schema           | Align GraphQL nullability with missing-key behavior and nullable result types                                            | Review the null and missing-key guidance above                                               |
| Scoped service or threading errors appear | Review `DataLoaderServiceScope` and service lifetimes                                                                    | [Dependency Injection](/docs/hotchocolate/v16/build/resolvers/service-injection)             |
| Work continues after a client disconnects | Pass `CancellationToken` through every data-source call                                                                  | [Resolvers](/docs/hotchocolate/v16/build/resolvers)                                          |
| Keys behave strangely after batching      | Do not store the rented key list                                                                                         | Keep key lists scoped to the DataLoader method call                                          |

## Next Steps

Select the page that matches your data-access scenario:

- [Create a source-generated DataLoader](./dataloader-attribute)
- [Load one entity by ID](./batch-dataloader)
- [Load one-to-many relationships](./group-dataloader)
- [Configure cache behavior](./cache-dataloader)
- Review the loading-shape, error, DI scope, and cancellation guidance on this page.
- [Review resolver fundamentals](/docs/hotchocolate/v16/build/resolvers)
- [Shape database queries with projections](/docs/hotchocolate/v16/build/filtering-sorting-projections/projection-options)
- [Page, filter, and sort collection fields](/docs/hotchocolate/v16/build/pagination)
