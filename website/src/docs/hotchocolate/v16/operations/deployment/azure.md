---
title: Deploy Hot Chocolate on Azure
---

You deploy Hot Chocolate to Azure as an ASP.NET Core application. Azure provides the hosting environment, while Hot Chocolate manages the GraphQL endpoint, HTTP and streaming transports, execution pipeline, operation document storage, and GraphQL-specific security features.

This guide focuses on deploying standalone Hot Chocolate v16 on ASP.NET Core. It does not cover Fusion gateway deployment. The instructions assume TLS terminates at Azure or an Azure ingress, and that requests reach Kestrel or your ASP.NET Core container over a trusted network path.

Use this page as a deployment and operations checklist after your GraphQL server works locally. For creating Azure resources like App Service, Container Apps, AKS, Redis, Blob Storage, identity, networking, or monitoring, refer to Microsoft Learn.

# Prerequisites

Before configuring deployment, ensure you have:

- A .NET ASP.NET Core app using Hot Chocolate v16
- Chosen an Azure host: App Service, Container Apps, or AKS
- A public or private HTTPS endpoint and a known GraphQL path (usually `/graphql`)
- An authentication provider and a CORS policy if browser clients access the API
- Azure Cache for Redis if you need subscriptions across replicas or APQ documents to persist through restarts and scale-out
- An Azure Blob Storage container if you deploy trusted operation documents to Blob Storage
- Application Insights or an Azure Monitor workspace if you export telemetry
- A secure location for secrets and environment settings: app settings, Key Vault references, Container Apps secrets, Kubernetes secrets, or managed identity-backed configuration

# Start with a production-safe GraphQL endpoint

Begin with a single, canonical GraphQL endpoint. Make all production behaviors explicit so you can run the same deployment artifact in development, staging, and production by changing only configuration.

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
    .DisableIntrospection(disable: !builder.Environment.IsDevelopment())
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

// Enable this only if you use WebSocket subscriptions.
// app.UseWebSockets();

app.MapGraphQL("/graphql");

app.Run();
```

The `builder.AddGraphQL(...)` call is the entry point for ASP.NET Core hosting in v16. Leave `disableDefaultSecurity` as `false` unless you intentionally replace the default policy. The default policy enables cost analysis, disables introspection outside development, and adds field-cycle validation. Swap in your authentication scheme (such as Microsoft Entra ID or JWT bearer) in place of `AddAuthentication()` as needed.

Always call `UseCors`, `UseAuthentication`, and `UseAuthorization` before `MapGraphQL`. CORS must run before authentication for browser clients, and Hot Chocolate authorization needs the ASP.NET Core user before the GraphQL request is created. Do not use wildcard origins when credentials are allowed.

`MapGraphQL("/graphql")` handles HTTP POST, HTTP GET (if enabled), schema download (if enabled), Nitro (if enabled), and WebSocket traffic (if WebSocket middleware is registered). In production, disable Nitro with `Tool.Enable = false`. Disable schema SDL download with `EnableSchemaRequests = false` unless you require `/graphql?sdl` in production.

Enable GET only for cacheable queries or persisted-operation flows. For production, set `AllowedGetOperations = AllowedGetOperations.Query`. If browser clients use GET and you need CSRF protection, require the preflight header with `EnforceGetRequestsPreflightHeader = true` and configure clients to send it.

Keep multipart requests disabled unless your schema uses the `Upload` scalar. If you enable multipart uploads, set `EnforceMultipartRequestsPreflightHeader = true` so clients must send `GraphQL-preflight`.

After deployment, verify the endpoint:

```bash
curl -sS https://api.example.com/graphql \
  -H 'Content-Type: application/json' \
  --data '{ "query": "{ __typename }" }'
