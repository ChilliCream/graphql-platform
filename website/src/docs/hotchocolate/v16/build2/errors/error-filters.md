---
title: Error filters
---

An error filter is a global transformation step for GraphQL errors that Hot Chocolate handles during execution. Use it when a resolver exception or another execution error needs a stable, safe client response before the error is serialized.

```csharp
builder
    .AddGraphQL()
    .AddErrorFilter(error =>
    {
        if (error.Exception is ProductNotFoundException)
        {
            return error
                .WithMessage("The product was not found.")
                .WithCode("PRODUCT_NOT_FOUND");
        }

        return error;
    });
```

The filter receives an immutable `IError` and must return an `IError`. Return the original error when the filter does not handle it. Methods such as `WithMessage`, `WithCode`, `WithException`, and `SetExtension` return a changed error instead of mutating the input.

# What an error filter changes

An error filter can change the `IError` that is sent to the client:

| `IError` member        | Common use                                                       |
| ---------------------- | ---------------------------------------------------------------- |
| `Message`              | Replace internal exception text with a public message.           |
| `Code`                 | Add a stable value serialized as `extensions.code`.              |
| `Extensions`           | Add small, safe metadata with `SetExtension`.                    |
| `Exception`            | Keep or remove server-side exception data from the error object. |
| `Path` and `Locations` | Preserve the failed field path and document locations.           |

An error filter cannot change response data, undo GraphQL null propagation, or handle HTTP, WebSocket, network, or client failures outside the GraphQL execution pipeline. It should not be the main logging or tracing mechanism. Use diagnostic events and instrumentation for server-side observability.

Preserve `Path` and `Locations` unless you have a specific reason to replace them. They help clients and operators identify which field failed.

# When to use an error filter

| Need                                        | Prefer                                                 | Why                                                                |
| ------------------------------------------- | ------------------------------------------------------ | ------------------------------------------------------------------ |
| Rewrite unhandled execution errors globally | `IErrorFilter`                                         | Gives one client-facing shape for resolver and execution failures. |
| Throw an intentional resolver error         | `GraphQLException` or returned `IError`                | The resolver owns that error.                                      |
| Return data and report a field issue        | `IResolverContext.ReportError`                         | The field can report a non-terminating error.                      |
| Model expected mutation failure             | Mutation conventions, `FieldResult`, or a result union | The client can query a typed domain contract.                      |
| Observe errors server-side                  | Instrumentation and logging                            | Keeps diagnostics separate from response shaping.                  |

If clients are expected to branch on the outcome as normal application flow, model that outcome in the schema. Use filters for execution failures that cross-cut fields or need production redaction.

# Default exception behavior

When a resolver throws an unhandled exception, Hot Chocolate creates a GraphQL error. By default, the client sees a generic message:

```json
{
  "data": {
    "productById": null
  },
  "errors": [
    {
      "message": "Unexpected Execution Error",
      "locations": [{ "line": 2, "column": 3 }],
      "path": ["productById"]
    }
  ]
}
```

The original exception is attached to the `IError` on the server. It is not serialized by default in production-oriented settings.

`IncludeExceptionDetails` controls whether exception messages and stack traces are added to GraphQL errors. It defaults to debugger-attached behavior. Gate it by environment and keep it disabled in production:

```csharp
builder
    .AddGraphQL()
    .ModifyRequestOptions(options =>
    {
        options.IncludeExceptionDetails = builder.Environment.IsDevelopment();
    });
```

> Do not enable exception details in production. Exception messages and stack traces can expose internal types, SQL details, file paths, connection information, and other sensitive data.

# Register a delegate filter

Use a delegate for small transformations:

```csharp
builder
    .AddGraphQL()
    .AddErrorFilter(error =>
    {
        if (error.Exception is ProductNotFoundException exception)
        {
            return error
                .WithMessage("The product was not found.")
                .WithCode("PRODUCT_NOT_FOUND")
                .SetExtension("productId", exception.ProductId);
        }

        return error;
    });
```

The `Code` property is serialized as `extensions.code`:

```json
{
  "errors": [
    {
      "message": "The product was not found.",
      "locations": [{ "line": 2, "column": 3 }],
      "path": ["productById"],
      "extensions": {
        "code": "PRODUCT_NOT_FOUND",
        "productId": "42"
      }
    }
  ],
  "data": {
    "productById": null
  }
}
```

Only add extension values that are safe, stable, and small. Do not add stack traces, raw exception messages, SQL text, connection strings, tenant identifiers, private resource existence signals, or large objects.

# Implement `IErrorFilter`

Use a class when the logic is reusable, needs constructor dependencies, or maps several exception types.

