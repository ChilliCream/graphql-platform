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

## Delegate-based interceptor

For lightweight interception logic, you can register a delegate instead of creating a class. The delegate receives the `HttpContext`, the `IRequestExecutor`, the `OperationRequestBuilder`, and a `CancellationToken`:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddHttpRequestInterceptor(
        async (context, executor, builder, ct) =>
        {
            var tenantId = context.Request.Headers["X-Tenant-Id"]
                .FirstOrDefault();

            if (tenantId is not null)
            {
                builder.SetProperty("TenantId", tenantId);
            }
        });
```

The delegate-based interceptor extends `DefaultHttpRequestInterceptor`. Your delegate runs first, and then the default `OnCreateAsync` implementation executes, which sets up the `ClaimsPrincipal` and other global state. This means you can safely set properties in the delegate without worrying about missing base behavior.

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
        ISocketSession session, IOperationMessagePayload connectionInitMessage,
        CancellationToken cancellationToken)
    {
        return base.OnConnectAsync(session, connectionInitMessage,
            cancellationToken);
    }

    public override ValueTask OnRequestAsync(ISocketSession session,
        string operationSessionId, OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        return base.OnRequestAsync(session, operationSessionId,
            requestBuilder, cancellationToken);
    }

    public override ValueTask<OperationResult> OnResultAsync(
        ISocketSession session, string operationSessionId,
        OperationResult result, CancellationToken cancellationToken)
    {
        return base.OnResultAsync(session, operationSessionId, result,
            cancellationToken);
    }

    public override ValueTask OnCompleteAsync(ISocketSession session,
        string operationSessionId, CancellationToken cancellationToken)
    {
        return base.OnCompleteAsync(session, operationSessionId,
            cancellationToken);
    }

    public override ValueTask<IReadOnlyDictionary<string, object?>?> OnPingAsync(
        ISocketSession session, IOperationMessagePayload pingMessage,
        CancellationToken cancellationToken)
    {
        return base.OnPingAsync(session, pingMessage, cancellationToken);
    }

    public override ValueTask OnPongAsync(ISocketSession session,
        IOperationMessagePayload pongMessage,
        CancellationToken cancellationToken)
    {
        return base.OnPongAsync(session, pongMessage, cancellationToken);
    }

    public override ValueTask OnCloseAsync(ISocketSession session,
        CancellationToken cancellationToken)
    {
        return base.OnCloseAsync(session, cancellationToken);
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

This method is invoked **once** when a client sends a `connection_init` message to initialize a WebSocket connection. You can accept or reject specific connection requests.

```csharp
public override ValueTask<ConnectionStatus> OnConnectAsync(
    ISocketSession session, IOperationMessagePayload connectionInitMessage,
    CancellationToken cancellationToken)
{
    if (condition)
    {
        return new(ConnectionStatus.Reject("Connection rejected for X reason!"));
    }

    return new(ConnectionStatus.Accept());
}
```

The `connectionInitMessage` payload contains any data the client sent with the `connection_init` message. If a client sends a payload (for example, an auth token), you can access it:

```csharp
public override ValueTask<ConnectionStatus> OnConnectAsync(
    ISocketSession session, IOperationMessagePayload connectionInitMessage,
    CancellationToken cancellationToken)
{
    if (connectionInitMessage.As<Dictionary<string, object?>>()
        ?.TryGetValue("authToken", out var token) == true)
    {
        // Validate token ...
    }

    return new(ConnectionStatus.Accept());
}
```

## OnRequestAsync

This method is invoked for **every** GraphQL request a client sends using the already established WebSocket connection. It receives the `operationSessionId` assigned by the client for this particular operation. It is a good place to set global state variables, extend the identity of the authenticated user, or perform any per-request work.

```csharp
public override ValueTask OnRequestAsync(ISocketSession session,
    string operationSessionId, OperationRequestBuilder requestBuilder,
    CancellationToken cancellationToken)
{
    return base.OnRequestAsync(session, operationSessionId, requestBuilder,
        cancellationToken);
}
```

> Warning: Always invoke `base.OnRequestAsync`. The default implementation adds dependency injection services and important global state variables such as the `ClaimsPrincipal`. Skipping this call can lead to unexpected issues.

Most of the configuration is done through the `OperationRequestBuilder` injected as an argument to this method.

[Learn more about the OperationRequestBuilder](#operationrequestbuilder)

If you want to fail the request before it is executed, throw a `GraphQLException`. The middleware translates this exception to a proper GraphQL error response for the client.

## OnResultAsync

This method is invoked before each result is serialized and sent to the client. You can modify the `OperationResult` or replace it entirely. For subscriptions, this method is called for every emitted result.

```csharp
public override ValueTask<OperationResult> OnResultAsync(
    ISocketSession session, string operationSessionId,
    OperationResult result, CancellationToken cancellationToken)
{
    // Inspect or modify the result before it is sent to the client.
    return base.OnResultAsync(session, operationSessionId, result,
        cancellationToken);
}
```

The default implementation returns the result unmodified.

## OnCompleteAsync

This method is invoked when an operation finishes execution. For subscriptions, it fires when the subscription stream completes or is stopped by the client. This method is guaranteed to run even if the operation fails or the connection closes, making it a reliable place for cleanup logic.

```csharp
public override ValueTask OnCompleteAsync(ISocketSession session,
    string operationSessionId, CancellationToken cancellationToken)
{
    // Clean up resources for the completed operation.
    return base.OnCompleteAsync(session, operationSessionId,
        cancellationToken);
}
```

> Note: The cancellation token may already be canceled if the connection closed unexpectedly. Handle `OperationCanceledException` if your cleanup logic calls async APIs.

## OnPingAsync

This method is invoked when the server receives a `ping` message from the client. Return a dictionary of key-value pairs to include as the payload in the corresponding `pong` response, or return `null` for an empty pong.

```csharp
public override ValueTask<IReadOnlyDictionary<string, object?>?> OnPingAsync(
    ISocketSession session, IOperationMessagePayload pingMessage,
    CancellationToken cancellationToken)
{
    return new(new Dictionary<string, object?>
    {
        ["serverTime"] = DateTimeOffset.UtcNow.ToString("O"),
    });
}
```

## OnPongAsync

This method is invoked when the server receives a `pong` message from the client in response to a server-initiated ping. You can use it for latency tracking or connection health monitoring.

```csharp
public override ValueTask OnPongAsync(ISocketSession session,
    IOperationMessagePayload pongMessage,
    CancellationToken cancellationToken)
{
    // Log round-trip time or update health metrics.
    return base.OnPongAsync(session, pongMessage, cancellationToken);
}
```

## OnCloseAsync

This method is invoked once when the client closes the WebSocket connection or the connection is terminated in any other way.

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
- [Dependency Injection](/docs/hotchocolate/v16/resolvers-and-data/dependency-injection) for details on service injection and switching providers.
- [Introspection](/docs/hotchocolate/v16/securing-your-api/introspection) for controlling introspection on a per-request basis.
