---
title: Request context
---

The `RequestContext` is the per-operation envelope that Hot Chocolate uses throughout the v16 request pipeline. It is essential for writing request middleware, diagnostics, interceptors, enrichers, or other execution engine integrations.

In most cases, resolver code should not access `RequestContext` directly. Instead, place request-specific data such as tenant IDs, correlation IDs, and user information into global state before execution. Then, access this data in resolvers using resolver parameters or `IResolverContext`.

```text
HTTP or WebSocket message
  -> interceptor configures OperationRequestBuilder
  -> executor initializes RequestContext
  -> request middleware parses, validates, selects, coerces, executes
  -> resolvers use IResolverContext
  -> IExecutionResult returns to the transport
```

# Choosing the Right Context API

Several APIs use the term "context." Select the one that matches the lifetime of your task.

| API                            | Lifetime                                              | Available where                                                                                      | Use it for                                                                                                                                  | Avoid using it for                                           |
| ------------------------------ | ----------------------------------------------------- | ---------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------ |
| `RequestContext`               | One operation request item                            | Request middleware, request diagnostics, `IRequestContextEnricher`, low-level execution integrations | Pipeline state, document metadata, selected operation data, coerced variables, request services, cancellation, request result               | Routine field resolver logic or transport protocol details   |
| `IResolverContext`             | One field resolver invocation                         | Field middleware and resolvers                                                                       | Arguments, parent value, selected field, resolver services, resolver errors, global state, scoped state, local state, resolver cancellation | Whole-request pipeline control                               |
| `HttpContext`                  | One ASP.NET Core HTTP request or WebSocket connection | ASP.NET Core middleware, HTTP interceptors, socket session interceptors, HTTP-specific resolver code | Headers, cookies, endpoint data, request services, response details                                                                         | Non-HTTP execution paths or transport-neutral resolver state |
| `IExecutionResult.ContextData` | Result lifetime                                       | Result processing and server integrations                                                            | Server-side result metadata and cleanup coordination                                                                                        | Data you expect clients to receive                           |

`RequestContext.ContextData`, `IResolverContext.ContextData`, and `IExecutionResult.ContextData` each have distinct lifetimes. In resolver code, global state refers to request-wide data exposed through `IResolverContext.ContextData`, `[GlobalState]`, and global-state helper methods.

# Adding Request Data Before Execution

Data derived from the transport layer should be added in an interceptor. This approach keeps resolvers transport-neutral and allows Hot Chocolate to initialize the request with the correct services and built-in state.

```csharp
using HotChocolate;
using HotChocolate.AspNetCore;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Http;

public static class GraphQLStateKeys
{
    public const string TenantId = "TenantId";
    public const string CorrelationId = "CorrelationId";
}

public sealed class TenantHttpRequestInterceptor : DefaultHttpRequestInterceptor
{
    public override async ValueTask OnCreateAsync(
        HttpContext context,
        IRequestExecutor requestExecutor,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        await base.OnCreateAsync(
            context,
            requestExecutor,
            requestBuilder,
            cancellationToken);

        var tenantId = context.Request.Headers["X-Tenant-Id"]
            .FirstOrDefault()
            ?.Trim();

        if (string.IsNullOrEmpty(tenantId))
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("The X-Tenant-Id header is required.")
                    .SetCode("TENANT_REQUIRED")
                    .Build());
        }

        requestBuilder.SetGlobalState(GraphQLStateKeys.TenantId, tenantId);

        var correlationId = context.Request.Headers["X-Correlation-Id"]
            .FirstOrDefault()
            ?? context.TraceIdentifier;

        requestBuilder.TryAddGlobalState(
            GraphQLStateKeys.CorrelationId,
            correlationId);
    }
}
```

