---
title: Troubleshoot slow requests
---

Use this guide when you encounter slow requests in a Hot Chocolate v16 production environment. You'll learn how to pinpoint which phase, field, or dependency is responsible for latency, and how to address it efficiently. The process begins with a reported operation, leverages low-cardinality telemetry to break down request time, and leads you to the most targeted fix.

This page focuses on Hot Chocolate v16 server operations. For Fusion gateway tracing or distributed subgraph troubleshooting, refer to Fusion-specific documentation.

# Prerequisites

Before you start troubleshooting production latency, make sure you have:

- A Hot Chocolate v16 server set up with `app.MapGraphQL()`.
- The `HotChocolate.Diagnostics` package installed.
- Instrumentation enabled via `.AddInstrumentation()`.
- OpenTelemetry tracing configured with `.AddHotChocolateInstrumentation()`, `.AddAspNetCoreInstrumentation()`, `.AddHttpClientInstrumentation()`, and any database instrumentation relevant to your stack.
- Logs or trace data that include trace ID, span ID, service name, deployment version, and environment.
- Named client operations. If you use trusted documents or automatic persisted operations, collect the document ID or hash.
- A safe way to reproduce the operation, including sanitized variables and the same paging, filtering, sorting, and authorization inputs.

Set up production-safe tracing like this:

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
            // Add EF Core, MongoDB, Marten, or other database instrumentation as needed.
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

A typical trace will look like this:

```text
HTTP POST /graphql
└─ query
   ├─ Parse HTTP Request
   ├─ GraphQL Document Validation
   ├─ GraphQL Operation Planning
   ├─ Query.products
   └─ Format HTTP Response
```

GraphQL spans include attributes such as `graphql.operation.type`, `graphql.operation.name`, `graphql.document.hash`, and `graphql.document.id` when available. Avoid enabling document or variable capture globally in production, as these can contain sensitive data and create high-cardinality telemetry.

# Confirm the slow operation before tuning

Don’t start by tuning average `/graphql` latency. First, identify the specific operation and traffic segment that are slow.

Filter traces or logs using stable criteria:

```text
service.name = "Catalog.Api"
http.route = "/graphql"
duration >= 1000ms
deployment.version = "2026.02.14.5"
graphql.operation.name = "ProductSearch"
```

If the operation is anonymous, use `graphql.document.hash` or `graphql.document.id` to track the incident. Ask the client team to name the operation for long-term tuning. Named operations make dashboards clearer, alerts more actionable, and handoffs safer.

Before changing any code or settings, capture an incident summary like this:

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

This summary helps you avoid two common mistakes: chasing unrelated infrastructure noise and optimizing the wrong operation.

# Capture detailed spans for a focused investigation

Keep default scopes for normal production traffic. When you need more detail, increase span granularity for a limited window, environment, or sampled group.

**Production baseline:**

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddInstrumentation(options =>
    {
        options.RequestDetails = RequestDetails.Default;
    });
```

`RequestDetails.Default` includes request ID, document hash, operation name, and extensions, but not variables or the full document.

**Investigation mode:**

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

This adds spans for document parsing, validation, complexity analysis, variable coercion, operation compilation, execution, resolver execution, and DataLoader batches when those phases occur.

**Local-only controlled mode:**

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

Use this only with non-sensitive data in local or staging environments. `RequestDetails.All` can add variables and document text to HTTP spans. `IncludeDataLoaderKeys` adds `graphql.dataloader.batch.keys`, which may expose identifiers and create high-cardinality traces.

## Add low-cardinality context to spans

Use an `ActivityEnricher` to add safe request tags, such as tenant tier or application version. Avoid tagging tenant IDs, user IDs, raw variables, or unbounded strings.

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

Register the enricher:

```csharp
builder.Services.AddSingleton<ActivityEnricher, CatalogActivityEnricher>();

builder
    .AddGraphQL()
    .AddApplicationService<ActivityEnricher>();
```

If you need more granularity inside a resolver span, add your own `ActivitySource`:

```csharp
using System.Diagnostics;

namespace Catalog.Api.Diagnostics;

