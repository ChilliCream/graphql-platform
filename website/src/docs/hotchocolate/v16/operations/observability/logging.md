---
title: Production Logging
---

This page shows how to operate Hot Chocolate v16 logging safely in production. You use the normal ASP.NET Core logging pipeline for hosting and application logs, then add Hot Chocolate diagnostic listeners or OpenTelemetry instrumentation when you need GraphQL request, resolver, validation, and DataLoader context.

Fusion gateway diagnostics use separate instrumentation and are outside the scope of this page.

# Prerequisites

Choose the setup that matches your goal.

| Goal                              | Required packages                                               | Configure                                                                                                   |
| --------------------------------- | --------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------- |
| ASP.NET Core and application logs | Your normal ASP.NET Core logging providers                      | `Logging` in `appsettings.json`, `ILogger<T>` in your services                                              |
| GraphQL request lifecycle logs    | Hot Chocolate server packages                                   | `ExecutionDiagnosticEventListener`, `ServerDiagnosticEventListener`, or `DataLoaderDiagnosticEventListener` |
| Logs correlated with traces       | `HotChocolate.Diagnostics`, OpenTelemetry packages, an exporter | `.AddInstrumentation()`, `AddOpenTelemetry`, `AddHotChocolateInstrumentation()`                             |

For OpenTelemetry examples, add the packages you use in production:

```bash
dotnet add package HotChocolate.Diagnostics
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Instrumentation.AspNetCore
dotnet add package OpenTelemetry.Instrumentation.Http
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
```

You also need access to `Program.cs` and your logging configuration, usually `appsettings.json`.

# Configure a safe production logging baseline

Start with normal ASP.NET Core logging and server-side exception logging. Keep exception details out of client GraphQL responses outside development.

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Catalog.Api": "Information"
    }
  }
}
```

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .ModifyRequestOptions(options =>
    {
        options.IncludeExceptionDetails = builder.Environment.IsDevelopment();
    })
    .AddErrorFilter<LoggingErrorFilter>();

var app = builder.Build();
app.MapGraphQL();
app.Run();
```

```csharp
// Infrastructure/LoggingErrorFilter.cs
namespace Catalog.Api.Infrastructure;

public sealed class LoggingErrorFilter : IErrorFilter
{
    private readonly ILogger<LoggingErrorFilter> _logger;

    public LoggingErrorFilter(ILogger<LoggingErrorFilter> logger)
    {
        _logger = logger;
    }

    public IError OnError(IError error)
    {
        if (error.Exception is null)
        {
            return error;
        }

        _logger.LogError(
            error.Exception,
            "GraphQL error {ErrorCode} at {Path}",
            error.Code,
            error.Path);

        return error
            .WithMessage("An internal error occurred.")
            .WithCode("INTERNAL_ERROR")
            .WithException(null);
    }
}
```

Expected client response for an unhandled resolver exception:

```json
{
  "errors": [
    {
      "message": "An internal error occurred.",
      "path": ["viewer"],
      "extensions": {
        "code": "INTERNAL_ERROR"
      }
    }
  ],
  "data": {
    "viewer": null
  }
}
```

Your server log contains the exception type and stack trace. The GraphQL response contains a stable, safe message and code. Do not log variables, raw documents, headers, cookies, or authorization tokens in this baseline.

See [Error Handling](/docs/hotchocolate/v16/guides/error-handling), [Errors](/docs/hotchocolate/v16/api-reference/errors), and [Options Reference](/docs/hotchocolate/v16/api-reference/options) for the client-facing error model and `IncludeExceptionDetails` option.

# Understand what Hot Chocolate logs by default

Hot Chocolate v16 does not require a special logging provider. ASP.NET Core request logging still records HTTP method, path, status code, routing failures, and unhandled ASP.NET Core pipeline exceptions for the `/graphql` endpoint.

GraphQL operation details are different. Operation names, document hashes, validation failures, resolver timing, and DataLoader batches are exposed through Hot Chocolate diagnostic events and OpenTelemetry activities. Do not expect a category filter such as `Logging:LogLevel:HotChocolate` to create a stream of GraphQL request lifecycle logs.

