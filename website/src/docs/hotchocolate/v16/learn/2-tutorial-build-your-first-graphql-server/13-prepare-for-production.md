---
title: "Prepare for production"
description: "Add production readiness checks to the tutorial server: request limits, persisted operations, warmup, telemetry, and a repeatable smoke test."
---

The previous chapter established your first layer of security, setting boundaries for who can access or modify protected data.

Now, you will take the first steps toward making your server production-ready. This chapter will not cover every deployment detail, but it will help you make your tutorial server safe for team review and clarify what work remains before a real launch.

By the end of this chapter, you will:

- Define what production-ready means for this tutorial server
- Add request and cost limits to protect against unknown traffic
- Choose a strategy for persisted operations
- Configure a trusted document path for production review
- Warm up the executor during startup
- Add OpenTelemetry instrumentation
- Run a production-style smoke test
- Record the remaining deployment checklist

# What does production-ready mean for this API?

A server that works locally is not ready for production. Production readiness means your team can answer these questions with evidence:

| Area | Production question |
| --- | --- |
| Access | Which users and clients can reach each field? |
| Request budget | How much work can one request ask the server to do? |
| Operation control | Who can introduce a new operation into production? |
| Startup | Does the schema and a typical request path initialize before traffic arrives? |
| Observability | Can you see successful requests, rejected requests, errors, and latency? |
| Verification | Can you repeat a smoke test before deployment? |

This chapter focuses on the GraphQL server itself. It does not replace a full platform guide, security audit, or SRE playbook. Your real application will also need secret management, TLS, database migrations, backups, health checks, rate limiting, CORS, hosting configuration, rollback strategy, and a real identity provider.

Continue using the authorization checks from the previous chapter. The steps below assume you already know which operations should succeed for an allowed user and which protected fields should fail for anonymous or disallowed users.

# Set request cost limits before accepting unknown traffic

GraphQL gives clients flexibility, but that means a single request can demand too much work through deep nesting, large pages, aliases, fragments, filtering, or sorting.

Cost analysis lets you set a request budget before execution. Hot Chocolate will reject operations that exceed your configured field or type cost, before any resolvers run.

Open `Program.cs` and add a basic budget to the GraphQL builder:

```csharp
builder
    .AddGraphQL()
    .AddFiltering()
    .AddMutationConventions(applyToAllMutations: true)
    .AddInMemorySubscriptions()
    .AddAuthorization()
    .ModifyParserOptions(options =>
    {
        options.MaxAllowedFields = 256;
        options.MaxAllowedRecursionDepth = 50;
        options.MaxAllowedTokens = 4_096;
    })
    .AddMaxExecutionDepthRule(8, skipIntrospectionFields: true)
    .SetMaxAllowedValidationErrors(5)
    .ModifyRequestOptions(options =>
    {
        options.ExecutionTimeout = TimeSpan.FromSeconds(10);
    })
    .ModifyCostOptions(options =>
    {
        options.MaxFieldCost = 1_000;
        options.MaxTypeCost = 1_000;
        options.EnforceCostLimits = true;
    })
    .AddTypes();
```

Keep any GraphQL registrations from earlier chapters. The new parts are parser limits, validation limits, an execution timeout, and cost limits.

These values are a starting point. Adjust them based on your real client operations. Different APIs (public, private, internal) will need different budgets.

## Measure cost before tuning

Start the server:

```bash
dotnet run
```

Send a typical read operation with the `GraphQL-Cost: report` header:

```bash
curl -s http://localhost:5095/graphql \
  -H "Content-Type: application/json" \
  -H "GraphQL-Cost: report" \
  -d '{"query":"query ProductionSmokeTest { books(first: 2) { nodes { id title author { id name } } pageInfo { hasNextPage endCursor } } }"}'
```

You should see:

- `data.books` in the response
- Cost information in the `extensions` field
- The operation stays within your configured field and type cost

If cost information is missing, check that the header reaches the `/graphql` endpoint and compare your setup with [Cost Analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis).

After measuring expected operations, try a larger request or temporarily lower your limits. The goal is to see a GraphQL error before execution, not a slow resolver call.

For a full list of parser, validation, and execution safeguards, see [Request Limits](/docs/hotchocolate/v16/securing-your-api/request-limits).

