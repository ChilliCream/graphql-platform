---
title: "Service Injection"
---

Resolvers frequently require application services such as repositories, domain services, HTTP clients, DataLoaders, tenant providers, or EF Core contexts. In Hot Chocolate v16, the standard approach is to register these services with ASP.NET Core dependency injection and receive them as resolver parameters.

This page explains how service injection works and how service scopes behave during GraphQL execution. For a broader overview of resolver parameters, see [Resolver Signatures](./resolver-signature). For details on attributes that bind arguments, parents, state, and selections, refer to [Parameter Attributes](./parameter-attributes).

## What you will learn

- Register an application service and inject it into a resolver without attributes.
- Access services from code-first resolver delegates with `ctx.Service<T>()`.
- Use `[Service("key")]` for keyed services and nullable parameters for optional service paths.
- Choose resolver parameter injection instead of constructor injection into GraphQL type definitions.
- Predict scoped service behavior for queries, mutations, batch resolvers, and DataLoaders.
- Override request and resolver service scopes when a field has a specific lifetime need.
- Work safely with EF Core and other non-thread-safe services.
- Know when to use DI, global state, context state, DataLoader, schema services, or request provider replacement.

## Register a service and inject it into a resolver

Register your application services with the standard ASP.NET Core service collection. Hot Chocolate will use this provider when executing resolvers.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<ProductService>();

builder
    .AddGraphQL()
    .AddTypes();
```

The service can be any regular application type:

```csharp
public sealed record Product(int Id, string Name);

public sealed class ProductService
{
    private static readonly Product[] s_products =
    [
        new(1, "Trail Shoe"),
        new(2, "Rain Jacket")
    ];

    public Task<Product?> GetProductByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(
            s_products.FirstOrDefault(product => product.Id == id));
    }
}
```

To use the service, accept it as a parameter in your resolver method:

```csharp
[QueryType]
public static partial class ProductQueries
{
    public static Task<Product?> GetProductByIdAsync(
        int id,
        ProductService products,
        CancellationToken cancellationToken)
        => products.GetProductByIdAsync(id, cancellationToken);
}
```

Hot Chocolate automatically binds each parameter from the appropriate source:

| Parameter                             | Bound from           | Schema effect              |
| ------------------------------------- | -------------------- | -------------------------- |
| `int id`                              | GraphQL argument     | Exposed as `id: Int!`.     |
| `ProductService products`             | ASP.NET Core DI      | Not exposed in the schema. |
| `CancellationToken cancellationToken` | Request cancellation | Not exposed in the schema. |

Expected SDL excerpt:

```graphql
type Query {
  productById(id: Int!): Product
}

type Product {
  id: Int!
  name: String!
}
```

Client operation:

```graphql
query GetProduct {
  productById(id: 1) {
    id
    name
  }
}
```

Example response:

```json
{
  "data": {
    "productById": {
      "id": 1,
      "name": "Trail Shoe"
    }
  }
}
```

In v16, Hot Chocolate infers registered service parameters automatically. You do not need to use `[Service]` for this common resolver pattern.

## Use `ctx.Service<T>()` in code-first resolvers

When configuring a field with the descriptor API, access services through the resolver context.

```csharp
public sealed class ProductQueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("productById")
            .Argument("id", a => a.Type<NonNullType<IntType>>())
            .Resolve(async (ctx, cancellationToken) =>
            {
                var id = ctx.ArgumentValue<int>("id");
                var products = ctx.Service<ProductService>();

                return await products.GetProductByIdAsync(
                    id,
                    cancellationToken);
            });
    }
}
```

For implementation-first methods, prefer typed resolver parameters. For descriptor resolver delegates, use `ctx.Service<T>()`. Avoid using `IServiceProvider` directly when a typed parameter or `ctx.Service<T>()` is available.

To access keyed services in descriptor resolvers, provide the key to `ctx.Service<T>()`:

```csharp
var prices = ctx.Service<IPriceService>("retail");
```

## Use `[Service]` for keyed and explicit service binding

While Hot Chocolate v16 typically infers registered services, `[Service]` is still helpful when you need a keyed service or want to explicitly mark a parameter as a service.

Register a keyed service with ASP.NET Core DI:

```csharp
builder.Services.AddKeyedScoped<IPriceService, RetailPriceService>("retail");
```

Inject it with `[Service("key")]`:

```csharp
public interface IPriceService
{
    Task<decimal> GetCostAsync(int productId, CancellationToken cancellationToken);
}

