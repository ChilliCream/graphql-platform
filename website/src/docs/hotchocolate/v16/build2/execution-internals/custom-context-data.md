---
title: Custom context data
---

Custom context data is per-operation metadata that moves through the Hot Chocolate execution pipeline. Use it for small facts that the execution engine, middleware, resolvers, or diagnostics need to share without coupling resolver code to HTTP headers, WebSocket payloads, or another transport API.

A common flow looks like this:

```text
HTTP or WebSocket request
        |
        v
Interceptor validates tenant and correlation IDs
        |
        v
OperationRequestBuilder.SetGlobalState(...)
        |
        v
RequestContext.ContextData
        |
        +--> request middleware and diagnostics
        |
        +--> field middleware and resolvers
                  |
                  +--> ScopedContextData for one field branch
                  |
                  +--> LocalContextData for one field pipeline
```

Context data is not a service locator. Use it for execution metadata. Use dependency injection for services, DataLoader for batching and request caching, arguments for client input, typed features for infrastructure state, and result extensions for client-visible output metadata.

# Choose the right mechanism

| Need                                                                                | Use                                                      | Why                                                                   |
| ----------------------------------------------------------------------------------- | -------------------------------------------------------- | --------------------------------------------------------------------- |
| A tenant ID, correlation ID, culture, or feature flag known for one operation       | Global state, stored in `ContextData`                    | Every resolver and middleware component in the operation can read it. |
| A value computed by a parent field that child selections need                       | Scoped state, stored in `ScopedContextData`              | The value flows down one field branch and is isolated from siblings.  |
| A value shared between middleware components and the resolver for the current field | Local state, stored in `LocalContextData`                | The value stays inside one field pipeline.                            |
| Repositories, EF contexts, caches, clocks, clients, or other behavior               | Dependency injection                                     | DI owns service lifetime, scopes, and disposal.                       |
| Request batching and caching for data access                                        | DataLoader                                               | DataLoader coordinates loading across resolvers.                      |
| Strongly typed execution infrastructure state                                       | `RequestContext.Features` or `IResolverContext.Features` | Features avoid string keys and casts.                                 |
| GraphQL input from the client                                                       | Arguments                                                | Arguments are part of the schema contract.                            |
| Metadata that should be returned with a result                                      | Result extensions or operation result context data       | Resolver-visible context data is not the response payload.            |

# Mental model

All runtime context data stores use string keys and `object?` values, but each store has a different owner and propagation rule.

```text
Operation request
+--------------------------------------------------+
| ContextData / global state                       |
|                                                  |
|  Query.product                                   |
|  +--------------------------------------------+  |
|  | ScopedContextData for product branch       |  |
|  |                                            |  |
|  |  product.name field pipeline               |  |
|  |  +--------------------------------------+  |  |
|  |  | LocalContextData for product.name     |  |  |
|  |  +--------------------------------------+  |  |
|  |                                            |  |
|  |  product.price field pipeline              |  |
|  |  +--------------------------------------+  |  |
|  |  | LocalContextData for product.price    |  |  |
|  |  +--------------------------------------+  |  |
|  +--------------------------------------------+  |
+--------------------------------------------------+
```

`IResolverContext` and `IMiddlewareContext` are access points. They expose global, scoped, and local state, but they are not extra storage scopes.

Type-system descriptor metadata and field definitions are separate concepts. This page covers runtime execution state.

## Request and global context data

Global state is the public v16 feature for request-wide values. Hot Chocolate stores it in `RequestContext.ContextData` for the operation. Request middleware, enrichers, and request diagnostics access that dictionary through `RequestContext.ContextData`. Field middleware and resolvers access the same request-wide values through `IResolverContext.ContextData` or the global state helper methods.

Use global state when a value applies to the whole operation request:

- Tenant IDs.
- Correlation IDs.
- Selected cultures.
- Small immutable user or request snapshots.
- Operation flags created before execution.

The initial values are set on `OperationRequestBuilder`:

| API                                                     | Behavior                                            |
| ------------------------------------------------------- | --------------------------------------------------- |
| `SetGlobalState(IReadOnlyDictionary<string, object?>?)` | Replaces the initial global state dictionary.       |
| `SetGlobalState(string, object?)`                       | Sets or overwrites one initial value.               |
| `AddGlobalState(string, object?)`                       | Adds one initial value and fails on duplicate keys. |
| `TryAddGlobalState(string, object?)`                    | Adds one initial value only when the key is absent. |
| `RemoveGlobalState(string)`                             | Removes one initial value from the builder.         |

