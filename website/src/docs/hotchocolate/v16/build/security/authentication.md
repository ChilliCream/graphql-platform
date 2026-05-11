---
title: Authentication
---

Authentication determines who is calling your GraphQL API. Authentication is managed by ASP.NET Core. Hot Chocolate does not parse JWTs, validate cookies, issue tokens, or implement login flows. Instead, it receives the `ClaimsPrincipal` that ASP.NET Core places on `HttpContext.User` and makes that principal available during GraphQL execution.

A typical request flows as follows:

```text
client credentials -> ASP.NET Core authentication -> HttpContext.User -> Hot Chocolate request state -> resolver
```

Use this page to learn how to configure identity, send credentials, or access the current user. For controlling access to fields, types, operations, or endpoints, see [authorization](../attributes/authorize).

| Concept                     | Responsibility                                                                            | Typical configuration                                             |
| --------------------------- | ----------------------------------------------------------------------------------------- | ----------------------------------------------------------------- |
| Authentication              | Validate credentials and set `HttpContext.User`                                           | ASP.NET Core `AddAuthentication` and `UseAuthentication`          |
| Hot Chocolate authorization | Apply GraphQL authorization rules such as `[Authorize]`, `.Authorize()`, and `@authorize` | `HotChocolate.AspNetCore.Authorization` and `.AddAuthorization()` |
| Endpoint authorization      | Challenge or forbid before GraphQL execution                                              | ASP.NET Core `UseAuthorization` and `RequireAuthorization()`      |
| Identity mapping            | Convert claims into a domain user, tenant, or viewer object                               | Request interceptors or global state                              |

# Configuring ASP.NET Core Authentication

Register an ASP.NET Core authentication scheme in `Program.cs`. The following example uses JWT bearer tokens with placeholder identity provider values:

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://identity.example.com";
        options.Audience = "graphql-api";
    });
```

Install the `Microsoft.AspNetCore.Authentication.JwtBearer` package to use the JWT bearer handler. Refer to your identity provider and ASP.NET Core authentication documentation for details on issuer, audience, token validation, certificates, key rotation, and production secrets.

Common credential styles all use the same Hot Chocolate integration point:

| Credential style | ASP.NET Core setup                                                           | GraphQL transport note                                                                                                    |
| ---------------- | ---------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------- |
| Bearer token     | `AddJwtBearer`                                                               | Send `Authorization: Bearer <token>` on HTTP requests and on WebSocket upgrade requests when the client supports headers. |
| Cookie           | Cookie authentication, ASP.NET Core Identity, or OpenID Connect with cookies | Browsers send cookies according to SameSite, CORS, domain, secure, and credentials settings.                              |
| Custom handler   | `AddScheme<TOptions, THandler>`                                              | Hot Chocolate sees the resulting `ClaimsPrincipal`, not the credential format.                                            |

# Add Authentication Middleware Before GraphQL

Add the authentication middleware before the GraphQL endpoint is executed:

```csharp
app.UseAuthentication();
app.UseAuthorization();

app.MapGraphQL();
```

`UseAuthentication()` runs authentication handlers and populates `HttpContext.User`. `UseAuthorization()` enforces ASP.NET Core endpoint authorization and policies. It does not authenticate a request by itself, but most secured ASP.NET Core apps include it after `UseAuthentication()`.

If you enable WebSocket subscriptions, add WebSocket middleware before mapping GraphQL:

```csharp
app.UseAuthentication();
app.UseAuthorization();
app.UseWebSockets();

app.MapGraphQL();
```

When credentials are valid, `HttpContext.User.Identity.IsAuthenticated` is true before Hot Chocolate creates the GraphQL request. If credentials are absent, public GraphQL fields can still execute and will usually see an anonymous principal unless endpoint or field authorization blocks the request first. If credentials are invalid, the result depends on the ASP.NET Core authentication handler and whether endpoint authorization requires an authenticated user.

# Authentication vs. Field Authorization

Reading the current user and protecting a field are separate concerns.

You do not need GraphQL `.AddAuthorization()` to access `ClaimsPrincipal` in resolvers. Hot Chocolate's default HTTP and WebSocket interceptors copy the ASP.NET Core user into GraphQL request state.

You do need Hot Chocolate authorization services when you use GraphQL authorization features:

```csharp
builder
    .AddGraphQL()
    .AddAuthorization()
    .AddQueryType<Query>();
```

Use field, type, role, or policy rules when you want to control access. Use resolver code to read identity data needed to load the current user's data.

# Sending Authenticated HTTP Requests

For GraphQL over HTTP, send credentials as you would for any ASP.NET Core endpoint. A bearer token request looks like this:

```http
POST /graphql HTTP/1.1
Host: api.example.com
Authorization: Bearer <token>
Content-Type: application/json

