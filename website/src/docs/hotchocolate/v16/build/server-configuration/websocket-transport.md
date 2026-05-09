---
title: WebSocket transport
---

GraphQL subscriptions require three main components: a subscription field in the schema, a subscription provider to deliver events, and a transport to send subscription results to the client. This page explains how to configure the WebSocket transport for Hot Chocolate v16 on ASP.NET Core.

| Piece                 | Responsibility                                                         | Learn more                                                                             |
| --------------------- | ---------------------------------------------------------------------- | -------------------------------------------------------------------------------------- |
| Subscription field    | Defines what the client can subscribe to.                              | [Subscriptions](/docs/hotchocolate/v16/build/schema-elements/operations-subscriptions) |
| Subscription provider | Moves published events to active subscriptions.                        | [Subscriptions](/docs/hotchocolate/v16/build/schema-elements/operations-subscriptions) |
| WebSocket transport   | Carries connection, operation, result, keep-alive, and close messages. | This page                                                                              |

# When to use WebSockets

Choose WebSockets when your clients use GraphQL over WebSocket and require a persistent connection for one or more active subscription operations.

| Transport             | Best fit                                                                                                                                 | Notes                                                                            |
| --------------------- | ---------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------- |
| WebSocket             | Subscription clients that use GraphQL over WebSocket, bidirectional protocol messages, and multiple active operations on one connection. | Requires ASP.NET Core WebSocket middleware and WebSocket-capable infrastructure. |
| SSE or HTTP streaming | Server-to-client streams over HTTP infrastructure.                                                                                       | Configure this through the HTTP transport.                                       |
| Plain HTTP            | Queries and mutations that complete in one response.                                                                                     | Use the GraphQL HTTP endpoint.                                                   |

Not every subscription client needs WebSockets. Use SSE or HTTP streaming if your client and infrastructure are better suited for HTTP-based streams.

# Enable WebSockets on the combined endpoint

Begin with the combined `/graphql` endpoint. This endpoint handles both HTTP requests and WebSocket upgrades on the same path, provided you register the ASP.NET Core WebSocket middleware.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQL()
    .AddSubscriptionType<Subscription>()
    .AddInMemorySubscriptions();

var app = builder.Build();

app.UseWebSockets();

app.MapGraphQL();

app.Run();
```

`AddSubscriptionType<Subscription>()` adds subscription fields to your schema. `AddInMemorySubscriptions()` registers a subscription provider for single-instance development. `UseWebSockets()` enables the ASP.NET Core WebSocket feature. `MapGraphQL()` allows WebSocket upgrade requests at `/graphql` in addition to GraphQL HTTP requests.

Make sure to register `UseWebSockets()` before the GraphQL endpoint handles requests. If you place it after `MapGraphQL()` or omit it, HTTP queries will still work, but WebSocket subscription clients will not be able to connect.

# Map a dedicated WebSocket endpoint

A dedicated endpoint is helpful when clients, proxies, or endpoint policies require a separate route for WebSocket traffic.

```csharp
app.UseWebSockets();

