---
title: Execution Engine
description: Learn about the Hot Chocolate v16 request execution pipeline, keyed middleware, and how to add custom middleware with precise ordering.
---

# Overview

The Hot Chocolate execution engine processes GraphQL requests through a pipeline of request middleware. Each middleware handles one step of execution, such as parsing the document, validating semantics, or executing the operation. You can extend, replace, or reorder middleware in this pipeline.

# Request Pipeline

When a GraphQL request arrives, the execution engine passes a `RequestContext` through a chain of middleware. Each middleware performs its work and then calls the next middleware in the chain. The default pipeline includes the following middleware, in order:

1. **InstrumentationMiddleware** -- Records diagnostic events and telemetry
2. **ExceptionMiddleware** -- Catches unhandled exceptions and converts them to GraphQL errors
3. **TimeoutMiddleware** -- Enforces execution timeout
4. **DocumentCacheMiddleware** -- Looks up previously parsed documents in the cache
5. **DocumentParserMiddleware** -- Parses the GraphQL document from source text
6. **DocumentValidationMiddleware** -- Validates the document against the schema
7. **OperationCacheMiddleware** -- Looks up previously compiled operations in the cache
8. **OperationResolverMiddleware** -- Resolves and compiles the operation from the document
9. **SkipWarmupExecutionMiddleware** -- Short-circuits execution for warmup requests
10. **OperationVariableCoercionMiddleware** -- Coerces variable values to their expected types
11. **OperationExecutionMiddleware** -- Executes the compiled operation and produces a result

# RequestContext

In v16, the `IRequestContext` interface has been replaced by the concrete `RequestContext` class. This class carries all request state through the pipeline.

Key properties on `RequestContext`:

| Property                | Type                                       | Description                             |
| ----------------------- | ------------------------------------------ | --------------------------------------- |
| `Schema`                | `ISchemaDefinition`                        | The schema the request executes against |
| `Request`               | `IOperationRequest`                        | The incoming GraphQL request            |
| `RequestServices`       | `IServiceProvider`                         | The request-scoped service provider     |
| `OperationDocumentInfo` | `OperationDocumentInfo`                    | Parsed document metadata                |
| `RequestAborted`        | `CancellationToken`                        | Cancellation token for the request      |
| `VariableValues`        | `ImmutableArray<IVariableValueCollection>` | Coerced variable value sets             |
| `Result`                | `IExecutionResult?`                        | The execution result                    |
| `ContextData`           | `IDictionary<string, object?>`             | Arbitrary request state                 |
| `Features`              | `IFeatureCollection`                       | Feature collection for extensibility    |

# OperationDocumentInfo

Document-related information that was previously scattered across `IRequestContext` properties is now consolidated into the `OperationDocumentInfo` class, accessible via `RequestContext.OperationDocumentInfo`.

| Property         | Type                    | Description                                                |
| ---------------- | ----------------------- | ---------------------------------------------------------- |
| `Document`       | `DocumentNode?`         | The parsed GraphQL document                                |
| `Id`             | `OperationDocumentId`   | Unique document identifier                                 |
| `Hash`           | `OperationDocumentHash` | Hash of the document                                       |
| `OperationCount` | `int`                   | Number of operation definitions in the document            |
| `IsCached`       | `bool`                  | Whether the document was retrieved from the cache          |
| `IsPersisted`    | `bool`                  | Whether the document came from a persisted operation store |
| `IsValidated`    | `bool`                  | Whether the document has been validated                    |

# Keyed Middleware Pipeline

In v16, the request pipeline uses a keyed middleware system. Every built-in middleware has a unique key defined in `WellKnownRequestMiddleware`. This allows you to insert custom middleware at a precise position relative to any built-in middleware.

## Adding Custom Middleware

Use the `UseRequest()` method on `IRequestExecutorBuilder` to add middleware. The method accepts optional `key`, `before`, and `after` parameters for positioning.

### Append to the end of the pipeline

When you call `UseRequest()` without positioning parameters, the middleware is appended to the end of the pipeline:

```csharp
builder.Services
    .AddGraphQLServer()
    .UseRequest(next => async context =>
    {
        // Custom logic before the next middleware
        await next(context);
        // Custom logic after the next middleware
    });
```

### Insert before a specific middleware

Use the `before` parameter with a `WellKnownRequestMiddleware` constant to insert your middleware before a built-in one:

```csharp
builder.Services
    .AddGraphQLServer()
    .UseRequest(
        middleware: next => async context =>
        {
            // Runs before document validation
            await next(context);
        },
        key: "MyPreValidationMiddleware",
        before: WellKnownRequestMiddleware.DocumentValidationMiddleware);
```

### Insert after a specific middleware

Use the `after` parameter to insert your middleware after a built-in one:

```csharp
builder.Services
    .AddGraphQLServer()
    .UseRequest(
        middleware: next => async context =>
        {
            // Runs after document parsing completes
            await next(context);
        },
        key: "MyPostParsingMiddleware",
        after: WellKnownRequestMiddleware.DocumentParserMiddleware);
```

### Prevent duplicate registration

Set `allowMultiple` to `false` (the default) so that if a middleware with the same key already exists, the registration is skipped:

```csharp
builder.Services
    .AddGraphQLServer()
    .UseRequest(
        middleware: next => async context =>
        {
            await next(context);
        },
        key: "MyMiddleware",
        after: WellKnownRequestMiddleware.ExceptionMiddleware,
        allowMultiple: false);
```

> You can specify either `before` or `after`, but not both at the same time. If neither is specified, the middleware is appended to the end.

## Using a Class-Based Middleware

You can define middleware as a class instead of a delegate. The class receives the next `RequestDelegate` in its constructor:

```csharp
public class MyRequestMiddleware
{
    private readonly RequestDelegate _next;

    public MyRequestMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async ValueTask InvokeAsync(RequestContext context)
    {
        // Pre-processing logic

        await _next(context);

        // Post-processing logic
    }
}
```

Register it with precise positioning using the generic `UseRequest<T>()` overload:

```csharp
builder.Services
    .AddGraphQLServer()
    .UseRequest<MyRequestMiddleware>(
        key: "MyRequestMiddleware",
        after: WellKnownRequestMiddleware.DocumentValidationMiddleware);
```

## WellKnownRequestMiddleware Constants

The `WellKnownRequestMiddleware` static class provides constants for all built-in middleware keys:

| Constant                                        | Description                            |
| ----------------------------------------------- | -------------------------------------- |
| `InstrumentationMiddleware`                     | Diagnostic events and telemetry        |
| `ExceptionMiddleware`                           | Unhandled exception handling           |
| `TimeoutMiddleware`                             | Execution timeout enforcement          |
| `DocumentCacheMiddleware`                       | Document cache lookup                  |
| `DocumentParserMiddleware`                      | GraphQL document parsing               |
| `DocumentValidationMiddleware`                  | Document validation                    |
| `OperationCacheMiddleware`                      | Compiled operation cache lookup        |
| `OperationResolverMiddleware`                   | Operation resolution and compilation   |
| `SkipWarmupExecutionMiddleware`                 | Warmup request short-circuit           |
| `OperationVariableCoercionMiddleware`           | Variable coercion                      |
| `OperationExecutionMiddleware`                  | Operation execution                    |
| `ReadPersistedOperationMiddleware`              | Persisted operation lookup             |
| `WritePersistedOperationMiddleware`             | Persisted operation storage            |
| `PersistedOperationNotFoundMiddleware`          | Persisted operation not-found handling |
| `AutomaticPersistedOperationNotFoundMiddleware` | APQ not-found handling                 |
| `OnlyPersistedOperationsAllowed`                | Enforce persisted-operations-only mode |
| `AuthorizeRequestMiddleware`                    | Request authorization                  |
| `PrepareAuthorizationMiddleware`                | Authorization preparation              |
| `CostAnalyzerMiddleware`                        | Cost analysis                          |

# Field Middleware

Field middleware runs during field resolution, allowing you to add logic before or after a resolver executes. Field middleware is separate from request middleware and operates at the field level.

[Learn more about field middleware](/docs/hotchocolate/v16/execution-engine/field-middleware)

# Resolver Compiler

The resolver compiler builds an optimized resolver pipeline for each field. You can customize it by providing parameter expression builders.

# Next Steps

- [Field middleware](/docs/hotchocolate/v16/execution-engine/field-middleware) for per-field processing
- [Instrumentation](/docs/hotchocolate/v16/server/instrumentation) for diagnostic events
- [Error handling](/docs/hotchocolate/v16/api-reference/errors) for error filters and error builders