{"query":"query { me { id name } }"}
```

GET requests, persisted operation requests, and multipart requests follow the same HTTP authentication rules. Server-Sent Events also use an HTTP request, commonly with `Accept: text/event-stream`, so the same authentication middleware processes the request before Hot Chocolate handles it.

Cookie authentication works when the client sends the cookie to the GraphQL endpoint. For browser applications, verify CORS, `fetch` credentials mode, cookie domain, SameSite, and secure settings.

# Accessing the Current User in Resolvers

Resolver parameter injection is the preferred code-first approach for implementation-first types. Use `ClaimsPrincipal?` for fields that allow anonymous callers:

```csharp
using System.Security.Claims;

[QueryType]
public static partial class UserQueries
{
    public static User? GetMe(ClaimsPrincipal? user, UserService users)
    {
        var userId = user?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId is null)
        {
            return null;
        }

        return users.GetById(userId);
    }
}
```

For descriptor-based types, use `context.GetUser()`:

```csharp
using System.Security.Claims;
using HotChocolate.Types;

public sealed class UserQueriesType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("me")
            .Resolve(context =>
            {
                var user = context.GetUser();
                var userId = user?.FindFirstValue(ClaimTypes.NameIdentifier);

                if (userId is null)
                {
                    return null;
                }

                return context.Service<UserService>().GetById(userId);
            });
    }
}
```

A nullable `ClaimsPrincipal?` or `context.GetUser()` works well for public fields. Use a non-nullable `ClaimsPrincipal` parameter only when the field should always execute with a principal in GraphQL request state.

For tenant, viewer, or domain-user objects, consider mapping claims once in a request interceptor and storing the mapped value in request state. This keeps resolvers focused on application data and centralizes identity normalization. See the interceptor and global state pages for full patterns.

# How Hot Chocolate Receives the User

Hot Chocolate includes default interceptors for ASP.NET Core transports:

- `DefaultHttpRequestInterceptor.OnCreateAsync(...)` reads `HttpContext.User` for HTTP requests and stores the `ClaimsPrincipal` in GraphQL request state.
- `DefaultSocketSessionInterceptor.OnRequestAsync(...)` reads `session.Connection.HttpContext.User` for each WebSocket operation and stores the `ClaimsPrincipal` in GraphQL request state.
- `context.GetUser()` reads that same request state entry.

If you replace these interceptors, call the base implementation or set the user state yourself. Otherwise, resolvers may see an anonymous or missing user even when ASP.NET Core authenticated the request.

A request interceptor can also read the authenticated user and add application-specific state:

```csharp
using System.Security.Claims;
using HotChocolate.Execution;

