---
title: "DataLoader Performance"
---

This page helps you operate DataLoaders in Hot Chocolate v16 production systems. Use it when an operation still creates N+1 source calls, batches are too small, batches are too large, or request memory grows with high-cardinality queries.

It assumes you already understand resolvers and the basic DataLoader pattern. If you need to create loaders first, start with [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader). Fusion composition and gateway behavior are out of scope for this page.

# Prerequisites

Before you tune DataLoader behavior, make sure you have:

- A Hot Chocolate v16 server.
- DataLoaders registered through the source generator or DI.
- A representative GraphQL operation that shows the problem under production-like concurrency.
- Access to source metrics for your database, REST service, MongoDB cluster, Marten store, or other upstream.
- A safe telemetry environment. DataLoader keys can contain tenant IDs, user IDs, or other sensitive values.

Prefer source-generated DataLoaders for most application code. Use manual DataLoader classes when you need custom constructor behavior, custom options, or a specialized implementation.

# Start by measuring a suspected N+1 operation

Enable Hot Chocolate instrumentation and OpenTelemetry first. Then add a small DataLoader diagnostic listener for counters that you want to aggregate per operation or per loader.

```csharp
// Program.cs
using OpenTelemetry.Resources;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQL()
    .AddTypes()
    .AddInstrumentation()
    .AddDiagnosticEventListener<DataLoaderMetricsListener>();

builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("Catalog.Api"))
    .WithMetrics(m =>
    {
        m.AddMeter("Catalog.GraphQL.DataLoaders");
        m.AddOtlpExporter();
    })
    .WithTracing(t =>
    {
        t.AddAspNetCoreInstrumentation();
        t.AddHttpClientInstrumentation();
        t.AddHotChocolateInstrumentation();
        t.AddOtlpExporter();
    });
```

Hot Chocolate creates DataLoader spans with the batch size. You can include keys while debugging, but only in a safe environment:

```csharp
builder
    .AddGraphQL()
    .AddInstrumentation(o =>
    {
        o.IncludeDataLoaderKeys = true;
    });
```

`IncludeDataLoaderKeys` can leak sensitive data and create high-cardinality telemetry. Turn it off after you verify duplicate-key behavior.

Add a listener for inexpensive counters and histograms. Diagnostic event handlers run synchronously during request execution, so keep the handler cheap and export data through your telemetry provider.

```csharp
// Diagnostics/DataLoaderMetricsListener.cs
using System.Diagnostics;
using System.Diagnostics.Metrics;
using GreenDonut;

public sealed class DataLoaderMetricsListener : DataLoaderDiagnosticEventListener
{
    private static readonly Meter Meter = new("Catalog.GraphQL.DataLoaders");
    private static readonly Counter<long> CacheHits = Meter.CreateCounter<long>(
        "graphql.dataloader.cache_hits");
    private static readonly Counter<long> BatchErrors = Meter.CreateCounter<long>(
        "graphql.dataloader.batch_errors");
    private static readonly Counter<long> BatchItemErrors = Meter.CreateCounter<long>(
        "graphql.dataloader.batch_item_errors");
    private static readonly Counter<long> BatchResultsCounter = Meter.CreateCounter<long>(
        "graphql.dataloader.batch_results");
    private static readonly Histogram<int> BatchSize = Meter.CreateHistogram<int>(
        "graphql.dataloader.batch_size");
    private static readonly Histogram<double> BatchDuration = Meter.CreateHistogram<double>(
        "graphql.dataloader.batch_duration_ms");

    public override IDisposable ExecuteBatch<TKey>(
        IDataLoader dataLoader,
        IReadOnlyList<TKey> keys)
    {
        var loaderName = dataLoader.GetType().Name;
        BatchSize.Record(keys.Count, new("loader", loaderName));

        return new BatchScope(loaderName);
    }

    public override void ResolvedTaskFromCache(
        IDataLoader dataLoader,
        PromiseCacheKey cacheKey,
        Task task)
    {
        CacheHits.Add(1, new("loader", dataLoader.GetType().Name));
    }

    public override void BatchError<TKey>(IReadOnlyList<TKey> keys, Exception error)
    {
        BatchErrors.Add(1);
    }

    public override void BatchResults<TKey, TValue>(
        IReadOnlyList<TKey> keys,
        ReadOnlySpan<Result<TValue?>> values)
        where TValue : default
    {
        BatchResultsCounter.Add(1);
    }

    public override void BatchItemError<TKey>(TKey key, Exception error)
    {
        BatchItemErrors.Add(1);
    }

    private sealed class BatchScope(string loaderName) : IDisposable
    {
        private readonly long _started = Stopwatch.GetTimestamp();

        public void Dispose()
        {
            BatchDuration.Record(
                Stopwatch.GetElapsedTime(_started).TotalMilliseconds,
                new("loader", loaderName));
        }
    }
}
```

