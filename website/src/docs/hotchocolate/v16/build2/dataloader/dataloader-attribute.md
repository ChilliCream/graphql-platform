---
title: "DataLoader attribute"
---

Nested GraphQL fields can create an N+1 data-access pattern: one resolver loads a list, then a child resolver performs one lookup for every item in that list. In Hot Chocolate v16, the `[DataLoader]` attribute lets you write a static loading method and let the GreenDonut source generator create the DataLoader type, generated interface, and registration hooks.

Use this page when you want source-generated DataLoaders. Manual [batch](./batch-dataloader), [group](./group-dataloader), and [cache](./cache-dataloader) classes are covered on the focused DataLoader pages.

## Start from the resolver you want to write

Assume `Product` stores a foreign key to `Brand`:

```csharp
public sealed record Product(int Id, string Name, int BrandId);

public sealed record Brand(int Id, string Name);
```

Your resolver should depend on a generated DataLoader interface and call `LoadAsync` with the key from the parent value:

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

`IBrandByIdDataLoader` is generated. You do not write that interface by hand. The resulting schema can expose the relationship as a normal field:

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

Add `using GreenDonut;`, place a static method in a static class, and annotate the method with `[DataLoader]`. For a one-to-one lookup, the first parameter is `IReadOnlyList<TKey>` and the method returns a dictionary keyed by the same values passed to `LoadAsync`.

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

The generator creates `BrandByIdDataLoader` and, by default, `IBrandByIdDataLoader`. The name comes from `GetBrandByIdAsync`: `Get` and `Async` are removed, then `DataLoader` is appended.

The returned dictionary controls result mapping:

| Requested key       | Dictionary entry | Resolver result                                                |
| ------------------- | ---------------- | -------------------------------------------------------------- |
| Key is present      | Matching value   | That value                                                     |
| Key is missing      | No entry         | The default value, often `null` for nullable reference results |
| Batch method throws | Exception        | Awaiting loads for that batch fail                             |

> The `IReadOnlyList<TKey>` passed to a generated DataLoader method is rented for the fetch. Do not store it or use it after the method returns.

## Understand batching, deduplication, and request caching

Many resolvers can call the same generated DataLoader during one request. Hot Chocolate queues the keys, dispatches a batch at the right execution point, removes duplicate keys, and caches resolved values for the request.

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

For example, keys `[1, 2, 1, 3, 2]` can be fetched as `[1, 2, 3]`. Later `LoadAsync(1)` calls in the same GraphQL request reuse the cached result for `BrandByIdDataLoader`. A new GraphQL request starts with a new DataLoader cache.

Complex operations can still produce more than one batch. DataLoader coordinates repeated key lookups, but it does not replace pagination, filtering, projections, indexes, or other data-source optimization.

## Create a one-to-many DataLoader

Use `ILookup<TKey, TValue>` when one key maps to many values. The generated DataLoader returns an array for each key and returns an empty array when the lookup has no entries for a requested key.

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

The generated interface is `IProductsByBrandIdDataLoader`, and `LoadAsync(brand.Id, cancellationToken)` returns `Product[]`:

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

A dictionary whose value is a collection is also a supported batch-loader shape:

```csharp
public static Task<IReadOnlyDictionary<int, Product[]>> GetProductListByBrandIdAsync(
    IReadOnlyList<int> brandIds,
    CatalogContext db,
    CancellationToken cancellationToken)
{
    // Fetch products and return a dictionary keyed by BrandId.
}
```

That shape is a batch DataLoader with a collection value. The formal generated group-loader shape is `ILookup<TKey, TValue>`.

## Create a scalar cache loader

If the first parameter is a scalar key and the method returns one value, the generator creates a cache DataLoader. Use this when the backing source already exposes a one-key API but you still want request-scoped deduplication and the same `LoadAsync` consumption pattern.

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

The generated interface is `IDisplayNameByUserIdDataLoader`. Repeated loads for the same user ID in one GraphQL request share the cached task. Prefer the `IReadOnlyList<TKey>` batch shape when the backing store can fetch many keys with one call.

## Classify DataLoader method parameters

The first parameter defines the key shape. Every parameter after the key or keys parameter is classified by the generator.

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

DataLoader method services use dependency injection, like resolver services. In v16, scoped services in query resolvers and DataLoaders use resolver or DataLoader scope by default. Review service lifetimes before changing the generated scope, especially for non-thread-safe services such as EF Core contexts.

## Configure generated names, access, and service scope

Default naming removes common method words:

| Method name                   | Generated class           | Generated interface        |
| ----------------------------- | ------------------------- | -------------------------- |
| `GetBrandByIdAsync`           | `BrandByIdDataLoader`     | `IBrandByIdDataLoader`     |
| `LoadBrandByIdAsync`          | `LoadBrandByIdDataLoader` | `ILoadBrandByIdDataLoader` |
| `GetBrandByIdDataLoaderAsync` | `BrandByIdDataLoader`     | `IBrandByIdDataLoader`     |

