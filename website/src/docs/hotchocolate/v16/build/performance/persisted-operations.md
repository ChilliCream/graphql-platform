---
title: Persisted operations
---

Persisted operations allow a Hot Chocolate server to execute operation documents that have been published to operation document storage before any requests arrive. Instead of sending the full GraphQL document, a client sends a stable operation document ID, often a hash of the document, and Hot Chocolate retrieves the stored document before proceeding with execution.

Persisted operations are best used when you control the client build and release process. They help reduce request size, provide stable IDs for HTTP caching and observability, and enable you to validate the set of client operations before deployment.

> Persisted operations are distinct from trusted document policies. Configuring the persisted operation pipeline instructs Hot Chocolate to load documents by ID, but it does not block standard `query` requests unless you explicitly enable a persisted-only policy.

# Choosing between persisted operations, APQ, and trusted documents

| Feature                        | Registration time           | Client sends                                                                     | Runtime storage writes | Best fit                                                      | Learn more                                                                                                |
| ------------------------------ | --------------------------- | -------------------------------------------------------------------------------- | ---------------------- | ------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------- |
| Persisted operations           | Before or during deployment | `id`, route ID, or `extensions.persistedQuery` for an already published document | Usually none           | First-party clients with CI control                           | This page                                                                                                 |
| Automatic persisted operations | After a runtime miss        | Hash first, then hash plus full `query` on a miss                                | Yes                    | Clients that cannot publish operation documents ahead of time | [Automatic persisted operations](/docs/hotchocolate/v16/build/performance/automatic-persisted-operations) |
| Trusted documents              | Before production traffic   | Approved operation IDs, sometimes matching document bodies                       | Usually none           | Allow-list security posture                                   | [Trusted documents](/docs/hotchocolate/v16/build/security/trusted-documents)                              |

Select APQ if you cannot publish the operation set before clients begin sending requests. Use trusted documents when your primary goal is to reject any non-approved documents. Persisted operations provide the storage and lookup foundation for both production publishing and trusted-document enforcement.

# How the request flow works

A typical production workflow includes the following steps:

1. Extract operation documents from client source code.
2. Assign each operation document an ID, commonly a hash.
3. Validate every document against the schema version that will serve it.
4. Publish the documents to operation document storage.
5. Deploy the server and the published operation set.
6. Deploy clients that send operation IDs instead of full documents.
7. Keep old IDs available while older clients may still send them.

At runtime:

1. The HTTP layer reads an operation document ID from the request `id`, from Apollo-style `extensions.persistedQuery`, or from the dedicated persisted operation route.
2. `UsePersistedOperationPipeline()` includes `UseReadPersistedOperation()`, which reads from `IOperationDocumentStorage` when the request has an ID and no document body.
3. Hot Chocolate validates the ID format before looking up storage. Valid IDs contain only `a-z`, `A-Z`, `0-9`, `-`, and `_`.
4. If the ID is not found, Hot Chocolate returns HTTP 400 with error code `HC0020` and the message `The specified persisted operation key is invalid.`
5. If the document is found, Hot Chocolate continues through document caching, parsing or loaded document handling, validation, operation caching, variable coercion, and execution.

Persisted operations enhance performance by reducing network payloads and providing caches with a stable key. By default, Hot Chocolate still validates persisted documents. You can skip validation with `SkipPersistedDocumentValidation`, but only if your publish pipeline has already validated the exact operation set against the deployed schema.

# Configuring the persisted operation pipeline

To get started, add a storage provider package and configure the persisted operation request pipeline:

<PackageInstallation packageName="HotChocolate.PersistedOperations.FileSystem" />

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .UsePersistedOperationPipeline()
    .AddFileSystemOperationDocumentStorage("./persisted_operations");
```

`UsePersistedOperationPipeline()` enables lookup by operation document ID. By default, standard GraphQL requests with a `query` body are still accepted. Refer to the trusted documents guidance if you want to reject non-persisted requests.

# Choosing operation document storage

Hot Chocolate uses `IOperationDocumentStorage` as the storage abstraction.

```csharp
public interface IOperationDocumentStorage
{
    ValueTask<IOperationDocument?> TryReadAsync(
        OperationDocumentId documentId,
        CancellationToken cancellationToken = default);

