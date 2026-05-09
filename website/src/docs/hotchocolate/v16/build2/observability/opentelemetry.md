---
title: OpenTelemetry
---

OpenTelemetry turns Hot Chocolate execution into distributed traces. Without GraphQL instrumentation, a trace often shows one ASP.NET Core span for `/graphql`. With Hot Chocolate instrumentation, the same request can show HTTP parsing, validation, operation planning, resolver fields, and DataLoader batches.

This page focuses on Hot Chocolate v16 tracing. It uses OTLP because it works with collectors and many tracing backends.

# Mental model

Hot Chocolate and OpenTelemetry need two registrations:

```text
GraphQL builder
  .AddInstrumentation()
      creates Activity spans from Hot Chocolate diagnostic events

OpenTelemetry tracing builder
  .AddHotChocolateInstrumentation()
      subscribes to ActivitySource: HotChocolate.Diagnostics
```

`AddInstrumentation()` creates activities. `AddHotChocolateInstrumentation()` tells the OpenTelemetry SDK to listen to the `HotChocolate.Diagnostics` activity source. You need both.

For end-to-end traces, add host instrumentation too. ASP.NET Core instrumentation creates the inbound HTTP server span. HTTP client, database, and application instrumentation add the downstream spans that resolver code creates.

# Install packages

Install `HotChocolate.Diagnostics` with the same `16.x` version as the other `HotChocolate.*` packages in your application.

<PackageInstallation packageName="HotChocolate.Diagnostics" />

Install the OpenTelemetry packages used by the examples:

```bash
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Instrumentation.AspNetCore
dotnet add package OpenTelemetry.Instrumentation.Http
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
```

| Package                                        | Purpose                                                           |
| ---------------------------------------------- | ----------------------------------------------------------------- |
| `HotChocolate.Diagnostics`                     | Adds Hot Chocolate activity instrumentation APIs.                 |
| `OpenTelemetry.Extensions.Hosting`             | Registers OpenTelemetry with the .NET host.                       |
| `OpenTelemetry.Instrumentation.AspNetCore`     | Creates inbound HTTP server spans.                                |
| `OpenTelemetry.Instrumentation.Http`           | Creates spans for `HttpClient` calls from resolvers and services. |
| `OpenTelemetry.Exporter.OpenTelemetryProtocol` | Exports traces to an OTLP collector or backend.                   |

Add exporter-specific packages only when your backend requires a different exporter.

# Enable Hot Chocolate activities

Add instrumentation to the GraphQL builder:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddInstrumentation();
```

This registers Hot Chocolate diagnostic listeners for server, execution, and DataLoader events. It does not export spans by itself.

If your project centralizes GraphQL setup, put the call in the shared convention method:

```csharp
public static class GraphQLConventions
{
    public static IRequestExecutorBuilder AddGraphQLConventions(
        this IRequestExecutorBuilder builder)
    {
        return builder
            .AddQueryType<Query>()
            .AddInstrumentation();
    }
}
```

# Register Hot Chocolate with OpenTelemetry

Configure the tracing provider and add the Hot Chocolate source:

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
    .ConfigureResource(resource => resource.AddService("Catalog.Api"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddHotChocolateInstrumentation()
        .AddOtlpExporter());

var app = builder.Build();

app.MapGraphQL();

await app.RunWithGraphQLCommandsAsync(args);
```

| Call                                    | What it adds                                            |
| --------------------------------------- | ------------------------------------------------------- |
| `ConfigureResource(...AddService(...))` | Sets the service name shown in the backend.             |
| `AddAspNetCoreInstrumentation()`        | Adds the ASP.NET Core HTTP server span.                 |
| `AddHttpClientInstrumentation()`        | Adds outbound HTTP spans from resolvers or services.    |
| `AddHotChocolateInstrumentation()`      | Subscribes OpenTelemetry to `HotChocolate.Diagnostics`. |
| `AddOtlpExporter()`                     | Sends traces to the configured OTLP endpoint.           |

