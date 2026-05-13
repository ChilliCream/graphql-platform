---
title: Projections
---

GraphQL clients specify which fields they need. Projections take advantage of this by translating the requested fields directly into optimized database queries. If a client requests only `name` and `email`, Hot Chocolate queries only those columns from the database.

```graphql
{
  users {
    email
    address {
      street
    }
  }
}
```

```sql
SELECT "u"."Email", "a"."Id" IS NOT NULL, "a"."Street"
FROM "Users" AS "u"
LEFT JOIN "Address" AS "a" ON "u"."AddressId" = "a"."Id"
```

In Hot Chocolate v16, `QueryContext<T>` is the recommended way to apply projections. It combines projection, filtering, and sorting into a single parameter that Hot Chocolate injects into your resolver automatically. You apply it to your `IQueryable` with the `.With()` extension method, giving you full control over your data pipeline.

# Getting Started

Projections are part of the `HotChocolate.Data` package.

<PackageInstallation packageName="HotChocolate.Data" />

Register filtering and sorting on the schema. This also registers `QueryContext<T>` support automatically:

```csharp
builder
    .AddGraphQL()
    .AddFiltering()
    .AddSorting();
```

Add a `QueryContext<T>` parameter to your resolver. Hot Chocolate constructs it at runtime from the GraphQL selection set, filter arguments, and sort arguments:

```csharp
[QueryType]
public static partial class ProductQueries
{
    [UseFiltering]
    [UseSorting]
    public static async Task<Page<Product>> GetProductsAsync(
        PagingArguments pagingArgs,
        QueryContext<Product> query,
        CatalogContext db,
        CancellationToken cancellationToken)
        => await db.Products
            .With(query)
            .ToPageAsync(pagingArgs, cancellationToken);
}
```

The `[UseFiltering]` and `[UseSorting]` attributes generate the `where` and `order` arguments in the schema. The `QueryContext<Product>` parameter receives the projection selector (from the GraphQL selection set), the filter predicate, and the sort definition. Calling `.With(query)` applies all three to the `IQueryable` in the correct order: filter, sort, then project.

# How QueryContext Works

`QueryContext<T>` is a simple record with three properties:

| Property    | Type                         | Source                                |
| ----------- | ---------------------------- | ------------------------------------- |
| `Selector`  | `Expression<Func<T, T>>?`    | Built from the GraphQL selection set  |
| `Predicate` | `Expression<Func<T, bool>>?` | Built from `[UseFiltering]` arguments |
| `Sorting`   | `SortDefinition<T>?`         | Built from `[UseSorting]` arguments   |

When you call `.With(queryContext)` on an `IQueryable<T>`, it applies these in order:

1. **Filter** the data with `Where(predicate)`
2. **Sort** the results with `OrderBy(sorting)`
3. **Project** only the requested columns with `Select(selector)`

This order is important for query efficiency. Filtering first reduces the dataset, sorting arranges the filtered results, and projecting last ensures only the needed columns are selected.

# Default Sort Order

When users don't provide explicit sort arguments, you often want a stable default order (for example, for pagination). The `.With()` method accepts an optional sort modifier:

```csharp
public async Task<Page<Product>> GetProductsAsync(
    PagingArguments pagingArgs,
    QueryContext<Product>? query = null,
    CancellationToken cancellationToken = default)
    => await context.Products
        .With(query, DefaultOrder)
        .ToPageAsync(pagingArgs, cancellationToken);

private static SortDefinition<Product> DefaultOrder(SortDefinition<Product> sort)
    => sort.IfEmpty(o => o.AddDescending(t => t.Name)).AddAscending(t => t.Id);
```

The `IfEmpty` method applies the default sort only when the client did not provide a sort argument. The `AddAscending(t => t.Id)` call always appends a tiebreaker to ensure stable cursor-based pagination.

# Using QueryContext with Services

A common pattern is to pass `QueryContext<T>` from your resolver into a service layer. This keeps your resolvers thin and your data access logic reusable:

```csharp
[QueryType]
public static partial class ProductQueries
{
    [UseConnection(IncludeTotalCount = true, EnableRelativeCursors = true)]
    [UseFiltering]
    [UseSorting]
    public static async Task<ProductConnection> GetProductsAsync(
        PagingArguments pagingArgs,
        QueryContext<Product> query,
        ProductService productService,
        CancellationToken cancellationToken)
    {
        var page = await productService.GetProductsAsync(
            pagingArgs, query, cancellationToken);
        return new ProductConnection(page);
    }
}
```

The service applies `QueryContext<T>` to the EF Core `DbSet`:

```csharp
public class ProductService(CatalogContext context)
{
    public async Task<Page<Product>> GetProductsAsync(
        PagingArguments pagingArgs,
        QueryContext<Product>? query = null,
        CancellationToken cancellationToken = default)
        => await context.Products
            .With(query, DefaultOrder)
            .ToPageAsync(pagingArgs, cancellationToken);

    private static SortDefinition<Product> DefaultOrder(
        SortDefinition<Product> sort)
        => sort
            .IfEmpty(o => o.AddDescending(t => t.Name))
            .AddAscending(t => t.Id);
}
```

Making the `QueryContext<T>` parameter nullable with a default of `null` allows you to call the service from places that don't have a GraphQL context, such as background jobs or unit tests.

# Using QueryContext with DataLoaders

`QueryContext<T>` integrates with GreenDonut DataLoaders to enable batched data fetching with projections. Use `.With(query)` on a DataLoader to branch it with the current query context:

```csharp
public class ProductService(
    CatalogContext context,
    IProductBatchingContext batchingContext)
{
    public async Task<Product?> GetProductByIdAsync(
        int id,
        QueryContext<Product>? query = null,
        CancellationToken cancellationToken = default)
        => await batchingContext.ProductById
            .With(query)
            .LoadAsync(id, cancellationToken);
}
```

The DataLoader itself receives `QueryContext<T>` and applies it to the batch query:

```csharp
[DataLoaderGroup("ProductBatchingContext")]
internal static class ProductDataLoader
{
    [DataLoader]
    public static async Task<Dictionary<int, Product>> GetProductByIdAsync(
        IReadOnlyList<int> ids,
        QueryContext<Product> query,
        CatalogContext context,
        CancellationToken cancellationToken)
    {
        ids = ids.EnsureOrdered();
        return await context.Products
            .Where(t => ids.Contains(t.Id))
            .With(query)
            .ToDictionaryAsync(t => t.Id, cancellationToken);
    }
}
```

For batched collection DataLoaders (for example, loading products by brand), you can combine `QueryContext<T>` with pagination:

```csharp
[DataLoader]
public static async Task<Dictionary<int, Page<Product>>>
    GetProductsByBrandAsync(
        IReadOnlyList<int> brandIds,
        PagingArguments pagingArgs,
        QueryContext<Product> query,
        CatalogContext context,
        CancellationToken cancellationToken)
{
    brandIds = brandIds.EnsureOrdered();
    return await context.Products
        .Where(t => brandIds.Contains(t.BrandId))
        .With(query, s => s.AddAscending(t => t.Id))
        .ToBatchPageAsync(
            t => t.BrandId,
            pagingArgs,
            cancellationToken);
}
```

# Nested Resolvers

In object type resolvers, `QueryContext<T>` is typed to the entity being resolved, not the parent. This means each resolver gets the projection, filter, and sort context for its own return type:

```csharp
[ObjectType<Product>]
public static partial class ProductNode
{
    [BindMember(nameof(Product.BrandId))]
    public static async Task<Brand?> GetBrandAsync(
        [Parent(requires: nameof(Product.BrandId))] Product product,
        QueryContext<Brand> query,
        BrandService brandService,
        CancellationToken cancellationToken)
        => await brandService.GetBrandByIdAsync(
            product.BrandId, query, cancellationToken);
}
```