Use the metrics to answer these questions:

| Signal                   | Healthy result                               | What to investigate                                                  |
| ------------------------ | -------------------------------------------- | -------------------------------------------------------------------- |
| Source calls per request | Close to expected batch count                | Resolver bypasses DataLoader or the batch method loops per key       |
| Keys per batch p50/p95   | Matches operation fan-out and backend limits | Tiny batches or oversized batches                                    |
| Cache hits per request   | Repeated keys resolve from cache             | Volatile keys, fragmented lookup shapes, or disabled cache           |
| Batch duration p95/p99   | Below your backend SLO                       | Slow SQL plans, REST rate limits, missing indexes, oversized batches |
| `BatchError` count       | Zero under normal load                       | Source outage or whole-batch failure                                 |
| `BatchItemError` count   | Only expected per-key failures               | Bad keys or partial upstream failures                                |

For example, this query requests five products and their brands:

```graphql
query ProductBrands {
  products(first: 5) {
    nodes {
      id
      name
      brand {
        id
        name
      }
    }
  }
}
```

If the five products reference brand IDs `[1, 2, 1, 3, 2]`, the expected DataLoader result is one brand batch with three unique keys, not five brand source calls.

```text
Expected debug signal
BrandByIdDataLoader batches/request: 1
BrandByIdDataLoader keys/batch: 3
Brand source calls/request: 1
```

# Use the request-scoped cache mental model

A DataLoader is a request coordination mechanism. It is not a database abstraction and it is not a shared application cache.

During one GraphQL request, resolvers call `LoadAsync`. Each call queues a key and returns a task. Hot Chocolate dispatches the batch later, after the current resolver wave has queued its work. The DataLoader deduplicates keys, sends one source call for the unique keys in that batch, and shares the resulting task with every caller that requested the same key.

```text
Wave 1: products(first: 5) returns products
Wave 2: brand resolvers call LoadAsync with [1, 2, 1, 3, 2]
Dispatch: BrandByIdDataLoader executes one batch for [1, 2, 3]
Wave 3: brand resolvers receive cached tasks/results
```

Each GraphQL request gets fresh DataLoader and cache state. Duplicate keys inside the same request share one task or result. A later request starts with an empty DataLoader cache unless you add a separate second-level cache.

For one-to-one loaders, missing keys resolve as `null` unless the loader returns a per-key error. For one-to-many loaders, return an empty collection in the resolver or use `LoadOrEmptyAsync` when the generated loader supports it.

# Design lookup shapes that batch well

A lookup shape is the complete set of inputs that changes a result. Create one DataLoader per lookup shape and keep the keys stable and comparable.

```csharp
// DataLoaders/BrandDataLoaders.cs
internal static class BrandDataLoaders
{
    [DataLoader]
    public static async Task<Dictionary<int, Brand>> GetBrandByIdAsync(
        IReadOnlyList<int> brandIds,
        CatalogContext db,
        CancellationToken ct)
        => await db.Brands
            .AsNoTracking()
            .Where(b => brandIds.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, ct);
}
```

Inject the generated interface into resolvers or services:

```csharp
// Types/ProductNode.cs
[ObjectType<Product>]
public static partial class ProductNode
{
    public static async Task<Brand?> GetBrandAsync(
        [Parent] Product product,
        IBrandByIdDataLoader brandsById,
        CancellationToken ct)
        => await brandsById.LoadAsync(product.BrandId, ct);
}
```

Keep result-changing arguments in the loader shape. A paged `ProductsByBrand` lookup has more inputs than `brandId`, because paging, projection, filtering, and sorting change the result.