Register the interceptor with the GraphQL server:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddHttpRequestInterceptor<TenantHttpRequestInterceptor>();
```

When deriving from `DefaultHttpRequestInterceptor`, always call `base.OnCreateAsync(...)`. The default HTTP interceptor sets up request services and adds built-in global state such as `ClaimsPrincipal` and `HttpContext`. For WebSocket requests, use `DefaultSocketSessionInterceptor.OnRequestAsync`.

# Accessing Request Data in Resolvers

For known global state keys, prefer using resolver parameters.

```csharp
[QueryType]
public static partial class Query
{
    public static Task<IReadOnlyList<Product>> GetProductsAsync(
        [GlobalState(GraphQLStateKeys.TenantId)] string tenantId,
        ProductService products,
        CancellationToken cancellationToken)
        => products.GetByTenantAsync(tenantId, cancellationToken);
}
```

Expected SDL excerpt:

```graphql
type Query {
  products: [Product!]!
}
```

The tenant ID does not become a GraphQL argument. Hot Chocolate supplies it from request global state.

Use `IResolverContext` when the key is dynamic or optional:

```csharp
using HotChocolate.Resolvers;

[QueryType]
public static partial class DiagnosticsQueries
{
    public static string GetCorrelationId(IResolverContext context)
        => context.GetGlobalStateOrDefault<string>(
            GraphQLStateKeys.CorrelationId,
            "not-provided");
}
```

Do not read or mutate `RequestContext` from a resolver. Field resolvers can run in parallel, and the request context is an execution-engine object with a pooled lifetime.

# Know what lives in RequestContext

`RequestContext` exposes the request pipeline state for one operation request item.

| Property                | What it contains                                                                                                                                                                   | When to use it                                                                        |
| ----------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------- |
| `Schema`                | The schema definition used by the executor                                                                                                                                         | Middleware or diagnostics that need schema metadata                                   |
| `ExecutorVersion`       | The initialized request executor version                                                                                                                                           | Cache or diagnostics integrations that need executor identity                         |
| `Request`               | The original `IOperationRequest`, including document, document id or hash, operation name, raw variables, extensions, flags, features, initial global state, and optional services | Middleware that needs original request data                                           |
| `RequestServices`       | The service provider used for request execution                                                                                                                                    | Request middleware, enrichers, and low-level integrations                             |
| `OperationDocumentInfo` | Parsed document metadata such as `Document`, `Id`, `Hash`, `OperationCount`, `IsCached`, `IsPersisted`, and `IsValidated`                                                          | Middleware after document lookup, parsing, or validation                              |
| `RequestAborted`        | The request-level cancellation token                                                                                                                                               | Middleware and integrations that call cancellable APIs                                |
| `RequestIndex`          | The index of the item in an operation request batch, or `-1` outside request batches                                                                                               | Batch-aware diagnostics and result correlation                                        |
| `VariableValues`        | Coerced variable collections after variable coercion                                                                                                                               | Request middleware or diagnostics after variable coercion                             |
| `Result`                | The execution result assigned by the request pipeline                                                                                                                              | Middleware that observes or short-circuits results                                    |
| `ContextData`           | Mutable request-wide global state for the operation                                                                                                                                | Request middleware and enrichers. Resolver code should use resolver global-state APIs |
| `Features`              | Advanced feature collection used by Hot Chocolate integrations                                                                                                                     | Framework-level integrations or documented feature contracts                          |

Hot Chocolate v16 uses the concrete `RequestContext` type. The old `IRequestContext` interface no longer exists.

# Check which data is available

Custom request middleware can run before, after, or between built-in middleware. Place it where the state you need has been produced.

| Phase                                | Request context data normally available                                                                                        | Notes                                                                                    |
| ------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------- |
| Executor setup                       | `Schema`, `ExecutorVersion`, `Request`, `RequestServices`, `RequestAborted`, `RequestIndex`, initial `ContextData`, `Features` | Available before built-in middleware runs                                                |
| Document lookup or parsing           | `OperationDocumentInfo.Document`, `Id`, `Hash`, `IsCached`, `IsPersisted` as applicable                                        | A syntax or persisted-operation error can stop later phases                              |
| Validation                           | `OperationDocumentInfo.IsValidated`                                                                                            | Validation errors stop before resolver execution                                         |
| Operation resolution and compilation | `TryGetOperation(...)`, `GetOperation()`, `TryGetOperationDefinition(...)`, `TryGetOperationId(...)`                           | Use `TryGetOperation` when middleware may run before this phase or after a short-circuit |
| Variable coercion                    | `VariableValues`                                                                                                               | Variable batches can produce more than one coerced variable collection                   |
| Execution or exception handling      | `Result`                                                                                                                       | Middleware can assign `Result` and stop the pipeline                                     |

Optional features such as persisted operations, authorization, cost analysis, warmup, and Fusion can add middleware or short-circuit before later data exists.

# Inspect the selected operation in request middleware

Anchor request middleware with `WellKnownRequestMiddleware` keys when ordering matters. This middleware runs after operation resolution, reads the compiled operation if it exists, then continues execution.

```csharp
using HotChocolate.Execution;
using HotChocolate.Execution.Pipeline;

