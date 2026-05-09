---
title: "Metrics"
---

Hot Chocolate v16 helps you measure production behavior by emitting OpenTelemetry spans from the `HotChocolate.Diagnostics` activity source. It does not ship native Hot Chocolate `Meter` instruments for GraphQL request latency, resolver latency, or DataLoader batch metrics. In production, you usually combine standard .NET metrics, span-derived metrics from your collector or backend, and a small number of custom metrics when you need a signal that traces do not provide.

Fusion gateway diagnostics are separate from this page. This page covers a Hot Chocolate v16 server.

# Measure GraphQL health without leaking data

Start with the questions you need to answer during an incident. Then choose the lowest-cardinality signal that answers each question.

| Question                                   | Signal                                     | Source                                               | Safe dimensions                                                             | Next page                                                                    |
| ------------------------------------------ | ------------------------------------------ | ---------------------------------------------------- | --------------------------------------------------------------------------- | ---------------------------------------------------------------------------- |
| Is GraphQL traffic healthy?                | Request rate, duration, status code        | ASP.NET Core metrics for `/graphql`                  | route, method, status, service, environment                                 | [HTTP transport](/docs/hotchocolate/v16/server/http-transport)               |
| Which operations spend the latency budget? | Operation duration                         | Span-derived metrics from `HotChocolate.Diagnostics` | `graphql.operation.type`, `graphql.operation.name`, `graphql.document.hash` | [Tracing](/docs/hotchocolate/v16/operations/observability/tracing)           |
| Are clients sending invalid requests?      | Validation failures                        | Validation spans and root request errors             | operation type, operation name, `error.type`                                | [Query limits](/docs/hotchocolate/v16/securing-your-api/request-limits)      |
| Which resolvers are slow?                  | Resolver duration                          | Resolver spans                                       | `graphql.field.schema_coordinate`                                           | [Performance tuning](/docs/hotchocolate/v16/guides/performance)              |
| Are DataLoaders batching well?             | Batch size, batch duration, batch failures | DataLoader spans or custom meters                    | `graphql.dataloader.name`                                                   | [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader)           |
| Are expensive operations being rejected?   | Cost distribution, cost rejections         | Cost spans, request errors, custom listener metrics  | operation name, document hash, error type                                   | [Cost analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis)      |
| Are persisted operations working?          | Storage hits, misses, untrusted rejections | Span events or custom listener metrics               | `graphql.document.id`, `graphql.document.hash`                              | [Persisted operations](/docs/hotchocolate/v16/performance/trusted-documents) |
| Is the host saturated?                     | CPU, GC, thread pool, dependency latency   | Runtime, HTTP client, database instrumentation       | service, environment, dependency name                                       | [Performance tuning](/docs/hotchocolate/v16/guides/performance)              |

Use traces for detail and metrics for trends. A trace tells you why one request was slow. A metric tells you whether slow requests are common enough to page someone.

# Prerequisites

You need a Hot Chocolate v16 server and the diagnostics package:

```bash
dotnet add package HotChocolate.Diagnostics
```

For OpenTelemetry with OTLP export, add the hosting, tracing, metrics, and exporter packages used by the examples on this page:

```bash
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Instrumentation.AspNetCore
dotnet add package OpenTelemetry.Instrumentation.Http
dotnet add package OpenTelemetry.Instrumentation.Runtime
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
```

Your telemetry backend must accept traces to show Hot Chocolate spans. Span-derived metrics, such as operation p95 latency grouped by `graphql.operation.name`, require support in your collector or backend. Prometheus can be part of this setup, but it exports .NET or custom metrics unless you add span-to-metric processing yourself.

# Enable Hot Chocolate traces and .NET metrics

Configure GraphQL instrumentation and OpenTelemetry together. Keep tracing and metrics separate so you know which pipeline produces each signal.

```csharp
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddInstrumentation();

builder.Services
    .AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddHotChocolateInstrumentation()
            .AddOtlpExporter();
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddOtlpExporter();
    });

var app = builder.Build();

app.MapGraphQL();

app.Run();
```

