---
title: "Trusted documents"
---

Trusted documents let you run a Hot Chocolate server that accepts only operation documents you approved before production traffic uses them. Hot Chocolate calls the underlying feature **persisted operations**. When you generate those operations during a client build, publish them to storage or Nitro, and enable strict mode, they become a production allowlist.

Use this page when you want to harden a Hot Chocolate v16 ASP.NET Core server. Fusion gateway configuration is separate and is not covered here.

# Prerequisites

Before you enable strict mode, make sure you have:

- A Hot Chocolate v16 ASP.NET Core server that calls `MapGraphQL()`.
- Clients that can generate deterministic operation IDs and send `id` with optional `variables`.
- A CI/CD step that publishes operation documents before clients send their IDs.
- One operation document storage provider or Nitro-backed client registry.
- Authentication, authorization, rate limiting, request limits, and cost analysis configured as separate controls.

Install the package that matches your storage strategy:

| Need                                             | Package                                             |
| ------------------------------------------------ | --------------------------------------------------- |
| File system storage                              | `HotChocolate.PersistedOperations.FileSystem`       |
| Redis storage                                    | `HotChocolate.PersistedOperations.Redis`            |
| Azure Blob Storage                               | `HotChocolate.PersistedOperations.AzureBlobStorage` |
| In-memory storage for development, tests, or APQ | `HotChocolate.PersistedOperations.InMemory`         |
| Nitro client registry distribution               | `ChilliCream.Nitro`                                 |
| Schema export in CI                              | `HotChocolate.AspNetCore.CommandLine`               |

# Configure trusted documents in strict mode

Start with strict mode in a non-production environment that already has operation files. The example below uses SHA-256 hex IDs and file-system storage.

<PackageInstallation packageName="HotChocolate.PersistedOperations.FileSystem" />

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
    });

var app = builder.Build();

app.MapGraphQL();

app.Run();

public sealed class Query
{
    public string Viewer => "Ada";
}
```

Create an operation file whose name is the operation ID plus `.graphql`:

```text
persisted_operations/0c4bd0ac822f9b98f47e85adaaa049fb68636497cfdee924311521f106051855.graphql
```

```graphql
query Viewer {
  viewer
}
```

The ID above is the SHA-256 hex hash of the exact UTF-8 document text `query Viewer { viewer }`.

> Note: Hot Chocolate v16 registers an MD5 hex hash provider by default. The explicit `AddMD5DocumentHashProvider()`, `AddSha1DocumentHashProvider()`, and `AddSha256DocumentHashProvider()` overloads default to Base64 when you omit `HashFormat`. Configure the provider and format explicitly so your client manifest, storage keys, and server agree.

With `OnlyAllowPersistedDocuments = true`, a known `id` executes. A request that sends only `query` is rejected with `HC0067`.

# Send a trusted-document request

A trusted-document request uses the normal GraphQL over HTTP transport, but the body contains `id` instead of `query`. Variables remain dynamic per request. The operation shape comes from the stored document.

**Request**

```bash
curl http://localhost:5000/graphql \
  -H 'Content-Type: application/json' \
  -d '{"id":"0c4bd0ac822f9b98f47e85adaaa049fb68636497cfdee924311521f106051855"}'
```

**Response**

```json
{
  "data": {
    "viewer": "Ada"
  }
}
```

A request that sends an ad hoc document to the same strict endpoint is rejected.

**Request**

```bash
curl http://localhost:5000/graphql \
  -H 'Content-Type: application/json' \
  -d '{"query":"{ __typename }"}'
