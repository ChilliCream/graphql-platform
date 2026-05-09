---
title: Observability
---

When a production GraphQL incident occurs, you need answers to questions like:

- Which operation was executed, and how long did it take?
- At what stage did the request fail: HTTP parsing, document parsing, validation, cost analysis, compilation, resolver execution, DataLoader batching, or response formatting?
- Which downstream calls were involved in the same request?
- Did the trace, log, or metric contain data that is safe to store?

Hot Chocolate v16 provides three main entry points for observability:

1. **Diagnostic events**: Synchronous lifecycle hooks for custom logging, metrics, and in-process policies.
2. **OpenTelemetry instrumentation**: Converts selected Hot Chocolate events into .NET `Activity` spans.
3. **Activity enrichment**: Allows you to add safe, bounded tags to Hot Chocolate activities.

This page introduces observability for the Hot Chocolate server. It does not cover vendor-specific backends, comprehensive OpenTelemetry theory, Fusion diagnostics, or dashboard design.

# Mental Model: Hooks, Activities, and Enrichment

```text
HTTP or WebSocket transport
        |
        v
Hot Chocolate diagnostic events
  |          |             |
server   execution     DataLoader
  |          |             |
  +----------+-------------+
             |
     AddInstrumentation()
             |
ActivitySource: HotChocolate.Diagnostics
             |
OpenTelemetry TracerProvider
             |
Exporter, backend, dashboard
```

Diagnostic events provide lifecycle information from Hot Chocolate. When you call `.AddInstrumentation()`, Hot Chocolate registers listeners that create activities from selected server, execution, and DataLoader events. Adding `.AddHotChocolateInstrumentation()` registers the `HotChocolate.Diagnostics` activity source with OpenTelemetry tracing.

The `ActivityEnricher` customizes activities emitted by Hot Chocolate. It does not create a separate tracing setup and does not replace diagnostic listeners.

For logging and metrics, use standard .NET patterns. Choose `ILogger`, OpenTelemetry logging, `Meter`, or diagnostic listeners as needed. The built-in observability surface in v16 is based on traces and activities. You can build custom metrics from diagnostic events.

# Choosing Your Observability Approach

| Goal                                                          | Use                                                              | Learn more                                                              |
| ------------------------------------------------------------- | ---------------------------------------------------------------- | ----------------------------------------------------------------------- |
| View request timelines across GraphQL and downstream services | `.AddInstrumentation()` plus `.AddHotChocolateInstrumentation()` | [OpenTelemetry](./opentelemetry)                                        |
| Add business or deployment context to GraphQL spans           | `ActivityEnricher`                                               | [Activity enrichment](./activity-enrichment)                            |
| Log selected GraphQL lifecycle events                         | Diagnostic listener plus `ILogger`                               | [Diagnostic events](./diagnostic-events)                                |
| Publish custom counters or histograms                         | Diagnostic listener plus `Meter`                                 | [Diagnostic events](./diagnostic-events), [Performance](../performance) |
| Inspect server, execution, and DataLoader event contracts     | Listener reference                                               | [Diagnostic events](./diagnostic-events)                                |
| Investigate missing, noisy, or unsafe telemetry               | Checklist on this page, then tracing details                     | [OpenTelemetry](./opentelemetry)                                        |

```text
Need request timeline?        -> OpenTelemetry tracing
Need extra span tags?         -> ActivityEnricher
Need custom logs?             -> Diagnostic event listener + ILogger
Need counters or histograms?  -> Diagnostic event listener + Meter
Need lifecycle method details? -> Diagnostic events reference
```

# Quick Start: Trace a Hot Chocolate Server

Install `HotChocolate.Diagnostics` with the same version as your other Hot Chocolate packages.

<PackageInstallation packageName="HotChocolate.Diagnostics" />

Add instrumentation to the GraphQL builder:

```csharp
using HotChocolate.Diagnostics;

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddInstrumentation();
```

Configure OpenTelemetry tracing and register Hot Chocolate's activity source:

```csharp
using OpenTelemetry.Trace;

builder.Services
    .AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddHotChocolateInstrumentation()
        .AddOtlpExporter());
```