Schema validation can produce internal Hot Chocolate validation log entries, such as `HCV0001`, during schema construction. Those entries are not the production request telemetry surface.

Use category filters for the hosting and application logs you already have:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "HotChocolate": "Warning"
    }
  }
}
```

If you need GraphQL operation fields in logs, add a diagnostic listener or OpenTelemetry instrumentation.

# Add GraphQL request context to log scopes

Use an execution diagnostic listener to attach safe GraphQL identifiers to a logging scope. The scope is active while Hot Chocolate executes the GraphQL request.

```csharp
// Program.cs
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddDiagnosticEventListener<GraphQLLoggingListener>();
```

```csharp
// Diagnostics/GraphQLLoggingListener.cs
using System.Diagnostics;
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;

namespace Catalog.Api.Diagnostics;

public sealed class GraphQLLoggingListener : ExecutionDiagnosticEventListener
{
    private readonly ILogger<GraphQLLoggingListener> _logger;

    public GraphQLLoggingListener(ILogger<GraphQLLoggingListener> logger)
    {
        _logger = logger;
    }

    public override IDisposable ExecuteRequest(RequestContext context)
    {
        var documentInfo = context.OperationDocumentInfo;
        var fields = new Dictionary<string, object?>
        {
            ["TraceId"] = Activity.Current?.TraceId.ToString(),
            ["GraphQLRequestIndex"] = context.RequestIndex,
            ["GraphQLOperationName"] = context.Request.OperationName,
            ["GraphQLDocumentHash"] = FormatHash(documentInfo.Hash),
            ["GraphQLDocumentId"] = FormatId(documentInfo.Id)
        };

        return new RequestLogScope(_logger, fields);
    }

    private static string? FormatHash(OperationDocumentHash hash)
        => hash.IsEmpty ? null : $"{hash.AlgorithmName}:{hash.Value}";

    private static string? FormatId(OperationDocumentId id)
        => id.IsEmpty ? null : id.Value;

    private sealed class RequestLogScope : IDisposable
    {
        private readonly ILogger _logger;
        private readonly IDisposable? _scope;
        private readonly long _startTimestamp = Stopwatch.GetTimestamp();

        public RequestLogScope(
            ILogger logger,
            IReadOnlyDictionary<string, object?> fields)
        {
            _logger = logger;
            _scope = logger.BeginScope(fields);
        }

        public void Dispose()
        {
            var elapsed = Stopwatch.GetElapsedTime(_startTimestamp);

            _logger.LogInformation(
                "GraphQL request completed in {ElapsedMilliseconds} ms",
                elapsed.TotalMilliseconds);

            _scope?.Dispose();
        }
    }
}
```

Expected structured log properties:

```text
Message="GraphQL request completed in 42.7 ms"
TraceId="8b9f0f7d6a3c0e2a9f6a7d8e9c0b1a2f"
GraphQLOperationName="GetViewer"
GraphQLDocumentHash="sha256:2f7c..."
GraphQLDocumentId="GetViewer"
ElapsedMilliseconds=42.7
```

Diagnostic listeners are created once and handlers run synchronously as part of request execution. Keep listener work small, use structured fields, and enqueue expensive export work to a background service. Avoid raw `context.Request.Document`, variables, extensions, and headers in production logs.

Operation type is available as the `graphql.operation.type` OpenTelemetry attribute after Hot Chocolate resolves the operation. Prefer OpenTelemetry spans for that field unless your custom listener records it later in the execution pipeline.

# Log request, validation, resolver, subscription, and DataLoader failures

Use the hook that matches the failure phase. This keeps alerts actionable and avoids treating client mistakes as server incidents.

| Failure phase                      | Hook                                                      | Recommended level                                           | Safe fields                                                              |
| ---------------------------------- | --------------------------------------------------------- | ----------------------------------------------------------- | ------------------------------------------------------------------------ |
| HTTP transport failure             | `ServerDiagnosticEventListener.HttpRequestError`          | Warning or Error, depending on cause                        | HTTP path, method, status, exception type                                |
| HTTP GraphQL parse failure         | `ServerDiagnosticEventListener.ParserErrors`              | Information or Warning                                      | Error code, count, trace ID                                              |
| Request setup or execution failure | `ExecutionDiagnosticEventListener.RequestError`           | Error for exceptions, Warning for unexpected request errors | Operation name, document hash or ID, exception type, error code          |
| Validation failure                 | `ExecutionDiagnosticEventListener.ValidationErrors`       | Debug, Information, or Warning for abuse detection          | Error count, first error code, operation name, document hash             |
| Resolver failure                   | `ExecutionDiagnosticEventListener.ResolverError`          | Error for unexpected exceptions                             | Path, schema coordinate, error code, exception type                      |
| Subscription event failure         | `ExecutionDiagnosticEventListener.SubscriptionEventError` | Error                                                       | Subscription ID, operation name, exception type                          |
| DataLoader batch failure           | `DataLoaderDiagnosticEventListener.BatchError`            | Error                                                       | DataLoader type, batch size, exception type                              |
| DataLoader item failure            | `DataLoaderDiagnosticEventListener.BatchItemError`        | Warning or Error                                            | DataLoader type if available, exception type. Do not log keys by default |

Register listeners with the GraphQL builder:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddDiagnosticEventListener<GraphQLFailureListener>()
    .AddDiagnosticEventListener<GraphQLServerFailureListener>()
    .AddDiagnosticEventListener<DataLoaderFailureListener>();
```