# Decide how clients can send operations

Before deploying, decide who is allowed to introduce new GraphQL operations into production.

| Team situation | Recommended posture | Why |
| --- | --- | --- |
| Local tutorial or early development | Dynamic operations | You are still changing the schema and running ad hoc Nitro requests. |
| First-party clients with a release pipeline | Trusted documents | Clients publish known operations before deployment, and the server blocks unknown documents. |
| Many clients or runtime operation discovery | Automatic persisted operations | Clients can store operations at runtime and benefit from hash-based requests and caching. |

For a closed or first-party API, trusted documents are the recommended production target. The client build extracts operations, computes Hot Chocolate document hashes, publishes documents to operation storage, and the server executes requests by document hash. When ready, enable `OnlyAllowPersistedDocuments` to block dynamic operation documents.

Automatic persisted operations (APQ) solve a different problem. APQ lets clients discover and store operation documents at runtime, reducing request size and helping with caching. However, APQ is not an allow-list unless you pair it with storage, policy, and rollout controls.

This tutorial shows the server-side setup for trusted documents, not a full client publishing workflow.

# Configure a trusted document path for production

Add the filesystem storage package:

```bash
dotnet add package HotChocolate.PersistedOperations.FileSystem
```

Create a `persisted_operations` folder in your server project. Your client publishing workflow or operation registry should place one file per operation in that folder:

```text
persisted_operations/
  <document-hash>.graphql
```

For filesystem storage, the file name must be the Hot Chocolate document hash with a `.graphql` extension. The request `id` must use the same document hash (without the extension). The file content is the GraphQL operation document that produced that hash.

Example document:

```graphql
query ProductionSmokeTest {
  books(first: 2) {
    nodes {
      id
      title
      author {
        id
        name
      }
    }
    pageInfo {
      hasNextPage
      endCursor
    }
  }
}
```

Register the persisted operation pipeline and storage folder:

```csharp
builder
    .AddGraphQL()
    .AddFiltering()
    .AddMutationConventions(applyToAllMutations: true)
    .AddInMemorySubscriptions()
    .AddAuthorization()
    .ModifyParserOptions(options =>
    {
        options.MaxAllowedFields = 256;
        options.MaxAllowedRecursionDepth = 50;
        options.MaxAllowedTokens = 4_096;
    })
    .AddMaxExecutionDepthRule(8, skipIntrospectionFields: true)
    .SetMaxAllowedValidationErrors(5)
    .ModifyRequestOptions(options =>
    {
        options.ExecutionTimeout = TimeSpan.FromSeconds(10);
        options.PersistedOperations.OnlyAllowPersistedDocuments = true;
    })
    .ModifyCostOptions(options =>
    {
        options.MaxFieldCost = 1_000;
        options.MaxTypeCost = 1_000;
        options.EnforceCostLimits = true;
    })
    .UsePersistedOperationPipeline()
    .AddFileSystemOperationDocumentStorage("./persisted_operations")
    .AddTypes();
```

Enable `OnlyAllowPersistedDocuments` only after you have at least one stored operation and a documented developer workflow. This setting blocks Nitro and ad hoc requests that send a new `query` document, unless you add a deliberate, authenticated bypass.

Verify the stored operation by sending the document hash as `id`:

```bash
curl -s http://localhost:5095/graphql \
  -H "Content-Type: application/json" \
  -d '{"id":"<document-hash>"}'
```

You should see:

- The stored operation executes and returns `data.books`
- A request with a new `query` document fails when `OnlyAllowPersistedDocuments` is enabled
- The error message states that only persisted operations are allowed

If the operation is not found, check the file name, folder path, hash algorithm, request `id`, and the working directory used by `dotnet run`.

For more on durable storage and registry workflows, see [Persisted Operations](/docs/hotchocolate/v16/performance/trusted-documents) and the [Nitro client registry](/docs/nitro/apis/client-registry). If your team needs runtime operation storage, see [Automatic Persisted Operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations).

# Warm up the executor before serving traffic

Hot Chocolate builds the schema eagerly at startup. Warmup tasks go further: they run representative requests against the executor so caches are populated before the first user request.

Add a warmup task for the smoke-test query:

```csharp
using HotChocolate.Execution;
```

