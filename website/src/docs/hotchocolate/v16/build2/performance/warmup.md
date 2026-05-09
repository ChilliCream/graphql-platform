---
title: Warmup
---

Warmup moves predictable GraphQL startup and first-request work to a controlled point in the application lifecycle. In Hot Chocolate v16, schema and request executor creation are eager by default. Add warmup tasks when you also want representative operations to populate the document and prepared-operation caches before production traffic reaches the endpoint.

Use warmup for costs that are tied to Hot Chocolate setup and operation preparation:

- Schema services, schema creation, and request executor creation.
- Parsing and validating selected operation documents.
- Preparing selected operations for repeated execution.
- Detecting schema or warmup operation failures before the app is considered ready.

Warmup does not replace load testing, persisted operation publishing, schema checks in CI, or readiness probes.

# Start with the v16 default

A typical v16 server uses `AddGraphQL()` and maps the GraphQL endpoint:

```csharp
builder
    .AddGraphQL()
    .AddTypes();

app.MapGraphQL();
```

With the default options, Hot Chocolate creates the request executor during application startup. The request executor is the per-schema runtime object that executes GraphQL requests. Creating it builds schema services, the schema, and the execution pipeline for that schema.

This default is enough when the main cold-start cost is schema and executor creation. Add operation warmup only when the first live requests still spend noticeable time in parse, validation, or operation preparation.

# Warm representative operations

Register warmup tasks with `AddWarmupTask(...)`. A warmup task runs when a new request executor is created.

```csharp
builder
    .AddGraphQL()
    .AddTypes()
    .AddWarmupTask(async (executor, cancellationToken) =>
    {
        var request = OperationRequestBuilder.New()
            .SetDocument("""
                query GetProducts {
                    products(first: 10) {
                        nodes {
                            id
                            name
                        }
                    }
                }
                """)
            .SetOperationName("GetProducts")
            .MarkAsWarmupRequest()
            .Build();

        await executor.ExecuteAsync(request, cancellationToken);
    });
```

`MarkAsWarmupRequest()` tells Hot Chocolate to prepare the request for warmup and then stop before operation execution. The result is a `WarmupExecutionResult`, and resolvers are not executed. This lets the document cache and prepared-operation cache receive entries without startup database calls or resolver side effects.

Match the request shape your clients send:

- Use the same document text when clients send document text.
- Use the same operation name when the operation is named.
- Use the same document ID when the runtime request uses a document ID.
- Keep the warmup list small because warmup increases startup time.

If the real request includes `operationName`, include `.SetOperationName(...)`. Operation name participates in prepared-operation cache lookup.

# Warm persisted or trusted operations

Persisted operation storage and warmup caches solve different problems. Persisted operation storage stores known documents by ID or hash. Warmup caches are in-memory and belong to one request executor.

When a client later executes by document ID, warm that same ID and include the document text so Hot Chocolate can parse and prepare it:

```csharp
builder
    .AddGraphQL()
    .AddTypes()
    .AddWarmupTask(async (executor, cancellationToken) =>
    {
        var request = OperationRequestBuilder.New()
            .SetDocumentId("products-by-brand")
            .SetDocument("""
                query ProductsByBrand($brandId: ID!) {
                    products(where: { brandId: { eq: $brandId } }) {
                        nodes {
                            id
                            name
                        }
                    }
                }
                """)
            .SetOperationName("ProductsByBrand")
            .MarkAsWarmupRequest()
            .Build();

        await executor.ExecuteAsync(request, cancellationToken);
    });
```

Marked warmup requests can bypass persisted-operation enforcement so the warmup document can populate the caches. Do not treat a successful warmup request as proof that a normal production request is allowed. Test persisted and trusted-document policies with normal HTTP requests.

# Build a reusable warmup task

Use `IRequestExecutorWarmupTask` when the warmup logic is shared, needs constructor dependencies, or needs explicit rebuild behavior.

```csharp
builder.Services.AddSingleton<ProductWarmupOperations>();

builder
    .AddGraphQL()
    .AddTypes()
    .AddApplicationService<ProductWarmupOperations>()
    .AddWarmupTask<ProductWarmupTask>();

public sealed class ProductWarmupTask(
    ProductWarmupOperations operations)
    : IRequestExecutorWarmupTask
{
    public bool ApplyOnlyOnStartup => true;

    public async Task WarmupAsync(
        IRequestExecutor executor,
        CancellationToken cancellationToken)
    {
        var request = OperationRequestBuilder.New()
            .SetDocument(operations.GetProductsDocument)
            .SetOperationName("GetProducts")
            .MarkAsWarmupRequest()
            .Build();

        await executor.ExecuteAsync(request, cancellationToken);
    }
}

public sealed class ProductWarmupOperations
{
    public string GetProductsDocument => """
        query GetProducts {
            products(first: 10) {
                nodes {
                    id
                    name
                }
            }
        }
        """;
}
```

