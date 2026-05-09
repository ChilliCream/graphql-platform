---
title: Deploy Hot Chocolate on AWS
---

You deploy Hot Chocolate to AWS as an ASP.NET Core service. AWS hosts, routes, secures, and observes the process; Hot Chocolate owns the GraphQL endpoint, transports, execution pipeline, subscriptions, persisted operations, and GraphQL-specific security behavior.

A typical production path looks like this:

```text
Client or CloudFront
  -> ALB or API Gateway
  -> ECS task, EKS pod, VM, or managed ASP.NET Core host
  -> Hot Chocolate endpoint at /graphql
  -> databases, ElastiCache Redis, and downstream services
```

This page covers standalone Hot Chocolate v16 server deployments. Fusion gateway and subgraph operations are out of scope. Use the AWS documentation for creating VPCs, clusters, target groups, certificates, IAM roles, CloudFront distributions, API Gateway stages, and Kubernetes objects.

# Prerequisites

Before you apply the AWS guidance, make sure you have:

- A working Hot Chocolate v16 ASP.NET Core server.
- A .NET runtime target or a container image for the service.
- One public GraphQL endpoint path, usually `/graphql`.
- A production authentication and authorization plan.
- A decision on subscriptions, SSE, incremental delivery, uploads, batching, trusted documents, and APQ.
- AWS DNS, TLS certificate, network, and deployment target decisions.
- ElastiCache Redis connection details when subscriptions, trusted documents, or APQ must work across instances or survive restarts.
- A telemetry route, for example OTLP to an AWS Distro for OpenTelemetry Collector.
- Runtime configuration for secrets and environment-specific settings.

# Choose an AWS host by GraphQL behavior

Choose the host by the transports your GraphQL clients need, not only by how you package the app.

| Hosting model                                      | Best fit                                                                                                                       | WebSocket and SSE fit                                                                                                                               | Scale-out notes                                                                                                              | Hot Chocolate caveats                                                                                                                               |
| -------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------ | --------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------- |
| ECS on Fargate                                     | Containerized ASP.NET Core services where your team wants managed compute without Kubernetes.                                  | Works well behind an ALB when the listener, target group, and security groups preserve WebSocket upgrades and long HTTP responses.                  | Scale tasks horizontally. Use health checks and deployment grace periods. Put Redis-backed features in ElastiCache.          | Recommended default for many AWS Hot Chocolate APIs. Keep Nitro, schema download, and upload limits explicit.                                       |
| EKS                                                | Teams that already operate Kubernetes and need ingress controllers, HPA, network policy, sidecars, or daemonsets.              | Depends on your ingress. Test WebSocket upgrades, SSE, `multipart/mixed`, and JSON Lines through the full path.                                     | Map readiness and liveness probes to ASP.NET Core health endpoints. Use disruption budgets for subscription-heavy workloads. | Do not rely on pod-local memory for subscriptions, APQ, or trusted documents.                                                                       |
| Elastic Beanstalk, App Runner, or VM-style hosting | Simpler ASP.NET Core deployments where the managed front end supports your required request sizes, idle timeouts, and headers. | Verify platform-specific WebSocket and streaming behavior before using subscriptions or incremental delivery.                                       | Scale instances behind a managed load balancer. Keep configuration in platform settings or secrets.                          | Good for HTTP query and mutation APIs. Re-test long-lived connections after platform changes.                                                       |
| Lambda with API Gateway                            | Short-lived queries and mutations when you already adapt ASP.NET Core to Lambda.                                               | Not a default fit for Hot Chocolate subscriptions or long-lived SSE. API Gateway WebSocket APIs use a different connection-management architecture. | Cold starts, body limits, and integration timeouts matter.                                                                   | Do not choose Lambda to host normal Hot Chocolate subscription traffic unless an AWS-specific architecture owns connection state outside this page. |

For internet-facing APIs, prefer a long-running ASP.NET Core host when clients use subscriptions, SSE, incremental delivery, file uploads, or operation warmup. Raising AWS timeouts is rarely the right first response to slow GraphQL operations. Start with request limits, cost analysis, pagination, DataLoader usage, and resolver optimization.