```

**Response**

```json
{
  "errors": [
    {
      "message": "Only persisted operations are allowed.",
      "extensions": {
        "code": "HC0067"
      }
    }
  ]
}
```

You can also send `id` and `variables` as GET query parameters when you use GET for cacheable requests. Content negotiation follows the normal [HTTP transport](/docs/hotchocolate/v16/server/http-transport) behavior.

Relay examples often use `doc_id`. Hot Chocolate expects the request field to be named `id`.

# Why trusted documents harden production

Trusted documents turn operation shape into a release artifact. Production traffic can no longer introduce new selection sets, fragment structures, aliases, or deep traversal paths unless those documents were approved and published first.

| Risk                            | How trusted documents help                                            | What still matters                                                                                            |
| ------------------------------- | --------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------- |
| Arbitrary deep queries          | Strict mode rejects documents that are not in storage.                | Keep [request limits](/docs/hotchocolate/v16/securing-your-api/request-limits) for fallback and bypass paths. |
| Alias or fragment amplification | Attackers cannot submit new amplified documents to a strict endpoint. | Review approved operations and use cost analysis where documents are accepted.                                |
| Unknown clients                 | Only clients with published IDs can execute operations.               | Authenticate clients and rotate credentials.                                                                  |
| Breaking schema changes         | Registered client operations can be validated before schema publish.  | Keep schema and client versions staged together.                                                              |
| Leaked endpoint                 | The endpoint still exists, but ad hoc documents fail.                 | Use transport security, auth, rate limits, and monitoring.                                                    |
| Compromised approved client     | Approved operations can still execute.                                | Enforce resolver-level authorization and business rules.                                                      |
| Admin or debug tooling          | You can authorize a narrow bypass.                                    | Do not use static shared headers as production bypasses.                                                      |

Trusted documents do not repair broken access control inside an approved operation. Resolvers must still enforce authorization and tenant boundaries.

# Trusted documents vs APQ

Automatic persisted operations, often called APQ, solve a different problem. APQ lets a client send a hash first. On a miss, Hot Chocolate returns `PersistedQueryNotFound` with `HC0020`, and the client can retry with the full document so the server stores it at runtime.

| Question                        | Trusted documents                                          | Automatic persisted operations                                     |
| ------------------------------- | ---------------------------------------------------------- | ------------------------------------------------------------------ |
| When are operations registered? | Before deployment.                                         | At runtime after a miss.                                           |
| Who can add operation shapes?   | Your build and publish pipeline.                           | Any client allowed to send the fallback document.                  |
| First request behavior          | ID executes only if storage already contains the document. | Hash-only miss returns `PersistedQueryNotFound`.                   |
| Storage writes                  | CI/CD, Nitro, or an administrative process.                | Server writes during client traffic.                               |
| Fallback                        | Optional authorized bypass or compatibility mode.          | Full-document retry is part of the APQ flow.                       |
| Security posture                | Good for production allowlisting.                          | Good for network and cache efficiency, not an allowlist by itself. |

Use [automatic persisted operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations) when runtime registration is acceptable. Use trusted documents when production must reject unknown operation shapes.

# Build operation artifacts at client build time

Generate the allowlist from the same documents your client sends. Do not reformat, minify, or normalize documents differently between client, manifest, storage, and server.

A Relay build can write a Relay-style JSON map:

```js
// relay.config.js
module.exports = {
  src: "./src",
  schema: "./schema.graphql",
  persistConfig: {
    file: "./persisted_queries.json",
    algorithm: "MD5",
  },
};
```

Example output:

```json
{
  "913abc361487c481cf6015841c0eca22": "query Viewer { viewer }",
  "0e7cf2125e8eb711b470cc72c73ca77e": "query Book($id: ID!) { book(id: $id) { title } }"
}
```

For file-system storage, publish a folder of GraphQL files:

```text
persisted_operations/
  913abc361487c481cf6015841c0eca22.graphql
  0e7cf2125e8eb711b470cc72c73ca77e.graphql
```

Hot Chocolate's default file map appends `.graphql`. For Base64 IDs, it maps `/` to `-`, maps `+` to `_`, and removes trailing `=` padding. Hex IDs use the hash as the file name.

# Validate and publish the allowlist in CI/CD

Treat trusted documents like a deployment dependency. Publish storage before clients depend on new IDs.

A typical release flow is:

1. Export the server schema.

   ```bash
   dotnet run -- schema export --output schema.graphql
   ```

   To enable the command, install `HotChocolate.AspNetCore.CommandLine` and return the command exit code from `Program.cs`:

   ```csharp
   var builder = WebApplication.CreateBuilder(args);

   builder.AddGraphQL().AddQueryType<Query>();

   var app = builder.Build();

   app.MapGraphQL();

   return await app.RunWithGraphQLCommandsAsync(args);
   ```

2. Validate the schema and client operations in pull requests.

   ```bash
   nitro schema validate \
     --api-id "$NITRO_API_ID" \
     --stage production \
     --schema-file schema.graphql

   nitro client validate \
     --client-id "$NITRO_CLIENT_ID" \
     --stage production \
     --operations-file persisted_queries.json
   ```

3. On release, upload and publish the schema and client versions.

   ```bash
   nitro schema upload --api-id "$NITRO_API_ID" --tag "$GIT_SHA" --schema-file schema.graphql
   nitro schema publish --api-id "$NITRO_API_ID" --tag "$GIT_SHA" --stage production

   nitro client upload --client-id "$NITRO_CLIENT_ID" --tag "$GIT_SHA" --operations-file persisted_queries.json
   nitro client publish --client-id "$NITRO_CLIENT_ID" --tag "$GIT_SHA" --stage production
   ```

4. If you do not use Nitro-backed distribution, copy the generated `.graphql` files into your container image, mounted volume, Redis database, or Azure Blob container before the server receives traffic.

5. During blue/green or rolling deployments, keep old and new operation IDs available until old clients and servers are drained.

For rollback, roll back the client, republish the previous client version, or keep both operation sets published until traffic stabilizes.

# Use Nitro schema and client registries

Nitro can manage the release workflow instead of self-managed storage. The schema registry tracks schema versions by tag and active schema per stage. The client registry tracks client versions and their persisted operations per stage, validates operations against schema changes, and distributes operations to the Hot Chocolate server.

Install `ChilliCream.Nitro` and connect the server to the registry:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddNitro()
    .UsePersistedOperationPipeline()
    .ModifyRequestOptions(o =>
    {
        o.PersistedOperations.OnlyAllowPersistedDocuments = true;
    });
```

