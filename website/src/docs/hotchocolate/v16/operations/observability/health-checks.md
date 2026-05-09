---
title: Health checks
---

Hot Chocolate v16 servers accept HTTP requests only after ASP.NET Core completes application startup. By default, schema and request executor initialization occur during startup, making readiness probes reliable when you separate them from GraphQL traffic.

Always use ASP.NET Core health checks for platform probes. Do not direct Kubernetes, load balancer, or container platform probes to `/graphql`.

This page focuses on Hot Chocolate v16 servers hosted with ASP.NET Core. Health checks for Fusion gateways involve different concerns and are not covered here.

# Prerequisites

Before you begin, ensure you have:

- A Hot Chocolate v16 server set up with `builder.AddGraphQL()` or `builder.Services.AddGraphQLServer()`
- GraphQL mapped using `app.MapGraphQL()`, typically at `/graphql`
- An ASP.NET Core app where you can register `AddHealthChecks()` and map `MapHealthChecks()` endpoints
- A list of critical dependencies required for GraphQL requests, such as SQL databases, Redis, message brokers, downstream HTTP services, or persisted-operation storage
- For Kubernetes, access to your Deployment probe configuration

# Set up health endpoints for your Hot Chocolate service

Define separate endpoints for GraphQL, liveness, and readiness. Here’s a typical setup:

```csharp
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddHealthChecks()
    .AddCheck(
        "self",
        () => HealthCheckResult.Healthy(),
        tags: ["live"]);

builder
    .AddGraphQL()
    .AddQueryType<Query>();

var app = builder.Build();

app.MapGraphQL("/graphql");

app.MapHealthChecks("/healthz/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

app.MapHealthChecks("/healthz/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();
```

When you call the liveness endpoint with the default ASP.NET Core response writer, you should see:

```bash
curl -i http://localhost:5000/healthz/live
```

```text
HTTP/1.1 200 OK

Healthy
```

The `/healthz/live` endpoint checks if the ASP.NET Core process is responsive. The `/healthz/ready` endpoint determines if the instance is ready to receive user traffic. When all `ready` checks pass, the readiness endpoint returns HTTP `200`. If any `ready` check fails, it returns HTTP `503`.

If no checks are tagged as `ready`, ASP.NET Core may report the readiness endpoint as healthy. For production, always add at least one real readiness check for a required dependency, such as a database, cache, message broker, downstream service, or storage.

If your organization uses `/health` and `/alive` endpoints, you can keep those paths and apply the same logic:

```csharp
app.MapHealthChecks("/alive", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
```

# Understand liveness, readiness, and startup

Each probe serves a distinct purpose:

| Probe     | Question                                                             | Endpoint                               | Includes                                                        | Excludes                                                      | Failure action                                                |
| --------- | -------------------------------------------------------------------- | -------------------------------------- | --------------------------------------------------------------- | ------------------------------------------------------------- | ------------------------------------------------------------- |
| Liveness  | Should the platform restart this process?                            | `/healthz/live`                        | A cheap in-process check, such as `self`.                       | Databases, Redis, downstream APIs, GraphQL execution.         | Restart the container or process.                             |
| Readiness | Should the platform send user traffic to this instance?              | `/healthz/ready`                       | Critical dependencies required by normal GraphQL requests.      | Optional dependencies and expensive business work.            | Remove the instance from traffic until healthy.               |
| Startup   | Should the platform keep waiting before liveness enforcement starts? | `/healthz/ready` or `/healthz/startup` | The same condition that must become true before traffic starts. | Long-running business jobs that do not block serving traffic. | Keep waiting, then fail startup if the threshold is exceeded. |

- **Liveness** checks if the process is running. It should not depend on external services, so a database outage does not cause all pods to restart.
- **Readiness** checks if the instance is ready for user traffic. It should be stricter than liveness, as it controls whether new GraphQL requests reach the instance.
- **Startup** probes help when initialization is slow. Use them if schema creation, schema export, dependency startup, cold caches, or warmup tasks need more time than your liveness settings allow.

# How readiness relates to Hot Chocolate v16 startup

By default, Hot Chocolate v16 builds the schema and request executor during application startup. The ASP.NET Core warmup hosted service calls `IRequestExecutorProvider` to create the executor for each non-lazy schema before startup completes.

This behavior affects readiness in several ways:

- Schema configuration errors surface at startup, not on the first GraphQL request.
- Kestrel does not accept HTTP requests until hosted-service startup is finished.
- A standard HTTP readiness probe cannot succeed until the request executor is ready.

If you enable lazy initialization:

```csharp
builder
    .AddGraphQL()
    .ModifyOptions(options => options.LazyInitialization = true);
```

Hot Chocolate delays schema creation until the schema is first needed. In this mode, a readiness endpoint that only checks HTTP responsiveness does not guarantee that GraphQL is ready. For production, use eager initialization if you rely on readiness probes, unless you have a specific reason and have tested the first-request path.

Also, readiness is only as strong as the checks you register. If `/healthz/ready` checks only `self`, it confirms HTTP responsiveness but not that resolvers can reach their required dependencies.

# Warm the request executor before readiness

To ensure Hot Chocolate parses, validates, and prepares representative operations before the instance receives traffic, add warmup tasks.

```csharp
using HotChocolate.Execution;

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddWarmupTask(async (executor, cancellationToken) =>
    {
        var request = OperationRequestBuilder.New()
            .SetDocument("query ProductCard { products { id name } }")
            .SetOperationName("ProductCard")
            .MarkAsWarmupRequest()
            .Build();

        await executor.ExecuteAsync(request, cancellationToken);
    });
```

`MarkAsWarmupRequest()` tells Hot Chocolate to skip execution but still warm up the request preparation caches. Use this to warm the parser, document, validation, and operation preparation paths without invoking resolvers or causing side effects.

Keep in mind:

- Warmup tasks block during initial startup. Keep them fast and always honor the cancellation token.
- Always include the operation name if clients use one, as it affects the operation cache key.
- Warmup requests bypass security and skip execution. Do not use them to test authorization, persisted-operation storage, database connectivity, or resolver logic.
- Warmup tasks run at startup and whenever Hot Chocolate rebuilds a request executor at runtime. During a runtime rebuild, the old executor continues serving traffic until the new one is warmed.

If you need more structure or want a startup-only warmup, implement `IRequestExecutorWarmupTask`:

```csharp
using HotChocolate.Execution;

public sealed class ProductCardWarmupTask : IRequestExecutorWarmupTask
{
    public bool ApplyOnlyOnStartup => true;

    public async Task WarmupAsync(
        IRequestExecutor executor,
        CancellationToken cancellationToken)
    {
        var request = OperationRequestBuilder.New()
            .SetDocument("query ProductCard { products { id name } }")
            .SetOperationName("ProductCard")
            .MarkAsWarmupRequest()
            .Build();

        await executor.ExecuteAsync(request, cancellationToken);
    }
}
```

Register the warmup task with the GraphQL builder:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddWarmupTask<ProductCardWarmupTask>();
```

Generic warmup tasks are activated from Hot Chocolate schema services. If your task needs application services, cross-register them with `.AddApplicationService<T>()`, use a factory overload, or access the root service provider as needed. For more on the service-provider model, see the v15 to v16 migration guide.

# Add dependency checks to readiness

Readiness checks should cover all dependencies required for normal GraphQL requests. Liveness checks should not include these dependencies.

If you use Entity Framework Core health checks, tag your database check as `ready`:

```csharp
builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<AppDbContext>(tags: ["ready"]);
```

For Redis, if you use a package like `AspNetCore.HealthChecks.Redis`, tag the check as `ready` as well:

```csharp
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");

builder.Services
    .AddHealthChecks()
    .AddRedis(
        redisConnectionString!,
        name: "redis",
        tags: ["ready"]);
```

The exact package and method overloads depend on your health-check integration. Hot Chocolate does not provide or own these health-check packages.

If you need to check a required service without a built-in integration, implement `IHealthCheck`:

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;

public sealed class CatalogHealthCheck(IHttpClientFactory httpClientFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient("catalog");
        using var response = await client.GetAsync("/health", cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return HealthCheckResult.Healthy();
        }

        return HealthCheckResult.Unhealthy(
            $"Catalog returned HTTP {(int)response.StatusCode}.");
    }
}
```

Register your custom check as a readiness check:

```csharp
builder.Services.AddHttpClient("catalog", client =>
{
    client.BaseAddress = new Uri("https://catalog.internal");
});

builder.Services
    .AddHealthChecks()
    .AddCheck<CatalogHealthCheck>("catalog", tags: ["ready"]);
```

You should see the following behavior:

- `/healthz/live` returns HTTP `200` as long as the process is responsive
- `/healthz/ready` returns HTTP `503` if a critical dependency check fails
- Logs and health-check results identify the failing check by name, such as `catalog` or `redis`

Keep dependency checks lightweight, fast, and predictable. Use simple connection checks, pings, or small health endpoints. Avoid running database migrations, large queries, cache fills, schema exports, or resolver execution in health checks.