```csharp
// DataLoaders/ProductDataLoaders.cs
using GreenDonut.Data;

internal static class ProductDataLoaders
{
    [DataLoader]
    public static async Task<Dictionary<int, Page<Product>>> GetProductsByBrandAsync(
        IReadOnlyList<int> brandIds,
        PagingArguments pagingArguments,
        QueryContext<Product> queryContext,
        CatalogContext db,
        CancellationToken ct)
        => await db.Products
            .AsNoTracking()
            .Where(p => brandIds.Contains(p.BrandId))
            .With(queryContext)
            .ToBatchPageAsync(p => p.BrandId, pagingArguments, ct);
}
```

Use the generated branch with the same arguments at the call site:

```csharp
// Services/ProductService.cs
using GreenDonut.Data;

public sealed class ProductService(IProductsByBrandDataLoader productsByBrand)
{
    public async Task<Page<Product>> GetProductsByBrandAsync(
        int brandId,
        PagingArguments pagingArguments,
        QueryContext<Product>? queryContext,
        CancellationToken ct)
        => await productsByBrand
            .With(pagingArguments, queryContext ?? QueryContext<Product>.Empty)
            .LoadOrEmptyAsync(brandId, ct);
}
```

Follow these design rules:

- Use `BrandById`, `ProductsByBrand`, and `PricesByProductId` as separate loaders.
- Include every argument that changes the result. Do not hide a result-changing value in ambient state.
- Keep unrelated data out of the same loader.
- Keep data access out of field resolvers. Resolvers should translate GraphQL selection into application calls.
- Use loaders for nested connections and aggregations, not only entity-by-ID fields.

# Tune maximum batch size for the backend

`DataLoaderOptions.MaxBatchSize` defaults to `1024`. When more unique keys are queued, GreenDonut splits work into multiple batches. Setting `MaxBatchSize` to `0` disables DataLoader batch splitting, but use that only when the operation and backend are bounded.

Lower the maximum when the source has tighter limits. Raise it only when metrics show excessive splitting and the backend handles larger batches efficiently.

| Backend signal                                     | Starting decision                                                    |
| -------------------------------------------------- | -------------------------------------------------------------------- |
| SQL parameter limit or poor `IN` query plan        | Set the batch size below the parameter limit and inspect query plans |
| REST batch endpoint returns `413` or `429`         | Set the batch size below payload and rate-limit thresholds           |
| MongoDB or Marten slow query or document-size risk | Reduce batch size and inspect indexes/query shape                    |
| High request memory with many unique keys          | Reduce batch size and add query limits                               |
| Many small split batches with low source latency   | Consider raising the batch size after load testing                   |

For manual loaders, accept the non-null `DataLoaderOptions` from DI and pass it to the base class:

```csharp
// DataLoaders/ProductByIdDataLoader.cs
public sealed class ProductByIdDataLoader : BatchDataLoader<int, Product>
{
    private readonly IDbContextFactory<CatalogContext> _dbContextFactory;

    public ProductByIdDataLoader(
        IDbContextFactory<CatalogContext> dbContextFactory,
        IBatchScheduler batchScheduler,
        DataLoaderOptions options)
        : base(batchScheduler, options)
    {
        _dbContextFactory = dbContextFactory;
    }

    protected override async Task<IReadOnlyDictionary<int, Product>> LoadBatchAsync(
        IReadOnlyList<int> keys,
        CancellationToken ct)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(ct);

        return await db.Products
            .AsNoTracking()
            .Where(p => keys.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);
    }
}
```

The source-generated `[DataLoader]` attribute does not expose `MaxBatchSize`. If you need per-loader batch-size control, use a manual loader or a verified DI customization that preserves the default cache and diagnostic events.

# Understand dispatch timing before changing it

Resolvers run in waves. Calls to `LoadAsync` queue keys until execution needs the queued DataLoader work. The batch dispatcher can also force age-based dispatch. The effective default `BatchDispatcherOptions.MaxBatchWaitTimeUs` is `50_000` microseconds.

You can configure the dispatcher explicitly:

```csharp
// Program.cs
builder
    .AddGraphQL()
    .AddDefaultBatchDispatcher(
        new BatchDispatcherOptions
        {
            MaxBatchWaitTimeUs = 50_000
        });
```

