---
title: Control Error Exposure
---

This page shows how to keep Hot Chocolate v16 production error responses safe while preserving the diagnostics your team needs in logs, traces, and tests.

## Prerequisites

You have an ASP.NET Core Hot Chocolate v16 server configured with `builder.AddGraphQL()` and `app.MapGraphQL()`. You can change GraphQL options per environment, for example with `builder.Environment` or application configuration. You also understand that a GraphQL response can contain partial `data` and a top-level `errors` array in the same response.

For the logging example, your application uses `Microsoft.Extensions.Logging`. If the error filter reads `HttpContext.TraceIdentifier`, register `IHttpContextAccessor` and make it available to schema services with `.AddApplicationService<IHttpContextAccessor>()`.

## Return a production-safe error response

Start from the response contract you want clients to see. A resolver can fail with a sensitive exception while a sibling field still succeeds.

```csharp
public sealed class Query
{
    public string SafeField() => "ok";

    public string Secret()
    {
        throw new InvalidOperationException(
            "Connection string Host=db;Password=secret leaked");
    }
}
```

With production masking and the filter shown later on this page, the response should be intentionally boring:

```json
{
  "data": {
    "safeField": "ok",
    "secret": null
  },
  "errors": [
    {
      "message": "An internal error occurred.",
      "path": ["secret"],
      "extensions": {
        "code": "INTERNAL_SERVER_ERROR",
        "correlationId": "0HMSABC123"
      }
    }
  ]
}
```

The response does not contain the secret string, the original exception message, a stack trace, SQL, a connection string, a password, a file path, or a machine name. Operators use the `correlationId` to find the full exception in server-side logs or traces.

## Understand what Hot Chocolate masks by default

Unhandled resolver exceptions are unexpected system failures. Hot Chocolate catches the exception, creates an `IError` with the message `"Unexpected Execution Error"`, keeps the original exception on `IError.Exception`, and serializes a top-level GraphQL error. The field that failed becomes `null`. If the field is non-null, normal GraphQL null propagation applies.

Without a custom filter and with exception details disabled, the same failing field produces a generic error:

```json
{
  "data": {
    "safeField": "ok",
    "secret": null
  },
  "errors": [
    {
      "message": "Unexpected Execution Error",
      "path": ["secret"]
    }
  ]
}
```

Exception details are not serialized unless `IncludeExceptionDetails` is enabled and the error still carries `IError.Exception`. Network and transport failures are a separate concern. Teach clients to inspect the GraphQL `errors` array for execution errors instead of assuming every resolver exception becomes an HTTP 500 response.

## Configure exception details by environment

Set `IncludeExceptionDetails` explicitly in every deployment. Its default is based on `Debugger.IsAttached`, not `IHostEnvironment.IsDevelopment()`.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQL()
    .ModifyRequestOptions(options =>
    {
        options.IncludeExceptionDetails = builder.Environment.IsDevelopment();
    })
    .AddQueryType<Query>();

var app = builder.Build();
app.MapGraphQL();
app.Run();
```

Use a configuration flag if your organization needs more control, but make `false` the production default:

```csharp
builder
    .AddGraphQL()
    .ModifyRequestOptions(options =>
    {
        options.IncludeExceptionDetails = false;
    });
```

When `IncludeExceptionDetails` is `true`, Hot Chocolate adds debug information under `extensions.exception.message` and `extensions.exception.stackTrace` for errors that still carry an exception. Never enable it on public production endpoints. If a production filter calls `WithException(null)`, debug details cannot be added later for that error, so make exception stripping conditional or register separate development and production filters.

## Add a safe error filter with a correlation ID

Use an `IErrorFilter` to rewrite unexpected exceptions for clients and log the original exception on the server.

```csharp
using System;
using System.Diagnostics;
using HotChocolate;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

public sealed class SafeExceptionErrorFilter : IErrorFilter
{
    private readonly ILogger _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SafeExceptionErrorFilter(
        ILoggerFactory loggerFactory,
        IHttpContextAccessor httpContextAccessor)
    {
        _logger = loggerFactory.CreateLogger<SafeExceptionErrorFilter>();
        _httpContextAccessor = httpContextAccessor;
    }

    public IError OnError(IError error)
    {
        if (error.Exception is null)
        {
            return error;
        }

        var correlationId = GetCorrelationId();

        _logger.LogError(
            error.Exception,
            "GraphQL unexpected error {CorrelationId} at {Path}",
            correlationId,
            error.Path);

        return error
            .WithMessage("An internal error occurred.")
            .WithCode("INTERNAL_SERVER_ERROR")
            .SetExtension("correlationId", correlationId);
    }