# Start from a production Hot Chocolate endpoint

Use one canonical endpoint and make production behavior explicit. This baseline keeps development tools private, requires deliberate GET and upload behavior, and keeps WebSocket settings close to the GraphQL server configuration.

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

// Keep this only when WebSocket subscriptions are supported by the AWS path.
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

Verify the deployed endpoint:

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

`builder.AddGraphQL(...)` uses the v16 ASP.NET Core hosting style. Keep the default security policy enabled unless you replace it deliberately. The default policy includes production-oriented protections such as cost analysis, non-development introspection behavior, and field-cycle validation.

Place `UseCors`, `UseAuthentication`, `UseAuthorization`, and `UseWebSockets` before `MapGraphQL`. Hot Chocolate authorization depends on the ASP.NET Core user, browser clients depend on CORS, and WebSocket subscriptions depend on ASP.NET Core WebSocket middleware.

# Expose only the paths you intend to support

`MapGraphQL("/graphql")` maps a combined endpoint. Route that path through AWS intentionally.

| Path                                      | What it does                                                                                                                                         | Production guidance                                                                    |
| ----------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------- |
| `/graphql`                                | HTTP POST, HTTP GET when enabled, multipart when enabled, WebSocket upgrades when `UseWebSockets()` is registered, Nitro browser entry when enabled. | Keep as the public client path. Disable Nitro in production.                           |
| `/graphql?sdl` and schema file paths      | Schema SDL download controlled by `EnableSchemaRequests` and `EnableSchemaFileSupport`.                                                              | Disable publicly unless schema download is part of your contract.                      |
| `/graphql/schema` or a custom schema path | SDL endpoint when you use `MapGraphQLSchema`.                                                                                                        | Put behind internal routing or authentication if you expose it.                        |
| `/graphql/ws`                             | Optional split WebSocket endpoint with `MapGraphQLWebSocket`.                                                                                        | Use when AWS routes WebSocket traffic separately from HTTP traffic.                    |
| `/graphql/persisted/{operationId}`        | Persisted-operation HTTP endpoint when you map it with `MapGraphQLPersistedOperations`.                                                              | Use when clients execute trusted documents by URL. Configure CDN cache keys carefully. |
| `/health/live` and `/health/ready`        | ASP.NET Core health endpoints outside GraphQL.                                                                                                       | Use for ALB target groups, ECS health checks, and EKS probes. Do not probe `/graphql`. |

Use split endpoints only when you need separate paths or policies:

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

Keep health checks off the GraphQL endpoint. Probes should not execute GraphQL documents, require CORS headers, or depend on client authentication flows.

# Put ALB, API Gateway, or CloudFront in front of GraphQL

Hot Chocolate chooses response formats and transports from HTTP methods and headers. Your AWS edge must preserve those methods and headers.

| Client behavior                   | Required request shape                                                                               | AWS checks                                                                                                 |
| --------------------------------- | ---------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------- |
| Standard queries and mutations    | `POST`, `Content-Type: application/json`, `Accept: application/graphql-response+json`.               | Forward body and `Accept`. Do not cache personalized POST responses.                                       |
| Cacheable queries or APQ          | `GET` with query string or `extensions`. Hot Chocolate should allow only configured operation types. | Forward query strings and headers that affect auth, tenant, locale, and content negotiation.               |
| Multipart upload                  | `POST`, multipart form content type, `GraphQL-preflight: 1`.                                         | Preserve the preflight header and align request body limits at every layer.                                |
| Incremental delivery and batching | `Accept: multipart/mixed`, `text/event-stream`, or `application/jsonl`.                              | Disable buffering where your proxy supports it and test streaming through the full path.                   |
| WebSocket subscriptions           | `Upgrade: websocket`, `Connection: Upgrade`, and a GraphQL WebSocket subprotocol.                    | Route to an endpoint with `UseWebSockets()` and proxy upgrade support. Align idle timeout and keep-alives. |
| Auth and CORS                     | `Authorization`, cookies when used, `Origin`, `Access-Control-Request-*`.                            | Forward headers that ASP.NET Core authentication and CORS require. Never cache across auth boundaries.     |

