---
title: Error filters
---

Error filters in Hot Chocolate provide a global way to transform GraphQL errors during execution. Use an error filter when you need to ensure that resolver exceptions or other execution errors are converted into stable, safe responses for clients before serialization.

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

A filter receives an immutable `IError` and must return an `IError`. If the filter does not handle the error, return the original error. Methods like `WithMessage`, `WithCode`, `WithException`, and `SetExtension` create a new error instance with the specified changes, leaving the input unchanged.

# What an error filter can change

An error filter can modify the `IError` sent to the client:

| `IError` member        | Common use                                                       |
| ---------------------- | ---------------------------------------------------------------- |
| `Message`              | Replace internal exception text with a public message.           |
| `Code`                 | Add a stable value serialized as `extensions.code`.              |
| `Extensions`           | Add small, safe metadata with `SetExtension`.                    |
| `Exception`            | Keep or remove server-side exception data from the error object. |
| `Path` and `Locations` | Preserve the failed field path and document locations.           |

An error filter cannot modify response data, reverse GraphQL null propagation, or handle HTTP, WebSocket, network, or client failures outside the GraphQL execution pipeline. It is not intended for logging or tracing. For server-side observability, use diagnostic events and instrumentation.

Keep `Path` and `Locations` unless you have a specific reason to change them. These help clients and operators identify which field failed.

# When to use an error filter

| Need                                        | Prefer                                                 | Why                                                                            |
| ------------------------------------------- | ------------------------------------------------------ | ------------------------------------------------------------------------------ |
| Rewrite unhandled execution errors globally | `IErrorFilter`                                         | Provides a consistent client-facing shape for resolver and execution failures. |
| Throw an intentional resolver error         | `GraphQLException` or returned `IError`                | The resolver controls the error.                                               |
| Return data and report a field issue        | `IResolverContext.ReportError`                         | The field can report a non-terminating error.                                  |
| Model expected mutation failure             | Mutation conventions, `FieldResult`, or a result union | The client can query a typed domain contract.                                  |
| Observe errors server-side                  | Instrumentation and logging                            | Keeps diagnostics separate from response shaping.                              |

If clients are expected to branch on the outcome as part of normal application flow, model that outcome in the schema. Use error filters for execution failures that affect multiple fields or require production redaction.

# Default exception behavior

When a resolver throws an unhandled exception, Hot Chocolate creates a GraphQL error. By default, the client receives a generic message:

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

The original exception is attached to the `IError` on the server, but is not serialized by default in production settings.

The `IncludeExceptionDetails` option controls whether exception messages and stack traces are included in GraphQL errors. By default, this is enabled only when a debugger is attached. Always gate this by environment and keep it disabled in production:

```csharp
builder
    .AddGraphQL()
    .ModifyRequestOptions(options =>
    {
        options.IncludeExceptionDetails = builder.Environment.IsDevelopment();
    });
```

> Do not enable exception details in production. Exception messages and stack traces can reveal internal types, SQL details, file paths, connection information, and other sensitive data.

# Registering a delegate filter

Use a delegate for small, focused transformations:

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

The `Code` property is serialized as `extensions.code` in the response:

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

Only add extension values that are safe, stable, and small. Avoid including stack traces, raw exception messages, SQL text, connection strings, tenant identifiers, private resource existence signals, or large objects.

# Implementing `IErrorFilter`

Use a class-based filter when the logic is reusable, requires constructor dependencies, or needs to map several exception types.

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

`OnError` must always return an `IError`. Returning `null` is invalid and will cause another execution error. Keep filter logic defensive, deterministic, and fast. Avoid blocking I/O, remote calls, heavy serialization, or expensive reflection.

# Using `ErrorBuilder` for multiple changes

The `With*` helpers are convenient for a few changes. When you need to update several properties, use `ErrorBuilder.FromError(error)` to review all changes in one place:

```csharp
return ErrorBuilder
    .FromError(error)
    .SetMessage("The product was not found.")
    .SetCode("PRODUCT_NOT_FOUND")
    .SetExtension("productId", exception.ProductId)
    .Build();
```

Use `ErrorBuilder.New()` when creating a new error outside a filter. Inside a filter, prefer `FromError(error)` to preserve existing path, locations, and other useful fields unless you intend to change them.

# Injecting services into a filter

Error filters are schema services. If your filter needs an application service, make it available to schema services using `AddApplicationService<T>()`.

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

`AddApplicationService<T>()` resolves the application service during schema initialization and registers it with schema services. Use singleton-safe dependencies for this pattern. Resolver method injection does not require this cross-registration.

The factory overload `AddErrorFilter<T>(Func<IServiceProvider, T>)` receives the schema services provider. Treat root provider access as an advanced option; for most cases, use `AddApplicationService<T>()` for clarity.

# Composing multiple filters

You can register more than one filter. Filters run in registration order, and each receives the error returned by the previous filter.

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

Later filters can overwrite `Message`, `Code`, and extension values set by earlier filters. Place broad redaction filters after specific mapping filters, and preserve errors that already have an intentional public code.

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

# Keeping logging and audit separate

Filters may access the original exception, but response shaping and observability are distinct concerns.

- Use instrumentation, diagnostic events, logging, and tracing to record server-side details.
- Use error filters to determine what clients may see.
- Do not add audit records, stack traces, or log payloads to `extensions`.
- If you log from a filter, keep it brief and do not replace your normal instrumentation pipeline.

This separation protects the public error contract and keeps operational data on the server.

# Production masking pattern

A typical production filter has two parts:

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

Do not expose raw messages from unexpected exceptions. Issues like database timeouts, file access failures, or downstream service errors may include details that should not be sent to clients.

If you need to remove the exception object from the filtered `IError`, use `WithException(null)`. The main protection remains keeping `IncludeExceptionDetails` disabled in production and avoiding unsafe extension data.

# Testing the error JSON

Test filters at the GraphQL response boundary. Focus on the public JSON shape, the stable code, and the absence of private details.

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

For production tests, also verify that the response does not contain stack trace fragments, internal exception type names, SQL text, connection strings, or the original unexpected exception message. Use server logs or diagnostic events to review full exception details.

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

Filters can return the original error, a changed error, or an aggregate error. Most applications should return one `IError` per input error unless there is a specific client contract for multiple top-level errors.

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
- [Errors API reference](/docs/hotchocolate/v16/build/errors) for `GraphQLException`, returned errors, `ReportError`, and exception details.
- [Error handling guide](/docs/hotchocolate/v16/_leagcy/guides/error-handling) for a broader request-error and domain-error walkthrough.
- [Diagnostic events](/docs/hotchocolate/v16/build/observability/diagnostic-events) for logging, tracing, and metrics.
- [Service injection](/docs/hotchocolate/v16/build/resolvers/service-injection) for resolver service injection patterns.
- [Dependency injection](/docs/hotchocolate/v16/build/resolvers/service-injection) for schema services and application services.
