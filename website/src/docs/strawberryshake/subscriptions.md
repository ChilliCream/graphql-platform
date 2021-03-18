---
title: "Subscriptions"
---

Subscriptions in GraphQL represent real-time events that are represented as a stream of query responses. In most cases subscriptions are used over WebSockets but can also used with other protocols. For transport questions please head over to the [network docs](../strawberryshake/networking).

GraphQL subscriptions can be used through the reactive APIs like queries. Instead of a single network request the store will subscribe to the GraphQL result stream and update the store for each new result that comes in.

# Setup

In order to create a subscription we start like with a query or a mutation. We create a new GraphQL file and add to it a named subscription operation.

```graphql
subscription OnSessionUpdated {
  onSessionScheduled {
    title
  }
}
```

After this we need to add a subscription protocol package to our project. If you do not have any special needs use the [StrawberryShake.Transport.WebSockets](https://www.nuget.org/packages/StrawberryShake.Transport.WebSockets) package.

When you now build your project the StrawberryShake source generator will kick in and generate your new subscription.

# Configuration

While the generator will generate all the subscription code we still need to configure how we can connect to the server. For this head over to the dependency injection and add the `ConfigureWebSockets` method.

```csharp

```