Resolvers and field middleware can read or write request-wide state with these helpers:

| API                                               | Behavior                                                                                       |
| ------------------------------------------------- | ---------------------------------------------------------------------------------------------- |
| `GetGlobalState<T>(string)`                       | Reads required state and throws when the key is missing or the value cannot be cast to `T`.    |
| `GetGlobalStateOrDefault<T>(string)`              | Returns `default` when the key is missing or the value cannot be cast to `T`.                  |
| `GetGlobalStateOrDefault<T>(string, T)`           | Returns the supplied default value when the key is missing or the value cannot be cast to `T`. |
| `SetGlobalState<T>(string, T)`                    | Sets or overwrites request-wide state during execution.                                        |
| `GetOrSetGlobalState<T>(string, Func<string, T>)` | Reads an existing value or creates and stores one.                                             |

The pooled v16 request context uses a thread-safe dictionary implementation for request context data. That does not make stored objects thread-safe. Store immutable values, records, strings, value types, or read-only DTOs when parallel resolvers can read the same value.

## Scoped context data

Scoped state lives on `IResolverContext.ScopedContextData`. It is an immutable dictionary for one field hierarchy. A value set while resolving a field is visible to descendant selections in that branch, but not to sibling branches.

```text
query
+-- product(id: 1)       SetScopedState("CanSeeCost", true)
|   +-- name             can read scoped state
|   +-- cost             can read scoped state
|
+-- product(id: 2)       separate sibling branch
    +-- cost             cannot read the first branch value
```

Use scoped state for branch-specific facts:

- A parent resolver computes an authorization decision that children need.
- A parent resolver parses a value once and children reuse it.
- Field middleware marks a branch with metadata before child selections run.

Scoped state APIs:

| API                                               | Behavior                                                                    |
| ------------------------------------------------- | --------------------------------------------------------------------------- |
| `GetScopedState<T>(string)`                       | Reads required scoped state and throws on missing or incompatible values.   |
| `GetScopedStateOrDefault<T>(string)`              | Returns `default` when the key is missing or incompatible.                  |
| `GetScopedStateOrDefault<T>(string, T)`           | Returns the supplied default value when the key is missing or incompatible. |
| `SetScopedState<T>(string, T)`                    | Sets or overwrites a key by replacing the immutable dictionary instance.    |
| `GetOrSetScopedState<T>(string, Func<string, T>)` | Reads an existing value or creates and stores one.                          |
| `RemoveScopedState(string)`                       | Removes a scoped value by replacing the immutable dictionary instance.      |
| `[ScopedState]`                                   | Binds a resolver parameter from scoped state.                               |

## Local context data

Local state lives on `IResolverContext.LocalContextData`. It is an immutable dictionary for the current field pipeline. It is useful when field middleware components need to share metadata with each other or with the resolver for the same field.

Local state does not flow to child fields. Use scoped state when descendants need the value.

Local state APIs:

| API                                              | Behavior                                                                    |
| ------------------------------------------------ | --------------------------------------------------------------------------- |
| `GetLocalState<T>(string)`                       | Reads required local state and throws on missing or incompatible values.    |
| `GetLocalStateOrDefault<T>(string)`              | Returns `default` when the key is missing or incompatible.                  |
| `GetLocalStateOrDefault<T>(string, T)`           | Returns the supplied default value when the key is missing or incompatible. |
| `SetLocalState<T>(string, T)`                    | Sets or overwrites a key by replacing the immutable dictionary instance.    |
| `GetOrSetLocalState<T>(string, Func<string, T>)` | Reads an existing value or creates and stores one.                          |
| `RemoveLocalState(string)`                       | Removes a local value by replacing the immutable dictionary instance.       |
| `[LocalState]`                                   | Binds a resolver parameter from local state.                                |

# Define stable keys

Use constants for custom keys. Prefer a namespace-like prefix so your keys do not collide with libraries or Hot Chocolate built-in keys.

```csharp
public static class GraphQLContextKeys
{
    public const string TenantId = "Contoso.Inventory.TenantId";
    public const string CorrelationId = "Contoso.Inventory.CorrelationId";
    public const string CanSeeCost = "Contoso.Inventory.CanSeeCost";
    public const string FieldStartedAt = "Contoso.Inventory.FieldStartedAt";
}
```

Hot Chocolate uses well-known key constants for its own features. Do not reuse those keys unless the feature documentation tells you to do so.

