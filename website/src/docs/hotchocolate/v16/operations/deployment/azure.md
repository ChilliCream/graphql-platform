---
title: Deploy Hot Chocolate on Azure
---

You deploy Hot Chocolate to Azure as an ASP.NET Core app. Azure hosts the process; Hot Chocolate owns the GraphQL endpoint, HTTP and streaming transports, execution pipeline, operation document storage, and GraphQL-specific security choices.

This page covers standalone Hot Chocolate v16 on ASP.NET Core. Fusion gateway deployment is out of scope. The guidance assumes that TLS terminates at Azure or an Azure ingress and that traffic reaches Kestrel or an ASP.NET Core container over a trusted path.

Use this page as an operational checklist after your GraphQL server runs locally. It does not replace Microsoft Learn for creating App Service, Container Apps, AKS, Azure Cache for Redis, Azure Blob Storage, identity, networking, or Azure Monitor resources.

# Prerequisites

Before you change deployment settings, make sure you have:

- A .NET ASP.NET Core app using Hot Chocolate v16.
- An Azure host selected: App Service, Container Apps, or AKS.
- A public or private HTTPS endpoint and a known GraphQL path, usually `/graphql`.
- An authentication provider and a CORS policy if browser clients call the API.
- Azure Cache for Redis when subscriptions must work across replicas or when APQ documents must survive restarts and scale-out.
- An Azure Blob Storage container when trusted operation documents are deployed to Blob Storage.
- Application Insights or an Azure Monitor workspace when you export telemetry.
- A place for secrets and per-environment settings: app settings, Key Vault references, Container Apps secrets, Kubernetes secrets, or managed identity-backed configuration.

# Start with a production-safe GraphQL endpoint

Start with one canonical GraphQL endpoint. Then make every production behavior explicit so the same artifact can run in development, staging, and production with different configuration.

```csharp
// Program.cs
using HotChocolate.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("GraphQLClients", policy =>
    {
        policy
            .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [])
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

builder
    .AddGraphQL(maxAllowedRequestSize: 1 * 1000 * 1000)
    .AddAuthorization()
    .AddQueryType<Query>()
    .AllowIntrospection(builder.Environment.IsDevelopment())
    .ModifyServerOptions(options =>
    {
        options.Tool.Enable = builder.Environment.IsDevelopment();
        options.EnableSchemaRequests = builder.Environment.IsDevelopment();
        options.EnableGetRequests = true;
        options.AllowedGetOperations = AllowedGetOperations.Query;
        options.EnforceGetRequestsPreflightHeader = true;
        options.EnableMultipartRequests = false;
        options.EnforceMultipartRequestsPreflightHeader = true;
    });

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors("GraphQLClients");
app.UseAuthentication();
app.UseAuthorization();

// Add this only when WebSocket subscriptions are enabled.
// app.UseWebSockets();

app.MapGraphQL("/graphql");

app.Run();
```

`builder.AddGraphQL(...)` is the v16 ASP.NET Core hosting entry point. Keep `disableDefaultSecurity` at its default value of `false` unless you replace the default policy deliberately. The default policy adds cost analysis, disables introspection outside development, and adds the field-cycle validation rule. Replace `AddAuthentication()` with your ASP.NET Core authentication scheme, for example Microsoft Entra ID or JWT bearer.

Put `UseCors`, `UseAuthentication`, and `UseAuthorization` before `MapGraphQL`. Browser clients need CORS before authentication, and Hot Chocolate authorization needs the ASP.NET Core user before the GraphQL request is created. Avoid wildcard origins when credentials are allowed.

`MapGraphQL("/graphql")` handles HTTP POST, HTTP GET when enabled, schema download when enabled, Nitro when enabled, and WebSocket traffic when ASP.NET Core WebSocket middleware is registered. Disable Nitro in production with `Tool.Enable = false`. Disable schema SDL download in production with `EnableSchemaRequests = false` unless downloading `/graphql?sdl` is part of your production contract.