Use dispatch timing as a final tuning lever. Under-batching is often caused by resolver shape, serialized awaits, direct source calls, or fragmented keys.

```text
Good batching
product resolvers start together
  -> each calls BrandById.LoadAsync(...)
  -> one brand batch dispatches

Fragmented batching
resolver loops over products
  -> await BrandById.LoadAsync(product1.BrandId)
  -> await BrandById.LoadAsync(product2.BrandId)
  -> several small batches can dispatch
```

Longer waits can increase request latency. Shorter waits can reduce batch size. Change the dispatcher only after your metrics show that resolver shape and lookup design are correct.

# Keep EF Core loaders safe under concurrency

EF Core `DbContext` instances are not thread-safe. Hot Chocolate's default query and DataLoader service scope behavior gives each DataLoader execution separate scoped services, which protects scoped services that cannot be shared across concurrent resolver work.

For source-generated loaders, use `AsNoTracking()` for read-heavy paths and request a DataLoader scope when you need a dedicated loader scope:

```csharp
internal static class BrandDataLoaders
{
    [DataLoader(ServiceScope = DataLoaderServiceScope.DataLoaderScope)]
    public static async Task<Dictionary<int, Brand>> GetBrandByIdAsync(
        IReadOnlyList<int> brandIds,
        CatalogContext db,
        CancellationToken ct)
        => await db.Brands
            .AsNoTracking()
            .Where(b => brandIds.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, ct);
}
```

For manual EF loaders, inject `IDbContextFactory<T>` and create the context inside `LoadBatchAsync`:

```csharp
protected override async Task<IReadOnlyDictionary<int, Product>> LoadBatchAsync(
    IReadOnlyList<int> keys,
    CancellationToken ct)
{
    await using var db = await _dbContextFactory.CreateDbContextAsync(ct);

    return await db.Products
        .AsNoTracking()
        .Where(p => keys.Contains(p.Id))
        .ToDictionaryAsync(p => p.Id, ct);
}
```

Expected result: no `A second operation started on this context before a previous operation completed` errors, fewer tracked entities, and lower per-request memory for read paths.

# Apply the same batching rules to other upstreams

MongoDB, Marten, REST, and other sources still need stable DataLoader keys and bounded batch sizes. Use each integration page for query shaping, filtering, sorting, projections, paging, and executable behavior:

- [MongoDB](/docs/hotchocolate/v16/integrations/mongodb)
- [Marten](/docs/hotchocolate/v16/integrations/marten)
- [Fetching from REST](/docs/hotchocolate/v16/resolvers-and-data/fetching-from-rest)

Do not write a batch method that loops over keys and sends one upstream call per key:

```csharp
// Anti-pattern: this keeps the N+1 behavior inside the DataLoader.
foreach (var key in keys)
{
    products.Add(await client.GetByIdAsync(key, ct));
}
```

Use an upstream batch API instead:

```csharp
// DataLoaders/ProductByIdDataLoader.cs
public sealed class ProductByIdDataLoader : BatchDataLoader<int, ProductDto>
{
    private readonly ProductClient _client;

    public ProductByIdDataLoader(
        ProductClient client,
        IBatchScheduler batchScheduler,
        DataLoaderOptions options)
        : base(batchScheduler, options)
    {
        _client = client;
    }

    protected override async Task<IReadOnlyDictionary<int, ProductDto>> LoadBatchAsync(
        IReadOnlyList<int> keys,
        CancellationToken ct)
    {
        var products = await _client.GetByIdsAsync(keys, ct);
        return products.ToDictionary(p => p.Id);
    }
}
```

Cap batch size for backend query constraints, payload limits, document-size limits, or rate limits. Preserve the cancellation token. Decide whether partial upstream failures should become per-key errors or fail the whole batch.

# Handle batch errors and cancellation deliberately

Failure behavior affects both client errors and operator dashboards.

| Behavior                                      | Diagnostic event        | Effect                                                                                 |
| --------------------------------------------- | ----------------------- | -------------------------------------------------------------------------------------- |
| The batch method throws                       | `BatchError`            | The whole batch fails, affected promises fault, and affected cache entries are removed |
| One key returns a `Result<T>` error           | `BatchItemError`        | That key fails while other keys can succeed                                            |
| Request cancellation occurs before completion | Pending promises cancel | Source calls should observe the same `CancellationToken`                               |

