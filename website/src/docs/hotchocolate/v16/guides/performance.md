---
title: "Performance Tuning"
---

Hot Chocolate is designed for high throughput out of the box. The schema is built eagerly at startup, operations are cached after their first execution, and DataLoaders batch database calls automatically. This guide covers the tuning options available when you need to go further.

# Warmup

The schema is constructed eagerly at startup by default. You can go a step further and register warmup tasks that pre-populate in-memory caches before the server starts accepting traffic. This eliminates cold-start latency for the first requests after deployment.

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddWarmupTask(async (executor, cancellationToken) =>
    {
        var request = OperationRequestBuilder.New()
            .SetDocument("query GetProducts { products(first: 10) { nodes { id name } } }")
            .SetOperationName("GetProducts")
            .MarkAsWarmupRequest()
            .Build();

        await executor.ExecuteAsync(request, cancellationToken);
    });
```

`MarkAsWarmupRequest()` populates the document and operation caches without executing the operation, which avoids side effects during startup. Include the operation name in the warmup request because it is part of the cache key.

[Learn more about server warmup](/docs/hotchocolate/v16/server/warmup)

# Operation Caching

Hot Chocolate caches parsed and compiled operations so that repeated requests skip parsing and validation. Two cache sizes control this behavior:

- `PreparedOperationCacheSize` controls the compiled operation cache (default `256`, minimum `16`).
- `OperationDocumentCacheSize` controls the parsed document cache (default `256`, minimum `16`).

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .ModifyOptions(options =>
    {
        options.PreparedOperationCacheSize = 1024;
        options.OperationDocumentCacheSize = 1024;
    });
```

In v16, each cache is scoped to a single schema instance. If your application hosts multiple schemas, each schema maintains its own caches.

For APIs with a known set of operations, consider using [persisted operations](/docs/hotchocolate/v16/performance/trusted-documents) to eliminate parsing and validation entirely.

# DataLoader Batching

DataLoaders collect individual fetch requests during resolver execution and dispatch them as a single batch. This turns N+1 database queries into one query per batch. Hot Chocolate manages DataLoader lifecycle automatically within each request scope.

The default `MaxBatchSize` for DataLoaders is `1024`. If your data source has a lower limit on batch sizes (for example, a SQL `IN` clause limit), you can adjust this through `DataLoaderOptions` when creating a manual DataLoader class:

```csharp
// DataLoaders/ProductByIdDataLoader.cs
public class ProductByIdDataLoader : BatchDataLoader<int, Product>
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
        CancellationToken cancellationToken)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Products
            .Where(p => keys.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);
    }
}
```

For most applications, the source-generated DataLoader approach (using the `[DataLoader]` attribute) is the recommended starting point.

[Learn more about DataLoaders](/docs/hotchocolate/v16/resolvers-and-data/dataloader)

# Projections and Database Efficiency

Use `[UseProjection]` to translate GraphQL field selections into database-level `SELECT` clauses. When a client requests only `name` and `email`, Hot Chocolate queries only those columns from the database rather than loading entire entities.

```csharp
// Types/UserQueries.cs
[QueryType]
public static partial class UserQueries
{
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<User> GetUsers(CatalogContext db)
        => db.Users;
}
```

Combine `[UseProjection]` with `[UseFiltering]` and `[UseSorting]` to push filtering and ordering down to the database as well. Apply them in this order: `UsePaging` > `UseProjection` > `UseFiltering` > `UseSorting`.

As an alternative to middleware stacking, `QueryContext<T>` integrates projection, filtering, and sorting into a single return type:

```csharp
// Types/UserQueries.cs
[QueryType]
public static partial class UserQueries
{
    public static QueryContext<User> GetUsers(CatalogContext db)
        => db.Users.AsQueryContext();
}
```

Do not combine `QueryContext<T>` with `[UseProjection]` on the same field. The HC0099 analyzer warns when both are present.

[Learn more about projections](/docs/hotchocolate/v16/resolvers-and-data/projections)

# Cost Analysis for Resource Protection

