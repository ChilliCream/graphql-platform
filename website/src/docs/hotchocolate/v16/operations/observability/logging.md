---
title: Production Logging
---

This guide explains how to set up safe, effective logging for Hot Chocolate v16 in production. You will use the standard ASP.NET Core logging pipeline for host and application logs, and add Hot Chocolate diagnostic listeners or OpenTelemetry instrumentation to capture GraphQL-specific context such as requests, resolvers, validation, and DataLoader activity.

Fusion gateway diagnostics use different instrumentation and are not covered here.

# Prerequisites

Start by choosing the setup that matches your needs:

| Goal                              | Required packages                                               | How to configure                                                                                                     |
| --------------------------------- | --------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------- |
| ASP.NET Core and application logs | Your usual ASP.NET Core logging providers                       | Configure `Logging` in `appsettings.json`, use `ILogger<T>` in your services                                         |
| GraphQL request lifecycle logs    | Hot Chocolate server packages                                   | Register `ExecutionDiagnosticEventListener`, `ServerDiagnosticEventListener`, or `DataLoaderDiagnosticEventListener` |
| Logs correlated with traces       | `HotChocolate.Diagnostics`, OpenTelemetry packages, an exporter | Use `.AddInstrumentation()`, `AddOpenTelemetry`, and `AddHotChocolateInstrumentation()`                              |

For OpenTelemetry, add the following packages:

```bash
dotnet add package HotChocolate.Diagnostics
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Instrumentation.AspNetCore
dotnet add package OpenTelemetry.Instrumentation.Http
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
```

You will also need access to `Program.cs` and your logging configuration, typically in `appsettings.json`.

# Setting a Safe Production Logging Baseline

Begin with standard ASP.NET Core logging and server-side exception logging. In production, do not expose exception details in GraphQL responses.

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

When an unhandled resolver exception occurs, the client receives:

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

The server log will contain the exception type and stack trace, while the GraphQL response returns a stable, safe message and code. Do not log variables, raw documents, headers, cookies, or authorization tokens in this baseline.

For more on error handling and client-facing error models, see the [Error Handling](/docs/hotchocolate/v16/guides/error-handling), [Errors](/docs/hotchocolate/v16/api-reference/errors), and [Options Reference](/docs/hotchocolate/v16/api-reference/options) pages.

# What Hot Chocolate Logs by Default

Hot Chocolate v16 does not require a special logging provider. ASP.NET Core request logging continues to record HTTP method, path, status code, routing failures, and unhandled pipeline exceptions for the `/graphql` endpoint.

GraphQL operation details—such as operation names, document hashes, validation failures, resolver timing, and DataLoader batches—are available through Hot Chocolate diagnostic events and OpenTelemetry activities. Setting a category filter like `Logging:LogLevel:HotChocolate` will not produce a stream of GraphQL request lifecycle logs.

Schema validation may produce internal log entries (e.g., `HCV0001`) during schema construction, but these are not part of the production request telemetry.

Continue to use category filters for your existing host and application logs:

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

If you need GraphQL operation fields in your logs, add a diagnostic listener or OpenTelemetry instrumentation.

# Adding GraphQL Request Context to Log Scopes

To include safe GraphQL identifiers in your log scopes, use an execution diagnostic listener. The scope remains active for the duration of the GraphQL request.

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

This produces structured log properties like:

```text
Message="GraphQL request completed in 42.7 ms"
TraceId="8b9f0f7d6a3c0e2a9f6a7d8e9c0b1a2f"
GraphQLOperationName="GetViewer"
GraphQLDocumentHash="sha256:2f7c..."
GraphQLDocumentId="GetViewer"
ElapsedMilliseconds=42.7
```

Diagnostic listeners are created once, and their handlers run synchronously during request execution. Keep listener logic lightweight, use structured fields, and offload expensive work to background services. Avoid logging raw documents, variables, extensions, or headers in production.

The operation type is available as the `graphql.operation.type` OpenTelemetry attribute after Hot Chocolate resolves the operation. Prefer OpenTelemetry spans for this field unless your custom listener records it later in the pipeline.

# Logging Failures: Requests, Validation, Resolvers, Subscriptions, and DataLoaders

Choose the diagnostic hook that matches the failure phase. This approach keeps alerts actionable and prevents client mistakes from being treated as server incidents.

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