Throw when the source cannot answer the batch, for example a database outage. Use per-key errors when individual keys are invalid and the rest of the batch can succeed.

```csharp
protected override async Task<IReadOnlyDictionary<int, Product>> LoadBatchAsync(
    IReadOnlyList<int> keys,
    CancellationToken ct)
{
    // Throwing here fails the whole batch.
    var products = await _client.GetByIdsAsync(keys, ct);
    return products.ToDictionary(p => p.Id);
}
```

Pass `CancellationToken` through to EF Core, MongoDB, Marten, REST, and any other upstream. Do not convert cancellation into successful `null` values. Log the loader name, source, operation name, and whether the failure was a whole-batch error or a per-key error.

# Control memory and cache lifetime

The request cache retains tasks and results until the GraphQL request ends. Memory grows with the number of unique keys and the size of the loaded values. Large batches also allocate larger key and result buffers.

The `IReadOnlyList<TKey>` passed to a source-generated batch method or `LoadBatchAsync` is rented. Do not store it in a field, capture it in a background task, or use it after the method returns.

```csharp
// Anti-pattern: keys can be reused after the method returns.
private IReadOnlyList<int>? _lastKeys;

protected override Task<IReadOnlyDictionary<int, Product>> LoadBatchAsync(
    IReadOnlyList<int> keys,
    CancellationToken ct)
{
    _lastKeys = keys;
    return LoadProductsAsync(keys, ct);
}
```

If you need a stable copy for work inside the method, copy only what you need:

```csharp
protected override async Task<IReadOnlyDictionary<int, Product>> LoadBatchAsync(
    IReadOnlyList<int> keys,
    CancellationToken ct)
{
    var ids = keys.ToArray();

    return await db.Products
        .AsNoTracking()
        .Where(p => ids.Contains(p.Id))
        .ToDictionaryAsync(p => p.Id, ct);
}
```

Use a second-level cache only with explicit eviction, size, privacy, and consistency rules. For untrusted high-cardinality operations, combine DataLoader tuning with [cost analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis), pagination limits, and [trusted documents](/docs/hotchocolate/v16/performance/trusted-documents).

# Load test DataLoader behavior before and after tuning

Use representative GraphQL documents, not only synthetic single-field queries. Capture a baseline, change one variable, and compare the same signals again under production-like concurrency.

| Scenario            | Why it matters                                  | Expected DataLoader signal                              |
| ------------------- | ----------------------------------------------- | ------------------------------------------------------- |
| Repeated keys       | Verifies request-scoped deduplication           | Cache hits increase, source calls stay low              |
| Mostly unique keys  | Exposes high-cardinality memory and source load | Cache hits stay low, keys/batch approaches fan-out      |
| Nested connections  | Exposes loader shape and paging branches        | Batches align with each relationship and page arguments |
| Nested aggregations | Exposes expensive fan-out under metadata fields | Aggregation loader batches by parent IDs                |
| REST batch endpoint | Exposes payload and rate-limit boundaries       | No `413`, no `429`, p95 batch latency under SLO         |

A useful acceptance target is specific:

```text
BrandByIdDataLoader p95 batch size: 20 to 500
Brand REST endpoint: no 429 or 413 responses
GraphQL request p95: under 250 ms at target concurrency
BatchError and BatchItemError: zero except expected bad-key tests
```

Watch request p50/p95/p99 latency, batch duration p95, source calls per request, keys per batch distribution, cache hits, memory, backend CPU/IO, and error rate.

# Troubleshoot missed batches and remaining N+1 behavior

Use this checklist when you still see N+1 source calls after adding DataLoaders.

