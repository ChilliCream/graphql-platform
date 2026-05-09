---
title: "Query limits"
---

A production GraphQL server needs more than one limit. A request can be too large, too deep, too fragmented, too expensive, too broad through pagination, or too slow after execution starts. Hot Chocolate v16 gives you controls at each stage of the request pipeline so you can reject unbounded work before it reaches the next stage.

This page focuses on Hot Chocolate v16 server operations. Fusion gateway and subgraph tuning are out of scope for this page.

# Start with the layered limit model

Think about query limits as a set of gates. Each gate protects a different resource and runs before the server spends more work.

| Layer                | Risk it reduces                                                                                                    | Primary options                                                                                                                                                                                | Failure phase                      |
| -------------------- | ------------------------------------------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------- |
| Transport            | Large bodies, CSRF-prone GET requests, multipart upload abuse, batched request amplification                       | `AddGraphQL(maxAllowedRequestSize: ...)`, `GraphQLServerOptions.Batching`, `MaxBatchSize`, GET and multipart options                                                                           | HTTP request handling              |
| Parser               | Large documents, too many tokens, too many fields, parser recursion                                                | `ModifyParserOptions(...)`                                                                                                                                                                     | GraphQL parsing                    |
| Validation and shape | Deep selections, recursive schema coordinates, fragment fan-out, alias comparison work, too many validation errors | `AddMaxExecutionDepthRule(...)`, `AddMaxAllowedFieldCycleDepthRule(...)`, `SetMaxAllowedFieldMergeComparisons(...)`, `SetMaxAllowedValidationErrors(...)`, `SetIntrospectionAllowedDepth(...)` | Document validation                |
| Cost analysis        | Resolver work and data fan-out that depth alone does not capture                                                   | `ModifyCostOptions(...)`, `[Cost]`, `[ListSize]`                                                                                                                                               | Validation, before execution       |
| Pagination           | Multiplication through nested lists                                                                                | `ModifyPagingOptions(...)`, `[UsePaging(...)]`                                                                                                                                                 | Validation and resolver middleware |
| Execution            | Accepted operations that occupy execution slots or run too long                                                    | `MaxConcurrentExecutions`, `ExecutionTimeout`, resolver `CancellationToken`                                                                                                                    | Execution                          |
| Trusted documents    | Arbitrary operation shapes from clients you control                                                                | `UsePersistedOperationPipeline()`, `OnlyAllowPersistedDocuments`, `AllowDocumentBody`, `MapGraphQLPersistedOperations()`                                                                       | Request pipeline                   |

Depth, cost, pagination, batching, and concurrency are separate controls. A shallow query can request large pages. A small document can repeat through a batch. A valid operation can still wait behind other executions or call a slow downstream system.

# Prerequisites

You need:

- A Hot Chocolate v16 ASP.NET Core server.
- Representative operations from each client, tenant, or environment you plan to support.
- A decision about the API audience: public, partner-only, or first-party.
- Default security enabled unless you deliberately replace it. `AddGraphQL()` enables default security when `disableDefaultSecurity` is `false`.

The examples use this minimal shape unless a section shows a different route setup:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQL()
    .AddQueryType<Query>();

var app = builder.Build();

app.MapGraphQL();

app.Run();

public sealed class Query
{
    public string Hello() => "world";
}
```

# Apply a production query-limit baseline

Use this as a starting point, then tune it from real operations. The numbers are not universal defaults.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQL(maxAllowedRequestSize: 1_000_000) // 1 MB request body.
    .AddQueryType<Query>()
    .AddMaxExecutionDepthRule(
        maxAllowedExecutionDepth: 10,
        skipIntrospectionFields: true)
    .AddMaxAllowedFieldCycleDepthRule(defaultCycleLimit: 3)
    .ModifyParserOptions(o =>
    {
        o.MaxAllowedFields = 500;
        o.MaxAllowedNodes = 5_000;
        o.MaxAllowedTokens = 10_000;
        o.MaxAllowedDirectives = 4;
        o.MaxAllowedRecursionDepth = 100;
    })
    .ModifyValidationOptions(o =>
    {
        o.MaxAllowedFragmentVisits = 1_000;
    })
    .SetMaxAllowedFieldMergeComparisons(50_000)
    .SetMaxAllowedValidationErrors(5)
    .ModifyCostOptions(o =>
    {
        o.MaxFieldCost = 2_000;
        o.MaxTypeCost = 2_000;
        o.EnforceCostLimits = true;
    })
    .ModifyPagingOptions(o =>
    {
        o.MaxPageSize = 50;
        o.DefaultPageSize = 20;
        o.RequirePagingBoundaries = true;
    })
    .ModifyServerOptions(o =>
    {
        o.Batching = AllowedBatching.None;
        o.MaxBatchSize = 50; // Applies if batching is later enabled.
        o.MaxConcurrentExecutions = 64;
        o.EnforceGetRequestsPreflightHeader = true;
    })
    .ModifyRequestOptions(o =>
    {
        o.ExecutionTimeout = TimeSpan.FromSeconds(10);
    })
    .AddGlobalObjectIdentification(o =>
    {
        o.MaxAllowedNodeBatchSize = 25;
    });
```