For frequently used global state parameters, wrap the key in a custom attribute:

```csharp
using HotChocolate;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class TenantIdAttribute : GlobalStateAttribute
{
    public TenantIdAttribute()
        : base(GraphQLContextKeys.TenantId)
    {
    }
}
```

# Initialize request state before execution

## HTTP requests

Use `DefaultHttpRequestInterceptor.OnCreateAsync` to validate transport-specific input and populate initial global state.

```csharp
using HotChocolate;
using HotChocolate.AspNetCore;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Http;

public sealed class TenantHttpRequestInterceptor : DefaultHttpRequestInterceptor
{
    public override ValueTask OnCreateAsync(
        HttpContext context,
        IRequestExecutor requestExecutor,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        var tenantId = context.Request.Headers["X-Tenant-Id"]
            .FirstOrDefault()
            ?.Trim();

        if (string.IsNullOrEmpty(tenantId))
        {
            throw new GraphQLException("The X-Tenant-Id header is required.");
        }

        requestBuilder.SetGlobalState(GraphQLContextKeys.TenantId, tenantId);

        var correlationId = context.Request.Headers["X-Correlation-ID"]
            .FirstOrDefault()
            ?.Trim();

        if (!string.IsNullOrEmpty(correlationId))
        {
            requestBuilder.TryAddGlobalState(
                GraphQLContextKeys.CorrelationId,
                correlationId);
        }

        return base.OnCreateAsync(
            context,
            requestExecutor,
            requestBuilder,
            cancellationToken);
    }
}
```

Register the interceptor on the GraphQL builder:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddHttpRequestInterceptor<TenantHttpRequestInterceptor>();
```

Call the base implementation unless you intentionally replace the default behavior. The default interceptor adds request services and built-in global state such as the authenticated user.

## WebSocket operations

A WebSocket connection can carry multiple GraphQL operations. Set per-operation values in `DefaultSocketSessionInterceptor.OnRequestAsync`.

```csharp
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.Execution;

public sealed class TenantSocketSessionInterceptor : DefaultSocketSessionInterceptor
{
    public override ValueTask OnRequestAsync(
        ISocketSession session,
        string operationSessionId,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        var tenantId = session.Connection.HttpContext.Request.Headers["X-Tenant-Id"]
            .FirstOrDefault()
            ?.Trim();

        if (!string.IsNullOrEmpty(tenantId))
        {
            requestBuilder.SetGlobalState(GraphQLContextKeys.TenantId, tenantId);
        }

        return base.OnRequestAsync(
            session,
            operationSessionId,
            requestBuilder,
            cancellationToken);
    }
}
```

State set on the request builder applies to that operation request, not every operation on the socket.

## Request middleware

Request middleware has access to `RequestContext`. Use it for metadata derived inside the request pipeline, and position it before the middleware or execution phase that needs the value.

```csharp
using HotChocolate.Execution;
using System.Diagnostics;

builder
    .AddGraphQL()
    .UseRequest(next => async context =>
    {
        if (!context.ContextData.ContainsKey(GraphQLContextKeys.CorrelationId))
        {
            context.ContextData[GraphQLContextKeys.CorrelationId] =
                Activity.Current?.TraceId.ToString();
        }

        await next(context);
    });
```

State written after `await next(context)` is available only to code that runs later in that same control flow. It cannot affect resolvers or middleware components that have already executed.

## Request context enrichers

`IRequestContextEnricher` is an advanced hook that can mutate `RequestContext` before execution. Use it when a reusable convention needs to enrich many schemas or services.

```csharp
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

public sealed class UserInfoRequestContextEnricher : IRequestContextEnricher
{
    public void Enrich(RequestContext context)
    {
        if (context.ContextData.TryGetValue(nameof(ClaimsPrincipal), out var value)
            && value is ClaimsPrincipal principal
            && principal.Identity?.IsAuthenticated == true)
        {
            context.ContextData["Contoso.Inventory.UserInfo"] =
                new UserInfo(principal.Identity.Name ?? "unknown");
        }
    }
}

