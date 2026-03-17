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

## GraphQLServerOptions

You can influence the behavior of the middleware registered by `MapGraphQL` using `GraphQLServerOptions`.

### EnableSchemaRequests

```csharp
endpoints.MapGraphQL().WithOptions(o => o.EnableSchemaRequests = false);
```

This setting controls whether the schema of the GraphQL server can be downloaded by appending `?sdl` to the endpoint.

### EnableGetRequests

```csharp
endpoints.MapGraphQL().WithOptions(o => o.EnableGetRequests = false);
```

This setting controls whether the GraphQL server handles GraphQL operations sent via the query string in an HTTP GET request.

### AllowedGetOperations

```csharp
endpoints.MapGraphQL().WithOptions(o => o.AllowedGetOperations = AllowedGetOperations.Query);
```

If [EnableGetRequests](#enablegetrequests) is `true`, you can control the allowed operations for HTTP GET requests using the `AllowedGetOperations` setting.

By default, only queries are accepted via HTTP GET. You can also allow mutations by setting `AllowedGetOperations` to `AllowedGetOperations.QueryAndMutation`.

### EnableMultipartRequests

```csharp
endpoints.MapGraphQL().WithOptions(o => o.EnableMultipartRequests = false);
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
    endpoints.MapGraphQL().WithOptions(o => o.Tool.Enable = env.IsDevelopment());
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
endpoints.MapNitroApp("/ui").WithOptions(o => o.Enable = false);
```

This setting controls whether Nitro is served.

### GraphQLEndpoint

```csharp
endpoints.MapNitroApp("/ui").WithOptions(o => o.GraphQLEndpoint = "/my/graphql/endpoint");
```

This setting sets the GraphQL endpoint to use when creating new documents within Nitro.

### UseBrowserUrlAsGraphQLEndpoint

```csharp
endpoints.MapNitroApp("/ui").WithOptions(o => o.UseBrowserUrlAsGraphQLEndpoint = true);
```

If set to `true`, the current browser URL is treated as the GraphQL endpoint when creating new documents within Nitro.

> Warning: [GraphQLEndpoint](#graphqlendpoint) takes precedence over this setting.

### Document

```csharp
endpoints.MapNitroApp("/ui").WithOptions(o => o.Document = "{ __typename }");
```

This setting lets you set a default GraphQL document that serves as a placeholder for each new document created using Nitro.

### UseGet

```csharp
endpoints.MapNitroApp("/ui").WithOptions(o => o.UseGet = true);
```

This setting controls the default HTTP method used to execute GraphQL operations when creating new documents within Nitro. When set to `true`, HTTP GET is used instead of the default HTTP POST.

### HttpHeaders

```csharp
endpoints.MapNitroApp("/ui").WithOptions(o =>
{
    o.HttpHeaders = new HeaderDictionary
    {
        { "Content-Type", "application/json" }
    };
});
```

This setting lets you specify default HTTP headers that are added to each new document created using Nitro.

### IncludeCookies

```csharp
endpoints.MapNitroApp("/ui").WithOptions(o => o.IncludeCookies = true);
```

This setting specifies the default for including cookies in cross-origin requests when creating new documents within Nitro.

### Title

```csharp
endpoints.MapNitroApp("/ui").WithOptions(o => o.Title = "My GraphQL explorer");
```

This setting controls the tab name when Nitro is opened inside a web browser.

### DisableTelemetry

```csharp
endpoints.MapNitroApp("/ui").WithOptions(o => o.DisableTelemetry = true);
```

This setting lets you disable telemetry events.

### GaTrackingId

```csharp
endpoints.MapNitroApp("/ui").WithOptions(o => o.GaTrackingId = "google-analytics-id");
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
endpoints.MapGraphQLHttp("/graphql/http").WithOptions(o => o.EnableGetRequests = false);
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

# Troubleshooting

## Nitro not loading in the browser

Check that the `Tool.Enable` setting is not set to `false`. In production environments, Nitro is often disabled. Set `o.Tool.Enable = true` or conditionally enable it based on the hosting environment.

## WebSocket requests not working

Ensure that the ASP.NET Core WebSocket middleware is registered before calling `MapGraphQL()`. Add `app.UseWebSockets()` to your middleware pipeline.

# Next Steps

- [HTTP Transport](/docs/hotchocolate/v16/server/http-transport) for details on request and response formatting.
- [Interceptors](/docs/hotchocolate/v16/server/interceptors) for hooking into request processing.
- [Introspection](/docs/hotchocolate/v16/server/introspection) for controlling schema visibility.