Expected behavior:

- Valid representative operations run.
- Oversized bodies fail before parsing.
- Oversized documents fail during parsing.
- Too-deep, cyclic, fragment-heavy, or alias-heavy documents fail validation.
- Over-budget operations fail before resolver execution.
- Missing or too-large page boundaries fail when paging boundaries are required.
- Over-batched requests fail when batching is enabled.
- Long-running operations fail with timeout errors.

# Limit request size and transport amplification

Start at the HTTP layer. Transport limits stop large input and request multiplication before the GraphQL parser or executor spends work.

```csharp
builder
    .AddGraphQL(maxAllowedRequestSize: 1_000_000)
    .ModifyServerOptions(o =>
    {
        o.Batching = AllowedBatching.VariableBatching;
        o.MaxBatchSize = 25;
        o.AllowedGetOperations = AllowedGetOperations.Query;
        o.EnforceGetRequestsPreflightHeader = true;
    });
```

`maxAllowedRequestSize` limits the GraphQL request body read by Hot Chocolate. The default is about 20 MB. Reduce it when your clients send small operations and variables.

Batching is disabled by default in v16 with `AllowedBatching.None`. If you enable `AllowedBatching.VariableBatching`, `AllowedBatching.RequestBatching`, or `AllowedBatching.All`, set a conservative `MaxBatchSize`. A `MaxBatchSize` value of `0` means unlimited and should not be used for public endpoints.

GET requests are enabled by default for queries. Keep `AllowedGetOperations = AllowedGetOperations.Query` unless you have a specific reason to allow mutations over GET. Consider `EnforceGetRequestsPreflightHeader = true` for browser-facing endpoints.

Multipart requests are enabled by default, and multipart preflight enforcement is enabled by default. If your API does not accept uploads, disable multipart requests on the endpoint. Schema SDL and Nitro endpoint availability are production hardening concerns, but they are not query limits. See [Endpoints](/docs/hotchocolate/v16/server/endpoints) for endpoint-specific options.

# Limit parser work

Parser limits run before validation, authorization, and resolver execution. They protect CPU and memory while Hot Chocolate reads the GraphQL document.

```csharp
builder
    .AddGraphQL()
    .ModifyParserOptions(o =>
    {
        o.MaxAllowedFields = 500;
        o.MaxAllowedNodes = 5_000;
        o.MaxAllowedTokens = 10_000;
        o.MaxAllowedDirectives = 4;
        o.MaxAllowedRecursionDepth = 100;
    });
```

| Option                     |          Default | Use it to limit                                                                                                                          |
| -------------------------- | ---------------: | ---------------------------------------------------------------------------------------------------------------------------------------- |
| `MaxAllowedFields`         |           `2048` | Total field selections in a document. This is the most practical size proxy for generated client operations.                             |
| `MaxAllowedNodes`          |   `int.MaxValue` | Total AST nodes produced by the parser.                                                                                                  |
| `MaxAllowedTokens`         |   `int.MaxValue` | Total lexer tokens processed from the request document.                                                                                  |
| `MaxAllowedDirectives`     | `4` per location | Repeatable directives on one field, operation, or fragment definition.                                                                   |
| `MaxAllowedRecursionDepth` |            `200` | Parser recursion through nested selection sets, input values, and type references. This is not execution depth.                          |
| `IncludeLocations`         |           `true` | Error source locations. Turning it off can reduce memory, but it also makes diagnostics less helpful. Treat it as an expert tuning knob. |

