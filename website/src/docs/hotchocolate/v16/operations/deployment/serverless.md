---
title: Serverless deployments
---

Serverless and scale-to-zero platforms can run a Hot Chocolate v16 GraphQL server well when the endpoint is stateless, HTTP-based, and designed around provider limits. They are a poor fit for workloads that need long-lived connections, local durable files, large request bodies, or work that runs close to the platform timeout.

This page focuses on Hot Chocolate server operations. Fusion gateways are out of scope.

# Prerequisites

Before you deploy Hot Chocolate to a serverless platform, make these decisions explicit:

- You have a Hot Chocolate v16 server running on ASP.NET Core, or you use the Hot Chocolate Azure Functions integration.
- You know your provider limits for request body size, request duration, idle streaming, WebSockets, file system persistence, and instance pre-warming.
- You can store durable state outside the process, for example in Redis, Azure Blob Storage, object storage, a database, or another provider-managed service.
- You know whether the endpoint serves public clients, first-party clients, or trusted back-end clients. This affects persisted operations, GET requests, Nitro, CORS, authentication, and request limits.

# Decide whether serverless fits your GraphQL workload

Use serverless for bounded HTTP query and mutation workloads. Split out workloads that need long-lived transport, large uploads, or background processing.

| Workload                           | Serverless fit            | Hot Chocolate action                                                                            | Platform question                                           |
| ---------------------------------- | ------------------------- | ----------------------------------------------------------------------------------------------- | ----------------------------------------------------------- |
| Stateless queries and mutations    | Good                      | Use HTTP GET and POST. Keep state in external services.                                         | What cold-start budget can you tolerate?                    |
| Bursty first-party traffic         | Good                      | Use trusted or persisted operations with durable storage.                                       | Can a CDN or edge cache cache GET responses?                |
| Public untrusted traffic           | Conditional               | Keep default security, add limits, authentication, CORS, and trusted operations where possible. | Do you have WAF and rate-limit controls outside the app?    |
| Large multipart uploads            | Poor for GraphQL compute  | Return a presigned upload URL and upload directly to object storage.                            | What are the body size and memory limits?                   |
| Subscriptions, WebSockets, and SSE | Poor or platform-specific | Use an always-on service or a managed real-time gateway.                                        | What are the connection, idle, and maximum duration limits? |
| Long-running and batch operations  | Poor                      | Queue a job and expose a status query.                                                          | What is the maximum invocation duration?                    |

A serverless instance is disposable. It may start cold, handle one or more requests, scale out, and disappear without notice. Design the GraphQL endpoint so correctness does not depend on process memory, local disk, or post-response work.

# Start from a minimal serverless-safe endpoint

Most serverless ASP.NET Core adapters wrap the same application shape. Start with an HTTP-only endpoint when your platform does not support WebSockets, or when you do not want Nitro, schema SDL downloads, or WebSocket middleware on the public route.

```csharp
using HotChocolate.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

builder
    .AddGraphQL(maxAllowedRequestSize: 1 * 1000 * 1024)
    .AddQueryType<Query>()
    .AddAuthorization()
    .ModifyRequestOptions(o =>
    {
        o.ExecutionTimeout = TimeSpan.FromSeconds(10);
    });

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGraphQLHttp("/graphql")
    .WithOptions(o =>
    {
        o.AllowedGetOperations = AllowedGetOperations.Query;
        o.EnforceGetRequestsPreflightHeader = true;
        o.EnforceMultipartRequestsPreflightHeader = true;
    });

app.Run();

public sealed class Query
{
    public string Ping() => "pong";
}
```

With this configuration, `/graphql` accepts GraphQL HTTP POST requests and GET queries. `MapGraphQLHttp()` maps only the HTTP transport, so it does not serve Nitro, SDL downloads, or WebSocket subscriptions.

Verify the endpoint with a small POST request:

```bash
curl http://localhost:5000/graphql \
  -H "Content-Type: application/json" \
  -d '{"query":"{ ping }"}'
```

Expected response:

```json
{
  "data": {
    "ping": "pong"
  }
}
```

Use `MapGraphQL()` only when you intentionally want the combined endpoint behavior. The combined endpoint handles HTTP, multipart, WebSockets when WebSocket middleware is enabled, Nitro in browsers, and schema SDL requests via `?sdl`. For production, make those choices explicit:

```csharp
app.MapGraphQL("/graphql")
    .WithOptions(o =>
    {
        o.Tool.Enable = false;
        o.EnableSchemaRequests = false;
        o.AllowedGetOperations = AllowedGetOperations.Query;
    });
```