builder
    .AddGraphQL()
    .UseRequest(
        middleware: next => async context =>
        {
            if (context.TryGetOperation(out var operation, out var operationId))
            {
                var operationName = operation.Name ?? "anonymous";
                context.ContextData["OperationLabel"] =
                    $"{operationName}:{operationId}";
            }

            await next(context);
        },
        key: "OperationLabelMiddleware",
        after: WellKnownRequestMiddleware.OperationResolverMiddleware);
```

Use `GetOperation()` only when a missing selected operation is a programming error. Use `TryGetOperation(...)` for middleware that can run before operation resolution or after validation, authorization, or warmup has stopped execution.

# Observe or short-circuit results

`RequestContext.Result` is the output of the request pipeline. Middleware that runs after execution can observe it.

```csharp
using HotChocolate.Execution;
using HotChocolate.Execution.Pipeline;

builder
    .AddGraphQL()
    .UseRequest(
        middleware: next => async context =>
        {
            await next(context);

            if (context.Result is not null)
            {
                var correlationId = context.ContextData.TryGetValue(
                    GraphQLStateKeys.CorrelationId,
                    out var value)
                    ? value?.ToString()
                    : null;

                // Record lightweight metrics with the correlation id here.
            }
        },
        key: "RequestResultObserver",
        after: WellKnownRequestMiddleware.OperationExecutionMiddleware);
```

Request middleware can also stop the pipeline by assigning an `IExecutionResult` and returning without calling `next`.

```csharp
using HotChocolate.Execution;
using HotChocolate.Execution.Pipeline;

builder
    .AddGraphQL()
    .UseRequest(
        middleware: next => async context =>
        {
            if (!context.ContextData.ContainsKey(GraphQLStateKeys.TenantId))
            {
                context.Result = OperationResult.FromError(
                    ErrorBuilder.New()
                        .SetMessage("A tenant is required.")
                        .SetCode("TENANT_REQUIRED")
                        .Build());
                return;
            }

            await next(context);
        },
        key: "TenantGate",
        before: WellKnownRequestMiddleware.OperationExecutionMiddleware);