`AddNitro()` can read these environment variables:

- `NITRO_API_KEY`
- `NITRO_API_ID`
- `NITRO_STAGE`

Treat these values as deployment secrets. Do not commit real keys.

Use the Nitro pages for full registry and CLI details:

- [Schema registry](/docs/nitro/apis/schema-registry)
- [Client registry](/docs/nitro/apis/client-registry)
- [Nitro schema CLI](/docs/nitro/cli/schema)
- [Nitro client CLI](/docs/nitro/cli/client)

# Choose an operation document storage

| Provider              | Package                                             | Minimal API                                                       | Production fit                                                                                | Operational risks and notes                                                                          |
| --------------------- | --------------------------------------------------- | ----------------------------------------------------------------- | --------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------- |
| File system           | `HotChocolate.PersistedOperations.FileSystem`       | `AddFileSystemOperationDocumentStorage("./persisted_operations")` | Immutable container images, read-only mounted artifacts, or simple single-region deployments. | Deploy files to every instance. Verify the root path, permissions, and file-name mapping.            |
| Redis                 | `HotChocolate.PersistedOperations.Redis`            | `AddRedisOperationDocumentStorage(...)`                           | Shared storage across multiple instances.                                                     | Watch eviction policy, TTL if `queryExpiration` is set, connectivity, and warmup.                    |
| Azure Blob Storage    | `HotChocolate.PersistedOperations.AzureBlobStorage` | `AddAzureBlobStorageOperationDocumentStorage(...)`                | Durable external storage.                                                                     | The container must exist. Monitor permissions, latency, and lifecycle policies.                      |
| Nitro client registry | `ChilliCream.Nitro`                                 | `AddNitro()`                                                      | Managed validation and operation distribution by stage.                                       | Keep API key, API ID, and stage correct. Monitor registry and cache reachability.                    |
| In-memory             | `HotChocolate.PersistedOperations.InMemory`         | `AddInMemoryOperationDocumentStorage()`                           | Local development, tests, APQ demos, or single-instance experiments.                          | Not durable. Not enough for strict multi-instance production unless you preload it with custom code. |

# Choose and verify the hash algorithm

Hot Chocolate supports MD5, SHA-1, and SHA-256 providers with `HashFormat.Hex` or `HashFormat.Base64`.

For new internal workflows, prefer SHA-256 hex unless your client ecosystem requires a different format. Relay commonly uses MD5, so configure the server and storage to match Relay output.

```csharp
builder
    .AddGraphQL()
    .AddSha256DocumentHashProvider(HashFormat.Hex)
    .UsePersistedOperationPipeline();
```

APQ extensions use provider-specific fields such as `md5Hash` or `sha256Hash`. Trusted-document requests use the final `id` value.

| Algorithm and format | Request ID example                                                 | File name example                                                          |
| -------------------- | ------------------------------------------------------------------ | -------------------------------------------------------------------------- |
| SHA-256 hex          | `7f56e67dd21ab3f30d1ff8b7bed08893f0a0db86449836189b361dd1e56ddb4b` | `7f56e67dd21ab3f30d1ff8b7bed08893f0a0db86449836189b361dd1e56ddb4b.graphql` |
| MD5 hex              | `a73defcdf38e5891e91b9ba532cf4c36`                                 | `a73defcdf38e5891e91b9ba532cf4c36.graphql`                                 |
| MD5 Base64           | `71yeex4k3iYWQgg9TilDIg==`                                         | `71yeex4k3iYWQgg9TilDIg.graphql`                                           |