Set the OTLP endpoint with an environment variable in local development or deployment:

```bash
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
```

The endpoint, protocol, headers, and credentials depend on your collector or tracing backend.

# Export traces with OTLP

The default `AddOtlpExporter()` reads standard OpenTelemetry exporter configuration. Prefer environment variables for deployment so the application code is independent of the backend.

Common settings:

| Setting                       | Purpose                                          | Example                                                           |
| ----------------------------- | ------------------------------------------------ | ----------------------------------------------------------------- |
| Service name                  | Groups spans under a stable service identity.    | `Catalog.Api`                                                     |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Collector or backend endpoint.                   | `http://localhost:4317`                                           |
| Sampler                       | Controls which traces are recorded and exported. | Development `AlwaysOnSampler`; production ratio or tail sampling. |

If you need code-based exporter configuration:

```csharp
builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("Catalog.Api"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHotChocolateInstrumentation()
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://localhost:4317");
        }));
```

# Verify your first trace

Start the application, send a named operation, then look for spans from `HotChocolate.Diagnostics`.

```bash
curl -s http://localhost:5000/graphql \
  -H "Content-Type: application/json" \
  -H "Accept: application/graphql-response+json" \
  --data '{"query":"query GetProducts { products { id name } }"}'
```

A trace can contain this shape when HTTP instrumentation is enabled and the relevant Hot Chocolate scopes are active:

```text
HTTP POST /graphql                         ASP.NET Core instrumentation
  GraphQL HTTP POST, or query              Hot Chocolate request span
    Parse HTTP Request
    GraphQL Operation, displayed as query  when ExecuteRequest is enabled
      GraphQL Document Validation
      GraphQL Operation Planning
      Product.name                         resolver field span
      GraphQL DataLoader Batch ProductByIdDataLoader
    Format HTTP Response
```

The exact tree depends on the request, transport, enabled `ActivityScopes`, caching, and sampling.

Check these values first:

| What to check                                            | Expected value                                                              |
| -------------------------------------------------------- | --------------------------------------------------------------------------- |
| Activity source                                          | `HotChocolate.Diagnostics`                                                  |
| GraphQL operation activity operation name                | `GraphQL Operation` when `ExecuteRequest` creates a separate operation span |
| GraphQL operation activity display name after completion | `query`, `mutation`, or `subscription`                                      |
| Operation name attribute                                 | `graphql.operation.name`, for example `GetProducts`                         |
| Processing type attribute                                | `graphql.processing.type`                                                   |
| Document identity                                        | `graphql.document.hash`, and sometimes `graphql.document.id`                |

Hot Chocolate v16 keeps the root span display name low-cardinality. Use `graphql.operation.name` for the client-supplied operation name.

# Choose instrumentation detail safely

Start with defaults. Increase detail for local debugging or a bounded production incident, then return to the smallest scope set that answers the question.

```csharp
using HotChocolate.Diagnostics;

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddInstrumentation(options =>
    {
        options.Scopes = ActivityScopes.Default;
    });
```

`ActivityScopes.Default` includes:

- `ExecuteHttpRequest`
- `ParseHttpRequest`
- `ValidateDocument`
- `CompileOperation`
- `ResolveFieldValue`
- `FormatHttpResponse`
- `DataLoaderBatch`

`ActivityScopes.All` also includes `ExecuteRequest`, `ParseDocument`, `AnalyzeComplexity`, `CoerceVariables`, and `ExecuteOperation`.

| Scope setting            | Use                                                                      |
| ------------------------ | ------------------------------------------------------------------------ |
| `ActivityScopes.Default` | Production starting point. Includes resolver and DataLoader batch spans. |
| `ActivityScopes.All`     | Local diagnostics or a short investigation window. Produces more spans.  |
| `ActivityScopes.None`    | Special cases where Hot Chocolate activities must be disabled.           |
| Individual flags         | Build a smaller set, for example validation and planning only.           |