```

Use this for request-level decisions. Resolver errors belong in resolver APIs such as `IResolverContext.ReportError(...)`, `GraphQLException`, typed errors, or error filters.

# Work with request services

`RequestContext.RequestServices` is the provider used to execute the request. For ASP.NET Core HTTP requests, the default interceptor calls `TrySetServices(context.RequestServices)`. If an operation request arrives without services, the executor creates a request scope.

You can set a provider before execution:

```csharp
requestBuilder.SetServices(serviceProvider);
```

In field execution, `IResolverContext.RequestServices` preserves the original request provider. `IResolverContext.Services` is the resolver provider, and middleware can replace it with a resolver scope. Resolver dependencies should normally be parameters instead of manually resolving from `RequestContext.RequestServices`.

# Use the correct state lifetime

| State               | API                                                                                                                                     | Lifetime               | Typical use                                            |
| ------------------- | --------------------------------------------------------------------------------------------------------------------------------------- | ---------------------- | ------------------------------------------------------ |
| Global state        | `OperationRequestBuilder.SetGlobalState(...)`, `RequestContext.ContextData`, `[GlobalState]`, `IResolverContext.GetGlobalState<T>(...)` | One operation request  | Tenant, correlation, culture, authenticated user facts |
| Scoped state        | `IResolverContext.ScopedContextData`, `[ScopedState]`, scoped-state helpers                                                             | Resolver subtree       | Parent resolver data for child fields                  |
| Local state         | `IResolverContext.LocalContextData`, `[LocalState]`, local-state helpers                                                                | Current field pipeline | Field middleware data for one resolver                 |
| Result context data | `IExecutionResult.ContextData`                                                                                                          | Result lifetime        | Server-side result metadata, cleanup coordination      |

Use stable constants or custom attributes for application keys. Prefer immutable values such as strings, records, value types, or read-only DTOs. Do not store mutable per-request objects and update them from parallel resolvers.

# Enrich request context before the pipeline

`IRequestContextEnricher` is an advanced integration hook. It runs after the executor initializes `RequestContext` and before the request pipeline delegate runs. Use it when state depends on request services or schema-level integrations that are not available in a transport interceptor.

```csharp
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

public sealed record UserInfo(string UserId, string DisplayName);

public interface IUserInfoProvider
{
    UserInfo? GetCurrentUser();
}

public sealed class UserInfoRequestContextEnricher : IRequestContextEnricher
{
    public void Enrich(RequestContext context)
    {
        var provider = context.RequestServices
            .GetRequiredService<IUserInfoProvider>();

        var user = provider.GetCurrentUser();

        if (user is not null)
        {
            context.ContextData[nameof(UserInfo)] = user;
        }
    }
}
```

Register the enricher as a schema service:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .Services
    .AddSingleton<IRequestContextEnricher, UserInfoRequestContextEnricher>();
```

Resolvers should still consume the value through global-state APIs:

```csharp
[QueryType]
public static partial class UserQueries
{
    public static string ViewerName(
        [GlobalState(nameof(UserInfo))] UserInfo user)
        => user.DisplayName;
}
```

Use interceptors first for HTTP headers, WebSocket payloads, and transport identity. Use enrichers for execution integrations that need the initialized request context.

# Handle variables and cancellation

`Request.VariableValues` contains raw request variables. `RequestContext.VariableValues` contains coerced variable collections after `OperationVariableCoercionMiddleware`. Resolver code should use argument parameters, argument APIs, or `IResolverContext.Variables` instead of request-level variable collections.

For cancellation, request middleware and low-level integrations should pass `context.RequestAborted` to cancellable APIs:

```csharp
builder
    .AddGraphQL()
    .UseRequest(
        middleware: next => async context =>
        {
            await AuditRequestAsync(context.Request, context.RequestAborted);
            await next(context);
        },
        key: "AuditRequest");
```

Resolvers should accept `CancellationToken` parameters or use `IResolverContext.RequestAborted`:

```csharp
[QueryType]
public static partial class Query
{
    public static Task<Product?> GetProductAsync(
        int id,
        ProductService products,
        CancellationToken cancellationToken)
        => products.GetByIdAsync(id, cancellationToken);
}
```

If an operation is canceled, exception middleware handles `OperationCanceledException` and produces an error result when the cancellation reaches the request pipeline.

# Read RequestContext in diagnostics

Diagnostic listeners and Activity enrichers receive `RequestContext` for request-level events such as execute request, parse, validate, coerce variables, compile operation, execute operation, and request errors.

