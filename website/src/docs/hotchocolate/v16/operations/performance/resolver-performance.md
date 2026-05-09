---
title: "Resolver Performance"
---

This page guides you in making Hot Chocolate v16 resolvers fast and reliable under production concurrency. It focuses on resolver code within a single Hot Chocolate server, covering async I/O, cancellation, DataLoader batching, provider-side query shaping, service lifetimes, middleware cost, allocations, instrumentation, and troubleshooting.

Fusion gateway planning and distributed query execution are not covered here. Use this page when a field resolver, nested relationship, or data-fetching path in a single Hot Chocolate server is slow.

# Prerequisites

You should be familiar with C# `async`/`await`, ASP.NET Core dependency injection, and GraphQL field selections. You do not need to use Entity Framework Core, MongoDB, or Marten, but the examples show how resolver shapes change when a backend supports filtering, sorting, projection, or paging.

Before tuning a resolver, know where to find deeper topics:

- [Resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers): resolver signatures, parent values, cancellation, and batch resolvers
- [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader): source-generated loaders, request caching, execution waves, and batch resolvers
- [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections), [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering), [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting), [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination): provider-side query shaping
- [Dependency Injection](/docs/hotchocolate/v16/resolvers-and-data/dependency-injection): resolver scopes, request scopes, and schema services
- [Instrumentation](/docs/hotchocolate/v16/server/instrumentation): OpenTelemetry and diagnostic listeners

# Use the Fast Resolver Shape

A performant nested resolver acts as a thin adapter. It reads the parent value, receives services via method parameters, accepts a `CancellationToken`, and delegates I/O to a DataLoader or application service.

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

For this resolver, a query like:

```graphql
query ProductsWithBrands {
  products(first: 20) {
    nodes {
      name
      brand {
        name
      }
    }
  }
}
```

Hot Chocolate calls `GetBrandAsync` for each selected product, but `IBrandByIdDataLoader` batches the unique brand IDs and executes a single backend call per batch.

Follow these principles for fast resolvers:

- Keep resolvers thin; put business logic and data access in services or DataLoaders
- Inject dependencies as method parameters, not via type definition constructors
- Return `Task<T>` for async I/O
- Accept and pass `CancellationToken` to all async APIs
- Avoid data-access loops in nested resolvers unless provider middleware shapes the query
- Use `ValueTask<T>` only as an advanced optimization when APIs often complete synchronously and you have measured allocation pressure

# How Resolver Execution Works

Hot Chocolate executes a GraphQL operation as a tree of field resolvers:

1. Parent fields resolve before their children
2. Sibling selections in a query can run in parallel
3. Top-level mutation fields run sequentially, but their child selections can run in parallel
4. DataLoaders collect keys during a resolver wave, dispatch between waves, deduplicate keys, and cache results for the request
5. Field middleware wraps every selected field where applied

For example, given:

```graphql
query ProductsWithRelations {
  products(first: 3) {
    nodes {
      name
      brand {
        name
      }
      type {
        name
      }
    }
  }
}
```

The execution proceeds as:

```text
Wave 1: Resolve products(first: 3)
        -> Product 1, Product 2, Product 3

Wave 2: Resolve brand and type for each product
        -> BrandById queues [1, 2, 1]
        -> ProductTypeById queues [4, 4, 5]

Dispatch: BrandById loads unique brand IDs [1, 2]
          ProductTypeById loads unique type IDs [4, 5]

Wave 3: Resolve Brand.name and ProductType.name
```

This model highlights common performance issues. If a nested resolver calls a database service directly, it can trigger N backend calls. Shared scoped services that are not thread-safe can fail when sibling fields run in parallel. Middleware that runs for every leaf field can dominate performance in large selection sets.

Do not rely on side effects or execution order in query resolvers. Keep side effects in top-level mutations or explicit workflows.

# Measure Before You Change Resolver Code

Always start with evidence. Capture the operation name, variables, authenticated user, request headers affecting authorization or selection, and representative data volume.

Add Hot Chocolate instrumentation and OpenTelemetry:

```csharp
// Program.cs
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddInstrumentation();

builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("Catalog.Api"))
    .WithTracing(t =>
    {
        t.AddAspNetCoreInstrumentation();
        t.AddHttpClientInstrumentation();
        t.AddHotChocolateInstrumentation();
        t.AddOtlpExporter();
    });
```

With default activity scopes, inspect the request span, field resolver spans, and DataLoader batch spans. DataLoader spans include `graphql.dataloader.batch.size`, which helps you spot missed batching.

Enable all scopes only when you need more detail:

```csharp
builder
    .AddGraphQL()
    .AddInstrumentation(options =>
    {
        options.Scopes = ActivityScopes.All;
    });
```

More scopes add overhead. In production, use sampling or targeted diagnostics for known slow operations.

If you write a diagnostic listener, keep it lightweight:

```csharp
public sealed class ResolverTimingListener : ExecutionDiagnosticEventListener
{
    public override bool EnableResolveFieldValue => true;

    public override IDisposable ResolveFieldValue(IMiddlewareContext context)
    {
        var start = Stopwatch.GetTimestamp();
        return new FieldScope(context.Selection.Field.Name, start);
    }

    private sealed class FieldScope(string fieldName, long start) : IDisposable
    {
        public void Dispose()
        {
            var elapsed = Stopwatch.GetElapsedTime(start);

            if (elapsed > TimeSpan.FromMilliseconds(100))
            {
                // Log a small, low-cardinality signal.
                Console.WriteLine($"Slow GraphQL field: {fieldName}");
            }
        }
    }
}
```

Diagnostic handlers run synchronously as part of the request. Do not export traces, call remote systems, write large logs, or serialize operation documents inside the handler. DataLoader keys, operation documents, and variable values may contain sensitive data and can create high-cardinality telemetry.

# Avoid Blocking and Sync-over-Async

Blocking calls waste request threads and cause latency spikes under load.

Do not use patterns like:

```csharp
public static Brand GetBrand(int id, IBrandClient brandClient)
{
    // Blocks the request thread while async I/O is in progress.
    return brandClient.GetBrandAsync(id).Result;
}
```

Instead, use async I/O with cancellation:

```csharp
public static async Task<Brand> GetBrandAsync(
    int id,
    IBrandClient brandClient,
    CancellationToken ct)
    => await brandClient.GetBrandAsync(id, ct);
```

Also avoid `.Wait()`, `GetAwaiter().GetResult()`, `Thread.Sleep`, synchronous EF or network APIs, and blocking locks in field resolvers. Do not wrap blocking I/O in `Task.Run`; fix the underlying API or move the work out of the request path.

For CPU-heavy work, use precomputed data, a cache, a background job, or a bounded application service. Do not start unbounded CPU work from every selected child field.

# Fix N+1 with DataLoader or Batch Resolvers

Use a DataLoader when many resolver calls need the same lookup shape. Create one loader per shape, such as `BrandById`, `BrandByName`, or `ProductsByBrandId`.

A problematic nested resolver:

```csharp
[ObjectType<Product>]
public static partial class ProductNode
{
    public static async Task<Brand> GetBrandAsync(
        [Parent] Product product,
        BrandService brandService,
        CancellationToken ct)
        => await brandService.GetByIdAsync(product.BrandId, ct);
}
```

For 20 products, this can trigger 20 backend calls.

Instead, use a source-generated DataLoader:

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
            .AsNoTracking()
            .Where(b => ids.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, ct);
}
```

And a fast resolver:

```csharp
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

For `products(first: 20) { nodes { brand { name } } }`, the backend sees:

```text
1 products query
1 brand batch for unique BrandId values
```

For one-to-many relationships, return grouped results and convert missing groups to an empty collection:

```csharp
internal static class ProductDataLoaders
{
    [DataLoader]
    public static async Task<Dictionary<int, Product[]>> GetProductsByBrandIdAsync(
        IReadOnlyList<int> brandIds,
        CatalogContext db,
        CancellationToken ct)
        => await db.Products
            .AsNoTracking()
            .Where(p => brandIds.Contains(p.BrandId))
            .GroupBy(p => p.BrandId)
            .Select(g => new { g.Key, Items = g.ToArray() })
            .ToDictionaryAsync(g => g.Key, g => g.Items, ct);
}

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

DataLoader caching is request-scoped. Use a separate cache for cross-request caching. The `IReadOnlyList<TKey>` passed into a DataLoader is rented; do not store or use it after the method returns.

Use a [batch resolver](/docs/hotchocolate/v16/resolvers-and-data/dataloader#batch-resolvers) when batching is specific to one field and does not benefit from key-based request caching across fields.

# Push Filtering, Sorting, Projection, and Paging to the Data Source

Let the backend handle set operations. Return a provider-aware shape and let Hot Chocolate middleware translate selected fields, filters, sort order, and page arguments.

```csharp
// Types/ProductQueries.cs
[QueryType]
public static partial class ProductQueries
{
    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Product> GetProducts(CatalogContext db)
        => db.Products.AsNoTracking();
}
```

Order matters: `[UsePaging]`, then `[UseProjection]`, then `[UseFiltering]`, then `[UseSorting]`.

Do not materialize before middleware runs:

```csharp
[UsePaging]
[UseProjection]
[UseFiltering]
[UseSorting]
public static async Task<List<Product>> GetProductsAsync(
    CatalogContext db,
    CancellationToken ct)
    => await db.Products.ToListAsync(ct); // Loads all rows too early.