builder.Services.AddSingleton<IRequestContextEnricher, UserInfoRequestContextEnricher>();
```

Prefer interceptors when the value comes from HTTP or WebSocket details. Prefer request middleware when the value depends on pipeline order.

# Read state in resolvers

## Parameter attributes

Use `[GlobalState]`, `[ScopedState]`, and `[LocalState]` for implementation-first resolver parameters. These parameters do not appear in the GraphQL schema.

```csharp
[QueryType]
public static partial class ProductQueries
{
    public static Task<IReadOnlyList<Product>> GetProductsAsync(
        [TenantId] string tenantId,
        ProductService products,
        CancellationToken cancellationToken)
        => products.GetByTenantAsync(tenantId, cancellationToken);
}
```

Use an explicit key when the parameter name differs from the stored key:

```csharp
public static string GetTenant(
    [GlobalState(GraphQLContextKeys.TenantId)] string tenantId)
    => tenantId;
```

If no key is supplied, Hot Chocolate uses the parameter name as the key:

```csharp
public static string GetTenant([GlobalState] string tenantId)
    => tenantId;
```

The example above reads the key `tenantId`, not `Contoso.Inventory.TenantId`.

Required state parameters throw when the value is missing or incompatible. Nullable parameters or parameters with default values can receive the default value.

```csharp
public static string GetCorrelationId(
    [GlobalState(GraphQLContextKeys.CorrelationId)] string? correlationId = null)
    => correlationId ?? "not-provided";
```

Scoped and local attributes follow the same key rules:

```csharp
[ExtendObjectType<Product>]
public static partial class ProductResolvers
{
    public static bool CanSeeCost(
        [ScopedState(GraphQLContextKeys.CanSeeCost)] bool canSeeCost)
        => canSeeCost;

    public static long FieldStartedAt(
        [LocalState(GraphQLContextKeys.FieldStartedAt)] long startedAt)
        => startedAt;
}
```

Use local state parameters only for values set earlier in the same field pipeline.

## Resolver context methods

Use `IResolverContext` when you need explicit control or when writing delegate resolvers.

```csharp
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("products")
            .Resolve(async context =>
            {
                var tenantId = context.GetGlobalState<string>(
                    GraphQLContextKeys.TenantId);

                var correlationId = context.GetGlobalStateOrDefault<string>(
                    GraphQLContextKeys.CorrelationId,
                    "not-provided");

                var products = context.Service<ProductService>();

                return await products.GetByTenantAsync(
                    tenantId,
                    context.RequestAborted);
            });
    }
}
```

Use `Get*State<T>` for required values. Use `Get*StateOrDefault<T>` when missing or incompatible state is valid. During debugging, prefer `Get*State<T>` or explicit `TryGetValue` checks because optional reads can hide key or type mistakes.

# Write scoped state for child fields

Set scoped state before descendant fields need it. A parent resolver can compute a branch-specific decision and store it for child selections.

```csharp
public sealed class Product
{
    public required string Id { get; init; }

    public required string Name { get; init; }
}

[QueryType]
public static partial class ProductQueries
{
    public static async Task<Product> GetProductAsync(
        string id,
        IResolverContext context,
        AuthorizationService authorization,
        CancellationToken cancellationToken)
    {
        var product = await authorization.GetProductAsync(id, cancellationToken);
        var canSeeCost = await authorization.CanSeeCostAsync(
            product,
            cancellationToken);

        context.SetScopedState(GraphQLContextKeys.CanSeeCost, canSeeCost);

        return product;
    }
}

[ExtendObjectType<Product>]
public static partial class ProductResolvers
{
    public static decimal? GetCost(
        [Parent] Product product,
        [ScopedState(GraphQLContextKeys.CanSeeCost)] bool canSeeCost)
        => canSeeCost ? LoadCost(product.Id) : null;
}
```

Scoped state is copied into descendant resolver tasks. Sibling branches do not receive the value.

# Use local state in field middleware

Local state is for middleware-to-middleware or middleware-to-resolver communication inside one field pipeline.

```csharp
using System.Diagnostics;

public static class FieldTimingMiddleware
{
    public static IObjectFieldDescriptor UseFieldTiming(
        this IObjectFieldDescriptor descriptor)
        => descriptor.Use(next => async context =>
        {
            context.SetLocalState(
                GraphQLContextKeys.FieldStartedAt,
                Stopwatch.GetTimestamp());

            await next(context);

            var startedAt = context.GetLocalState<long>(
                GraphQLContextKeys.FieldStartedAt);

            var elapsed = Stopwatch.GetElapsedTime(startedAt);
            context.ContextData["Contoso.Inventory.LastFieldElapsed"] = elapsed;
        });
}
```

A resolver for the same field can also read local state:

```csharp
descriptor
    .Field("timedName")
    .UseFieldTiming()
    .Resolve(context =>
    {
        var startedAt = context.GetLocalState<long>(
            GraphQLContextKeys.FieldStartedAt);

        return $"started at {startedAt}";
    });
