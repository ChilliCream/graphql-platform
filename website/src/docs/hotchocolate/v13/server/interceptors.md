---
title: Interceptors
---

Interceptors allow us to hook into protocol-specific events. We can, for example, intercept an incoming HTTP request or a client connecting or disconnecting a WebSocket session.

# IHttpRequestInterceptor

Each GraphQL request sent via HTTP can be intercepted using an `IHttpRequestInterceptor` before it is being executed. Per default Hot Chocolate registers a `DefaultHttpRequestInterceptor` for this purpose.

We can create a new class inheriting from `DefaultHttpRequestInterceptor` to provide our own logic for request interception.

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

Once we have defined our custom `HttpRequestInterceptor`, we also have to register it.

```csharp
services
    .AddGraphQLServer()
    .AddHttpRequestInterceptor<HttpRequestInterceptor>();
```

If needed, we can also inject services into our custom `HttpRequestInterceptor` using its constructor.

## OnCreateAsync

This method is invoked for **every** GraphQL request sent via HTTP. It is a great place to set global state variables, extend the identity of the authenticated user or do anything that we want to do on a per-request basis.

```csharp
public override ValueTask OnCreateAsync(HttpContext context,
    IRequestExecutor requestExecutor, OperationRequestBuilder requestBuilder,
    CancellationToken cancellationToken)
{
    return base.OnCreateAsync(context, requestExecutor, requestBuilder,
        cancellationToken);
}
```

> Warning: `base.OnCreateAsync` should always be invoked, since the default implementation takes care of adding the dependency injection services as well as some important global state variables, such as the `ClaimsPrinicpal`. Not doing this can lead to unexpected issues.

Most of the configuration will be done through the `OperationRequestBuilder`, injected as argument to this method.

[Learn more about the OperationRequestBuilder](#operationrequestbuilder)

If we want to fail the request, before it is being executed, we can throw a `GraphQLException`. The middleware will then translate this exception to a proper GraphQL error response for the client.

# ISocketSessionInterceptor

Each GraphQL request sent over WebSockets can be intercepted using an `ISocketSessionInterceptor` before it is being executed. Since WebSockets are long lived connections, we can also intercept specific lifecycle events, such as connecting or disconnecting. Per default Hot Chocolate registers a `DefaultSocketSessionInterceptor` for this purpose.

We can create a new class inheriting from `DefaultSocketSessionInterceptor` to provide our own logic for request / lifecycle interception.

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

Once we have defined our custom `SocketSessionInterceptor`, we also have to register it.

```csharp
services
    .AddGraphQLServer()
    .AddSocketSessionInterceptor<SocketSessionInterceptor>();
```

If needed, we can also inject services into our custom `HttpRequestInterceptor` using its constructor.

We do not have to override every method shown above, we can also only override the ones we are interested in.

## OnConnectAsync

This method is invoked **once**, when a client attempts to initialize a WebSocket connection. We have the option to either accept or reject specific connection requests.

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

We also get access to the `InitializeConnectionMessage`. If a client sends a payload with this message, for example an auth token, we can access the `Payload` like the following.

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

This method is invoked for **every** GraphQL request a client sends using the already established WebSocket connection. It is a great place to set global state variables, extend the identity of the authenticated user or anything that we want to do on a per-request basis.

```csharp
public override ValueTask OnRequestAsync(ISocketConnection connection,
    OperationRequestBuilder requestBuilder, CancellationToken cancellationToken)
{
    return base.OnRequestAsync(connection, requestBuilder, cancellationToken);
}
```

> Warning: `base.OnRequestAsync` should always be invoked, since the default implementation takes care of adding the dependency injection services as well as some important global state variables, such as the `ClaimsPrinicpal`. Not doing this can lead to unexpected issues.

Most of the configuration will be done through the `OperationRequestBuilder`, injected as argument to this method.

[Learn more about the OperationRequestBuilder](#operationrequestbuilder)

If we want to fail the request, before it is being executed, we can throw a `GraphQLException`. The middleware will then translate this exception to a proper GraphQL error response for the client.

## OnCloseAsync

This method is invoked, once a client closes the WebSocket connection or the connection is terminated in any other way.

# OperationRequestBuilder

The `OperationRequestBuilder` allows us to influence the execution of a GraphQL request.

It has many capabilities, but most of them are only used internally. In the following we are going to cover the methods that are most relevant to us as consumers.

## Properties

We can set `Properties`, also called Global State, on the `OperationRequestBuilder`, which can then be referenced in middleware, field resolvers, etc.

[Learn more about Global State](/docs/hotchocolate/v13/server/global-state)

### SetProperty

`SetProperty` allows us to add a key-value pair, where the key is a `string` and the value can be anything, i.e. an `object`.

```csharp
requestBuilder.SetProperty("name", "value");
requestBuilder.SetProperty("name", 123);
requestBuilder.SetProperty("name", new User { Name = "Joe" });
```

There is also `TrySetProperty`, which only adds the property, if it hasn't yet been added.

```csharp
requestBuilder.TryAddProperty("name", 123);
```

### SetProperties

`SetProperties` allows us to set all properties at once.

```csharp
var properties = new Dictionary<string, object>
{
    { "name", "value" }
};

requestBuilder.SetProperties(properties);
```

> Warning: This overwrites all previous properties, which is especially catastrophic, when called after the default implementation of an interceptor has added properties.

## SetServices

`SetServices` allows us to add an `IServiceProvider` which should be used for dependency injection during the request.

```csharp
var provider = new ServiceCollection()
                .AddSingleton<ExampleService>()
                .BuildServiceProvider();

requestBuilder.SetServices(provider);
```

There is also `TrySetServices`, which only sets the `IServiceProvider`, if it hasn't yet been set.

## AllowIntrospection

If we have disabled introspection globally, `AllowIntrospection` allows us to enable it for specific requests.

```csharp
requestBuilder.AllowIntrospection();
```

## SkipComplexityAnalysis

When using the [operation complexity feature](/docs/hotchocolate/v13/security/operation-complexity), we can skip the complexity analysis for specific requests.

```csharp
requestBuilder.SkipComplexityAnalysis();
```

## SetMaximumAllowedComplexity

When using the [operation complexity feature](/docs/hotchocolate/v13/security/operation-complexity), we can overwrite the global complexity limit for specific requests.

```csharp
requestBuilder.SetMaximumAllowedComplexity(5000);
```
