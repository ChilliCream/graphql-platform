---
title: "Subscriptions and realtime"
description: "Learn when to use GraphQL subscriptions with Hot Chocolate v16, how events move through topics and transports, and what to plan before running realtime workloads in production."
---

Should the client poll for updates, or should the server deliver new results as they happen? This is the core question when considering realtime features. GraphQL subscriptions are designed for scenarios where a client is connected, interested in user-facing changes, and needs to receive new GraphQL results while that interest remains. Subscriptions are not a substitute for queries, durable workflow state, or offline messaging.

# When to use realtime in your API

Subscriptions are best used when the user experience benefits from seeing changes as they occur, while the user is actively engaged with the application. Consider the following guidance:

| Use subscriptions when | Prefer another approach when |
| --- | --- |
| A notification, message, alert, or status change should appear during an active session. | A screen needs an initial view of data. Use a query. |
| A client wants to follow one order, job, conversation, dashboard, or collaborative workspace. | A user action already knows when to refresh. Refetch after the mutation. |
| The client can tolerate receiving updates only while it is connected. | The product requires offline delivery, audit history, or guaranteed replay. Model durable state and query it. |
| The event payload is small and the expected fan-out is bounded. | The work creates a large export, report, or batch process. Model an async job and query its status. |

Subscriptions provide active delivery. If a client disconnects, the subscription ends. If your product needs to recover after a reconnect, ensure the source of truth is queryable. Model resources such as `Job`, `OperationStatus`, `Pending`, `Running`, `Completed`, and `Failed`, so the client can request the current state or any missed changes.

Subscription fields typically use event-oriented names:

- `orderStatusChanged`
- `messageReceived`
- `inventoryLevelChanged`
- `bookAdded`

These names represent domain events in your schema. They should not reveal broker names, database tables, or infrastructure-specific topics.

# Subscriptions stream GraphQL results

A subscription operation starts once and can yield zero, one, or many results over time. Each result is a GraphQL response shaped by the selection set in the operation.

```graphql
subscription WatchOrder($orderId: ID!) {
  orderStatusChanged(orderId: $orderId) {
    status
    updatedAt
  }
}
```

For example, a single active subscription might receive these results:

```json
{
  "data": {
    "orderStatusChanged": {
      "status": "PACKING",
      "updatedAt": "2025-02-12T10:15:30Z"
    }
  }
}
```

```json
{
  "data": {
    "orderStatusChanged": {
      "status": "SHIPPED",
      "updatedAt": "2025-02-12T10:22:04Z"
    }
  }
}
```

The operation document is part of the GraphQL schema contract. Clients select fields, variables, fragments, and operation names the same way they do for queries and mutations. For more on the operation model, see [Operations](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/operations/). For client-side concerns such as documents, headers, generated code, caches, and subscription handling, see [Clients](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/clients/).

