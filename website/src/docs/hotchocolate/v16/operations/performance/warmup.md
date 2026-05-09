---
title: Warmup
---

# Hot Chocolate v16: Warming Up Your Server

In production, you want your Hot Chocolate server to be ready before it starts handling real traffic. In v16, the schema and request executor are created during application startup by default. You can also warm up specific operations—parsing, validating, and compiling them ahead of time—so the first user request avoids cold paths.

This guide focuses on Hot Chocolate server warmup for ASP.NET Core. Fusion gateway warmup uses different APIs and is not covered here.

## Why Warm Up?

A smooth production rollout typically follows this sequence:

1. The container or process starts.
2. Hot Chocolate builds the schema and creates the request executor.
3. Optional warmup tasks compile and cache selected operations.
4. Readiness turns green.
5. The first user request skips schema, executor, parse, validate, and compile cold paths for warmed operations.

Warmup moves work from the first user request to startup. It does not eliminate the work, so keep your warmup list in line with your startup budget and service-level objectives.

Use operation warmup for a small set of high-value requests—such as homepage queries, common persisted operations, or expensive query shapes. Warmup is not a substitute for general performance tuning, DataLoader design, database connection management, or observability.

## Prerequisites

Before configuring operation warmup, ensure you have:

- A Hot Chocolate v16 ASP.NET Core server set up with `AddGraphQLServer()` or equivalent.
- A GraphQL endpoint mapped with `app.MapGraphQL()`.
- A short list of representative production operations.
- Operation names and document IDs or hashes for persisted operations.
- Access to application logs, diagnostic events, or OpenTelemetry traces.
- Control over readiness or startup probes if deploying behind Kubernetes, a load balancer, or an autoscaler.

## Start with the v16 Eager Initialization Default

Most production deployments should use the v16 default setup:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>();

var app = builder.Build();

app.MapGraphQL();
app.Run();
```

With this configuration:

- Hot Chocolate creates the schema and request executor during application startup.
- Schema configuration errors fail startup and appear in the logs.
- You do not need to call `.InitializeOnStartup()` in v16.

`LazyInitialization` is `false` by default. The ASP.NET Core warmup service ensures Hot Chocolate creates each configured executor at startup, removing schema and executor cold starts from the first GraphQL request.

However, this default does not precompile every client operation. The first request for a specific operation may still incur parsing, validation, compilation, and cache population costs unless you warm up that operation.

# Warm Common Operations Without Executing Resolvers

To prepopulate execution caches without running field resolvers, use `AddWarmupTask` with a marked warmup request:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddWarmupTask(async (executor, cancellationToken) =>
    {
        var request = OperationRequestBuilder.New()
            .SetDocument("""
                query GetProducts {
                  products(first: 10) {
                    nodes { id name }
                  }
                }
                """)
            .SetDocumentId("products-homepage-v1")
            .SetOperationName("GetProducts")
            .MarkAsWarmupRequest()
            .Build();

        await executor.ExecuteAsync(request, cancellationToken);
    });
```

What happens:

- Startup waits for the warmup task to finish.
- The marked request fills the document and prepared operation caches.
- Resolver side effects do not run; the request returns a warmup result before execution.
- Later requests with the same document ID and operation name benefit from the warmed caches.

`MarkAsWarmupRequest()` is essential for safe operation warmup. It allows the pipeline to parse, validate, and compile the operation, but stops before resolver execution. Always use queries (not mutations) for warmup.

To maximize cache hits, match the request shape to what clients will send:

- Use the same document ID or hash as your clients.
- Use the same operation name, since the prepared operation cache key includes it.
- Pass the provided cancellation token so startup cancellation and shutdown do not hang behind your warmup code.

Variables are not part of the prepared operation cache key, but variable coercion still happens per request. Even a warmed operation can spend time coercing large or complex variable values.

# Warm Persisted Operations by Document ID

If you use persisted operations or trusted documents, you can warm up specific operations when you know in advance which documents your clients will send:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .UsePersistedOperationPipeline()
    .AddWarmupTask(async (executor, cancellationToken) =>
    {
        var request = OperationRequestBuilder.New()
            .SetDocument("query Viewer { viewer { id displayName } }")
            .SetDocumentId("<same-id-or-hash-the-client-sends>")
            .SetOperationName("Viewer")
            .MarkAsWarmupRequest()
            .Build();

        await executor.ExecuteAsync(request, cancellationToken);
    });