| Symptom                            | Metric                            | Likely cause                                            | Fix                                                           |
| ---------------------------------- | --------------------------------- | ------------------------------------------------------- | ------------------------------------------------------------- |
| One source call per parent         | Source calls equal parent count   | Batch method loops over keys                            | Replace per-key source calls with one batch query or endpoint |
| No cache hits for repeated IDs     | Cache hits stay near zero         | Key includes volatile values                            | Use stable key fields and sorted argument values              |
| Many loader names for same data    | Batches split by type             | Lookup shape fragmentation                              | Consolidate equivalent lookup shapes                          |
| Tiny batches under nested list     | Keys/batch near one               | Resolver awaits serially or bypasses loader             | Queue loader calls through injected interfaces                |
| DataLoader not reused in a request | Cache never hits                  | Loader constructed manually in resolver                 | Inject the generated interface or DI-registered loader        |
| Direct database calls remain       | Source traces show resolver calls | Resolver bypasses DataLoader on one path                | Route all nested lookup paths through the loader              |
| Duplicate work across pages        | Batches differ by arguments       | Pagination or selection creates different result shapes | Treat each distinct page/selection as a distinct shape        |

Before:

```csharp
public static async Task<Brand?> GetBrandAsync(
    [Parent] Product product,
    CatalogContext db,
    CancellationToken ct)
    => await db.Brands.FirstOrDefaultAsync(b => b.Id == product.BrandId, ct);
```

After:

```csharp
public static async Task<Brand?> GetBrandAsync(
    [Parent] Product product,
    IBrandByIdDataLoader brandsById,
    CancellationToken ct)
    => await brandsById.LoadAsync(product.BrandId, ct);
```

Use `IncludeDataLoaderKeys` temporarily to verify duplicate keys. Disable it after the investigation.

# Troubleshoot oversized batches and slow sources

A DataLoader prevents N+1 source calls. It does not make expensive operations cheap. One oversized batch can be slower than several smaller batches.

| Symptom                              | Likely cause                           | Fix                                                                              |
| ------------------------------------ | -------------------------------------- | -------------------------------------------------------------------------------- |
| High p95 batch duration              | Batch exceeds backend sweet spot       | Lower `MaxBatchSize` and inspect source query plans                              |
| SQL timeouts or plan regressions     | Large `IN` list or missing index       | Add indexes, reduce batch size, inspect generated SQL                            |
| MongoDB or Marten slow queries       | Query shape scans too much data        | Add indexes and use provider filtering, sorting, projection, and paging guidance |
| REST `413`                           | Payload too large                      | Lower batch size or change endpoint contract                                     |
| REST `429`                           | Rate limit exceeded                    | Lower batch size, add backoff upstream, or reduce operation fan-out              |
| Memory spikes                        | High unique key count and large values | Add query limits, reduce batch size, review trusted operations                   |
| Low cache hit rate with huge batches | Mostly unique keys                     | Use cost analysis and paging to bound fan-out                                    |

Validate fixes with the same load test that exposed the problem.

# Verify the outcome

After each change, verify that:

- The target operation still returns the same GraphQL result.
- Source calls per request match the expected number of batches.
- Repeated keys produce cache hits inside a request.
- Batch size p95 stays below backend limits.
- Batch duration and request latency stay under your SLO.
- Batch errors, item errors, and cancellation behavior are visible in telemetry.
- EF Core loaders do not produce concurrent `DbContext` operation errors.
- Memory remains bounded for high-cardinality operations.

# Decide what to read next

- Create loaders: [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader)
- Diagnose N+1 before tuning: [Diagnose N+1](/docs/hotchocolate/v16/build/data/diagnose-n-plus-one)
- Improve resolver work that is slow without fan-out: [Resolver Performance](/docs/hotchocolate/v16/operations/performance/resolver-performance)
- Add tracing and diagnostic events: [OpenTelemetry](/docs/hotchocolate/v16/operations/observability/opentelemetry), [Metrics](/docs/hotchocolate/v16/operations/observability/metrics), and [Diagnostics Events](/docs/hotchocolate/v16/operations/observability/diagnostics-events)
- Use EF Core safely: [Entity Framework Core](/docs/hotchocolate/v16/integrations/entity-framework)
- Tune MongoDB query shaping: [MongoDB](/docs/hotchocolate/v16/integrations/mongodb)
- Tune Marten query shaping: [Marten](/docs/hotchocolate/v16/integrations/marten)
- Bound high-cardinality operations: [Query Limits](/docs/hotchocolate/v16/operations/security-hardening/query-limits), [Trusted Documents](/docs/hotchocolate/v16/operations/security-hardening/trusted-documents), and [Caching](/docs/hotchocolate/v16/operations/performance/caching)
