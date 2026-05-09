---
title: Global state
---

Global state is request-wide data for one GraphQL operation. Use it when the server learns a fact at the transport boundary, such as a tenant ID or correlation ID, and many resolvers need that value without reading HTTP headers, WebSocket payloads, or other transport-specific APIs.

Global state is not application-wide state. It is not shared between users, connections, or operations. Hot Chocolate stores it in `RequestContext.ContextData` for the operation and exposes it to resolvers through `[GlobalState]` and `IResolverContext`.

```text
HTTP or WebSocket request
        |
        v
Transport hook validates request data
        |
        v
OperationRequestBuilder.SetGlobalState(...)
        |
        v
RequestContext.ContextData
        |
        v
Resolver parameter or IResolverContext
```

# When to use global state

Use global state for request facts that are known before execution starts:

- Tenant IDs.
- Correlation IDs.
- Selected cultures or locales.
- Request feature flags.
- Small immutable current-user DTOs when authentication data needs a domain-specific shape.

Prefer immutable values such as strings, value types, records, or read-only DTOs. Query fields can execute in parallel, so do not store a mutable object and update it from multiple resolvers.

| Need                                                           | Use                                                           | Why                                                                               |
| -------------------------------------------------------------- | ------------------------------------------------------------- | --------------------------------------------------------------------------------- |
| Share a tenant ID from headers with many resolvers             | Global state                                                  | The value is a request fact, and resolvers stay transport-neutral.                |
| Access the authenticated user                                  | `ClaimsPrincipal`, `context.GetUser()`, or authorization APIs | Identity belongs to the authentication flow.                                      |
| Inject repositories, EF contexts, caches, or services          | Dependency injection                                          | Service lifetimes and disposal belong to DI.                                      |
| Pass computed data from a parent resolver to child fields      | Scoped state                                                  | The value follows one resolver branch.                                            |
| Pass data from field middleware to the resolver for that field | Local state                                                   | The value belongs to one field pipeline.                                          |
| Read an HTTP-only value in one resolver                        | `HttpContext` access                                          | Keep transport-specific logic local when it is not a cross-cutting GraphQL value. |

# Add global state before execution

Transport interceptors are the usual place to validate incoming request data and add global state. The following interceptor reads a required tenant header and an optional correlation header from HTTP.

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

        if (!IsValidTenantId(tenantId))
        {
            throw new GraphQLException("The X-Tenant-Id header is invalid.");
        }

        requestBuilder.SetGlobalState("TenantId", tenantId);

        var correlationId = context.Request.Headers["X-Correlation-ID"]
            .FirstOrDefault()
            ?.Trim();

        if (!string.IsNullOrEmpty(correlationId))
        {
            requestBuilder.TryAddGlobalState("CorrelationId", correlationId);
        }

        return base.OnCreateAsync(
            context,
            requestExecutor,
            requestBuilder,
            cancellationToken);
    }

    private static bool IsValidTenantId(string tenantId)
        => tenantId.All(c => char.IsLetterOrDigit(c) || c == '-');
}
```

Register the interceptor on the same GraphQL server builder that defines your schema:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddHttpRequestInterceptor<TenantHttpRequestInterceptor>();
```

Always call `base.OnCreateAsync` when you derive from `DefaultHttpRequestInterceptor`. The default implementation adds request services and built-in global state such as `ClaimsPrincipal` and `HttpContext`.

For WebSockets, set operation values in `DefaultSocketSessionInterceptor.OnRequestAsync`. A WebSocket connection can carry multiple operations, and each operation has its own global state.

```csharp
using HotChocolate;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Subscriptions;

public sealed class TenantSocketSessionInterceptor : DefaultSocketSessionInterceptor
{
    public override ValueTask OnRequestAsync(
        ISocketSession session,
        string operationSessionId,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken = default)
    {
        var tenantId = session.Connection.HttpContext.Request.Headers["X-Tenant-Id"]
            .FirstOrDefault()
            ?.Trim();

        if (!string.IsNullOrEmpty(tenantId))
        {
            requestBuilder.SetGlobalState("TenantId", tenantId);
        }

        return base.OnRequestAsync(
            session,
            operationSessionId,
            requestBuilder,
            cancellationToken);
    }
}
```