Expected result:

- Your trace backend shows spans from the `HotChocolate.Diagnostics` activity source.
- Your metrics backend shows ASP.NET Core, HTTP client, and runtime metrics.
- GraphQL-specific metric names appear only if your backend derives metrics from spans or you add custom `Meter` instruments.

If you use service defaults, you can also export OTLP only when `OTEL_EXPORTER_OTLP_ENDPOINT` is configured:

```csharp
if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
{
    builder.Services.AddOpenTelemetry().UseOtlpExporter();
}
```

# Choose activity scopes for production

`AddInstrumentation()` creates spans for selected Hot Chocolate diagnostic scopes. More scopes give you more detail, but they also create more spans and increase storage cost.

| Scope set                | Includes                                                                                                                                             | When to use                                                                              |
| ------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------- |
| `ActivityScopes.Default` | HTTP request execution, HTTP request parsing, validation, operation compilation, resolver field values, HTTP response formatting, DataLoader batches | Start here for production visibility.                                                    |
| `ActivityScopes.All`     | Everything in `Default`, plus execute request, parse document, operation cost analysis, variable coercion, and execute operation                     | Use during an investigation or when your backend cost model can handle the extra volume. |

Make the default explicit when you want configuration to document the production choice:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddInstrumentation(options =>
    {
        options.Scopes = ActivityScopes.Default;
    });
```

Temporarily broaden scopes when you need cost tags or deeper execution timing:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddInstrumentation(options =>
    {
        options.Scopes = ActivityScopes.All;
    });
```

Resolver and DataLoader scopes can create many spans for large operations. If telemetry overhead becomes visible, reduce scopes, sample traces, or move high-volume signals to custom metrics with bounded labels.

# Measure request latency and throughput

Build your first health panels from ASP.NET Core metrics, then add Hot Chocolate span-derived metrics for GraphQL-specific breakdowns.

| Panel                           | Source               | Dimensions                                                                  | What it answers                           |
| ------------------------------- | -------------------- | --------------------------------------------------------------------------- | ----------------------------------------- |
| `/graphql` request rate         | ASP.NET Core metrics | route, method, status                                                       | Did traffic change?                       |
| `/graphql` p50/p95/p99 duration | ASP.NET Core metrics | route, status                                                               | Is all GraphQL traffic slow?              |
| Status-code distribution        | ASP.NET Core metrics | status                                                                      | Are transport errors increasing?          |
| Operation duration              | Span-derived metrics | `graphql.operation.type`, `graphql.operation.name`, `graphql.document.hash` | Is one operation class slow?              |
| Slowest named operations        | Span-derived metrics | operation name, document hash                                               | Which client workflow should you inspect? |

Hot Chocolate keeps root span names low-cardinality. In v16, the root GraphQL span name includes the operation type, such as `query`, `mutation`, or `subscription`; the operation name is an attribute named `graphql.operation.name`. For anonymous operations, group by `graphql.operation.type` and `graphql.document.hash` instead of the raw document body.

Subscriptions need a separate view. A WebSocket connection can stay open for a long time, while each subscription event has its own execution behavior. Track connection health with transport metrics and event execution with Hot Chocolate spans.

# Count errors and validation failures

Do not treat all GraphQL errors as the same operational problem.

| Error signal                                | Source                         | Typical next action                                                |
| ------------------------------------------- | ------------------------------ | ------------------------------------------------------------------ |
| HTTP 4xx or 5xx on `/graphql`               | ASP.NET Core metrics           | Check transport, authentication, request size, or server failures. |
| Validation span status is `Error`           | Hot Chocolate validation spans | Fix clients, schema changes, or query validation rules.            |
| Resolver span status is `Error`             | Resolver spans                 | Inspect resolver code, dependencies, DataLoaders, and data shape.  |
| Root request span has `graphql.error.count` | Root Hot Chocolate span        | Split by operation and error type, then inspect traces or logs.    |

