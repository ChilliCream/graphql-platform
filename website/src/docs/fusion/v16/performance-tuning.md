---
title: "Performance Tuning"
---

The Fusion gateway proxies every GraphQL operation to one or more subgraphs over HTTP. The defaults work well out of the box, but high-throughput or latency-sensitive deployments can benefit from tuning the transport layer.

This page covers:

- Configuring the HTTP transport
- Enabling HTTP/2 for multiplexed subgraph communication
- Deduplicating identical in-flight requests
- Limiting concurrent request processing to maximize throughput

## Configure the HTTP Transport

Fusion uses a **named `HttpClient`** to communicate with subgraphs. The default client name is `"fusion"`, and you configure it through the standard `IHttpClientFactory` pattern. This gives you full control over connection behavior, timeouts, and message handlers.

A baseline `Program.cs` that registers the named client:

```csharp
var builder = WebApplication.CreateBuilder(args);

// 1. Register the named HTTP client for subgraph communication
builder.Services.AddHttpClient("fusion");

// 2. Configure the Fusion gateway
builder
    .AddGraphQLGateway()
    .AddFileSystemConfiguration("./gateway.far");

var app = builder.Build();
app.MapGraphQLHttp();
app.Run();
```

1. **Named HTTP client `"fusion"`**: the client the gateway uses to call subgraphs. Any handler configuration you add here applies to all subgraph requests.
2. **Gateway registration**: wires up the Fusion execution engine and loads the composed schema.

## HTTP/2

HTTP/2 multiplexes multiple requests over a single TCP connection, which reduces connection overhead when the gateway sends many concurrent requests to a subgraph. This is especially beneficial when subgraphs are behind a load balancer that supports HTTP/2.

### With TLS

When your subgraphs use TLS (HTTPS), HTTP/2 is negotiated automatically via ALPN. Enable `EnableMultipleHttp2Connections` to allow the gateway to open additional HTTP/2 connections when a single connection's stream limit is reached:

```csharp
builder.Services
    .AddHttpClient("fusion")
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        EnableMultipleHttp2Connections = true,
    });
```

No additional version configuration is needed. .NET negotiates HTTP/2 over TLS by default.

### Without TLS

In many Kubernetes deployments, services communicate over plaintext HTTP inside the cluster. HTTP/2 cleartext (h2c) requires explicit opt-in because .NET defaults to HTTP/1.1 for unencrypted connections.

To force HTTP/2 without TLS, set `DefaultRequestVersion` and `DefaultVersionPolicy` on the `HttpClient`:

```csharp
builder.Services
    .AddHttpClient("fusion", httpClient =>
    {
        httpClient.DefaultRequestVersion = HttpVersion.Version20;
        httpClient.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
    })
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        EnableMultipleHttp2Connections = true,
    });
```

The subgraph must also be configured to accept HTTP/2 over cleartext. By default, Kestrel only listens on HTTP/1.1 for non-TLS endpoints. Enable h2c in each subgraph's `Program.cs`:

```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5001, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });
});
```

If you are unsure whether your infrastructure supports HTTP/2 cleartext end-to-end, **HTTP/1.1 works well** for most internal deployments. Switch to HTTP/2 only when you have confirmed support on both the gateway and all subgraphs.

## Request Deduplication

When multiple identical query requests are in flight to the same subgraph at the same time, **request deduplication** ensures only one HTTP request is actually sent. The first request becomes the "leader" and executes normally. Subsequent identical requests become "followers" that wait for the leader's response. Each caller receives an independent copy of the result.

### When It Helps

Deduplication is most effective when:

- **Burst traffic** hits the gateway with the same query. For example, a popular product page refreshing across many clients simultaneously.
- **Public APIs** serve unauthenticated traffic where many users send the same queries.
- **The same user** sends identical concurrent requests (e.g., a UI that fires duplicate fetches).

### Security Model

The deduplication hash includes the request body, URL, and the values of configurable **hash headers**. By default, `Authorization` and `Cookie` headers are included in the hash. This means:

- **Unauthenticated or public queries**: no auth headers, so identical queries from any client are deduplicated. High hit rate.
- **Same user, same query, concurrent**: identical tokens produce the same hash and are deduplicated.
- **Different users, same query**: different tokens produce different hashes. Never deduplicated. One user's response is never shared with another.

### How to Enable

Add the request deduplication message handler with `.AddRequestDeduplication()` to the named HTTP client builder:

```csharp
builder.Services
    .AddHttpClient("fusion")
    .AddRequestDeduplication();
```

### Customizing Hash Headers

By default, the `Authorization` and `Cookie` headers are included in the deduplication hash, which covers most setups. If you need additional headers to be part of the hash, for instance a tenant identifier in a multi-tenant application, add them to `HashHeaders`:

```csharp
.AddRequestDeduplication(options =>
{
    options.HashHeaders = ["Authorization", "Cookie", "X-Tenant-Id"];
});
```

For **service-to-service communication** where the gateway does not receive cookies, you can remove `Cookie` from the hash:

```csharp
.AddRequestDeduplication(options =>
{
    options.HashHeaders = ["Authorization"];
});
```

### What Gets Deduplicated

Only **query operations** are deduplicated. The following are **not** deduplicated:

- **Mutations**: not safe to coalesce because they have side effects.
- **Subscriptions**: long-lived connections that are inherently unique.
- **File uploads**: multipart requests bypass deduplication.

## Concurrent Execution Limiting

The gateway limits the number of simultaneous **executions** it processes using a **concurrency gate**. An execution is the work of running a single GraphQL operation end-to-end. Each query or mutation counts as one execution, and each event a subscription emits counts as one execution while its selection set runs. Capping concurrency keeps the gateway operating in its optimal throughput range. Too much work competing for the same resources (thread pool, memory, connections) can reduce overall throughput rather than increase it.

The default limit is **64 concurrent executions**. The default is calibrated for small containers 1 to 4 CPUs. Depending on your CPU count and typical operation cost, you may want to increase or decrease this value to find the optimal throughput for your hardware. The limit does not reject work; it queues it, and the GraphQL executor processes at most 64 executions concurrently by default.

Subscriptions participate in this limit like any other operation. Each event the gateway processes consumes a slot. Idle subscriptions between events cost nothing.

Set the limit through `ModifyServerOptions` on the gateway builder:

```csharp
builder
    .AddGraphQLGateway()
    .AddFileSystemConfiguration("./gateway.far")
    .ModifyServerOptions(options =>
    {
        options.MaxConcurrentExecutions = 128;
    });
```

You can override this limit for a specific HTTP endpoint using `WithOptions`:

```csharp
app.MapGraphQLHttp()
    .WithOptions(options =>
    {
        options.MaxConcurrentExecutions = 256;
    });
```

### Tuning Guidance

- **Too low**: executions queue behind the concurrency gate, adding latency even when the gateway and subgraphs have spare capacity. Subscriptions with high event rates feel this first.
- **Too high**: the gateway runs more work than it can efficiently process, leading to thread pool starvation and increased latency across queries, mutations, and subscription events.

Start with the default of 64 and adjust based on your workload. If you expect many long-lived subscriptions firing frequent events, factor those into your sizing. They now contend for the same slots as queries and mutations. Set to `null` to disable the limit entirely.

### Execution Cancellation

Every execution is bounded by the `ExecutionTimeout` option (default 30 seconds). This applies uniformly to queries, mutations, subscription handshakes, and each subscription event. The budget covers both the time an execution spends waiting for a concurrency slot and the time it spends running. When the budget is exceeded, the execution is cancelled and the caller receives a clean timeout error.

`ExecutionTimeout` is the single setting that controls cancellation for every execution. Configure it with `ModifyRequestOptions`:

```csharp
builder
    .AddGraphQLGateway()
    .ModifyRequestOptions(o => o.ExecutionTimeout = TimeSpan.FromSeconds(10));
```

If executions routinely time out at the gate, that is a signal to scale out or raise `MaxConcurrentExecutions`. Increasing `ExecutionTimeout` only defers the problem.

## Next Steps

- **"I need CDN and HTTP response caching behavior"**: [Cache Control](/docs/fusion/v16/cache-control) covers `@cacheControl`, composition merge behavior, and gateway response headers.
- **"I need to secure my gateway"**: [Authentication and Authorization](/docs/fusion/v16/authentication-and-authorization) covers JWT validation, header propagation, and subgraph-level authorization.
- **"I need to deploy this"**: [Deployment & CI/CD](/docs/fusion/v16/deployment-and-ci-cd) covers production deployment patterns and CI pipeline setup.
- **"I want to monitor performance"**: Observability and distributed tracing will be covered in future documentation.
