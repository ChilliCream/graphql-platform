---
title: Interceptors
---

Interceptors allow you to customize a GraphQL operation request at the transport boundary. Use them when you need to transfer data from ASP.NET Core or a WebSocket session into Hot Chocolate request state before execution begins.

A typical use case is tenant routing. ASP.NET Core middleware handles authentication, the interceptor validates tenant information from claims or headers, and resolvers access the tenant from global state.

```text
ASP.NET Core middleware
  -> GraphQL endpoint
  -> HTTP request interceptor or socket session interceptor
  -> Hot Chocolate request pipeline
  -> field middleware and resolvers
  -> result formatting
```

For WebSockets, the socket interceptor also observes lifecycle events:

```text
HTTP upgrade
  -> connection_init
  -> OnConnectAsync
  -> OnRequestAsync
  -> execution
  -> OnResultAsync
  -> OnCompleteAsync
  -> OnCloseAsync
```

The HTTP extension point is `IHttpRequestInterceptor.OnCreateAsync`. For WebSockets, use `ISocketSessionInterceptor`, typically by deriving from `DefaultSocketSessionInterceptor`.

# Choose the right extension point

Interceptors are one tool in the server pipeline. Pick the smallest hook that matches the work you need to do.

| Use this                       | Best for                                                                                                       | Runs when                                                     | Do not use it for                                |
| ------------------------------ | -------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------- | ------------------------------------------------ |
| ASP.NET Core middleware        | CORS, authentication, authorization, logging, WebSocket upgrade                                                | Before the GraphQL endpoint                                   | GraphQL operation state or resolver data         |
| Endpoint options               | `MapGraphQL`, `MapGraphQLHttp`, `MapGraphQLWebSocket`, GET, multipart, Nitro, socket options                   | While mapping endpoints                                       | Per-operation tenant, user, or correlation state |
| HTTP request interceptor       | Copy headers, claims, request services, tenant ids, correlation ids, or policy flags into an operation request | When Hot Chocolate creates a request from HTTP traffic        | Endpoint routing or final response formatting    |
| Socket session interceptor     | Accept or reject `connection_init`, add state for WebSocket operations, inspect socket results                 | During GraphQL over WebSocket connection and operation events | ASP.NET Core WebSocket configuration             |
| Request middleware             | Change execution behavior after the operation request exists                                                   | Inside the Hot Chocolate execution pipeline                   | Reading HTTP headers directly                    |
| Field middleware and resolvers | Field-level behavior and data fetching                                                                         | While fields execute                                          | Transport-level validation                       |
| Error filters and formatters   | Shape errors or transport output                                                                               | After errors or results exist                                 | Request enrichment                               |

Interceptors can enrich state and identity. They do not replace ASP.NET Core authentication middleware, endpoint `.RequireAuthorization()`, or GraphQL authorization policies.

# Preserve the default behavior

Hot Chocolate registers default interceptors. When you derive from them, keep the default setup unless you have a specific reason to replace it.

`DefaultHttpRequestInterceptor`:

- Sets request services from `HttpContext.RequestServices` when no services were set earlier.
- Adds `HttpContext` and `ClaimsPrincipal` to global state.
- Adds request features for the user, HTTP context, file uploads, operation plan flags, and cost switches.

`DefaultSocketSessionInterceptor.OnRequestAsync`:

- Adds `HttpContext`, `ISocketSession`, the operation session id, and `ClaimsPrincipal` to global state.
- Adds request features for the user, HTTP context, and the socket connection service scope factory.

If you override `DefaultHttpRequestInterceptor.OnCreateAsync`, call `base.OnCreateAsync(...)`. If you override `DefaultSocketSessionInterceptor.OnRequestAsync`, call `base.OnRequestAsync(...)`. Skipping base behavior can remove request services, authenticated user data, file upload support, or operation metadata.

Delegate HTTP interceptors are different. The delegate runs first, then Hot Chocolate runs the default HTTP interceptor for you.

# Add state with an HTTP request interceptor

Use `DefaultHttpRequestInterceptor` when the logic has validation, dependencies, or enough behavior to test separately.

This example reads a tenant id from an authenticated claim. It falls back to a header only for trusted internal requests. The tenant id becomes operation global state.