```csharp
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;

public sealed class RequestDiagnostics : ExecutionDiagnosticEventListener
{
    public override IDisposable ExecuteRequest(RequestContext context)
    {
        var operationName = context.Request.OperationName ?? "anonymous";
        var documentId = context.GetOperationDocumentId().Value;

        // Add lightweight tags or start timing here.
        return EmptyScope;
    }
}
```

Register the listener with the schema builder:

```csharp
builder
    .AddGraphQL()
    .AddDiagnosticEventListener<RequestDiagnostics>();
```

Keep diagnostic handlers fast and non-blocking. Do not perform slow I/O in synchronous diagnostic callbacks. Use resolver diagnostic events, field middleware, or `IResolverContext` when you need field-level errors or field paths.

# Apply safe lifetime rules

`RequestContext` instances are pooled and reset. Treat them as request-pipeline state, not as data you can store for later.

- Do not store `RequestContext`, `ContextData`, `Features`, or service scopes beyond the request or result lifetime.
- Do not mutate `RequestContext` from resolvers.
- Do not assume `HttpContext` exists for executor-created, test, in-memory, or non-HTTP requests.
- Do not assume a property is populated before the middleware phase that creates it.
- Do not overwrite built-in global state unless you are replacing that behavior intentionally.
- Call base interceptor methods unless you are taking responsibility for all default behavior.
- Use `TryGet*` helpers when middleware can run before a phase or after a short-circuit.
- Treat `Features` as an advanced extension point.

# Troubleshoot common issues

| Symptom                                          | Likely cause                                                                                                                                         | Fix                                                                                                     |
| ------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------- |
| `GetOperation()` throws                          | Middleware ran before operation resolution, the request failed earlier, or another middleware short-circuited execution                              | Anchor after `WellKnownRequestMiddleware.OperationResolverMiddleware` and use `TryGetOperation(...)`    |
| `VariableValues` is empty                        | Middleware ran before variable coercion, validation failed, or a previous middleware assigned `Result`                                               | Anchor after `WellKnownRequestMiddleware.OperationVariableCoercionMiddleware` and handle short-circuits |
| Resolver cannot find `ClaimsPrincipal`           | A custom interceptor skipped base behavior, or a non-HTTP executor path did not add user global state                                                | Call the base interceptor method or set user state with `OperationRequestBuilder.SetUser(...)`          |
| `HttpContext` is missing                         | The request did not come from the ASP.NET Core HTTP or WebSocket transport                                                                           | Keep transport work in interceptors or pass needed facts through global state                           |
| Services come from an unexpected provider        | `SetServices` or `TrySetServices` was not called, base interceptor behavior was skipped, or resolver middleware replaced `IResolverContext.Services` | Preserve default interceptor setup and check whether you need request services or resolver services     |
| State appears to leak or disappear               | Code stored pooled request context state past the result lifetime, or used mutable global state from parallel resolvers                              | Store immutable values and copy data you need after execution                                           |
| Client cannot see `IExecutionResult.ContextData` | Result context data is server-side metadata                                                                                                          | Use GraphQL `extensions` or transport response formatting for client-visible metadata                   |

# Next steps

- Add request data from HTTP or WebSocket with [interceptors](/docs/hotchocolate/v16/build/server-configuration/interceptors).
- Share request facts with resolvers through [global state](/docs/hotchocolate/v16/build/server-configuration/global-state).
- Learn resolver APIs in [resolvers](/docs/hotchocolate/v16/build/resolvers).
- Review resolver service injection in [service injection](/docs/hotchocolate/v16/build/resolvers/service-injection).
- Add tracing or metrics with [instrumentation](/docs/hotchocolate/v16/build/observability).
- Shape resolver errors with [errors](/docs/hotchocolate/v16/build/errors).
- Review HTTP protocol behavior in [HTTP transport](/docs/hotchocolate/v16/build/server-configuration/http-transport).
- Migrate old `IRequestContext` code with the [v15 to v16 migration guide](/docs/hotchocolate/v16/_leagcy/migrating/migrate-from-15-to-16#irequestcontext).
