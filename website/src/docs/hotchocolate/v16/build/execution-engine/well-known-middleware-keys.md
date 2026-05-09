---
title: Well-known middleware keys
---

Well-known middleware keys provide stable anchor points within the Hot Chocolate v16 execution pipeline. These keys let you specify exactly where your custom middleware should run in relation to built-in middleware.

Hot Chocolate uses two main middleware pipelines:

- **Request middleware** executes once per operation request. Use `HotChocolate.Execution.WellKnownRequestMiddleware` with `UseRequest(..., before:, after:)` to position your middleware.
- **Field middleware** executes for each selected field. The `HotChocolate.WellKnownMiddleware` class provides identifiers for built-in field middleware used for validation and diagnostics. Field middleware is ordered by the sequence of descriptor method calls or by the `Order` attribute, not by request-style anchors.

Always use the provided constants rather than copying string values. Optional feature keys are only available if the corresponding feature is registered.

# Adding Request Middleware at a Specific Point

In v16, the request pipeline passes a `RequestContext` through each middleware component. You can add custom middleware using `UseRequest` on `IRequestExecutorBuilder`.

```csharp
using HotChocolate.Execution;

builder
    .AddGraphQL()
    .UseRequest(
        middleware: next => async context =>
        {
            // This runs after parsing and before subsequent middleware.
            await next(context);
        },
        key: "Example.GraphQL.AfterParsing",
        after: WellKnownRequestMiddleware.DocumentParserMiddleware);
```

To run middleware before validation, anchor it before the validation middleware:

```csharp
using HotChocolate.Execution;

builder
    .AddGraphQL()
    .UseRequest(
        middleware: next => async context =>
        {
            await next(context);
        },
        key: "Example.GraphQL.BeforeValidation",
        before: WellKnownRequestMiddleware.DocumentValidationMiddleware);
```

Use a namespaced custom key for your middleware. Set `allowMultiple: false` if a shared library might register the same middleware more than once.

```csharp
using HotChocolate.Execution;

builder
    .AddGraphQL()
    .UseRequest(
        middleware: next => async context =>
        {
            await next(context);
        },
        key: "Contoso.GraphQL.Audit",
        after: WellKnownRequestMiddleware.ExceptionMiddleware,
        allowMultiple: false);
```

**Positioning rules for `UseRequest`:**

- Specify either `before` or `after`, not both.
- When positioning middleware and `allowMultiple` is `false`, you must provide a `key`.
- If the anchor key is missing, executor construction fails with an invalid operation error.
- If you omit both `before` and `after`, the middleware is appended to the end of the request pipeline.

## Using Class-Based Request Middleware

If your middleware has dependencies or is part of a reusable package, implement it as a class.

```csharp
using HotChocolate.Execution;

public sealed class AuditRequestMiddleware
{
    private readonly RequestDelegate _next;

    public AuditRequestMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async ValueTask InvokeAsync(RequestContext context)
    {
        await _next(context);
    }
}
```

Register the class-based middleware with the same keying approach:

```csharp
using HotChocolate.Execution;

builder
    .AddGraphQL()
    .UseRequest<AuditRequestMiddleware>(
        key: "Contoso.GraphQL.Audit",
        after: WellKnownRequestMiddleware.DocumentValidationMiddleware);
```

# Reference: Request Middleware Keys

The following constants are defined on `HotChocolate.Execution.WellKnownRequestMiddleware`.

## Core Executor Pipeline

The standard schema executor uses these core keys in the following order:

| Order | Constant                              | Description                                    | Typical Custom Anchor                                                                           |
| ----- | ------------------------------------- | ---------------------------------------------- | ----------------------------------------------------------------------------------------------- |
| 1     | `InstrumentationMiddleware`           | Diagnostic events and telemetry                | Rare. Prefer instrumentation hooks for telemetry.                                               |
| 2     | `ExceptionMiddleware`                 | Converts uncaught exceptions to GraphQL errors | Place request cleanup or auditing after this to ensure exception handling wraps your code.      |
| 3     | `TimeoutMiddleware`                   | Sets up execution timeout and cancellation     | Place cache or request preparation after timeout if it should observe cancellation.             |
| 4     | `DocumentCacheMiddleware`             | Looks up parsed document cache                 | Anchor persisted-operation work near this only when building request pipeline extensions.       |
| 5     | `DocumentParserMiddleware`            | Parses the GraphQL document                    | Run after this if your code needs the parsed document.                                          |
| 6     | `DocumentValidationMiddleware`        | Validates the document                         | Run before to add validation context, or after to depend on a valid document.                   |
| 7     | `OperationCacheMiddleware`            | Looks up prepared operation cache              | For advanced operation preparation extensions.                                                  |
| 8     | `OperationResolverMiddleware`         | Selects and compiles the operation             | Run after this if you need the selected operation.                                              |
| 9     | `SkipWarmupExecutionMiddleware`       | Short-circuits warmup                          | Rare.                                                                                           |
| 10    | `OperationVariableCoercionMiddleware` | Coerces variables                              | Run after this if you need coerced variable values.                                             |
| 11    | `ConcurrencyGateMiddleware`           | Controls concurrency                           | Run before to measure queueing, or after for work that should count against execution capacity. |
| 12    | `OperationExecutionMiddleware`        | Executes the operation                         | Run before to inspect the final request state before resolvers execute.                         |

## Persisted Operation Keys

Persisted operations and automatic persisted operations insert middleware between document cache and parsing:

| Constant                                        | Description                                                                 | Availability                        |
| ----------------------------------------------- | --------------------------------------------------------------------------- | ----------------------------------- |
| `ReadPersistedOperationMiddleware`              | Looks up the stored operation document                                      | Persisted operation features        |
| `PersistedOperationNotFoundMiddleware`          | Handles missing persisted operations                                        | Persisted operation pipelines       |
| `OnlyPersistedOperationsAllowed`                | Rejects non-persisted operations when only persisted operations are allowed | Persisted operation pipelines       |
| `AutomaticPersistedOperationNotFoundMiddleware` | Handles APQ not-found responses                                             | Automatic persisted query pipelines |
| `WritePersistedOperationMiddleware`             | Stores an automatic persisted operation                                     | Automatic persisted query pipelines |

**Important:** Only anchor to these keys if the corresponding persisted operation feature is registered.

## Optional Feature Keys

| Constant                         | Description                                     | Default Insertion Point               | Availability          |
| -------------------------------- | ----------------------------------------------- | ------------------------------------- | --------------------- |
| `PrepareAuthorizationMiddleware` | Prepares GraphQL authorization state            | Before `DocumentValidationMiddleware` | GraphQL authorization |
| `AuthorizeRequestMiddleware`     | Performs request authorization after validation | After `DocumentValidationMiddleware`  | GraphQL authorization |
| `CostAnalyzerMiddleware`         | Runs cost analysis                              | After `DocumentValidationMiddleware`  | Cost analysis         |
| `QueryCacheMiddleware`           | Enables query result caching                    | After `TimeoutMiddleware`             | Query cache feature   |

For example, to run middleware after request authorization (only when GraphQL authorization is enabled):

```csharp
using HotChocolate.Execution;

builder
    .AddGraphQL()
    .AddAuthorization()
    .UseRequest(
        middleware: next => async context =>
        {
            await next(context);
        },
        key: "Contoso.GraphQL.AfterAuthorization",
        after: WellKnownRequestMiddleware.AuthorizeRequestMiddleware);
```

If `.AddAuthorization()` is not included in the schema configuration, the anchor will not exist.

To run middleware after cost analysis (only when cost analysis is registered):

```csharp
using HotChocolate.Execution;

builder
    .AddGraphQL()
    .AddCostAnalyzer()
    .UseRequest(
        middleware: next => async context =>
        {
            await next(context);
        },
        key: "Contoso.GraphQL.AfterCost",
        after: WellKnownRequestMiddleware.CostAnalyzerMiddleware);
```

## Fusion Gateway Keys

| Constant                       | Description                        | Availability            |
| ------------------------------ | ---------------------------------- | ----------------------- |
| `OperationPlanCacheMiddleware` | Fusion operation plan cache lookup | Fusion gateway pipeline |
| `OperationPlanMiddleware`      | Fusion operation planning          | Fusion gateway pipeline |

These keys are specific to Fusion gateway pipelines. Use them only when configuring a Fusion gateway.

# Field Middleware Keys: Diagnostics and Validation