```

With this approach:

- Startup can warm the known operation, even if your persisted-operation pipeline would reject non-persisted client requests.
- The first client request with the same document ID or hash and operation name uses the warmed execution caches.
- Warmup does not publish documents to any external registry or storage system.

Warmup requests can bypass the persisted-document-only check because they are internal startup work. Never expose a client feature that allows remote callers to mark a request as a warmup request.

For more on publishing and storage, see [trusted documents](/docs/hotchocolate/v16/operations/security-hardening/trusted-documents) and [automatic persisted operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations).

# Choose Safe Operations to Warm

Select a small, intentional set of operations to warm. The best candidates are high-traffic, stable documents with explicit operation names and known document IDs (if using persisted operations).

| Operation shape                | Warmup fit  | Guidance                                                                                      |
| ------------------------------ | ----------- | --------------------------------------------------------------------------------------------- |
| Homepage query                 | High        | Warm if it is common and read-only.                                                           |
| Generated persisted operation  | High        | Warm if the document ID or hash is stable and known at deployment.                            |
| Expensive representative query | Medium      | Warm one or two shapes that impact first-request SLOs.                                        |
| Admin-only query               | Conditional | Warm only if validation does not require per-user context, or model the context deliberately. |
| Mutation                       | Low         | Avoid mutations. Use a read-only query that covers similar schema paths.                      |
| Subscription                   | Low         | Do not use startup warmup for subscription connection behavior.                               |

Marked warmup requests do not execute resolvers. They do not warm databases, DataLoaders, HTTP clients, authentication providers, or downstream caches. If those systems need their own startup strategy, configure and measure them separately.

Review your warmup list after schema changes, client operation changes, cache-size adjustments, or traffic shifts. Warming too many operations can slow rollout and scale-out.

# Register Reusable Warmup Tasks

If you need to warm up multiple operations or require constructor injection, move your warmup logic into an `IRequestExecutorWarmupTask`:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddWarmupTask<ProductOperationWarmupTask>();

public sealed class ProductOperationWarmupTask : IRequestExecutorWarmupTask
{
    public bool ApplyOnlyOnStartup => false;

    public async Task WarmupAsync(
        IRequestExecutor executor,
        CancellationToken cancellationToken)
    {
        var request = OperationRequestBuilder.New()
            .SetDocument("query GetProducts { products(first: 10) { nodes { id name } } }")
            .SetDocumentId("products-homepage-v1")
            .SetOperationName("GetProducts")
            .MarkAsWarmupRequest()
            .Build();

        await executor.ExecuteAsync(request, cancellationToken);
    }
}
```

What to expect:

- The task runs before the initial executor is ready.
- If the executor is rebuilt later, the old executor keeps serving requests until the replacement finishes warmup.
- With `ApplyOnlyOnStartup` set to `false`, the task also runs for later executor creations.

Set `ApplyOnlyOnStartup => true` if you want the task to run only for the initial executor. Delegate warmup tasks registered with `AddWarmupTask((executor, ct) => ...)` run for each new executor.

You can register warmup tasks as delegates, task instances, generic types, factories, or with a `skipIf` condition. The `skipIf` function receives the application service provider, so you can check environment or configuration:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddWarmupTask<LargeWarmupTask>(
        services => services.GetRequiredService<IHostEnvironment>().IsDevelopment());
```

Here, `LargeWarmupTask` is not registered in development, but can run in other environments.

Warmup tasks run sequentially for a single executor. Keep each task bounded, log progress, and avoid unbounded external calls.

# Access Application Services from Warmup Tasks

Warmup tasks are activated from Hot Chocolate schema services. While Hot Chocolate services like `IDocumentCache` and `IPreparedOperationCache` are available by default, you must explicitly make your own application services available:

```csharp
builder.Services.AddSingleton<WarmupOperationCatalog>();

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddApplicationService<WarmupOperationCatalog>()
    .AddWarmupTask<CatalogWarmupTask>();

public sealed class CatalogWarmupTask(
    IDocumentCache documentCache,
    IPreparedOperationCache operationCache,
    WarmupOperationCatalog catalog) : IRequestExecutorWarmupTask
{
    public bool ApplyOnlyOnStartup => false;

    public async Task WarmupAsync(
        IRequestExecutor executor,
        CancellationToken cancellationToken)
    {
        foreach (var operation in catalog.GetOperations())
        {
            var request = OperationRequestBuilder.New()
                .SetDocument(operation.Document)
                .SetDocumentId(operation.DocumentId)
                .SetOperationName(operation.OperationName)
                .MarkAsWarmupRequest()
                .Build();

            await executor.ExecuteAsync(request, cancellationToken);
        }
    }
}
```

With this setup:

- The task can resolve both schema services and the application `WarmupOperationCatalog`.
- Startup does not fail with a missing-service activation error for `WarmupOperationCatalog`.

`AddApplicationService<T>()` resolves the application service once during schema initialization and registers it in schema services. In factory scenarios, you can also use `GetRootServiceProvider()` from the schema service provider if you need access to application services intentionally.

# Coordinate Warmup with Readiness and Startup Probes

Expose a readiness endpoint and ensure your platform allows enough time for startup warmup:

```csharp
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/health/ready");
app.MapGraphQL();
app.Run();
```

Example Kubernetes probes:

```yaml
startupProbe:
  httpGet:
    path: /health/ready
    port: 8080
  failureThreshold: 30
  periodSeconds: 2
