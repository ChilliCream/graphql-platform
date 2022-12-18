---
title: "Authentication"
---

To access a protected API with Strawberry Shake, you need to proof the user's identity to the server.
Each network protocol of Strawberry Shake handles authentication a bit different.

# HTTP

Strawberry Shake uses the `HttpClientFactory` to generate a `HttpClient` on every request.
You can either register a `HttpClient` directly on the `ServiceCollection` or use the `ConfigureHttpClient` method on the client builder.

## ConfigureHttpClient

The generated extension method to register the client on the service collection, returns a builder that can be used to configure the http client.

```csharp
 services
    .AddConferenceClient()
    .ConfigureHttpClient(client =>
    {
        client.BaseAddress =
            new Uri("https://workshop.chillicream.com/graphql/");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "Your Oauth token");
    });
```

There is an overload of the `ConfigureHttpClient` method that provides access to the `IServiceProvider`, in case the access token is stored there.

```csharp
services
    .AddConferenceClient()
    .ConfigureHttpClient((serviceProvider, client) =>
    {
        var token = serviceProvider.GetRequiredService<ISomeService>().Token;
    });
```

The second parameter of `ConfigureHttpClient` allows direct access to the `HttpClientBuilder`. Use this delegate to register extensions like Polly.

```csharp
services
    .AddConferenceClient()
    .ConfigureHttpClient(
        client => { /*...*/ },
        builder => builder.AddPolly());

```

## HttpClientFactory

In case you want to configure the `HttpClient` directly on the `ServiceCollection`, Strawberry Shake generates you a property `ClientName`, that you can use to set the correct name for the client.

```csharp
services.AddHttpClient(
    ConferenceClient.ClientName,
    client => client.BaseAddress =
        new Uri("https://workshop.chillicream.com/graphql/"));

services.AddConferenceClient();
```

# Websockets

There are three common ways to do authentication a request over a web socket. You can either specify the authentication headers, use cookies or send the access token with the first message over the socket.
Similar to the `HttpClient`, you can configure the a web socket client over the client builder or the `ServiceCollection`.

Strawberry Shake uses a `IWebSocketClient` that provides a similar interface as the `HttpClient` has.

## ConfigureWebsocketClient

You can configure the web socket client directly on the client builder after you registered it on the service collection.

```csharp
services
    .AddConferenceClient()
    .ConfigureWebSocketClient(client =>
    {
        client.Uri = new Uri("ws://localhost:" + port + "/graphql");
        client.Socket.Options.SetRequestHeader("Authorization", "Bearer ...");
    });
```

You can also access the `IServiceProvider` with the following overload:

```csharp
services
    .AddConferenceClient()
    .ConfigureWebSocketClient((serviceProvider, client) =>
    {
        var token = serviceProvider.GetRequiredService<ISomeService>().Token;
    });
```

The second parameter of the `ConfigureWebSocketClient` method, can be used to access the `IWebSocketClientBuilder`

```csharp
services
    .AddConferenceClient()
    .ConfigureWebSocketClient(
        (serviceProvider, client) =>
            {
                var token = serviceProvider.GetRequiredService<ISomeService>().Token;
            },
            builder =>
                builder.ConfigureConnectionInterceptor<CustomConnectionInterceptor>());
```

## WebSocketClientFactory

If you prefer to use the `ServiceCollection` to configure your web socket, you can use the `AddWebSocketClient` method. Strawberry Shake generates a `ClientName` property, on each client. You can use this, to easily specify the correct name of the client.

```csharp
services
    .AddWebSocketClient(
        ConferenceClient.ClientName,
        client => client.Uri =
            new Uri("wss://workshop.chillicream.cloud/graphql/"));

services.AddConferenceClient();
```

## IWebSocketClient

On a `IWebSocketClient` you can configure the `Uri` of your endpoint. You can also directly set a `ISocketConnectionInterceptor` on the client, to intercept the connection and configure the initial payload. You do also have access to the underlying `ClientWebSocket` to configure headers or cookies.

```csharp
IWebSocketClient client;
client.Uri = new Uri("wss://workshop.chillicream.cloud/graphql/");
client.Socket.Options.SetRequestHeader("Authorization", "Bearer …");
client.ConnectionInterceptor = new CustomConnectionInterceptor();
```

## Initial payload

In JavaScript it is not possible to add headers to a web socket. Therefor many GraphQL server do not use HTTP headers for the authentication of web sockets. Instead, they send the authentication token with the first payload to the server.

You can specify create this payload with a `ISocketConnectionInterceptor`

```csharp
public class CustomConnectionInterceptor
    : ISocketConnectionInterceptor
{
    // the object returned by this method, will be included in the connection initialization message
    public ValueTask<object?> CreateConnectionInitPayload(
        ISocketProtocol protocol,
        CancellationToken cancellationToken)
    {
        return new ValueTask<object?>(
            new Dictionary<string, string> { ["authToken"] = "..." });
    }
}
```

You can set the connection interceptor directly on the `IWebSocketClient` or on the `IWebSocketClientBuilder`.
