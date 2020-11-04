---
title: Batching
---

Hot Chocolate supports operation batching and request batching. But before we get into the details lets reflect on what batching is and what you can do with it.

[![Batching](../../../shared/batching.png)](https://youtu.be/ViXL0YQnioU?t=626)

# Introduction

With batching we have added the capability run a sequence of operations. The batch is executed in order and the results of each request is yielded to the user once it has been computed. This means that we do not have to wait for the complete batch to be completed and can use the results as they are written to the response stream.

This means that with batching we are able to delay expensive queries. Essentially, we can do something that we also can do with `@defer`.

Let's say we had a news site and wanted to fetch the stories and for each story the first two comments.

We could do that with a single query like the following:

```graphql
query NewsFeed {
  stories {
    id @export(as: "ids")
    actor
    message
    comments(first: 2) {
      actor
      message
    }
  }
}
```

But in our case, we want the story content to be available really, really quickly and we do not mind if the comments appear a little later. So, what we could do here is to break this query into two and send them in as batch.

`POST /graphql?batchOperations=[NewsFeed, StoryComments]`

```graphql
query NewsFeed {
  stories {
    id @export(as: "ids")
    actor
    message
  }
}

query StoryComments {
  stories(ids: $ids) {
    comments(first: 2) {
      actor
      message
    }
  }
}
```

The key to do this is our `@export` directive which is able to export results as variables for the next queries in the batch. Each query can add to the variables collection.

Batching combined with `@export` becomes really interesting if you think about mutations. With this you can create a sequence of mutations that should run on your server and each result of a mutation can become a input for the next mutation in the chain. This allows you to efficiently write flows that run asynchronously on the server.

# Operation Batching

With operation batching you basically send in the same request as before. You can either opt to send plain GraphQL or send in the GraphQL-JSON-request.

> More about the request structure can be read [here](/docs/hotchocolate/v10/server).

Since we are sending in multiple operations, we specify the sequence with as a query parameter:

`POST /graphql?batchOperations=[Operation1, Operation2, Operation3, Operation4]`

Currently we write the result as JSON-array into the HTTP-response-stream. Each result is written to the response stream as it appears so you could grab each element as it appears. If you want to change the serialization format you can implement `IResponseStreamSerializer` and register you custom serializer with the dependency injection.

```csharp
services.AddResponseStreamSerializer<CustomResponseStreamSerializer>();
```

# Request Batching

Request batching is essentially a way to send in multiple GraphQL-JSON-requests. These requests are basically wrapped into a JSON-array and send in the same way as the standard GraphQL-JSON-request.

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

# Export Directive

The export directive allows to export the results of a query into a global variable pool from which each query in the sequence can pull data in.

```graphql
query NewsFeed {
  stories {
    id @export(as: "ids")
    actor
    message
  }
}

query StoryComments {
  stories(ids: $ids) {
    comments(first: 2) {
      actor
      message
    }
  }
}
```

In the above example we are exporting the `id` field result into a variable `ids`. Since we are exporting multiple ids the variable is essentially becoming a list. In our example we will get a list of `System.String`. As we collect the variables, we will hold them as the native .NET type and only coerce them once we have to create the variable inputs for the next operation.

As can be seen in the above example we have not declared any variable for the next operation and are just using `$ids`. While we still could declare the variable explicitly, we can infer the variable declaration. The query engine will essentially rewrite the query.

```graphql
query StoryComments($ids: [ID!]) {
  stories(ids: $ids) {
    comments(first: 2) {
      actor
      message
    }
  }
}
```

You can also export objects, so you are not limited to scalars.

```graphql
query NewsFeed {
  stories @export {
    id
    actor
    message
  }
}
```

In the above example we would export a list of story objects that would be coerced and converted to fit into an input object.

# IBatchQueryExecutor

If you want to write tests or implement your own batching middleware, then you just have to inject `IBatchQueryExecutor`. The batch executor will return a `IBatchQueryExecutionResult` which is essentially a `IResponseStream`.

The response stream allows to read the results from the stream as they become available.

```csharp
while (!responseStream.IsCompleted)
{
    IReadOnlyQueryResult queryResult = await responseStream.ReadAsync();
    Console.WriteLine(query.ToJson());
}
```
