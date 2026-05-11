---
title: "DataLoader attribute"
---

# DataLoader Attribute

Nested GraphQL fields often introduce the N+1 data-access problem: a parent resolver loads a list, and then a child resolver performs a separate lookup for each item. Hot Chocolate addresses this with the `[DataLoader]` attribute. By annotating a static loading method, you enable the GreenDonut source generator to create the DataLoader type, its interface, and registration hooks automatically.

Refer to this page when you want to generate DataLoaders from source. For manual [batch](./batch-dataloader), [group](./group-dataloader), and [cache](./cache-dataloader) DataLoader classes, see their dedicated pages.

## Start from the resolver you want to write

Suppose you have a `Product` entity that stores a foreign key to a `Brand`:

```csharp
public sealed record Product(int Id, string Name, int BrandId);

public sealed record Brand(int Id, string Name);
```

In your resolver, depend on the generated DataLoader interface and call `LoadAsync` with the key from the parent value:

```csharp
using HotChocolate;
using HotChocolate.Types;

[ObjectType<Product>]
public static partial class ProductNode
{
    public static async Task<Brand?> GetBrandAsync(
        [Parent] Product product,
        IBrandByIdDataLoader brandById,
        CancellationToken cancellationToken)
    {
        return await brandById.LoadAsync(product.BrandId, cancellationToken);
    }
}
```

The `IBrandByIdDataLoader` interface is generated for you. You do not need to write it manually. The resulting schema can expose the relationship as a standard field:

```graphql
type Product {
  id: Int!
  name: String!
  brand: Brand
}

type Brand {
  id: Int!
  name: String!
}
```

## Create a one-to-one DataLoader

To define a one-to-one DataLoader, add `using GreenDonut;`, place a static method in a static class, and annotate the method with `[DataLoader]`. For this pattern, the first parameter is `IReadOnlyList<TKey>`, and the method returns a dictionary keyed by the same values passed to `LoadAsync`.

```csharp
using GreenDonut;
using Microsoft.EntityFrameworkCore;

internal static class BrandDataLoaders
{
    [DataLoader]
    public static async Task<IReadOnlyDictionary<int, Brand>> GetBrandByIdAsync(
        IReadOnlyList<int> ids,
        CatalogContext db,
        CancellationToken cancellationToken)
    {
        return await db.Brands
            .Where(brand => ids.Contains(brand.Id))
            .ToDictionaryAsync(brand => brand.Id, cancellationToken);
    }
}
```

The generator creates `BrandByIdDataLoader` and, by default, `IBrandByIdDataLoader`. The name is derived from `GetBrandByIdAsync` by removing `Get` and `Async`, then appending `DataLoader`.

The returned dictionary determines how results are mapped:

| Requested key       | Dictionary entry | Resolver result                                                |
| ------------------- | ---------------- | -------------------------------------------------------------- |
| Key is present      | Matching value   | That value                                                     |
| Key is missing      | No entry         | The default value, often `null` for nullable reference results |
| Batch method throws | Exception        | Awaiting loads for that batch fail                             |

> The `IReadOnlyList<TKey>` passed to a generated DataLoader method is rented for the fetch. Do not store or use it after the method returns.

## Batching, deduplication, and request caching

Multiple resolvers can call the same generated DataLoader within a single request. Hot Chocolate collects the keys, dispatches a batch at the appropriate execution point, removes duplicate keys, and caches resolved values for the duration of the request.

```text
Product.brand resolvers call LoadAsync(brandId)
        |
        v
DataLoader queues keys during the execution wave
        |
        v
Generated loader calls GetBrandByIdAsync once with IReadOnlyList<int>
        |
        v
Results are mapped by key and cached for the request
```

For instance, if keys `[1, 2, 1, 3, 2]` are requested, the DataLoader fetches `[1, 2, 3]`. Any subsequent `LoadAsync(1)` calls in the same GraphQL request reuse the cached result. Each new GraphQL request starts with a fresh DataLoader cache.

Complex queries may still result in multiple batches. DataLoader coordinates repeated key lookups, but does not replace pagination, filtering, projections, indexes, or other data-source optimizations.

## Create a one-to-many DataLoader

When one key maps to multiple values, use `ILookup<TKey, TValue>`. The generated DataLoader returns an array for each key, and provides an empty array if there are no entries for a requested key.

