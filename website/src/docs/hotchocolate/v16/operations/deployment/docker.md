---
title: Deploy with Docker
---

This page shows you how to package and run a standalone Hot Chocolate v16 ASP.NET Core GraphQL server in Docker. Fusion gateway deployment is a separate topic and is not covered here.

A typical containerized Hot Chocolate deployment looks like this:

1. A reverse proxy or load balancer terminates TLS and forwards traffic.
2. One or more ASP.NET Core containers host Hot Chocolate and expose `/graphql` or your chosen endpoint path.
3. Optional backing services provide storage, Redis pub/sub, automatic persisted operations, trusted documents, and telemetry collection.

The Docker image should contain the published application and its runtime dependencies. Connection strings, Redis endpoints, telemetry endpoints, credentials, and endpoint exposure policy belong in runtime configuration.

# Prerequisites

Before you build the image, identify these values for your app:

| Item             | What you need                                                                    |
| ---------------- | -------------------------------------------------------------------------------- |
| Project path     | The `.csproj` that builds your GraphQL server.                                   |
| App DLL          | The published application DLL, for example `YourApp.dll`.                        |
| Target framework | The .NET major version used by the app. Use matching SDK and runtime image tags. |
| GraphQL path     | `/graphql` by default, unless you call `app.MapGraphQL("/some/path")`.           |
| Backing services | Database, Redis, OpenTelemetry collector, or other services your app uses.       |

You also need Docker and Docker Compose:

```bash
docker compose version
```

You can verify that the app publishes before you involve Docker:

```bash
dotnet publish ./src/YourApp/YourApp.csproj -c Release
```

# Build a production image

Start with a multi-stage Dockerfile. Use the SDK image to restore and publish, then copy the published output into the smaller ASP.NET Core runtime image.

```dockerfile
# Dockerfile
# Use image tags that match your app target framework.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first to improve Docker layer caching.
COPY src/YourApp/YourApp.csproj ./src/YourApp/
RUN dotnet restore ./src/YourApp/YourApp.csproj

# Copy the rest of the source and publish the app.
COPY . .
RUN dotnet publish ./src/YourApp/YourApp.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Current .NET runtime images include a non-root app user.
# Keep this when your app does not need privileged ports or extra write access.
USER app

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "YourApp.dll"]
```

Build the image from the repository root:

```bash
docker build -t my-graphql-api .
```

Run it locally:

```bash
docker run --rm -p 8080:8080 my-graphql-api
```

A successful startup includes an ASP.NET Core log line similar to:

```text
Now listening on: http://[::]:8080
```

Add a `.dockerignore` for `bin/`, `obj/`, `.git/`, local secret files, and test artifacts. Do not bake connection strings, Redis endpoints, tokens, certificates, or Nitro credentials into the image.

# Configure the container with environment variables

ASP.NET Core reads environment variables through the standard configuration system. Use this to change runtime settings without rebuilding the image.

```bash
docker run --rm \
  -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ASPNETCORE_HTTP_PORTS=8080 \
  -e ConnectionStrings__CatalogDb="Host=postgres;Database=catalog;Username=app;Password=<secret>" \
  -e Redis__Configuration="redis:6379" \
  -e OTEL_EXPORTER_OTLP_ENDPOINT="http://otel-collector:4317" \
  my-graphql-api
```

Use double underscores to express nested configuration keys. For example, `ConnectionStrings__CatalogDb` maps to `ConnectionStrings:CatalogDb`.

Inside a container, `localhost` means the container itself. Use Compose service names such as `redis:6379` or managed service host names when the app connects to another container or external service.

Read app-specific settings from `builder.Configuration`:

```csharp
using StackExchange.Redis;

var redisConfiguration = builder.Configuration["Redis:Configuration"];

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(redisConfiguration!));
```

Store real passwords and tokens in platform secrets, Docker secrets, Kubernetes secrets, or a managed secret provider. The examples on this page use placeholders.

# Run a local Hot Chocolate stack with Compose

Use Compose to run the GraphQL container with local infrastructure. Include Redis when you use Redis-backed subscriptions, automatic persisted operations, or trusted documents.

```yaml
# compose.yml
services:
  graphql-api:
    build: .
    image: my-graphql-api
    ports:
      - "8080:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ASPNETCORE_HTTP_PORTS: 8080
      Redis__Configuration: redis:6379
      OTEL_EXPORTER_OTLP_ENDPOINT: http://otel-collector:4317
    depends_on:
      - redis

  redis:
    image: redis:7
    ports:
      - "6379:6379"

  # Add your app-specific database when needed.
  # postgres:
  #   image: postgres:17
  #   environment:
  #     POSTGRES_USER: app
  #     POSTGRES_PASSWORD: <secret>
  #     POSTGRES_DB: catalog
  #   volumes:
  #     - postgres-data:/var/lib/postgresql/data
# volumes:
#   postgres-data:
```

