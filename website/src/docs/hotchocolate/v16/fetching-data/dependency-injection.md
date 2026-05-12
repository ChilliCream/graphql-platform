---
title: Dependency Injection
---

If you're unfamiliar with dependency injection, the ASP.NET Core documentation is a good starting point:

- [Dependency injection in ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/dependency-injection)

Dependency injection in Hot Chocolate works the same as in a regular ASP.NET Core application: register services with the DI container as usual.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddSingleton<MySingletonService>()
    .AddScoped<MyScopedService>()
    .AddTransient<MyTransientService>();
```

Hot Chocolate automatically recognizes types registered as services in the DI container and injects them into resolver method parameters without requiring any attribute. This works similarly to [Minimal APIs parameter binding](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/parameter-binding).

When the execution engine encounters a resolver parameter whose type is registered in the DI container, it resolves the service automatically. You do not need to apply the `[Service]` attribute.

```csharp
public static async Task<Book?> GetBookByIdAsync(
    Guid id,
    BookService bookService)
{
    return await bookService.GetBookAsync(id);
}
```

# Resolver Injection

Injecting services at the method level has several benefits:

- The execution engine can optimize the resolver and adjust the execution strategy based on the needs of a specific service.
- Refactoring (moving the resolver method between classes) becomes easier because the resolver does not depend on its outer class.
- If many resolvers are colocated in a single class we only need to resolve the services that are needed for resolvers that are actually being executed

In the following example, `BookService` is injected automatically when it is registered as a service in the DI container:

<ExampleTabs>
<Implementation>

```csharp
[QueryType]
public static partial class Query
{
    public static async Task<Book?> GetBookByIdAsync(
        Guid id,
        BookService bookService)
    {
        return await bookService.GetBookAsync(id);
    }
}
```

</Implementation>
<Code>

```csharp
public sealed class QueryType : ObjectType
{
    protected override void Configure(
        IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("bookById")
            .Argument("id", a => a.Type<NonNullType<UuidType>>())
            .Resolve<Book?>(async ctx => await ctx
                .Service<BookService>()
                .GetBookAsync(ctx.ArgumentValue<Guid>("id")));
    }
}
```

</Code>
</ExampleTabs>

## Service Scoping

In GraphQL, resolvers are expected to be side-effect free. The execution engine may run them in parallel or out of order, so relying on shared mutable state within services can lead to issues. For example, if multiple resolvers use the same Entity Framework DbContext concurrently, it can cause thread-safety problems and execution errors.

To avoid this, Hot Chocolate creates a new service scope for each async resolver and each DataLoader dispatch. This ensures every resolver or DataLoader execution receives its own service instance.

If you want to change the default scoping behavior, you can update the GraphQL options.

```csharp
builder
    .AddGraphQL()
    .ModifyOptions(o =>
    {
        o.DefaultQueryDependencyInjectionScope =
            DependencyInjectionScope.Resolver;
        o.DefaultMutationDependencyInjectionScope =
            DependencyInjectionScope.Request;
    });
