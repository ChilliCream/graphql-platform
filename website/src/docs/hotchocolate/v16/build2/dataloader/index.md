---
title: "DataLoader"
---

DataLoader is Hot Chocolate's batching and request-cache layer for resolver data access. Use it when many resolvers need related data by key during the same GraphQL request.

This page helps you recognize N+1 fields, understand how Hot Chocolate batches keys between resolver waves, and choose the right implementation page. If resolver concepts are new to you, start with [Resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers).

## Recognize an N+1 field

N+1 often appears when a root resolver returns a list and a child resolver loads related data for each item.

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

A client can ask for products and their brands in one operation:

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

Without a DataLoader, the `brand` resolver can run one database query per product:

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

The root field loads products once. Then each product asks for its brand. Five products can produce five brand queries, and duplicate brand IDs can repeat the same work.

## Batch keys between resolver waves

A DataLoader lets each resolver ask for one key while Hot Chocolate collects those keys, dispatches them in batches, and reuses duplicate work within the request.

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

The DataLoader cache is scoped to one GraphQL request. Duplicate `LoadAsync` calls for the same key share the same pending work and resolved value during that request. A new request starts with a new cache.

Complex execution paths can still produce more than one batch. DataLoader reduces repeated per-object calls, but it does not promise one database call for an entire operation.

## Choose the right optimization

DataLoader complements database-side query shaping. It does not make an inefficient root query efficient. If your root resolver returns an unbounded list, loads unused columns, or misses a database index, fix that at the data source.

| You need to                                      | Use                                                                                                                                    | Read next                                 |
| ------------------------------------------------ | -------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------- |
| Reduce rows in a collection                      | [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination) or [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering) | Shape the root query at the data source   |
| Order rows                                       | [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting)                                                                           | Push ordering to the data source          |
| Reduce selected columns or selected subtrees     | [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections) or `QueryContext<T>`                                              | Project selected fields from `IQueryable` |
| Load related data for many parent objects by key | DataLoader                                                                                                                             | Batch and cache key-based resolver loads  |
| Batch one field without a reusable key cache     | [Batch resolver](./batch-resolvers)                                                                                                    | Batch a field-local computation           |

For broader data access guidance, see [Fetching from databases](/docs/hotchocolate/v16/resolvers-and-data/fetching-from-databases) and [Entity Framework](/docs/hotchocolate/v16/integrations/entity-framework).

## Start with source-generated DataLoaders

For most v16 code, write a static method, add `[DataLoader]`, inject the generated interface into resolvers, and call `LoadAsync`.

The source generator derives names from your method. `GetBrandByIdAsync` becomes `BrandByIdDataLoader` and `IBrandByIdDataLoader` because `Get` and `Async` are stripped.

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

This produces a `brand` field that can be selected from `Product`:

```graphql
type Product {
  id: ID!
  name: String!
  brand: Brand
}
```

Registered services can be method parameters. In the example, `CatalogContext db` is resolved from dependency injection, and the `CancellationToken` is passed through to Entity Framework.

> The key list passed to a DataLoader method is rented. Do not store `IReadOnlyList<TKey>` or use it after the method returns.

Learn the full source-generator model in [Source-generated DataLoaders](./source-generated), then use [Batch DataLoaders](./batch) for one key to one value.

## Load one-to-many data by key

Use a grouped DataLoader when one parent key maps to many child objects, such as products by brand ID.

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

Model parents without children as an empty collection in the GraphQL field. Use [Grouped DataLoaders](./group) for return-shape details and one-to-many guidance.

## Choose a loading shape

| Your data-access task                                         | Start with                                      | Return shape                                                 | Read next                            |
| ------------------------------------------------------------- | ----------------------------------------------- | ------------------------------------------------------------ | ------------------------------------ |
| Entity by ID                                                  | Source-generated batch DataLoader               | Dictionary keyed by ID                                       | [Batch DataLoaders](./batch)         |
| Parent ID to child collection                                 | Source-generated grouped DataLoader             | Lookup or dictionary values with collections                 | [Grouped DataLoaders](./group)       |
| Existing cache or single-key data source                      | Cache DataLoader or generated single-key loader | One value per key                                            | [Cache behavior](./cache)            |
| Unsupported signature, custom scheduling, or advanced options | Manual DataLoader                               | `BatchDataLoader`, `GroupedDataLoader`, or `CacheDataLoader` | [Manual DataLoaders](./manual)       |
| One field, no reusable cache                                  | Batch resolver                                  | One result per parent                                        | [Batch resolvers](./batch-resolvers) |

