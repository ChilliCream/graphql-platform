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

### Options

We can influence the behavior of the registered middlewares using `GraphQLServerOptions`.

```csharp
endpoints.MapGraphQL().WithOptions(new GraphQLServerOptions
{

});
```

## MapBananaCakePop

## MapGraphQLHttp

## MapGraphQLWebsocket

## MapGraphQLSchema

# Request options

# Schema Options

# Multiple schemas
