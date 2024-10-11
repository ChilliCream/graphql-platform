---
title: Dependency Injection
---

If you are unfamiliar with the term "dependency injection", we recommend the following articles to get you started:

- [Dependency injection in .NET](https://docs.microsoft.com/dotnet/core/extensions/dependency-injection)
- [Dependency injection in ASP.NET Core](https://docs.microsoft.com/aspnet/core/fundamentals/dependency-injection)

Dependency injection with Hot Chocolate works almost the same as with a regular ASP.NET Core application. For instance, nothing changes about how you add services to the dependency injection container.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddSingleton<MySingletonService>()
    .AddScoped<MyScopedService>()
    .AddTransient<MyTransientService>();
```

Injecting these services into Hot Chocolate resolvers works a bit different though.

# Resolver injection

The correct way to inject dependencies into your resolvers is by injecting them into your resolver method as an argument.

[Learn more about why constructor injection into GraphQL types is a bad idea](#constructor-injection)

Injecting dependencies at the method-level has a couple of benefits:

- The resolver can be optimized and the execution strategy can be adjusted depending on the needs of a specific service.
- Refactoring, i.e. moving the resolver method between classes, becomes easier, since the resolver does not have any dependencies on its outer class.

You might have already encountered this concept in regular ASP.NET Core applications in form of the [`Microsoft.AspNetCore.Mvc.FromServicesAttribute`](https://docs.microsoft.com/dotnet/api/microsoft.aspnetcore.mvc.fromservicesattribute).

While you can also use this attribute to inject services into Hot Chocolate resolvers, we recommend using our own `HotChocolate.ServiceAttribute`.

```csharp
public class Query
{
    public Foo GetFoo(string bar, [Service] FooService fooService)
        => // Omitted code for brevity
}
```

Our own attribute also accepts a [ServiceKind](#servicekind) which can be used to specify the strategy with which the service should be injected.

```csharp
public Foo GetFoo([Service(ServiceKind.Synchronized)] Service service)
    => // Omitted code for brevity
```

If you want to avoid cluttering your resolvers with too many attributes, you can also [register your services as well-known services](#registerservice), allowing you to omit the `ServiceAttribute`.

If you are working with the `IResolverContext`, for example in the `Resolve()` callback, you can use the `Service<T>` method to access your dependencies.

```csharp
descriptor
    .Field("foo")
    .Resolve(context =>
    {
        FooService service = context.Service<FooService>();

        // Omitted code for brevity
    });
```

If you are trying to inject a Entity Framework Core `DbContext`, be sure to checkout our [guidance on working with Entity Framework Core](/docs/hotchocolate/v12/integrations/entity-framework).

# Constructor injection

When starting out with Hot Chocolate you might be inclined to inject dependencies into your GraphQL type definitions using the constructor.

You should <strong style="color: red">avoid</strong> doing this, because

- GraphQL type definitions are singleton and your injected dependency will therefore also become a singleton.
- access to this dependency can not be synchronized by Hot Chocolate during the execution of a request.

Of course this does not apply within your own dependencies. Your `ServiceA` class can still inject `ServiceB` through the constructor.

When you need to access dependency injection services in your resolvers, try to stick to the [method-level dependency injection approach](#resolver-injection) outlined above.

# RegisterService

Having to specify an attribute to inject a service can become quite tedious when said service is injected into multiple resolvers.

If you want to omit the attribute, you can simply call `RegisterService<T>` on the `IRequestExecutorBuilder`. The Hot Chocolate Resolver Compiler will then take care of wiring up all of the `T` in the method signature of your resolvers to the dependency injection mechanism.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTransient<FooService>();

builder.Services
    .AddGraphQLServer()
    .RegisterService<FooService>()
    .AddQueryType<Query>();

public class Query
{
    public Foo GetFoo(FooService FooService)
        => // Omitted code for brevity
}
```

> Warning: You still have to register the service with a lifetime in the actual dependency injection container, for example by calling `services.AddTransient<T>`. `RegisterService<T>` on its own is not enough.

You can also specify a [ServiceKind](#servicekind) as argument to the `RegisterService<T>` method.

```csharp
services
    .AddGraphQLServer()
    .RegisterService<FooService>(ServiceKind.Synchronized);
```

If you are registering an interface, you need to call `RegisterService` with the interface as the generic type parameter.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTransient<IFooService, FooService>();

builder.Services
    .AddGraphQLServer()
    .RegisterService<IFooService>()
    .AddQueryType<Query>();

public class Query
{
    public Foo GetFoo(IFooService FooService)
        => // Omitted code for brevity
}
```

# UseServiceScope

Per default scoped services are scoped to the current request. If you want to resolve the services for a particular resolver using a dedicated [`IServiceScope`](https://docs.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.iservicescope), you can use the `UseServiceScope` middleware.

<ExampleTabs>
<Implementation>

```csharp
public class Query
{
    [UseServiceScope]
    public Foo GetFoo([Service] Service1 service1, [Service] Service2 service2)
        => // Omitted code for brevity
}
```

</Implementation>
<Code>

```csharp
descriptor.Field("foo")
    .UseServiceScope()
    .Resolve(context =>
    {
        Service1 service1 = context.Service<Service1>();
        Service2 service2 = context.Service<Service2>();

        // Omitted code for brevity
    });
```

</Code>
<Schema>

Take a look at the implementation-first or code-first example.

</Schema>
</ExampleTabs>

If `Service1` and `Service2` are scoped services they will both be resolved from the same [`IServiceScope`](https://docs.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.iservicescope) that only exists for this particular resolver. If the resolver is invoked multiple times, the [`IServiceScope`](https://docs.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.iservicescope) will be different each time. The resolver-scoped [`IServiceScope`](https://docs.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.iservicescope) and all services resolved with it, are disposed as soon as the resolver has been executed.

# ServiceKind

When injecting a service you can specify a `ServiceKind` to instruct Hot Chocolate to use a certain strategy when injecting the service.

## ServiceKind.Default

The services are injected according to their [service lifetime](https://docs.microsoft.com/dotnet/core/extensions/dependency-injection#service-lifetimes).

- Singleton: The same instance of the service is injected into the resolver throughout the lifetime of the GraphQL server.
- Scoped: The same instance of the service is injected into the resolver throughout the lifetime of a request, since the service is being resolved from a request-scoped [`IServiceScope`](https://docs.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.iservicescope).
- Transient: A new instance of the service is injected into the resolver for each resolver invocation.

## ServiceKind.Synchronized

Per default (most) resolvers are executed in parallel. Your service might not support being accessed concurrently. If this is the case, you can inject the service using the `ServiceKind.Synchronized`. This will cause the resolver to run serially, which means that no other resolver will be executed, while this resolver is still running.

> Warning: This synchronization only applies within the same request. If your service is a Singleton the `ServiceKind.Synchronized` does not prevent the resolver from running concurrently in two separate requests.

## ServiceKind.Resolver

If the service is scoped it will be resolved from a resolver-scoped [`IServiceScope`](https://docs.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.iservicescope), similar to how the [`UseServiceScope`](#useservicescope) middleware works. Except that only this specific service, not other services accessed by the resolver, is provided using this resolver-scoped [`IServiceScope`](https://docs.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.iservicescope).

If two scoped services within the same resolver are injected using `ServiceKind.Resolver` they will be resolved from the same resolver-scoped [`IServiceScope`](https://docs.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.iservicescope). If the [`UseServiceScope`](#useservicescope) middleware is already applied to the resolver, services injected using `ServiceKind.Resolver` will be resolved from this resolver-scoped [`IServiceScope`](https://docs.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.iservicescope).

The resolver-scoped [`IServiceScope`](https://docs.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.iservicescope) and all services resolved with it, are disposed as soon as the resolver has been executed.

## ServiceKind.Pooled

If your service is registered as an `ObjectPool<T>` and the service is injected using the `ServiceKind.Pooled`, one instance of the service will be resolved from the pool for each invocation of the resolver and returned after the resolver has finished executing.

```csharp
var builder = WebApplication.CreateBuilder(args);

var pool = new ObjectPool<FooService>();

builder.Services.AddSingleton<ObjectPool<FooService>>(pool);

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

While Hot Chocolate's internals rely heavily on Microsoft's dependency injection container, you are not required to manage your own dependencies using this container. Per default Hot Chocolate uses the request-scoped [`HttpContext.RequestServices`](https://docs.microsoft.com/dotnet/api/microsoft.aspnetcore.http.httpcontext.requestservices) `IServiceProvider` to provide services to your resolvers.

You can switch out the service provider used for GraphQL requests, as long as your dependency injection container implements the [`IServiceProvider`](https://docs.microsoft.com/dotnet/api/system.iserviceprovider) interface.

To switch out the service provider you need to call [`SetServices`](/docs/hotchocolate/v12/server/interceptors#setservices) on the [`IQueryRequestBuilder`](/docs/hotchocolate/v12/server/interceptors#iqueryrequestbuilder) in both the [`IHttpRequestInterceptor`](/docs/hotchocolate/v12/server/interceptors#ihttprequestinterceptor) and the [`ISocketSessionInterceptor`](/docs/hotchocolate/v12/server/interceptors#isocketsessioninterceptor).

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

[Learn more about interceptors](/docs/hotchocolate/v12/server/interceptors)
