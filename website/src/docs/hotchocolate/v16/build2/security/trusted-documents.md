---
title: Trusted documents
---

Trusted documents turn a GraphQL endpoint for controlled clients into an allowlist of known operations. The client build extracts each operation, a release process registers it with the server, and production traffic references the registered operation by an ID instead of sending arbitrary GraphQL text.

In Hot Chocolate v16 this security policy is built on persisted operations. Persisted operations provide the storage and lookup mechanism. Trusted documents are the production rule that only documents from that controlled workflow may execute.

Use trusted documents when you own the clients, such as web apps, mobile apps, internal services, or private APIs. Public APIs with unknown client documents still need cost analysis, request limits, authentication, authorization, introspection policy, and rate limiting.

# Threat model

A normal GraphQL endpoint lets callers choose the operation shape at request time. That flexibility is useful for public APIs, but it also means untrusted input can reach parsing, validation, planning, and execution.

Trusted documents reduce that attack surface:

1. Client source contains GraphQL operations.
2. CI extracts the operations and assigns stable operation IDs, often hashes.
3. CI validates the operations against the schema version being deployed.
4. The release publishes the documents to Nitro client registry or to an `IOperationDocumentStorage` implementation.
5. Production clients send an operation ID and variables.
6. Hot Chocolate loads the registered document and executes it.
7. Unknown IDs and non-persisted documents are rejected when enforcement is enabled.

The ID is not a secret and does not authorize the caller. Trust comes from server-side registration through your build, review, registry, storage, and deployment process. Keep authentication and authorization in place for user identity and data access.

# Trusted documents, persisted operations, and APQ

| Concept                              | Primary goal                      | How documents appear on the server                                                              | Production security posture                |
| ------------------------------------ | --------------------------------- | ----------------------------------------------------------------------------------------------- | ------------------------------------------ |
| Trusted documents                    | Allow only known operations       | Published before production traffic uses them                                                   | Enforce with `OnlyAllowPersistedDocuments` |
| Persisted operations                 | Store and load documents by ID    | Provided by file system, Redis, Azure Blob Storage, in-memory storage, Nitro, or custom storage | Mechanism used by trusted documents        |
| Automatic persisted operations (APQ) | Reduce repeated document transfer | Client can send a hash, receive a miss, then retry with the full document                       | Not a trusted-document boundary by itself  |

APQ is useful for performance, but a client can introduce new operation text during the APQ handshake unless you add a separate allowlist policy. Use the APQ page for that negotiation flow: [Automatic persisted operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations). Use the persisted operations page for storage mechanics: [Persisted operations](/docs/hotchocolate/v16/performance/trusted-documents).

# Configure the persisted operation pipeline

The persisted operation pipeline loads documents from storage before parsing and validation continue.

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .UsePersistedOperationPipeline();
```

This enables lookup, but it does not block dynamic operation text by itself. Add enforcement after your storage or registry contains every operation your clients need.

For APQ, use `UseAutomaticPersistedOperationPipeline()` instead and follow the APQ documentation. Do not mix the APQ mental model with trusted-document enforcement.

# Register operation document storage

Choose storage that matches your release model. Trusted operation storage is part of the client-server contract. If an operation disappears from storage while a deployed client still references it, that client breaks.

| Storage            | Package                                             | Registration                                       | Production note                                                                                    |
| ------------------ | --------------------------------------------------- | -------------------------------------------------- | -------------------------------------------------------------------------------------------------- |
| File system        | `HotChocolate.PersistedOperations.FileSystem`       | `AddFileSystemOperationDocumentStorage(...)`       | Good for immutable container images or mounted release artifacts. Files are named by operation ID. |
| Redis              | `HotChocolate.PersistedOperations.Redis`            | `AddRedisOperationDocumentStorage(...)`            | Useful for shared infrastructure. Avoid expirations that remove active trusted documents.          |
| Azure Blob Storage | `HotChocolate.PersistedOperations.AzureBlobStorage` | `AddAzureBlobStorageOperationDocumentStorage(...)` | Use a provisioned container and treat lifecycle policies as release policy.                        |
| In-memory          | `HotChocolate.PersistedOperations.InMemory`         | `AddInMemoryOperationDocumentStorage()`            | Useful for tests and demos, not durable production allowlists.                                     |
| Custom             | Your application                                    | Implement `IOperationDocumentStorage`              | Use when an internal registry or release system owns the allowlist.                                |

Example with file-system storage:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .UsePersistedOperationPipeline()
    .AddFileSystemOperationDocumentStorage("./persisted_operations");
```

