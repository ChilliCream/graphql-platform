---
title: OpenTelemetry tracing
---

Hot Chocolate emits GraphQL trace spans through `System.Diagnostics.ActivitySource`. Use those spans to answer production questions such as: Which operation is slow? Which resolver or DataLoader batch dominates latency? Did a request fail in HTTP parsing, validation, execution, or response formatting?

This page covers Hot Chocolate v16 server tracing. It does not cover Fusion diagnostics. Fusion uses separate diagnostics options and should be documented on Fusion-specific pages. Metrics are also separate: `AddHotChocolateInstrumentation()` registers the Hot Chocolate `ActivitySource` for traces, not a Hot Chocolate `Meter`.

# Prerequisites

You need:

- A Hot Chocolate v16 server with `app.MapGraphQL()`.
- The `HotChocolate.Diagnostics` package.
- OpenTelemetry packages for hosting, ASP.NET Core, outgoing HTTP, and your exporter.
- A local dashboard, collector, or vendor backend that accepts OTLP or another OpenTelemetry exporter.

Install the packages used in the examples:

```bash
dotnet add package HotChocolate.Diagnostics
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Instrumentation.AspNetCore
dotnet add package OpenTelemetry.Instrumentation.Http
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
```

Expected result: packages restore and the application compiles.

# Configure Hot Chocolate tracing

Add `.AddInstrumentation()` to your GraphQL builder and add `.AddHotChocolateInstrumentation()` to the OpenTelemetry tracing pipeline.

```csharp
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddInstrumentation();

builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resource =>
        resource.AddService(serviceName: builder.Environment.ApplicationName))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddHotChocolateInstrumentation()
            .AddOtlpExporter();
    });

var app = builder.Build();

app.MapGraphQL();

app.Run();

public sealed class Query
{
    public string[] GetBooks()
    {
        return ["The Fellowship of the Ring", "The Two Towers"];
    }
}
```

`AddInstrumentation()` registers Hot Chocolate diagnostic listeners that create `Activity` spans. `AddHotChocolateInstrumentation()` registers the `HotChocolate.Diagnostics` `ActivitySource` with OpenTelemetry. ASP.NET Core instrumentation creates the incoming HTTP server span, and Hot Chocolate spans are correlated under it.

## Configure tracing in Aspire service defaults

If your application uses a shared ServiceDefaults project, keep the exporter and common ASP.NET Core filters there, then add Hot Chocolate to the tracing builder.

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceDefaults
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.Filter = context =>
                            !context.Request.Path.StartsWithSegments(HealthEndpointPath)
                            && !context.Request.Path.StartsWithSegments(AlivenessEndpointPath);
                    })
                    .AddHttpClientInstrumentation()
                    .AddHotChocolateInstrumentation();
            });

        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        return builder;
    }
}
```

Expected result: when `OTEL_EXPORTER_OTLP_ENDPOINT` is set, traces are exported through OTLP. Health checks do not clutter traces.

# Verify the first trace

Run the application and send a named operation:

```graphql
query GetBooks {
  books
}
```

Open your tracing backend and search for the service name or `graphql.operation.name = GetBooks`.

A successful trace usually contains spans similar to this tree:

```text
HTTP POST /graphql                 # ASP.NET Core span
└─ query                            # Hot Chocolate effective request span
   ├─ Parse HTTP Request
   ├─ GraphQL Document Validation
   ├─ GraphQL Operation Planning
   ├─ Query.books                   # resolver span
   └─ Format HTTP Response