```csharp
// Diagnostics/GraphQLFailureListener.cs
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Resolvers;

namespace Catalog.Api.Diagnostics;

public sealed class GraphQLFailureListener : ExecutionDiagnosticEventListener
{
    private readonly ILogger<GraphQLFailureListener> _logger;

    public GraphQLFailureListener(ILogger<GraphQLFailureListener> logger)
    {
        _logger = logger;
    }

    public override void RequestError(RequestContext context, Exception error)
    {
        _logger.LogError(
            error,
            "GraphQL request failed for operation {OperationName} with document {DocumentHash}",
            context.Request.OperationName,
            FormatHash(context));
    }

    public override void ValidationErrors(
        RequestContext context,
        IReadOnlyList<IError> errors)
    {
        _logger.LogInformation(
            "GraphQL validation failed with {ErrorCount} errors. First code: {ErrorCode}",
            errors.Count,
            errors.Count > 0 ? errors[0].Code : null);
    }

    public override void ResolverError(IMiddlewareContext context, IError error)
    {
        _logger.LogError(
            error.Exception,
            "GraphQL resolver error {ErrorCode} at {Path} on {SchemaCoordinate}",
            error.Code,
            context.Path,
            context.Selection.Field.Coordinate);
    }

    public override void SubscriptionEventError(
        RequestContext context,
        ulong subscriptionId,
        Exception exception)
    {
        _logger.LogError(
            exception,
            "GraphQL subscription event {SubscriptionId} failed for operation {OperationName}",
            subscriptionId,
            context.Request.OperationName);
    }

    private static string? FormatHash(RequestContext context)
    {
        var hash = context.OperationDocumentInfo.Hash;
        return hash.IsEmpty ? null : $"{hash.AlgorithmName}:{hash.Value}";
    }
}
```

```csharp
// Diagnostics/GraphQLServerFailureListener.cs
using HotChocolate.AspNetCore.Instrumentation;
using Microsoft.AspNetCore.Http;

namespace Catalog.Api.Diagnostics;

public sealed class GraphQLServerFailureListener : ServerDiagnosticEventListener
{
    private readonly ILogger<GraphQLServerFailureListener> _logger;

    public GraphQLServerFailureListener(ILogger<GraphQLServerFailureListener> logger)
    {
        _logger = logger;
    }

    public override void HttpRequestError(HttpContext context, Exception exception)
    {
        _logger.LogError(
            exception,
            "GraphQL HTTP request failed for {Method} {Path}",
            context.Request.Method,
            context.Request.Path);
    }

    public override void ParserErrors(
        HttpContext context,
        IReadOnlyList<IError> errors)
    {
        _logger.LogInformation(
            "GraphQL HTTP request parsing failed with {ErrorCount} errors for {Path}",
            errors.Count,
            context.Request.Path);
    }
}
```