Development detail example:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddInstrumentation(options =>
    {
        options.Scopes = ActivityScopes.All;
        options.RequestDetails = RequestDetails.All;
        options.IncludeDocument = true;
        options.IncludeDataLoaderKeys = true;
    });

builder.Services
    .AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .SetSampler(new AlwaysOnSampler())
        .AddHotChocolateInstrumentation()
        .AddOtlpExporter());
```

Use that pattern only with data that is safe to export. It can include full documents, variables, request extensions, and DataLoader keys.

Production-oriented example:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddInstrumentation(options =>
    {
        options.Scopes = ActivityScopes.Default;
        options.RequestDetails =
            RequestDetails.Id |
            RequestDetails.Hash |
            RequestDetails.OperationName;
        options.IncludeDocument = false;
        options.IncludeDataLoaderKeys = false;
        options.MaxErrorEvents = 10;
    });
```

This keeps operation names and document hashes while omitting variables, extensions, full document text, and DataLoader keys.

# Protect private data

Telemetry is copied out of the request path and may be retained by collectors or vendors. Treat it like an application data export.

| Option                  | Default                  | Risk                                                                                                              |
| ----------------------- | ------------------------ | ----------------------------------------------------------------------------------------------------------------- |
| `RequestDetails`        | `RequestDetails.Default` | Default includes document id, document hash, operation name, and extensions. Review extensions before export.     |
| `RequestDetails.All`    | Off                      | Adds variables and document to HTTP request spans. Use only when safe.                                            |
| `IncludeDocument`       | `false`                  | Adds `graphql.document.body` to operation spans. The document can contain sensitive literals.                     |
| `IncludeDataLoaderKeys` | `false`                  | Adds `graphql.dataloader.batch.keys`. Keys can expose ids, tenant data, or business identifiers.                  |
| `MaxErrorEvents`        | `10`                     | Caps `graphql.error` events. `0` suppresses individual error events, but `graphql.error.count` remains available. |

Use hashes, trusted document ids, operation names, sanitized error codes, and bounded custom tags for dashboards. Avoid raw user input, emails, access tokens, full variable payloads, and high-cardinality identifiers.

# Understand common spans and attributes

Hot Chocolate emits spans aligned with the proposed GraphQL semantic conventions and a small set of Hot Chocolate specific attributes.

| Span display name                                | Key marker            | Enabled by                                                                | Useful attributes                                                                                                                 |
| ------------------------------------------------ | --------------------- | ------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------- |
| `GraphQL HTTP POST`, `GraphQL HTTP GET`          | HTTP transport span   | `ExecuteHttpRequest`                                                      | `graphql.http.kind`, `graphql.schema.name`, `graphql.http.request.type`                                                           |
| `Parse HTTP Request`                             | HTTP parsing span     | `ParseHttpRequest`                                                        | Error events when HTTP request parsing fails.                                                                                     |
| `query`, `mutation`, `subscription`              | `request`             | `ExecuteRequest`, or HTTP span reuse when HTTP instrumentation is enabled | `graphql.operation.type`, `graphql.operation.name`, `graphql.document.hash`, `graphql.document.id`, `graphql.error.count`         |
| `GraphQL Document Parsing`                       | Document parsing span | `ParseDocument`                                                           | `graphql.document.hash`                                                                                                           |
| `GraphQL Document Validation`                    | `validate`            | `ValidateDocument`                                                        | `graphql.document.hash`, validation error status.                                                                                 |
| `GraphQL Complexity Analysis`                    | Cost analysis span    | `AnalyzeComplexity`                                                       | `graphql.operation.fieldCost`, `graphql.operation.typeCost`                                                                       |
| `GraphQL Variable Coercion`                      | `variable_coercion`   | `CoerceVariables`                                                         | Operation and document attributes.                                                                                                |
| `GraphQL Operation Planning`                     | `plan`                | `CompileOperation`                                                        | `graphql.operation.type`, `graphql.operation.name`, `graphql.document.hash`                                                       |
| `GraphQL Operation Execution`                    | `execute`             | `ExecuteOperation`                                                        | Operation and error status attributes.                                                                                            |
| `Query.hero` or `Product.name`                   | `resolve`             | `ResolveFieldValue`                                                       | `graphql.field.name`, `graphql.field.path`, `graphql.field.parent_type`, `graphql.field.schema_coordinate`, `graphql.field.alias` |
| `GraphQL DataLoader Batch ProductByIdDataLoader` | `dataloader_batch`    | `DataLoaderBatch`                                                         | `graphql.dataloader.name`, `graphql.dataloader.batch.size`, optional `graphql.dataloader.batch.keys`                              |

