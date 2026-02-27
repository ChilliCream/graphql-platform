---
title: Batching
---

Batching allows you to send and execute a sequence of GraphQL operations in a single request.

# Enabling batching

Batching is disabled per default as a security measure, so you need to first enable it explicitly:

```csharp
app.MapGraphQL().WithOptions(new GraphQLServerOptions
{
    EnableBatching = true
});
```

# Operation batching

You probably already know that you can send a GraphQL request document with multiple operations to a GraphQL server. However, normally you also have to specify the name of a **single** operation you wish to execute.

```json
{
  "query": "query Operation1 { stories { id } } query Operation2 { me { name } }",
  "operationName": "Operation1"
}
```

With operation batching you can specify a list of operation names you wish to execute in a sequence:

```text
POST /graphql?batchOperations=[Operation2,Operation1]
{
  "query": "query Operation1 { stories { id } } query Operation2 { me { name } }"
}
```

The above request would first execute the operation with the name `Operation2` and then the operation with the name `Operation1`. The results are also emitted to the response stream in the specified order.

# Request batching

Request batching allows you to send a JSON array of regular GraphQL documents to your server:

```json
[
    {
        # The query document.
        "query": "query getHero { hero { name } }",
        # The name of the operation that shall be executed.
        "operationName": "getHero",
        # A key under which a query document was saved on the server.
        "id": "W5vrrAIypCbniaIYeroNnw==",
        # The variable values for this request.
        "variables": {
            "a": 1,
            "b": "abc"
        },
        # Custom properties that can be passed to the execution engine context data.
        "extensions": {
            "a": 1,
            "b": "abc"
        }
    },
    {
        # The query document.
        "query": "query getHero { hero { name } }",
        # The name of the operation that shall be executed.
        "operationName": "getHero",
        # A key under which a query document was saved on the server.
        "id": "W5vrrAIypCbniaIYeroNnw==",
        # The variable values for this request.
        "variables": {
            "a": 1,
            "b": "abc"
        },
        # Custom properties that can be passed to the execution engine context data.
        "extensions": {
            "a": 1,
            "b": "abc"
        }
    },
]
```

The documents are executed and emitted to the response stream in the order specified by their placement in the JSON array.

Each result in the response stream includes a `requestIndex` field (0-based) that correlates the result back to its position in the request array.

# Variable batching

Variable batching allows you to execute a **single operation multiple times** with different sets of variables. Instead of sending `variables` as an object, you send it as an array of objects:

```json
{
  "query": "query GetHero($episode: Episode!) { hero(episode: $episode) { name } }",
  "variables": [
    { "episode": "JEDI" },
    { "episode": "EMPIRE" },
    { "episode": "NEWHOPE" }
  ]
}
```

The operation executes once per variable set. Each result in the response stream includes both a `requestIndex` and a `variableIndex` (0-based) so the client can match results to their corresponding variable set.

You can also combine variable batching with request batching. In this case, one or more entries in the request array can use an array of variables:

```json
[
  {
    "query": "query GetHero($episode: Episode!) { hero(episode: $episode) { name } }",
    "variables": [{ "episode": "JEDI" }, { "episode": "EMPIRE" }]
  },
  {
    "query": "{ __typename }"
  }
]
```

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
