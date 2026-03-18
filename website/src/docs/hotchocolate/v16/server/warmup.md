---
title: Warmup
---

By default, Hot Chocolate constructs the schema eagerly during server startup. This means the schema and request executor are fully initialized before Kestrel begins accepting requests, ensuring initial requests perform optimally without any cold-start penalty.

This eager initialization also tightens the development feedback loop because schema misconfigurations cause errors at startup rather than when the first request arrives.

In environments with load balancers, this default behavior works well with health checks and readiness probes, since your server does not report as ready until the schema is fully constructed.

# Warming Up the Executor

While eager initialization ensures your schema is ready at startup, you might want to go further and pre-populate in-memory caches like the document cache and operation cache before serving any requests.

Register warmup tasks using the `AddWarmupTask()` method to execute requests against the newly created schema during initialization:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddWarmupTask(async (executor, cancellationToken) =>
    {
        await executor.ExecuteAsync("{ __typename }", cancellationToken);
    });
```

The warmup process is blocking. The server does not start answering requests until both the schema creation and all warmup tasks have finished.

By default, warmup tasks run both at server startup and whenever the schema is rebuilt at runtime (for example, when using [dynamic schemas](/docs/hotchocolate/v16/building-a-schema/dynamic-schemas)). When the request executor changes, warmup tasks execute in the background while requests continue to be handled by the old request executor. Once warmup completes, requests are served by the new and already warmed-up request executor.

Since the execution of an operation could have side-effects, you might want to warm up the executor but skip the actual execution of the request. Mark an operation as a warmup request for this purpose:

```csharp
var request = OperationRequestBuilder.New()
    .SetDocument("{ __typename }")
    .MarkAsWarmupRequest()
    .Build();

await executor.ExecuteAsync(request, cancellationToken);
```

Requests marked as warmup requests skip security measures like persisted operations and finish without actually executing the specified operation.

Keep in mind that the operation name is part of the operation cache. If your client sends an operation name, include that operation name in the warmup request as well, or the actual request will miss the cache:

```csharp
var request = OperationRequestBuilder.New()
    .SetDocument("query testQuery { __typename }")
    .SetOperationName("testQuery")
    .MarkAsWarmupRequest()
    .Build();

await executor.ExecuteAsync(request, cancellationToken);
```

## Custom Warmup Tasks

For more control over warmup behavior, implement the `IRequestExecutorWarmupTask` interface:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddWarmupTask<MyWarmupTask>();

public class MyWarmupTask : IRequestExecutorWarmupTask
{
    public bool ApplyOnlyOnStartup => false;

    public async Task WarmupAsync(
        IRequestExecutor executor,
        CancellationToken cancellationToken)
    {
        // Your warmup logic here
        await executor.ExecuteAsync("{ __typename }", cancellationToken);
    }
}
```

The `ApplyOnlyOnStartup` property controls whether the warmup task runs only at server startup (`true`) or also when the request executor is rebuilt at runtime (`false`, the default).

You can register your custom warmup task using either the delegate form or the generic form:

```csharp
// Delegate form
builder.Services
    .AddGraphQLServer()
    .AddWarmupTask(async (executor, ct) =>
    {
        await executor.ExecuteAsync("{ __typename }", ct);
    });

// Generic form with IRequestExecutorWarmupTask
builder.Services
    .AddGraphQLServer()
    .AddWarmupTask<MyWarmupTask>();
```

## Exporting the Schema on Startup

If you need to export the schema as part of your startup process (for example, for CI/CD or schema registry integration), use the `ExportSchemaOnStartup()` method:

```csharp
builder.Services
    .AddGraphQLServer()
    .ExportSchemaOnStartup("./schema.graphql");
```

This writes the schema SDL to the specified file path during server initialization.

<!--
### Accessing services

You can inject services into your custom warmup task through constructor injection. This includes both Hot Chocolate's built-in schema services like `IDocumentCache` or `IPreparedOperationCache`, as well as any application services you've registered:

```csharp
public class MyWarmupTask(IDocumentCache documentCache, MyService myService)
    : IRequestExecutorWarmupTask
{
    // ...
}
```

## Skipping reporting

If you've set up [instrumentation](/docs/hotchocolate/v16/server/instrumentation), you might want to skip reporting certain events in the case of a warmup request.

You can use the `RequestContext.IsWarmupRequest()` method to determine whether a request is a warmup request or not:

```csharp
public class MyExecutionEventListener : ExecutionDiagnosticEventListener
{
    public override void RequestError(RequestContext context,
        Exception exception)
    {
        if (context.IsWarmupRequest())
        {
            return;
        }

        // Reporting
    }
}
```
-->

# Opting into Lazy Initialization

If you need to defer schema construction until the first request (though this is rarely recommended), you can opt into lazy initialization:

```csharp
builder.Services
    .AddGraphQLServer()
    .ModifyOptions(options => options.LazyInitialization = true)
```

With lazy initialization enabled, the schema is constructed when it is first needed, either when a request is executed or when the schema is otherwise accessed. Depending on the size of your schema and the configured warmup tasks, this causes initial requests to run longer than they would with eager initialization.

# Troubleshooting

## Warmup task runs but cache misses still occur

The operation name is part of the cache key. If your client sends an operation name that differs from the one in your warmup request (or omits it when the warmup includes one), the cache lookup will miss. Ensure the warmup request matches what the client sends.

## Server takes too long to start

If your warmup tasks execute heavy operations, consider marking them as warmup requests with `MarkAsWarmupRequest()`. This skips the actual execution of the operation while still populating the document and operation caches.

## Schema errors surface at startup

This is the expected behavior of eager initialization. Fix the schema errors before proceeding. If you need to defer errors to runtime (not recommended), set `options.LazyInitialization = true`.

# Next Steps

- [Instrumentation](/docs/hotchocolate/v16/server/instrumentation) for monitoring request execution and tracing.
- [Dynamic Schemas](/docs/hotchocolate/v16/building-a-schema/dynamic-schemas) for schemas that change at runtime.
- [Migrate from v15 to v16](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16#eager-initialization-by-default) for migration details on the `InitializeOnStartup` removal.
