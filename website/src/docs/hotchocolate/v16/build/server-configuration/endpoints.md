---
title: Endpoints
---

A Hot Chocolate schema becomes accessible to clients when you map it into the ASP.NET Core route table. First, configure the schema and request executor using `builder.AddGraphQL()`. Then, expose the schema with `app.MapGraphQL()` or another specialized endpoint mapping method.

Refer to this page when you need to select the endpoint URL, separate transports onto different paths, apply endpoint policies, or troubleshoot client connectivity issues with your GraphQL server.

# Exposing GraphQL at `/graphql`

The combined endpoint is the default and recommended starting point for most applications.

```csharp
#nullable enable

var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQL()
    .AddQueryType<Query>();

var app = builder.Build();

app.MapGraphQL();

app.Run();

public sealed class Query
{
    public string Hello() => "world";
}
```

By default, `MapGraphQL()` maps Hot Chocolate to `/graphql` if you do not specify a path.

You can run the following operation against `POST /graphql`:

```graphql
query {
  hello
}
```

The expected result is:

```json
{
  "data": {
    "hello": "world"
  }
}
```

The expected SDL is:

```graphql
type Query {
  hello: String!
}
```

The combined endpoint supports these request surfaces beneath the mapped path:

| Surface      | Default behavior                                                                                                                    |
| ------------ | ----------------------------------------------------------------------------------------------------------------------------------- |
| GraphQL HTTP | `GET`, `POST`, and multipart requests use `/graphql`.                                                                               |
| WebSockets   | Subscription clients connect to `/graphql` after `app.UseWebSockets()` is registered.                                               |
| SDL download | `GET /graphql?sdl` returns the schema when schema requests and schema file support are enabled.                                     |
| Nitro        | Browser navigation to `/graphql` opens Nitro when the tool is enabled. Older docs may refer to this browser IDE as Banana Cake Pop. |
| Fallback     | Requests to the endpoint that do not match a GraphQL surface return `404`.                                                          |

# Choosing the Endpoint Path

Specify a path if you do not want clients to use the default `/graphql` route.

```csharp
app.MapGraphQL("/api/graphql");
```

Select a stable path early in your project. Many components, such as clients, Nitro documents, gateway configurations, reverse proxy rewrites, CORS rules, authorization policies, persisted operation URLs, and telemetry, may depend on this path.

If you change the route, be sure to update every component that references the old URL:

- Client base URLs
- Nitro `GraphQLEndpoint` settings
- Reverse proxy rewrite rules and path base settings
- CORS origins, methods, and headers
- Gateway or router configuration
- Monitoring dashboards and synthetic checks

# Default: Use the Combined Endpoint

The combined endpoint provides a compact and consistent layout for both development and production environments:

```csharp
app.UseWebSockets();

app.MapGraphQL("/graphql");
```

Register `UseWebSockets()` before mapping the endpoint if you plan to support subscriptions over WebSockets. If you omit this, HTTP requests will still work, but WebSocket upgrade requests will fail.

Server-Sent Events (SSE) do not have a dedicated endpoint mapping method. SSE is negotiated by the HTTP transport on the GraphQL HTTP endpoint. For details on payload formats, status codes, content negotiation, SSE behavior, WebSocket subprotocols, and interceptor examples, see the transport and interceptor documentation.

Use split endpoints when different surfaces require distinct paths, policies, or exposure rules.

# Splitting Endpoint Responsibilities

## Mapping Only GraphQL HTTP Requests

Use `MapGraphQLHttp()` to create an execution endpoint for HTTP traffic that does not include Nitro, SDL download, or a standalone WebSocket endpoint.

```csharp
app.MapGraphQLHttp("/graphql");
```

This endpoint handles `POST`, `GET`, and multipart requests as defined by `GraphQLServerOptions`. It does not map Nitro or serve schema download requests, so `Tool` and `EnableSchemaRequests` are not relevant for a standalone HTTP mapping.

See [HTTP Transport](/docs/hotchocolate/v16/build/server-configuration/http-transport) for details on request bodies, headers, status codes, content negotiation, and SSE.

## Mapping WebSocket Subscriptions Separately

Use `MapGraphQLWebSocket()` if WebSocket traffic should use a different path or policy.

```csharp
app.UseWebSockets();

app.MapGraphQLWebSocket("/graphql/ws");
```

The default WebSocket path is `/graphql/ws`. You can configure protocol timing with `GraphQLSocketOptions`:

```csharp
app
    .MapGraphQLWebSocket("/graphql/ws")
    .WithOptions(options =>
    {
        options.ConnectionInitializationTimeout = TimeSpan.FromSeconds(10);
        options.KeepAliveInterval = TimeSpan.FromSeconds(5);
    });
```

For details on connection initialization payloads, subprotocols, and keep-alive protocol, see the WebSocket transport documentation.

## Mapping Nitro Separately

Map a standalone Nitro endpoint if the browser IDE should use a different path or policy than execution.

```csharp
app.MapGraphQLHttp("/graphql");
app.MapNitroApp("/graphql/ui", relativeRequestPath: "..");
```

The default standalone Nitro path is `/graphql/ui`. The `relativeRequestPath` tells Nitro which GraphQL endpoint to use when creating documents. For a `/graphql/ui` to `/graphql` layout, use `..`.

Configure Nitro with `NitroAppOptions`:

```csharp
app
    .MapNitroApp("/graphql/ui")
    .WithOptions(options =>
    {
        options.Title = "Products API";
        options.GraphQLEndpoint = "..";
        options.IncludeCookies = true;
    });
```

Common Nitro options include `Enable`, `GraphQLEndpoint`, `UseBrowserUrlAsGraphQLEndpoint`, `Document`, `UseGet`, `HttpHeaders`, `IncludeCookies`, `Title`, `DisableTelemetry`, and `GaTrackingId`.

## Mapping SDL Endpoints

`MapGraphQL()` supports `GET /graphql?sdl` when schema requests and schema file support are enabled. You can also expose SDL through dedicated endpoints:

```csharp
app.MapGraphQLSchema("/graphql/sdl");
app.MapGraphQLSemanticNonNullSchema("/graphql/semantic-non-null-schema.graphql");
```

`MapGraphQLSchema()` defaults to `/graphql/sdl`. `MapGraphQLSemanticNonNullSchema()` defaults to `/graphql/semantic-non-null-schema.graphql` and is used for semantic non-null SDL scenarios. For schema export workflows, see the [Command Line](/docs/hotchocolate/v16/build/server-configuration/command-line) page.

SDL endpoint exposure and GraphQL introspection are separate controls. Use [Introspection](/docs/hotchocolate/v16/build/security/introspection) to manage introspection behavior.

## Mapping Persisted Operation Endpoints

Use `MapGraphQLPersistedOperations()` to expose registered operations through REST-like URLs.

```csharp
app.MapGraphQLPersistedOperations(
    "/graphql/persisted",
    requireOperationName: true);
```

The default path is `/graphql/persisted`. This maps the endpoint surface only. For storing, publishing, and validating trusted documents, see [Trusted Documents](/docs/hotchocolate/v16/build/security/trusted-documents) and [Private API](/docs/hotchocolate/v16/_leagcy/guides/private-api).

# Configuring Endpoint Behavior

## Setting Schema-Level Defaults

Use `ModifyServerOptions(...)` on the GraphQL builder to apply consistent behavior wherever the schema is mapped.

```csharp
builder
    .AddGraphQL()
    .ModifyServerOptions(options =>
    {
        options.EnableGetRequests = false;
        options.EnableSchemaRequests = builder.Environment.IsDevelopment();
        options.Tool.Enable = builder.Environment.IsDevelopment();
    })
    .AddQueryType<Query>();
```

Schema-level defaults are part of the request executor. Use them for decisions that should apply across the environment, such as GET support, multipart support, SDL downloads, Nitro exposure, batching, concurrency, and WebSocket timing.

## Overriding a Single Mapped Endpoint

Use `WithOptions(...)` on a mapping if a specific endpoint requires different behavior.

```csharp
app
    .MapGraphQL("/graphql")
    .WithOptions(options => options.EnableGetRequests = false);
```

Endpoint overrides are layered on top of schema-level defaults.

| Mapping                 | Options hook                                                                                     |
| ----------------------- | ------------------------------------------------------------------------------------------------ |
| `MapGraphQL()`          | `WithOptions(Action<GraphQLServerOptions>)` and Nitro options through `options.Tool`.            |
| `MapGraphQLHttp()`      | `WithOptions(Action<GraphQLServerOptions>)`, except Nitro and SDL download options do not apply. |
| `MapGraphQLWebSocket()` | `WithOptions(Action<GraphQLSocketOptions>)`.                                                     |
| `MapNitroApp()`         | `WithOptions(Action<NitroAppOptions>)`.                                                          |

