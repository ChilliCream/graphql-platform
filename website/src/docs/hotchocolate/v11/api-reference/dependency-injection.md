---
title: Dependency Injection
---

Hot Chocolate is deeply integrated with `Microsoft.Extension.DependencyInjection` but can also be used with other dependency injection frameworks as long as they implement `IServiceProvider`.

While it is possible to have constructor dependency injection on GraphQL type objects themselves it should be avoided. Think of the GraphQL types as `System.Type`s. Dependency injection is best used on the runtime types, that are passed through the execution engine.

**GraphQL Type**

```csharp
public class QueryType : ObjectType<Query>
{
    // code omitted for brevity
}
```

**Runtime Type**

```csharp
public class Query
{
    // code omitted for brevity
}
```

Root runtime types and extension runtime types do not need to be registered with the dependency injection. Hot Chocolate will use the dependency injection and handle them as singletons by keeping the instance for the lifetime of the `IRequestExecutor`.

Lets say we have a singleton service `IUserRepository` and our class `Query`.

```csharp
public class Query
{
    public Query(IUserRepository repository)
    {

    }

    // code omitted for brevity
}
```

With this configuration we can just do the following:

```csharp
services
    .AddSingleton<IUserRepository, UserRepository>()
    .AddGraphQLServer()
    .AddQueryType<Query>()
```

But if we had a scoped user repository and used constructor injection on our `Query` we would need to register our `Query` class with the dependency injection and Hot Chocolate would resolve it automatically from there.

```csharp
services
    .AddScoped<IUserRepository, UserRepository>()
    .AddScoped<Query>()
    .AddGraphQLServer()
    .AddQueryType<Query>()
```

The same behavior is true for type extensions.

```csharp
[ExtendObjectType(OperationTypes.Query)]
public class UserQueries
{
    public UserQueries(IUserRepository repository)
    {

    }

    // code omitted for brevity
}
```

```csharp
services
    .AddScoped<IUserRepository, UserRepository>()
    .AddScoped<UserQueries>()
    .AddGraphQLServer()
    .AddQueryType()
    .AddTypeExtension<UserQueries>()
```

# Method-Level Dependency Injection

Hot Chocolate also allows for method-level dependency injection. This allows you to create side-effect free resolvers regarding the dependency injection. We in general advise to use method-level dependency injection on resolvers for better execution performance and simpler maintenance.

```csharp
public async Task<User> GetUserAsync([Service] IUserRepository repository)
{
    // The user repository can just be used by this resolver,
    // it does not matter if it is transient, scoped or singleton.
}
```

In this case you can keep the simpler configuration and let Hot Chocolate take care of the lifetime.

```csharp
services
    .AddSingleton<IUserRepository, UserRepository>()
    .AddGraphQLServer()
    .AddQueryType<Query>()
```

This also benefits the execution engine since it knows which services are used and how to optimize execution.

> Note: For method-level dependency injection we also allow to reuse the `FromServicesAttribute` from ASP.NET core.

# Custom Dependency Injection Container

In order to override the default dependency injection container you need to override the `DefaultHttpRequestInterceptor` and the `DefaultSocketSessionInterceptor`.

1. Inherit from `DefaultHttpRequestInterceptor` and override `OnCreateAsync`.

```csharp
public class CustomHttpRequestInterceptor : DefaultHttpRequestInterceptor
{
    public async override ValueTask OnCreateAsync(
        HttpContext context,
        IRequestExecutor requestExecutor,
        IQueryRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        await base.OnCreateAsync(context, requestExecutor, requestBuilder, cancellationToken);
        requestBuilder.SetServices(CUSTOM_SERVICE_PROVIDER);
    }
}
```

2. Register `CustomHttpRequestInterceptor` with the root `ServiceCollection`.

```csharp
services
    .AddGraphQLServer()
    .AddHttpRequestInterceptor<CustomHttpRequestInterceptor>();
```

3. Next, inherit from `DefaultSocketSessionInterceptor` and override `OnRequestAsync`.

```csharp
public class CustomSocketSessionInterceptor : DefaultSocketSessionInterceptor
{
    public async override ValueTask OnRequestAsync(
        ISocketConnection connection,
        IQueryRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        await OnRequestAsync(connection, requestBuilder, cancellationToken);
        requestBuilder.SetServices(CUSTOM_SERVICE_PROVIDER);
    }
}
```

4. Last, register your `CustomSocketSessionInterceptor` with the root `ServiceCollection`.

```csharp
services
    .AddGraphQLServer()
    .AddSocketSessionInterceptor<CustomSocketSessionInterceptor>();
```