```

Do not use local state for child selections. Use scoped state for that path.

# Use context data in diagnostics

Execution diagnostics can read context data for logging or activity enrichment. Request events receive `RequestContext`; field events receive `IMiddlewareContext` when field-value diagnostics are enabled.

```csharp
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

public sealed class CorrelationDiagnosticListener(
    ILogger<CorrelationDiagnosticListener> logger)
    : ExecutionDiagnosticEventListener
{
    public override IDisposable ExecuteRequest(RequestContext context)
    {
        if (context.ContextData.TryGetValue(
            GraphQLContextKeys.CorrelationId,
            out var value)
            && value is string correlationId)
        {
            logger.LogInformation(
                "Executing GraphQL request {CorrelationId}",
                correlationId);
        }

        return EmptyScope;
    }

    public override bool EnableResolveFieldValue => true;

    public override IDisposable ResolveFieldValue(IMiddlewareContext context)
    {
        var tenantId = context.GetGlobalStateOrDefault<string>(
            GraphQLContextKeys.TenantId,
            "unknown");

        Activity.Current?.SetTag("graphql.tenant_id", tenantId);

        return EmptyScope;
    }
}

builder
    .AddGraphQL()
    .AddDiagnosticEventListener<CorrelationDiagnosticListener>();
```

Keep diagnostic work lightweight. Do not mutate state late and expect already executed resolvers to observe it. Execution listeners are long lived, so do not store per-request values on listener fields.

# Safety rules

## Store values with the right lifetime

Store lightweight metadata, immutable snapshots, and request-owned values. Avoid storing:

- Disposable services.
- EF `DbContext` instances.
- Mutable unsynchronized collections.
- Large request caches.
- Tenant-specific singleton state.
- `RequestContext`, `IResolverContext`, or state dictionaries beyond the request.

If the value has behavior or disposal requirements, inject a service. If the value coordinates data loading, use DataLoader. If multiple parallel resolvers must update a value, use synchronization or immutable updates.

## Know the cleanup boundary

Global state belongs to one operation request and is cleared when the pooled request context is reset. Scoped and local dictionaries are immutable snapshots owned by resolver or middleware contexts. Stored values can still be mutable and shared, so choose values that match the execution lifetime.

## Avoid late writes

Set values before the component that needs them runs.

| Write location                                  | Who can observe it                                                                            |
| ----------------------------------------------- | --------------------------------------------------------------------------------------------- |
| Interceptor before execution                    | Request middleware, resolvers, diagnostics for the operation.                                 |
| Request middleware before `await next(context)` | Later request middleware and execution phases.                                                |
| Field middleware before `await next(context)`   | Later field middleware and the resolver for that field. Scoped state can flow to descendants. |
| Field middleware after `await next(context)`    | Later code on the return path of the same field pipeline.                                     |

# Testing

Test the contract at the boundary where the state is created and at the resolver or middleware that consumes it.

For resolver tests, build an operation request with the same state shape used in production:

```csharp
[Fact]
public async Task Tenant_Should_Use_Global_State_When_Request_Contains_Tenant()
{
    // arrange
    var executor = await new ServiceCollection()
        .AddGraphQL()
        .AddQueryType<Query>()
        .Services
        .BuildServiceProvider()
        .GetRequiredService<IRequestExecutorResolver>()
        .GetRequestExecutorAsync();

    var request = OperationRequestBuilder.New()
        .SetDocument("{ tenant }")
        .SetGlobalState(GraphQLContextKeys.TenantId, "tenant-a")
        .Build();

    // act
    var result = await executor.ExecuteAsync(request);

    // assert
    result.MatchInlineSnapshot("""
        {
          "data": {
            "tenant": "tenant-a"
          }
        }
        """);
}