Field middleware does not use `UseRequest` and does not provide public `before` or `after` anchor parameters on object field descriptors. Instead, order field middleware by the sequence of descriptor method calls, or by the `Order` attribute when using descriptor attributes.

The following constants are defined on `HotChocolate.WellKnownMiddleware`:

| Constant               | Key Value                                  | Registered By                                                 | Application Guidance                                                                               |
| ---------------------- | ------------------------------------------ | ------------------------------------------------------------- | -------------------------------------------------------------------------------------------------- |
| `Paging`               | `HotChocolate.Types.Paging`                | `.UsePaging()`, `[UsePaging]`                                 | Common data middleware. Follow the required order below.                                           |
| `Projection`           | `HotChocolate.Data.Projection`             | `.UseProjection()`, `[UseProjection]`                         | Common data middleware. Follow the required order below.                                           |
| `Filtering`            | `HotChocolate.Data.Filtering`              | `.UseFiltering()`, `[UseFiltering]`                           | Common data middleware. Follow the required order below.                                           |
| `Sorting`              | `HotChocolate.Data.Sorting`                | `.UseSorting()`, `[UseSorting]`                               | Common data middleware. Follow the required order below.                                           |
| `DataLoader`           | `HotChocolate.Fetching.DataLoader`         | DataLoader field helpers                                      | Feature-specific. Prefer the DataLoader APIs.                                                      |
| `GlobalId`             | `HotChocolate.Types.GlobalId`              | Relay global ID and node helpers                              | Feature-specific. Prefer Relay APIs.                                                               |
| `SingleOrDefault`      | `HotChocolate.Data.SingleOrDefault`        | Data projection helpers                                       | Feature-specific. Prefer documented data APIs.                                                     |
| `Authorization`        | `HotChocolate.Authorization`               | `.Authorize()`, `[Authorize]`, authorization type interceptor | Feature-specific. Configure authorization through the authorization APIs.                          |
| `DbContext`            | `HotChocolate.Data.EF.UseDbContext`        | Legacy or provider data middleware                            | Participates in data order validation. Do not add in new v16 code unless provider docs require it. |
| `ToList`               | `HotChocolate.Data.EF.ToList`              | Provider integrations                                         | Provider-oriented diagnostic key.                                                                  |
| `ResolverServiceScope` | `HotChocolate.Resolvers.ServiceScope`      | Resolver service infrastructure                               | Infrastructure diagnostic key.                                                                     |
| `PooledService`        | `HotChocolate.Resolvers.PooledService`     | Resolver service infrastructure                               | Infrastructure diagnostic key.                                                                     |
| `ResolverService`      | `HotChocolate.Resolvers.ResolverService`   | Resolver service infrastructure                               | Infrastructure diagnostic key.                                                                     |
| `MutationArguments`    | `HotChocolate.Types.Mutations.Arguments`   | Mutation convention interceptor                               | Mutation convention diagnostic key.                                                                |
| `MutationErrors`       | `HotChocolate.Types.Mutations.Errors`      | Mutation convention interceptor                               | Mutation convention diagnostic key.                                                                |
| `MutationErrorNull`    | `HotChocolate.Types.Mutations.Errors.Null` | Mutation convention interceptor                               | Mutation convention diagnostic key.                                                                |
| `MutationResult`       | `HotChocolate.Types.Mutations.Result`      | Mutation convention interceptor                               | Mutation convention diagnostic key.                                                                |

Most application code should not use these raw key values directly. Instead, use the feature method or attribute that registers the middleware.

# Applying Data Field Middleware in the Supported Order

For paging, projection, filtering, and sorting, use this declaration order:

```csharp
public sealed class QueryType : ObjectType<Query>
{
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor
            .Field(t => t.GetUsers(default!))
            .UsePaging()
            .UseProjection()
            .UseFiltering()
            .UseSorting();
    }
}

public sealed class Query
{
    public IQueryable<User> GetUsers(UserDbContext dbContext)
    {
        return dbContext.Users;
    }
}
```

This order may seem inverted at first. Field middleware calls `next(context)` in declaration order, and the resolver result flows back in reverse order. This allows sorting, filtering, and projection to shape the result before paging creates the connection or page.

If you use attributes, maintain the same top-to-bottom order:

```csharp
public sealed class Query
{
    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<User> GetUsers(UserDbContext dbContext)
    {
        return dbContext.Users;
    }
}
```

