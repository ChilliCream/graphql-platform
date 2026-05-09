---
title: Diagnostic events and listeners
---

# Observing Hot Chocolate with Diagnostic Event Listeners

Hot Chocolate diagnostic event listeners allow you to monitor the GraphQL server from within the request pipeline. Use them when you need a Hot Chocolate-specific hook for production troubleshooting, custom metrics, audit-safe logs, or to verify that a request passed through a particular execution stage.

## Example: Logging Request Duration

```csharp
using System.Diagnostics;
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using Microsoft.Extensions.Logging;

public sealed class RequestTimingListener(
    ILogger<RequestTimingListener> logger)
    : ExecutionDiagnosticEventListener
{
    public override IDisposable ExecuteRequest(RequestContext context)
    {
        return new RequestScope(logger, Stopwatch.GetTimestamp());
    }

    private sealed class RequestScope(
        ILogger<RequestTimingListener> logger,
        long started)
        : IDisposable
    {
        public void Dispose()
        {
            var elapsed = Stopwatch.GetElapsedTime(started);
            logger.LogInformation(
                "GraphQL request completed in {ElapsedMilliseconds} ms",
                elapsed.TotalMilliseconds);
        }
    }
}
```

Register the listener on the GraphQL builder for your endpoint:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddDiagnosticEventListener<RequestTimingListener>();
```

Expected log output:

```text
info: RequestTimingListener[0]
      GraphQL request completed in 18.4 ms
```

Diagnostic listeners observe Hot Chocolate events synchronously. They do not replace OpenTelemetry, ASP.NET Core logging, interceptors, or error filters. Use them when you need direct access to Hot Chocolate execution, transport, or GreenDonut DataLoader events that are not exposed by other extension points.

---

# Prerequisites

You need a Hot Chocolate v16 ASP.NET Core server. For example:

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
    public string GetStatus() => "ok";
}
```

Before adding a listener, verify the baseline:

```graphql
{
  __typename
}
```

Expected result:

```json
{
  "data": {
    "__typename": "Query"
  }
}
```

Listener examples use these namespaces:

| Listener type                    | Namespaces                                                             |
| -------------------------------- | ---------------------------------------------------------------------- |
| Execution events                 | `HotChocolate.Execution`, `HotChocolate.Execution.Instrumentation`     |
| HTTP and WebSocket server events | `HotChocolate.AspNetCore.Instrumentation`, `Microsoft.AspNetCore.Http` |
| DataLoader events                | `GreenDonut`                                                           |
| Logging examples                 | `Microsoft.Extensions.Logging`                                         |
| Metrics examples                 | `System.Diagnostics.Metrics`                                           |

This page covers Hot Chocolate server diagnostics. For Fusion gateway diagnostics, see the Fusion documentation.

---

# Selecting the Right Extension Point

Decide what you want to achieve, then choose the narrowest extension point that fits your goal.

| Goal                                                                                              | Use                                                            | Why                                                                                                                                                                         |
| ------------------------------------------------------------------------------------------------- | -------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Export standard spans to Jaeger, OTLP, Azure Monitor, or another backend                          | `AddInstrumentation()` and `.AddHotChocolateInstrumentation()` | Hot Chocolate creates `Activity` spans that OpenTelemetry exporters understand. See [OpenTelemetry tracing](/docs/hotchocolate/v16/operations/observability/opentelemetry). |
| Add or remove tags on built-in Hot Chocolate spans                                                | `ActivityEnricher`                                             | Change exported activity metadata without writing a separate listener.                                                                                                      |
| Record a custom metric or audit-safe log from a Hot Chocolate lifecycle event                     | Diagnostic event listener                                      | Get an in-process hook with access to Hot Chocolate context objects.                                                                                                        |
| Change request data, global state, authentication-derived state, or WebSocket connection behavior | Request interceptors or ASP.NET Core middleware                | These APIs change behavior. See [interceptors](/docs/hotchocolate/v16/server/interceptors).                                                                                 |
| Shape GraphQL errors returned to clients                                                          | Error filters                                                  | Listeners observe errors. Error filters rewrite error responses. See [error handling](/docs/hotchocolate/v16/guides/error-handling).                                        |
| Write resolver business logs                                                                      | `ILogger` in resolver or application code                      | Domain logs belong where the business action happens.                                                                                                                       |