```csharp
using System.Net;
using HotChocolate;
using HotChocolate.AspNetCore;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Http;

public static class GraphQLStateKeys
{
    public const string TenantId = "TenantId";
}

public sealed class TenantHttpRequestInterceptor : DefaultHttpRequestInterceptor
{
    public override ValueTask OnCreateAsync(
        HttpContext context,
        IRequestExecutor requestExecutor,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        var tenantId = context.User.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrWhiteSpace(tenantId) &&
            context.Request.Headers.TryGetValue("X-Tenant-Id", out var headerValue) &&
            IsTrustedInternalRequest(context))
        {
            tenantId = headerValue.ToString();
        }

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("A tenant is required.")
                    .SetCode("TENANT_REQUIRED")
                    .Build());
        }

        requestBuilder.SetGlobalState(GraphQLStateKeys.TenantId, tenantId);

        return base.OnCreateAsync(
            context,
            requestExecutor,
            requestBuilder,
            cancellationToken);
    }

    private static bool IsTrustedInternalRequest(HttpContext context)
        => context.Connection.RemoteIpAddress is { } address &&
           IPAddress.IsLoopback(address);
}
```

Register the interceptor with the GraphQL server.

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddHttpRequestInterceptor<TenantHttpRequestInterceptor>();
```

Resolvers can read the value with `[GlobalState]`.

```csharp
public sealed class Query
{
    public string Tenant([GlobalState(GraphQLStateKeys.TenantId)] string tenantId)
        => tenantId;
}
```

Query:

```graphql
query {
  tenant
}
```

Result:

```json
{
  "data": {
    "tenant": "tenant-123"
  }
}
```

Do not store request-specific data in interceptor instance fields. Interceptors registered with `AddHttpRequestInterceptor<T>()` are schema services and are singleton by default.

# Add small HTTP state with a delegate

Use the delegate overload for short, self-contained enrichment. This example reads a correlation id header and falls back to `HttpContext.TraceIdentifier`.

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddHttpRequestInterceptor(
        (context, executor, requestBuilder, cancellationToken) =>
        {
            var correlationId =
                context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                ?? context.TraceIdentifier;

            requestBuilder.SetGlobalState("CorrelationId", correlationId);

            return ValueTask.CompletedTask;
        });
```

The delegate runs before the default HTTP interceptor. The default interceptor still adds request services, `HttpContext`, `ClaimsPrincipal`, file upload features, and other built-in state.

Use a class-based interceptor instead when you need constructor dependencies, async validation, or shared tests.

# Extend the authenticated user before execution

Run ASP.NET Core authentication before GraphQL. Then use an HTTP interceptor when you need to add claims that are specific to GraphQL execution.

```csharp
using System.Security.Claims;
using HotChocolate.AspNetCore;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Http;

public sealed class UserEnrichmentInterceptor : DefaultHttpRequestInterceptor
{
    public override ValueTask OnCreateAsync(
        HttpContext context,
        IRequestExecutor requestExecutor,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var identity = new ClaimsIdentity("GraphQL");
            identity.AddClaim(new Claim("graphql_access", "true"));

            context.User.AddIdentity(identity);
        }

        return base.OnCreateAsync(
            context,
            requestExecutor,
            requestBuilder,
            cancellationToken);
    }
}
```

Order matters. The default interceptor copies `context.User` into Hot Chocolate request features and global state. If you add claims after calling base, resolvers and authorization handlers may see the earlier principal.

`requestBuilder.SetUser(claimsPrincipal)` is a targeted API that sets the `ClaimsPrincipal` global state value. Use `context.User` when you want the default interceptor to copy the updated ASP.NET Core principal consistently.

# Reject an HTTP GraphQL request safely

Throw `GraphQLException` from `OnCreateAsync` to stop execution before resolvers run. Return sanitized errors. Do not include raw tokens, connection strings, stack traces, or backend exception details.

```csharp
throw new GraphQLException(
    ErrorBuilder.New()
        .SetMessage("The tenant is not allowed for this request.")
        .SetCode("TENANT_NOT_ALLOWED")
        .Build());
```

The client receives a GraphQL error response and the operation does not execute.

```json
{
  "errors": [
    {
      "message": "The tenant is not allowed for this request.",
      "extensions": {
        "code": "TENANT_NOT_ALLOWED"
      }
    }
  ]
}
```

Use error filters or result formatting when you need broad error shaping across the whole server.

# Use request services and application services

HTTP interceptors can resolve request-scoped ASP.NET Core services from `HttpContext.RequestServices`.

