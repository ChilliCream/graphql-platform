---
title: "Subscriptions"
---

> We are still working on the documentation for Strawberry Shake, so help us by finding typos, missing things, or write some additional docs with us.

Subscriptions in GraphQL represent real-time events that are represented as a stream of query responses. In most cases, subscriptions are used over WebSockets but can also be used with other protocols.

> For transport questions, please head over to the [network docs](/docs/strawberryshake/v15/networking).

GraphQL subscriptions can be used through reactive APIs like queries. Instead of a single network request, the store will subscribe to the GraphQL response stream and update the store for each new result.

# Setup

> This section will be based on the [getting started tutorial](/docs/strawberryshake/v15/get-started).

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
