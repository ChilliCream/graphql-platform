---
title: Health checks
---

A Hot Chocolate server can accept HTTP requests only after ASP.NET Core finishes starting the application. In Hot Chocolate v16, schema and request executor initialization happen during startup by default, so readiness probes work well when you keep them separate from GraphQL traffic.

Use ASP.NET Core health checks for platform probes. Do not send Kubernetes, load balancer, or container platform probes to `/graphql`.

This page covers standard Hot Chocolate v16 servers hosted on ASP.NET Core. Fusion gateway health checks have separate gateway concerns and are outside the scope of this page.

# Prerequisites

You need:

- A Hot Chocolate v16 server configured with `builder.AddGraphQL()` or `builder.Services.AddGraphQLServer()`.
- GraphQL mapped with `app.MapGraphQL()`, usually at `/graphql`.
- An ASP.NET Core application where you can register `AddHealthChecks()` and map `MapHealthChecks()` endpoints.
- A list of dependencies that must work before normal GraphQL requests can succeed, such as SQL databases, Redis, message brokers, downstream HTTP services, or persisted-operation storage.
- For Kubernetes, access to the Deployment probe configuration.

# Configure health endpoints for a Hot Chocolate service

Start with separate endpoints for GraphQL, liveness, and readiness.

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

Expected result with the default ASP.NET Core response writer:

```bash
curl -i http://localhost:5000/healthz/live
```

```text
HTTP/1.1 200 OK

Healthy
```

`/healthz/live` answers whether the ASP.NET Core process can respond. `/healthz/ready` answers whether the instance should receive user traffic. The readiness endpoint returns HTTP `200` when all selected `ready` checks pass and HTTP `503` when any selected `ready` check is unhealthy.

If no checks match the `ready` predicate, ASP.NET Core can report the readiness endpoint as healthy. Add at least one real readiness check for production, usually a required database, cache, message broker, downstream service, or storage component.

If your organization already uses `/health` and `/alive`, keep those paths and apply the same semantics:

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

Use each probe for one decision.

| Probe     | Question                                                             | Endpoint                               | Includes                                                        | Excludes                                                      | Failure action                                                |
| --------- | -------------------------------------------------------------------- | -------------------------------------- | --------------------------------------------------------------- | ------------------------------------------------------------- | ------------------------------------------------------------- |
| Liveness  | Should the platform restart this process?                            | `/healthz/live`                        | A cheap in-process check, such as `self`.                       | Databases, Redis, downstream APIs, GraphQL execution.         | Restart the container or process.                             |
| Readiness | Should the platform send user traffic to this instance?              | `/healthz/ready`                       | Critical dependencies required by normal GraphQL requests.      | Optional dependencies and expensive business work.            | Remove the instance from traffic until healthy.               |
| Startup   | Should the platform keep waiting before liveness enforcement starts? | `/healthz/ready` or `/healthz/startup` | The same condition that must become true before traffic starts. | Long-running business jobs that do not block serving traffic. | Keep waiting, then fail startup if the threshold is exceeded. |

Liveness protects the process. Keep it independent of external services so a database outage does not cause every pod to restart at once.

Readiness protects users. Make it stricter than liveness, because it decides whether new GraphQL requests should reach this instance.

Startup probes protect slow initialization. Use them when large schemas, schema export, dependency startup, cold caches, or warmup tasks need more time than your regular liveness settings allow.

# Connect readiness to Hot Chocolate v16 startup

Hot Chocolate v16 constructs the schema and request executor during application startup by default. The ASP.NET Core warmup hosted service asks `IRequestExecutorProvider` to create the executor for each non-lazy schema before startup completes.

That matters for readiness:

- Schema configuration errors fail at startup instead of on the first GraphQL request.
- Kestrel does not begin accepting HTTP requests until hosted-service startup completes.
- A normal HTTP readiness probe cannot succeed before the eager request executor is available.