## Restricting Development-Only Surfaces

Intentionally expose Nitro and SDL downloads. A typical production setup enables these features only in development.

```csharp
builder
    .AddGraphQL()
    .ModifyServerOptions(options =>
    {
        options.Tool.Enable = builder.Environment.IsDevelopment();
        options.EnableSchemaRequests = builder.Environment.IsDevelopment();
        options.EnableSchemaFileSupport = builder.Environment.IsDevelopment();
    })
    .AddQueryType<Query>();
```

If you map standalone SDL or Nitro endpoints, secure or conditionally map those endpoints as well.

```csharp
if (app.Environment.IsDevelopment())
{
    app.MapNitroApp("/graphql/ui");
    app.MapGraphQLSchema("/graphql/sdl");
}

app.MapGraphQLHttp("/graphql");
```

## Restricting HTTP Methods and Operation Types

By default, GET execution is enabled, but only queries are permitted over GET. For production APIs that do not require GET, disable GET execution or restrict it to queries only.

```csharp
builder
    .AddGraphQL()
    .ModifyServerOptions(options =>
    {
        options.EnableGetRequests = false;
        options.AllowedGetOperations = AllowedGetOperations.Query;
    });
```

If you enable `EnforceGetRequestsPreflightHeader`, clients must send the required GraphQL preflight header. For more on CSRF and HTTP header behavior, see the HTTP transport documentation.

## Handling Uploads Deliberately

Multipart upload requests are enabled by default, as is multipart preflight header enforcement.

```csharp
builder
    .AddGraphQL()
    .ModifyServerOptions(options =>
    {
        options.EnableMultipartRequests = true;
        options.EnforceMultipartRequestsPreflightHeader = true;
    });
```

When enforcement is enabled, multipart clients must send the `GraphQL-Preflight` header. For details on upload scalar usage, multipart body shape, Kestrel and IIS limits, and `FormOptions`, see the [Files](/docs/hotchocolate/v16/_leagcy/server/files) documentation.

# Applying ASP.NET Core Endpoint Policies

Endpoint policies protect the ASP.NET Core route, while schema authorization secures fields and types within GraphQL execution. Use both when you need to enforce boundaries at both levels.

```csharp
app.UseAuthentication();
app.UseAuthorization();

app.MapGraphQL("/graphql").RequireAuthorization();
```

Calling `RequireAuthorization()` on the combined endpoint applies the policy to the entire route surface, including GraphQL HTTP, WebSockets, Nitro, and SDL requests beneath that route. Use split endpoints if you need different policies for different surfaces.

```csharp
app.UseAuthentication();
app.UseAuthorization();

app
    .MapGraphQLHttp("/graphql")
    .RequireAuthorization();

app.MapNitroApp("/graphql/ui");
```

You can also apply other ASP.NET Core endpoint policies, such as rate limiting:

```csharp
app
    .MapGraphQL("/graphql")
    .RequireRateLimiting("graphql");
```

Register middleware in the correct ASP.NET Core order before mapping endpoints. A typical secured GraphQL app uses this order:

```csharp
app.UseForwardedHeaders();
app.UseRouting();
app.UseCors("graphql");
app.UseAuthentication();
app.UseAuthorization();
app.UseWebSockets();

app.MapGraphQL("/graphql");
```

Minimal hosting does not require `UseRouting()` for basic endpoint mapping, but if your app uses explicit routing middleware, maintain this order.

For field or type authorization within the schema, use `[Authorize]` from `HotChocolate.Authorization` and `.AddAuthorization()` on the GraphQL builder. See [Authorize attribute](/docs/hotchocolate/v16/build/attributes/authorize) for more on schema authorization.

# Hosting Multiple Schemas

When your application hosts more than one request executor, always pass the schema name explicitly. Do not rely on implicit schema resolution in multi-schema scenarios.

```csharp
builder.Services
    .AddGraphQLServer("Public")
    .AddQueryType<PublicQuery>();

builder.Services
    .AddGraphQLServer("Admin")
    .AddQueryType<AdminQuery>();

var app = builder.Build();

app.MapGraphQL("/graphql", "Public");

app
    .MapGraphQL("/admin/graphql", "Admin")
    .RequireAuthorization("Admin");
```