You can expect output like:

```text
Phase=Validation ErrorCount=2 ErrorCode=HC0011
Phase=Resolver Path=/viewer/orders/0/total SchemaCoordinate=Order.total ErrorCode=INTERNAL_ERROR
Phase=DataLoader BatchSize=50 ExceptionType=SqlException
```

Use `IErrorFilter` to shape client errors, and diagnostic listeners to inform operators where failures occur.

# Redacting Variables, Documents, Extensions, and PII

Treat all client-controlled values as sensitive until you have reviewed them.

| Field                                  | Production default                      | Reason                                                              |
| -------------------------------------- | --------------------------------------- | ------------------------------------------------------------------- |
| Operation name                         | Usually safe                            | Low cardinality if clients use descriptive names                    |
| Operation type                         | Usually safe                            | `query`, `mutation`, or `subscription`                              |
| Document hash                          | Usually safe                            | Useful for grouping without exposing the document                   |
| Trusted document ID                    | Usually safe when IDs are non-sensitive | Useful for persisted and trusted operations                         |
| Error code                             | Usually safe                            | Stable for operational grouping                                     |
| Error path and schema coordinate       | Usually safe                            | Identifies failing field without argument values                    |
| Raw document                           | Sensitive                               | May contain literal argument values and private field names         |
| Variables                              | Sensitive                               | Often include names, emails, tokens, search text, or IDs            |
| Request extensions                     | Sensitive                               | Client-controlled vendor data, included by `RequestDetails.Default` |
| Headers, cookies, authorization tokens | Sensitive                               | Authentication and session data                                     |
| DataLoader keys                        | Sensitive and high-cardinality          | Often contain database IDs or tenant data                           |
| Exception messages                     | Potentially sensitive                   | Can reveal SQL, URLs, file paths, or secrets                        |

For conservative production tracing, restrict request details:

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

`RequestDetails.Default` includes `Id`, `Hash`, `OperationName`, and `Extensions`. Use the explicit allowlist above if request extensions may contain sensitive data. `RequestDetails.All` includes variables and the document—do not use this as a production default.

Use high-detail settings only behind an environment gate and for short-term troubleshooting:

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

`InstrumentationOptions.IncludeDocument` emits `graphql.document.body`. `InstrumentationOptions.IncludeDataLoaderKeys` emits `graphql.dataloader.batch.keys`. Keep both disabled unless you have reviewed, bounded, and retained the captured values according to your data policy.

Masking errors for clients does not redact server logs. Always redact before writing logs, scopes, span attributes, or span events.

# Sending Logs and Traces to OpenTelemetry

Use OpenTelemetry logs for structured log export, and Hot Chocolate instrumentation for GraphQL spans. Set `OTEL_EXPORTER_OTLP_ENDPOINT` to match your collector or vendor.

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

In your observability backend, you should see:

```text
Log: GraphQL request completed in 42.7 ms
TraceId: 8b9f0f7d6a3c0e2a9f6a7d8e9c0b1a2f
SpanId: 3d2f5a7b9c1e0d4f
Properties: GraphQLOperationName=GetViewer, GraphQLDocumentHash=sha256:2f7c...

Trace: ASP.NET Core request span -> GraphQL Operation span -> resolver and DataLoader spans
```

`IncludeScopes` allows scope fields from `ILogger.BeginScope` to travel with OpenTelemetry log records. `ParseStateValues` preserves named message-template properties when supported by the log exporter. Trace and span IDs come from the active .NET `Activity`.

# Choosing Log Levels, Sampling, and Noise Controls

Use log levels consistently so alerts have the same meaning across your APIs.

| Event                                         | Recommended level              | Notes                                                                 |
| --------------------------------------------- | ------------------------------ | --------------------------------------------------------------------- |
| Startup and schema validation problem         | Error or Critical              | The service may fail to start or serve an invalid schema              |
| HTTP request failure before GraphQL execution | Warning or Error               | Level depends on whether the cause is client input or server failure  |
| Syntax or parse error                         | Debug, Information, or Warning | Use Warning for abuse or malformed traffic                            |
| Validation error                              | Debug or Information           | Use Warning only for suspicious volume or policy violations           |
| Expected domain error                         | Debug, Information, or no log  | Prefer typed schema errors for business outcomes                      |
| Unhandled request or resolver exception       | Error                          | Page or alert when it affects production users                        |
| Slow request                                  | Warning                        | Include threshold, elapsed time, operation name, hash or ID, trace ID |
| DataLoader batch failure                      | Error                          | Do not log keys unless approved for logs                              |