```csharp
// Diagnostics/DataLoaderFailureListener.cs
using GreenDonut;

namespace Catalog.Api.Diagnostics;

public sealed class DataLoaderFailureListener : DataLoaderDiagnosticEventListener
{
    private readonly ILogger<DataLoaderFailureListener> _logger;

    public DataLoaderFailureListener(ILogger<DataLoaderFailureListener> logger)
    {
        _logger = logger;
    }

    public override void BatchError<TKey>(
        IReadOnlyList<TKey> keys,
        Exception error)
    {
        _logger.LogError(
            error,
            "DataLoader batch failed with {BatchSize} keys",
            keys.Count);
    }

    public override void BatchItemError<TKey>(TKey key, Exception error)
    {
        _logger.LogWarning(
            error,
            "DataLoader item failed. Key value was not logged");
    }
}
```

Expected output separates the phase:

```text
Phase=Validation ErrorCount=2 ErrorCode=HC0011
Phase=Resolver Path=/viewer/orders/0/total SchemaCoordinate=Order.total ErrorCode=INTERNAL_ERROR
Phase=DataLoader BatchSize=50 ExceptionType=SqlException
```

Use `IErrorFilter` for final client error shaping. Use diagnostic listeners when operators need to know where the failure happened.

# Redact variables, documents, extensions, and PII

Treat every client-controlled value as sensitive until you have reviewed it.

| Field                                  | Production default                      | Reason                                                              |
| -------------------------------------- | --------------------------------------- | ------------------------------------------------------------------- |
| Operation name                         | Usually safe                            | Low cardinality when clients name operations well                   |
| Operation type                         | Usually safe                            | `query`, `mutation`, or `subscription`                              |
| Document hash                          | Usually safe                            | Useful for grouping without exposing the document                   |
| Trusted document ID                    | Usually safe when IDs are non-sensitive | Useful for persisted and trusted operations                         |
| Error code                             | Usually safe                            | Stable operational grouping field                                   |
| Error path and schema coordinate       | Usually safe                            | Identifies failing field without argument values                    |
| Raw document                           | Sensitive                               | Can contain literal argument values and private field names         |
| Variables                              | Sensitive                               | Often contain names, emails, tokens, search text, or IDs            |
| Request extensions                     | Sensitive                               | Client-controlled vendor data, included by `RequestDetails.Default` |
| Headers, cookies, authorization tokens | Sensitive                               | Authentication and session data                                     |
| DataLoader keys                        | Sensitive and high-cardinality          | Often contain database IDs or tenant data                           |
| Exception messages                     | Potentially sensitive                   | Can reveal SQL, URLs, file paths, or secrets                        |

For conservative production tracing, limit request details:

```csharp
using HotChocolate.Diagnostics;

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddInstrumentation(options =>
    {
        options.RequestDetails =
            RequestDetails.Id
            | RequestDetails.Hash
            | RequestDetails.OperationName;
    });
```

`RequestDetails.Default` includes `Id`, `Hash`, `OperationName`, and `Extensions`. Use the explicit allowlist above when request extensions may contain sensitive client data. `RequestDetails.All` includes variables and the document. Do not use it as a production default.

Use high-detail settings only behind an environment gate and for short-lived troubleshooting:

```csharp
using HotChocolate.Diagnostics;

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddInstrumentation(options =>
    {
        if (builder.Environment.IsDevelopment())
        {
            options.RequestDetails = RequestDetails.All;
            options.Scopes = ActivityScopes.All;
            options.IncludeDocument = true;
            options.IncludeDataLoaderKeys = true;
        }
        else
        {
            options.RequestDetails =
                RequestDetails.Id
                | RequestDetails.Hash
                | RequestDetails.OperationName;
            options.Scopes = ActivityScopes.Default;
            options.IncludeDocument = false;
            options.IncludeDataLoaderKeys = false;
        }
    });
```

`InstrumentationOptions.IncludeDocument` emits `graphql.document.body`. `InstrumentationOptions.IncludeDataLoaderKeys` emits `graphql.dataloader.batch.keys`. Keep both disabled unless the captured values are approved, bounded, and retained according to your data policy.

