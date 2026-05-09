---
title: Troubleshoot slow requests
---

Use this runbook when a Hot Chocolate v16 request is slow in production and you need to find the phase, field, or dependency that owns the latency. The workflow starts with a reported operation, uses low-cardinality telemetry to split request time, then routes you to the smallest safe fix.

This page covers Hot Chocolate v16 server operations. Fusion gateway tracing and distributed subgraph troubleshooting use separate diagnostics concepts and belong on Fusion-specific pages.

# Prerequisites

You need these pieces before you can troubleshoot production latency with confidence:

- A Hot Chocolate v16 server mapped with `app.MapGraphQL()`.
- The `HotChocolate.Diagnostics` package.
- Hot Chocolate instrumentation enabled with `.AddInstrumentation()`.
- OpenTelemetry tracing with `.AddHotChocolateInstrumentation()`, `.AddAspNetCoreInstrumentation()`, `.AddHttpClientInstrumentation()`, and provider-specific database instrumentation for your stack.
- Logs or trace resources that include trace ID, span ID, service name, deployment version, and environment.
- Named client operations. If clients use trusted documents or automatic persisted operations, collect the document ID or hash.
- A safe way to reproduce the operation with sanitized variables and the same paging, filtering, sorting, and authorization inputs.

Start with production-safe tracing:

```csharp
using HotChocolate.Diagnostics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddInstrumentation(options =>
    {
        options.RequestDetails = RequestDetails.Default;
    });

builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resource =>
    {
        resource
            .AddService(builder.Environment.ApplicationName)
            .AddAttributes(
            [
                new KeyValuePair<string, object>(
                    "deployment.version",
                    builder.Configuration["Build:Version"] ?? "unknown")
            ]);
    })
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            // Add EF Core, MongoDB, Marten, or vendor database instrumentation here.
            .AddHotChocolateInstrumentation()
            .AddOtlpExporter();
    });

var app = builder.Build();
app.MapGraphQL();
app.Run();

public sealed class Query
{
    public string[] GetProducts()
    {
        return ["Chai", "Chang"];
    }
}
```

Expected trace output:

```text
HTTP POST /graphql
└─ query
   ├─ Parse HTTP Request
   ├─ GraphQL Document Validation
   ├─ GraphQL Operation Planning
   ├─ Query.products
   └─ Format HTTP Response
```

The GraphQL spans include attributes such as `graphql.operation.type`, `graphql.operation.name`, `graphql.document.hash`, and `graphql.document.id` when those values are available. Do not enable document or variable capture globally in production. Documents, variables, extensions, headers, and DataLoader keys can contain sensitive data and can create high-cardinality telemetry.

# Confirm the slow operation before you tune

Do not start by tuning average `/graphql` latency. Find the specific operation and traffic segment first.

Search traces or logs with stable filters:

```text
service.name = "Catalog.Api"
http.route = "/graphql"
duration >= 1000ms
deployment.version = "2026.02.14.5"
graphql.operation.name = "ProductSearch"
```

If the operation is anonymous, use `graphql.document.hash` or `graphql.document.id` for the incident, then ask the client team to name the operation before long-term tuning. Operation names give you lower-cardinality dashboards, clearer alerts, and safer handoffs.

Capture this incident summary before you change code or settings:

| Field               | Value                                          |
| ------------------- | ---------------------------------------------- |
| Operation name      | `ProductSearch`                                |
| Document hash or ID | `sha256:...` or `products.search.v3`           |
| p50 / p95 / p99     | `180 ms / 1450 ms / 4200 ms`                   |
| Error rate          | `0.3%`                                         |
| Sample trace ID     | `4bf92f3577b34da6a3ce929d0e0e4736`             |
| Client and version  | `web 8.12.0`                                   |
| Safe segment        | `premium tier`, `region=eu`                    |
| Deployment window   | `started after 2026.02.14.5`                   |
| Pattern             | `first request after deploy` or `steady-state` |

This table prevents two common mistakes: chasing unrelated infrastructure noise and optimizing a different operation than the one users reported.

# Capture detailed spans for one investigation window

