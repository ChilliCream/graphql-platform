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

# Response formats

Batch results are delivered as a result stream. This allows us to "stream" the result data back to your client, as soon as an item in the batch has been executed.

Depending on the `Accept` header your client is specifying in its requests, Hot Chocolate will decide to either use `multipart/mixed` or a `text/event-stream` response `Content-Type` to deliver the results. If no `Accept` header or a wildcard is specified, `multipart/mixed` is used.

If you're using a JavaScript client, we can highly recommend

- [meros](https://github.com/maraisr/meros) for handling `multipart/mixed` responses
- [graphql-sse](https://github.com/enisdenjo/graphql-sse) for handling `text/event-stream` responses

<!-- spell-checker:ignore Cbnia, Yero -->
