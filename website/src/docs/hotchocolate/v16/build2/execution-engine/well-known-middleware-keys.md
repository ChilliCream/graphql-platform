---
title: Well-known middleware keys
---

Use well-known middleware keys when you need a stable point in the Hot Chocolate v16 execution pipeline. The key tells Hot Chocolate which built-in middleware you want to run before or after.

There are two different pipelines:

- **Request middleware** runs once for an operation request. Use `HotChocolate.Execution.WellKnownRequestMiddleware` with `UseRequest(..., before:, after:)`.
- **Field middleware** runs for a selected field. `HotChocolate.WellKnownMiddleware` identifies built-in field middleware for validation and diagnostics. Order field middleware with descriptor chain order or attribute `Order`, not request-style anchors.

Use constants instead of copying string values. Treat optional feature keys as available only when that feature is registered.

# Add request middleware at a known point

The v16 request pipeline passes a `RequestContext` through middleware. Add custom middleware with `UseRequest` on `IRequestExecutorBuilder`.

```csharp
using HotChocolate.Execution;

builder
    .AddGraphQL()
    .UseRequest(
        middleware: next => async context =>
        {
            // Runs after parsing and before later middleware.
            await next(context);
        },
        key: "Example.GraphQL.AfterParsing",
        after: WellKnownRequestMiddleware.DocumentParserMiddleware);
```

If you want to run before validation, anchor before validation instead:

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

Use a namespaced custom key for your middleware. Keep `allowMultiple: false` when a shared library might register the same middleware more than once.

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

`UseRequest` has these positioning rules:

- Specify `before` or `after`, not both.
- When you position middleware and `allowMultiple` is `false`, provide a `key`.
- If the anchor key is missing, executor construction fails with an invalid operation error.
- When you omit `before` and `after`, the middleware is appended to the request pipeline.

## Use class-based request middleware

Use a class when the middleware has dependencies or belongs in a reusable package.

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

Register it with the same keys:

```csharp
using HotChocolate.Execution;

builder
    .AddGraphQL()
    .UseRequest<AuditRequestMiddleware>(
        key: "Contoso.GraphQL.Audit",
        after: WellKnownRequestMiddleware.DocumentValidationMiddleware);
```

# Request middleware key reference

These constants are defined on `HotChocolate.Execution.WellKnownRequestMiddleware`.

## Core executor pipeline

The ordinary schema executor uses these core keys in this order.

| Order | Constant                              | What it identifies                              | Typical custom anchor                                                                                 |
| ----- | ------------------------------------- | ----------------------------------------------- | ----------------------------------------------------------------------------------------------------- |
| 1     | `InstrumentationMiddleware`           | Diagnostic events and telemetry.                | Rare. Prefer instrumentation hooks for telemetry.                                                     |
| 2     | `ExceptionMiddleware`                 | Converts uncaught exceptions to GraphQL errors. | Put request cleanup or auditing after it when you want exception handling around your code.           |
| 3     | `TimeoutMiddleware`                   | Execution timeout and cancellation setup.       | Put cache or request preparation after timeout if it should observe cancellation.                     |
| 4     | `DocumentCacheMiddleware`             | Parsed document cache lookup.                   | Anchor persisted-operation work near this only when building request pipeline extensions.             |
| 5     | `DocumentParserMiddleware`            | GraphQL document parsing.                       | Run after it when your code needs the parsed document.                                                |
| 6     | `DocumentValidationMiddleware`        | Document validation.                            | Run before it to add validation context, or after it to depend on a valid document.                   |
| 7     | `OperationCacheMiddleware`            | Prepared operation cache lookup.                | Advanced operation preparation extensions.                                                            |
| 8     | `OperationResolverMiddleware`         | Operation selection and compilation.            | Run after it when you need the selected operation.                                                    |
| 9     | `SkipWarmupExecutionMiddleware`       | Warmup short-circuit.                           | Rare.                                                                                                 |
| 10    | `OperationVariableCoercionMiddleware` | Variable coercion.                              | Run after it when you need coerced variable values.                                                   |
| 11    | `ConcurrencyGateMiddleware`           | Concurrency control.                            | Run before it to measure queueing, or after it for work that should count against execution capacity. |
| 12    | `OperationExecutionMiddleware`        | Operation execution.                            | Run before it to inspect final request state before resolvers execute.                                |

## Persisted operation keys

Persisted operations and automatic persisted operations insert middleware between document cache and parsing.

| Constant                                        | What it identifies                                                           | Availability                         |
| ----------------------------------------------- | ---------------------------------------------------------------------------- | ------------------------------------ |
| `ReadPersistedOperationMiddleware`              | Looks up the stored operation document.                                      | Persisted operation features.        |
| `PersistedOperationNotFoundMiddleware`          | Handles missing persisted operations.                                        | Persisted operation pipelines.       |
| `OnlyPersistedOperationsAllowed`                | Rejects non-persisted operations when only persisted operations are allowed. | Persisted operation pipelines.       |
| `AutomaticPersistedOperationNotFoundMiddleware` | Handles APQ not-found responses.                                             | Automatic persisted query pipelines. |
| `WritePersistedOperationMiddleware`             | Stores an automatic persisted operation.                                     | Automatic persisted query pipelines. |

Do not anchor to these keys unless the persisted operation feature that owns the key is registered.

## Optional feature keys

