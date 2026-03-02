---
title: Batching
---

Batching allows you to send and execute multiple GraphQL operations in a single HTTP request. Hot Chocolate supports two forms of batching: **variable batching** and **request batching**. Both deliver results as a stream, so the client receives each result as soon as it's ready — without waiting for the entire batch to complete.

Variable batching is based on an [open proposal](https://github.com/graphql/graphql-over-http/pull/307) to the [GraphQL over HTTP specification](https://github.com/graphql/graphql-over-http).

# Enabling batching

Batching is disabled by default as a security measure. You need to explicitly enable the types of batching you want to allow:

```csharp
services
    .AddGraphQLServer()
    .ModifyServerOptions(o => o.Batching = AllowedBatching.VariableBatching);
```

You can combine flags to enable multiple batching modes:

```csharp
services
    .AddGraphQLServer()
    .ModifyServerOptions(o => o.Batching =
        AllowedBatching.VariableBatching | AllowedBatching.RequestBatching);
```

> Note: If your GraphQL server is a Fusion subgraph, both variable batching and request batching are enabled by default. You do not need to configure this explicitly.

You can also limit the maximum number of operations in a single batch:

```csharp
services
    .AddGraphQLServer()
    .ModifyServerOptions(o => o.MaxBatchSize = 1024);
```

# Variable batching

Variable batching lets you execute a **single operation multiple times** with different sets of variables. Instead of sending `variables` as an object, you send it as an array of objects:

```json
{
  "query": "query getHero { hero { name } }",
  "operationName": "getHero",
  "id": "W5vrrAIypCbniaIYeroNnw==",
  "variables": [
    {
      "a": 1,
      "b": "abc"
    },
    {
      "a": 2,
      "b": "def"
    }
  ],
  "extensions": {
    "a": 1,
    "b": "abc"
  }
}
```

The operation executes once per variable set. Each result in the response stream includes a `variableIndex` (0-based) so the client can match results back to their corresponding variable set:

```jsonl
{ "data": { "hero": { "name": "R2-D2" } }, "variableIndex": 0 }
{ "data": { "hero": { "name": "Luke Skywalker" } }, "variableIndex": 1 }
```

Results are delivered out of order — whichever variable set finishes first is streamed to the client first. The `variableIndex` field is how the client correlates each result back to its input.

# Request batching

Request batching allows you to send a JSON array of independent GraphQL operations in a single HTTP request. Each entry in the array is a complete operation with its own query, variables, and operation name.

Individual entries in the array can also use variable batching by providing `variables` as an array:

```json
[
  {
    "query": "query getHero { hero { name } }",
    "operationName": "getHero",
    "id": "W5vrrAIypCbniaIYeroNnw==",
    "variables": {
      "a": 1,
      "b": "abc"
    },
    "extensions": {
      "a": 1,
      "b": "abc"
    }
  },
  {
    "query": "query getHero { hero { name } }",
    "operationName": "getHero",
    "id": "W5vrrAIypCbniaIYeroNnw==",
    "variables": [
      {
        "a": 1,
        "b": "abc"
      },
      {
        "a": 2,
        "b": "def"
      }
    ],
    "extensions": {
      "a": 1,
      "b": "abc"
    }
  }
]
```

Each result includes a `requestIndex` (0-based) that identifies which entry in the request array it belongs to. When an entry uses variable batching, its results also include a `variableIndex`:

```jsonl
{ "data": { "hero": { "name": "R2-D2" } }, "requestIndex": 1, "variableIndex": 0 }
{ "data": { "hero": { "name": "Han Solo" } }, "requestIndex": 0 }
{ "data": { "hero": { "name": "Luke Skywalker" } }, "requestIndex": 1, "variableIndex": 1 }
```

Like variable batching, results are delivered out of order. In the example above, the second request (index 1) returned its first variable result before the first request (index 0) completed. The `requestIndex` and `variableIndex` fields let the client reassemble the results correctly.

# Response formats

Batch results are delivered as a result stream. This allows Hot Chocolate to stream result data back to your client as soon as each item in the batch has been executed.

The response transport is selected via the `Accept` header:

| Accept header       | Transport  | Content-Type        |
| ------------------- | ---------- | ------------------- |
| `multipart/mixed`   | Multipart  | `multipart/mixed`   |
| `text/event-stream` | SSE        | `text/event-stream` |
| `application/jsonl` | JSON Lines | `application/jsonl` |

If no streaming `Accept` header is provided, the default is `multipart/mixed`.

**JSON Lines** (`application/jsonl`) is especially well-suited for batch responses. Each result is written as a single line of JSON, making it easy for clients to parse results incrementally:

```text
{"requestIndex":0,"data":{"hero":{"name":"R2-D2"}}}
{"requestIndex":1,"data":{"hero":{"name":"Luke Skywalker"}}}
```

If you're using a JavaScript client, we can highly recommend

- [meros](https://github.com/maraisr/meros) for handling `multipart/mixed` responses
- [graphql-sse](https://github.com/enisdenjo/graphql-sse) for handling `text/event-stream` responses

For more details about these streaming transports, see [HTTP transport](/docs/hotchocolate/v16/server/http-transport#streaming-transports).

<!-- spell-checker:ignore Cbnia, Yero -->