`AddGraphQL(maxAllowedRequestSize: ...)` rejects request bodies that exceed the configured size before parsing. The ASP.NET Core default is `20 * 1000 * 1024` bytes. Lower it when your clients use small operations or persisted operations.

# Use Azure Functions integration when that is your host

Hot Chocolate includes Azure Functions integration packages. Use these APIs instead of mixing ASP.NET Core endpoint mapping into an Azure Functions app.

For in-process Azure Functions, register GraphQL in the Functions host builder:

<PackageInstallation packageName="HotChocolate.AzureFunctions" />

```csharp
using Microsoft.Azure.Functions.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]

public sealed class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        builder
            .AddGraphQLFunction(maxAllowedRequestSize: 1 * 1000 * 1000)
            .AddQueryType<Query>()
            .ModifyFunctionOptions(o =>
            {
                o.Tool.Enable = false;
                o.EnableSchemaRequests = false;
            });
    }
}
```

A function can execute the request through `IGraphQLRequestExecutor`:

```csharp
using HotChocolate.AzureFunctions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;

public sealed class GraphQLFunction
{
    [FunctionName("GraphQL")]
    public Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "graphql")] HttpRequest request,
        [GraphQL] IGraphQLRequestExecutor executor)
        => executor.ExecuteAsync(request);
}
```

For isolated-process Azure Functions, configure the `IHostBuilder` and inject `IGraphQLRequestExecutor` into your function class:

<PackageInstallation packageName="HotChocolate.AzureFunctions.IsolatedProcess" />

```csharp
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .AddGraphQLFunction(graphQL => graphQL
        .AddQueryType<Query>()
        .ModifyFunctionOptions(o =>
        {
            o.Tool.Enable = false;
            o.EnableSchemaRequests = false;
        }))
    .Build();

host.Run();
```

```csharp
using HotChocolate.AzureFunctions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

public sealed class GraphQLFunction(IGraphQLRequestExecutor executor)
{
    [Function("GraphQL")]
    public Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "graphql")] HttpRequestData request)
        => executor.ExecuteAsync(request);
}
```

The Hot Chocolate Azure Functions default route is `/api/graphql`. The default maximum request size is `20 * 1000 * 1000` bytes. `ModifyFunctionOptions` configures the same `GraphQLServerOptions` used by ASP.NET Core endpoints, including Nitro, schema requests, GET, multipart, and WebSocket options. Azure trigger authorization, plans, networking, managed identity, and deployment remain Azure platform concerns.

# Warm the schema without doing real work

Hot Chocolate v16 eagerly constructs the schema and request executor during startup. That catches schema errors before traffic and avoids schema construction on the first request. You can also warm selected documents and operation caches:

```csharp
using HotChocolate.Execution;

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddWarmupTask(async (executor, ct) =>
    {
        var request = OperationRequestBuilder.New()
            .SetDocument("query GetViewer { viewer { id name } }")
            .SetOperationName("GetViewer")
            .MarkAsWarmupRequest()
            .Build();

        await executor.ExecuteAsync(request, ct);
    });
```

Warmup tasks block startup. Keep them bounded so a new serverless instance can start within the provider limit. Include the operation name when clients send one because the operation name is part of the operation cache key.

`MarkAsWarmupRequest()` primes parsing, validation, and operation preparation without executing resolvers. It also skips security measures such as persisted-operation enforcement. Do not use warmup requests as a security test.

If you implement `IRequestExecutorWarmupTask`, use `ApplyOnlyOnStartup` when a task should not run on runtime schema rebuilds. Avoid `LazyInitialization` on serverless unless startup limits force it and you accept slower first requests.

# Treat every instance as disposable

Serverless platforms can recycle instances at any time and scale requests across multiple instances. Keep all durable and shared state outside the process.

| Do not rely on                              | Use instead                                                                                              |
| ------------------------------------------- | -------------------------------------------------------------------------------------------------------- |
| In-memory APQ storage                       | Redis or Azure Blob operation document storage                                                           |
| Local filesystem persisted operations       | Redis, Azure Blob Storage, or a validated shared durable mount                                           |
| In-memory subscriptions                     | Redis, NATS, or Postgres plus a host that supports long-lived transport, or a separate real-time service |
| Local uploads or temporary files            | Object storage and presigned upload URLs                                                                 |
| Singleton mutable counters or session state | A database, distributed cache, or idempotency store                                                      |