The client, publisher, and server must agree on the operation ID. If the ID is a hash, they must also agree on the hash algorithm and format.

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddSha256DocumentHashProvider(HashFormat.Hex)
    .UsePersistedOperationPipeline()
    .AddFileSystemOperationDocumentStorage("./persisted_operations");
```

Hot Chocolate v16 also provides `AddMD5DocumentHashProvider()` and `AddSha1DocumentHashProvider()`. Configure hash providers on the GraphQL builder. Use a provider and format that match your client compiler and publishing workflow.

# Enforce trusted documents only

Enable the allowlist policy with `OnlyAllowPersistedDocuments`.

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .UsePersistedOperationPipeline()
    .AddFileSystemOperationDocumentStorage("./persisted_operations")
    .ModifyRequestOptions(o =>
    {
        o.PersistedOperations.OnlyAllowPersistedDocuments = true;
    });
```

With this option enabled, an operation can execute when it resolves to a persisted document by ID. A standard request body can also be accepted when `AllowDocumentBody = true` and the document matches a persisted document. Warmup requests are exempt internally, and application code can create a per-request override with `AllowNonPersistedOperation()`.

The strict production target for locked-down first-party APIs is:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .UsePersistedOperationPipeline()
    .AddFileSystemOperationDocumentStorage("./persisted_operations")
    .ModifyRequestOptions(o =>
    {
        o.PersistedOperations.OnlyAllowPersistedDocuments = true;
        o.PersistedOperations.AllowDocumentBody = false;
    });
```

`AllowDocumentBody` is `false` by default. Setting it explicitly documents the security decision. With `OnlyAllowPersistedDocuments = true` and `AllowDocumentBody = false`, document-body requests that do not provide an operation ID are rejected, and the ASP.NET Core request parser does not need to read incoming GraphQL document text for that strict path.

# Choose a request model

## Route-based persisted operation endpoint

For first-party production APIs, `MapGraphQLPersistedOperations()` exposes registered operations through deterministic URLs. The route supplies the operation ID, so clients do not send `query` or `id` in the body.

```csharp
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapGraphQL();
}

app.MapGraphQLPersistedOperations();

app.Run();
```

The default path is `/graphql/persisted`. Hot Chocolate maps `/{operationId}` and `/{operationId}/{operationName}` under that path. You can change the path or require operation names:

```csharp
app.MapGraphQLPersistedOperations("/api/operations");
app.MapGraphQLPersistedOperations(requireOperationName: true);
```

POST example:

```http
POST /graphql/persisted/0c95d31ca29272475bf837f944f4e513/GetProducts
Content-Type: application/json