```csharp
builder
    .AddGraphQL()
    .AddFiltering()
    .AddMutationConventions(applyToAllMutations: true)
    .AddInMemorySubscriptions()
    .AddAuthorization()
    .ModifyParserOptions(options =>
    {
        options.MaxAllowedFields = 256;
        options.MaxAllowedRecursionDepth = 50;
        options.MaxAllowedTokens = 4_096;
    })
    .AddMaxExecutionDepthRule(8, skipIntrospectionFields: true)
    .SetMaxAllowedValidationErrors(5)
    .ModifyRequestOptions(options =>
    {
        options.ExecutionTimeout = TimeSpan.FromSeconds(10);
        options.PersistedOperations.OnlyAllowPersistedDocuments = true;
    })
    .ModifyCostOptions(options =>
    {
        options.MaxFieldCost = 1_000;
        options.MaxTypeCost = 1_000;
        options.EnforceCostLimits = true;
    })
    .UsePersistedOperationPipeline()
    .AddFileSystemOperationDocumentStorage("./persisted_operations")
    .AddWarmupTask(async (executor, cancellationToken) =>
    {
        var request = OperationRequestBuilder.New()
            .SetDocument("""
                query ProductionSmokeTest {
                  books(first: 2) {
                    nodes {
                      id
                      title
                      author {
                        id
                        name
                      }
                    }
                    pageInfo {
                      hasNextPage
                      endCursor
                    }
                  }
                }
                """)
            .SetOperationName("ProductionSmokeTest")
            .MarkAsWarmupRequest()
            .Build();

        await executor.ExecuteAsync(request, cancellationToken);
    })
    .AddTypes();
```

`MarkAsWarmupRequest()` populates caches without executing the operation. Use it for warmup queries that should not hit the database or trigger side effects. Include the operation name, as it is part of the cache key.

Warmup blocks startup. Keep warmup operations read-only, representative, and bounded. Do not warm up mutations or subscriptions in this tutorial.

You should see:

- `dotnet run` starts without a warmup exception
- Startup completes after schema creation and warmup
- The `ProductionSmokeTest` operation succeeds after startup

For custom warmup tasks, schema export, and lazy initialization, see [Warmup](/docs/hotchocolate/v16/server/warmup).

# Add observability before debugging production issues

Production systems need telemetry. You must be able to see whether requests succeed, whether limits reject an operation, which operation ran, and where time is spent.

Add the Hot Chocolate diagnostics package:

```bash
dotnet add package HotChocolate.Diagnostics
```

Add OpenTelemetry packages:

```bash
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Instrumentation.AspNetCore
dotnet add package OpenTelemetry.Instrumentation.Http
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
```

Add these using directives if not already present:

```csharp
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
```

Register instrumentation on the GraphQL builder:

```csharp
builder
    .AddGraphQL()
    .AddFiltering()
    .AddMutationConventions(applyToAllMutations: true)
    .AddInMemorySubscriptions()
    .AddAuthorization()
    // Existing limits, persisted operation setup, and warmup omitted.
    .AddInstrumentation()
    .AddTypes();
```

Then register OpenTelemetry with ASP.NET Core and Hot Chocolate instrumentation:

```csharp
builder.Logging.AddOpenTelemetry(
    options =>
    {
        options.IncludeFormattedMessage = true;
        options.IncludeScopes = true;
        options.ParseStateValues = true;
        options.SetResourceBuilder(
            ResourceBuilder.CreateDefault().AddService("LibraryServer"));
    });

builder.Services
    .AddOpenTelemetry()
    .WithTracing(
        options =>
        {
            options.AddAspNetCoreInstrumentation();
            options.AddHttpClientInstrumentation();
            options.AddHotChocolateInstrumentation();
            options.AddOtlpExporter();
        });
```

You should see:

- A successful GraphQL request emits a trace
- A rejected request emits a trace or error signal, depending on your exporter and sampling settings
- The root GraphQL span includes the operation type
- The operation name appears when the client sends one
- Trusted document requests include the document hash attribute

Name client operations before production. Operation names make traces useful without creating high-cardinality span names.

If you add custom diagnostic event listeners later, keep handlers fast. Diagnostic event handlers run synchronously as part of the GraphQL request. Offload slow work to run outside the request path.