readinessProbe:
  httpGet:
    path: /health/ready
    port: 8080
  periodSeconds: 5
```

With this setup, new pods receive traffic only after the ASP.NET Core app is running and startup warmup has completed.

Use a startup probe if schema build or operation warmup might exceed default probe grace periods. Set thresholds based on your measured worst-case startup time plus some headroom. If startup fails due to schema creation or a warmup task error, readiness should never turn green for that instance.

ASP.NET Core health checks do not automatically validate every future dynamic schema rebuild. If your app rebuilds schemas at runtime, add custom health logic only if you have a concrete readiness contract to enforce.

# Tune Cold Starts for Your Hosting Model

Different hosting models have different priorities for startup time and first-request latency.

| Hosting model                        | Recommended warmup strategy                                                                                      |
| ------------------------------------ | ---------------------------------------------------------------------------------------------------------------- |
| Kubernetes                           | Use eager initialization, targeted operation warmup, startup probes, and readiness probes.                       |
| Long-running containers or VMs       | Use the v16 default and add operation warmup for first-request SLOs.                                             |
| Autoscaling                          | Warm only operations that materially affect SLOs. Too much warmup slows scale-out.                               |
| Serverless or strict startup budgets | Keep warmup small. Compare eager startup with provisioned or minimum instances before using lazy initialization. |
| Local development                    | Use the default. Skip expensive tasks with `skipIf` when they slow feedback loops.                               |

Eager initialization increases startup work but reduces first GraphQL request latency. Lazy initialization can make the process appear to start sooner, but it moves schema and executor creation to the first GraphQL request. Use lazy initialization only for constrained platforms, not as the default in production.

# Use Lazy Initialization Only for Special Cases

Enable lazy initialization only if your platform's startup budget is more important than first-request latency:

```csharp
builder.Services
    .AddGraphQLServer()
    .ModifyOptions(options =>
    {
        options.LazyInitialization = true;
    });
```

With this setting:

- Process startup can finish with less GraphQL startup work.
- The first request or first executor access creates the schema and request executor.
- Registered warmup tasks run when the executor is created, so the first GraphQL request pays the warmup cost.

Avoid lazy initialization for normal production behind load balancers. It can block initial requests and delay schema error detection until runtime.

# Export the Schema During Startup

You can export the schema as part of startup using `ExportSchemaOnStartup()`, which is implemented as a warmup task:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .ExportSchemaOnStartup("./schema.graphql");
```

This writes the schema SDL to `./schema.graphql` during startup.

If you omit the path, Hot Chocolate writes `schema.graphqls` in the current directory. The export runs on startup and after later schema changes unless you skip it. Use this for CI/CD or schema registry workflows, and account for file-system permissions and startup I/O.

# Measure Cold-Start and First-Request Latency

Measure your application's performance before and after adding operation warmup:

1. Record application startup duration from process start to readiness.
2. Record the duration of each warmup task.
3. Send the first real GraphQL request after readiness and record its latency.
4. Send the same request again and compare latency.
5. Check diagnostic events for cache additions during warmup and cache retrievals during real requests.

You can use a diagnostic listener to tag and time warmup requests:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddDiagnosticEventListener<WarmupTimingListener>();

public sealed class WarmupTimingListener : ExecutionDiagnosticEventListener
{
    public override IDisposable ExecuteRequest(RequestContext context)
    {
        return new RequestTimer(context, Stopwatch.StartNew());
    }

