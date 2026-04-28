---
title: Endpoints
---

Hot Chocolate provides a set of ASP.NET Core middleware for making the GraphQL server available via HTTP and WebSockets. There are also middleware for hosting the [Nitro](/products/nitro) GraphQL IDE and an endpoint for downloading the schema in its SDL representation.

# MapGraphQL

Call `MapGraphQL()` on the `IEndpointRouteBuilder` to register all of the middleware a standard GraphQL server requires.

```csharp
app.UseRouting();

app.UseEndpoints(endpoints =>
{
    endpoints.MapGraphQL();
});
```

With .NET 6+ Minimal APIs, you can call `MapGraphQL()` on the `app` builder directly since it implements `IEndpointRouteBuilder`:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Omitted code for brevity

var app = builder.Build();

app.MapGraphQL();

app.Run();
```

The middleware registered by `MapGraphQL` makes the GraphQL server available at `/graphql` by default.

You can customize the endpoint:

```csharp
endpoints.MapGraphQL("/my/graphql/endpoint");
```

Calling `MapGraphQL()` enables the following functionality on the specified endpoint:

- HTTP GET and HTTP POST GraphQL requests are handled (Multipart included)
- WebSocket GraphQL requests are handled (if the ASP.NET Core WebSocket middleware has been registered)
- Including the query string `?sdl` after the endpoint downloads the GraphQL schema
- Accessing the endpoint from a browser loads the [Nitro](/products/nitro) GraphQL IDE

You can customize the combined middleware using `GraphQLServerOptions` as shown below, or include only the parts of the middleware you need and configure them individually.

The following middleware are available:

- [MapNitroApp](#mapnitroapp)
- [MapGraphQLHttp](#mapgraphqlhttp)
- [MapGraphQLWebsocket](#mapgraphqlwebsocket)
- [MapGraphQLSchema](#mapgraphqlschema)
- [MapGraphQLPersistedOperations](#mapgraphqlpersistedoperations)

## GraphQLServerOptions

You can influence the behavior of the middleware registered by `MapGraphQL` using `GraphQLServerOptions`.

### EnableSchemaRequests

```csharp
endpoints.MapGraphQL().WithOptions((GraphQLServerOptions option) =>
    {
        option.EnableSchemaRequests = false;
    });
```

This setting controls whether the schema of the GraphQL server can be downloaded by appending `?sdl` to the endpoint.

### EnableGetRequests

```csharp
endpoints.MapGraphQL().WithOptions((GraphQLServerOptions option) =>
    {
        option.EnableGetRequests = false;
    });
```

This setting controls whether the GraphQL server handles GraphQL operations sent via the query string in an HTTP GET request.

### AllowedGetOperations

```csharp
endpoints.MapGraphQL().WithOptions((GraphQLServerOptions option) =>
    {
        option.AllowedGetOperations = AllowedGetOperations.Query;
    });
```

If [EnableGetRequests](#enablegetrequests) is `true`, you can control the allowed operations for HTTP GET requests using the `AllowedGetOperations` setting.

By default, only queries are accepted via HTTP GET. You can also allow mutations by setting `AllowedGetOperations` to `AllowedGetOperations.QueryAndMutation`.

### EnableMultipartRequests

```csharp
endpoints.MapGraphQL().WithOptions((GraphQLServerOptions option) =>
    {
        option.EnableMultipartRequests = false;
    });
```

This setting controls whether the GraphQL server handles HTTP multipart forms (file uploads).

[Learn more about uploading files](/docs/hotchocolate/v16/server/files#upload-scalar)

### Tool

You can specify options for Nitro using the `Tool` property.

For example, you could enable Nitro only during development:

```csharp
app.UseRouting();

app.UseEndpoints(endpoints =>
{
    endpoints.MapGraphQL().WithOptions((NitroAppOptions option) =>
    {
        option.Enable = env.IsDevelopment();
    });
});
```

[Learn more about possible NitroAppOptions](#nitroappoptions)

# MapNitroApp

Call `MapNitroApp()` on the `IEndpointRouteBuilder` to serve [Nitro](/products/nitro) on a different endpoint than the actual GraphQL endpoint.

```csharp
app.UseRouting();