```

If the operation uses DataLoaders, you may also see `GraphQL DataLoader Dispatch` and `GraphQL DataLoader Batch <Name>` spans.

Expected result: your backend shows spans from the `HotChocolate.Diagnostics` source, not only ASP.NET Core spans.

# Understand what spans you get

Hot Chocolate uses the `HotChocolate.Diagnostics` `ActivitySource`. `AddHotChocolateInstrumentation()` is equivalent to registering that source with `TracerProviderBuilder.AddSource("HotChocolate.Diagnostics")`.

The root or effective request span uses a low-cardinality display name: `query`, `mutation`, or `subscription`. The operation name is emitted as `graphql.operation.name`. Build dashboards from attributes, not root span display names.

By default, `ActivityScopes.Default` includes:

- `ExecuteHttpRequest`
- `ParseHttpRequest`
- `ValidateDocument`
- `CompileOperation`
- `ResolveFieldValue`
- `FormatHttpResponse`
- `DataLoaderBatch`

Additional scopes add request execution, document parsing, complexity analysis, variable coercion, and operation execution spans. Resolver spans are useful during performance investigations, but they can be high volume on large operations.

The ASP.NET Core server span comes from `OpenTelemetry.Instrumentation.AspNetCore`, not Hot Chocolate. Keep it enabled when you want incoming route, status code, and HTTP context correlation.

# Span attributes reference

Use these attributes for search, alerts, and dashboards. Attribute values are more stable than span names.

## Request and operation attributes

| Attribute                 | Description                                                                                             |
| ------------------------- | ------------------------------------------------------------------------------------------------------- |
| `graphql.processing.type` | Phase represented by the span, such as `request`, `validate`, `plan`, `resolve`, or `dataloader_batch`. |
| `graphql.operation.type`  | Operation type: `query`, `mutation`, or `subscription`.                                                 |
| `graphql.operation.name`  | Client-provided operation name, when present.                                                           |
| `graphql.document.hash`   | Document hash formatted as `<algorithm>:<hash>`.                                                        |
| `graphql.document.id`     | Trusted or persisted document ID, when available.                                                       |
| `graphql.document.body`   | Parsed document body when `IncludeDocument` is enabled.                                                 |
| `graphql.schema.name`     | Hot Chocolate schema name for HTTP request spans.                                                       |

## HTTP request detail attributes

These attributes appear on GraphQL HTTP request spans when the selected `RequestDetails` flags allow them.

| Attribute                         | Description                                                                  |
| --------------------------------- | ---------------------------------------------------------------------------- |
| `graphql.http.kind`               | HTTP GraphQL request kind, such as POST, GET, multipart, or schema download. |
| `graphql.http.request.type`       | `single`, `batch`, or `operation_batch`.                                     |
| `graphql.http.request.query.id`   | Document ID from the HTTP request.                                           |
| `graphql.http.request.query.hash` | Document hash from the HTTP request.                                         |
| `graphql.http.request.query.body` | HTTP request document body when `RequestDetails.Document` is enabled.        |
| `graphql.http.request.operation`  | Operation name for a single request.                                         |
| `graphql.http.request.operations` | Operation names for an operation batch.                                      |
| `graphql.http.request.variables`  | Variables when `RequestDetails.Variables` is enabled.                        |
| `graphql.http.request.extensions` | Request extensions when `RequestDetails.Extensions` is enabled.              |
| `graphql.http.request[{index}].*` | Indexed request details for batched HTTP requests.                           |

## Resolver attributes

| Attribute                         | Description                                          |
| --------------------------------- | ---------------------------------------------------- |
| `graphql.field.alias`             | Response name used by the selection.                 |
| `graphql.field.name`              | Schema field name.                                   |
| `graphql.field.path`              | Runtime response path, for example `books[0].title`. |
| `graphql.field.parent_type`       | Parent GraphQL type.                                 |
| `graphql.field.schema_coordinate` | Schema coordinate, for example `Query.books`.        |

## DataLoader attributes

| Attribute                       | Description                                         |
| ------------------------------- | --------------------------------------------------- |
| `graphql.dataloader.name`       | DataLoader name.                                    |
| `graphql.dataloader.batch.size` | Number of keys in the batch.                        |
| `graphql.dataloader.batch.keys` | Batch keys when `IncludeDataLoaderKeys` is enabled. |

## Error attributes and events

| Attribute or event           | Description                                                                                                                              |
| ---------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------- |
| `graphql.error.count`        | Total GraphQL error count on the effective request span.                                                                                 |
| `graphql.error`              | Activity event emitted for GraphQL errors.                                                                                               |
| `graphql.error.message`      | Error message on `graphql.error` events.                                                                                                 |
| `graphql.error.code`         | GraphQL error code, when set.                                                                                                            |
| `graphql.document.locations` | GraphQL source locations on error events.                                                                                                |
| `error.type`                 | Exception type, GraphQL error code, or phase fallback such as `GRAPHQL_PARSE_FAILED`, `GRAPHQL_VALIDATION_FAILED`, or `EXECUTION_ERROR`. |

# Choose the right detail level

Start with defaults in production. Increase detail for a short diagnostic window, or reduce detail when resolver spans create too much volume.

Enable all Hot Chocolate scopes temporarily:

```csharp
using HotChocolate.Diagnostics;

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddInstrumentation(options =>
    {
        options.Scopes = ActivityScopes.All;
    });