```

Expected response:

```json
{ "data": { "__typename": "Query" } }
```

A browser request to `/graphql` in production should not show Nitro if you have disabled it for the production environment.

# Choose an Azure hosting model for GraphQL

Select your Azure host based on the GraphQL behaviors you need, not only the deployment format.

| Azure host      | Use when                                                                                           | Considerations                                                                                                                                                         | Hot Chocolate settings to review                                                                                            |
| --------------- | -------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------- |
| App Service     | You want a managed ASP.NET Core host for your GraphQL API.                                         | Enable WebSockets in App Service settings for subscriptions. Know the front-end timeout for long HTTP requests. ARR affinity helps reconnects but is not shared state. | `UseWebSockets`, Redis subscriptions, shared persisted-operation storage, Nitro, introspection, request size, health checks |
| Container Apps  | You deploy containers, use revisions, and want KEDA-driven scaling.                                | Keep `minReplicas` above zero if cold starts or active subscriptions matter. Review ingress, sticky sessions, and probes.                                              | Redis subscriptions, shared persisted-operation storage, readiness probes, warmup tasks, streaming transport                |
| AKS             | You already operate Kubernetes or need advanced ingress, HPA, pod disruption budgets, or sidecars. | Ingress must support WebSocket upgrades and SSE. Pods may terminate while clients hold subscriptions. Configure probes and graceful termination.                       | Redis subscriptions, shared persisted-operation storage, `GraphQLSocketOptions`, health checks, graceful rollout            |
| Azure Functions | You have a limited HTTP-triggered scenario and accept platform trade-offs.                         | Cold starts, execution limits, and long-lived WebSocket subscriptions make it a poor fit for production GraphQL subscriptions.                                         | Prefer a long-running ASP.NET Core host for production subscriptions                                                        |
| Static Web Apps | You host a frontend only.                                                                          | Not a Hot Chocolate server host.                                                                                                                                       | Host the GraphQL API separately                                                                                             |

For all hosts, do not solve slow operations by increasing platform request timeouts. Instead, use cost analysis, validation and pagination limits, streaming transports, and resolver optimization.

# Configure subscriptions for scale-out

`AddInMemorySubscriptions()` is suitable for local development and single-instance deployments. In Azure, when you scale out, events may be published on one replica while a subscriber is connected to another. To support this, use Azure Cache for Redis as the subscription provider.

First, install the Redis provider:

<PackageInstallation packageName="HotChocolate.Subscriptions.Redis" />

Register the Redis connection as a singleton and configure subscriptions:

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

You do not need to change your resolvers when switching from in-memory to Redis subscriptions. Continue to use `ITopicEventSender` and `ITopicEventReceiver`; the provider handles message delivery between replicas.

Set a `TopicPrefix` if multiple services or environments share a Redis cache. This prevents staging and production deployments from listening on the same topics.

Enable the transport your clients use:

```csharp
var app = builder.Build();

app.UseWebSockets();
app.MapGraphQL("/graphql");
```

WebSocket clients require both `UseWebSockets()` and Azure host support for WebSocket upgrades. SSE subscriptions do not require `UseWebSockets()`, but they do keep long-running HTTP responses open, so review proxy timeouts and buffering.

After setup, an event published on one replica should reach subscribers connected to any other replica. Azure SignalR Service is not a Hot Chocolate subscription backplane unless you have verified a working integration for your application.

# Choose WebSocket or SSE for Azure networks

Hot Chocolate exposes subscriptions through the standard GraphQL endpoint. Your choice of transport, WebSocket or SSE, affects proxy compatibility, client support, and reconnect behavior.

WebSocket is full duplex and widely supported by GraphQL subscription clients. Hot Chocolate supports both `graphql-transport-ws` and `graphql-ws` protocols; the client selects the protocol using the `Sec-WebSocket-Protocol` header. For App Service, enable WebSockets in the platform settings. Application Gateway and other proxies must preserve upgrade requests. After the upgrade, some WAF and header rewrite features may no longer inspect or modify payloads.

SSE (Server-Sent Events) uses the GraphQL HTTP endpoint with content negotiation:

```http
POST /graphql HTTP/1.1
Host: api.example.com
Accept: text/event-stream
Content-Type: application/json

