---
title: "Persisted operations"
---

Persisted operations reduce the work your Hot Chocolate server does for repeated GraphQL requests. Instead of sending the full operation document every time, the client sends a stable operation ID or hash.

**Regular request**

```json
{
  "query": "query GetViewer { viewer { name } }"
}
```

**Persisted request**

```json
{
  "id": "de2f6441dad1bc388d4782e610e68b0907e9ec9bbe41b6a966426ee764afb6e5",
  "variables": {}
}
```

Use this page to choose and configure persisted operations for a Hot Chocolate v16 server. Fusion gateway persisted operations are configured separately and are not covered here.

# How persisted operations improve performance

Persisted operations make operation identity stable. On a hit, Hot Chocolate can resolve a known operation document before the normal parser, validator, and operation planning stages do work for a client-supplied document.

The exact savings depend on the mode and storage:

- Automatic persisted operations (APQ) reduce request bytes after the first request and improve document cache consistency.
- Trusted documents let first-party clients send only an operation ID. In strict mode, `OnlyAllowPersistedDocuments = true` with `AllowDocumentBody = false` tells the HTTP parser not to read the incoming `query` body.
- Stored documents can still be parsed when the storage returns text. The document cache and storage implementation determine whether Hot Chocolate can reuse a parsed `DocumentNode`.
- Validation still runs unless the document is cached or you enable `SkipPersistedDocumentValidation` for prevalidated trusted documents.
- Stable IDs make GET routes and CDN cache keys deterministic.

# Choose APQ or trusted documents

| Mode                                 | Best for                                                                  | Registration time         | First request behavior                                                            | Parser and validation posture                                                                                  | Storage requirement                                  | CDN friendliness                                                                           | Security posture                      |
| ------------------------------------ | ------------------------------------------------------------------------- | ------------------------- | --------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------- | ------------------------------------------------------------------------------------------ | ------------------------------------- |
| Automatic persisted operations (APQ) | Public APIs, partner APIs, or clients where build-time publishing is hard | Runtime                   | Hash-only request misses, then the client retries with the full document and hash | The fallback document is parsed and validated before it is stored                                              | Shared storage is recommended for production         | Good with GET, but miss and fallback traffic still exists                                  | Performance feature, not an allowlist |
| Trusted documents                    | First-party web, mobile, and internal clients you control                 | Build or release pipeline | ID executes only when storage already contains the document                       | Strict mode can avoid parsing incoming documents. Validation can be skipped only after publish-time validation | Storage must be populated before clients use new IDs | Excellent with persisted routes such as `/graphql/persisted/{operationId}/{operationName}` | Can reject ad-hoc operation shapes    |

Choose APQ when clients need runtime registration. Choose trusted documents when production should execute only operations you published ahead of time.

# Check prerequisites before you start

Before you copy the code, decide these details:

- You run a Hot Chocolate v16 server.
- Your client can send Apollo-style APQ extensions or Hot Chocolate persisted operation `id` requests.
- You chose one operation document storage provider.
- The client, publish pipeline, storage keys, and server use the same hash algorithm and format.
- Trusted documents have an extraction and publish workflow, for example Relay, Strawberry Shake, Nitro client registry, or CI artifacts that write `.graphql` files.
- CDN caching has a cache-control policy and a strategy for authorization and `Vary` headers.

Install the package that matches your storage:

| Storage               | Package                                             |
| --------------------- | --------------------------------------------------- |
| In memory             | `HotChocolate.PersistedOperations.InMemory`         |
| Redis                 | `HotChocolate.PersistedOperations.Redis`            |
| File system           | `HotChocolate.PersistedOperations.FileSystem`       |
| Azure Blob Storage    | `HotChocolate.PersistedOperations.AzureBlobStorage` |
| Nitro client registry | `ChilliCream.Nitro`                                 |

# Enable APQ for runtime registration

APQ lets a client optimistically send a hash. If the server does not know the hash, the client retries with the full operation document and the same hash.

Install the in-memory storage package for a local walkthrough:

<PackageInstallation packageName="HotChocolate.PersistedOperations.InMemory" />

Configure the APQ pipeline and storage:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .UseAutomaticPersistedOperationPipeline()
    .AddInMemoryOperationDocumentStorage();

var app = builder.Build();

app.MapGraphQL();

app.Run();

public sealed class Query
{
    public string Viewer => "Ada";
}
```

Hot Chocolate v16 registers MD5 hex as the default server hash provider. Configure the provider explicitly when your client expects a different algorithm or format. Apollo APQ clients commonly use SHA-256 hex:

```csharp
builder.Services.AddMemoryCache();

