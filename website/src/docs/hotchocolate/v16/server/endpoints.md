---
title: Endpoints
---

Hot Chocolate comes with a set of ASP.NET Core middleware used for making the GraphQL server available via HTTP and WebSockets. There are also middleware for hosting our GraphQL IDE [Nitro](/products/nitro) as well as an endpoint used for downloading the schema in its SDL representation.

# MapGraphQL

We can call `MapGraphQL()` on the `IEndpointRouteBuilder` to register all of the middleware a standard GraphQL server requires.

```csharp
app.UseRouting();

app.UseEndpoints(endpoints =>
{
    endpoints.MapGraphQL();
});
```

If you are using .NET 6 Minimal APIs, you can also call `MapGraphQL()` on the `app` builder directly, since it implements `IEndpointRouteBuilder`:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Omitted code for brevity

var app = builder.Build();

app.MapGraphQL();

app.Run();
```

The middleware registered by `MapGraphQL` makes the GraphQL server available at `/graphql` per default.

We can customize the endpoint at which the GraphQL server is hosted like the following.

```csharp
endpoints.MapGraphQL("/my/graphql/endpoint");
```

Calling `MapGraphQL()` will enable the following functionality on the specified endpoint:

- HTTP GET and HTTP POST GraphQL requests are handled (Multipart included)
- WebSocket GraphQL requests are handled (if the ASP.NET Core WebSocket Middleware has been registered)
- Including the query string `?sdl` after the endpoint will download the GraphQL schema
- Accessing the endpoint from a browser will load our GraphQL IDE [Nitro](/products/nitro)

We can customize the combined middleware using `GraphQLServerOptions` as shown below or we can only include the parts of the middleware we need and configure them explicitly.

The following middleware are available:

- [MapNitroApp](#mapnitroapp)
- [MapGraphQLHttp](#mapgraphqlhttp)
- [MapGraphQLWebsocket](#mapgraphqlwebsocket)
- [MapGraphQLSchema](#mapgraphqlschema)

## GraphQLServerOptions

We can influence the behavior of the middleware registered by `MapGraphQL` using `GraphQLServerOptions`.

### EnableSchemaRequests

```csharp
endpoints.MapGraphQL().WithOptions(o => o.EnableSchemaRequests = false);
```

This setting controls whether the schema of the GraphQL server can be downloaded by appending `?sdl` to the endpoint.

### EnableGetRequests

```csharp
endpoints.MapGraphQL().WithOptions(o => o.EnableGetRequests = false);
```

This setting controls whether the GraphQL server is able to handle GraphQL operations sent via the query string in a HTTP GET request.

### AllowedGetOperations

```csharp
endpoints.MapGraphQL().WithOptions(o => o.AllowedGetOperations = AllowedGetOperations.Query);
```

If [EnableGetRequests](#enablegetrequests) is `true` we can control the allowed operations for HTTP GET requests using the `AllowedGetOperations` setting.

Per default only queries are accepted via HTTP GET. We can also allow mutations by setting `AllowedGetOperations` to `AllowedGetOperations.QueryAndMutation`.

### EnableMultipartRequests

```csharp
endpoints.MapGraphQL().WithOptions(o => o.EnableMultipartRequests = false);
```

This setting controls whether the GraphQL server is able to handle HTTP Multipart forms, i.e. file uploads.

[Learn more about uploading files](/docs/hotchocolate/v16/server/files#upload-scalar)

### Tool

We can specify options for Nitro using the `Tool` property.

We could for example only enable Nitro during development.

```csharp
app.UseRouting();

app.UseEndpoints(endpoints =>
{
    endpoints.MapGraphQL().WithOptions(o => o.Tool.Enable = env.IsDevelopment());
});
```

[Learn more about possible NitroAppOptions](#nitroappoptions)

# MapNitroApp

We can call `MapNitroApp()` on the `IEndpointRouteBuilder` to serve [Nitro](/products/nitro) on a different endpoint than the actual GraphQL endpoint.

```csharp
app.UseRouting();

