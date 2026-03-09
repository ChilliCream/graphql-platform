---
title: "Networking"
---

Strawberry Shake supports multiple network protocols to communicate with your GraphQL server. Each transport integration is represented by a specific NuGet package to keep your client size as small as possible.

# Protocols

| Transport | Protocol                                                                                                          | Package                                                                                                     | Strawberry Shake Version |
| --------- | ----------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------- | ------------------------ |
| HTTP      | [GraphQL over HTTP](https://github.com/michaelstaib/graphql-over-http)                                            | [StrawberryShake.Transport.Http](https://www.nuget.org/packages/StrawberryShake.Transport.Http)             | 11.1                     |
| WebSocket | [subscriptions-transport-ws](https://github.com/apollographql/subscriptions-transport-ws/blob/master/PROTOCOL.md) | [StrawberryShake.Transport.WebSockets](https://www.nuget.org/packages/StrawberryShake.Transport.WebSockets) | 11.1                     |
| WebSocket | [graphql-transport-ws](https://github.com/graphql/graphql-over-http/pull/140)                                     | StrawberryShake.Transport.WebSockets                                                                        | 12.0                     |
| SignalR   | GraphQL over SignalR                                                                                              | StrawberryShake.Transport.SignalR                                                                           | 12.0                     |
| gRPC      | GraphQL over gRPC                                                                                                 | StrawberryShake.Transport.gRPC                                                                              | 12.0                     |
| InMemory  | Hot Chocolate In-Memory                                                                                           | [StrawberryShake.Transport.InMemory](https://www.nuget.org/packages/StrawberryShake.Transport.InMemory)     | 11.1                     |

# Transport Profiles

In order to have a small client size and generate the optimized client for your use-case Strawberry Shake uses transport profiles. By default Strawberry Shake will generate a client that uses `GraphQL over HTTP` for queries and mutations and `graphql-transport-ws` for subscriptions. Meaning if you are only using queries and mutations you need to add the package `StrawberryShake.Transport.Http`.

There are cases in which we want to define specialized transport profiles where we define for each request type a specific transport. You can define those transport profiles in your `.graphqlrc.json`.

The following `.graphqlrc.json` can be copied into our getting started example and will create two transport profiles. The first is called `Default` and matches the internal default. It will use `GraphQL over HTTP` by default and use `graphql-transport-ws` for subscriptions. The second profile is called `WebSocket` and will also use `GraphQL over HTTP` by default but for mutations and subscriptions it will use `graphql-transport-ws`.

```json
{
  "schema": "schema.graphql",
  "documents": "**/*.graphql",
  "extensions": {
    "strawberryShake": {
      "name": "ConferenceClient",
      "namespace": "Demo.GraphQL",
      "url": "http://localhost:5050/graphql",
      "dependencyInjection": true,
      "transportProfiles": [
        {
          "name": "Default",
          "default": "HTTP",
          "subscription": "WebSocket"
        },
        {
          "name": "WebSocket",
          "default": "HTTP",
          "mutation": "WebSocket",
          "subscription": "WebSocket"
        }
      ]
    }
  }
}
```

The generator will generate the dependency injection code with a new enum called `ConferenceClientProfileKind`. The name of the enum is inferred from your GraphQL client name. The enum can be passed into the dependency injection setup method and allows you to switch between the two transport profiles through configuration.

```csharp
builder.Services
    .AddConferenceClient(profile: ConferenceClientProfileKind.WebSocket)
    .ConfigureHttpClient(client => client.BaseAddress = new Uri("http://localhost:5050/graphql"))
    .ConfigureWebSocketClient(client => client.Uri = new Uri("ws://localhost:5050/graphql"));
```
