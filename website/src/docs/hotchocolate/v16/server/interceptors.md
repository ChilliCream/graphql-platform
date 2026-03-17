---
title: Interceptors
---

Interceptors let you hook into protocol-specific events. You can intercept an incoming HTTP request or handle a client connecting to or disconnecting from a WebSocket session.

# IHttpRequestInterceptor

Each GraphQL request sent via HTTP can be intercepted using an `IHttpRequestInterceptor` before it is executed. By default, Hot Chocolate registers a `DefaultHttpRequestInterceptor` for this purpose.

Create a new class inheriting from `DefaultHttpRequestInterceptor` to provide your own logic for request interception:

```csharp
public class HttpRequestInterceptor : DefaultHttpRequestInterceptor
{
    public override ValueTask OnCreateAsync(HttpContext context,
        IRequestExecutor requestExecutor, OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        return base.OnCreateAsync(context, requestExecutor, requestBuilder,
            cancellationToken);
    }
}
```

Register your custom `HttpRequestInterceptor`:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddHttpRequestInterceptor<HttpRequestInterceptor>();
```

If needed, you can inject services into your custom `HttpRequestInterceptor` using its constructor.

## OnCreateAsync

This method is invoked for **every** GraphQL request sent via HTTP. It is a good place to set global state variables, extend the identity of the authenticated user, or perform any per-request work.

```csharp
public override ValueTask OnCreateAsync(HttpContext context,
    IRequestExecutor requestExecutor, OperationRequestBuilder requestBuilder,
    CancellationToken cancellationToken)
{
    return base.OnCreateAsync(context, requestExecutor, requestBuilder,
        cancellationToken);
}
```

> Warning: Always invoke `base.OnCreateAsync`. The default implementation adds dependency injection services and important global state variables such as the `ClaimsPrincipal`. Skipping this call can lead to unexpected issues.

Most of the configuration is done through the `OperationRequestBuilder` injected as an argument to this method.

[Learn more about the OperationRequestBuilder](#operationrequestbuilder)

If you want to fail the request before it is executed, throw a `GraphQLException`. The middleware translates this exception to a proper GraphQL error response for the client.

# ISocketSessionInterceptor

Each GraphQL request sent over WebSockets can be intercepted using an `ISocketSessionInterceptor` before it is executed. Since WebSockets are long-lived connections, you can also intercept specific lifecycle events such as connecting and disconnecting. By default, Hot Chocolate registers a `DefaultSocketSessionInterceptor` for this purpose.

Create a new class inheriting from `DefaultSocketSessionInterceptor` to provide your own logic:

```csharp
public class SocketSessionInterceptor : DefaultSocketSessionInterceptor
{
    public override ValueTask<ConnectionStatus> OnConnectAsync(
        ISocketConnection connection, InitializeConnectionMessage message,
        CancellationToken cancellationToken)
    {
        return base.OnConnectAsync(connection, message, cancellationToken);
    }

    public override ValueTask OnRequestAsync(ISocketConnection connection,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        return base.OnRequestAsync(connection, requestBuilder,
            cancellationToken);
    }

    public override ValueTask OnCloseAsync(ISocketConnection connection,
        CancellationToken cancellationToken)
    {
        return base.OnCloseAsync(connection, cancellationToken);
    }
}
```

Register your custom `SocketSessionInterceptor`:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddSocketSessionInterceptor<SocketSessionInterceptor>();
```

If needed, you can inject services into your custom `SocketSessionInterceptor` using its constructor.

You do not have to override every method shown above. Override only the ones you need.

## OnConnectAsync

This method is invoked **once** when a client attempts to initialize a WebSocket connection. You can accept or reject specific connection requests.

```csharp
public async override ValueTask<ConnectionStatus> OnConnectAsync(
    ISocketConnection connection, InitializeConnectionMessage message,
    CancellationToken cancellationToken)
{
    if (condition)
    {
        return ConnectionStatus.Reject("Connection rejected for X reason!");
    }

    return ConnectionStatus.Accept();
}
```

