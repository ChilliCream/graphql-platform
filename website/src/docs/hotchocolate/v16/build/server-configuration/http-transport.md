---
title: HTTP transport
---

Hot Chocolate processes GraphQL operations over HTTP using ASP.NET Core endpoints. This page explains how to send requests that follow Hot Chocolate defaults, select response formats, configure transport options, and troubleshoot common HTTP issues.

Most examples use the default `/graphql` route. For details on changing paths, splitting HTTP and WebSocket routes, hosting Nitro separately, or exposing SDL endpoints, see [Endpoint mapping](/docs/hotchocolate/v16/build/server-configuration/endpoints).

## What you will learn

- Send GraphQL operations with HTTP POST and GET.
- Choose request `Content-Type` and response `Accept` headers.
- Understand GraphQL over HTTP response bodies, media types, and status codes.
- Configure GET, multipart, batching, request size, and response formatting.
- Plan for CORS, authorization, reverse proxies, uploads, batching, persisted operations, and WebSockets.

---

## Start with an HTTP endpoint

A minimal server maps the combined GraphQL endpoint at `/graphql`. This endpoint supports HTTP POST and GET requests. If the ASP.NET Core WebSocket middleware is registered, it can also handle WebSocket requests.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQL()
    .AddQueryType<Query>();

var app = builder.Build();

app.MapGraphQL();

return await app.RunWithGraphQLCommandsAsync(args);

public sealed class Query
{
    public string Hello() => "world";
}
```

To create an HTTP-only route, use `MapGraphQLHttp` instead:

```csharp
app.MapGraphQLHttp("/graphql");
```

Choose `MapGraphQLHttp` when you need to separate WebSocket traffic, Nitro, or SDL downloads onto different routes or proxy rules.

---

## Send a POST request

POST is the standard method for GraphQL clients. It supports queries, mutations, large variables, and request bodies that should not be exposed in URLs.

```bash
curl -s http://localhost:5000/graphql \
  -H "Content-Type: application/json" \
  -H "Accept: application/graphql-response+json" \
  --data '{"query":"query { hello }"}'
```

Expected response body:

```json
{
  "data": {
    "hello": "world"
  }
}
```

Set these headers explicitly:

| Header                                      | Purpose                                             |
| ------------------------------------------- | --------------------------------------------------- |
| `Content-Type: application/json`            | Tells Hot Chocolate how to parse the request body.  |
| `Accept: application/graphql-response+json` | Asks for the GraphQL over HTTP response media type. |

A POST JSON body can contain these fields:

| Field           | Purpose                                               | Notes                                                     |
| --------------- | ----------------------------------------------------- | --------------------------------------------------------- |
| `query`         | GraphQL document text.                                | Omit it when the request uses a trusted document `id`.    |
| `id`            | Persisted or trusted document identifier.             | Useful for trusted documents and cache-friendly requests. |
| `operationName` | Operation to execute from a multi-operation document. | Omit it when the document contains one operation.         |
| `variables`     | JSON object for variable values.                      | Can be an array only when variable batching is enabled.   |
| `extensions`    | Extension data such as APQ metadata.                  | Used by features such as automatic persisted operations.  |
| `onError`       | Batch error behavior.                                 | Relevant to batching clients.                             |

Example with variables:

```bash
curl -s http://localhost:5000/graphql \
  -H "Content-Type: application/json" \
  -H "Accept: application/graphql-response+json" \
  --data '{"query":"query GetBook($id: ID!) { bookById(id: $id) { title } }","variables":{"id":"1"}}'
```

---

## Send a GET request for a query

GET requests place the same logical fields in the query string. Hot Chocolate treats a GET request as a GraphQL request when the URL includes `query`, `id`, or `extensions`.

```bash
curl -G http://localhost:5000/graphql \
  -H "Accept: application/graphql-response+json" \
  --data-urlencode 'query=query GetBook($id: ID!) { bookById(id: $id) { title } }' \
  --data-urlencode 'variables={"id":"1"}'
