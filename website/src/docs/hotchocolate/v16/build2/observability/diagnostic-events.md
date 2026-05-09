---
title: Diagnostic events
---

Diagnostic events are Hot Chocolate lifecycle callbacks. Use them when standard traces tell you that a GraphQL request is slow or failing, but your application needs a custom log line, custom metric, audit marker, or narrowly scoped timing signal.

This page covers custom diagnostic listeners in Hot Chocolate v16. It does not configure exporters, dashboards, or span attributes. For standard distributed tracing, start with [OpenTelemetry](./opentelemetry). For adding safe tags to Hot Chocolate spans, use `ActivityEnricher`. For changing client-visible GraphQL errors, use error filters rather than diagnostic listeners.

# Mental model

```text
ASP.NET Core transport
  HTTP parsing, request shape, response formatting, WebSocket sessions
        |
        v
GraphQL execution
  request, parsing, validation, cost, variables, operation, resolvers,
  subscriptions, caches, persisted documents, executor lifecycle
        |
        v
DataLoader
  cache hits, batch execution, batch results, dispatch coordinator
```

Hot Chocolate exposes three listener base classes:

| Listener                            | Use it for                                                                                                                           |
| ----------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------ |
| `ServerDiagnosticEventListener`     | GraphQL over HTTP and WebSocket transport events.                                                                                    |
| `ExecutionDiagnosticEventListener`  | GraphQL request execution, resolver execution, subscriptions, caches, persisted or trusted documents, and executor lifecycle events. |
| `DataLoaderDiagnosticEventListener` | GreenDonut DataLoader cache, batch, item error, and dispatch events.                                                                 |

Some methods return `IDisposable`. Those methods start a scope. Hot Chocolate disposes the returned object when the observed operation ends, so the scope can measure duration or emit a completion signal. Methods returning `void` are instant events. Return `EmptyScope` from a scope method when you do not need completion logic.

Diagnostic handlers run synchronously on the request path. Keep them fast, avoid blocking I/O, and move expensive work to a bounded background service.

# Register a listener

Register listeners on the GraphQL builder with `AddDiagnosticEventListener<T>()`.

```csharp
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using Microsoft.Extensions.Logging;

public sealed class RequestErrorListener(
    ILogger<RequestErrorListener> logger)
    : ExecutionDiagnosticEventListener
{
    public override void RequestError(RequestContext context, IError error)
    {
        logger.LogWarning(
            "GraphQL request failed with code {Code}",
            error.Code ?? "UNKNOWN");
    }

    public override void RequestError(RequestContext context, Exception error)
    {
        logger.LogError(error, "GraphQL request failed with an exception");
    }
}

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddDiagnosticEventListener<RequestErrorListener>();
```

You can also use the factory overload when the listener instance is created manually.

```csharp
var listener = new RequestErrorListener(logger);

builder
    .AddGraphQL()
    .AddDiagnosticEventListener(_ => listener);
```

Multiple execution listeners can be registered. Hot Chocolate calls them in registration order.

## Dependency injection and lifetime

Listener instances are long lived. Do not store per-request state on the listener object. Store request state in the returned scope object, local variables, `RequestContext.ContextData`, or a thread-safe application service.

Execution listeners are activated from schema services. If an execution listener needs an application service, register that service with the application service provider and expose it to schema services with `.AddApplicationService<T>()`.

```csharp
builder.Services.AddSingleton<GraphQLMetrics>();

builder
    .AddGraphQL()
    .AddApplicationService<GraphQLMetrics>()
    .AddDiagnosticEventListener<MetricsExecutionListener>();
```

DataLoader listeners are registered through the application service provider. Server listeners are registered through the diagnostic source metadata on their base class.

# Time work with a scope

Use a scope when you need duration. This listener logs only slow GraphQL requests and records safe identifiers.