public sealed class RetailPriceService : IPriceService
{
    public Task<decimal> GetCostAsync(
        int productId,
        CancellationToken cancellationToken)
        => Task.FromResult(19.99m);
}

[QueryType]
public static partial class ProductQueries
{
    public static Task<decimal> GetRetailCostAsync(
        int productId,
        [Service("retail")] IPriceService prices,
        CancellationToken cancellationToken)
        => prices.GetCostAsync(productId, cancellationToken);
}
```

Expected SDL excerpt:

```graphql
type Query {
  retailCost(productId: Int!): Decimal!
}
```

### Make optional services nullable

When Hot Chocolate binds a parameter as a service, a nullable service parameter can receive `null`. A non-nullable service parameter must be resolved from the current service provider.

Use a nullable service parameter only if your application logic allows for the absence of the service:

```csharp
public interface IPromotionService
{
    string CurrentLabel { get; }
}

[QueryType]
public static partial class ProductQueries
{
    public static string GetPromotionLabel(
        [Service("promotion")] IPromotionService? promotions)
        => promotions?.CurrentLabel ?? "Standard";
}
```

If an optional service might not be registered, add `[Service]` or `[Service("key")]` to ensure the parameter is treated as a service rather than a GraphQL argument.

## Prefer resolver parameters over type constructors

GraphQL type definitions are schema-level components and may outlive individual requests. Resolver parameters, on the other hand, are provided at execution time. Place application services on resolver methods so Hot Chocolate can resolve them from the appropriate request or resolver scope.

Avoid injecting services into GraphQL type definition constructors:

```csharp
public sealed class ProductType(ProductService products) : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor
            .Field("related")
            .Resolve(async ctx =>
            {
                var product = ctx.Parent<Product>();
                return await products.GetRelatedAsync(
                    product.Id,
                    ctx.RequestAborted);
            });
    }
}
```

Instead, access services within the resolver itself:

```csharp
public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor
            .Field("related")
            .Resolve(async ctx =>
            {
                var product = ctx.Parent<Product>();
                var products = ctx.Service<ProductService>();

                return await products.GetRelatedAsync(
                    product.Id,
                    ctx.RequestAborted);
            });
    }
}
```

You can still use constructor injection inside your application services:

```csharp
public sealed class ProductService(HttpClient httpClient)
{
    public Task<IReadOnlyList<Product>> GetRelatedAsync(
        int productId,
        CancellationToken cancellationToken)
    {
        // Call the downstream API here.
        return Task.FromResult<IReadOnlyList<Product>>([]);
    }
}
```

## Understand scoped services during execution

Singleton and transient services behave as they do in standard ASP.NET Core DI. Scoped services, however, depend on the GraphQL DI scope chosen for the field.

| Scope          | Meaning                                             |
| -------------- | --------------------------------------------------- |
| Request scope  | One scoped instance for the entire GraphQL request. |
| Resolver scope | A new async scope for each resolver execution.      |

Hot Chocolate selects safe defaults for common execution scenarios:

| Boundary                  | Default service scope                                                | Why it matters                                                                                   |
| ------------------------- | -------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------ |
| Query fields              | `DependencyInjectionScope.Resolver`                                  | Sibling query fields can run in parallel, so each resolver receives its own scoped instance.     |
| Top-level mutation fields | `DependencyInjectionScope.Request`                                   | Top-level mutations execute sequentially, so they can share request-scoped services.             |
| Batch resolvers           | The field's configured `DependencyInjectionScope`                    | With resolver scope, the batch resolver execution gets one resolver scope for that batched call. |
| DataLoaders               | `DataLoaderServiceScope.Default` unless configured on the DataLoader | DataLoader service scope is configured on the DataLoader, not on the field.                      |

Query execution shape:

```text
GraphQL query request
  request service provider
    Query.productById(id: 1) -> resolver scope A
    Query.featuredProducts   -> resolver scope B
    Product.brand            -> resolver scope C
```

Mutation execution shape:

```text
GraphQL mutation request
  request service provider
    Mutation.createProduct -> request scope
    Mutation.renameProduct -> same request scope