public sealed class CurrentUserInterceptor : DefaultHttpRequestInterceptor
{
    public override ValueTask OnCreateAsync(
        HttpContext context,
        IRequestExecutor requestExecutor,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        var principal = context.User;
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId is not null)
        {
            requestBuilder.SetGlobalState("currentUserId", userId);
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
    .AddHttpRequestInterceptor<CurrentUserInterceptor>();
```

# Endpoint Authorization vs. Field Authorization

Endpoint authorization protects the ASP.NET Core endpoint before GraphQL execution:

```csharp
app.MapGraphQL().RequireAuthorization();
```

Use endpoint authorization when every request through that endpoint should require an authenticated caller. This can affect GraphQL HTTP, WebSocket connections, SDL download, and Nitro when they share the same endpoint.

Field authorization runs inside GraphQL execution. Use `[Authorize]`, descriptor `.Authorize()`, or schema directives when public and protected fields share a schema or endpoint. Prefer authorization policies over resolver `if` checks when the rule is an access-control decision, especially when the rule is reused or must be audited. Resolver checks are better suited for shaping data after access has already been granted.

# Authenticating Subscriptions and WebSocket Operations

WebSocket subscriptions use a long-lived connection, so you must decide where authentication occurs.

One common pattern is to authenticate the HTTP upgrade request with standard ASP.NET Core authentication. Cookies work naturally for browser clients. Some non-browser clients can send an `Authorization` header during the upgrade.

Another approach is to send authentication data in the GraphQL over WebSocket `connection_init` payload:

```json
{ "type": "connection_init", "payload": { "authorization": "Bearer <token>" } }
```

The default socket interceptor accepts connections. If sockets must be authenticated, enforce this through endpoint authorization, a custom `ISocketSessionInterceptor`, or both.

When a token is supplied only in `connection_init`, validating the token in `OnConnectAsync` is not sufficient. The resulting `ClaimsPrincipal` must also be available to later operation requests. One pattern is to store the validated principal on the connection `HttpContext` and assign it before the base `OnRequestAsync` copies the user into GraphQL request state:

```csharp
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.AspNetCore.Subscriptions.Protocols;

public sealed class AuthSocketSessionInterceptor : DefaultSocketSessionInterceptor
{
    private const string SocketUserKey = "socketUser";

    public override async ValueTask<ConnectionStatus> OnConnectAsync(
        ISocketSession session,
        IOperationMessagePayload connectionInitMessage,
        CancellationToken cancellationToken = default)
    {
        var payload = connectionInitMessage.Payload?.Deserialize<AuthPayload>();
        var principal = await ValidateTokenAsync(
            payload?.Authorization,
            cancellationToken);

        if (principal is null)
        {
            return ConnectionStatus.Reject();
        }

        session.Connection.HttpContext.Items[SocketUserKey] = principal;
        return ConnectionStatus.Accept();
    }

    public override ValueTask OnRequestAsync(
        ISocketSession session,
        string operationSessionId,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken = default)
    {
        if (session.Connection.HttpContext.Items[SocketUserKey] is ClaimsPrincipal user)
        {
            session.Connection.HttpContext.User = user;
        }

        return base.OnRequestAsync(
            session,
            operationSessionId,
            requestBuilder,
            cancellationToken);
    }

    private static ValueTask<ClaimsPrincipal?> ValidateTokenAsync(
        string? authorization,
        CancellationToken cancellationToken)
    {
        // Replace this with your token validation service.
        return ValueTask.FromResult<ClaimsPrincipal?>(null);
    }

    private sealed class AuthPayload
    {
        [JsonPropertyName("authorization")]
        public string? Authorization { get; init; }
    }
}
```

Register the socket interceptor on the GraphQL builder:

```csharp
builder
    .AddGraphQL()
    .AddSocketSessionInterceptor<AuthSocketSessionInterceptor>();
```

Plan for token expiration on long-lived subscriptions. Your application should close the socket, ask the client to reconnect, or revalidate according to your security requirements.

# Testing Authenticated Requests

Executor-level tests can set the user directly on the operation request:

```csharp
using System.Security.Claims;
using HotChocolate.Execution;

var identity = new ClaimsIdentity(
    [new Claim(ClaimTypes.NameIdentifier, "user-123")],
    authenticationType: "Test");

var claimsPrincipal = new ClaimsPrincipal(identity);

var request = OperationRequestBuilder.New()
    .SetDocument("query { me { id } }")
    .SetUser(claimsPrincipal)
    .Build();
```

ASP.NET Core integration tests should exercise the real endpoint shape. Use a test authentication handler, or send a token accepted by the test configuration. Assert GraphQL response data for public fields. Expect HTTP 401 or 403 only when endpoint authorization is configured to challenge or forbid before GraphQL execution.

For WebSocket tests, send `connection_init` with and without the required auth payload. Assert `connection_ack` for accepted connections and a rejected connection or close status for unauthorized sockets according to your interceptor.

# Troubleshooting

| Symptom                                                                            | Likely cause                                                                                                      | Fix                                                                                                                                |
| ---------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------- |
| Resolver sees an anonymous user                                                    | `UseAuthentication()` is missing, runs after GraphQL, uses the wrong default scheme, or no credentials were sent  | Configure the scheme, add middleware before `MapGraphQL()`, and inspect headers or cookies                                         |
| `UseAuthorization()` is present but the user is anonymous                          | Authorization middleware does not authenticate                                                                    | Add and order `UseAuthentication()` before `UseAuthorization()`                                                                    |
| Non-nullable `ClaimsPrincipal` parameter fails                                     | No principal is present in GraphQL request state                                                                  | Use `ClaimsPrincipal?` for anonymous fields, or ensure the default interceptor behavior runs                                       |
| `[Authorize]` has no effect                                                        | Hot Chocolate authorization package or `.AddAuthorization()` is missing, or the wrong attribute namespace is used | Install `HotChocolate.AspNetCore.Authorization`, call `.AddGraphQL().AddAuthorization()`, and use Hot Chocolate authorization APIs |
| Field auth returns a GraphQL error with HTTP 200                                   | Field authorization runs inside GraphQL execution                                                                 | Use endpoint authorization when the whole endpoint should challenge before execution                                               |
| Nitro is blocked by an authentication challenge                                    | `RequireAuthorization()` was applied to the combined endpoint that also serves Nitro                              | Split endpoint mappings or keep Nitro on an unprotected endpoint                                                                   |
| WebSocket accepts anonymous clients                                                | The default socket interceptor accepts connections                                                                | Require auth on the upgrade request or implement `ISocketSessionInterceptor.OnConnectAsync`                                        |
| WebSocket resolver sees an anonymous user after `connection_init` token validation | The validated principal was not propagated to operation request state                                             | Store the principal for the connection and set `HttpContext.User` or request state before each operation                           |
| Custom interceptor removes the user                                                | The base interceptor method was not called                                                                        | Call `base.OnCreateAsync(...)` or `base.OnRequestAsync(...)`, or explicitly set the required state                                 |

# Next Steps

- [Authorization](../attributes/authorize): protect fields, types, policies, and endpoints.
- [Interceptors](../server-configuration/interceptors): customize HTTP and WebSocket request creation.
- [Global state](../server-configuration/global-state): store a mapped user, tenant, or viewer value.
- [HTTP transport](../server-configuration/http-transport): understand GraphQL over HTTP and SSE behavior.
- [WebSocket transport](../server-configuration/websocket-transport): configure WebSocket subscriptions.
- [Endpoints](../server-configuration/endpoints): split Nitro, HTTP, WebSocket, and SDL endpoints.