{ "query": "subscription { onBookAdded { title } }" }
```

SSE can traverse some networks more reliably because it is HTTP server-to-client streaming, not a bidirectional socket. However, clients still need logic to reconnect and resubscribe after deployments, scaling events, or idle connection closures.

Set your WebSocket keep-alive interval below the idle timeout of any proxies in your path. In v16, the default keep-alive interval is 5 seconds, and the default connection initialization timeout is 10 seconds. You can adjust these:

```csharp
builder
    .AddGraphQL()
    .ModifyServerOptions(options =>
    {
        options.Sockets.KeepAliveInterval = TimeSpan.FromSeconds(10);
        options.Sockets.ConnectionInitializationTimeout = TimeSpan.FromSeconds(10);
    });
```

To verify WebSocket, use a GraphQL WebSocket client and confirm the negotiated subprotocol matches your client library. To verify SSE, send `Accept: text/event-stream` and check that the response is a stream of GraphQL SSE events, such as `event: next` followed by `event: complete` for a finite stream.

# Store persisted and trusted operations in Azure

Use trusted documents when your first-party clients have a known set of operations before deployment. Use Automatic Persisted Queries (APQ) when clients register operation documents at runtime by first sending a hash, then sending the document if the hash is not found.

Do not use in-memory operation storage for multi-replica Azure deployments. In-memory storage is lost on restart and is not shared across replicas.

## Use Azure Blob Storage for trusted documents

Install the Blob Storage provider:

<PackageInstallation packageName="HotChocolate.PersistedOperations.AzureBlobStorage" />

The Blob container must exist before deployment. Each blob name is the operation hash, and the blob content is the GraphQL document.

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

Use managed identity for the `BlobContainerClient` when possible. Assign identity and roles in your Azure infrastructure, not in code. Use Blob lifecycle management if you need retention rules for old operation documents.

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

For trusted documents, use `.UsePersistedOperationPipeline()`. For APQ, use `.UseAutomaticPersistedOperationPipeline()`. Redis supports both patterns. Set `queryExpiration` for runtime APQ documents. For trusted documents, avoid expiration unless your deployment process republishes them before clients need them.

## Lock production to known documents

To restrict production APIs to only persisted documents:

```csharp
builder
    .AddGraphQL()
    .UsePersistedOperationPipeline()
    .ModifyRequestOptions(options =>
    {
        options.PersistedOperations.OnlyAllowPersistedDocuments = true;
    });