**Note:** Do not mutate requests, results, context state, or errors from a diagnostic listener. If you need to change behavior, use an interceptor, middleware, resolver code, or an error filter.

---

# Registering Diagnostic Listeners

Register listeners on the same `IRequestExecutorBuilder` that builds the schema used by `app.MapGraphQL()`:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddDiagnosticEventListener<MyExecutionListener>();
```

You can register multiple listeners. Hot Chocolate invokes all registered listeners, but do not depend on their order for correctness.

## Registering a Test Instance or Factory-Created Listener

Use the factory overload when a test needs to inspect the exact listener instance:

```csharp
var listener = new CountingExecutionListener();

var services = new ServiceCollection()
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddDiagnosticEventListener(_ => listener)
    .Services
    .BuildServiceProvider();
```

The factory receives the schema service provider for execution listeners. If you need the application service provider, use `GetRootServiceProvider()` from the schema service provider.

## Using Constructor Injection

Execution listeners are created from schema services in v16. If a listener constructor needs an application service, register the service in the application container and expose it to schema services with `AddApplicationService<T>()`:

```csharp
builder.Services.AddSingleton<RequestTimingSink>();

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddApplicationService<RequestTimingSink>()
    .AddDiagnosticEventListener<RequestTimingListener>();

public sealed class RequestTimingListener(RequestTimingSink sink)
    : ExecutionDiagnosticEventListener
{
    public override IDisposable ExecuteRequest(RequestContext context)
    {
        sink.IncrementStartedRequests();
        return EmptyScope;
    }
}
```

- DataLoader listeners are registered in the application service collection.
- Server listeners, such as `ServerDiagnosticEventListener`, are also registered through `AddDiagnosticEventListener<T>()`.

Listener instances are long-lived. Keep mutable per-request state out of listener fields. Store per-request data in the scope object you return, local variables, thread-safe services, or metric instruments.

---

# Measuring Request Duration with Execution Scopes

Scope-returning methods start an operation and return an `IDisposable`. Hot Chocolate disposes that scope when the operation completes or fails.

```csharp
using System.Diagnostics;
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using Microsoft.Extensions.Logging;

public sealed class RequestTimingListener(
    ILogger<RequestTimingListener> logger)
    : ExecutionDiagnosticEventListener
{
    public override IDisposable ExecuteRequest(RequestContext context)
    {
        var started = Stopwatch.GetTimestamp();
        return new RequestScope(logger, started);
    }

    private sealed class RequestScope(
        ILogger<RequestTimingListener> logger,
        long started)
        : IDisposable
    {
        public void Dispose()
        {
            var elapsed = Stopwatch.GetElapsedTime(started);
            logger.LogInformation(
                "graphql.request.duration {ElapsedMilliseconds}ms",
                elapsed.TotalMilliseconds);
        }
    }
}
```

If you only need to observe the start of an event, return `EmptyScope`:

```csharp
public override IDisposable ExecuteRequest(RequestContext context)
{
    logger.LogDebug("GraphQL request started.");
    return EmptyScope;
}
```

Keep scopes focused and efficient. Every handler runs on the request path and adds latency. Use low-cardinality, privacy-safe labels such as schema name, operation type, trusted document ID, or document hash. Avoid raw documents, variables, extension values, user IDs, tenant IDs, request IDs, and arbitrary field paths.

---

# Recording Validation, Request, and Resolver Errors

Different error types surface on different listener methods. Use the most specific event so your counters and logs point to the correct pipeline stage.

```csharp
using System.Diagnostics.Metrics;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using Microsoft.Extensions.Logging;

