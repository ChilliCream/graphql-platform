---
title: Exception details
---

Unexpected .NET exceptions are server-side failures. A GraphQL response is client-visible, so Hot Chocolate keeps exception messages and stack traces out of normal responses and gives you controls for local debugging, redaction, logging, and testing.

Use this page for unexpected resolver or execution exceptions. If the client should treat the outcome as part of normal application flow, map it to a client-safe error code, a typed domain error, mutation conventions, or another schema contract instead of exposing exception details.

# What happens when a resolver throws?

The following resolver throws an ordinary .NET exception:

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

During execution Hot Chocolate handles the failure in this order:

1. The resolver or execution code throws an exception.
2. Hot Chocolate creates an `IError` with the message `Unexpected Execution Error`.
3. The original exception is attached to `IError.Exception` on the server.
4. Registered `IErrorFilter` instances can inspect and transform the error.
5. When `IncludeExceptionDetails` is enabled and the error still has an exception, Hot Chocolate adds debug details to `extensions.exception`.
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

The exact `data` shape depends on GraphQL null propagation. A nullable failed field can become `null` while sibling fields continue. A non-null failure can propagate farther and can make `data` become `null`.

# Configure exception detail visibility

`IncludeExceptionDetails` is a `bool` request option configured with `ModifyRequestOptions`:

```csharp
builder
    .AddGraphQL()
    .ModifyRequestOptions(options =>
    {
        options.IncludeExceptionDetails = builder.Environment.IsDevelopment();
    });
```

If you do not configure the option, Hot Chocolate v16 uses `Debugger.IsAttached` as the default. That default is useful for local debugging, but production services should use an explicit policy so local development, CI, staging, and production are predictable.

For production, keep details disabled:

```csharp
builder
    .AddGraphQL()
    .ModifyRequestOptions(options =>
    {
        options.IncludeExceptionDetails = false;
    });
```

> Warning: Do not enable exception details in production. Exception messages and stack traces can expose type names, file paths, SQL, downstream responses, tenant data, tokens, personal data, or other implementation details.

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

The debug extension contains:

| Field                             | Meaning                                                                               |
| --------------------------------- | ------------------------------------------------------------------------------------- |
| `extensions.exception.message`    | The original .NET exception message.                                                  |
| `extensions.exception.stackTrace` | The .NET stack trace. This value can be `null` when the exception has no stack trace. |

Do not design clients to depend on `extensions.exception`. Treat it as development-only diagnostic output.

# Redact exceptions with an error filter

An error filter can log the original exception, return a safe message, add a stable code, and remove the exception before the debug detail filter can serialize it.

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

In v16, class error filters are schema-level services. Constructor dependencies that come from the application service provider must be cross-registered with `AddApplicationService<T>()`, or supplied by a documented factory pattern. Services added this way are resolved during schema initialization and used as singletons in schema services.

`WithException(null)` is an intentional tradeoff. It prevents later debug serialization for that error, but later filters will not see the exception through `error.Exception`. Log or capture what you need before clearing it.

# Add safe client metadata

Clients usually need stable error metadata, not stack traces. Prefer a safe message, `extensions.code`, and optional small identifiers that your support process understands:

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

Only expose values that are safe for the current user and stable enough to document. Avoid raw exception messages, exception type names, stack traces, file paths, SQL, connection strings, environment variables, tokens, tenant identifiers, personal data, and raw downstream service responses.

# Error filters, logging, and diagnostics

Error filters shape the GraphQL error before serialization. Registered filters run in registration order, and each filter receives the output of the previous filter. In the v16 server pipeline, the built-in debug filter is added when `IncludeExceptionDetails` is enabled, so a custom filter can sanitize an error before debug details are appended.

Use filters for response shaping. Use logging and diagnostics for operations:

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

For traces, Hot Chocolate's instrumentation includes OpenTelemetry enrichment hooks such as `EnrichRequestError(RequestContext, Exception, Activity)` and `EnrichResolverError(IMiddlewareContext, IError, Activity)`. Do not send full operation documents, variables, exception details, or personal data to telemetry systems unless your data-handling policy allows it.

# Test redaction and debug output

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

Add a second test with `IncludeExceptionDetails = true` when you want to verify local diagnostic output. Assert or snapshot that `extensions.exception.message` is present. If your production filter calls `WithException(null)`, test that `extensions.exception` is absent even when details are enabled.

# Troubleshooting

## I still see `Unexpected Execution Error`

That is expected when exception details are disabled or a filter has sanitized the error. Check server logs, diagnostic listeners, and traces for the original exception. Enable details only in a local development environment when you need the stack trace in the GraphQL response.

## Details do not appear in development

Check these items:

- The executor that handled the request has `IncludeExceptionDetails = true`.
- The process is running in the expected environment when you use `builder.Environment.IsDevelopment()`.
- A custom error filter did not call `WithException(null)` before debug details were added.
- The error has an attached exception. Manually built errors need `SetException(...)` or `ErrorBuilder.FromException(...)` if they are expected to carry exception details.
- You configured the same schema or executor that received the request.

## Details appear in production

Search your application and tests for unconditional `IncludeExceptionDetails = true`. Replace it with environment-gated configuration or explicit `false`. Check each executor separately, including copied sample code, background services, and any gateway or subgraph host you own. Also check custom filters for unsafe extensions that copy exception messages or stack traces manually.

## My error filter cannot use my service

In v16, schema-level components such as error filters and diagnostic listeners use schema services. If a filter constructor needs an application service, register that service with `AddApplicationService<T>()`. Avoid scoped request services in filter constructors because the schema service instance is created during schema initialization.

## Clients need actionable errors

Do not enable exception details to solve this. Add stable `extensions.code` values, safe guidance text, or a support correlation ID. For expected business outcomes, model the outcome in the schema with typed domain errors, mutation conventions, `FieldResult`, or another client-safe contract.

## TypeConverter exceptions reach my filter

Hot Chocolate v16 propagates TypeConverter exceptions to error filters. Treat them like other unexpected exceptions: log them internally, return a safe message and code, and avoid exposing converter exception details in production.

# What is safe to expose?

| Usually safe                        | Avoid exposing                                                       |
| ----------------------------------- | -------------------------------------------------------------------- |
| Generic messages                    | Raw exception messages                                               |
| Stable error codes                  | Stack traces                                                         |
| Correlation or request IDs          | File paths, type names, method names                                 |
| Public support or retry guidance    | SQL, connection strings, environment variables                       |
| Public validation or domain details | Tokens, secrets, tenant IDs, personal data, raw downstream responses |

"Safe" depends on your application's data classification and compliance requirements. Keep full details in logs and traces with appropriate access controls, not in the client response.

# Next steps

- [Error filters](/docs/hotchocolate/v16/build2/errors/error-filters) for response transformations, ordering, and dependency injection.
- [Error builder](/docs/hotchocolate/v16/build2/errors/error-builder) for constructing client-safe `IError` values.
- [Instrumentation](/docs/hotchocolate/v16/server/instrumentation) for diagnostic listeners and OpenTelemetry.
- [Options reference](/docs/hotchocolate/v16/api-reference/options) for `ModifyRequestOptions` and `IncludeExceptionDetails`.
- [Error handling guide](/docs/hotchocolate/v16/guides/error-handling) for broader request and domain error patterns.
