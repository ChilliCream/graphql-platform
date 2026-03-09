---
title: Warmup
---

By default the creation of Hot Chocolate's schema is lazy. If a request is about to be executed against the schema or the schema is otherwise needed, it will be constructed on the fly.

Depending on the size of your schema this might be undesired, since it will cause initial requests to run longer than they would, if the schema was already constructed.

In an environment with a load balancer, you might also want to utilize something like a Readiness Probe to determine when your server is ready (meaning fully initialized) to handle requests.

# Initializing the schema on startup

If you want the schema creation process to happen at server startup, rather than lazily, you can chain in a call to `InitializeOnStartup()` on the `IRequestExecutorBuilder`.

```csharp
builder.Services
    .AddGraphQLServer()
    .InitializeOnStartup()
```

This will cause a hosted service to be executed as part of the server startup process, taking care of the schema creation. This process is blocking, meaning Kestrel won't answer requests until the construction of the schema is done. If you're using standard ASP.NET Core health checks, this will already suffice to implement a simple Readiness Probe.

This also has the added benefit that schema misconfigurations will cause errors at startup, tightening the feedback loop while developing.

# Warming up the executor

Creating the schema at startup is already a big win for the performance of initial requests. Though, you might want to go one step further and already initialize in-memory caches like the document and operation cache, before serving any requests.

For this the `InitializeOnStartup()` method contains an argument called `warmup` that allows you to pass a callback where you can execute requests against the newly created schema.

```csharp
builder.Services
    .AddGraphQLServer()
    .InitializeOnStartup(
        warmup: async (executor, cancellationToken) => {
            await executor.ExecuteAsync("{ __typename }");
        });
```

The warmup process is also blocking, meaning the server won't start answering requests until both the schema creation and the warmup process is finished.

Since the execution of an operation could have side-effects, you might want to only warmup the executor, but skip the actual execution of the request. For this you can mark an operation as a warmup request.

```csharp
var request = OperationRequestBuilder.New()
  .SetDocument("{ __typename }")
  .MarkAsWarmupRequest()
  .Build();

await executor.ExecuteAsync(request);
```

Requests marked as warmup requests will be able to skip security measures like persisted operations and will finish without actually executing the specified operation.

Keep in mind that the operation name is part of the operation cache. If your client is sending an operation name, you also want to include that operation name in the warmup request, or the actual request will miss the cache.

```csharp
var request = OperationRequestBuilder.New()
    .SetDocument("query testQuery { __typename }")
    .SetOperationName("testQuery")
    .MarkAsWarmupRequest()
    .Build();
```

## Skipping reporting

If you've implemented a custom diagnostic event listener as described [here](/docs/hotchocolate/v15/server/instrumentation#execution-events) you might want to skip reporting certain events in the case of a warmup request.

You can use the `IRequestContext.IsWarmupRequest()` method to determine whether a request is a warmup request or not.

```csharp
public class MyExecutionEventListener : ExecutionDiagnosticEventListener
{
    public override void RequestError(IRequestContext context,
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

## Keeping the executor warm

By default the warmup only takes place at server startup. If you're using [dynamic schemas](/docs/hotchocolate/v15/defining-a-schema/dynamic-schemas) for instance, your schema might change throughout the lifetime of the server.
In this case the warmup will not apply to subsequent schema changes, unless you set the `keepWarm` argument to `true`.

```csharp
builder.Services
    .AddGraphQLServer()
    .InitializeOnStartup(
        keepWarm: true,
        warmup: /* ... */);
```

If set to `true`, the schema and its warmup task will be executed in the background, while requests are still handled by the old schema. Once the warmup is finished requests will be served by the new and already warmed up schema.