Keep GET enabled only for cacheable queries or persisted-operation flows. Keep `AllowedGetOperations = AllowedGetOperations.Query` for production. If browser clients use GET and you need CSRF protection, require the preflight header with `EnforceGetRequestsPreflightHeader = true` and configure clients to send it.

Keep multipart requests disabled unless your schema uses the `Upload` scalar. When you enable multipart uploads, keep `EnforceMultipartRequestsPreflightHeader = true` so clients must send `GraphQL-preflight`.

Verify the endpoint after deployment:

```bash
curl -sS https://api.example.com/graphql \
  -H 'Content-Type: application/json' \
  --data '{ "query": "{ __typename }" }'
```

Expected response:

```json
{ "data": { "__typename": "Query" } }
```

A browser request to `/graphql` in production should not show Nitro when the production environment setting disables it.

# Choose an Azure hosting model for GraphQL behavior

Choose the Azure host based on the GraphQL behaviors you need to operate, not only on the deployment format.

| Azure host      | Choose it when                                                                                                                              | Watch out for                                                                                                                                                                                                   | Hot Chocolate settings to revisit                                                                                            |
| --------------- | ------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------- |
| App Service     | You want a managed ASP.NET Core host for a GraphQL API.                                                                                     | Enable WebSockets in App Service settings when WebSocket subscriptions are used. Know the front-end timeout for normal long HTTP requests. ARR affinity can reduce reconnect churn, but it is not shared state. | `UseWebSockets`, Redis subscriptions, shared persisted-operation storage, Nitro, introspection, request size, health checks. |
| Container Apps  | You deploy containers, use revisions, and want KEDA-driven scaling.                                                                         | Keep `minReplicas` above zero when cold starts or active subscription traffic matter. Review ingress transport, sticky sessions if you still have replica-local state, and probes.                              | Redis subscriptions, shared persisted-operation storage, readiness probes, warmup tasks, streaming transport choice.         |
| AKS             | Your team already operates Kubernetes or needs ingress, HPA, pod disruption budgets, network policy, sidecars, or detailed rollout control. | Ingress must support WebSocket upgrades and SSE streaming. Pods can terminate while clients hold subscriptions. Configure probes and graceful termination.                                                      | Redis subscriptions, shared persisted-operation storage, `GraphQLSocketOptions`, health checks, graceful rollout checks.     |
| Azure Functions | You have a limited HTTP-triggered scenario and accept the platform trade-offs.                                                              | Cold starts, execution limits, and long-lived WebSocket subscription hosting make it a poor fit for this page.                                                                                                  | Prefer a long-running ASP.NET Core host for production GraphQL subscriptions.                                                |
| Static Web Apps | You host a frontend only.                                                                                                                   | It is not a Hot Chocolate server host.                                                                                                                                                                          | Host the GraphQL API separately.                                                                                             |

For all hosts, avoid solving slow operations by raising platform request timeouts. Use cost analysis, validation limits, pagination limits, streaming transports, and resolver optimization.

# Configure subscriptions for scale-out

`AddInMemorySubscriptions()` works for local development and single-instance deployments. In Azure, events may be published on one replica while a subscriber is connected to another. Use Azure Cache for Redis as the subscription provider when you scale out.

Install the Redis provider:

<PackageInstallation packageName="HotChocolate.Subscriptions.Redis" />

Register the Redis connection as a singleton and use it for subscriptions:

```csharp
// Program.cs
using HotChocolate.Subscriptions;
using StackExchange.Redis;

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("Redis");
    return ConnectionMultiplexer.Connect(connectionString!);
});

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddSubscriptionType<Subscription>()
    .AddRedisSubscriptions(
        options: new SubscriptionOptions
        {
            TopicPrefix = $"catalog-{builder.Environment.EnvironmentName}"
        });
```

`ITopicEventSender` and `ITopicEventReceiver` stay the same when you switch from in-memory subscriptions to Redis. Your resolvers publish through `ITopicEventSender`; the provider decides how messages move between replicas.