```csharp
public sealed class ApplicationErrorFilter : IErrorFilter
{
    public IError OnError(IError error)
    {
        if (error.Exception is ProductNotFoundException exception)
        {
            return error
                .WithMessage("The product was not found.")
                .WithCode("PRODUCT_NOT_FOUND")
                .SetExtension("productId", exception.ProductId);
        }

        if (error.Exception is not null)
        {
            return error
                .WithMessage("An internal error occurred.")
                .WithCode("INTERNAL_ERROR");
        }

        return error;
    }
}
```

Register the class on the same GraphQL builder that configures the schema:

```csharp
builder
    .AddGraphQL()
    .AddErrorFilter<ApplicationErrorFilter>();
```

`OnError` must return an `IError` every time. Returning `null` is invalid and causes another execution error. Keep filter logic defensive, deterministic, and fast. Avoid blocking I/O, remote calls, heavy serialization, and expensive reflection.

# Use `ErrorBuilder` for several changes

The `With*` helpers read well for a few changes. Use `ErrorBuilder.FromError(error)` when several properties are easier to review in one block.

```csharp
return ErrorBuilder
    .FromError(error)
    .SetMessage("The product was not found.")
    .SetCode("PRODUCT_NOT_FOUND")
    .SetExtension("productId", exception.ProductId)
    .Build();
```

Use `ErrorBuilder.New()` when you create a new error outside a filter. Inside a filter, prefer `FromError(error)` so existing path, locations, and other useful fields are preserved unless you intentionally change them.

# Inject services into a filter

In v16, error filters are schema services. If a filter needs an application service, make that service available to schema services with `AddApplicationService<T>()`.

```csharp
builder.Services.AddSingleton<ErrorCodeMapper>();

builder
    .AddGraphQL()
    .AddApplicationService<ErrorCodeMapper>()
    .AddErrorFilter<ApplicationErrorFilter>();
```

```csharp
public sealed class ApplicationErrorFilter : IErrorFilter
{
    private readonly ErrorCodeMapper _codes;

    public ApplicationErrorFilter(ErrorCodeMapper codes)
    {
        _codes = codes;
    }

    public IError OnError(IError error)
    {
        if (error.Exception is null)
        {
            return error;
        }

        var publicError = _codes.Map(error.Exception);

        if (publicError is null)
        {
            return error
                .WithMessage("An internal error occurred.")
                .WithCode("INTERNAL_ERROR");
        }

        return error
            .WithMessage(publicError.Message)
            .WithCode(publicError.Code);
    }
}
```

`AddApplicationService<T>()` resolves the application service during schema initialization and registers it with schema services. Use singleton-safe dependencies for this pattern. Resolver method injection does not need this cross-registration.

The factory overload `AddErrorFilter<T>(Func<IServiceProvider, T>)` receives the schema services provider. Treat root provider access as an advanced option and keep the common path explicit with `AddApplicationService<T>()`.

# Compose multiple filters

You can register more than one filter. Filters run in registration order, and each filter receives the error returned by the previous filter.

```csharp
builder
    .AddGraphQL()
    .AddErrorFilter<KnownExceptionErrorFilter>()
    .AddErrorFilter<SafeMetadataErrorFilter>()
    .AddErrorFilter<ProductionRedactionErrorFilter>();
```

```text
IError from execution
  -> KnownExceptionErrorFilter
  -> SafeMetadataErrorFilter
  -> ProductionRedactionErrorFilter
  -> serialized GraphQL error
```

Later filters can overwrite `Message`, `Code`, and extension values set by earlier filters. Put broad redaction filters after specific mapping filters, and preserve errors that already have an intentional public code.

```csharp
public sealed class ProductionRedactionErrorFilter : IErrorFilter
{
    public IError OnError(IError error)
    {
        if (error.Exception is null || error.Code is not null)
        {
            return error;
        }

        return error
            .WithMessage("An internal error occurred.")
            .WithCode("INTERNAL_ERROR");
    }
}
```

# Keep logging and audit separate

Filters may see the original exception, but response shaping and observability are different concerns.

- Use instrumentation, diagnostic events, logging, and tracing to record server-side details.
- Use error filters to decide what clients may see.
- Do not add audit records, stack traces, or log payloads to `extensions`.
- If you log from a filter, keep it short and do not replace your normal instrumentation pipeline.

This separation protects the public error contract and keeps operational data on the server.

# Production masking pattern

A production filter usually has two parts:

1. Map known exception types to safe messages and stable codes.
2. Redact all other exception-backed errors to a generic fallback.