{
  "variables": {
    "first": 10
  }
}
```

GET example:

```http
GET /graphql/persisted/0c95d31ca29272475bf837f944f4e513/GetProducts?variables={"first":10}
```

When `requireOperationName: true` is configured, requests must include the operation name route segment.

## Standard GraphQL endpoint with `id`

Use the standard endpoint when clients keep using GraphQL HTTP requests but send an operation ID instead of the document body.

```json
{
  "id": "0c95d31ca29272475bf837f944f4e513",
  "variables": {
    "first": 10
  }
}
```

Hot Chocolate-style persisted operation requests use the top-level `id` field. Some clients, including Relay examples, use `doc_id`; map that value to `id` before sending the request to Hot Chocolate.

# Release workflow

Use the same schema version for validation, publishing, and server deployment.

1. Export or publish the schema that the next server deployment will run.
2. Extract operations from every production client build.
3. Generate operation IDs with the documented algorithm and format.
4. Validate every operation against the target schema in CI.
5. Publish the validated documents to Nitro client registry or operation storage.
6. Deploy the server with `UsePersistedOperationPipeline()` and the same storage or registry integration.
7. Deploy clients that send route-based operation IDs or standard `id` requests.
8. Enable strict enforcement after storage, server, and clients are aligned.

Nitro client registry can own schema-aware operation publishing and client version management. Link your deployment process to the registry docs instead of duplicating command details here: [Client Registry](/docs/nitro/apis/client-registry).

# Development and rollout modes

| Mode                       | Configuration                                                                     | Use                                                                                              |
| -------------------------- | --------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------ |
| Prepare                    | `UsePersistedOperationPipeline()`, storage, `OnlyAllowPersistedDocuments = false` | Verify lookup and publishing while dynamic documents still work.                                 |
| Compatibility              | `OnlyAllowPersistedDocuments = true`, `AllowDocumentBody = true`                  | Allow legacy clients that still send full documents, but only when the document matches storage. |
| Strict                     | `OnlyAllowPersistedDocuments = true`, `AllowDocumentBody = false`                 | Production target for locked-down first-party APIs.                                              |
| Break-glass or development | `AllowNonPersistedOperation()` from an interceptor                                | Permit dynamic operations only for protected tooling or development traffic.                     |

A development interceptor can allow non-persisted operations without weakening production traffic:

```csharp
public sealed class DevToolsInterceptor : DefaultHttpRequestInterceptor
{
    private readonly IHostEnvironment _environment;

    public DevToolsInterceptor(IHostEnvironment environment)
    {
        _environment = environment;
    }

    public override ValueTask OnCreateAsync(
        HttpContext context,
        IRequestExecutor requestExecutor,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        if (_environment.IsDevelopment())
        {
            requestBuilder.AllowNonPersistedOperation();
        }

        return base.OnCreateAsync(
            context,
            requestExecutor,
            requestBuilder,
            cancellationToken);
    }
}
```

Register the interceptor with the GraphQL server:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddHttpRequestInterceptor<DevToolsInterceptor>()
    .UsePersistedOperationPipeline()
    .ModifyRequestOptions(o =>
    {
        o.PersistedOperations.OnlyAllowPersistedDocuments = true;
        o.PersistedOperations.AllowDocumentBody = false;
    });
```

Do not use a header-only bypass as a production control. If you need a production break-glass path, require authentication, authorization, logging, and review.

# Advanced options

## Customize the not-allowed error

Requests rejected by strict enforcement return `Only persisted operations are allowed.` with code `HC0067`. You can replace that error with `OperationNotAllowedError`.

```csharp
builder.Services
    .AddGraphQLServer()
    .ModifyRequestOptions(o =>
    {
        o.PersistedOperations.OnlyAllowPersistedDocuments = true;
        o.PersistedOperations.OperationNotAllowedError = ErrorBuilder.New()
            .SetMessage("This API only accepts registered operations.")
            .SetCode("TRUSTED_DOCUMENT_REQUIRED")
            .Build();
    });
```

## Skip validation for loaded documents

`SkipPersistedDocumentValidation` tells Hot Chocolate that loaded persisted documents do not need validation during request execution.

```csharp
builder.Services
    .AddGraphQLServer()
    .ModifyRequestOptions(o =>
    {
        o.PersistedOperations.SkipPersistedDocumentValidation = true;
    });
```

Only consider this when your publish workflow validates every document against the same schema version before deployment. Keep validation enabled if storage can contain documents from another schema version or an unreviewed source.