```

For projection, filtering, and sorting without projection middleware, return `QueryContext<T>`:

```csharp
[QueryType]
public static partial class ProductQueries
{
    public static QueryContext<Product> GetProducts(CatalogContext db)
        => db.Products.AsNoTracking().AsQueryContext();
}
```

Do not combine `QueryContext<T>` with `[UseProjection]` on the same field. The HC0099 analyzer will warn if both are present.

Projection has limits:

- Custom field resolvers are not projected into the database by default
- If a child resolver needs a parent value not selected by the client, mark that parent field with `[IsProjected(true)]`
- Projected members need public setters so Hot Chocolate can construct projected objects

Use DataLoader for relationship fields that do not translate cleanly into a single provider query.

# Choose the Right Backend Query Shape

| Backend      | Preferred resolver shape                                              | Performance check                                                                                                                                 | Deeper docs                                                                                                                                                            |
| ------------ | --------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| EF Core      | `IQueryable<T>` for lists, async terminal operations for single items | Keep filtering, sorting, projection, and paging in SQL. Use `AsNoTracking()` for read-only paths. Use DataLoader or projection for relationships. | [Fetching from Databases](/docs/hotchocolate/v16/resolvers-and-data/fetching-from-databases), [Entity Framework](/docs/hotchocolate/v16/integrations/entity-framework) |
| MongoDB      | `IExecutable<T>` from `AsExecutable()`                                | Register MongoDB paging, projection, filtering, and sorting providers. Measure projections with real indexes and explain plans.                   | [MongoDB](/docs/hotchocolate/v16/integrations/mongodb)                                                                                                                 |
| Marten       | `IQueryable<T>` with Marten conventions                               | Pagination and projections work out of the box. Filtering and sorting need Marten conventions.                                                    | [Marten](/docs/hotchocolate/v16/integrations/marten)                                                                                                                   |
| External API | DataLoader, batch endpoint, or `Connection<T>`                        | Batch per lookup shape. Preserve remote cursors instead of fetching all pages.                                                                    | [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination)                                                                                                     |

MongoDB example:

```csharp
[QueryType]
public static partial class PersonQueries
{
    [UsePaging]
    [UseProjection]
    [UseSorting]
    [UseFiltering]
    public static IExecutable<Person> GetPersons(IMongoCollection<Person> collection)
        => collection.AsExecutable();
}
```

Avoid mixing multiple backends in one resolver path unless you measure the fan-out. When a GraphQL field needs data from both a database and an external service, use batching and explicit caching boundaries.

# Page Large Result Sets Deliberately

Apply `[UsePaging]` to fields that can grow. In v16, pagination defaults to `DefaultPageSize = 10` and `MaxPageSize = 50` unless you configure them.

```csharp
[QueryType]
public static partial class ProductQueries
{
    [UsePaging(MaxPageSize = 100, DefaultPageSize = 25)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Product> GetProducts(CatalogContext db)
        => db.Products.AsNoTracking().OrderBy(p => p.Id);
}
```

Use stable ordering for deterministic pages. Avoid `IncludeTotalCount` on hot paths unless clients need it, as total count can add a separate database query or remote call.

For an already paged external API, return a `Connection<T>` from the received page instead of loading every remote page:

```csharp
[UsePaging]
public static async Task<Connection<Product>> GetProductsAsync(
    string? after,
    int? first,
    ProductApiClient client,
    CancellationToken ct)
{
    var page = await client.GetProductsAsync(after, first, ct);

    var edges = page.Items
        .Select(p => new Edge<Product>(p, p.Cursor))
        .ToList();

    var pageInfo = new ConnectionPageInfo(
        page.HasNextPage,
        page.HasPreviousPage,
        edges.FirstOrDefault()?.Cursor,
        edges.LastOrDefault()?.Cursor);

    return new Connection<Product>(edges, pageInfo);
}
```

For large one-to-many relationships, do not use an unbounded group DataLoader. Add field-level pagination or design a paged loader.

# Scope Services for Parallel Resolver Execution

Prefer method-level service injection. Hot Chocolate v16 automatically recognizes registered services in resolver parameters.

```csharp
public static async Task<Product?> GetProductByIdAsync(
    int id,
    CatalogContext db,
    CancellationToken ct)
    => await db.Products.FindAsync([id], ct);
```

Avoid constructor injection into GraphQL type definitions. Type definitions are singletons, so constructor dependencies can be shared across requests and cannot be synchronized by Hot Chocolate during resolver execution.

By default, scoped services are resolver-scoped for queries and DataLoaders, and request-scoped for mutations:

```csharp
builder
    .AddGraphQL()
    .ModifyOptions(o =>
    {
        o.DefaultQueryDependencyInjectionScope =
            DependencyInjectionScope.Resolver;
        o.DefaultMutationDependencyInjectionScope =
            DependencyInjectionScope.Request;
    });
```

This default is important for EF Core because `DbContext` is not thread-safe. Resolver scope prevents parallel query fields from using the same scoped instance.

Use `[UseRequestScope]` only when shared request-scoped state is required and safe:

```csharp
[QueryType]
public static partial class ProductQueries
{
    [UseRequestScope]
    public static async Task<Product?> GetProductByIdAsync(
        int id,
        CatalogContext db,
        CancellationToken ct)
        => await db.Products.FindAsync([id], ct);
}
```

Do not use request scope to hide lifetime problems. It can reintroduce concurrent access issues when a shared service is not thread-safe.

When a DataLoader needs a dedicated scoped lifetime, configure its service scope:

```csharp
[DataLoader(ServiceScope = DataLoaderServiceScope.DataLoaderScope)]
public static async Task<Dictionary<int, Brand>> GetBrandByIdAsync(
    IReadOnlyList<int> ids,
    CatalogContext db,
    CancellationToken ct)
    => await db.Brands
        .AsNoTracking()
        .Where(b => ids.Contains(b.Id))
        .ToDictionaryAsync(b => b.Id, ct);
```

If schema services such as diagnostic listeners, error filters, or activity enrichers need application services, use `AddApplicationService<T>()`. Do not inject scoped request services into singleton diagnostics.

# Keep Field Middleware and Authorization Efficient

Field middleware runs for every selected field where applied. Each middleware adds work to the resolver pipeline.

A minimal middleware should do bounded work and call the next delegate:

```csharp
public static class TrimStringMiddlewareExtensions
{
    public static IObjectFieldDescriptor UseTrimmedString(
        this IObjectFieldDescriptor descriptor)
        => descriptor.Use(next => async context =>
        {
            await next(context);

            if (context.Result is string value)
            {
                context.Result = value.Trim();
            }
        });
}
```

Avoid network calls, database calls, and large allocations in generic middleware. If middleware needs external data, batch it through a service or DataLoader and apply it narrowly.

Authorization has similar performance trade-offs. Field and type authorization must preserve your security model, but repeated expensive leaf-field checks can multiply work. When possible, authorize an object-returning parent field once instead of repeating the same expensive policy on many child fields.

# Fetch Conditionally When Selections Change Cost

Use `[IsSelected]` for targeted optional work, such as calling an external inventory service only when the client selects inventory data. Prefer projections and DataLoaders first; use selection-aware branches only for measured hot paths.

```csharp
public static async Task<ProductDetails> GetDetailsAsync(
    [Parent] Product product,
    [IsSelected(nameof(ProductDetails.Inventory))] bool inventorySelected,
    IProductDetailsDataLoader detailsLoader,
    IInventoryService inventory,
    CancellationToken ct)
{
    var details = await detailsLoader.LoadAsync(product.Id, ct);

    if (inventorySelected)
    {
        details.Inventory = await inventory.GetStockAsync(product.Id, ct);
    }

    return details;
}
```

Here, the inventory service is only called if the `inventory` field is selected under `details`.

Keep selection-aware branches small and well-tested. Do not parse raw GraphQL query text to make performance decisions. If a child resolver needs a parent property that projections might omit, use `[IsProjected(true)]` on that parent property.

# Minimize Allocations in Hot Resolvers

Focus on allocation improvements after identifying a hot field. Start with readability and provider-side query shaping, then address measured allocation sources.

Common allocation fixes:

- Do not call `ToList()`, `ToArray()`, or `Select(...).ToList()` before paging, filtering, sorting, or projection middleware can translate the query
- Avoid per-item closures, reflection, dynamic dispatch, and repeated string formatting in hot nested resolvers
- Avoid building errors, log messages, or formatted strings unless needed
- Use `AsNoTracking()` for read-only EF Core queries
- Return `[]` or `Array.Empty<T>()` for empty arrays where appropriate
- Do not store DataLoader key lists or parent context lists beyond the method body
- Use pooled writers only for custom serialization or buffer-writing scenarios. See [Performance Tuning](/docs/hotchocolate/v16/guides/performance) before adding pooling complexity

Before:

```csharp
[UsePaging]
public static IEnumerable<Product> GetProducts(CatalogContext db)
    => db.Products
        .AsNoTracking()
        .Select(p => new Product { Id = p.Id, Name = p.Name })
        .ToList();
```

After:

```csharp
[UsePaging]
[UseProjection]
public static IQueryable<Product> GetProducts(CatalogContext db)
    => db.Products.AsNoTracking();
```

The improved resolver lets Hot Chocolate and the provider build the selected shape after the client supplies paging and field selections.

# Handle CPU-Heavy Resolver Work Safely

CPU-heavy work can dominate request latency even without I/O. Examples include image generation, document conversion, ML inference, encryption, normalization, and complex aggregation.

Avoid this pattern on fields that appear in lists:

```csharp
public static string GetThumbnailUrl([Parent] Product product)
{
    var bytes = ThumbnailGenerator.Render(product.ImageBytes);
    return Storage.WriteThumbnail(product.Id, bytes);
}
```

A better resolver reads precomputed state or queues work outside the request:

```csharp
public static ProductImage GetImage([Parent] Product product)
{
    return new ProductImage(
        product.ThumbnailUrl,
        product.ThumbnailStatus);
}
```

If CPU work must run during the request, bound concurrency in an application service, make cancellation cooperative, and avoid starting one CPU job per selected child field. Combine this with cost analysis and page-size limits so clients cannot multiply expensive fields without bounds.

# Use Streaming for Perceived Latency, Not Cheaper Resolvers

The `@defer` and `@stream` directives can improve time-to-first-byte for expensive or lower-priority selections. They do not reduce total resolver CPU, database work, or downstream service cost.

```graphql
query ProductPage {
  products(first: 10) {
    nodes {
      name
      ...DeferredDetails @defer {
        description
        recommendations {
          name
        }
      }
    }
  }
}
```

With streaming, the initial payload can arrive before the deferred details, depending on server configuration and the HTTP transport used. Deferred resolvers still execute, so they still require batching, cancellation, paging, and provider-side shaping.

Configure incremental delivery in server and transport settings. See Performance Tuning and HTTP Transport for details.

# Benchmark and Load Test Representative Resolver Paths

Verify resolver changes using the operation shapes your clients send. Include realistic variables, selection sets, authorization paths, page sizes, and data volume.

Track these signals:

| Metric                                | What it tells you                             | Likely fix                                                                        |
| ------------------------------------- | --------------------------------------------- | --------------------------------------------------------------------------------- |
| p95 and p99 latency                   | Whether tail latency improved                 | Remove blocking calls, batch I/O, reduce downstream calls                         |
| Allocations and GC                    | Whether hot fields allocate too much          | Avoid early materialization, per-item strings, reflection, or large object graphs |
| SQL, MongoDB, or Marten query count   | Whether provider work is bounded              | Return `IQueryable<T>`, `IExecutable<T>`, or `QueryContext<T>`                    |
| External call count                   | Whether resolvers fan out                     | Add DataLoader, batch endpoints, or request caching                               |
| DataLoader batch size and batch count | Whether batching works                        | Use one loader per lookup shape and avoid execution shapes that split batches     |
| Error and cancellation rate           | Whether failures cause retries or wasted work | Pass cancellation tokens and model domain errors intentionally                    |

Separate cold-start and warmup behavior from steady state. Warm caches intentionally when measuring steady state. Use microbenchmarks for isolated CPU or allocation hot spots, not end-to-end resolver behavior.

If your test infrastructure supports it, add integration tests that assert backend query counts or DataLoader batch counts for important nested selections.

# Anti-Patterns and Fixes

| Symptom                                                   | Likely cause                                     | Fix                                                                                                 |
| --------------------------------------------------------- | ------------------------------------------------ | --------------------------------------------------------------------------------------------------- |
| A nested field creates one query per parent               | N+1 resolver                                     | Use a source-generated DataLoader or batch resolver.                                                |
| Throughput collapses under concurrency                    | Blocking calls or sync-over-async                | Use async APIs with `CancellationToken`.                                                            |
| EF Core reports concurrent operations on the same context | Shared `DbContext` in parallel query fields      | Use resolver scope, DataLoader scope, `IDbContextFactory`, or provider integration.                 |
| Filtering or paging happens in memory                     | Resolver calls `ToList()` before data middleware | Return `IQueryable<T>`, `IExecutable<T>`, or `QueryContext<T>`.                                     |
| SQL does not include selected columns                     | Custom resolver prevents projection              | Move logic, use DataLoader, or mark required parent fields with `[IsProjected(true)]`.              |
| Large child collections overload memory                   | Unbounded relationship field                     | Add `[UsePaging]`, a paged DataLoader, or an external API `Connection<T>`.                          |
| Hot operation spends time in counts                       | `IncludeTotalCount` on a hot path                | Disable total count or make the count path explicit.                                                |
| Traces show middleware or auth dominates                  | Repeated per-field work                          | Cache or batch, move checks to a parent field where semantics allow it, or narrow middleware usage. |
| CPU usage spikes without extra I/O                        | Per-field CPU-heavy computation                  | Precompute, cache, queue, or use a bounded service.                                                 |
| Nullable fields fail repeatedly                           | Resolver throws or returns unexpected nulls      | Model expected domain errors, understand null propagation, and avoid retry storms.                  |
| Instrumentation slows the request                         | Too many scopes or synchronous handlers          | Reduce scopes, sample traces, and enqueue expensive diagnostic work.                                |

# Troubleshoot a Slow Resolver Report

Follow this process:

1. Reproduce the exact operation name, variables, authenticated user, request headers, and selected fields
2. Confirm data volume and page arguments
3. Enable tracing in a safe environment or sampled production path
4. Find the slow field path. Compare resolver duration with DataLoader batch duration and downstream spans
5. Check DataLoader batch size and number of batches. Many small batches can indicate missed batching or an execution shape that splits work
6. Inspect SQL, MongoDB, or Marten query shape. Check selected fields, filters, sorting, limits, and indexes
7. Search resolver code for `.Result`, `.Wait()`, `Thread.Sleep`, synchronous I/O, early materialization, CPU-heavy loops, and per-item service calls
8. Check DI scope, shared state, field middleware, authorization, and diagnostic listeners
9. Verify cancellation by aborting a request or using a short client timeout in a safe test
10. Re-run the representative operation and compare latency, allocations, backend call counts, and batch sizes

# Next Steps

- Write resolver signatures with parent values, cancellation, and service injection: Resolvers
- Batch and cache related data: DataLoader
- Shape database work: Projections, Pagination, Filtering, Sorting, and Fetching from Databases
- Tune backend integrations: Entity Framework, MongoDB, and Marten
- Fix resolver lifetimes: Dependency Injection
- Inspect traces and diagnostic events: Instrumentation, OpenTelemetry operations, and troubleshooting slow requests
- Bound expensive operations: Cost Analysis and Performance Tuning
- Understand middleware cost: Field Middleware and Authorization

A better resolver reads precomputed state or queues work outside the request:

```csharp
public static ProductImage GetImage([Parent] Product product)
{
    return new ProductImage(
        product.ThumbnailUrl,
        product.ThumbnailStatus);
}
```

If CPU work must run during the request, bound concurrency in an application service, make cancellation cooperative where possible, and avoid starting one CPU job per selected child field. Combine this with [cost analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis) and page-size limits so clients cannot multiply expensive fields without bounds.

# Use streaming for perceived latency, not cheaper resolvers

`@defer` and `@stream` can improve time-to-first-byte for expensive or lower-priority selections. They do not reduce total resolver CPU, database work, or downstream service cost by themselves.

```graphql
query ProductPage {
  products(first: 10) {
    nodes {
      name
      ...DeferredDetails @defer {
        description
        recommendations {
          name
        }
      }
    }
  }
}
```

Expected behavior: the initial payload can arrive before the deferred details, depending on server configuration and the HTTP transport selected by the client. The deferred resolvers still execute, so they still need batching, cancellation, paging, and provider-side shaping.

Configure incremental delivery in server and transport settings. See [Performance Tuning](/docs/hotchocolate/v16/guides/performance) and [HTTP Transport](/docs/hotchocolate/v16/server/http-transport) for the wire format and server options.

# Benchmark and load test representative resolver paths

Verify resolver changes with the operation shapes your clients send. Include realistic variables, selection sets, authorization paths, page sizes, and data volume.

Track these signals:

| Metric                                | What it tells you                             | Likely fix                                                                        |
| ------------------------------------- | --------------------------------------------- | --------------------------------------------------------------------------------- |
| p95 and p99 latency                   | Whether tail latency improved                 | Remove blocking calls, batch I/O, reduce downstream calls                         |
| Allocations and GC                    | Whether hot fields allocate too much          | Avoid early materialization, per-item strings, reflection, or large object graphs |
| SQL, MongoDB, or Marten query count   | Whether provider work is bounded              | Return `IQueryable<T>`, `IExecutable<T>`, or `QueryContext<T>`                    |
| External call count                   | Whether resolvers fan out                     | Add DataLoader, batch endpoints, or request caching                               |
| DataLoader batch size and batch count | Whether batching works                        | Use one loader per lookup shape and avoid execution shapes that split batches     |
| Error and cancellation rate           | Whether failures cause retries or wasted work | Pass cancellation tokens and model domain errors intentionally                    |

Separate cold-start and warmup behavior from steady state. Warm caches intentionally when measuring steady state. Use microbenchmarks for isolated CPU or allocation hot spots, not end-to-end resolver behavior.

When your test infrastructure supports it, add integration tests that assert backend query counts or DataLoader batch counts for important nested selections.

# Anti-patterns and fixes

| Symptom                                                   | Likely cause                                     | Fix                                                                                                 |
| --------------------------------------------------------- | ------------------------------------------------ | --------------------------------------------------------------------------------------------------- |
| A nested field creates one query per parent               | N+1 resolver                                     | Use a source-generated DataLoader or batch resolver.                                                |
| Throughput collapses under concurrency                    | Blocking calls or sync-over-async                | Use async APIs with `CancellationToken`.                                                            |
| EF Core reports concurrent operations on the same context | Shared `DbContext` in parallel query fields      | Use resolver scope, DataLoader scope, `IDbContextFactory`, or provider integration.                 |
| Filtering or paging happens in memory                     | Resolver calls `ToList()` before data middleware | Return `IQueryable<T>`, `IExecutable<T>`, or `QueryContext<T>`.                                     |
| SQL does not include selected columns                     | Custom resolver prevents projection              | Move logic, use DataLoader, or mark required parent fields with `[IsProjected(true)]`.              |
| Large child collections overload memory                   | Unbounded relationship field                     | Add `[UsePaging]`, a paged DataLoader, or an external API `Connection<T>`.                          |
| Hot operation spends time in counts                       | `IncludeTotalCount` on a hot path                | Disable total count or make the count path explicit.                                                |
| Traces show middleware or auth dominates                  | Repeated per-field work                          | Cache or batch, move checks to a parent field where semantics allow it, or narrow middleware usage. |
| CPU usage spikes without extra I/O                        | Per-field CPU-heavy computation                  | Precompute, cache, queue, or use a bounded service.                                                 |
| Nullable fields fail repeatedly                           | Resolver throws or returns unexpected nulls      | Model expected domain errors, understand null propagation, and avoid retry storms.                  |
| Instrumentation slows the request                         | Too many scopes or synchronous handlers          | Reduce scopes, sample traces, and enqueue expensive diagnostic work.                                |

# Troubleshoot a slow resolver report

Use this ordered flow:

1. Reproduce the exact operation name, variables, authenticated user shape, request headers, and selected fields.
2. Confirm data volume and page arguments.
3. Enable tracing in a safe environment or sampled production path.
4. Find the slow field path. Compare resolver duration with DataLoader batch duration and downstream spans.
5. Check DataLoader batch size and number of batches. Many small batches can indicate missed batching or an execution shape that splits work.
6. Inspect SQL, MongoDB, or Marten query shape. Check selected fields, filters, sorting, limits, and indexes.
7. Search resolver code for `.Result`, `.Wait()`, `Thread.Sleep`, synchronous I/O, early materialization, CPU-heavy loops, and per-item service calls.
8. Check DI scope, shared state, field middleware, authorization, and diagnostic listeners.
9. Verify cancellation by aborting a request or using a short client timeout in a safe test.
10. Re-run the representative operation and compare latency, allocations, backend call counts, and batch sizes.

# Next steps

- Write resolver signatures with parent values, cancellation, and service injection: [Resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers).
- Batch and cache related data: [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader).
- Shape database work: [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections), [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination), [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering), [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting), and [Fetching from Databases](/docs/hotchocolate/v16/resolvers-and-data/fetching-from-databases).
- Tune backend integrations: [Entity Framework](/docs/hotchocolate/v16/integrations/entity-framework), [MongoDB](/docs/hotchocolate/v16/integrations/mongodb), and [Marten](/docs/hotchocolate/v16/integrations/marten).
- Fix resolver lifetimes: [Dependency Injection](/docs/hotchocolate/v16/resolvers-and-data/dependency-injection).
- Inspect traces and diagnostic events: [Instrumentation](/docs/hotchocolate/v16/server/instrumentation), [OpenTelemetry operations](/docs/hotchocolate/v16/operations/observability/opentelemetry), and [troubleshooting slow requests](/docs/hotchocolate/v16/operations/observability/troubleshoot-slow-requests).
- Bound expensive operations: [Cost Analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis) and [Performance Tuning](/docs/hotchocolate/v16/guides/performance).
- Understand middleware cost: [Field Middleware](/docs/hotchocolate/v16/execution-engine/field-middleware) and [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization).
