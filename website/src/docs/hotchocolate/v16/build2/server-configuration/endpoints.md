---
title: Endpoints
---

A Hot Chocolate schema becomes available to clients when you map it into the ASP.NET Core route table. Configure the schema and request executor with `builder.AddGraphQL()`, then expose it with `app.MapGraphQL()` or one of the specialized endpoint mapping methods.

Use this page when you need to choose the URL, split transports across paths, apply endpoint policies, or troubleshoot why a client cannot reach your GraphQL server.

# Expose GraphQL at `/graphql`

Start with the combined endpoint. It is the default for most applications.

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

`MapGraphQL()` maps Hot Chocolate at `/graphql` when you do not pass a path.

Run this operation against `POST /graphql`:

```graphql
query {
  hello
}
```

Expected result shape:

```json
{
  "data": {
    "hello": "world"
  }
}
```

Expected SDL:

```graphql
type Query {
  hello: String!
}
```

The combined endpoint handles these request surfaces below the mapped path:

| Surface      | Default behavior                                                                                                             |
| ------------ | ---------------------------------------------------------------------------------------------------------------------------- |
| GraphQL HTTP | `GET`, `POST`, and multipart requests use `/graphql`.                                                                        |
| WebSockets   | Subscription clients connect to `/graphql` after `app.UseWebSockets()` is registered.                                        |
| SDL download | `GET /graphql?sdl` returns the schema when schema requests and schema file support are enabled.                              |
| Nitro        | Browser navigation to `/graphql` opens Nitro when the tool is enabled. Older docs may call this browser IDE Banana Cake Pop. |
| Fallback     | Requests below the endpoint that do not match a GraphQL surface return `404`.                                                |

# Choose the endpoint path

Pass a path when clients should not use `/graphql`.

```csharp
app.MapGraphQL("/api/graphql");
```

Choose a stable path early. Clients, Nitro documents, gateway configuration, reverse proxy rewrites, CORS rules, authorization policies, persisted operation URLs, and telemetry may all depend on it.

When you change the route, update every component that knows the old URL:

- Client base URLs.
- Nitro `GraphQLEndpoint` settings.
- Reverse proxy rewrite rules and path base settings.
- CORS origins, methods, and headers.
- Gateway or router configuration.
- Monitoring dashboards and synthetic checks.

# Use the combined endpoint by default

The combined endpoint keeps the common development and production layout compact:

```csharp
app.UseWebSockets();

app.MapGraphQL("/graphql");
```

Register `UseWebSockets()` before endpoint mapping when subscriptions use WebSockets. Without it, HTTP requests can still work while WebSocket upgrade requests fail.

Server-Sent Events do not have a separate endpoint mapping method. SSE is negotiated by the HTTP transport on the GraphQL HTTP endpoint. Keep payload formats, status codes, content negotiation, SSE behavior, WebSocket subprotocols, and interceptor examples in the transport and interceptor pages.

Use split endpoints when different surfaces need different paths, policies, or exposure rules.

# Split endpoint responsibilities

## Map only GraphQL HTTP requests

Use `MapGraphQLHttp()` when you want an execution endpoint for HTTP traffic without Nitro, SDL download, or a standalone WebSocket endpoint.

```csharp
app.MapGraphQLHttp("/graphql");
```

This endpoint handles `POST`, `GET`, and multipart requests according to `GraphQLServerOptions`. It does not map Nitro. It also does not serve schema download requests by itself, so `Tool` and `EnableSchemaRequests` are not relevant to a standalone HTTP mapping.

Read [HTTP Transport](/docs/hotchocolate/v16/server/http-transport) for request bodies, headers, status codes, content negotiation, and SSE details.

## Map WebSocket subscriptions separately

Use `MapGraphQLWebSocket()` when WebSocket traffic needs a separate path or policy.

```csharp
app.UseWebSockets();

app.MapGraphQLWebSocket("/graphql/ws");
```

The default WebSocket path is `/graphql/ws`. Configure protocol timing with `GraphQLSocketOptions`:

```csharp
app
    .MapGraphQLWebSocket("/graphql/ws")
    .WithOptions(options =>
    {
        options.ConnectionInitializationTimeout = TimeSpan.FromSeconds(10);
        options.KeepAliveInterval = TimeSpan.FromSeconds(5);
    });
```

