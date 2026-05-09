---
title: Batching
---

Batching allows you to send multiple GraphQL executions in a single HTTP request. This approach can reduce the number of HTTP round trips for clients that need to start several independent operations at once, such as dashboards that load viewer data, notifications, and product summaries together.

Keep in mind that batching does not decrease the work performed by resolvers. Hot Chocolate still executes each operation in the batch, applies validation and cost controls, and streams each result back to the client. Batching is a transport optimization with security implications.

# Choosing a batching pattern

| Pattern             | Request shape                                                                                                         | Enable with                        | Result index                                                                   | Use when                                                       |
| ------------------- | --------------------------------------------------------------------------------------------------------------------- | ---------------------------------- | ------------------------------------------------------------------------------ | -------------------------------------------------------------- |
| Request batching    | POST body is a JSON array of GraphQL request objects                                                                  | `AllowedBatching.RequestBatching`  | `requestIndex`                                                                 | A client has several independent operations to start together. |
| Operation batching  | One request object contains a document with multiple named operations, selected with `?batchOperations=[Name1,Name2]` | `AllowedBatching.RequestBatching`  | `requestIndex`                                                                 | One document already contains the named operations to execute. |
| Variable batching   | One operation runs with several variable sets                                                                         | `AllowedBatching.VariableBatching` | `variableIndex`, or `requestIndex` plus `variableIndex` inside a request batch | One operation needs many variable inputs.                      |
| DataLoader batching | Resolver calls are grouped during execution                                                                           | DataLoader APIs                    | Not a transport field                                                          | You need to avoid N+1 data access.                             |

Request batching and operation batching are features of the HTTP transport layer. DataLoader batching addresses a different concern within GraphQL execution. Persisted operations and trusted documents also serve a different purpose: they reduce or restrict request text, but do not combine multiple executions into a single HTTP request.

# Enabling batching

```csharp
using HotChocolate.AspNetCore;

builder
    .AddGraphQL()
    .ModifyServerOptions(o =>
    {
        o.Batching = AllowedBatching.RequestBatching;
        o.MaxBatchSize = 10;
    });
```

By default, `GraphQLServerOptions.Batching` is set to `AllowedBatching.None`. Only enable the batching modes required for your endpoint.

- `AllowedBatching.RequestBatching` enables both JSON-array request batching and `batchOperations` operation batching.
- `AllowedBatching.VariableBatching` enables variable arrays for a single operation.
- `AllowedBatching.All` enables both request and variable batching.
- `MaxBatchSize` defaults to `1024`. Setting it to `0` removes the limit.
- `MaxConcurrentExecutions` defaults to `64` and still applies while batch items are executing.

For public APIs, use a smaller `MaxBatchSize`. Since one HTTP request can represent many GraphQL executions, limiting only the number of requests is not sufficient.

You can also override batching for a specific endpoint:

```csharp
app.MapGraphQL().WithOptions(o =>
{
    o.Batching = AllowedBatching.None;
});
```

Fusion source schemas enable request and variable batching by default. Be sure to set explicit limits when source schemas may receive uncontrolled traffic.

# Sending a request batch

```http
POST /graphql
Content-Type: application/json
Accept: application/graphql-response+jsonl

[
  {
    "query": "query GetViewer { viewer { id name } }"
  },
  {
    "query": "query GetProducts($first: Int!) { products(first: $first) { nodes { id name } } }",
    "variables": { "first": 5 }
  }
]
```

Each item in the array is a standard GraphQL request object. It can include `query`, `id`, `operationName`, `variables`, and `extensions`. Entries may use persisted operation identifiers if your server is configured for them.

Do not send empty arrays. Hot Chocolate will reject an empty request batch with `The GraphQL batch request has no elements.` If request batching is disabled, a JSON array body is rejected as `Invalid GraphQL Request.`

A request batch can include an entry with variable batching only if `AllowedBatching.VariableBatching` is also enabled.

# Sending an operation batch

```http
POST /graphql?batchOperations=[GetViewer,GetProducts]
Content-Type: application/json
Accept: application/graphql-response+jsonl

{
  "query": "query GetViewer { viewer { id name } } query GetProducts { products(first: 5) { nodes { id name } } }"
}
```

Operation batching uses a single GraphQL request body containing a document with multiple named operations. The `batchOperations` query parameter selects which operations to execute and determines their index order.