ALB is a common fit for long-running ASP.NET Core services, WebSockets, and normal HTTP GraphQL. Set the ALB idle timeout above your expected quiet periods or keep Hot Chocolate keep-alive pings below that timeout. The default Hot Chocolate WebSocket keep-alive interval is 5 seconds. The sample uses 30 seconds as an example value you should align with your path.

API Gateway can be valid for simple HTTP GraphQL, especially short queries and mutations. Before you choose it for subscriptions, SSE, incremental delivery, batching, or uploads, confirm the current body size limits, integration timeout, streaming support, and WebSocket API architecture in the AWS documentation.

CloudFront can front GraphQL, but caching GraphQL responses is safe only when the cache key includes every value that changes the response. For authenticated APIs, avoid caching personalized responses unless your cache policy varies by authorization context and you have tested tenant isolation. GET persisted queries are the most common cacheable shape.

Verify transport preservation through AWS, not only against Kestrel:

```bash
curl -i https://api.example.com/graphql \
  -H 'Content-Type: application/json' \
  -H 'Accept: application/graphql-response+json' \
  --data '{ "query": "{ __typename }" }'
```

Expected headers include a GraphQL JSON content type, and the body should contain:

```json
{ "data": { "__typename": "Query" } }
```

For streaming, send the same `Accept` header your client uses and confirm the response arrives incrementally. A response that appears only after the whole operation completes usually points to proxy buffering or an unsupported integration path.

# Run subscriptions on AWS

In-memory subscriptions work for one instance and local development. On AWS, a subscriber may connect to one task while a mutation publishes on another. Use ElastiCache Redis when events must reach every instance.

Install the Redis provider:

<PackageInstallation packageName="HotChocolate.Subscriptions.Redis" />

Register one shared `IConnectionMultiplexer` and use Redis subscriptions:

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

Enable the WebSocket transport when clients use WebSocket subscriptions:

```csharp
app.UseWebSockets();
app.MapGraphQL("/graphql");
```

The publishing code does not change when you switch providers:

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

Use a `TopicPrefix` when multiple environments, services, preview stacks, or tests share one Redis deployment. Configure TLS, authentication, security groups, and Redis endpoints through AWS-managed configuration or secrets. SSE subscriptions do not require `UseWebSockets()`, but they still hold long-running HTTP responses and need timeout and buffering tests.

# Store trusted documents and APQ in Redis when you scale out

Avoid in-memory operation document storage on multi-instance AWS deployments. Requests may route to any task or pod, and deployments restart instances. Redis gives every instance access to the same operation documents.

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

APQ first asks by hash. If Redis does not contain the document, Hot Chocolate returns a persisted-query-not-found error and the client retries with the full document. After Redis stores it, any instance can execute the hash-only request.

Align the hash provider with your clients. Hot Chocolate uses MD5 by default for persisted operation document hashes unless you configure another provider, such as SHA-256.

# Configure health checks and startup readiness

Hot Chocolate v16 builds the schema and request executor eagerly during startup by default. This works well with ALB target groups, ECS health checks, and EKS readiness probes because the app does not become ready until the executor is initialized.

Add health endpoints outside GraphQL:

```csharp
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
```

Warm frequently used operations before traffic reaches a new instance:

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

Avoid `LazyInitialization` in production unless you have chosen that cold-start trade-off. Lazy initialization moves schema construction to the first request and can make new targets look healthy before they can answer GraphQL quickly.

# Send Hot Chocolate telemetry to AWS observability

Hot Chocolate emits OpenTelemetry spans through its diagnostics package. Export OTLP to an ADOT Collector running as an ECS sidecar, EKS daemonset, EKS sidecar, or managed collector, then route to CloudWatch, X-Ray-compatible backends, or another tracing system.

Install the packages:

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