public sealed class ErrorDiagnosticsListener(
    ILogger<ErrorDiagnosticsListener> logger)
    : ExecutionDiagnosticEventListener
{
    private static readonly Meter s_meter = new("Demo.GraphQL");
    private static readonly Counter<long> s_validationErrors =
        s_meter.CreateCounter<long>("graphql.validation.errors");

    public override void ValidationErrors(
        RequestContext context,
        IReadOnlyList<IError> errors)
    {
        foreach (var error in errors)
        {
            s_validationErrors.Add(
                1,
                new KeyValuePair<string, object?>(
                    "code",
                    error.Code ?? "UNSPECIFIED"));
        }
    }

    public override void RequestError(RequestContext context, Exception error)
    {
        logger.LogError(
            error,
            "GraphQL request failed with an unhandled exception.");
    }

    public override void RequestError(RequestContext context, IError error)
    {
        logger.LogWarning(
            "GraphQL request failed with code {Code}.",
            error.Code ?? "UNSPECIFIED");
    }
}
```

Expected signal for an invalid query:

```text
graphql.validation.errors{code="HC0011"} 1
```

- Validation errors come from document validation.
- Request errors indicate request-pipeline termination or unhandled exceptions near request completion.
- Resolver errors are reported through `ResolverError(IMiddlewareContext, IError)` or `ResolverError(RequestContext, ISelection, IError)` depending on the execution path.

```csharp
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Resolvers;
using Microsoft.Extensions.Logging;

public sealed class ResolverErrorListener(
    ILogger<ResolverErrorListener> logger)
    : ExecutionDiagnosticEventListener
{
    public override void ResolverError(IMiddlewareContext context, IError error)
    {
        var field = context.Selection.Field;
        var coordinate = $"{field.DeclaringType.Name}.{field.Name}";
        logger.LogWarning(
            "Resolver {Coordinate} produced error code {Code}.",
            coordinate,
            error.Code ?? "UNSPECIFIED");
    }

    public override void ResolverError(
        RequestContext context,
        ISelection selection,
        IError error)
    {
        var field = selection.Field;
        var coordinate = $"{field.DeclaringType.Name}.{field.Name}";
        logger.LogWarning(
            "Resolver {Coordinate} produced error code {Code}.",
            coordinate,
            error.Code ?? "UNSPECIFIED");
    }
}
```

HTTP parsing and malformed transport errors can occur before an execution `RequestContext` exists. Use server events such as `ParserErrors(HttpContext, IReadOnlyList<IError>)` and `HttpRequestError` for those cases.

**Important:** Never log variables, full documents, resolver results, authorization headers, or exception messages without an approved redaction policy.

---

# Observing Field Resolvers (When Needed)

Field-level diagnostics generate high volumes of data. Enable them for targeted investigations, sampling, local debugging, or temporary production diagnostics.

```csharp
using System.Diagnostics;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Resolvers;
using Microsoft.Extensions.Logging;

public sealed class ResolverTimingListener(
    ILogger<ResolverTimingListener> logger)
    : ExecutionDiagnosticEventListener
{
    public override bool EnableResolveFieldValue => true;

    public override IDisposable ResolveFieldValue(IMiddlewareContext context)
    {
        var field = context.Selection.Field;
        var coordinate = $"{field.DeclaringType.Name}.{field.Name}";
        return new ResolverScope(logger, coordinate, Stopwatch.GetTimestamp());
    }

    private sealed class ResolverScope(
        ILogger<ResolverTimingListener> logger,
        string coordinate,
        long started)
        : IDisposable
    {
        public void Dispose()
        {
            var elapsed = Stopwatch.GetElapsedTime(started);
            logger.LogInformation(
                "Resolver {Coordinate} completed in {ElapsedMilliseconds} ms.",
                coordinate,
                elapsed.TotalMilliseconds);
        }
    }
}
```

Expected output for `{ status }`:

```text
info: ResolverTimingListener[0]
      Resolver Query.status completed in 0.3 ms.
```

- `ResolveFieldValue` and `RunTask` require `EnableResolveFieldValue` to be `true` for the listener to receive those events.
- Use labels such as parent type, field name, schema coordinate, and operation type.
- Do not label metrics with argument values, resolver results, user identifiers, or response paths that include list indexes.

For standard trace visualization of resolver timing, prefer [OpenTelemetry tracing](/docs/hotchocolate/v16/operations/observability/opentelemetry) and adjust `ActivityScopes` as needed.

---

# Monitoring DataLoader Batching

Use DataLoader events when requests execute GreenDonut DataLoaders. These events help you detect N+1 behavior, poor batch sizes, cache misses, and batch failures.

```csharp
using System.Diagnostics.Metrics;
using GreenDonut;

