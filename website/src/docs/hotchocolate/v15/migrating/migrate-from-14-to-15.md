---
title: Migrate Hot Chocolate from 14 to 15
---

This guide will walk you through the manual migration steps to update your Hot Chocolate GraphQL server to version 15.

Start by installing the latest `15.x.x` version of **all** of the `HotChocolate.*` packages referenced by your project.

> This guide is still a work in progress with more updates to follow.

# Breaking changes

Things that have been removed or had a change in behavior that may cause your code not to compile or lead to unexpected behavior at runtime if not addressed.

## Supported target frameworks

Support for .NET Standard 2.0, .NET 6, and .NET 7 has been removed.

## F# support removed

`HotChocolate.Types.FSharp` has been replaced by the community project [FSharp.HotChocolate](https://www.nuget.org/packages/FSharp.HotChocolate).

## Runtime type changes

- The runtime type for `LocalDateType` and `DateType` has been changed from `DateTime` to `DateOnly`.
- The runtime type for `LocalTimeType` has been changed from `DateTime` to `TimeOnly`.

## DateTime serialized in universal time for the Date type

`DateTime`s are now serialized in universal time for the `Date` type.

For example, the `DateTime` `2018-06-11 02:46:14` in a time zone of `04:00` will now serialize as `2018-06-10` and not `2018-06-11`.

Use the `LocalDate` type if you do not want the date to be converted to universal time.

## LocalDate, LocalTime, and Date scalars enforce a specific format

- `LocalDate`: `yyyy-MM-dd`
- `LocalTime`: `HH:mm:ss`
- `Date`: `yyyy-MM-dd`

Please ensure that your clients are sending date/time strings in the correct format to avoid errors.

## LocalDate and LocalTime scalars moved

`LocalDate` and `LocalTime` have been moved from `HotChocolate.Types.Scalars` to `HotChocolate.Types`, and are therefore available without installing the additional package.

## DateOnly and TimeOnly binding change

- `DateOnly` is now bound to `LocalDateType` instead of `DateType`.
- `TimeOnly` is now bound to `LocalTimeType` instead of `TimeSpanType`.

## DataLoaderOptions are now required

Starting with Hot Chocolate 15, the `DataLoaderOptions` must be passed down to the DataLoaderBase constructor.

```csharp
public class ProductByIdDataLoader : BatchDataLoader<int, Product>
{
    private readonly IServiceProvider _services;

    public ProductDataLoader1(
        IBatchScheduler batchScheduler,
        DataLoaderOptions options) // the options are now required ...
        : base(batchScheduler, options)
    {
    }
}
```

## DataLoader Dependency Injection

DataLoader must not be manually registered with the dependency injection and must use the extension methods provided by GreenDonut.

```csharp
services.AddDataLoader<ProductByIdDataLoader>();
services.AddDataLoader<IProductByIdDataLoader, ProductByIdDataLoader>();
services.AddDataLoader<IProductByIdDataLoader>(sp => ....);
```

We recommend to use the source-generated DataLoaders and let the source generator write the registration code for you.

> If you register DataLoader manually they will be stuck in the auto-dispatch mode, which basically means that they will no longer batch.

DataLoader are available as scoped services and can be injected like any other scoped service.

```csharp
public class ProductService(IProductByIdDataLoader productByIdData)
{
    public async Task<Product> GetProductById(int id)
    {
        return await productByIdDataLoader.LoadAsync(id);
    }
}
```

# Deprecations

## GroupDataLoader

We no longer recommend using the `GroupDataLoader`, as the same functionality can be achieved with a BatchDataLoader, which provides greater flexibility in determining the type of list returned.

Use the following patter to replace the `GroupDataLoader`:

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
```

## AdHoc DataLoader

The ad-hoc DataLoader methods on IResolverContext have been deprecated.

```csharp
public async Task<Product?> GetProductById(int id, IResolverContext context, CatalogContext catalogContext)
{
    return context
        .BatchDataLoader<string, string>(
            (productIds, ct) => catalogContext.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct),
            "productById")
        .LoadAsync(id);
}
```

Use the source-generated DataLoaders instead.

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
```

This approach leads to better resolvers and helps avoid errors when capturing the context within the resolver.

```csharp
public async Task<Product?> GetProductById(int id, IProductDataLoader productDataLoader)
    => await productDataLoader.LoadAsync(id);
```

To pass state to the DataLoader, use the new branching and state APIs available on the DataLoader.
