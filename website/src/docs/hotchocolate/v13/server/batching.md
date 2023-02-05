---
title: Batching
---

Batching allows you to send and execute a sequence of GraphQL operations in a single request.

This becomes really powerful in combination with our [@export](#export-directive) directive, especially considering mutations. You could for example create a sequence of mutations and export the result of an earlier mutation as the input for a later mutation.

# Operation batching

You probably already know that you can send a GraphQL request document with multiple operations to a GraphQL server. However, normally you also have to specify the name of a **single** operation you wish to execute.

```json
{
  "query": "query Operation1 { stories { id } } query Operation2 { me { name } }",
  "operationName": "Operation1"
}
```

With operation batching you can specify a list of operation names you wish to execute in a sequence:

```
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

# @export directive

The `@export` directive allows you to export the results of a query into a global variable pool from which each query in the sequence can pull data in. Different queries in the sequence can write to the same exported variable, as long as the type matches, but there can also be multiple exported variables of different types. You could for example query for a set of Ids in one query and inject those Ids as variables into a second query, later in the same batch.

Before you can start using the `@export` directive, you need to register it with your schema.

```csharp
builder.Services.AddGraphQLServer()
    .AddExportDirectiveType()
    // Ommited for brevity
```

Now you can annotate the directive on fields in your GraphQL query.

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

In the above example we are exporting the `id` field result into a variable `ids`. Since we are exporting multiple Ids the variable is turned into a list. If the Id is of type `String`, we would get a list of `System.String`. As we collect the variables, we will hold them as the native .NET type and only coerce them once we have to create the variable inputs for the next operation.

As can be seen in the above example we have not declared any variables for the next operation and are just using `$ids`. While we could still declare the `ids` variable explicitly, we can infer the variable declaration. The query engine will essentially rewrite the query.

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