Cost analysis calculates the cost of every operation before execution and rejects operations that exceed your budget. Even on private APIs, cost analysis protects against accidentally expensive operations during development. It catches runaway queries before they reach production.

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .ModifyCostOptions(options =>
    {
        options.MaxFieldCost = 5_000;
        options.MaxTypeCost = 5_000;
        options.EnforceCostLimits = true;
    });
```

Use the `GraphQL-Cost: report` HTTP header to inspect the cost of any operation without changing enforcement. Send your most complex expected operations and verify they fall within your limits.

[Learn more about cost analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis)

# Reduce Response Size

Large responses increase serialization time, network transfer time, and client parsing time. Two features help you deliver data incrementally.

## Incremental Delivery with `@defer` and `@stream`

`@defer` lets clients mark fragments that can arrive after the initial response. `@stream` lets clients receive list items incrementally. Both reduce time-to-first-byte for operations that include expensive or low-priority fields.

Enable these directives in schema options:

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .ModifyOptions(options =>
    {
        options.EnableDefer = true;
        options.EnableStream = true;
    });
```

In v16, the default incremental delivery wire format is v0.2, which uses `pending`, `incremental`, and `completed` fields to track deferred fragments.

[Learn more about incremental delivery](/docs/hotchocolate/v16/server/http-transport)

## Persisted Operations

Persisted operations reduce request size by replacing the full operation document with a hash. This saves bandwidth on every request and lets the server skip parsing for known operations.

[Learn more about persisted operations](/docs/hotchocolate/v16/performance/trusted-documents)

# Instrumentation for Bottleneck Detection

Use OpenTelemetry to find slow resolvers and DataLoaders. Hot Chocolate ships with a built-in OpenTelemetry integration aligned with the proposed GraphQL semantic conventions.

For custom diagnostics, implement a diagnostic event listener:

```csharp
// Diagnostics/PerformanceEventListener.cs
public class PerformanceEventListener : ExecutionDiagnosticEventListener
{
    private readonly ILogger<PerformanceEventListener> _logger;

    public PerformanceEventListener(ILogger<PerformanceEventListener> logger)
        => _logger = logger;

    public override IDisposable ExecuteRequest(RequestContext context)
    {
        var start = DateTime.UtcNow;

        return new RequestScope(_logger, context, start);
    }

    private sealed class RequestScope(
        ILogger logger,
        RequestContext context,
        DateTime start) : IDisposable
    {
        public void Dispose()
        {
            var elapsed = DateTime.UtcNow - start;
            if (elapsed > TimeSpan.FromMilliseconds(500))
            {
                logger.LogWarning(
                    "Slow request detected: {Document} took {Elapsed}ms",
                    context.Request.Document,
                    elapsed.TotalMilliseconds);
            }
        }
    }
}
```

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddDiagnosticEventListener<PerformanceEventListener>();
```

Diagnostic event handlers execute synchronously as part of the GraphQL request. Enqueue expensive work (such as writing to an external monitoring service) to a background service to avoid adding latency.

[Learn more about instrumentation](/docs/hotchocolate/v16/server/instrumentation)

# Next Steps

- **Server warmup:** [Warmup](/docs/hotchocolate/v16/server/warmup) covers custom warmup tasks and lazy initialization.
- **Persisted operations:** [Persisted Operations](/docs/hotchocolate/v16/performance/trusted-documents) covers both pre-stored and automatic persisted operations.
- **DataLoaders:** [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader) covers source-generated DataLoaders, manual DataLoader classes, and batch resolvers.
- **Projections:** [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections) covers the `[UseProjection]` middleware and `QueryContext<T>`.
- **Cost analysis:** [Cost Analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis) covers custom weights, filtering and sorting costs, and the tuning guide.
- **Instrumentation:** [Instrumentation](/docs/hotchocolate/v16/server/instrumentation) covers diagnostic event listeners and OpenTelemetry integration.
- **Configuration reference:** [Options](/docs/hotchocolate/v16/api-reference/options) lists all schema, request, and server options with their defaults.