```

GET is enabled by default and restricted to queries. Retain this default for cache safety, and use POST for mutations.

Use GET for idempotent operations when the URL benefits browsers, gateways, proxies, or CDNs. For public cache keys, prefer trusted documents or automatic persisted operations, since long ad hoc query strings can exceed URL length limits in clients, load balancers, and proxies.

> **Watch out:** Query-string values are always strings. If the URL contains `operationName=null`, Hot Chocolate interprets this as the literal operation name `"null"`. Omit the parameter if you do not intend to specify an operation name.

---

## Choose the right request form

| Scenario                      | Use            | Required request header                                  | Important option                            |
| ----------------------------- | -------------- | -------------------------------------------------------- | ------------------------------------------- |
| General GraphQL operation     | POST JSON      | `Content-Type: application/json`                         | `AddGraphQL(maxAllowedRequestSize: ...)`    |
| Cacheable query               | GET            | `Accept: application/graphql-response+json`              | `EnableGetRequests`, `AllowedGetOperations` |
| Mutation                      | POST JSON      | `Content-Type: application/json`                         | GET mutations are disabled by default.      |
| File upload                   | POST multipart | `Content-Type: multipart/form-data`, `GraphQL-Preflight` | `EnableMultipartRequests`                   |
| Trusted document              | GET or POST    | Depends on method.                                       | Persisted operation pipeline.               |
| Automatic persisted operation | GET or POST    | Depends on method.                                       | APQ pipeline and storage.                   |
| Batch                         | POST JSON      | Streaming `Accept` recommended.                          | `Batching`, `MaxBatchSize`                  |

---

## Configure HTTP transport options

Set schema-wide defaults using `ModifyServerOptions`:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .ModifyServerOptions(options =>
    {
        options.EnableGetRequests = true;
        options.AllowedGetOperations = AllowedGetOperations.Query;
        options.EnableMultipartRequests = true;
        options.EnforceGetRequestsPreflightHeader = false;
        options.EnforceMultipartRequestsPreflightHeader = true;
        options.Batching = AllowedBatching.None;
        options.MaxBatchSize = 1024;
    });
```

To override options for a specific endpoint, use `WithOptions`:

```csharp
app.MapGraphQLHttp("/graphql")
    .WithOptions(options =>
    {
        options.EnableGetRequests = false;
        options.EnableMultipartRequests = false;
    });
```

Schema-wide options define the default policy. Endpoint options can narrow or override this policy for a specific mapped route.

| Option                                    | Default                      | Applies to        | When to change it                                                                 |
| ----------------------------------------- | ---------------------------- | ----------------- | --------------------------------------------------------------------------------- |
| `EnableGetRequests`                       | `true`                       | HTTP GET          | Disable when all clients must use POST.                                           |
| `AllowedGetOperations`                    | `AllowedGetOperations.Query` | HTTP GET          | Keep query-only unless a compatibility requirement allows another operation kind. |
| `EnableMultipartRequests`                 | `true`                       | Multipart POST    | Disable when the API does not accept uploads.                                     |
| `EnforceGetRequestsPreflightHeader`       | `false`                      | HTTP GET          | Enable when browser CSRF policy requires a non-standard header on GET.            |
| `EnforceMultipartRequestsPreflightHeader` | `true`                       | Multipart POST    | Keep enabled for upload endpoints.                                                |
| `Batching`                                | `AllowedBatching.None`       | POST JSON batches | Enable only the batching modes your clients need.                                 |
| `MaxBatchSize`                            | `1024`                       | Batches           | Lower it for public APIs or high-traffic endpoints. `0` means unlimited.          |
| `MaxConcurrentExecutions`                 | `64`                         | Server execution  | Tune for throughput and downstream capacity.                                      |

Set the maximum allowed GraphQL request body size when registering the server. The default is approximately 20 MB.

```csharp
builder
    .AddGraphQL(maxAllowedRequestSize: 10 * 1000 * 1024)
    .AddQueryType<Query>();
```

Ensure that ASP.NET Core, Kestrel, IIS, and reverse proxy request limits are consistent with this value. A hosting layer may reject large requests before Hot Chocolate processes them.

---

## Choose response formats with Accept

By default, Hot Chocolate uses the GraphQL over HTTP response format. The client selects the response format using the `Accept` header.

| `Accept` value                      | Response format               | Use it for                                                                             |
| ----------------------------------- | ----------------------------- | -------------------------------------------------------------------------------------- |
| `application/graphql-response+json` | Single GraphQL JSON response. | Standard queries and mutations.                                                        |
| `application/json`                  | Legacy JSON response.         | Older clients that require legacy status-code behavior.                                |
| `multipart/mixed`                   | Multipart response stream.    | Incremental delivery and batch streams.                                                |
| `text/event-stream`                 | Server-Sent Events.           | HTTP streaming clients, including subscriptions or incremental results when supported. |
| `application/jsonl`                 | JSON Lines stream.            | Batch and streaming clients that parse one JSON result per line.                       |