Useful dashboard filters and groupings:

| Goal                          | Attribute or filter                                                                     |
| ----------------------------- | --------------------------------------------------------------------------------------- |
| Find GraphQL spans            | `ActivitySource` or instrumentation scope name equals `HotChocolate.Diagnostics`        |
| Group operation spans by type | `graphql.operation.type`                                                                |
| Group named operations        | `graphql.operation.name`                                                                |
| Find failed GraphQL requests  | Span status is error or `graphql.error.count` exists                                    |
| Find slow resolvers           | `graphql.processing.type = resolve`, grouped by `graphql.field.schema_coordinate`       |
| Find large DataLoader batches | `graphql.processing.type = dataloader_batch`, sorted by `graphql.dataloader.batch.size` |

Dashboard query syntax differs between backends, so use the attribute names as the portable part.

# Sampling and production setup

Hot Chocolate creates activities when instrumentation is enabled. The OpenTelemetry sampler decides which traces are recorded and exported.

Development often benefits from an always-on sampler:

```csharp
builder.Services
    .AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .SetSampler(new AlwaysOnSampler())
        .AddAspNetCoreInstrumentation()
        .AddHotChocolateInstrumentation()
        .AddOtlpExporter());
```

For production, prefer one of these patterns:

- Parent-based ratio sampling in the application.
- Tail sampling in an OpenTelemetry Collector.
- A backend sampling policy that keeps errors and representative successful requests.

Example application-side ratio sampler:

```csharp
builder.Services
    .AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(0.05)))
        .AddAspNetCoreInstrumentation()
        .AddHotChocolateInstrumentation()
        .AddOtlpExporter());
```

Sampling reduces volume. It does not make sensitive attributes safe. Decide what can be recorded before sampling is applied.

If health checks make traces noisy, filter them in ASP.NET Core instrumentation:

```csharp
builder.Services
    .AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(options =>
        {
            options.Filter = httpContext =>
                httpContext.Request.Path != "/health" &&
                httpContext.Request.Path != "/alive";
        })
        .AddHotChocolateInstrumentation()
        .AddOtlpExporter());
```

That filter applies to ASP.NET Core HTTP spans. It is not a Hot Chocolate scope filter.

# Relation to diagnostic events and enrichment

OpenTelemetry tracing is built on Hot Chocolate diagnostic events. `AddInstrumentation()` registers built-in diagnostic listeners that convert selected events to `Activity` spans.

Use the other observability APIs when you need different behavior:

| Need                                                             | Use                   |
| ---------------------------------------------------------------- | --------------------- |
| Custom log lines or custom metrics from GraphQL lifecycle events | Diagnostic events     |
| Custom tags on Hot Chocolate spans                               | `ActivityEnricher`    |
| Export Hot Chocolate spans to a backend                          | OpenTelemetry tracing |

`AddHotChocolateInstrumentation()` registers the tracing source. It does not register a Hot Chocolate metrics stream. Use normal .NET metrics or diagnostic listeners for custom metrics.

# Troubleshooting