public sealed class DataLoaderMetricsListener : DataLoaderDiagnosticEventListener
{
    private static readonly Meter s_meter = new("Demo.GraphQL");
    private static readonly Histogram<int> s_batchSize =
        s_meter.CreateHistogram<int>("graphql.dataloader.batch.size");
    private static readonly Counter<long> s_cacheHits =
        s_meter.CreateCounter<long>("graphql.dataloader.cache.hits");
    private static readonly Counter<long> s_batchErrors =
        s_meter.CreateCounter<long>("graphql.dataloader.batch.errors");

    public override IDisposable ExecuteBatch<TKey>(
        IDataLoader dataLoader,
        IReadOnlyList<TKey> keys)
    {
        s_batchSize.Record(keys.Count);
        return EmptyScope;
    }

    public override void ResolvedTaskFromCache(
        IDataLoader dataLoader,
        PromiseCacheKey cacheKey,
        Task task)
    {
        s_cacheHits.Add(1);
    }

    public override void BatchError<TKey>(
        IReadOnlyList<TKey> keys,
        Exception error)
    {
        s_batchErrors.Add(1);
    }
}
```

Register the listener as usual:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddDiagnosticEventListener<DataLoaderMetricsListener>();
```

Expected signals:

```text
graphql.dataloader.batch.size 12
graphql.dataloader.cache.hits 5
graphql.dataloader.batch.errors 0
```

- Do not record raw DataLoader keys by default. Keys can contain tenant IDs, user IDs, emails, or other high-cardinality values. Batch size histograms and cache or error counters are safer than key-level logs.
- For deeper dispatcher investigations, use events like `RunBatchDispatchCoordinator()`, `BatchDispatchError(Exception)`, `BatchEvaluated(int openBatches)`, and `BatchDispatched(int dispatchedBatches)`. These are high-volume events.

See [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader) for batching semantics and cache behavior.

---

# Observing HTTP and WebSocket Transport Events

Server events cover GraphQL-over-HTTP parsing, request kind, batching, operation batching, response formatting, and WebSocket session lifetime. These are useful when a request fails before execution starts.

```csharp
using HotChocolate;
using HotChocolate.AspNetCore.Instrumentation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

public sealed class TransportDiagnosticsListener(
    ILogger<TransportDiagnosticsListener> logger)
    : ServerDiagnosticEventListener
{
    public override IDisposable ExecuteHttpRequest(
        HttpContext context,
        HttpRequestKind kind)
    {
        logger.LogDebug(
            "GraphQL HTTP request {Method} {Path} has kind {Kind}.",
            context.Request.Method,
            context.Request.Path,
            kind);
        return EmptyScope;
    }

    public override void ParserErrors(
        HttpContext context,
        IReadOnlyList<IError> errors)
    {
        logger.LogWarning(
            "GraphQL HTTP parse failed on {Path} with {ErrorCount} errors.",
            context.Request.Path,
            errors.Count);
    }

    public override void HttpRequestError(HttpContext context, IError error)
    {
        logger.LogWarning(
            "GraphQL HTTP request failed with code {Code}.",
            error.Code ?? "UNSPECIFIED");
    }

    public override void HttpRequestError(HttpContext context, Exception exception)
    {
        logger.LogError(
            exception,
            "GraphQL HTTP request failed before execution completed.");
    }

    public override void WebSocketSessionError(
        HttpContext context,
        Exception exception)
    {
        logger.LogWarning(
            exception,
            "GraphQL WebSocket session ended with an error.");
    }
}
```

Other transport events include `StartSingleRequest`, `StartBatchRequest`, `StartOperationBatchRequest`, `ParseHttpRequest`, `FormatHttpResponse`, and `WebSocketSession`. You can time response formatting with `FormatHttpResponse(HttpContext, OperationResult)`, but avoid inspecting large result payloads in production.

See [HTTP transport](/docs/hotchocolate/v16/server/http-transport) for request formats, batching, streaming responses, and status code behavior.