Operation batching is controlled by the same `AllowedBatching.RequestBatching` flag. If the parameter syntax is invalid, the operation list is missing, the body is malformed, or request batching is disabled, the server will return a bad request such as `Invalid GraphQL Request.`

The response is streamed. Do not rely on the order of the stream. Use the `requestIndex` field in each result to match it to the selected operation.

# Reading streamed batch responses

```jsonl
{"requestIndex":1,"data":{"products":{"nodes":[{"id":"1","name":"Chai"}]}}}
{"requestIndex":0,"data":{"viewer":{"id":"me","name":"Ada"}}}
```

Batch responses are streamed as results become available. Hot Chocolate writes each item result as soon as its execution is ready. Clients must correlate results by index, not by array position or arrival order.

| Accept header                        | Response content type                | Notes                                                                      |
| ------------------------------------ | ------------------------------------ | -------------------------------------------------------------------------- |
| `application/graphql-response+jsonl` | `application/graphql-response+jsonl` | JSON Lines response stream used by the v16 HTTP client tests.              |
| `application/jsonl`                  | `application/jsonl`                  | JSON Lines response stream.                                                |
| `multipart/mixed`                    | `multipart/mixed`                    | Default streaming transport when no streaming `Accept` header is supplied. |
| `text/event-stream`                  | `text/event-stream`                  | Use when the client already supports SSE parsing.                          |

These streaming transports are also used for incremental delivery features such as `@defer` and `@stream`. While batching and incremental delivery are separate concepts, both require a client that can parse a response stream.

Result index fields:

- Request batches include `requestIndex`.
- Operation batches include `requestIndex`.
- Variable batches include `variableIndex`.
- A request-batch entry that also uses variable batching includes both `requestIndex` and `variableIndex`.

Execution errors for an item are returned with that item's result after execution starts. Malformed JSON, disabled batching, empty batches, and request batches exceeding `MaxBatchSize` can cause the entire HTTP request to fail before any item executes.

# Client examples

## .NET HTTP client

```csharp
using HotChocolate.Transport;

var batch = new OperationBatchRequest(
[
    new OperationRequest("query GetViewer { viewer { id name } }"),
    new OperationRequest("query GetProducts { products(first: 5) { nodes { id name } } }")
]);

using var response = await client.PostAsync(batch, requestUri, cancellationToken);
response.EnsureSuccessStatusCode();

await foreach (var result in response
    .ReadAsResultStreamAsync()
    .WithCancellation(cancellationToken))
{
    var requestIndex = result.RequestIndex;
    // Store the result under requestIndex.
}
```

`OperationBatchRequest` represents a JSON-array request batch. The `ReadAsResultStreamAsync()` method reads streamed results.

## JavaScript JSON Lines parser

```js
const response = await fetch("/graphql", {
  method: "POST",
  headers: {
    "content-type": "application/json",
    accept: "application/graphql-response+jsonl",
  },
  body: JSON.stringify([
    { query: "query GetViewer { viewer { id name } }" },
    { query: "query GetProducts { products(first: 5) { nodes { id name } } }" },
  ]),
});

const resultsByRequestIndex = new Map();
const reader = response.body.pipeThrough(new TextDecoderStream()).getReader();

let buffer = "";

function readLine(line) {
  if (line.length > 0) {
    const result = JSON.parse(line);
    resultsByRequestIndex.set(result.requestIndex, result);
  }
}

for (;;) {
  const { value, done } = await reader.read();

  if (done) {
    readLine(buffer.trim());
    break;
  }

  buffer += value;

  for (;;) {
    const lineEnd = buffer.indexOf("\n");

    if (lineEnd < 0) {
      break;
    }

    readLine(buffer.slice(0, lineEnd).trim());
    buffer = buffer.slice(lineEnd + 1);
  }
}
```

For `multipart/mixed`, use a multipart response parser such as `meros`. For `text/event-stream`, use an SSE parser such as `graphql-sse`.

# Protecting the server from amplified work

Batching turns a single HTTP request into multiple executions. Configure limits and monitoring based on batch items, not only requests.