Keep connection initialization payloads, subprotocols, and keep-alive protocol details in the WebSocket transport page.

## Map Nitro separately

Use a standalone Nitro endpoint when the browser IDE needs a different path or policy than execution.

```csharp
app.MapGraphQLHttp("/graphql");
app.MapNitroApp("/graphql/ui", relativeRequestPath: "..");
```

The default standalone Nitro path is `/graphql/ui`. The `relativeRequestPath` value tells Nitro which GraphQL endpoint to use when it creates documents. For the `/graphql/ui` to `/graphql` layout, use `..`.

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

## Map SDL endpoints

`MapGraphQL()` supports `GET /graphql?sdl` when schema requests and schema file support are enabled. You can also expose SDL through dedicated endpoints.

```csharp
app.MapGraphQLSchema("/graphql/sdl");
app.MapGraphQLSemanticNonNullSchema("/graphql/semantic-non-null-schema.graphql");
```

`MapGraphQLSchema()` defaults to `/graphql/sdl`. `MapGraphQLSemanticNonNullSchema()` defaults to `/graphql/semantic-non-null-schema.graphql` and exists for semantic non-null SDL scenarios. Use the [Command Line](/docs/hotchocolate/v16/server/command-line) page for schema export workflows.

SDL endpoint exposure and GraphQL introspection are different controls. Use [Introspection](/docs/hotchocolate/v16/securing-your-api/introspection) when you need to control introspection behavior.

## Map persisted operation endpoints

Use `MapGraphQLPersistedOperations()` to expose registered operations through REST-like URLs.

```csharp
app.MapGraphQLPersistedOperations(
    "/graphql/persisted",
    requireOperationName: true);
```

The default path is `/graphql/persisted`. This maps the endpoint surface only. Store, publish, and validate trusted documents with the [Trusted Documents](/docs/hotchocolate/v16/performance/trusted-documents) and [Private API](/docs/hotchocolate/v16/guides/private-api) guidance.

# Configure endpoint behavior

## Set schema-level defaults

Use `ModifyServerOptions(...)` on the GraphQL builder when the same behavior should apply everywhere that schema is mapped.

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

Schema-level defaults belong with the request executor. Use them for environment-wide decisions such as GET support, multipart support, SDL downloads, Nitro exposure, batching, concurrency, and WebSocket timing.

## Override one mapped endpoint

Use `WithOptions(...)` on a mapping when one endpoint needs different behavior.

```csharp
app
    .MapGraphQL("/graphql")
    .WithOptions(options => options.EnableGetRequests = false);
```

Endpoint overrides are applied on top of schema-level defaults.

| Mapping                 | Options hook                                                                                     |
| ----------------------- | ------------------------------------------------------------------------------------------------ |
| `MapGraphQL()`          | `WithOptions(Action<GraphQLServerOptions>)` and Nitro options through `options.Tool`.            |
| `MapGraphQLHttp()`      | `WithOptions(Action<GraphQLServerOptions>)`, except Nitro and SDL download options do not apply. |
| `MapGraphQLWebSocket()` | `WithOptions(Action<GraphQLSocketOptions>)`.                                                     |
| `MapNitroApp()`         | `WithOptions(Action<NitroAppOptions>)`.                                                          |

## Harden development-only surfaces

Expose Nitro and SDL downloads intentionally. A common production setup enables them in development only.

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

If you map standalone SDL or Nitro endpoints, secure or conditionally map those endpoints too.

```csharp
if (app.Environment.IsDevelopment())
{
    app.MapNitroApp("/graphql/ui");
    app.MapGraphQLSchema("/graphql/sdl");
}

app.MapGraphQLHttp("/graphql");
```

## Restrict HTTP methods and operation types

GET execution is enabled by default, but only queries are allowed over GET by default. Disable GET execution or keep it query-only for production APIs that do not need GET.

```csharp
builder
    .AddGraphQL()
    .ModifyServerOptions(options =>
    {
        options.EnableGetRequests = false;
        options.AllowedGetOperations = AllowedGetOperations.Query;
    });
```

If you enable `EnforceGetRequestsPreflightHeader`, clients must send the required GraphQL preflight header. Keep detailed CSRF and HTTP header behavior in the HTTP transport page.