Set the OTLP endpoint with environment configuration, for example `OTEL_EXPORTER_OTLP_ENDPOINT=http://adot-collector:4317` in ECS or EKS. Expected result: GraphQL spans appear in your AWS telemetry pipeline with attributes such as operation type, operation name, document hash, and trusted document id when available.

Keep span cardinality low. Enable document text, variables, detailed request data, or field-level spans only for targeted debugging because they add overhead and can expose sensitive data.

# Size and scale the service safely

Use Hot Chocolate controls before you add more AWS capacity. These controls reduce per-instance risk and make scaling behavior more predictable.

| Control               | Example                                                             | Why it matters on AWS                                                                                      |
| --------------------- | ------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------- |
| Execution concurrency | `options.MaxConcurrentExecutions = 64`                              | Caps concurrent GraphQL executions per instance. Avoid `null` or `0` unless you deliberately want no gate. |
| Execution timeout     | `options.ExecutionTimeout = TimeSpan.FromSeconds(10)`               | Keeps resolver work below client, ALB, API Gateway, and downstream timeouts.                               |
| Parser limits         | `ModifyParserOptions`                                               | Rejects abusive documents before validation and execution.                                                 |
| Validation limits     | depth, fragment visits, field merge comparisons, field-cycle limits | Reduces CPU and memory pressure from adversarial queries.                                                  |
| Cost analysis         | default security policy or explicit cost settings                   | Protects public endpoints from expensive but valid documents.                                              |
| Pagination limits     | connection and collection settings                                  | Prevents large result sets from turning into memory and downstream pressure.                               |
| Batching limits       | `Batching` and `MaxBatchSize`                                       | Keeps batch requests bounded and tests streaming response support.                                         |
| Persisted operations  | trusted documents or APQ                                            | Reduces payload size and can limit public clients to known operations.                                     |

Example tuning fragment:

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

Scale on the signals that match your workload: CPU, memory, request latency, queued requests, downstream saturation, Redis pub/sub throughput, active subscription connection count, and error rate. For subscription-heavy services, connection count and reconnect storms during deployment can matter as much as CPU.

# Manage auth, CORS, and secrets for AWS environments

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

Store Redis, database, auth, signing, and API secrets in AWS Secrets Manager or Systems Manager Parameter Store, then inject them as environment variables or configuration providers. Do not hard-code Redis endpoints, signing keys, client secrets, or authority values in source code or images. Rotate secrets without rebuilding images where possible.

If you use cookies, review `SameSite`, `Secure`, forwarded headers, CloudFront header forwarding, and cache policies. Configure CORS for browser clients. Do not broaden production CORS rules because Nitro needs access. Keep Nitro disabled publicly or protect it with network and authorization controls.

# Handle uploads and body limits

Prefer presigned S3 upload URLs for large files. Let GraphQL authorize and create upload metadata, then let the client upload bytes directly to S3.

| Approach         | Choose it when                                                               | AWS and Hot Chocolate checks                                                                                                                  |
| ---------------- | ---------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------- |
| Presigned S3 URL | Files are large, uploads are frequent, or clients can upload directly to S3. | GraphQL mutation returns URL and metadata. S3 receives file bytes. Hot Chocolate request size stays small.                                    |
| `Upload` scalar  | Files are small and must flow through the GraphQL server.                    | Register `UploadType`, enable multipart, require `GraphQL-preflight: 1`, and align AWS, Kestrel, ASP.NET Core form, and Hot Chocolate limits. |

Configure multipart only when your schema uses uploads:

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

A `413 Payload Too Large` can happen before Hot Chocolate sees the request. Align the AWS edge or proxy body limit, Kestrel request body size, `FormOptions.MultipartBodyLengthLimit`, `AddGraphQL(maxAllowedRequestSize)`, and application validation.

# Troubleshoot AWS deployment failures

Use the symptom to inspect both the Hot Chocolate setting and the AWS layer.

| Symptom                                            | Likely Hot Chocolate cause                                                                  | Likely AWS or proxy cause                                                                               | Next check                                                                                       |
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

# Next steps

Use these pages for the Hot Chocolate details behind each AWS decision:

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