To verify a miss, reproduce the hash from the exact operation text and compare it with the client ID, manifest key, storage key, and server hash provider.

# Roll out strict mode safely

Use a rollout ladder instead of switching production traffic to strict ID-only mode in one step.

1. **Prepare:** Configure `UsePersistedOperationPipeline()` and storage. Keep ad hoc documents allowed.
2. **Observe:** Record lookup misses and client request shapes. Do not reject ad hoc documents yet.
3. **Compatibility:** Require documents to be persisted, but allow full document bodies when they match storage.
4. **Strict:** Require ID-only trusted-document requests.
5. **Bypass:** Allow ad hoc operations only for authenticated and authorized internal tooling.

Compatibility mode:

```csharp
builder
    .AddGraphQL()
    .UsePersistedOperationPipeline()
    .ModifyRequestOptions(o =>
    {
        o.PersistedOperations.OnlyAllowPersistedDocuments = true;
        o.PersistedOperations.AllowDocumentBody = true;
    });
```

Strict mode:

```csharp
builder
    .AddGraphQL()
    .UsePersistedOperationPipeline()
    .ModifyRequestOptions(o =>
    {
        o.PersistedOperations.OnlyAllowPersistedDocuments = true;
        o.PersistedOperations.AllowDocumentBody = false;
    });
```

Authorized bypass:

```csharp
using HotChocolate.AspNetCore;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Authorization;

namespace Api.Security;

public sealed class InternalToolRequestInterceptor : DefaultHttpRequestInterceptor
{
    private readonly IAuthorizationService _authorization;

    public InternalToolRequestInterceptor(IAuthorizationService authorization)
    {
        _authorization = authorization;
    }

    public override async ValueTask OnCreateAsync(
        HttpContext context,
        IRequestExecutor requestExecutor,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        var result = await _authorization.AuthorizeAsync(
            context.User,
            resource: null,
            policyName: "GraphQLInternalTool");

        if (result.Succeeded)
        {
            requestBuilder.AllowNonPersistedOperation();
        }

        await base.OnCreateAsync(
            context,
            requestExecutor,
            requestBuilder,
            cancellationToken);
    }
}
```

Register it with your server:

```csharp
builder
    .AddGraphQL()
    .AddHttpRequestInterceptor<InternalToolRequestInterceptor>();
```

Keep request limits and cost analysis enabled for compatibility and bypass paths. You can customize `OperationNotAllowedError`, but keep logs and metrics clear enough to distinguish strict-mode rejections from other failures.

# Keep query limits as fallback controls

Strict ID-only mode reduces parser and validation exposure for normal client traffic. It does not cover every path. Development environments, admin tooling, authorized bypasses, compatibility rollout, APQ endpoints, schema endpoints, and misconfigured clients can still send documents.

Keep these controls in place:

```csharp
builder
    .AddGraphQL()
    .ModifyParserOptions(o =>
    {
        o.MaxAllowedFields = 1024;
        o.MaxAllowedRecursionDepth = 100;
    })
    .AddMaxExecutionDepthRule(10)
    .ModifyRequestOptions(o =>
    {
        o.ExecutionTimeout = TimeSpan.FromSeconds(10);
    });
```

Use [cost analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis) in report mode while tuning approved operations, or enforce it on paths that still accept full documents.

# Monitor rejected and missing operations

Monitor both storage misses and strict-mode rejections:

- `DocumentNotFoundInStorage(RequestContext, OperationDocumentId)` means a request supplied an ID that storage could not resolve.
- `UntrustedDocumentRejected(RequestContext)` means strict mode rejected an ad hoc document.
- `HC0020` means either `The specified persisted operation key is invalid.` for trusted-document misses, or `PersistedQueryNotFound` for APQ misses.
- `HC0067` means `Only persisted operations are allowed.`

Add an execution diagnostic listener when you need custom logging:

```csharp
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;

namespace Api.Observability;

public sealed class PersistedOperationEventListener : ExecutionDiagnosticEventListener
{
    private readonly ILogger<PersistedOperationEventListener> _logger;

    public PersistedOperationEventListener(
        ILogger<PersistedOperationEventListener> logger)
    {
        _logger = logger;
    }

    public override void DocumentNotFoundInStorage(
        RequestContext context,
        OperationDocumentId documentId)
    {
        _logger.LogWarning(
            "Persisted operation {OperationId} was not found in storage.",
            documentId.Value);
    }

    public override void UntrustedDocumentRejected(RequestContext context)
    {
        _logger.LogWarning("An untrusted GraphQL document was rejected.");
    }
}
```

