---
title: Dependency Injection
---

import { ExampleTabs, Annotation, Code, Schema } from "../../../components/mdx/example-tabs"

If you are unfamiliar with the term "dependency injection", we recommend the following articles to get you started:

- [Dependency injection in .NET](https://docs.microsoft.com/dotnet/core/extensions/dependency-injection)
- [Dependency injection in ASP.NET Core](https://docs.microsoft.com/aspnet/core/fundamentals/dependency-injection)

While Hot Chocolate's internals rely heavily on Microsoft's dependency injection container, you are not required to manage your own dependencies using this container. You can [switch out the service provider](#switching-the-service-provider) and use your own dependency injection mechanism.

When you are using Microsoft's dependency injection container you can continue to register your dependencies like you usually would:

```csharp
services
    .AddSingleton<MySingletonService>()
    .AddScoped<MyScopedService>()
    .AddTransient<MyTransientService>();
```

Just how you access them within the context of Hot Chocolate changes.

# Constructor injection

When starting out with Hot Chocolate you might be inclined to inject dependencies into your GraphQL type definitions using the constructor.

You should <strong style="color: red">avoid</strong> doing this, because

- GraphQL type definitions are singleton and your injected dependency will therefore also become a singleton.
- access to this dependency can not be synchronized by Hot Chocolate during the execution of a request.

Of course this does not apply within your own dependencies. Your `ServiceA` class can still inject `ServiceB` through the constructor.

# Resolver injection

The correct way to inject dependencies into your resolvers is by accessing them on the resolver-level. You can use the `HotChocolate.ServiceAttribute` to designate your dependencies to be injected.

```csharp
public class Query
{
    public User GetUser(string userId, [Service] UserService userService)
        => userService.GetUser(userId);
}
```

The `HotChocolate.ServiceAttribute` also accepts a [`ServiceKind`](#servicekind) which can be used to specify the strategy with which the service should be injected.

```csharp
public Foo GetFoo([Service(ServiceKind.Synchronized)] Service service)
    => // Omitted code for brevity
```

> Note: We also allow the usage of the `Microsoft.AspNetCore.Mvc.FromServicesAttribute`, but with it you can't specify a [`ServiceKind`](#servicekind).

If you are working with the `IResolverContext`, for example in the `Resolve()` callback, you can use the `Service<T>` method to access your dependencies.

```csharp
descriptor
    .Field("users")
    .Resolve(context =>
    {
        var userService = context.Service<UserService>();

        return userService.GetUsers();
    });
```

# RegisterService

Having to specify an attribute to inject a service can become quite tedious when said service is injected into multiple resolvers.

If you want to omit the attribute, you can simply call `RegisterService<T>` on the `IRequestExecutorBuilder`. Now if Hot Chocolate encounters a `T` in the method signature of a resolver, it will automatically resolve the service from the dependency injection container and inject it into the method.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddTransient<UserService>();

builder.Services
    .AddGraphQLServer()
    .RegisterService<UserService>()
    .AddQueryType<Query>();

public class Query
{
    public List<User> GetUsers(UserService userService)
        => userService.GetUsers();
}
```

> ⚠️ Note: You still have to register the service with a lifetime in the actual dependency injection container, for example by calling `services.AddTransient<T>`. `RegisterService<T>` on its own is not enough.

You can also specify a [`ServiceKind`](#servicekind) as argument to the `RegisterService<T>` method.

```csharp
services
    .AddGraphQLServer()
    .RegisterService<UserService>(ServiceKind.Synchronized);
```

If you are registering an interface, you need to call `RegisterService` with the interface as the generic type parameter.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddTransient<IUserService, UserService>();

builder.Services
    .AddGraphQLServer()
    .RegisterService<IUserService>()
    .AddQueryType<Query>();

public class Query
{
    public List<User> GetUsers(IUserService userService)
        => userService.GetUsers();
}
```

# UseServiceScope

Per default scoped services are scoped to the current request. If you want to resolve the services for a particular resolver using a dedicated scope, you can use the `UseServiceScope` middleware.

<ExampleTabs>
<Annotation>

```csharp
public class Query
{
    [UseServiceScope]
    public Foo GetFoo([Service] Service1 service1, [Service] Service2 service2)
        => // Omitted code for brevity
}
```

</Annotation>
<Code>

```csharp
descriptor.Field("foo")
    .UseServiceScope()
    .Resolve(context =>
    {
        var service1 = context.Service<Service1>();
        var service2 = context.Service<Service2>();

        // Omitted code for brevity
    });
```

</Code>
<Schema>

Take a look at the Annotation-based or Code-first example.

</Schema>
</ExampleTabs>

If `Service1` and `Service2` are scoped services they will both be resolved from the same scope that only exists for this particular resolver. If the resolver is invoked multiple times, the scope will be different each time.

# ServiceKind

When injecting a service you can specify a `ServiceKind` to instruct Hot Chocolate on how you want a particular service to be injected.

### ServiceKind.Default

The services are injected according to their [service lifetime](https://docs.microsoft.com/dotnet/core/extensions/dependency-injection#service-lifetimes).

- Singleton: The same instance of a the service is injected into the resolver throughout the lifetime of the GraphQL server.
- Scoped: The same instance of a the service is injected into the resolver throughout the lifetime of a request, since the service is being resolved from a request-scoped service scope.
- Transient: A new instance of a the service is injected into the resolver for each resolver invocation.

### ServiceKind.Synchronized

Per default (most) resolvers are executed in parallel. Your service might not support being accessed concurrently. If this is the case, you can inject the service using the `ServiceKind.Synchronized`. This will cause the entire resolver to run serially, which means that no other resolver will be executed, while this resolver is still running.

> ⚠️ Note: This synchronization only applies within the same request. If your service is a Singleton the `ServiceKind.Synchronized` does not prevent the resolver from running concurrently in two separate requests.

### ServiceKind.Resolver

If the service is scoped it will be resolved from a resolver-scoped service scope, similar to how the [`UseServiceScope`](#useservicescope) middleware works. Except that only this specific service, not other services accessed by the resolver, is provided using this resolver-scoped service scope.

If two scoped services within the same resolver are injected using `ServiceKind.Resolver` they will be resolved from the same resolver-scoped service scope. If the [`UseServiceScope`](#useservicescope) middleware is already applied to the resolver, services injected using `ServiceKind.Resolver` will be resolved from this resolver-scoped service scope.

### ServiceKind.Pooled

If your service is registered as an `ObjectPool<T>` and the service is injected using the `ServiceKind.Pooled`, one instance of the service will be resolved from the pool for each invocation of the resolver and returned after the resolver has finished executing.

```csharp
var builder = WebApplication.CreateBuilder(args);

var pool = new ObjectPool<FooService>();

builder.AddSingleton<ObjectPool<FooService>>(pool);

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>();

public class Query
{
    public Foo GetFoo([Service(ServiceKind.Pooled)] FooService service)
        => // Omitted code for brevity
}
```

[Learn more about `ObjectPool<T>`](https://docs.microsoft.com/dotnet/api/microsoft.extensions.objectpool.objectpool-1)

# Switching the service provider

Per default Hot Chocolate uses the request scoped `IServiceProvider` to provide services to your resolvers: [`HttpContext.RequestServices`](https://docs.microsoft.com/dotnet/api/microsoft.aspnetcore.http.httpcontext.requestservices).

You can switch out the service provider used for GraphQL requests, as long as your Dependency Injection container implements the [`IServiceProvider`](https://docs.microsoft.com/dotnet/api/system.iserviceprovider) interface.

To switch out the service provider you need to call [`SetServices`](/docs/hotchocolate/server/interceptors#setservices) on the [`IQueryRequestBuilder`](/docs/hotchocolate/server/interceptors#iqueryrequestbuilder) in both the [`IHttpRequestInterceptor`](/docs/hotchocolate/server/interceptors#ihttprequestinterceptor) and the [`ISocketSessionInterceptor`](/docs/hotchocolate/server/interceptors#isocketsessioninterceptor).

```csharp
public class HttpRequestInterceptor : DefaultHttpRequestInterceptor
{
    public override async ValueTask OnCreateAsync(HttpContext context,
        IRequestExecutor requestExecutor, IQueryRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        // keeping this line is important!
        await base.OnCreateAsync(context, requestExecutor, requestBuilder,
            cancellationToken);

        requestBuilder.SetServices(YOUR_SERVICE_PROVIDER);
    }
}

public class SocketSessionInterceptor : DefaultSocketSessionInterceptor
{
    public override async ValueTask OnRequestAsync(ISocketConnection connection,
        IQueryRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        // keeping this line is important!
        await base.OnRequestAsync(connection, requestBuilder,
            cancellationToken);

        requestBuilder.SetServices(YOUR_SERVICE_PROVIDER);
    }
}
```

You also need to register these interceptors for them to take effect.

```csharp
services
    .AddGraphQLServer()
    .AddHttpRequestInterceptor<HttpRequestInterceptor>()
    .AddSocketSessionInterceptor<SocketSessionInterceptor>();
```

[Learn more about interceptors](/docs/hotchocolate/server/interceptors)