public static class CatalogTelemetry
{
    public static readonly ActivitySource Source = new("Catalog.Api");
}
```

Register the source with tracing:

```csharp
tracing.AddSource("Catalog.Api");
```

Instrument business logic for deeper insight:

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

With this setup, a slow resolver breaks down into business service spans, letting you see if data access, remote calls, authorization, or CPU work is the main contributor.

# Split latency by request phase

Once you have a trace, compare the duration of each major span:

- ASP.NET Core HTTP server span
- Hot Chocolate HTTP parse and response formatting spans
- GraphQL document parse, validation, complexity analysis, variable coercion, compile, and execution spans
- Resolver spans grouped by `graphql.field.schema_coordinate`
- DataLoader spans grouped by `graphql.dataloader.name` and `graphql.dataloader.batch.size`
- Database, outbound HTTP, and custom business spans
- Client, proxy, CDN, and load balancer timings (when the server trace is shorter than the client duration)

Build dashboards using attributes, not just span display names. For example:

| Panel                       | Group by                                                                |
| --------------------------- | ----------------------------------------------------------------------- |
| Slow operations p95 and p99 | `graphql.operation.name`, `graphql.document.hash`, `deployment.version` |
| Phase breakdown per trace   | `graphql.processing.type`                                               |
| Slow resolver coordinates   | `graphql.field.schema_coordinate`                                       |
| DataLoader batches          | `graphql.dataloader.name`, `graphql.dataloader.batch.size`              |
| Database work per trace     | data source span name, table or collection (when safe)                  |
| Outbound HTTP               | host, route template, status code                                       |
| Response work               | response bytes, format span, time to first byte (when available)        |

After inspecting a sample trace, use this decision table to guide your next steps:

| Dominant phase                          | Evidence                                                                 | Likely causes                                                            | Where to go next                                                                                                                           |
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

**What to look for:**

- Long spans for document parsing, validation, complexity analysis, variable coercion, or operation compilation
- Many first-time operations after deployment
- Changing document text for the same logical operation
- Missing operation names
- Automatic persisted operation misses followed by a retry with the full document
- Large or fragment-heavy documents

**Common causes:**

- `OperationDocumentCacheSize` or `PreparedOperationCacheSize` is too small (default: `256`, minimum: `16`)
- Clients inline literals or generate aliases, causing every request to have a different document hash
- APQ uses process-local or cold storage in a multi-instance deployment
- Cost analysis is working hard on a large, nested operation

**How to fix:**

First, address document identity before increasing cache sizes:

```csharp
builder
    .AddGraphQL()
    .ModifyRequestOptions(options =>
    {
        options.OperationDocumentCacheSize = 1024;
        options.PreparedOperationCacheSize = 1024;
    });
```

Only increase cache sizes if traces show churn for a stable set of operations. If the document text changes on every request, larger caches only mask the problem.

Prioritize these steps:

1. Name every operation.
2. Move changing values into variables instead of inline literals.
3. Use [trusted documents](/docs/hotchocolate/v16/performance/trusted-documents) or [automatic persisted operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations) for known clients.
4. Use durable operation document storage for APQ in multi-instance deployments.
5. Warm up representative operations at startup.
6. Use `GraphQL-Cost: validate` in local or staging to analyze cost without executing resolvers.

**Expected result:** repeated requests for the same operation show shorter parse, validation, and compile spans. Persisted requests use a document ID or hash and avoid resending the full document.

# If query depth, cost, or request limits are involved

**What to look for:**

- Validation errors for parser limits, execution depth, field-cycle depth, validation error caps, or timeouts
- High `graphql.operation.fieldCost` or `graphql.operation.typeCost` in complexity spans
- `GraphQL-Cost: report` shows high cost for a request that still executes
- The operation is allowed, but nested pages multiply the work

**How to measure and tune:**

Measure cost with a controlled request:

```bash
curl -s http://localhost:5000/graphql \
  -H 'Content-Type: application/json' \
  -H 'GraphQL-Cost: report' \
  --data '{"query":"query Products { products(first: 20) { nodes { id name } } }"}'
