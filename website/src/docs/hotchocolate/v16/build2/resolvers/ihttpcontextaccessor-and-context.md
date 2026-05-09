---
title: "Access HTTP and request data safely in resolvers"
---

Resolvers should prefer GraphQL request APIs over raw ASP.NET Core HTTP APIs. Reach for `ClaimsPrincipal`, `IResolverContext`, services, global state, and cancellation first. Use `IHttpContextAccessor` or `HttpContext` only when a field is intentionally tied to HTTP details such as headers, cookies, host data, or connection metadata.

This page helps you choose the right boundary API before you read `IHttpContextAccessor.HttpContext` from a resolver.

## What you will learn

- Read the current authenticated user without coupling a resolver to HTTP.
- Use `IResolverContext` for arguments, parent values, services, request state, and cancellation.
- Promote repeated HTTP-derived values, such as tenant IDs, into Hot Chocolate global state.
- Use `IHttpContextAccessor` and direct `HttpContext` parameters only for HTTP-specific fields.
- Avoid common context problems in HTTP requests, WebSocket subscriptions, tests, and non-HTTP execution.

---

## Choose the context API first

| Resolver need                                                       | Prefer                                                             | Use raw HTTP only when                                                  |
| ------------------------------------------------------------------- | ------------------------------------------------------------------ | ----------------------------------------------------------------------- |
| Current user or claims                                              | `ClaimsPrincipal` parameter or `context.GetUser()`                 | You need a raw ASP.NET Core authentication artifact.                    |
| Authorization                                                       | `[Authorize]` with authorization policies                          | Do not use manual `HttpContext.User` checks as the only access control. |
| Tenant, locale, correlation ID, or another repeated request value   | Interceptor validates the value, then writes global state          | A single HTTP-only field needs the original header or cookie.           |
| GraphQL arguments, parent values, field metadata, or resolver state | `IResolverContext`                                                 | Not applicable.                                                         |
| Application dependency                                              | Resolver service parameter or `context.Service<T>()`               | A field is deliberately integrating with ASP.NET Core.                  |
| Cancellation                                                        | `CancellationToken` parameter or `context.RequestAborted`          | Not applicable.                                                         |
| Headers, cookies, host, scheme, connection details                  | `IHttpContextAccessor` or direct `HttpContext` parameter           | The field is explicitly HTTP-only.                                      |
| WebSocket connection payload                                        | `ISocketSessionInterceptor` writes global state for each operation | Do not rely on HTTP request interceptors for WebSocket operations.      |
| Request pipeline middleware state                                   | `RequestContext` in request middleware                             | Do not request `RequestContext` from field resolvers.                   |

Rule of thumb: if the value is part of your GraphQL operation model, pass it through resolver parameters, services, or global state. If the value is part of the HTTP transport, keep the HTTP access narrow and guarded.

---

## Read the current user through resolver-friendly APIs

Use a `ClaimsPrincipal` resolver parameter when the field needs identity data. Hot Chocolate populates the current principal from ASP.NET Core authentication through the default HTTP and socket interceptors.

```csharp
using System.Security.Claims;

[QueryType]
public static partial class UserQueries
{
    public static async Task<User?> GetMeAsync(
        ClaimsPrincipal user,
        UserService users,
        CancellationToken ct)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId is null)
        {
            return null;
        }

        return await users.GetByIdAsync(userId, ct);
    }
}
```

Expected SDL excerpt:

```graphql
type Query {
  me: User
}
```

A client can query the field like this:

```graphql
query GetMe {
  me {
    id
    displayName
  }
}
```

Example authenticated response:

```json
{
  "data": {
    "me": {
      "id": "123",
      "displayName": "Ada Lovelace"
    }
  }
}
```

Use `ClaimsPrincipal?` when identity is optional and the field is allowed to run for anonymous users:

```csharp
using System.Security.Claims;

[QueryType]
public static partial class UserQueries
{
    public static string GetViewerLabel(ClaimsPrincipal? user)
        => user?.FindFirstValue(ClaimTypes.Name) ?? "anonymous";
}
```

> **Watch out:** `[Authorize]` controls access. A `ClaimsPrincipal` parameter reads the current identity, but it does not enforce authorization by itself.

### Use `context.GetUser()` in descriptor-based resolvers

If you define the field with the fluent descriptor API, read the user from `IResolverContext`:

```csharp
using System.Security.Claims;

public sealed class UserQueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("me")
            .Resolve(async context =>
            {
                var user = context.GetUser();
                var userId = user?.FindFirstValue(ClaimTypes.NameIdentifier);

                if (userId is null)
                {
                    return null;
                }

                var users = context.Service<UserService>();
                return await users.GetByIdAsync(userId, context.RequestAborted);
            });
    }
}
```