For nested connection fields, combine `QueryContext<T>` with `[UseConnection]`, `[UseFiltering]`, and `[UseSorting]`:

```csharp
[ObjectType<Brand>]
public static partial class BrandNode
{
    [UseConnection(EnableRelativeCursors = true)]
    [UseFiltering]
    [UseSorting]
    public static async Task<PageConnection<Product>> GetProductsAsync(
        [Parent(requires: nameof(Brand.Id))] Brand brand,
        PagingArguments pagingArgs,
        QueryContext<Product> query,
        ProductService productService,
        CancellationToken cancellationToken)
    {
        var page = await productService.GetProductsByBrandAsync(
            brand.Id, pagingArgs, query, cancellationToken);
        return new PageConnection<Product>(page);
    }
}
```

# Including Additional Fields

Sometimes a DataLoader or batch resolver needs a field that the client didn't request (for example, the `Id` for dictionary keying). Use `.Include()` to ensure specific properties are always projected:

```csharp
[BatchResolver]
public static async Task<List<Supplier?>> GetSupplierAsync(
    [Parent(requires: nameof(Brand.SupplierId))] List<Brand> brands,
    QueryContext<Supplier> query,
    CatalogContext context,
    CancellationToken cancellationToken)
{
    var supplierIds = brands
        .Select(b => b.SupplierId)
        .Distinct()
        .ToList();

    var suppliers = await context.Suppliers
        .Where(s => supplierIds.Contains(s.Id))
        .With(query.Include(s => s.Id))
        .ToDictionaryAsync(s => s.Id, cancellationToken);

    return brands
        .Select(b => suppliers.GetValueOrDefault(b.SupplierId))
        .ToList();
}
```

The `.Include(s => s.Id)` call adds the `Id` property to the projection selector so it is always available for the dictionary key, even if the client did not request it.

# Migrating from UseProjection

If you are migrating from the `[UseProjection]` attribute approach, the key changes are:

**Before (attribute-based):**

```csharp
[QueryType]
public static partial class ProductQueries
{
    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Product> GetProducts(CatalogContext db)
        => db.Products;
}
```

**After (QueryContext):**

```csharp
[QueryType]
public static partial class ProductQueries
{
    [UseConnection]
    [UseFiltering]
    [UseSorting]
    public static async Task<ProductConnection> GetProductsAsync(
        PagingArguments pagingArgs,
        QueryContext<Product> query,
        CatalogContext db,
        CancellationToken cancellationToken)
    {
        var page = await db.Products
            .With(query)
            .ToPageAsync(pagingArgs, cancellationToken);
        return new ProductConnection(page);
    }
}
```

The main differences:

- **No `[UseProjection]` attribute.** `QueryContext<T>` handles projections via the `Selector` it receives from the GraphQL selection set.
- **Explicit data pipeline.** You control when and how filtering, sorting, and projection are applied to your query through the `.With()` call.
- **Service layer friendly.** You can pass `QueryContext<T>` into services and DataLoaders, making your data access logic reusable and testable.
- **No middleware ordering concerns.** With `[UseProjection]`, you had to maintain a strict attribute order (`UsePaging` > `UseProjection` > `UseFiltering` > `UseSorting`). With `QueryContext<T>`, the `.With()` method applies operations in the correct order automatically.

> Do not combine `QueryContext<T>` with `[UseProjection]` on the same field. Each applies its own `Select` expression, leading to unexpected behavior. The HC0099 analyzer warns when both are present.

# Next Steps

- **Need to filter results?** See [Filtering](/docs/hotchocolate/v16/fetching-data/filtering).
- **Need to sort results?** See [Sorting](/docs/hotchocolate/v16/fetching-data/sorting).
- **Need to page through results?** See [Pagination](/docs/hotchocolate/v16/fetching-data/pagination).
- **Need to integrate with Entity Framework?** See [Entity Framework Integration](/docs/hotchocolate/v16/fetching-data/integrations/entity-framework).
