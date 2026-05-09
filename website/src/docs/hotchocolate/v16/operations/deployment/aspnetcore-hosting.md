---
title: ASP.NET Core Hosting
---

This page shows how to host one Hot Chocolate v16 GraphQL service on ASP.NET Core for production. It does not cover Fusion gateway hosting. Treat Fusion as a separate deployment topic.

You will start with a production-shaped `Program.cs`, then adjust the endpoint, ASP.NET Core middleware, security behavior, resource limits, reverse proxy settings, realtime transports, warmup, health checks, and telemetry.

# Prerequisites

You need:

- .NET 8 or newer with ASP.NET Core minimal hosting.
- Hot Chocolate v16 packages installed.
- Existing schema types or resolvers.
- A public GraphQL route, for example `/graphql`.
- The browser origins that may call the service.
- An authentication scheme when protected data is exposed.
- A deployment target, such as Kestrel, IIS, Azure App Service, a container platform, or a reverse proxy.
- A subscription provider when you use subscriptions. In-memory subscriptions are appropriate for one app instance only.

# Start with a production-shaped Program.cs

The following host exposes a small schema at `/graphql`, enables environment-specific endpoint behavior, and places ASP.NET Core middleware in the order most deployments need.

```csharp
using System.Text;
using HotChocolate.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var isDevelopment = builder.Environment.IsDevelopment();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
        | ForwardedHeaders.XForwardedHost
        | ForwardedHeaders.XForwardedProto;

    // In production, configure KnownProxies or KnownNetworks for your edge.
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("GraphQLClients", policy =>
    {
        policy
            .WithOrigins("https://app.example.com")
            .WithMethods("GET", "POST")
            .WithHeaders(
                "Authorization",
                "Content-Type",
                "GraphQL-Preflight",
                "X-Requested-With");
    });
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Replace this sample key and validation settings with your real issuer,
        // audience, and key management.
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = "https://issuer.example.com",
            ValidAudience = "https://api.example.com",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("replace-with-a-secure-development-key"))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);

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
        options.EnforceGetRequestsPreflightHeader = true;
        options.MaxConcurrentExecutions = 64;
    });

var app = builder.Build();

app.UseForwardedHeaders();

if (!isDevelopment)
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCors("GraphQLClients");
app.UseAuthentication();
app.UseAuthorization();
app.UseWebSockets();

app.MapHealthChecks("/health");
app.MapHealthChecks("/alive", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("live")
});

app.MapGraphQL("/graphql")
    .WithOptions(options =>
    {
        options.Tool.Enable = isDevelopment;
        options.EnableSchemaRequests = isDevelopment;
        options.EnableSchemaFileSupport = isDevelopment;
    });

app.Run();

public sealed class Query
{
    public string GetStatus() => "ok";
}
```

Run the app and verify the endpoint:

```bash
curl -s http://localhost:5000/graphql \
  -H 'Content-Type: application/json' \
  --data '{"query":"{ status }"}'
```

Expected response:

```json
{
  "data": {
    "status": "ok"
  }
}
```

The example uses `builder.AddGraphQL(...)`, the v16 minimal hosting style. You can use `builder.Services.AddGraphQLServer(...)` instead when your project prefers service collection configuration.

# Choose the public GraphQL route

`app.MapGraphQL()` maps the integrated endpoint tree to `/graphql` by default. It is more than one exact URL. Under the mapped base path, Hot Chocolate handles HTTP POST, HTTP GET, multipart uploads, WebSocket upgrades when ASP.NET Core WebSockets are enabled, schema SDL downloads when enabled, and Nitro when enabled.

Use one stable public route and configure clients, probes, and proxies around it:

```csharp
app.MapGraphQL("/api/graphql");
```

With `app.MapGraphQL("/graphql")`, schema SDL requests are available when `EnableSchemaRequests` and `EnableSchemaFileSupport` are enabled. The integrated endpoint recognizes `?SDL`, `/graphql/schema`, `/graphql/schema/`, and `/graphql/schema.graphql`.

