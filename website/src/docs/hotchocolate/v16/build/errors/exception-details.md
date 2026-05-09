---
title: Exception details
---

When a .NET exception occurs unexpectedly on the server, Hot Chocolate keeps exception messages and stack traces out of the GraphQL response by default. This protects sensitive information from being exposed to clients. You have options for local debugging, redaction, logging, and testing.

This page covers how to handle unexpected resolver or execution exceptions. If an error is part of normal application flow, use a client-safe error code, a typed domain error, mutation conventions, or another schema contract instead of exposing exception details.

# What happens when a resolver throws?

Consider this resolver, which throws a standard .NET exception:

```csharp
[QueryType]
public static partial class ProductQueries
{
    public static string? GetProductName([ID] int id)
    {
        throw new InvalidOperationException("Demo resolver failure.");
    }
}
```

When this exception is thrown, Hot Chocolate processes it as follows:

1. The resolver or execution code throws an exception.
2. Hot Chocolate creates an `IError` with the message `Unexpected Execution Error`.
3. The original exception is attached to `IError.Exception` on the server.
4. Registered `IErrorFilter` instances can inspect and transform the error.
5. If `IncludeExceptionDetails` is enabled and the error still has an exception, Hot Chocolate adds debug details to `extensions.exception`.
6. The response is serialized with GraphQL fields such as `message`, `locations`, `path`, `extensions`, and `data` nulling.

With exception details disabled, the client receives a redacted response:

```json
{
  "data": {
    "productName": null
  },
  "errors": [
    {
      "message": "Unexpected Execution Error",
      "locations": [{ "line": 2, "column": 3 }],
      "path": ["productName"]
    }
  ]
}
```

The exact shape of `data` depends on GraphQL null propagation. If a nullable field fails, it becomes `null` and sibling fields continue. If a non-null field fails, the null can propagate and cause `data` to become `null`.

# Configuring exception detail visibility

You control exception detail visibility with the `IncludeExceptionDetails` option, set via `ModifyRequestOptions`:

```csharp
builder
    .AddGraphQL()
    .ModifyRequestOptions(options =>
    {
        options.IncludeExceptionDetails = builder.Environment.IsDevelopment();
    });
```

If you do not set this option, Hot Chocolate v16 defaults to `Debugger.IsAttached`. This is helpful for local debugging, but for production, use an explicit policy so that local development, CI, staging, and production environments behave predictably.

For production, always disable exception details:

```csharp
builder
    .AddGraphQL()
    .ModifyRequestOptions(options =>
    {
        options.IncludeExceptionDetails = false;
    });
```

> Warning: Never enable exception details in production. Exception messages and stack traces can reveal type names, file paths, SQL, downstream responses, tenant data, tokens, personal data, or other implementation details.

# What appears when details are enabled?

When `IncludeExceptionDetails` is `true`, Hot Chocolate adds an `exception` object under `extensions` for errors that still have `IError.Exception` set:

```json
{
  "data": {
    "productName": null
  },
  "errors": [
    {
      "message": "Unexpected Execution Error",
      "locations": [{ "line": 2, "column": 3 }],
      "path": ["productName"],
      "extensions": {
        "exception": {
          "message": "Demo resolver failure.",
          "stackTrace": "at ProductQueries.GetProductName(Int32 id) in ..."
        }
      }
    }
  ]
}
```

The debug extension includes:

| Field                             | Meaning                                                                             |
| --------------------------------- | ----------------------------------------------------------------------------------- |
| `extensions.exception.message`    | The original .NET exception message.                                                |
| `extensions.exception.stackTrace` | The .NET stack trace. This value can be `null` if the exception has no stack trace. |

Clients should not depend on `extensions.exception`. Treat it as diagnostic output for development only.

# Redacting exceptions with an error filter

An error filter can log the original exception, return a safe message, add a stable code, and remove the exception before debug details are serialized.