---

# Keeping Diagnostics Safe and Fast

Every diagnostic handler runs on the request path. Review listener code as you would production middleware.

| Avoid                                                                                                                        | Use instead                                                                                                    |
| ---------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------- |
| Blocking I/O, synchronous network calls, file writes, and long locks                                                         | Non-blocking metrics/logging APIs or a bounded background queue                                                |
| Reflection-heavy code and JSON serialization of request or result objects                                                    | Precomputed low-cardinality fields such as operation type or field coordinate                                  |
| Raw documents, variables, extensions, headers, resolver arguments, resolver results, DataLoader keys, and exception messages | Redacted error code, operation type, document hash, trusted document ID, schema name                           |
| Metric labels with user ID, tenant ID, raw operation text, request ID, path indexes, raw error message, or DataLoader key    | Labels such as schema, operation type, stable error code, field coordinate, HTTP request kind, status category |
| Field-level events on all traffic without sampling or a clear retention plan                                                 | Request-level events, OpenTelemetry sampling, or temporary field diagnostics                                   |

For OpenTelemetry, start with production-safe defaults. `RequestDetails.Default` avoids the most sensitive request details. `RequestDetails.All`, `ActivityScopes.All`, and `IncludeDataLoaderKeys` require explicit privacy and performance review.

---

# Testing Diagnostic Listeners

Test listeners with representative requests before deploying them. The factory overload lets tests assert on the listener instance.

```csharp
using System.Collections.Concurrent;
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public sealed class DiagnosticListenerTests
{
    [Fact]
    public async Task ExecuteRequest_Should_Record_Request_When_Query_Runs()
    {
        // arrange
        var listener = new CountingListener();
        var services = new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddDiagnosticEventListener(_ => listener)
            .Services
            .BuildServiceProvider();

        // act
        await services.ExecuteRequestAsync("{ status }");

        // assert
        Assert.Equal(1, listener.RequestCount);
    }

    [Fact]
    public async Task ResolveFieldValue_Should_Record_Field_When_Enabled()
    {
        // arrange
        var listener = new CountingListener();
        var services = new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddDiagnosticEventListener(_ => listener)
            .Services
            .BuildServiceProvider();

        // act
        await services.ExecuteRequestAsync("{ status }");

        // assert
        Assert.Contains("Query.status", listener.Fields);
    }

    private sealed class CountingListener : ExecutionDiagnosticEventListener
    {
        public int RequestCount { get; private set; }
        public ConcurrentBag<string> Fields { get; } = [];
        public override bool EnableResolveFieldValue => true;
        public override IDisposable ExecuteRequest(RequestContext context)
        {
            RequestCount++;
            return EmptyScope;
        }
        public override IDisposable ResolveFieldValue(IMiddlewareContext context)
        {
            var field = context.Selection.Field;
            Fields.Add($"{field.DeclaringType.Name}.{field.Name}");
            return EmptyScope;
        }
    }

    public sealed class Query
    {
        public string GetStatus() => "ok";
    }
}
```

- For constructor injection tests, register the dependency in `ServiceCollection` and add `.AddApplicationService<T>()` before `.AddDiagnosticEventListener<TListener>()`.
- For error tests, execute an invalid query or a resolver that throws, then assert the event count or captured field coordinate. Do not assert wall-clock durations except to confirm a metric or log was recorded.

---

# Event Reference

This reference maps production tasks to the main listener APIs. Methods that return `IDisposable` are scopes. Disposal marks the end of that phase.

## ExecutionDiagnosticEventListener

