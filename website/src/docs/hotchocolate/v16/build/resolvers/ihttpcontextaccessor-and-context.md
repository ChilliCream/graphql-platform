---
title: "Access HTTP and request data safely in resolvers"
---

When writing resolvers, prioritize GraphQL request APIs instead of directly using ASP.NET Core HTTP APIs. Start with `ClaimsPrincipal`, `IResolverContext`, services, global state, and cancellation tokens. Only use `IHttpContextAccessor` or `HttpContext` if your field must access HTTP-specific details like headers, cookies, host information, or connection metadata.

This page guides you in selecting the appropriate boundary API before accessing `IHttpContextAccessor.HttpContext` within a resolver.

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

As a general rule, if a value belongs to your GraphQL operation model, pass it using resolver parameters, services, or global state. If the value comes from the HTTP transport, restrict HTTP access to only where it is necessary and keep it well-contained.

---

## Read the current user through resolver-friendly APIs

To access identity data in a field, use a `ClaimsPrincipal` resolver parameter. Hot Chocolate automatically provides the current principal from ASP.NET Core authentication using the default HTTP and socket interceptors.

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

If identity is optional and the field should support anonymous users, use `ClaimsPrincipal?` as the parameter type:

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

When defining a field with the fluent descriptor API, retrieve the user from `IResolverContext`:

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

Keep `ClaimsPrincipal` and `IResolverContext` at the GraphQL boundary. Pass domain-specific values, like `userId`, into your application services rather than passing the entire resolver context.

---

## Use `IResolverContext` for GraphQL resolver data

`IResolverContext` provides context for field resolvers. It offers access to the current field execution, including parent values, arguments, services, global state, context dictionaries, and cancellation tokens.

Use `IResolverContext` when a resolver delegate requires more information than standard method parameters can provide:

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