Use a `TopicPrefix` when multiple services or environments share a Redis cache. This prevents a staging deployment and a production deployment from listening on the same topic names.

Enable the transport your clients use:

```csharp
var app = builder.Build();

app.UseWebSockets();
app.MapGraphQL("/graphql");
```

WebSocket clients need `UseWebSockets()` and Azure host support for WebSocket upgrades. SSE subscriptions do not need `UseWebSockets()`, but they still hold long-running HTTP responses and require proxy timeout and buffering review.

Expected result: an event published on replica A reaches a subscriber connected to replica B. Azure SignalR Service is not a Hot Chocolate subscription backplane unless you have verified a current integration for your application.

# Pick WebSocket or SSE for Azure networks

Hot Chocolate exposes subscriptions through the standard GraphQL endpoint. The transport choice affects proxies, clients, and reconnect behavior.

WebSocket is full duplex and broadly supported by GraphQL subscription clients. Hot Chocolate supports `graphql-transport-ws` and `graphql-ws`; the client selects the protocol with `Sec-WebSocket-Protocol`. App Service requires the WebSockets setting for direct WebSocket traffic. Application Gateway and other proxies must preserve upgrade requests. After the upgrade, some WAF and header rewrite features no longer inspect or modify payloads.

SSE uses the GraphQL HTTP endpoint with content negotiation:

```http
POST /graphql HTTP/1.1
Host: api.example.com
Accept: text/event-stream
Content-Type: application/json

{ "query": "subscription { onBookAdded { title } }" }
```

SSE can pass through some networks more predictably because it is HTTP server-to-client streaming. It is not a bidirectional socket. Clients still need reconnect and resubscribe logic for deployments, scale events, and idle connection closure.

Tune WebSocket keep-alives below the idle timeout of the proxies in your path. The v16 default keep-alive interval is 5 seconds, and the default connection initialization timeout is 10 seconds.

```csharp
builder
    .AddGraphQL()
    .ModifyServerOptions(options =>
    {
        options.Sockets.KeepAliveInterval = TimeSpan.FromSeconds(10);
        options.Sockets.ConnectionInitializationTimeout = TimeSpan.FromSeconds(10);
    });
```

Verify WebSocket with a GraphQL WebSocket client and check that the negotiated subprotocol matches your client library. Verify SSE by sending `Accept: text/event-stream` and checking that the response is a stream of GraphQL SSE events, for example `event: next` followed by `event: complete` for a finite streaming operation.

# Store persisted and trusted operations in Azure

Use trusted documents when first-party clients have a known set of operations before deployment. Use APQ when clients register operation documents at runtime by first sending a hash and then sending the document on a cache miss.

Do not use in-memory operation storage for multi-replica Azure deployments. In-memory storage disappears on restart and is not shared across replicas.

## Use Azure Blob Storage for deployed trusted documents

Install the Blob Storage provider:

<PackageInstallation packageName="HotChocolate.PersistedOperations.AzureBlobStorage" />

The container must already exist. Each blob name is the operation hash, and the blob content is the GraphQL document.

```csharp
// Program.cs
using Azure.Identity;
using Azure.Storage.Blobs;

builder.Services.AddSingleton(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var containerUri = new Uri(configuration["Storage:PersistedOperationsContainerUri"]!);
    return new BlobContainerClient(containerUri, new DefaultAzureCredential());
});

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .UsePersistedOperationPipeline()
    .AddAzureBlobStorageOperationDocumentStorage(sp =>
        sp.GetRequiredService<BlobContainerClient>());
```

Use managed identity for the `BlobContainerClient` when possible. Put exact identity assignment and role configuration in your Azure infrastructure, not in `Program.cs`. Use Blob lifecycle management if old operation documents need retention rules.

## Use Redis for APQ or low-latency shared storage

Install the Redis persisted operations provider:

<PackageInstallation packageName="HotChocolate.PersistedOperations.Redis" />

```csharp
// Program.cs
using StackExchange.Redis;

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    return ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis")!);
});

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .UseAutomaticPersistedOperationPipeline()
    .AddRedisOperationDocumentStorage(queryExpiration: TimeSpan.FromHours(12));
```

For trusted documents, use `.UsePersistedOperationPipeline()`. For APQ, use `.UseAutomaticPersistedOperationPipeline()`. Redis can serve either pattern. The `queryExpiration` value is useful for runtime APQ documents. Avoid an expiration for trusted documents unless your deployment process republishes them before clients need them.

## Lock production to known documents

For locked-down production APIs, allow only persisted documents:

```csharp
builder
    .AddGraphQL()
    .UsePersistedOperationPipeline()
    .ModifyRequestOptions(options =>
    {
        options.PersistedOperations.OnlyAllowPersistedDocuments = true;
    });
```

If operators or internal tooling need dynamic operations, gate that exception with an HTTP request interceptor and an authorization policy. Do not leave dynamic operations open for every production caller.

Clients can execute a persisted document by sending the hash as `id`:

```json
{
  "id": "0c95d31ca29272475bf837f944f4e513",
  "variables": { "first": 10 }
}
```

Expected result: the request resolves after an app restart and from every replica because the operation document storage is shared.

If you use deterministic persisted-operation routes, map them separately:

```csharp
app.MapGraphQL("/graphql");
app.MapGraphQLPersistedOperations("/graphql/persisted", requireOperationName: true);
```

A route request uses the operation ID in the URL:

```http
GET /graphql/persisted/0c95d31ca29272475bf837f944f4e513/GetProducts?variables={"first":10}
```

Align the hash provider and encoding with clients. Hot Chocolate defaults to MD5 with base64 for APQ and supports MD5, SHA-1, and SHA-256 with configurable encoding.

# Connect Application Insights through OpenTelemetry

Hot Chocolate emits OpenTelemetry spans when you add the diagnostics package and enable instrumentation. Application Insights receives those spans through Azure Monitor OpenTelemetry configuration.

Install the Hot Chocolate diagnostics package and the OpenTelemetry packages you use for Azure Monitor export. The Azure Monitor sample uses `Azure.Monitor.OpenTelemetry.AspNetCore` in addition to the OpenTelemetry ASP.NET Core, HTTP client, and Hot Chocolate instrumentation packages.

<PackageInstallation packageName="HotChocolate.Diagnostics" />

```csharp
// Program.cs
using Azure.Monitor.OpenTelemetry.AspNetCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddInstrumentation();

builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resource =>
        resource.AddService(serviceName: "catalog-graphql"))
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation();
        tracing.AddHttpClientInstrumentation();
        tracing.AddHotChocolateInstrumentation();
    })
    // Requires Azure.Monitor.OpenTelemetry.AspNetCore.
    // Configure APPLICATIONINSIGHTS_CONNECTION_STRING or managed identity as documented by Microsoft Learn.
    .UseAzureMonitor();
```

`AddInstrumentation()` enables Hot Chocolate instrumentation. `AddHotChocolateInstrumentation()` connects those events to OpenTelemetry. `UseAzureMonitor()` exports telemetry to Azure Monitor and Application Insights when the Azure Monitor OpenTelemetry package and configuration are present.

Prefer low-cardinality root span names. Filter by attributes such as `graphql.operation.type`, `graphql.operation.name`, `graphql.document.hash`, and errors. Enable field-level scopes, resolver details, document text, variables, or `ActivityScopes.All` only after you review overhead and sensitive-data exposure.

If Application Insights shows ASP.NET Core spans but no GraphQL spans, check that both `.AddInstrumentation()` and `.AddHotChocolateInstrumentation()` are registered. If no spans reach Azure, check the Azure Monitor exporter, connection string or identity, sampling, and network egress.