# Relation to introspection and cost analysis

Trusted documents do not disable introspection. Decide introspection separately with the guidance in [Introspection](introspection.md). In a strict first-party deployment you may expose `MapGraphQL()` only in development and expose persisted operation routes in production, but that is an endpoint decision, not an automatic effect of trusted documents.

Trusted documents also do not replace cost analysis. They reduce exposure to unknown operation shapes, while cost analysis budgets the work for operations that reach validation and execution. Keep cost analysis, request limits, authentication, and authorization as defense in depth. See [Cost analysis](cost-analysis.md) and [Execution depth and limits](execution-depth-and-limits.md).

# Troubleshooting

| Symptom or error                                                                                    | Likely cause                                                                                                                        | Fix                                                                                                                                                                |
| --------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `HC0067`, `Only persisted operations are allowed.`                                                  | A raw or untrusted operation reached strict enforcement.                                                                            | Publish the operation, send a route-based or `id` request, use a short `AllowDocumentBody = true` compatibility window, or use an authorized interceptor override. |
| `HC0020`, `The specified persisted operation key is invalid.`                                       | The operation ID was not found in storage.                                                                                          | Verify the publish step, deployed storage, file or key naming, hash algorithm, hash format, route ID, and client version.                                          |
| `HC0020`, `PersistedQueryNotFound`                                                                  | APQ miss.                                                                                                                           | Follow the APQ retry flow on the APQ page. Do not treat this as trusted-document allowlist behavior.                                                               |
| `The operation id has an invalid format.` or `The GraphQL document ID contains invalid characters.` | The ID or route parameter contains unsupported characters.                                                                          | Use a stable hash or ID format accepted by Hot Chocolate and your storage provider.                                                                                |
| `400 Bad Request` from the persisted operation route                                                | `requireOperationName: true` is enabled and the route omitted the operation name.                                                   | Call `/{operationId}/{operationName}`.                                                                                                                             |
| Client sends `query` in strict mode                                                                 | Legacy network layer still sends document text.                                                                                     | Remove `query`, use the route-based endpoint, or run a short compatibility phase with `AllowDocumentBody = true`.                                                  |
| Works in development but fails in production                                                        | Development maps `MapGraphQL()` or permits overrides, while production only accepts persisted routes, or storage was not populated. | Verify endpoint path, release order, storage contents, and environment-specific interceptor logic.                                                                 |

# Production checklist

- Every production client operation is extracted in CI.
- Operations are validated against the schema version that will be deployed.
- Operation IDs use a documented algorithm and format.
- Nitro client registry or operation storage is populated before strict enforcement.
- The server uses `UsePersistedOperationPipeline()`.
- Strict production policy uses `OnlyAllowPersistedDocuments = true`.
- Locked-down first-party APIs set `AllowDocumentBody = false`.
- Route-based production APIs map `MapGraphQLPersistedOperations()` and restrict `MapGraphQL()` to development or protected tooling.
- Monitoring alerts on `HC0067` and `HC0020` spikes.
- Break-glass paths are authenticated, authorized, logged, and audited.
- Request limits, cost analysis, authentication, authorization, and introspection policy remain configured.

# Next steps

- [Security overview](index.md) for choosing trusted documents or dynamic-operation controls.
- [Cost analysis](cost-analysis.md) and [Execution depth and limits](execution-depth-and-limits.md) for defense in depth.
- [Introspection](introspection.md) for schema metadata exposure policy.
- [Authentication](authentication.md) and [Authorization](authorization.md) for identity and data access.
- [Interceptors](/docs/hotchocolate/v16/server/interceptors) for per-request overrides.
- [Endpoints](/docs/hotchocolate/v16/server/endpoints) for `MapGraphQLPersistedOperations()` details.
- [Persisted operations](/docs/hotchocolate/v16/performance/trusted-documents) for storage and hash details.
- [Automatic persisted operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations) for APQ negotiation.
