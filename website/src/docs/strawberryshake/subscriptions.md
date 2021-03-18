---
title: "Subscriptions"
---

Subscriptions in GraphQL represent real-time events that are represented as a stream of query responses. In most cases subscriptions are used over WebSockets but can also used with other protocols. For transport questions please head over to the [network docs](../strawberryshake/networking).

GraphQL subscriptions can be used through async enumerables and our reactive interface.

```graphql
subscription OnSessionUpdated {
  onSessionScheduled {
    title
  }
}
```
