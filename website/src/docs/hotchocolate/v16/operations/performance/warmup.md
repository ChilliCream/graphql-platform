---
title: Warmup
---

Production warmup makes a new Hot Chocolate server useful before traffic reaches it. In v16 the schema and request executor are created during application startup by default. You can add operation warmup tasks when you also want common operations parsed, validated, and compiled before the first user request.

This page covers Hot Chocolate server warmup for ASP.NET Core. Fusion gateway warmup uses separate gateway APIs and is outside the scope of this page.

# Warm instances before traffic

A healthy production rollout should follow this lifecycle:

1. The container or process starts.
2. Hot Chocolate builds the schema and creates the request executor.
3. Optional warmup tasks compile and cache selected operations.
4. Readiness turns green.
5. The first user request avoids schema, executor, parse, validate, and compile cold paths for warmed operations.

Warmup shifts work from the first user request to startup. It does not remove the work, so keep your warmup list aligned with your startup budget and service-level objectives.

Use operation warmup for a small set of high-value requests, such as homepage queries, common persisted operations, and representative expensive query shapes. Do not use this page as a replacement for general performance tuning, DataLoader design, database connection management, or observability.

# Prerequisites

Before you configure operation warmup, you need:

- A Hot Chocolate v16 ASP.NET Core server configured with `AddGraphQLServer()` or an equivalent `AddGraphQL()` setup.
- A GraphQL endpoint mapped with `app.MapGraphQL()`.
- A short list of representative production operations.
- Operation names and document IDs or hashes for persisted operations.
- Access to application logs, diagnostic events, or OpenTelemetry traces.
- Readiness or startup probe control when you deploy behind Kubernetes, a load balancer, or an autoscaler.

# Rely on the v16 eager-start default first

Most production deployments should start with the v16 default:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>();

var app = builder.Build();

app.MapGraphQL();
app.Run();
```

Expected result:

- Hot Chocolate creates the schema and request executor during application startup.
- Schema configuration errors fail startup and appear in startup logs.
- No `.InitializeOnStartup()` call is needed in v16.

`LazyInitialization` defaults to `false`. The ASP.NET Core hosted warmup service asks Hot Chocolate to create each configured executor during startup. This removes the schema and executor cold start from the first GraphQL request.

This default does not precompile every client operation. The first request for a particular operation can still pay parsing, validation, compilation, and cache population costs unless you warm that operation.

# Warm common operations without executing resolvers

Use `AddWarmupTask` with a marked warmup request to populate execution caches without running field resolvers:

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

Expected result:

- Startup waits until the warmup task finishes.
- The marked request can populate the document cache and prepared operation cache.
- Resolver side effects do not run because the request returns a warmup result before operation execution.
- Later requests with the same document ID and operation name can hit the warmed caches.

`MarkAsWarmupRequest()` is the safety switch for operation warmup. It lets the request pipeline parse, validate, and compile the operation, then stops before resolver execution. Use queries, not mutations, for warmup examples and production warmup lists.

Match the future request shape as closely as the cache keys require:

- Include the same document ID or hash that clients send when you want document cache hits.
- Include the same operation name that clients send because the prepared operation cache key includes the operation name.
- Pass the provided cancellation token so startup cancellation and shutdown do not hang behind your warmup code.

Variables are not part of the prepared operation cache key, but variable coercion still happens per request. A warmed operation can still spend time coercing large or complex variable values.

# Warm persisted operations by document ID

Persisted operations and trusted documents benefit from targeted warmup when you already know the documents your clients will send:

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

Expected result:

- Startup can warm the known operation even when your persisted-operation pipeline rejects non-persisted client requests.
- The first client request using the same document ID or hash and operation name can use the warmed execution caches.
- Warmup does not publish documents to an external registry or storage system.

A warmup request can bypass the persisted-document-only check because it is internal startup work. Do not expose any client feature that lets a remote caller mark a request as a warmup request.

For publishing and storage details, see [trusted documents](/docs/hotchocolate/v16/operations/security-hardening/trusted-documents) and [automatic persisted operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations).

# Choose safe operations to warm

Warm a small, deliberate set of operations. The best candidates have high traffic, stable documents, explicit operation names, and known document IDs when persisted operations are used.

| Operation shape                | Warmup fit  | Guidance                                                                                           |
| ------------------------------ | ----------- | -------------------------------------------------------------------------------------------------- |
| Homepage query                 | High        | Warm it when it is common and read-only.                                                           |
| Generated persisted operation  | High        | Warm it when the document ID or hash is stable and known at deployment time.                       |
| Expensive representative query | Medium      | Warm one or two shapes that affect first-request SLOs.                                             |
| Admin-only query               | Conditional | Warm it only when validation does not require per-user context, or model the context deliberately. |
| Mutation                       | Low         | Avoid mutations. Use a read-only query that exercises similar schema paths.                        |
| Subscription                   | Low         | Do not use startup warmup for subscription connection behavior.                                    |

Marked warmup requests do not execute resolvers. They do not warm databases, DataLoaders, HTTP clients, authentication providers, or downstream caches. If those systems need their own startup strategy, configure that separately and measure it separately.

Revisit the warmup list after schema changes, client operation changes, cache-size changes, or traffic shifts. Over-warming slows rollout and scale-out.

# Register reusable warmup tasks

Move repeated warmup logic into an `IRequestExecutorWarmupTask` when you have more than one operation or need constructor injection:

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

Expected result:

- The task runs before the initial executor is ready.
- When the executor is rebuilt later, the old executor keeps serving requests until the replacement executor finishes warmup.
- Because `ApplyOnlyOnStartup` is `false`, the task also runs for later executor creations.

Use `ApplyOnlyOnStartup => true` for work that should run only for the initial executor. Delegate warmup tasks registered with `AddWarmupTask((executor, ct) => ...)` run for each newly created executor.

Available registration forms include delegate, task instance, generic task type, factory, `bool skipIf`, and `Func<IServiceProvider, bool> skipIf`. The `skipIf` service provider is the application service provider, which makes it useful for environment or configuration checks:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddWarmupTask<LargeWarmupTask>(
        services => services.GetRequiredService<IHostEnvironment>().IsDevelopment());
```