## Handle uploads deliberately

Multipart upload requests are enabled by default. Multipart preflight header enforcement is also enabled by default.

```csharp
builder
    .AddGraphQL()
    .ModifyServerOptions(options =>
    {
        options.EnableMultipartRequests = true;
        options.EnforceMultipartRequestsPreflightHeader = true;
    });
```

When enforcement is on, multipart clients must send `GraphQL-Preflight`. Keep upload scalar usage, multipart body shape, Kestrel limits, IIS limits, and `FormOptions` in [Files](/docs/hotchocolate/v16/server/files).

# Apply ASP.NET Core endpoint policies

Endpoint policies protect the ASP.NET Core route. Schema authorization protects fields and types inside GraphQL execution. Use both when you need both boundaries.

```csharp
app.UseAuthentication();
app.UseAuthorization();

app.MapGraphQL("/graphql").RequireAuthorization();
```

`RequireAuthorization()` on the combined endpoint applies to the combined route surface, including GraphQL HTTP, WebSockets, Nitro, and SDL requests below that route. Split endpoints when the policies differ.

```csharp
app.UseAuthentication();
app.UseAuthorization();

app
    .MapGraphQLHttp("/graphql")
    .RequireAuthorization();

app.MapNitroApp("/graphql/ui");
```

You can also apply other ASP.NET Core endpoint policies such as rate limiting.

```csharp
app
    .MapGraphQL("/graphql")
    .RequireRateLimiting("graphql");
```

Register surrounding middleware in ASP.NET Core order before mapped endpoints. A typical secured GraphQL app uses this order:

```csharp
app.UseForwardedHeaders();
app.UseRouting();
app.UseCors("graphql");
app.UseAuthentication();
app.UseAuthorization();
app.UseWebSockets();

app.MapGraphQL("/graphql");
```

Minimal hosting does not require `UseRouting()` for basic endpoint mapping, but apps that use explicit routing middleware can keep this order.

Use `[Authorize]` from `HotChocolate.Authorization` and `.AddAuthorization()` on the GraphQL builder when you need field or type authorization inside the schema. Read [Authorize attribute](/docs/hotchocolate/v16/build2/attributes/authorize) for schema authorization.

# Host multiple schemas

Pass the schema name when one app hosts more than one request executor. Do not rely on implicit schema resolution in multi-schema applications.

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

Use named schemas for public and admin APIs, bounded contexts, side-by-side migrations, and test hosts. All GraphQL execution mapping methods accept an optional `schemaName`. `MapNitroApp()` is Nitro-only and points at a GraphQL endpoint URL instead of owning a schema.

Options are named too. Configure defaults on the builder for the schema you intend to map.

# Keep paths stable behind reverse proxies

When ASP.NET Core runs behind a reverse proxy or under a path base, the URL clients see may differ from the route your app maps. Keep the external URL, forwarded headers, path base, proxy rewrite rules, and Nitro endpoint configuration aligned.

For example, if a proxy exposes `/api/graphql` and forwards to an app that maps `/graphql`, configure the proxy rewrite and client URL together. If Nitro is served behind the same prefix, set `GraphQLEndpoint` to the browser-visible GraphQL URL or a correct relative path.

# Consider persisted-operation-only production

Some private APIs expose only trusted documents in production.

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

Use this pattern only after clients can publish and call persisted operations. Keep the normal GraphQL endpoint until the trusted document workflow is verified.

# Endpoint reference