Start the stack:

```bash
docker compose up --build
```

`depends_on` controls startup order. It does not prove that Redis, PostgreSQL, or another dependency is ready to accept connections. Keep your application startup, retry, and health-check behavior explicit.

Verify the GraphQL endpoint from the host:

```bash
curl http://localhost:8080/graphql \
  -H "Content-Type: application/json" \
  -d '{"query":"{ __typename }"}'
```

Expected response:

```json
{ "data": { "__typename": "Query" } }
```

If your root query type has a different schema name, the value may differ. The important check is that the container receives the request and returns GraphQL JSON.

# Expose the GraphQL endpoint intentionally

`app.MapGraphQL()` maps the standard GraphQL endpoint at `/graphql`.

```csharp
var app = builder.Build();

app.MapGraphQL();

app.Run();
```

Use an explicit path when your public route requires it:

```csharp
app.MapGraphQL("/api/graphql");
```

Keep the app route and reverse proxy route aligned. If the proxy rewrites `/graphql` to `/api/graphql`, document that rewrite and keep clients, Nitro, persisted operation tooling, and probes pointed at the public path.

`MapGraphQL()` enables these endpoint behaviors on the selected path:

- HTTP GET and POST GraphQL requests.
- Multipart requests when enabled.
- WebSocket GraphQL requests when `app.UseWebSockets()` runs before endpoint mapping.
- Schema SDL download with `?sdl` when schema requests are enabled.
- Nitro for browser requests when the tool is enabled.

If your API requires authentication, apply ASP.NET Core authorization to the endpoint after you configure authentication and authorization services:

```csharp
app.MapGraphQL("/graphql").RequireAuthorization();
```

Verify the selected path:

```bash
curl http://localhost:8080/graphql \
  -H "Content-Type: application/json" \
  -d '{"query":"{ __typename }"}'
```

# Decide whether Nitro should be exposed

Nitro is useful during development and internal operations, but it exposes an interactive GraphQL IDE through a browser request to the GraphQL endpoint. Make that a production decision.

Disable Nitro on the public GraphQL endpoint unless your policy allows it:

```csharp
app.MapGraphQL()
    .WithOptions(o =>
    {
        o.Tool.Enable = app.Environment.IsDevelopment();
        o.EnableSchemaRequests = false;
    });
```

If operators need Nitro, map it separately and protect the route with your normal network controls and authorization policy:

```csharp
app.MapGraphQL("/graphql")
    .WithOptions(o =>
    {
        o.Tool.Enable = false;
        o.EnableSchemaRequests = false;
    });

app.MapNitroApp("/graphql/ui")
    .WithOptions(o => o.GraphQLEndpoint = "/graphql")
    .RequireAuthorization("InternalOperators");
```

After deployment, browse to the public `/graphql` endpoint. It should either open Nitro because you chose to expose it, or return no interactive tool because you disabled it.

# Bind ports, TLS, and forwarded headers correctly

The container listens on an internal port such as `8080`. Docker publishes that internal port to a host port:

```yaml
ports:
  - "8080:8080"
```

The left side is the host port. The right side is the container port configured by `ASPNETCORE_HTTP_PORTS` and `EXPOSE`.

In production, TLS often terminates at a reverse proxy or load balancer. The app may receive plain HTTP inside the container network. Configure forwarded headers when the app needs the original public scheme or host for redirects, authentication callbacks, generated absolute URLs, or security decisions.

```csharp
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedHost |
        ForwardedHeaders.XForwardedProto;
});

var app = builder.Build();

app.UseForwardedHeaders();
app.MapGraphQL();
```

Restrict trusted proxy networks in your production configuration. The exact settings depend on your hosting platform.

Use this proxy checklist:

- Preserve the public GraphQL path or configure an intentional rewrite.
- Forward the original host and scheme when the app relies on them.
- Do not route sibling containers through `localhost`.
- Keep health probes pointed at the container port and the mapped health endpoint.

# Add health and readiness checks

Hot Chocolate v16 eagerly builds the schema and request executor during startup by default. Schema configuration errors fail before the host is ready, which is useful for container readiness.

Add a small health endpoint for Docker, orchestrators, and load balancers:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();

builder
    .AddGraphQL()
    .AddQueryType<Query>();

var app = builder.Build();