```csharp
using Microsoft.Extensions.DependencyInjection;

var tenantValidator =
    context.RequestServices.GetRequiredService<ITenantValidator>();

var isValid = await tenantValidator.IsValidAsync(tenantId, cancellationToken);
```

Constructor injection for interceptors uses schema services. If the interceptor needs application services in its constructor, make them available to the schema with `AddApplicationService<T>()`, or use the factory overload when that is the right fit.

Most applications should not replace request services with `requestBuilder.SetServices(...)`. The default HTTP interceptor calls `TrySetServices(context.RequestServices)`, which gives resolvers and execution components the expected scoped services.

# Accept or reject a WebSocket connection

Use `DefaultSocketSessionInterceptor` for GraphQL over WebSocket events. Register it with `AddSocketSessionInterceptor<T>()`.

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddSubscriptionType<Subscription>()
    .AddSocketSessionInterceptor<ConnectionInitSocketSessionInterceptor>();
```

`OnConnectAsync` runs when the client sends `connection_init`. It accepts or rejects the logical GraphQL socket session. It does not configure the ASP.NET Core WebSocket upgrade.

```csharp
using System.Text.Json.Serialization;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.AspNetCore.Subscriptions.Protocols;
using HotChocolate.Text.Json;

public sealed class ConnectionInitSocketSessionInterceptor : DefaultSocketSessionInterceptor
{
    public override ValueTask<ConnectionStatus> OnConnectAsync(
        ISocketSession session,
        IOperationMessagePayload connectionInitMessage,
        CancellationToken cancellationToken = default)
    {
        var payload =
            connectionInitMessage.Payload?.Deserialize<ConnectionInitPayload>();

        if (string.IsNullOrWhiteSpace(payload?.Token))
        {
            return new(ConnectionStatus.Reject());
        }

        return base.OnConnectAsync(
            session,
            connectionInitMessage,
            cancellationToken);
    }

    private sealed class ConnectionInitPayload
    {
        [JsonPropertyName("token")]
        public string? Token { get; init; }
    }
}
```

Use `ConnectionStatus.Reject()` or a sanitized `ConnectionStatus.Reject("...")`. The rejection message can be visible to clients.

# Add state to each WebSocket operation

`OnRequestAsync` runs when a client registers an operation on an existing socket session. It does not run for every event emitted by a subscription.

Use this hook to add state that resolvers need for WebSocket operations.

```csharp
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Http;

public sealed class TenantOperationSocketSessionInterceptor : DefaultSocketSessionInterceptor
{
    public override ValueTask OnRequestAsync(
        ISocketSession session,
        string operationSessionId,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken = default)
    {
        var user = session.Connection.HttpContext.User;
        var tenantId = user.FindFirst("tenant_id")?.Value;

        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            requestBuilder.SetGlobalState(GraphQLStateKeys.TenantId, tenantId);
        }