    ValueTask SaveAsync(
        OperationDocumentId documentId,
        IOperationDocument document,
        CancellationToken cancellationToken = default);
}
```

| Provider           | Package                                             | Registration API                                   | Durable                             | Runtime writes | Production notes                                                                                                                     |
| ------------------ | --------------------------------------------------- | -------------------------------------------------- | ----------------------------------- | -------------- | ------------------------------------------------------------------------------------------------------------------------------------ |
| File system        | `HotChocolate.PersistedOperations.FileSystem`       | `AddFileSystemOperationDocumentStorage(...)`       | Yes, if deployed with durable files | Yes            | Good for containers or app deployments where operation files are versioned with the release.                                         |
| Redis              | `HotChocolate.PersistedOperations.Redis`            | `AddRedisOperationDocumentStorage(...)`            | Depends on Redis configuration      | Yes            | Good for shared storage across replicas. Avoid expiration for pre-published documents unless CI repopulates before clients use them. |
| Azure Blob Storage | `HotChocolate.PersistedOperations.AzureBlobStorage` | `AddAzureBlobStorageOperationDocumentStorage(...)` | Yes                                 | Yes            | Good for shared cloud storage. The container must exist. Lifecycle rules must not remove active operation documents.                 |
| In-memory          | `HotChocolate.PersistedOperations.InMemory`         | `AddInMemoryOperationDocumentStorage()`            | No                                  | Yes            | Use for local development, tests, demos, and APQ examples. Not durable across restarts.                                              |
| Custom             | Your implementation                                 | Register `IOperationDocumentStorage`               | Your choice                         | Your choice    | Use for existing registries, tenant-aware stores, or custom observability.                                                           |

## File system storage

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .UsePersistedOperationPipeline()
    .AddFileSystemOperationDocumentStorage("./persisted_operations");
```

If no path is passed, the default root is `persisted_operations`. The default file map reads and writes `{documentId}.graphql` files. For example:

```text
persisted_operations/0c95d31ca29272475bf837f944f4e513.graphql
```

```graphql
query GetViewer {
  viewer {
    id
    name
  }
}
```

Deploy this directory with the application or container. Update it atomically so every running server instance can read the IDs that active clients send.

## Redis storage

```csharp
using StackExchange.Redis;

builder.Services.AddSingleton<IConnectionMultiplexer>(
    _ => ConnectionMultiplexer.Connect("localhost:6379"));

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .UsePersistedOperationPipeline()
    .AddRedisOperationDocumentStorage();
```

You can also provide the database yourself:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .UsePersistedOperationPipeline()
    .AddRedisOperationDocumentStorage(services =>
        services.GetRequiredService<IConnectionMultiplexer>().GetDatabase());
```

Redis keys are operation document IDs. Values are GraphQL operation documents. An optional expiration can be provided:

```csharp
.AddRedisOperationDocumentStorage(
    services => services.GetRequiredService<IConnectionMultiplexer>().GetDatabase(),
    queryExpiration: TimeSpan.FromHours(12));
```

Expiration is useful for APQ or temporary stores. For pre-published persisted operations, expiration can break old clients unless your publish job restores the key before any client uses it.

## Azure Blob Storage

<PackageInstallation packageName="HotChocolate.PersistedOperations.AzureBlobStorage" />

```csharp
using Azure.Storage.Blobs;

builder.Services.AddSingleton(_ =>
    new BlobServiceClient(builder.Configuration.GetConnectionString("Storage")));

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .UsePersistedOperationPipeline()
    .AddAzureBlobStorageOperationDocumentStorage(services =>
        services
            .GetRequiredService<BlobServiceClient>()
            .GetBlobContainerClient("hotchocolate"));
```

The blob container must already exist. Blob names are based on the document ID with a `.graphql` suffix. Saved blobs use the `application/graphql` content type and cache headers.

## In-memory storage

<PackageInstallation packageName="HotChocolate.PersistedOperations.InMemory" />

```csharp
builder.Services.AddMemoryCache();

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .UsePersistedOperationPipeline()
    .AddInMemoryOperationDocumentStorage();
```

In-memory storage is process-local. Use it for development and tests, not for production clients that depend on IDs remaining available after restart.

## Custom storage

Register a custom `IOperationDocumentStorage` when your operation registry lives in another system.

```csharp
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.PersistedOperations;

public sealed class DatabaseOperationDocumentStorage : IOperationDocumentStorage
{
    private readonly OperationDocumentDbContext _dbContext;

