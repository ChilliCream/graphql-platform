---
title: HTTP transport
---

Hot Chocolate receives GraphQL operations over HTTP through ASP.NET Core endpoints. Use this page to send requests that match Hot Chocolate v16 defaults, choose response formats, configure transport options, and diagnose common HTTP issues.

Most examples use the default `/graphql` route. Read [Endpoint mapping](/docs/hotchocolate/v16/build2/server-configuration/endpoints) when you need to change paths, split HTTP and WebSocket routes, host Nitro separately, or expose SDL endpoints.

## What you will learn

- Send GraphQL operations with HTTP POST and GET.
- Choose request `Content-Type` and response `Accept` headers.
- Understand GraphQL over HTTP response bodies, media types, and status codes.
- Configure GET, multipart, batching, request size, and response formatting.
- Plan for CORS, authorization, reverse proxies, uploads, batching, persisted operations, and WebSockets.

---

## Start with an HTTP endpoint

A minimal server maps the combined GraphQL endpoint at `/graphql`. That endpoint handles HTTP POST and GET requests. It can also handle WebSocket requests when the ASP.NET Core WebSocket middleware is registered.

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

If you want an HTTP-only route, map `MapGraphQLHttp` instead:

```csharp
app.MapGraphQLHttp("/graphql");
```

Use `MapGraphQLHttp` when WebSocket traffic, Nitro, or SDL download must use separate routes or separate proxy rules.

---

## Send a POST request

POST is the default choice for GraphQL clients. It works for queries, mutations, large variables, and request bodies that should not appear in URLs.

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

Use these headers deliberately:

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

GET requests put the same logical fields in the query string. Hot Chocolate handles a GET request as a GraphQL request when the URL includes `query`, `id`, or `extensions`.

```bash
curl -G http://localhost:5000/graphql \
  -H "Accept: application/graphql-response+json" \
  --data-urlencode 'query=query GetBook($id: ID!) { bookById(id: $id) { title } }' \
  --data-urlencode 'variables={"id":"1"}'
```

GET is enabled by default and limited to queries by default. Keep that default for cache safety. Use POST for mutations.

Use GET when the operation is idempotent and the URL is useful to a browser, gateway, proxy, or CDN. Prefer trusted documents or automatic persisted operations for public cache keys, because long ad hoc query strings can hit URL length limits in clients, load balancers, and proxies.

> **Watch out:** Query-string values are strings. If the URL contains `operationName=null`, Hot Chocolate receives the literal operation name `"null"`. Omit the parameter when you mean no operation name.

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

Configure schema-wide defaults with `ModifyServerOptions`:

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

Override options for one endpoint with `WithOptions`:

```csharp
app.MapGraphQLHttp("/graphql")
    .WithOptions(options =>
    {
        options.EnableGetRequests = false;
        options.EnableMultipartRequests = false;
    });
```

The schema-wide options are the default policy for the schema. Endpoint options narrow or change that policy for a mapped route.

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

Configure the maximum GraphQL request body size when registering the server. The default is about 20 MB.

```csharp
builder
    .AddGraphQL(maxAllowedRequestSize: 10 * 1000 * 1024)
    .AddQueryType<Query>();
```

Keep ASP.NET Core, Kestrel, IIS, and reverse proxy request limits aligned with this value. A hosting layer can reject a large request before Hot Chocolate receives it.

---

## Choose response formats with Accept

Hot Chocolate v16 follows the GraphQL over HTTP response format by default. The client chooses the response format with the `Accept` header.

| `Accept` value                      | Response format               | Use it for                                                                             |
| ----------------------------------- | ----------------------------- | -------------------------------------------------------------------------------------- |
| `application/graphql-response+json` | Single GraphQL JSON response. | Standard queries and mutations.                                                        |
| `application/json`                  | Legacy JSON response.         | Older clients that require legacy status-code behavior.                                |
| `multipart/mixed`                   | Multipart response stream.    | Incremental delivery and batch streams.                                                |
| `text/event-stream`                 | Server-Sent Events.           | HTTP streaming clients, including subscriptions or incremental results when supported. |
| `application/jsonl`                 | JSON Lines stream.            | Batch and streaming clients that parse one JSON result per line.                       |

When the client sends no `Accept` header or sends `*/*`, Hot Chocolate chooses its default response format. For single results, that is `application/graphql-response+json`. For result streams, that is `multipart/mixed` unless the client asks for another streaming format.

Use `Accept: application/json` only as a compatibility bridge. It opts the response out of GraphQL over HTTP behavior and uses legacy `application/json` behavior.

A valid request can still receive `406 Not Acceptable` when the `Accept` header asks for a response format Hot Chocolate cannot produce.

---

## Understand HTTP status codes

With `Accept: application/graphql-response+json`, Hot Chocolate can use HTTP status codes to describe transport-level request outcomes.