    private string GetCorrelationId()
    {
        var traceIdentifier = _httpContextAccessor.HttpContext?.TraceIdentifier;

        if (!string.IsNullOrWhiteSpace(traceIdentifier))
        {
            return traceIdentifier;
        }

        var traceId = Activity.Current?.TraceId.ToString();

        if (!string.IsNullOrWhiteSpace(traceId))
        {
            return traceId;
        }

        return Guid.NewGuid().ToString("N");
    }
}
```

Register application services before you register the filter. Error filters are schema services, so constructor dependencies that come from ASP.NET Core DI must be cross-registered with `.AddApplicationService<T>()`.

```csharp
builder.Services.AddHttpContextAccessor();

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .ModifyRequestOptions(options =>
    {
        options.IncludeExceptionDetails = builder.Environment.IsDevelopment();
    })
    .AddApplicationService<IHttpContextAccessor>()
    .AddApplicationService<ILoggerFactory>()
    .AddErrorFilter<SafeExceptionErrorFilter>();
```

The filter rewrites only errors with `error.Exception is not null`. It leaves validation, authorization, and intentionally modeled domain errors alone. `IError` is immutable, so methods such as `WithMessage`, `WithCode`, `SetExtension`, `RemoveExtension`, and `WithException` return a new error.

Do not copy `Exception.Message`, stack traces, SQL, connection strings, tokens, headers, raw variables, claims, personally identifiable information, or internal URLs into the response. You may call `.WithException(null)` as production-only hardening after logging if you never want later filters to serialize exception details for that error.

## Choose safe error codes and extensions

Treat error extensions as a public client contract. Add only values you are willing to document, support, and review for privacy.

| Response content                                 | Guidance                                                                                                               |
| ------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------- |
| Generic client message                           | Safe for unexpected failures, for example `"An internal error occurred."`.                                             |
| Stable error code                                | Safe when documented, for example `INTERNAL_SERVER_ERROR`, `RATE_LIMITED`, `AUTH_NOT_AUTHORIZED`, or `USERNAME_TAKEN`. |
| Correlation ID                                   | Safe when it is opaque and does not encode tenant, user, host, or secret data.                                         |
| Retry hint or category                           | Safe when it is coarse-grained and stable.                                                                             |
| Validation field path                            | Safe when the schema and input field names are public.                                                                 |
| Raw exception message                            | Unsafe. It can contain implementation details or secrets.                                                              |
| Stack trace                                      | Unsafe. It exposes code paths, package names, and file locations.                                                      |
| SQL, connection strings, provider errors         | Unsafe. They can expose schema, credentials, hosts, and infrastructure.                                                |
| File paths, machine names, internal URLs         | Unsafe. They expose deployment details.                                                                                |
| Headers, tokens, claims, raw variables           | Unsafe unless your response policy explicitly permits and redacts them.                                                |
| Emails, phone numbers, addresses, tenant secrets | Unsafe unless they are intentionally part of the schema contract.                                                      |
| Exception type names or provider codes           | Avoid as public codes. Prefer stable product codes over `SqlException` or provider-specific names.                     |

## Keep domain errors separate from unexpected exceptions

Expected business outcomes are not system failures. Model validation failures, conflicts, duplicate names, out-of-stock results, and permission-aware business denials as part of your schema contract.

Avoid this pattern for client-handled outcomes:

```csharp
[MutationType]
public static partial class ProductMutations
{
    public static Product RenameProduct(string name)
    {
        if (name.Length < 3)
        {
            throw new InvalidProductNameException("Product names need 3 characters.");
        }

        return new Product(name);
    }
}
```

If the exception is not mapped as a domain error, it becomes a masked top-level error and clients lose the typed contract they need. Prefer mutation conventions or explicit result types for expected failures:

```csharp
[MutationType]
public static partial class ProductMutations
{
    [Error(typeof(InvalidProductNameException))]
    public static Product RenameProduct(string name)
    {
        if (name.Length < 3)
        {
            throw new InvalidProductNameException("Product names need 3 characters.");
        }

        return new Product(name);
    }
}
```

With mutation conventions, expected errors appear as typed objects on the mutation payload, not as unexpected top-level `errors`. Review those messages and fields for privacy as carefully as any other client-facing schema field. Clients should key off typed errors or stable codes, not free-form messages.

Use `GraphQLException`, returned `IError`, and `IResolverContext.ReportError` only when a top-level GraphQL error is the intended contract. For more on resolver-level authoring, see [Error Handling](/docs/hotchocolate/v16/guides/error-handling) and [Mutation conventions](/docs/hotchocolate/v16/building-a-schema/mutations#mutation-conventions).

## Handle validation and authorization errors safely

Parse and validation errors are request errors. They can expose operation text locations and schema field names, so treat schema names as part of your public API surface.

Authorization errors are not unhandled exceptions. Hot Chocolate uses stable codes such as `AUTH_NOT_AUTHENTICATED` and `AUTH_NOT_AUTHORIZED`.

```json
{
  "errors": [
    {
      "message": "The current user is not authorized to access this resource.",
      "path": ["adminReport"],
      "extensions": {
        "code": "AUTH_NOT_AUTHORIZED"
      }
    }
  ]
}
```

If you customize authorization messages, keep them generic. Do not reveal policy names, role lists, ownership checks, authorization service names, or resource internals. Use instrumentation for server-side details about parser, validation, and authorization failures.

## Verify HTTP status code expectations

A GraphQL response can contain `errors` even when the HTTP request succeeded at the transport layer. With the GraphQL over HTTP response format, Hot Chocolate determines status codes according to transport rules. With legacy `Accept: application/json`, Hot Chocolate returns HTTP 200 for every request.

Handle resolver exceptions through the GraphQL response shape and `extensions.code`. Do not expose exception details or rely on HTTP 500 for resolver failures. If you customize HTTP status codes through a response formatter, test your clients first because status-code changes can break client assumptions. See [HTTP Transport](/docs/hotchocolate/v16/server/http-transport) for response formats and status-code customization.

## Log details server-side

Masking is useful only when operators can still diagnose the failure. The filter above logs the full exception with the same correlation ID that appears in the client response.

```text
fail: GraphQL unexpected error 0HMSABC123 at /secret
System.InvalidOperationException: Connection string Host=db;Password=secret leaked
```

The log contains sensitive diagnostic detail. The client response does not.

You can also centralize logging with `ExecutionDiagnosticEventListener.RequestError` and resolver error instrumentation. Diagnostic event handlers execute synchronously as part of the GraphQL request, so keep handlers short and enqueue expensive work. Include the correlation ID, a safe operation name, path, code, and trace ID. Avoid logging raw variables unless your logging policy permits and redacts them. Avoid double logging if both a filter and a diagnostic listener handle the same exception.

## Trace errors with OpenTelemetry

Add Hot Chocolate instrumentation when you want to join GraphQL spans, server logs, and client correlation IDs.

```csharp
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddInstrumentation();

