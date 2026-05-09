---
title: Deploy Hot Chocolate on AWS
---

You can deploy Hot Chocolate v16 as an ASP.NET Core service on AWS. AWS manages hosting, routing, security, and monitoring, while Hot Chocolate provides the GraphQL endpoint, transports, execution pipeline, subscriptions, persisted operations, and GraphQL-specific security features.

A typical production deployment path is:

```text
Client or CloudFront
  -> ALB or API Gateway
  -> ECS task, EKS pod, VM, or managed ASP.NET Core host
  -> Hot Chocolate endpoint at /graphql
  -> databases, ElastiCache Redis, and downstream services
```

This guide focuses on standalone Hot Chocolate v16 server deployments. Fusion gateway and subgraph scenarios are not covered. For AWS infrastructure setup, such as VPCs, clusters, target groups, certificates, IAM roles, CloudFront distributions, API Gateway stages, and Kubernetes objects, refer to the AWS documentation.

# Prerequisites

Before deploying, ensure you have:

- A working Hot Chocolate v16 ASP.NET Core server
- A .NET runtime target or container image for your service
- A public GraphQL endpoint path (usually `/graphql`)
- A production authentication and authorization plan
- Decisions on subscriptions, SSE, incremental delivery, uploads, batching, trusted documents, and APQ
- AWS DNS, TLS certificate, network, and deployment target choices
- ElastiCache Redis connection details if subscriptions, trusted documents, or APQ must work across instances or survive restarts
- A telemetry route (for example, OTLP to an AWS Distro for OpenTelemetry Collector)
- Runtime configuration for secrets and environment-specific settings

# Choose an AWS Host Based on GraphQL Needs

Select your AWS hosting model based on the transports your GraphQL clients require, not only how you package your app.

| Hosting Model                                      | Best Fit                                                                                                                       | WebSocket and SSE Support                                                                                                                           | Scale-Out Notes                                                                                                              | Hot Chocolate Considerations                                                                                                  |
| -------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------ | --------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------- |
| ECS on Fargate                                     | Containerized ASP.NET Core services where you want managed compute without Kubernetes.                                         | Works well behind an ALB if the listener, target group, and security groups preserve WebSocket upgrades and long HTTP responses.                    | Scale tasks horizontally. Use health checks and deployment grace periods. Use ElastiCache for Redis-backed features.         | Recommended default for many AWS Hot Chocolate APIs. Explicitly set Nitro, schema download, and upload limits.                |
| EKS                                                | Teams already using Kubernetes and needing ingress controllers, HPA, network policy, sidecars, or daemonsets.                  | Depends on your ingress. Test WebSocket upgrades, SSE, `multipart/mixed`, and JSON Lines through the full path.                                     | Map readiness and liveness probes to ASP.NET Core health endpoints. Use disruption budgets for subscription-heavy workloads. | Do not rely on pod-local memory for subscriptions, APQ, or trusted documents.                                                 |
| Elastic Beanstalk, App Runner, or VM-style hosting | Simpler ASP.NET Core deployments where the managed front end supports your required request sizes, idle timeouts, and headers. | Verify platform-specific WebSocket and streaming behavior before using subscriptions or incremental delivery.                                       | Scale instances behind a managed load balancer. Store configuration in platform settings or secrets.                         | Good for HTTP query and mutation APIs. Re-test long-lived connections after platform changes.                                 |
| Lambda with API Gateway                            | Short-lived queries and mutations when you already adapt ASP.NET Core to Lambda.                                               | Not a default fit for Hot Chocolate subscriptions or long-lived SSE. API Gateway WebSocket APIs use a different connection-management architecture. | Cold starts, body limits, and integration timeouts matter.                                                                   | Do not use Lambda for normal Hot Chocolate subscription traffic unless an AWS-specific architecture manages connection state. |

For internet-facing APIs, use a long-running ASP.NET Core host if clients need subscriptions, SSE, incremental delivery, file uploads, or operation warmup. If GraphQL operations are slow, do not start by raising AWS timeouts. Instead, review request limits, cost analysis, pagination, DataLoader usage, and resolver performance.