| Task                                | Methods                                                                                                                                                                                                                                                                        | Scope?                              | Production notes                                                                                                                  |
| ----------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ----------------------------------- | --------------------------------------------------------------------------------------------------------------------------------- |
| Request lifecycle                   | `ExecuteRequest(RequestContext)`, `RequestError(RequestContext, Exception)`, `RequestError(RequestContext, IError)`                                                                                                                                                            | `ExecuteRequest`                    | Use for request duration and final request failure signals.                                                                       |
| Document pipeline                   | `ParseDocument(RequestContext)`, `ValidateDocument(RequestContext)`, `ValidationErrors(RequestContext, IReadOnlyList<IError>)`, `CoerceVariables(RequestContext)`, `CompileOperation(RequestContext)`, `ExecuteOperation(RequestContext)`                                      | All except `ValidationErrors`       | Count validation failures by stable code. Avoid documents and variables.                                                          |
| Cost analysis                       | `AnalyzeOperationCost(RequestContext)`, `OperationCost(RequestContext, double fieldCost, double typeCost)`                                                                                                                                                                     | `AnalyzeOperationCost`              | Useful with cost limits and persisted operation policies.                                                                         |
| Resolver and task execution         | `ResolveFieldValue(IMiddlewareContext)`, `ResolverError(IMiddlewareContext, IError)`, `ResolverError(RequestContext, ISelection, IError)`, `RunTask(IExecutionTask)`, `TaskError(IExecutionTask, IError)`, `StartProcessing(RequestContext)`, `StopProcessing(RequestContext)` | `ResolveFieldValue`, `RunTask`      | High volume. `ResolveFieldValue` and `RunTask` require `EnableResolveFieldValue`.                                                 |
| Streaming, subscriptions, and defer | `ExecuteSubscription(RequestContext, ulong)`, `OnSubscriptionEvent(RequestContext, ulong)`, `SubscriptionEventError(RequestContext, ulong, Exception)`, `ExecuteStream(IOperation)`, `ExecuteDeferredTask()`                                                                   | All except `SubscriptionEventError` | Distinguish long-lived subscription setup from each subscription event.                                                           |
| Caches and trusted documents        | `AddedDocumentToCache`, `RetrievedDocumentFromCache`, `RetrievedDocumentFromStorage`, `DocumentNotFoundInStorage(RequestContext, OperationDocumentId)`, `UntrustedDocumentRejected(RequestContext)`, `AddedOperationToCache`, `RetrievedOperationFromCache`                    | No                                  | Prefer document ID or hash over operation text.                                                                                   |
| Executor and batching               | `DispatchBatch(RequestContext)`, `ExecutorCreated(string, IRequestExecutor)`, `ExecutorEvicted(string, IRequestExecutor)`                                                                                                                                                      | `DispatchBatch`                     | Executor events help with startup, warmup, and schema reload troubleshooting. See [warmup](/docs/hotchocolate/v16/server/warmup). |

## ServerDiagnosticEventListener

| Task                        | Methods                                                                                                                                                                                              | Scope?                                   | Production notes                                                           |
| --------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------- | -------------------------------------------------------------------------- |
| HTTP request envelope       | `ExecuteHttpRequest(HttpContext, HttpRequestKind)`                                                                                                                                                   | Yes                                      | Use for request kind and transport timing before execution context exists. |
| HTTP request shape          | `StartSingleRequest(HttpContext, GraphQLRequest)`, `StartBatchRequest(HttpContext, IReadOnlyList<GraphQLRequest>)`, `StartOperationBatchRequest(HttpContext, GraphQLRequest, IReadOnlyList<string>)` | No                                       | Avoid recording raw request documents or variables.                        |
| HTTP parsing and formatting | `ParseHttpRequest(HttpContext)`, `ParserErrors(HttpContext, IReadOnlyList<IError>)`, `FormatHttpResponse(HttpContext, OperationResult)`                                                              | `ParseHttpRequest`, `FormatHttpResponse` | Parse failures may never reach execution listeners.                        |
| HTTP failures               | `HttpRequestError(HttpContext, IError)`, `HttpRequestError(HttpContext, Exception)`                                                                                                                  | No                                       | Log path, method, status category, and redacted error code.                |
| WebSocket session           | `WebSocketSession(HttpContext)`, `WebSocketSessionError(HttpContext, Exception)`                                                                                                                     | `WebSocketSession`                       | Track connection lifetime separately from subscription event execution.    |

## DataLoaderDiagnosticEventListener

