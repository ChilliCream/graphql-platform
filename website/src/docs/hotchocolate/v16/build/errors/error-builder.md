---
title: Error builder
---

Hot Chocolate uses `IError` values to represent GraphQL execution errors that are visible to clients. When you need to create such an error in a resolver, interceptor, or helper, use `ErrorBuilder` to set a safe message, a stable error code, and any optional metadata.

`IError` values appear in the top-level `errors` array of a GraphQL response. These are distinct from typed domain errors, such as mutation payload errors or result union members, which are returned as part of your schema data.

```json
{
  "data": {
    "product": {
      "id": "UHJvZHVjdDox",
      "imageUrl": null
    }
  },
  "errors": [
    {
      "message": "Product image is temporarily unavailable.",
      "locations": [{ "line": 4, "column": 5 }],
      "path": ["product", "imageUrl"],
      "extensions": {
        "code": "PRODUCT_IMAGE_UNAVAILABLE"
      }
    }
  ]
}
```

## Choosing the right error model

Use a GraphQL execution error when the server cannot complete a requested field or operation. Use a domain error when the application completes the operation path and needs to return a business result that clients can query.

| Scenario                                                | Recommended model                                                          |
| ------------------------------------------------------- | -------------------------------------------------------------------------- |
| Expected mutation validation or business rule           | Mutation conventions, typed payload errors, or `FieldResult`               |
| Expected query alternative that has fields              | Result unions or another schema model                                      |
| A field cannot resolve, but sibling fields can continue | `context.ReportError(error)`, return `IError`, or throw `GraphQLException` |
| One item in a batch resolver fails                      | `ResolverResult.Fail(error)`                                               |
| A request must fail before execution in an interceptor  | Throw `GraphQLException`                                                   |
| An unexpected exception occurs                          | Let it flow to error handling, then map or redact it with error filters    |

Avoid placing routine mutation validation in the top-level `errors` array. Model cases such as invalid quantity, unavailable payment method, or business-rule rejection as schema data when clients need to branch on them as part of the normal contract.

## Building a minimal error

To create an error, start with `ErrorBuilder.New()`, set a client-facing message, assign a stable code, and call `Build()`.

```csharp
IError error = ErrorBuilder.New()
    .SetMessage("Product image is temporarily unavailable.")
    .SetCode("PRODUCT_IMAGE_UNAVAILABLE")
    .Build();
```

`SetMessage` is required, and the message must not be empty. If you call `Build()` without a message, Hot Chocolate throws an `InvalidOperationException` with the message `Message is required`.

`SetCode` writes the value to `extensions.code` in the GraphQL response. Passing `null` or an empty string to `SetCode` removes the code.

Treat the code as the machine-readable contract. Clients should branch on `extensions.code` rather than on message text, which may change or be translated.

## Adding client-safe extension data

Use extensions to include extra JSON data that clients can safely consume.

```csharp
IError error = ErrorBuilder.New()
    .SetMessage("Rate limit exceeded.")
    .SetCode("RATE_LIMITED")
    .SetExtension("retryAfterSeconds", 60)
    .Build();
```

Guidelines for extension data:

- Reserve `extensions.code` for the primary error code.
- Document every custom extension key that clients depend on.
- Use JSON-friendly values: strings, numbers, booleans, arrays, and small objects.
- Do not include stack traces, raw exception messages, tokens, connection strings, secrets, personal data, or arbitrary internal objects.

`RemoveExtension` and `ClearExtensions` are helpful in shared helpers and filters when you need to remove data before an error reaches the client.

## Adding path and location information

The `path` property identifies the response path that produced the error. The `locations` property identifies positions in the GraphQL document.

Within a resolver, prefer `ReportError(IError)` when the current field is the source of the error. Hot Chocolate will fill in missing path and location information from the resolver context.

```csharp
[QueryType]
public static partial class ProductQueries
{
    public static string? GetProductImageUrl(
        [ID] int productId,
        ProductService products,
        IResolverContext context)
    {
        var imageUrl = products.FindImageUrl(productId);

        if (imageUrl is null)
        {
            context.ReportError(
                ErrorBuilder.New()
                    .SetMessage("Product image is temporarily unavailable.")
                    .SetCode("PRODUCT_IMAGE_UNAVAILABLE")
                    .Build());

            return null;
        }

        return imageUrl;
    }
}
```

Set the path explicitly when you know the correct path outside of automatic resolver completion.

```csharp
IError error = ErrorBuilder.New()
    .SetMessage("Product image is temporarily unavailable.")
    .SetCode("PRODUCT_IMAGE_UNAVAILABLE")
    .SetPath(context.Path)
    .Build();
```

Use `AddLocation(new Location(line, column))` when you know a document location. When working with syntax nodes, Hot Chocolate also provides helpers such as `AddLocation(syntaxNode)`, `TryAddLocation(syntaxNode)`, and `AddLocations(syntaxNodes)`.