Note that `RequestContext` is different: it is used in request middleware, not in typical field resolver methods. For more details, see [Understand `RequestContext` versus `IResolverContext`](#understand-requestcontext-versus-iresolvercontext).

---

## Promote repeated HTTP data into global state

When multiple resolvers require a value originating from HTTP data, read and validate it once at the transport boundary. Then, store a normalized version in Hot Chocolate's global state for the GraphQL operation.

This approach makes resolvers portable across HTTP, WebSocket subscriptions, tests, and other execution paths.

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

Apply `[GlobalState]` when a source-generated resolver requires a single value from global state:

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

Use `IResolverContext` if the field already requires context for other purposes:

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

For optional state, you can use a nullable `[GlobalState]` parameter, provide a default parameter value, or call `context.GetGlobalStateOrDefault<T>("Key")`, depending on your resolver style.

---

## Use `IHttpContextAccessor` only for HTTP-specific details

`IHttpContextAccessor` is an ASP.NET Core service that is helpful when a field needs to expose HTTP-specific data, such as a correlation header. It is not recommended for accessing the current user, request services, or domain request state.

Register the accessor in `Program.cs`:

```csharp
builder.Services.AddHttpContextAccessor();
```

Then, inject it as a resolver service parameter:

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

> **Warning:** `IHttpContextAccessor.HttpContext` may be `null` outside an active ASP.NET Core HTTP request, in tests, in non-HTTP execution, or after the transport-specific context is gone. Always check for `null` or move the value into global state earlier in the pipeline.

If several resolvers require the same HTTP-only behavior, prefer a small request-scoped service:

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

Resolvers can then depend on `CorrelationIdAccessor`, avoiding repeated direct HTTP access.

---

## Use direct `HttpContext` resolver parameters sparingly

Hot Chocolate ASP.NET Core can bind a resolver parameter of type `HttpContext` from the operation's global state. While this is concise, it tightly couples the field to the ASP.NET Core transport.

Reserve this approach for fields that are strictly HTTP-specific:

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

Direct `HttpContext` binding relies on the default ASP.NET Core interceptor path. If the `HttpContext` global state is missing, binding will fail with a missing state error. For values that can be made transport-neutral, use global state instead.

Take care when working with host, scheme, forwarded headers, and cookies. These values depend on ASP.NET Core proxy and security settings, and header or cookie values should always be treated as untrusted input.

---

## Resolve services through Hot Chocolate

Inject services directly into resolvers or use `context.Service<T>()` rather than calling `httpContext.RequestServices.GetRequiredService<T>()`.

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

The default ASP.NET Core integration configures request services for GraphQL execution. If you need to replace request services in an interceptor, use `OperationRequestBuilder.SetServices(...)` or `TrySetServices(...)` for every relevant transport path.

---

## Pass cancellation from the resolver boundary

Hot Chocolate provides cancellation via a `CancellationToken` parameter on resolver methods and through `context.RequestAborted` on `IResolverContext`. Pass this token to service calls, EF Core queries, HTTP clients, and DataLoader invocations.

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

Do not hide cancellation tokens inside HTTP accessors or service locators. Keep cancellation visible at the resolver boundary.

---

## Understand `RequestContext` versus `IResolverContext`

`IResolverContext` is used within field resolvers or field middleware. Most resolver code should rely on this type for GraphQL execution data.

`RequestContext` is the concrete request pipeline context, intended for request middleware, not for standard field resolver methods.

```csharp
builder
    .AddGraphQL()
    .UseRequest(next => async context =>
    {
        // context is RequestContext here, not IResolverContext.
        await next(context);
    });
```

When writing a field resolver, use `IResolverContext`. When implementing request middleware, use `RequestContext`.

---

## Handle WebSocket subscriptions and non-HTTP execution

HTTP GraphQL requests are processed via `IHttpRequestInterceptor.OnCreateAsync`, while WebSocket operations use `ISocketSessionInterceptor.OnRequestAsync`. If your subscription resolvers require tenant, locale, or user-related operation state, ensure you populate that state for each WebSocket operation as well.

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

The default socket interceptor adds connection `HttpContext`, `ISocketSession`, the operation session ID, and `ClaimsPrincipal` to global state. Call `base.OnRequestAsync` unless you need to override this behavior.

Whenever possible, design resolvers to be transport-neutral. For example, a resolver that uses `[GlobalState("TenantId")] string tenantId` will work for HTTP, WebSocket, tests, and any other execution path that provides the same global state key.

---

## Troubleshoot context access

| Symptom                                                  | What to check                                                                                                                                                                                                                     |
| -------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `ClaimsPrincipal` parameter throws or is empty           | Ensure ASP.NET Core authentication runs before the GraphQL endpoint. Confirm custom HTTP and socket interceptors call their base methods. Use `ClaimsPrincipal?` for optional identity access. Use `[Authorize]` for enforcement. |
| `IHttpContextAccessor.HttpContext` is `null`             | The resolver may be running outside an active HTTP request, in a test, or through a non-HTTP execution path. Guard for `null` or use a value promoted into global state.                                                          |
| Direct `HttpContext` parameter is missing                | Direct binding reads the `HttpContext` value from global state, which is added by the ASP.NET Core interceptor path. Prefer global state for transport-neutral values.                                                            |
| Tenant or correlation header is missing in subscriptions | HTTP request interceptors do not initialize every WebSocket operation. Use `ISocketSessionInterceptor.OnRequestAsync` to add operation global state.                                                                              |
| Global state key is missing                              | Verify the interceptor runs for the transport, the key name and casing match, and optional values use `GetGlobalStateOrDefault<T>()` or a nullable/default parameter.                                                             |
| Services resolve from the wrong provider                 | Use resolver DI or `context.Service<T>()`. If you replace request services, configure `SetServices(...)` or `TrySetServices(...)` in every relevant interceptor path.                                                             |

---

## Use current API names

| Need                                            | Use                                              | Avoid                                                               |
| ----------------------------------------------- | ------------------------------------------------ | ------------------------------------------------------------------- |
| Set or replace one global state value           | `requestBuilder.SetGlobalState("Key", value)`    | `SetProperty("Key", value)`                                         |
| Add a global state value only when missing      | `requestBuilder.TryAddGlobalState("Key", value)` | `TryAddProperty("Key", value)`                                      |
| Add a global state value and fail on duplicates | `requestBuilder.AddGlobalState("Key", value)`    | `AddProperty("Key", value)`                                         |
| Read required global state                      | `context.GetGlobalState<T>("Key")`               | `context.GetGlobalValue<T>("Key")`                                  |
| Read optional global state                      | `context.GetGlobalStateOrDefault<T>("Key")`      | Using a required read as optional behavior                          |
| Replace request services                        | `requestBuilder.SetServices(...)`                | Service location through `HttpContext.RequestServices` in resolvers |

---

## Go next

- [Resolver Signature](./resolver-signature): Learn about resolver method parameters, return types, and cancellation.
- [Resolvers](/docs/hotchocolate/v16/build/resolvers): Explore general resolver patterns.
- [Dependency Injection](/docs/hotchocolate/v16/build/resolvers/service-injection): Understand resolver service injection.
- [Global State](/docs/hotchocolate/v16/build/server-configuration/global-state): Set up server-wide global state.
- [Interceptors](/docs/hotchocolate/v16/build/server-configuration/interceptors): Configure HTTP and WebSocket request interception.
- [Authentication](/docs/hotchocolate/v16/build/security/authentication) and [Authorization](/docs/hotchocolate/v16/build/security/authorization): Set up security.
- [Subscriptions](/docs/hotchocolate/v16/build/type-system/operations-subscriptions): Learn about WebSocket and SSE transport behavior.