```csharp
using GreenDonut;
using Microsoft.EntityFrameworkCore;

internal static class ProductDataLoaders
{
    [DataLoader]
    public static async Task<ILookup<int, Product>> GetProductsByBrandIdAsync(
        IReadOnlyList<int> brandIds,
        CatalogContext db,
        CancellationToken cancellationToken)
    {
        var products = await db.Products
            .Where(product => brandIds.Contains(product.BrandId))
            .OrderBy(product => product.Name)
            .ToListAsync(cancellationToken);

        return products.ToLookup(product => product.BrandId);
    }
}
```

The generated interface is `IProductsByBrandIdDataLoader`. Calling `LoadAsync(brand.Id, cancellationToken)` returns a `Product[]`:

```csharp
using HotChocolate;
using HotChocolate.Types;

[ObjectType<Brand>]
public static partial class BrandNode
{
    public static async Task<Product[]> GetProductsAsync(
        [Parent] Brand brand,
        IProductsByBrandIdDataLoader productsByBrandId,
        CancellationToken cancellationToken)
    {
        return await productsByBrandId.LoadAsync(brand.Id, cancellationToken);
    }
}
```

A dictionary with a collection as its value is also a supported batch-loader shape:

```csharp
public static Task<IReadOnlyDictionary<int, Product[]>> GetProductListByBrandIdAsync(
    IReadOnlyList<int> brandIds,
    CatalogContext db,
    CancellationToken cancellationToken)
{
    // Fetch products and return a dictionary keyed by BrandId.
}
```

This is a batch DataLoader with a collection value. The formal group-loader shape is `ILookup<TKey, TValue>`.

## Create a scalar cache loader

When the first parameter is a scalar key and the method returns a single value, the generator creates a cache DataLoader. This is useful when the backing source already provides a one-key API, but you want request-scoped deduplication and the same `LoadAsync` pattern.

```csharp
using GreenDonut;

internal static class UserDataLoaders
{
    [DataLoader]
    public static Task<string?> GetDisplayNameByUserIdAsync(
        int userId,
        UserDirectory users,
        CancellationToken cancellationToken)
    {
        return users.GetDisplayNameAsync(userId, cancellationToken);
    }
}
```

The generated interface is `IDisplayNameByUserIdDataLoader`. Multiple loads for the same user ID within a single GraphQL request share the cached task. Prefer the `IReadOnlyList<TKey>` batch shape if your backing store can fetch many keys in one call.

## Classify DataLoader method parameters

The first parameter determines the key shape. All parameters after the key or keys are classified by the generator as follows:

| Parameter shape                     | Bound from                         | Use for                                                                     |
| ----------------------------------- | ---------------------------------- | --------------------------------------------------------------------------- |
| `CancellationToken`                 | Fetch cancellation                 | Pass cancellation to database, HTTP, or service calls.                      |
| `GreenDonut.Data.ISelectorBuilder`  | `GreenDonut.Data.Selector` state   | Advanced selection-aware data integration.                                  |
| `GreenDonut.Data.IPredicateBuilder` | `GreenDonut.Data.Predicate` state  | Advanced filtering-aware data integration.                                  |
| `GreenDonut.Data.PagingArguments`   | `GreenDonut.Data.PagingArgs` state | Paging-aware DataLoader APIs.                                               |
| `GreenDonut.Data.SortDefinition<T>` | `GreenDonut.Data.Sorting` state    | Sorting-aware data integration.                                             |
| `GreenDonut.Data.QueryContext<T>`   | DataLoader query context           | Advanced query pipeline integration.                                        |
| `[DataLoaderState("key")] T value`  | Custom DataLoader state            | Tenant IDs, locale values, or other state supplied through GreenDonut APIs. |
| Any other registered service type   | Dependency injection               | Repositories, EF Core contexts, API clients, and domain services.           |

`[DataLoaderState]` supports required, nullable, and defaulted parameters:

```csharp
using GreenDonut;

internal static class TenantProductDataLoaders
{
    [DataLoader]
    public static async Task<IReadOnlyDictionary<int, TenantProduct>> GetProductByIdAsync(
        IReadOnlyList<int> ids,
        [DataLoaderState("tenantId")] string tenantId,
        TenantCatalogContext db,
        CancellationToken cancellationToken)
    {
        return await db.TenantProducts
            .Where(product => product.TenantId == tenantId)
            .Where(product => ids.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id, cancellationToken);
    }
}
```