app.MapGraphQLHttp("/graphql");
app.MapGraphQLWebSocket("/graphql/ws");
```

If you do not specify a path, `MapGraphQLWebSocket()` defaults to `/graphql/ws`. The combined endpoint is more concise for most applications, but split endpoints are useful if your reverse proxy has separate upgrade rules or you want to apply different endpoint metadata to HTTP and WebSocket traffic.

# Use the modern WebSocket subprotocol

During the HTTP upgrade, the client must request a supported value in the `Sec-WebSocket-Protocol` header. Hot Chocolate selects a supported subprotocol and then processes messages for that protocol.

| Subprotocol string     | Status                                                               | Client notes                                                             |
| ---------------------- | -------------------------------------------------------------------- | ------------------------------------------------------------------------ |
| `graphql-transport-ws` | Modern GraphQL over WebSocket protocol, recommended for new clients. | Used by the JavaScript `graphql-ws` package.                             |
| `graphql-ws`           | Legacy Apollo subscriptions transport protocol.                      | Supported for older clients. Prefer migration to `graphql-transport-ws`. |

The package name `graphql-ws` and the legacy subprotocol string `graphql-ws` can be confusing. New JavaScript clients from the `graphql-ws` package use the modern `graphql-transport-ws` subprotocol.

If the upgrade request does not include a supported subprotocol, Hot Chocolate closes the socket with a protocol error.

# WebSocket message flow overview

The modern protocol follows a clear lifecycle, which can help you debug connection issues.

| Step | Client                                                                    | Server                                                  | Notes                                                                |
| ---- | ------------------------------------------------------------------------- | ------------------------------------------------------- | -------------------------------------------------------------------- |
| 1    | Sends an HTTP upgrade with `Sec-WebSocket-Protocol`.                      | Accepts the socket and selects a supported subprotocol. | Requires `UseWebSockets()`.                                          |
| 2    | Sends `connection_init`.                                                  | Runs `ISocketSessionInterceptor.OnConnectAsync`.        | Must happen before the initialization timeout.                       |
| 3    | Waits for the connection result.                                          | Sends `connection_ack` when accepted.                   | Rejected connections close.                                          |
| 4    | Sends `subscribe` with a unique operation id and GraphQL request payload. | Executes the operation.                                 | `subscribe` before accepted initialization is unauthorized.          |
| 5    | Receives one or more `next` messages.                                     | Sends operation results.                                | Subscriptions usually produce many results.                          |
| 6    | Sends `complete`, or receives `error` or `complete`.                      | Stops the operation.                                    | Syntax and validation failures are operation-level `error` messages. |
| 7    | Responds to server `ping` with `pong`, and may send `ping`.               | Sends keep-alive `ping` messages when configured.       | Applies to the modern protocol.                                      |
| 8    | Closes the socket.                                                        | Runs close cleanup.                                     | Close may be normal or error driven.                                 |

You can run multiple operations on a single WebSocket connection, as long as each active operation id is unique. Always send `complete` before reusing an operation id.

# Configure initialization timeout and keep-alive

Use schema-level options when every WebSocket endpoint for the schema should use the same timing.

```csharp
builder
    .AddGraphQL()
    .ModifyServerOptions(options =>
    {
        options.Sockets.ConnectionInitializationTimeout = TimeSpan.FromSeconds(15);
        options.Sockets.KeepAliveInterval = TimeSpan.FromSeconds(20);
    });
```

Use endpoint options when a dedicated WebSocket route needs different timing.

```csharp
app
    .MapGraphQLWebSocket("/graphql/ws")
    .WithOptions(options =>
    {
        options.ConnectionInitializationTimeout = TimeSpan.FromSeconds(15);
        options.KeepAliveInterval = TimeSpan.FromSeconds(20);
    });
```

| Option                            | Default         | What it controls                                                     | Common tuning reason                                              |
| --------------------------------- | --------------- | -------------------------------------------------------------------- | ----------------------------------------------------------------- |
| `ConnectionInitializationTimeout` | 10 seconds      | How long the server waits for `connection_init`.                     | Slow clients or debugging connection setup.                       |
| `KeepAliveInterval`               | 5 seconds       | How often the server sends keep-alive messages after initialization. | Keep proxies and load balancers from treating the socket as idle. |
| `KeepAliveInterval = null`        | Not the default | Disables server keep-alive messages.                                 | Use only when infrastructure or client behavior requires it.      |

Do not hide a missing `connection_init` by increasing the timeout. Fix client initialization first. Set keep-alive below the idle timeout of proxies and load balancers that close quiet sockets.

# Authenticate WebSocket connections

WebSocket authentication has three layers:

1. ASP.NET Core authentication can authenticate the HTTP upgrade request, for example with cookies or headers the browser sends during the upgrade.
2. `connection_init.payload` is WebSocket protocol data. Hot Chocolate does not automatically turn payload fields into `HttpContext.User`.
3. GraphQL authorization runs for operations after Hot Chocolate builds the operation request.

When a client can send normal authentication headers or cookies during the upgrade, configure ASP.NET Core authentication in the pipeline and keep using your GraphQL authorization rules. The default socket session interceptor copies the upgrade request user into each GraphQL operation request.

When the client sends a token in `connection_init.payload`, validate it with a socket session interceptor.

```csharp
builder
    .AddGraphQL()
    .AddSocketSessionInterceptor<AuthSocketSessionInterceptor>();