Keep `ClaimsPrincipal` and `IResolverContext` at the GraphQL boundary. Pass domain values such as `userId` into application services instead of passing the full resolver context into service code.

---

## Use `IResolverContext` for GraphQL resolver data

`IResolverContext` is the field resolver context. It gives you access to the current field execution, including parent values, arguments, services, global state, context dictionaries, and cancellation.

Use it when a resolver delegate needs more than ordinary method parameters can express:

```csharp
public sealed class OrderQueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("orders")
            .Argument("status", a => a.Type<StringType>())
            .Resolve(async context =>
            {
                var tenantId = context.GetGlobalState<string>("TenantId");
                var status = context.ArgumentValue<string?>("status");
                var orders = context.Service<OrderService>();

                return await orders.GetOrdersForTenantAsync(
                    tenantId,
                    status,
                    context.RequestAborted);
            });
    }
}
```

Expected SDL excerpt:

```graphql
type Query {
  orders(status: String): [Order!]!
}
```

Common resolver-context APIs:

| API                                                        | Use it for                                                         |
| ---------------------------------------------------------- | ------------------------------------------------------------------ |
| `context.Parent<T>()`                                      | Read the value returned by the parent field.                       |
| `context.ArgumentValue<T>("name")`                         | Read a coerced GraphQL argument value.                             |
| `context.ArgumentOptional<T>("name")`                      | Distinguish omitted arguments from explicit values.                |
| `context.Service<T>()`                                     | Resolve an application service from the resolver service provider. |
| `context.RequestAborted`                                   | Pass cancellation to async work.                                   |
| `context.GetGlobalState<T>("Key")`                         | Read required per-operation state.                                 |
| `context.GetGlobalStateOrDefault<T>("Key")`                | Read optional per-operation state.                                 |
| `context.ContextData`                                      | Lowest-level dictionary for global request state.                  |
| `context.ScopedContextData` and `context.LocalContextData` | Advanced field middleware state.                                   |

