---
title: ASP.Net
---

Hot Chocolate supports ASP.Net Core and ASP.Net Framework out of the box. This means you can get started very quickly with both variants. There are example projects for both in our [repository](https://github.com/ChilliCream/hotchocolate/tree/master/examples) on GitHub.

# HTTP Usage

Hot Chocolate has implemented the [recommendations](https://graphql.org/learn/serving-over-http/) for serving GraphQL over HTTP. We are also supporting request batching over HTTP and subscriptions over websocket.

## HTTP POST

The post request is the most used variant for GraphQL request over HTTP.

application/json: the POST body will be parsed as a JSON object of parameters.

```json
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
}
```

application/graphql: The POST body will be parsed as GraphQL query string, which provides the query parameter.

```graphql
query getHero {
  hero {
    name
  }
}
```

The response in both cases will be JSON by default. The response serializers can be swapped out and you could for instance go protobuf.

```json
{
  "data": {
    "hero": {
      "name": "R2-D2"
    }
  }
}
```

## HTTP GET

GraphQL can also be served through an HTTP GET request. You have the same options as with the POST request just that the request properties are provided as query parameters. GET request can be a good choice if you are looking to cache GraphQL requests.

`http://localhost/graphql?query=query+getUser($id:ID){user(id:$id){name}}&variables={"id":"4"}`

## HTTP GET Schema

Although you can get access to the schema metadata through introspection, we also support fetching the GraphQL schema SDL. The GraphQL schema SDL is richer with information and easier to read.

SDL schema available in v10 under:
`https://yourserver/GraphQL/Schema`

## HTTP POST Batching

We support two kinds of batching variants.

The first variant to batch is on request base, you basically send in an array of GraphQL request and the query engine will issue the results in order.

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

The second variant is called operation batching where you send in one request with multiple operations and specify the operations that shall be executed:

`http://localhost/graphql?batchOperations=[a,b]`

```json
{
    # The query document.
    "query": "query a { hero { name } } query b { hero { name } }",

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
}
```

The executer will write the results to the stream as soon as they are available. This means that depending on your client implementation you can start using the results as they appear in order.

By default, we are serializing the result as a JSON array, but you can change the format to make it work better with your client implementation.

More about batching can be found [here](/docs/hotchocolate/v10/execution-engine/batching).

# WebSocket Support

We have implemented the [GraphQL over WebSocket Protocol](https://github.com/apollographql/subscriptions-transport-ws/blob/master/PROTOCOL.md) specified by Apollo.

# SignalR Support

> We are still working on SignalR support and will publish the documentation once we are finished.

# ASP.Net Core

The ASP.Net Core implementation is implemented modular. This means that each protocol that we have implemented is represented by a specific middleware that is provided as a separate package. Fear not we also provide a meta-package that bundles all the middleware components together.

If you just want to get started adding the `HotChocolate.AspNetCore` package.

Next add the schema and all needed services for the middleware components:

```csharp
services.AddGraphQL(
    SchemaBuilder.New()
        .AddQueryType<QueryType>()
        .AddType<CharacterType>())
```

> More about the schema builder can be found [here](/docs/hotchocolate/v10/schema).

After we have setup the schema and its services, we now have to configure the middleware components.

```csharp
app.UseGraphQL();
```

And we are done basically.

## Supported Core Components

It is also possible to setup only the components and services that you need. The following packages are available:

- HotChocolate.AspNetCore.HttpPost
- HotChocolate.AspNetCore.HttpGet
- HotChocolate.AspNetCore.HttpGetSchema
- HotChocolate.AspNetCore.Subscriptions
- HotChocolate.AspNetCore.Authorization

Instead of using `UseGraphQL` you can opt with these packages to use specific middleware components like `UseGraphQLHttpGet`.

# ASP.Net Framework

The ASP.Net Framework implementation is implemented modular on top of OWIN. This means that each protocol that we have implemented is represented by a specific middleware that is provided as a separate package. Fear not we also provide a meta-package that bundles all the middleware components together.

> Currently there is no support for the subscription websockets protocol an ASP.Net Framework

If you just want to get started adding the `HotChocolate.AspNetClassic` package.

Next add the schema and all needed services for the middleware components:

```csharp
services.AddGraphQL(
    SchemaBuilder.New()
        .AddQueryType<QueryType>()
        .AddType<CharacterType>())
```

> More about the schema builder can be found [here](/docs/hotchocolate/v10/schema).

After we have setup the schema and its services, we now have to configure the middleware components.

```csharp
app.UseGraphQL(serviceProvider);
```

And we are done basically.

## Supported Framework Components

It is also possible to setup only the components and services that you need. The following packages are available:

- HotChocolate.AspNetClassic.HttpPost
- HotChocolate.AspNetClassic.HttpGet
- HotChocolate.AspNetClassic.HttpGetSchema
- HotChocolate.AspNetClassic.Authorization

Instead of using `UseGraphQL` you can opt with these packages to use specific middleware components like `UseGraphQLHttpGet`.

# Custom Serializers

There are two response serializers that can be customized. By default we have added JSON serializers. You can customize serialization by implementing the following interfaces:

- IQueryResultSerializer
- IResponseStreamSerializer

We have a added some helper extension to swap the default serializer out:

```csharp
services.AddResponseStreamSerializer<CustomResponseStreamSerializer>();
```