Keep default scopes in normal production traffic. Increase span detail for one bounded investigation window, one environment, or one sampled traffic group.

Production baseline:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddInstrumentation(options =>
    {
        options.RequestDetails = RequestDetails.Default;
    });
```

`RequestDetails.Default` includes request ID, document hash, operation name, and extensions. It does not include variables or the full document.

Investigation mode:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddInstrumentation(options =>
    {
        options.Scopes = ActivityScopes.All;
        options.RequestDetails =
            RequestDetails.Id |
            RequestDetails.Hash |
            RequestDetails.OperationName;
    });
```

Expected result: traces add spans for document parsing, validation, complexity analysis, variable coercion, operation compilation, operation execution, resolver execution, and DataLoader batches when those phases occur.

Local-only controlled mode:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddInstrumentation(options =>
    {
        options.Scopes = ActivityScopes.All;
        options.RequestDetails = RequestDetails.All;
        options.IncludeDataLoaderKeys = true;
    });
```

Use this only with non-sensitive data in local or staging reproduction. `RequestDetails.All` can add variables and document text to HTTP spans. `IncludeDataLoaderKeys` adds `graphql.dataloader.batch.keys`, which can expose identifiers and can create high-cardinality traces.

## Add low-cardinality context to spans

Use an `ActivityEnricher` for safe request tags such as tenant tier or application version. Avoid tenant IDs, user IDs, raw variables, and unbounded strings.

```csharp
using System.Diagnostics;
using HotChocolate.Diagnostics;
using HotChocolate.Execution;

namespace Catalog.Api.Diagnostics;

public sealed class CatalogActivityEnricher : ActivityEnricher
{
    public CatalogActivityEnricher(InstrumentationOptions options)
        : base(options)
    {
    }

    public override void EnrichExecuteRequest(RequestContext context, Activity activity)
    {
        base.EnrichExecuteRequest(context, activity);

        if (context.ContextData.TryGetValue("TenantTier", out var tier))
        {
            activity.SetTag("catalog.tenant_tier", tier?.ToString());
        }
    }
}
```

Register it:

```csharp
builder.Services.AddSingleton<ActivityEnricher, CatalogActivityEnricher>();

builder
    .AddGraphQL()
    .AddApplicationService<ActivityEnricher>();
```

For service-layer branches that are too coarse inside one resolver span, add your own `ActivitySource`:

```csharp
using System.Diagnostics;

namespace Catalog.Api.Diagnostics;

public static class CatalogTelemetry
{
    public static readonly ActivitySource Source = new("Catalog.Api");
}
```

```csharp
tracing.AddSource("Catalog.Api");
```

```csharp
using Catalog.Api.Diagnostics;

