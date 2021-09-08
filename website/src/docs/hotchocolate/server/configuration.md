---
title: Configuration
---

Configuration

# Services

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

The `AddGraphQLServer()` method also has an optional `schemaName` argument, which becomes relevant as soon as we want to host [multiple schemas](#multiple-schemas) using a single GraphQL server. Most users will be able to safely ignore this argument.

# Middleware

Once we have registered all of the relevant services it is time for middlewares. Hot Chocolate offers some extension methods on the `IEndpointRouteBuilder` allowing us to either register all GraphQL server middlewares using one call or being explicit about the middlewares we want to add.

## MapGraphQL

We can call `MapGraphQL()` on the `IEndpointRouteBuilder` in the `Configure()` method of the `Startup` class to register all of the middlewares a standard GraphQL server requires.

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

Per default this makes our GraphQL server available at `/graphql`. We can customize the path at which the GraphQL server responds like the following.

```csharp
endpoints.MapGraphQL("/my/graphql/endpoint");
```

Calling `MapGraphQL()` will enable the following functionality on the specified endpoint:

- HTTP GET and HTTP POST GraphQL requests are handled (Multipart included)
- WebSocket GraphQL requests are handled (if the ASP.NET Core WebSocket Middleware has been registered)
- Including the query string `?sdl` after the endpoint will download the GraphQL schema
- Accessing the endpoint from a browser will load our GraphQL IDE [Banana Cake Pop](/docs/bananacakepop)

### GraphQLServerOptions

We can influence the behavior of the middlewares registered by `MapGraphQL` using `GraphQLServerOptions`.

#### EnableSchemaRequests

```csharp
endpoints.MapGraphQL().WithOptions(new GraphQLServerOptions
{
    EnableSchemaRequests = false
});
```

This setting controls whether the schema of the GraphQL server can be downloaded by appending `?sdl` to the endpoint.

#### EnableGetRequests

```csharp
endpoints.MapGraphQL().WithOptions(new GraphQLServerOptions
{
    EnableGetRequests = false
});
```

This setting controls whether the GraphQL server is able to handle GraphQL operations sent via the query string in a HTTP GET request.

#### EnableMultipartRequests

```csharp
endpoints.MapGraphQL().WithOptions(new GraphQLServerOptions
{
    EnableMultipartRequests = false
});
```

This setting controls whether the GraphQL server is able to handle HTTP Multipart forms, i.e. file uploads.

#### AllowedGetOperations

```csharp
endpoints.MapGraphQL().WithOptions(new GraphQLServerOptions
{
    AllowedGetOperations = AllowedGetOperations.Query
});
```

Using this setting and the `AllowedGetOperations` we can control whether our GraphQL server only accepts queries or queries and mutations.

#### Tool

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
                    Enabled = env.IsDevelopment()
                }
            });
        });
    }
}
```

[Learn more about possible GraphQLToolOptions](#GraphQLToolOptions)

## MapBananaCakePop

If we want to serve [Banana Cake Pop](/docs/bananacakepop) on a different route than the actual GraphQL server, we can do so using the `MapBananaCakePop` middleware.

```csharp
public class Startup
{
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapBananaCakePop("/ui");
        });
    }
}
```

This would make Banana Cake Pop accessible at the `/ui` endpoint.

### GraphQLToolOptions

We can configure Banana Cake Pop using `GraphQLToolOptions`.

#### Enable

```csharp
endpoints.MapBananaCakePop("/ui").WithOptions(new GraphQLToolOptions
{
    Enable = false
});
```

This setting controls whether Banana Cake Pop should be served or not.

#### GraphQLEndpoint

```csharp
endpoints.MapBananaCakePop("/ui").WithOptions(new GraphQLToolOptions
{
    GraphQLEndpoint = "/my/graphql/endpoint"
});
```

This setting sets the GraphQL endpoint to use when creating new documents within Banana Cake Pop.

#### UseBrowserUrlAsGraphQLEndpoint

```csharp
endpoints.MapBananaCakePop("/ui").WithOptions(new GraphQLToolOptions
{
    UseBrowserUrlAsGraphQLEndpoint = true
});
```

If set to `true` the current Web Browser URL is treated as the GraphQL endpoint when creating new documents within Banana Cake Pop.

> ⚠️ Note: [GraphQLEndpoint](#GraphQLEndpoint) takes precedence over this setting.

#### Document

```csharp
endpoints.MapBananaCakePop("/ui").WithOptions(new GraphQLToolOptions
{
    Document = "{ __typename }"
});
```

This setting allows us to set a default GraphQL document that should be a placeholder for each new document created using Banana Cake Pop.

#### HttpMethod

#### HttpHeaders

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

#### IncludeCookies

#### Title

```csharp
endpoints.MapBananaCakePop("/ui").WithOptions(new GraphQLToolOptions
{
    Title = "My GraphQL explorer"
});
```

This setting controls the tab name, when Banana Cake Pop is opened inside of a Web Browser.

#### DisableTelemetry

#### GaTrackingId

## MapGraphQLHttp

## MapGraphQLWebsocket

## MapGraphQLSchema