A generated operation with hundreds of repeated selections can trip `MaxAllowedFields`. A deeply nested input object or selection set can trip `MaxAllowedRecursionDepth`. These are parser errors, so they happen before validation rules and before cost analysis.

# Limit validation work and query shape

Validation limits reject documents whose shape is expensive even if the document is syntactically valid.

```csharp
builder
    .AddGraphQL()
    .AddMaxExecutionDepthRule(10, skipIntrospectionFields: true)
    .AddMaxAllowedFieldCycleDepthRule(
        defaultCycleLimit: 3,
        coordinateCycleLimits:
        [
            (new SchemaCoordinate("Category", "parent"), 5)
        ])
    .ModifyValidationOptions(o =>
    {
        o.MaxAllowedFragmentVisits = 1_000;
    })
    .SetMaxAllowedFieldMergeComparisons(50_000)
    .SetMaxAllowedValidationErrors(5)
    .SetIntrospectionAllowedDepth(8, 1);
```

Use these controls for different problems:

| Control                                   |                                           Default | What it protects                                                                                              |
| ----------------------------------------- | ------------------------------------------------: | ------------------------------------------------------------------------------------------------------------- |
| `AddMaxExecutionDepthRule(...)`           |                             Off unless you add it | Logical field selection depth, such as `user.friends.friends.friends`.                                        |
| `skipIntrospectionFields: true`           |                              `false` when omitted | Keeps introspection available under a business-field depth limit. Pair it with introspection-specific limits. |
| `AddMaxAllowedFieldCycleDepthRule(...)`   | Enabled by the default production security policy | Repeated schema coordinates such as `User.friends` or `Category.parent`.                                      |
| `MaxAllowedFragmentVisits`                |                                           `1_000` | Repeated fragment spread traversal during validation.                                                         |
| `SetMaxAllowedFieldMergeComparisons(...)` |                                         `100_000` | Work done by the overlapping fields validation rule.                                                          |
| `SetMaxAllowedValidationErrors(...)`      |                                               `5` | Memory and response size from invalid documents that produce many errors.                                     |
| `SetIntrospectionAllowedDepth(8, 1)`      |                                         `16`, `1` | Recursive introspection through `ofType` and introspection list fields.                                       |

Execution depth and parser recursion depth are different. Parser recursion counts nested syntax while the parser reads a document. Execution depth counts the logical selection path against your schema during validation.

# Set cost budgets for operation complexity

Cost analysis estimates work before execution. It catches cases that depth alone cannot catch, such as a shallow query that asks for a large page or calls a high-cost resolver.

```csharp
builder
    .AddGraphQL()
    .ModifyCostOptions(o =>
    {
        o.MaxFieldCost = 2_000;
        o.MaxTypeCost = 2_000;
        o.EnforceCostLimits = true;
    });

[Cost(50)]
public static Task<Report> GetReportAsync(
    ReportService reports,
    CancellationToken cancellationToken)
    => reports.CreateReportAsync(cancellationToken);

[ListSize(
    AssumedSize = 100,
    SlicingArguments = ["first", "last"],
    SizedFields = ["edges", "nodes"])]
public static IEnumerable<Book> GetBooks()
    => [];
```

Hot Chocolate computes two metrics:

- **Field cost** estimates resolver and field execution impact.
- **Type cost** estimates data fan-out and object materialization impact.

The default budgets are `MaxFieldCost = 1000` and `MaxTypeCost = 1000`, with `EnforceCostLimits = true`. Async resolvers receive a default cost of `10.0`. Filtering and sorting also add cost.

Use `[Cost]` or `.Cost(...)` for resolvers that call external systems, run expensive database queries, or aggregate data. Use `[ListSize]` or `.ListSize(...)` for custom list fields, especially when they are not Hot Chocolate paging fields.

Use the `GraphQL-Cost` header to observe costs:

| Header value | Behavior                                                        |
| ------------ | --------------------------------------------------------------- |
| `report`     | Executes the request and includes cost metrics in `extensions`. |
| `validate`   | Returns cost metrics without executing resolvers.               |

Do not use `SkipAnalyzer` for public traffic. `SkipAnalyzer = true` disables the analyzer and enforcement. If you need a measurement window, prefer `EnforceCostLimits = false` so cost metrics are still reported.

See [Cost Analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis) for the full cost model.