Expected result: Application Insights transaction or trace views show GraphQL root spans correlated with ASP.NET Core requests and downstream HTTP calls.

# Expose health and readiness endpoints

Hot Chocolate v16 constructs schemas eagerly by default. Schema construction failures fail startup, and the server is not ready until schema creation completes. Use ASP.NET Core health checks for liveness and readiness. Do not add a separate Hot Chocolate-specific health API.

```csharp
// Program.cs
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

builder.Services
    .AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddCheck<GraphQLDependencyHealthCheck>("graphql-dependencies", tags: ["ready"]);

var app = builder.Build();

app.MapHealthChecks("/livez", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

app.MapHealthChecks("/readyz", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapGraphQL("/graphql");
```

Put dependencies required for GraphQL execution in readiness: databases, Redis, storage, downstream services, or identity metadata when they are on the critical path. Keep liveness lightweight so Azure does not restart a healthy process because a dependency is temporarily unavailable. Do not expose detailed dependency errors publicly.

Avoid `LazyInitialization = true` in production unless you accept first-request schema initialization latency and readiness semantics that do not include schema construction.

Use warmup tasks to pre-populate Hot Chocolate caches with important operations before the server receives traffic:

```csharp
using HotChocolate.Execution;

builder
    .AddGraphQL()
    .AddWarmupTask(async (executor, cancellationToken) =>
    {
        var request = OperationRequestBuilder.New()
            .SetDocument("query Warmup { __typename }")
            .SetOperationName("Warmup")
            .MarkAsWarmupRequest()
            .Build();

        await executor.ExecuteAsync(request, cancellationToken);
    });
```

A warmup request skips persisted-operation enforcement and does not execute resolver side effects. Use it for parser, validation, and operation cache warmup. Expected result: `/readyz` returns success only after startup and critical dependencies are ready.

# Handle secrets and environment configuration

Keep credentials out of source. Store Redis, storage, auth, CORS, Nitro, persisted-operation, and telemetry settings in Azure app settings, Key Vault references, Container Apps secrets, Kubernetes secrets, or managed identity-backed configuration.

| Configuration key                                                          | Purpose                                                            |
| -------------------------------------------------------------------------- | ------------------------------------------------------------------ |
| `GraphQL:EndpointPath`                                                     | Canonical route, commonly `/graphql`.                              |
| `GraphQL:EnableNitro`                                                      | Enables Nitro in development or controlled internal environments.  |
| `GraphQL:EnableSchemaRequests`                                             | Controls SDL download through the GraphQL endpoint.                |
| `GraphQL:EnableGetRequests`                                                | Enables cacheable GET or persisted-operation flows.                |
| `GraphQL:OnlyAllowPersistedDocuments`                                      | Locks execution to known persisted documents.                      |
| `GraphQL:MaxAllowedRequestSize`                                            | Limits GraphQL request body size inside Hot Chocolate.             |
| `GraphQL:ExecutionTimeoutSeconds`                                          | Bounds execution time for normal requests.                         |
| `GraphQL:MaxFieldCost` and `GraphQL:MaxTypeCost`                           | Tune cost-analysis limits per environment.                         |
| `ConnectionStrings:Redis`                                                  | Azure Cache for Redis connection string or configuration endpoint. |
| `Storage:PersistedOperationsContainerUri`                                  | Blob container URI for trusted operation documents.                |
| `Cors:Origins`                                                             | Allowed browser origins.                                           |
| `AzureMonitor:ConnectionString` or `APPLICATIONINSIGHTS_CONNECTION_STRING` | Azure Monitor export configuration.                                |

Use managed identity for Azure Blob Storage when possible. Never hardcode Redis connection strings, JWT signing keys, storage keys, or Application Insights connection strings in `Program.cs`.

Before swapping App Service slots, shifting Container Apps traffic, or rolling AKS pods, verify that the target environment has compatible settings for Nitro, introspection, schema SDL download, GET requests, multipart upload, request size, cost limits, persisted-only enforcement, Redis, Blob Storage, auth, CORS, and telemetry.

