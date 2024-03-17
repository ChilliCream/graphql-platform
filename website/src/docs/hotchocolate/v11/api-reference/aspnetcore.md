---
title: ASP.NET Core
---

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

If no path is specified, the GraphQL middleware will follow the spec recommendation to map the endpoint to `/graphql`.

`http://example.com/graphql`

`http://product.example.com/graphql`

`http://example.com/product/graphql`

## GraphQL HTTP POST requests

The GraphQL HTTP POST request is the most commonly used variant for GraphQL requests over HTTP and is specified [here](https://github.com/graphql/graphql-over-http/blob/master/spec/GraphQLOverHTTP.md#post).

**request:**

```http
POST /graphql
HOST: foo.example
Content-Type: application/json

{
  "query": "query($id: ID!){user(id:$id){name}}",
  "variables": { "id": "QVBJcy5ndXJ1" }
}
```

**response:**

```http
HTTP/1.1 200 OK
Content-Type: application/json

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
query ($id: ID!) {
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

**request:**

```http
GET /graphql?query=query(%24id%3A%20ID!)%7Buser(id%3A%24id)%7Bname%7D%7D&variables=%7B%22id%22%3A%22QVBJcy5ndXJ1%22%7D`
HOST: foo.example
```

**response:**

```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "data": {
    "user": {
      "name": "Jon Doe"
    }
  }
}
```

> Note: {query} and {operationName} parameters are encoded as raw strings in the query component. Therefore if the query string contained operationName=null then it should be interpreted as the {operationName} being the string "null". If a literal null is desired, the parameter (e.g. {operationName}) should be omitted.

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

The Hot Chocolate GraphQL server supports incremental delivery over HTTP, which essentially uses HTTP chunked transfer encoding combined with the [specification of multipart content defined by the W3 in rfc1341](https://www.w3.org/Protocols/rfc1341/7_2_Multipart.html).

The incremental delivery is at the moment at the RFC stage and is specified [here](https://github.com/graphql/graphql-over-http/blob/master/rfcs/IncrementalDelivery.md).

Incremental delivery is used with `@defer`, `@stream`, and with request batching.

# Additional Requests

Apart from the requests defined by the GraphQL over HTTP spec, Hot Chocolate allows you to batch requests, download the GraphQL SDL, and many more things.

> Many of the request types stated in this section are on their way into the GraphQL over HTTP spec, and we will update this document as the spec, and its RFCs change.

## GraphQL Schema request

Although you can access and query the schema definition through introspection, we support fetching the GraphQL schema SDL as a file. The GraphQL schema SDL is richer with more information and easier to read.

**request:**

```http
GET /graphql?sdl
HOST: foo.example
```

**response:**

```http
HTTP/1.1 200 OK
Content-Type: application/graphql

type Query {
  hello: String!
}
```

## GraphQL HTTP POST batching request

We support two kinds of batching variants.

The first variant to batch GraphQL requests is by sending in an array of GraphQL requests. Hot Chocolate will execute them in order.

```http
POST /graphql
HOST: foo.example
Content-Type: application/json

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

The second GraphQL batching variant is called operation batching, where you send in one GraphQL request document with multiple operations. The operation execution order is then specified as a query param.

```http
POST /graphql?batchOperations=[a,b]
HOST: foo.example
Content-Type: application/json

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

By default, the GraphQL server will use the **incremental delivery over HTTP** specification to write the stream results as soon as they are available. This means that depending on your client implementation; you can start using the results as they appear in order.

The serialization defaults can be changed like the following:

```csharp
services.AddHttpResultSerializer(
    batchSerialization: HttpResultSerialization.JsonArray,
    deferSerialization: HttpResultSerialization.MultiPartChunked)
```

> More about batching can be found [here](/docs/hotchocolate/v10/execution-engine/batching).

## GraphQL multipart request specification

Hot Chocolate implements the GraphQL multipart request specification which allows for file upload streams in your browser. The GraphQL multipart request specification can be found [here](https://github.com/jaydenseric/graphql-multipart-request-spec).

In order to use file upload streams in your input types or as an argument register the `Upload` scalar like the following.

```csharp
service
    .AddGraphQLServer()
    ...
    .AddType<UploadType>();
```

In your resolver or input type you can then use the `IFile` interface to use the upload scalar.

```csharp
public class Mutation
{
    public async Task<bool> UploadFileAsync(IFile file)
    {
        using Stream stream = file.OpenReadStream();
        // you can now work with standard stream functionality of .NET to handle the file.
    }
}
```

> Note, that the `Upload` scalar can only be used as an input type and does not work on output types.

If you need to upload large files or set custom upload size limits, you can configure those by registering custom [`FormOptions`](https://docs.microsoft.com/dotnet/api/microsoft.aspnetcore.http.features.formoptions).

```csharp
services.Configure<FormOptions>(options =>
{
    // Set the limit to 256 MB
    options.MultipartBodyLengthLimit = 268435456;
});
```

Based on your WebServer you might need to configure these limits elsewhere as well. [Kestrel](https://docs.microsoft.com/aspnet/core/mvc/models/file-uploads#kestrel-maximum-request-body-size) and [IIS](https://docs.microsoft.com/aspnet/core/mvc/models/file-uploads#iis) are covered in the ASP.NET Core Documentation.

# Subscription Transport

Subscriptions are by default delivered over WebSocket. We have implemented the [GraphQL over WebSocket Protocol](https://github.com/apollographql/subscriptions-transport-ws/blob/master/PROTOCOL.md) specified by Apollo.

## Alternative Transport Protocols

With version 11.1, we will add alternative transport protocols like the [new proposal for the GraphQL over HTTP spec](https://github.com/graphql/graphql-over-http/pull/140).

Moreover, we are working on allowing this protocol to be used over SignalR, which gives more flexibility to use subscriptions.

# Tooling

The Hot Chocolate GraphQL server comes right out of the gate with excellent tooling. By default, we are mapping our GraphQL IDE Banana Cake Pop to the GraphQL endpoint. This means you just need to open your browser and navigate to the configured endpoint to send requests to your server, explore your schema, or build-up tests.

![GraphQL IDE](../../../images/get-started-bcp-query.png)

The GraphQL IDE can be disabled by specifying tool options:

```csharp
endpoints
    .MapGraphQL()
    .WithOptions(
        new GraphQLServerOptions
        {
            Tool = { Enable = false }
        }));
```

# Serialization

The Hot Chocolate GraphQL server has abstracted the result serialization with the `IHttpResultSerializer` interface. The server uses the registered implementation to resolve the HTTP status code, the HTTP content type, and the serialized response from a GraphQL execution result.

```csharp
/// <summary>
/// This interface specifies how a GraphQL result is serialized to a HTTP response.
/// </summary>
public interface IHttpResultSerializer
{
    /// <summary>
    /// Gets the HTTP content type for the specified execution result.
    /// </summary>
    /// <param name="result">
    /// The GraphQL execution result.
    /// </param>
    /// <returns>
    /// Returns a string representing the content type,
    /// eg. "application/json; charset=utf-8".
    /// </returns>
    string GetContentType(IExecutionResult result);

    /// <summary>
    /// Gets the HTTP status code for the specified execution result.
    /// </summary>
    /// <param name="result">
    /// The GraphQL execution result.
    /// </param>
    /// <returns>
    /// Returns the HTTP status code, eg. <see cref="HttpStatusCode.OK"/>.
    /// </returns>
    HttpStatusCode GetStatusCode(IExecutionResult result);

    /// <summary>
    /// Serializes the specified execution result.
    /// </summary>
    /// <param name="result">
    /// The GraphQL execution result.
    /// </param>
    /// <param name="stream">
    /// The HTTP response stream.
    /// </param>
    /// <param name="cancellationToken">
    /// The request cancellation token.
    /// </param>
    ValueTask SerializeAsync(
        IExecutionResult result,
        Stream stream,
        CancellationToken cancellationToken);
}
```

We have a default implementation (`DefaultHttpResultSerializer`) that can be used to built custom logic on top of the original implementation to make extensibility easier. By default, we are using `System.Text.Json` to serialize GraphQL execution results to JSON.

A custom implementation of the result serializer is registered like the following:

```csharp
services.AddHttpResultSerializer<MyCustomHttpResultSerializer>();
```

If you, for instance, wanted to add some special error code handling when some error happened during execution, you could implement this like the following:

```csharp
public class MyCustomHttpResultSerializer : DefaultHttpResultSerializer
{
    public override HttpStatusCode GetStatusCode(IExecutionResult result)
    {
        if (result is IQueryResult queryResult &&
            queryResult.Errors?.Count > 0 &&
            queryResult.Errors.Any(error => error.Code == "SOME_AUTH_ISSUE"))
        {
            return HttpStatusCode.Forbidden;
        }

        return base.GetStatusCode(result);
    }
}
```

# GraphQL request customization

The GraphQL server allows you to customize how the GraphQL request is created. For this, you need to implement the `IHttpRequestInterceptor`. For convenience reasons, we provide a default implementation (`DefaultHttpRequestInterceptor`) that can be extended.

```csharp
public class DefaultHttpRequestInterceptor : IHttpRequestInterceptor
{
    public virtual ValueTask OnCreateAsync(
        HttpContext context,
        IRequestExecutor requestExecutor,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        requestBuilder.TrySetServices(context.RequestServices);
        requestBuilder.TryAddProperty(nameof(HttpContext), context);
        requestBuilder.TryAddProperty(nameof(ClaimsPrincipal), context.User);
        requestBuilder.TryAddProperty(nameof(CancellationToken), context.RequestAborted);

        if (context.IsTracingEnabled())
        {
            requestBuilder.TryAddProperty(WellKnownContextData.EnableTracing, true);
        }

        return default;
    }
}
```

Suppose you want to add more data to a GraphQL request; override the `OnCreateAsync` method, and add your custom data as a request property. These request properties are mapped to the request context data, which can be accessed in the field resolver or a field middleware through the `context`.

```csharp
if(context.ContextData.ContainsKey(nameof(HttpContext)))
{
    // some logic
}
```

The context data can also be injected into resolver methods.

```csharp
public string MyResolver([GlobalState(nameof(HttpContext))] HttpContext context)
{
    // some logic
}
```

A good practice is to inherit from the `GlobalStateAttribute` to create a custom typed attribute.

```csharp
public string MyResolver([HttpContext] HttpContext context)
{
    // some logic
}
```

A custom http request interceptor can be registered like the following:

```csharp
services.AddSocketSessionInterceptor<MyCustomHttpRequestInterceptor>();
```

## Request Errors

The interceptor can be used to do general request validation. This essentially allows to fail the request before the GraphQL context is created. In order to create a GraphQL error response simply throw a `GraphQLException` in the `OnCreateAsync` method. The middleware will translate these to a proper GraphQL error response for the client. You also can customize the status code behavior by using the HTTP result serializer mentioned above.

# Subscription session handling

The Hot Chocolate GraphQL server allows you to interact with the server's socket session handling by implementing `ISocketSessionInterceptor`. For convenience reasons, we provide a default implementation (`DefaultSocketSessionInterceptor`) that can be extended.

```csharp
public class DefaultSocketSessionInterceptor : ISocketSessionInterceptor
{
    public virtual ValueTask<ConnectionStatus> OnConnectAsync(
        ISocketConnection connection,
        InitializeConnectionMessage message,
        CancellationToken cancellationToken) =>
        new ValueTask<ConnectionStatus>(ConnectionStatus.Accept());

    public virtual ValueTask OnRequestAsync(
        ISocketConnection connection,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        HttpContext context = connection.HttpContext;
        requestBuilder.TrySetServices(connection.RequestServices);
        requestBuilder.TryAddProperty(nameof(CancellationToken), connection.RequestAborted);
        requestBuilder.TryAddProperty(nameof(HttpContext), context);
        requestBuilder.TryAddProperty(nameof(ClaimsPrincipal), context.User);

        if (connection.HttpContext.IsTracingEnabled())
        {
            requestBuilder.TryAddProperty(WellKnownContextData.EnableTracing, true);
        }

        return default;
    }

    public virtual ValueTask OnCloseAsync(
        ISocketConnection connection,
        CancellationToken cancellationToken) =>
        default;
}
```

A custom socket session interceptor can be registered like the following:

```csharp
services.AddSocketSessionInterceptor<MyCustomSocketSessionInterceptor>();
```