```

You should see a response like:

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

Use `GraphQL-Cost: validate` to get cost metrics without running resolvers.

Tune cost and paging options based on real operations:

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

Use limits as guardrails, not as a replacement for fixing slow but valid operations. For more, see [Request limits](/docs/hotchocolate/v16/securing-your-api/request-limits) and [Cost analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis).

# If execution or resolver spans dominate

**What to look for:**

- A single resolver span takes most of the request duration
- Many resolver spans together account for most of the request
- The top coordinate (by `graphql.field.schema_coordinate`) is consistent across slow traces
- Downstream HTTP, database, or business spans appear under the slow resolver

**Common causes:**

- Blocking `.Result` or `.Wait()` on asynchronous work
- CPU-heavy transformations in a field resolver
- Large allocations or repeated serialization
- Per-field remote calls
- Missing `CancellationToken` propagation
- Authorization or business policy checks repeated in each field

**How to fix:**

Replace sync-over-async with a true async resolver and propagate cancellation:

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

If a resolver fetches reusable values by key, move that logic into a DataLoader. For resolvers with multiple business branches, add custom `ActivitySource` spans around each branch so the next trace can pinpoint which dependency or service path is slow.

**Expected result:** the slow coordinate either shrinks, or the trace reveals a downstream call that another team should own.

# If the trace shows N+1 or poor DataLoader batching

**What to look for:**

- Database query count grows with the number of `nodes` or page size
- Repeated child resolver spans under each parent node
- No `GraphQL DataLoader Batch ...` span for a key lookup
- Many small batches with the same `graphql.dataloader.name`
- `graphql.dataloader.batch.size` stays near `1` under a list field
- Cache hit/miss evidence shows repeated keys, or DataLoaders are constructed manually outside the request scope

DataLoader coordinates request execution and caches per request. It is not a database abstraction or a cross-request cache.

**How to fix:**

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

**Before and after:**

| Request                                                 | Without DataLoader                                        | With DataLoader                                                    |
| ------------------------------------------------------- | --------------------------------------------------------- | ------------------------------------------------------------------ |
| `products(first: 20) { nodes { name brand { name } } }` | 1 product query plus 20 brand queries                     | 1 product query plus 1 brand batch query                           |
| Trace symptom                                           | repeated `Product.brand` resolver spans and many DB spans | one `BrandById` batch span with batch size near unique brand count |

Use `[BatchResolver]` for field-specific values that do not need cross-field caching. Use aggregation DataLoaders for nested connection aggregations. Always keep DataLoaders request-scoped, inject generated interfaces into resolvers, and avoid manually constructing DataLoaders.

For Entity Framework Core, see the [EF integration](/docs/hotchocolate/v16/integrations/entity-framework). If a DataLoader needs a factory, inject `IDbContextFactory<T>` and create the `DbContext` inside the batch method. Never share a single EF `DbContext` across parallel query resolvers.

**Expected result:** `graphql.dataloader.batch.size` increases, backend query count stops scaling linearly with returned nodes, and p95 latency improves at larger page sizes.

# If database or data-store time dominates

**What to look for:**

- Database spans dominate total request duration
- Rows read, scanned documents, or query duration far exceed the number of returned GraphQL nodes
- SQL selects too many columns
- MongoDB projections do not reduce work
- Marten LINQ translation produces unexpected queries
- Filtering or sorting happens in memory

**Common causes:**

- Missing `[UseProjection]` or missing `QueryContext<T>`
- Middleware order is incorrect. The correct order is: `UsePaging` > `UseProjection` > `UseFiltering` > `UseSorting`
- A resolver calls `ToList()` before Hot Chocolate applies middleware
- Indexes do not support filter or sort fields
- Broad `contains`, nested `some`/`all`, multi-field sorts, or null ordering hit unindexed paths
- `IncludeTotalCount` adds count queries on a hot path
- EF `DbContext` scope changes cause parallel-operation errors or force serialized execution

**How to fix:**

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

Use `QueryContext<T>` if you want projection, filtering, and sorting in one return type:

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

For MongoDB, return `IExecutable<T>` using `AsExecutable()` so the provider can translate paging, filtering, sorting, and projection:

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

For Marten, follow the [Marten integration](/docs/hotchocolate/v16/integrations/marten) and verify generated queries with your database explain tools.

**Expected result:** queries read fewer rows, columns, or documents, use the right indexes, and database duration drops without changing the GraphQL result shape.

# If paging, filtering, sorting, or connection work is the trigger

**What to look for:**

- Latency increases as clients raise `first` or `last`
- Slow traces request nested connections
- `totalCount` appears on hot paths
- Aggregations are requested under many parents
- Broad filters or multi-field sorts correlate with slow DB spans
- Cost reports show list-size multiplication

**How to fix:**

Set page size limits globally or on hot fields:

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

**Example query shapes:**

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

**Expected result:** aggregation-only queries do not load product nodes, page sizes are explicit, and database work scales with the requested connection parts.

Restrict filters and sorts to indexed fields for production operations. For more, see [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination), [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering), and [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting).

# If first requests after deployment are slow

**What to look for:**

- Only the first request after deployment or per operation is slow
- Later identical requests are fast
- Executor creation or cache-fill events appear
- Parse, validation, or compile spans dominate only on first use

Hot Chocolate v16 initializes schemas eagerly by default (`LazyInitialization` is `false`). Setting it to `true` moves schema and executor creation to the first request, which speeds up startup but increases first-request latency.

**How to fix:**

Warm up representative operations at startup:

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

`MarkAsWarmupRequest()` prepares parsing, validation, and operation preparation without executing resolvers. Always include the operation name if clients send one, as it is part of the operation cache key.

**Expected result:** the first live request for warmed operations is nearly as fast as steady-state. If downstream connection pools are cold, warm them separately during application startup.

# If persisted or automatic persisted operations miss

**What to look for:**

- An APQ-optimized request returns `PersistedQueryNotFound`
- The client sends a second request with the full document
- Misses increase after deployment, instance rotation, or cache eviction
- Server instances do not share operation document storage
- The hash format or algorithm differs between client and server

**How to fix:**

1. Use [trusted documents](/docs/hotchocolate/v16/performance/trusted-documents) for known operations if your deployment can publish documents ahead of time.
2. Use durable operation document storage for [automatic persisted operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations) in multi-instance production.
3. Verify hash algorithm and document normalization with the client team.
4. Track miss rate by document hash or ID. Never tag raw documents.

**Expected result:** known operations use the optimized path, avoid extra round trips, and reduce parse and validate overhead.

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

# Reproducing the slow request locally or in staging

When a production trace points to a likely cause, reproduce the issue in a safe environment to verify your fix.

**What to collect:**

- Operation name
- Document hash or ID
- Sanitized document (only if policy allows)
- Variable shape (no secrets, tokens, emails, or raw IDs)
- Page sizes, filters, sorts, and selected connection parts
- Headers affecting authorization, tenant, locale, or routing
- Representative data volume
- Deployment version and feature flags

Replay the request using `curl`, Nitro, or an integration test. For local diagnostics, enable `ActivityScopes.All` and `RequestDetails.All` only when data is safe, and set `IncludeDataLoaderKeys = true` only for non-sensitive, bounded keys. Use `GraphQL-Cost: report` or `GraphQL-Cost: validate` to capture cost. Collect database explain plans for SQL, MongoDB, or Marten queries generated by the operation.

**Expected result:** the local or staging trace shows the same dominant phase as production. If not, document any production-only dependencies that remain unreproduced.

# Validating the fix

Compare the same operation before and after your change. Keep the operation name, document hash or ID, variables, page sizes, filters, sorts, data volume, and deployment window as similar as possible.

Use this table to track improvements:

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

**Watch for shifted bottlenecks:** fixing one area (like database time) can reveal new issues in serialization, proxies, or outbound HTTP.

# Remediation checklist

After identifying the dominant phase, use this checklist to ensure a thorough fix:

- Name every client operation
- Prefer trusted documents or persisted operations for known clients
- Size operation document and prepared operation caches for your real operation set
- Use cost analysis, depth rules, field-cycle rules, parser limits, and execution timeouts as guardrails
- Use DataLoaders or batch resolvers for fan-out lookups
- Return provider-translatable shapes (`IQueryable<T>`, `IExecutable<T>`, or `QueryContext<T>`) until Hot Chocolate middleware applies
- Use projections and ensure correct middleware order
- Bound page sizes and avoid `totalCount` on hot paths unless needed
- Restrict expensive filters and sorts to indexed fields
- Propagate `CancellationToken` to databases and HTTP clients
- Keep diagnostic listeners, log scopes, and trace enrichers fast and low-cardinality
- Validate fixes with before-and-after traces

# Escalation checklist

If you need to hand off the incident to another team or file a Hot Chocolate issue, provide:

- Operation name, document hash or ID, and sanitized document (only if allowed)
- Variable shape (no secrets or personal data)
- Trace ID, deployment version, environment, and affected safe segment
- Phase breakdown and dominant span
- Top resolver coordinates from `graphql.field.schema_coordinate`
- DataLoader names, batch sizes, cache hit/miss evidence, and whether keys were inspected (only in safe reproduction)
- Database and external calls, query plans, rows/documents read, and indexes used
- Cost metrics, depth or request-limit errors, and page/filter/sort arguments
- Persisted operation or APQ miss rate and storage type
- Transport, proxy, client timing, and response size
- Reproduction steps and validation table
- Ownership guess: application resolver, data store, platform/proxy, client query, or Hot Chocolate

# Next steps

- [OpenTelemetry tracing](/docs/hotchocolate/v16/operations/observability/opentelemetry): trace setup and span attributes
- [Production logging](/docs/hotchocolate/v16/operations/observability/logging): safe logs and trace correlation
- [Metrics](/docs/hotchocolate/v16/operations/observability/metrics): latency and cost dashboards
- [Performance tuning](/docs/hotchocolate/v16/guides/performance): broader performance practices
- [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader): batching and request-scoped caching
- [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections), [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination), [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering), [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting): data-shaping fixes
- [Warmup](/docs/hotchocolate/v16/server/warmup): startup and cache preparation
- [Request limits](/docs/hotchocolate/v16/securing-your-api/request-limits), [Cost analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis): guardrails
- [Trusted documents](/docs/hotchocolate/v16/performance/trusted-documents), [Automatic persisted operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations): document identity and APQ behavior