# Tune request size, uploads, and execution limits

Several layers can reject a request before Hot Chocolate sees it: Azure ingress, Application Gateway, IIS/App Service, Kestrel, ASP.NET Core form parsing, and Hot Chocolate request parsing. Identify the layer before changing limits.

Limit GraphQL request bodies in Hot Chocolate:

```csharp
builder.AddGraphQL(maxAllowedRequestSize: 1 * 1000 * 1000);
```

Configure multipart limits when you accept file uploads:

```csharp
using Microsoft.AspNetCore.Http.Features;

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 256L * 1024L * 1024L;
});

builder
    .AddGraphQL()
    .AddType<UploadType>()
    .ModifyServerOptions(options =>
    {
        options.EnableMultipartRequests = true;
        options.EnforceMultipartRequestsPreflightHeader = true;
    });
```

Clients must include `GraphQL-preflight` for multipart uploads:

```bash
curl https://api.example.com/graphql \
  -H 'GraphQL-preflight: 1' \
  -F operations='{ "query": "mutation ($file: Upload!) { uploadFile(file: $file) }", "variables": { "file": null } }' \
  -F map='{ "0": ["variables.file"] }' \
  -F 0=@file.txt
```

For large files, prefer presigned Azure Blob Storage upload URLs. Use GraphQL to authorize the upload and return metadata or a URL, then send the file directly to storage.

Use execution and cost limits for expensive operations:

```csharp
builder
    .AddGraphQL()
    .ModifyRequestOptions(options =>
    {
        options.ExecutionTimeout = TimeSpan.FromSeconds(10);
    })
    .ModifyCostOptions(options =>
    {
        options.MaxFieldCost = 1_000;
        options.MaxTypeCost = 1_000;
    });
```

Expected results:

- A too-large GraphQL JSON request fails at the Hot Chocolate request limit with a clear GraphQL or HTTP error.
- A too-large multipart request may fail earlier with `413 Payload Too Large` from ASP.NET Core or Azure.
- An expensive operation fails during validation or cost analysis before resolvers consume resources.

# Scale and deploy safely

Use this checklist before you add replicas or shift production traffic:

- Use Redis subscriptions when more than one replica can publish or receive subscription events.
- Use shared operation document storage for APQ or trusted documents. Choose Redis or Azure Blob Storage based on the operation workflow.
- Set minimum replicas or always-on settings when latency, cold starts, or subscriptions matter.
- Publish trusted operation artifacts before, or with, the server version that requires them.
- Keep schema changes compatible with deployed clients. Use a schema and client release workflow when multiple client versions are active.
- Run warmup tasks or smoke tests for important operations before traffic shifts.
- Expect WebSocket and SSE clients to disconnect during slot swaps, revision changes, pod restarts, and node drains. Clients must reconnect and resubscribe.
- For AKS, configure graceful termination and drain behavior long enough for normal in-flight requests. Do not expect subscriptions to survive pod termination.
- For App Service slots and Container Apps revisions, verify production settings on the target before traffic moves.

A deployment smoke test can reuse a small operation:

```bash
curl -f -sS https://api.example.com/readyz
curl -f -sS https://api.example.com/graphql \
  -H 'Content-Type: application/json' \
  --data '{ "query": "query DeploymentSmoke { __typename }", "operationName": "DeploymentSmoke" }'
```

Expected response:

```json
{ "data": { "__typename": "Query" } }
```

# Verify the deployment

Run the checks that match the features you enabled.

## Check readiness

```bash
curl -i https://api.example.com/readyz
```

Expected result: `200 OK` only after the app and critical GraphQL dependencies are ready.

## Check POST execution

```bash
curl -sS https://api.example.com/graphql \
  -H 'Content-Type: application/json' \
  --data '{ "query": "{ __typename }" }'
```

Expected response:

```json
{ "data": { "__typename": "Query" } }
```