Request-level errors thrown before execution, such as those from an interceptor, may not have a field path. Add path or location data only when it is accurate and useful.

## Reporting an error and continuing resolution

`IResolverContext.ReportError` adds a non-terminating field error. The resolver can still return a value, often `null` for the field that failed.

```csharp
[QueryType]
public static partial class ProductQueries
{
    public static string? GetProductImageUrl(
        [ID] int productId,
        ProductService products,
        IResolverContext context)
    {
        try
        {
            return products.GetImageUrl(productId);
        }
        catch (ImageStoreUnavailableException ex)
        {
            context.ReportError(
                ErrorBuilder.FromException(ex)
                    .SetMessage("Product image is temporarily unavailable.")
                    .SetCode("PRODUCT_IMAGE_UNAVAILABLE")
                    .Build());

            return null;
        }
    }
}
```

Use this pattern when the field can be `null` and the rest of the response should continue. Error filters still run. Missing path and location data can be completed from the resolver context.

Common overloads include:

| API                                                                 | Use when                                                                        |
| ------------------------------------------------------------------- | ------------------------------------------------------------------------------- |
| `ReportError(string errorMessage)`                                  | You need a message only.                                                        |
| `ReportError(IError error)`                                         | You need a structured error with code, extensions, path, or exception metadata. |
| `ReportError(Exception exception, Action<ErrorBuilder>? configure)` | You caught an exception and want to shape a safe public error.                  |

## Returning or throwing constructed errors

A resolver can return an `IError` when the resolver result itself should become a field error.

```csharp
public static object GetProductImageUrl(
    [ID] int productId,
    ProductService products)
{
    var imageUrl = products.FindImageUrl(productId);

    if (imageUrl is null)
    {
        return ErrorBuilder.New()
            .SetMessage("Product image is temporarily unavailable.")
            .SetCode("PRODUCT_IMAGE_UNAVAILABLE")
            .Build();
    }

    return imageUrl;
}
```

Hot Chocolate also supports returning `IEnumerable<IError>` when a resolver needs to report multiple errors as its result.

Throw `GraphQLException` when the current flow should stop with one or more GraphQL errors.

```csharp
throw new GraphQLException(
    ErrorBuilder.New()
        .SetMessage("Product image is temporarily unavailable.")
        .SetCode("PRODUCT_IMAGE_UNAVAILABLE")
        .Build());
```

For multiple errors, pass more than one `IError`.

```csharp
throw new GraphQLException(error1, error2);
```

`GraphQLException(string)` is concise, but it cannot set a code, extensions, path, or locations. Use `ErrorBuilder` when clients need structured error data.

In field resolvers, a thrown `GraphQLException` becomes a field error. In request interceptors, throwing `GraphQLException` can fail the request before execution starts.

## Using errors in batch resolver results

Batch resolvers can fail one item while allowing other items to complete. Return `ResolverResult.Fail(IError)` for the failed item.

```csharp
return users.Select<User, ResolverResult>(user =>
{
    if (user.Email is null)
    {
        return ResolverResult.Fail(
            ErrorBuilder.New()
                .SetMessage("User has no email address.")
                .SetCode("USER_EMAIL_MISSING")
                .Build());
    }

    return ResolverResult.Ok(user.IsVerified ? "verified" : "pending");
}).ToList();
```

This approach reports an error for the failed item and lets other batch items complete. See [DataLoader](/docs/hotchocolate/v16/build/dataloader) for full batch resolver setup.

## Modifying an existing error

`IError` values are immutable from the caller's perspective. Methods such as `WithMessage`, `WithCode`, `WithPath`, `WithLocations`, `WithExtensions`, `SetExtension`, `RemoveExtension`, and `WithException` return a new error.

```csharp
IError publicError = error
    .WithMessage("An internal error occurred.")
    .WithCode("INTERNAL_ERROR")
    .WithException(null);
```

Use `ErrorBuilder.FromError(error)` when you want to make several changes at once.

```csharp
IError publicError = ErrorBuilder.FromError(error)
    .SetMessage("An internal error occurred.")
    .SetCode("INTERNAL_ERROR")
    .RemoveExtension("debugId")
    .Build();
```

This pattern is common in `IErrorFilter` implementations, where errors are mapped, redacted, or assigned public codes.

## Attaching or preserving exceptions carefully

`IError.Exception` is server-side metadata for filters and diagnostics. By default, it is not part of the client-facing contract.

Use `SetException` or `WithException` in infrastructure code when the exception should remain available to filters. Avoid deriving public messages from raw exception text.

```csharp
IError error = ErrorBuilder.New()
    .SetMessage("Product image is temporarily unavailable.")
    .SetCode("PRODUCT_IMAGE_UNAVAILABLE")
    .SetException(exception)
    .Build();
```

`ErrorBuilder.FromException(exception)` creates an error with the message `Unexpected Execution Error` and attaches the exception. Filters can then translate it into a safe public message and code.

