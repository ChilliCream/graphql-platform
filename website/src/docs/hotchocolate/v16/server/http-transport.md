---
title: HTTP Transport
---

Hot Chocolate implements the latest version of the [GraphQL over HTTP specification](https://github.com/graphql/graphql-over-http/blob/a1e6d8ca248c9a19eb59a2eedd988c204909ee3f/spec/GraphQLOverHTTP.md).

# Response Formats and Content Negotiation

Hot Chocolate uses the HTTP `Accept` header to determine how to format the response. Four response formats are available:

| Accept header                       | Format             | Use case                                            |
| ----------------------------------- | ------------------ | --------------------------------------------------- |
| `application/graphql-response+json` | Single JSON result | Standard queries and mutations (default)            |
| `multipart/mixed`                   | Multipart          | Incremental delivery (`@defer`/`@stream`), batching |
| `text/event-stream`                 | Server-Sent Events | Subscriptions, streaming, incremental delivery      |
| `application/jsonl`                 | JSON Lines         | Streaming, batch responses                          |

When a client sends no `Accept` header or sends `*/*`, the server responds with `application/graphql-response+json` for single results. For streaming operations, the server defaults to `multipart/mixed` unless the client explicitly requests a different format.

When the client sends `Accept: application/json`, it opts out of the GraphQL over HTTP specification and receives legacy-style responses with a `200` status code for all requests.

# Types of Requests

GraphQL requests over HTTP can be performed via either the POST or GET HTTP verb.

## POST Requests

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

## GET Requests

GraphQL can also be served through an HTTP GET request. You have the same options as the HTTP POST request, but the request properties are provided as query parameters. GraphQL HTTP GET requests can be a good choice when you want to cache GraphQL requests.

For example, if you wanted to execute the following GraphQL query:

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

# DefaultHttpResponseFormatter

The `DefaultHttpResponseFormatter` abstracts how responses are delivered over HTTP.

You can override certain aspects of the formatter by creating your own formatter that inherits from `DefaultHttpResponseFormatter`:

```csharp
public class CustomHttpResponseFormatter : DefaultHttpResponseFormatter
{
    // ...
}
```

Register the formatter:

```csharp
builder.Services.AddHttpResponseFormatter<CustomHttpResponseFormatter>();
```

If you want to pass `HttpResponseFormatterOptions` to a custom formatter, make the following adjustments:

```csharp
var options = new HttpResponseFormatterOptions();

builder.Services.AddHttpResponseFormatter(_ => new CustomHttpResponseFormatter(options));

public class CustomHttpResponseFormatter : DefaultHttpResponseFormatter
{
    public CustomHttpResponseFormatter(HttpResponseFormatterOptions options) : base(options)
    {

    }
}
```

## Customizing Status Codes

You can use a custom formatter to alter the HTTP status code in certain conditions.

> Warning: Altering status codes can break the assumptions of your server's clients and might lead to issues. Proceed with caution.

```csharp
public class CustomHttpResponseFormatter : DefaultHttpResponseFormatter
{
    protected override HttpStatusCode OnDetermineStatusCode(
        IOperationResult result, FormatInfo format,
        HttpStatusCode? proposedStatusCode)
    {
        if (result.Errors?.Count > 0 &&
            result.Errors.Any(error => error.Code == "SOME_AUTH_ISSUE"))
        {
            return HttpStatusCode.Forbidden;
        }

        // In all other cases let Hot Chocolate figure out the
        // appropriate status code.
        return base.OnDetermineStatusCode(result, format, proposedStatusCode);
    }
}
```

# JSON Serialization

You can alter some JSON serialization settings when configuring the `HttpResponseFormatter`.

## Stripping Nulls from Response

By default, the JSON in your GraphQL responses contains `null`. If you want to reduce payload size and your clients can handle it, strip nulls from responses:

```csharp
var options = new HttpResponseFormatterOptions
{
    Json = new JsonResultFormatterOptions
    {
        NullIgnoreCondition = JsonNullIgnoreCondition.All
    }
};

builder.Services.AddHttpResponseFormatter(options);
```

## Indenting JSON in Response

By default, the JSON in your GraphQL responses is not indented. If you want to indent your JSON:

```csharp
builder.Services.AddHttpResponseFormatter(indented: true);
```

Be aware that indenting JSON results in a slightly larger response size.

If you are defining other `HttpResponseFormatterOptions`, configure the indentation through the `Json` property:

```csharp
var options = new HttpResponseFormatterOptions
{
    Json = new JsonResultFormatterOptions
    {
        Indented = true
    }
};

builder.Services.AddHttpResponseFormatter(options);
```

# Incremental Delivery (`@defer` / `@stream`)

When using `@defer` or `@stream`, Hot Chocolate streams results to the client using one of three transport formats, selected via the `Accept` header:

| Accept header       | Transport  | Content-Type      |
| ------------------- | ---------- | ----------------- |
| `multipart/mixed`   | Multipart  | multipart/mixed   |
| `text/event-stream` | SSE        | text/event-stream |
| `application/jsonl` | JSON Lines | application/jsonl |

If no streaming `Accept` header is provided, the default is `multipart/mixed`.

## Incremental Delivery Wire Format

There are two wire formats for how incremental results are represented in the response payload.

**v0.2 (default in v16)** uses `pending`, `incremental` with `id`, and `completed` to track deferred fragments:

```json
{"data":{"product":{"name":"Abc"}},"pending":[{"id":"2","path":["product"]}],"hasNext":true}
{"incremental":[{"id":"2","data":{"description":"Abc desc"}}],"completed":[{"id":"2"}],"hasNext":false}
```

**v0.1 (legacy)** uses `path` and `label` directly on incremental entries:

```json
{"data":{"product":{"name":"Abc"}},"hasNext":true}
{"incremental":[{"data":{"description":"Abc desc"},"path":["product"]}],"hasNext":false}
```

In v16, the default changed from v0.1 to v0.2. If your clients depend on the legacy format, you have two options: client-driven format selection or changing the server default.

### Client-Driven Format Selection

Clients choose which format they want by adding the `incrementalSpec` parameter to the `Accept` header:

```text
Accept: multipart/mixed; incrementalSpec=v0.1
Accept: text/event-stream; incrementalSpec=v0.2
Accept: application/jsonl; incrementalSpec=v0.1
```

When the client does not specify `incrementalSpec`, the server default is used.

### Changing the Server Default

The default incremental delivery format is v0.2. To change it server-wide:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddHttpResponseFormatter(
        incrementalDeliveryFormat: IncrementalDeliveryFormat.Version_0_1);
```

Or with the options overload:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddHttpResponseFormatter(
        new HttpResponseFormatterOptions { /* ... */ },
        incrementalDeliveryFormat: IncrementalDeliveryFormat.Version_0_1);
```

The server default is only used as a fallback. A client that sends `incrementalSpec=v0.1` or `incrementalSpec=v0.2` in the `Accept` header always gets the format it asked for, regardless of the server default.

# Streaming Transports

Hot Chocolate supports three streaming transport formats for delivering result streams (incremental delivery, batching, and subscriptions). The client selects the format via the `Accept` header.

## Multipart (`multipart/mixed`)

The default streaming transport. Each result is sent as a separate MIME part separated by a boundary string. This is the most widely supported format.

```text
Accept: multipart/mixed
```

## Server-Sent Events (`text/event-stream`)

Results are delivered as SSE events. This transport works well with browser `EventSource` APIs and proxies that support SSE.

```text
Accept: text/event-stream
```

Each result is sent as an `event: next` message with the JSON payload in the `data:` field. A final `event: complete` message signals the end of the stream.

## JSON Lines (`application/jsonl`)

Each result is written as a single line of JSON, separated by newlines. This format is compact and straightforward to parse incrementally, making it well-suited for batch responses.

```text
Accept: application/jsonl
```

```text
{"data":{"hero":{"name":"R2-D2"}}}
{"data":{"hero":{"name":"Luke Skywalker"}}}
```

The server sends periodic keep-alive messages (a space followed by a newline) to prevent connection timeouts.

# Batching

Hot Chocolate supports operation batching, request batching, and variable batching. These features let you send and execute multiple GraphQL operations in a single HTTP request, with results streamed back using one of the transport formats above.

For full details on how to enable and use batching, see the [Batching](/docs/hotchocolate/v16/server/batching) page.

# Supporting Legacy Clients

Your clients might not yet support the [GraphQL over HTTP specification](https://github.com/graphql/graphql-over-http/blob/a1e6d8ca248c9a19eb59a2eedd988c204909ee3f/spec/GraphQLOverHTTP.md). This can be problematic if they cannot handle a different response `Content-Type` or HTTP status codes besides `200`.

If you have control over the client, you can either:

- Update the client to support the GraphQL over HTTP specification
- Send the `Accept: application/json` request header in your HTTP requests, signaling that your client only understands the legacy format

If you cannot update or change the `Accept` header your clients are sending, configure that a missing `Accept` header or a wildcard like `*/*` should be treated as `application/json`:

```csharp
builder.Services.AddHttpResponseFormatter(new HttpResponseFormatterOptions {
    HttpTransportVersion = HttpTransportVersion.Legacy
});
```

An `Accept` header with the value `application/json` opts you out of the [GraphQL over HTTP](https://github.com/graphql/graphql-over-http/blob/a1e6d8ca248c9a19eb59a2eedd988c204909ee3f/spec/GraphQLOverHTTP.md) specification. The response `Content-Type` becomes `application/json` and a status code of 200 is returned for every request, even if it had validation errors or a valid response could not be produced.

# WebSocket Transport

Hot Chocolate supports GraphQL over WebSocket for real-time communication, including subscriptions. WebSocket connections stay open, allowing the server to push results to the client as they become available.

## Supported Sub-Protocols

Hot Chocolate supports two WebSocket sub-protocols:

| Sub-protocol           | Description                                                                                                                                                                                                        |
| ---------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `graphql-transport-ws` | The modern protocol defined by the [graphql-ws](https://github.com/enisdenjo/graphql-ws/blob/master/PROTOCOL.md) library. This is the recommended protocol for new projects.                                       |
| `graphql-ws`           | The legacy protocol defined by Apollo's [subscriptions-transport-ws](https://github.com/apollographql/subscriptions-transport-ws/blob/master/PROTOCOL.md). Use this for backward compatibility with older clients. |

The client selects its preferred sub-protocol via the standard WebSocket `Sec-WebSocket-Protocol` header during the handshake. Hot Chocolate negotiates and accepts whichever protocol the client requests.

## Enabling WebSocket Support

You must register the ASP.NET Core WebSocket middleware before calling `MapGraphQL()`. Without this, WebSocket upgrade requests are not handled.

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddSubscriptionType<Subscription>();

var app = builder.Build();

app.UseWebSockets(); // Required before MapGraphQL()
app.MapGraphQL();

app.Run();
```

## WebSocket Options

The `GraphQLSocketOptions` class controls WebSocket behavior:

| Property                          | Type        | Default                    | Description                                                                                                                                                                    |
| --------------------------------- | ----------- | -------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `ConnectionInitializationTimeout` | `TimeSpan`  | `TimeSpan.FromSeconds(10)` | The time a client has to send a `connection_init` message after opening the WebSocket. If the client does not initialize within this window, the server closes the connection. |
| `KeepAliveInterval`               | `TimeSpan?` | `TimeSpan.FromSeconds(5)`  | The interval at which the server sends keep-alive pings to prevent idle connections from being dropped. Set to `null` to disable keep-alive.                                   |

Configure these options through `ModifyServerOptions`:

```csharp
builder.Services
    .AddGraphQLServer()
    .ModifyServerOptions(o =>
    {
        o.Sockets.ConnectionInitializationTimeout = TimeSpan.FromSeconds(30);
        o.Sockets.KeepAliveInterval = TimeSpan.FromSeconds(12);
    });
```

You can also configure WebSocket options per-endpoint when using `MapGraphQLWebSocket`:

```csharp
app.MapGraphQLWebSocket("/graphql/ws")
    .WithOptions(o =>
    {
        o.ConnectionInitializationTimeout = TimeSpan.FromSeconds(30);
        o.KeepAliveInterval = TimeSpan.FromSeconds(12);
    });
```

## Connection Lifecycle

A WebSocket connection follows this sequence:

1. The client opens a WebSocket connection and specifies the sub-protocol.
2. The client sends a `connection_init` message within the `ConnectionInitializationTimeout` window.
3. The server responds with `connection_ack`.
4. The client subscribes to operations by sending `subscribe` messages.
5. The server pushes results via `next` messages.
6. When an operation completes, the server sends a `complete` message.
7. The server sends periodic keep-alive pings at the `KeepAliveInterval`.
8. Either side can close the connection.

# Server-Sent Events (SSE)

Server-Sent Events provide an HTTP-based alternative to WebSocket for receiving streaming results. SSE is content-negotiated: the client requests it by sending `Accept: text/event-stream` on the standard GraphQL HTTP endpoint. There is no separate SSE endpoint.

SSE follows the [GraphQL over SSE](https://github.com/graphql/graphql-over-http/blob/main/rfcs/GraphQLOverSSE.md) specification.

## When to Use SSE

SSE is useful in the following scenarios:

- **Subscriptions over HTTP**: When WebSocket connections are blocked by firewalls, proxies, or load balancers, SSE provides an alternative path for receiving real-time updates.
- **Incremental delivery**: `@defer` and `@stream` results can be streamed via SSE.
- **Browser compatibility**: The browser `EventSource` API natively supports SSE without additional libraries.

## SSE Wire Format

The server sends each result as an SSE event:

```text
event: next
data: {"data":{"onMessageReceived":{"body":"Hello"}}}

event: next
data: {"data":{"onMessageReceived":{"body":"World"}}}

event: complete
data:
```

Each result is delivered as an `event: next` message with the JSON payload in the `data:` field. A final `event: complete` message signals the end of the stream.

## SSE for Single Results

SSE is not limited to streaming. A client can send `Accept: text/event-stream` for a standard query, and the server responds with a single `next` event followed by `complete`. This can be useful when you want a uniform transport across all operation types.

# Preflight Header Enforcement

Hot Chocolate provides two settings for enforcing preflight headers as a defense against cross-site request forgery (CSRF) attacks. These settings require that certain requests include a non-standard header (such as `X-Requested-With` or `GraphQL-Preflight`), which triggers a CORS preflight check in browsers.

| Property                                  | Type   | Default | Description                                                                                                                                              |
| ----------------------------------------- | ------ | ------- | -------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `EnforceGetRequestsPreflightHeader`       | `bool` | `false` | When `true`, HTTP GET requests must include a preflight header. Prevents a browser from issuing GET requests via `<script>` or `<img>` tags.             |
| `EnforceMultipartRequestsPreflightHeader` | `bool` | `true`  | When `true`, multipart form requests must include a preflight header. Prevents a browser from submitting multipart forms via standard `<form>` elements. |

Configure these settings through `ModifyServerOptions` or per-endpoint via `WithOptions`:

```csharp
builder.Services
    .AddGraphQLServer()
    .ModifyServerOptions(o =>
    {
        o.EnforceGetRequestsPreflightHeader = true;
        o.EnforceMultipartRequestsPreflightHeader = true;
    });
```

```csharp
app.MapGraphQL().WithOptions(o =>
{
    o.EnforceGetRequestsPreflightHeader = true;
});
```

If a request is rejected because it lacks the required preflight header, the server responds with a `400 Bad Request` status.

# Troubleshooting

## Clients receive v0.2 format unexpectedly

In v16, the default incremental delivery format changed from v0.1 to v0.2. If your clients expect v0.1, either update the clients to send `incrementalSpec=v0.1` in the `Accept` header, or change the server default to `IncrementalDeliveryFormat.Version_0_1`.

## "Unexpected Content-Type" errors on legacy clients

If your clients expect `application/json` responses and cannot handle `application/graphql-response+json`, configure `HttpTransportVersion.Legacy` in the response formatter options.

## WebSocket connections fail or are not upgraded

Verify that `app.UseWebSockets()` is called before `app.MapGraphQL()` in your middleware pipeline. Without the ASP.NET Core WebSocket middleware registered, WebSocket upgrade requests are ignored and fall through to the HTTP handler, which returns a `404` response.

## WebSocket connections drop after a period of inactivity

The default keep-alive interval is 5 seconds. If your infrastructure (load balancers, proxies) has shorter idle timeouts, decrease `KeepAliveInterval`. If you disabled keep-alive by setting it to `null`, re-enable it or configure your infrastructure to allow long-lived connections.

## Client times out during connection initialization

The server closes the WebSocket if the client does not send `connection_init` within 10 seconds (the default `ConnectionInitializationTimeout`). If your client needs more time (for example, to complete authentication), increase the timeout via `ModifyServerOptions`.

## SSE connections are closed prematurely by a proxy

Some reverse proxies and CDNs buffer SSE responses or enforce short timeouts on streaming connections. Configure your proxy to disable response buffering for the GraphQL endpoint and set a sufficiently long read timeout.

## Multipart requests are rejected with 400

If `EnforceMultipartRequestsPreflightHeader` is `true` (the default), the client must include a preflight header such as `GraphQL-Preflight: 1` with multipart requests. Check that your client sends this header.

# Next Steps

- [Endpoints](/docs/hotchocolate/v16/server/endpoints) for configuring the GraphQL middleware and per-endpoint options.
- [Batching](/docs/hotchocolate/v16/server/batching) for details on variable batching and request batching.
- [Subscriptions](/docs/hotchocolate/v16/building-a-schema/subscriptions) for defining subscription types and event publishing.
- [Interceptors](/docs/hotchocolate/v16/server/interceptors) for hooking into WebSocket and HTTP request processing.
- [Migrate from v15 to v16](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16#new-default-incremental-delivery-format-for-defer-and-stream) for the incremental delivery migration details.

<!-- spell-checker:ignore Bname, Buser -->