Client error masking is not server-log redaction. Redact before writing logs, scopes, span attributes, or span events.

# Send logs and traces to OpenTelemetry

Use OpenTelemetry logs for structured log export and Hot Chocolate instrumentation for GraphQL spans. Set `OTEL_EXPORTER_OTLP_ENDPOINT` according to your collector or vendor.

```csharp
// Program.cs
using HotChocolate.Diagnostics;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

var resource = ResourceBuilder
    .CreateDefault()
    .AddService(builder.Environment.ApplicationName);

builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
    logging.ParseStateValues = true;
    logging.SetResourceBuilder(resource);
    logging.AddOtlpExporter();
});

builder.Services
    .AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.SetResourceBuilder(resource);
        tracing.AddAspNetCoreInstrumentation();
        tracing.AddHttpClientInstrumentation();
        tracing.AddHotChocolateInstrumentation();
        tracing.AddOtlpExporter();
    });

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddDiagnosticEventListener<GraphQLLoggingListener>()
    .AddInstrumentation(options =>
    {
        options.RequestDetails =
            RequestDetails.Id
            | RequestDetails.Hash
            | RequestDetails.OperationName;
    });
```

Expected result in your observability backend:

```text
Log: GraphQL request completed in 42.7 ms
TraceId: 8b9f0f7d6a3c0e2a9f6a7d8e9c0b1a2f
SpanId: 3d2f5a7b9c1e0d4f
Properties: GraphQLOperationName=GetViewer, GraphQLDocumentHash=sha256:2f7c...

Trace: ASP.NET Core request span -> GraphQL Operation span -> resolver and DataLoader spans
```

`IncludeScopes` lets scope fields from `ILogger.BeginScope` travel with OpenTelemetry log records. `ParseStateValues` preserves named message-template properties when the log exporter supports structured state. Trace IDs and span IDs come from the active .NET `Activity`.

For detailed tracing setup, see [Instrumentation](/docs/hotchocolate/v16/server/instrumentation).

# Choose log levels, sampling, and noise controls

Use levels consistently so alerts mean the same thing across APIs.

| Event                                         | Recommended level              | Notes                                                                 |
| --------------------------------------------- | ------------------------------ | --------------------------------------------------------------------- |
| Startup and schema validation problem         | Error or Critical              | The service may fail to start or serve an invalid schema              |
| HTTP request failure before GraphQL execution | Warning or Error               | Base the level on whether the cause is client input or server failure |
| Syntax or parse error                         | Debug, Information, or Warning | Use Warning when tracking abuse or malformed traffic                  |
| Validation error                              | Debug or Information           | Use Warning only for suspicious volume or policy violations           |
| Expected domain error                         | Debug, Information, or no log  | Prefer typed schema errors for business outcomes                      |
| Unhandled request or resolver exception       | Error                          | Page or alert when it affects production users                        |
| Slow request                                  | Warning                        | Include threshold, elapsed time, operation name, hash or ID, trace ID |
| DataLoader batch failure                      | Error                          | Do not log keys unless they are approved for logs                     |

Control trace volume separately from log volume. OpenTelemetry sampling affects traces, not every `ILogger` record emitted by your application.

```csharp
builder.Services
    .AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.SetSampler(
            builder.Environment.IsDevelopment()
                ? new AlwaysOnSampler()
                : new ParentBasedSampler(new TraceIdRatioBasedSampler(0.05)));

        tracing.AddAspNetCoreInstrumentation();
        tracing.AddHttpClientInstrumentation();
        tracing.AddHotChocolateInstrumentation();
        tracing.AddOtlpExporter();
    });
```

Reduce GraphQL span detail when volume is high:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddInstrumentation(options =>
    {
        options.Scopes = ActivityScopes.Default;
        options.MaxErrorEvents = 3;
    });