```

Remove resolver spans while keeping transport, validation, planning, and DataLoader batch visibility:

```csharp
using HotChocolate.Diagnostics;

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddInstrumentation(options =>
    {
        options.Scopes =
            ActivityScopes.ExecuteHttpRequest
            | ActivityScopes.ParseHttpRequest
            | ActivityScopes.ValidateDocument
            | ActivityScopes.CompileOperation
            | ActivityScopes.FormatHttpResponse
            | ActivityScopes.DataLoaderBatch;
    });
```

DataLoader dispatch coordinator spans may still appear because they are emitted by the DataLoader diagnostic listener and are not controlled by the `DataLoaderBatch` flag.

Control trace volume with OpenTelemetry sampling:

```csharp
using OpenTelemetry.Trace;

builder.Services
    .AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(0.10)))
            .AddAspNetCoreInstrumentation()
            .AddHotChocolateInstrumentation()
            .AddOtlpExporter();
    });
```

Head sampling can drop the whole trace before Hot Chocolate spans are exported. Use parent-based sampling when upstream systems already make sampling decisions.

# Protect private GraphQL data

Hot Chocolate defaults avoid request variables and document bodies. Keep that posture unless you have explicit approval to export raw GraphQL payloads to your observability backend.

A production-safe configuration often keeps only operation identity fields:

```csharp
using HotChocolate.Diagnostics;

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddInstrumentation(options =>
    {
        options.RequestDetails =
            RequestDetails.OperationName
            | RequestDetails.Hash
            | RequestDetails.Id;
        options.IncludeDocument = false;
        options.IncludeDataLoaderKeys = false;
    });
```

Review these options before enabling them:

- `RequestDetails.All` includes variables and HTTP request documents.
- `RequestDetails.Document` emits the HTTP request document as `graphql.http.request.query.body`.
- `RequestDetails.Variables` emits request variables.
- `IncludeDocument` emits the parsed document as `graphql.document.body`.
- `IncludeDataLoaderKeys` emits DataLoader batch keys.
- `RequestDetails.Default` includes extensions, so do not put secrets in GraphQL request extensions.

Prefer operation name, document hash, trusted document ID, and redacted low-cardinality custom tags.

# Correlate traces with requests, logs, and errors

Enable ASP.NET Core and HTTP client instrumentation to keep the GraphQL spans connected to inbound and outbound work. OpenTelemetry uses W3C trace context, so downstream HTTP calls can share the same trace when the receiving service also honors trace context.

Add OpenTelemetry logging when you want logs to carry trace and span IDs in the same backend:

```csharp
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;

builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
    logging.ParseStateValues = true;
    logging.SetResourceBuilder(
        ResourceBuilder.CreateDefault().AddService(builder.Environment.ApplicationName));
});
```

GraphQL errors set span status to error and add `error.type`. The effective request span also gets `graphql.error.count` and up to 10 `graphql.error` events by default. Lower the cap for noisy operations or set it to `0` to suppress root GraphQL error events while keeping the error count.

```csharp
using HotChocolate.Diagnostics;

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddInstrumentation(options =>
    {
        options.MaxErrorEvents = 3;
    });
```

Useful backend filters include `trace_id`, `graphql.operation.name`, `error.type`, `graphql.field.path`, and `graphql.dataloader.name`.

# Export to common backends

Prefer OTLP to a collector or platform endpoint. The same Hot Chocolate setup works with local dashboards and vendor backends because Hot Chocolate emits standard `Activity` spans.

Common deployment patterns:

- Local development with the Aspire dashboard and `UseOtlpExporter()`.
- Jaeger or Grafana Tempo through an OpenTelemetry Collector or direct OTLP endpoint.
- Azure Monitor and Application Insights through the Azure Monitor OpenTelemetry exporter.
- Any vendor backend that accepts OTLP traces.

Typical OTLP environment variables:

```bash
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
OTEL_EXPORTER_OTLP_PROTOCOL=grpc
OTEL_EXPORTER_OTLP_HEADERS=api-key=replace-with-secret-from-configuration
```

Set a stable service name with `ConfigureResource(... AddService(...))`. Without a useful service name, finding GraphQL traces in shared backends becomes harder.

For Azure-specific setup, see [Deploy to Azure](/docs/hotchocolate/v16/operations/deployment/azure#connect-application-insights-through-opentelemetry).

# Add custom tags with `ActivityEnricher`

Use `ActivityEnricher` when you need low-cardinality, redacted attributes on Hot Chocolate spans. Register the enricher as an application service so the schema service provider can resolve it.

```csharp
using System.Diagnostics;
using HotChocolate.Diagnostics;
using HotChocolate.Execution;
using HotChocolate.Resolvers;