Add instrumentation for ASP.NET Core, HTTP client, database, and application `ActivitySource` as needed. Hot Chocolate spans describe the GraphQL portion of the request. For end-to-end traces, instrument the host and downstream systems as well.

# What Hot Chocolate Can Observe

Hot Chocolate emits events in three main layers:

## Transport and Server Layer

This layer covers GraphQL over HTTP requests, single and batch requests, operation batches, HTTP parsing errors, response formatting, and WebSocket sessions. A single HTTP request may contain one or more GraphQL operations.

## Execution Layer

The execution layer includes request execution, document parsing, validation, cost analysis, variable coercion, operation compilation, operation execution, resolver fields, subscriptions, document cache activity, persisted or trusted document lookup, operation cache activity, and executor lifecycle events.

In v16, the root GraphQL activity name is low-cardinality. When Hot Chocolate knows the operation type, the display name is `query`, `mutation`, or `subscription`. The operation name is emitted as the `graphql.operation.name` attribute.

## DataLoader Layer

This layer includes batch dispatch, batch execution, batch size, cache behavior, batch errors, and item errors. DataLoader spans help you distinguish time spent waiting for batched data access from time spent inside resolvers.

Some emitted attributes are Hot Chocolate extensions to the proposed GraphQL OpenTelemetry semantic conventions. See the [OpenTelemetry page](./opentelemetry) for details on span families and attributes.

# Production Defaults: Start Small, Add Detail as Needed

Begin with the default options and add detail only when you have a specific question. Increasing the scope produces more spans and more synchronous work on the request path.

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddInstrumentation(options =>
    {
        options.Scopes = ActivityScopes.Default;
        options.RequestDetails = RequestDetails.Default;
        options.IncludeDocument = false;
        options.IncludeDataLoaderKeys = false;
        options.MaxErrorEvents = 10;
    });
```

Key v16 options:

| Option                          | Default                  | Purpose                                                          |
| ------------------------------- | ------------------------ | ---------------------------------------------------------------- |
| `InstrumentationOptions.Scopes` | `ActivityScopes.Default` | Select which Hot Chocolate activities are created.               |
| `RequestDetails`                | `RequestDetails.Default` | Include request id, hash, operation name, and extensions.        |
| `IncludeDocument`               | `false`                  | Include the parsed document on activities. Review privacy first. |
| `IncludeDataLoaderKeys`         | `false`                  | Include DataLoader batch keys. Enable only if the keys are safe. |
| `MaxErrorEvents`                | `10`                     | Cap `graphql.error` events on the root GraphQL activity.         |

`ActivityScopes.Default` includes resolver field spans through `ResolveFieldValue`. This is useful for latency investigation, but can generate high volume for large operations. Use `ActivityScopes.All` for local diagnostics or during a bounded incident window, then return to a smaller scope set.

Prefer operation names, document hashes, and trusted document ids over full document bodies. Review request extensions before retaining them in strict environments, as applications often place tenant, client, or authentication-related metadata there.

# Data Safety and Cardinality

Telemetry is part of your data handling surface. Exporters and backends may retain it longer than application logs.

| Data               | Safer default                                      | Higher-risk option                                |
| ------------------ | -------------------------------------------------- | ------------------------------------------------- |
| Operation identity | Operation name, document hash, trusted document id | Full document body                                |
| Variables          | Omit or redact                                     | `RequestDetails.Variables`                        |
| Extensions         | Bounded and reviewed values                        | Unreviewed client or authentication metadata      |
| DataLoader keys    | Batch size and counts                              | `IncludeDataLoaderKeys`                           |
| Errors             | Code, type, count                                  | Message, exception message, stack trace           |
| Custom tags        | Low-cardinality classification                     | User ids, emails, raw tenant ids, free-form input |

GraphQL error responses, server logs, and OpenTelemetry events are separate surfaces. `IncludeExceptionDetails` controls what exception details are visible to clients, but telemetry and logs may still contain exception information if your listeners, enrichers, or exporters record it.

# When to Use Diagnostic Events

Use diagnostic events for custom logging, custom metrics, policy hooks, or integration with existing in-process observability code.

Hot Chocolate v16 provides listener base classes for the main event families:

- `ServerDiagnosticEventListener`
- `ExecutionDiagnosticEventListener`
- `DataLoaderDiagnosticEventListener`

Register a listener with `.AddDiagnosticEventListener<T>()`:

```csharp
using System.Diagnostics;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using Microsoft.Extensions.Logging;