app.UseEndpoints(endpoints =>
{
    endpoints.MapNitroApp("/graphql/ui");
});
```

This makes Nitro accessible via a web browser at the `/graphql/ui` endpoint.

## NitroAppOptions

You can configure Nitro using `NitroAppOptions`.

### Enable

```csharp
endpoints.MapNitroApp("/ui").WithOptions((NitroAppOptions option) =>
    {
        option.Enable = false;
    });
```

This setting controls whether Nitro is served.

### GraphQLEndpoint

```csharp
endpoints.MapNitroApp("/ui").WithOptions((NitroAppOptions option) =>
    {
        option.GraphQLEndpoint = "/my/graphql/endpoint";
    });
```

This setting sets the GraphQL endpoint to use when creating new documents within Nitro.

### UseBrowserUrlAsGraphQLEndpoint

```csharp
endpoints.MapNitroApp("/ui").WithOptions((NitroAppOptions option) =>
    {
        option.UseBrowserUrlAsGraphQLEndpoint = true;
    });
```

If set to `true`, the current browser URL is treated as the GraphQL endpoint when creating new documents within Nitro.

> Warning: [GraphQLEndpoint](#graphqlendpoint) takes precedence over this setting.

### Document

```csharp
endpoints.MapNitroApp("/ui").WithOptions((NitroAppOptions option) =>
    {
        option.Document = "{ __typename }";
    });
```

This setting lets you set a default GraphQL document that serves as a placeholder for each new document created using Nitro.

### UseGet

```csharp
endpoints.MapNitroApp("/ui").WithOptions((NitroAppOptions option) =>
    {
        option.UseGet = true;
    });
```

This setting controls the default HTTP method used to execute GraphQL operations when creating new documents within Nitro. When set to `true`, HTTP GET is used instead of the default HTTP POST.

### HttpHeaders

```csharp
endpoints.MapNitroApp("/ui").WithOptions((NitroAppOptions option) =>
{
    option.HttpHeaders = new HeaderDictionary
    {
        { "Content-Type", "application/json" }
    };
});
```

This setting lets you specify default HTTP headers that are added to each new document created using Nitro.

### IncludeCookies

```csharp
endpoints.MapNitroApp("/ui").WithOptions((NitroAppOptions option) =>
    {
        option.IncludeCookies = true;
    });
```

This setting specifies the default for including cookies in cross-origin requests when creating new documents within Nitro.

### Title

```csharp
endpoints.MapNitroApp("/ui").WithOptions((NitroAppOptions option) =>
    {
        option.Title = "My GraphQL explorer";
    });
```

This setting controls the tab name when Nitro is opened inside a web browser.

### DisableTelemetry

```csharp
endpoints.MapNitroApp("/ui").WithOptions((NitroAppOptions option) =>
    {
        option.DisableTelemetry = true;
    });
```

This setting lets you disable telemetry events.

### GaTrackingId

```csharp
endpoints.MapNitroApp("/ui").WithOptions((NitroAppOptions option) =>
    {
        option.GaTrackingId = "google-analytics-id";
    });
```

This setting lets you set a custom Google Analytics ID, which allows you to gain insights into the usage of Nitro hosted as part of your GraphQL server.

The following information is collected:

| Name                 | Description                                                           |
| -------------------- | --------------------------------------------------------------------- |
| `deviceId`           | Random string generated on a per-device basis                         |
| `operatingSystem`    | Name of the operating system: `Windows`, `macOS`, `Linux` & `Unknown` |
| `userAgent`          | `User-Agent` header                                                   |
| `applicationType`    | The type of application: `app` (Electron) or `middleware`             |
| `applicationVersion` | Version of Nitro                                                      |

# MapGraphQLHttp

Call `MapGraphQLHttp()` on the `IEndpointRouteBuilder` to make your GraphQL server available via HTTP at a specific endpoint.

```csharp
app.UseRouting();

app.UseEndpoints(endpoints =>
{
    endpoints.MapGraphQLHttp("/graphql/http");
});
```

With the above configuration, you can issue HTTP GET/POST requests against the `/graphql/http` endpoint.

## GraphQLServerOptions

The HTTP endpoint can also be configured with per-endpoint overrides using `WithOptions`:

```csharp
endpoints.MapGraphQLHttp("/graphql/http").WithOptions(o =>
    {
        option.EnableGetRequests = false;
    });