# Set Up a Production Hot Chocolate Endpoint

Expose a single canonical endpoint and make production behavior explicit. This approach keeps development tools private, requires deliberate GET and upload behavior, and keeps WebSocket settings close to the GraphQL server configuration.

```csharp
// Program.cs
using HotChocolate.AspNetCore;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);
var isDevelopment = builder.Environment.IsDevelopment();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
        | ForwardedHeaders.XForwardedHost
        | ForwardedHeaders.XForwardedProto;
    // In production, configure KnownProxies or KnownNetworks for your AWS edge.
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("GraphQLClients", policy =>
    {
        policy
            .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [])
            .WithMethods("GET", "POST")
            .WithHeaders(
                "Authorization",
                "Content-Type",
                "Accept",
                "GraphQL-preflight")
            .AllowCredentials();
    });
});

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

builder
    .AddGraphQL(maxAllowedRequestSize: 1 * 1000 * 1000)
    .AddQueryType<Query>()
    .AddAuthorization()
    .ModifyRequestOptions(options =>
    {
        options.ExecutionTimeout = TimeSpan.FromSeconds(10);
        options.IncludeExceptionDetails = isDevelopment;
    })
    .ModifyServerOptions(options =>
    {
        options.AllowedGetOperations = AllowedGetOperations.Query;
        options.EnforceGetRequestsPreflightHeader = true;
        options.EnableMultipartRequests = false;
        options.EnforceMultipartRequestsPreflightHeader = true;
        options.MaxConcurrentExecutions = 64;
        options.Sockets.KeepAliveInterval = TimeSpan.FromSeconds(30);
        options.Sockets.ConnectionInitializationTimeout = TimeSpan.FromSeconds(10);
    });

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseCors("GraphQLClients");
app.UseAuthentication();
app.UseAuthorization();

// Only enable when WebSocket subscriptions are supported by your AWS path.
app.UseWebSockets();

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

app.MapGraphQL("/graphql")
    .WithOptions(options =>
    {
        options.Tool.Enable = isDevelopment;
        options.EnableSchemaRequests = false;
        options.EnableSchemaFileSupport = false;
    });

app.Run();

public sealed class Query
{
    public string GetStatus() => "ok";
}
```

To verify your deployed endpoint:

```bash
curl -sS https://api.example.com/graphql \
  -H 'Content-Type: application/json' \
  -H 'Accept: application/graphql-response+json' \
  --data '{ "query": "{ status }" }'
```

Expected response:

```json
{ "data": { "status": "ok" } }
```

The `builder.AddGraphQL(...)` call uses the v16 ASP.NET Core hosting style. Keep the default security policy unless you have a specific reason to change it. The default policy includes production protections such as cost analysis, restricted introspection, and field-cycle validation.

Place `UseCors`, `UseAuthentication`, `UseAuthorization`, and `UseWebSockets` before `MapGraphQL`. Hot Chocolate authorization depends on the ASP.NET Core user, browser clients require CORS, and WebSocket subscriptions depend on the ASP.NET Core WebSocket middleware.

# Expose Only the Intended Paths

`MapGraphQL("/graphql")` creates a combined endpoint. Route this path through AWS intentionally.