```csharp
public sealed class SafeExceptionErrorFilter : IErrorFilter
{
    public IError OnError(IError error)
    {
        return error.Exception switch
        {
            ProductNotFoundException exception => error
                .WithMessage("The product was not found.")
                .WithCode("PRODUCT_NOT_FOUND")
                .SetExtension("productId", exception.ProductId),

            ValidationException => error
                .WithMessage("The request is invalid.")
                .WithCode("BAD_USER_INPUT"),

            null => error,

            _ => error
                .WithMessage("An internal error occurred.")
                .WithCode("INTERNAL_ERROR")
        };
    }
}
```

Do not expose raw messages from unexpected exceptions. A database timeout, file access failure, or downstream service error may include details that do not belong in a client response.

If you need to remove the exception object from the filtered `IError`, `WithException(null)` is available. The primary protection is still to keep `IncludeExceptionDetails` disabled in production and to avoid unsafe extension data.

# Test the error JSON

Test filters at the GraphQL response boundary. The important assertions are the public JSON shape, the stable code, and the absence of private details.

```json
{
  "errors": [
    {
      "message": "An internal error occurred.",
      "path": ["productById"],
      "extensions": {
        "code": "INTERNAL_ERROR"
      }
    }
  ],
  "data": {
    "productById": null
  }
}
```

For production tests, also assert that the response does not contain stack trace fragments, internal exception type names, SQL text, connection strings, or the original unexpected exception message. Keep the server log or diagnostic event as the place to verify full exception details.

# API reference

| API                                                                    | Purpose                                                                  |
| ---------------------------------------------------------------------- | ------------------------------------------------------------------------ |
| `IErrorFilter.OnError(IError error)`                                   | Receives an execution error and returns the original or rewritten error. |
| `IRequestExecutorBuilder.AddErrorFilter(Func<IError, IError>)`         | Registers a delegate filter.                                             |
| `IRequestExecutorBuilder.AddErrorFilter<T>()`                          | Registers a class-based filter.                                          |
| `IRequestExecutorBuilder.AddErrorFilter<T>(Func<IServiceProvider, T>)` | Registers a filter created from schema services.                         |
| `IServiceCollection.AddErrorFilter(...)`                               | Service collection overloads for filter registration.                    |
| `WithMessage`, `WithCode`, `SetExtension`, `WithException`             | Create changed `IError` values from an existing error.                   |
| `ErrorBuilder.FromError(error)`                                        | Builds a changed error while preserving existing fields by default.      |
| `RequestExecutorOptions.IncludeExceptionDetails`                       | Controls exception detail serialization.                                 |

Filters can return the original error, a changed error, or an aggregate error. Most applications should return one `IError` per input error unless they have a specific client contract for multiple top-level errors.

# Troubleshooting

| Symptom                                                      | Likely cause                                                                                | Fix                                                                                                      |
| ------------------------------------------------------------ | ------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------- |
| Filter does not run                                          | It is registered on a different schema builder, or the failure is outside GraphQL execution | Register it on the same `AddGraphQL()` chain and verify the failure reaches GraphQL execution.           |
| Constructor dependency cannot be resolved                    | The filter is a schema service and the dependency exists only in application services       | Register the dependency and cross-register it with `AddApplicationService<T>()`.                         |
| Safe mutation errors are overwritten                         | A global filter rewrites every error                                                        | Transform specific exception types or `error.Exception is not null`, and preserve existing public codes. |
| `extensions.code` is missing                                 | No filter called `WithCode`, or a later filter replaced the error                           | Add a stable code and check filter order.                                                                |
| Stack trace or raw exception text appears in responses       | `IncludeExceptionDetails` is enabled or unsafe data was added to extensions                 | Disable exception details outside development and remove unsafe extension values.                        |
| Earlier filter output changes later                          | Filters run in registration order                                                           | Put broad redaction last and avoid overwriting known public errors.                                      |
| Client code depends on message text                          | No stable code or typed schema outcome exists                                               | Add `extensions.code` for top-level GraphQL errors or model expected outcomes in the schema.             |
| Another `Unexpected Execution Error` appears after filtering | The filter returned `null` or threw                                                         | Always return an `IError` and keep filter code defensive.                                                |

# Where to go next

- [Error handling overview](./) for choosing between top-level errors, typed domain results, and transport concerns.
- [Error builder](./error-builder) for constructing `IError` values.
- [Errors API reference](/docs/hotchocolate/v16/api-reference/errors) for `GraphQLException`, returned errors, `ReportError`, and exception details.
- [Error handling guide](/docs/hotchocolate/v16/guides/error-handling) for a broader request-error and domain-error walkthrough.
- [Diagnostic events](/docs/hotchocolate/v16/build2/observability/diagnostic-events) for logging, tracing, and metrics.
- [Service injection](/docs/hotchocolate/v16/build2/resolvers/service-injection) for resolver service injection patterns.
- [Dependency injection](/docs/hotchocolate/v16/resolvers-and-data/dependency-injection) for schema services and application services.