```

You can also override the scope on a per-resolver basis:

<ExampleTabs>
<Implementation>

```csharp
[QueryType]
public static class Query
{
    [UseRequestScope]
    public static async Task<Book?> GetBookByIdAsync(
        Guid id,
        BookService bookService) => // ...
}
```

</Implementation>
<Code>

```csharp
public sealed class QueryType : ObjectType
{
    protected override void Configure(
        IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("bookById")
            // ...
            .UseRequestScope();
    }
}
```

</Code>
</ExampleTabs>

# Constructor Injection

If you're coming from an ASP.NET core controller-based workflow, you might be used to injecting services into your types via the constructor. In Hot Chocolate, you should <strong style="color: red">avoid</strong> injecting services into GraphQL type definitions for these reasons:

- GraphQL type definitions are registered as singletons, so constructor-injected services would also become singletons.
- Hot Chocolate cannot synchronize access to those services during request execution, which can cause issues with services that are not thread-safe (such as EF Core's DbContext).

> _Note:_ This guidance does not apply to your own application services. For example, `ServiceA` can still inject `ServiceB` via constructor injection.

# Keyed Services

A keyed service is a service registered in the DI container with an associated key or name. This allows you to register multiple instances of the same service type, each identified by a unique key. Keyed services are useful when you need different configurations or implementations of the same service type within your application.

You can register a keyed service like this:

```csharp
builder.Services.AddKeyedScoped<BookService>("bookService");
```

<ExampleTabs>
<Implementation>

You can then access the keyed service in your resolver by applying the `[Service]` attribute with the key:

```csharp
[QueryType]
public static class Query
{
    public static async Task<Book?> GetBookByIdAsync(
        Guid id,
        [Service("bookService")] BookService bookService)
    {
        return await bookService.GetBookAsync(id);
    }
}
```

</Implementation>
<Code>

You can also access the keyed service in your resolver by passing the key to the `Service` helper on the resolver context.

```csharp
public sealed class QueryType : ObjectType
{
    protected override void Configure(
        IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("bookById")
            .Argument("id", a => a.Type<NonNullType<UuidType>>())
            .Resolve<Book?>(async ctx => await ctx
                .Service<BookService>("bookService")
                .GetBookAsync(ctx.ArgumentValue<Guid>("id")));
    }
}
```

</Code>
</ExampleTabs>

# Switching the Service Provider

While Hot Chocolate's internals rely on Microsoft's dependency injection container, you are not required to manage your own dependencies using Microsoft`s container. By default, Hot Chocolate uses the request-scoped [`HttpContext.RequestServices`](https://docs.microsoft.com/dotnet/api/microsoft.aspnetcore.http.httpcontext.requestservices) `IServiceProvider` to provide services to your resolvers.

You can switch out the service provider used for GraphQL requests, as long as your DI container implements the [`IServiceProvider`](https://docs.microsoft.com/dotnet/api/system.iserviceprovider) interface and supports scoping.

To switch the service provider, call [`SetServices`](/docs/hotchocolate/v16/server/interceptors#setservices) on the [`OperationRequestBuilder`](/docs/hotchocolate/v16/server/interceptors#operationrequestbuilder) in both the [`IHttpRequestInterceptor`](/docs/hotchocolate/v16/server/interceptors#ihttprequestinterceptor) and the [`ISocketSessionInterceptor`](/docs/hotchocolate/v16/server/interceptors#isocketsessioninterceptor).

```csharp
public sealed class HttpRequestInterceptor
    : DefaultHttpRequestInterceptor
{
    public override async ValueTask OnCreateAsync(
        HttpContext context,
        IRequestExecutor requestExecutor,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        // keeping these lines is important!
        await base.OnCreateAsync(
            context,
            requestExecutor,
            requestBuilder,
            cancellationToken);

        requestBuilder.SetServices(YOUR_SERVICE_PROVIDER);
    }
}

public sealed class SocketSessionInterceptor
    : DefaultSocketSessionInterceptor
{
    public override async ValueTask OnRequestAsync(
        ISocketConnection connection,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        // keeping these lines is important!
        await base.OnRequestAsync(
            connection,
            requestBuilder,
            cancellationToken);

        requestBuilder.SetServices(YOUR_SERVICE_PROVIDER);
    }
}
```

Register these interceptors for them to take effect:

```csharp
builder
    .AddGraphQL()
    .AddHttpRequestInterceptor<HttpRequestInterceptor>()
    .AddSocketSessionInterceptor<SocketSessionInterceptor>();
```

[Learn more about interceptors](/docs/hotchocolate/v16/server/interceptors)

# Next Steps

- [Interceptors](/docs/hotchocolate/v16/server/interceptors) for setting request-scoped state and services.
- [Global State](/docs/hotchocolate/v16/server/global-state) for sharing per-request data between resolvers.
- [Migrate from v15 to v16](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16#clearer-separation-between-schema-and-application-services) for the full migration details on schema vs application services.