If the client omits the `Accept` header or sends `*/*`, Hot Chocolate selects its default response format. For single results, this is `application/graphql-response+json`. For result streams, the default is `multipart/mixed` unless the client requests a different streaming format.

Use `Accept: application/json` only for compatibility. This disables GraphQL over HTTP behavior and returns the legacy `application/json` response.

A valid request may still receive `406 Not Acceptable` if the `Accept` header requests a response format that Hot Chocolate cannot produce.

---

## Understand HTTP status codes

When using `Accept: application/graphql-response+json`, Hot Chocolate uses HTTP status codes to indicate transport-level request outcomes.

| Status                      | Meaning                                                               | Common causes                                                                                                             |
| --------------------------- | --------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------- |
| `200 OK`                    | A well-formed GraphQL response body was produced.                     | Successful execution, or execution errors returned in the GraphQL `errors` array.                                         |
| `400 Bad Request`           | The request is invalid before normal execution.                       | Invalid JSON, invalid GraphQL document, invalid request shape, missing required preflight header, request body too large. |
| `405 Method Not Allowed`    | The operation is not allowed for the HTTP method.                     | Mutation over GET while GET is limited to queries.                                                                        |
| `406 Not Acceptable`        | The client asked for an unsupported response media type.              | Unsupported `Accept` header.                                                                                              |
| `500 Internal Server Error` | The server failed before a normal GraphQL response could be produced. | Unhandled server failure outside normal result formatting.                                                                |

Execution errors often return HTTP `200` because the server produces a valid GraphQL response body containing an `errors` array. Do not rely solely on the HTTP status code to detect GraphQL field or resolver errors.

With `Accept: application/json`, legacy behavior returns `application/json` and status codes compatible with older clients, including `200` for operation results that newer clients may expect to distinguish using GraphQL over HTTP status codes.

---

## Configure legacy response behavior

If a legacy client cannot send `Accept: application/json`, configure the default transport version for missing or wildcard `Accept` headers:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddHttpResponseFormatter(new HttpResponseFormatterOptions
    {
        HttpTransportVersion = HttpTransportVersion.Legacy
    });
```

Use this as a migration setting. New clients should send `Accept: application/graphql-response+json` and handle GraphQL over HTTP status codes.

---

## Stream incremental results over HTTP

Incremental delivery, batching, and some subscription clients use response streams. The client chooses the stream format using the `Accept` header:

```http
Accept: multipart/mixed
```

```http
Accept: text/event-stream
```

```http
Accept: application/jsonl
```

The default incremental delivery wire format is v0.2. Clients can request a specific format using the `incrementalSpec` parameter:

```http
Accept: multipart/mixed; incrementalSpec=v0.2
```

Use `incrementalSpec=v0.1` only for clients that still parse the older incremental delivery shape.

Reverse proxies may buffer or delay streaming responses. Test streaming clients through the same proxy, gateway, and CDN path used in production. Disable response buffering where necessary, and review idle timeouts for long-running streams.

---

## Enable batching only for intended clients

Hot Chocolate supports variable batching and request batching over HTTP POST, but batching is disabled by default.

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .ModifyServerOptions(options =>
    {
        options.Batching = AllowedBatching.VariableBatching;
        options.MaxBatchSize = 100;
    });
```

Variable batching sends a single operation with an array of variable sets. Request batching sends a JSON array of operations. Results are streamed back and include correlation fields such as `variableIndex` or `requestIndex`.

Use a streaming `Accept` header when the client can parse streams:

```http
Accept: application/jsonl
```

Read [Batching](/docs/hotchocolate/v16/build/performance/batching) for request shapes, response ordering, and production limits.

---

## Upload files with multipart requests

File uploads follow the GraphQL multipart request specification, which differs from the core GraphQL over HTTP JSON request shape. The request uses `Content-Type: multipart/form-data` and includes `operations`, `map`, and file parts.

Hot Chocolate enables multipart requests by default and enforces the `GraphQL-Preflight` header by default.