DataLoader is safe in this model because it is a per-request cache. Each GraphQL request gets fresh DataLoader instances and an empty cache. DataLoader reduces N+1 queries within one request, but it is not a cross-request cache and it is not shared across instances.

Store sessions, idempotency records, persisted operation documents, subscription events, uploaded files, and domain data in external services.

# Prefer trusted operations for smaller and safer requests

Trusted operations, also called persisted operations, let first-party clients execute a pre-registered operation by ID. This reduces request size and lets you block dynamic documents in production.

<PackageInstallation packageName="HotChocolate.PersistedOperations.Redis" />

```csharp
using StackExchange.Redis;

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .UsePersistedOperationPipeline()
    .AddRedisOperationDocumentStorage(services =>
        ConnectionMultiplexer.Connect("localhost:6379").GetDatabase())
    .ModifyRequestOptions(o =>
    {
        o.PersistedOperations.OnlyAllowPersistedDocuments = true;
    });

app.MapGraphQLPersistedOperations("/graphql/operations", requireOperationName: true);
```

Clients can then call a persisted operation route such as:

```http
GET /graphql/operations/0c95d31ca29272475bf837f944f4e513/GetViewer
```

Use durable shared storage for serverless. Redis and Azure Blob Storage are good candidates. Filesystem storage is safe only when the mount is shared, durable, and operationally validated. In-memory APQ storage is useful for local development, but it does not survive restarts and each scaled instance has its own cache.

For mixed or third-party clients, [automatic persisted operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations) can work when the storage is shared. Plan for the first request of a new operation to return a persisted-query-not-found response, then for the client to retry with the full document.

# Keep request bodies and uploads inside platform limits

Set limits at every layer that can reject the request: the provider, reverse proxy, ASP.NET Core form parsing, and Hot Chocolate request parsing.

```csharp
using Microsoft.AspNetCore.Http.Features;

builder.AddGraphQL(maxAllowedRequestSize: 1 * 1000 * 1024);

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024;
});
```

Hot Chocolate supports multipart uploads with `UploadType` and `IFile`:

```csharp
builder
    .AddGraphQL()
    .AddMutationType<Mutation>()
    .AddType<UploadType>();
```

For serverless, prefer a mutation that returns a presigned URL and uploads the file directly to object storage:

```csharp
public record CreateUploadUrlPayload(string UploadUrl, string FileId);

public sealed class Mutation
{
    public CreateUploadUrlPayload CreateProfilePictureUploadUrl()
        => new("https://storage.example/upload/abc?signature=...", "abc");
}
```

The client flow is:

1. Call the GraphQL mutation to get `uploadUrl` and `fileId`.
2. Upload the bytes directly to object storage using `uploadUrl`.
3. Call another mutation, or rely on storage events, to attach the uploaded file to domain data.

This keeps large bodies out of the GraphQL invocation, avoids buffering pressure, and lets object storage enforce upload limits. If you intentionally use multipart GraphQL uploads, keep `EnforceMultipartRequestsPreflightHeader = true` and require clients to send `GraphQL-Preflight: 1`.

# Align GraphQL timeouts with provider timeouts

Hot Chocolate aborts GraphQL requests after 30 seconds by default. The timeout is not enforced while a debugger is attached. On serverless, set the GraphQL timeout below the platform invocation timeout so Hot Chocolate can return a GraphQL error and cancel downstream work before the platform terminates the process.

```csharp
builder
    .AddGraphQL()
    .ModifyRequestOptions(o =>
    {
        o.ExecutionTimeout = TimeSpan.FromSeconds(10);
    });
```

Pass cancellation tokens through resolvers and data access calls:

```csharp
public async Task<Product?> GetProductAsync(
    int id,
    ProductRepository repository,
    CancellationToken cancellationToken)
    => await repository.GetByIdAsync(id, cancellationToken);
```

Avoid long polling, blocking I/O, synchronous waits, and large fan-out in resolvers. Move long-running work to queues or jobs, return a job ID from a mutation, and expose a status query.

# Be explicit about subscriptions, WebSockets, SSE, and streaming

Hot Chocolate supports WebSocket subscriptions, SSE, and incremental delivery over HTTP. Serverless platforms decide whether the underlying connection can remain open long enough.