| Task                 | Methods                                                                                                                                                       | Scope?                        | Production notes                                                           |
| -------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------- | -------------------------------------------------------------------------- |
| Cache behavior       | `ResolvedTaskFromCache(IDataLoader, PromiseCacheKey, Task)`                                                                                                   | No                            | Count cache hits. Do not record raw cache keys.                            |
| Batch execution      | `ExecuteBatch<TKey>(IDataLoader, IReadOnlyList<TKey>) where TKey : notnull`, `BatchResults<TKey, TValue>(IReadOnlyList<TKey>, ReadOnlySpan<Result<TValue?>>)` | `ExecuteBatch`                | Record batch size and duration. Avoid key labels.                          |
| Batch failures       | `BatchError<TKey>(IReadOnlyList<TKey>, Exception)`, `BatchItemError<TKey>(TKey, Exception)`                                                                   | No                            | Count errors by DataLoader name or stable category, not by key or message. |
| Dispatcher lifecycle | `RunBatchDispatchCoordinator()`, `BatchDispatchError(Exception)`, `BatchEvaluated(int openBatches)`, `BatchDispatched(int dispatchedBatches)`                 | `RunBatchDispatchCoordinator` | High-volume internals for focused investigations.                          |

---

# Troubleshooting

| Symptom                                                       | Likely cause                                                                                                                                | Fix                                                                                                                                                                               |
| ------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Listener never fires                                          | Registered on the wrong schema builder, endpoint uses another executor, or the class inherits the wrong base listener                       | Register on the same `AddGraphQL()` chain and inherit `ExecutionDiagnosticEventListener`, `ServerDiagnosticEventListener`, or `DataLoaderDiagnosticEventListener` as appropriate. |
| Constructor service cannot be resolved                        | Execution listeners are activated from schema services                                                                                      | Register the dependency in application services and expose it with `.AddApplicationService<T>()`, or use the factory and `GetRootServiceProvider()`.                              |
| Field resolver event never fires                              | `EnableResolveFieldValue` is `false`                                                                                                        | Override `public override bool EnableResolveFieldValue => true;` and run a query that resolves the target field.                                                                  |
| Transport parse errors do not hit `RequestError`              | The request failed before execution context creation                                                                                        | Implement `ServerDiagnosticEventListener.ParserErrors` or `HttpRequestError`.                                                                                                     |
| DataLoader events do not fire                                 | The request path does not use GreenDonut DataLoaders, values are not batched, or the listener is registered in a different service provider | Execute a resolver that calls a DataLoader and register a DataLoader listener on the GraphQL builder.                                                                             |
| Custom listener logic does not run after adding OpenTelemetry | `AddInstrumentation()` registers built-in activity listeners, not your custom listener                                                      | Also call `.AddDiagnosticEventListener<YourListener>()`.                                                                                                                          |
| Metrics backend creates too many series                       | Labels contain high-cardinality values                                                                                                      | Remove raw operation text, variables, keys, user IDs, tenant IDs, request IDs, and raw errors from tags.                                                                          |
| Requests slow down after deployment                           | The handler performs heavy synchronous work, or field-level events run for all traffic                                                      | Sample, disable field events, reduce allocations, or move expensive work to a background queue.                                                                                   |
| Counts are duplicated                                         | Multiple schemas, multiple listeners, batching, retries, or both custom metrics and span-derived metrics count the same event               | Document counting boundaries and label schema or source.                                                                                                                          |

---

# Next Steps

- Use [OpenTelemetry tracing](/docs/hotchocolate/v16/operations/observability/opentelemetry) for standard spans, exporters, `AddInstrumentation`, `ActivityScopes`, `RequestDetails`, and `ActivityEnricher`.
- Use [metrics](/docs/hotchocolate/v16/operations/observability/metrics) for metric naming, histograms, counters, and cardinality guidance.
- Review [HTTP transport](/docs/hotchocolate/v16/server/http-transport) for batching, status codes, and streaming response behavior.
- Review [interceptors](/docs/hotchocolate/v16/server/interceptors) when you need to mutate request state.
- Review [error handling](/docs/hotchocolate/v16/guides/error-handling) when you need to shape errors returned to clients.
- Review [warmup](/docs/hotchocolate/v16/server/warmup) for executor startup, warmup, and cache pre-population.
- Review [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader) for batching and request-scoped cache behavior.