The [GraphQL specification defines subscriptions](https://spec.graphql.org/October2021/#sec-Subscription) as operations that produce a response stream. Hot Chocolate provides the schema, execution, provider, and ASP.NET Core transport components to make this stream available to connected clients.

# How an event becomes a streamed result

A subscription follows a path from a domain change to a streamed GraphQL result:

```text
Application change accepted
  -> publisher sends an event payload
  -> topic routes the event to interested subscribers
  -> subscription field receives the payload
  -> Hot Chocolate executes the selected fields
  -> WebSocket or SSE sends the result to the client
```

In Hot Chocolate, application code typically publishes events using `ITopicEventSender` after a change is accepted. Subscription fields marked with `[Subscribe]` receive matching events, and the event payload is passed into the resolver using `[EventMessage]`.

The topic acts as the link between publishers and subscribers. By default, simple examples use the subscription method name as the topic. When the audience depends on an argument, a `[Topic]` pattern can route by resource, tenant, or user.

For implementation details and working examples, see [Subscriptions](/docs/hotchocolate/v16/building-a-schema/subscriptions/).

# Designing topics: who should receive the event?

Start by asking:

> Which connected clients should receive this event?

Topics are routing keys, not public API names. The schema field tells clients what event they can subscribe to, while the topic tells the server-side provider which subscribers should receive a published payload.

| Scenario | Public field | Internal topic shape | Recipient model |
| --- | --- | --- | --- |
| Public catalog feed | `bookAdded` | `OnBookAdded` from `nameof(BookSubscriptions.OnBookAdded)`, or `books:added` | All active clients watching the feed. |
| One order screen | `orderStatusChanged(orderId: ID!)` | `order:{orderId}` | Clients watching that order. |
| Tenant notifications | `alertRaised` | `tenant:{tenantId}:alerts` | Clients authorized for that tenant. |

Align topic design with authorization. Filtering a payload after it reaches the wrong subscriber is not the same as preventing delivery to the wrong audience. For multi-tenant and user-scoped streams, include the scope in the topic or subscription path so the provider only routes to intended recipients.

Be mindful of topic cardinality. Using a topic per resource is often appropriate for order details or chat rooms. Creating a topic for every rapidly changing value can introduce noise and operational complexity. Name topics after stable routing concepts such as tenant, user, resource, or channel.

# Choosing a transport: WebSocket or SSE

Before selecting a subscription transport, consider this deployment question:

Can the client and every network layer maintain a streaming connection?

| Transport | Use it when | Verify before production |
| --- | --- | --- |
| WebSocket | Your GraphQL client supports the `graphql-transport-ws` protocol and your infrastructure allows WebSocket upgrades. | ASP.NET Core WebSocket middleware is enabled, proxies allow upgrades, and authentication works during connection initialization and operation execution. |
| Server-Sent Events | You want an HTTP server-to-client stream, often in environments where HTTP streaming is more reliable than WebSocket. | The client requests `Accept: text/event-stream`, proxies do not buffer the response, and idle timeouts are understood. |

WebSocket and SSE are transports between the client and the GraphQL endpoint. The subscription provider is the server-side pub/sub backend that moves events between publishers and subscribers. You often need to make both decisions: one for connected clients, and one for delivery across server instances.

Hot Chocolate exposes GraphQL over HTTP through the [HTTP transport](/docs/hotchocolate/v16/server/http-transport/) and endpoint middleware via [Endpoints](/docs/hotchocolate/v16/server/endpoints/). For broader hosting considerations, see Microsoft's [proxy and load balancer configuration](https://learn.microsoft.com/aspnet/core/host-and-deploy/proxy-load-balancer). Relevant external references include the [GraphQL over HTTP specification](https://github.com/graphql/graphql-over-http/blob/main/spec/GraphQLOverHTTP.md), the [`graphql-ws` library](https://github.com/enisdenjo/graphql-ws) (which implements the `graphql-transport-ws` protocol), and [`graphql-sse`](https://github.com/enisdenjo/graphql-sse).

# Manually verifying the stream with Nitro

Use Nitro to confirm that your endpoint, transport, authentication, subscription execution, topic routing, and streamed payloads are working before debugging application code or infrastructure.

1. Open a saved subscription document or create one in the operation editor.
2. Select the endpoint environment that points to your Hot Chocolate server.
3. Configure WebSocket or SSE connection settings.
4. Add authentication headers, connection parameters, tenant IDs, or resource IDs as needed.
5. Run the subscription and keep it active.
6. Trigger the event through the UI, a mutation, a test action, or another known publisher path.
7. Inspect streamed responses, including `data`, `errors`, paths, extensions, status, duration, and response history.

```graphql
subscription WatchOrder($id: ID!) {
  orderStatusChanged(orderId: $id) {
    status
    updatedAt
  }
}
```

Expected behavior: Nitro keeps the operation active and displays a new response each time a matching order status event is published.

If something fails, use the first failing boundary to guide your next diagnostic step:

| Boundary | What to check |
| --- | --- |
| Cannot connect | Endpoint URL, WebSocket upgrade, SSE subscription URL, proxy settings. |
| Connects but auth fails | Headers, cookies, connection initialization payload, token lifetime. |
| Operation starts but no payload arrives | Publisher path, topic values, provider registration, selected environment. |
| Payload arrives with `errors` | Subscription field execution, authorization, nullability, resolver errors. |

Nitro documentation that supports this workflow:

- [Connection Settings](/docs/nitro/documents/connection-settings/)
- [Authentication](/docs/nitro/documents/authentication/)
- [Environments](/docs/nitro/environments/)
- [Operations](/docs/nitro/documents/operations/)
- [Response Pane](/docs/nitro/documents/response/)

# Selecting a delivery backend for your deployment

The subscription provider delivers events between server-side publishers and subscribers. It does not replace WebSocket or SSE. Instead, it answers this question:

Can a subscriber connected to one server instance receive an event published on another server instance?

| Deployment shape | Provider choice | Trade-off |
| --- | --- | --- |
| Local tutorial or one server process | In-memory provider | No external infrastructure, but events stay inside one process and are lost on restart. |
| Multiple Hot Chocolate instances | External provider | Events can cross instances, but you now operate a broker or database channel. |
| Existing broker platform | Supported provider that your operations team can run and monitor | Reuse operational maturity, but align topic isolation, network access, and failure handling. |

Hot Chocolate v16 documents in-memory, Redis, NATS, and PostgreSQL providers in [Subscriptions](/docs/hotchocolate/v16/building-a-schema/subscriptions/). Choose based on your deployment shape and operational ownership, not on preference alone.

A common scale-out failure looks like this:

```text
Client is connected to instance A
Mutation runs on instance B
Instance B publishes to in-memory subscriptions
Instance A never sees the event
```

An external provider gives both instances a shared delivery path. However, this does not make events durable for disconnected clients unless both the provider and your application design explicitly support replay. Treat durable history, missed-update recovery, and workflow progress as queryable application data.

# Planning for connection lifecycle, authentication, and reconnects

Subscriptions introduce policies that queries and mutations often do not require, because the connection is long-lived.

Before releasing, review this readiness checklist:

| Policy | Question to answer |
| --- | --- |
| Connection authentication | How does the client authenticate the initial HTTP, WebSocket, or SSE connection? |
| WebSocket initialization | Which values can the client send in `connection_init`, and how do you validate them? |
| Operation authorization | Does each subscription operation and selected field enforce the same policies as queries and mutations? |
| Reconnect behavior | What should the client do after the connection closes, the token expires, or the server restarts? |
| Missed-event recovery | Can the client query current state by stable ID, timestamp, version, or sequence checkpoint? |
| Timeout policy | Which keep-alive, idle timeout, proxy timeout, and client retry settings are expected? |

Authorize both access to the stream and access to the selected data. A WebSocket connection may be accepted, but a subscription operation can still fail authorization. Hot Chocolate allows you to intercept HTTP requests and WebSocket sessions in [Interceptors](/docs/hotchocolate/v16/server/interceptors/), and field-level authorization is covered in [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization/).

Do not design a subscription as the only record of workflow progress. If the client must recover after a reconnect, provide a query such as `orderById`, `jobById`, or `changesSince(checkpoint:)` to rebuild the current view.

# Keeping realtime workloads manageable and observable

Realtime features affect capacity planning, since a single event can trigger many GraphQL executions and transport writes.

Before going to production, consider these questions:

| Capacity question | Why it matters |
| --- | --- |
| How many clients may be connected at once? | Each connection consumes server, transport, and network resources. |
| How many subscription operations can one client run? | A single connection can still create multiple active server-side subscribers. |
| Which event has the largest fan-out? | One publish can produce many result payloads. |
| How large is the selected payload? | Large selections multiply bandwidth and resolver work across subscribers. |
| What happens when a client reads slowly? | Slow clients can increase buffering and delay delivery. |
| Where are broker, transport, and resolver errors visible? | You need logs, traces, metrics, and smoke tests that include realtime paths. |
| Which layer can close or buffer streams? | Proxies, gateways, load balancers, and browsers can affect WebSocket and SSE behavior. |

Use production smoke tests that keep a subscription active, publish a known event, and verify the streamed response. Pair this with server instrumentation and broker monitoring. Nitro can help observe operations through [Operation Monitoring](/docs/nitro/open-telemetry/operation-monitoring/).

# Troubleshooting missing or delayed events

| Symptom | Likely cause | Fix | Verify |
| --- | --- | --- | --- |
| The client connects but receives no events. | The event is not published, the topic value differs, or the wrong environment is selected. | Log or inspect the publisher path and topic value. Run the same subscription in Nitro. | Trigger a known event and confirm a streamed payload appears. |
| It works on one server and fails after scale-out. | The deployment still uses the in-memory provider. | Configure a supported external subscription provider. | Connect to one instance, publish through another, and observe delivery. |
| WebSocket closes after connect. | Connection initialization, authentication, or proxy upgrade handling fails. | Inspect `connection_init` payload handling, auth logs, and WebSocket upgrade settings. | Nitro or the client reaches an accepted active subscription. |
| SSE starts but events arrive late. | Infrastructure buffers `text/event-stream` responses. | Disable buffering for the GraphQL route and review proxy timeout settings. | Response chunks reach the client as events are published. |
| Auth succeeds for the connection but the subscription result contains errors. | Per-operation authorization or selected field authorization fails. | Check Hot Chocolate authorization rules and WebSocket `OnRequestAsync` behavior. | The same user can execute the authorized subscription and selected fields. |
| The client reconnects and expects old events. | The subscription stream is being treated as durable history. | Model queryable state, replay, or an async operation status resource. | A follow-up query recovers current state or missed changes. |
| Nitro connects but no payload arrives after a known trigger. | Topic mismatch, event published before commit, or provider delivery issue. | Publish after the change is accepted, compare topic values, and inspect provider health. | The next accepted change produces one streamed result. |

When troubleshooting, isolate the first boundary that fails: connection settings, authentication, subscription execution, event trigger, topic routing, provider delivery, streamed response, or field execution.

# Next steps: where to implement

Use this mental model to guide your next step:

- **Build your first subscription:** start with [Subscriptions](/docs/hotchocolate/v16/building-a-schema/subscriptions/).
- **Define subscription fields, publish events, and configure providers:** see [Subscriptions](/docs/hotchocolate/v16/building-a-schema/subscriptions/).
- **Configure GraphQL endpoints and transports:** see [Endpoints](/docs/hotchocolate/v16/server/endpoints/) and [HTTP transport](/docs/hotchocolate/v16/server/http-transport/).
- **Secure connection and operation behavior:** see [Authentication](/docs/hotchocolate/v16/securing-your-api/authentication/), [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization/), and [Interceptors](/docs/hotchocolate/v16/server/interceptors/).
- **Verify manually:** use Nitro [Connection Settings](/docs/nitro/documents/connection-settings/) and [Response Pane](/docs/nitro/documents/response/).
- **Prepare for production rollout:** review [Instrumentation](/docs/hotchocolate/v16/server/instrumentation/), [Endpoints](/docs/hotchocolate/v16/server/endpoints/), [Performance](/docs/hotchocolate/v16/performance/), and Microsoft's [proxy and load balancer configuration](https://learn.microsoft.com/aspnet/core/host-and-deploy/proxy-load-balancer).

A production-ready subscription design names the subscription operation, routes events through topics, selects a streaming transport, chooses a delivery backend, and defines lifecycle, authorization, recovery, and observability policies.