app.UseEndpoints(endpoints =>
{
    endpoints.MapNitroApp("/graphql/ui");
});
```

This would make Nitro accessible via a Web Browser at the `/graphql/ui` endpoint.

## NitroAppOptions

We can configure Nitro using `NitroAppOptions`.

### Enable

```csharp
endpoints.MapNitroApp("/ui").WithOptions(o => o.Enable = false);
```

This setting controls whether Nitro should be served or not.

### GraphQLEndpoint

```csharp
endpoints.MapNitroApp("/ui").WithOptions(o => o.GraphQLEndpoint = "/my/graphql/endpoint");
```

This setting sets the GraphQL endpoint to use when creating new documents within Nitro.

### UseBrowserUrlAsGraphQLEndpoint

```csharp
endpoints.MapNitroApp("/ui").WithOptions(o => o.UseBrowserUrlAsGraphQLEndpoint = true);
```

If set to `true` the current Web Browser URL is treated as the GraphQL endpoint when creating new documents within Nitro.

> Warning: [GraphQLEndpoint](#graphqlendpoint) takes precedence over this setting.

### Document

```csharp
endpoints.MapNitroApp("/ui").WithOptions(o => o.Document = "{ __typename }");
```

This setting allows us to set a default GraphQL document that should be a placeholder for each new document created using Nitro.

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

This setting allows us to specify default HTTP Headers that will be added to each new document created using Nitro.

### IncludeCookies

```csharp
endpoints.MapNitroApp("/ui").WithOptions(o => o.IncludeCookies = true);
```

This setting specifies the default for including cookies in cross-origin when creating new documents within Nitro.

### Title

```csharp
endpoints.MapNitroApp("/ui").WithOptions(o => o.Title = "My GraphQL explorer");
```

This setting controls the tab name, when Nitro is opened inside of a Web Browser.

### DisableTelemetry

```csharp
endpoints.MapNitroApp("/ui").WithOptions(o => o.DisableTelemetry = true);
```

This setting allows us to disable telemetry events.

### GaTrackingId

```csharp
endpoints.MapNitroApp("/ui").WithOptions(o => o.GaTrackingId = "google-analytics-id");
```

This setting allows us to set a custom Google Analytics Id, which in turn allows us to gain insights into the usage of Nitro hosted as part of our GraphQL server.

The following information is collected:

| Name                 | Description                                                           |
| -------------------- | --------------------------------------------------------------------- |
| `deviceId`           | Random string generated on a per-device basis                         |
| `operatingSystem`    | Name of the operating system: `Windows`, `macOS`, `Linux` & `Unknown` |
| `userAgent`          | `User-Agent` header                                                   |
| `applicationType`    | The type of application: `app` (Electron) or `middleware`             |
| `applicationVersion` | Version of Nitro                                                      |

# MapGraphQLHttp

We can call `MapGraphQLHttp()` on the `IEndpointRouteBuilder` to make our GraphQL server available via HTTP at a specific endpoint.

```csharp
app.UseRouting();

app.UseEndpoints(endpoints =>
{
    endpoints.MapGraphQLHttp("/graphql/http");
});
```

With the above configuration we could now issue HTTP GET / POST requests against the `/graphql/http` endpoint.

## GraphQLServerOptions

The HTTP endpoint can also be configured with per-endpoint overrides using `WithOptions`.

```csharp
endpoints.MapGraphQLHttp("/graphql/http").WithOptions(o => o.EnableGetRequests = false);
```

The same `GraphQLServerOptions` properties available on `MapGraphQL` can be overridden here, except for `Tool` and `EnableSchemaRequests` which are not applicable to standalone HTTP endpoints.

[Learn more about GraphQLServerOptions](#graphqlserveroptions)

# MapGraphQLWebsocket

We can call `MapGraphQLWebSocket()` on the `IEndpointRouteBuilder` to make our GraphQL server available via WebSockets at a specific endpoint.

```csharp
app.UseRouting();

app.UseEndpoints(endpoints =>
{
    endpoints.MapGraphQLWebSocket("/graphql/ws");
});
```

With the above configuration we could now issue GraphQL subscription requests via WebSocket against the `/graphql/ws` endpoint.

# MapGraphQLSchema

We can call `MapGraphQLSchema()` on the `IEndpointRouteBuilder` to make our GraphQL schema available at a specific endpoint.

```csharp
app.UseRouting();

app.UseEndpoints(endpoints =>
{
    endpoints.MapGraphQLSchema("/graphql/schema");
});
```

With the above configuration we could now download our `schema.graphql` file from the `/graphql/schema` endpoint.
