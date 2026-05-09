---
title: Errors
---

Errors are a fundamental part of your GraphQL contract. In Hot Chocolate, the first decision is not which API to call, but whether a failure is a GraphQL execution problem, an expected product outcome, or a transport concern outside GraphQL execution.

Use top-level GraphQL errors when a request cannot be executed, validation fails, authorization rejects a selected field, or a resolver fails while producing a field. Use typed schema results when the client should handle the outcome as part of the normal product flow, such as a rejected command, a conflict, or invalid business input.

A helpful rule:

> If the client should branch on the outcome as part of the product flow, model it in the schema. If GraphQL execution failed, report a GraphQL error.

# Choosing the Right Error Model

| Situation                                                                      | Use                                                                                    | Client sees                                                                            |
| ------------------------------------------------------------------------------ | -------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------- |
| Invalid GraphQL document, validation failure, or a request that cannot execute | GraphQL request error                                                                  | Top-level `errors`, usually no useful `data`                                           |
| Resolver throws an unexpected exception                                        | Field error                                                                            | Top-level `errors`, field value becomes `null` or propagates through non-null wrappers |
| Resolver intentionally fails with one or more GraphQL errors                   | `GraphQLException` carrying `IError` values                                            | Top-level `errors` with your message, code, path, and extensions                       |
| Resolver can report a recoverable field issue and still continue               | `IResolverContext.ReportError`                                                         | Top-level `errors` plus the resolver return value or `null`                            |
| Business rule rejects a command or query result                                | Typed domain result, mutation payload errors, explicit result unions, or `FieldResult` | Data selected from schema fields, often a typed `errors` field on a payload            |
| Field authorization rejects access                                             | Hot Chocolate authorization                                                            | Top-level `errors` with an authorization code and normal null behavior                 |
| HTTP, WebSocket, network, or client parsing failure                            | Transport or client handling                                                           | Outside this errors section                                                            |

Do not use top-level GraphQL errors for product outcomes that clients should query, type-check, and display as part of the user experience.

# GraphQL Error Response Structure

A valid GraphQL response can include both `data` and `errors`. This is normal for partial success.

```json
{
  "data": {
    "products": [
      {
        "id": "1",
        "name": "Chili Oil"
      }
    ],
    "viewerBasket": null
  },
  "errors": [
    {
      "message": "The current user is not authorized to access this resource.",
      "path": ["viewerBasket"],
      "extensions": {
        "code": "AUTH_NOT_AUTHENTICATED"
      }
    }
  ]
}
```

Hot Chocolate represents GraphQL execution errors with `IError`. An `IError` includes:

- `Message`: the client-facing message
- `Code`: serialized as `extensions.code`
- `Path`: the response path that failed
- `Locations`: the document locations related to the error
- `Extensions`: custom non-spec fields
- `Exception`: the server-side exception associated with the error

`IError` values are immutable. Methods such as `WithMessage`, `WithCode`, `WithException`, and `SetExtension` return a new error. Use `ErrorBuilder` to create an error from scratch or to change several properties at once.

```csharp
var error = ErrorBuilder
    .New()
    .SetMessage("Rate limit exceeded.")
    .SetCode("RATE_LIMITED")
    .Build();
```

The `Exception` property is server-side only. It is not serialized to clients by default.

# Request Errors and Field Errors

A request error occurs before a field value can be produced. Examples include syntax errors, validation errors, and operation selection errors. These errors appear in the top-level `errors` array.

A field error occurs while resolving a selected field. Hot Chocolate records the error, sets the field result to `null`, and applies normal GraphQL null propagation.

```json
{
  "data": {
    "userById": null
  },
  "errors": [
    {
      "message": "Unexpected Execution Error",
      "locations": [{ "line": 2, "column": 3 }],
      "path": ["userById"]
    }
  ]
}
```

If the field type is nullable, only that field becomes `null`. If the field type is non-null, the `null` value propagates to the nearest nullable parent. If a non-null root field has no nullable parent, the entire `data` value can become `null`.

Hot Chocolate defaults to `ErrorHandlingMode.Propagate`. You can use `ErrorHandlingMode.Null` through request options if you need different null handling.