        return base.OnRequestAsync(
            session,
            operationSessionId,
            requestBuilder,
            cancellationToken);
    }
}
```

Calling base preserves built-in WebSocket state, including `HttpContext`, `ISocketSession`, the operation session id, `ClaimsPrincipal`, and the socket connection service scope factory.

If connection initialization validates a token, also decide how each operation will receive the identity or tenant it needs. `OnConnectAsync` accepts the socket session, but it does not change `HttpContext.User` by itself.

# Inspect socket results and cleanup

Override only the socket hooks you need.

| Method            | Runs when                                               | Common uses                             | Return value         | Watch for                                      |
| ----------------- | ------------------------------------------------------- | --------------------------------------- | -------------------- | ---------------------------------------------- |
| `OnConnectAsync`  | A client sends `connection_init`                        | Accept or reject the socket session     | `ConnectionStatus`   | Rejection details can be visible to clients    |
| `OnRequestAsync`  | An operation is registered                              | Add per-operation state and services    | `ValueTask`          | Call base to preserve defaults                 |
| `OnResultAsync`   | Before a socket result is serialized                    | Inspect or replace an `OperationResult` | `OperationResult`    | Subscriptions can call this many times         |
| `OnCompleteAsync` | An operation completes, fails, or the connection closes | Cleanup for an operation                | `ValueTask`          | The cancellation token may already be canceled |
| `OnPingAsync`     | A client ping arrives                                   | Return optional pong payload            | Dictionary or `null` | Keep payloads small                            |
| `OnPongAsync`     | A client pong arrives                                   | Observe health or latency               | `ValueTask`          | Avoid blocking the socket loop                 |
| `OnCloseAsync`    | The connection closes                                   | Connection-level cleanup                | `ValueTask`          | Cleanup must tolerate canceled work            |

# Use OperationRequestBuilder from interceptors

Most interceptor customizations use global state or request services.

| API                              | Use it for                         | Notes                                           |
| -------------------------------- | ---------------------------------- | ----------------------------------------------- |
| `SetGlobalState(name, value)`    | Add or overwrite one key           | Good for your own state keys                    |
| `AddGlobalState(name, value)`    | Add one key and fail on duplicates | Useful when duplicates indicate a bug           |
| `TryAddGlobalState(name, value)` | Add one key only when missing      | Used by default interceptors for built-in state |
| `SetGlobalState(dictionary)`     | Replace all initial global state   | Can remove default or earlier values            |
| `RemoveGlobalState(name)`        | Remove one key                     | Avoid removing built-in keys unless intentional |
| `SetServices(services)`          | Replace request services           | Advanced, can break scoped service behavior     |
| `TrySetServices(services)`       | Set services only when unset       | Used by the default HTTP interceptor            |

Avoid overwriting these built-in global state keys unless you intend to replace Hot Chocolate defaults:

- `nameof(HttpContext)`
- `nameof(ClaimsPrincipal)`
- `nameof(ISocketSession)` for WebSocket operations
- `OperationSessionId` from `HotChocolate.ExecutionContextData`
- Operation plan and cost keys from `HotChocolate.ExecutionContextData`

Request shape APIs such as `SetDocument`, `SetOperationName`, `SetVariableValues`, and `SetExtensions` are low-level. Prefer transport settings and request middleware unless you have a focused integration scenario.

# Troubleshooting

| Symptom                                                          | Likely cause                                                                 | Fix                                                                                                                 |
| ---------------------------------------------------------------- | ---------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------- |
| Resolvers no longer receive services or `ClaimsPrincipal`        | A custom class skipped `base.OnCreateAsync` or `base.OnRequestAsync`         | Call base and avoid replacing services or all global state                                                          |
| Constructor injection cannot resolve an application service      | The interceptor is created from schema services                              | Use `AddApplicationService<T>()`, a factory overload, or `HttpContext.RequestServices` for request-scoped HTTP work |
| Custom global state disappears                                   | Duplicate keys, replacement via `SetGlobalState(dictionary)`, or ordering    | Use constants and choose `SetGlobalState`, `AddGlobalState`, or `TryAddGlobalState` deliberately                    |
| HTTP tenant headers work, subscriptions do not                   | WebSocket operations do not send a new HTTP request for each operation       | Use `OnRequestAsync` and state from the socket `HttpContext.User` or validated connection data                      |
| WebSocket connect auth succeeds, but resolvers see old user data | `OnConnectAsync` accepted the session but did not update per-operation state | Set resolver state in `OnRequestAsync` and make identity changes before base behavior copies user state             |
| Rejections expose sensitive information                          | Raw exception text or detailed reject messages are sent to clients           | Use sanitized `GraphQLException` errors and sanitized `ConnectionStatus` values                                     |
| File uploads or operation plan and cost behavior stopped working | The HTTP default interceptor behavior was replaced                           | Derive from `DefaultHttpRequestInterceptor` and call base                                                           |

# When middleware is a better fit

Use ASP.NET Core middleware for host concerns: authentication, CORS, response compression, logging scopes, and WebSocket upgrade behavior.

Use Hot Chocolate request middleware when the operation request already exists and you need to inspect or modify execution behavior.

Use field middleware or resolver code when the concern belongs to one field, one type, data fetching, authorization at field level, or resolver-local state.

Use error filters and formatters when the goal is consistent error or result shaping rather than request enrichment.

# Next steps

- Read [Server configuration](/docs/hotchocolate/v16/build/server-configuration) for the complete hosting setup.
- Read [Endpoints](/docs/hotchocolate/v16/build/server-configuration/endpoints) for endpoint mapping and authorization.
- Read [HTTP transport](/docs/hotchocolate/v16/build/server-configuration/http-transport) for POST, GET, multipart, and response formats.
- Read [Global state](/docs/hotchocolate/v16/build/server-configuration/global-state) for global state and resolver access patterns.
- Read [Resolver parameter attributes](/docs/hotchocolate/v16/build/resolvers/parameter-attributes) for `[GlobalState]`.
- Read [Service injection](/docs/hotchocolate/v16/build/resolvers/service-injection) for schema services, application services, and resolver DI.