```

These defaults protect non-thread-safe scoped services during parallel query execution, while maintaining request-wide behavior for sequential mutations.

## Change the DI scope for a resolver

Stick with the defaults unless a field has a specific lifetime requirement.

Good reasons to change scope include:

| Need                                                                              | Prefer                                                |
| --------------------------------------------------------------------------------- | ----------------------------------------------------- |
| A query field must share request-scoped state and all access is safe              | Request scope for that field.                         |
| A mutation field uses a non-thread-safe scoped service and should not share state | Resolver scope for that field.                        |
| Your app has a different default execution policy                                 | Global default options plus targeted field overrides. |

Set global defaults with schema options:

```csharp
builder
    .AddGraphQL()
    .ModifyOptions(options =>
    {
        options.DefaultQueryDependencyInjectionScope =
            DependencyInjectionScope.Resolver;
        options.DefaultMutationDependencyInjectionScope =
            DependencyInjectionScope.Request;
    });
```

Override implementation-first resolvers with attributes:

```csharp
[QueryType]
public static partial class ProductQueries
{
    [UseRequestScope]
    public static Task<Product?> GetProductByIdAsync(
        int id,
        ProductService products,
        CancellationToken cancellationToken)
        => products.GetProductByIdAsync(id, cancellationToken);
}

[MutationType]
public static partial class ProductMutations
{
    [UseResolverScope]
    public static Task<Product> RenameProductAsync(
        int id,
        string name,
        ProductService products,
        CancellationToken cancellationToken)
        => products.RenameProductAsync(id, name, cancellationToken);
}
```

Override code-first fields with descriptor middleware:

```csharp
descriptor
    .Field("productById")
    .Argument("id", a => a.Type<NonNullType<IntType>>())
    .UseRequestScope()
    .Resolve(async (ctx, cancellationToken) =>
    {
        var products = ctx.Service<ProductService>();
        var id = ctx.ArgumentValue<int>("id");

        return await products.GetProductByIdAsync(id, cancellationToken);
    });

descriptor
    .Field("renameProduct")
    .Argument("id", a => a.Type<NonNullType<IntType>>())
    .Argument("name", a => a.Type<NonNullType<StringType>>())
    .UseResolverScope()
    .Resolve(async (ctx, cancellationToken) =>
    {
        var products = ctx.Service<ProductService>();

        return await products.RenameProductAsync(
            ctx.ArgumentValue<int>("id"),
            ctx.ArgumentValue<string>("name"),
            cancellationToken);
    });
```

## Work safely with EF Core and non-thread-safe services

EF Core `DbContext` is a scoped service and does not support concurrent operations on the same instance. Hot Chocolate's default query resolver scope ensures that sibling query fields do not share a single scoped `DbContext` instance.

Directly injecting the context into a resolver is safe when you use the default query scope:

```csharp
[QueryType]
public static partial class ProductQueries
{
    public static async Task<Product?> GetProductByIdAsync(
        int id,
        CatalogContext db,
        CancellationToken cancellationToken)
        => await db.Products.FindAsync([id], cancellationToken);
}
```

Mutation resolvers use the request scope by default because top-level mutations run sequentially:

```csharp
[MutationType]
public static partial class ProductMutations
{
    public static async Task<Product> CreateProductAsync(
        CreateProductInput input,
        CatalogContext db,
        CancellationToken cancellationToken)
    {
        var product = new Product(input.Id, input.Name);
        db.Products.Add(product);
        await db.SaveChangesAsync(cancellationToken);
        return product;
    }
}
```

> **Warning:** Do not force parallel query fields that share an EF Core `DbContext` into request scope. EF Core will report an error if a second operation starts before the previous one completes.

For services and DataLoaders that manage data access, prefer using `IDbContextFactory<T>` and dispose of the context you create.

<PackageInstallation packageName="HotChocolate.Data.EntityFramework" />

```csharp
builder.Services.AddDbContextFactory<CatalogContext>(
    options => options.UseSqlServer("YOUR_CONNECTION_STRING"));

builder.Services.AddScoped<ProductService>();

builder
    .AddGraphQL()
    .RegisterDbContextFactory<CatalogContext>()
    .AddTypes();