| Path                                    | What It Does                                                                                                                                        | Production Guidance                                                                    |
| --------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------- |
| `/graphql`                              | Handles HTTP POST, HTTP GET (if enabled), multipart (if enabled), WebSocket upgrades (if `UseWebSockets()` is registered), and Nitro browser entry. | Use as the public client path. Disable Nitro in production.                            |
| `/graphql?sdl` and schema file paths    | Schema SDL download, controlled by `EnableSchemaRequests` and `EnableSchemaFileSupport`.                                                            | Disable publicly unless schema download is part of your contract.                      |
| `/graphql/schema` or custom schema path | SDL endpoint when using `MapGraphQLSchema`.                                                                                                         | Restrict with internal routing or authentication if exposed.                           |
| `/graphql/ws`                           | Optional split WebSocket endpoint with `MapGraphQLWebSocket`.                                                                                       | Use when AWS routes WebSocket traffic separately from HTTP.                            |
| `/graphql/persisted/{operationId}`      | Persisted-operation HTTP endpoint when mapped with `MapGraphQLPersistedOperations`.                                                                 | Use for trusted document execution by URL. Configure CDN cache keys carefully.         |
| `/health/live` and `/health/ready`      | ASP.NET Core health endpoints outside GraphQL.                                                                                                      | Use for ALB target groups, ECS health checks, and EKS probes. Do not probe `/graphql`. |

Use split endpoints only if you need separate paths or policies:

```csharp
app.MapGraphQLHttp("/graphql");
app.MapGraphQLWebSocket("/graphql/ws");
app.MapGraphQLSchema("/graphql/schema");
app.MapGraphQLPersistedOperations("/graphql/persisted");

app.MapNitroApp("/graphql/ui")
    .WithOptions(options =>
    {
        options.Enable = app.Environment.IsDevelopment();
        options.GraphQLEndpoint = "/graphql";
    });
```

Keep health checks off the GraphQL endpoint. Probes should not execute GraphQL documents, require CORS headers, or depend on client authentication.

# Place ALB, API Gateway, or CloudFront in Front of GraphQL

Hot Chocolate determines response formats and transports based on HTTP methods and headers. Your AWS edge must preserve these methods and headers.

| Client Behavior                   | Required Request Shape                                                                               | AWS Checks                                                                                                 |
| --------------------------------- | ---------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------- |
| Standard queries and mutations    | `POST`, `Content-Type: application/json`, `Accept: application/graphql-response+json`.               | Forward body and `Accept`. Do not cache personalized POST responses.                                       |
| Cacheable queries or APQ          | `GET` with query string or `extensions`. Hot Chocolate should allow only configured operation types. | Forward query strings and headers that affect auth, tenant, locale, and content negotiation.               |
| Multipart upload                  | `POST`, multipart form content type, `GraphQL-preflight: 1`.                                         | Preserve the preflight header and align request body limits at every layer.                                |
| Incremental delivery and batching | `Accept: multipart/mixed`, `text/event-stream`, or `application/jsonl`.                              | Disable buffering where your proxy supports it and test streaming through the full path.                   |
| WebSocket subscriptions           | `Upgrade: websocket`, `Connection: Upgrade`, and a GraphQL WebSocket subprotocol.                    | Route to an endpoint with `UseWebSockets()` and proxy upgrade support. Align idle timeout and keep-alives. |
| Auth and CORS                     | `Authorization`, cookies (if used), `Origin`, `Access-Control-Request-*`.                            | Forward headers required by ASP.NET Core authentication and CORS. Never cache across auth boundaries.      |

ALB is a common choice for long-running ASP.NET Core services, WebSockets, and standard HTTP GraphQL. Set the ALB idle timeout above your expected quiet periods, or keep Hot Chocolate keep-alive pings below that timeout. The default Hot Chocolate WebSocket keep-alive interval is 5 seconds; the sample uses 30 seconds as an example. Adjust it as needed.

API Gateway works for simple HTTP GraphQL, especially short queries and mutations. Before using it for subscriptions, SSE, incremental delivery, batching, or uploads, check AWS documentation for current body size limits, integration timeout, streaming support, and WebSocket API architecture.

CloudFront can front GraphQL, but only cache GraphQL responses when the cache key includes every value that changes the response. For authenticated APIs, avoid caching personalized responses unless your cache policy varies by authorization context and you have tested tenant isolation. GET persisted queries are the most common cacheable shape.

Verify that AWS preserves transports through the full route, not Kestrel alone:

```bash
curl -i https://api.example.com/graphql \
  -H 'Content-Type: application/json' \
  -H 'Accept: application/graphql-response+json' \
  --data '{ "query": "{ __typename }" }'
```

Expected headers should include a GraphQL JSON content type, and the body should contain:

```json
{ "data": { "__typename": "Query" } }
```

For streaming, use the same `Accept` header as your client and confirm the response arrives incrementally. If the response appears only after the operation completes, proxy buffering or an unsupported integration path is likely the cause.

# Run Subscriptions on AWS

In-memory subscriptions work for a single instance or local development. On AWS, a subscriber may connect to one task while a mutation publishes on another. Use ElastiCache Redis to ensure events reach every instance.

Install the Redis provider:

<PackageInstallation packageName="HotChocolate.Subscriptions.Redis" />

Register a shared `IConnectionMultiplexer` and enable Redis subscriptions:

```csharp
using HotChocolate.Subscriptions;
using StackExchange.Redis;

var redisConnectionString = builder.Configuration.GetConnectionString("Redis");

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(redisConnectionString!));

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddSubscriptionType<Subscription>()
    .AddRedisSubscriptions(
        new SubscriptionOptions
        {
            TopicPrefix = "orders-prod:"
        });
```

Enable WebSocket transport if clients use WebSocket subscriptions:

```csharp
app.UseWebSockets();
app.MapGraphQL("/graphql");
```

Your publishing code does not change when you switch providers:

```csharp
public sealed class Mutation
{
    public async Task<Order> UpdateOrderStatusAsync(
        string orderId,
        OrderStatus status,
        ITopicEventSender sender,
        CancellationToken cancellationToken)
    {
        var order = new Order(orderId, status);
        await sender.SendAsync("OrderStatusChanged", order, cancellationToken);
        return order;
    }
}
```

Expected behavior: a mutation handled by task A publishes to Redis, and a subscriber connected to task B receives the event.

Use a `TopicPrefix` if multiple environments, services, preview stacks, or tests share one Redis deployment. Configure TLS, authentication, security groups, and Redis endpoints using AWS-managed configuration or secrets. SSE subscriptions do not require `UseWebSockets()`, but they still use long-running HTTP responses and need timeout and buffering tests.

# Store Trusted Documents and APQ in Redis for Scale-Out

Do not use in-memory operation document storage for multi-instance AWS deployments. Requests may route to any task or pod, and deployments restart instances. Redis ensures every instance can access the same operation documents.

Install Redis persisted operation storage:

<PackageInstallation packageName="HotChocolate.PersistedOperations.Redis" />

Use trusted documents when clients publish known operations during build or deployment:

```csharp
using StackExchange.Redis;

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(
        builder.Configuration.GetConnectionString("Redis")!));

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .UsePersistedOperationPipeline()
    .AddRedisOperationDocumentStorage(
        sp => sp.GetRequiredService<IConnectionMultiplexer>())
    .ModifyRequestOptions(options =>
    {
        options.PersistedOperations.OnlyAllowPersistedDocuments = true;
    });
```

A trusted-document client sends the operation `id` instead of the full document:

```json
{
  "id": "0c95d31ca29272475bf837f944f4e513",
  "variables": {
    "id": "42"
  }
}
```

Use APQ when clients register operation documents at runtime:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .UseAutomaticPersistedOperationPipeline()
    .AddRedisOperationDocumentStorage(
        sp => sp.GetRequiredService<IConnectionMultiplexer>(),
        queryExpiration: TimeSpan.FromDays(7));
```

With APQ, the client first requests by hash. If Redis does not contain the document, Hot Chocolate returns a persisted-query-not-found error and the client retries with the full document. After Redis stores it, any instance can execute the hash-only request.

Align the hash provider with your clients. Hot Chocolate uses MD5 by default for persisted operation document hashes unless you configure another provider, such as SHA-256.

# Configure Health Checks and Startup Readiness

Hot Chocolate v16 eagerly builds the schema and request executor during startup. This works well with ALB target groups, ECS health checks, and EKS readiness probes because the app is not marked ready until the executor is initialized.

Add health endpoints outside GraphQL:

```csharp
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
```

Warm up frequently used operations before traffic reaches a new instance:

```csharp
using HotChocolate.Execution;

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddWarmupTask(async (executor, cancellationToken) =>
    {
        var request = OperationRequestBuilder.New()
            .SetDocument("{ __typename }")
            .MarkAsWarmupRequest()
            .Build();
        await executor.ExecuteAsync(request, cancellationToken);
    });
