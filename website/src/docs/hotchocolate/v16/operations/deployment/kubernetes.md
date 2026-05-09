---
title: Deploy to Kubernetes
---

This page shows you how to deploy and operate one Hot Chocolate v16 ASP.NET Core GraphQL server on Kubernetes. It focuses on the Kubernetes decisions that change GraphQL behavior: endpoint paths, startup readiness, request limits, scaling, subscriptions, persisted operations, streaming transports, graceful shutdown, secrets, and telemetry.

Fusion gateway deployment, composition, and source schema routing are out of scope for this page. If you deploy Fusion, use the Fusion documentation for gateway-specific topology and composition guidance.

# Check what you need before you deploy

Start with an app and image that already work outside the cluster.

You need:

| Item                | What to know                                                                         |
| ------------------- | ------------------------------------------------------------------------------------ |
| Image               | The full image name and tag that the cluster can pull.                               |
| Container port      | The port ASP.NET Core listens on, for example `8080`.                                |
| GraphQL path        | `/graphql` by default, unless you call `app.MapGraphQL("/some/path")`.               |
| Public route        | The external host and path, for example `https://api.example.com/graphql`.           |
| Enabled transports  | HTTP POST, optional GET, WebSocket subscriptions, SSE, multipart uploads, batching.  |
| Shared dependencies | Redis, databases, OpenTelemetry collector, external secret provider, object storage. |
| Replica plan        | Use one replica until subscription and persisted-operation state are shared.         |

Verify the app locally before Kubernetes is involved:

```bash
curl http://localhost:8080/graphql \
  -H 'Content-Type: application/json' \
  --data '{ "query": "query DeploymentSmoke { __typename }", "operationName": "DeploymentSmoke" }'
```

Expected response:

```json
{ "data": { "__typename": "Query" } }
```

Your root query type name may differ if you renamed it.

Smoke test the image with the same port the Deployment will use:

```bash
docker run --rm -p 8080:8080 \
  -e ASPNETCORE_URLS=http://+:8080 \
  ghcr.io/example/catalog-api:1.0.0
```

Then run the same `curl` command against `http://localhost:8080/graphql`.

# Deploy a Hot Chocolate server to Kubernetes

Apply a Deployment, Service, and Ingress that match your image, port, and GraphQL path. Start with one replica. Add replicas after you decide how to share subscription and persisted-operation state.

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: catalog-api
  labels:
    app: catalog-api
spec:
  replicas: 1
  selector:
    matchLabels:
      app: catalog-api
  template:
    metadata:
      labels:
        app: catalog-api
    spec:
      containers:
        - name: catalog-api
          image: ghcr.io/example/catalog-api:1.0.0
          imagePullPolicy: IfNotPresent
          ports:
            - name: http
              containerPort: 8080
          env:
            - name: ASPNETCORE_ENVIRONMENT
              value: Production
            - name: ASPNETCORE_URLS
              value: http://+:8080
          resources:
            requests:
              cpu: 250m
              memory: 256Mi
            limits:
              cpu: "1"
              memory: 512Mi
---
apiVersion: v1
kind: Service
metadata:
  name: catalog-api
spec:
  selector:
    app: catalog-api
  ports:
    - name: http
      port: 80
      targetPort: http
---
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: catalog-api
spec:
  ingressClassName: nginx
  rules:
    - host: api.example.com
      http:
        paths:
          - path: /graphql
            pathType: Prefix
            backend:
              service:
                name: catalog-api
                port:
                  name: http
```

Apply the manifest:

```bash
kubectl apply -f catalog-api.yaml
kubectl rollout status deployment/catalog-api
kubectl get pods -l app=catalog-api
```

Verify the public endpoint:

```bash
curl https://api.example.com/graphql \
  -H 'Content-Type: application/json' \
  --data '{ "query": "query DeploymentSmoke { __typename }", "operationName": "DeploymentSmoke" }'