public sealed class AuthSocketSessionInterceptor : DefaultSocketSessionInterceptor
{
    public override ValueTask<ConnectionStatus> OnConnectAsync(
        ISocketSession session,
        IOperationMessagePayload connectionInitMessage,
        CancellationToken cancellationToken)
    {
        var payload = connectionInitMessage.As<Dictionary<string, object?>>();

        if (payload?.TryGetValue("authorization", out var value) == true &&
            value is string token &&
            IsValidToken(token))
        {
            return new(ConnectionStatus.Accept());
        }

        return new(ConnectionStatus.Reject("Unauthorized."));
    }

    private static bool IsValidToken(string token)
        => token == "Bearer example-token";
}
```

Keep the full token validation logic in your application services. If your interceptor needs services, inject them through its constructor.

# Add per-operation request state

`OnConnectAsync` runs once for the socket. `OnRequestAsync` runs for every operation sent on that socket. Use `OnRequestAsync` to add tenant ids, connection metadata, or data that you validated during connection initialization.

```csharp
public sealed class TenantSocketSessionInterceptor : DefaultSocketSessionInterceptor
{
    public override async ValueTask OnRequestAsync(
        ISocketSession session,
        string operationSessionId,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        await base.OnRequestAsync(
            session,
            operationSessionId,
            requestBuilder,
            cancellationToken);

        requestBuilder.TryAddGlobalState("tenantId", "example-tenant");
    }
}
```

Always call `base.OnRequestAsync(...)` unless you are replacing the default behavior intentionally. The default implementation adds important request state such as `HttpContext`, `ClaimsPrincipal`, `ISocketSession`, and the operation session id.

# Handle ping, pong, and close events

`KeepAliveInterval` controls server keep-alive messages. With the modern protocol, the server sends `ping` messages after the connection is initialized. Clients should answer with `pong`.

Socket session interceptors can observe client `ping` and `pong` messages and close cleanup. For example, you can include server time in a `pong` response to a client `ping`.

```csharp
public override ValueTask<IReadOnlyDictionary<string, object?>?> OnPingAsync(
    ISocketSession session,
    IOperationMessagePayload pingMessage,
    CancellationToken cancellationToken)
{
    IReadOnlyDictionary<string, object?> payload = new Dictionary<string, object?>
    {
        ["serverTime"] = DateTimeOffset.UtcNow
    };

    return new(payload);
}
```

Use `OnPongAsync` for observability such as latency tracking. Use `OnCloseAsync` to clean up connection-scoped resources.

# Connect with a modern JavaScript client

The JavaScript `graphql-ws` package speaks the modern `graphql-transport-ws` protocol.

```ts
import { createClient } from "graphql-ws";