```

Warmup tasks block startup. Set ECS health check grace periods, ALB target group thresholds, EKS readiness initial delays, and rollout settings long enough for schema creation and warmup. Keep liveness separate from readiness. Do not make liveness depend on databases or Redis unless you want AWS to restart the process when that dependency fails.

Avoid `LazyInitialization` in production unless you accept the cold-start trade-off. Lazy initialization moves schema construction to the first request and can make new targets appear healthy before they can answer GraphQL quickly.

# Send Hot Chocolate Telemetry to AWS Observability

Hot Chocolate emits OpenTelemetry spans through its diagnostics package. Export OTLP to an ADOT Collector running as an ECS sidecar, EKS daemonset, EKS sidecar, or managed collector, then route to CloudWatch, X-Ray-compatible backends, or another tracing system.

Install the required packages:

<PackageInstallation packageName="HotChocolate.Diagnostics" />

```bash
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Instrumentation.AspNetCore
dotnet add package OpenTelemetry.Instrumentation.Http
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
```

Configure Hot Chocolate instrumentation and OTLP export:

```csharp
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddInstrumentation();

builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resource =>
    {
        resource.AddService("orders-graphql");
    })
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddHotChocolateInstrumentation()
            .AddOtlpExporter();
    });
```

Set the OTLP endpoint using environment configuration, for example `OTEL_EXPORTER_OTLP_ENDPOINT=http://adot-collector:4317` in ECS or EKS. You should see GraphQL spans in your AWS telemetry pipeline with attributes such as operation type, operation name, document hash, and trusted document id when available.

Keep span cardinality low. Enable document text, variables, detailed request data, or field-level spans only for targeted debugging, as these add overhead and may expose sensitive data.

# Size and Scale the Service Safely

Use Hot Chocolate controls before increasing AWS capacity. These controls reduce per-instance risk and make scaling more predictable.

| Control               | Example                                                             | Why It Matters on AWS                                                                         |
| --------------------- | ------------------------------------------------------------------- | --------------------------------------------------------------------------------------------- |
| Execution concurrency | `options.MaxConcurrentExecutions = 64`                              | Caps concurrent GraphQL executions per instance. Avoid `null` or `0` unless you want no gate. |
| Execution timeout     | `options.ExecutionTimeout = TimeSpan.FromSeconds(10)`               | Keeps resolver work below client, ALB, API Gateway, and downstream timeouts.                  |
| Parser limits         | `ModifyParserOptions`                                               | Rejects abusive documents before validation and execution.                                    |
| Validation limits     | depth, fragment visits, field merge comparisons, field-cycle limits | Reduces CPU and memory pressure from adversarial queries.                                     |
| Cost analysis         | default security policy or explicit cost settings                   | Protects public endpoints from expensive but valid documents.                                 |
| Pagination limits     | connection and collection settings                                  | Prevents large result sets from causing memory and downstream pressure.                       |
| Batching limits       | `Batching` and `MaxBatchSize`                                       | Keeps batch requests bounded and tests streaming response support.                            |
| Persisted operations  | trusted documents or APQ                                            | Reduces payload size and can limit public clients to known operations.                        |

Example tuning:

```csharp
builder
    .AddGraphQL(maxAllowedRequestSize: 1 * 1000 * 1000)
    .AddQueryType<Query>()
    .AddMaxExecutionDepthRule(10)
    .ModifyParserOptions(options =>
    {
        options.MaxAllowedFields = 1024;
        options.MaxAllowedRecursionDepth = 100;
    })
    .ModifyRequestOptions(options =>
    {
        options.ExecutionTimeout = TimeSpan.FromSeconds(10);
    })
    .ModifyServerOptions(options =>
    {
        options.MaxConcurrentExecutions = 64;
        options.Batching = AllowedBatching.None;
        options.MaxBatchSize = 100;
    });
```

Scale based on signals that match your workload: CPU, memory, request latency, queued requests, downstream saturation, Redis pub/sub throughput, active subscription connection count, and error rate. For subscription-heavy services, connection count and reconnect storms during deployment can be as important as CPU.

# Manage Auth, CORS, and Secrets in AWS Environments

Use ASP.NET Core configuration so the same artifact runs in every environment with different AWS-provided settings.

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

var redisConnectionString = builder.Configuration.GetConnectionString("Redis");

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(redisConnectionString!));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Auth:Authority"];
        options.Audience = builder.Configuration["Auth:Audience"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true
        };
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("GraphQLClients", policy =>
    {
        policy
            .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [])
            .WithMethods("GET", "POST")
            .WithHeaders("Authorization", "Content-Type", "Accept", "GraphQL-preflight")
            .AllowCredentials();
    });
});
```

Store Redis, database, auth, signing, and API secrets in AWS Secrets Manager or Systems Manager Parameter Store, then inject them as environment variables or configuration providers. Do not hard-code Redis endpoints, signing keys, client secrets, or authority values in source code or images. Rotate secrets without rebuilding images when possible.

If you use cookies, review `SameSite`, `Secure`, forwarded headers, CloudFront header forwarding, and cache policies. Configure CORS for browser clients. Do not broaden production CORS rules for Nitro. Keep Nitro disabled publicly or protect it with network and authorization controls.

# Handle Uploads and Body Limits

For large files, prefer presigned S3 upload URLs. Let GraphQL authorize and create upload metadata, then have the client upload bytes directly to S3.

| Approach         | Use When                                                               | AWS and Hot Chocolate Checks                                                                                                                  |
| ---------------- | ---------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------- |
| Presigned S3 URL | Files are large, uploads are frequent, or clients can upload directly. | GraphQL mutation returns URL and metadata. S3 receives file bytes. Hot Chocolate request size stays small.                                    |
| `Upload` scalar  | Files are small and must flow through the GraphQL server.              | Register `UploadType`, enable multipart, require `GraphQL-preflight: 1`, and align AWS, Kestrel, ASP.NET Core form, and Hot Chocolate limits. |

Enable multipart only when your schema supports uploads:

```csharp
using Microsoft.AspNetCore.Http.Features;

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 20 * 1000 * 1000;
});

builder
    .AddGraphQL(maxAllowedRequestSize: 20 * 1000 * 1000)
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddType<UploadType>()
    .ModifyServerOptions(options =>
    {
        options.EnableMultipartRequests = true;
        options.EnforceMultipartRequestsPreflightHeader = true;
    });
```

Test with the required preflight header:

```bash
curl https://api.example.com/graphql \
  -H 'GraphQL-preflight: 1' \
  -F operations='{ "query": "mutation ($file: Upload!) { uploadFile(file: $file) }", "variables": { "file": null } }' \
  -F map='{ "0": ["variables.file"] }' \
  -F 0=@file.txt