Expected result: `LargeWarmupTask` is not registered in development, but it can run in other environments.

Warmup tasks run sequentially for a single executor. Keep each task bounded, log useful progress, and avoid unbounded external calls.

# Access application services from warmup tasks

Warmup tasks are activated from Hot Chocolate schema services. Hot Chocolate services such as `IDocumentCache` and `IPreparedOperationCache` live there. Application services need to be made available explicitly:

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

Expected result:

- The task can resolve schema services and the application `WarmupOperationCatalog`.
- Startup does not fail with a missing-service activation error for `WarmupOperationCatalog`.

`AddApplicationService<T>()` resolves the application service once during schema initialization and registers it in schema services. In factory scenarios, you can also use `GetRootServiceProvider()` from the schema service provider when you intentionally need access to application services.

# Coordinate warmup with readiness and startup probes

Expose a readiness endpoint and give your platform enough time for startup warmup:

```csharp
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/health/ready");
app.MapGraphQL();
app.Run();
```

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

Expected result: New pods receive traffic only after the ASP.NET Core app is running and the startup warmup path has completed.

Use a startup probe when schema build or operation warmup can exceed default probe grace periods. Set thresholds from measured worst-case startup time plus headroom. If startup fails because schema creation or a warmup task throws, readiness should never turn green for that instance.

ASP.NET Core health checks do not automatically validate every future dynamic schema rebuild. If your application rebuilds schemas at runtime, add application-specific health logic only when you have a concrete readiness contract to enforce.

For endpoint hosting basics, see [ASP.NET Core hosting](/docs/hotchocolate/v16/operations/deployment/aspnetcore-hosting). For probe design, see [health checks](/docs/hotchocolate/v16/operations/observability/health-checks).

# Tune cold starts for your hosting model

Different hosting models value startup time and first-request latency differently.

| Hosting model                        | Recommended warmup strategy                                                                                      |
| ------------------------------------ | ---------------------------------------------------------------------------------------------------------------- |
| Kubernetes                           | Use eager initialization, targeted operation warmup, startup probes, and readiness probes.                       |
| Long-running containers or VMs       | Use the v16 default and add operation warmup for first-request SLOs.                                             |
| Autoscaling                          | Warm only operations that materially affect SLOs. Too much warmup slows scale-out.                               |
| Serverless or strict startup budgets | Keep warmup small. Compare eager startup with provisioned or minimum instances before using lazy initialization. |
| Local development                    | Use the default. Skip expensive tasks with `skipIf` when they slow feedback loops.                               |

Eager initialization increases startup work and reduces first GraphQL request latency. Lazy initialization can make the process appear to start sooner, but it moves schema and executor creation to the first GraphQL request. Treat lazy initialization as an exception for constrained platforms, not the normal production setting.

# Opt into lazy initialization only for special cases

Use lazy initialization only when a platform startup budget matters more than first-request latency:

```csharp
builder.Services
    .AddGraphQLServer()
    .ModifyOptions(options =>
    {
        options.LazyInitialization = true;
    });
```

Expected result:

- Process startup can complete with less GraphQL startup work.
- The first request or first executor access creates the schema and request executor.
- Registered warmup tasks can run when that executor is created, so the first GraphQL request can pay the warmup cost.

Do not use lazy initialization for normal production deployments behind load balancers. It can block initial requests and hide schema errors until runtime.

# Export the schema during startup

`ExportSchemaOnStartup()` is related startup work implemented as a warmup task:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .ExportSchemaOnStartup("./schema.graphql");
```

Expected result: Hot Chocolate writes the schema SDL to `./schema.graphql` during startup.

If you omit the path, Hot Chocolate writes `schema.graphqls` in the current directory. The task runs on startup and later schema changes unless you skip it. Use this for CI/CD or schema registry workflows, and account for file-system permissions and startup I/O.

# Measure cold-start and first-request latency

Measure before and after you add operation warmup:

1. Record application startup duration from process start to readiness success.
2. Record duration for each warmup task.
3. Send the first real GraphQL request after readiness and record latency.
4. Send the same request again and compare latency.
5. Check diagnostic events for cache additions during warmup and cache retrievals during real requests.

A small diagnostic listener can tag warmup requests and time GraphQL requests:

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

            Console.WriteLine(
                $"GraphQL {kind} request completed in {stopwatch.ElapsedMilliseconds} ms");
        }
    }
}
```