    public DatabaseOperationDocumentStorage(OperationDocumentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<IOperationDocument?> TryReadAsync(
        OperationDocumentId documentId,
        CancellationToken cancellationToken = default)
    {
        var body = await _dbContext.OperationDocuments
            .Where(t => t.Id == documentId.Value)
            .Select(t => t.Body)
            .SingleOrDefaultAsync(cancellationToken);

        return body is null
            ? null
            : new OperationDocument(Utf8GraphQLParser.Parse(body));
    }

    public async ValueTask SaveAsync(
        OperationDocumentId documentId,
        IOperationDocument document,
        CancellationToken cancellationToken = default)
    {
        _dbContext.OperationDocuments.Add(new OperationDocumentEntity
        {
            Id = documentId.Value,
            Body = document.ToString()
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
```

```csharp
builder.Services.AddScoped<IOperationDocumentStorage, DatabaseOperationDocumentStorage>();
```

For regular persisted operations, prioritize reliable reads during request execution. If the same storage is also used for APQ, `SaveAsync` must be safe under concurrent runtime writes.

# Generate, validate, and publish documents

Hot Chocolate does not require a specific client tool. The important part is that the client, server, and publish job agree on the operation document ID and operation text.

A CI workflow can be:

1. Export the server schema for the release.
2. Extract operations from Relay, Strawberry Shake, Nitro client registry, or another client build tool.
3. Generate IDs with the hash algorithm and format used by the server.
4. Validate every operation against the release schema.
5. Publish the operation documents to the selected storage provider.
6. Deploy the storage update before, or at the same time as, clients that send the new IDs.
7. Retain old IDs until older clients are no longer active.

Many tools produce a JSON map from operation ID to document text:

```json
{
  "0c95d31ca29272475bf837f944f4e513": "query GetViewer { viewer { id name } }"
}
```

For file system storage, a publish step can convert that map into `.graphql` files:

```js
import { mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { join } from "node:path";

const source = JSON.parse(readFileSync("persisted_operations.json", "utf8"));
const target = "persisted_operations";

mkdirSync(target, { recursive: true });

for (const [id, document] of Object.entries(source)) {
  writeFileSync(join(target, `${id}.graphql`), document);
}
```

This script is an illustration of the artifact shape. Use the validation and publishing tools that fit your client stack. For managed validation, client versioning, and operation publishing, see the [Nitro client registry](/docs/nitro/apis/client-registry).

# Configure operation document IDs and hashes

An operation document ID is the value used for lookup. A hash is one way to create that ID.

Hot Chocolate supports these document hash providers:

```csharp
builder
    .AddGraphQL()
    .AddSha256DocumentHashProvider(HashFormat.Hex)
    .AddQueryType<Query>()
    .UsePersistedOperationPipeline()
    .AddFileSystemOperationDocumentStorage("./persisted_operations");
```

| API                                  | Algorithm | Default format when called |
| ------------------------------------ | --------- | -------------------------- |
| `AddMD5DocumentHashProvider(...)`    | MD5       | `HashFormat.Base64`        |
| `AddSha1DocumentHashProvider(...)`   | SHA1      | `HashFormat.Base64`        |
| `AddSha256DocumentHashProvider(...)` | SHA256    | `HashFormat.Base64`        |

The built-in provider registered by Hot Chocolate is MD5 with `HashFormat.Hex`. If your client artifacts use Relay-style MD5 hex IDs, you do not need to override the provider. If you do override it, pass the format explicitly:

```csharp
builder
    .AddGraphQL()
    .AddMD5DocumentHashProvider(HashFormat.Hex)
    .AddQueryType<Query>()
    .UsePersistedOperationPipeline()
    .AddFileSystemOperationDocumentStorage("./persisted_operations");
```

`HashFormat.Hex` produces lowercase hex. `HashFormat.Base64` produces URL-safe base64 without padding. Plain base64 IDs containing `+`, `/`, or `=` are not valid operation document IDs for HTTP requests.

Hash mismatches are a common source of not-found errors. Check the algorithm, encoding format, file name, and exact operation text. Whitespace and document normalization must match whatever the client used when it created the ID.

# Send persisted operation requests

## Standard GraphQL HTTP endpoint

A standard POST request can send `id`, `operationName`, and `variables` without a `query` field:

```json
{
  "id": "0c95d31ca29272475bf837f944f4e513",
  "operationName": "GetViewer",
  "variables": {
    "includeDetails": true
  }
}
```

If the stored document contains a single operation, `operationName` can be omitted.

Relay documentation may call this value `doc_id`. Hot Chocolate expects `id` in the standard GraphQL request body.

Apollo-style persisted query extensions can also address a pre-published document:

```json
{
  "extensions": {
    "persistedQuery": {
      "version": 1,
      "sha256Hash": "7f56e67dd21ab3f30d1ff8b7bed08893f0a0db86449836189b361dd1e56ddb4b"
    }
  },
  "variables": {
    "includeDetails": true
  }
}
```

Use the field that matches the active hash provider: `md5Hash`, `sha1Hash`, or `sha256Hash`. A miss for a regular persisted operation returns the persisted operation not-found error. The APQ miss-and-upload flow is covered by [Automatic persisted operations](/docs/hotchocolate/v16/build/performance/automatic-persisted-operations).

HTTP GET can also carry GraphQL requests and is useful for caching scenarios. See [HTTP transport](/docs/hotchocolate/v16/build/server-configuration/http-transport) for general GET and POST behavior.

## Dedicated persisted operation endpoint

Hot Chocolate can expose a dedicated route for persisted operations:

```csharp
app.MapGraphQL();
app.MapGraphQLPersistedOperations();
```

The default path is `/graphql/persisted`.

```http
GET /graphql/persisted/0c95d31ca29272475bf837f944f4e513
GET /graphql/persisted/0c95d31ca29272475bf837f944f4e513/GetViewer
```

For POST requests, the operation ID and optional operation name come from the route. Variables and extensions come from the request body.

```http
POST /graphql/persisted/0c95d31ca29272475bf837f944f4e513/GetViewer
Content-Type: application/json

{
  "variables": {
    "includeDetails": true
  }
}
```

Require route operation names when your documents can contain multiple operations:

```csharp
app.MapGraphQLPersistedOperations(requireOperationName: true);
```

If `requireOperationName` is `true` and the route omits the operation name, the endpoint returns a bad request.

# Optional persisted-only policy

Persisted-only enforcement belongs with trusted documents, but the main options affect persisted operation behavior:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .UsePersistedOperationPipeline()
    .ModifyRequestOptions(options =>
    {
        options.PersistedOperations.OnlyAllowPersistedDocuments = true;
        options.PersistedOperations.AllowDocumentBody = false;
    });
```

| Option or API                  | Effect                                                                                                    |
| ------------------------------ | --------------------------------------------------------------------------------------------------------- |
| `OnlyAllowPersistedDocuments`  | Rejects requests that are not loaded from persisted operation storage.                                    |
| `AllowDocumentBody`            | When persisted-only mode is enabled, allows a full document body only if it matches a persisted document. |
| `OperationNotAllowedError`     | Customizes the error returned when non-persisted operations are rejected.                                 |
| `AllowNonPersistedOperation()` | Per-request override, commonly used from an interceptor for controlled development or admin scenarios.    |

See [Trusted documents](/docs/hotchocolate/v16/build/security/trusted-documents) before using this as a security boundary.

# Advanced validation option

`SkipPersistedDocumentValidation` marks loaded persisted documents as already validated:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .UsePersistedOperationPipeline()
    .ModifyRequestOptions(options =>
    {
        options.PersistedOperations.SkipPersistedDocumentValidation = true;
    });
```

Enable this only when your publication pipeline validates every operation against the exact schema version that will execute it. Stored documents are not automatically safe. A stale or unvalidated document can otherwise bypass normal validation.

# Deploy safely

Use this checklist for production rollouts:

- Generate the schema artifact for the release.
- Extract client operations from the matching client build.
- Validate operations against the release schema.
- Publish operation documents to file system, Redis, Azure Blob Storage, or your custom store.
- Deploy storage and server changes before clients send new IDs.
- Keep previous operation IDs available during client rollout and rollback windows.
- Monitor `HC0020` errors, invalid ID format errors, storage read failures, and unexpected misses.
- Avoid deleting, expiring, or replacing operation documents while any active client can still reference them.

For multi-environment deployments, keep operation stores isolated by environment. A staging client should not publish documents into production storage, and a production server should not read from a preview container or Redis database. If several schema versions run at the same time, publish the union of active operation IDs or use versioned storage roots.

# Migration notes

Older Hot Chocolate versions and older workshop material may use persisted query names such as `UsePersistedQueryPipeline()` or `AddFileSystemQueryStorage(...)`. Use persisted operation APIs instead:

| Older name                       | Current name                                  |
| -------------------------------- | --------------------------------------------- |
| `UsePersistedQueryPipeline()`    | `UsePersistedOperationPipeline()`             |
| `AddFileSystemQueryStorage(...)` | `AddFileSystemOperationDocumentStorage(...)`  |
| Persisted query storage packages | `HotChocolate.PersistedOperations.*` packages |

Keep the client request field as `id` for standard GraphQL HTTP requests.

# Troubleshooting

| Symptom                                                           | Likely cause                                                                                | What to check                                                                                          |
| ----------------------------------------------------------------- | ------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------ |
| `HC0020` with `The specified persisted operation key is invalid.` | The ID was not found, the wrong storage was configured, or the operation was not published. | Check the operation ID, storage root or database, file or key name, and publish job logs.              |
| `HC0020` with `PersistedQueryNotFound`                            | APQ miss, not regular persisted operation lookup.                                           | Follow the APQ flow or pre-publish the document.                                                       |
| HTTP 400 for invalid ID format                                    | ID contains unsupported characters.                                                         | Use hex or Hot Chocolate URL-safe base64. Avoid `+`, `/`, and `=`.                                     |
| File system lookup misses                                         | File not copied, wrong root, missing `.graphql` suffix, or file map mismatch.               | Inspect the deployed `persisted_operations` directory inside the running app or container.             |
| Redis lookup misses                                               | Wrong database, key mismatch, expiration removed the key, or publish job did not run.       | Inspect the Redis database used by `AddRedisOperationDocumentStorage(...)`.                            |
| Azure Blob lookup misses                                          | Wrong container, blob name mismatch, or lifecycle policy deleted the blob.                  | Check that `{documentId}.graphql` exists in the configured container.                                  |
| Hash mismatch                                                     | Client and server use different algorithms, formats, or document text.                      | Compare `Add*DocumentHashProvider(...)`, `HashFormat`, generated IDs, and published document contents. |
| Full `query` requests still work                                  | Expected default behavior.                                                                  | Enable trusted-document policy if you want persisted-only execution.                                   |

# API quick reference

| API                                                | Use                                                                |
| -------------------------------------------------- | ------------------------------------------------------------------ |
| `UsePersistedOperationPipeline()`                  | Adds the persisted operation request pipeline.                     |
| `AddFileSystemOperationDocumentStorage(...)`       | Stores documents as `.graphql` files.                              |
| `AddRedisOperationDocumentStorage(...)`            | Stores documents as Redis string values keyed by operation ID.     |
| `AddAzureBlobStorageOperationDocumentStorage(...)` | Stores documents as Azure blobs named `{documentId}.graphql`.      |
| `AddInMemoryOperationDocumentStorage()`            | Stores documents in `IMemoryCache`.                                |
| `IOperationDocumentStorage`                        | Storage contract for custom providers.                             |
| `AddMD5DocumentHashProvider(...)`                  | Configures MD5 document IDs.                                       |
| `AddSha1DocumentHashProvider(...)`                 | Configures SHA1 document IDs.                                      |
| `AddSha256DocumentHashProvider(...)`               | Configures SHA256 document IDs.                                    |
| `HashFormat.Hex`                                   | Lowercase hex format.                                              |
| `HashFormat.Base64`                                | URL-safe base64 without padding.                                   |
| `MapGraphQLPersistedOperations(...)`               | Maps the dedicated persisted operation endpoint.                   |
| `PersistedOperationOptions`                        | Configures persisted-only policy and advanced validation behavior. |

# Next steps

- [Automatic persisted operations](/docs/hotchocolate/v16/build/performance/automatic-persisted-operations) for runtime hash negotiation and storage.
- [Trusted documents](/docs/hotchocolate/v16/build/security/trusted-documents) for allow-list enforcement.
- [HTTP transport](/docs/hotchocolate/v16/build/server-configuration/http-transport) for standard GraphQL HTTP request formats.
- [Interceptors](/docs/hotchocolate/v16/build/server-configuration/interceptors) for per-request overrides.
- [Nitro client registry](/docs/nitro/apis/client-registry) for managed operation validation and publishing.
