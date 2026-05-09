---
title: Control Error Exposure
---

Learn how to keep Hot Chocolate v16 error responses safe in production while still providing your team with the diagnostics needed in logs, traces, and tests.

## Prerequisites

You need an ASP.NET Core Hot Chocolate v16 server set up with `builder.AddGraphQL()` and `app.MapGraphQL()`. You should be able to adjust GraphQL options per environment, for example using `builder.Environment` or your app configuration. Remember, a GraphQL response can include both partial `data` and a top-level `errors` array.

For the logging example, your app should use `Microsoft.Extensions.Logging`. If your error filter accesses `HttpContext.TraceIdentifier`, register `IHttpContextAccessor` and make it available to schema services with `.AddApplicationService<IHttpContextAccessor>()`.

## Producing Safe Error Responses in Production

Start by defining the response contract you want clients to receive. In GraphQL, a resolver can throw a sensitive exception while a sibling field still succeeds. For example:

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

With proper error masking and the filter described later, the production response should look like this:

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

This response does not leak the secret string, exception message, stack trace, SQL, connection string, password, file path, or machine name. Operators can use the `correlationId` to locate the full exception in server logs or traces.

## What Hot Chocolate Masks by Default

When a resolver throws an unhandled exception, Hot Chocolate treats it as an unexpected system failure. It catches the exception, creates an `IError` with the message `"Unexpected Execution Error"`, attaches the original exception to `IError.Exception`, and serializes a top-level GraphQL error. The failed field becomes `null`. If the field is non-nullable, standard GraphQL null propagation applies.

If you do not use a custom filter and exception details are disabled, the error response is generic:

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

Exception details are only included if `IncludeExceptionDetails` is enabled and the error still carries an exception. Network and transport errors are handled separately. Teach clients to check the GraphQL `errors` array for execution errors, rather than assuming every resolver exception results in an HTTP 500.

## Configuring Exception Details per Environment

Always set `IncludeExceptionDetails` explicitly for every deployment. Its default depends on `Debugger.IsAttached`, not `IHostEnvironment.IsDevelopment()`.

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

If you need more control, use a configuration flag, but always default to `false` in production:

```csharp
builder
    .AddGraphQL()
    .ModifyRequestOptions(options =>
    {
        options.IncludeExceptionDetails = false;
    });
```

When `IncludeExceptionDetails` is `true`, Hot Chocolate adds debug information under `extensions.exception.message` and `extensions.exception.stackTrace` for errors that still carry an exception. Never enable this on public production endpoints. If a production filter calls `WithException(null)`, debug details cannot be added later for that error. Make exception stripping conditional, or register separate filters for development and production.

## Add a Safe Error Filter with a Correlation ID

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

## Choose Safe Error Codes and Extensions

Treat error extensions as part of your public client contract. Only add values you are willing to document, support, and review for privacy.

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

## Keep Domain Errors Separate from Unexpected Exceptions

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

## Handle Validation and Authorization Errors Safely

Parse and validation errors are request errors. They may expose operation text locations and schema field names, so treat schema names as part of your public API surface.

Authorization errors are not unhandled exceptions. Hot Chocolate uses stable codes such as `AUTH_NOT_AUTHENTICATED` and `AUTH_NOT_AUTHORIZED`:

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

## Verify HTTP Status Code Expectations

A GraphQL response can contain `errors` even when the HTTP request succeeded at the transport layer. With the GraphQL over HTTP response format, Hot Chocolate determines status codes according to transport rules. With legacy `Accept: application/json`, Hot Chocolate returns HTTP 200 for every request.

Handle resolver exceptions through the GraphQL response shape and `extensions.code`. Do not expose exception details or rely on HTTP 500 for resolver failures. If you customize HTTP status codes through a response formatter, test your clients first because status-code changes can break client assumptions. See [HTTP Transport](/docs/hotchocolate/v16/server/http-transport) for response formats and status-code customization.

## Log Details Server-Side

Masking errors is only effective if operators can still diagnose failures. The error filter above logs the full exception with the same correlation ID that appears in the client response:

```text
fail: GraphQL unexpected error 0HMSABC123 at /secret
System.InvalidOperationException: Connection string Host=db;Password=secret leaked
```