```

The same `GraphQLServerOptions` properties available on `MapGraphQL` can be overridden here, except for `Tool` and `EnableSchemaRequests` which are not applicable to standalone HTTP endpoints.

[Learn more about GraphQLServerOptions](#graphqlserveroptions)

# MapGraphQLWebsocket

Call `MapGraphQLWebSocket()` on the `IEndpointRouteBuilder` to make your GraphQL server available via WebSockets at a specific endpoint.

```csharp
app.UseRouting();

app.UseEndpoints(endpoints =>
{
    endpoints.MapGraphQLWebSocket("/graphql/ws");
});
```

With the above configuration, you can issue GraphQL subscription requests via WebSocket against the `/graphql/ws` endpoint.

# MapGraphQLSchema

Call `MapGraphQLSchema()` on the `IEndpointRouteBuilder` to make your GraphQL schema available at a specific endpoint.

```csharp
app.UseRouting();

app.UseEndpoints(endpoints =>
{
    endpoints.MapGraphQLSchema("/graphql/schema");
});
```

With the above configuration, you can download your `schema.graphql` file from the `/graphql/schema` endpoint.

# MapGraphQLPersistedOperations

Call `MapGraphQLPersistedOperations()` on the `IEndpointRouteBuilder` to expose persisted operations via REST-like URLs. This enables clients to execute pre-registered GraphQL operations using a simple URL pattern instead of sending a full GraphQL request body.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddPersistedOperations(); // Register a persisted operation storage provider

var app = builder.Build();

app.MapGraphQL();
app.MapGraphQLPersistedOperations();

app.Run();
```

The default path is `/graphql/persisted`. The endpoint supports two URL patterns:

| Pattern                          | Example                             | Description                                                    |
| -------------------------------- | ----------------------------------- | -------------------------------------------------------------- |
| `/{operationId}`                 | `/graphql/persisted/abc123`         | Execute a persisted operation by its ID                        |
| `/{operationId}/{operationName}` | `/graphql/persisted/abc123/GetUser` | Execute a specific named operation within a persisted document |

Both GET and POST requests are supported. With POST requests, you can pass variables and extensions in the request body.

## Custom Path

You can customize the path:

```csharp
app.MapGraphQLPersistedOperations("/api/operations");
```

## Requiring an Operation Name

If you want to enforce that clients always specify an operation name in the URL, set `requireOperationName` to `true`:

```csharp
app.MapGraphQLPersistedOperations(requireOperationName: true);
```

When enabled, requests to `/{operationId}` without an operation name return a `400 Bad Request` response.

For details on storing and managing persisted operations, see [Trusted Documents](/docs/hotchocolate/v16/securing-your-api/trusted-documents).

# AddGraphQL Parameters

The `AddGraphQL()` method on `WebApplicationBuilder` accepts parameters that control request parsing and default security behavior.

```csharp
builder.AddGraphQL(
    maxAllowedRequestSize: 20 * 1000 * 1024,  // ~20 MB (default)
    disableDefaultSecurity: false);             // default
```

## maxAllowedRequestSize

Controls the maximum allowed size (in bytes) of an incoming GraphQL request body. The default is `20 * 1000 * 1024` (approximately 20 MB). If a request exceeds this limit, it is rejected before parsing.

Reduce this value if you expect only small queries and want to protect against excessively large payloads:

```csharp
builder.AddGraphQL(
    maxAllowedRequestSize: 1 * 1000 * 1024); // ~1 MB
```

## disableDefaultSecurity

When `false` (the default), `AddGraphQL()` automatically enables these security features:

- **Cost analysis**: Protects against expensive queries by analyzing the computational cost of each operation.
- **Introspection disabled in production**: Introspection is automatically turned off when `IHostEnvironment.IsDevelopment()` returns `false`.
- **MaxAllowedFieldCycleDepthRule**: Prevents deeply cyclic field selections in production.

If you need full control over which security features are enabled, set `disableDefaultSecurity` to `true` and configure each feature individually:

```csharp
builder
    .AddGraphQL(disableDefaultSecurity: true)
    .AddCostAnalyzer(); // Opt in to specific features manually
```

> Warning: Disabling default security removes important protections. Only do this if you are configuring equivalent protections manually.

# GraphQLServerOptions Reference

The full set of properties available on `GraphQLServerOptions` is listed below. You can set these via `ModifyServerOptions` (schema-level) or `WithOptions` (per-endpoint).