Root request spans can include `graphql.error` events. `MaxErrorEvents` defaults to `10`, and `0` suppresses those events. The total error count remains available as the `graphql.error.count` tag.

Use safe labels for error metrics:

| Use as a label           | Avoid as a label                  |
| ------------------------ | --------------------------------- |
| `graphql.operation.type` | `graphql.error.message`           |
| `graphql.operation.name` | Exception messages                |
| `error.type`             | Variables                         |
| status code              | Raw document body                 |
| service and environment  | User IDs, tenant IDs, request IDs |

Error messages and exception text belong in traces or structured logs. Link metrics to traces or logs when your backend supports exemplars or correlation IDs. See [logging](/docs/hotchocolate/v16/operations/observability/logging) for log-specific guidance.

# Find slow resolvers without exploding cardinality

Resolver spans use `graphql.processing.type=resolve` and include field attributes:

| Attribute                         | Production use                                                            |
| --------------------------------- | ------------------------------------------------------------------------- |
| `graphql.field.schema_coordinate` | Best dimension for resolver metrics, for example `Product.name`.          |
| `graphql.field.name`              | Useful when combined with parent type.                                    |
| `graphql.field.parent_type`       | Useful when your backend cannot use schema coordinate.                    |
| `graphql.field.path`              | Avoid as a metric label because list indexes can create high cardinality. |
| `graphql.field.alias`             | Avoid as a metric label because clients control aliases.                  |

Build panels that aggregate by `graphql.field.schema_coordinate`:

| Panel                        | What to do next                                               |
| ---------------------------- | ------------------------------------------------------------- |
| Top 10 resolver p95 duration | Check database queries, REST calls, projections, and caching. |
| Resolver error count         | Inspect failing fields and their dependencies.                |
| Resolver duration heatmap    | Look for tail latency and outliers.                           |

If one field is slow because it loads related data repeatedly, move the investigation to [DataLoader batching](/docs/hotchocolate/v16/resolvers-and-data/dataloader). If the field over-fetches from a database, review [projections](/docs/hotchocolate/v16/resolvers-and-data/projections).

# Monitor DataLoader batching

DataLoader spans are enabled by `ActivityScopes.Default`. Batch spans include the DataLoader name and batch size:

| Attribute or event                                                   | Meaning                                        |
| -------------------------------------------------------------------- | ---------------------------------------------- |
| `graphql.processing.type=dataloader_batch`                           | The span represents one DataLoader batch.      |
| `graphql.dataloader.name`                                            | The DataLoader class name.                     |
| `graphql.dataloader.batch.size`                                      | Number of keys in the batch.                   |
| `BatchEvaluated` event with `graphql.dataloader.batches.open`        | Number of open batches at dispatch evaluation. |
| `BatchDispatched` event with `graphql.dataloader.batches.dispatched` | Number of batches dispatched.                  |

Keep `IncludeDataLoaderKeys` disabled in production unless keys are bounded and non-sensitive:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddInstrumentation(options =>
    {
        options.IncludeDataLoaderKeys = false;
    });
```

Useful panels:

| Panel                                               | What it tells you                                                       |
| --------------------------------------------------- | ----------------------------------------------------------------------- |
| Batch size p50 and p95 by `graphql.dataloader.name` | Tiny batches can indicate N+1 behavior or resolver ordering problems.   |
| Batch duration by DataLoader name                   | Slow batches point to data source latency or inefficient batch queries. |
| Failed batches by DataLoader name                   | Batch functions are throwing or dependencies are failing.               |
| Open vs dispatched batch counts                     | Dispatch behavior changed, if your backend exposes span events.         |

# Track operation cost, depth, and rejected requests

Cost analysis protects the server before execution. When you enable cost analysis instrumentation, spans can carry `graphql.operation.fieldCost` and `graphql.operation.typeCost`.

Cost spans are not part of the default activity scopes. Enable them with `ActivityScopes.AnalyzeComplexity` or `ActivityScopes.All` when you need cost visibility:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddInstrumentation(options =>
    {
        options.Scopes = ActivityScopes.Default | ActivityScopes.AnalyzeComplexity;
    });
```

