---
title: OpenTelemetry tracing
---

Hot Chocolate v16 emits GraphQL trace spans using `System.Diagnostics.ActivitySource`. These spans help you answer key production questions: Which operations are slow? Which resolvers or DataLoader batches contribute most to latency? Where do requests fail: HTTP parsing, validation, execution, or response formatting?

This page explains how to enable and use tracing in Hot Chocolate v16 servers. It does not cover Fusion diagnostics, which use separate options. Metrics are also separate: `AddHotChocolateInstrumentation()` registers the Hot Chocolate `ActivitySource` for traces, not a `Meter`.

# Prerequisites

Before you start, make sure you have:

- A Hot Chocolate v16 server with `app.MapGraphQL()`
- The `HotChocolate.Diagnostics` package
- OpenTelemetry packages for hosting, ASP.NET Core, outgoing HTTP, and your chosen exporter
- Access to a dashboard, collector, or vendor backend that accepts OTLP or another OpenTelemetry exporter

Install the required packages:

```bash
dotnet add package HotChocolate.Diagnostics
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Instrumentation.AspNetCore
dotnet add package OpenTelemetry.Instrumentation.Http
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
```

After installing, restore packages and ensure your application builds.

# Set up Hot Chocolate tracing

To enable tracing, add `.AddInstrumentation()` to your GraphQL builder and `.AddHotChocolateInstrumentation()` to the OpenTelemetry tracing pipeline:

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

`AddInstrumentation()` enables Hot Chocolate's diagnostic listeners, which create `Activity` spans. `AddHotChocolateInstrumentation()` registers the `HotChocolate.Diagnostics` `ActivitySource` with OpenTelemetry. ASP.NET Core instrumentation creates the parent HTTP server span, and Hot Chocolate spans are nested under it.

## Using Aspire service defaults

If your app uses a shared ServiceDefaults project, keep the exporter and common ASP.NET Core filters there, and add Hot Chocolate to the tracing builder:

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

When `OTEL_EXPORTER_OTLP_ENDPOINT` is set, traces export through OTLP. Health checks are filtered out of traces.

# Verify tracing works

Start your application and send a named GraphQL operation:

```graphql
query GetBooks {
  books
}
```

In your tracing backend, search for the service name or for `graphql.operation.name = GetBooks`.

A typical trace includes spans like this:

```text
HTTP POST /graphql                 # ASP.NET Core span
└─ query                            # Hot Chocolate request span
   ├─ Parse HTTP Request
   ├─ GraphQL Document Validation
   ├─ GraphQL Operation Planning
   ├─ Query.books                   # resolver span
   └─ Format HTTP Response
```

If your operation uses DataLoaders, you may also see `GraphQL DataLoader Dispatch` and `GraphQL DataLoader Batch <Name>` spans.

You should see spans from the `HotChocolate.Diagnostics` source, not only ASP.NET Core spans.

# What spans does Hot Chocolate emit?

Hot Chocolate uses the `HotChocolate.Diagnostics` `ActivitySource`. Calling `AddHotChocolateInstrumentation()` is equivalent to registering this source with `TracerProviderBuilder.AddSource("HotChocolate.Diagnostics")`.

The root span for each request uses a display name like `query`, `mutation`, or `subscription`. The operation name appears as the `graphql.operation.name` attribute. Build dashboards using attributes, not root span names.

By default, `ActivityScopes.Default` includes these scopes:

- `ExecuteHttpRequest`
- `ParseHttpRequest`
- `ValidateDocument`
- `CompileOperation`
- `ResolveFieldValue`
- `FormatHttpResponse`
- `DataLoaderBatch`

You can enable additional scopes for more detail, such as request execution, document parsing, complexity analysis, variable coercion, and operation execution. Resolver spans are helpful for performance investigations but can be high volume for large operations.

The parent HTTP server span comes from `OpenTelemetry.Instrumentation.AspNetCore`, not Hot Chocolate. Keep it enabled to correlate incoming routes, status codes, and HTTP context.

# Span attributes reference

Use these attributes for searching, alerting, and building dashboards. Attribute values are more stable than span names.

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

These attributes appear on GraphQL HTTP request spans when the selected `RequestDetails` flags are enabled.

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

# Adjusting trace detail

Start with the default settings in production. Increase detail for short diagnostic windows, or reduce detail if resolver spans create too much volume.

To enable all Hot Chocolate scopes temporarily:

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

To remove resolver spans but keep transport, validation, planning, and DataLoader batch visibility:

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

Note: DataLoader dispatch coordinator spans may still appear, as they are emitted by the DataLoader diagnostic listener and not controlled by the `DataLoaderBatch` flag.

To control trace volume, use OpenTelemetry sampling:

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

Head sampling can drop the entire trace before Hot Chocolate spans are exported. Use parent-based sampling if upstream systems already make sampling decisions.

# Protecting private GraphQL data

By default, Hot Chocolate avoids exporting request variables and document bodies. Keep this posture unless you have explicit approval to export raw GraphQL payloads to your observability backend.

A production-safe configuration typically keeps only operation identity fields:

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

