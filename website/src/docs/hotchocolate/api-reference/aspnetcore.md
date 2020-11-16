Hot Chocolate comes with integration to the ASP.NET Core endpoints API. The middleware implementation follows the current GraphQL over HTTP Spec.

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app.UseRouting()

    app.UseEndpoints(endpoints =>
    {
        endpoints.MapGraphQL();
    });
}
```

# GraphQL over HTTP Spec

The following GraphQL requests follow the current GraphQL over HTTP spec draft.

If no path is specified the GraphQL middleware will follow the spec recommendation to map the endpoint to `/graphql`.

`http://example.com/graphql`

`http://product.example.com/graphql`

`http://example.com/product/graphql`

## GraphQL HTTP POST requests

The GraphQL HTTP POST request is the most commonly used variant for GraphQL requests over HTTP and is specified [here](https://github.com/graphql/graphql-over-http/blob/master/spec/GraphQLOverHTTP.md#post).

For example if the `Content-Type` is `application/json` then the request body may be:

```json
{
  "query": "query($id: ID!){user(id:$id){name}}",
  "variables": { "id": "QVBJcy5ndXJ1" }
}
```

The response is by default returned as a JSON result with `Content-Type` `application/json`:

```json
{
  "data": {
    "user": {
      "name": "Jon Doe"
    }
  }
}
```

## GraphQL HTTP GET request

GraphQL can also be served through an HTTP GET request. You have the same options as the HTTP POST request, just that the request properties are provided as query parameters. GraphQL HTTP GET requests can be a good choice if you are looking to cache GraphQL requests.

For example, if we wanted to execute the following GraphQL query:

```graphql
query($id: ID!) {
  user(id: $id) {
    name
  }
}
```

With the following query variables:

```json
{
  "id": "QVBJcy5ndXJ1"
}
```

This request could be sent via an HTTP GET as follows:

`http://example.com/graphql?query=query(%24id%3A%20ID!)%7Buser(id%3A%24id)%7Bname%7D%7D&variables=%7B%22id%22%3A%22QVBJcy5ndXJ1%22%7D`

> Note: {query} and {operationName} parameters are encoded as raw strings in the query component. Therefore if the query string contained operationName=null then it should be interpreted as the {operationName} being the string "null". If a literal null is desired, the parameter (e.g. {operationName}) should be omitted.

The response is by default returned as a JSON result with `Content-Type` `application/json`:

```json
{
  "data": {
    "user": {
      "name": "Jon Doe"
    }
  }
}
```

The GraphQL HTTP GET request is specified [here](https://github.com/graphql/graphql-over-http/blob/master/spec/GraphQLOverHTTP.md#get).

By default, Hot Chocolate will only serve query operations when HTTP GET requests are used. You can change this default by specifying the GraphQL server options.

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app.UseRouting()

    app.UseEndpoints(endpoints =>
    {
        endpoints
            .MapGraphQL()
            .WithOptions(new GraphQLServerOptions
            {
                AllowedGetOperations = AllowedGetOperations.QueryAndMutation
            });
    });
}
```

You can also entirely deactivate HTTP GET request handling.

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app.UseRouting()

    app.UseEndpoints(endpoints =>
    {
        endpoints
            .MapGraphQL()
            .WithOptions(new GraphQLServerOptions
            {
                EnableGetRequests = false
            });
    });
}
```

## Incremental Delivery over HTTP

The Hot Chocolate GraphQL server also supports incremental delivery over HTTP which essentially uses HTTP chunked transfer encoding in combination with [specification of multipart content defined by the W3 in rfc1341](https://www.w3.org/Protocols/rfc1341/7_2_Multipart.html).

The incremental delivery is at the moment at the RFC stage and are specified [here](https://github.com/graphql/graphql-over-http/blob/master/rfcs/IncrementalDelivery.md).

# Additional ....

Apart from the requests that are specified

# Grap Schema

Although you can access and query the schema definition through introspection, we support fetching the GraphQL schema SDL as a file. The GraphQL schema SDL is richer with more information and easier to read.

`http://localhost/graphql?sdl`

# HTTP POST Batching

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