builder.Logging.AddOpenTelemetry(
    logging => logging.SetResourceBuilder(
        ResourceBuilder.CreateDefault().AddService("ProductsApi")));

builder.Services
    .AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation();
        tracing.AddHotChocolateInstrumentation();
        tracing.AddOtlpExporter();
    });
```

Use `Activity.Current?.TraceId` as the response correlation ID or store it alongside your existing request ID. If you add a custom `ActivityEnricher`, set only safe, low-cardinality tags such as `graphql.error.correlation_id`. Avoid raw documents, variables, exception messages, personally identifiable information, tenant or user identifiers, and secrets unless your telemetry policy approves and redacts them. Broad tracing, especially resolver-level spans, adds runtime cost.

## Run operational exposure checks

Run these checks against a non-production environment that uses production error settings:

1. Add or trigger a known resolver exception that contains a sentinel secret string, for example `Password=sentinel-secret`.
2. Send a request with the same `Accept` header your clients use.
3. Verify the HTTP status expected by your chosen transport and content negotiation.
4. Inspect the GraphQL response body separately.
5. Search the full response for the sentinel secret, exception type, stack trace markers, SQL or provider text, and `extensions.exception`.
6. Verify unexpected errors include a stable `extensions.code` and an opaque `extensions.correlationId`.
7. Verify logs or traces contain the correlation ID and the full exception server-side.
8. Repeat with authorization errors, validation errors, and expected domain errors so your team knows which details are intentionally visible.
9. Run the production configuration in CI or a deployment smoke test, not only under a local debugger.

A passing production check says: no stack trace, no secret, no `extensions.exception`, stable code present, correlation ID present, and matching server log found.

## Test masked and development responses

Test the pipeline with the same registration style you use in `Program.cs`. Prefer CookieCrumble snapshots for the whole response and use targeted string assertions for secret absence or presence.

```csharp
using System;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public sealed class ErrorExposureTests
{
    [Fact]
    public async Task MaskUnexpectedException_Should_NotExposeSensitiveDetails_When_ExceptionDetailsDisabled()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddLogging()
            .AddHttpContextAccessor()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyRequestOptions(options => options.IncludeExceptionDetails = false)
            .AddErrorFilter(error =>
            {
                if (error.Exception is null)
                {
                    return error;
                }

                return error
                    .WithMessage("An internal error occurred.")
                    .WithCode("INTERNAL_SERVER_ERROR")
                    .SetExtension("correlationId", "test-correlation-id")
                    .WithException(null);
            })
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync("{ safeField secret }");
        var json = result.ToJson();

        // assert
        Assert.Contains("INTERNAL_SERVER_ERROR", json);
        Assert.Contains("test-correlation-id", json);
        Assert.DoesNotContain("Password=secret", json);
        Assert.DoesNotContain("\"exception\"", json);
    }

    [Fact]
    public async Task MaskUnexpectedException_Should_ExposeDebugDetails_When_ExceptionDetailsEnabledInDevelopment()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyRequestOptions(options => options.IncludeExceptionDetails = true)
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync("{ secret }");
        var json = result.ToJson();

        // assert
        Assert.Contains("Password=secret", json);
        Assert.Contains("stackTrace", json);
    }
}