```csharp
using System.Diagnostics;
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using Microsoft.Extensions.Logging;

public sealed class SlowRequestListener(
    ILogger<SlowRequestListener> logger)
    : ExecutionDiagnosticEventListener
{
    private static readonly TimeSpan SlowThreshold = TimeSpan.FromMilliseconds(250);

    public override IDisposable ExecuteRequest(RequestContext context)
    {
        return new RequestScope(context, logger, Stopwatch.GetTimestamp());
    }

    private sealed class RequestScope(
        RequestContext context,
        ILogger logger,
        long started) : IDisposable
    {
        public void Dispose()
        {
            var elapsed = Stopwatch.GetElapsedTime(started);

            if (elapsed < SlowThreshold)
            {
                return;
            }

            logger.LogInformation(
                "Slow GraphQL request {OperationName} {DocumentHash} took {ElapsedMilliseconds} ms",
                context.Request.OperationName ?? "<anonymous>",
                context.Request.DocumentHash.ToString(),
                elapsed.TotalMilliseconds);
        }
    }
}
```

The example logs operation name and document hash. It does not log raw documents, variables, extensions, authorization headers, resolver results, or DataLoader keys.

# Observe execution events

Use `ExecutionDiagnosticEventListener` for GraphQL execution phases.

```csharp
using System.Diagnostics.Metrics;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;

public sealed class ErrorMetricsListener(GraphQLMetrics metrics)
    : ExecutionDiagnosticEventListener
{
    public override void ValidationErrors(
        RequestContext context,
        IReadOnlyList<IError> errors)
    {
        metrics.ValidationErrors.Add(errors.Count);
    }

    public override void RequestError(RequestContext context, IError error)
    {
        metrics.RequestErrors.Add(1,
            new KeyValuePair<string, object?>("code", error.Code ?? "UNKNOWN"));
    }

    public override void OperationCost(
        RequestContext context,
        double fieldCost,
        double typeCost)
    {
        metrics.FieldCost.Record(fieldCost);
        metrics.TypeCost.Record(typeCost);
    }
}

public sealed class GraphQLMetrics
{
    private readonly Meter _meter = new("Example.GraphQL");

    public Counter<long> ValidationErrors { get; }
    public Counter<long> RequestErrors { get; }
    public Histogram<double> FieldCost { get; }
    public Histogram<double> TypeCost { get; }

    public GraphQLMetrics()
    {
        ValidationErrors = _meter.CreateCounter<long>("graphql.validation_errors");
        RequestErrors = _meter.CreateCounter<long>("graphql.request_errors");
        FieldCost = _meter.CreateHistogram<double>("graphql.operation.field_cost");
        TypeCost = _meter.CreateHistogram<double>("graphql.operation.type_cost");
    }
}
```

Execution event groups:

| Group                           | Events                                                                                                       |
| ------------------------------- | ------------------------------------------------------------------------------------------------------------ |
| Request lifetime                | `ExecuteRequest`, `RequestError(RequestContext, Exception)`, `RequestError(RequestContext, IError)`          |
| Document phases                 | `ParseDocument`, `ValidateDocument`, `ValidationErrors`, `CoerceVariables`                                   |
| Cost and operation preparation  | `AnalyzeOperationCost`, `OperationCost`, `CompileOperation`                                                  |
| Operation execution             | `ExecuteOperation`, `StartProcessing`, `StopProcessing`, `RunTask`, `TaskError`                              |
| Resolver execution              | `ResolveFieldValue`, `ResolverError`                                                                         |
| Subscriptions                   | `ExecuteSubscription`, `OnSubscriptionEvent`, `SubscriptionEventError`                                       |
| Document and operation caches   | `AddedDocumentToCache`, `RetrievedDocumentFromCache`, `AddedOperationToCache`, `RetrievedOperationFromCache` |
| Persisted and trusted documents | `RetrievedDocumentFromStorage`, `DocumentNotFoundInStorage`, `UntrustedDocumentRejected`                     |
| Schema executor lifecycle       | `ExecutorCreated`, `ExecutorEvicted`                                                                         |

Hot Chocolate v16 exposes executor lifecycle events, not a separate schema diagnostic listener.

# Observe resolver execution only when needed

Resolver events can be high volume. Custom execution listeners do not receive `ResolveFieldValue` and `RunTask` by default. Opt in by overriding `EnableResolveFieldValue`.