Control trace volume separately from log volume. OpenTelemetry sampling affects traces, not every `ILogger` record your application emits.

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

Reduce GraphQL span detail if volume is high:

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

# Diagnosing Slow Resolvers and DataLoaders with Logs and Traces

When you need resolver and DataLoader timing, start with traces. Logs help you identify the slow operation, while traces show the execution tree.

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

A typical log entry looks like:

```text
Slow GraphQL request GetOrders with document sha256:91ab... took 847.3 ms. TraceId: 8b9f0f7d6a3c0e2a9f6a7d8e9c0b1a2f
```

To investigate:

1. Find the slow log by `GraphQLDocumentHash`, operation name, or route.
2. Open the trace using the `TraceId`.
3. Inspect `ResolveFieldValue` spans for slow fields.
4. Inspect `DataLoaderBatch` spans for large or slow batches.
5. Add application logs inside expensive resolvers or services, using named placeholders and no sensitive payloads.

After your investigation, turn off targeted field-level and DataLoader tracing if the volume is too high.

See [Performance Tuning](/docs/hotchocolate/v16/guides/performance), [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader), [Trusted Documents](/docs/hotchocolate/v16/performance/trusted-documents), and [Automatic Persisted Operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations) for more.

# Reference: Diagnostic Hooks and Structured Fields

Use this table to decide where to log specific events:

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

For a full reference of diagnostic events and OpenTelemetry, see [Instrumentation](/docs/hotchocolate/v16/server/instrumentation).

# Troubleshooting: Missing Logs or Trace Links

If you see HTTP logs but not GraphQL operation details, use this checklist:

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

To verify, run this query:

```graphql
query GetViewer {
  viewer {
    id
    name
  }
}
```

You should see telemetry like:

```text
HTTP log: POST /graphql 200
Application log: GraphQL request completed in N ms, GraphQLOperationName=GetViewer
Trace attributes: graphql.operation.name=GetViewer, graphql.document.hash=sha256:...
```

Diagnostic listeners run synchronously. If adding a listener increases request latency, move expensive work to a queue or background service.

# Troubleshooting: Excessive or Sensitive Logs

If you find sensitive data in production logs, stop the source immediately. Then follow your organization's incident process for retention, rotation, purging, and disclosure.

Immediate rollback steps:

1. Set `IncludeExceptionDetails` to `builder.Environment.IsDevelopment()` or `false`.
2. Remove `RequestDetails.All`.
3. Use `RequestDetails.Id | RequestDetails.Hash | RequestDetails.OperationName`, or `RequestDetails.None` for the strictest trace mode.
4. Set `IncludeDocument = false`.
5. Set `IncludeDataLoaderKeys = false`.
6. Remove variables, extensions, headers, cookies, authorization tokens, raw documents, and DataLoader keys from custom log scopes.
7. Reduce `ActivityScopes.All` to `ActivityScopes.Default` or a smaller targeted set.
8. Lower `MaxErrorEvents`, or set it to `0` if root `graphql.error` span events overwhelm your backend.
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

If noisy validation errors or expensive operations drive log volume, also review [Request Limits](/docs/hotchocolate/v16/securing-your-api/request-limits) and [Cost Analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis).

# Next Steps

- See [Instrumentation](/docs/hotchocolate/v16/server/instrumentation) for the full diagnostic event and OpenTelemetry tracing reference.
- See [Error Handling](/docs/hotchocolate/v16/guides/error-handling) and [Errors](/docs/hotchocolate/v16/api-reference/errors) for error filters and client error shaping.
- See [HTTP Transport](/docs/hotchocolate/v16/server/http-transport) for troubleshooting transport behavior.
- See [Performance Tuning](/docs/hotchocolate/v16/guides/performance) and [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader) when slow logs point to resolver or batching work.
- See [Trusted Documents](/docs/hotchocolate/v16/performance/trusted-documents) and [Automatic Persisted Operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations) to reduce document exposure.
- See [Request Limits](/docs/hotchocolate/v16/securing-your-api/request-limits) and [Cost Analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis) for noisy clients and expensive operations.