# Read global state in resolvers

Use `[GlobalState]` for implementation-first resolver methods. The value is supplied by Hot Chocolate and does not appear in the GraphQL schema.

```csharp
[QueryType]
public static partial class ProductQueries
{
    public static Task<IReadOnlyList<Product>> GetProductsAsync(
        [GlobalState("TenantId")] string tenantId,
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

The service parameter still comes from dependency injection. The tenant ID comes from request global state.

If you omit the key, Hot Chocolate uses the parameter name as the key:

```csharp
public static string GetTenant([GlobalState] string tenantId)
    => tenantId;
```

This reads the key `tenantId`. Use explicit keys or constants when your stored key uses different casing, such as `TenantId`.

For optional state, make the parameter nullable or give it a default value:

```csharp
[QueryType]
public static partial class DiagnosticsQueries
{
    public static string GetCorrelationId(
        [GlobalState("CorrelationId")] string? correlationId = null)
        => correlationId ?? "not-provided";
}
```

A required `[GlobalState]` parameter throws when the key is missing or the stored value has the wrong type. Use optional parameters only when missing state is valid for that field.

# Centralize repeated keys with a custom attribute

Create a custom attribute when many resolvers read the same key.

```csharp
using HotChocolate;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class TenantIdAttribute : GlobalStateAttribute
{
    public TenantIdAttribute()
        : base("TenantId")
    {
    }
}

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

This avoids key drift between resolvers.

# Read global state from code-first or delegate resolvers

When you write a resolver delegate, read global state from `IResolverContext`.

```csharp
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("products")
            .Resolve(async context =>
            {
                var tenantId = context.GetGlobalState<string>("TenantId");
                var correlationId = context.GetGlobalStateOrDefault<string>("CorrelationId");
                var products = context.Service<ProductService>();

                return await products.GetByTenantAsync(
                    tenantId,
                    context.RequestAborted);
            });
    }
}
```

Use `GetGlobalState<T>` for required values. It throws when the key is missing or the value cannot be cast to `T`.

Use `GetGlobalStateOrDefault<T>` for optional values. It returns the default value of `T` when the key is missing or the value cannot be cast to `T`.

If you need custom diagnostics, inspect `ContextData` directly:

```csharp
if (!context.ContextData.TryGetValue("TenantId", out var value)
    || value is not string tenantId)
{
    throw new GraphQLException("The TenantId global state value is missing.");
}
```

Prefer the typed helper methods for normal resolver code.

# Choose the right state scope

Global, scoped, and local state use different backing dictionaries and different lifetimes.

| State kind | Backing context     | Lifetime               | Set from                                                                        | Read from                            | Typical use                                 |
| ---------- | ------------------- | ---------------------- | ------------------------------------------------------------------------------- | ------------------------------------ | ------------------------------------------- |
| Global     | `ContextData`       | One operation/request  | `OperationRequestBuilder`, request context enrichment, advanced resolver writes | `[GlobalState]`, `GetGlobalState<T>` | Tenant ID, correlation ID, request user DTO |
| Scoped     | `ScopedContextData` | One resolver branch    | Resolver or field middleware                                                    | `[ScopedState]`, `GetScopedState<T>` | Parent-to-child computed data               |
| Local      | `LocalContextData`  | Current field pipeline | Field middleware or resolver                                                    | `[LocalState]`, `GetLocalState<T>`   | Middleware-to-resolver handoff              |

Do not use global state as a service locator. If the value has a service lifetime, depends on disposal, or coordinates shared work, register it with dependency injection. If the value belongs to a field branch or field pipeline, use scoped or local state instead.

# Know the global state APIs