| Symptom                                                 | Likely cause                                                                                                                                                                                  | Fix                                                                                                                |
| ------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------ |
| No Hot Chocolate spans                                  | Missing package, missing `.AddInstrumentation()`, missing `.AddHotChocolateInstrumentation()`, sampler drops traces, or no GraphQL request was executed.                                      | Confirm the package and both registrations, use a temporary always-on sampler in development, then send a request. |
| Only ASP.NET Core HTTP spans appear                     | OpenTelemetry is configured, but the Hot Chocolate source is not registered or GraphQL instrumentation is not enabled.                                                                        | Add `.AddInstrumentation()` to the GraphQL builder and `.AddHotChocolateInstrumentation()` to tracing.             |
| Activities appear in a debugger, but not in the backend | Exporter missing, collector unreachable, endpoint or protocol mismatch, authentication missing, or sampler drops traces.                                                                      | Check OTLP configuration, collector logs, backend credentials, and sampler settings.                               |
| Too many spans                                          | `ActivityScopes.All`, resolver spans on large operations, or no sampling.                                                                                                                     | Reduce scopes, sample traces, and avoid permanent broad tracing on high-volume traffic.                            |
| Sensitive data appears                                  | `RequestDetails.All`, `RequestDetails.Extensions`, `RequestDetails.Variables`, `RequestDetails.Document`, `IncludeDocument`, `IncludeDataLoaderKeys`, or custom enrichment exported raw data. | Narrow request details, disable full documents and DataLoader keys, and review enrichers.                          |
| DataLoader keys are missing                             | `IncludeDataLoaderKeys` defaults to `false`.                                                                                                                                                  | Enable it only in a safe environment or with safe keys.                                                            |
| GraphQL span is error, but HTTP status is 200           | GraphQL execution errors can be represented in a successful HTTP response.                                                                                                                    | Inspect `graphql.error.count`, `graphql.error` events, `error.type`, and the response errors.                      |
| Operation name is not in the span name                  | v16 uses a low-cardinality display name.                                                                                                                                                      | Use `graphql.operation.name` for the operation name.                                                               |
| Validation or planning spans are missing                | The relevant `ActivityScopes` flag is disabled or the operation was served from a cache path that did not run that phase.                                                                     | Enable the scope needed for the investigation and account for persisted or cached documents.                       |

# Production checklist

- Use the same `16.x` version for all `HotChocolate.*` packages.
- Register both `.AddInstrumentation()` and `.AddHotChocolateInstrumentation()`.
- Set a stable service name with `ConfigureResource(...AddService(...))`.
- Add ASP.NET Core, HTTP client, database, and application instrumentation as needed.
- Keep default or narrow `ActivityScopes` until an investigation needs more detail.
- Use sampling or collector policies before high-volume rollout.
- Review `RequestDetails.Default` because it includes extensions.
- Keep `IncludeDocument` and `IncludeDataLoaderKeys` disabled unless the exported values are approved.
- Cap error events with `MaxErrorEvents`.
- Keep custom diagnostic listeners and enrichers fast, bounded, and low-cardinality.
- Test exporter connectivity and sampling in an environment that resembles production.

# Next steps

- [Observability overview](./index): choose between tracing, diagnostic events, and enrichment.
- [Diagnostic events](./diagnostic-events): build custom listeners for logs, metrics, and lifecycle hooks.
- [Activity enrichment](./activity-enrichment): add safe custom tags to Hot Chocolate spans.
- [Execution engine](../execution-engine): understand the phases behind validation, planning, execution, and resolver spans.
- [DataLoader](../dataloader): understand the batching behavior behind DataLoader spans.
- [Performance](../performance): use traces to investigate slow resolvers, batching, and request latency.
- [HTTP transport](../server-configuration/http-transport): understand GET, POST, batching, and response behavior.
- [Trusted documents](../security/trusted-documents): use document ids and hashes instead of full documents.
- [Migrate from v15 to v16](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16#addinstrumentation): review telemetry naming and option changes.
