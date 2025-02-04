---
title: "Subscriptions"
---

> We are still working on the documentation for Strawberry Shake, so help us by finding typos, missing things, or write some additional docs with us.

Subscriptions in GraphQL represent real-time events that are represented as a stream of query responses. In most cases, subscriptions are used over WebSockets but can also be used with other protocols.

> For transport questions, please head over to the [network docs](/docs/strawberryshake/v13/networking).

GraphQL subscriptions can be used through reactive APIs like queries. Instead of a single network request, the store will subscribe to the GraphQL response stream and update the store for each new result.

# Setup

> This section will be based on the [getting started tutorial](/docs/strawberryshake/v13/get-started).

To create a subscription, we start with everything in Strawberry Shake by creating a GraphQL file.

1. Create a new GraphQL file and call it `OnSessionUpdated` with the following content.

```graphql
subscription OnSessionUpdated {
  onSessionScheduled {
    title
  }
}
```

2. Add the [StrawberryShake.Transport.WebSockets](https://www.nuget.org/packages/StrawberryShake.Transport.WebSockets) package to your project.

3. Build your project so that the code-generator kicks in.

4. Configure the transport settings for the WebSocket.

```csharp
builder.Services
  .AddConferenceClient()
  .ConfigureHttpClient(client => client.BaseAddress = new Uri("http://localhost:5050/graphql"))
  .ConfigureWebSocketClient(client => client.Uri = new Uri("ws://localhost:5050/graphql"));
```

5. A subscription can now be used like any other query by subscribing to it.

```csharp
var session =
    ConferenceClient
        .OnSessionUpdated
        .Watch()
        .Subscribe(result =>
        {
            // do something with the result
        });
```

Remember, `session` is an `IDisposable` and will stop receiving events when `Dispose` is invoked.

# Websocket Authentication

When working with GraphQL subscriptions over WebSockets, you may want to authenticate incoming WebSocket connections using JSON Web Tokens. Normally, HTTP headers are sent with each request for standard APIs, but WebSockets behave differently. After a successful HTTP handshake, the protocol is "upgraded" to WebSockets, and additional headers cannot be easily injected for subsequent messages.

Instead, the recommended approach is to send your token via the `connection_init` message when the WebSocket connection is first established. Hot Chocolate allows you to intercept this initial message, extract the token, and then authenticate the user in a way similar to standard HTTP requests.

An example implementation of this approach can be found in the [Hot Chocolate Examples repository](https://github.com/ChilliCream/hotchocolate-examples/tree/master/misc/WebsocketAuthentication).

## Why a Special Approach for WebSockets

- **Single HTTP Handshake**: A WebSocket connection is established once. After that, you cannot update HTTP headers on the same connection.
- **`connection_init` Payload**: GraphQL subscription clients send a `connection_init` message when establishing the subscription. This payload can include extra properties (e.g., `authorization`), which Hot Chocolate can use for authentication.
- **Long-Lived Connections**: Because WebSockets are persistent, tokens might remain valid for the entire duration of the connection. It is advisable to ensure that you handle token expiration appropriately-often by closing the connection if security policies require it.

## Core Concepts

1. **Stub (or "Skip") Authentication Scheme**  
   The initial WebSocket upgrade request is directed to a "stub" authentication scheme that simply indicates "no authentication result" for upgrade requests. This prevents the request from failing before you can intercept and handle the token manually.

2. **Forwarding the Default Scheme**  
   In standard HTTP scenarios, the default scheme (e.g., JWT bearer) is used to authenticate. However, if the request is recognized as a WebSocket upgrade, the framework forwards it to the "stub" scheme first. That way, you don’t attempt to validate a token at the moment of the upgrade handshake.

3. **Intercepting `connection_init`**  
   Once the WebSocket is established, the client sends `connection_init` containing authentication data. A custom `SocketSessionInterceptor` (or similar) reads the token from `connection_init` (e.g., under a key like `authorization`), stores it in the `HttpContext`, and triggers a fresh authentication attempt-this time using the real JWT bearer scheme.

4. **Hot Chocolate Integration**  
   Hot Chocolate's subscription middleware allows you to plug into the subscription lifecycle. By customizing the session interceptor (`OnConnectAsync`), you can decide whether to accept or reject the connection based on successful authentication.

## Testing the Flow

1. **Open Nitro**  
   Use a local instance of Nitro (e.g., `https://localhost:5095/graphql`) to send GraphQL queries and subscriptions.

2. **Retrieve an Access Token**  
   Request a token from your `/token` endpoint. This endpoint should return a valid JWT that is trusted by your API.

3. **Configure Nitro**

   - In Nitro, open the **Settings** of your document / api.
   - Under **Authentication**, choose **Bearer Token** and paste your JWT.
   - Nitro will automatically include the token in the `connection_init` message under an `authorization` parameter when opening a WebSocket connection.

4. **Run Your Subscription**  
   Execute the subscription query of your choice. For example:

   ```graphql
   subscription {
     onTimedEvent {
       count
       isAuthenticated
     }
   }
   ```

   The server-side resolver can check `isAuthenticated` to demonstrate whether the current user is authenticated (based on the token you provided).