```csharp
using System.Diagnostics;
using HotChocolate;
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
        return new ResolverScope(context, logger, Stopwatch.GetTimestamp());
    }

    public override void ResolverError(IMiddlewareContext context, IError error)
    {
        logger.LogWarning(
            "Resolver error at {TypeName}.{FieldName} with code {Code}",
            context.ObjectType.Name,
            context.Selection.Field.Name,
            error.Code ?? "UNKNOWN");
    }

    private sealed class ResolverScope(
        IMiddlewareContext context,
        ILogger logger,
        long started) : IDisposable
    {
        public void Dispose()
        {
            var elapsed = Stopwatch.GetElapsedTime(started);

            logger.LogDebug(
                "Resolved {TypeName}.{FieldName} in {ElapsedMilliseconds} ms",
                context.ObjectType.Name,
                context.Selection.Field.Name,
                elapsed.TotalMilliseconds);
        }
    }
}
```

Use parent type, field definition name, duration buckets, and bounded error codes as labels. Avoid aliases, argument values, variable values, full paths with list indexes, and resolver results.

# Observe HTTP and WebSocket transport events

Use `ServerDiagnosticEventListener` for GraphQL transport diagnostics.

```csharp
using HotChocolate;
using HotChocolate.AspNetCore.Instrumentation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

public sealed class TransportLoggingListener(
    ILogger<TransportLoggingListener> logger)
    : ServerDiagnosticEventListener
{
    public override IDisposable ExecuteHttpRequest(
        HttpContext context,
        HttpRequestKind kind)
    {
        logger.LogDebug("GraphQL HTTP request kind {Kind}", kind);
        return EmptyScope;
    }

    public override void HttpRequestError(HttpContext context, IError error)
    {
        logger.LogWarning(
            "GraphQL HTTP request failed with code {Code}",
            error.Code ?? "UNKNOWN");
    }

    public override void HttpRequestError(HttpContext context, Exception exception)
    {
        logger.LogError(exception, "GraphQL HTTP request failed");
    }
}
```

Do not log request bodies, variables, authorization headers, cookies, or WebSocket payloads from transport listeners.

# Observe DataLoader batches

Use `DataLoaderDiagnosticEventListener` for GreenDonut DataLoader batching and cache diagnostics. Prefer counts and sizes over keys.

```csharp
using System.Diagnostics.Metrics;
using GreenDonut;
using HotChocolate;

public sealed class DataLoaderMetricsListener(GraphQLDataLoaderMetrics metrics)
    : DataLoaderDiagnosticEventListener
{
    public override IDisposable ExecuteBatch<TKey>(
        IDataLoader dataLoader,
        IReadOnlyList<TKey> keys)
    {
        metrics.BatchSize.Record(keys.Count);
        return EmptyScope;
    }

    public override void BatchError<TKey>(
        IReadOnlyList<TKey> keys,
        Exception error)
    {
        metrics.BatchErrors.Add(1);
    }

    public override void BatchItemError<TKey>(TKey key, Exception error)
    {
        metrics.BatchItemErrors.Add(1);
    }

    public override void ResolvedTaskFromCache(
        IDataLoader dataLoader,
        PromiseCacheKey cacheKey,
        Task task)
    {
        metrics.CacheHits.Add(1);
    }
}

public sealed class GraphQLDataLoaderMetrics
{
    private readonly Meter _meter = new("Example.GraphQL.DataLoader");

    public Histogram<int> BatchSize { get; }
    public Counter<long> BatchErrors { get; }
    public Counter<long> BatchItemErrors { get; }
    public Counter<long> CacheHits { get; }

    public GraphQLDataLoaderMetrics()
    {
        BatchSize = _meter.CreateHistogram<int>("graphql.dataloader.batch_size");
        BatchErrors = _meter.CreateCounter<long>("graphql.dataloader.batch_errors");
        BatchItemErrors = _meter.CreateCounter<long>("graphql.dataloader.batch_item_errors");
        CacheHits = _meter.CreateCounter<long>("graphql.dataloader.cache_hits");
    }
}
```

DataLoader keys can contain user data, database ids, or high-cardinality values. Do not place keys in log messages or metric labels unless they have been reviewed and scrubbed.

# Event reference

## Execution events