`RequestContext` is different. It belongs to request middleware, not to ordinary field resolver methods. See [Understand `RequestContext` versus `IResolverContext`](#understand-requestcontext-versus-iresolvercontext).

---

## Promote repeated HTTP data into global state

If many resolvers need a value that starts as HTTP data, read and validate it once at the transport boundary. Then store a normalized value in Hot Chocolate global state for the GraphQL operation.

This pattern keeps resolvers portable across HTTP, WebSocket subscriptions, tests, and other execution paths.

### Write the value in an HTTP request interceptor

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

        if (!string.IsNullOrEmpty(tenantId))
        {
            if (!IsValidTenantId(tenantId))
            {
                throw new GraphQLException("The tenant header is invalid.");
            }

            requestBuilder.SetGlobalState("TenantId", tenantId);
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

Register the interceptor with your GraphQL builder:

```csharp
builder
    .AddGraphQL()
    .AddHttpRequestInterceptor<TenantHttpRequestInterceptor>();
```

> **Watch out:** Always call `base.OnCreateAsync` in custom HTTP interceptors. The default implementation adds request services and built-in global state such as `ClaimsPrincipal` and `HttpContext`.

### Consume global state with a resolver parameter

Use `[GlobalState]` when a source-generated resolver needs one value from global state:

```csharp
[QueryType]
public static partial class OrderQueries
{
    public static Task<IReadOnlyList<Order>> GetOrdersAsync(
        [GlobalState("TenantId")] string tenantId,
        OrderService orders,
        CancellationToken ct)
        => orders.GetOrdersForTenantAsync(tenantId, ct);
}
```

Expected SDL excerpt:

```graphql
type Query {
  orders: [Order!]!
}
```

The global state parameter is not exposed as a client argument.

### Consume global state through `IResolverContext`

Use `IResolverContext` when the field already needs context for other reasons:

```csharp
[QueryType]
public static partial class OrderQueries
{
    public static Task<IReadOnlyList<Order>> GetOrdersAsync(
        IResolverContext context,
        OrderService orders)
    {
        var tenantId = context.GetGlobalState<string>("TenantId");
        return orders.GetOrdersForTenantAsync(tenantId, context.RequestAborted);
    }
}
```

A request with header `X-Tenant-Id: tenant-a` can then execute:

```graphql
query GetOrders {
  orders {
    id
    total
  }
}
```

Example response:

```json
{
  "data": {
    "orders": [{ "id": "ord-1", "total": 42.5 }]
  }
}
```

For optional state, use a nullable `[GlobalState]` parameter, a default parameter value, or `context.GetGlobalStateOrDefault<T>("Key")`, depending on the resolver style.

---

## Use `IHttpContextAccessor` only for HTTP-specific details

`IHttpContextAccessor` is an ASP.NET Core service. It is useful when a field intentionally exposes HTTP data, such as an incoming correlation header. It is not the preferred API for the current user, request services, or domain request state.

Register the accessor in `Program.cs`:

```csharp
builder.Services.AddHttpContextAccessor();
```

Then inject it as a resolver service parameter:

```csharp
using Microsoft.AspNetCore.Http;

[QueryType]
public static partial class DiagnosticsQueries
{
    public static string GetCorrelationId(IHttpContextAccessor accessor)
        => accessor.HttpContext?.Request.Headers["X-Correlation-ID"]
            .FirstOrDefault()
            ?? "unknown";
}
```

Expected SDL excerpt:

```graphql
type Query {
  correlationId: String!
}
```

> **Watch out:** `IHttpContextAccessor.HttpContext` can be `null` outside an active ASP.NET Core HTTP request, in tests, in non-HTTP execution, or after transport-specific context is unavailable. Guard for `null` or promote the value into global state earlier.

Prefer a small request-scoped service when several resolvers need the same HTTP-only behavior:

```csharp
using Microsoft.AspNetCore.Http;

public sealed class CorrelationIdAccessor(IHttpContextAccessor accessor)
{
    public string Current
        => accessor.HttpContext?.Request.Headers["X-Correlation-ID"]
            .FirstOrDefault()
            ?? "unknown";
}
```

Resolvers can then depend on `CorrelationIdAccessor` instead of repeating HTTP access.

---

## Use direct `HttpContext` resolver parameters sparingly

Hot Chocolate ASP.NET Core can bind a resolver parameter of type `HttpContext` from operation global state. This is concise, but it couples the field to the ASP.NET Core transport.

Use it only for HTTP-only fields:

```csharp
using Microsoft.AspNetCore.Http;

[QueryType]
public static partial class DiagnosticsQueries
{
    public static string? GetUserAgent(HttpContext httpContext)
        => httpContext.Request.Headers.UserAgent.FirstOrDefault();
}
```

Expected SDL excerpt:

```graphql
type Query {
  userAgent: String
}
```

Direct `HttpContext` binding depends on the default ASP.NET Core interceptor path. If no `HttpContext` global state exists, binding fails with a missing state error. Use global state for values that can be made transport-neutral.

Be careful with host, scheme, forwarded headers, and cookies. These values depend on ASP.NET Core proxy and security configuration, and header or cookie values are untrusted input.

---

## Resolve services through Hot Chocolate

Use resolver service injection or `context.Service<T>()` instead of `httpContext.RequestServices.GetRequiredService<T>()`.

```csharp
[QueryType]
public static partial class ProductQueries
{
    public static Task<Product?> GetProductAsync(
        int id,
        ProductService products,
        CancellationToken ct)
        => products.GetProductAsync(id, ct);
}
```

Descriptor-based equivalent:

```csharp
public sealed class ProductQueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("product")
            .Argument("id", a => a.Type<NonNullType<IntType>>())
            .Resolve(async context =>
            {
                var id = context.ArgumentValue<int>("id");
                var products = context.Service<ProductService>();

                return await products.GetProductAsync(
                    id,
                    context.RequestAborted);
            });
    }
}
```

The default ASP.NET Core integration sets request services for GraphQL execution. If you need to replace request services in an interceptor, use `OperationRequestBuilder.SetServices(...)` or `TrySetServices(...)` consistently for each relevant transport.

---

## Pass cancellation from the resolver boundary

Hot Chocolate supplies cancellation through a `CancellationToken` parameter on resolver methods and through `context.RequestAborted` on `IResolverContext`. Pass it to service calls, EF Core queries, HTTP clients, and DataLoader calls.

```csharp
[QueryType]
public static partial class ProductQueries
{
    public static Task<Product?> GetProductAsync(
        int id,
        ProductService products,
        CancellationToken ct)
        => products.GetProductAsync(id, ct);
}
```

Avoid hiding cancellation inside an HTTP accessor or a service locator. Keep it visible at the resolver boundary.

---

## Understand `RequestContext` versus `IResolverContext`

`IResolverContext` is the context inside a field resolver or field middleware. Most resolver code should use this type when it needs GraphQL execution data.

`RequestContext` is the concrete request pipeline context in v16. It is used by request middleware, not by ordinary field resolver methods.

```csharp
builder
    .AddGraphQL()
    .UseRequest(next => async context =>
    {
        // context is RequestContext here, not IResolverContext.
        await next(context);
    });
```

If you are writing a field resolver, request `IResolverContext`. If you are writing request middleware, use `RequestContext`.

---

## Handle WebSocket subscriptions and non-HTTP execution

HTTP GraphQL requests run through `IHttpRequestInterceptor.OnCreateAsync`. WebSocket operations run through `ISocketSessionInterceptor.OnRequestAsync`. If subscription resolvers need tenant, locale, or user-related operation state, populate that state for each WebSocket operation too.

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
        CancellationToken cancellationToken = default)
    {
        // Copy validated connection or session values into operation global state.
        requestBuilder.SetGlobalState("TenantId", "tenant-from-session");

        return base.OnRequestAsync(
            session,
            operationSessionId,
            requestBuilder,
            cancellationToken);
    }
}
```

The default socket interceptor adds connection `HttpContext`, `ISocketSession`, the operation session ID, and `ClaimsPrincipal` global state. Call `base.OnRequestAsync` unless you intentionally replace that behavior.

Keep resolvers transport-neutral when possible. A resolver that consumes `[GlobalState("TenantId")] string tenantId` works for HTTP, WebSocket, tests, and other execution paths that provide the same global state key.

---

## Troubleshoot context access

| Symptom                                                  | What to check                                                                                                                                                                                                                            |
| -------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `ClaimsPrincipal` parameter throws or is empty           | Confirm ASP.NET Core authentication runs before GraphQL endpoint execution. Confirm custom HTTP and socket interceptors call their base methods. Use `ClaimsPrincipal?` for optional identity access. Use `[Authorize]` for enforcement. |
| `IHttpContextAccessor.HttpContext` is `null`             | The resolver may be running outside an active HTTP request, in a test, or through a non-HTTP execution path. Guard for `null` or read a value promoted into global state.                                                                |
| Direct `HttpContext` parameter is missing                | Direct binding reads the `HttpContext` value from global state. The value is added by the ASP.NET Core interceptor path. Prefer global state for transport-neutral values.                                                               |
| Tenant or correlation header is missing in subscriptions | HTTP request interceptors do not initialize every WebSocket operation. Use `ISocketSessionInterceptor.OnRequestAsync` to add operation global state.                                                                                     |
| Global state key is missing                              | Verify the interceptor runs for the transport, the key name and casing match, and optional values use `GetGlobalStateOrDefault<T>()` or a nullable/default parameter.                                                                    |
| Services resolve from the wrong provider                 | Use resolver DI or `context.Service<T>()`. If you replace request services, configure `SetServices(...)` or `TrySetServices(...)` in every relevant interceptor path.                                                                    |

---

## Use v16 API names

| Need                                            | Use in v16                                       | Do not use in new v16 examples                                      |
| ----------------------------------------------- | ------------------------------------------------ | ------------------------------------------------------------------- |
| Set or replace one global state value           | `requestBuilder.SetGlobalState("Key", value)`    | `SetProperty("Key", value)`                                         |
| Add a global state value only when missing      | `requestBuilder.TryAddGlobalState("Key", value)` | `TryAddProperty("Key", value)`                                      |
| Add a global state value and fail on duplicates | `requestBuilder.AddGlobalState("Key", value)`    | `AddProperty("Key", value)`                                         |
| Read required global state                      | `context.GetGlobalState<T>("Key")`               | `context.GetGlobalValue<T>("Key")`                                  |
| Read optional global state                      | `context.GetGlobalStateOrDefault<T>("Key")`      | Using a required read as optional behavior                          |
| Replace request services                        | `requestBuilder.SetServices(...)`                | Service location through `HttpContext.RequestServices` in resolvers |

---

## Go next

- [Resolver Signature](./resolver-signature) for resolver method parameters, return shapes, and cancellation.
- [Resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers) for general resolver patterns.
- [Dependency Injection](/docs/hotchocolate/v16/resolvers-and-data/dependency-injection) for resolver service injection.
- [Global State](/docs/hotchocolate/v16/server/global-state) for server-wide global state setup.
- [Interceptors](/docs/hotchocolate/v16/server/interceptors) for HTTP and WebSocket request interception.
- [Authentication](/docs/hotchocolate/v16/securing-your-api/authentication) and [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization) for security setup.
- [Subscriptions](/docs/hotchocolate/v16/building-a-schema/subscriptions) for WebSocket and SSE transport behavior.