You also get access to the `InitializeConnectionMessage`. If a client sends a payload with this message (for example, an auth token), you can access it:

```csharp
public async override ValueTask<ConnectionStatus> OnConnectAsync(
    ISocketConnection connection, InitializeConnectionMessage message,
    CancellationToken cancellationToken)
{
    if (message.Payload?.TryGetValue("MyKey", out object? value) == true)
    {
        // ...
    }

    return ConnectionStatus.Accept();
}
```

## OnRequestAsync

This method is invoked for **every** GraphQL request a client sends using the already established WebSocket connection. It is a good place to set global state variables, extend the identity of the authenticated user, or perform any per-request work.

```csharp
public override ValueTask OnRequestAsync(ISocketConnection connection,
    OperationRequestBuilder requestBuilder, CancellationToken cancellationToken)
{
    return base.OnRequestAsync(connection, requestBuilder, cancellationToken);
}
```

> Warning: Always invoke `base.OnRequestAsync`. The default implementation adds dependency injection services and important global state variables such as the `ClaimsPrincipal`. Skipping this call can lead to unexpected issues.

Most of the configuration is done through the `OperationRequestBuilder` injected as an argument to this method.

[Learn more about the OperationRequestBuilder](#operationrequestbuilder)

If you want to fail the request before it is executed, throw a `GraphQLException`. The middleware translates this exception to a proper GraphQL error response for the client.

## OnCloseAsync

This method is invoked once a client closes the WebSocket connection or the connection is terminated in any other way.

# OperationRequestBuilder

The `OperationRequestBuilder` lets you influence the execution of a GraphQL request.

It has many capabilities, but most are used internally. The following sections cover the methods that are most relevant to you as a consumer.

## Properties

You can set `Properties`, also called Global State, on the `OperationRequestBuilder`. These can then be referenced in middleware, field resolvers, and other components.

[Learn more about Global State](/docs/hotchocolate/v16/server/global-state)

### SetProperty

`SetProperty` lets you add a key-value pair where the key is a `string` and the value can be anything (`object`).

```csharp
requestBuilder.SetProperty("name", "value");
requestBuilder.SetProperty("name", 123);
requestBuilder.SetProperty("name", new User { Name = "Joe" });
```

There is also `TrySetProperty`, which adds the property only if it has not been added yet:

```csharp
requestBuilder.TryAddProperty("name", 123);
```

### SetProperties

`SetProperties` lets you set all properties at once.

```csharp
var properties = new Dictionary<string, object>
{
    { "name", "value" }
};

requestBuilder.SetProperties(properties);
```

> Warning: This overwrites all previous properties. This is especially problematic when called after the default implementation of an interceptor has added properties.

## SetServices

`SetServices` lets you add an `IServiceProvider` to use for dependency injection during the request.

```csharp
var provider = new ServiceCollection()
    .AddSingleton<ExampleService>()
    .BuildServiceProvider();

requestBuilder.SetServices(provider);
```

There is also `TrySetServices`, which sets the `IServiceProvider` only if it has not been set yet.

## AllowIntrospection

If you have disabled introspection globally, `AllowIntrospection` lets you enable it for specific requests.

```csharp
requestBuilder.AllowIntrospection();
```

# Troubleshooting

## Interceptor not being called

Verify that you registered the interceptor using `AddHttpRequestInterceptor<T>()` or `AddSocketSessionInterceptor<T>()` on the `IRequestExecutorBuilder`. If the interceptor depends on application services, you may need to register them with `AddApplicationService<T>()`.

## Missing ClaimsPrincipal in resolvers

Always call `base.OnCreateAsync` (for HTTP) or `base.OnRequestAsync` (for WebSocket) in your interceptor. The base implementation is responsible for setting up the `ClaimsPrincipal` and other global state.

# Next Steps

- [Global State](/docs/hotchocolate/v16/server/global-state) for sharing per-request data between resolvers.
- [Dependency Injection](/docs/hotchocolate/v16/server/dependency-injection) for details on service injection and switching providers.
- [Introspection](/docs/hotchocolate/v16/server/introspection) for controlling introspection on a per-request basis.