```csharp
public sealed class GraphQLErrorLogger
{
    private readonly ILogger<GraphQLErrorLogger> _logger;

    public GraphQLErrorLogger(ILogger<GraphQLErrorLogger> logger)
    {
        _logger = logger;
    }

    public void LogUnhandled(Exception exception)
    {
        _logger.LogError(exception, "Unhandled GraphQL exception.");
    }
}

public sealed class SafeErrorFilter : IErrorFilter
{
    private readonly GraphQLErrorLogger _logger;

    public SafeErrorFilter(GraphQLErrorLogger logger)
    {
        _logger = logger;
    }

    public IError OnError(IError error)
    {
        if (error.Exception is null)
        {
            return error;
        }

        _logger.LogUnhandled(error.Exception);

        return error
            .WithMessage("An internal error occurred.")
            .WithCode("INTERNAL_ERROR")
            .WithException(null);
    }
}
```

Register the application service and make it available to schema services before registering the filter:

```csharp
builder.Services.AddSingleton<GraphQLErrorLogger>();

builder
    .AddGraphQL()
    .AddApplicationService<GraphQLErrorLogger>()
    .AddErrorFilter<SafeErrorFilter>()
    .ModifyRequestOptions(options =>
    {
        options.IncludeExceptionDetails = builder.Environment.IsDevelopment();
    });
```

In v16, class error filters are schema-level services. Constructor dependencies from the application service provider must be cross-registered with `AddApplicationService<T>()`, or supplied by a documented factory pattern. Services added this way are resolved during schema initialization and used as singletons in schema services.

Calling `WithException(null)` is intentional. It prevents later debug serialization for that error, but later filters will not see the exception through `error.Exception`. Log or capture what you need before clearing it.

# Adding safe client metadata

Clients typically need stable error metadata, not stack traces. Prefer a safe message, `extensions.code`, and optional small identifiers that your support process can use:

```json
{
  "data": {
    "productName": null
  },
  "errors": [
    {
      "message": "An internal error occurred.",
      "locations": [{ "line": 2, "column": 3 }],
      "path": ["productName"],
      "extensions": {
        "code": "INTERNAL_ERROR",
        "correlationId": "00-4bf92f3577b34da6a3ce929d0e0e4736"
      }
    }
  ]
}
```

Only expose values that are safe for the current user and stable enough to document. Avoid exposing raw exception messages, exception type names, stack traces, file paths, SQL, connection strings, environment variables, tokens, tenant identifiers, personal data, or raw downstream service responses.

# Error filters, logging, and diagnostics

Error filters shape the GraphQL error before serialization. Registered filters run in order, and each filter receives the output of the previous one. In the v16 server pipeline, the built-in debug filter is added when `IncludeExceptionDetails` is enabled, so a custom filter can sanitize an error before debug details are appended.

Use filters to shape responses. Use logging and diagnostics for operational monitoring:

```csharp
public sealed class GraphQLDiagnostics : ExecutionDiagnosticEventListener
{
    private readonly ILogger<GraphQLDiagnostics> _logger;

    public GraphQLDiagnostics(ILogger<GraphQLDiagnostics> logger)
    {
        _logger = logger;
    }

    public override void RequestError(RequestContext context, Exception exception)
    {
        _logger.LogError(exception, "A GraphQL request error occurred.");
    }
}
```

```csharp
builder
    .AddGraphQL()
    .AddDiagnosticEventListener<GraphQLDiagnostics>();
```

For traces, Hot Chocolate's instrumentation includes OpenTelemetry enrichment hooks such as `EnrichRequestError(RequestContext, Exception, Activity)` and `EnrichResolverError(IMiddlewareContext, IError, Activity)`. Only send full operation documents, variables, exception details, or personal data to telemetry systems if your data-handling policy allows it.

# Testing redaction and debug output

Lock down both policies with tests. Use snapshot testing for the full GraphQL response when possible.