public sealed class PriceService
{
    public async Task<IReadOnlyList<Price>> GetPricesAsync(
        IReadOnlyList<int> productIds,
        CancellationToken cancellationToken)
    {
        using var activity = CatalogTelemetry.Source.StartActivity("Catalog.PriceLookup");
        activity?.SetTag("catalog.price_lookup.size", productIds.Count);

        return await LoadPricesAsync(productIds, cancellationToken);
    }
}
```

Expected result: a slow resolver separates into business service spans so you can see whether data access, remote calls, authorization, or CPU work dominates.

# Split latency by request phase

Compare the duration of these spans and timings in one trace:

- ASP.NET Core HTTP server span.
- Hot Chocolate HTTP parse and response formatting spans.
- GraphQL document parse, validation, complexity analysis, variable coercion, compile, and execution spans.
- Resolver spans grouped by `graphql.field.schema_coordinate`.
- DataLoader spans grouped by `graphql.dataloader.name` and `graphql.dataloader.batch.size`.
- Database spans, outbound HTTP spans, and custom business spans.
- Client, proxy, CDN, and load balancer timings when the server trace is shorter than the client duration.

Build dashboards around attributes, not span display names:

| Panel                       | Group by                                                                |
| --------------------------- | ----------------------------------------------------------------------- |
| Slow operations p95 and p99 | `graphql.operation.name`, `graphql.document.hash`, `deployment.version` |
| Phase breakdown per trace   | `graphql.processing.type`                                               |
| Slow resolver coordinates   | `graphql.field.schema_coordinate`                                       |
| DataLoader batches          | `graphql.dataloader.name`, `graphql.dataloader.batch.size`              |
| Database work per trace     | data source span name, table or collection when safe                    |
| Outbound HTTP               | host, route template, status code                                       |
| Response work               | response bytes, format span, time to first byte when available          |

Use this decision table after you inspect a sample trace:

| Dominant phase                          | Evidence                                                                 | Likely causes                                                            | Go to                                                                                                                                      |
| --------------------------------------- | ------------------------------------------------------------------------ | ------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------ |
| Parse, validate, complexity, or compile | Pre-execution spans dominate                                             | cache churn, unique documents, APQ misses, large documents               | [If parse, validate, complexity, or compile dominates](#if-parse-validate-complexity-or-compile-dominates)                                 |
| Limit or cost handling                  | validation errors, timeout, high cost metrics                            | unsafe shape, nested multipliers, limits too low                         | [If query depth, cost, or request limits are involved](#if-query-depth-cost-or-request-limits-are-involved)                                |
| Resolver execution                      | one coordinate or many resolver spans dominate                           | blocking work, per-field calls, CPU work                                 | [If execution or resolver spans dominate](#if-execution-or-resolver-spans-dominate)                                                        |
| DataLoader                              | missing batch spans or small batches                                     | N+1, request-scope misuse, repeated keys                                 | [If the trace shows N+1 or poor DataLoader batching](#if-the-trace-shows-n1-or-poor-dataloader-batching)                                   |
| Database or data store                  | DB spans dominate                                                        | missing projection, early materialization, indexes, provider translation | [If database or data-store time dominates](#if-database-or-data-store-time-dominates)                                                      |
| Paging, filtering, sorting, connection  | latency changes with `first`, filters, sorts, `totalCount`, aggregations | unbounded pages, nested multipliers, broad filters                       | [If paging, filtering, sorting, or connection work is the trigger](#if-paging-filtering-sorting-or-connection-work-is-the-trigger)         |
| First request                           | first request after deploy is slow, later requests are fast              | cold caches, lazy initialization, unwarmed operations                    | [If first requests after deployment are slow](#if-first-requests-after-deployment-are-slow)                                                |
| Persisted operation lookup              | `PersistedQueryNotFound`, retry with full document                       | APQ storage miss, hash mismatch, cache eviction                          | [If persisted or automatic persisted operations miss](#if-persisted-or-automatic-persisted-operations-miss)                                |
| Transport or proxy                      | GraphQL execution is fast, client sees slow response                     | large response, buffering, compression, streaming support                | [If HTTP transport, serialization, streaming, or proxy time dominates](#if-http-transport-serialization-streaming-or-proxy-time-dominates) |
| Authorization or diagnostics            | custom middleware, auth, logging, exporter spans dominate                | per-field remote checks, synchronous listeners                           | [If authorization, middleware, logging, or diagnostics are expensive](#if-authorization-middleware-logging-or-diagnostics-are-expensive)   |

# If parse, validate, complexity, or compile dominates

Evidence to look for:

- Long spans for document parsing, validation, complexity analysis, variable coercion, or operation compilation.
- Many first-time operations after deployment.
- Changing document text for the same logical operation.
- Missing operation names.
- Automatic persisted operation misses followed by a retry with the full document.
- Large or fragment-heavy documents.

Likely causes:

- `OperationDocumentCacheSize` is too small. The default is `256`, with a minimum of `16`.
- `PreparedOperationCacheSize` is too small. The default is `256`, with a minimum of `16`.
- Clients inline literals or generated aliases so every request has a different document hash.
- APQ uses process-local or cold storage in a multi-instance deployment.
- Cost analysis is doing legitimate work for a large nested operation.

Fix the document identity before you raise cache sizes:

```csharp
builder
    .AddGraphQL()
    .ModifyRequestOptions(options =>
    {
        options.OperationDocumentCacheSize = 1024;
        options.PreparedOperationCacheSize = 1024;
    });