app.MapHealthChecks("/healthz");
app.MapGraphQL();

app.Run();
```

Verify it from the host:

```bash
curl -i http://localhost:8080/healthz
```

Expected result:

```text
HTTP/1.1 200 OK
Healthy
```

Keep the basic health endpoint independent of Nitro and GraphQL query execution unless you intentionally want a deeper check. Add database or Redis checks when the container should leave rotation if those dependencies are unavailable.

A Dockerfile `HEALTHCHECK` or Compose `healthcheck` requires an HTTP client inside the runtime image. The ASP.NET Core runtime image may not include `curl` or `wget`. Prefer platform HTTP probes when available, or add a tool to the image deliberately.

```yaml
# Local example only, use this when your runtime image includes wget.
healthcheck:
  test: ["CMD", "wget", "-qO-", "http://localhost:8080/healthz"]
  interval: 10s
  timeout: 3s
  retries: 3
```

# Warm schema and operation caches before traffic

Eager initialization builds the schema and request executor before traffic reaches the app. You can also warm representative operations so parser, document, and operation caches are populated before production traffic arrives.

```csharp
using HotChocolate.Execution;

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddWarmupTask(async (executor, ct) =>
    {
        var request = OperationRequestBuilder.New()
            .SetDocument("query HealthShape { __typename }")
            .SetOperationName("HealthShape")
            .MarkAsWarmupRequest()
            .Build();

        await executor.ExecuteAsync(request, ct);
    });
```

`MarkAsWarmupRequest()` prepares the request without executing resolvers, which avoids mutation side effects and avoids calling data sources during warmup. Include the operation name when real clients send one because it participates in the operation cache key.

Warmup blocks startup for the initial executor. Coordinate container startup deadlines with your schema size and warmup workload. Avoid `LazyInitialization = true` in production unless startup deadlines matter more than first-request latency.

# Scale subscriptions and persisted operations with Redis

In-memory subscriptions work for one app instance and local development. Events are lost on restart and are not shared across replicas. Use Redis when multiple app containers need to deliver events to subscribers connected to different replicas.

Install the Redis packages for the features you use:

```bash
dotnet add package HotChocolate.Subscriptions.Redis
dotnet add package HotChocolate.PersistedOperations.Redis
dotnet add package StackExchange.Redis
```

Register one `IConnectionMultiplexer` and reuse it:

```csharp
using StackExchange.Redis;

var redisConfiguration = builder.Configuration["Redis:Configuration"];

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(redisConfiguration!));

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddSubscriptionType<Subscription>()
    .AddRedisSubscriptions()
    .UseAutomaticPersistedOperationPipeline()
    .AddRedisOperationDocumentStorage();
```

For trusted documents instead of automatic persisted operations, use the persisted operation pipeline:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .UsePersistedOperationPipeline()
    .AddRedisOperationDocumentStorage();
```

Register exactly one subscription provider. Do not combine `AddInMemorySubscriptions()` with `AddRedisSubscriptions()` in the same schema.

In Compose, the app container connects to Redis with the service name:

```yaml
environment:
  Redis__Configuration: redis:6379
```

Use managed Redis or persistent Redis configuration when operation documents must survive Redis restarts. NATS and PostgreSQL subscription providers are also available, see [Subscriptions](/docs/hotchocolate/v16/building-a-schema/subscriptions) for provider details.

# Configure uploads and request limits together

Multipart uploads require the `Upload` scalar and clients must send the `GraphQL-preflight` header.

```csharp
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<FormOptions>(o =>
{
    // 25 MB multipart body limit.
    o.MultipartBodyLengthLimit = 25 * 1000 * 1000;
});

builder.Services
    .AddGraphQLServer(maxAllowedRequestSize: 20 * 1000 * 1000)
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddType<UploadType>();
```

`maxAllowedRequestSize` limits the parsed GraphQL HTTP request payload. The Hot Chocolate default is 20 MB, expressed as `20 * 1000 * 1024` bytes. `FormOptions.MultipartBodyLengthLimit` controls ASP.NET Core multipart body size.

If your API does not accept uploads, disable multipart handling at the endpoint:

```csharp
app.MapGraphQL()
    .WithOptions(o => o.EnableMultipartRequests = false);
```

Align all body-size limits:

| Layer                    | What to configure                                 |
| ------------------------ | ------------------------------------------------- |
| Proxy, ingress, CDN, WAF | Maximum request body size and buffering policy.   |
| Kestrel                  | Maximum request body size when you set one.       |
| ASP.NET Core forms       | `FormOptions.MultipartBodyLengthLimit`.           |
| Hot Chocolate            | `maxAllowedRequestSize` for GraphQL HTTP parsing. |