```bash
curl http://localhost:5000/graphql \
  -H "GraphQL-Preflight: 1" \
  -F 'operations={"query":"mutation ($file: Upload!) { uploadFile(file: $file) }","variables":{"file":null}}' \
  -F 'map={"0":["variables.file"]}' \
  -F '0=@./picture.png'
```

Register `UploadType` and implement upload resolvers as described on the file upload page. Review ASP.NET Core form limits, Kestrel or IIS body limits, and proxy upload limits for large files. For large production assets, consider a dedicated upload endpoint or presigned object-storage URLs.

See [File uploads](/docs/hotchocolate/v16/_leagcy/server/files) for schema setup, resolver parameters, multi-file lists, and hosting limits.

---

## Use persisted operations for cache-friendly HTTP

Trusted documents and automatic persisted operations reduce request payload size and make GET routes more cacheable, since clients can send a stable identifier instead of a full GraphQL document.

A trusted document request can send `id` instead of `query`:

```json
{
  "id": "GetBookById",
  "variables": {
    "id": "1"
  }
}
```

This approach also works with GET:

```bash
curl -G http://localhost:5000/graphql \
  -H "Accept: application/graphql-response+json" \
  --data-urlencode 'id=GetBookById' \
  --data-urlencode 'variables={"id":"1"}'
```

Automatic persisted operations use `extensions.persistedQuery` to send a hash first, then send the full document if there is a cache miss.

See [Trusted documents](/docs/hotchocolate/v16/build/security/trusted-documents), [Automatic persisted operations](/docs/hotchocolate/v16/build/performance/automatic-persisted-operations), and [Cache control](/docs/hotchocolate/v16/build/performance/cache-control) before using GET responses with a CDN.

---

## Customize HTTP responses

### Format JSON responses

Indent JSON responses for easier human readability:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddHttpResponseFormatter(indented: true);
```

Adjust formatter options for more control over JSON output:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddHttpResponseFormatter(new HttpResponseFormatterOptions
    {
        Json = new JsonResultFormatterOptions
        {
            NullIgnoreCondition = JsonNullIgnoreCondition.All
        }
    });
```

### Customize status codes with care

A custom formatter can alter status-code behavior. Test thoroughly with all clients, as many GraphQL clients have specific expectations for HTTP and GraphQL error handling.

```csharp
using System.Net;
using HotChocolate.AspNetCore;
using HotChocolate.Execution;

public sealed class CustomHttpResponseFormatter : DefaultHttpResponseFormatter
{
    protected override HttpStatusCode OnDetermineStatusCode(
        OperationResult result,
        FormatInfo format,
        HttpStatusCode? proposedStatusCode)
    {
        if (result.Errors?.Any(error => error.Code == "FORBIDDEN") is true)
        {
            return HttpStatusCode.Forbidden;
        }

        return base.OnDetermineStatusCode(result, format, proposedStatusCode);
    }
}
```

Register it with the schema:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddHttpResponseFormatter<CustomHttpResponseFormatter>();
```

Use Hot Chocolate cache-control APIs to manage cache headers, rather than implementing custom cache logic in a response formatter.

---

## Plan CORS, auth, and reverse proxy behavior

Hot Chocolate operates within the ASP.NET Core pipeline. Configure hosting concerns before mapping the GraphQL endpoint.

```csharp
var app = builder.Build();

app.UseForwardedHeaders();
app.UseCors("GraphQLClients");
app.UseAuthentication();
app.UseAuthorization();

app.MapGraphQL();