# Bound pagination fan-out

Nested pages multiply. If a query asks for `products(first: 50)` and each product asks for `reviews(first: 50)`, the nested review resolver can see up to 2,500 items before deeper selections add more work.

```csharp
builder
    .AddGraphQL()
    .ModifyPagingOptions(o =>
    {
        o.MaxPageSize = 50;
        o.DefaultPageSize = 20;
        o.RequirePagingBoundaries = true;
    });

[UsePaging(MaxPageSize = 10, DefaultPageSize = 10)]
public static IQueryable<Review> GetReviews(
    [Parent] Product product,
    CatalogContext db)
    => db.Reviews
        .Where(r => r.ProductId == product.Id)
        .OrderBy(r => r.Id);
```

`MaxPageSize` defaults to `50`, `DefaultPageSize` defaults to `10`, and `RequirePagingBoundaries` defaults to `false`. For public APIs, require clients to send `first` or `last` so they state the requested page size.

Cost analysis uses `MaxPageSize` as the assumed size for paginated fields. Lower `MaxPageSize` on nested or expensive relationships to reduce both actual fan-out and estimated cost.

Be intentional with `IncludeTotalCount = true`. Counting can be expensive depending on your data source. Prefer deterministic ordering and cursor-based navigation. Avoid API designs that force clients into random access over unbounded offsets. Pass `CancellationToken` through service-layer paging so timeouts can stop downstream work.

See [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination) for pagination options and cursor behavior.

# Bound execution time and concurrency

Some operations pass validation and cost analysis but still run too long or wait behind other work. Bound both execution time and concurrent executions.

```csharp
builder
    .AddGraphQL()
    .ModifyServerOptions(o =>
    {
        o.MaxConcurrentExecutions = 64;
    })
    .ModifyRequestOptions(o =>
    {
        o.ExecutionTimeout = TimeSpan.FromSeconds(10);
    });

public static Task<IReadOnlyList<Product>> GetProductsAsync(
    ProductService products,
    CancellationToken cancellationToken)
    => products.GetProductsAsync(cancellationToken);
```

`ExecutionTimeout` defaults to 30 seconds, or 30 minutes when a debugger is attached. Values below 100 ms are clamped to 100 ms. In v16, the timeout also covers time spent waiting for the execution concurrency gate.

`MaxConcurrentExecutions` defaults to `64`. `null` disables the concurrency gate, and tests also cover `0` as disabled. Avoid disabled concurrency in production.

Concurrency limits are not a replacement for API gateway rate limits, tenant quotas, or identity-aware throttling. They protect the Hot Chocolate execution engine on this server instance. Resolver code, EF queries, HTTP calls, and paging services should accept and pass `CancellationToken` so timeout cancellation reaches downstream work.

# Limit `nodes` batch lookups

If you use Relay global object identification, the plural `nodes(ids: [ID!]!)` field can fan out into many object lookups.

```csharp
builder
    .AddGraphQL()
    .AddGlobalObjectIdentification(o =>
    {
        o.MaxAllowedNodeBatchSize = 25;
    });
```

`MaxAllowedNodeBatchSize` defaults to `50`. In v16, configure it through `AddGlobalObjectIdentification(...)`. Requests with too many IDs in `nodes` fail validation.

If your API contract does not need the plural `nodes` field, evaluate whether `AddNodesField = false` fits your clients.

# Use trusted documents when you control the clients