Warmup task types are activated from schema services. If a task needs an application service, make that service available to schema services with `.AddApplicationService<T>()`.

`ApplyOnlyOnStartup` controls runtime rebuilds:

| Value   | Behavior                                                                                                             |
| ------- | -------------------------------------------------------------------------------------------------------------------- |
| `false` | Run on initial executor creation and on later executor rebuilds. Delegate warmup tasks use this behavior by default. |
| `true`  | Run only during initial executor creation. Use this for startup-only work.                                           |

Warmup tasks for one executor run in registration order. Honor the `CancellationToken` so host shutdown and deployment timeouts can stop startup cleanly.

# Register warmup conditionally

Every environment does not need the same startup work. The `skipIf` overload receives the application service provider. If it returns `true`, the warmup task is not registered.

```csharp
builder
    .AddGraphQL()
    .AddTypes()
    .AddWarmupTask<ProductWarmupTask>(
        skipIf: services =>
            services.GetRequiredService<IHostEnvironment>().IsDevelopment());
```

Common choices:

- Skip expensive operation warmup in development.
- Run only critical warmup tasks in constrained startup budgets.
- Disable optional warmup through configuration for short-lived review environments.

Keep production behavior explicit. If warmup changes readiness timing, document that choice with the deployment configuration.

# Align warmup with readiness and liveness

Startup warmup is blocking for non-lazy schemas. Hot Chocolate registers hosted startup initialization, asks the request executor provider for each configured non-lazy schema, and waits for those executors to be created. Registered startup warmup tasks run as part of executor creation.

Use readiness and liveness for different questions:

| Probe     | Question                                  | Warmup guidance                                                                 |
| --------- | ----------------------------------------- | ------------------------------------------------------------------------------- |
| Readiness | Can this instance receive traffic?        | Return ready only after application startup and required warmup have completed. |
| Liveness  | Should the platform restart this process? | Do not fail liveness because warmup is still running during startup.            |

For Kubernetes, container apps, or load balancers, route traffic only after readiness passes. Avoid readiness checks that only prove the process is alive, because they can send traffic before schema or operation warmup has completed.

If startup time becomes too long, reduce the warmed operation set before turning on lazy initialization. Lazy initialization moves schema and executor creation to the first GraphQL access.

```csharp
builder
    .AddGraphQL()
    .AddTypes()
    .ModifyOptions(options =>
    {
        options.LazyInitialization = true;
    });
```

Use `LazyInitialization = true` only when faster process startup is more important than first-request latency and delayed schema errors. It is a deliberate production tradeoff, not the recommended default.

# Handle multiple schemas and rebuilds

Named schemas are initialized independently. Configure warmup on each schema that needs it.

```csharp
builder
    .AddGraphQL("products")
    .AddTypes()
    .AddWarmupTask<ProductWarmupTask>();

builder
    .AddGraphQL("inventory")
    .AddTypes()
    .AddWarmupTask<InventoryWarmupTask>();
```

The startup service creates all non-lazy schema executors and awaits them. Caches, schema options, and warmup tasks are per schema and per request executor.

When an executor is rebuilt at runtime, for example after a schema eviction, Hot Chocolate keeps serving requests with the old executor while the replacement executor is created and warmed. After applicable warmup tasks finish, the replacement executor can be published. Tasks with `ApplyOnlyOnStartup = true` are skipped for non-initial rebuilds.

# Tune cache sizes after measuring

Marked warmup requests can populate these per-schema caches:

| Cache                    | Option                       | Default | Minimum |
| ------------------------ | ---------------------------- | ------- | ------- |
| Operation document cache | `OperationDocumentCacheSize` | `256`   | `16`    |
| Prepared-operation cache | `PreparedOperationCacheSize` | `256`   | `16`    |

Increase cache sizes when warmed operations are evicted before traffic arrives, or when the hot working set is larger than the default cache capacity.

```csharp
builder
    .AddGraphQL()
    .AddTypes()
    .ModifyOptions(options =>
    {
        options.OperationDocumentCacheSize = 1024;
        options.PreparedOperationCacheSize = 1024;
    });
```

Do not increase cache sizes without measuring memory use and cache behavior. Warming too many operations can hide cache churn during startup and then evict useful entries under real traffic.

# Export the schema during startup

`ExportSchemaOnStartup(...)` uses the warmup infrastructure to write SDL during executor creation.

```csharp
builder
    .AddGraphQL()
    .AddTypes()
    .ExportSchemaOnStartup("./schema.graphql");
```

Use startup export when the running app should produce the file. For CI/CD or registry workflows, prefer command-line schema export so deployments do not depend on runtime file writes.

# Measure warmup impact

Compare these measurements before and after adding warmup:

- Time from process start to readiness.
- First GraphQL request latency after deployment.
- Second and later latency for the same operation.
- Executor rebuild duration when dynamic schemas or schema eviction are used.
- Cache hit behavior for warmed documents and operations, when instrumentation is available.

Keep comparisons consistent. Use the same schema name, document text, operation name, document ID, persisted-operation path, and server instance. Marked warmup requests are not resolver benchmarks, so do not attribute database or remote-service improvements to them.

Use Hot Chocolate instrumentation and application logs to place timestamps around startup, executor creation, warmup task start and finish, first request, and later requests. Diagnostic handlers run during request processing, so keep measurement handlers lightweight.

# Troubleshooting

| Symptom                                          | Likely cause                                                                           | What to check                                                                                                                         |
| ------------------------------------------------ | -------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------- |
| Startup takes longer after adding warmup         | Warmup is blocking startup                                                             | Reduce the operation list, use marked warmup requests, or skip optional tasks by environment.                                         |
| First request is still slow                      | Resolver, database, remote service, or different request shape dominates               | Compare first and second requests, then verify document text, operation name, document ID, schema name, and persisted-operation path. |
| Prepared-operation cache is missed               | Operation name or document identity differs                                            | Include the same operation name and document ID or text that clients send.                                                            |
| Warmed entries are evicted                       | Warmed set and live traffic exceed cache capacity                                      | Warm fewer operations or tune `OperationDocumentCacheSize` and `PreparedOperationCacheSize`.                                          |
| Resolvers run during startup                     | The request was not marked as a warmup request, or real execution was intentional      | Add `.MarkAsWarmupRequest()` for cache-only warmup. Use idempotent operations for real execution.                                     |
| Warmup task cannot resolve a service             | The task is activated from schema services                                             | Register application dependencies and expose them with `.AddApplicationService<T>()`.                                                 |
| Startup fails during warmup                      | Schema error, invalid operation, dependency failure, cancellation, or timeout          | Log task and operation names, honor cancellation, and avoid unbounded external calls.                                                 |
| First GraphQL request creates the executor       | `LazyInitialization = true` or startup initialization was not part of the server setup | Disable lazy initialization for production readiness unless the tradeoff is intentional.                                              |
| Warmup passed but trusted-document requests fail | Marked warmup requests can bypass persisted-operation enforcement                      | Test trusted-document policy with normal HTTP requests.                                                                               |

# API reference

| API or option                                   | Use                                                                                  |
| ----------------------------------------------- | ------------------------------------------------------------------------------------ |
| `AddWarmupTask(...)`                            | Register a delegate, instance, generic type, or factory warmup task.                 |
| `IRequestExecutorWarmupTask`                    | Implement reusable warmup logic with `WarmupAsync` and `ApplyOnlyOnStartup`.         |
| `ApplyOnlyOnStartup`                            | Skip the task on executor rebuilds when `true`.                                      |
| `OperationRequestBuilder.MarkAsWarmupRequest()` | Mark a request for cache warmup without resolver execution.                          |
| `RequestContext.IsWarmupRequest()`              | Detect marked warmup requests in code that receives a `RequestContext`.              |
| `SchemaOptions.LazyInitialization`              | Defer executor creation until first use when set to `true`.                          |
| `OperationDocumentCacheSize`                    | Configure the per-schema document cache size. Default `256`, minimum `16`.           |
| `PreparedOperationCacheSize`                    | Configure the per-schema prepared-operation cache size. Default `256`, minimum `16`. |
| `ExportSchemaOnStartup(...)`                    | Export SDL through the startup warmup infrastructure.                                |

# When lazy initialization is acceptable

Lazy initialization can be acceptable for development tools, short-lived jobs, or applications where GraphQL is rarely used and first-request latency is not important. It can also help when a platform imposes a strict process-start budget and readiness is handled by a later explicit check.

For production APIs that receive traffic soon after deployment, keep the v16 eager default and use bounded warmup tasks for the operations that matter.

# Next steps

| Goal                                   | Read next                                                                                                  |
| -------------------------------------- | ---------------------------------------------------------------------------------------------------------- |
| Choose the right performance technique | [Performance](/docs/hotchocolate/v16/build2/performance)                                                   |
| Configure APQ request flows            | [Automatic persisted operations](/docs/hotchocolate/v16/build2/performance/automatic-persisted-operations) |
| Configure trusted documents            | [Trusted documents](/docs/hotchocolate/v16/build2/security/trusted-documents)                              |
| Understand execution phases            | [Execution engine](/docs/hotchocolate/v16/build2/execution-engine)                                         |
| Measure request behavior               | [Instrumentation](/docs/hotchocolate/v16/server/instrumentation)                                           |
| Review v15 migration changes           | [Migrate from v15 to v16](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16)                          |