This guarantee ends if you opt into lazy initialization:

```csharp
builder
    .AddGraphQL()
    .ModifyOptions(options => options.LazyInitialization = true);
```

With lazy initialization, Hot Chocolate builds the schema when the schema is first needed. A readiness endpoint that only checks HTTP responsiveness no longer proves that GraphQL is warmed. Use eager initialization for production services that depend on readiness probes unless you have a specific startup tradeoff and have tested the first request path.

Readiness also ends at the checks you register. If `/healthz/ready` only checks `self`, it proves that the process can answer HTTP, not that resolvers can reach their required dependencies.

# Warm the request executor before readiness succeeds

Add warmup tasks when you want Hot Chocolate to parse, validate, and prepare representative operations before the instance receives traffic.

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

`MarkAsWarmupRequest()` tells Hot Chocolate to skip execution while still warming request preparation caches. Use it when your goal is to warm parser, document, validation, and operation preparation paths without invoking resolvers or causing side effects.

Important details:

- Warmup is blocking during initial startup. Keep each task bounded and honor the cancellation token.
- Include the operation name when clients send one. The operation name participates in the operation cache key.
- Requests marked as warmup requests bypass security measures and skip execution. Do not use them to prove authorization, persisted-operation storage, database connectivity, or resolver behavior.
- Warmup tasks run at startup and when Hot Chocolate rebuilds a request executor at runtime. During a runtime rebuild, the old executor keeps serving traffic until the new executor is warmed.

Use `IRequestExecutorWarmupTask` when warmup needs structure or a startup-only decision:

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

Register it with the GraphQL builder:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddWarmupTask<ProductCardWarmupTask>();
```

Generic warmup tasks are activated from Hot Chocolate schema services. If the task needs application services, cross-register those services with `.AddApplicationService<T>()`, use a factory overload, or access the root service provider where appropriate. See [the v15 to v16 migration guide](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16#clearer-separation-between-schema-and-application-services) for the service-provider model.

# Add dependency checks to readiness

Readiness should include dependencies that must work for normal GraphQL requests to succeed. Liveness should not.

If your application already uses Entity Framework Core health checks, tag the database check as `ready`:

```csharp
builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<AppDbContext>(tags: ["ready"]);
```

If your application uses a Redis health-check package, such as `AspNetCore.HealthChecks.Redis`, tag that check as `ready` too:

```csharp
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");

builder.Services
    .AddHealthChecks()
    .AddRedis(
        redisConnectionString!,
        name: "redis",
        tags: ["ready"]);
```

Package names and method overloads depend on the health-check integration you choose. Hot Chocolate does not own database, Redis, or message-broker health-check packages.

For a required service without a built-in integration, implement `IHealthCheck`:

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

Register it as readiness:

```csharp
builder.Services.AddHttpClient("catalog", client =>
{
    client.BaseAddress = new Uri("https://catalog.internal");
});

builder.Services
    .AddHealthChecks()
    .AddCheck<CatalogHealthCheck>("catalog", tags: ["ready"]);
```

Expected behavior:

- `/healthz/live` stays HTTP `200` when the process is responsive.
- `/healthz/ready` returns HTTP `503` when a critical dependency check is unhealthy.
- Logs and health-check results identify the failing check name, such as `catalog` or `redis`.

Keep dependency checks cheap, bounded, and deterministic. Prefer lightweight connection checks, pings, or small health endpoints over business queries. Do not run database migrations, large queries, cache fills, schema export, or resolver execution inside health checks.

If a dependency is optional, do not add it to readiness unless the instance should stop receiving all GraphQL traffic when that dependency is down.

# Configure Kubernetes probes

Map Kubernetes probes to the endpoint semantics.

```yaml
startupProbe:
  httpGet:
    path: /healthz/ready
    port: 8080 # Match the container HTTP port.
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

Expected result: new pods do not receive traffic until readiness passes, and slow startup does not cause liveness restart loops.