```

If operators or internal tools need dynamic operations, allow exceptions through an HTTP request interceptor and an authorization policy. Do not leave dynamic operations open for all production callers.

Clients execute a persisted document by sending the hash as `id`:

```json
{
  "id": "0c95d31ca29272475bf837f944f4e513",
  "variables": { "first": 10 }
}
```

After setup, requests resolve after app restarts and from any replica because operation document storage is shared.

If you use deterministic persisted-operation routes, map them separately:

```csharp
app.MapGraphQL("/graphql");
app.MapGraphQLPersistedOperations("/graphql/persisted", requireOperationName: true);
```

A route request uses the operation ID in the URL:

```http
GET /graphql/persisted/0c95d31ca29272475bf837f944f4e513/GetProducts?variables={"first":10}
```

Align the hash provider and encoding with your clients. Hot Chocolate defaults to MD5 with base64 for APQ and supports MD5, SHA-1, and SHA-256 with configurable encoding.

# Connect Application Insights through OpenTelemetry

Hot Chocolate emits OpenTelemetry spans when you add the diagnostics package and enable instrumentation. Application Insights receives these spans through Azure Monitor's OpenTelemetry integration.

Install the Hot Chocolate diagnostics package and the OpenTelemetry packages you need for Azure Monitor export. The following example uses `Azure.Monitor.OpenTelemetry.AspNetCore` along with the OpenTelemetry ASP.NET Core, HTTP client, and Hot Chocolate instrumentation packages.

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

`AddInstrumentation()` enables Hot Chocolate's OpenTelemetry support. `AddHotChocolateInstrumentation()` connects those events to OpenTelemetry. `UseAzureMonitor()` exports telemetry to Azure Monitor and Application Insights when the Azure Monitor OpenTelemetry package and configuration are present.

Prefer low-cardinality root span names. Filter by attributes such as `graphql.operation.type`, `graphql.operation.name`, `graphql.document.hash`, and errors. Enable field-level scopes, resolver details, document text, variables, or `ActivityScopes.All` only after you review the overhead and sensitive-data exposure.

If Application Insights shows ASP.NET Core spans but no GraphQL spans, check that both `.AddInstrumentation()` and `.AddHotChocolateInstrumentation()` are registered. If no spans reach Azure, check the Azure Monitor exporter, connection string or identity, sampling, and network egress.

After setup, Application Insights transaction or trace views should show GraphQL root spans correlated with ASP.NET Core requests and downstream HTTP calls.

# Expose health and readiness endpoints

Hot Chocolate v16 builds schemas eagerly by default. If schema construction fails, startup fails and the server is not ready until schema creation completes. Use ASP.NET Core health checks for liveness and readiness. Do not add a separate Hot Chocolate-specific health API.

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

Include all dependencies required for GraphQL execution in the readiness check: databases, Redis, storage, downstream services, or identity metadata if they are on the critical path. Keep liveness checks lightweight so Azure does not restart a healthy process due to a temporary dependency outage. Do not expose detailed dependency errors publicly.

Avoid `LazyInitialization = true` in production unless you accept schema initialization latency on the first request and readiness checks that do not include schema construction.

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

A warmup request skips persisted-operation enforcement and does not execute resolver side effects. Use it to warm up the parser, validation, and operation caches. After setup, `/readyz` returns success only after startup and all critical dependencies are ready.

# Handle secrets and environment configuration

Never store credentials in source code. Store Redis, storage, authentication, CORS, Nitro, persisted-operation, and telemetry settings in Azure app settings, Key Vault references, Container Apps secrets, Kubernetes secrets, or managed identity-backed configuration.

| Configuration key                                                          | Purpose                                                           |
| -------------------------------------------------------------------------- | ----------------------------------------------------------------- |
| `GraphQL:EndpointPath`                                                     | Canonical route, usually `/graphql`.                              |
| `GraphQL:EnableNitro`                                                      | Enables Nitro in development or controlled internal environments. |
| `GraphQL:EnableSchemaRequests`                                             | Controls SDL download through the GraphQL endpoint.               |
| `GraphQL:EnableGetRequests`                                                | Enables cacheable GET or persisted-operation flows.               |
| `GraphQL:OnlyAllowPersistedDocuments`                                      | Restricts execution to known persisted documents.                 |
| `GraphQL:MaxAllowedRequestSize`                                            | Limits GraphQL request body size in Hot Chocolate.                |
| `GraphQL:ExecutionTimeoutSeconds`                                          | Sets execution time limit for requests.                           |
| `GraphQL:MaxFieldCost` and `GraphQL:MaxTypeCost`                           | Tune cost-analysis limits per environment.                        |
| `ConnectionStrings:Redis`                                                  | Azure Cache for Redis connection string or endpoint.              |
| `Storage:PersistedOperationsContainerUri`                                  | Blob container URI for trusted operation documents.               |
| `Cors:Origins`                                                             | Allowed browser origins.                                          |
| `AzureMonitor:ConnectionString` or `APPLICATIONINSIGHTS_CONNECTION_STRING` | Azure Monitor export configuration.                               |

Use managed identity for Azure Blob Storage when possible. Never hardcode Redis connection strings, JWT signing keys, storage keys, or Application Insights connection strings in code.

Before swapping App Service slots, shifting Container Apps traffic, or rolling AKS pods, verify that the target environment has compatible settings for Nitro, introspection, schema SDL download, GET requests, multipart upload, request size, cost limits, persisted-only enforcement, Redis, Blob Storage, authentication, CORS, and telemetry.

# Tune request size, uploads, and execution limits

Requests can be rejected at several layers before reaching Hot Chocolate: Azure ingress, Application Gateway, IIS/App Service, Kestrel, ASP.NET Core form parsing, or Hot Chocolate request parsing. Identify which layer is responsible before changing limits.

To limit GraphQL request bodies in Hot Chocolate:

```csharp
builder.AddGraphQL(maxAllowedRequestSize: 1 * 1000 * 1000);
```

To configure multipart limits for file uploads:

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

Clients must include the `GraphQL-preflight` header for multipart uploads:

```bash
curl https://api.example.com/graphql \
  -H 'GraphQL-preflight: 1' \
  -F operations='{ "query": "mutation ($file: Upload!) { uploadFile(file: $file) }", "variables": { "file": null } }' \
  -F map='{ "0": ["variables.file"] }' \
  -F 0=@file.txt