Use split endpoints only when you need separate routes or policies:

```csharp
app.MapGraphQLHttp("/graphql");          // POST, GET, multipart
app.MapGraphQLWebSocket("/graphql/ws");  // WebSocket subscriptions
app.MapGraphQLSchema("/graphql/sdl");    // SDL download
app.MapNitroApp("/graphql/ui")
    .WithOptions(options =>
    {
        options.GraphQLEndpoint = "/graphql";
    });
app.MapGraphQLPersistedOperations("/graphql/persisted");
```

Avoid mapping both integrated and split endpoints publicly unless you intend to expose both route shapes. Duplicate routes make proxy rules, auth policies, and client configuration harder to reason about.

# Put ASP.NET Core middleware in the right order

ASP.NET Core middleware order determines which features the GraphQL endpoint sees. Place middleware that modifies the request before `MapGraphQL()`.

```csharp
app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseCors("GraphQLClients");
app.UseAuthentication();
app.UseAuthorization();
app.UseWebSockets();

app.MapGraphQL("/graphql");
```

| Middleware                       | Required when                                                 | Must appear before                                                  | Common symptom when wrong                                       |
| -------------------------------- | ------------------------------------------------------------- | ------------------------------------------------------------------- | --------------------------------------------------------------- |
| `UseForwardedHeaders()`          | TLS terminates at a proxy or load balancer                    | Code that reads scheme, host, remote IP, or generates absolute URLs | Redirects use `http`, host is internal, client IP is the proxy  |
| `UseHttpsRedirection()` and HSTS | ASP.NET Core owns HTTPS redirect or HSTS policy               | Public endpoints                                                    | Redirect loops when forwarded headers are wrong                 |
| `UseCors()`                      | Browser clients call GraphQL cross-origin                     | `MapGraphQL()`                                                      | Browser preflight fails before GraphQL runs                     |
| `UseAuthentication()`            | Requests carry cookies, bearer tokens, or another auth scheme | `UseAuthorization()` and `MapGraphQL()`                             | `HttpContext.User` is empty in resolvers                        |
| `UseAuthorization()`             | ASP.NET Core endpoint policies or GraphQL authorization run   | `MapGraphQL()`                                                      | Protected endpoint or fields do not evaluate policies correctly |
| `UseWebSockets()`                | Clients use WebSocket subscriptions                           | `MapGraphQL()` or `MapGraphQLWebSocket()`                           | WebSocket upgrade returns HTTP failure                          |

Minimal APIs do not require `UseRouting()` and `UseEndpoints()` for this basic pattern. Use the legacy routing style only when your application already depends on it.

# Secure browser access with HTTPS, CORS, and preflight headers

Prefer HTTPS for all public GraphQL traffic. Configure CORS for exact browser origins and headers. Do not use `AllowAnyOrigin()` for production GraphQL APIs.

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("GraphQLClients", policy =>
    {
        policy
            .WithOrigins("https://app.example.com")
            .WithMethods("GET", "POST")
            .WithHeaders(
                "Authorization",
                "Content-Type",
                "GraphQL-Preflight",
                "X-Requested-With",
                "Client-Name",
                "Client-Version");
    });
});

var app = builder.Build();

app.UseCors("GraphQLClients");
app.MapGraphQL("/graphql");
```

Hot Chocolate can require a non-standard header on browser requests that otherwise look like simple cross-site requests:

```csharp
builder
    .AddGraphQL()
    .ModifyServerOptions(options =>
    {
        options.EnableGetRequests = true;
        options.EnforceGetRequestsPreflightHeader = true;
        options.EnforceMultipartRequestsPreflightHeader = true;
    });