public sealed class RequestLoggingListener(
    ILogger<RequestLoggingListener> logger)
    : ExecutionDiagnosticEventListener
{
    public override IDisposable ExecuteRequest(RequestContext context)
    {
        return new RequestScope(logger);
    }

    public override void RequestError(RequestContext context, IError error)
    {
        logger.LogWarning(
            "GraphQL request error {Code}",
            error.Code ?? "UNKNOWN");
    }

    private sealed class RequestScope(ILogger logger) : IDisposable
    {
        private readonly long _started = Stopwatch.GetTimestamp();

        public void Dispose()
        {
            var elapsed = Stopwatch.GetElapsedTime(_started);

            logger.LogInformation(
                "GraphQL request completed in {ElapsedMilliseconds} ms",
                elapsed.TotalMilliseconds);
        }
    }
}

builder
    .AddGraphQL()
    .AddDiagnosticEventListener<RequestLoggingListener>();
```

Diagnostic handlers run synchronously as part of the request. Keep them fast, avoid blocking I/O, and enqueue expensive work for a background service. Scope methods return `IDisposable`; return `EmptyScope` if you do not need completion logic. For custom execution listeners, `EnableResolveFieldValue` defaults to `false` because resolver field events can be high volume.

If a listener requires application services from the schema service provider, use the v16 application service registration pattern described in [service injection](../resolvers/service-injection).

# When to Use OpenTelemetry Tracing

Use OpenTelemetry when you need request timelines, distributed trace relationships, exporter integration, and correlation with ASP.NET Core, HTTP client, database, and application spans.

Hot Chocolate tracing can include these span families:

- HTTP request parsing and response formatting
- GraphQL request or root operation span
- Document parsing and validation
- Cost analysis (when enabled)
- Variable coercion, operation compilation, and operation execution
- Resolver field spans
- DataLoader batch and dispatch spans
- Subscription event spans

OpenTelemetry sampling controls how many traces are exported. Sampling does not make unsafe attributes safe. Treat full documents, variables, extensions, DataLoader keys, exception details, and custom tags as data that must be reviewed before export.

For setup, scope tuning, request details, span attributes, and migration notes, see [OpenTelemetry](./opentelemetry).

# When to Enrich Activities

Use `ActivityEnricher` to add safe, bounded context to Hot Chocolate activities. Good tags classify behavior; avoid tags that copy user input or create unbounded cardinality.

```csharp
using System.Diagnostics;
using HotChocolate.Diagnostics;
using HotChocolate.Execution;

public sealed class GraphQLActivityEnricher(
    InstrumentationOptions options)
    : ActivityEnricher(options)
{
    public override void EnrichCompileOperation(
        RequestContext context,
        Activity activity)
    {
        activity.SetTag("app.graphql.schema_variant", "public");
    }
}

builder.Services.AddSingleton<ActivityEnricher, GraphQLActivityEnricher>();

builder
    .AddGraphQL()
    .AddApplicationService<ActivityEnricher>()
    .AddInstrumentation();