If a dependency is optional, do not include it in readiness unless the instance should stop receiving all GraphQL traffic when that dependency is unavailable.

# Configure Kubernetes probes

Map your Kubernetes probes to the correct health endpoints:

```yaml
startupProbe:
  httpGet:
    path: /healthz/ready
    port: 8080 # Match your container's HTTP port
  failureThreshold: 30
  periodSeconds: 2
readinessProbe:
  httpGet:
    path: /healthz/ready
    port: 8080
  periodSeconds: 10
  timeoutSeconds: 2
livenessProbe:
  httpGet:
    path: /healthz/live
    port: 8080
  periodSeconds: 20
  timeoutSeconds: 2
```

With this setup, new pods do not receive traffic until readiness passes, and slow startup does not trigger liveness restarts.

Adjust probe values for your schema size and dependencies:

- Set `livenessProbe` to `/healthz/live`
- Set `readinessProbe` to `/healthz/ready`
- Use `startupProbe` if schema creation, warmup, schema export, dependency startup, or cold caches need extra time
- Tune `failureThreshold`, `periodSeconds`, and `timeoutSeconds` so startup has enough time before liveness can restart the pod
- Always keep readiness stricter than liveness

This approach also applies to Azure Container Apps, load balancers, App Service health checks, Aspire service defaults, and other platforms. Configure the path, port, timeout, and exposure rules to fit your deployment.

# Do not use GraphQL operations as health probes

Do not configure your platform to send GraphQL operations to `/graphql` for health checks.

**Bad examples:**

```yaml
readinessProbe:
  httpGet:
    path: /graphql?query=%7B__typename%7D
    port: 8080
```

```graphql
query HealthProbe {
  health {
    status
    dependencies
  }
}
```

GraphQL probes can trigger parsing, validation, authorization, resolver execution, database calls, memory allocation, and will show up in GraphQL metrics. If every pod, node, and load balancer runs a resolver-heavy health query frequently, it can create a denial-of-service risk.

Even `{ __typename }` enters the GraphQL pipeline and does not guarantee dependency readiness. Introspection, schema downloads, Nitro, and broad queries are not health endpoints and may be disabled or restricted in production.

Always use platform probes for process and traffic decisions:

```text
GET /healthz/live
GET /healthz/ready
```

If you need a business-level synthetic transaction, run it from external monitoring at a lower frequency and with clear SLO ownership. Do not use it for liveness or readiness.

# Secure and expose health endpoints deliberately

Health endpoints can reveal dependency names, topology, and failure states if you include detailed responses. Keep detailed diagnostics in logs and telemetry, not in public health responses.

Expose probe endpoints only to the orchestrator, load balancer, private network, or trusted ingress whenever possible. Decide on authentication based on your hosting platform. Kubernetes HTTP probes typically require unauthenticated access within the cluster, but an external health page may need a different policy.

If the orchestrator cannot send credentials and network policy restricts access, explicitly allow anonymous access:

```csharp
app.MapHealthChecks("/healthz/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
})
.AllowAnonymous();

app.MapHealthChecks("/healthz/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
})
.AllowAnonymous();
```

Review this decision before exposing endpoints through a public ingress. You can use separate internal and external endpoints if a public load balancer needs a limited probe but operators require detailed internal diagnostics.

If you customize `HealthCheckOptions.ResponseWriter`, keep public responses minimal. Do not include dependency names, exception messages, connection strings, host names, or shard names in unauthenticated public responses. Log these details instead.

# Troubleshoot failing or flapping probes

When probes fail or behave unexpectedly, start by checking the endpoint, selected health checks, startup logs, and platform configuration.

```bash
curl -i http://localhost:5000/healthz/live
curl -i http://localhost:5000/healthz/ready
kubectl describe pod <pod-name>
kubectl logs <pod-name>
```

You should see:

- Healthy liveness returns HTTP `200`
- Unhealthy readiness returns HTTP `503`
- Application logs identify the failing health-check name or startup exception