```

`EnforceGetRequestsPreflightHeader` is `false` by default. Enable it when public browser GET requests are allowed and you want a browser preflight guard. `EnforceMultipartRequestsPreflightHeader` is `true` by default, so upload clients need an allowed header such as `GraphQL-Preflight`.

By default, GET requests are enabled for queries only. Disable GET if you do not need cacheable query URLs:

```csharp
app.MapGraphQL("/graphql")
    .WithOptions(options => options.EnableGetRequests = false);
```

# Add authentication and authorization at the right layer

ASP.NET Core authentication validates the request and creates `HttpContext.User`. ASP.NET Core authorization registers policies. Hot Chocolate authorization enables the GraphQL `@authorize` directive and field or type authorization middleware.

```csharp
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("EmployeesOnly", policy => policy.RequireAuthenticatedUser());
});

builder
    .AddGraphQL()
    .AddAuthorization()
    .AddQueryType<Query>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGraphQL("/graphql");
```

Use GraphQL field or type authorization for partial data access. Protect the entire endpoint only when every operation requires an authenticated user:

```csharp
app.MapGraphQL("/graphql").RequireAuthorization();
```

When authorization fails inside the GraphQL execution pipeline, Hot Chocolate returns GraphQL errors for protected fields instead of leaking data. See [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization) for field, type, role, and policy examples.

# Separate development, staging, and production behavior

Keep developer ergonomics in development and remove public production disclosure. Capture the environment before `builder.Build()` so options can use it.

```csharp
var isDevelopment = builder.Environment.IsDevelopment();

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .ModifyRequestOptions(options =>
    {
        options.IncludeExceptionDetails = isDevelopment;
    })
    .DisableIntrospection(disable: !isDevelopment);

var app = builder.Build();

app.MapGraphQL("/graphql")
    .WithOptions(options =>
    {
        options.Tool.Enable = isDevelopment;
        options.EnableSchemaRequests = isDevelopment;
        options.EnableSchemaFileSupport = isDevelopment;
    });
```

By default, Hot Chocolate v16 applies default security when you do not pass `disableDefaultSecurity: true` to `AddGraphQL` or `AddGraphQLServer`. Default security adds cost analysis, disables introspection outside development, and adds the max allowed field cycle depth rule. Keep default security enabled unless you replace every control intentionally.

Use `.DisableIntrospection(...)` for v16. Request-level allowlisting still uses `OperationRequestBuilder.AllowIntrospection()` in a request interceptor.

Never enable `IncludeExceptionDetails` on public production endpoints. It can expose exception messages and stack traces in GraphQL errors.

# Bound request size, execution time, batching, and concurrency

Production GraphQL hosts need several limits because one request can contain large JSON, many operations, deep selection sets, long-running resolvers, or multipart file streams.

```csharp
builder
    .AddGraphQL(maxAllowedRequestSize: 1 * 1000 * 1000)
    .AddQueryType<Query>()
    .ModifyRequestOptions(options =>
    {
        options.ExecutionTimeout = TimeSpan.FromSeconds(10);
    })
    .ModifyServerOptions(options =>
    {
        options.MaxConcurrentExecutions = 64;
        options.Batching = AllowedBatching.None;
    });