builder
    .AddGraphQL()
    .AddSha256DocumentHashProvider(HashFormat.Hex)
    .AddQueryType<Query>()
    .UseAutomaticPersistedOperationPipeline()
    .AddInMemoryOperationDocumentStorage();
```

The explicit `AddMD5DocumentHashProvider()`, `AddSha1DocumentHashProvider()`, and `AddSha256DocumentHashProvider()` overloads default to Base64 when you omit `HashFormat`, so pass `HashFormat.Hex` when you want URL-friendly hex IDs.

## Verify the APQ flow

The SHA-256 hex hash below belongs to the exact UTF-8 document `{ __typename }`.

1. Send a hash-only GET request.

```bash
curl -g 'http://localhost:5000/graphql?extensions={"persistedQuery":{"version":1,"sha256Hash":"7f56e67dd21ab3f30d1ff8b7bed08893f0a0db86449836189b361dd1e56ddb4b"}}'
```

Expected response:

```json
{
  "errors": [
    {
      "message": "PersistedQueryNotFound",
      "extensions": {
        "code": "HC0020"
      }
    }
  ]
}
```

2. Retry with the full document and the same hash.

```bash
curl -g 'http://localhost:5000/graphql?query={%20__typename%20}&extensions={"persistedQuery":{"version":1,"sha256Hash":"7f56e67dd21ab3f30d1ff8b7bed08893f0a0db86449836189b361dd1e56ddb4b"}}'
```

Expected response:

```json
{
  "data": {
    "__typename": "Query"
  },
  "extensions": {
    "persistedQuery": {
      "sha256Hash": "7f56e67dd21ab3f30d1ff8b7bed08893f0a0db86449836189b361dd1e56ddb4b",
      "persisted": true
    }
  }
}
```

3. Send the hash-only request again.

```bash
curl -g 'http://localhost:5000/graphql?extensions={"persistedQuery":{"version":1,"sha256Hash":"7f56e67dd21ab3f30d1ff8b7bed08893f0a0db86449836189b361dd1e56ddb4b"}}'
```

Expected response:

```json
{
  "data": {
    "__typename": "Query"
  }
}
```

APQ also works with POST requests. GET is useful when you want HTTP caches to see a deterministic URL.

For production APQ, avoid per-process in-memory storage unless cold misses are acceptable. A server restart or load-balanced request to another instance causes a repeat miss. Use Redis or another shared store when hit rate matters across instances.

# Enable trusted documents for first-party clients

Trusted documents are persisted operations you publish before clients send traffic. The server loads the operation by ID, and strict mode rejects ad-hoc documents.

Install file-system storage for a self-contained example:

<PackageInstallation packageName="HotChocolate.PersistedOperations.FileSystem" />

Create this file:

```text
persisted_operations/0c4bd0ac822f9b98f47e85adaaa049fb68636497cfdee924311521f106051855.graphql
```

```graphql
query Viewer {
  viewer
}
```

The file name uses the SHA-256 hex hash of the exact document text `query Viewer { viewer }`.

Configure the server:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQL()
    .AddSha256DocumentHashProvider(HashFormat.Hex)
    .AddQueryType<Query>()
    .UsePersistedOperationPipeline()
    .AddFileSystemOperationDocumentStorage("./persisted_operations")
    .ModifyRequestOptions(o =>
    {
        o.PersistedOperations.OnlyAllowPersistedDocuments = true;
        o.PersistedOperations.AllowDocumentBody = false;
    });

var app = builder.Build();

app.MapGraphQL();
app.MapGraphQLPersistedOperations();

app.Run();

public sealed class Query
{
    public string Viewer => "Ada";
}
```

A standard persisted operation POST sends `id`, variables, and extensions. It does not send `query`:

```bash
curl http://localhost:5000/graphql \
  -H 'Content-Type: application/json' \
  -d '{"id":"0c4bd0ac822f9b98f47e85adaaa049fb68636497cfdee924311521f106051855"}'
```

Expected response:

```json
{
  "data": {
    "viewer": "Ada"
  }
}
```

The route-based endpoint puts the operation ID and optional operation name in the URL:

```http
GET /graphql/persisted/0c4bd0ac822f9b98f47e85adaaa049fb68636497cfdee924311521f106051855/Viewer
```

For POST route requests, send only `variables` and `extensions` in the body:

```json
{
  "variables": {}
}
```

`MapGraphQLPersistedOperations()` maps `/graphql/persisted/{operationId}` and `/graphql/persisted/{operationId}/{operationName}`. Use `requireOperationName: true` when you want every route request to include the operation name:

```csharp
app.MapGraphQLPersistedOperations(requireOperationName: true);
```

## Migrate clients safely

During migration, you can require that every operation matches storage while still accepting a full document body:

```csharp
builder
    .AddGraphQL()
    .AddSha256DocumentHashProvider(HashFormat.Hex)
    .AddQueryType<Query>()
    .UsePersistedOperationPipeline()
    .AddFileSystemOperationDocumentStorage("./persisted_operations")
    .ModifyRequestOptions(o =>
    {
        o.PersistedOperations.OnlyAllowPersistedDocuments = true;
        o.PersistedOperations.AllowDocumentBody = true;
    });
```

This compatibility mode is useful for legacy clients that still send `query`. It is not the final strict mode. Move to `AllowDocumentBody = false` after clients send IDs or persisted routes.

Use `AllowNonPersistedOperation()` from an authenticated [HTTP request interceptor](/docs/hotchocolate/v16/server/interceptors) only for trusted development or administrative scenarios.

# Skip repeated validation only after publish-time validation

`SkipPersistedDocumentValidation` marks loaded persisted documents as already validated:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .UsePersistedOperationPipeline()
    .AddFileSystemOperationDocumentStorage("./persisted_operations")
    .ModifyRequestOptions(o =>
    {
        o.PersistedOperations.OnlyAllowPersistedDocuments = true;
        o.PersistedOperations.AllowDocumentBody = false;
        o.PersistedOperations.SkipPersistedDocumentValidation = true;
    });