```

Expected response:

```json
{ "data": { "__typename": "Query" } }
```

# Configure the GraphQL endpoint that Kubernetes exposes

The manifests above assume that the app listens on port `8080` and maps GraphQL at `/graphql`.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQL(maxAllowedRequestSize: 1 * 1000 * 1000)
    .AddQueryType<Query>()
    .ModifyServerOptions(options =>
    {
        options.Tool.Enable = false;
        options.EnableSchemaRequests = false;
        options.EnableGetRequests = false;
        options.EnableMultipartRequests = false;
        options.MaxConcurrentExecutions = 64;
    });

var app = builder.Build();

app.MapGraphQL("/graphql");

app.Run();
```

`builder.AddGraphQL(...)` is the v16 ASP.NET Core hosting entry point. If your project already uses `builder.Services.AddGraphQLServer(...)`, keep that convention. The `maxAllowedRequestSize` argument limits the GraphQL HTTP request body before parsing.

Keep default Hot Chocolate security enabled unless you replace it deliberately. The default policy adds cost analysis, disables introspection outside development, and adds the field-cycle validation rule.

Enable WebSockets only when clients use WebSocket subscriptions:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddSubscriptionType<Subscription>()
    .ModifyServerOptions(options =>
    {
        options.Sockets.ConnectionInitializationTimeout = TimeSpan.FromSeconds(30);
        options.Sockets.KeepAliveInterval = TimeSpan.FromSeconds(12);
    });

var app = builder.Build();

app.UseWebSockets();
app.MapGraphQL("/graphql");
```

`UseWebSockets()` must run before `MapGraphQL()`. Browser visits should not expose Nitro on a public production endpoint unless you intentionally enable it or mount Nitro on a protected route.

See [Endpoints](/docs/hotchocolate/v16/server/endpoints) for `MapGraphQL`, endpoint options, Nitro, schema downloads, and split endpoint mapping.

# Publish the correct external path and base URL

Hot Chocolate maps an application path. Ingress maps an external path to the Service. Keep those two paths aligned.

## Route `/graphql` without a rewrite

Use this when the public path and application path are both `/graphql`.

```yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: catalog-api
spec:
  ingressClassName: nginx
  rules:
    - host: api.example.com
      http:
        paths:
          - path: /graphql
            pathType: Prefix
            backend:
              service:
                name: catalog-api
                port:
                  name: http
```

The app maps the same path:

```csharp
app.MapGraphQL("/graphql");
```

## Expose `/api/graphql` and rewrite to `/graphql`

This NGINX Ingress example exposes `/api/graphql` externally and sends `/graphql` to the app.

```yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: catalog-api
  annotations:
    nginx.ingress.kubernetes.io/rewrite-target: /$2
spec:
  ingressClassName: nginx
  rules:
    - host: api.example.com
      http:
        paths:
          - path: /api(/|$)(.*)
            pathType: ImplementationSpecific
            backend:
              service:
                name: catalog-api
                port:
                  name: http
```

The app still maps `/graphql`:

```csharp
app.MapGraphQL("/graphql");
```

Clients call:

```bash
curl https://api.example.com/api/graphql \
  -H 'Content-Type: application/json' \
  --data '{ "query": "{ __typename }" }'
```

WebSocket clients use the matching public path: `wss://api.example.com/api/graphql`.

## Preserve a path base in the app

If the ingress preserves `/api`, configure ASP.NET Core path base and keep the Hot Chocolate endpoint relative to it.

```csharp
var app = builder.Build();

app.UsePathBase("/api");
app.MapGraphQL("/graphql");
```

With this setup, the external path is `/api/graphql` and the endpoint path inside the app is `/graphql` after the path base is removed.

## Forward the public scheme and host

When TLS terminates at the ingress, Kestrel sees HTTP from the proxy unless you process forwarded headers. Configure this when auth callbacks, redirects, Nitro endpoint metadata, generated links, or logs need the public scheme and host.

```csharp
using Microsoft.AspNetCore.HttpOverrides;

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
        | ForwardedHeaders.XForwardedHost
        | ForwardedHeaders.XForwardedProto;

    // Configure KnownProxies or KnownNetworks for your ingress in production.
});

var app = builder.Build();

app.UseForwardedHeaders();
app.MapGraphQL("/graphql");
```