```

Important defaults:

| Limit                     |                                            Default | Production guidance                                                                                          |
| ------------------------- | -------------------------------------------------: | ------------------------------------------------------------------------------------------------------------ |
| GraphQL request body size |                           `20 * 1000 * 1024` bytes | Lower it for APIs that do not accept large documents or uploads.                                             |
| Execution timeout         | 30 seconds, or 30 minutes with a debugger attached | Set an explicit production timeout that matches resolver and downstream service behavior. Minimum is 100 ms. |
| Concurrent executions     |                                               `64` | Treat this as backpressure. `null` disables the gate. Load test before raising it.                           |
| Batching                  |                             `AllowedBatching.None` | Enable batching only for clients that need it. `MaxBatchSize` defaults to `1024` when batching is enabled.   |

File uploads need aligned limits beyond Hot Chocolate. Configure `FormOptions`, Kestrel, IIS or Azure App Service, and your reverse proxy so they agree on the maximum multipart body size. See [Files](/docs/hotchocolate/v16/server/files) for upload-specific configuration.

For deeper controls, combine hosting limits with [Request Limits](/docs/hotchocolate/v16/securing-your-api/request-limits), [Cost Analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis), [Batching](/docs/hotchocolate/v16/server/batching), [Trusted Documents](/docs/hotchocolate/v16/performance/trusted-documents), and [Automatic Persisted Operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations).

# Host behind Kestrel, IIS, Azure App Service, or a reverse proxy

Hot Chocolate runs inside ASP.NET Core. Kestrel can serve traffic directly, behind IIS, behind Azure App Service infrastructure, or behind a reverse proxy. The edge must support the GraphQL transports you expose.

Configure forwarded headers when TLS terminates before Kestrel:

```csharp
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
        | ForwardedHeaders.XForwardedHost
        | ForwardedHeaders.XForwardedProto;

    // Configure KnownProxies or KnownNetworks for your production edge.
});

var app = builder.Build();

app.UseForwardedHeaders();
```

Review this checklist for your edge infrastructure:

- POST bodies reach the GraphQL route.
- GET query strings reach the route if GET is enabled.
- Multipart uploads are allowed at every layer when uploads are enabled.
- WebSocket upgrades are enabled when subscriptions use WebSockets.
- Streaming responses are not buffered when clients use SSE, incremental delivery, or JSON Lines.
- Proxy idle timeouts are longer than expected operation streams and compatible with WebSocket keep-alive.
- Health probes hit readiness endpoints, not Nitro or the GraphQL IDE.
- Request size limits match across proxy, hosting platform, Kestrel, form parsing, and Hot Chocolate.
- Multiple app instances use a shared subscription provider when subscription events must cross instances. Avoid in-memory subscriptions for scaled-out production unless sticky sessions and disconnect behavior are acceptable.

For Azure App Service and IIS, confirm that WebSockets are enabled when needed and that request filtering, ARR, and platform timeouts match your GraphQL traffic profile.

# Support WebSockets, SSE, and graceful deployments

WebSocket subscriptions require ASP.NET Core WebSocket middleware before the GraphQL endpoint:

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

`ConnectionInitializationTimeout` defaults to 10 seconds. `KeepAliveInterval` defaults to 5 seconds and can be `null` to disable keep-alive pings. When you split endpoints, configure socket options on `MapGraphQLWebSocket()` if that route needs different settings.

SSE does not use a separate endpoint. Clients request it on the standard GraphQL HTTP endpoint with `Accept: text/event-stream`. Incremental delivery can use `multipart/mixed`, `text/event-stream`, or `application/jsonl` based on the `Accept` header.

Rolling deployments can close long-lived connections. Rely on client reconnect behavior, host shutdown timeouts, and subscription provider durability. Do not assume WebSocket deployments are disconnect-free.

# Warm the schema before receiving traffic

Hot Chocolate v16 initializes the schema and request executor eagerly during startup by default. Startup does not complete until schema creation and startup warmup tasks complete. This catches schema failures before the app becomes ready and reduces first-request latency.

Add warmup tasks for common operations to populate document and operation caches:

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

A request marked with `MarkAsWarmupRequest()` warms parse, validation, and preparation paths without executing resolvers. It also skips checks such as persisted operation enforcement so you can warm the executor with known documents. If clients send an operation name, include it because the operation name participates in operation cache keys:

```csharp
var request = OperationRequestBuilder.New()
    .SetDocument("query StatusWarmup { __typename }")
    .SetOperationName("StatusWarmup")
    .MarkAsWarmupRequest()
    .Build();