# Creating and Reporting GraphQL Errors

Throw a standard exception for unexpected failures. Hot Chocolate catches it, keeps the original exception server-side, and sends a redacted error message by default.

```csharp
[QueryType]
public static class Query
{
    public static async Task<Basket> GetBasketAsync(
        BasketService baskets,
        CancellationToken cancellationToken)
    {
        return await baskets.GetCurrentBasketAsync(cancellationToken);
    }
}
```

If `GetCurrentBasketAsync` throws because the database is unavailable, the client receives `Unexpected Execution Error` unless exception details are enabled.

Throw `GraphQLException` when your resolver needs to intentionally produce one or more GraphQL errors.

```csharp
throw new GraphQLException(
    ErrorBuilder
        .New()
        .SetMessage("The basket cannot be checked out.")
        .SetCode("BASKET_CHECKOUT_FAILED")
        .Build());
```

Use `IResolverContext.ReportError` for a non-terminating field error. The resolver can still return data.

```csharp
public static Basket GetBasket(IResolverContext context)
{
    context.ReportError(
        ErrorBuilder
            .New()
            .SetMessage("Some basket items could not be priced.")
            .SetCode("BASKET_PARTIAL_PRICING")
            .Build());

    return new Basket([]);
}
```

`ReportError` has overloads for a message, an exception with an optional `ErrorBuilder` callback, and an `IError`.

# Domain Errors Belong in the Schema

Expected business failures should usually be represented as data, not as top-level GraphQL errors. For example, a user entering a negative basket quantity is different from a database outage. The negative quantity is part of the application contract. Clients should be able to select fields for that outcome and handle it with generated types.

With mutation conventions, domain errors can appear on the mutation payload:

```graphql
type AddToBasketPayload {
  shoppingBasket: ShoppingBasket
  errors: [AddToBasketError!]
}

union AddToBasketError = QuantityCannotBeNegativeError

type QuantityCannotBeNegativeError implements Error {
  message: String!
  code: String!
}
```

A resolver can also return an explicit success-or-error shape with `FieldResult` where the schema declares the possible errors.

```csharp
public static async Task<FieldResult<ShoppingBasket, QuantityCannotBeNegativeError>> AddToBasketAsync(
    AddToBasketInput input,
    BasketService baskets,
    CancellationToken cancellationToken)
{
    if (input.Quantity < 1)
    {
        return new QuantityCannotBeNegativeError(
            "Quantity must be at least 1.",
            "QUANTITY_CANNOT_BE_NEGATIVE");
    }

    return await baskets.AddItemAsync(input, cancellationToken);
}
```

Use explicit public error types for client contracts. Avoid exposing exception class names, stack traces, or internal exception messages as schema fields in sensitive domains.

# Error Filters and Response Shaping

An error filter rewrites an `IError` before it is sent to the client. Use filters to sanitize messages, add codes, or enrich extensions for GraphQL errors. Use instrumentation for logging, tracing, and metrics.

```csharp
builder
    .AddGraphQL()
    .AddErrorFilter(error =>
    {
        if (error.Exception is not null)
        {
            return error
                .WithMessage("An internal error occurred.")
                .WithCode("INTERNAL_ERROR");
        }

        return error;
    });
```

Filters run in registration order, and each filter receives the error returned by the previous filter. `IErrorFilter.OnError` must return an `IError`. Returning `null` is invalid.

Filters do not replace domain modeling. If a client must handle a business outcome, put that outcome in the schema instead of rewriting an execution error.

# Exception Details and Production Safety

`IncludeExceptionDetails` controls whether exception messages and stack traces are added to GraphQL errors. It defaults to `true` only when a debugger is attached.

```csharp
builder
    .AddGraphQL()
    .ModifyRequestOptions(options =>
    {
        options.IncludeExceptionDetails = builder.Environment.IsDevelopment();
    });
```

Do not enable exception details in production. Exception messages and stack traces can expose internal types, database details, file paths, connection information, and other sensitive data.

For production APIs:

- Keep unexpected failure messages generic.
- Add stable custom error codes for client branching.
- Log and trace server-side failures with observability tools.
- Use error filters to shape GraphQL errors.
- Use typed schema results for expected product outcomes.

