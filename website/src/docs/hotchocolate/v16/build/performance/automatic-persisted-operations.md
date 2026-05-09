---
title: Automatic persisted operations
---

Automatic persisted operations (APQ), as known in Apollo clients, allow a client and Hot Chocolate to register operation documents at runtime. The client first sends a hash. If the server already has a document for that hash, it executes the operation without needing the full GraphQL document again. If the server does not have the document, the client retries with the same hash and includes the full document. Hot Chocolate then stores the document for future requests.

APQ is useful when clients frequently execute the same operations and you want to reduce repeated request payload sizes without requiring a build-time publish step. APQ is a performance optimization. It differs from trusted documents because the first successful registration still requires the client to send the operation text.

# Configuring the server

For local development, add in-memory operation document storage:

<PackageInstallation packageName="HotChocolate.PersistedOperations.InMemory" />

The in-memory storage relies on `IMemoryCache`, so be sure to register memory cache with your application services. Configure the APQ pipeline and select an explicit hash provider so both client and server agree on the hash property and format.

```csharp
builder.Services.AddMemoryCache();

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddSha256DocumentHashProvider(HashFormat.Hex)
    .UseAutomaticPersistedOperationPipeline()
    .AddInMemoryOperationDocumentStorage();
```

`UseAutomaticPersistedOperationPipeline()` sets up the runtime registration pipeline. Do not use `UsePersistedOperationPipeline()` for APQ, as that pipeline is intended for operation documents that have already been registered or published.

Hot Chocolate v16 offers these hash providers:

| Provider                             | Request property | Algorithm name | Formats                                                   |
| ------------------------------------ | ---------------- | -------------- | --------------------------------------------------------- |
| `AddMD5DocumentHashProvider(...)`    | `md5Hash`        | `md5`          | `HashFormat.Hex` or URL-safe unpadded `HashFormat.Base64` |
| `AddSha1DocumentHashProvider(...)`   | `sha1Hash`       | `sha1`         | `HashFormat.Hex` or URL-safe unpadded `HashFormat.Base64` |
| `AddSha256DocumentHashProvider(...)` | `sha256Hash`     | `sha256`       | `HashFormat.Hex` or URL-safe unpadded `HashFormat.Base64` |

By default, these provider methods use base64 if you omit the format. In production, configure the provider explicitly, especially if you use Apollo-style clients that typically send `sha256Hash` as hex.

# How the APQ handshake works

The following examples use this operation text:

```graphql
{
  __typename
}
```

Its SHA-256 hex hash is:

```text
7f56e67dd21ab3f30d1ff8b7bed08893f0a0db86449836189b361dd1e56ddb4b
```

Any change to whitespace, comments, generated output, or minification will alter the hash.

## 1. Sending a hash-only request

The client begins with an optimized request, sending variables and `operationName` if needed, along with `extensions.persistedQuery.<configuredHashName>`.

```bash
curl -i http://localhost:5000/graphql \
  -H 'Content-Type: application/json' \
  -H 'Accept: application/graphql-response+json, application/json' \
  --data '{"extensions":{"persistedQuery":{"version":1,"sha256Hash":"7f56e67dd21ab3f30d1ff8b7bed08893f0a0db86449836189b361dd1e56ddb4b"}}}'
```

The `version` field is part of the Apollo request shape. Hot Chocolate uses the configured hash property, in this case `sha256Hash`, to look up the operation document.

If the store is cold, Hot Chocolate returns a miss:

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

This APQ miss response is returned with HTTP status code `400`.

## 2. Retrying with the full document

After receiving `PersistedQueryNotFound`, the client retries using the same hash and includes the full `query` text.

```bash
curl -i http://localhost:5000/graphql \
  -H 'Content-Type: application/json' \
  -H 'Accept: application/graphql-response+json, application/json' \
  --data '{"query":"{ __typename }","extensions":{"persistedQuery":{"version":1,"sha256Hash":"7f56e67dd21ab3f30d1ff8b7bed08893f0a0db86449836189b361dd1e56ddb4b"}}}'
```