Track these panels when you tune limits:

| Panel                   | Source                                    | Action                                                                                                        |
| ----------------------- | ----------------------------------------- | ------------------------------------------------------------------------------------------------------------- |
| Field cost distribution | Cost span attributes                      | Adjust client operation shape, paging, or field weights.                                                      |
| Type cost distribution  | Cost span attributes                      | Tune type cost limits or pagination defaults.                                                                 |
| Cost-limit rejections   | Request errors or custom listener metrics | Review [cost analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis) and expected client workloads. |
| Latency vs cost         | Backend-specific trace links or exemplars | Decide whether high cost predicts high latency in your schema.                                                |

If you use depth limits or custom validation rules, measure their failures as validation failures or add custom diagnostic listener metrics. Do not assume a built-in depth metric exists.

# Measure persisted operation hits, misses, and cache behavior

Persisted operations reduce request size and let known operations avoid dynamic parsing work. Hot Chocolate v16 emits span events on the request span for persisted-operation and cache behavior, including:

- `RetrievedDocumentFromCache`
- `RetrievedDocumentFromStorage`
- `DocumentNotFoundInStorage`
- `UntrustedDocumentRejected`
- `AddedDocumentToCache`
- `AddedOperationToCache`

These are span events, not built-in metric instruments. Some backends let you derive metrics from span events. Others require a custom `ExecutionDiagnosticEventListener`.

Use document identifiers as dimensions:

| Use                     | Avoid                   |
| ----------------------- | ----------------------- |
| `graphql.document.id`   | `graphql.document.body` |
| `graphql.document.hash` | variables               |
| operation name          | request extensions      |

Useful panels include persisted document storage hits, document-not-found misses, untrusted document rejections, cache-add rate, and latency split by persisted versus dynamic requests when your backend exposes the needed span data. See [persisted operations](/docs/hotchocolate/v16/performance/trusted-documents) and [automatic persisted operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations) for setup details.

# Design labels that stay safe and affordable

Metric labels must be bounded, stable, and safe to share with your observability backend.

| Prefer                                           | Why                                                    |
| ------------------------------------------------ | ------------------------------------------------------ |
| service name and environment                     | Required for routing and ownership.                    |
| stable schema name                               | Useful when a process hosts multiple schemas.          |
| `graphql.operation.type`                         | Bounded to query, mutation, subscription.              |
| `graphql.operation.name`                         | Useful when clients name operations consistently.      |
| `graphql.document.hash` or `graphql.document.id` | Stable grouping for anonymous or persisted operations. |
| `error.type`                                     | Lets you group failures without exposing messages.     |
| `graphql.field.schema_coordinate`                | Stable resolver identity.                              |
| `graphql.dataloader.name`                        | Stable batching identity.                              |

| Avoid                             | Risk                                                   |
| --------------------------------- | ------------------------------------------------------ |
| document body                     | Sensitive data and high cardinality.                   |
| variables and extensions          | Often contain PII, tokens, or unbounded IDs.           |
| DataLoader keys                   | Often contain database IDs or tenant data.             |
| field path                        | List indexes and nested paths create high cardinality. |
| aliases                           | Client-controlled and unbounded.                       |
| error messages and exception text | Sensitive and unbounded.                               |
| raw user ID or tenant ID          | High-cardinality and often sensitive.                  |
| request ID                        | One time series per request.                           |

`RequestDetails.Document`, `RequestDetails.Variables`, `IncludeDocument`, and `IncludeDataLoaderKeys` are privacy-sensitive. Keep them disabled for production metrics unless you have a documented retention, access, and redaction policy.

This custom metric uses bounded labels only:

```csharp
s_batchSize.Record(
    keys.Count,
    new KeyValuePair<string, object?>("graphql.dataloader.name", dataLoader.GetType().Name));
```

