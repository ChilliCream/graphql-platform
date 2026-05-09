---
title: "Metrics"
---

Hot Chocolate v16 enables you to monitor your GraphQL server by emitting OpenTelemetry spans from the `HotChocolate.Diagnostics` activity source. While it does not provide built-in `Meter` instruments for GraphQL request latency, resolver latency, or DataLoader batch metrics, you can combine standard .NET metrics, span-derived metrics from your observability backend, and custom metrics to get the signals you need.

This page focuses on Hot Chocolate v16 server metrics. Fusion gateway diagnostics are covered elsewhere.

# Choose the right signals for GraphQL health

When monitoring your GraphQL server, start by identifying the questions you need to answer during incidents. For each question, select the lowest-cardinality signal that provides the answer without leaking sensitive data.

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

Use traces to investigate individual requests and metrics to spot trends. Traces explain why a request was slow; metrics show if slow requests are frequent enough to require action.

# Prerequisites

To get started, set up a Hot Chocolate v16 server and add the diagnostics package:

```bash
dotnet add package HotChocolate.Diagnostics
```

For OpenTelemetry with OTLP export, add these packages:

```bash
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Instrumentation.AspNetCore
dotnet add package OpenTelemetry.Instrumentation.Http
dotnet add package OpenTelemetry.Instrumentation.Runtime
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
```

Your telemetry backend must support traces to display Hot Chocolate spans. If you want span-derived metrics (such as operation p95 latency grouped by `graphql.operation.name`), your collector or backend must support this. Prometheus can be used, but it only exports .NET or custom metrics unless you add span-to-metric processing.

# Enable traces and metrics

Configure GraphQL instrumentation and OpenTelemetry together. Keep tracing and metrics pipelines separate so you can track which system produces each signal.

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

After setup:

- Your trace backend displays spans from `HotChocolate.Diagnostics`.
- Your metrics backend shows ASP.NET Core, HTTP client, and runtime metrics.
- GraphQL-specific metrics appear only if your backend derives them from spans or you add custom `Meter` instruments.

If you use service defaults, you can export OTLP only when `OTEL_EXPORTER_OTLP_ENDPOINT` is set:

```csharp
if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
{
    builder.Services.AddOpenTelemetry().UseOtlpExporter();
}
```

# Choose activity scopes for production

`AddInstrumentation()` creates spans for selected Hot Chocolate diagnostic scopes. More scopes provide more detail but also increase span volume and storage cost.

| Scope set                | Includes                                                                                                                                             | When to use                                                          |
| ------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------- |
| `ActivityScopes.Default` | HTTP request execution, HTTP request parsing, validation, operation compilation, resolver field values, HTTP response formatting, DataLoader batches | Use for production visibility.                                       |
| `ActivityScopes.All`     | Everything in `Default`, plus execute request, parse document, operation cost analysis, variable coercion, and execute operation                     | Use for investigations or when your backend can handle extra volume. |

To make your production choice explicit:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddInstrumentation(options =>
    {
        options.Scopes = ActivityScopes.Default;
    });
```

Broaden scopes temporarily when you need more detail:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddInstrumentation(options =>
    {
        options.Scopes = ActivityScopes.All;
    });
```

Resolver and DataLoader scopes can generate many spans for large operations. If you notice telemetry overhead, reduce scopes, sample traces, or move high-volume signals to custom metrics with bounded labels.

# Measure request latency and throughput

Start with ASP.NET Core metrics for basic health panels, then add Hot Chocolate span-derived metrics for GraphQL-specific insights.

| Panel                           | Source               | Dimensions                                                                  | What it answers                           |
| ------------------------------- | -------------------- | --------------------------------------------------------------------------- | ----------------------------------------- |
| `/graphql` request rate         | ASP.NET Core metrics | route, method, status                                                       | Did traffic change?                       |
| `/graphql` p50/p95/p99 duration | ASP.NET Core metrics | route, status                                                               | Is all GraphQL traffic slow?              |
| Status-code distribution        | ASP.NET Core metrics | status                                                                      | Are transport errors increasing?          |
| Operation duration              | Span-derived metrics | `graphql.operation.type`, `graphql.operation.name`, `graphql.document.hash` | Is one operation class slow?              |
| Slowest named operations        | Span-derived metrics | operation name, document hash                                               | Which client workflow should you inspect? |

Hot Chocolate keeps root span names low-cardinality. In v16, the root GraphQL span name includes the operation type (such as `query`, `mutation`, or `subscription`). The operation name is stored in the `graphql.operation.name` attribute. For anonymous operations, group by `graphql.operation.type` and `graphql.document.hash` instead of the raw document body.

Subscriptions require a separate view. WebSocket connections can stay open for a long time, and each subscription event has its own execution. Track connection health with transport metrics and event execution with Hot Chocolate spans.

# Count errors and validation failures

Not all GraphQL errors indicate the same operational problem. Use the right signals to distinguish between them:

| Error signal                                | Source                         | Typical next action                                                |
| ------------------------------------------- | ------------------------------ | ------------------------------------------------------------------ |
| HTTP 4xx or 5xx on `/graphql`               | ASP.NET Core metrics           | Check transport, authentication, request size, or server failures. |
| Validation span status is `Error`           | Hot Chocolate validation spans | Fix clients, schema changes, or query validation rules.            |
| Resolver span status is `Error`             | Resolver spans                 | Inspect resolver code, dependencies, DataLoaders, and data shape.  |
| Root request span has `graphql.error.count` | Root Hot Chocolate span        | Split by operation and error type, then inspect traces or logs.    |

Root request spans can include `graphql.error` events. By default, `MaxErrorEvents` is `10`. Setting it to `0` suppresses these events, but the total error count is always available as the `graphql.error.count` tag.

Use only safe labels for error metrics:

| Use as a label           | Avoid as a label                  |
| ------------------------ | --------------------------------- |
| `graphql.operation.type` | `graphql.error.message`           |
| `graphql.operation.name` | Exception messages                |
| `error.type`             | Variables                         |
| status code              | Raw document body                 |
| service and environment  | User IDs, tenant IDs, request IDs |

Error messages and exception text belong in traces or structured logs, not in metric labels. Link metrics to traces or logs using exemplars or correlation IDs if your backend supports them. For more on logging, see the [logging guide](/docs/hotchocolate/v16/operations/observability/logging).

# Find slow resolvers without high cardinality

Resolver spans use `graphql.processing.type=resolve` and include several field attributes:

| Attribute                         | Production use                                                   |
| --------------------------------- | ---------------------------------------------------------------- |
| `graphql.field.schema_coordinate` | Best dimension for resolver metrics, for example `Product.name`. |
| `graphql.field.name`              | Useful when combined with parent type.                           |
| `graphql.field.parent_type`       | Useful when your backend cannot use schema coordinate.           |
| `graphql.field.path`              | Avoid as a metric label; list indexes create high cardinality.   |
| `graphql.field.alias`             | Avoid as a metric label; clients control aliases.                |

Build panels that aggregate by `graphql.field.schema_coordinate`:

| Panel                        | What to do next                                               |
| ---------------------------- | ------------------------------------------------------------- |
| Top 10 resolver p95 duration | Check database queries, REST calls, projections, and caching. |
| Resolver error count         | Inspect failing fields and their dependencies.                |
| Resolver duration heatmap    | Look for tail latency and outliers.                           |

If a field is slow due to repeated data loading, investigate [DataLoader batching](/docs/hotchocolate/v16/resolvers-and-data/dataloader). If it over-fetches from a database, review [projections](/docs/hotchocolate/v16/resolvers-and-data/projections).

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

Useful panels for DataLoader health:

| Panel                                               | What it tells you                                                       |
| --------------------------------------------------- | ----------------------------------------------------------------------- |
| Batch size p50 and p95 by `graphql.dataloader.name` | Tiny batches can indicate N+1 behavior or resolver ordering problems.   |
| Batch duration by DataLoader name                   | Slow batches point to data source latency or inefficient batch queries. |
| Failed batches by DataLoader name                   | Batch functions are throwing or dependencies are failing.               |
| Open vs dispatched batch counts                     | Dispatch behavior changed, if your backend exposes span events.         |

# Track operation cost, depth, and rejections

Cost analysis protects your server before execution. When you enable cost analysis instrumentation, spans can include `graphql.operation.fieldCost` and `graphql.operation.typeCost`.

Cost spans are not included in the default activity scopes. Enable them with `ActivityScopes.AnalyzeComplexity` or `ActivityScopes.All` when you need cost visibility:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddInstrumentation(options =>
    {
        options.Scopes = ActivityScopes.Default | ActivityScopes.AnalyzeComplexity;
    });