```

For large files, use presigned Azure Blob Storage upload URLs. Authorize the upload with GraphQL, then upload the file directly to storage.

Set execution and cost limits to prevent expensive operations from consuming resources:

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

- A GraphQL JSON request that is too large fails at the Hot Chocolate request limit with a clear error.
- A multipart request that is too large may fail earlier with `413 Payload Too Large` from ASP.NET Core or Azure.
- An expensive operation fails during validation or cost analysis before resolvers run.

# Scale and deploy safely

Before adding replicas or shifting production traffic, review this checklist:

- Use Redis subscriptions if more than one replica can publish or receive subscription events.
- Use shared operation document storage for APQ or trusted documents. Choose Redis or Azure Blob Storage based on your workflow.
- Set minimum replicas or always-on settings if latency, cold starts, or subscriptions are important.
- Publish trusted operation artifacts before or with the server version that requires them.
- Keep schema changes compatible with deployed clients. Use a schema and client release workflow if multiple client versions are active.
- Run warmup tasks or smoke tests for important operations before shifting traffic.
- Expect WebSocket and SSE clients to disconnect during slot swaps, revision changes, pod restarts, and node drains. Clients must reconnect and resubscribe.
- For AKS, configure graceful termination and drain behavior long enough for in-flight requests. Do not expect subscriptions to survive pod termination.
- For App Service slots and Container Apps revisions, verify production settings on the target before moving traffic.

You can use a small operation for a deployment smoke test:

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

Expected result: `200 OK` only after the app and all critical GraphQL dependencies are ready.

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

If the preflight header is missing, expect a client error. If GET is disabled, the endpoint should reject the request.

## Check Nitro and schema policy

Request `/graphql` from a browser or send an HTML accept header:

```bash
curl -i https://api.example.com/graphql -H 'Accept: text/html'
```

In production, Nitro should not be served when `Tool.Enable` is false.

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

Expected result for emitted events:

```text
event: next
data: {"data":{"onBookAdded":{"title":"GraphQL in Action"}}}
```

If Redis subscriptions are configured, publish an event on one replica and confirm a subscriber on another replica receives it.

## Check telemetry

Send a successful request and a request that produces a GraphQL error. Application Insights should show correlated ASP.NET Core and GraphQL spans, including operation type, operation name (if provided), document hash (if available), and errors.

# Troubleshoot Azure deployment issues

| Symptom                                                           | Likely cause                                                                                                                          | Check                                                                                          | Fix                                                                                          |
| ----------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------- |
| `404` on `/graphql`                                               | Route mismatch, base path or ingress rewrite, or GraphQL was not mapped.                                                              | Check `MapGraphQL` path and Azure route or ingress rules.                                      | Align the external route with `MapGraphQL("/graphql")` or update ingress rewrite rules.      |
| Nitro appears in production                                       | `Tool.Enable` is not environment-gated, or the slot/revision has the wrong setting.                                                   | Request `/graphql` with `Accept: text/html` and inspect environment settings.                  | Disable Nitro through `ModifyServerOptions` or configuration for production.                 |
| Browser GET returns `400`                                         | GET preflight is enforced and the header is missing, or GET is disabled.                                                              | Check `EnableGetRequests` and `EnforceGetRequestsPreflightHeader`.                             | Add the `GraphQL-preflight` header or use POST.                                              |
| Multipart upload returns `400`                                    | Missing `GraphQL-preflight`, multipart disabled, or malformed `operations` and `map` fields.                                          | Compare the request with the multipart spec shape.                                             | Enable multipart only when needed and send the required preflight header.                    |
| Upload returns `413`                                              | Hot Chocolate, ASP.NET Core form limits, Kestrel, IIS/App Service, Application Gateway, or ingress rejected the body.                 | Identify which layer emitted the response.                                                     | Tune the correct layer or use presigned Blob Storage uploads for large files.                |
| `401`, `403`, or missing user                                     | Middleware order, CORS credentials, JWT issuer or audience, or WebSocket auth handshake.                                              | Check `UseCors`, `UseAuthentication`, `UseAuthorization`, JWT settings, and interceptors.      | Put middleware before `MapGraphQL`, fix CORS origins, and handle socket authentication.      |
| WebSocket handshake fails with `400`, `502`, or close code `1006` | Host WebSockets disabled, proxy does not preserve upgrade, subprotocol mismatch, idle timeout, TLS or proxy issue, or keep-alive slow | Check App Service WebSockets, ingress logs, `Sec-WebSocket-Protocol`, and keep-alive settings. | Enable WebSockets, preserve upgrade headers, use a supported protocol, and tune keep-alives. |
| SSE starts then stops                                             | Proxy buffering or idle timeout, missing client reconnect, or ingress cuts long responses.                                            | Check proxy and ingress behavior for streaming responses.                                      | Disable buffering where applicable, tune timeouts, and add client reconnect logic.           |
| Subscriptions work locally but not after scale-out                | In-memory subscriptions.                                                                                                              | Check whether `AddInMemorySubscriptions()` is used in production.                              | Use `HotChocolate.Subscriptions.Redis` and a topic prefix.                                   |
| Persisted operation not found after restart or on another replica | In-memory operation storage.                                                                                                          | Restart the app or send the request to another replica.                                        | Use Redis or Azure Blob Storage operation document storage and verify hash encoding.         |
| Trusted operation works in staging but not production             | Missing container, wrong container or prefix, operation artifact not deployed, or identity lacks storage permission.                  | Check Blob container, blob names, deployment artifacts, and identity role assignments.         | Create the container before startup, deploy operation blobs, and grant storage access.       |
| Slow first request or failed startup                              | Schema initialization or warmup issue, dependency unavailable, or lazy initialization enabled.                                        | Check startup logs and `/readyz`.                                                              | Keep eager initialization, fix dependencies, and use warmup tasks for important operations.  |
| No GraphQL spans in Application Insights                          | Missing `.AddInstrumentation()`, missing `.AddHotChocolateInstrumentation()`, exporter not configured, sampling, or identity problem  | Compare local OpenTelemetry output with Azure export.                                          | Register both Hot Chocolate instrumentation calls and configure Azure Monitor export.        |
| Introspection unexpectedly allowed or denied                      | Default security, explicit `.DisableIntrospection(...)`, environment name, or interceptor allowlist.                                  | Send an introspection query and inspect environment/configuration.                             | Make the introspection policy explicit and test the production slot or revision.             |

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