The log contains sensitive diagnostic details, but the client response does not.

You can also centralize logging with `ExecutionDiagnosticEventListener.RequestError` and resolver error instrumentation. Diagnostic event handlers run synchronously as part of the GraphQL request, so keep handlers short and enqueue expensive work. Always include the correlation ID, a safe operation name, path, code, and trace ID. Avoid logging raw variables unless your logging policy allows and redacts them. Prevent double logging if both a filter and a diagnostic listener handle the same exception.

## Trace Errors with OpenTelemetry

Add Hot Chocolate instrumentation to correlate GraphQL spans, server logs, and client correlation IDs.

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

You can use `Activity.Current?.TraceId` as the response correlation ID or store it with your existing request ID. If you add a custom `ActivityEnricher`, only set safe, low-cardinality tags such as `graphql.error.correlation_id`. Do not include raw documents, variables, exception messages, personally identifiable information, tenant or user identifiers, or secrets unless your telemetry policy allows and redacts them. Broad tracing, especially at the resolver level, adds runtime cost.

## Run Operational Exposure Checks

Test your error exposure controls in a non-production environment that uses production error settings:

1. Add or trigger a resolver exception that contains a sentinel secret string, such as `Password=sentinel-secret`.
2. Send a request using the same `Accept` header as your clients.
3. Verify the HTTP status code matches your transport and content negotiation expectations.
4. Inspect the GraphQL response body directly.
5. Search the response for the sentinel secret, exception type, stack trace markers, SQL or provider text, and `extensions.exception`.
6. Confirm that unexpected errors include a stable `extensions.code` and an opaque `extensions.correlationId`.
7. Check that logs or traces contain the correlation ID and the full exception server-side.
8. Repeat with authorization errors, validation errors, and expected domain errors so your team knows which details are intentionally visible.
9. Run the production configuration in CI or a deployment smoke test, not just under a local debugger.

A passing check means: no stack trace, no secret, no `extensions.exception`, stable code present, correlation ID present, and a matching server log found.

## Test Masked and Development Responses

Test your error masking pipeline using the same registration style as in `Program.cs`. Prefer CookieCrumble snapshots for the full response, and use targeted string assertions to check for the absence or presence of secrets.

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

The production test ensures sensitive details are absent and safe client fields are present. The development test ensures debug details appear only when `IncludeExceptionDetails` is enabled and the exception remains on the error.

## Troubleshoot Error Exposure

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

## What This Page Does Not Cover

This page covers only Hot Chocolate server operations. Fusion gateway error exposure uses separate APIs and is out of scope here.

Error exposure controls do not replace request cost limits, request size limits, introspection controls, trusted documents, authentication, authorization, CSRF protection, or transport hardening. Use these controls together for defense in depth.

## Next Steps

- Review [Error Handling](/docs/hotchocolate/v16/guides/error-handling) for the full Hot Chocolate error model.
- Review [Errors](/docs/hotchocolate/v16/api-reference/errors) for `IError`, `GraphQLException`, `IResolverContext.ReportError`, `ErrorBuilder`, and `IErrorFilter`.
- Review [Options](/docs/hotchocolate/v16/api-reference/options) for `IncludeExceptionDetails` and request options.
- Review [Dependency injection](/docs/hotchocolate/v16/resolvers-and-data/dependency-injection#application-services-in-schema-services) for schema services and `.AddApplicationService<T>()`.
- Review [HTTP Transport](/docs/hotchocolate/v16/server/http-transport) for status code and content negotiation behavior.
- Review [Instrumentation](/docs/hotchocolate/v16/server/instrumentation) for diagnostic listeners and OpenTelemetry.
- Review [Testing](/docs/hotchocolate/v16/guides/testing) for executor-based tests.
- Review [Authentication](/docs/hotchocolate/v16/securing-your-api/authentication), [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization), [request limits](/docs/hotchocolate/v16/securing-your-api/request-limits), [cost analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis), [introspection](/docs/hotchocolate/v16/securing-your-api/introspection), and [trusted documents](/docs/hotchocolate/v16/performance/trusted-documents) for adjacent security controls.