For multi-tenant systems, prefer a bounded tenant tier, plan, region, or shard label over a raw tenant ID.

# Build a dashboard that leads to action

A useful dashboard tells the on-call engineer what to inspect next.

| Row                | Panels                                                                        | Follow-up                                                            |
| ------------------ | ----------------------------------------------------------------------------- | -------------------------------------------------------------------- |
| Health             | Request rate, p95 and p99 latency, error rate, CPU, GC, thread pool           | Decide whether the incident affects all GraphQL traffic or the host. |
| Client correctness | Validation failures, persisted document misses, untrusted document rejections | Identify bad deploys, outdated clients, or registry issues.          |
| Operation hotspots | Slow operations, high-cost operations, failed operations                      | Open traces for affected operation names or document hashes.         |
| Resolver hotspots  | Slow fields, resolver errors                                                  | Inspect DataLoader, projection, database, REST, or caching behavior. |
| DataLoader health  | Batch size, batch duration, batch failures, dispatch counts                   | Fix tiny batches, oversized batches, or failing batch functions.     |
| Dependencies       | Database latency, HTTP client latency, runtime saturation                     | Escalate to the dependency owner or tune host capacity.              |

Prefer panel descriptions and dimensions over copied query syntax. Metric names differ across OpenTelemetry SDK versions, exporters, collectors, and backends. If you use PromQL, verify the exported metric names in your Prometheus target before committing dashboards.

# Set SLOs and alerts

Start with API-level SLOs. Resolver and DataLoader metrics are excellent investigation signals, but they usually should not page by themselves unless they affect request success or latency.

Example alert descriptions:

| Alert                          | Trigger shape                                                                        | Route to                         |
| ------------------------------ | ------------------------------------------------------------------------------------ | -------------------------------- |
| GraphQL error budget burn      | Successful `/graphql` request ratio drops below your SLO over short and long windows | API on-call                      |
| Latency budget burn            | p95 or p99 duration for critical named operations exceeds target                     | API on-call                      |
| Validation failure spike       | Validation errors rise above baseline for sustained time                             | Client or platform owner         |
| Persisted-operation miss spike | `DocumentNotFoundInStorage` or untrusted rejections rise after a deploy              | Client registry or release owner |
| Cost-rejection spike           | Cost-limit rejections rise above expected background level                           | API and client owners            |
| DataLoader failure spike       | Batch failures increase and request SLO is affected                                  | Owning service team              |
| Runtime saturation             | CPU, GC pause, or thread pool queueing correlates with latency                       | Platform or service owner        |

Alert on burn rate or sustained symptoms. A single slow resolver span should lead to an investigation dashboard, not a page.

# Add custom metrics from diagnostic events

Use custom metrics when your backend cannot derive the metric you need from spans or span events. Diagnostic event listeners run synchronously during GraphQL execution, so keep them lightweight.

The following listener records DataLoader batch metrics with a custom .NET `Meter`:

```csharp
using System.Diagnostics.Metrics;
using GreenDonut;

namespace Demo.Diagnostics;

public sealed class GraphQLDataLoaderMetrics : DataLoaderDiagnosticEventListener
{
    public const string MeterName = "Demo.GraphQL";

    private static readonly Meter s_meter = new(MeterName, "1.0.0");

    private static readonly Counter<long> s_batchesExecuted =
        s_meter.CreateCounter<long>("graphql.dataloader.batches.executed");

    private static readonly Counter<long> s_batchesFailed =
        s_meter.CreateCounter<long>("graphql.dataloader.batches.failed");

    private static readonly Histogram<int> s_batchSize =
        s_meter.CreateHistogram<int>("graphql.dataloader.batch.size");

    public override IDisposable ExecuteBatch<TKey>(
        IDataLoader dataLoader,
        IReadOnlyList<TKey> keys)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("graphql.dataloader.name", dataLoader.GetType().Name)
        };

        s_batchesExecuted.Add(1, tags);
        s_batchSize.Record(keys.Count, tags);

        return EmptyScope;
    }

    public override void BatchError<TKey>(IReadOnlyList<TKey> keys, Exception error)
    {
        s_batchesFailed.Add(1);
    }
}
```