```

```csharp
public sealed class ProductService(
    IDbContextFactory<CatalogContext> dbContextFactory)
{
    public async Task<Product?> GetProductByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        await using var db =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await db.Products.FindAsync([id], cancellationToken);
    }
}
```

`RegisterDbContextFactory<T>()` enables Hot Chocolate resolver compiler support for factory-backed `DbContext` resolver parameters. You still need `AddDbContextFactory<T>()` or `AddPooledDbContextFactory<T>()` in ASP.NET Core DI.

## Inject services into batch resolvers

A batch resolver executes once for a set of parent values. You can inject services into a batch resolver in the same way as with a regular resolver.

```csharp
public sealed record Customer(int Id, int ShippingAddressId);
public sealed record Address(int Id, string Street);

public sealed class AddressService
{
    public Task<IReadOnlyDictionary<int, Address>> GetByIdAsync(
        IReadOnlyList<int> ids,
        CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyDictionary<int, Address>>(
            new Dictionary<int, Address>
            {
                [1] = new(1, "100 GraphQL Way")
            });
}

[ObjectType<Customer>]
public static partial class CustomerNode
{
    [BatchResolver]
    public static async Task<List<Address?>> GetShippingAddressAsync(
        [Parent] List<Customer> customers,
        AddressService addresses,
        CancellationToken cancellationToken)
    {
        var ids = customers
            .Select(customer => customer.ShippingAddressId)
            .Distinct()
            .ToArray();

        var byId = await addresses.GetByIdAsync(ids, cancellationToken);

        return customers
            .Select(customer =>
                byId.GetValueOrDefault(customer.ShippingAddressId))
            .ToList();
    }
}
```

The returned list must match the count and order of the parent list. The service scope is determined by the field's configured DI scope. If the field uses resolver scope, the batch resolver call receives a single resolver scope for that execution.

Use a DataLoader if multiple fields require the same batched lookup or request-level caching. For more on batching, see the [DataLoader](/docs/hotchocolate/v16/build/dataloader) documentation.

## Keep DataLoader service boundaries clear

Resolvers frequently inject DataLoaders instead of data services to enable batching and caching. DataLoaders can also receive services, but their service boundary is set on the DataLoader itself.

```csharp
[DataLoader(ServiceScope = DataLoaderServiceScope.DataLoaderScope)]
public static async Task<Dictionary<int, Product>> GetProductByIdAsync(
    IReadOnlyList<int> ids,
    IDbContextFactory<CatalogContext> dbContextFactory,
    CancellationToken cancellationToken)
{
    await using var db =
        await dbContextFactory.CreateDbContextAsync(cancellationToken);

    return await db.Products
        .Where(product => ids.Contains(product.Id))
        .ToDictionaryAsync(product => product.Id, cancellationToken);
}
```

The `[DataLoader]` attribute generates an interface such as `IProductByIdDataLoader`. A resolver can then inject this generated interface as a runtime parameter:

```csharp
public static async Task<Product?> GetProductAsync(
    int id,
    IProductByIdDataLoader productById,
    CancellationToken cancellationToken)
    => await productById.LoadAsync(id, cancellationToken);
```

DataLoader service scope options:

| Option                                   | Effect                                                                |
| ---------------------------------------- | --------------------------------------------------------------------- |
| `DataLoaderServiceScope.Default`         | Use the DataLoader default behavior.                                  |
| `DataLoaderServiceScope.DataLoaderScope` | Create a dedicated DataLoader service scope.                          |
| `DataLoaderServiceScope.OriginalScope`   | Resolve services from the original provider passed to the DataLoader. |

For manual DataLoader classes, constructor injection is the normal pattern for `IBatchScheduler`, `DataLoaderOptions`, and application dependencies. If the DataLoader creates a scope or a `DbContext`, dispose it inside the load method.

## Use application services in schema-level components

Resolvers access the application service provider at execution time. Schema-level components, however, are constructed from Hot Chocolate schema services. If a schema-level component requires an application service, cross-register it using `.AddApplicationService<T>()`.

```csharp
builder.Services.AddSingleton<MyAuditService>();

builder
    .AddGraphQL()
    .AddApplicationService<MyAuditService>()
    .AddDiagnosticEventListener<MyDiagnosticEventListener>();
```

Use this approach for application services required by schema-level components such as:

- HTTP request interceptors.
- Socket session interceptors.
- Error filters.
- Diagnostic event listeners.
- Operation compiler optimizers.
- Transaction scope handlers.
- Document storage providers.
- Custom instrumentation activity enrichers.

> **Important:** Resolver parameters do not need `.AddApplicationService<T>()`. Resolver parameters continue to use the application service provider directly.

Services registered with `.AddApplicationService<T>()` are resolved from the application provider during schema initialization and registered as singletons in the schema service provider.

## Choose DI, state, or DataLoader for the job

Choose the boundary that best fits your data and scenario.

| Need                                                    | Use                                                 | Example                                                 | Why                                                                 |
| ------------------------------------------------------- | --------------------------------------------------- | ------------------------------------------------------- | ------------------------------------------------------------------- |
| Application behavior or data access                     | Resolver service parameter                          | `ProductService products`                               | Keeps resolvers thin and uses ASP.NET Core DI lifetimes.            |
| Keyed application dependency                            | `[Service("retail")]` or `ctx.Service<T>("retail")` | Retail and wholesale price services                     | Selects the named DI registration.                                  |
| Per-request value from HTTP, auth, or tenant resolution | Global state                                        | `[GlobalState("TenantId")] string tenantId`             | Shares one immutable request value across resolvers and middleware. |
| Field pipeline value                                    | Scoped or local context state                       | Middleware writes a computed value for a child resolver | Keeps field-specific data inside GraphQL execution state.           |
| Batched lookup and request cache                        | DataLoader                                          | `IProductByIdDataLoader`                                | Batches related loads and caches per request.                       |
| Application service inside a schema component           | `.AddApplicationService<T>()`                       | Diagnostic listener needs `MyAuditService`              | Bridges application services into schema services.                  |

## Advanced: initialize resolver-scoped services from request-scoped state

`AddScopedServiceInitializer<TService>()` allows you to copy or initialize state from a request-scoped instance into a resolver-scoped instance before resolver execution.

```csharp
public sealed class RequestTenant
{
    public string? TenantId { get; set; }
}

builder.Services.AddScoped<RequestTenant>();

builder
    .AddGraphQL()
    .AddScopedServiceInitializer<RequestTenant>(
        (requestTenant, resolverTenant) =>
        {
            resolverTenant.TenantId = requestTenant.TenantId;
        })
    .AddTypes();
```

Use this only for advanced resolver-scope initialization. For most scenarios, prefer explicit service design or global state for passing tenant IDs, user IDs, correlation IDs, and similar request data.

## Advanced: replace the request service provider

By default, Hot Chocolate uses `HttpContext.RequestServices` for resolver services. You can replace the provider for a GraphQL request using an interceptor and `OperationRequestBuilder.SetServices()`.

```csharp
public sealed class TenantServicesSocketInterceptor(IServiceProvider tenantServices)
    : DefaultSocketSessionInterceptor
{
    public override async ValueTask OnRequestAsync(
        ISocketConnection connection,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        await base.OnRequestAsync(
            connection,
            requestBuilder,
            cancellationToken);

        requestBuilder.SetServices(tenantServices);
    }
}

builder
    .AddGraphQL()
    .AddSocketSessionInterceptor<TenantServicesSocketInterceptor>();
```

Always call the base interceptor method to ensure services, claims, and request state are initialized. Replacing the provider affects all resolvers in the request. Avoid using `SetServices()` for standard resolver dependency injection.

HTTP request interceptors support the same provider replacement pattern.

## Troubleshooting

| Symptom                                                                                | Likely cause                                                                                                            | Solution                                                                                                                 |
| -------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------ |
| A service parameter appears as a GraphQL argument.                                     | The service type is not registered, the registered type differs from the parameter type, or inference is not available. | Register the service, inject the registered interface, or add `[Service]` for explicit service binding.                  |
| Hot Chocolate cannot resolve a keyed service.                                          | The key or service type does not match the DI registration.                                                             | Check `AddKeyedScoped`, `AddKeyedSingleton`, or `AddKeyedTransient`, then match the key in `[Service("key")]`.           |
| An optional service became a required dependency.                                      | The parameter is non-nullable or not marked as a service.                                                               | Use a nullable service parameter and add `[Service]` or `[Service("key")]` when the registration may be absent.          |
| A scoped service has shared state between parallel query fields.                       | The field or global query default uses request scope.                                                                   | Return query fields to `DependencyInjectionScope.Resolver` unless the shared service is thread-safe.                     |
| EF Core reports concurrent use of a context.                                           | One `DbContext` instance is being used by parallel work.                                                                | Use the default query resolver scope, or move data access into a service or DataLoader that uses `IDbContextFactory<T>`. |
| A diagnostic listener, error filter, or interceptor cannot get an application service. | The component is built from schema services.                                                                            | Register the application service and add `.AddApplicationService<T>()`.                                                  |
| An interceptor changed services, claims, or state unexpectedly.                        | The base interceptor method was skipped, or `SetServices()` replaced the request provider.                              | Call the base method and use `SetServices()` only for advanced provider replacement.                                     |

## Quick reference

### Service access APIs

| Scenario                         | API                                        | Notes                                  |
| -------------------------------- | ------------------------------------------ | -------------------------------------- |
| Registered resolver service      | `ProductService products`                  | Preferred v16 resolver pattern.        |
| Explicit service parameter       | `[Service] ProductService products`        | Use for clarity or inference fallback. |
| Keyed service parameter          | `[Service("retail")] IPriceService prices` | Primary keyed service pattern.         |
| Code-first resolver service      | `ctx.Service<ProductService>()`            | Use inside resolver delegates.         |
| Keyed code-first service         | `ctx.Service<IPriceService>("retail")`     | Key must match DI registration.        |
| Schema-level application service | `.AddApplicationService<T>()`              | Not needed for resolver parameters.    |
| Request provider replacement     | `OperationRequestBuilder.SetServices()`    | Advanced interceptor scenario.         |

### Scope APIs

| Scenario                            | API                                                                               | Effect                                 |
| ----------------------------------- | --------------------------------------------------------------------------------- | -------------------------------------- |
| Query resolver scoped service       | `DependencyInjectionScope.Resolver`                                               | Default for query fields.              |
| Mutation resolver scoped service    | `DependencyInjectionScope.Request`                                                | Default for top-level mutation fields. |
| Global default scopes               | `DefaultQueryDependencyInjectionScope`, `DefaultMutationDependencyInjectionScope` | Set with `.ModifyOptions(...)`.        |
| Implementation-first request scope  | `[UseRequestScope]`                                                               | Uses request scope for one resolver.   |
| Implementation-first resolver scope | `[UseResolverScope]`                                                              | Uses resolver scope for one resolver.  |
| Code-first request scope            | `.UseRequestScope()`                                                              | Uses request scope for one field.      |
| Code-first resolver scope           | `.UseResolverScope()`                                                             | Uses resolver scope for one field.     |
| DataLoader service scope            | `DataLoaderServiceScope`                                                          | DataLoader-specific service boundary.  |

### Service choice guide

| Need                                             | Prefer                                           | Avoid                                                 |
| ------------------------------------------------ | ------------------------------------------------ | ----------------------------------------------------- |
| Call application or domain logic from a resolver | Resolver service parameter                       | Constructor injection into GraphQL type definitions.  |
| Share a per-request value such as tenant ID      | Global state                                     | Mutable singleton state.                              |
| Pass data through field middleware               | Scoped or local context state                    | Static or ambient mutable state.                      |
| Batch and cache entity loading                   | DataLoader                                       | Per-parent duplicate service calls.                   |
| Use EF Core in query fields                      | Default resolver scope or `IDbContextFactory<T>` | Sharing one `DbContext` across parallel query fields. |
| Use an app service in a schema component         | `.AddApplicationService<T>()`                    | Assuming resolver DI rules apply to schema services.  |

## Where to go next

| Goal                                                | Page                                                                                  |
| --------------------------------------------------- | ------------------------------------------------------------------------------------- |
| Review resolver parameter binding and return shapes | [Resolver Signatures](./resolver-signature)                                           |
| Review resolver parameter attributes                | [Parameter Attributes](./parameter-attributes)                                        |
| Access parent values in nested resolvers            | [Parent access](./parent-attribute)                                                   |
| Batch and cache related data                        | [DataLoader](/docs/hotchocolate/v16/build/dataloader)                                 |
| Integrate EF Core safely                            | [Entity Framework Core](/docs/hotchocolate/v16/_leagcy/integrations/entity-framework) |
| Share request data with resolvers                   | [Global State](/docs/hotchocolate/v16/build/server-configuration/global-state)        |
| Customize request creation                          | [Interceptors](/docs/hotchocolate/v16/build/server-configuration/interceptors)        |