C# reflection does not guarantee attribute order. Hot Chocolate middleware attributes use `Order`, which is usually set from the caller's line number. If you derive from a middleware attribute, pass the `order` argument through:

```csharp
using System.Reflection;
using System.Runtime.CompilerServices;
using HotChocolate.Types.Descriptors;

public sealed class UseAuditAttribute : ObjectFieldDescriptorAttribute
{
    public UseAuditAttribute([CallerLineNumber] int order = 0)
    {
        Order = order;
    }

    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo? member)
    {
        descriptor.UseAudit();
    }
}
```

# Placing Custom Field Middleware by Result Needs

Insert custom field middleware in the descriptor chain at the point where its `await next(context)` should execute.

```csharp
public static class ObjectFieldDescriptorExtensions
{
    public static IObjectFieldDescriptor UseAudit(
        this IObjectFieldDescriptor descriptor)
    {
        return descriptor.Use(next => async context =>
        {
            // Runs before later middleware and the resolver.
            await next(context);
            // Runs after later middleware and the resolver have produced a result.
        });
    }
}
```

Common placements:

```csharp
descriptor
    .Field(t => t.GetUsers(default!))
    .UseAudit()
    .UsePaging()
    .UseProjection()
    .UseFiltering()
    .UseSorting();
```

`UseAudit` above wraps the data middleware and sees the final field result after paging has run.

```csharp
descriptor
    .Field(t => t.GetUsers(default!))
    .UsePaging()
    .UseProjection()
    .UseFiltering()
    .UseAudit()
    .UseSorting();
```

`UseAudit` above runs between filtering and sorting in the field middleware chain. Use this style when you need a specific position relative to paging, projection, filtering, or sorting.

```csharp
descriptor
    .Field(t => t.GetUsers(default!))
    .UsePaging()
    .UseProjection()
    .UseFiltering()
    .UseSorting()
    .UseAudit();
```

`UseAudit` above runs closest to the resolver. After it awaits `next(context)`, it sees the raw resolver result before the data middleware transforms it on the way back out.

# Troubleshoot order problems

## HC0050 reports invalid data middleware order

Check for this order on the field:

1. `UsePaging`
2. `UseProjection`
3. `UseFiltering`
4. `UseSorting`

If a provider or legacy setup adds `UseDbContext`, it must appear before those data middleware keys. `ValidatePipelineOrder` is enabled by default. The validator checks `DbContext`, `Paging`, `Projection`, `Filtering`, and `Sorting`; other middleware may appear as `...` in the error message.

## Duplicate data middleware is detected

Look for repeated descriptor calls, repeated attributes, inherited attributes, or conventions that add the same data middleware. For example, do not combine `[UseProjection]` with another convention that adds projection to the same field.

## A request middleware anchor is not found

Confirm that the feature that owns the anchor is registered. Authorization, cost analysis, query cache, persisted operations, and Fusion planning add optional keys. Also confirm that you are configuring the ordinary executor or the Fusion gateway pipeline you intended.

## Authorization does not run

Request middleware keys are not a replacement for authorization setup. Configure ASP.NET Core authentication and authorization, call the GraphQL authorization registration method, and use Hot Chocolate authorization attributes or descriptors.

# When descriptor order is enough

Most custom field middleware does not need key-level knowledge. If your code wraps a resolver, reads `IMiddlewareContext.Result`, or adds per-field logging, place it directly in the field descriptor chain. Use well-known field keys mainly to understand diagnostics, `HC0050`, and the built-in data middleware order.

For request middleware, append without an anchor when your middleware does not depend on parsed documents, validation, selected operations, coerced variables, authorization, cost analysis, or execution timing.

# Checklist

- Use `WellKnownRequestMiddleware` constants with `UseRequest(before:)` or `UseRequest(after:)` for request middleware.
- Give custom request middleware a stable, namespaced key.
- Do not anchor to optional feature keys unless the feature is registered.
- Use descriptor chain order or attribute `Order` for field middleware.
- Declare data middleware as `UsePaging`, `UseProjection`, `UseFiltering`, `UseSorting`.
- Treat raw key strings as diagnostics, not application configuration.
- Recheck custom anchors when moving to Fusion or upgrading major versions.