Limits reduce the blast radius of dynamic operations. Trusted documents remove arbitrary operation shapes from production traffic for clients you control.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddNitro()
    .UsePersistedOperationPipeline()
    .ModifyRequestOptions(o =>
    {
        o.PersistedOperations.OnlyAllowPersistedDocuments = true;
        o.PersistedOperations.AllowDocumentBody = false;
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapGraphQL();
}

app.MapGraphQLPersistedOperations();

app.Run();
```

`UsePersistedOperationPipeline()` loads registered operation documents by ID. `OnlyAllowPersistedDocuments = true` requires the operation to be registered. `AllowDocumentBody = false` skips reading untrusted GraphQL document bodies. This is the default, but setting it explicitly documents the production posture.

`MapGraphQLPersistedOperations()` exposes persisted operation routes, such as `/graphql/persisted/{operationId}` and `/graphql/persisted/{operationId}/{operationName}`. Keep `MapGraphQL()` for development only if you need ad-hoc operations, Nitro, or schema exploration.

Use `AllowNonPersistedOperation()` only from server-side policy, such as a development environment, an authenticated admin route, or a short migration window. Do not use an unauthenticated header as a production bypass.

Keep transport, timeout, concurrency, paging, and resolver cancellation limits. Trusted documents do not bound variables, batch size, or slow downstream systems by themselves.

See [Trusted Documents](/docs/hotchocolate/v16/performance/trusted-documents) and [First-Party API](/docs/hotchocolate/v16/guides/private-api) for the full setup.

# Apply role-specific limits only from server-side policy

Hot Chocolate does not provide a single declarative role matrix for every query limit. Some limits are server-wide, and some can be overridden per request.

Cost and execution depth can be overridden through `OperationRequestBuilder` from an HTTP request interceptor. Depth overrides require `allowRequestOverrides: true` when you register the depth rule.

```csharp
builder
    .AddGraphQL()
    .AddMaxExecutionDepthRule(
        maxAllowedExecutionDepth: 10,
        allowRequestOverrides: true)
    .AddHttpRequestInterceptor<TieredLimitsInterceptor>();

public sealed class TieredLimitsInterceptor : DefaultHttpRequestInterceptor
{
    public override ValueTask OnCreateAsync(
        HttpContext context,
        IRequestExecutor requestExecutor,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        if (context.User.IsInRole("PartnerApi"))
        {
            requestBuilder.SetMaximumAllowedExecutionDepth(15);

            var cost = requestExecutor.GetCostOptions();
            requestBuilder.SetCostOptions(cost with
            {
                MaxFieldCost = 5_000,
                MaxTypeCost = 5_000
            });
        }

        return base.OnCreateAsync(
            context,
            requestExecutor,
            requestBuilder,
            cancellationToken);
    }
}
```

Use authenticated claims, client registry metadata, or environment checks. Never trust a user-supplied header by itself. Prefer strict defaults and narrow raises for trusted clients. Do not expose `SkipExecutionDepthAnalysis()` or cost analyzer bypasses to public traffic.

Parser limits, HTTP body size, server batching options, and server concurrency are server-wide in the documented APIs covered here.

# Tune limits by environment

Bind limit values from configuration so staging and production can be adjusted without code edits.

```csharp
var limits = builder.Configuration
    .GetSection("GraphQL:Limits")
    .Get<QueryLimitOptions>()
    ?? new QueryLimitOptions();

builder
    .AddGraphQL(maxAllowedRequestSize: limits.MaxAllowedRequestSize)
    .AddQueryType<Query>()
    .AddMaxExecutionDepthRule(limits.MaxExecutionDepth)
    .ModifyCostOptions(o =>
    {
        o.MaxFieldCost = limits.MaxFieldCost;
        o.MaxTypeCost = limits.MaxTypeCost;
    })
    .ModifyPagingOptions(o =>
    {
        o.MaxPageSize = limits.MaxPageSize;
        o.DefaultPageSize = limits.DefaultPageSize;
        o.RequirePagingBoundaries = limits.RequirePagingBoundaries;
    })
    .ModifyServerOptions(o =>
    {
        o.MaxConcurrentExecutions = limits.MaxConcurrentExecutions;
        o.MaxBatchSize = limits.MaxBatchSize;
    })
    .ModifyRequestOptions(o =>
    {
        o.ExecutionTimeout = TimeSpan.FromSeconds(limits.ExecutionTimeoutSeconds);
    });

public sealed class QueryLimitOptions
{
    public int MaxAllowedRequestSize { get; set; } = 1_000_000;

    public int MaxExecutionDepth { get; set; } = 10;

    public double MaxFieldCost { get; set; } = 2_000;

    public double MaxTypeCost { get; set; } = 2_000;

    public int MaxPageSize { get; set; } = 50;

    public int DefaultPageSize { get; set; } = 20;

    public bool RequirePagingBoundaries { get; set; } = true;

    public int? MaxConcurrentExecutions { get; set; } = 64;

    public int MaxBatchSize { get; set; } = 50;

    public int ExecutionTimeoutSeconds { get; set; } = 10;
}
```

Recommended environment posture:

| Environment | Recommended behavior                                                                                                                                           |
| ----------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Development | Allow Nitro, introspection, dynamic operations, and longer timeouts so developers can explore the schema.                                                      |
| Staging     | Enforce production-like transport, parser, validation, paging, batching, timeout, and concurrency limits. Collect cost reports from representative operations. |
| Production  | Enforce budgets, use trusted documents for first-party APIs, keep page sizes conservative, avoid unlimited batches, and keep the concurrency gate enabled.     |

Do not set `disableDefaultSecurity: true` unless you deliberately replace the default protections and document the replacement.

# Verify limits before rollout

Before you tighten limits, measure real operations and run adversarial probes.

Use `GraphQL-Cost: report` to execute and return cost metrics. Use `GraphQL-Cost: validate` to return cost metrics without executing resolvers.

```http
POST /graphql HTTP/1.1
Content-Type: application/json
GraphQL-Cost: validate

{"query":"query Products { products(first: 20) { nodes { id name } } }"}
```

Expected result: the response includes cost metrics in `extensions`, and resolvers are not executed when the header value is `validate`.

Use this probe matrix as a rollout checklist:

| Probe                                                                   | Expected rejection or signal                              |
| ----------------------------------------------------------------------- | --------------------------------------------------------- |
| Body over `maxAllowedRequestSize`                                       | HTTP request fails before GraphQL parsing.                |
| Document over parser field, node, token, directive, or recursion budget | Parser error.                                             |
| Selection over execution depth                                          | Validation error.                                         |
| Repeated schema coordinate over cycle depth                             | Validation error.                                         |
| Fragment spread fan-out over `MaxAllowedFragmentVisits`                 | Validation error.                                         |
| Alias-heavy document over field merge comparisons                       | Validation error.                                         |
| Too many invalid selections                                             | Validation stops at `SetMaxAllowedValidationErrors(...)`. |
| Page request over `MaxPageSize`                                         | Paging validation error.                                  |
| Missing `first` or `last` when boundaries are required                  | Paging validation error.                                  |
| Operation over `MaxFieldCost` or `MaxTypeCost`                          | Cost error before resolver execution.                     |
| Batch over `MaxBatchSize`                                               | Transport-level batch rejection.                          |
| `nodes` request over `MaxAllowedNodeBatchSize`                          | Validation error.                                         |
| Slow resolver over `ExecutionTimeout`                                   | Timeout error and cancellation token is signaled.         |
| Unknown persisted operation ID                                          | Persisted-operation rejection.                            |
| Partner-only override from an untrusted request                         | Default public limits still apply.                        |

# Monitor rejected operations and tune safely

Query-limit rejections are operational signals. Track the reason and the client context so you can distinguish attack traffic, generated-client regressions, and valid operations that need a budget change.

| Metric or log field          | Useful dimensions                                    | Action                                                                                                                 |
| ---------------------------- | ---------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------- |
| Request body rejected        | Route, client name, content length                   | Check generated documents, variables, batching, and upload paths.                                                      |
| Parser limit rejected        | Limit name, operation name, document hash            | Inspect generated fragments and repeated selections.                                                                   |
| Validation limit rejected    | Rule name, operation name, client version            | Split the operation, flatten the shape, or tune a narrow rule.                                                         |
| Cost rejected                | Field cost, type cost, configured budgets            | Run `GraphQL-Cost: validate`, lower nested page caps, add `[Cost]` or `[ListSize]`, or grant a server-side override.   |
| Paging rejected              | Field name, requested page size, max page size       | Reduce client page size or lower per-field fan-out.                                                                    |
| Batch rejected               | Batch mode, requested count, max count               | Fix generated client behavior or reduce batch size.                                                                    |
| Timeout                      | Operation name, elapsed time, wait time if available | Propagate cancellation, optimize resolvers, add DataLoader batching where appropriate, and inspect downstream latency. |
| Persisted operation rejected | Operation ID, operation name, client version         | Verify publication, hash algorithm, route, and client deployment.                                                      |

Avoid logging full variables or operation text when they can contain sensitive data. Prefer operation names, persisted operation IDs, hashes, client versions, tenant IDs, and authenticated subjects when safe for your privacy model.

# Troubleshoot rejected operations

| Symptom                                          | Likely limit                                             | Diagnostic                                                                                        | Preferred fix                                                                                             | Avoid                                                                  |
| ------------------------------------------------ | -------------------------------------------------------- | ------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------- |
| Large request fails before GraphQL errors appear | Request body size                                        | Compare content length with `maxAllowedRequestSize`                                               | Reduce variables, remove operation text with trusted documents, split uploads or batches                  | Raising the body limit for every client without measuring payloads     |
| Parser error on generated operation              | Parser field, node, token, directive, or recursion limit | Count selections and inspect generated fragments                                                  | Reduce repeated selections, fragment expansion, or recursion                                              | Disabling parser limits                                                |
| Valid-looking nested query fails validation      | Execution depth or field cycle depth                     | Inspect the selection path and repeated schema coordinates                                        | Flatten the selection, split the operation, or raise the limit for trusted clients only                   | Public `SkipExecutionDepthAnalysis()`                                  |
| Fragment-heavy query fails validation            | Fragment visits or field merge comparisons               | Expand fragments and check alias duplication                                                      | Reduce spread fan-out and duplicate aliases                                                               | Raising comparison limits without understanding the generated document |
| Shallow query exceeds cost                       | Cost analysis                                            | Send `GraphQL-Cost: validate`                                                                     | Lower nested `MaxPageSize`, annotate expensive fields, add accurate list sizes, or raise budgets narrowly | `SkipAnalyzer` in production                                           |
| Page request fails                               | Paging cap or required boundaries                        | Check `first`, `last`, and field-specific `MaxPageSize`                                           | Add a boundary or request fewer items                                                                     | Increasing global page size for one field                              |
| Operation times out                              | Execution timeout or concurrency wait                    | Check resolver latency and concurrent load                                                        | Pass cancellation tokens, optimize data access, and tune concurrency                                      | Treating concurrency limits as rate limiting                           |
| Trusted document request is rejected             | Persisted operation policy                               | Verify operation ID, operation name, hash provider, storage publication, route, and client format | Publish the operation and send the expected persisted route or ID                                         | Re-enabling dynamic operations for all production traffic              |

# Test the limit configuration

Add tests before tightening production limits. Each boundary should have a negative probe and a representative valid operation.

| Test area         | Positive case                                          | Negative case                                                                |
| ----------------- | ------------------------------------------------------ | ---------------------------------------------------------------------------- |
| Request body      | A normal operation with typical variables succeeds     | Oversized body is rejected                                                   |
| Parser            | Representative generated operation parses              | Document over field, token, node, directive, or recursion budget is rejected |
| Validation depth  | Normal nested operation succeeds                       | Over-depth operation is rejected                                             |
| Field cycle       | Approved self-reference depth succeeds                 | Repeated coordinate over the cycle limit is rejected                         |
| Cost              | Representative operation is under budget               | High page size or high-cost resolver exceeds `MaxFieldCost` or `MaxTypeCost` |
| Pagination        | `first` or `last` within limits succeeds               | Missing boundary or oversized page is rejected                               |
| Batching          | Allowed batch size succeeds when batching is enabled   | Batch over `MaxBatchSize` is rejected                                        |
| Node batch        | `nodes` request within limit succeeds                  | `nodes` request over `MaxAllowedNodeBatchSize` is rejected                   |
| Timeout           | Fast resolver succeeds                                 | Slow cooperative resolver observes cancellation                              |
| Trusted documents | Known operation ID executes                            | Unknown ID and dynamic document are rejected                                 |
| Role override     | Partner request receives the higher server-side budget | Public request with the same operation uses default limits                   |

# Next steps

- Read [Limit query depth, cost, and complexity](/docs/hotchocolate/v16/build/security/limit-query-depth-cost-and-complexity) for the conceptual build-time security track.
- Use [Request Limits](/docs/hotchocolate/v16/securing-your-api/request-limits) for focused parser, validation, timeout, and `nodes` reference.
- Use [Cost Analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis) for the full cost model and annotations.
- Use [Introspection](/docs/hotchocolate/v16/securing-your-api/introspection) for schema discovery and introspection controls.
- Use [Trusted Documents](/docs/hotchocolate/v16/performance/trusted-documents) and [First-Party API](/docs/hotchocolate/v16/guides/private-api) when you control the clients.
- Use [Batching](/docs/hotchocolate/v16/server/batching), [Endpoints](/docs/hotchocolate/v16/server/endpoints), [Interceptors](/docs/hotchocolate/v16/server/interceptors), and [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination) for the related server features.