| API                                                                             | Behavior                                       | Use                                                  |
| ------------------------------------------------------------------------------- | ---------------------------------------------- | ---------------------------------------------------- |
| `OperationRequestBuilder.SetGlobalState(string, object?)`                       | Sets or overwrites one key before execution.   | Main API for request values.                         |
| `OperationRequestBuilder.AddGlobalState(string, object?)`                       | Adds one key and fails on duplicate.           | Use when duplicate state is a bug.                   |
| `OperationRequestBuilder.TryAddGlobalState(string, object?)`                    | Adds one key only when it is absent.           | Preserve values set earlier.                         |
| `OperationRequestBuilder.SetGlobalState(IReadOnlyDictionary<string, object?>?)` | Replaces the initial global state dictionary.  | Advanced setup. Take care not to remove defaults.    |
| `OperationRequestBuilder.RemoveGlobalState(string)`                             | Removes one key from the initial state.        | Rare request customization.                          |
| `IResolverContext.GetGlobalState<T>(string)`                                    | Reads required state.                          | Throws on missing or wrong type.                     |
| `IResolverContext.GetGlobalStateOrDefault<T>(string)`                           | Reads optional state.                          | Returns default on missing or wrong type.            |
| `IResolverContext.SetGlobalState<T>(string, T)`                                 | Writes global state during resolver execution. | Advanced. Avoid cross-resolver mutable accumulators. |

# Handle missing values at the edge

For required values, validate before execution starts. Throw a `GraphQLException` from the interceptor with a clear message.

```csharp
if (string.IsNullOrEmpty(tenantId))
{
    throw new GraphQLException("The X-Tenant-Id header is required.");
}
```

For optional values, either do not set the key or set a normalized value. Then read it with a nullable `[GlobalState]` parameter or `GetGlobalStateOrDefault<T>`.

Keep key names and value types consistent. A key typo or type mismatch has the same effect as a missing value for optional reads, and it throws for required reads.

# Test global state usage

Test the transport boundary and the resolver contract.

```csharp
[Fact]
public async Task Tenant_Should_Use_Global_State()
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
        .SetGlobalState("TenantId", "tenant-a")
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
    public string Tenant([GlobalState("TenantId")] string tenantId)
        => tenantId;
}
```

In integration tests, send the HTTP or WebSocket input that your interceptor reads and assert the resolver result. In unit tests for resolver delegates, build an `OperationRequest` with `SetGlobalState` so the resolver sees the same state shape as production.

# Troubleshoot global state

## `SetProperty`, `TryAddProperty`, or `GetGlobalValue` does not compile

Those names are from older Hot Chocolate versions. In v16 use `SetGlobalState`, `AddGlobalState`, `TryAddGlobalState`, `GetGlobalState`, and `GetGlobalStateOrDefault`.

## `[GlobalState]` throws because the value is missing

Check the key name and casing. Confirm the interceptor is registered on the GraphQL builder that handles the request. Confirm the request path sets the state for the current operation. Use a nullable or defaulted parameter only when the value is optional.

## The value is always null or default

Verify the incoming header, payload, or request value exists. Verify the stored value type matches the resolver parameter or generic type. `GetGlobalStateOrDefault<T>` returns default for wrong types, so use `GetGlobalState<T>` or explicit `ContextData` validation while debugging.

## `ClaimsPrincipal` or `HttpContext` is unavailable

Check that custom interceptors call the base method. The default HTTP and WebSocket interceptors add built-in state such as `ClaimsPrincipal` and `HttpContext`. For identity, prefer `ClaimsPrincipal`, `context.GetUser()`, and authorization APIs over custom state when they meet the requirement.

## State behaves unpredictably under load

Look for mutable objects stored in global state and changed by parallel resolvers. Replace them with immutable request values, scoped services, DataLoaders, or thread-safe abstractions.

## A WebSocket value is missing in another operation

Global state is per operation, not per socket connection. Store connection-level facts on connection setup if needed, but copy operation values into global state in `OnRequestAsync` for every operation that needs them.

# Next steps

- [Interceptors](/docs/hotchocolate/v16/build2/server-configuration/interceptors) for HTTP and WebSocket lifecycle details.
- [HTTP context access](/docs/hotchocolate/v16/build2/resolvers/ihttpcontextaccessor-and-context) for transport-specific resolver access.
- [Parameter attributes](/docs/hotchocolate/v16/build2/resolvers/parameter-attributes) for `[GlobalState]`, `[ScopedState]`, and `[LocalState]`.