```

Lazy initialization shifts schema creation to the first request. That tradeoff is rarely a good fit for production APIs that need predictable readiness and latency.

# Expose health checks and observability

Use ASP.NET Core health checks for liveness and readiness. Hot Chocolate eager initialization and startup warmup block startup, so a normal readiness endpoint reflects GraphQL startup success. Add application-specific readiness checks for databases, caches, message brokers, and subscription backplanes.

```csharp
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);

var app = builder.Build();

app.MapHealthChecks("/health");
app.MapHealthChecks("/alive", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("live")
});
```

Keep health endpoints network-restricted or secured according to your platform rules.

Add Hot Chocolate instrumentation when you collect OpenTelemetry traces and metrics:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddInstrumentation();

builder.Services
    .AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation();
        tracing.AddHttpClientInstrumentation();
        tracing.AddHotChocolateInstrumentation();
        tracing.AddOtlpExporter();
    });
```

Combine GraphQL telemetry with ASP.NET Core, HttpClient, runtime, and database instrumentation so you can connect a slow operation to downstream calls. Avoid expensive synchronous diagnostic listeners on hot paths because they run as part of request execution.

# Troubleshoot production hosting failures

| Symptom                                    | Likely layer             | What to check                                                                                                          |
| ------------------------------------------ | ------------------------ | ---------------------------------------------------------------------------------------------------------------------- |
| `404` on `/graphql`                        | Routing or proxy         | Public path, app base path, proxy rewrite, and whether you split endpoints.                                            |
| Nitro opens in production                  | Endpoint options         | Gate `Tool.Enable` by environment.                                                                                     |
| Schema downloads in production             | Endpoint options         | Gate `EnableSchemaRequests` and `EnableSchemaFileSupport` by environment.                                              |
| Public introspection works                 | Security configuration   | Keep default security enabled or configure `.DisableIntrospection(disable: true)`.                                     |
| `401`, `403`, or missing user in resolvers | Auth pipeline            | Authentication scheme, token/cookie forwarding, `UseAuthentication()`, and `UseAuthorization()` order.                 |
| Browser preflight fails                    | CORS                     | Origin, method, and headers such as `Authorization`, `Content-Type`, `GraphQL-Preflight`, and client-specific headers. |
| WebSocket subscriptions fail               | WebSocket or proxy       | `UseWebSockets()`, proxy upgrade support, auth token protocol, and idle timeout.                                       |
| SSE or incremental delivery stalls         | Proxy or client headers  | Response buffering and `Accept` values such as `text/event-stream`, `multipart/mixed`, or `application/jsonl`.         |
| `413` or multipart failures                | Request size limits      | Hot Chocolate request size, `FormOptions`, Kestrel, IIS or Azure App Service, and proxy body limits.                   |
| First request is slow                      | Startup and cache warmup | Eager initialization, warmup tasks, and whether lazy initialization is enabled.                                        |
| Unexpected status code or content type     | HTTP negotiation         | Clients sending `Accept: application/json` receive legacy-style GraphQL HTTP behavior.                                 |

# Next steps

- [Endpoints](/docs/hotchocolate/v16/server/endpoints)
- [HTTP Transport](/docs/hotchocolate/v16/server/http-transport)
- [Authentication](/docs/hotchocolate/v16/securing-your-api/authentication)
- [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization)
- [Request Limits](/docs/hotchocolate/v16/securing-your-api/request-limits)
- [Cost Analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis)
- [Introspection](/docs/hotchocolate/v16/securing-your-api/introspection)
- [Warmup](/docs/hotchocolate/v16/server/warmup)
- [Instrumentation](/docs/hotchocolate/v16/server/instrumentation)
- [Files](/docs/hotchocolate/v16/server/files)
- [Batching](/docs/hotchocolate/v16/server/batching)
- [Trusted Documents](/docs/hotchocolate/v16/performance/trusted-documents)
- [Automatic Persisted Operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations)

For gateway deployment, use the Fusion deployment documentation when it is available. Fusion is a separate hosting concern from this single-service ASP.NET Core page.