Keep detailed `[DataLoader]` attributes, `Lookups`, `DataLoaderGroup`, `DataLoaderState`, manual class options, cache settings, and advanced generator features on the child pages.

## Plan services, lifetimes, and cancellation

Use method parameters for registered services, and pass the cancellation token to every database, HTTP, or service call.

Scoped services in query resolvers and DataLoaders use resolver or DataLoader scope by default. Mutations use the request scope. For generated loaders, `[DataLoader(ServiceScope = ...)]` supports `DataLoaderServiceScope.Default`, `DataLoaderServiceScope.DataLoaderScope`, and `DataLoaderServiceScope.OriginalScope`.

Review [Dependency Injection](/docs/hotchocolate/v16/resolvers-and-data/dependency-injection) and [Source-generated DataLoaders](./source-generated) before you change service scope. Avoid sharing non-thread-safe services across concurrently executing query resolvers.

## Understand errors, nulls, and missing keys

Keep nullability and error behavior visible in your schema and resolver signatures.

| Situation                                               | What to expect                                                                      |
| ------------------------------------------------------- | ----------------------------------------------------------------------------------- |
| One-to-one DataLoader does not return a value for a key | Missing entries can resolve to `null`; align GraphQL nullability with that behavior |
| One-to-many DataLoader has no children for a key        | Return an empty collection from the field                                           |
| Batch fetch throws                                      | Awaiting loads for that batch observe the exception                                 |
| Batch resolver needs partial errors                     | Use `ResolverResult` on the batch resolver page                                     |

Per-key DataLoader error handling depends on the loader shape. Use the focused child pages before you design partial-error behavior.

## Diagnose common symptoms

| Symptom                                   | Check first                                                                                                               | More help                                                                              |
| ----------------------------------------- | ------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------- |
| You still see N+1 queries                 | Confirm child resolvers call the same DataLoader interface and await `LoadAsync` instead of calling the database directly | [Troubleshooting](./troubleshooting)                                                   |
| Root query cost remains high              | Use projection, paging, filtering, or sorting at the data source                                                          | [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections)                   |
| Child rows are missing                    | Ensure returned dictionary or lookup keys match requested keys                                                            | [Grouped DataLoaders](./group)                                                         |
| Null values surprise the schema           | Align GraphQL nullability with missing-key behavior and nullable result types                                             | [Troubleshooting](./troubleshooting)                                                   |
| Scoped service or threading errors appear | Review `DataLoaderServiceScope` and service lifetimes                                                                     | [Dependency Injection](/docs/hotchocolate/v16/resolvers-and-data/dependency-injection) |
| Work continues after a client disconnects | Pass `CancellationToken` through every data-source call                                                                   | [Resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers)                       |
| Keys behave strangely after batching      | Remove any storage of the rented key list                                                                                 | [Manual DataLoaders](./manual)                                                         |

## Choose your next page

Choose the page that matches the data-access problem you are solving.

- [Create a source-generated DataLoader](./source-generated)
- [Load one entity by ID](./batch)
- [Load one-to-many relationships](./group)
- [Configure cache behavior](./cache)
- [Write a manual DataLoader](./manual)
- [Use a batch resolver for one field](./batch-resolvers)
- [Troubleshoot N+1, nulls, errors, DI scope, and cancellation](./troubleshooting)
- [Review resolver fundamentals](/docs/hotchocolate/v16/resolvers-and-data/resolvers)
- [Shape database queries with projections](/docs/hotchocolate/v16/resolvers-and-data/projections)
- [Page, filter, and sort collection fields](/docs/hotchocolate/v16/resolvers-and-data/pagination)