| API                                                                                                           | Default path                                | Purpose                                                            | `schemaName` | Options                                                        |
| ------------------------------------------------------------------------------------------------------------- | ------------------------------------------- | ------------------------------------------------------------------ | ------------ | -------------------------------------------------------------- |
| `MapGraphQL(path = "/graphql", schemaName = null)`                                                            | `/graphql`                                  | Combined HTTP, multipart, GET, WebSocket, SDL, and Nitro endpoint. | Yes          | `GraphQLServerOptions`                                         |
| `MapGraphQLHttp(pattern = "/graphql", schemaName = null)`                                                     | `/graphql`                                  | HTTP `POST`, `GET`, and multipart execution.                       | Yes          | `GraphQLServerOptions` without Nitro and SDL download behavior |
| `MapGraphQLWebSocket(pattern = "/graphql/ws", schemaName = null)`                                             | `/graphql/ws`                               | WebSocket subscriptions.                                           | Yes          | `GraphQLSocketOptions`                                         |
| `MapNitroApp(toolPath = "/graphql/ui", relativeRequestPath = "..")`                                           | `/graphql/ui`                               | Nitro browser IDE.                                                 | No           | `NitroAppOptions`                                              |
| `MapGraphQLSchema(pattern = "/graphql/sdl", schemaName = null)`                                               | `/graphql/sdl`                              | SDL download endpoint.                                             | Yes          | Endpoint policies                                              |
| `MapGraphQLSemanticNonNullSchema(pattern = "/graphql/semantic-non-null-schema.graphql", schemaName = null)`   | `/graphql/semantic-non-null-schema.graphql` | Semantic non-null SDL endpoint.                                    | Yes          | Endpoint policies                                              |
| `MapGraphQLPersistedOperations(path = "/graphql/persisted", schemaName = null, requireOperationName = false)` | `/graphql/persisted`                        | Persisted operation execution by URL.                              | Yes          | Endpoint policies                                              |

# Endpoint option reference

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

Use [Options](/docs/hotchocolate/v16/api-reference/options) for broader server option reference. Use [Batching](/docs/hotchocolate/v16/server/batching) before enabling batching in production.

# Troubleshooting

## `/graphql` returns 404

Check that the app calls `app.MapGraphQL()` or the split endpoint you intend to use. Confirm that clients use the same path that you mapped. If a reverse proxy is involved, verify path base and rewrite behavior.

## Nitro appears in production

Check `Tool.Enable` in `ModifyServerOptions(...)` and endpoint `WithOptions(...)`. Also check whether a standalone `MapNitroApp()` is still mapped without an environment condition or endpoint policy.

## Nitro cannot reach GraphQL

Check `GraphQLEndpoint` or `relativeRequestPath`. For `/graphql/ui` serving `/graphql`, use `..`. Behind a proxy, make Nitro point at the browser-visible GraphQL URL or a correct relative URL.

## WebSocket subscriptions fail

Register `app.UseWebSockets()` before mapping the combined endpoint or `MapGraphQLWebSocket()`. Confirm that the client connects to `/graphql` for the combined endpoint or `/graphql/ws` for the split default.

## SSE does not have a route

SSE is negotiated on the HTTP endpoint. Do not look for `MapGraphQLSse()`. Read the HTTP transport page for headers and response behavior.

## `?sdl` or `/graphql/sdl` is still accessible

Check `EnableSchemaRequests`, `EnableSchemaFileSupport`, and explicit `MapGraphQLSchema()` or `MapGraphQLSemanticNonNullSchema()` calls. Introspection settings do not automatically remove SDL endpoints.

## GET requests or GET mutations are rejected

Check `EnableGetRequests`, `AllowedGetOperations`, and `EnforceGetRequestsPreflightHeader`. GET mutations require explicit opt-in and should be rare.

## Multipart uploads return a preflight or header error

Check `EnableMultipartRequests` and `EnforceMultipartRequestsPreflightHeader`. When preflight enforcement is enabled, send `GraphQL-Preflight` with multipart upload requests.

## Authorization locks out Nitro or SDL

`RequireAuthorization()` on `MapGraphQL()` protects the combined endpoint. Split `MapGraphQLHttp().RequireAuthorization()` from `MapNitroApp()` or SDL endpoints when execution, IDE access, and schema downloads need different policies.

## The wrong schema is served

Pass `schemaName` explicitly to every mapped GraphQL endpoint in multi-schema apps. Confirm that schema-level options were applied to the intended named builder.

# Next steps

- Read [HTTP Transport](/docs/hotchocolate/v16/server/http-transport) for request formats, headers, SSE, and status codes.
- Read [Files](/docs/hotchocolate/v16/server/files) for multipart upload setup.
- Read [Interceptors](/docs/hotchocolate/v16/server/interceptors) for request and socket interception.
- Read [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization) and [Authorize attribute](/docs/hotchocolate/v16/build2/attributes/authorize) to combine endpoint and schema authorization.
- Read [Trusted Documents](/docs/hotchocolate/v16/performance/trusted-documents) before exposing persisted-operation-only endpoints.