```csharp
[Fact]
public async Task ProductName_Should_Redact_Exception_When_Details_Are_Disabled()
{
    // arrange
    var executor = await new ServiceCollection()
        .AddGraphQL()
        .AddQueryType<ProductQueries>()
        .ModifyRequestOptions(options =>
        {
            options.IncludeExceptionDetails = false;
        })
        .BuildRequestExecutorAsync();

    // act
    var result = await executor.ExecuteAsync("{ productName(id: 1) }");

    // assert
    result.MatchInlineSnapshot(
        """
        {
          "errors": [
            {
              "message": "Unexpected Execution Error",
              "locations": [
                {
                  "line": 1,
                  "column": 3
                }
              ],
              "path": [
                "productName"
              ]
            }
          ],
          "data": {
            "productName": null
          }
        }
        """);
}
```

Add a second test with `IncludeExceptionDetails = true` to verify local diagnostic output. Assert or snapshot that `extensions.exception.message` is present. If your production filter calls `WithException(null)`, test that `extensions.exception` is absent even when details are enabled.

# Troubleshooting

## I still see `Unexpected Execution Error`

This is expected when exception details are disabled or a filter has sanitized the error. Check server logs, diagnostic listeners, and traces for the original exception. Enable details only in a local development environment when you need the stack trace in the GraphQL response.

## Details do not appear in development

Check the following:

- The executor that handled the request has `IncludeExceptionDetails = true`.
- The process is running in the expected environment when you use `builder.Environment.IsDevelopment()`.
- A custom error filter did not call `WithException(null)` before debug details were added.
- The error has an attached exception. Manually built errors need `SetException(...)` or `ErrorBuilder.FromException(...)` if they are expected to carry exception details.
- You configured the same schema or executor that received the request.

## Details appear in production

Search your application and tests for unconditional `IncludeExceptionDetails = true`. Replace it with environment-gated configuration or explicit `false`. Check each executor separately, including copied sample code, background services, and any gateway or subgraph host you own. Also check custom filters for unsafe extensions that copy exception messages or stack traces manually.

## My error filter cannot use my service

In v16, schema-level components such as error filters and diagnostic listeners use schema services. If a filter constructor needs an application service, register that service with `AddApplicationService<T>()`. Avoid using scoped request services in filter constructors because the schema service instance is created during schema initialization.

## Clients need actionable errors

Do not enable exception details to address this. Add stable `extensions.code` values, safe guidance text, or a support correlation ID. For expected business outcomes, model the outcome in the schema with typed domain errors, mutation conventions, `FieldResult`, or another client-safe contract.

## TypeConverter exceptions reach my filter

Hot Chocolate v16 propagates TypeConverter exceptions to error filters. Handle them like other unexpected exceptions: log them internally, return a safe message and code, and avoid exposing converter exception details in production.

# What is safe to expose?

| Usually safe                        | Avoid exposing                                                       |
| ----------------------------------- | -------------------------------------------------------------------- |
| Generic messages                    | Raw exception messages                                               |
| Stable error codes                  | Stack traces                                                         |
| Correlation or request IDs          | File paths, type names, method names                                 |
| Public support or retry guidance    | SQL, connection strings, environment variables                       |
| Public validation or domain details | Tokens, secrets, tenant IDs, personal data, raw downstream responses |

What is "safe" depends on your application's data classification and compliance requirements. Keep full details in logs and traces with appropriate access controls, not in the client response.

# Next steps

- [Error filters](/docs/hotchocolate/v16/build/errors/error-filters) for response transformations, ordering, and dependency injection.
- [Error builder](/docs/hotchocolate/v16/build/errors/error-builder) for constructing client-safe `IError` values.
- [Instrumentation](/docs/hotchocolate/v16/build/observability) for diagnostic listeners and OpenTelemetry.
- [Options reference](/docs/hotchocolate/v16/build/server-configuration/schema-options) for `ModifyRequestOptions` and `IncludeExceptionDetails`.
- [Error handling guide](/docs/hotchocolate/v16/_leagcy/guides/error-handling) for broader request and domain error patterns.