| Status                      | Meaning                                                               | Common causes                                                                                                             |
| --------------------------- | --------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------- |
| `200 OK`                    | A well-formed GraphQL response body was produced.                     | Successful execution, or execution errors returned in the GraphQL `errors` array.                                         |
| `400 Bad Request`           | The request is invalid before normal execution.                       | Invalid JSON, invalid GraphQL document, invalid request shape, missing required preflight header, request body too large. |
| `405 Method Not Allowed`    | The operation is not allowed for the HTTP method.                     | Mutation over GET while GET is limited to queries.                                                                        |
| `406 Not Acceptable`        | The client asked for an unsupported response media type.              | Unsupported `Accept` header.                                                                                              |
| `500 Internal Server Error` | The server failed before a normal GraphQL response could be produced. | Unhandled server failure outside normal result formatting.                                                                |

Execution errors are often HTTP `200` because the server produced a valid GraphQL response body with an `errors` array. Do not use the HTTP status code as the only signal for GraphQL field or resolver errors.

With `Accept: application/json`, legacy behavior returns `application/json` and status-code behavior compatible with older clients, including `200` for operation results that newer clients may expect to identify through GraphQL over HTTP status codes.

---

## Configure legacy response behavior

If a legacy client cannot send `Accept: application/json`, you can configure the default transport version for missing or wildcard `Accept` headers:

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

Incremental delivery, batching, and some subscription clients use response streams. The client selects the stream format with `Accept`:

```http
Accept: multipart/mixed
```

```http
Accept: text/event-stream
```

```http
Accept: application/jsonl
```

In v16, the default incremental delivery wire format is v0.2. A client can request a specific format with the `incrementalSpec` parameter:

```http
Accept: multipart/mixed; incrementalSpec=v0.2
```

Use `incrementalSpec=v0.1` only for clients that still parse the older incremental delivery shape.

Reverse proxies can buffer or delay streaming responses. Test streaming clients through the same proxy, gateway, and CDN path you use in production. Disable response buffering where your infrastructure requires it, and review idle timeouts for long-running streams.

---

## Enable batching only for intended clients

Hot Chocolate supports variable batching and request batching over HTTP POST. Batching is disabled by default in v16.

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

Variable batching sends one operation with an array of variable sets. Request batching sends a JSON array of operations. Results are streamed back and include correlation fields such as `variableIndex` or `requestIndex`.

Use a streaming `Accept` header when the client can parse streams:

```http
Accept: application/jsonl
```

Read [Batching](/docs/hotchocolate/v16/build2/server-configuration/batching) for request shapes, response ordering, and production limits.

---

## Upload files with multipart requests

File uploads use the GraphQL multipart request specification, not the core GraphQL over HTTP JSON request shape. The request uses `Content-Type: multipart/form-data` with `operations`, `map`, and file parts.

Hot Chocolate enables multipart requests by default and enforces the `GraphQL-Preflight` header by default.

```bash
curl http://localhost:5000/graphql \
  -H "GraphQL-Preflight: 1" \
  -F 'operations={"query":"mutation ($file: Upload!) { uploadFile(file: $file) }","variables":{"file":null}}' \
  -F 'map={"0":["variables.file"]}' \
  -F '0=@./picture.png'
```

Register `UploadType` and implement upload resolvers on the file upload page. Review ASP.NET Core form limits, Kestrel or IIS body limits, and proxy upload limits for large files. For large production assets, consider a dedicated upload endpoint or presigned object-storage URLs.

Read [File uploads](/docs/hotchocolate/v16/build2/server-configuration/file-uploads) for schema setup, resolver parameters, multi-file lists, and hosting limits.

---

## Use persisted operations for cache-friendly HTTP

Trusted documents and automatic persisted operations reduce request payload size. They also make GET routes more cacheable because clients can send a stable identifier instead of a full GraphQL document.

A trusted document request can send `id` instead of `query`:

```json
{
  "id": "GetBookById",
  "variables": {
    "id": "1"
  }
}
```

The same idea works with GET:

```bash
curl -G http://localhost:5000/graphql \
  -H "Accept: application/graphql-response+json" \
  --data-urlencode 'id=GetBookById' \
  --data-urlencode 'variables={"id":"1"}'
```

Automatic persisted operations use `extensions.persistedQuery` to send a hash first, then send the full document on a cache miss.

Read [Trusted documents](/docs/hotchocolate/v16/build2/performance/trusted-documents), [Automatic persisted operations](/docs/hotchocolate/v16/build2/performance/automatic-persisted-operations), and [Cache control](/docs/hotchocolate/v16/build2/server-configuration/cache-control) before using GET responses with a CDN.

---

## Customize HTTP responses

### Format JSON responses