# Add startup, readiness, and liveness probes

Hot Chocolate v16 builds the schema eagerly by default. Startup warmup tasks also block startup. That behavior works well with Kubernetes because a pod does not accept traffic until the host starts successfully.

Use ASP.NET Core health checks for probes. Do not probe `/graphql` with arbitrary POST requests unless your app owns a cheap health operation with no resolver side effects.

```csharp
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddHealthChecks()
    .AddCheck(
        "self",
        () => HealthCheckResult.Healthy(),
        tags: new[] { "live" });

builder
    .AddGraphQL()
    .AddQueryType<Query>();

var app = builder.Build();

app.MapHealthChecks(
    "/health/live",
    new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("live")
    });

app.MapHealthChecks("/health/ready");
app.MapGraphQL("/graphql");

app.Run();
```

Add dependency checks, such as database or Redis checks, to readiness when the pod must leave rotation if those dependencies are unavailable. Keep liveness narrow. Liveness should detect a stuck process, not transient database failure.

```yaml
containers:
  - name: catalog-api
    image: ghcr.io/example/catalog-api:1.0.0
    ports:
      - name: http
        containerPort: 8080
    startupProbe:
      httpGet:
        path: /health/ready
        port: http
      periodSeconds: 5
      failureThreshold: 24
    readinessProbe:
      httpGet:
        path: /health/ready
        port: http
      periodSeconds: 10
      timeoutSeconds: 2
      failureThreshold: 3
    livenessProbe:
      httpGet:
        path: /health/live
        port: http
      periodSeconds: 20
      timeoutSeconds: 2
      failureThreshold: 3
```

Expected rollout behavior:

- Schema creation failures prevent startup and keep the pod unready.
- Warmup failures prevent startup when the warmup task throws.
- Rolling updates wait for a ready pod before sending GraphQL traffic.
- Liveness does not restart a pod because Redis had a brief outage, unless you intentionally add Redis to liveness.

See [Warmup](/docs/hotchocolate/v16/server/warmup) for startup initialization details.

# Warm the schema and operation caches before traffic

Eager schema initialization is the default in v16. Add warmup tasks for representative operations so parsing, validation, and operation preparation happen before the first live request.

```csharp
using HotChocolate.Execution;

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddWarmupTask(async (executor, cancellationToken) =>
    {
        var request = OperationRequestBuilder.New()
            .SetDocument("query ProductListWarmup { products(first: 10) { nodes { id name } } }")
            .SetOperationName("ProductListWarmup")
            .MarkAsWarmupRequest()
            .Build();

        await executor.ExecuteAsync(request, cancellationToken);
    });
```

`MarkAsWarmupRequest()` prepares the operation without executing resolvers. That avoids mutation side effects and avoids calling downstream services during warmup. It also bypasses persisted-operation enforcement, so you can warm the executor with known documents even when production clients use trusted documents.

Include the operation name when clients send one. The operation name participates in the operation cache key.

Avoid `LazyInitialization = true` for production rollouts unless you accept first-request schema latency and readiness that does not include schema creation.

Expected behavior: startup completes after schema creation and warmup. A bad schema or invalid warmup document fails during rollout instead of on the first production request.

# Set resource and request limits before autoscaling

Kubernetes CPU and memory limits protect the node. Hot Chocolate limits protect the GraphQL execution engine inside each pod. Configure both before you add replicas.

```yaml
resources:
  requests:
    cpu: 250m
    memory: 256Mi
  limits:
    cpu: "1"
    memory: 512Mi
```

Use GraphQL limits that match your traffic and downstream services:

```csharp
builder
    .AddGraphQL(maxAllowedRequestSize: 1_048_576)
    .AddQueryType<Query>()
    .ModifyParserOptions(options =>
    {
        options.MaxAllowedFields = 1024;
        options.MaxAllowedDirectives = 4;
        options.MaxAllowedRecursionDepth = 100;
    })
    .ModifyRequestOptions(options =>
    {
        options.ExecutionTimeout = TimeSpan.FromSeconds(10);
    })
    .ModifyPagingOptions(options =>
    {
        options.MaxPageSize = 50;
        options.RequirePagingBoundaries = true;
    })
    .ModifyCostOptions(options =>
    {
        options.MaxFieldCost = 1_000;
        options.MaxTypeCost = 1_000;
        options.EnforceCostLimits = true;
    })
    .ModifyServerOptions(options =>
    {
        options.Batching = AllowedBatching.None;
        options.MaxBatchSize = 50;
        options.MaxConcurrentExecutions = 64;
    })
    .AddMaxExecutionDepthRule(12)
    .SetMaxAllowedValidationErrors(5)
    .SetMaxAllowedFieldMergeComparisons(50_000);
```

Important defaults to account for:

| Limit                     |                                     Default | Kubernetes guidance                                                         |
| ------------------------- | ------------------------------------------: | --------------------------------------------------------------------------- |
| GraphQL request body size |                    `20 * 1000 * 1024` bytes | Lower it for APIs that do not need large request bodies.                    |
| Execution timeout         |                                  30 seconds | Set an explicit timeout that fits ingress and downstream timeouts.          |
| Max concurrent executions |                                        `64` | Treat this as per-pod backpressure. `null` disables the gate.               |
| Batching                  |                      `AllowedBatching.None` | Enable only for known clients, then set `MaxBatchSize`.                     |
| `MaxBatchSize`            |                                      `1024` | Lower it if one HTTP request can fan out too far.                           |
| Cost limits               | `MaxFieldCost = 1000`, `MaxTypeCost = 1000` | Measure real operations before increasing limits.                           |
| Cursor `MaxPageSize`      |                                        `50` | Keep it conservative because cost analysis uses it for list size estimates. |

Measure representative operations with the cost header:

```bash
curl https://api.example.com/graphql \
  -H 'Content-Type: application/json' \
  -H 'GraphQL-Cost: report' \
  --data '{ "query": "query CostProbe { products(first: 50) { nodes { id name } } }" }'
```

Use `GraphQL-Cost: validate` to validate cost behavior while tuning. Expensive requests should fail predictably with GraphQL validation or cost errors instead of exhausting a pod.

See [Request Limits](/docs/hotchocolate/v16/securing-your-api/request-limits), [Cost Analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis), [Batching](/docs/hotchocolate/v16/server/batching), and [Options](/docs/hotchocolate/v16/api-reference/options).

# Configure request and upload limits across every layer

Four layers can reject large requests:

| Layer              | Controls                                                     |
| ------------------ | ------------------------------------------------------------ |
| Ingress, CDN, WAF  | Public request body size and buffering.                      |
| Kestrel            | Host-level request body size if you configure one.           |
| ASP.NET Core forms | `FormOptions.MultipartBodyLengthLimit` for multipart bodies. |
| Hot Chocolate      | `maxAllowedRequestSize` for GraphQL HTTP request parsing.    |

Set the Hot Chocolate GraphQL body limit:

```csharp
builder
    .AddGraphQL(maxAllowedRequestSize: 1_048_576)
    .AddQueryType<Query>();
```

If your API accepts multipart uploads, align form and ingress limits:

```csharp
using Microsoft.AspNetCore.Http.Features;

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 25 * 1000 * 1000;
});

builder
    .AddGraphQL(maxAllowedRequestSize: 1_048_576)
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddType<UploadType>()
    .ModifyServerOptions(options =>
    {
        options.EnableMultipartRequests = true;
        options.EnforceMultipartRequestsPreflightHeader = true;
    });
```

NGINX Ingress uses `proxy-body-size` for request body limits:

```yaml
metadata:
  annotations:
    nginx.ingress.kubernetes.io/proxy-body-size: "25m"
```

Upload clients must send the multipart preflight header:

```bash
curl https://api.example.com/graphql \
  -H 'GraphQL-preflight: 1' \
  -F operations='{ "query": "mutation ($file: Upload!) { uploadFile(file: $file) }", "variables": { "file": null } }' \
  -F map='{ "0": ["variables.file"] }' \
  -F 0=@file.txt
```

If the response is `413 Payload Too Large`, identify which layer generated it. Ingress can reject before Hot Chocolate sees the request. For large user files, prefer presigned upload URLs and use GraphQL to authorize the upload and store metadata.

See [Files](/docs/hotchocolate/v16/server/files) for upload-specific APIs.

# Scale replicas safely

Queries and mutations scale horizontally when your resolvers and application services are stateless or use shared external state. Some GraphQL features have pod-local state by default.

| Feature                                | One replica                                                               | Multiple replicas                                                                            |
| -------------------------------------- | ------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------- |
| Queries and mutations                  | Usually safe when app services are stateless.                             | No sticky sessions required for standard HTTP requests.                                      |
| In-memory subscriptions                | Works only for clients connected to the same pod that receives the event. | Use Redis, NATS, or Postgres subscriptions. Sticky sessions do not share events across pods. |
| WebSocket connections                  | Connected to one pod.                                                     | Clients reconnect and resubscribe when pods roll. Connections are not movable.               |
| SSE streams                            | Connected to one pod over HTTP.                                           | Configure ingress timeouts and expect reconnects during pod termination.                     |
| APQ with in-memory storage             | Hits only the pod that learned the operation.                             | Use Redis or another shared operation document storage.                                      |
| Trusted documents on filesystem        | Works if files are present.                                               | Bake identical files into the image or mount the same read-only storage in every pod.        |
| Schema, document, and operation caches | Warm per executor instance.                                               | Each pod warms its own caches.                                                               |
| DataLoader                             | Per request.                                                              | No shared state required.                                                                    |

After shared state is configured, add an HPA:

```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: catalog-api
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: catalog-api
  minReplicas: 2
  maxReplicas: 10
  metrics:
    - type: Resource
      resource:
        name: cpu
        target:
          type: Utilization
          averageUtilization: 70
```

Add a PodDisruptionBudget when voluntary disruptions should keep at least one pod available:

```yaml
apiVersion: policy/v1
kind: PodDisruptionBudget
metadata:
  name: catalog-api
spec:
  minAvailable: 1
  selector:
    matchLabels:
      app: catalog-api
```

Scale based on observed latency, CPU, memory, and concurrency saturation. Raising replica count does not fix an unbounded operation. Tune per-pod limits first.

# Use Redis for subscriptions in multi-pod deployments

In-memory subscriptions are for local development and single-pod deployments. In Kubernetes, Redis lets an event published on one pod reach subscribers connected to another pod. Redis carries events, not connections. WebSocket and SSE clients still reconnect when their pod terminates.

Install packages:

```bash
dotnet add package HotChocolate.Subscriptions.Redis
dotnet add package StackExchange.Redis
```

Store the Redis connection string in a Secret:

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: catalog-api-secrets
stringData:
  ConnectionStrings__Redis: redis:6379
```

Load it into the pod:

```yaml
env:
  - name: ConnectionStrings__Redis
    valueFrom:
      secretKeyRef:
        name: catalog-api-secrets
        key: ConnectionStrings__Redis
```

Register one Redis connection and one subscription provider:

```csharp
using HotChocolate.Subscriptions;
using StackExchange.Redis;

var redisConnection = builder.Configuration.GetConnectionString("Redis");

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(redisConnection!));

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddSubscriptionType<Subscription>()
    .AddRedisSubscriptions(
        new SubscriptionOptions
        {
            TopicPrefix = "catalog-prod"
        });