DataLoader method services use dependency injection, similar to resolver services. Scoped services in query resolvers and DataLoaders use resolver or DataLoader scope by default. Review service lifetimes before changing the generated scope, especially for non-thread-safe services such as EF Core contexts.

## Configure generated names, access, and service scope

By default, generated names remove common method words:

| Method name                   | Generated class           | Generated interface        |
| ----------------------------- | ------------------------- | -------------------------- |
| `GetBrandByIdAsync`           | `BrandByIdDataLoader`     | `IBrandByIdDataLoader`     |
| `LoadBrandByIdAsync`          | `LoadBrandByIdDataLoader` | `ILoadBrandByIdDataLoader` |
| `GetBrandByIdDataLoaderAsync` | `BrandByIdDataLoader`     | `IBrandByIdDataLoader`     |

If you want a stable generated name that does not follow the method name, use the attribute constructor:

```csharp
using GreenDonut;

internal static class BrandDataLoaders
{
    [DataLoader("BrandById")]
    public static Task<IReadOnlyDictionary<int, Brand>> LoadBrandsAsync(
        IReadOnlyList<int> ids,
        CatalogContext db,
        CancellationToken cancellationToken)
    {
        return db.GetBrandsByIdAsync(ids, cancellationToken);
    }
}
```

Method-level options:

| Option             | Values                                             | Default                       | Use when                                                                     |
| ------------------ | -------------------------------------------------- | ----------------------------- | ---------------------------------------------------------------------------- |
| Constructor `name` | Any valid generated type name stem                 | Derived from method name      | You want `BrandByIdDataLoader` from a method named `LoadBrandsAsync`.        |
| `ServiceScope`     | `Default`, `DataLoaderScope`, `OriginalScope`      | Assembly or generator default | You need to choose the service provider scope used for service parameters.   |
| `AccessModifier`   | `Default`, `Public`, `PublicInterface`, `Internal` | Assembly or generator default | Generated types must cross assembly or package boundaries, or stay internal. |
| `Lookups`          | String array of method names                       | Empty                         | You need advanced alternate cache lookup support.                            |

```csharp
using GreenDonut;

internal static class BrandDataLoaders
{
    [DataLoader(
        "BrandById",
        ServiceScope = DataLoaderServiceScope.DataLoaderScope,
        AccessModifier = DataLoaderAccessModifier.PublicInterface)]
    public static Task<IReadOnlyDictionary<int, Brand>> LoadBrandsAsync(
        IReadOnlyList<int> ids,
        CatalogContext db,
        CancellationToken cancellationToken)
    {
        return db.GetBrandsByIdAsync(ids, cancellationToken);
    }
}
```

`DataLoaderAccessModifier` values:

| Value             | Generated class               | Generated interface           |
| ----------------- | ----------------------------- | ----------------------------- |
| `Default`         | Generator or assembly default | Generator or assembly default |
| `Public`          | Public                        | Public                        |
| `PublicInterface` | Internal                      | Public                        |
| `Internal`        | Internal                      | Internal                      |

`DataLoaderServiceScope` values:

| Value             | Behavior                                                                               |
| ----------------- | -------------------------------------------------------------------------------------- |
| `Default`         | Uses the configured/default DataLoader service-scope behavior.                         |
| `DataLoaderScope` | Resolves service parameters from a DataLoader-specific scope.                          |
| `OriginalScope`   | Resolves service parameters from the original service provider passed into the loader. |

## Set assembly defaults

To apply the same generator settings to all generated DataLoaders in a project, use `[assembly: DataLoaderDefaults]`:

```csharp
using GreenDonut;

[assembly: DataLoaderDefaults(
    ServiceScope = DataLoaderServiceScope.DataLoaderScope,
    AccessModifier = DataLoaderAccessModifier.PublicInterface,
    GenerateInterfaces = true,
    GenerateRegistrationCode = true)]
```