# Error Codes

Hot Chocolate serializes `IError.Code` as `extensions.code`.

```json
{
  "errors": [
    {
      "message": "An internal error occurred.",
      "path": ["viewerBasket"],
      "extensions": {
        "code": "INTERNAL_ERROR"
      }
    }
  ]
}
```

Use codes when a client must branch on a top-level GraphQL error. Keep codes stable, documented, and independent from human-readable messages. For domain errors, prefer typed fields on schema error objects, often including a domain-specific `code` field when the client needs one.

Hot Chocolate also emits built-in codes for some features. Authorization failures can include codes such as `AUTH_NOT_AUTHENTICATED`. Treat documented scenario codes as useful response data, but do not build your application contract on every internal Hot Chocolate code unless that code is documented for your scenario.

# Authorization and Validation Errors

Validation errors occur when the GraphQL document is invalid for the schema or execution rules. They are top-level GraphQL errors because execution cannot proceed as requested.

Hot Chocolate authorization can reject a selected field during execution. In that case, the response can still include sibling data, and the rejected field follows the same null behavior as other field errors.

```json
{
  "errors": [
    {
      "message": "The current user is not authorized to access this resource.",
      "path": ["viewerBasket"],
      "extensions": {
        "code": "AUTH_NOT_AUTHENTICATED"
      }
    }
  ],
  "data": {
    "viewerBasket": null
  }
}
```

If authorization is part of business state, for example if a mutation returns `NotAllowed` as a normal product outcome, model that as a typed domain result instead.

# Troubleshooting

| Symptom                                                     | Likely cause                                                    | What to do                                                                                              |
| ----------------------------------------------------------- | --------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------- |
| Client sees `Unexpected Execution Error`                    | A resolver threw an unexpected exception and details are hidden | Check server logs and diagnostics. Add a filter or code only if the client needs a safe response shape. |
| `data` is `null` after one nested field failed              | Null propagation crossed non-null fields                        | Review the field nullability and the error path. Use nullable boundaries where partial data is useful.  |
| A business validation error appears in top-level `errors`   | The resolver threw instead of returning a domain result         | Model the outcome with mutation payload errors, an explicit union/result type, or `FieldResult`.        |
| Clients parse error messages                                | No stable code or typed field exists                            | Add `extensions.code` for GraphQL errors or schema fields for domain errors.                            |
| An error filter causes another `Unexpected Execution Error` | The filter returned `null` or threw                             | Always return an `IError` and keep filter logic defensive.                                              |
| Exception details appear in responses                       | `IncludeExceptionDetails` is enabled                            | Disable it outside development.                                                                         |

# Where to Go Next

- [Create and report errors](./create-and-report-errors) for `IError`, `ErrorBuilder`, `GraphQLException`, returned errors, and `ReportError`.
- [Error filters](./error-filters) for `IErrorFilter`, filter ordering, dependency injection, sanitization, and enrichment.
- [Exception details](./exception-details) for `IncludeExceptionDetails`, debugger defaults, and production redaction.
- [Domain errors](./domain-errors) for choosing between GraphQL errors and typed schema results.
- [Mutation errors](./mutation-errors) for mutation conventions, `[Error]`, generated payload errors, factories, and custom error interfaces.
- [FieldResult](./field-result) for explicit success-or-error resolver returns.
- [Query result errors](./query-result-errors) for query result patterns and explicit result unions.
- [Error codes](./error-codes) for `extensions.code`, custom code naming, and client contracts.
- [Null propagation](./null-propagation) for partial data, non-null wrappers, and `ErrorHandlingMode`.
- [Troubleshooting](./troubleshooting) for common response-shape and production-safety issues.

Related topics:

- [Authorization](/docs/hotchocolate/v16/build/security/authorization) for field authorization behavior.
- [Diagnostic events](/docs/hotchocolate/v16/build/observability/diagnostic-events) for server-side error observation.
- [Mutation fields](/docs/hotchocolate/v16/build/schema-elements/operations-mutations) for mutation schema design.
- [Request options](/docs/hotchocolate/v16/build/server-configuration/schema-options) for execution options such as `IncludeExceptionDetails`.