For exporter details, span attributes, sampling, and field-level scopes, see [Instrumentation](/docs/hotchocolate/v16/server/instrumentation).

# Run a production-style smoke test

Smoke tests prove that the server starts and that the important controls work together. Run them after you change production settings and before you deploy.

Build the project:

```bash
dotnet build
```

Expected checkpoint:

```text
Build succeeded.
```

Start the server:

```bash
dotnet run
```

Use the port printed by ASP.NET Core. The tutorial examples use `http://localhost:5095/graphql`; replace it if your server prints another port.

| Step | Command or action | Expected result | Failure points to |
| --- | --- | --- | --- |
| Startup | Start the server | Startup completes after schema creation and warmup | Schema error, missing storage folder, invalid warmup operation, missing package |
| Expected operation | Send `ProductionSmokeTest` by document hash or query document, depending on your operation policy | Response contains `data.books` | Resolver, database, persisted operation, authorization, or cost issue |
| Authenticated access | Send the protected-field request from the security chapter with a local JWT | Response contains `data.totalBookCount` with no authorization error | Token, authentication middleware, authorization policy, or persisted operation issue |
| Access boundary | Send the protected-field request from the security chapter without credentials | Response contains the expected authorization error | Missing authentication or authorization registration |
| Request budget | Send an over-budget operation or lower limits in a local branch | Request is rejected before resolver execution | Cost options not registered, header test not reaching endpoint, limit too high for the test |
| Operation control | Send a new dynamic `query` while persisted-only mode is enabled | Request is rejected with the persisted operation error | `OnlyAllowPersistedDocuments` not enabled or bypassed |
| Telemetry | Inspect your exporter, collector, or console telemetry | Success and failure paths are visible | Missing package, missing registration, exporter issue, sampling drop |

Example expected operation request when dynamic documents are allowed:

```bash
curl -s http://localhost:5095/graphql \
  -H "Content-Type: application/json" \
  -H "GraphQL-Cost: report" \
  -d '{"query":"query ProductionSmokeTest { books(first: 2) { nodes { id title author { id name } } pageInfo { hasNextPage endCursor } } }"}'
```

Example request when trusted documents are enforced:

```bash
curl -s http://localhost:5095/graphql \
  -H "Content-Type: application/json" \
  -d '{"id":"<document-hash>"}'
```

Example authenticated protected-field request when dynamic documents are allowed:

```bash
curl -s http://localhost:5095/graphql \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <token>" \
  -d '{"query":"query ReadCatalogAndReports { books(first: 2) { nodes { id title author { name } } } totalBookCount }"}'
```

Use a local JWT from the security chapter:

```bash
dotnet user-jwts create --name tutorial-user
```

Copy the value after `Token:` into the `Authorization` header.

Expected authenticated response shape:

```json
{
  "data": {
    "books": {
      "nodes": [
        {
          "id": 1,
          "title": "The Left Hand of Darkness",
          "author": {
            "name": "Ursula K. Le Guin"
          }
        }
      ]
    },
    "totalBookCount": 3
  }
}
```

Your rows and count can differ. The checkpoint is that the response has no authorization error and `totalBookCount` returns an integer.

If trusted documents are enforced, publish the same `ReadCatalogAndReports` document to `persisted_operations/<document-hash>.graphql`, then send the matching hash with the same header:

```bash
curl -s http://localhost:5095/graphql \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <token>" \
  -d '{"id":"<document-hash>"}'
```

Example dynamic request that should fail when trusted documents are enforced:

```bash
curl -s http://localhost:5095/graphql \
  -H "Content-Type: application/json" \
  -d '{"query":"{ __typename }"}'
```

Expected rejected response shape:

```json
{
  "errors": [
    {
      "message": "Only persisted operations are allowed."
    }
  ]
}
```

Your response may include an error `extensions.code` value. The checkpoint is that the dynamic operation does not execute.

# Final readiness checklist for team review

Use this checklist as a handoff from tutorial project to real application planning.

## Completed in this chapter