public sealed class Query
{
    public string Tenant(
        [GlobalState(GraphQLContextKeys.TenantId)] string tenantId)
        => tenantId;
}
```

For HTTP or WebSocket integration tests, send the header, payload, or connection data that your interceptor reads and assert a resolver result. For scoped and local state, test the field branch or middleware pipeline that owns the value, including a sibling branch when isolation matters.

# Troubleshooting

| Symptom                                                    | Likely cause                                                                                        | Fix                                                                                                                             |
| ---------------------------------------------------------- | --------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------- |
| A value is `null` or default                               | Key mismatch, wrong type, missing transport data, or `Get*StateOrDefault<T>` hiding a cast failure. | Use constants, validate the type with `Get*State<T>`, and check the boundary that sets the value.                               |
| `[GlobalState]`, `[ScopedState]`, or `[LocalState]` throws | Required parameter is missing or incompatible.                                                      | Set the value earlier, use the correct scope, or make the parameter nullable with a default when absence is valid.              |
| `AddGlobalState` throws                                    | The key was already added.                                                                          | Use `SetGlobalState` for overwrite behavior or `TryAddGlobalState` when the first value should win.                             |
| A sibling field cannot see scoped state                    | Scoped state is branch-local.                                                                       | Use global state when every branch needs the value.                                                                             |
| A child field cannot see local state                       | Local state stays in the current field pipeline.                                                    | Use scoped state for descendant selections.                                                                                     |
| A value disappears between middleware components           | Middleware order issue, late write, or remove call.                                                 | Set the value before `await next(context)` and confirm middleware ordering.                                                     |
| Behavior changes under load                                | Mutable value is shared by parallel resolvers.                                                      | Store immutable values or use synchronization.                                                                                  |
| A service is disposed or used from the wrong scope         | A service instance was stored in context data.                                                      | Inject the service with DI instead.                                                                                             |
| Older samples do not compile                               | The sample uses v15 or older names.                                                                 | Use v16 APIs: `SetGlobalState`, `AddGlobalState`, `TryAddGlobalState`, `GetGlobalState`, `GetScopedState`, and `GetLocalState`. |

# API reference

## Context surfaces

| Surface                                                   | Type                                    | Used from                                          | Scope                  |
| --------------------------------------------------------- | --------------------------------------- | -------------------------------------------------- | ---------------------- |
| `RequestContext.ContextData`                              | `IDictionary<string, object?>`          | request middleware, enrichers, request diagnostics | operation request      |
| `IResolverContext.ContextData`                            | `IDictionary<string, object?>`          | resolvers and field middleware                     | operation request      |
| `IMiddlewareContext.ContextData`                          | `IDictionary<string, object?>`          | field middleware                                   | operation request      |
| `IResolverContext.ScopedContextData`                      | `IImmutableDictionary<string, object?>` | resolvers and field middleware                     | field branch           |
| `IResolverContext.LocalContextData`                       | `IImmutableDictionary<string, object?>` | resolvers and field middleware                     | current field pipeline |
| `RequestContext.Features` and `IResolverContext.Features` | feature collection                      | infrastructure extensions                          | typed feature state    |

## Resolver APIs

| State  | Read required       | Read optional                | Write               | Read or create           | Remove              |
| ------ | ------------------- | ---------------------------- | ------------------- | ------------------------ | ------------------- |
| Global | `GetGlobalState<T>` | `GetGlobalStateOrDefault<T>` | `SetGlobalState<T>` | `GetOrSetGlobalState<T>` | none                |
| Scoped | `GetScopedState<T>` | `GetScopedStateOrDefault<T>` | `SetScopedState<T>` | `GetOrSetScopedState<T>` | `RemoveScopedState` |
| Local  | `GetLocalState<T>`  | `GetLocalStateOrDefault<T>`  | `SetLocalState<T>`  | `GetOrSetLocalState<T>`  | `RemoveLocalState`  |

## Parameter attributes

| Attribute       | Store                        | Key behavior                                                |
| --------------- | ---------------------------- | ----------------------------------------------------------- |
| `[GlobalState]` | request/global `ContextData` | Uses the parameter name unless an explicit key is supplied. |
| `[ScopedState]` | `ScopedContextData`          | Uses the parameter name unless an explicit key is supplied. |
| `[LocalState]`  | `LocalContextData`           | Uses the parameter name unless an explicit key is supplied. |

# Next steps

- [Global state](../server-configuration/global-state) for request-wide state at the transport boundary.
- [Interceptors](../server-configuration/interceptors) for HTTP and WebSocket hooks.
- [Request middleware](../execution-engine/request-middleware) and [field middleware](../execution-engine/field-middleware) for pipeline ordering.
- [Parameter attributes](../resolvers/parameter-attributes) for resolver parameter binding.
- [Service injection](../resolvers/service-injection) when the value is a service or resource.
- [DataLoader](../dataloader) for request batching and caching.
- [Diagnostic events](../observability/diagnostic-events) for execution instrumentation.
