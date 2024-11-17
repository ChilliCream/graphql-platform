---
title: Dependency injection
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

Injecting these services into Hot Chocolate resolvers works in a similar way to [Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/parameter-binding), in that the parameters are bound implicitly when the type is configured as a service, without the need to apply an attribute.

# Resolver injection

The correct way to inject dependencies into your resolvers is by injecting them into your resolver method as an argument.

[Learn more about why constructor injection into GraphQL types is a bad idea](#constructor-injection)

Injecting dependencies at the method-level has a couple of benefits:

- The resolver can be optimized and the execution strategy can be adjusted depending on the needs of a specific service.
- Refactoring, i.e. moving the resolver method between classes, becomes easier, since the resolver does not have any dependencies on its outer class.

In the following example, `BookService` will be injected automatically when registered as a service in the DI container.

<ExampleTabs>
<Implementation>

```csharp
[QueryType]
public static class Query
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
<Schema>

Take a look at the implementation-first or code-first example.

</Schema>
</ExampleTabs>

## Default scope

By default, scoped services are scoped to the resolver for queries and DataLoaders, and to the current request for mutations. This means that each execution of a query or DataLoader that accepts a scoped service will receive a **separate** instance, avoiding threading issues with services that do not support multi-threading (f.e. Entity Framework DbContexts). Since mutations are executed sequentially, they receive the **same** request-scoped instance.

These defaults can be changed globally as follows:

```csharp
builder.Services
    .AddGraphQLServer()
    .ModifyOptions(o =>
    {
        o.DefaultQueryDependencyInjectionScope =
            DependencyInjectionScope.Resolver;
        o.DefaultMutationDependencyInjectionScope =
            DependencyInjectionScope.Request;
    });
```

They can also be overridden on a per-resolver basis:

<ExampleTabs>
<Implementation>

```csharp
[QueryType]
public static class Query
{
    [UseRequestScope] // ⬅️
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
            .UseRequestScope(); // ⬅️
    }
}
```

</Code>
<Schema>

Take a look at the implementation-first or code-first example.

</Schema>
</ExampleTabs>

# Constructor injection

When starting out with Hot Chocolate you might be inclined to inject dependencies into your GraphQL type definitions using the constructor.

You should <strong style="color: red">avoid</strong> doing this, because:

- GraphQL type definitions are singleton and your injected dependency will therefore also become a singleton.
- Access to this dependency can not be synchronized by Hot Chocolate during the execution of a request.

Of course this does not apply within your own dependencies. Your `ServiceA` class can still inject `ServiceB` through the constructor.

When you need to access dependency injection services in your resolvers, try to stick to the [method-level dependency injection approach](#resolver-injection) outlined above.

# Keyed services

A keyed service registered like this:

```csharp
builder.Services.AddKeyedScoped<BookService>("bookService");
```

... can be accessed in your resolver with the following code:

<ExampleTabs>
<Implementation>

```csharp
[QueryType]
public static class Query
{
    public static async Task<Book?> GetBookByIdAsync(
        Guid id,
        [Service("bookService")] BookService bookService) // ⬅️
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
                .Service<BookService>("bookService") // ⬅️
                .GetBookAsync(ctx.ArgumentValue<Guid>("id")));
    }
}
```

</Code>
<Schema>

Take a look at the implementation-first or code-first example.

</Schema>
</ExampleTabs>

# Switching the service provider

While Hot Chocolate's internals rely heavily on Microsoft's dependency injection container, you are not required to manage your own dependencies using this container. By default Hot Chocolate uses the request-scoped [`HttpContext.RequestServices`](https://docs.microsoft.com/dotnet/api/microsoft.aspnetcore.http.httpcontext.requestservices) `IServiceProvider` to provide services to your resolvers.

You can switch out the service provider used for GraphQL requests, as long as your dependency injection container implements the [`IServiceProvider`](https://docs.microsoft.com/dotnet/api/system.iserviceprovider) interface.

To switch out the service provider you need to call [`SetServices`](/docs/hotchocolate/v15/server/interceptors#setservices) on the [`OperationRequestBuilder`](/docs/hotchocolate/v15/server/interceptors#operationrequestbuilder) in both the [`IHttpRequestInterceptor`](/docs/hotchocolate/v15/server/interceptors#ihttprequestinterceptor) and the [`ISocketSessionInterceptor`](/docs/hotchocolate/v15/server/interceptors#isocketsessioninterceptor).

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

You also need to register these interceptors for them to take effect.

```csharp
builder.Services
    .AddGraphQLServer()
    .AddHttpRequestInterceptor<HttpRequestInterceptor>()
    .AddSocketSessionInterceptor<SocketSessionInterceptor>();
```

[Learn more about interceptors](/docs/hotchocolate/v15/server/interceptors)