| Setting                    | Default   | Effect                                                                                                                                          |
| -------------------------- | --------- | ----------------------------------------------------------------------------------------------------------------------------------------------- |
| `ServiceScope`             | `Default` | Sets the default service scope for generated DataLoader method services.                                                                        |
| `AccessModifier`           | `Default` | Sets the default visibility for generated classes and interfaces.                                                                               |
| `GenerateInterfaces`       | `true`    | Generates `I{Name}DataLoader` interfaces when enabled. Disable it to inject the generated class directly.                                       |
| `GenerateRegistrationCode` | `true`    | Keeps generated registration helpers enabled. If you turn it off, register loaders through another supported path and inspect generated output. |

Method-level `ServiceScope` and `AccessModifier` override the assembly defaults for that method.

## Register generated DataLoaders

The source generator can emit registration code, but your application must call the generated registration extension. There are two common approaches:

### Register through the Hot Chocolate type module

If your GraphQL project uses Hot Chocolate source-generated type modules, the generated `IRequestExecutorBuilder` extension registers both generated GraphQL types and DataLoaders.

```csharp
using HotChocolate;

[assembly: Module("CatalogTypes")]
```

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<CatalogContext>();

builder
    .AddGraphQL()
    .AddCatalogTypes(); // Generated for a module named CatalogTypes.
```

The exact extension name depends on your assembly name or module attribute. Inspect the generated output to confirm the method name before documenting it in shared templates.

### Register through a DataLoader-only module

Use `[assembly: DataLoaderModule("Catalog")]` to generate an `IServiceCollection` registration extension for DataLoaders, without relying on the type module path.

```csharp
using GreenDonut;

[assembly: DataLoaderModule("Catalog")]
```

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<CatalogContext>();
builder.Services.AddCatalog();

builder
    .AddGraphQL()
    .AddTypes();
```

For `DataLoaderModule("Catalog")`, the generated service extension is named `AddCatalog`. Set `IsInternal = true` if the generated extension class should be internal:

```csharp
using GreenDonut;

[assembly: DataLoaderModule("Catalog", IsInternal = true)]
```

When interfaces are generated, registration maps `I{Name}DataLoader` to `{Name}DataLoader`. If `GenerateInterfaces = false`, registration uses the generated class.

## Inspect generated code

If a type name, interface, or registration method is unclear, inspect the generated code. Most IDEs display source-generated files under dependencies, analyzers, or generated files.

Look for these files:

| Generated file kind                 | What to confirm                                                       |
| ----------------------------------- | --------------------------------------------------------------------- |
| `GreenDonutDataLoader.*.g.cs`       | Generated class name, interface name, loader kind, and method call.   |
| `GreenDonutDataLoaderModule.*.g.cs` | `IServiceCollection` extension from `[DataLoaderModule]`.             |
| `HotChocolateTypeModule.*.g.cs`     | `IRequestExecutorBuilder` extension used by type module registration. |

Generated files are build output. Do not edit them. Change the `[DataLoader]` method, assembly attributes, or registration call instead.

## Supported method shapes

| Goal                          | First parameter       | Return shape                                                                                                            | Generated behavior                               |
| ----------------------------- | --------------------- | ----------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------ |
| One key to one value          | `IReadOnlyList<TKey>` | `Task<IReadOnlyDictionary<TKey, TValue>>`, `Task<IDictionary<TKey, TValue>>`, or a compatible dictionary implementation | Batch DataLoader                                 |
| One key to many values        | `IReadOnlyList<TKey>` | `Task<ILookup<TKey, TValue>>`                                                                                           | Group DataLoader that returns `TValue[]` per key |
| One key to a collection value | `IReadOnlyList<TKey>` | Dictionary where `TValue` is a collection, such as `Product[]`                                                          | Batch DataLoader with a collection value         |
| Scalar key to scalar value    | `TKey`                | `Task<TValue>`                                                                                                          | Cache DataLoader                                 |

Method constraints:

- The method must have at least one parameter.
- The method must be static.
- The method must be public, internal, or protected internal.
- The method cannot be generic.
- The first parameter determines the key shape.
- Batch and group signatures should use `IReadOnlyList<TKey>` for keys.
- Do not store rented key lists after the method returns.

## Choosing between generated loaders, batch resolvers, and manual loaders

