---
title: Warmup
---

By default, Hot Chocolate constructs the schema eagerly during server startup. This means the schema and request executor are fully initialized before Kestrel begins accepting requests, ensuring initial requests perform optimally without any cold-start penalty.

This eager initialization also tightens the development feedback loop as schema misconfigurations will cause errors at startup rather than when the first request arrives.

In environments with load balancers, this default behavior works seamlessly with health checks and Readiness Probes, since your server won't report as ready until the schema is fully constructed.

# Warming up the executor

While eager initialization ensures your schema is ready at startup, you might want to go further and pre-populate in-memory caches like the document and operation cache before serving any requests.

You can add warmup tasks using the `AddWarmupTask()` method, which allows you to execute requests against the newly created schema during initialization:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddWarmupTask(async (executor, cancellationToken) =>
    {
        await executor.ExecuteAsync("{ __typename }", cancellationToken);
    });
```

The warmup process is blocking, meaning the server won't start answering requests until both the schema creation and all warmup tasks have finished.

By default, warmup tasks run both at server startup and whenever the schema is rebuilt at runtime (for example, when using [dynamic schemas](/docs/hotchocolate/v16/defining-a-schema/dynamic-schemas)). When the request executor changes, warmup tasks execute in the background while requests continue to be handled by the old request executor. Once warmup is complete, requests will be served by the new and already warmed-up request executor.

Since the execution of an operation could have side-effects, you might want to only warm up the executor but skip the actual execution of the request. For this you can mark an operation as a warmup request:

```csharp
var request = OperationRequestBuilder.New()
    .SetDocument("{ __typename }")
    .MarkAsWarmupRequest()
    .Build();

await executor.ExecuteAsync(request, cancellationToken);
```

Requests marked as warmup requests will be able to skip security measures like persisted operations and will finish without actually executing the specified operation.

Keep in mind that the operation name is part of the operation cache. If your client is sending an operation name, you also want to include that operation name in the warmup request, or the actual request will miss the cache:

```csharp
var request = OperationRequestBuilder.New()
    .SetDocument("query testQuery { __typename }")
    .SetOperationName("testQuery")
    .MarkAsWarmupRequest()
    .Build();

await executor.ExecuteAsync(request, cancellationToken);
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

## Custom warmup tasks

For more control over warmup behavior, you can implement the `IRequestExecutorWarmupTask` interface:

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

The `ApplyOnlyOnStartup` property controls whether the warmup task should run only at server startup (`true`) or also when the request executor is rebuilt at runtime (`false`, the default).
Register your custom warmup task using any of these approaches:

# Opting into lazy initialization

If you need to defer schema construction until the first request (though this is rarely recommended), you can opt into lazy initialization:

```csharp
builder.Services
    .AddGraphQLServer()
    .ModifyOptions(options => options.LazyInitialization = true)
```

With lazy initialization enabled, the schema will only be constructed when it's first needed. Either when a request is executed or when the schema is otherwise accessed. Depending on the size of your schema and the configured warmup tasks, this will cause initial requests to run longer than they would with eager initialization.