- `RequestDetails.All` includes variables and HTTP request documents
- `RequestDetails.Document` emits the HTTP request document as `graphql.http.request.query.body`
- `RequestDetails.Variables` emits request variables
- `IncludeDocument` emits the parsed document as `graphql.document.body`
- `IncludeDataLoaderKeys` emits DataLoader batch keys
- `RequestDetails.Default` includes extensions, so do not put secrets in GraphQL request extensions

Prefer to export only operation name, document hash, trusted document ID, and redacted low-cardinality custom tags.

# Correlating traces with requests, logs, and errors

Enable ASP.NET Core and HTTP client instrumentation to keep GraphQL spans connected to inbound and outbound work. OpenTelemetry uses W3C trace context, so downstream HTTP calls can share the same trace if the receiving service honors trace context.

To include trace and span IDs in logs, add OpenTelemetry logging:

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

When GraphQL errors occur, the span status is set to error and `error.type` is added. The request span also gets `graphql.error.count` and, by default, up to 10 `graphql.error` events. You can lower this cap for noisy operations or set it to `0` to suppress root GraphQL error events while keeping the error count:

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

# Exporting to common backends

Prefer OTLP to a collector or platform endpoint. The same Hot Chocolate setup works with local dashboards and vendor backends because Hot Chocolate emits standard `Activity` spans.

Common deployment patterns include:

- Local development with the Aspire dashboard and `UseOtlpExporter()`
- Jaeger or Grafana Tempo through an OpenTelemetry Collector or direct OTLP endpoint
- Azure Monitor and Application Insights through the Azure Monitor OpenTelemetry exporter
- Any vendor backend that accepts OTLP traces

Set these environment variables for OTLP:

```bash
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
OTEL_EXPORTER_OTLP_PROTOCOL=grpc
OTEL_EXPORTER_OTLP_HEADERS=api-key=replace-with-secret-from-configuration
```

Always set a stable service name using `ConfigureResource(... AddService(...))`. Without a clear service name, finding GraphQL traces in shared backends is difficult.

# Adding custom tags with `ActivityEnricher`

Use `ActivityEnricher` to add low-cardinality, redacted attributes to Hot Chocolate spans. Register your enricher as an application service so the schema service provider can resolve it:

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

Your exported GraphQL spans now include `app.tenant.tier` or `app.graphql.field_group` without losing standard Hot Chocolate attributes.

Do not add raw user IDs, access tokens, variables, or unbounded values as span attributes. High-cardinality attributes increase storage costs and make dashboards noisy.

# Troubleshooting: missing or unexpected spans

| Symptom                        | What to check                                                                                                                                                                           |
| ------------------------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| No GraphQL spans               | Verify `HotChocolate.Diagnostics`, `.AddInstrumentation()`, `.AddHotChocolateInstrumentation()`, exporter configuration, sampler configuration, and that requests reach `MapGraphQL()`. |
| Only ASP.NET Core spans        | The `HotChocolate.Diagnostics` source is not registered. Use `.AddHotChocolateInstrumentation()` or `.AddSource("HotChocolate.Diagnostics")`.                                           |
| No ASP.NET Core parent span    | Add `OpenTelemetry.Instrumentation.AspNetCore` and `.AddAspNetCoreInstrumentation()`.                                                                                                   |
| Missing resolver spans         | Check `options.Scopes`. Resolver spans require `ActivityScopes.ResolveFieldValue`, which is included in `ActivityScopes.Default` unless you override the scopes.                        |
| Too many resolver spans        | Remove `ResolveFieldValue`, add sampling, or group by low-cardinality custom tags instead of per-field dashboard dimensions.                                                            |
| Missing DataLoader keys        | Set `IncludeDataLoaderKeys = true` only after a privacy review.                                                                                                                         |
| Missing document or variables  | These are opt-in. Check `RequestDetails.Document`, `RequestDetails.Variables`, and `IncludeDocument`.                                                                                   |
| Blank operation name           | The client sent an anonymous operation or a single unnamed operation. Use named operations, trusted documents, or client registry workflows.                                            |
| Health checks appear in traces | Filter `/health` and `/alive` in ASP.NET Core instrumentation.                                                                                                                          |
| v15 dashboards broke           | Update dashboards for v16 names. Do not use `RenameRootActivity`, `RequestDetails.Operation`, `RequestDetails.Query`, `graphql.document`, or `graphql.selection.field.*`.               |

# Next steps

- See [Performance Tuning](/docs/hotchocolate/v16/guides/performance) to connect slow spans to execution, DataLoader, caching, and query-shaping decisions.
- Use [Instrumentation](/docs/hotchocolate/v16/server/instrumentation) for custom diagnostic event listeners.
- See [HTTP Transport](/docs/hotchocolate/v16/server/http-transport) for request transport behavior.
- Use [Errors](/docs/hotchocolate/v16/api-reference/errors) to shape GraphQL errors before they appear in traces.
- Use [Trusted Documents](/docs/hotchocolate/v16/performance/trusted-documents) for stable document IDs and hashes.
- See [Metrics](/docs/hotchocolate/v16/operations/observability/metrics) for custom meters or .NET and GreenDonut metrics.