```

`ActivityScopes.Default` includes HTTP request execution, HTTP request parsing, document validation, operation compilation, field resolver spans, HTTP response formatting, and DataLoader batch spans. `ActivityScopes.All` adds request execution, document parsing, complexity analysis, variable coercion, and operation execution spans.

Resolver and DataLoader spans can be high volume for large queries. `MaxErrorEvents` defaults to `10`. Set it to `0` to suppress root `graphql.error` events while retaining `graphql.error.count` when errors exist.

Add sampling or rate limiting inside custom listeners for repeated validation errors, abusive clients, or noisy expected domain failures.

# Diagnose slow resolvers and DataLoaders with logs linked to traces

Start with traces when you need resolver and DataLoader timing. Logs identify the slow operation. Traces show the execution tree.

```csharp
// Program.cs
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddDiagnosticEventListener<SlowGraphQLRequestListener>()
    .AddInstrumentation(options =>
    {
        options.Scopes =
            ActivityScopes.ExecuteHttpRequest
            | ActivityScopes.ExecuteRequest
            | ActivityScopes.ResolveFieldValue
            | ActivityScopes.DataLoaderBatch;
        options.RequestDetails =
            RequestDetails.Id
            | RequestDetails.Hash
            | RequestDetails.OperationName;
    });
```

```csharp
// Diagnostics/SlowGraphQLRequestListener.cs
using System.Diagnostics;
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;

namespace Catalog.Api.Diagnostics;

public sealed class SlowGraphQLRequestListener : ExecutionDiagnosticEventListener
{
    private static readonly TimeSpan s_threshold = TimeSpan.FromMilliseconds(500);
    private readonly ILogger<SlowGraphQLRequestListener> _logger;

    public SlowGraphQLRequestListener(ILogger<SlowGraphQLRequestListener> logger)
    {
        _logger = logger;
    }

    public override IDisposable ExecuteRequest(RequestContext context)
        => new SlowRequestScope(_logger, context, Stopwatch.GetTimestamp());

    private sealed class SlowRequestScope : IDisposable
    {
        private readonly ILogger _logger;
        private readonly RequestContext _context;
        private readonly long _startTimestamp;

        public SlowRequestScope(
            ILogger logger,
            RequestContext context,
            long startTimestamp)
        {
            _logger = logger;
            _context = context;
            _startTimestamp = startTimestamp;
        }

        public void Dispose()
        {
            var elapsed = Stopwatch.GetElapsedTime(_startTimestamp);
            if (elapsed <= s_threshold)
            {
                return;
            }

            _logger.LogWarning(
                "Slow GraphQL request {OperationName} with document {DocumentHash} took {ElapsedMilliseconds} ms. TraceId: {TraceId}",
                _context.Request.OperationName,
                FormatHash(_context),
                elapsed.TotalMilliseconds,
                Activity.Current?.TraceId.ToString());
        }