Do not enable `IncludeExceptionDetails` in production. Exception details can expose security-sensitive data.

## Testing the expected error JSON

Tests should assert the client contract, especially `message`, `path`, and `extensions.code`.

```json
{
  "errors": [
    {
      "message": "Product image is temporarily unavailable.",
      "path": ["product", "imageUrl"],
      "extensions": {
        "code": "PRODUCT_IMAGE_UNAVAILABLE"
      }
    }
  ],
  "data": {
    "product": {
      "imageUrl": null
    }
  }
}
```

Prefer stable codes in assertions. Assert message text when the message is part of the documented API contract. Avoid asserting stack traces, exception type names, or other data that should not be visible in production responses.

## When to use error filters

Use `ErrorBuilder` at the call site when the resolver already knows the public error and code.

Use an error filter when the mapping is cross-cutting:

- Map exception types to public codes.
- Redact messages for unexpected exceptions.
- Remove unsafe extensions.
- Add correlation ids or support ids.
- Normalize error codes across the schema.

Filters run after errors are produced. Each filter receives the error returned by the previous filter, and a filter must return an `IError`.

## API quick reference

### `IError` response mapping

| `IError` property | GraphQL response field     | Notes                                                          |
| ----------------- | -------------------------- | -------------------------------------------------------------- |
| `Message`         | `errors[].message`         | Required. Use safe client-facing text.                         |
| `Code`            | `errors[].extensions.code` | Optional stable machine-readable code.                         |
| `Path`            | `errors[].path`            | Optional response path. Often completed from resolver context. |
| `Locations`       | `errors[].locations`       | Optional GraphQL document positions.                           |
| `Extensions`      | `errors[].extensions`      | Optional custom JSON metadata.                                 |
| `Exception`       | Not serialized by default  | Server-side metadata for filters and diagnostics.              |

### Core `ErrorBuilder` methods

| Method                          | Purpose                                                           |
| ------------------------------- | ----------------------------------------------------------------- |
| `New()`                         | Start a new error.                                                |
| `FromError(IError)`             | Copy an existing error into a builder.                            |
| `FromException(Exception)`      | Start with `Unexpected Execution Error` and attach the exception. |
| `SetMessage(string)`            | Set the required public message.                                  |
| `SetCode(string?)`              | Set or remove `extensions.code`.                                  |
| `SetPath(Path?)`                | Set the GraphQL response path.                                    |
| `AddLocation(Location)`         | Add a document location.                                          |
| `TryAddLocation(Location)`      | Add a location when available.                                    |
| `ClearLocations()`              | Remove all locations.                                             |
| `SetException(Exception?)`      | Attach or clear server-side exception metadata.                   |
| `SetExtension(string, object?)` | Set one extension value.                                          |
| `RemoveExtension(string)`       | Remove one extension value.                                       |
| `ClearExtensions()`             | Remove all extension values.                                      |
| `Build()`                       | Create the immutable `IError`.                                    |

Useful extension methods include `SetCoordinate(SchemaCoordinate)`, `SetInputPath(Path?)`, formatted `SetMessage(...)` overloads, `AddLocation(ISyntaxNode)`, `TryAddLocation(ISyntaxNode?)`, and `AddLocations(IEnumerable<ISyntaxNode>)`.

## Troubleshooting

| Problem                                                     | What to check                                                                                                        |
| ----------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------- |
| `Build()` throws `Message is required`.                     | Call `SetMessage` before `Build()` and ensure the message is not empty.                                              |
| The client cannot find the code.                            | Use `SetCode` or `WithCode`. The code appears at `extensions.code`.                                                  |
| The error has no path or location.                          | Use `ReportError(IError)` inside the resolver, or set path and location explicitly when you know the correct values. |
| The response leaks implementation details.                  | Remove exception text and unsafe extension data. Review filters and `IncludeExceptionDetails`.                       |
| Expected mutation validation appears in top-level `errors`. | Model it as typed payload data through mutation conventions or `FieldResult`.                                        |
| Filter changes appear in an unexpected order.               | Review error filter registration order. Each filter receives the previous filter result.                             |
| A filter causes another unexpected error.                   | Ensure the filter returns an `IError`. Returning `null` is invalid.                                                  |

## Next steps

- [Resolver result handling](/docs/hotchocolate/v16/build/resolvers/resolver-result-handling) for choosing between values, `IError`, `ReportError`, and typed result models.
- [Mutation conventions](/docs/hotchocolate/v16/build/type-system/operations-mutations) for typed payload errors.
- [DataLoader](/docs/hotchocolate/v16/build/dataloader) for batch resolvers and `ResolverResult.Fail`.
- [Interceptors](/docs/hotchocolate/v16/build/server-configuration/interceptors) for failing a request before execution.
- [Instrumentation](/docs/hotchocolate/v16/build/observability) for logging and diagnostics.
- [Options reference](/docs/hotchocolate/v16/build/server-configuration/schema-options) for `IncludeExceptionDetails`.