- [ ] Request parser limits are configured.
- [ ] Validation limits are configured.
- [ ] Execution timeout is configured.
- [ ] Cost limits are enforced.
- [ ] Expected client operations have been measured with `GraphQL-Cost: report` or `validate`.
- [ ] The team has chosen dynamic operations, trusted documents, or APQ for the first production rollout.
- [ ] A persisted operation storage path or registry workflow is documented.
- [ ] `OnlyAllowPersistedDocuments` is enabled for production review when the stored operation workflow exists.
- [ ] A safe representative warmup task runs during startup.
- [ ] OpenTelemetry instrumentation emits request traces.
- [ ] The smoke test covers success, authorization failure, budget failure, operation policy failure, and telemetry.

## Carried from earlier chapters

- [ ] Data access uses bounded paging for collection fields.
- [ ] DataLoader removes N+1 relationship loading on the important nested paths.
- [ ] Mutations model expected domain errors.
- [ ] Subscriptions use a provider that matches the deployment topology.
- [ ] Tests cover the schema contract and at least one important operation.
- [ ] The client sends named operations and handles `data` and `errors`.
- [ ] Authentication and field authorization are reviewed.

## Still outside this tutorial

- [ ] Secrets are not stored in source control.
- [ ] A real identity provider and authorization policy set are configured.
- [ ] TLS, CORS, rate limiting, and hosting security headers are configured at the platform boundary.
- [ ] Database migrations, backups, retention, and restore tests exist.
- [ ] Operation publishing or the client registry is part of the release process.
- [ ] Health checks and readiness checks match schema startup and warmup behavior.
- [ ] Scale-out requirements are documented for subscriptions and storage.
- [ ] Alerts, dashboards, sampling, and log retention are configured.
- [ ] Rollback and incident response steps are written.

# Troubleshooting production checks

| Symptom | Likely cause | Fix | Verify |
| --- | --- | --- | --- |
| Cost report is missing | Header missing, endpoint mismatch, or cost options not active | Send `GraphQL-Cost: report` to `/graphql` and confirm `ModifyCostOptions` is in the active builder chain | Response includes cost data in `extensions` |
| Expected query is rejected by cost limits | Budget is lower than the measured operation cost | Measure representative operations and tune `MaxFieldCost` and `MaxTypeCost` with evidence | Expected operations pass, intentionally large operations fail |
| Persisted operation is not found | File name, document hash, folder path, or request `id` does not match | Compare the stored `.graphql` file name with the request `id`, hash provider, and running working directory | Sending the stored document hash succeeds |
| Nitro stops accepting ad hoc queries | Persisted-only mode blocks dynamic operation documents | Disable the switch for local development or add an authenticated developer bypass | Production mode blocks ad hoc operations, development workflow is documented |
| Warmup fails startup | Operation text, operation name, services, storage, or authorization path is wrong | Run the operation as a normal request, then mark it as warmup after it works | Startup completes and the smoke test passes |
| Warmup takes too long | Too many operations or execution work in warmup | Keep one bounded read operation and use `MarkAsWarmupRequest()` when execution is not required | Startup time stays within your readiness target |
| No Hot Chocolate spans appear | Missing package, missing `.AddInstrumentation()`, missing `AddHotChocolateInstrumentation()`, exporter issue, or sampling drop | Verify packages, registrations, exporter endpoint, and sampling policy | A known GraphQL request creates a trace |

# Next steps

If your local project diverges, compare your work with [Source code and checkpoints](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/00-source-code-and-checkpoints/). If you get stuck, see [Stuck?](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/stuck/).

Then choose the next production area to focus on:

- [Performance Tuning](/docs/hotchocolate/v16/guides/performance/) for caching, DataLoader, projections, response size, cost analysis, and instrumentation.
- [Request Limits](/docs/hotchocolate/v16/securing-your-api/request-limits/) for parser, validation, and execution safeguards.
- [Cost Analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis/) for weights, list sizes, reports, and tuning.
- [Persisted Operations](/docs/hotchocolate/v16/performance/trusted-documents/) for trusted documents and operation storage.
- [Automatic Persisted Operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations/) for APQ.
- [Warmup](/docs/hotchocolate/v16/server/warmup/) for startup behavior and custom warmup tasks.
- [Instrumentation](/docs/hotchocolate/v16/server/instrumentation/) for OpenTelemetry and diagnostic event listeners.

Your final checkpoint is not a claim that the tutorial project is fully deployed. Instead, it is a reviewed server configuration, a repeatable smoke test, and a written list of production decisions that remain.