Oversized requests should fail at a known layer with a predictable status, often `413 Payload Too Large`. A missing `GraphQL-preflight` header is a different failure and should produce a request error instead of a size-limit response.

For large files, prefer presigned upload URLs. Let GraphQL authorize and coordinate the upload, then send the bytes directly to object storage or a dedicated upload endpoint.

Keep execution limits separate from body limits. Hot Chocolate aborts requests after 30 seconds by default. Configure execution timeout with `ModifyRequestOptions` when long-running mutations require a different policy.

# Emit logs, metrics, and traces from containers

Write application logs to stdout and stderr through ASP.NET Core logging. Docker and platform collectors can then read them with standard tooling:

```bash
docker logs graphql-api
```

Add Hot Chocolate instrumentation and OpenTelemetry when you need distributed traces:

```bash
dotnet add package HotChocolate.Diagnostics
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Instrumentation.AspNetCore
dotnet add package OpenTelemetry.Instrumentation.Http
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
```

```csharp
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var serviceName = builder.Configuration["OTEL_SERVICE_NAME"] ?? "my-graphql-api";

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddInstrumentation();

builder.Logging.AddOpenTelemetry(o =>
{
    o.IncludeFormattedMessage = true;
    o.IncludeScopes = true;
    o.ParseStateValues = true;
    o.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName));
});

builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(serviceName))
    .WithTracing(t =>
    {
        t.AddAspNetCoreInstrumentation();
        t.AddHttpClientInstrumentation();
        t.AddHotChocolateInstrumentation();
        t.AddOtlpExporter();
    });
```

Configure the collector endpoint at runtime:

```yaml
environment:
  OTEL_EXPORTER_OTLP_ENDPOINT: http://otel-collector:4317
  OTEL_SERVICE_NAME: my-graphql-api
```

Be deliberate with `ActivityScopes.All` and `RequestDetails.Document`. They add overhead and can send GraphQL documents or sensitive values to telemetry backends.

# Support WebSockets and SSE behind reverse proxies

Register WebSocket middleware before you map GraphQL when clients use WebSocket subscriptions:

```csharp
app.UseWebSockets();

app.MapGraphQL()
    .WithOptions(o =>
    {
        // Default is 5 seconds. Tune this only when proxy policy requires it.
        o.Sockets.KeepAliveInterval = TimeSpan.FromSeconds(12);
    });
```

Configure the reverse proxy or load balancer to support WebSockets:

- Forward `Upgrade` and `Connection` headers.
- Allow the `graphql-transport-ws` subprotocol for new clients.
- Set idle timeouts longer than expected subscription lifetimes.
- Use sticky sessions only while you still use in-memory subscriptions. Prefer Redis for multi-replica deployments.

SSE uses the normal GraphQL HTTP endpoint. There is no separate SSE endpoint. Clients request streaming responses with `Accept: text/event-stream`.

Proxy checklist for SSE:

- Disable response buffering for the GraphQL route.
- Allow long-lived HTTP responses.
- Preserve streaming response headers.
- Do not force `Accept: application/json` for streaming clients.

A working streaming deployment keeps subscription clients connected and delivers events through the proxy without stalls or premature disconnects.

# Production hardening checklist

| Decision                                 | Why it matters                                                                    | Where to configure                                                                    |
| ---------------------------------------- | --------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------- |
| Run as non-root                          | Reduces container privilege.                                                      | Dockerfile `USER app` when supported by the runtime image.                            |
| Use Production environment               | Enables production ASP.NET Core behavior.                                         | `ASPNETCORE_ENVIRONMENT=Production`.                                                  |
| Keep default Hot Chocolate security      | Preserves parser, validation, and execution protections.                          | Do not disable default security unless you provide an explicit replacement.           |
| Control Nitro exposure                   | Prevents accidental public IDE access.                                            | `GraphQLServerOptions.Tool.Enable` or a protected `MapNitroApp` route.                |
| Control schema SDL download              | Meets policies that restrict schema publication.                                  | `GraphQLServerOptions.EnableSchemaRequests`.                                          |
| Configure auth and CORS                  | Protects data and browser access.                                                 | ASP.NET Core middleware and proxy policy, not the Dockerfile.                         |
| Use platform secrets                     | Keeps credentials out of images and source.                                       | Docker secrets, Kubernetes secrets, managed secret providers.                         |
| Set CPU and memory limits                | Warmup, large queries, and uploads need bounded resources.                        | Compose, orchestrator, or cloud platform settings.                                    |
| Use durable shared storage when required | Keeps subscriptions and operation documents working across replicas and restarts. | Redis, filesystem mounts, Azure Blob Storage, or another supported provider.          |
| Rebuild for base image updates           | Applies runtime security patches.                                                 | CI/CD image build pipeline.                                                           |
| Use writable configured paths            | Read-only image layers cannot store generated files.                              | Mounted volumes or external storage for schema exports and persisted-operation files. |