        private static string? FormatHash(RequestContext context)
        {
            var hash = context.OperationDocumentInfo.Hash;
            return hash.IsEmpty ? null : $"{hash.AlgorithmName}:{hash.Value}";
        }
    }
}
```

Expected log:

```text
Slow GraphQL request GetOrders with document sha256:91ab... took 847.3 ms. TraceId: 8b9f0f7d6a3c0e2a9f6a7d8e9c0b1a2f
```

Use this workflow:

1. Find the slow log by `GraphQLDocumentHash`, operation name, or route.
2. Open the trace by `TraceId`.
3. Inspect `ResolveFieldValue` spans for slow fields.
4. Inspect `DataLoaderBatch` spans for large or slow batches.
5. Add application logs inside expensive resolvers or services with named placeholders and no sensitive payloads.

Turn targeted field-level and DataLoader tracing off after the investigation if the volume is too high.

See [Performance Tuning](/docs/hotchocolate/v16/guides/performance), [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader), [Trusted Documents](/docs/hotchocolate/v16/performance/trusted-documents), and [Automatic Persisted Operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations).

# Reference: diagnostic hooks and structured fields

Use this table when you decide where a logging requirement belongs.

| Hook                     | When it fires                                   | Level guideline                    | Safe fields                                                             |
| ------------------------ | ----------------------------------------------- | ---------------------------------- | ----------------------------------------------------------------------- |
| `ExecuteHttpRequest`     | GraphQL over HTTP request starts                | Debug or scope only                | HTTP method, path, trace ID                                             |
| `HttpRequestError`       | HTTP transport processing fails                 | Warning or Error                   | HTTP method, path, exception type                                       |
| `ParseHttpRequest`       | HTTP body or query string parsing starts        | Debug or scope only                | Trace ID                                                                |
| `ParserErrors`           | HTTP GraphQL request parsing fails              | Information or Warning             | Error count, first error code                                           |
| `ExecuteRequest`         | GraphQL request execution starts                | Scope or Information on completion | Operation name, request index, document hash, document ID, elapsed time |
| `RequestError`           | Request execution reports an exception or error | Error for exceptions               | Operation name, document hash or ID, exception type, error code         |
| `ValidationErrors`       | Document validation fails                       | Debug, Information, or Warning     | Error count, first error code, document hash                            |
| `ResolverError`          | Field resolver reports an error                 | Error for unexpected exceptions    | Path, schema coordinate, error code, exception type                     |
| `SubscriptionEventError` | Subscription event result creation fails        | Error                              | Subscription ID, operation name, exception type                         |
| `ExecuteBatch`           | DataLoader batch starts                         | Debug or trace span                | DataLoader type, batch size                                             |
| `BatchError`             | Entire DataLoader batch fails                   | Error                              | Batch size, exception type                                              |
| `BatchItemError`         | One DataLoader item fails                       | Warning or Error                   | Exception type. Avoid key values                                        |

Use these structured fields consistently:

| Field                                | Classification                 | Notes                                                          |
| ------------------------------------ | ------------------------------ | -------------------------------------------------------------- |
| `TraceId`, `SpanId`                  | Safe by default                | Required for log-to-trace navigation                           |
| HTTP method, status, route path      | Safe by default                | Avoid full URLs with query strings when they contain variables |
| Request index                        | Safe by default                | Useful for batched GraphQL requests                            |
| Operation name                       | Usually safe                   | Encourage named operations from clients                        |
| Operation type                       | Safe by default                | Available on OpenTelemetry spans as `graphql.operation.type`   |
| Document hash                        | Safe by default                | Prefer over raw document text                                  |
| Trusted document ID                  | Usually safe                   | Ensure IDs do not encode user data                             |
| Persisted, cached, validated flags   | Safe by default                | Useful for cache and persisted operation diagnosis             |
| Error code and error count           | Safe by default                | Avoid raw exception messages in indexed properties             |
| Error path and schema coordinate     | Safe by default                | Shows failing field without argument values                    |
| DataLoader batch size                | Safe by default                | Useful for tuning                                              |
| DataLoader keys                      | Sensitive and high-cardinality | Keep disabled unless approved                                  |
| Variables, document body, extensions | Sensitive                      | Do not log by default                                          |

For the full diagnostic event and OpenTelemetry reference, see [Instrumentation](/docs/hotchocolate/v16/server/instrumentation).

# Troubleshoot missing logs or trace links

Use this checklist when you see HTTP logs but not GraphQL operation details.

| Symptom                                                 | Check                                                                                                                                                        |
| ------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Only ASP.NET Core HTTP logs appear                      | Add a Hot Chocolate diagnostic listener or `.AddInstrumentation()` for GraphQL lifecycle telemetry                                                           |
| Custom listener never runs                              | Register `.AddDiagnosticEventListener<T>()` on the same GraphQL builder that configures the mapped endpoint                                                  |
| Listener logs do not include scope fields               | Enable scopes in your provider. For OpenTelemetry logs, set `IncludeScopes = true`                                                                           |
| Named placeholders are flattened into message text only | For OpenTelemetry logs, set `ParseStateValues = true` and verify backend support                                                                             |
| No Hot Chocolate spans appear                           | Install `HotChocolate.Diagnostics`, call `.AddInstrumentation()` on the GraphQL builder, and call `.AddHotChocolateInstrumentation()` on the tracing builder |
| Trace IDs are missing from logs                         | Ensure there is an active `Activity`, usually from ASP.NET Core or OpenTelemetry tracing instrumentation                                                     |
| Spans are created locally but not exported              | Check `OTEL_EXPORTER_OTLP_ENDPOINT`, exporter package, credentials, network policy, and collector health                                                     |
| Resolver spans are missing                              | Confirm `ActivityScopes.ResolveFieldValue` is enabled. It is included in `ActivityScopes.Default` for OpenTelemetry instrumentation                          |
| DataLoader spans are missing                            | Confirm `ActivityScopes.DataLoaderBatch` is enabled and the request uses DataLoaders                                                                         |
| Logs are delayed or dropped                             | Check provider batching, exporter backpressure, and application shutdown flushing                                                                            |

Verification query:

```graphql
query GetViewer {
  viewer {
    id
    name
  }
}
```

Expected telemetry:

```text
HTTP log: POST /graphql 200
Application log: GraphQL request completed in N ms, GraphQLOperationName=GetViewer
Trace attributes: graphql.operation.name=GetViewer, graphql.document.hash=sha256:...
```

Diagnostic listeners run synchronously. If adding a listener changes request latency, remove expensive work from the handler and move it to a queue or background service.

# Troubleshoot excessive or sensitive logs

If production logs contain sensitive data, stop the source first. Then follow your organization's incident process for retention, rotation, purge, and disclosure.

Immediate rollback checklist:

1. Set `IncludeExceptionDetails` to `builder.Environment.IsDevelopment()` or `false`.
2. Remove `RequestDetails.All`.
3. Use `RequestDetails.Id | RequestDetails.Hash | RequestDetails.OperationName`, or `RequestDetails.None` for the most restrictive trace mode.
4. Set `IncludeDocument = false`.
5. Set `IncludeDataLoaderKeys = false`.
6. Remove variables, extensions, headers, cookies, authorization tokens, raw documents, and DataLoader keys from custom log scopes.
7. Reduce `ActivityScopes.All` to `ActivityScopes.Default` or a smaller targeted set.
8. Lower `MaxErrorEvents`, or set it to `0` when root `graphql.error` span events overwhelm the backend.
9. Lower expected domain and client errors to Debug or Information, or stop logging them.
10. Sample repeated validation errors and rate-limit abusive clients separately.
11. Ensure error filters call `.WithException(null)` before errors are serialized to clients.

Before:

```csharp
builder
    .AddGraphQL()
    .AddInstrumentation(options =>
    {
        options.RequestDetails = RequestDetails.All;
        options.Scopes = ActivityScopes.All;
        options.IncludeDocument = true;
        options.IncludeDataLoaderKeys = true;
    })
    .ModifyRequestOptions(options =>
    {
        options.IncludeExceptionDetails = true;
    });