Use named schemas for public and admin APIs, bounded contexts, side-by-side migrations, and test hosts. All GraphQL execution mapping methods accept an optional `schemaName`. `MapNitroApp()` is Nitro-only and points to a GraphQL endpoint URL rather than owning a schema.

Options are also named. Configure defaults on the builder for the schema you intend to map.

# Keeping Paths Stable Behind Reverse Proxies

When ASP.NET Core runs behind a reverse proxy or under a path base, the URL visible to clients may differ from the route your app maps. Ensure the external URL, forwarded headers, path base, proxy rewrite rules, and Nitro endpoint configuration are all aligned.

For example, if a proxy exposes `/api/graphql` and forwards to an app that maps `/graphql`, configure both the proxy rewrite and the client URL accordingly. If Nitro is served behind the same prefix, set `GraphQLEndpoint` to the browser-visible GraphQL URL or an appropriate relative path.

# Considering Persisted-Operation-Only Production

Some private APIs expose only trusted documents in production environments.

```csharp
if (app.Environment.IsDevelopment())
{
    app.MapGraphQL();
}
else
{
    app.MapGraphQLPersistedOperations("/graphql");
}
```

Adopt this pattern only after clients are able to publish and call persisted operations. Retain the standard GraphQL endpoint until the trusted document workflow is fully verified.

# Endpoint Reference

| API                                                                                                           | Default Path                                | Purpose                                                            | `schemaName` | Options                                                        |
| ------------------------------------------------------------------------------------------------------------- | ------------------------------------------- | ------------------------------------------------------------------ | ------------ | -------------------------------------------------------------- |
| `MapGraphQL(path = "/graphql", schemaName = null)`                                                            | `/graphql`                                  | Combined HTTP, multipart, GET, WebSocket, SDL, and Nitro endpoint. | Yes          | `GraphQLServerOptions`                                         |
| `MapGraphQLHttp(pattern = "/graphql", schemaName = null)`                                                     | `/graphql`                                  | HTTP `POST`, `GET`, and multipart execution.                       | Yes          | `GraphQLServerOptions` without Nitro and SDL download behavior |
| `MapGraphQLWebSocket(pattern = "/graphql/ws", schemaName = null)`                                             | `/graphql/ws`                               | WebSocket subscriptions.                                           | Yes          | `GraphQLSocketOptions`                                         |
| `MapNitroApp(toolPath = "/graphql/ui", relativeRequestPath = "..")`                                           | `/graphql/ui`                               | Nitro browser IDE.                                                 | No           | `NitroAppOptions`                                              |
| `MapGraphQLSchema(pattern = "/graphql/sdl", schemaName = null)`                                               | `/graphql/sdl`                              | SDL download endpoint.                                             | Yes          | Endpoint policies                                              |
| `MapGraphQLSemanticNonNullSchema(pattern = "/graphql/semantic-non-null-schema.graphql", schemaName = null)`   | `/graphql/semantic-non-null-schema.graphql` | Semantic non-null SDL endpoint.                                    | Yes          | Endpoint policies                                              |
| `MapGraphQLPersistedOperations(path = "/graphql/persisted", schemaName = null, requireOperationName = false)` | `/graphql/persisted`                        | Persisted operation execution by URL.                              | Yes          | Endpoint policies                                              |

# Endpoint Option Reference

| Option                                    | Default                      | Applies to                     | Change it when...                                                           |
| ----------------------------------------- | ---------------------------- | ------------------------------ | --------------------------------------------------------------------------- |
| `EnableGetRequests`                       | `true`                       | GraphQL HTTP                   | You want to reject all GraphQL GET execution.                               |
| `AllowedGetOperations`                    | `AllowedGetOperations.Query` | GraphQL HTTP                   | You need to control which operation types may use GET.                      |
| `EnforceGetRequestsPreflightHeader`       | `false`                      | GraphQL HTTP                   | You require the GraphQL preflight header on GET requests.                   |
| `EnableMultipartRequests`                 | `true`                       | GraphQL HTTP                   | You want to reject multipart upload requests.                               |
| `EnforceMultipartRequestsPreflightHeader` | `true`                       | GraphQL HTTP                   | You require upload clients to send `GraphQL-Preflight`.                     |
| `EnableSchemaRequests`                    | `true`                       | Combined endpoint SDL requests | You want to disable `?sdl` handling.                                        |
| `EnableSchemaFileSupport`                 | `true`                       | SDL file support               | You want to disable schema SDL file support.                                |
| `Batching`                                | `AllowedBatching.None`       | GraphQL HTTP execution         | You intentionally support request or variable batching.                     |
| `MaxBatchSize`                            | `1024`                       | GraphQL HTTP execution         | You allow batching and need an operation limit.                             |
| `MaxConcurrentExecutions`                 | `64`                         | GraphQL execution              | You need to change the concurrent execution limit. `null` means unlimited.  |
| `Sockets.ConnectionInitializationTimeout` | `10` seconds                 | WebSockets                     | You need to change how long the server waits for connection initialization. |
| `Sockets.KeepAliveInterval`               | `5` seconds                  | WebSockets                     | You need to change the keep-alive interval.                                 |
| `Tool.Enable`                             | Enabled by Nitro options     | Nitro                          | You want to disable or conditionally expose the browser IDE.                |
| `Tool.GraphQLEndpoint`                    | Depends on the mapping       | Nitro                          | Nitro must call a different browser-visible GraphQL URL.                    |