```

Track these panels when tuning limits:

| Panel                   | Source                                    | Action                                                                                                        |
| ----------------------- | ----------------------------------------- | ------------------------------------------------------------------------------------------------------------- |
| Field cost distribution | Cost span attributes                      | Adjust client operation shape, paging, or field weights.                                                      |
| Type cost distribution  | Cost span attributes                      | Tune type cost limits or pagination defaults.                                                                 |
| Cost-limit rejections   | Request errors or custom listener metrics | Review [cost analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis) and expected client workloads. |
| Latency vs cost         | Backend-specific trace links or exemplars | Decide whether high cost predicts high latency in your schema.                                                |

If you use depth limits or custom validation rules, measure their failures as validation failures or add custom diagnostic listener metrics. There is no built-in depth metric.

# Measure persisted operation hits, misses, and cache behavior

Persisted operations reduce request size and let known operations skip dynamic parsing. Hot Chocolate v16 emits span events on the request span for persisted-operation and cache behavior, including:

- `RetrievedDocumentFromCache`
- `RetrievedDocumentFromStorage`
- `DocumentNotFoundInStorage`
- `UntrustedDocumentRejected`
- `AddedDocumentToCache`
- `AddedOperationToCache`

These are span events, not built-in metric instruments. Some backends can derive metrics from span events; others require a custom `ExecutionDiagnosticEventListener`.

Use document identifiers as dimensions:

| Use                     | Avoid                   |
| ----------------------- | ----------------------- |
| `graphql.document.id`   | `graphql.document.body` |
| `graphql.document.hash` | variables               |
| operation name          | request extensions      |

Panels to consider: persisted document storage hits, document-not-found misses, untrusted document rejections, cache-add rate, and latency split by persisted versus dynamic requests. See [persisted operations](/docs/hotchocolate/v16/performance/trusted-documents) and [automatic persisted operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations) for setup details.

# Design safe and affordable metric labels

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

Options like `RequestDetails.Document`, `RequestDetails.Variables`, `IncludeDocument`, and `IncludeDataLoaderKeys` are privacy-sensitive. Leave them disabled for production metrics unless you have a documented retention, access, and redaction policy.

Example of a custom metric with bounded labels:

```csharp
s_batchSize.Record(
    keys.Count,
    new KeyValuePair<string, object?>("graphql.dataloader.name", dataLoader.GetType().Name));
```

For multi-tenant systems, use a bounded tenant tier, plan, region, or shard label instead of a raw tenant ID.

# Build actionable dashboards

A good dashboard guides the on-call engineer to the next step. Organize panels by investigation flow:

| Row                | Panels                                                                        | Follow-up                                                            |
| ------------------ | ----------------------------------------------------------------------------- | -------------------------------------------------------------------- |
| Health             | Request rate, p95 and p99 latency, error rate, CPU, GC, thread pool           | Decide whether the incident affects all GraphQL traffic or the host. |
| Client correctness | Validation failures, persisted document misses, untrusted document rejections | Identify bad deploys, outdated clients, or registry issues.          |
| Operation hotspots | Slow operations, high-cost operations, failed operations                      | Open traces for affected operation names or document hashes.         |
| Resolver hotspots  | Slow fields, resolver errors                                                  | Inspect DataLoader, projection, database, REST, or caching behavior. |
| DataLoader health  | Batch size, batch duration, batch failures, dispatch counts                   | Fix tiny batches, oversized batches, or failing batch functions.     |
| Dependencies       | Database latency, HTTP client latency, runtime saturation                     | Escalate to the dependency owner or tune host capacity.              |

Prefer panel descriptions and dimensions over copied query syntax. Metric names can differ across OpenTelemetry SDK versions, exporters, collectors, and backends. If you use PromQL, verify metric names in your Prometheus target before finalizing dashboards.

# Set SLOs and alerts

Start with API-level SLOs. Resolver and DataLoader metrics are valuable for investigations, but usually should not trigger pages unless they affect request success or latency.

Example alert types:

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

If your backend cannot derive a needed metric from spans or span events, use custom metrics. Diagnostic event listeners run synchronously during GraphQL execution, so keep them lightweight.

Here is a listener that records DataLoader batch metrics with a custom .NET `Meter`:

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

Register the listener with Hot Chocolate and the meter with OpenTelemetry metrics:

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

Your metrics backend will now receive `graphql.dataloader.batches.executed`, `graphql.dataloader.batches.failed`, and `graphql.dataloader.batch.size` from the `Demo.GraphQL` meter.

If your custom listener needs application services, register them with the schema service provider as described in the [v15 to v16 migration guide](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16#clearer-separation-between-schema-and-application-services).

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

For production, use safe instrumentation options:

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

During investigations, you can temporarily increase scope detail:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddInstrumentation(options =>
    {
        options.Scopes = ActivityScopes.All;
    });
```

After the investigation, return to a smaller scope to control overhead and storage cost.

# Next steps

- [Tracing](/docs/hotchocolate/v16/operations/observability/tracing): Inspect spans, enrich activities, and connect traces to metrics.
- [Logging](/docs/hotchocolate/v16/operations/observability/logging): Record structured errors, correlation IDs, and request context that should not become metric labels.
- [Instrumentation](/docs/hotchocolate/v16/server/instrumentation): Review diagnostic event listeners and OpenTelemetry APIs.
- [Performance tuning](/docs/hotchocolate/v16/guides/performance): Respond to latency, cache, DataLoader, cost, and runtime bottlenecks.
- [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader): Fix batching and N+1 issues.
- [Cost analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis): Configure cost limits and report mode.
- [Persisted operations](/docs/hotchocolate/v16/performance/trusted-documents): Configure trusted documents and persisted-operation storage.
- [Automatic persisted operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations): Understand runtime persisted-operation negotiation.
- [HTTP transport](/docs/hotchocolate/v16/server/http-transport): Understand transport, batching, status codes, and streaming.
- [Migrate from v15 to v16](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16): Update instrumentation options and span attributes.