Tune the values for your schema size and dependencies:

- Point `livenessProbe` to `/healthz/live`.
- Point `readinessProbe` to `/healthz/ready`.
- Use `startupProbe` when schema creation, warmup tasks, schema export, dependency startup, or cold caches need a longer window.
- Set `failureThreshold`, `periodSeconds`, and `timeoutSeconds` so startup has enough time before liveness can restart the pod.
- Keep readiness stricter than liveness.

The same model applies to Azure Container Apps health probes, load balancer health probes, App Service health checks, Aspire service defaults, and other hosting platforms. Configure the platform path, port, timeout, and exposure rules to match your deployment.

# Do not use GraphQL operations as health probes

Do not configure platform probes to send GraphQL operations to `/graphql`.

Bad examples:

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

GraphQL probes can parse, validate, authorize, execute resolvers, hit databases, allocate memory, and appear in GraphQL metrics. A resolver-heavy health query can become a denial-of-service multiplier when every pod, node, and load balancer runs it on a short interval.

`{ __typename }` is cheap, but it still enters the GraphQL pipeline and does not prove dependency readiness. Introspection, schema downloads, Nitro, and broad queries are not health endpoints and may be disabled or restricted in production.

Use platform probes for process and traffic decisions:

```text
GET /healthz/live
GET /healthz/ready
```

If you need a business-level synthetic transaction, run it from external monitoring at a lower cadence with clear SLO ownership. Do not use it as liveness or readiness.

# Secure and expose health endpoints deliberately

Health endpoints can disclose dependency names, topology, and failure states if you include detailed responses. Keep detailed diagnostics in logs and telemetry, not public health responses.

Expose probe endpoints only to the orchestrator, load balancer, private network, or trusted ingress where possible. Decide whether authentication is required based on the hosting platform. Kubernetes HTTP probes usually need unauthenticated access inside the cluster, while an external health page may need a different policy.

If the orchestrator cannot send credentials and network policy limits access, make that endpoint anonymous explicitly:

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

Review this choice before exposing the endpoints through a public ingress. You can use separate internal and external endpoints when a public load balancer needs a limited probe but operators need detailed internal diagnostics.

If you customize `HealthCheckOptions.ResponseWriter`, keep the public response minimal. Dependency names, exception messages, connection strings, host names, and shard names belong in logs or telemetry, not in unauthenticated public responses.

# Troubleshoot failing or flapping probes

Start with the symptom, then verify the endpoint, selected checks, startup logs, and platform configuration.

```bash
curl -i http://localhost:5000/healthz/live
curl -i http://localhost:5000/healthz/ready
kubectl describe pod <pod-name>
kubectl logs <pod-name>
```

Expected checks:

- Healthy liveness returns HTTP `200`.
- Unhealthy readiness returns HTTP `503`.
- Application logs identify the failing health-check name or startup exception.

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

- Use [Warmup](/docs/hotchocolate/v16/server/warmup) to tune executor cache warming, startup warmup, schema export, and lazy initialization.
- Use [Endpoints](/docs/hotchocolate/v16/server/endpoints) to customize GraphQL transport, Nitro, schema downloads, and endpoint paths.
- Use [OpenTelemetry tracing](/docs/hotchocolate/v16/operations/observability/opentelemetry), [Metrics](/docs/hotchocolate/v16/operations/observability/metrics), and [Instrumentation](/docs/hotchocolate/v16/server/instrumentation) to observe startup, warmup, execution, and dependency failures.
- Use [Performance tuning](/docs/hotchocolate/v16/guides/performance) to reduce request overhead and cold paths.
- Use [trusted documents](/docs/hotchocolate/v16/performance/trusted-documents) and [automatic persisted operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations) to control production GraphQL request cost. Do not use them as probe substitutes.
- Review your hosting platform documentation for probe intervals, startup windows, timeout behavior, and network exposure.
