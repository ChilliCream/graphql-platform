---
title: Middleware
---

In Order for Hot Chocolate to be accessible using a HTTP endpoint, we need to register some middleware with ASP.NET Core.

<!-- # Services

In order for Hot Chocolate to function correctly we have to register some services that are used within the server middleware as well as in the schema creation process.

To register these services we have to call `AddGraphQLServer()` in the `ConfigureServices()` method of the `Startup` class.

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddQueryType<Query>();
    }
}
```

The `AddGraphQLServer()` method also has an optional `schemaName` argument, which becomes relevant as soon as we want to host multiple schemas using a single GraphQL server. Most users will be able to safely ignore this argument. -->

# MapGraphQL

We can call `MapGraphQL()` on the `IEndpointRouteBuilder` to register all of the middlewares a standard GraphQL server requires.

```csharp
public class Startup
{
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGraphQL();
        });
    }
}
```

Per default this makes our GraphQL server available at `/graphql`.

We can customize the path at which the GraphQL server responds like the following.

```csharp
endpoints.MapGraphQL("/my/graphql/endpoint");
```

Calling `MapGraphQL()` will enable the following functionality on the specified endpoint:

- HTTP GET and HTTP POST GraphQL requests are handled (Multipart included)
- WebSocket GraphQL requests are handled (if the ASP.NET Core WebSocket Middleware has been registered)
- Including the query string `?sdl` after the endpoint will download the GraphQL schema
- Accessing the endpoint from a browser will load our GraphQL IDE [Banana Cake Pop](/docs/bananacakepop)

We can customize the combined middleware using `GraphQLServerOptions` as shown below or we can only include the middlewares we need and configure them explicitly.

The following middlewares are available:

- [MapBananaCakePop](#mapbananacakepop)
- [MapGraphQLHttp](#mapgraphqlhttp)
- [MapGraphQLWebsocket](#mapgraphqlwebsocket)
- [MapGraphQLSchema](#mapgraphqlschema)

## GraphQLServerOptions

We can influence the behavior of the middlewares registered by `MapGraphQL` using `GraphQLServerOptions`.

### EnableSchemaRequests

```csharp
endpoints.MapGraphQL().WithOptions(new GraphQLServerOptions
{
    EnableSchemaRequests = false
});
```

This setting controls whether the schema of the GraphQL server can be downloaded by appending `?sdl` to the endpoint.

### EnableGetRequests

```csharp
endpoints.MapGraphQL().WithOptions(new GraphQLServerOptions
{
    EnableGetRequests = false
});
```

This setting controls whether the GraphQL server is able to handle GraphQL operations sent via the query string in a HTTP GET request.

### EnableMultipartRequests

```csharp
endpoints.MapGraphQL().WithOptions(new GraphQLServerOptions
{
    EnableMultipartRequests = false
});
```

This setting controls whether the GraphQL server is able to handle HTTP Multipart forms, i.e. file uploads.

### AllowedGetOperations

```csharp
endpoints.MapGraphQL().WithOptions(new GraphQLServerOptions
{
    AllowedGetOperations = AllowedGetOperations.Query
});
```

Using this setting and the `AllowedGetOperations` we can control whether our GraphQL server only accepts queries or queries and mutations.

### Tool

We can specify options for the Banana Cake Pop GraphQL IDE using the `Tool` property.

We could for example only enable Banana Cake Pop during development.

```csharp
public class Startup
{
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGraphQL().WithOptions(new GraphQLServerOptions
            {
                Tool = {
                    Enable = env.IsDevelopment()
                }
            });
        });
    }
}
```

[Learn more about possible GraphQLToolOptions](#graphqltooloptions)

# MapBananaCakePop

We can call `MapBananaCakePop()` on the `IEndpointRouteBuilder` to serve [Banana Cake Pop](/docs/bananacakepop) on a different endpoint than the actual GraphQL endpoint.

```csharp
public class Startup
{
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapBananaCakePop("/graphql/ui");
        });
    }
}
```

This would make Banana Cake Pop accessible via a Web Browser at the `/graphql/ui` endpoint.

## GraphQLToolOptions

We can configure Banana Cake Pop using `GraphQLToolOptions`.

### Enable

```csharp
endpoints.MapBananaCakePop("/ui").WithOptions(new GraphQLToolOptions
{
    Enable = false
});
```

This setting controls whether Banana Cake Pop should be served or not.

### GraphQLEndpoint

```csharp
endpoints.MapBananaCakePop("/ui").WithOptions(new GraphQLToolOptions
{
    GraphQLEndpoint = "/my/graphql/endpoint"
});
```

This setting sets the GraphQL endpoint to use when creating new documents within Banana Cake Pop.

### UseBrowserUrlAsGraphQLEndpoint

```csharp
endpoints.MapBananaCakePop("/ui").WithOptions(new GraphQLToolOptions
{
    UseBrowserUrlAsGraphQLEndpoint = true
});
```

If set to `true` the current Web Browser URL is treated as the GraphQL endpoint when creating new documents within Banana Cake Pop.

> ⚠️ Note: [GraphQLEndpoint](#graphqlendpoint) takes precedence over this setting.

### Document

```csharp
endpoints.MapBananaCakePop("/ui").WithOptions(new GraphQLToolOptions
{
    Document = "{ __typename }"
});
```

This setting allows us to set a default GraphQL document that should be a placeholder for each new document created using Banana Cake Pop.

### HttpMethod

```csharp
endpoints.MapBananaCakePop("/ui").WithOptions(new GraphQLToolOptions
{
    HttpMethod = DefaultHttpMethod.Get
});
```

This setting controls the default HTTP method used to execute GraphQL operations when creating new documents within Banana Cake Pop.

### HttpHeaders

```csharp
endpoints.MapBananaCakePop("/ui").WithOptions(new GraphQLToolOptions
{
    HttpHeaders = new HeaderDictionary
    {
        { "Content-Type", "application/json" }
    }
});
```

This setting allows us to specify default HTTP Headers that will be added to each new document created using Banana Cake Pop.

### IncludeCookies

```csharp
endpoints.MapBananaCakePop("/ui").WithOptions(new GraphQLToolOptions
{
    IncludeCookies = true
});
```

This setting specifies the default for including cookies in cross-origin when creating new documents within Banana Cake Pop.

### Title

```csharp
endpoints.MapBananaCakePop("/ui").WithOptions(new GraphQLToolOptions
{
    Title = "My GraphQL explorer"
});
```

This setting controls the tab name, when Banana Cake Pop is opened inside of a Web Browser.

### DisableTelemetry

```csharp
endpoints.MapBananaCakePop("/ui").WithOptions(new GraphQLToolOptions
{
    DisableTelemetry = true
});
```

This setting allows us to disable telemetry events.

### GaTrackingId

```csharp
endpoints.MapBananaCakePop("/ui").WithOptions(new GraphQLToolOptions
{
    GaTrackingId = "google-analytics-id"
});
```

This setting allows us to set a custom Google Analytics Id, which in turn allows us to gain insights into the usage of Banana Cake Pop hosted as part of our GraphQL server.

The following information is collected:

| Name                 | Description                                                           |
| -------------------- | --------------------------------------------------------------------- |
| `deviceId`           | Random string generated on a per-device basis                         |
| `operatingSystem`    | Name of the operating system: `Windows`, `macOS`, `Linux` & `Unknown` |
| `userAgent`          | `User-Agent` header                                                   |
| `applicationType`    | The type of application: `app` (Electron) or `middleware`             |
| `applicationVersion` | Version of Banana Cake Pop                                            |

# MapGraphQLHttp

We can call `MapGraphQLHttp()` on the `IEndpointRouteBuilder` to make our GraphQL server available via HTTP at a specific endpoint.

```csharp
public class Startup
{
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGraphQLHttp("/graphql/http");
        });
    }
}
```

With the above configuration we could now issue HTTP GET / POST requests against the `/graphql/http` endpoint.

## GraphQLHttpOptions

The HTTP endpoint can be configured using `GraphQLHttpOptions`.

```csharp
endpoints.MapGraphQLHttp("/graphql/http").WithOptions(new GraphQLHttpOptions
{
    EnableGetRequests = false
});
```

The `GraphQLHttpOptions` are the same as the `GraphQLServerOptions` except that there are no `Tool` and `EnableSchemaRequests` properties.

[Learn more about GraphQLServerOptions](#graphqlserveroptions)

# MapGraphQLWebsocket

We can call `MapGraphQLWebSocket()` on the `IEndpointRouteBuilder` to make our GraphQL server available via WebSockets at a specific endpoint.

```csharp
public class Startup
{
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGraphQLWebSocket("/graphql/ws");
        });
    }
}
```

With the above configuration we could now issue GraphQL subscription requests via WebSocket against the `/graphql/ws` endpoint.

# MapGraphQLSchema

We can call `MapGraphQLSchema()` on the `IEndpointRouteBuilder` to make our GraphQL schema available at a specific endpoint.

```csharp
public class Startup
{
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGraphQLSchema("/graphql/schema");
        });
    }
}
```

With the above configuration we could now download our `schema.graphql` file from the `/graphql/schema` endpoint.