Register the listener with Hot Chocolate and register the meter with OpenTelemetry metrics:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddDiagnosticEventListener<GraphQLDataLoaderMetrics>();

builder.Services
    .AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter(GraphQLDataLoaderMetrics.MeterName)
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation();
    });
```

Expected result: your metrics backend receives `graphql.dataloader.batches.executed`, `graphql.dataloader.batches.failed`, and `graphql.dataloader.batch.size` from the `Demo.GraphQL` meter.

When a custom listener needs application services, register those services with the schema service provider as described in the [v15 to v16 migration guide](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16#clearer-separation-between-schema-and-application-services).

# Troubleshoot missing or noisy metrics

| Symptom                                             | Check                                                                                                                                                                                       |
| --------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| No Hot Chocolate spans                              | Install `HotChocolate.Diagnostics`, call `.AddInstrumentation()`, call `.AddHotChocolateInstrumentation()` in `.WithTracing`, verify OTLP endpoint, sampling, and backend resource filters. |
| `.WithMetrics` has no GraphQL metrics               | This is expected unless you derive metrics from spans or add custom meters. Hot Chocolate v16 built-ins are trace-based.                                                                    |
| Resolver spans are missing                          | Verify `ActivityScopes.ResolveFieldValue` is enabled and traces are sampled. It is included in `ActivityScopes.Default`.                                                                    |
| DataLoader spans are missing                        | Verify `ActivityScopes.DataLoaderBatch` is enabled and your request uses DataLoaders. It is included in `ActivityScopes.Default`.                                                           |
| Cost tags are missing                               | Enable `ActivityScopes.AnalyzeComplexity` or `ActivityScopes.All`, and verify cost analysis runs for the operation.                                                                         |
| Persisted operation hit or miss metrics are missing | Check whether your backend exposes span events as metrics. If it does not, add a custom diagnostic listener.                                                                                |
| Cardinality explodes                                | Remove document body, variables, extensions, DataLoader keys, field path, aliases, error messages, raw user IDs, raw tenant IDs, and request IDs from labels.                               |
| Telemetry adds latency or cost                      | Reduce scopes, sample traces, lower `MaxErrorEvents`, avoid synchronous exporter work, and keep custom listener work minimal.                                                               |

Use safe instrumentation options for production:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddInstrumentation(options =>
    {
        options.RequestDetails = RequestDetails.Default;
        options.IncludeDocument = false;
        options.IncludeDataLoaderKeys = false;
        options.MaxErrorEvents = 10;
    });
```

During a short investigation, you can temporarily increase scope detail:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddInstrumentation(options =>
    {
        options.Scopes = ActivityScopes.All;
    });
```

Return to the smaller scope after the investigation to control overhead and storage cost.

# Next steps

- [Tracing](/docs/hotchocolate/v16/operations/observability/tracing): inspect spans, enrich activities, and connect traces to metrics.
- [Logging](/docs/hotchocolate/v16/operations/observability/logging): record structured errors, correlation IDs, and request context that should not become metric labels.
- [Instrumentation](/docs/hotchocolate/v16/server/instrumentation): review diagnostic event listeners and OpenTelemetry APIs.
- [Performance tuning](/docs/hotchocolate/v16/guides/performance): respond to latency, cache, DataLoader, cost, and runtime bottlenecks.
- [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader): fix batching and N+1 issues.
- [Cost analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis): configure cost limits and report mode.
- [Persisted operations](/docs/hotchocolate/v16/performance/trusted-documents): configure trusted documents and persisted-operation storage.
- [Automatic persisted operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations): understand runtime persisted-operation negotiation.
- [HTTP transport](/docs/hotchocolate/v16/server/http-transport): understand transport, batching, status codes, and streaming.
- [Migrate from v15 to v16](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16): update instrumentation options and span attributes.