```

Do not combine `AddInMemorySubscriptions()` and `AddRedisSubscriptions()` for the same schema. Use `TopicPrefix` when several GraphQL servers share one Redis instance.

Expected behavior: a mutation handled by pod A publishes an event through Redis, and a WebSocket or SSE subscriber connected to pod B receives it.

See [Subscriptions](/docs/hotchocolate/v16/building-a-schema/subscriptions) for schema fields, publishing, WebSocket, SSE, and provider options.

# Share persisted operation storage when needed

Persisted operations need shared storage when you run more than one replica or when operation documents must survive restarts.

Use trusted documents when you want an allowlist published before deployment:

```bash
dotnet add package HotChocolate.PersistedOperations.Redis
dotnet add package StackExchange.Redis
```

```csharp
using StackExchange.Redis;

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")!));

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .UsePersistedOperationPipeline()
    .AddRedisOperationDocumentStorage();
```

Use APQ when clients store operation documents dynamically at runtime:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .UseAutomaticPersistedOperationPipeline()
    .AddRedisOperationDocumentStorage();
```

`AddRedisOperationDocumentStorage()` uses the registered `IConnectionMultiplexer`. Overloads also accept an `IDatabase` factory or an `IConnectionMultiplexer` factory, with an optional expiration.

Storage choices:

| Storage            | Kubernetes guidance                                                                                                          |
| ------------------ | ---------------------------------------------------------------------------------------------------------------------------- |
| Redis              | Good fit for APQ and shared operation lookup across replicas. Use persistent Redis if documents must survive Redis restarts. |
| Filesystem         | Bake trusted documents into the image or mount identical read-only storage in every pod.                                     |
| Azure Blob Storage | Shared durable storage for trusted documents when Azure Blob fits your deployment model.                                     |
| In-memory          | Single pod or local development only. Misses after restart and inconsistent hits across pods are expected.                   |

`OnlyAllowPersistedDocuments` blocks dynamic operations. Enable it only after your clients and storage are ready.

See [Trusted Documents](/docs/hotchocolate/v16/performance/trusted-documents) and [Automatic Persisted Operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations).

# Configure Ingress for WebSocket and SSE

Streaming transports are sensitive to proxy behavior.

WebSocket subscriptions require:

- `app.UseWebSockets()` before `app.MapGraphQL()`.
- Ingress support for WebSocket upgrade requests.
- Idle timeouts longer than expected subscription lifetimes.
- The `graphql-transport-ws` subprotocol for current clients.

SSE uses the same GraphQL HTTP endpoint. Clients request it with `Accept: text/event-stream`. Disable proxy buffering and allow long-lived responses.

NGINX Ingress example:

```yaml
metadata:
  annotations:
    nginx.ingress.kubernetes.io/proxy-read-timeout: "3600"
    nginx.ingress.kubernetes.io/proxy-send-timeout: "3600"
    nginx.ingress.kubernetes.io/proxy-buffering: "off"
```

Tune WebSocket keep-alive only when your proxy policy requires it:

```csharp
builder
    .AddGraphQL()
    .ModifyServerOptions(options =>
    {
        options.Sockets.ConnectionInitializationTimeout = TimeSpan.FromSeconds(30);
        options.Sockets.KeepAliveInterval = TimeSpan.FromSeconds(12);
    });
```

The default connection initialization timeout is 10 seconds. The default keep-alive interval is 5 seconds.

Verify SSE with a streaming request:

```bash
curl -N https://api.example.com/graphql \
  -H 'Content-Type: application/json' \
  -H 'Accept: text/event-stream' \
  --data '{ "query": "subscription { bookAdded { title } }" }'
```

Expected response begins as SSE events and remains open while the subscription is active:

```text
event: next
data: {"data":{"bookAdded":{"title":"GraphQL in Action"}}}
```

See [HTTP Transport](/docs/hotchocolate/v16/server/http-transport) for media types, SSE, JSON Lines, multipart streaming, and WebSocket protocol details.

# Shut down gracefully during rollouts

Kubernetes sends `SIGTERM`, waits for `terminationGracePeriodSeconds`, then stops the container. ASP.NET Core stops accepting new requests and cancels in-flight work through cancellation tokens.

Configure enough grace time for normal in-flight operations:

```yaml
spec:
  terminationGracePeriodSeconds: 45
  containers:
    - name: catalog-api
      image: ghcr.io/example/catalog-api:1.0.0
      lifecycle:
        preStop:
          exec:
            command: ["/bin/sh", "-c", "sleep 5"]
```

Use a `preStop` delay only when your ingress or load balancer needs time to stop sending new requests after readiness changes. Do not use it to hide slow shutdown.

Resolvers and data access should accept `CancellationToken`:

```csharp
[QueryType]
public static partial class ProductQueries
{
    public static async Task<Product?> GetProductAsync(
        int id,
        CatalogContext db,
        CancellationToken cancellationToken)
    {
        return await db.Products.FindAsync([id], cancellationToken);
    }
}
```

Long-lived WebSocket and SSE clients must reconnect and resubscribe during rollouts, node drains, and pod restarts. Redis subscriptions ensure events published by other pods can still be delivered to connected clients, but they do not preserve a terminated connection.

# Manage secrets and environment-specific configuration

Use the same image in every environment. Supply environment-specific configuration through Kubernetes Secrets, ConfigMaps, or an external secret provider.

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: catalog-api-secrets
stringData:
  ConnectionStrings__CatalogDb: Host=postgres;Database=catalog;Username=app;Password=<secret>
  ConnectionStrings__Redis: redis:6379
  OTEL_EXPORTER_OTLP_ENDPOINT: http://otel-collector:4317
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: catalog-api
spec:
  template:
    spec:
      containers:
        - name: catalog-api
          image: ghcr.io/example/catalog-api:1.0.0
          envFrom:
            - secretRef:
                name: catalog-api-secrets
```

ASP.NET Core maps double underscores to hierarchical configuration. `ConnectionStrings__Redis` becomes `ConnectionStrings:Redis` and is available through `builder.Configuration.GetConnectionString("Redis")`.

Put database credentials, Redis credentials, auth settings, Nitro credentials, OTLP endpoints, and cloud credentials in cluster-managed secrets. Do not bake secrets into images or commit environment-specific manifests with real values.

# Export telemetry from the cluster

Hot Chocolate emits OpenTelemetry spans for GraphQL execution when you add instrumentation. Export traces to an OpenTelemetry collector in the cluster or to your managed telemetry backend.

Install packages:

```bash
dotnet add package HotChocolate.Diagnostics
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Instrumentation.AspNetCore
dotnet add package OpenTelemetry.Instrumentation.Http
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
```

Configure tracing:

```csharp
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var serviceName = builder.Configuration["OTEL_SERVICE_NAME"] ?? "catalog-api";

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddInstrumentation();

builder.Logging.AddOpenTelemetry(options =>
{
    options.IncludeFormattedMessage = true;
    options.IncludeScopes = true;
    options.ParseStateValues = true;
    options.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName));
});

builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName))
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation();
        tracing.AddHttpClientInstrumentation();
        tracing.AddHotChocolateInstrumentation();
        tracing.AddOtlpExporter();
    });
```

Provide OTLP settings and Kubernetes resource attributes through environment variables:

```yaml
env:
  - name: OTEL_SERVICE_NAME
    value: catalog-api
  - name: OTEL_EXPORTER_OTLP_ENDPOINT
    value: http://otel-collector:4317
  - name: OTEL_RESOURCE_ATTRIBUTES
    value: deployment.environment=prod,k8s.namespace.name=catalog
```

Expected traces include HTTP route and status, GraphQL operation type and name, document hash, persisted document ID when present, and DataLoader spans. Avoid `ActivityScopes.All` and `RequestDetails.Document` unless you need them because they add overhead and can send operation documents to telemetry backends.

See [Instrumentation](/docs/hotchocolate/v16/server/instrumentation) for options and emitted attributes.

# Troubleshoot Kubernetes failures

Start with the pod, Service, and ingress path:

```bash
kubectl describe pod -l app=catalog-api
kubectl logs deployment/catalog-api
kubectl get endpoints catalog-api
kubectl describe ingress catalog-api
curl -v https://api.example.com/graphql \
  -H 'Content-Type: application/json' \
  --data '{ "query": "{ __typename }" }'