Hot Chocolate parses and validates the operation, computes the configured document hash, compares it to the supplied hash, stores the document if they match, executes the operation, and returns a receipt in `extensions.persistedQuery`.

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

## 3. Sending the hash-only request again

Subsequent requests can omit `query` and send only the hash, variables, and `operationName` if needed.

```bash
curl -i http://localhost:5000/graphql \
  -H 'Content-Type: application/json' \
  -H 'Accept: application/graphql-response+json, application/json' \
  --data '{"extensions":{"persistedQuery":{"version":1,"sha256Hash":"7f56e67dd21ab3f30d1ff8b7bed08893f0a0db86449836189b361dd1e56ddb4b"}}}'
```

```json
{
  "data": {
    "__typename": "Query"
  }
}
```

Hot Chocolate can also resolve persisted operation IDs from top-level `id` and `documentId` request fields. However, Apollo-style APQ clients usually use `extensions.persistedQuery.sha256Hash`.

# Hash-only GET requests

Once an operation is registered, a hash-only GET request can help with cacheable query traffic. GET requests are enabled by default, and `AllowedGetOperations` defaults to `AllowedGetOperations.Query`. Avoid allowing mutations over GET unless your HTTP policy requires it.

```bash
curl -g 'http://localhost:5000/graphql?extensions=%7B%22persistedQuery%22%3A%7B%22version%22%3A1%2C%22sha256Hash%22%3A%227f56e67dd21ab3f30d1ff8b7bed08893f0a0db86449836189b361dd1e56ddb4b%22%7D%7D' \
  -H 'Accept: application/graphql-response+json, application/json'
```

If a GET request is rejected before execution, check the endpoint options `EnableGetRequests` and `AllowedGetOperations`. For full details on GET, POST, and content negotiation, see [HTTP transport](/docs/hotchocolate/v16/build/server-configuration/http-transport).

# Storage options

APQ requires writable operation document storage. In-memory storage is suitable for tests and single-process demos, but it is lost on restart and is not shared across server instances.

| Backend            | Package                                             | Registration                                       | Best for                            | APQ caveat                                      |
| ------------------ | --------------------------------------------------- | -------------------------------------------------- | ----------------------------------- | ----------------------------------------------- |
| In-memory          | `HotChocolate.PersistedOperations.InMemory`         | `AddInMemoryOperationDocumentStorage()`            | Local development and tests         | Lost on restart and not shared                  |
| Redis              | `HotChocolate.PersistedOperations.Redis`            | `AddRedisOperationDocumentStorage(...)`            | Shared cache for multiple instances | Expiration causes future miss and retry traffic |
| File system        | `HotChocolate.PersistedOperations.FileSystem`       | `AddFileSystemOperationDocumentStorage(...)`       | Writable local or shared disk       | Plan cleanup and write permissions              |
| Azure Blob Storage | `HotChocolate.PersistedOperations.AzureBlobStorage` | `AddAzureBlobStorageOperationDocumentStorage(...)` | Persistent shared storage           | Plan writes, lifecycle, and cleanup             |

For multi-instance deployments, use shared storage such as Redis:

<PackageInstallation packageName="HotChocolate.PersistedOperations.Redis" />

```csharp
builder.Services.AddSingleton<IConnectionMultiplexer>(
    _ => ConnectionMultiplexer.Connect("localhost:7000"));

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddSha256DocumentHashProvider(HashFormat.Hex)
    .UseAutomaticPersistedOperationPipeline()
    .AddRedisOperationDocumentStorage(
        queryExpiration: TimeSpan.FromHours(24));
```

The `queryExpiration` parameter limits cache growth, but expired entries behave like cold entries. Clients must be able to handle `PersistedQueryNotFound` and retry with the full document.

Monitor APQ miss rate, registration retry rate, hash mismatch rate, storage read/write failures, and storage size. Plan for cold starts, Redis flushes, operation text changes, hash provider changes, and deployment rollbacks.

# APQ, persisted operations, and trusted documents