Register it:

```csharp
builder
    .AddGraphQL()
    .AddDiagnosticEventListener<PersistedOperationEventListener>();
```

Tag logs, traces, and dashboards with endpoint, client name, client version, stage, operation ID prefix, storage provider, and deployment version when available. Alert separately on storage errors or latency, missing ID rate, and spikes in ad hoc rejections.

Triage misses by asking whether the ID is absent from deployment, belongs to a stale client, targets the wrong stage, or looks like probing traffic.

See [Instrumentation](/docs/hotchocolate/v16/server/instrumentation) for OpenTelemetry and diagnostic listener setup.

# Troubleshoot common failures

| Symptom                                       | Likely cause                                                                                                       | Checks                                                                               | Safe fix                                                                        |
| --------------------------------------------- | ------------------------------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------- |
| `HC0020` for a known client ID                | Operation not published, wrong stage, wrong key, Redis eviction, Azure/file permission issue, or storage outage.   | Inspect the manifest, storage key, Nitro stage, and storage health.                  | Publish the missing operation, restore storage, or roll back the client.        |
| `HC0067` for a client request                 | Client sent `query` instead of `id`, used `doc_id`, or strict mode was enabled before rollout.                     | Capture the HTTP body and headers. Check `AllowDocumentBody`.                        | Update the client request shape or use compatibility mode during migration.     |
| Known query body is rejected even when stored | `AllowDocumentBody` is false, or the stored text differs from the sent text.                                       | Compare exact document text after compiler transforms.                               | Send ID-only requests or publish the exact transformed document.                |
| Hash mismatch                                 | Algorithm, format, line endings, or minification differ.                                                           | Recompute the hash locally from exact UTF-8 text. Check `HashFormat`.                | Align client build, manifest, storage, and server provider.                     |
| File-system miss                              | File name mapping, root path, permissions, image contents, or volume mount is wrong.                               | Check `<id>.graphql`, Base64 mapping, working directory, and container file listing. | Publish files to every instance and fix path or permissions.                    |
| Registry or stage mismatch                    | Server points to `dev` while client published to `production`, or an old client version was unpublished too early. | Check `NITRO_API_ID`, `NITRO_STAGE`, and published client versions.                  | Republish the required version to the active stage.                             |
| Blue/green outage                             | New client deployed before storage, or old server cannot read the new set.                                         | Compare deployment timestamps and active operation sets.                             | Publish storage first and keep old plus new IDs during rollout.                 |
| Schema or client validation fails             | A registered operation uses a removed field, changed argument, or incompatible nullability.                        | Run `nitro schema validate` and `nitro client validate`.                             | Change the schema compatibly or update and republish the client operations.     |
| Spike in ad hoc rejections                    | Probing traffic, unauthorized admin tool, or APQ fallback hitting a strict endpoint.                               | Review source IPs, identities, user agents, and request bodies.                      | Block abusive traffic, authorize tooling, or route APQ to the correct endpoint. |

Error shape for a trusted-document miss:

```json
{
  "errors": [
    {
      "message": "The specified persisted operation key is invalid.",
      "extensions": {
        "code": "HC0020",
        "requestedKey": "unknown-id"
      }
    }
  ]
}
```

Error shape for an ad hoc document in strict mode:

```json
{
  "errors": [
    {
      "message": "Only persisted operations are allowed.",
      "extensions": {
        "code": "HC0067"
      }
    }
  ]
}
```

# Next steps

- Use [automatic persisted operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations) when runtime persistence and caching are the goal.
- Keep [request limits](/docs/hotchocolate/v16/securing-your-api/request-limits) and [cost analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis) as fallback defenses.
- Review [HTTP transport](/docs/hotchocolate/v16/server/http-transport) for POST, GET, and content negotiation behavior.
- Use [interceptors](/docs/hotchocolate/v16/server/interceptors) for authorized per-request bypasses.
- Use [instrumentation](/docs/hotchocolate/v16/server/instrumentation) for tracing and metrics.
- Use the [command line](/docs/hotchocolate/v16/server/command-line) to export schemas in CI.
- Use the [Nitro schema registry](/docs/nitro/apis/schema-registry) and [client registry](/docs/nitro/apis/client-registry) for managed validation and publishing.