```

| Symptom                                     | Check                                                                                                        | Fix                                                                                                      |
| ------------------------------------------- | ------------------------------------------------------------------------------------------------------------ | -------------------------------------------------------------------------------------------------------- |
| `404` from the app                          | `MapGraphQL` path, `UsePathBase`, ingress rewrite target, public path.                                       | Align external path and application path. Verify with `curl -v`.                                         |
| `502` or `503` during rollout               | Container port, Service `targetPort`, startup logs, startup probe, readiness probe, schema creation, warmup. | Match ports, inspect schema or warmup errors, increase startup probe deadline if cold start is expected. |
| Pod ready but GraphQL fails                 | App health checks do not include a required dependency, or endpoint options reject the request.              | Add dependency checks to readiness and inspect GraphQL error codes.                                      |
| First request is slow                       | Missing warmup, operation name mismatch in warmup, or lazy initialization enabled.                           | Add `AddWarmupTask`, include operation names, keep eager initialization.                                 |
| WebSocket handshake fails                   | `UseWebSockets()` missing, ingress blocks upgrade, wrong public path, subprotocol issue.                     | Register middleware before `MapGraphQL`, enable upgrade support, use the same route as HTTP GraphQL.     |
| SSE stalls or disconnects                   | Proxy buffering, short ingress timeouts, stripped `Accept` header.                                           | Disable buffering, increase read and send timeouts, preserve `Accept: text/event-stream`.                |
| Subscription events missing across replicas | In-memory subscription provider or mixed providers.                                                          | Register exactly one shared provider, such as Redis subscriptions.                                       |
| APQ or trusted document misses across pods  | In-memory storage, non-identical filesystem storage, Redis not shared, expiration too short.                 | Use shared Redis, shared read-only files, or another durable operation document storage.                 |
| Upload returns `413`                        | Ingress, Kestrel, `FormOptions`, or Hot Chocolate body limit rejected the request.                           | Identify the emitting layer and align limits.                                                            |
| Multipart upload returns `400`              | Missing preflight header or multipart disabled.                                                              | Send `GraphQL-preflight: 1` and keep `EnableMultipartRequests` enabled only for upload APIs.             |
| Requests cancel with 499-style proxy logs   | Client or proxy disconnected while resolvers were running.                                                   | Propagate `CancellationToken`, review proxy timeout, optimize long operations.                           |
| Legitimate operation is rejected            | Cost, depth, parser, validation, timeout, batching, or concurrency limit is too low.                         | Measure with `GraphQL-Cost: report`, then adjust the narrowest limit.                                    |
| Nitro is visible publicly                   | Tool enabled on the production endpoint.                                                                     | Set `options.Tool.Enable = false` or move Nitro to a protected route.                                    |

Use OpenTelemetry traces to connect HTTP errors, GraphQL validation errors, resolver latency, DataLoader batch sizes, and pod metadata.

# Next steps

- [Endpoints](/docs/hotchocolate/v16/server/endpoints) for `MapGraphQL`, Nitro, schema downloads, and endpoint options.
- [Warmup](/docs/hotchocolate/v16/server/warmup) for eager initialization and warmup tasks.
- [HTTP Transport](/docs/hotchocolate/v16/server/http-transport) for WebSocket, SSE, JSON Lines, multipart streaming, GET, and POST behavior.
- [Files](/docs/hotchocolate/v16/server/files) for `Upload` and multipart limits.
- [Subscriptions](/docs/hotchocolate/v16/building-a-schema/subscriptions) for subscription schema and providers.
- [Instrumentation](/docs/hotchocolate/v16/server/instrumentation) for OpenTelemetry setup.
- [Request Limits](/docs/hotchocolate/v16/securing-your-api/request-limits) and [Cost Analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis) for GraphQL resource protection.
- [Trusted Documents](/docs/hotchocolate/v16/performance/trusted-documents) and [Automatic Persisted Operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations) for operation storage.