public sealed class Query
{
    public string SafeField() => "ok";

    public string Secret()
    {
        throw new InvalidOperationException("Password=secret");
    }
}
```

The production test verifies that sensitive details are absent and safe client fields are present. The development test verifies that debug details appear only when `IncludeExceptionDetails` is enabled and the exception remains on the error.

## Troubleshoot error exposure

| Symptom                                              | Likely cause                                                                                                                                               | Fix                                                                                                                                              |
| ---------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------ |
| You still see stack traces in production.            | `IncludeExceptionDetails = true`, a debugger is attached, the wrong environment/configuration loaded, or a custom filter copied details into the response. | Set `IncludeExceptionDetails` explicitly to `false` in production and inspect filter output.                                                     |
| Development details disappeared.                     | A custom filter called `WithException(null)` before Hot Chocolate added debug details.                                                                     | Keep the exception in development or strip exceptions only in production.                                                                        |
| Your filter is not invoked.                          | The filter is registered on the wrong builder or schema, the executor was not rebuilt, or the request hits another endpoint.                               | Verify `.AddErrorFilter<T>()` registration on the GraphQL builder and restart the app.                                                           |
| `ILogger` or `IHttpContextAccessor` injection fails. | Error filters are schema services.                                                                                                                         | Register the application service and cross-register it with `.AddApplicationService<T>()`.                                                       |
| An expected business error becomes generic.          | The domain outcome is thrown as an unmapped exception.                                                                                                     | Model it with mutation conventions or an explicit result type.                                                                                   |
| Authorization errors reveal too much.                | A custom message exposes policies, roles, resource ownership, or internal services.                                                                        | Replace it with a generic message and a safe code.                                                                                               |
| The response correlation ID does not match logs.     | Different components generate different IDs.                                                                                                               | Generate or read the ID once per request. Prefer `HttpContext.TraceIdentifier`, incoming trace context, or a request-scoped correlation service. |
| Clients depend on error text.                        | Messages are being treated as stable identifiers.                                                                                                          | Add stable `extensions.code` values or typed domain errors.                                                                                      |
| HTTP status surprises clients.                       | Clients use different `Accept` headers or mix GraphQL over HTTP with legacy `application/json` behavior.                                                   | Verify the `Accept` header and response body before changing formatter status codes.                                                             |

## Know what this page does not cover

This page covers Hot Chocolate server operations only. Fusion gateway error exposure uses separate APIs and is out of scope here.

Error exposure controls do not replace request cost limits, request size limits, introspection controls, trusted documents, authentication, authorization, CSRF protection, or transport hardening. Use them together as defense in depth.

## Next steps

- Review [Error Handling](/docs/hotchocolate/v16/guides/error-handling) for the full Hot Chocolate error model.
- Review [Errors](/docs/hotchocolate/v16/api-reference/errors) for `IError`, `GraphQLException`, `IResolverContext.ReportError`, `ErrorBuilder`, and `IErrorFilter`.
- Review [Options](/docs/hotchocolate/v16/api-reference/options) for `IncludeExceptionDetails` and request options.
- Review [Dependency injection](/docs/hotchocolate/v16/resolvers-and-data/dependency-injection#application-services-in-schema-services) for schema services and `.AddApplicationService<T>()`.
- Review [HTTP Transport](/docs/hotchocolate/v16/server/http-transport) for status code and content negotiation behavior.
- Review [Instrumentation](/docs/hotchocolate/v16/server/instrumentation) for diagnostic listeners and OpenTelemetry.
- Review [Testing](/docs/hotchocolate/v16/guides/testing) for executor-based tests.
- Review [Authentication](/docs/hotchocolate/v16/securing-your-api/authentication), [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization), [request limits](/docs/hotchocolate/v16/securing-your-api/request-limits), [cost analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis), [introspection](/docs/hotchocolate/v16/securing-your-api/introspection), and [trusted documents](/docs/hotchocolate/v16/performance/trusted-documents) for adjacent security controls.