| Capability                                      | Hot Chocolate support                              | Serverless concern                                                     | Recommendation                                          |
| ----------------------------------------------- | -------------------------------------------------- | ---------------------------------------------------------------------- | ------------------------------------------------------- |
| HTTP queries and mutations                      | Supported over GET and POST                        | Bounded by request size and duration                                   | Best fit                                                |
| Incremental delivery over `multipart/mixed`     | Supported with `@defer` and `@stream`              | Response must stay open while chunks are sent                          | Use only when the provider and proxy support streaming  |
| SSE subscriptions and streaming                 | Supported through HTTP content negotiation         | Idle and maximum response duration limits may close the stream         | Validate with the exact hosting plan and proxy          |
| WebSocket subscriptions                         | Supported when `app.UseWebSockets()` is registered | Many serverless HTTP products do not support upgrades or long sessions | Prefer always-on hosting or a managed real-time service |
| Redis, NATS, or Postgres subscription providers | Supported for multi-instance event delivery        | Pub/sub does not keep the client transport alive                       | Use with a host that supports the required transport    |

For an HTTP-only serverless endpoint, map only HTTP:

```csharp
app.MapGraphQLHttp("/graphql");
```

When your platform supports WebSockets and you want subscriptions on that host, enable the WebSocket middleware and map the combined endpoint:

```csharp
app.UseWebSockets();
app.MapGraphQL();
```

Redis, NATS, and Postgres solve cross-instance event delivery. They do not solve connection lifetime limits imposed by the serverless host, CDN, proxy, or client network. If you cannot guarantee long-lived connections, use polling, webhooks, queues, a managed real-time gateway, or a separate always-on Hot Chocolate service for subscriptions.

# Harden public serverless endpoints

Make production endpoint behavior explicit so your security review does not depend on defaults hidden in code.

```csharp
app.MapGraphQL()
    .WithOptions(o =>
    {
        o.Tool.Enable = false;
        o.EnableSchemaRequests = false;
        o.AllowedGetOperations = AllowedGetOperations.Query;
        o.EnforceGetRequestsPreflightHeader = true;
        o.EnforceMultipartRequestsPreflightHeader = true;
    });
```

Use this checklist for internet-facing endpoints:

- Configure ASP.NET Core authentication and call `app.UseAuthentication()` before `app.UseAuthorization()`.
- Register `builder.Services.AddAuthorization()` for ASP.NET Core and `.AddAuthorization()` on the GraphQL builder when you use `@authorize`.
- Apply endpoint authorization or Hot Chocolate authorization policies. `.AddAuthorization()` does not automatically reject unauthenticated users.
- Use an explicit CORS policy and make sure provider edge CORS does not contradict the application policy.
- Disable Nitro in production unless you intentionally expose it.
- Disable schema SDL downloads unless you intentionally expose them.
- Limit GET to queries or disable GET when clients do not need it.
- Keep CSRF preflight checks enabled for multipart requests and enable them for GET when the endpoint is browser-accessible.
- Review request size, parser, validation, execution depth, cost, and timeout limits.
- Review introspection policy. Hot Chocolate disables introspection in production by default unless you change default security.
- Prefer trusted operations for first-party clients.

Keep Hot Chocolate default security enabled unless you deliberately replace it with equivalent protections. `AddGraphQL(disableDefaultSecurity: true)` removes built-in protections such as cost analysis, production introspection blocking, and field cycle depth limits.

# Observe cold starts, cache misses, and resolver latency

Add Hot Chocolate instrumentation and export OpenTelemetry traces. Include ASP.NET Core and HTTP client instrumentation so you can separate platform time, GraphQL execution, and downstream service calls.

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddInstrumentation();

builder.Services
    .AddOpenTelemetry()
    .WithTracing(b => b
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddHotChocolateInstrumentation()
        .AddOtlpExporter());