| Method                         | Type    | Main parameters                           | When it fires                                                                | Safe use                                            |
| ------------------------------ | ------- | ----------------------------------------- | ---------------------------------------------------------------------------- | --------------------------------------------------- |
| `ExecuteRequest`               | Scope   | `RequestContext`                          | Request execution starts.                                                    | Request duration and request-level counts.          |
| `RequestError`                 | Instant | `RequestContext`, `Exception` or `IError` | Request execution reports an exception or request-terminating GraphQL error. | Error counts by bounded type or code.               |
| `ParseDocument`                | Scope   | `RequestContext`                          | Document parsing starts.                                                     | Parse duration.                                     |
| `ValidateDocument`             | Scope   | `RequestContext`                          | Document validation starts.                                                  | Validation duration.                                |
| `ValidationErrors`             | Instant | `RequestContext`, `IReadOnlyList<IError>` | Validation produces errors.                                                  | Error count by safe code.                           |
| `AnalyzeOperationCost`         | Scope   | `RequestContext`                          | Operation cost analysis starts.                                              | Cost analysis duration.                             |
| `OperationCost`                | Instant | `RequestContext`, `fieldCost`, `typeCost` | Cost analysis reports values.                                                | Cost histograms.                                    |
| `CoerceVariables`              | Scope   | `RequestContext`                          | Variable coercion starts.                                                    | Coercion duration.                                  |
| `CompileOperation`             | Scope   | `RequestContext`                          | Operation compilation starts.                                                | Compilation duration and cache investigation.       |
| `ExecuteOperation`             | Scope   | `RequestContext`                          | Operation execution starts.                                                  | Operation duration.                                 |
| `StartProcessing`              | Instant | `RequestContext`                          | Execution starts processing work.                                            | Scheduler state counters.                           |
| `StopProcessing`               | Instant | `RequestContext`                          | Execution stops processing work.                                             | Scheduler state counters.                           |
| `RunTask`                      | Scope   | `IExecutionTask`                          | Execution task runs. Gated by `EnableResolveFieldValue`.                     | Targeted task timing.                               |
| `TaskError`                    | Instant | `IExecutionTask`, `IError`                | Execution task reports an error.                                             | Task error counts.                                  |
| `ResolveFieldValue`            | Scope   | `IMiddlewareContext`                      | Field resolver runs. Gated by `EnableResolveFieldValue`.                     | Targeted resolver timing.                           |
| `ResolverError`                | Instant | `IMiddlewareContext`, `IError`            | Resolver reports an error.                                                   | Resolver error counts by field coordinate and code. |
| `ExecuteSubscription`          | Scope   | `RequestContext`, `ulong`                 | Subscription is created.                                                     | Subscription lifetime.                              |
| `OnSubscriptionEvent`          | Scope   | `RequestContext`, `ulong`                 | Subscription event produces a result.                                        | Event processing duration.                          |
| `SubscriptionEventError`       | Instant | `RequestContext`, `ulong`, `Exception`    | Subscription event processing fails.                                         | Subscription error counts.                          |
| `AddedDocumentToCache`         | Instant | `RequestContext`                          | Document cache stores a document.                                            | Cache store count.                                  |
| `RetrievedDocumentFromCache`   | Instant | `RequestContext`                          | Document cache returns a document.                                           | Cache hit count.                                    |
| `RetrievedDocumentFromStorage` | Instant | `RequestContext`                          | Persisted document storage returns a document.                               | Persisted document hit count.                       |
| `DocumentNotFoundInStorage`    | Instant | `RequestContext`, `OperationDocumentId`   | Persisted document storage misses.                                           | Miss count by bounded category.                     |
| `UntrustedDocumentRejected`    | Instant | `RequestContext`                          | Request uses an untrusted document when trusted documents are required.      | Rejection count.                                    |
| `AddedOperationToCache`        | Instant | `RequestContext`                          | Operation cache stores a compiled operation.                                 | Cache store count.                                  |
| `RetrievedOperationFromCache`  | Instant | `RequestContext`                          | Operation cache returns a compiled operation.                                | Cache hit count.                                    |
| `ExecutorCreated`              | Instant | `string`, `IRequestExecutor`              | Request executor is created for a schema.                                    | Schema startup diagnostics.                         |
| `ExecutorEvicted`              | Instant | `string`, `IRequestExecutor`              | Request executor is evicted.                                                 | Schema reload diagnostics.                          |