```

Increase cache sizes only after traces show churn for a stable operation set. If the document text changes on every request, larger caches hide the symptom without removing the cause.

Prefer these fixes first:

1. Name every operation.
2. Move changing values into variables instead of inline literals.
3. Adopt [trusted documents](/docs/hotchocolate/v16/performance/trusted-documents) or [automatic persisted operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations) for known clients.
4. Use durable operation document storage when multiple instances serve APQ traffic.
5. Warm representative operations during startup.
6. Use `GraphQL-Cost: validate` in local or staging to understand cost analysis without executing resolvers.

Expected result: repeated requests for the same operation show shorter parse, validation, and compile spans. Persisted requests use a document ID or hash and avoid resending the full document on the optimized path.

# If query depth, cost, or request limits are involved

Evidence to look for:

- Validation errors for parser limits, execution depth, field-cycle depth, validation error caps, or timeout.
- High `graphql.operation.fieldCost` or `graphql.operation.typeCost` on complexity spans.
- `GraphQL-Cost: report` shows high cost for a request that still executes successfully.
- The operation is allowed, but nested pages multiply work.

Measure cost with a controlled request:

```bash
curl -s http://localhost:5000/graphql \
  -H 'Content-Type: application/json' \
  -H 'GraphQL-Cost: report' \
  --data '{"query":"query Products { products(first: 20) { nodes { id name } } }"}'
```

Expected response shape:

```json
{
  "data": {
    "products": {
      "nodes": [{ "id": "UHJvZHVjdDox", "name": "Chai" }]
    }
  },
  "extensions": {
    "operationCost": {
      "fieldCost": 1,
      "typeCost": 2
    }
  }
}
```

Use `GraphQL-Cost: validate` when you want metrics without running resolvers.

Tune costs from measured representative operations:

```csharp
builder
    .AddGraphQL()
    .ModifyCostOptions(options =>
    {
        options.MaxFieldCost = 5_000;
        options.MaxTypeCost = 5_000;
    })
    .ModifyPagingOptions(options =>
    {
        options.RequirePagingBoundaries = true;
    });
```

Annotate expensive fields and list sizes so the analyzer reflects actual work:

```csharp
public sealed class Query
{
    [Cost(50)]
    [ListSize(AssumedSize = 10, SlicingArgumentDefaultValue = 10)]
    public async Task<IReadOnlyList<Report>> GetReportsAsync(
        ReportService reports,
        CancellationToken cancellationToken)
    {
        return await reports.GetReportsAsync(cancellationToken);
    }
}
```

Example tuning table:

| Query shape                                                              | Before            | Change                                            | After                              |
| ------------------------------------------------------------------------ | ----------------- | ------------------------------------------------- | ---------------------------------- |
| `products(first: 50) { reviews(first: 50) { nodes { author { id } } } }` | field cost `5611` | lower nested page size and add list size metadata | field cost matches expected budget |
| expensive external report field                                          | under-costed      | `[Cost(50)]`                                      | cost reflects downstream work      |

Keep limits as guardrails, not as a substitute for fixing slow legitimate operations. For safety, review [Request limits](/docs/hotchocolate/v16/securing-your-api/request-limits) and [Cost analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis).

# If execution or resolver spans dominate

Evidence to look for:

- One resolver span owns most of the request duration.
- Many resolver spans aggregate into most of the request.
- The top coordinate, grouped by `graphql.field.schema_coordinate`, is stable across slow traces.
- Downstream HTTP, database, or custom business spans sit under the slow resolver.

Common causes:

- Blocking `.Result` or `.Wait()` on asynchronous work.
- CPU-heavy transformations in a field resolver.
- Large allocations or repeated serialization.
- Per-field remote calls.
- Missing `CancellationToken` propagation.
- Authorization or business policy checks repeated inside each field.

Replace sync-over-async with an async resolver and propagate cancellation:

```csharp
// Before
public Product GetProduct(int id, ProductService products)
{
    return products.GetProductAsync(id, CancellationToken.None).Result;
}