| Protection                | What to do                                                                          |
| ------------------------- | ----------------------------------------------------------------------------------- |
| `MaxBatchSize`            | Set a low production limit for request-array items and variable-array entries.      |
| `MaxConcurrentExecutions` | Keep a bounded execution capacity so one client cannot occupy all server workers.   |
| `ExecutionTimeout`        | Bound long-running operations and time spent waiting for execution capacity.        |
| Cost analysis             | Enforce cost per operation. A batch can multiply total cost in one HTTP request.    |
| Request body limits       | Bound large documents, variables, and JSON arrays before parsing.                   |
| Rate limiting             | Count batch items or selected operations, not only HTTP requests.                   |
| Trusted documents         | Restrict production clients to known operation text when your client set allows it. |

`MaxBatchSize` is enforced for JSON request batches and variable batches. Keep operation batching for trusted clients unless your surrounding controls limit the number of selected operations and request size.

# Observing batched traffic

A single HTTP request may now represent multiple GraphQL operations. Include batch-aware fields in logs, traces, and metrics:

- Batch type: request batch or operation batch.
- Batch size.
- Operation names, where safe to record.
- `requestIndex` and `variableIndex` when present.
- Per-item status, errors, duration, and cost.
- Total HTTP request duration and response streaming duration.

Transport diagnostics provide `StartSingleRequest`, `StartBatchRequest`, and `StartOperationBatchRequest`. Diagnostic event handlers run on the request path, so keep them low allocation and avoid blocking I/O.

# Troubleshooting batching

| Symptom                                                       | Likely cause                                                                                                                                            | Fix                                                                                                                                                              |
| ------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Invalid GraphQL Request.`                                    | Batching is disabled, the wrong `AllowedBatching` flag is set, `batchOperations` syntax is invalid, or the body shape does not match the batching mode. | Enable `AllowedBatching.RequestBatching`, send a JSON array for request batching, or send one object plus a valid `batchOperations` list for operation batching. |
| `The GraphQL batch request has no elements.`                  | The request body is an empty JSON array.                                                                                                                | Do not send empty batches.                                                                                                                                       |
| `The batch size exceeds the maximum allowed batch size of N.` | A request array or variable array is above `MaxBatchSize`.                                                                                              | Reduce client batch size or raise the limit only after reviewing cost and capacity.                                                                              |
| Results arrive in a different order than the request body.    | The response is streamed as item executions finish.                                                                                                     | Correlate by `requestIndex` and `variableIndex`.                                                                                                                 |
| Logs show one HTTP request but high resource use.             | The request contained many executions.                                                                                                                  | Record batch size and apply item-aware rate limits.                                                                                                              |
| DataLoader did not combine work across every batch item.      | DataLoader batching is scoped to execution behavior, not the HTTP batch envelope.                                                                       | Use DataLoader for N+1 data access and transport batching for round trips.                                                                                       |
| The client cannot parse the response.                         | The `Accept` header and parser do not match the response format.                                                                                        | Request JSON Lines, multipart, or SSE only when the client supports that parser.                                                                                 |

# API reference

| API or field                                   | Meaning                                                                               |
| ---------------------------------------------- | ------------------------------------------------------------------------------------- |
| `AllowedBatching.None`                         | Default. Disables batching.                                                           |
| `AllowedBatching.RequestBatching`              | Enables JSON-array request batching and operation batching with `batchOperations`.    |
| `AllowedBatching.VariableBatching`             | Enables variable arrays for one operation.                                            |
| `AllowedBatching.All`                          | Enables request batching and variable batching.                                       |
| `GraphQLServerOptions.Batching`                | Server option for allowed batching modes. Default is `None`.                          |
| `GraphQLServerOptions.MaxBatchSize`            | Maximum request-array or variable-array size. Default is `1024`. `0` means unlimited. |
| `GraphQLServerOptions.MaxConcurrentExecutions` | Maximum concurrent GraphQL executions. Default is `64`. `null` means unlimited.       |
| `batchOperations`                              | Query parameter for operation batching.                                               |
| `requestIndex`                                 | Result field for request and operation batch correlation.                             |
| `variableIndex`                                | Result field for variable batch correlation.                                          |

# Next steps

- [Performance overview](index.md)
- [Execution depth and limits](../security/execution-depth-and-limits.md)
- [Cost analysis](../security/cost-analysis.md)
- [Trusted documents](../security/trusted-documents.md)
- [Automatic persisted operations](automatic-persisted-operations.md)
- [DataLoader](../dataloader/index.md)
- [HTTP transport](../../server/http-transport.md)
- [Instrumentation](../../server/instrumentation.md)