```

Prefer tags such as deployment ring, schema variant, operation category, or tenant tier. Avoid user ids, emails, raw tenant ids, raw variables, and free-form input.

For method-level examples and DI details, see [Activity enrichment](./activity-enrichment).

# Logs, Errors, and Metrics Boundaries

Use standard ASP.NET Core logging for logs. Diagnostic listeners are helpful when a log line needs GraphQL lifecycle context. OpenTelemetry logging can export application logs, but it is separate from `.AddHotChocolateInstrumentation()` tracing.

Use Hot Chocolate error handling for client-safe GraphQL errors, error filters, and `IncludeExceptionDetails`. Telemetry is for server-side investigation and can include error events such as error code, error count, or exception information, depending on options, listeners, enrichers, and exporters. Review both surfaces.

Use custom metrics when you need counters or histograms. Good candidate signals include:

- Request errors and resolver errors by sanitized code or type
- Parse and validation failures
- Operation duration by operation type and safe operation name
- DataLoader batch size, batch failures, cache hits, and cache misses
- Persisted document misses and untrusted document rejections
- Cost analysis results and rejected operations
- Subscription event errors

This is a custom instrumentation pattern, not a built-in Hot Chocolate metrics stream.

# Troubleshooting Missing or Noisy Telemetry

## Missing Spans

- Ensure `HotChocolate.Diagnostics` is installed.
- Ensure `.AddInstrumentation()` is called on the GraphQL builder.
- Ensure `.AddHotChocolateInstrumentation()` is called in `WithTracing`.
- Ensure an exporter is configured.
- Ensure the sampler exports the traces you are testing.
- Ensure the relevant `ActivityScopes` are enabled.
- Ensure the backend is not filtering the service name or `HotChocolate.Diagnostics` source.

## Too Many Spans or High Overhead

- Reduce `ActivityScopes` to the smallest set that answers your question.
- Avoid using `ActivityScopes.All` permanently in production.
- Review resolver field spans for large operations.
- Keep `IncludeDataLoaderKeys` disabled unless keys are safe and needed.
- Check the sampler under representative load.
- Keep diagnostic event handlers fast and nonblocking.

## Sensitive Data Appears in Telemetry

- Check `RequestDetails`.
- Check `IncludeDocument`.
- Check `IncludeDataLoaderKeys`.
- Check custom `ActivityEnricher` tags.
- Check diagnostic listeners and log statements.
- Prefer hashes, ids, codes, and bounded classifications.

## Names Changed After Upgrading

If v15 traces or attributes no longer match your dashboards, review the migration notes for `.AddInstrumentation()`, root span naming, request details, DataLoader attributes, and the v16 `ActivityEnricher` constructor.

# Production Checklist

- Install `HotChocolate.Diagnostics` at the same version as your other Hot Chocolate packages.
- Register `.AddInstrumentation()` and `.AddHotChocolateInstrumentation()`.
- Add host and downstream instrumentation for end-to-end traces.
- Keep default or narrow scopes until an investigation requires more detail.
- Decide which request details are safe before export.
- Cap error events with `MaxErrorEvents`.
- Keep DataLoader keys and full documents out of telemetry unless approved.
- Use low-cardinality custom tags.
- Keep diagnostic event handlers synchronous, fast, and nonblocking.
- Validate sampling and exporter behavior under production-like load.

# Where to Go Next

- [OpenTelemetry](./opentelemetry): setup, scopes, request details, span families, exporter-neutral guidance, and expected output.
- [Diagnostic events](./diagnostic-events): listener types, event contracts, custom logging, custom metrics, synchronous handler guidance, and examples.
- [Activity enrichment](./activity-enrichment): `ActivityEnricher` implementation, safe tag design, DI registration, and v16 migration notes.
- [Execution engine](../execution-engine): where instrumentation fits into the request pipeline.
- [HTTP transport](../server-configuration/http-transport) and [WebSocket transport](../server-configuration/websocket-transport): transport behavior that affects traces.
- [Performance](../performance): interpreting traces for latency and throughput work.
- [Trusted documents](../security/trusted-documents) and [Automatic persisted operations](../performance/automatic-persisted-operations): document ids, hashes, misses, and rejected operations.
- [Cost analysis](../security/cost-analysis): cost analysis spans, limits, and rejected operations.
- [DataLoader](../dataloader): DataLoader behavior behind batch spans and metrics.
- [Interceptors](../server-configuration/interceptors): safe request metadata and correlation.
- [Service injection](../resolvers/service-injection): application services for listeners and enrichers.
- [Error handling](/docs/hotchocolate/v16/_leagcy/guides/error-handling) and [Errors API](/docs/hotchocolate/v16/build/errors): client-safe errors and server-side error handling.
- [Migrate from v15 to v16](/docs/hotchocolate/v16/_leagcy/migrating/migrate-from-15-to-16#addinstrumentation): renamed options, attributes, and enrichment APIs.