builder.Services.AddSingleton<ActivityEnricher, TenantActivityEnricher>();

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddApplicationService<ActivityEnricher>()
    .AddInstrumentation();

public sealed class TenantActivityEnricher(InstrumentationOptions options)
    : ActivityEnricher(options)
{
    public override void EnrichExecuteRequest(RequestContext context, Activity activity)
    {
        activity.SetTag("app.tenant.tier", "standard");
    }

    public override void EnrichResolveFieldValue(IMiddlewareContext context, Activity activity)
    {
        activity.SetTag("app.graphql.field_group", context.Selection.Field.Coordinate.Name);
    }
}
```

Expected result: exported GraphQL spans include `app.tenant.tier` or `app.graphql.field_group` without losing standard Hot Chocolate attributes.

Do not add raw user IDs, access tokens, variables, or unbounded values as span attributes. High-cardinality attributes increase storage cost and make dashboards noisy.

# Troubleshoot missing or unexpected spans

| Symptom                        | Check                                                                                                                                                                                   |
| ------------------------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| No GraphQL spans               | Verify `HotChocolate.Diagnostics`, `.AddInstrumentation()`, `.AddHotChocolateInstrumentation()`, exporter configuration, sampler configuration, and that requests reach `MapGraphQL()`. |
| Only ASP.NET Core spans        | The `HotChocolate.Diagnostics` source is not registered. Use `.AddHotChocolateInstrumentation()` or `.AddSource("HotChocolate.Diagnostics")`.                                           |
| No ASP.NET Core parent span    | Add `OpenTelemetry.Instrumentation.AspNetCore` and `.AddAspNetCoreInstrumentation()`.                                                                                                   |
| Missing resolver spans         | Check `options.Scopes`. Resolver spans require `ActivityScopes.ResolveFieldValue`, which is included in `ActivityScopes.Default` unless you replace the scopes.                         |
| Too many resolver spans        | Remove `ResolveFieldValue`, add sampling, or group by low-cardinality custom tags instead of per-field dashboard dimensions.                                                            |
| Missing DataLoader keys        | Set `IncludeDataLoaderKeys = true` only after privacy review.                                                                                                                           |
| Missing document or variables  | These are opt-in. Check `RequestDetails.Document`, `RequestDetails.Variables`, and `IncludeDocument`.                                                                                   |
| Blank operation name           | The client sent an anonymous operation or a single unnamed operation. Use named operations, trusted documents, or client registry workflows.                                            |
| Health checks appear in traces | Filter `/health` and `/alive` in ASP.NET Core instrumentation.                                                                                                                          |
| v15 dashboards broke           | Update dashboards for v16 names. Do not use `RenameRootActivity`, `RequestDetails.Operation`, `RequestDetails.Query`, `graphql.document`, or `graphql.selection.field.*`.               |

# Next steps

- Use [Performance Tuning](/docs/hotchocolate/v16/guides/performance) to connect slow spans to execution, DataLoader, caching, and query-shaping decisions.
- Use [Instrumentation](/docs/hotchocolate/v16/server/instrumentation) when you need custom diagnostic event listeners.
- Use [HTTP Transport](/docs/hotchocolate/v16/server/http-transport) for request transport behavior.
- Use [Errors](/docs/hotchocolate/v16/api-reference/errors) to shape GraphQL errors before they appear in traces.
- Use [Trusted Documents](/docs/hotchocolate/v16/performance/trusted-documents) for stable document IDs and hashes.
- Use [Metrics](/docs/hotchocolate/v16/operations/observability/metrics) when you need custom meters or adjacent .NET and GreenDonut metrics.