## Check GET policy

If GET is enabled and preflight is enforced:

```bash
curl -g -sS 'https://api.example.com/graphql?query={__typename}' \
  -H 'GraphQL-preflight: 1'
```

Expected response:

```json
{ "data": { "__typename": "Query" } }
```

Without the required preflight header, expect a client error. If GET is disabled, expect the endpoint to reject the request.

## Check Nitro and schema policy

Request `/graphql` from a browser or send an HTML accept header:

```bash
curl -i https://api.example.com/graphql -H 'Accept: text/html'
```

Expected result in production: Nitro is not served when `Tool.Enable` is false.

Check SDL download only if you intend to allow it:

```bash
curl -i 'https://api.example.com/graphql?sdl'
```

Expected result: success only when `EnableSchemaRequests` is true.

## Check persisted operations

```bash
curl -sS https://api.example.com/graphql \
  -H 'Content-Type: application/json' \
  --data '{ "id": "0c95d31ca29272475bf837f944f4e513", "variables": { "first": 10 } }'
```

Expected result: the registered operation executes. If you mapped persisted-operation routes, test the route too:

```bash
curl -g -sS 'https://api.example.com/graphql/persisted/0c95d31ca29272475bf837f944f4e513/GetProducts?variables={"first":10}'
```

## Check streaming transports

For WebSocket subscriptions, use a GraphQL WebSocket client and confirm the negotiated protocol is `graphql-transport-ws` or `graphql-ws`.

For SSE, send `Accept: text/event-stream` with an operation your schema supports:

```bash
curl -N https://api.example.com/graphql \
  -H 'Accept: text/event-stream' \
  -H 'Content-Type: application/json' \
  --data '{ "query": "subscription { onBookAdded { title } }" }'
```

Expected shape for emitted results:

```text
event: next
data: {"data":{"onBookAdded":{"title":"GraphQL in Action"}}}
```

When Redis subscriptions are configured, publish an event on one replica and confirm a subscriber connected to another replica receives it.

## Check telemetry

Send a successful request and a request that produces a GraphQL error. Application Insights should show correlated ASP.NET Core and GraphQL spans, including operation type, operation name when provided, document hash when available, and errors.

# Troubleshoot Azure deployment issues