    private sealed class RequestTimer(
        RequestContext context,
        Stopwatch stopwatch) : IDisposable
    {
        public void Dispose()
        {
            stopwatch.Stop();
            var kind = context.IsWarmupRequest() ? "warmup" : "user";
            Console.WriteLine($"GraphQL {kind} request completed in {stopwatch.ElapsedMilliseconds} ms");
        }
    }
}
```

Example output:

```text
GraphQL warmup request completed in 42 ms
GraphQL user request completed in 8 ms
```

Use diagnostic events like `ParseDocument`, `ValidateDocument`, `CompileOperation`, `AddedDocumentToCache`, `RetrievedDocumentFromCache`, `AddedOperationToCache`, `RetrievedOperationFromCache`, and `ExecutorCreated` to see where time is spent. Cache-add events should appear during warmup for matching operations; later real requests should show retrieval events.

Diagnostic handlers run synchronously as part of request processing. Avoid exporting telemetry or doing expensive work inline. For OpenTelemetry setup, see [OpenTelemetry](/docs/hotchocolate/v16/operations/observability/opentelemetry) and [diagnostic events](/docs/hotchocolate/v16/operations/observability/diagnostics-events).

Comparison table:

| Measurement             | Before operation warmup                       | After operation warmup                |
| ----------------------- | --------------------------------------------- | ------------------------------------- |
| Startup duration        | Lower                                         | Higher by warmup task duration        |
| First matching request  | Pays parse, validate, compile, and cache fill | Can hit document and operation caches |
| Second matching request | Usually warm                                  | Usually warm                          |

# Troubleshooting Warmup Problems

| Symptom                                  | Likely cause                                                                                                                                 | Fix                                                                                                                                                      |
| ---------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Startup fails with a schema error        | Eager initialization surfaced invalid schema or configuration.                                                                               | Fix the schema before rollout. This is the v16 default working as intended.                                                                              |
| Startup exceeds probe timeout            | Schema build or operation warmup takes longer than the probe budget.                                                                         | Reduce the warmup list, increase startup probe thresholds, split expensive work, and inspect schema build time.                                          |
| Warmup request executes side effects     | The request was not marked as a warmup request, or external warmup code ran outside the GraphQL resolver pipeline.                           | Add `.MarkAsWarmupRequest()`, use read-only queries, and keep side-effecting work out of operation warmup.                                               |
| First request is still slow              | Document ID mismatch, operation name mismatch, operation not warmed, cache eviction, variable coercion cost, or downstream systems are cold. | Warm with matching document ID and operation name, increase cache sizes if needed, and warm downstream systems separately.                               |
| Persisted operation request misses cache | The warmup document ID or hash does not match the client path, or storage/hash configuration differs.                                        | Warm with the same document ID or hash and operation name that clients send. Verify the persisted operation pipeline.                                    |
| Warmup task cannot resolve a service     | The task is activated from schema services, not the application service provider.                                                            | Add `.AddApplicationService<T>()`, inject schema services, or use a factory with `GetRootServiceProvider()` intentionally.                               |
| Dynamic schema rebuild causes latency    | Rebuild warmup tasks were skipped or the replacement executor had no matching cache entries.                                                 | Use `ApplyOnlyOnStartup => false` for tasks needed after rebuild. The old executor serves requests until the warmed replacement is ready.                |
| Startup hangs or cancels                 | Warmup code waits on unbounded external work or ignores cancellation.                                                                        | Pass cancellation tokens, add timeouts around external calls, and log around each task.                                                                  |
| Cache fills or churns                    | Too many operations are warmed for the configured cache sizes.                                                                               | Warm fewer operations or increase `PreparedOperationCacheSize` and `OperationDocumentCacheSize`. The default for each is `256`, and the minimum is `16`. |

You can configure cache sizes through schema options:

```csharp
builder.Services
    .AddGraphQLServer()
    .ModifyOptions(options =>
    {
        options.PreparedOperationCacheSize = 1024;
        options.OperationDocumentCacheSize = 1024;
    });
```

For more details, see the [options reference](/docs/hotchocolate/v16/api-reference/options).

# Migrate v15 Warmup Code to v16

When migrating to v16, remove `InitializeOnStartup()`. Eager schema and executor initialization is now the default.

```diff
builder.Services.AddGraphQLServer()
-    .InitializeOnStartup(warmup: (executor, ct) => { /* ... */ });
+    .AddWarmupTask((executor, ct) => { /* ... */ });
```

If your v15 warmup callback should also run after later executor rebuilds, use a delegate warmup task or a custom task with `ApplyOnlyOnStartup => false`. If it should run only for the initial executor, implement `IRequestExecutorWarmupTask` and return `true` from `ApplyOnlyOnStartup`.

Warmup tasks that need application services may require `.AddApplicationService<T>()` because v16 separates schema services from application services.

For full migration details, see [migrate from v15 to v16](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16#eager-initialization-by-default).

# Next Steps

- [ASP.NET Core hosting](/docs/hotchocolate/v16/operations/deployment/aspnetcore-hosting)
- [Kubernetes deployment](/docs/hotchocolate/v16/operations/deployment/kubernetes)
- [Serverless deployment](/docs/hotchocolate/v16/operations/deployment/serverless)
- [OpenTelemetry](/docs/hotchocolate/v16/operations/observability/opentelemetry)
- [Diagnostic events](/docs/hotchocolate/v16/operations/observability/diagnostics-events)
- [Health checks](/docs/hotchocolate/v16/operations/observability/health-checks)
- [Trusted documents](/docs/hotchocolate/v16/operations/security-hardening/trusted-documents)
- [Automatic persisted operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations)
- [Options reference](/docs/hotchocolate/v16/api-reference/options)
- [Dynamic schemas](/docs/hotchocolate/v16/building-a-schema/dynamic-schemas)
- [Migrate from v15 to v16](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16#eager-initialization-by-default)