## Server events

| Method                       | Type    | Main parameters                                          | When it fires                                    | Safe use                          |
| ---------------------------- | ------- | -------------------------------------------------------- | ------------------------------------------------ | --------------------------------- |
| `ExecuteHttpRequest`         | Scope   | `HttpContext`, `HttpRequestKind`                         | GraphQL HTTP request enters the transport layer. | Request kind timing.              |
| `StartSingleRequest`         | Instant | `HttpContext`, `GraphQLRequest`                          | A single GraphQL request starts.                 | Request shape counts.             |
| `StartBatchRequest`          | Instant | `HttpContext`, `IReadOnlyList<GraphQLRequest>`           | A request batch starts.                          | Batch request counts and sizes.   |
| `StartOperationBatchRequest` | Instant | `HttpContext`, `GraphQLRequest`, `IReadOnlyList<string>` | An operation batch starts.                       | Operation batch counts and sizes. |
| `HttpRequestError`           | Instant | `HttpContext`, `IError` or `Exception`                   | Transport processing reports an error.           | Transport error counts.           |
| `ParseHttpRequest`           | Scope   | `HttpContext`                                            | HTTP body parsing starts.                        | Parse duration.                   |
| `ParserErrors`               | Instant | `HttpContext`, `IReadOnlyList<IError>`                   | HTTP request parsing produces errors.            | Parse error counts.               |
| `FormatHttpResponse`         | Scope   | `HttpContext`, `OperationResult`                         | HTTP response formatting starts.                 | Formatting duration.              |
| `WebSocketSession`           | Scope   | `HttpContext`                                            | GraphQL WebSocket session starts.                | Session lifetime.                 |
| `WebSocketSessionError`      | Instant | `HttpContext`, `Exception`                               | WebSocket session fails.                         | Session error counts.             |

## DataLoader events

| Method                        | Type    | Main parameters                                        | When it fires                           | Safe use                       |
| ----------------------------- | ------- | ------------------------------------------------------ | --------------------------------------- | ------------------------------ |
| `ResolvedTaskFromCache`       | Instant | `IDataLoader`, `PromiseCacheKey`, `Task`               | DataLoader resolves an item from cache. | Cache hit count.               |
| `ExecuteBatch`                | Scope   | `IDataLoader`, `IReadOnlyList<TKey>`                   | DataLoader batch starts.                | Batch duration and size.       |
| `BatchResults`                | Instant | `IReadOnlyList<TKey>`, `ReadOnlySpan<Result<TValue?>>` | Batch returns results.                  | Result count.                  |
| `BatchError`                  | Instant | `IReadOnlyList<TKey>`, `Exception`                     | Batch fails.                            | Batch error count.             |
| `BatchItemError`              | Instant | `TKey`, `Exception`                                    | One batch item fails.                   | Item error count.              |
| `RunBatchDispatchCoordinator` | Scope   | None                                                   | Batch dispatch coordinator runs.        | Dispatch coordinator duration. |
| `BatchDispatchError`          | Instant | `Exception`                                            | Batch dispatch coordinator fails.       | Dispatch error count.          |
| `BatchEvaluated`              | Instant | `int openBatches`                                      | Dispatcher evaluates open batches.      | Open batch gauge or histogram. |
| `BatchDispatched`             | Instant | `int dispatchedBatches`                                | Dispatcher dispatches batches.          | Dispatched batch count.        |

# Diagnostic events, OpenTelemetry, logs, and errors

| Need                                                             | Use                                                                                                            |
| ---------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------- |
| Distributed traces and backend integration                       | `.AddInstrumentation()` plus `.AddHotChocolateInstrumentation()` on the [OpenTelemetry](./opentelemetry) page. |
| Extra safe tags on Hot Chocolate spans                           | `ActivityEnricher`.                                                                                            |
| Custom log lines or custom metrics from GraphQL lifecycle events | Diagnostic listeners from this page.                                                                           |
| Application spans outside GraphQL internals                      | Your own .NET `ActivitySource`.                                                                                |
| Client-safe GraphQL error shaping                                | Error filters and exception detail configuration.                                                              |