| Symptom                                                           | Likely cause                                                                                                                                               | Check                                                                                          | Fix                                                                                          |
| ----------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------- |
| `404` on `/graphql`                                               | Route mismatch, base path or ingress rewrite, or GraphQL was not mapped.                                                                                   | Check `MapGraphQL` path and Azure route or ingress rules.                                      | Align the external route with `MapGraphQL("/graphql")` or update ingress rewrite rules.      |
| Nitro appears in production                                       | `Tool.Enable` is not environment-gated, or the slot/revision has the wrong setting.                                                                        | Request `/graphql` with `Accept: text/html` and inspect environment settings.                  | Disable Nitro through `ModifyServerOptions` or configuration for production.                 |
| Browser GET returns `400`                                         | GET preflight is enforced and the header is missing, or GET is disabled.                                                                                   | Check `EnableGetRequests` and `EnforceGetRequestsPreflightHeader`.                             | Add the `GraphQL-preflight` header or use POST.                                              |
| Multipart upload returns `400`                                    | Missing `GraphQL-preflight`, multipart disabled, or malformed `operations` and `map` fields.                                                               | Compare the request with the multipart spec shape.                                             | Enable multipart only when needed and send the required preflight header.                    |
| Upload returns `413`                                              | Hot Chocolate, ASP.NET Core form limits, Kestrel, IIS/App Service, Application Gateway, or ingress rejected the body.                                      | Identify which layer emitted the response.                                                     | Tune the correct layer or use presigned Blob Storage uploads for large files.                |
| `401`, `403`, or missing user                                     | Middleware order, CORS credentials, JWT issuer or audience, or WebSocket auth handshake.                                                                   | Check `UseCors`, `UseAuthentication`, `UseAuthorization`, JWT settings, and interceptors.      | Put middleware before `MapGraphQL`, fix CORS origins, and handle socket authentication.      |
| WebSocket handshake fails with `400`, `502`, or close code `1006` | Host WebSockets disabled, proxy does not preserve upgrade, subprotocol mismatch, idle timeout, TLS or proxy issue, or keep-alive too slow.                 | Check App Service WebSockets, ingress logs, `Sec-WebSocket-Protocol`, and keep-alive settings. | Enable WebSockets, preserve upgrade headers, use a supported protocol, and tune keep-alives. |
| SSE starts then stops                                             | Proxy buffering or idle timeout, missing client reconnect, or ingress cuts long responses.                                                                 | Check proxy and ingress behavior for streaming responses.                                      | Disable buffering where applicable, tune timeouts, and add client reconnect logic.           |
| Subscriptions work locally but not after scale-out                | In-memory subscriptions.                                                                                                                                   | Check whether `AddInMemorySubscriptions()` is used in production.                              | Use `HotChocolate.Subscriptions.Redis` and a topic prefix.                                   |
| Persisted operation not found after restart or on another replica | In-memory operation storage.                                                                                                                               | Restart the app or send the request to another replica.                                        | Use Redis or Azure Blob Storage operation document storage and verify hash encoding.         |
| Trusted operation works in staging but not production             | Missing container, wrong container or prefix, operation artifact not deployed, or identity lacks storage permission.                                       | Check Blob container, blob names, deployment artifacts, and identity role assignments.         | Create the container before startup, deploy operation blobs, and grant storage access.       |
| Slow first request or failed startup                              | Schema initialization or warmup issue, dependency unavailable, or lazy initialization enabled.                                                             | Check startup logs and `/readyz`.                                                              | Keep eager initialization, fix dependencies, and use warmup tasks for important operations.  |
| No GraphQL spans in Application Insights                          | Missing `.AddInstrumentation()`, missing `.AddHotChocolateInstrumentation()`, exporter not configured, sampling, or connection string or identity problem. | Compare local OpenTelemetry output with Azure export.                                          | Register both Hot Chocolate instrumentation calls and configure Azure Monitor export.        |
| Introspection unexpectedly allowed or denied                      | Default security, explicit `.AllowIntrospection(...)`, environment name, or interceptor allowlist.                                                         | Send an introspection query and inspect environment/configuration.                             | Make the introspection policy explicit and test the production slot or revision.             |

# Next steps

Hot Chocolate v16 docs:

- [Endpoints](/docs/hotchocolate/v16/server/endpoints)
- [HTTP transport](/docs/hotchocolate/v16/server/http-transport)
- [Warmup](/docs/hotchocolate/v16/server/warmup)
- [Instrumentation](/docs/hotchocolate/v16/server/instrumentation)
- [Interceptors](/docs/hotchocolate/v16/server/interceptors)
- [Subscriptions](/docs/hotchocolate/v16/building-a-schema/subscriptions)
- [Trusted documents](/docs/hotchocolate/v16/performance/trusted-documents)
- [Automatic persisted operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations)
- [Authentication](/docs/hotchocolate/v16/securing-your-api/authentication)
- [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization)
- [Introspection](/docs/hotchocolate/v16/securing-your-api/introspection)
- [Request limits](/docs/hotchocolate/v16/securing-your-api/request-limits)
- [Cost analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis)
- [Files](/docs/hotchocolate/v16/server/files)
- [Cache control](/docs/hotchocolate/v16/server/cache-control)

Use Microsoft Learn for Azure platform details: App Service WebSockets, ARR affinity, request timeouts, Container Apps ingress, scaling, probes, sticky sessions, AKS ingress, probes, graceful shutdown, Application Gateway WebSocket behavior, Azure Monitor OpenTelemetry, Azure Cache for Redis, Azure Blob Storage managed identity, and ASP.NET Core request and multipart limits.