| Symptom                                     | Likely causes                                                                                                                                                                                                                  | What to check                                                                                      | Fix                                                                                                                                        |
| ------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | -------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------ |
| Readiness never succeeds.                   | Schema startup exception, warmup task exception, dependency check failure, missing `ready` checks, wrong path, wrong port, auth blocked the probe, or missing `MapHealthChecks`.                                               | Application startup logs, `/healthz/ready` locally, Kubernetes events, selected health-check tags. | Fix the startup exception or dependency, correct the path and port, register a real `ready` check, or adjust endpoint auth.                |
| Pod restarts during startup.                | Liveness starts too early, no startup probe, startup probe too strict, warmup exceeds the timeout, schema export is slow, or dependencies block startup.                                                                       | `kubectl describe pod`, probe failure counts, startup duration, warmup logs.                       | Add or relax `startupProbe`, tune warmup, move long nonessential jobs out of startup, or increase thresholds.                              |
| Readiness flaps.                            | Dependency timeout too low, transient database or Redis failures, connection pool exhaustion, expensive checks, or health-check cadence too high.                                                                              | Health-check durations, dependency telemetry, connection pool metrics, platform probe interval.    | Use cheaper checks, raise timeouts carefully, reduce cadence, tune pools, and fix dependency instability.                                  |
| First GraphQL request is still slow.        | `LazyInitialization` is enabled, warmup did not include the operation name, warmup skipped in the current environment, resolver execution was intentionally skipped, or a runtime schema rebuild is warming in the background. | GraphQL builder options, warmup task registration, operation names, startup logs, instrumentation. | Use eager initialization, add representative warmup requests, include operation names, and verify warmup runs in the deployed environment. |
| Probe traffic appears in GraphQL telemetry. | The platform probes `/graphql`, Nitro, or schema-download URLs.                                                                                                                                                                | ASP.NET Core route metrics, Hot Chocolate traces, platform probe config.                           | Change probes to `/healthz/live` and `/healthz/ready`.                                                                                     |
| Readiness says healthy but GraphQL fails.   | Missing dependency checks, optional dependency misclassified, auth or config error outside the health-check path, or warmup skipped resolver side effects.                                                                     | Failing operation logs, dependency list, health-check registrations, auth configuration.           | Add the missing required dependency checks, test auth separately, and use external synthetic monitoring for business flows.                |

Use [OpenTelemetry tracing](/docs/hotchocolate/v16/operations/observability/opentelemetry), [metrics](/docs/hotchocolate/v16/operations/observability/metrics), and [server instrumentation](/docs/hotchocolate/v16/server/instrumentation) to correlate startup, warmup, dependency, and execution failures.

# Reference: endpoint semantics

| Endpoint               | Used by                                                        | Includes                                                                                 | Excludes                                                       | Failure action                                                     |
| ---------------------- | -------------------------------------------------------------- | ---------------------------------------------------------------------------------------- | -------------------------------------------------------------- | ------------------------------------------------------------------ |
| `/healthz/live`        | Kubernetes liveness probe, process watchdogs.                  | Process responsiveness and cheap in-process checks.                                      | External dependencies, GraphQL operations, resolver execution. | Restart the instance.                                              |
| `/healthz/ready`       | Kubernetes readiness probe, load balancers, service discovery. | Critical dependencies and anything required before normal GraphQL traffic should arrive. | Optional dependencies and expensive business transactions.     | Remove the instance from traffic.                                  |
| `/healthz/startup`     | Platforms that require a separate startup endpoint.            | Startup readiness conditions.                                                            | Long-running work that should not block traffic.               | Keep waiting during startup, then fail if thresholds are exceeded. |
| `/graphql`             | GraphQL clients.                                               | Application GraphQL traffic over HTTP and WebSocket.                                     | Platform liveness, readiness, and startup probes.              | Depends on client request handling, not platform probe policy.     |
| `/health` and `/alive` | Common Aspire or organization-specific paths.                  | Equivalent semantics when you configure them that way.                                   | Any semantics you do not explicitly map.                       | Depends on how your platform maps each path.                       |

# Next steps

- Use [Warmup](/docs/hotchocolate/v16/server/warmup) to tune executor cache warming, startup warmup, schema export, and lazy initialization
- Use [Endpoints](/docs/hotchocolate/v16/server/endpoints) to customize GraphQL transport, Nitro, schema downloads, and endpoint paths
- Use [OpenTelemetry tracing](/docs/hotchocolate/v16/operations/observability/opentelemetry), [Metrics](/docs/hotchocolate/v16/operations/observability/metrics), and [Instrumentation](/docs/hotchocolate/v16/server/instrumentation) to observe startup, warmup, execution, and dependency failures
- Use [Performance tuning](/docs/hotchocolate/v16/guides/performance) to reduce request overhead and cold paths
- Use [trusted documents](/docs/hotchocolate/v16/performance/trusted-documents) and [automatic persisted operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations) to control production GraphQL request cost. Do not use them as probe substitutes
- Review your hosting platform documentation for probe intervals, startup windows, timeout behavior, and network exposure