# Troubleshoot Docker deployments

Start with the container state and logs:

```bash
docker compose ps
docker logs graphql-api
```

| Symptom                                       | Likely cause                                                                                                                             | Fix                                                                                                                                   |
| --------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------- |
| Container starts, but the host cannot connect | Wrong internal port, missing `ASPNETCORE_HTTP_PORTS`, wrong `ports:` mapping, app bound to the wrong address, or endpoint path mismatch. | Confirm `ASPNETCORE_HTTP_PORTS=8080`, `ports: "8080:8080"`, and the path passed to `MapGraphQL`.                                      |
| Browser opens Nitro when it should not        | Nitro is enabled on the public GraphQL endpoint.                                                                                         | Set `o.Tool.Enable = false` in production or move Nitro to a protected `MapNitroApp` route.                                           |
| App cannot reach Redis or PostgreSQL          | The app uses `localhost`, the service is not ready, the network is wrong, or the secret is wrong.                                        | Use service DNS such as `redis:6379`, verify Compose networking, add retries or health-aware startup, and check configuration values. |
| 502 or 504 during startup                     | Schema creation, warmup, or dependency checks exceed the platform startup timeout.                                                       | Inspect logs for schema errors, reduce warmup work, or raise startup deadlines.                                                       |
| Long GraphQL request times out                | Hot Chocolate execution timeout, proxy idle timeout, or buffered streaming response.                                                     | Review the 30 second default execution timeout, proxy timeouts, and streaming buffering settings.                                     |
| WebSockets fail behind a proxy                | Missing `UseWebSockets()`, upgrade headers are not forwarded, subprotocol is unsupported, or idle timeout is too short.                  | Register WebSocket middleware before `MapGraphQL`, forward upgrade headers, allow `graphql-transport-ws`, and tune timeouts.          |
| SSE stalls                                    | Proxy buffering, stripped `Accept` header, or idle timeout.                                                                              | Disable buffering for `/graphql`, preserve `Accept: text/event-stream`, and allow long-lived responses.                               |
| Upload returns 413                            | Proxy, Kestrel, form, or Hot Chocolate body limits are not aligned.                                                                      | Align all request-size limits and identify the layer that emits the response.                                                         |
| Upload returns 400 before size checks         | Missing multipart preflight header.                                                                                                      | Send `GraphQL-preflight: 1` from upload clients.                                                                                      |
| First request is slow                         | Warmup task is missing, the representative operation was not warmed, or lazy initialization is enabled.                                  | Add `AddWarmupTask`, include operation names, and keep eager initialization enabled for production.                                   |
| Persisted operation is missing after restart  | In-memory storage was used, Redis is not shared, Redis data expired, or filesystem storage path is not mounted.                          | Use shared Redis, mounted filesystem storage, or another durable operation document storage.                                          |
| Health check fails inside the container       | Runtime image lacks `curl` or `wget`, health endpoint requires auth, or `/healthz` is not mapped on the expected port.                   | Prefer platform HTTP probes, keep `/healthz` unauthenticated for probes, and verify the container port.                               |

# Next steps

- [Endpoints](/docs/hotchocolate/v16/server/endpoints) for `MapGraphQL`, Nitro, schema downloads, and endpoint options.
- [HTTP Transport](/docs/hotchocolate/v16/server/http-transport) for content negotiation and streaming formats.
- [Warmup](/docs/hotchocolate/v16/server/warmup) for eager initialization and request cache warmup.
- [Files](/docs/hotchocolate/v16/server/files) for the `Upload` scalar and multipart requests.
- [Instrumentation](/docs/hotchocolate/v16/server/instrumentation) for OpenTelemetry setup and tracing options.
- [Request Limits](/docs/hotchocolate/v16/securing-your-api/request-limits) for parser, validation, and execution limits.
- [Subscriptions](/docs/hotchocolate/v16/building-a-schema/subscriptions) for WebSocket, SSE, and subscription providers.
- [Persisted operations](/docs/hotchocolate/v16/performance/trusted-documents) for trusted documents.
- [Automatic persisted operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations) for APQ storage and request flow.