| Feature              | Primary goal                   | How documents reach the server                | Security boundary                                               |
| -------------------- | ------------------------------ | --------------------------------------------- | --------------------------------------------------------------- |
| APQ                  | Reduce repeated request size   | Client registers by hash at runtime           | No, clients can introduce new document text during registration |
| Persisted operations | Store and load documents by ID | Registered by code, storage, registry, or APQ | Mechanism, not a policy by itself                               |
| Trusted documents    | Allow only known operations    | Published before production traffic uses them | Yes, when enforced with trusted-document policy                 |

APQ does not reduce client bundle size, since the client still needs the operation text for a miss retry. If you want production clients to execute only reviewed operations, use [trusted documents](/docs/hotchocolate/v16/build/security/trusted-documents) rather than relying on APQ alone.

# Failure responses and troubleshooting

## `PersistedQueryNotFound` occurs every time

Check for these possible causes:

- The client never sends the full-document retry.
- The server uses `UsePersistedOperationPipeline()` instead of `UseAutomaticPersistedOperationPipeline()`.
- No `IOperationDocumentStorage` implementation is registered.
- In-memory storage was cleared by restart, or the next request reached a different instance.
- Redis `queryExpiration` expired the entry, or the backing store was flushed.
- The client sends `sha256Hash`, but the server is configured for `md5Hash` or `sha1Hash`.
- The client and server disagree on hex versus Hot Chocolate URL-safe unpadded base64.

## The response contains `persisted: false`

Hot Chocolate did not store the operation because the supplied hash did not match the computed hash for the full document.

```json
{
  "data": {
    "__typename": "Query"
  },
  "extensions": {
    "persistedQuery": {
      "sha256Hash": "wrong-hash",
      "expectedHashValue": "7f56e67dd21ab3f30d1ff8b7bed08893f0a0db86449836189b361dd1e56ddb4b",
      "expectedHashType": "sha256Hash",
      "expectedHashFormat": "Hex",
      "persisted": false
    }
  }
}
```

Recompute the hash from the exact UTF-8 operation text sent over HTTP. Check whitespace, comments, generated documents, minification, algorithm, format, and request property name.

## The request is rejected before execution

Possible causes include:

- The request contains malformed JSON in `extensions`.
- The request does not include `query`, `id`, `documentId`, or a recognizable configured hash property.
- A top-level ID or extracted hash contains invalid characters.
- HTTP GET is disabled, or the operation type is not allowed over GET.
- The `Accept` header cannot be negotiated.

## Hash-only GET responses are not cached

Consider these causes:

- The client still uses POST.
- The query string is not encoded consistently.
- Variables differ between requests.
- The CDN does not cache GraphQL endpoint responses by default.
- The first miss response was cached. Cache the later successful hash-only response, not the miss.

## Old examples use persisted query API names

Hot Chocolate renamed persisted query APIs to persisted operation APIs in earlier versions. For v16, use `UseAutomaticPersistedOperationPipeline()`, `UsePersistedOperationPipeline()`, and `AddInMemoryOperationDocumentStorage()`.

# Security considerations

APQ reduces transport overhead, but it does not prevent arbitrary operation registration. During a miss retry, parsing, validation, authorization, request limits, and cost analysis still apply. For public APIs, maintain request limits, cost analysis, authentication, authorization, introspection policy, and rate limiting.

Do not enable `OnlyAllowPersistedDocuments` as part of basic APQ setup. That option is for trusted-document enforcement and changes whether runtime registration is possible. Move to trusted documents when your release process can publish every operation before production traffic uses it.

# Next steps

- Read [Trusted documents](/docs/hotchocolate/v16/build/security/trusted-documents) if you need an allowlist of known operations.
- See [HTTP transport](/docs/hotchocolate/v16/build/server-configuration/http-transport) for GET, POST, and content negotiation details.
- Review [request limits](/docs/hotchocolate/v16/build/security/execution-depth-and-limits) and [cost analysis](/docs/hotchocolate/v16/build/security/cost-analysis) for defense in depth.
- Use shared storage before running APQ behind multiple server instances.