Use the attribute constructor when you want a stable generated name that does not follow the method name:

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

Use `[assembly: DataLoaderDefaults]` when one project should apply the same generator settings to all generated DataLoaders.

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

The source generator can emit registration code, but your application still has to call the generated registration extension. There are two common paths.

### Register through the Hot Chocolate type module

Use this path when your GraphQL project already uses Hot Chocolate source-generated type modules. The generated `IRequestExecutorBuilder` extension registers generated GraphQL types and generated DataLoaders.

```csharp
using HotChocolate.Types;

[assembly: Module("CatalogTypes")]
```

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<CatalogContext>();

builder
    .AddGraphQL()
    .AddCatalogTypes(); // Generated for a module named CatalogTypes.
```

The exact extension name depends on the assembly name or module attribute used by your project. Inspect generated output to confirm the method name before documenting it in shared application templates.

### Register through a DataLoader-only module

Use `[assembly: DataLoaderModule("Catalog")]` when you want generated `IServiceCollection` registration for DataLoaders without relying on the Hot Chocolate type module path.

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

For `DataLoaderModule("Catalog")`, the generated service extension is named `AddCatalog`. Set `IsInternal = true` when the generated extension class should be internal:

```csharp
using GreenDonut;

[assembly: DataLoaderModule("Catalog", IsInternal = true)]
```

When interfaces are generated, registration maps `I{Name}DataLoader` to `{Name}DataLoader`. When `GenerateInterfaces = false`, registration uses the generated class.

## Inspect generated code

Inspect generated code when a type name, interface, or registration method is not clear. IDEs usually show source-generated files under dependencies, analyzers, or generated files.

Useful files to look for include:

| Generated file kind                 | What to confirm                                                       |
| ----------------------------------- | --------------------------------------------------------------------- |
| `GreenDonutDataLoader.*.g.cs`       | Generated class name, interface name, loader kind, and method call.   |
| `GreenDonutDataLoaderModule.*.g.cs` | `IServiceCollection` extension from `[DataLoaderModule]`.             |
| `HotChocolateTypeModule.*.g.cs`     | `IRequestExecutorBuilder` extension used by type module registration. |

Generated files are build output. Do not edit them. Change the `[DataLoader]` method, assembly attributes, or registration call instead.

## Use supported method shapes

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

## Choose between generated loaders, batch resolvers, and manual loaders

| Use                             | When it fits                                                                                                   | Where to go next                                                                                                            |
| ------------------------------- | -------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------- |
| Source-generated `[DataLoader]` | The lookup is key-based, reusable, and fits the supported method shapes.                                       | This page                                                                                                                   |
| Batch resolver                  | The batched work belongs to one field and does not need reusable request cache entries by key.                 | [Current DataLoader guide](/docs/hotchocolate/v16/resolvers-and-data/dataloader#batch-resolvers)                            |
| Manual DataLoader               | You need custom class-based scheduling, caching, error mapping, constructor logic, or migration compatibility. | [Batch DataLoader](./batch-dataloader), [Group DataLoader](./group-dataloader), and [Cache DataLoaders](./cache-dataloader) |

Keep generated loaders as your first choice for ordinary keyed data access. Move to a field-local batch resolver or manual loader when the problem no longer matches the generated method shapes.

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

## Know the advanced hooks

`Lookups` adds alternate cache lookup methods for advanced entity-cache scenarios. The named methods live on the containing type and map loaded values to alternate keys or key-value transforms. Keep this for cases where one loaded entity should populate more than one cache key.

`DataLoaderGroup` groups multiple generated DataLoaders into injectable context classes. The attribute can be placed on a DataLoader method or on the containing class. Use it when a service needs a cohesive set of generated loaders instead of many constructor parameters.

GreenDonut data parameters such as selectors, predicates, paging arguments, sort definitions, and query contexts are integration hooks. Treat them as advanced state passed through DataLoader APIs, not as general GraphQL resolver arguments.

## Go next

- Review [DataLoader concepts](./index) for N+1 detection, execution waves, request caching, and optimization choices.
- Use [Batch DataLoader](./batch-dataloader) when you need manual one-to-one loader classes.
- Use [Group DataLoader](./group-dataloader) for one-to-many relationship patterns and manual group loaders.
- Use [Cache DataLoaders](./cache-dataloader) for manual or generated one-key cache loaders.
- Review [Service Injection](../resolvers/service-injection) before changing DataLoader service scopes.
- Review [Resolver Signatures](../resolvers/resolver-signature) and [Parent access](../resolvers/parent-attribute) for resolver parameter binding.
- Use the [current DataLoader guide](/docs/hotchocolate/v16/resolvers-and-data/dataloader) for grouped loaders and batch resolvers while the build2 child pages are being completed.