```

After:

```csharp
builder
    .AddGraphQL()
    .AddInstrumentation(options =>
    {
        options.RequestDetails =
            RequestDetails.Id
            | RequestDetails.Hash
            | RequestDetails.OperationName;
        options.Scopes = ActivityScopes.Default;
        options.IncludeDocument = false;
        options.IncludeDataLoaderKeys = false;
        options.MaxErrorEvents = 3;
    })
    .ModifyRequestOptions(options =>
    {
        options.IncludeExceptionDetails = builder.Environment.IsDevelopment();
    });
```

If noisy validation errors or expensive operations drive the log volume, also review [Request Limits](/docs/hotchocolate/v16/securing-your-api/request-limits) and [Cost Analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis).

# Next steps

- Use [Instrumentation](/docs/hotchocolate/v16/server/instrumentation) for the full diagnostic event and OpenTelemetry tracing reference.
- Use [Error Handling](/docs/hotchocolate/v16/guides/error-handling) and [Errors](/docs/hotchocolate/v16/api-reference/errors) for error filters and client error shaping.
- Use [HTTP Transport](/docs/hotchocolate/v16/server/http-transport) when troubleshooting transport behavior.
- Use [Performance Tuning](/docs/hotchocolate/v16/guides/performance) and [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader) when slow logs point to resolver or batching work.
- Use [Trusted Documents](/docs/hotchocolate/v16/performance/trusted-documents) and [Automatic Persisted Operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations) to reduce document exposure.
- Use [Request Limits](/docs/hotchocolate/v16/securing-your-api/request-limits) and [Cost Analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis) for noisy clients and expensive operations.