| Use                             | When it fits                                                                                                   | Where to go next                                                                                                            |
| ------------------------------- | -------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------- |
| Source-generated `[DataLoader]` | The lookup is key-based, reusable, and fits the supported method shapes.                                       | This page                                                                                                                   |
| Batch resolver                  | The batched work belongs to one field and does not need reusable request cache entries by key.                 | [Current DataLoader guide](/docs/hotchocolate/v16/build/dataloader#batch-resolvers)                                         |
| Manual DataLoader               | You need custom class-based scheduling, caching, error mapping, constructor logic, or migration compatibility. | [Batch DataLoader](./batch-dataloader), [Group DataLoader](./group-dataloader), and [Cache DataLoaders](./cache-dataloader) |

Prefer generated loaders for standard keyed data access. Use a field-local batch resolver or manual loader if your scenario does not fit the generated method shapes.

## Troubleshoot generated DataLoaders

| Symptom                                              | Likely cause                                                                                                                                                | Fix                                                                                                                                           |
| ---------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------- |
| `IBrandByIdDataLoader` cannot be found               | The generated name is different, interface generation is disabled, the source generator did not run, or the namespace is missing.                           | Check generated files, naming rules, `[DataLoader("...")]`, `GenerateInterfaces`, package/analyzer references, and `using` directives.        |
| The generated class exists but DI cannot resolve it  | The generated registration extension was not called, registration generation was disabled, or the wrong registration path was used.                         | Call the generated Hot Chocolate type module extension or the generated DataLoader module extension. Check `[DataLoaderDefaults]`.            |
| No loader is generated for the method                | The method has no key parameter, invalid visibility, a generic type parameter, unsupported return shape, missing `GreenDonut` reference, or compile errors. | Use a static public, internal, or protected internal non-generic method with a supported signature. Review analyzer diagnostics.              |
| The generated name is unexpected                     | `Get`, trailing `Async`, and trailing `DataLoader` are stripped, or a name override was not used.                                                           | Inspect generated files and use `[DataLoader("ExpectedName")]` when the name is part of your public API.                                      |
| Generated code fails around an instance method       | The generated class calls the method statically.                                                                                                            | Make the `[DataLoader]` method static.                                                                                                        |
| Scoped service errors appear                         | The selected service scope does not match the service lifetime or thread-safety needs.                                                                      | Review `DataLoaderServiceScope` and Hot Chocolate dependency injection defaults. Use `DataLoaderScope` when that matches your service design. |
| One-to-many loads return missing or null collections | A dictionary collection shape has no entry for the key, or the resolver expected `ILookup` empty-array behavior.                                            | Prefer `ILookup<TKey, TValue>` for group-loader empty arrays, or handle missing dictionary entries in the field resolver.                     |
| Keys behave strangely after batching                 | The method stores the rented key list or uses it after the fetch returns.                                                                                   | Copy keys into your own collection if you need them beyond the method call.                                                                   |
| You cannot find the generated registration method    | The project uses a different type module name, a DataLoader module was not declared, or registration code generation was disabled.                          | Inspect `HotChocolateTypeModule.*.g.cs` or `GreenDonutDataLoaderModule.*.g.cs`, then call the generated method that exists in your project.   |

## Advanced hooks

The `Lookups` option adds alternate cache lookup methods for advanced entity-cache scenarios. These methods reside on the containing type and map loaded values to alternate keys or key-value transforms. Use this when a loaded entity should populate more than one cache key.

`DataLoaderGroup` allows you to group multiple generated DataLoaders into injectable context classes. You can place the attribute on a DataLoader method or the containing class. This is helpful when a service needs a cohesive set of generated loaders rather than many constructor parameters.

GreenDonut data parameters, such as selectors, predicates, paging arguments, sort definitions, and query contexts, are integration hooks. Treat them as advanced state passed through DataLoader APIs, not as general GraphQL resolver arguments.

## Next steps

- Review [DataLoader concepts](./index) for N+1 detection, execution waves, request caching, and optimization strategies.
- Use [Batch DataLoader](./batch-dataloader) for manual one-to-one loader classes.
- Use [Group DataLoader](./group-dataloader) for one-to-many relationship patterns and manual group loaders.
- Use [Cache DataLoaders](./cache-dataloader) for manual or generated one-key cache loaders.
- Review [Service Injection](../resolvers/service-injection) before changing DataLoader service scopes.
- Review [Resolver Signatures](../resolvers/resolver-signature) and [Parent access](../resolvers/parent-attribute) for resolver parameter binding.
- Refer to the [current DataLoader guide](/docs/hotchocolate/v16/build/dataloader) for grouped loaders and batch resolvers while the build child pages are being completed.