```

Enable this only when every stored operation was validated against the exact schema during CI or publish. Keep validation on for APQ and for migration modes that accept full document bodies, because those paths still process runtime input. Schema changes can invalidate old operations, so your publish pipeline or registry must validate operations before the schema and clients reach production.

# Pick storage for your deployment shape

| Provider           | Package                                             | Method                                                                     | Durability                     | Multi-instance behavior                                               | Best for APQ                          | Best for trusted documents                                | Common failure                                                          |
| ------------------ | --------------------------------------------------- | -------------------------------------------------------------------------- | ------------------------------ | --------------------------------------------------------------------- | ------------------------------------- | --------------------------------------------------------- | ----------------------------------------------------------------------- |
| In memory          | `HotChocolate.PersistedOperations.InMemory`         | `.AddInMemoryOperationDocumentStorage()`                                   | Lost on restart                | Per process                                                           | Local development, tests, demos       | Tests only                                                | Cold misses after restart or load-balanced routing                      |
| Redis              | `HotChocolate.PersistedOperations.Redis`            | `.AddRedisOperationDocumentStorage(..., TimeSpan? queryExpiration = null)` | Durable as configured in Redis | Shared                                                                | Production APQ                        | Shared trusted storage when your publish job writes Redis | TTL expiration, wrong database, different Redis instance                |
| File system        | `HotChocolate.PersistedOperations.FileSystem`       | `.AddFileSystemOperationDocumentStorage("./persisted_operations")`         | Durable on disk or image       | Shared only when the directory is shared or baked into every instance | Small deployments or warmup workflows | CI-published artifacts                                    | Missing files, wrong path, permissions, container image mismatch        |
| Azure Blob Storage | `HotChocolate.PersistedOperations.AzureBlobStorage` | `.AddAzureBlobStorageOperationDocumentStorage(...)`                        | Shared durable object storage  | Shared                                                                | Cloud APQ when latency is acceptable  | Cloud-published artifacts                                 | Missing container, lifecycle policy deletes operations, storage latency |
| Custom             | Your package                                        | Implement `IOperationDocumentStorage`                                      | Your choice                    | Your choice                                                           | Specialized cache topology            | Registry integration                                      | Incorrect key mapping or missing save semantics                         |

In-memory storage requires `builder.Services.AddMemoryCache()`. Redis accepts an optional `queryExpiration`, which is useful for APQ churn but risky for trusted documents unless your publish process refreshes entries before clients need them.

File-system storage maps IDs to `{id}.graphql`. For Base64 IDs, Hot Chocolate maps `/` to `-`, maps `+` to `_`, removes trailing `=`, and appends `.graphql`. Hex IDs avoid path encoding problems.

Azure Blob Storage uses blob names ending in `.graphql`. Make sure the container exists and that lifecycle rules do not remove operations still used by web caches, mobile apps, or older client versions.

# Match hashes and request shapes

APQ uses the `extensions.persistedQuery` object:

```json
{
  "extensions": {
    "persistedQuery": {
      "version": 1,
      "sha256Hash": "7f56e67dd21ab3f30d1ff8b7bed08893f0a0db86449836189b361dd1e56ddb4b"
    }
  }
}
```

Hot Chocolate persisted operation requests use `id`:

```json
{
  "id": "0c4bd0ac822f9b98f47e85adaaa049fb68636497cfdee924311521f106051855",
  "variables": {}
}
```

Persisted operation routes use the path:

```http
GET /graphql/persisted/0c4bd0ac822f9b98f47e85adaaa049fb68636497cfdee924311521f106051855/Viewer?variables={}
```

Keep these values aligned:

- Hash providers: `AddMD5DocumentHashProvider`, `AddSha1DocumentHashProvider`, and `AddSha256DocumentHashProvider`.
- Hash formats: `HashFormat.Hex` and `HashFormat.Base64`.
- APQ JSON fields: `md5Hash`, `sha1Hash`, and `sha256Hash`.
- Apollo APQ clients usually expect SHA-256 hex.
- Relay persisted query examples often use `doc_id`. Map that value to Hot Chocolate's `id` field or put the ID in the persisted route.
- Base64 IDs in routes must be URL-safe or encoded. Hex avoids `/`, `+`, and `=` issues.

# Roll out clients without cold-cache incidents

## APQ rollout checklist

1. Enable APQ with full-document fallback.
2. Use shared storage before load-balanced rollout.
3. Monitor miss rate and fallback rate.
4. Warm critical operations by issuing fallback requests after deploy when cold starts matter.
5. Tune Redis expiration only after you know operation reuse patterns.

## Trusted document rollout checklist

1. Extract operations in CI.
2. Validate operations against the schema.
3. Publish or upload operations before deploying clients that reference them.
4. Keep old operation versions for mobile apps, service workers, cached web clients, and blue/green deployments.
5. Canary clients before you set `AllowDocumentBody = false` everywhere.
6. Roll back by keeping the previous operation set available or republishing the previous client version.

Relay can write a persisted query manifest during the client build:

```js
// relay.config.js
module.exports = {
  src: "./src",
  schema: "./schema.graphql",
  persistConfig: {
    file: "./persisted_queries.json",
    algorithm: "SHA256",
  },
};
```

A typical trusted-document release flow is: extract operations, validate them against the schema, publish them to storage or a registry, deploy the server, deploy the client, then monitor misses and rejections.

# Use GET and CDN caching for cacheable queries

Persisted routes are good CDN keys because the operation identity is in the path:

```http
GET /graphql/persisted/0c4bd0ac822f9b98f47e85adaaa049fb68636497cfdee924311521f106051855/Viewer?variables={}
```

Use persisted routes for cacheable query operations. Default GET handling is intended for queries, not mutations. Variables and extensions still affect the response, so include them in the CDN cache key. For authenticated or personalized data, configure `Cache-Control`, `Vary`, authorization headers, and CDN rules before allowing shared caching.

Require operation names when it helps observability and cache keys:

```csharp
app.MapGraphQLPersistedOperations(requireOperationName: true);
```

Pair persisted routes with response caching features where appropriate. See [cache control](/docs/hotchocolate/v16/server/cache-control), [HTTP transport](/docs/hotchocolate/v16/server/http-transport), and [endpoints](/docs/hotchocolate/v16/server/endpoints#mapgraphqlpersistedoperations) for transport and header details.

# Measure the improvement

Track baseline metrics before enabling persisted operations, then compare repeated operations after rollout.

Measure:

- Request payload bytes before and after.
- p50, p95, and p99 latency for repeated operations.
- APQ `PersistedQueryNotFound` rate.
- Storage hits, misses, and read latency.
- Diagnostic events: `RetrievedDocumentFromStorage`, `DocumentNotFoundInStorage`, and `UntrustedDocumentRejected`.
- APQ saves through `ExecutionContextData.DocumentSaved` or the persistence receipt with `persisted: true`.
- Parse, validation, and operation planning time from your instrumentation.
- Prepared operation cache hit, miss, and eviction signals when exposed.
- CDN hit rate and origin request reduction.

Useful metric names in your own telemetry can mirror the behavior, for example `graphql.persisted.storage.hit`, `graphql.persisted.storage.miss`, `graphql.apq.save`, `graphql.persisted.rejected`, and `graphql.persisted.route.cdn_hit`. See [instrumentation](/docs/hotchocolate/v16/server/instrumentation) for diagnostic integration.

# Understand security and query-limit trade-offs

| Option or API                     | What it does                                                    | Performance effect                           | Use with care                                           |
| --------------------------------- | --------------------------------------------------------------- | -------------------------------------------- | ------------------------------------------------------- |
| `OnlyAllowPersistedDocuments`     | Requires an operation to match persisted storage                | Blocks unknown operation shapes              | Enable after storage and clients are ready              |
| `AllowDocumentBody`               | Allows a full `query` body when it matches a persisted document | Keeps parser work for compatibility traffic  | Use during migration, then disable                      |
| `SkipPersistedDocumentValidation` | Treats loaded persisted documents as validated                  | Avoids repeated validation                   | Enable only after publish-time validation               |
| `OperationNotAllowedError`        | Customizes the strict-mode rejection error                      | No direct performance effect                 | Keep errors understandable for clients                  |
| `AllowNonPersistedOperation()`    | Bypasses strict mode for one request                            | Reintroduces ad-hoc parsing for that request | Protect with development checks or strong authorization |

APQ is a performance feature, not a strict allowlist. Trusted documents can reject ad-hoc operations when strict options are enabled. Request limits, cost analysis, authentication, and authorization still matter for public APIs, APQ fallback paths, and migration paths. See [request limits](/docs/hotchocolate/v16/securing-your-api/request-limits) and [cost analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis).

# Troubleshoot misses and rejected operations

| Symptom                                                                                   | Likely cause                                                                                       | How to confirm                                                                  | Fix                                                                          |
| ----------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------- | ---------------------------------------------------------------------------- |
| `PersistedQueryNotFound` with `HC0020` on the first APQ hash-only request                 | Expected APQ miss                                                                                  | Check that the request contains only `extensions.persistedQuery`                | Send the fallback request with full `query` and matching hash                |
| `PersistedQueryNotFound` repeats for APQ                                                  | Storage is per-process, expired, not shared, or the hash provider changed                          | Compare instance IDs, Redis TTL, storage contents, and configured hash provider | Use shared storage, adjust TTL, or align hash settings                       |
| `The specified persisted operation key is invalid.` with `HC0020`                         | Trusted document ID not found                                                                      | Look for `DocumentNotFoundInStorage` and inspect the requested key              | Publish the operation, fix the ID, or correct the route path                 |
| `Only persisted operations are allowed.`                                                  | Strict mode received no persisted ID or received a document body while `AllowDocumentBody = false` | Capture the HTTP body and route                                                 | Send `id`, use persisted routes, or use compatibility mode during migration  |
| `persisted: false` with `expectedHashValue`, `expectedHashType`, and `expectedHashFormat` | APQ fallback hash does not match the document or provider                                          | Compare the exact UTF-8 document text and configured provider                   | Recompute the hash and align algorithm and format                            |
| Route ID fails when using Base64                                                          | ID contains `/`, `+`, or `=` and was not encoded safely                                            | Inspect the raw URL path                                                        | URL-encode the ID, use Hot Chocolate's file mapping for files, or prefer hex |
| File-system misses                                                                        | Wrong directory, missing `{id}.graphql`, permission issue, or container did not include artifacts  | List the mounted directory in the running container                             | Copy files before startup and fix permissions or paths                       |
| Redis misses                                                                              | Wrong database, different Redis instance, expired TTL, or custom tooling wrote different keys      | Inspect Redis keys and TTL                                                      | Point all instances at the same database and align key format                |
| Relay sends `doc_id`                                                                      | Relay example naming does not match Hot Chocolate request naming                                   | Capture the POST body                                                           | Map `doc_id` to `id` in the network layer                                    |
| CDN caches wrong data                                                                     | Variables, extensions, or auth were omitted from the cache key                                     | Compare cached responses for different users or variables                       | Fix cache key, `Vary`, and `Cache-Control` rules                             |

# Go deeper next

- [Trusted documents hardening](/docs/hotchocolate/v16/operations/security-hardening/trusted-documents)
- [Private API guide](/docs/hotchocolate/v16/guides/private-api)
- [Automatic persisted operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations)
- [Trusted documents](/docs/hotchocolate/v16/performance/trusted-documents)
- [HTTP transport](/docs/hotchocolate/v16/server/http-transport)
- [Endpoints](/docs/hotchocolate/v16/server/endpoints#mapgraphqlpersistedoperations)
- [Cache control](/docs/hotchocolate/v16/server/cache-control)
- [Request limits](/docs/hotchocolate/v16/securing-your-api/request-limits)
- [Cost analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis)
- [Interceptors](/docs/hotchocolate/v16/server/interceptors)
- [Instrumentation](/docs/hotchocolate/v16/server/instrumentation)
- [Nitro client registry](/docs/nitro/apis/client-registry)