Indent JSON when humans read responses directly:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddHttpResponseFormatter(indented: true);
```

Configure formatter options when you need more control:

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

A custom formatter can change status-code behavior. Test this against every client, because many GraphQL clients have specific expectations for HTTP and GraphQL error handling.

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

Prefer Hot Chocolate cache-control APIs for cache headers instead of writing ad hoc cache logic in a response formatter.

---

## Plan CORS, auth, and reverse proxy behavior

Hot Chocolate runs inside the ASP.NET Core pipeline. Configure hosting concerns before the GraphQL endpoint.

```csharp
var app = builder.Build();

app.UseForwardedHeaders();
app.UseCors("GraphQLClients");
app.UseAuthentication();
app.UseAuthorization();

app.MapGraphQL();

return await app.RunWithGraphQLCommandsAsync(args);
```

Review these production concerns:

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

HTTP and WebSocket transports share the same schema and execution engine, but they use different protocol rules.

- HTTP POST and GET are the main path for queries and mutations.
- HTTP streaming formats, such as `multipart/mixed`, `text/event-stream`, and `application/jsonl`, can deliver result streams selected by `Accept`.
- WebSocket clients use a WebSocket upgrade and a GraphQL WebSocket subprotocol for long-lived sessions, especially subscriptions.
- `app.MapGraphQL()` includes WebSocket handling only when you call `app.UseWebSockets()` before mapping the endpoint.
- `MapGraphQLHttp()` maps HTTP traffic only.

Read [WebSockets](/docs/hotchocolate/v16/build2/server-configuration/websockets) for connection initialization, subprotocols, keep-alive settings, and subscription client behavior.

---

## Troubleshoot common HTTP transport issues

### The client receives 406 Not Acceptable

The `Accept` header asks for a response format Hot Chocolate cannot produce. Use `Accept: application/graphql-response+json` for normal single-result clients. Use a streaming format only when the client can parse that stream.

### The client expected 200 but receives 400

GraphQL over HTTP mode uses non-200 status codes for invalid requests. Check JSON syntax, request shape, GraphQL parse or validation errors, request size, and missing `GraphQL-Preflight` headers. Use legacy `application/json` only while migrating older clients.

### A mutation over GET fails

GET is limited to queries by default. Send mutations with POST. Allow GET mutations only for a documented compatibility requirement and review cache behavior before enabling it.

### A multipart upload fails with a preflight error

Send `GraphQL-Preflight: 1`, confirm `EnableMultipartRequests` is `true`, and check ASP.NET Core and proxy upload limits.

### A batch request is rejected

Batching is disabled by default. Enable the required `AllowedBatching` flags, set `MaxBatchSize`, and confirm the client can parse the response stream.

### A streaming response is buffered or delayed

Check the selected `Accept` header, the client parser, reverse proxy buffering, gateway timeouts, and CDN behavior. Streaming responses can use `Cache-Control: no-cache`.

### A GET request does not reach GraphQL

Confirm the URL includes `query`, `id`, or `extensions`. Confirm `EnableGetRequests` is `true` and that the request path matches the endpoint you mapped.

---

## Production checklist

- Clients send `Accept: application/graphql-response+json` unless they intentionally use legacy `application/json`.
- POST clients send `Content-Type: application/json`.
- GET remains limited to queries unless your HTTP policy permits more.
- Browser CORS policy allows required methods and custom headers.
- GET preflight enforcement is reviewed for browser CSRF risk.
- Multipart preflight enforcement remains enabled for uploads.
- Request size limits are aligned across Hot Chocolate, ASP.NET Core, and proxies.
- Batching remains disabled unless trusted clients need it, and `MaxBatchSize` is set.
- Streaming clients are tested through production proxies and gateways.
- Persisted operations and cache-control headers are used for CDN caching.
- Legacy clients are tested before switching them to `application/graphql-response+json`.

---

## Choose the next page

| If you need to                                         | Read next                                                                                                                                                                                       |
| ------------------------------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Change paths or split protocol endpoints               | [Endpoint mapping](/docs/hotchocolate/v16/build2/server-configuration/endpoints)                                                                                                                |
| Add subscriptions over WebSockets                      | [WebSockets](/docs/hotchocolate/v16/build2/server-configuration/websockets)                                                                                                                     |
| Enable request or variable batching                    | [Batching](/docs/hotchocolate/v16/build2/server-configuration/batching)                                                                                                                         |
| Accept file uploads                                    | [File uploads](/docs/hotchocolate/v16/build2/server-configuration/file-uploads)                                                                                                                 |
| Copy HTTP headers or claims into GraphQL request state | [Interceptors](/docs/hotchocolate/v16/build2/server-configuration/interceptors) and [Request state](/docs/hotchocolate/v16/build2/server-configuration/request-state)                           |
| Configure CDN response headers                         | [Cache control](/docs/hotchocolate/v16/build2/server-configuration/cache-control)                                                                                                               |
| Use stable operation identifiers                       | [Trusted documents](/docs/hotchocolate/v16/build2/performance/trusted-documents) and [Automatic persisted operations](/docs/hotchocolate/v16/build2/performance/automatic-persisted-operations) |