See [Options](/docs/hotchocolate/v16/build/server-configuration/schema-options) for a broader reference on server options. Review [Batching](/docs/hotchocolate/v16/build/performance/batching) before enabling batching in production.

# Troubleshooting

## `/graphql` Returns 404

Ensure the app calls `app.MapGraphQL()` or the intended split endpoint. Confirm that clients use the same path you mapped. If a reverse proxy is present, verify path base and rewrite configuration.

## Nitro Appears in Production

Check `Tool.Enable` in `ModifyServerOptions(...)` and endpoint `WithOptions(...)`. Also verify that a standalone `MapNitroApp()` is not mapped without an environment condition or endpoint policy.

## Nitro Cannot Reach GraphQL

Check `GraphQLEndpoint` or `relativeRequestPath`. For `/graphql/ui` serving `/graphql`, use `..`. If behind a proxy, ensure Nitro points to the browser-visible GraphQL URL or a correct relative URL.

## WebSocket Subscriptions Fail

Register `app.UseWebSockets()` before mapping the combined endpoint or `MapGraphQLWebSocket()`. Confirm the client connects to `/graphql` for the combined endpoint or `/graphql/ws` for the split default.

## SSE Does Not Have a Route

SSE is negotiated on the HTTP endpoint. There is no `MapGraphQLSse()`. See the HTTP transport documentation for headers and response behavior.

## `?sdl` or `/graphql/sdl` Is Still Accessible

Check `EnableSchemaRequests`, `EnableSchemaFileSupport`, and explicit `MapGraphQLSchema()` or `MapGraphQLSemanticNonNullSchema()` calls. Introspection settings do not automatically remove SDL endpoints.

## GET Requests or GET Mutations Are Rejected

Check `EnableGetRequests`, `AllowedGetOperations`, and `EnforceGetRequestsPreflightHeader`. GET mutations require explicit opt-in and are rarely needed.

## Multipart Uploads Return a Preflight or Header Error

Check `EnableMultipartRequests` and `EnforceMultipartRequestsPreflightHeader`. When preflight enforcement is enabled, send `GraphQL-Preflight` with multipart upload requests.

## Authorization Locks Out Nitro or SDL

`RequireAuthorization()` on `MapGraphQL()` protects the combined endpoint. Split `MapGraphQLHttp().RequireAuthorization()` from `MapNitroApp()` or SDL endpoints if execution, IDE access, and schema downloads require different policies.

## The Wrong Schema Is Served

Pass `schemaName` explicitly to every mapped GraphQL endpoint in multi-schema apps. Confirm that schema-level options are applied to the intended named builder.

# Next Steps

- See [HTTP Transport](/docs/hotchocolate/v16/build/server-configuration/http-transport) for request formats, headers, SSE, and status codes.
- Review [Files](/docs/hotchocolate/v16/_leagcy/server/files) for multipart upload setup.
- Explore [Interceptors](/docs/hotchocolate/v16/build/server-configuration/interceptors) for request and socket interception.
- Read [Authorization](/docs/hotchocolate/v16/build/security/authorization) and [Authorize attribute](/docs/hotchocolate/v16/build/attributes/authorize) to combine endpoint and schema authorization.
- Consult [Trusted Documents](/docs/hotchocolate/v16/build/security/trusted-documents) before exposing persisted-operation-only endpoints.