| Constant                         | What it identifies                               | Default insertion point                | Availability           |
| -------------------------------- | ------------------------------------------------ | -------------------------------------- | ---------------------- |
| `PrepareAuthorizationMiddleware` | Prepares GraphQL authorization state.            | Before `DocumentValidationMiddleware`. | GraphQL authorization. |
| `AuthorizeRequestMiddleware`     | Performs request authorization after validation. | After `DocumentValidationMiddleware`.  | GraphQL authorization. |
| `CostAnalyzerMiddleware`         | Runs cost analysis.                              | After `DocumentValidationMiddleware`.  | Cost analysis.         |
| `QueryCacheMiddleware`           | Runs query result caching support.               | After `TimeoutMiddleware`.             | Query cache feature.   |

For example, run after request authorization only when GraphQL authorization is enabled:

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

If `.AddAuthorization()` is not part of the schema configuration, the anchor does not exist.

Run after cost analysis only when cost analysis is registered:

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

## Fusion gateway keys

| Constant                       | What it identifies                  | Availability             |
| ------------------------------ | ----------------------------------- | ------------------------ |
| `OperationPlanCacheMiddleware` | Fusion operation plan cache lookup. | Fusion gateway pipeline. |
| `OperationPlanMiddleware`      | Fusion operation planning.          | Fusion gateway pipeline. |

These are not ordinary schema executor keys. Use them only when configuring a Fusion gateway pipeline.

# Field middleware keys are diagnostics and validation identifiers

Field middleware does not use `UseRequest` and does not expose public `before` or `after` anchor parameters on object field descriptors. Order field middleware by the order you call descriptor methods, or by `Order` when you use descriptor attributes.

The constants are defined on `HotChocolate.WellKnownMiddleware`.

| Constant               | Key value                                  | Registered by                                                 | App guidance                                                                                          |
| ---------------------- | ------------------------------------------ | ------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------- |
| `Paging`               | `HotChocolate.Types.Paging`                | `.UsePaging()`, `[UsePaging]`                                 | Common data middleware. Follow the required order below.                                              |
| `Projection`           | `HotChocolate.Data.Projection`             | `.UseProjection()`, `[UseProjection]`                         | Common data middleware. Follow the required order below.                                              |
| `Filtering`            | `HotChocolate.Data.Filtering`              | `.UseFiltering()`, `[UseFiltering]`                           | Common data middleware. Follow the required order below.                                              |
| `Sorting`              | `HotChocolate.Data.Sorting`                | `.UseSorting()`, `[UseSorting]`                               | Common data middleware. Follow the required order below.                                              |
| `DataLoader`           | `HotChocolate.Fetching.DataLoader`         | DataLoader field helpers                                      | Feature-specific. Prefer the DataLoader APIs.                                                         |
| `GlobalId`             | `HotChocolate.Types.GlobalId`              | Relay global ID and node helpers                              | Feature-specific. Prefer Relay APIs.                                                                  |
| `SingleOrDefault`      | `HotChocolate.Data.SingleOrDefault`        | Data projection helpers                                       | Feature-specific. Prefer documented data APIs.                                                        |
| `Authorization`        | `HotChocolate.Authorization`               | `.Authorize()`, `[Authorize]`, authorization type interceptor | Feature-specific. Configure authorization through the authorization APIs.                             |
| `DbContext`            | `HotChocolate.Data.EF.UseDbContext`        | Legacy or provider data middleware                            | Participates in data order validation. Do not add it in new v16 code unless provider docs require it. |
| `ToList`               | `HotChocolate.Data.EF.ToList`              | Provider integrations                                         | Provider-oriented diagnostic key.                                                                     |
| `ResolverServiceScope` | `HotChocolate.Resolvers.ServiceScope`      | Resolver service infrastructure                               | Infrastructure diagnostic key.                                                                        |
| `PooledService`        | `HotChocolate.Resolvers.PooledService`     | Resolver service infrastructure                               | Infrastructure diagnostic key.                                                                        |
| `ResolverService`      | `HotChocolate.Resolvers.ResolverService`   | Resolver service infrastructure                               | Infrastructure diagnostic key.                                                                        |
| `MutationArguments`    | `HotChocolate.Types.Mutations.Arguments`   | Mutation convention interceptor                               | Mutation convention diagnostic key.                                                                   |
| `MutationErrors`       | `HotChocolate.Types.Mutations.Errors`      | Mutation convention interceptor                               | Mutation convention diagnostic key.                                                                   |
| `MutationErrorNull`    | `HotChocolate.Types.Mutations.Errors.Null` | Mutation convention interceptor                               | Mutation convention diagnostic key.                                                                   |
| `MutationResult`       | `HotChocolate.Types.Mutations.Result`      | Mutation convention interceptor                               | Mutation convention diagnostic key.                                                                   |

Most application code should not use these raw key values. Use the feature method or attribute that registers the middleware.

# Apply data field middleware in the supported order

Use this declaration order for paging, projection, filtering, and sorting:

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

This order can look inverted at first. Field middleware calls `next(context)` in declaration order, then the resolver result flows back in reverse order. That lets sorting, filtering, and projection shape the result before paging creates the connection or page.

If you use attributes, keep the same top-to-bottom order:

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

C# reflection does not guarantee attribute order. Hot Chocolate middleware attributes use `Order`, usually set from the caller line number. If you derive from a middleware attribute, pass the `order` argument through.

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

# Place custom field middleware by result needs

Put custom field middleware in the descriptor chain where its `await next(context)` should sit.

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