const client = createClient({
  url: "ws://localhost:5000/graphql",
  connectionParams: {
    authorization: `Bearer ${token}`,
  },
});
```

`connectionParams` becomes the `connection_init.payload` that `OnConnectAsync` receives. If you mapped a dedicated WebSocket endpoint, use that URL instead:

```ts
const client = createClient({
  url: "ws://localhost:5000/graphql/ws",
});
```

A subscription operation sent through that connection uses normal GraphQL syntax.

```graphql
subscription OnBookAdded {
  onBookAdded {
    title
  }
}
```

# Prepare for reverse proxies and scale-out

A reverse proxy must allow WebSocket upgrades and forward the headers your authentication flow needs. Check that the proxy preserves or recreates these parts of the request:

- `Connection` and `Upgrade` handling for WebSocket upgrades.
- `Sec-WebSocket-Protocol` so Hot Chocolate can negotiate `graphql-transport-ws` or the legacy protocol.
- Authentication headers or cookies used by the upgrade request.
- Path rewrites for `/graphql` or `/graphql/ws`.

WebSocket transport keeps each client connected to one server instance. The subscription provider moves events to subscribers. `AddInMemorySubscriptions()` is suitable for local development and single-instance deployments. Use Redis, NATS, Postgres, or another distributed provider when events can be published on one server while subscribers are connected to another server. Sticky sessions can help infrastructure route an open socket, but they do not replace a distributed subscription provider.

# Read close codes and operation errors

Hot Chocolate uses protocol close codes for connection-level problems in the modern protocol.

| Code   | Meaning                           | Common cause                                                                                | Fix                                                                  |
| ------ | --------------------------------- | ------------------------------------------------------------------------------------------- | -------------------------------------------------------------------- |
| `4400` | Protocol error                    | Invalid JSON shape, missing `type`, invalid message type, or invalid `subscribe` structure. | Send valid protocol messages and request a supported subprotocol.    |
| `4401` | Unauthorized                      | The client subscribed before accepted initialization or the connection was rejected.        | Send `connection_init` and pass authentication checks.               |
| `4408` | Connection initialization timeout | The client did not send `connection_init` before the timeout.                               | Fix client initialization or tune `ConnectionInitializationTimeout`. |
| `4409` | Subscriber id not unique          | The client reused an active operation id.                                                   | Use unique ids and send `complete` before reuse.                     |
| `4429` | Too many init attempts            | The client sent `connection_init` more than once.                                           | Send one initialization message per socket.                          |

GraphQL parsing and validation failures usually produce an operation-level `error` message for the operation id instead of closing the whole socket.

# Troubleshoot WebSocket connections

| Symptom                                                           | Likely cause                                                                       | Fix                                                                                          |
| ----------------------------------------------------------------- | ---------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------- |
| Client receives `404` or never upgrades.                          | Wrong path, endpoint not mapped, or proxy blocks upgrade.                          | Verify the client URL, `MapGraphQL()` or `MapGraphQLWebSocket()`, and proxy upgrade support. |
| WebSocket feature is unavailable.                                 | `app.UseWebSockets()` is missing or ordered too late.                              | Call `app.UseWebSockets()` before GraphQL endpoint handling.                                 |
| Immediate protocol close.                                         | Missing or unsupported `Sec-WebSocket-Protocol`, or invalid first messages.        | Use `graphql-transport-ws` for new clients and send JSON protocol messages.                  |
| Close after about 10 seconds.                                     | Client did not send `connection_init`.                                             | Fix client initialization, then tune the timeout only if needed.                             |
| HTTP authentication works, but subscription authentication fails. | Token is only in `connection_init.payload` and no socket interceptor validates it. | Validate the payload in `ISocketSessionInterceptor` or authenticate the upgrade request.     |
| Connection is accepted, but the operation is unauthorized.        | Operation request lacks the expected user or global state.                         | Preserve `base.OnRequestAsync(...)` and add custom state per operation.                      |
| Subscription connects but receives no events.                     | Missing provider, mismatched topic, publisher not sending, or schema field issue.  | Verify subscription schema and provider configuration.                                       |
| Works locally but misses events on multiple instances.            | The in-memory provider is local to one server.                                     | Use Redis, NATS, Postgres, or another distributed provider.                                  |
| Duplicate id closes the socket.                                   | Active operation id was reused.                                                    | Use unique ids and send `complete` before reuse.                                             |
| Connections drop behind a load balancer.                          | Idle timeout or blocked WebSocket upgrade.                                         | Tune `KeepAliveInterval` and configure infrastructure for WebSockets.                        |

# Next steps

- Configure routes and endpoint policies with [Endpoint mapping](/docs/hotchocolate/v16/build/server-configuration/endpoints).
- Configure HTTP, SSE, multipart, and incremental delivery with [HTTP transport](/docs/hotchocolate/v16/build/server-configuration/http-transport).
- Build subscription fields, topics, publishing, and providers with [Subscriptions](/docs/hotchocolate/v16/build/schema-elements/operations-subscriptions).
- Customize the full socket lifecycle with [Interceptors](/docs/hotchocolate/v16/build/server-configuration/interceptors).
- Apply operation authorization with [Authorization](/docs/hotchocolate/v16/build/security/authorization).
