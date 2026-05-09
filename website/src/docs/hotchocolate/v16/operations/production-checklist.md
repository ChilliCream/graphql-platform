---
title: Production checklist
---

This checklist guides you through preparing a Hot Chocolate v16 source-schema service for production. It does not cover Fusion gateway operations.

Use this as your launch review, baseline configuration, and runbook starting point. Each section references the page with detailed API documentation.

# Production Readiness Overview

Before routing production traffic to a Hot Chocolate service, complete the following checklist for each service:

| Area               | Ready When                                                                                                                       | How to Verify                                                                                | More Details                                                                                                                                                                      |
| ------------------ | -------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Scope              | The service is a Hot Chocolate source service. Fusion gateway operations are tracked separately.                                 | The runbook names the service role and owner.                                                | This page                                                                                                                                                                         |
| Endpoint surface   | Only required HTTP, GET, multipart, WebSocket, SDL, Nitro, batching, and persisted-operation endpoints are exposed.              | Run endpoint smoke tests from both inside and outside the production network.                | [Endpoints](/docs/hotchocolate/v16/server/endpoints)                                                                                                                              |
| Identity           | Authentication middleware runs before authorization. Endpoint, field, and type authorization are tested.                         | Test anonymous, authenticated, missing-role, and role-present requests.                      | [Authentication](/docs/hotchocolate/v16/securing-your-api/authentication), [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization)                                |
| Discovery          | Introspection, `?sdl`, and Nitro follow an environment-specific policy.                                                          | Run an introspection query, request `?sdl`, and open the Nitro path.                         | [Introspection](/docs/hotchocolate/v16/securing-your-api/introspection)                                                                                                           |
| Query limits       | Parser, validation, timeout, field-cycle, cost, page size, node batch, request batch, and concurrency limits match real traffic. | Send representative operations with `GraphQL-Cost: report` and test rejected operations.     | [Request limits](/docs/hotchocolate/v16/securing-your-api/request-limits), [Cost analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis)                                |
| Trusted operations | You have chosen dynamic operations, trusted documents, APQ, or registry-backed persisted operations.                             | Client build and server deploy agree on operation IDs and storage.                           | [Persisted operations](/docs/hotchocolate/v16/performance/trusted-documents), [Automatic persisted operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations) |
| Uploads            | Multipart is disabled unless the schema uses `Upload`. All size, content, and storage limits are aligned.                        | Test valid, missing-preflight, too-large, wrong-type, and dangerous-name uploads.            | [Files](/docs/hotchocolate/v16/server/files)                                                                                                                                      |
| Errors             | Unhandled exceptions are masked for clients. Logs and traces retain root-cause details.                                          | Trigger a controlled exception in staging and inspect the response and logs.                 | [Error handling](/docs/hotchocolate/v16/guides/error-handling), [Errors](/docs/hotchocolate/v16/api-reference/errors)                                                             |
| Observability      | Traces, logs, metrics, dashboards, alerts, operation names or IDs, and correlation fields are present.                           | Find a GraphQL request in the tracing backend from HTTP span to resolver or DataLoader span. | [Instrumentation](/docs/hotchocolate/v16/server/instrumentation)                                                                                                                  |
| Readiness          | The schema builds eagerly, warmup completes, required dependencies are checked, and load balancers wait.                         | Restart a staging instance and confirm it becomes ready only after warmup.                   | [Warmup](/docs/hotchocolate/v16/server/warmup)                                                                                                                                    |
| Data access        | DataLoader coverage, projections, top-operation latency, and backend query counts are reviewed.                                  | Compare top operations against latency and backend query budgets.                            | [Performance tuning](/docs/hotchocolate/v16/guides/performance), [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader)                                               |
| Governance         | Schema export, snapshot tests, registry publish, client compatibility checks, and deprecation policy run in CI.                  | A pull request with a breaking schema change fails before deploy.                            | [Schema evolution](/docs/hotchocolate/v16/guides/schema-evolution), [Testing](/docs/hotchocolate/v16/guides/testing)                                                              |
| Deployment         | Proxy limits, CORS, HTTPS, WebSockets, durable persisted-operation storage, secrets, canary, and rollback are configured.        | Compare platform settings with Hot Chocolate server options.                                 | [Options](/docs/hotchocolate/v16/api-reference/options)                                                                                                                           |
| Runbook            | On-call knows owners, toggles, dashboards, storage, registry stage, and first checks.                                            | Walk through a production incident scenario before launch.                                   | [Keep a GraphQL runbook](#keep-a-graphql-runbook)                                                                                                                                 |

Your expected output is a completed launch checklist for each service, with owners and verification evidence attached to your release.

# Minimum Production Baseline

Before you begin, ensure:

- You have a working Hot Chocolate v16 ASP.NET Core server.
- You know if clients require GET, multipart uploads, WebSockets, Nitro, SDL download, dynamic operations, or registry integration.
- You have an ASP.NET Core authentication scheme if any data is protected.
- You have selected an OpenTelemetry exporter, logging destination, or platform telemetry backend.

Start with a conservative `Program.cs` and relax settings only as your clients require.

```csharp
using HotChocolate.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddHealthChecks();

builder
    .AddGraphQL(maxAllowedRequestSize: 1_048_576) // 1 MiB HTTP request body limit.
    .AddQueryType<Query>()
    .AddAuthorization()
    .AddInstrumentation()
    .ModifyRequestOptions(options =>
    {
        options.IncludeExceptionDetails = builder.Environment.IsDevelopment();
        options.ExecutionTimeout = TimeSpan.FromSeconds(10);
    })
    .ModifyCostOptions(options =>
    {
        options.MaxFieldCost = 1_000;
        options.MaxTypeCost = 1_000;
    });

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGraphQL().WithOptions(options =>
{
    options.Tool.Enable = app.Environment.IsDevelopment();
    options.EnableSchemaRequests = app.Environment.IsDevelopment();
    options.EnableMultipartRequests = false;
    options.AllowedGetOperations = AllowedGetOperations.Query;
    options.MaxConcurrentExecutions = 64;
});

app.MapHealthChecks("/health/ready");
app.MapHealthChecks("/health/live");

app.Run();
```

With this setup:

- Nitro and SDL downloads are available only in development.
- Exception details are shown only in development.
- Multipart upload requests are rejected until you enable them.
- HTTP GET accepts queries only.
- Authentication runs before authorization and before the GraphQL endpoint executes.
- Health endpoints are separate from GraphQL so your platform can probe the process.

If your project uses `builder.Services.AddGraphQLServer(...)`, continue using it. The hosting-builder `builder.AddGraphQL(...)` API is a concise v16 style that delegates to the server builder and keeps default security enabled unless you pass `disableDefaultSecurity: true`.

Keep default security enabled unless you intentionally replace production introspection policy, cost analysis, or the max field-cycle validation rule. This baseline is intentionally conservative. The following sections explain when to tighten or relax these settings.

# Minimize Endpoint Exposure

Review what `MapGraphQL()` exposes at your chosen path:

| Surface                              | Default                                               | Production Decision                                                                     | Risk if Wrong                                                      | Option or API                                         | More Details                                                                                   |
| ------------------------------------ | ----------------------------------------------------- | --------------------------------------------------------------------------------------- | ------------------------------------------------------------------ | ----------------------------------------------------- | ---------------------------------------------------------------------------------------------- |
| `/graphql` POST                      | Enabled                                               | Keep enabled for normal GraphQL over HTTP.                                              | Clients cannot execute operations if disabled.                     | `MapGraphQL()` or `MapGraphQLHttp()`                  | [Endpoints](/docs/hotchocolate/v16/server/endpoints)                                           |
| `/graphql` GET                       | Enabled, queries only                                 | Keep for cacheable queries, or disable when unused.                                     | GET can expand cache and CSRF exposure if policy is unclear.       | `EnableGetRequests`, `AllowedGetOperations`           | [Endpoints](/docs/hotchocolate/v16/server/endpoints#enablegetrequests)                         |
| `/graphql?sdl`                       | Enabled                                               | Disable outside developer or CI workflows unless your API policy allows SDL downloads.  | Your schema is downloadable even when introspection is blocked.    | `EnableSchemaRequests`, `EnableSchemaFileSupport`     | [Endpoints](/docs/hotchocolate/v16/server/endpoints#enableschemarequests)                      |
| Nitro in browser                     | Enabled through combined endpoint                     | Enable locally. In staging or production, require internal network and auth if enabled. | Anyone who can browse the endpoint gets an IDE.                    | `Tool.Enable`, `MapNitroApp()`                        | [Endpoints](/docs/hotchocolate/v16/server/endpoints#mapnitroapp)                               |
| WebSocket subscriptions              | Available when ASP.NET Core WebSockets are registered | Enable only for schemas that expose subscriptions.                                      | Idle connections, load balancer issues, and auth renewal problems. | `MapGraphQLWebSocket()`, `Sockets`                    | [Options](/docs/hotchocolate/v16/api-reference/options#websocket-options-graphqlsocketoptions) |
| Multipart upload                     | Enabled                                               | Disable unless you use `Upload`.                                                        | Unneeded large request parsing and upload attack surface.          | `EnableMultipartRequests`                             | [Files](/docs/hotchocolate/v16/server/files)                                                   |
| Batching                             | Disabled                                              | Enable only for clients that need it. Set a batch size.                                 | A single HTTP request can fan out into many executions.            | `Batching`, `MaxBatchSize`, `MaxConcurrentExecutions` | [Options](/docs/hotchocolate/v16/api-reference/options#server-options-modifyserveroptions)     |
| Persisted operation publish/download | Not part of the combined endpoint unless mapped       | Expose only where your registry or deployment workflow requires it.                     | Operation storage can be poisoned or leaked if public.             | `MapGraphQLPersistedOperations()`                     | [Persisted operations](/docs/hotchocolate/v16/performance/trusted-documents)                   |

Test endpoints from a network location that matches your production clients:

```bash
curl -i http://localhost:5000/graphql \
    -H 'Content-Type: application/json' \
    --data '{"query":"{ __typename }"}'
```

Expected: `HTTP/1.1 200 OK` and a GraphQL response like:

```json
{ "data": { "__typename": "Query" } }
```

```bash
curl -i 'http://localhost:5000/graphql?query={__typename}'
```

Expected: `200 OK` when GET is enabled for queries, or `405 Method Not Allowed` when GET is disabled.

```bash
curl -i 'http://localhost:5000/graphql?sdl'
```

Expected: schema SDL in allowed environments, or a blocked response such as `404 Not Found` in production.

```bash
curl -i http://localhost:5000/graphql \
    -H 'GraphQL-preflight: 1' \
    -F operations='{ "query": "mutation ($file: Upload!) { uploadFile(file: $file) }", "variables": { "file": null } }' \
    -F map='{ "0": ["variables.file"] }' \
    -F 0=@file.txt
```

Expected: a GraphQL upload response only when multipart is enabled and the schema registers `Upload`. Otherwise, the request should be rejected.

# Secure Identity and Field Access

Test at least one public and one protected field with these requests:

| Request                                               | Expected Result                                                        |
| ----------------------------------------------------- | ---------------------------------------------------------------------- |
| Anonymous request to protected field                  | GraphQL authorization error, protected data is `null` or absent.       |
| Authenticated request without required role or policy | Authorization error, protected data is not returned.                   |
| Authenticated request with required role or policy    | Protected data is returned.                                            |
| Anonymous request to explicitly public field          | Public data is returned, unless endpoint-level auth blocks all access. |

Configure identity in this order:

```csharp
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

builder
    .AddGraphQL()
    .AddAuthorization()
    .AddQueryType<Query>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGraphQL();
```

Use `HotChocolate.Authorization.AuthorizeAttribute` on GraphQL fields and types. Do not use `Microsoft.AspNetCore.Authorization.AuthorizeAttribute` for GraphQL schema members, as it does not run through the Hot Chocolate authorization pipeline.

Endpoint authorization and field authorization serve different purposes:

```csharp
app.MapGraphQL().RequireAuthorization();
```

`RequireAuthorization()` protects transport access to the endpoint. Field and type authorization are still important when a schema mixes public and private fields, or when internal callers use the same endpoint.

Expected unauthorized response:

```json
{
  "errors": [
    {
      "message": "The current user is not authorized to access this resource.",
      "extensions": { "code": "AUTH_NOT_AUTHENTICATED" }
    }
  ],
  "data": { "me": null }
}
```

# Control Schema Discovery and Tools

Set policy per environment. Introspection, SDL download, and Nitro are related but controlled separately.

| Environment       | Introspection                                     | `?sdl` schema download                           | Nitro                              | Recommended Verification                                                            |
| ----------------- | ------------------------------------------------- | ------------------------------------------------ | ---------------------------------- | ----------------------------------------------------------------------------------- |
| Local             | On                                                | On                                               | On                                 | Developers can use tooling without credentials unless the app itself requires auth. |
| CI                | Prefer schema export command or startup export    | On only for controlled export jobs               | Off                                | CI stores or publishes the expected schema artifact.                                |
| Staging           | Off by default, or allowlisted for internal users | Off unless needed by validation jobs             | Internal only                      | External network receives blocked responses.                                        |
| Production        | Off unless your public API policy requires it     | Off unless your policy requires downloadable SDL | Off, or internal and authenticated | Public clients cannot discover tools accidentally.                                  |
| Break-glass/admin | Per-request allowlist with owner and expiry       | Temporary path or protected job                  | Temporary internal access          | Runbook records who enabled access and when it expires.                             |

`AllowIntrospection(false)` controls introspection queries, but does not disable `?sdl`. Use endpoint options for SDL downloads and Nitro:

```csharp
builder
    .AddGraphQL()
    .AllowIntrospection(builder.Environment.IsDevelopment());

app.MapGraphQL().WithOptions(options =>
{
    options.EnableSchemaRequests = app.Environment.IsDevelopment();
    options.Tool.Enable = app.Environment.IsDevelopment();
});
```

Test with:

```bash
curl -i http://localhost:5000/graphql \
    -H 'Content-Type: application/json' \
    --data '{"query":"{ __schema { queryType { name } } }"}'
```

Expected in production: a GraphQL error such as `Introspection is not allowed for the current request.`

```bash
curl -i 'http://localhost:5000/graphql?sdl'
```

Expected in production: blocked response when `EnableSchemaRequests` is `false`.

Open `http://localhost:5000/graphql` in a browser. In production, Nitro UI should not appear unless your policy explicitly enables it.

# Bound Request Cost and Resource Usage

Tune limits based on observed operations, not guesses.

| Operation name or ID | Max page size | Field cost | Type cost  | Runtime p95  | Backend query count | Accepted or rejected     | Selected limit                                     |
| -------------------- | ------------- | ---------- | ---------- | ------------ | ------------------- | ------------------------ | -------------------------------------------------- |
| `GetProductList`     | `50`          | `240`      | `310`      | `120 ms`     | `2`                 | Accepted                 | Keep defaults                                      |
| `AdminReport`        | `100`         | `1,450`    | `2,200`    | `2.4 s`      | `6`                 | Accepted for admins only | Raise cost limit for admin path or split operation |
| Recursive test query | `50`          | Over limit | Over limit | Not executed | `0`                 | Rejected                 | Keep field-cycle and cost limits                   |

Configure limits close to the resource they protect:

```csharp
builder
    .AddGraphQL(maxAllowedRequestSize: 1_048_576)
    .ModifyParserOptions(options =>
    {
        options.MaxAllowedFields = 500;
        options.MaxAllowedNodes = 5_000;
        options.MaxAllowedTokens = 10_000;
    })
    .SetMaxAllowedValidationErrors(5)
    .SetMaxAllowedFieldMergeComparisons(50_000)
    .AddMaxAllowedFieldCycleDepthRule(defaultCycleLimit: 3)
    .ModifyRequestOptions(options =>
    {
        options.ExecutionTimeout = TimeSpan.FromSeconds(10);
    })
    .ModifyPagingOptions(options =>
    {
        options.DefaultPageSize = 10;
        options.MaxPageSize = 50;
        options.RequirePagingBoundaries = true;
    })
    .ModifyCostOptions(options =>
    {
        options.MaxFieldCost = 1_000;
        options.MaxTypeCost = 1_000;
    })
    .AddGlobalObjectIdentification(options =>
    {
        options.MaxAllowedNodeBatchSize = 25;
    });
```

Measure cost before you tighten limits:

```bash
curl -i http://localhost:5000/graphql \
    -H 'Content-Type: application/json' \
    -H 'GraphQL-Cost: report' \
    --data '{"query":"query GetTypename { __typename }"}'
```

Expected result: the operation executes and the response includes cost metrics in `extensions`.

```bash
curl -i http://localhost:5000/graphql \
    -H 'Content-Type: application/json' \
    -H 'GraphQL-Cost: validate' \
    --data '{"query":"query GetTypename { __typename }"}'
```

Expected result: the response reports cost metrics without executing the operation.

Trusted documents reduce dynamic query exposure, but they do not replace timeouts, concurrency limits, page boundaries, and backend limits. Keep resource limits for internal callers, warmup, rollout windows, and break-glass access.

# Choose trusted operations, APQ, or dynamic operations

Pick the persisted-operation model before launch.

| Client model              | Recommended model                                   | Why                                            | Operational requirements                                                           |
| ------------------------- | --------------------------------------------------- | ---------------------------------------------- | ---------------------------------------------------------------------------------- |
| Public third-party API    | Dynamic operations with strong limits and auth      | Unknown clients need to write new operations.  | Tight cost limits, documentation, auth, abuse monitoring.                          |
| First-party web or mobile | Trusted documents plus client registry workflow     | Clients are known at build time.               | Extract operations, publish them, deploy durable storage, block dynamic documents. |
| Partner API               | Trusted documents or allowlisted dynamic operations | Partner release cadence may differ from yours. | Compatibility checks, partner-specific rollout plan, emergency allowlist.          |
| Internal admin            | Dynamic operations behind strong controls           | Admins may need exploratory queries.           | Network controls, auth, audit logs, time-limited access.                           |

Trusted documents pre-register operation documents and let clients send only an `id`:

```json
{
  "id": "0c95d31ca29272475bf837f944f4e513",
  "variables": { "id": 123 }
}
```

Configure the persisted operation pipeline and block dynamic documents when every production client has registered operations:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .UsePersistedOperationPipeline()
    .AddFileSystemOperationDocumentStorage("./persisted_operations")
    .ModifyRequestOptions(options =>
    {
        options.PersistedOperations.OnlyAllowPersistedDocuments = true;
    });
```

`AllowDocumentBody` lets a request include a full document even when only persisted documents are allowed. Use it only for controlled migration or validation workflows. The strongest lock-down posture sends IDs only.

`AllowNonPersistedOperation()` is a break-glass or controlled internal-caller override. Put it behind explicit authorization and record an expiry in the runbook.

APQ stores operations at runtime. It reduces bandwidth and can work well with HTTP GET and CDN caching, but multi-instance deployments need durable shared operation storage such as Redis, Azure Blob Storage, or another supported provider.

Expected rollout plan:

1. Choose dynamic, trusted documents, APQ, or a hybrid.
2. Choose storage and hash algorithm.
3. Add the client build step that extracts operations.
4. Validate operations against the schema in CI or a registry.
5. Deploy storage before enabling `OnlyAllowPersistedDocuments`.
6. Define the emergency dynamic-operation bypass and owner.

# Set upload limits before enabling multipart

Multipart is enabled at the endpoint level by default. Disable it unless your schema uses `Upload`.

| Check            | Ready when                                                                                                                    |
| ---------------- | ----------------------------------------------------------------------------------------------------------------------------- |
| Schema need      | You have at least one mutation that accepts `Upload` or `IFile`.                                                              |
| Endpoint setting | `EnableMultipartRequests` is `true` only for upload-enabled endpoints.                                                        |
| Upload scalar    | The schema registers `.AddUploadType()` or `.AddType<UploadType>()`.                                                          |
| Client preflight | Clients send `GraphQL-preflight: 1`.                                                                                          |
| Size limits      | Hot Chocolate, ASP.NET Core, Kestrel or IIS, proxy, CDN or gateway, and storage limits match.                                 |
| Validation       | You validate size, content type, extension, file name, malware scan requirements, tenant or user ownership, and storage path. |
| Storage          | You stream to storage with `IFile.OpenReadStream()` and return a stable file URL or ID.                                       |
| Alternative      | Large files use presigned upload URLs or a dedicated upload endpoint.                                                         |

Minimal server configuration:

```csharp
using Microsoft.AspNetCore.Http.Features;

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10 MiB.
});

builder
    .AddGraphQL()
    .AddUploadType()
    .AddMutationType<Mutation>();

app.MapGraphQL().WithOptions(options =>
{
    options.EnableMultipartRequests = true;
});
```

Infrastructure alignment:

| Layer                  | Setting to confirm                                                               |
| ---------------------- | -------------------------------------------------------------------------------- |
| Hot Chocolate endpoint | `EnableMultipartRequests`, multipart preflight enforcement, request body size.   |
| ASP.NET Core           | `FormOptions.MultipartBodyLengthLimit`.                                          |
| Kestrel or IIS         | Maximum request body size and timeout.                                           |
| Reverse proxy          | Body size, buffering, timeout, WebSocket behavior if shared.                     |
| CDN or API gateway     | Upload size, method, header allowlist, timeout.                                  |
| Storage                | Object size, content-type policy, malware scanning, retention, tenant isolation. |

Boundary tests:

- Valid upload with `GraphQL-preflight: 1` returns the mutation payload.
- Missing preflight header is rejected.
- Too-large upload is rejected by the expected layer.
- Wrong content type or extension is rejected by application validation.
- Dangerous file names are sanitized or ignored before storage.

# Mask errors and preserve diagnostics

Compare response shapes before launch.

Unhandled exception in production:

```json
{
  "data": { "userById": null },
  "errors": [
    {
      "message": "Unexpected Execution Error",
      "path": ["userById"]
    }
  ]
}
```

Domain error with a stable code:

```json
{
  "errors": [
    {
      "message": "Rate limit exceeded.",
      "extensions": { "code": "RATE_LIMITED" }
    }
  ]
}
```

Authorization error:

```json
{
  "errors": [
    {
      "message": "The current user is not authorized to access this resource.",
      "extensions": { "code": "AUTH_NOT_AUTHENTICATED" }
    }
  ]
}
```

Set exception detail behavior explicitly:

```csharp
builder
    .AddGraphQL()
    .ModifyRequestOptions(options =>
    {
        options.IncludeExceptionDetails = builder.Environment.IsDevelopment();
    })
    .AddErrorFilter<LoggingErrorFilter>();

public sealed class LoggingErrorFilter : IErrorFilter
{
    private readonly ILogger<LoggingErrorFilter> _logger;

    public LoggingErrorFilter(ILogger<LoggingErrorFilter> logger)
    {
        _logger = logger;
    }

    public IError OnError(IError error)
    {
        if (error.Exception is null)
        {
            return error;
        }

        _logger.LogError(error.Exception, "Unhandled GraphQL exception.");

        return error
            .WithMessage("An internal error occurred.")
            .WithCode("INTERNAL_ERROR")
            .WithException(null);
    }
}
```

Log these fields when available:

- Trace ID and correlation ID.
- Operation name, operation type, document hash, or persisted operation ID.
- Authenticated user ID, tenant ID, and client ID, when allowed by your privacy policy.
- Error code, path, and category.
- Timeout, cost, validation, authorization, and persisted-operation miss indicators.

Avoid logging raw documents or variables that can contain secrets unless your organization explicitly permits it.

# Instrument what you need to operate

Minimum dashboard checklist:

| Signal                                   | Use it to answer                                                         |
| ---------------------------------------- | ------------------------------------------------------------------------ |
| GraphQL request rate                     | Is traffic normal for this service and stage?                            |
| Latency p50, p95, p99                    | Which operations regressed after deploy?                                 |
| Error rate                               | Are failures transport errors, GraphQL request errors, or domain errors? |
| Timeout rejects                          | Are operations exceeding `ExecutionTimeout`?                             |
| Cost and validation rejects              | Are clients sending operations outside your budget?                      |
| Auth rejects                             | Is authentication or authorization failing unexpectedly?                 |
| Top operation names or IDs               | Which clients or persisted operations drive load?                        |
| Persisted or APQ hits and misses         | Did client operation rollout or storage break?                           |
| DataLoader batch sizes and backend calls | Are resolvers batching correctly?                                        |
| Upload rejects and payload sizes         | Are upload limits working at the expected layer?                         |
| Startup, readiness, and warmup failures  | Can new instances safely receive traffic?                                |

Configure Hot Chocolate instrumentation with OpenTelemetry:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddInstrumentation();

builder.Services
    .AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation();
        tracing.AddHttpClientInstrumentation();
        tracing.AddHotChocolateInstrumentation();
        tracing.AddOtlpExporter();
    });
```

Alert on symptoms, not implementation details:

- Elevated 5xx responses or GraphQL request error rate.
- p95 or p99 latency regression on top operations.
- Timeout rejects or cost rejects spike.
- Persisted operation misses spike after deploy.
- Auth failures spike after identity-provider or config changes.
- Readiness or dependency checks fail.

Keep cardinality low. Operation type and operation name are safe dashboard dimensions. Document hash or persisted operation ID is useful for trusted operations. Full document text and variable values increase sensitive-data risk and should not be routine telemetry.

Diagnostic event handlers run synchronously during request execution. Enqueue expensive work to a background service instead of calling external systems inline.

# Prove readiness with health checks and warmup

Hot Chocolate v16 builds schemas eagerly by default. Startup should fail early for schema errors before Kestrel accepts traffic. Warmup goes further by filling document and operation caches before real traffic arrives.

Readiness checklist:

| Dependency or state         | Ready when                                                              |
| --------------------------- | ----------------------------------------------------------------------- |
| Schema                      | The request executor builds successfully at startup.                    |
| Database                    | Required database checks pass.                                          |
| Cache                       | Required cache checks pass.                                             |
| Object storage              | Required upload or persisted-operation storage is reachable.            |
| Persisted operation storage | Operation documents are loaded or registry-backed storage is available. |
| Configuration               | Environment toggles and secrets are present.                            |
| Warmup                      | Representative operations have been parsed and prepared.                |
| Registry state              | Schema and client registry stage match the deployment stage when used.  |

Warm a representative operation:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddWarmupTask(async (executor, cancellationToken) =>
    {
        var request = OperationRequestBuilder.New()
            .SetDocument("query GetProducts { products(first: 10) { nodes { id name } } }")
            .SetOperationName("GetProducts")
            .MarkAsWarmupRequest()
            .Build();

        await executor.ExecuteAsync(request, cancellationToken);
    });
```

Expected behavior: startup finishes after schema creation and warmup complete. The first real `GetProducts` request can reuse warmed parsing and operation caches. Because the operation name participates in the cache key, include it when clients send it.

Keep `LazyInitialization = false` in production unless you intentionally trade startup time for first-request latency. Map liveness and readiness separately and expose only what your hosting platform needs:

```csharp
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
```

ASP.NET Core health checks own dependency probing. Hot Chocolate makes those probes more meaningful by building the schema eagerly and blocking startup while warmup tasks run.

# Tune data access and caches

Review top operations with production-like data.

| Review item               | Question to answer                                                                     |
| ------------------------- | -------------------------------------------------------------------------------------- |
| Resolver inventory        | Which fields call databases, services, or storage?                                     |
| Backend calls per field   | Does one GraphQL list create N downstream calls?                                       |
| DataLoader coverage       | Are relationship lookups batched and cached per request?                               |
| Batch-size histogram      | Do batches stay below backend limits such as SQL parameter limits?                     |
| Projection usage          | Do collection fields push selection, filtering, sorting, and paging into the database? |
| Cache hit rate            | Are prepared operation and document caches sized for traffic shape?                    |
| Top operation p95 and p99 | Do hot operations meet the latency budget?                                             |
| Memory pressure           | Do large responses, batches, or cache sizes create allocation spikes?                  |

Use DataLoader to prevent N+1 fetches. The default DataLoader `MaxBatchSize` is `1024`; lower it when your backend has smaller safe batch limits.

For database-backed collections, apply paging, projection, filtering, and sorting in the documented order: `UsePaging`, `UseProjection`, `UseFiltering`, `UseSorting`.

Hot Chocolate caches parsed documents and prepared operations. The default `PreparedOperationCacheSize` and `OperationDocumentCacheSize` are `256`, with a minimum of `16`.

```csharp
builder
    .AddGraphQL()
    .ModifyOptions(options =>
    {
        options.PreparedOperationCacheSize = 1024;
        options.OperationDocumentCacheSize = 1024;
    });
```

Persisted operations stabilize hot operation shapes and can skip parsing and validation for known operations. They complement, rather than replace, DataLoader and database query review.

# Govern schema changes before deploy

Gate schema and operation changes in CI.

| Gate                         | Ready when                                                          | Failure example                                              |
| ---------------------------- | ------------------------------------------------------------------- | ------------------------------------------------------------ |
| Schema export                | CI exports SDL from the current server.                             | Schema cannot build or export.                               |
| Schema diff                  | Changes are classified as safe, dangerous, or breaking.             | A field is removed without migration.                        |
| Snapshot tests               | Expected SDL changes are reviewed in pull requests.                 | Nullability changes unexpectedly.                            |
| Client operation validation  | Known client operations validate against the candidate schema.      | Active mobile app operation no longer compiles.              |
| Persisted operation artifact | Client build publishes operation IDs expected by the server.        | Server deploy blocks dynamic docs but storage lacks new IDs. |
| Registry publish             | Schema and client registry stages are updated in the correct order. | Production stage points to the wrong schema.                 |
| Deployment promotion         | Canary or blue-green plan accounts for old and new client versions. | Rollback removes operations still used by clients.           |

Export schema SDL in CI:

```bash
dotnet run -- schema export --output schema.graphql
```

You can also export during startup for controlled jobs:

```csharp
builder
    .AddGraphQL()
    .ExportSchemaOnStartup("./schema.graphql");
```

Snapshot schemas with CookieCrumble when that is your project convention. Publish schemas to the Nitro schema registry when your organization uses registry workflows, and publish client operations to the client registry or an equivalent compatibility system.

Release checklist:

- New fields are additive and documented.
- Deprecated fields include reason and removal timeline.
- Breaking or dangerous changes are approved with client impact known.
- Active client versions validate against the target schema.
- Persisted operation storage contains old and new operation IDs during rollout.
- Rollback keeps schema and client operations compatible with both server versions.

# Align deployment and hosting configuration

Many production failures come from mismatched platform settings rather than GraphQL code.

| Layer                                          | Align before launch                                                                      |
| ---------------------------------------------- | ---------------------------------------------------------------------------------------- |
| Kestrel                                        | Request body size, timeouts, HTTPS, forwarded headers, WebSocket support.                |
| IIS, nginx, Envoy, or ingress                  | Body size, buffering, request timeout, response timeout, WebSocket upgrade headers.      |
| CDN or API gateway                             | GET caching policy, CORS, allowed headers, request size, persisted-operation cache keys. |
| Kubernetes, App Service, Aspire, or other host | Readiness and liveness probes, rollout strategy, secret injection, resource limits.      |
| Redis, blob, file, or registry storage         | Durable APQ or trusted-operation storage for every instance.                             |
| Identity provider                              | Token issuer, audience, signing keys, clock skew, role and policy claims.                |
| Observability backend                          | OTLP endpoint, sampling, dashboards, alert routes, retention.                            |
| Nitro registry                                 | API ID, stage, API key, schema publish, client publish, operation distribution.          |

Pin environment-specific values for:

- Nitro exposure.
- Introspection.
- SDL download.
- Multipart uploads.
- Dynamic operation policy.
- Cost and parser limits.
- APQ or trusted-operation storage.

Externalize secrets, auth settings, storage credentials, and registry API keys. Do not bake them into source code or container images.

Plan rollout with compatibility in mind. A blue-green or canary deployment must handle old clients, new clients, old persisted operations, new persisted operations, and rollback order.

# Troubleshoot production launch blockers

| Symptom                                           | Likely cause                                                                              | First check                                                       | Fix                                                                                | Deeper link                                                                                    |
| ------------------------------------------------- | ----------------------------------------------------------------------------------------- | ----------------------------------------------------------------- | ---------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------- |
| Nitro is visible in production.                   | `Tool.Enable` is true or `MapNitroApp()` is exposed.                                      | Open the endpoint in a browser from an external network.          | Disable Nitro or move it behind internal auth and network controls.                | [Endpoints](/docs/hotchocolate/v16/server/endpoints#mapnitroapp)                               |
| Introspection is unexpectedly blocked or allowed. | Environment toggle or per-request interceptor policy is wrong.                            | Run an introspection query and inspect `AllowIntrospection`.      | Set environment policy explicitly and test break-glass headers.                    | [Introspection](/docs/hotchocolate/v16/securing-your-api/introspection)                        |
| `?sdl` downloads the schema unexpectedly.         | SDL download is controlled separately from introspection.                                 | Request `/graphql?sdl`.                                           | Set `EnableSchemaRequests = false` and review schema file support.                 | [Endpoints](/docs/hotchocolate/v16/server/endpoints#enableschemarequests)                      |
| Legitimate query is rejected.                     | Cost, field-cycle, parser, validation, timeout, batch, or concurrency limit is too low.   | Inspect GraphQL error code and server logs.                       | Measure with `GraphQL-Cost: report`, then adjust the narrowest limit.              | [Request limits](/docs/hotchocolate/v16/securing-your-api/request-limits)                      |
| APQ or trusted operation returns not found.       | Client hash, storage, stage, or deployment order differs from the server.                 | Check operation ID in request and storage.                        | Republish operations, verify hash algorithm, use durable shared storage.           | [Persisted operations](/docs/hotchocolate/v16/performance/trusted-documents)                   |
| Upload fails with 413.                            | A body-size limit is lower than expected.                                                 | Compare app, server, proxy, gateway, and storage limits.          | Raise the intended layer or lower the documented maximum.                          | [Files](/docs/hotchocolate/v16/server/files#options)                                           |
| Upload fails with missing preflight or null file. | Client did not send `GraphQL-preflight: 1` or multipart map is wrong.                     | Capture the multipart request.                                    | Fix client headers and variables map.                                              | [Files](/docs/hotchocolate/v16/server/files#client-usage)                                      |
| First request is slow after deploy.               | Warmup does not include the operation name, or lazy initialization is enabled.            | Inspect startup logs and cache behavior.                          | Add `AddWarmupTask()` and keep eager initialization.                               | [Warmup](/docs/hotchocolate/v16/server/warmup)                                                 |
| Resolvers always see anonymous users.             | `UseAuthentication()` is missing or ordered after GraphQL.                                | Inspect ASP.NET Core middleware order.                            | Call `UseAuthentication()` before `UseAuthorization()` and before mapping GraphQL. | [Authentication](/docs/hotchocolate/v16/securing-your-api/authentication)                      |
| Authorization attributes do not work.             | The Microsoft `AuthorizeAttribute` was used on GraphQL members.                           | Check the attribute namespace.                                    | Use `HotChocolate.Authorization.AuthorizeAttribute`.                               | [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization)                        |
| WebSocket subscriptions fail.                     | WebSockets, connection initialization, auth, or load balancer upgrade support is missing. | Check `connection_init`, upgrade headers, and load balancer logs. | Enable WebSockets and tune socket options and proxy support.                       | [Options](/docs/hotchocolate/v16/api-reference/options#websocket-options-graphqlsocketoptions) |
| No GraphQL traces appear in OpenTelemetry.        | Missing package, `.AddInstrumentation()`, or `AddHotChocolateInstrumentation()`.          | Check startup logs and tracer provider setup.                     | Register Hot Chocolate instrumentation and exporter.                               | [Instrumentation](/docs/hotchocolate/v16/server/instrumentation)                               |
| Schema registry rejects a change.                 | Candidate schema breaks active clients or targets wrong stage.                            | Review registry diff and active client versions.                  | Deprecate first, update clients, or publish to the correct stage.                  | [Schema registry](/docs/nitro/apis/schema-registry)                                            |
| Health or readiness never becomes healthy.        | Dependency check, schema build, warmup, or config load fails.                             | Inspect startup logs and health check details.                    | Fix dependency, schema error, warmup operation, or missing secret.                 | [Warmup](/docs/hotchocolate/v16/server/warmup)                                                 |

# Keep a GraphQL runbook

Keep this table in your service runbook and update it during every production review.

| Field                    | Value to record                                                                                                     |
| ------------------------ | ------------------------------------------------------------------------------------------------------------------- |
| Service owner            | Team, on-call rotation, escalation channel.                                                                         |
| Endpoint inventory       | GraphQL path, Nitro path, SDL policy, WebSocket path, persisted-operation endpoints.                                |
| Environment toggles      | Nitro, introspection, SDL, multipart, uploads, dynamic operations, break-glass headers.                             |
| Persisted or APQ storage | Storage type, location, hash algorithm, rebuild or republish process.                                               |
| Registry state           | Nitro API, schema registry stage, client registry stage, API key owner.                                             |
| Dashboard and alerts     | Links for request rate, latency, errors, rejects, dependency health, and warmup.                                    |
| Top operations           | Operation names or IDs, owning clients, expected latency and cost.                                                  |
| Rollback steps           | Server rollback, schema rollback, client operation rollback, persisted storage rollback, registry publish rollback. |
| Break-glass process      | Who can allow dynamic operations or introspection, approval path, expiry, audit location.                           |
| Upload incident plan     | Disable switch, storage owner, malware scanning contact, retention and cleanup process.                             |

A useful runbook starts with first checks:

1. Is the service ready and receiving traffic?
2. Did the last deploy change schema, auth, persisted operations, or platform limits?
3. Which operation name or ID is failing?
4. Is the failure a transport error, GraphQL request error, domain error, authorization error, timeout, cost reject, or storage miss?
5. Can you mitigate with a documented toggle without exposing data or accepting unbounded traffic?

# Next steps

- Review endpoint details in [Endpoints](/docs/hotchocolate/v16/server/endpoints).
- Harden authentication and authorization with [Authentication](/docs/hotchocolate/v16/securing-your-api/authentication) and [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization).
- Tune limits with [Request limits](/docs/hotchocolate/v16/securing-your-api/request-limits) and [Cost analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis).
- Choose a persisted-operation strategy with [Persisted operations](/docs/hotchocolate/v16/performance/trusted-documents) and [Automatic persisted operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations).
- Add telemetry with [Instrumentation](/docs/hotchocolate/v16/server/instrumentation) and reduce cold starts with [Warmup](/docs/hotchocolate/v16/server/warmup).
- Govern releases with [Schema evolution](/docs/hotchocolate/v16/guides/schema-evolution), [Testing](/docs/hotchocolate/v16/guides/testing), [Schema registry](/docs/nitro/apis/schema-registry), and [Client registry](/docs/nitro/apis/client-registry).