`.AddInstrumentation()` registers Hot Chocolate listeners that create .NET `Activity` spans from selected diagnostic events. `.AddHotChocolateInstrumentation()` adds the `HotChocolate.Diagnostics` activity source to OpenTelemetry tracing. Custom diagnostic listeners are for your own side effects, logs, and metrics outside the built-in spans.

If all you need is another attribute on a built-in Hot Chocolate span, prefer `ActivityEnricher`. If all you need is an application log at a resolver or service boundary, prefer `ILogger` in that application code.

# Privacy and performance checklist

- Handlers run synchronously on the GraphQL request path.
- Do not block on network, disk, or exporter I/O in a handler.
- Keep labels low-cardinality: operation name, document hash or id, request kind, schema coordinate, event name, duration bucket, count, and bounded error code.
- Avoid raw GraphQL documents, variables, extensions, authorization data, cookies, DataLoader keys, resolver results, exception messages, and stack traces unless approved and scrubbed.
- Make listener state thread-safe.
- Do not store per-request state on the listener instance.
- Enable resolver-level events only for targeted diagnostics.
- Prefer thresholds or sampling for noisy logs.

# Test a custom listener

A focused test can register a listener instance with the factory overload, execute a request, and assert the listener observed the expected event.

```csharp
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using Microsoft.Extensions.DependencyInjection;

var listener = new TestExecutionListener();

var executor = await new ServiceCollection()
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddDiagnosticEventListener(_ => listener)
    .BuildRequestExecutorAsync();

await executor.ExecuteAsync("{ hello }");

Assert.Equal(1, listener.RequestCount);

private sealed class TestExecutionListener : ExecutionDiagnosticEventListener
{
    public int RequestCount;

    public override IDisposable ExecuteRequest(RequestContext context)
    {
        RequestCount++;
        return EmptyScope;
    }
}
```

For resolver event tests, set `EnableResolveFieldValue` to `true`. For dependency injection tests, register application services and expose them with `.AddApplicationService<T>()` when the listener is an execution listener.

# Troubleshooting

| Problem                                   | Likely cause                                                                        | Fix                                                                                                 |
| ----------------------------------------- | ----------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------- |
| Listener does not fire                    | It is registered on a different GraphQL builder or inherits the wrong base class.   | Register it on the same builder as the schema and use the listener base class for the event family. |
| Constructor dependency cannot be resolved | Execution listener is created from schema services.                                 | Register the dependency in application services and add `.AddApplicationService<T>()`.              |
| Resolver events do not fire               | `EnableResolveFieldValue` is `false`.                                               | Override `EnableResolveFieldValue` and return `true` for targeted diagnostics.                      |
| DataLoader events do not fire             | The request path does not use GreenDonut DataLoader.                                | Verify the resolver uses DataLoader APIs and the batch is reached.                                  |
| Server events do not fire                 | Requests are not reaching Hot Chocolate ASP.NET Core middleware.                    | Check endpoint mapping and middleware order.                                                        |
| Listener adds latency                     | Handler performs blocking work or captures too much data.                           | Remove blocking work, reduce labels, add thresholds, or enqueue work.                               |
| Logs or metrics are noisy                 | Labels include aliases, paths with indexes, keys, variables, or exception messages. | Use bounded labels and aggregate counts or histograms.                                              |
| You need to alter GraphQL errors          | Diagnostic listeners are observers.                                                 | Use error filters or exception detail configuration.                                                |

# Next steps

- [Observability overview](./): choose between diagnostic listeners, OpenTelemetry, and activity enrichment.
- [OpenTelemetry](./opentelemetry): configure tracing, scopes, request details, and exporters.
- `ActivityEnricher`: add safe attributes to Hot Chocolate activities.
- [Execution engine](../execution-engine): understand where execution diagnostics fit in the request pipeline.
- [Service injection](../resolvers/service-injection): use `.AddApplicationService<T>()` for schema-level components.
- [HTTP transport](../server-configuration/http-transport) and [WebSocket transport](../server-configuration/websocket-transport): understand transport behavior behind server events.
- [DataLoader](../dataloader): understand batching behavior behind DataLoader diagnostics.
- [Cost analysis](../security/cost-analysis): understand `AnalyzeOperationCost` and `OperationCost`.
