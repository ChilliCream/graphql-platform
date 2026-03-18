---
title: Dependency Injection
---

If you are unfamiliar with dependency injection, the following articles provide a good starting point:

- [Dependency injection in .NET](https://docs.microsoft.com/dotnet/core/extensions/dependency-injection)
- [Dependency injection in ASP.NET Core](https://docs.microsoft.com/aspnet/core/fundamentals/dependency-injection)

Dependency injection with Hot Chocolate works almost the same as with a regular ASP.NET Core application. Nothing changes about how you add services to the DI container.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddSingleton<MySingletonService>()
    .AddScoped<MyScopedService>()
    .AddTransient<MyTransientService>();
```

# Implicit Service Injection

In v16, Hot Chocolate automatically recognizes types registered as services in the DI container and injects them into resolver method parameters without requiring any attribute. This works similarly to [Minimal APIs parameter binding](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/parameter-binding).

When the execution engine encounters a resolver parameter whose type is registered in the DI container, it resolves the service automatically. You do not need to apply the `[Service]` attribute.

# Resolver Injection

Inject dependencies into your resolvers as method arguments. This is the recommended approach.

[Learn more about why constructor injection into GraphQL types is a bad idea](#constructor-injection)

Injecting dependencies at the method level has several benefits:

- The execution engine can optimize the resolver and adjust the execution strategy based on the needs of a specific service.
- Refactoring (moving the resolver method between classes) becomes easier because the resolver does not depend on its outer class.

In the following example, `BookService` is injected automatically when it is registered as a service in the DI container:

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

## Default Scope

By default, scoped services are scoped to the resolver for queries and DataLoaders, and to the current request for mutations. This means that each execution of a query or DataLoader that accepts a scoped service receives a **separate** instance, avoiding threading issues with services that do not support multi-threading (for example, Entity Framework DbContexts). Since mutations are executed sequentially, they receive the **same** request-scoped instance.

You can change these defaults globally:

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
<Schema>

Take a look at the implementation-first or code-first example.

</Schema>
</ExampleTabs>

# Application Services in Schema Services

Hot Chocolate maintains a separate internal service provider for schema services. If you need application services to be available within schema-level components (such as diagnostic event listeners, error filters, or interceptors), you must cross-register them using `AddApplicationService<T>()`:

```csharp
builder.Services.AddSingleton<MyService>();

builder.Services
    .AddGraphQLServer()
    .AddApplicationService<MyService>()
    .AddDiagnosticEventListener<MyDiagnosticEventListener>();
```

Services registered via `AddApplicationService<T>()` are resolved once during schema initialization from the application service provider and registered as singletons in the schema service provider.

The following configuration APIs require `AddApplicationService<T>()` for any application services they depend on:

- `AddHttpRequestInterceptor`
- `AddSocketSessionInterceptor`
- `AddErrorFilter`
- `AddDiagnosticEventListener`
- `AddOperationCompilerOptimizer`
- `AddTransactionScopeHandler`
- `AddRedisOperationDocumentStorage`
- `AddAzureBlobStorageOperationDocumentStorage`
- `AddInstrumentation` with a custom `ActivityEnricher`

> Note: Service injection into resolvers is not affected by this. Resolvers continue to use the application service provider directly.

# Constructor Injection

When starting out with Hot Chocolate you might be inclined to inject dependencies into your GraphQL type definitions using the constructor.

You should <strong style="color: red">avoid</strong> doing this, because:

- GraphQL type definitions are singletons and your injected dependency will also become a singleton.
- Access to this dependency cannot be synchronized by Hot Chocolate during request execution.

This does not apply within your own dependencies. Your `ServiceA` class can still inject `ServiceB` through the constructor.

When you need to access dependency injection services in your resolvers, use the [method-level dependency injection approach](#resolver-injection) described above.

# Keyed Services

A keyed service registered like this:

```csharp
builder.Services.AddKeyedScoped<BookService>("bookService");
```

...can be accessed in your resolver with the `[Service]` attribute specifying the key:

<ExampleTabs>
<Implementation>

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
<Schema>

Take a look at the implementation-first or code-first example.

</Schema>
</ExampleTabs>

# Switching the Service Provider

While Hot Chocolate's internals rely on Microsoft's dependency injection container, you are not required to manage your own dependencies using this container. By default, Hot Chocolate uses the request-scoped [`HttpContext.RequestServices`](https://docs.microsoft.com/dotnet/api/microsoft.aspnetcore.http.httpcontext.requestservices) `IServiceProvider` to provide services to your resolvers.

You can switch out the service provider used for GraphQL requests, as long as your DI container implements the [`IServiceProvider`](https://docs.microsoft.com/dotnet/api/system.iserviceprovider) interface.

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
builder.Services
    .AddGraphQLServer()
    .AddHttpRequestInterceptor<HttpRequestInterceptor>()
    .AddSocketSessionInterceptor<SocketSessionInterceptor>();
```

[Learn more about interceptors](/docs/hotchocolate/v16/server/interceptors)

# Next Steps

- [Interceptors](/docs/hotchocolate/v16/server/interceptors) for setting request-scoped state and services.
- [Global State](/docs/hotchocolate/v16/server/global-state) for sharing per-request data between resolvers.
- [Migrate from v15 to v16](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16#clearer-separation-between-schema-and-application-services) for the full migration details on schema vs application services.