return await app.RunWithGraphQLCommandsAsync(args);
```

Consider these production concerns:

| Concern           | What to check                                                                                                                                             |
| ----------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------- |
| CORS              | Allow the methods, headers, and credentials your browser clients use. Include custom headers such as `GraphQL-Preflight` when required.                   |
| Authentication    | Register ASP.NET Core authentication before authorization and endpoint mapping.                                                                           |
| Authorization     | Use ASP.NET Core policies and Hot Chocolate authorization attributes or middleware. Do not rely on transport checks as the only access control.           |
| Forwarded headers | Use forwarded header middleware when a proxy terminates TLS or rewrites host and scheme data.                                                             |
| Request size      | Align Hot Chocolate request size with Kestrel, IIS, form, gateway, and proxy limits.                                                                      |
| Streaming         | Turn off proxy response buffering where needed and set idle timeouts for `multipart/mixed`, SSE, and JSON Lines streams.                                  |
| Caching           | Use persisted operation identifiers and cache-control headers. Avoid caching arbitrary POST responses unless your infrastructure has a deliberate policy. |

---

## Relate HTTP to WebSockets

HTTP and WebSocket transports share the same schema and execution engine, but follow different protocol rules.

- HTTP POST and GET are the primary methods for queries and mutations.
- HTTP streaming formats, such as `multipart/mixed`, `text/event-stream`, and `application/jsonl`, deliver result streams as selected by the `Accept` header.
- WebSocket clients use a WebSocket upgrade and a GraphQL WebSocket subprotocol for long-lived sessions, especially subscriptions.
- `app.MapGraphQL()` includes WebSocket handling only if you call `app.UseWebSockets()` before mapping the endpoint.
- `MapGraphQLHttp()` maps only HTTP traffic.

See [WebSockets](/docs/hotchocolate/v16/build/server-configuration/websocket-transport) for connection initialization, subprotocols, keep-alive settings, and subscription client behavior.

---

## Troubleshoot common HTTP transport issues

### The client receives 406 Not Acceptable

The `Accept` header requests a response format that Hot Chocolate cannot produce. Use `Accept: application/graphql-response+json` for standard single-result clients. Use a streaming format only if the client can parse that stream.

### The client expected 200 but receives 400

GraphQL over HTTP mode uses non-200 status codes for invalid requests. Check JSON syntax, request shape, GraphQL parse or validation errors, request size, and missing `GraphQL-Preflight` headers. Use legacy `application/json` only while migrating older clients.

### A mutation over GET fails

GET is limited to queries by default. Send mutations with POST. Allow GET mutations only for a documented compatibility requirement, and review cache behavior before enabling it.

### A multipart upload fails with a preflight error

Send `GraphQL-Preflight: 1`, ensure `EnableMultipartRequests` is `true`, and check ASP.NET Core and proxy upload limits.

### A batch request is rejected

Batching is disabled by default. Enable the required `AllowedBatching` flags, set `MaxBatchSize`, and confirm the client can parse the response stream.

### A streaming response is buffered or delayed

Check the selected `Accept` header, the client parser, reverse proxy buffering, gateway timeouts, and CDN behavior. Streaming responses can use `Cache-Control: no-cache`.

### A GET request does not reach GraphQL

Ensure the URL includes `query`, `id`, or `extensions`. Confirm `EnableGetRequests` is `true` and that the request path matches the mapped endpoint.

---

## Production checklist

- Clients send `Accept: application/graphql-response+json` unless they intentionally use legacy `application/json`.
- POST clients use `Content-Type: application/json`.
- GET remains limited to queries unless your HTTP policy allows more.
- Browser CORS policy allows required methods and custom headers.
- GET preflight enforcement is reviewed for browser CSRF risk.
- Multipart preflight enforcement remains enabled for uploads.
- Request size limits are consistent across Hot Chocolate, ASP.NET Core, and proxies.
- Batching remains disabled unless trusted clients require it, and `MaxBatchSize` is set.
- Streaming clients are tested through production proxies and gateways.
- Persisted operations and cache-control headers are used for CDN caching.
- Legacy clients are tested before switching to `application/graphql-response+json`.

---

## Next steps

| If you need to                                         | Read next                                                                                                                                                                                  |
| ------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Change paths or split protocol endpoints               | [Endpoint mapping](/docs/hotchocolate/v16/build/server-configuration/endpoints)                                                                                                            |
| Add subscriptions over WebSockets                      | [WebSockets](/docs/hotchocolate/v16/build/server-configuration/websocket-transport)                                                                                                        |
| Enable request or variable batching                    | [Batching](/docs/hotchocolate/v16/build/performance/batching)                                                                                                                              |
| Accept file uploads                                    | [File uploads](/docs/hotchocolate/v16/_leagcy/server/files)                                                                                                                                |
| Copy HTTP headers or claims into GraphQL request state | [Interceptors](/docs/hotchocolate/v16/build/server-configuration/interceptors) and [Request state](/docs/hotchocolate/v16/build/server-configuration/global-state)                         |
| Configure CDN response headers                         | [Cache control](/docs/hotchocolate/v16/build/performance/cache-control)                                                                                                                    |
| Use stable operation identifiers                       | [Trusted documents](/docs/hotchocolate/v16/build/security/trusted-documents) and [Automatic persisted operations](/docs/hotchocolate/v16/build/performance/automatic-persisted-operations) |