// After
public async Task<Product> GetProductAsync(
    int id,
    ProductService products,
    CancellationToken cancellationToken)
{
    return await products.GetProductAsync(id, cancellationToken);
}
```

When a resolver fetches reusable values by key, move that lookup into a DataLoader. When a resolver contains several business branches, add custom `ActivitySource` spans around those branches so the next trace maps the slow coordinate to one dependency or service path.

Expected result: the slow coordinate either shrinks, or the trace identifies the downstream call that needs ownership from another team.

# If the trace shows N+1 or poor DataLoader batching

Evidence to look for:

- Database query count grows with `nodes` count or page size.
- Repeated child resolver spans appear under each parent node.
- No `GraphQL DataLoader Batch ...` span appears for a key lookup.
- Many small batches share the same `graphql.dataloader.name`.
- `graphql.dataloader.batch.size` stays near `1` under a list field.
- Cache hit and miss evidence shows repeated keys, or DataLoaders are constructed manually outside the request scope.

DataLoader coordinates request execution and caches per request. It is not a database abstraction and not a cross-request cache.

Use a source-generated DataLoader for shared key lookups:

```csharp
// DataLoaders/BrandDataLoaders.cs
internal static class BrandDataLoaders
{
    [DataLoader]
    public static async Task<Dictionary<int, Brand>> GetBrandByIdAsync(
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

```csharp
// Types/ProductNode.cs
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

Before and after:

| Request                                                 | Without DataLoader                                        | With DataLoader                                                    |
| ------------------------------------------------------- | --------------------------------------------------------- | ------------------------------------------------------------------ |
| `products(first: 20) { nodes { name brand { name } } }` | 1 product query plus 20 brand queries                     | 1 product query plus 1 brand batch query                           |
| Trace symptom                                           | repeated `Product.brand` resolver spans and many DB spans | one `BrandById` batch span with batch size near unique brand count |

Use `[BatchResolver]` when a value is field-specific and does not need cross-field caching. Use aggregation DataLoaders for nested connection aggregations. Keep DataLoaders request-scoped, inject generated interfaces into resolvers, and avoid manually constructing DataLoaders.

For Entity Framework Core, follow the [EF integration](/docs/hotchocolate/v16/integrations/entity-framework). If a DataLoader needs a factory, inject `IDbContextFactory<T>` and create the `DbContext` inside the batch method. Do not share one EF `DbContext` across parallel query resolvers.

Expected result: `graphql.dataloader.batch.size` increases, backend query count stops scaling linearly with returned nodes, and p95 improves at larger page sizes.

# If database or data-store time dominates

Evidence to look for:

- Database spans dominate total request duration.
- Rows read, scanned documents, or query duration greatly exceed returned GraphQL nodes.
- SQL selects too many columns.
- MongoDB projections do not reduce work.
- Marten LINQ translation produces an unexpected query.
- Filtering or sorting happens in memory.

Common causes:

- Missing `[UseProjection]` or missing `QueryContext<T>`.
- Middleware order is wrong. Use `UsePaging` > `UseProjection` > `UseFiltering` > `UseSorting`.
- A resolver calls `ToList()` before Hot Chocolate applies middleware.
- Indexes do not support filter or sort fields.
- Broad `contains`, nested `some` or `all`, multi-field sorts, or null ordering hit unindexed paths.
- `IncludeTotalCount` adds count queries on a hot path.
- EF `DbContext` scope changes cause parallel-operation errors or serialized execution.

Keep provider-translatable shapes until Hot Chocolate middleware applies:

```csharp
[QueryType]
public static partial class ProductQueries
{
    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Product> GetProducts(CatalogContext db)
    {
        return db.Products;
    }
}
```

Use `QueryContext<T>` when you want projection, filtering, and sorting in one return type:

```csharp
[QueryType]
public static partial class ProductQueries
{
    public static QueryContext<Product> GetProducts(CatalogContext db)
    {
        return db.Products.AsQueryContext();
    }
}
```

Do not combine `QueryContext<T>` with `[UseProjection]` on the same field.

For MongoDB, return `IExecutable<T>` through `AsExecutable()` so the MongoDB provider can translate paging, filtering, sorting, and projection:

```csharp
[UsePaging]
[UseProjection]
[UseSorting]
[UseFiltering]
public IExecutable<Person> GetPersons(IMongoCollection<Person> persons)
{
    return persons.AsExecutable();
}
```

For Marten, use the Marten filtering and sorting conventions documented in [Marten](/docs/hotchocolate/v16/integrations/marten), and verify generated queries with your database explain tools.

Expected result: queries read fewer rows, columns, or documents, use expected indexes, and reduce database duration without changing the GraphQL result shape.

# If paging, filtering, sorting, or connection work is the trigger

Evidence to look for:

- Latency increases when clients raise `first` or `last`.
- Slow traces request nested connections.
- `totalCount` appears on hot paths.
- Aggregations are requested under many parents.
- Broad filters or multi-field sorts correlate with slow DB spans.
- Cost reports show list-size multiplication.

Bound page sizes globally or on hot fields:

```csharp
builder
    .AddGraphQL()
    .ModifyPagingOptions(options =>
    {
        options.DefaultPageSize = 25;
        options.MaxPageSize = 100;
        options.RequirePagingBoundaries = true;
        options.IncludeTotalCount = false;
    });
```

Before and after query review:

```graphql
# Expensive shape
query ProductOverview {
  brands(first: 50) {
    nodes {
      products(first: 50) {
        totalCount
        nodes {
          id
          reviews(first: 50) {
            nodes {
              rating
            }
          }
        }
      }
    }
  }
}
```

```graphql
# Bounded shape
query ProductOverview {
  brands(first: 20) {
    nodes {
      products(first: 10) {
        nodes {
          id
        }
      }
    }
  }
}
```

For custom service-backed connections, use `ConnectionFlags` so aggregation-only requests do not load nodes:

```csharp
[UseConnection]
public static async Task<BrandProductsConnection> GetProductsAsync(
    [Parent] Brand brand,
    PagingArguments paging,
    QueryContext<Product> query,
    ConnectionFlags flags,
    ProductService products,
    CancellationToken cancellationToken)
{
    var page = flags is ConnectionFlags.None
        ? Page<Product>.Empty
        : await products.GetProductsByBrandAsync(
            brand.Id,
            paging,
            query,
            cancellationToken);

    return new BrandProductsConnection(brand.Id, page);
}
```

Expected result: an aggregation-only query does not load product nodes, page sizes are explicit, and database work scales with requested connection parts.

Restrict filters and sorts to indexed fields for production-facing operations. Review [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination), [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering), and [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting) when you need field-specific conventions.

# If first requests after deployment are slow

Evidence to look for:

- Only the first request after deployment or per operation is slow.
- Later identical requests are fast.
- Executor creation or cache-fill events appear.
- Parse, validation, or compile spans dominate only on first use.

Hot Chocolate v16 eagerly initializes schemas by default. `LazyInitialization` defaults to `false`. Setting it to `true` moves schema and executor creation to the first request, which can improve startup time at the cost of first-request latency.

Warm representative operations:

```csharp
using HotChocolate.Execution;

builder
    .AddGraphQL()
    .AddWarmupTask(async (executor, cancellationToken) =>
    {
        var request = OperationRequestBuilder.New()
            .SetDocument("query ProductCard { products(first: 10) { nodes { id name } } }")
            .SetOperationName("ProductCard")
            .MarkAsWarmupRequest()
            .Build();

        await executor.ExecuteAsync(request, cancellationToken);
    });
```

`MarkAsWarmupRequest()` prepares parsing, validation, and operation preparation paths without executing resolvers. Include the operation name when clients send one because it participates in the operation cache key.

Expected result: the first live request for warmed operations has latency close to steady state. If downstream connection pools are cold, warm them separately with safe application startup checks.

# If persisted or automatic persisted operations miss

Evidence to look for:

- An APQ optimized request returns `PersistedQueryNotFound`.
- The client sends a second request with the full document.
- Misses increase after deployment, instance rotation, or cache eviction.
- Different server instances do not share operation document storage.
- The hash format or algorithm differs between client and server.

Fixes:

1. Use [trusted documents](/docs/hotchocolate/v16/performance/trusted-documents) for known operations when your deployment workflow can publish documents ahead of time.
2. Use durable operation document storage for [automatic persisted operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations) in multi-instance production.
3. Verify hash algorithm and document normalization with the client team.
4. Track miss rate by document hash or ID. Do not tag raw documents.

Expected result: known operations take the optimized path, avoid the extra round trip, and reduce parse and validate overhead.

# If HTTP transport, serialization, streaming, or proxy time dominates

Evidence to look for:

- The Hot Chocolate execution span is fast, but the ASP.NET Core request span or client duration is slow.
- The response formatting span is long.
- Response bytes are large.
- Client reads are slow or canceled.
- A reverse proxy buffers incremental payloads before sending them to the client.
- Compression, CDN, load balancer, or client timeout occurs after GraphQL execution completed.

Compare timings in one table:

| Timing                  | Example   | Interpretation                      |
| ----------------------- | --------- | ----------------------------------- |
| Client duration         | `4200 ms` | User-observed latency               |
| Proxy duration          | `4100 ms` | Includes buffering and transfer     |
| ASP.NET Core span       | `3900 ms` | Server HTTP pipeline duration       |
| Hot Chocolate root span | `900 ms`  | GraphQL request work                |
| Execution span          | `650 ms`  | Resolver and DataLoader work        |
| Format response span    | `2200 ms` | Serialization or streaming pressure |
| Response bytes          | `9.8 MB`  | Payload size driver                 |

Fixes:

- Reduce selected fields, page sizes, nested connections, and `totalCount` on hot paths.
- Use persisted operations to reduce request payload size.
- Use `@defer` or `@stream` only when clients and intermediaries support incremental delivery and time to first byte matters.
- Check ASP.NET Core, reverse proxy, CDN, load balancer, and client timeouts against `ExecutionTimeout`.
- Verify proxy buffering and streaming support before relying on incremental delivery behavior.

Expected result: you identify whether the bottleneck is response size, serialization, transfer, proxy buffering, or GraphQL execution. See [HTTP transport](/docs/hotchocolate/v16/server/http-transport) for transport behavior.

# If authorization, middleware, logging, or diagnostics are expensive

Evidence to look for:

- Resolver spans include authorization or custom middleware work.
- Remote policy checks repeat per field.
- Logging calls or diagnostic listeners appear in the request path.
- Exporter or enrichment work correlates with latency spikes.

Keep diagnostic event listeners fast. Hot Chocolate invokes diagnostic event handlers synchronously as part of the GraphQL request. Enqueue expensive logging, auditing, or export work to a background service.

Add custom spans around policy evaluation when ownership is unclear:

```csharp
using System.Security.Claims;
using Catalog.Api.Diagnostics;

public sealed class CatalogAuthorizationService
{
    public async Task<bool> CanViewPricesAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        using var activity = CatalogTelemetry.Source.StartActivity("Catalog.AuthorizePrices");
        activity?.SetTag("catalog.auth.policy", "view-prices");

        return await EvaluatePolicyAsync(user, cancellationToken);
    }
}
```

Fixes:

- Cache authorization decisions safely per request or per user policy when valid.
- Avoid remote authorization calls per field. Batch or precompute decisions.
- Keep log scopes and trace enrichers low-cardinality.
- Move expensive logging/export work off the request path.

Expected result: auth, logging, and diagnostics time is separated from resolver data-fetch time, and repeated per-field overhead is reduced.

# Reproduce the slow request locally or in staging

Use a safe reproduction when the production trace identifies a likely cause but you need to verify a fix.

Collect:

- Operation name.
- Document hash or ID.
- Sanitized document only when policy allows it.
- Variable shape without secrets, tokens, emails, or raw identifiers.
- Page sizes, filters, sorts, and selected connection parts.
- Headers that affect authorization, tenant tier, locale, or routing.
- Representative data volume.
- Deployment version and feature flags.

Replay with `curl`, Nitro, or an integration test. For local-only diagnostics, enable `ActivityScopes.All`, `RequestDetails.All` when data is safe, and `IncludeDataLoaderKeys = true` only for non-sensitive bounded keys. Use `GraphQL-Cost: report` or `GraphQL-Cost: validate` to capture cost. Capture database explain plans for SQL, MongoDB, or Marten queries generated by the operation.

Expected result: the local or staging trace has the same dominant phase as production. If it does not, document the production-only dependency that remains unreproduced.

# Validate the fix

Compare the same operation before and after the change. Keep operation name, document hash or ID, variables, page sizes, filters, sorts, data volume, and deployment window as close as possible.

Use this table:

| Metric                | Before              | After             | Target                | Evidence         |
| --------------------- | ------------------- | ----------------- | --------------------- | ---------------- |
| p95 / p99             | `1450 ms / 4200 ms` | `420 ms / 900 ms` | `< 800 ms p95`        | trace links      |
| Error rate            | `0.3%`              | `0.1%`            | `< 0.2%`              | logs             |
| Dominant phase        | DB spans            | resolver + format | no single phase > 60% | trace breakdown  |
| DB query count        | `21`                | `2`               | `<= 3`                | DB spans         |
| Rows/documents read   | `50,000`            | `1,200`           | indexed query plan    | explain plan     |
| DataLoader batch size | `1`                 | `18`              | near unique key count | DataLoader span  |
| Cost                  | `field=5611`        | `field=890`       | below budget          | `GraphQL-Cost`   |
| Response bytes        | `9.8 MB`            | `1.1 MB`          | `< 2 MB`              | HTTP metrics     |
| Result shape          | unchanged           | unchanged         | no client regression  | integration test |

Watch for shifted bottlenecks. Reducing database time can expose serialization, proxy, or outbound HTTP time.

# Remediation checklist

Use this checklist after you identify the dominant phase:

- Name every client operation.
- Prefer trusted documents or persisted operations for known clients.
- Size operation document and prepared operation caches for the real operation set.
- Use cost analysis, depth rules, field-cycle rules, parser limits, and execution timeout as guardrails.
- Use DataLoaders or batch resolvers for fan-out lookups.
- Return provider-translatable shapes such as `IQueryable<T>`, `IExecutable<T>`, or `QueryContext<T>` until Hot Chocolate middleware applies.
- Use projections and the correct middleware order.
- Bound page sizes and avoid `totalCount` on hot paths unless clients need it.
- Restrict expensive filters and sorts to indexed fields.
- Propagate `CancellationToken` to databases and HTTP clients.
- Keep diagnostic listeners, log scopes, and trace enrichers fast and low-cardinality.
- Validate fixes with before and after traces.

# Escalation checklist

When you hand the incident to another team or file a Hot Chocolate issue, include:

- Operation name, document hash or ID, and sanitized document only when allowed.
- Variable shape without secrets or personal data.
- Trace ID, deployment version, environment, and affected safe segment.
- Phase breakdown and dominant span.
- Top resolver coordinates from `graphql.field.schema_coordinate`.
- DataLoader names, batch sizes, cache hit or miss evidence, and whether keys were inspected only in safe reproduction.
- Database and external calls, query plans, rows or documents read, and indexes used.
- Cost metrics, depth or request-limit errors, and page/filter/sort arguments.
- Persisted operation or APQ miss rate and storage type.
- Transport, proxy, client timing, and response size.
- Reproduction steps and validation table.
- Ownership guess: application resolver, data store, platform/proxy, client query, or Hot Chocolate.

# Next steps

- [OpenTelemetry tracing](/docs/hotchocolate/v16/operations/observability/opentelemetry) for trace setup and span attributes.
- [Production logging](/docs/hotchocolate/v16/operations/observability/logging) for safe logs and trace correlation.
- [Metrics](/docs/hotchocolate/v16/operations/observability/metrics) for latency and cost dashboards.
- [Performance tuning](/docs/hotchocolate/v16/guides/performance) for broader performance practices.
- [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader) for batching and request-scoped caching.
- [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections), [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination), [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering), and [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting) for data-shaping fixes.
- [Warmup](/docs/hotchocolate/v16/server/warmup) for startup and cache preparation.
- [Request limits](/docs/hotchocolate/v16/securing-your-api/request-limits) and [Cost analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis) for guardrails.
- [Trusted documents](/docs/hotchocolate/v16/performance/trusted-documents) and [Automatic persisted operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations) for document identity and APQ behavior.