Expected output:

```text
GraphQL warmup request completed in 42 ms
GraphQL user request completed in 8 ms
```

Use diagnostic events such as `ParseDocument`, `ValidateDocument`, `CompileOperation`, `AddedDocumentToCache`, `RetrievedDocumentFromCache`, `AddedOperationToCache`, `RetrievedOperationFromCache`, and `ExecutorCreated` to confirm where time moves. Cache-add events should appear during warmup for matching operations. Later real requests should show retrieval events.

Diagnostic handlers run synchronously as part of request processing. Do not export telemetry or perform expensive work inline. For OpenTelemetry setup, see [OpenTelemetry](/docs/hotchocolate/v16/operations/observability/opentelemetry) and [diagnostic events](/docs/hotchocolate/v16/operations/observability/diagnostics-events).

A useful comparison table looks like this:

| Measurement             | Before operation warmup                       | After operation warmup                |
| ----------------------- | --------------------------------------------- | ------------------------------------- |
| Startup duration        | Lower                                         | Higher by warmup task duration        |
| First matching request  | Pays parse, validate, compile, and cache fill | Can hit document and operation caches |
| Second matching request | Usually warm                                  | Usually warm                          |

# Troubleshoot warmup problems

| Symptom                                  | Likely cause                                                                                                                                 | Fix                                                                                                                                                      |
| ---------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Startup fails with a schema error        | Eager initialization surfaced invalid schema or configuration.                                                                               | Fix the schema before rollout. This is the v16 default working as intended.                                                                              |
| Startup exceeds probe timeout            | Schema build or operation warmup takes longer than the probe budget.                                                                         | Reduce the warmup list, increase startup probe thresholds, split expensive work, and inspect schema build time.                                          |
| Warmup request executes side effects     | The request was not marked as a warmup request, or external warmup code ran outside the GraphQL resolver pipeline.                           | Add `.MarkAsWarmupRequest()`, prefer read-only queries, and keep side-effecting work out of operation warmup.                                            |
| First request is still slow              | Document ID mismatch, operation name mismatch, operation not warmed, cache eviction, variable coercion cost, or downstream systems are cold. | Warm with matching document ID and operation name, increase cache sizes when needed, and warm downstream systems separately.                             |
| Persisted operation request misses cache | The warmup document ID or hash does not match the client path, or storage/hash configuration differs.                                        | Warm with the same document ID or hash and operation name that clients send. Verify the persisted operation pipeline.                                    |
| Warmup task cannot resolve a service     | The task is activated from schema services, not the application service provider.                                                            | Add `.AddApplicationService<T>()`, inject schema services, or use a factory with `GetRootServiceProvider()` intentionally.                               |
| Dynamic schema rebuild causes latency    | Rebuild warmup tasks were skipped or the replacement executor had no matching cache entries.                                                 | Use `ApplyOnlyOnStartup => false` for tasks needed after rebuild. The old executor serves requests until the warmed replacement is ready.                |
| Startup hangs or cancels                 | Warmup code waits on unbounded external work or ignores cancellation.                                                                        | Pass cancellation tokens, add timeouts around external calls, and log around each task.                                                                  |
| Cache fills or churns                    | Too many operations are warmed for the configured cache sizes.                                                                               | Warm fewer operations or increase `PreparedOperationCacheSize` and `OperationDocumentCacheSize`. The default for each is `256`, and the minimum is `16`. |

Configure cache sizes through schema options:

```csharp
builder.Services
    .AddGraphQLServer()
    .ModifyOptions(options =>
    {
        options.PreparedOperationCacheSize = 1024;
        options.OperationDocumentCacheSize = 1024;
    });
```

For more option details, see the [options reference](/docs/hotchocolate/v16/api-reference/options).

# Migrate v15 warmup code to v16

Remove `InitializeOnStartup()` when you migrate to v16. Eager schema and executor initialization is now the default.

```diff
builder.Services.AddGraphQLServer()
-    .InitializeOnStartup(warmup: (executor, ct) => { /* ... */ });
+    .AddWarmupTask((executor, ct) => { /* ... */ });
```

If your v15 warmup callback should also run after later executor rebuilds, use a delegate warmup task or a custom task with `ApplyOnlyOnStartup => false`. If it should run only for the initial executor, implement `IRequestExecutorWarmupTask` and return `true` from `ApplyOnlyOnStartup`.

Application services needed by warmup tasks may require `.AddApplicationService<T>()` because v16 separates schema services from application services.

For the complete migration context, see [migrate from v15 to v16](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16#eager-initialization-by-default).

# Next steps

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