```

Add the `HotChocolate.Diagnostics` package for `AddInstrumentation()` and the matching OpenTelemetry packages for your exporter.

<PackageInstallation packageName="HotChocolate.Diagnostics" />

Watch these signals in production:

- Startup and warmup duration in logs, because cold start may happen before a request span exists.
- GraphQL operation type and operation name.
- Request parsing, validation, operation compilation, execution, and resolver durations.
- Document and operation cache hits and misses.
- Persisted document storage hits, misses, and latency.
- DataLoader batch sizes, errors, and downstream call latency.
- Request size rejections, timeout errors, and HTTP status codes.
- Provider invocation duration and termination reason.

Hot Chocolate keeps root span names low-cardinality. The operation name is available as an attribute. Avoid recording full operation documents in production telemetry unless the data is safe for your environment. Field-level and all-scope instrumentation add overhead, so enable deeper scopes only when you need them.

# Troubleshoot common serverless failures

| Symptom                                          | Likely cause                                                                                     | Fix                                                                                                                                                       |
| ------------------------------------------------ | ------------------------------------------------------------------------------------------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------- |
| First request is slow                            | New instance startup, schema creation, operation compilation, or empty caches                    | Keep eager initialization, add bounded warmup tasks, measure startup logs, and check provider pre-warming options                                         |
| Startup exceeds provider limit                   | Warmup or startup work takes too long                                                            | Reduce warmup scope, move schema export or heavy setup to build or deploy, avoid external network calls during startup unless required                    |
| Persisted operation or APQ works and later fails | Storage is in-memory or local to one instance                                                    | Use Redis, Azure Blob Storage, or another shared durable store. Verify the operation hash and operation name                                              |
| WebSocket or SSE disconnects                     | Platform, CDN, or proxy closes idle or long-running connections                                  | Check idle and maximum duration limits. Move subscriptions to always-on hosting or a managed real-time service                                            |
| Multipart upload is rejected                     | A body limit, form limit, provider limit, preflight rule, or multipart shape rejects the request | Check `maxAllowedRequestSize`, `FormOptions.MultipartBodyLengthLimit`, provider body limits, CORS, `GraphQL-Preflight`, and the client multipart request  |
| Request is killed without a GraphQL error        | Platform timeout terminates the invocation first                                                 | Set `ExecutionTimeout` below the platform timeout, pass cancellation tokens, and inspect provider logs                                                    |
| Nitro or schema SDL endpoint is exposed          | The combined `MapGraphQL()` endpoint is mapped with default options                              | Use `MapGraphQLHttp()` for HTTP-only serving, or disable `Tool.Enable` and `EnableSchemaRequests` on `MapGraphQL()` or `ModifyFunctionOptions`            |
| Authentication works locally but fails in cloud  | Headers, middleware order, CORS, or proxy behavior differs                                       | Check provider auth headers, forwarded headers, CORS preflight, `UseAuthentication()` before `UseAuthorization()`, and request interceptor customizations |
| Data differs between requests or instances       | Correctness depends on process memory or singleton mutable state                                 | Move durable state to an external service. Use DataLoader only for per-request caching                                                                    |

# Verify the deployment

After deployment, run a small verification set against the deployed endpoint:

1. Send a POST query and confirm a normal GraphQL JSON response.
2. Send a GET query if GET is enabled, including the required preflight header when configured.
3. Request `?sdl` and open the endpoint in a browser to confirm schema downloads and Nitro match your production decision.
4. Send an oversized body and confirm it fails at the configured limit.
5. Execute a slow operation in a test environment and confirm Hot Chocolate times out before the provider does.
6. Restart or scale out the app and confirm persisted operations, APQ, sessions, uploads, and domain data still work.
7. If you support streaming or subscriptions, test through the same CDN, proxy, and hosting plan used in production.
8. Confirm traces and logs show GraphQL operation names, timeout errors, storage misses, and downstream latency.

# Choose your next page

- [Endpoints](/docs/hotchocolate/v16/server/endpoints) for `MapGraphQL`, `MapGraphQLHttp`, Nitro, schema downloads, and request size settings.
- [Warmup](/docs/hotchocolate/v16/server/warmup) for eager initialization and warmup tasks.
- [HTTP Transport](/docs/hotchocolate/v16/server/http-transport) for GET, POST, content negotiation, SSE, and incremental delivery.
- [Subscriptions](/docs/hotchocolate/v16/building-a-schema/subscriptions) for WebSocket, SSE, Redis, NATS, and Postgres subscription providers.
- [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader) for per-request batching and caching.
- [Trusted Documents](/docs/hotchocolate/v16/performance/trusted-documents) and [Automatic Persisted Operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations) for operation storage choices.
- [Files](/docs/hotchocolate/v16/server/files) for `UploadType`, multipart requests, and presigned upload URLs.
- [Request Limits](/docs/hotchocolate/v16/securing-your-api/request-limits), [Cost Analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis), and [Introspection](/docs/hotchocolate/v16/securing-your-api/introspection) for public API hardening.
- [Authentication](/docs/hotchocolate/v16/securing-your-api/authentication), [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization), and [Interceptors](/docs/hotchocolate/v16/server/interceptors) for identity and per-request context.
- [Instrumentation](/docs/hotchocolate/v16/server/instrumentation) for diagnostics and OpenTelemetry.