```

Expected response for a sample boolean mutation:

```json
{ "data": { "uploadFile": true } }
```

A `413 Payload Too Large` error can occur before Hot Chocolate processes the request. Align the AWS edge or proxy body limit, Kestrel request body size, `FormOptions.MultipartBodyLengthLimit`, `AddGraphQL(maxAllowedRequestSize)`, and application validation.

# Troubleshoot AWS Deployment Failures

When you encounter issues, check both Hot Chocolate settings and the AWS layer.

| Symptom                                            | Likely Hot Chocolate Cause                                                                  | Likely AWS or Proxy Cause                                                                               | Next Check                                                                                       |
| -------------------------------------------------- | ------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------ |
| `413 Payload Too Large`                            | `AddGraphQL(maxAllowedRequestSize)` or `FormOptions` is lower than the request.             | ALB, API Gateway, CloudFront, ingress, or Kestrel rejects the body first.                               | Align every body limit. Prefer presigned S3 URLs for large files.                                |
| `400` on upload                                    | Missing `GraphQL-preflight: 1`, multipart disabled, or invalid multipart map.               | Header stripped or body rewritten.                                                                      | Send the documented `curl` request through the public AWS URL.                                   |
| WebSocket closes during subscription               | Missing `UseWebSockets()`, wrong route, keep-alive too high, or initialization timeout.     | Upgrade headers not preserved, idle timeout too low, or unsupported route.                              | Check `Sec-WebSocket-Protocol`, ALB or ingress upgrade support, and `Sockets.KeepAliveInterval`. |
| SSE or incremental delivery buffers until complete | Client did not send `Accept: text/event-stream`, `multipart/mixed`, or `application/jsonl`. | Proxy buffers streaming responses or integration does not support streaming.                            | Test with `curl -N` or your real client through AWS.                                             |
| First request is slow after rollout                | Warmup missing or `LazyInitialization` enabled.                                             | Health grace period marks targets ready before startup work finishes.                                   | Use eager startup, warmup tasks, and longer readiness grace periods.                             |
| Persisted query not found after scale-out          | In-memory storage, hash provider mismatch, or TTL expiration.                               | Requests route to a different instance or Redis config differs between tasks.                           | Use Redis storage and verify Redis DB, key, TTL, and hash algorithm.                             |
| Events do not reach all instances                  | In-memory subscriptions or mismatched `TopicPrefix`.                                        | Redis security group, auth, TLS, route, or failover issue.                                              | Publish on one task and subscribe through another. Inspect Redis connectivity.                   |
| `504` or client timeout                            | Execution timeout, slow resolver, downstream saturation, or too much concurrency.           | ALB or API Gateway timeout is lower than operation duration.                                            | Compare Hot Chocolate `ExecutionTimeout`, AWS timeout, client timeout, and resolver telemetry.   |
| Nitro or schema is visible publicly                | `Tool.Enable`, `EnableSchemaRequests`, or `EnableSchemaFileSupport` is enabled.             | Wrong environment variable, route, or CloudFront behavior exposes the path.                             | Verify production environment settings and endpoint options on the public URL.                   |
| Auth works locally but fails behind AWS            | CORS order, missing forwarded headers, or auth scheme configuration.                        | `Authorization` stripped, cookies not forwarded, cache key omits auth, or `SameSite`/`Secure` mismatch. | Inspect request headers at the app and test preflight from the browser origin.                   |

# Next Steps

For more details on Hot Chocolate features and deployment options, see:

- [ASP.NET Core hosting](/docs/hotchocolate/v16/operations/deployment/aspnetcore-hosting)
- [Docker deployment](/docs/hotchocolate/v16/operations/deployment/docker)
- [Endpoint configuration](/docs/hotchocolate/v16/server/endpoints)
- [HTTP transport and content negotiation](/docs/hotchocolate/v16/server/http-transport)
- [Subscriptions](/docs/hotchocolate/v16/building-a-schema/subscriptions)
- [Trusted documents](/docs/hotchocolate/v16/performance/trusted-documents)
- [Automatic persisted operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations)
- [Instrumentation](/docs/hotchocolate/v16/server/instrumentation)
- [Warmup](/docs/hotchocolate/v16/server/warmup)
- [Files and uploads](/docs/hotchocolate/v16/server/files)
- [Request limits](/docs/hotchocolate/v16/securing-your-api/request-limits)
- [Cost analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis)
- [Authentication](/docs/hotchocolate/v16/securing-your-api/authentication)
- [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization)
- [Batching](/docs/hotchocolate/v16/server/batching)