| Property                                  | Type                   | Default       | Description                                                                                                                     |
| ----------------------------------------- | ---------------------- | ------------- | ------------------------------------------------------------------------------------------------------------------------------- |
| `EnableGetRequests`                       | `bool`                 | `true`        | Controls whether HTTP GET requests are accepted.                                                                                |
| `AllowedGetOperations`                    | `AllowedGetOperations` | `Query`       | Which operation types are allowed via HTTP GET. Values: `None`, `Query`, `Mutation`, `Subscription`, `QueryAndMutation`, `All`. |
| `EnableMultipartRequests`                 | `bool`                 | `true`        | Controls whether multipart form requests (file uploads) are accepted.                                                           |
| `EnableSchemaRequests`                    | `bool`                 | `true`        | Controls whether the schema SDL can be downloaded via `?sdl`.                                                                   |
| `EnableSchemaFileSupport`                 | `bool`                 | `true`        | Controls whether the schema SDL is served as a downloadable file.                                                               |
| `EnforceGetRequestsPreflightHeader`       | `bool`                 | `false`       | When `true`, GET requests must include a CSRF preflight header.                                                                 |
| `EnforceMultipartRequestsPreflightHeader` | `bool`                 | `true`        | When `true`, multipart requests must include a CSRF preflight header.                                                           |
| `Batching`                                | `AllowedBatching`      | `None`        | Which batching modes are allowed.                                                                                               |
| `MaxBatchSize`                            | `int`                  | `1024`        | Maximum number of operations in a single batch. `0` means unlimited.                                                            |
| `Sockets`                                 | `GraphQLSocketOptions` | _(see below)_ | WebSocket-specific options.                                                                                                     |
| `Tool`                                    | `NitroAppOptions`      | _(see below)_ | Nitro IDE options.                                                                                                              |

The `Sockets` property contains a `GraphQLSocketOptions` object with these properties:

| Property                          | Type        | Default                    | Description                                                              |
| --------------------------------- | ----------- | -------------------------- | ------------------------------------------------------------------------ |
| `ConnectionInitializationTimeout` | `TimeSpan`  | `TimeSpan.FromSeconds(10)` | Time the client has to send `connection_init` after opening a WebSocket. |
| `KeepAliveInterval`               | `TimeSpan?` | `TimeSpan.FromSeconds(5)`  | Interval for server keep-alive pings. `null` disables keep-alive.        |

# Per-Endpoint Configuration with WithOptions

Hot Chocolate uses a delegate-based `WithOptions` pattern to configure options per-endpoint. The delegate receives the options object, and you modify it in place. These overrides are applied on top of the schema-level defaults set via `ModifyServerOptions`.

## MapGraphQL

```csharp
app.MapGraphQL()
    .WithOptions((GraphQLServerOptions option) =>
    {
        option.EnableGetRequests = false;
        option.AllowedGetOperations = AllowedGetOperations.Query;
    })
    .WithOptions((NitroAppOptions option) =>
    {
        option.Enable = false;
    });
```

## MapGraphQLHttp

```csharp
app.MapGraphQLHttp("/graphql/http").WithOptions(o =>
{
    o.EnableMultipartRequests = false;
    o.EnforceGetRequestsPreflightHeader = true;
});
```

## MapGraphQLWebSocket

The WebSocket endpoint accepts a delegate over `GraphQLSocketOptions` directly:

```csharp
app.MapGraphQLWebSocket("/graphql/ws").WithOptions(o =>
{
    o.ConnectionInitializationTimeout = TimeSpan.FromSeconds(30);
    o.KeepAliveInterval = TimeSpan.FromSeconds(12);
});
```

## Schema-Level Defaults

To set defaults that apply to all endpoints, use `ModifyServerOptions` on the request executor builder:

```csharp
builder
    .AddGraphQL()
    .ModifyServerOptions(o =>
    {
        o.EnableGetRequests = false;
        o.Sockets.KeepAliveInterval = TimeSpan.FromSeconds(15);
        o.Tool.Enable = false;
    });
```

Per-endpoint `WithOptions` overrides take precedence over schema-level defaults.

# Next Steps

- [HTTP Transport](/docs/hotchocolate/v16/server/http-transport) for details on request formats, response formats, WebSocket transport, and SSE.
- [Interceptors](/docs/hotchocolate/v16/server/interceptors) for hooking into request processing.
- [Trusted Documents](/docs/hotchocolate/v16/securing-your-api/trusted-documents) for the full persisted operations workflow.
- [Cost Analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis) for understanding the default security cost analyzer.
