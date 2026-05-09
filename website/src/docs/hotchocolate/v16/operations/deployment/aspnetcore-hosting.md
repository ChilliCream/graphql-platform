---
title: ASP.NET Core Hosting
---

This guide explains how to host a single Hot Chocolate v16 GraphQL service on ASP.NET Core for production. It does not cover Fusion gateway hosting, which is a separate deployment topic.

You will start with a production-ready `Program.cs` and learn how to configure the endpoint, middleware, security, resource limits, reverse proxy, realtime transports, warmup, health checks, and telemetry.

# Prerequisites

Before you begin, ensure you have:

- .NET 8 or newer with ASP.NET Core minimal hosting
- Hot Chocolate v16 packages installed
- Existing schema types or resolvers
- A public GraphQL route (for example, `/graphql`)
- The browser origins that will access the service
- An authentication scheme if you expose protected data
- A deployment target (Kestrel, IIS, Azure App Service, a container platform, or a reverse proxy)
- A subscription provider if you use subscriptions (in-memory subscriptions are only suitable for a single app instance)

# Start with a Production-Ready Program.cs

The following example sets up a minimal ASP.NET Core host that exposes a small schema at `/graphql`. It configures environment-specific endpoint behavior and arranges middleware in the recommended order for production deployments.

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

To verify your setup, run the app and test the endpoint:

```bash
curl -s http://localhost:5000/graphql \
  -H 'Content-Type: application/json' \
  --data '{"query":"{ status }"}'
```

You should receive:

```json
{
  "data": {
    "status": "ok"
  }
}
```

This example uses `builder.AddGraphQL(...)`, which is the minimal hosting style in v16. If you prefer configuring through the service collection, you can use `builder.Services.AddGraphQLServer(...)` instead.

# Choose the Public GraphQL Route

By default, `app.MapGraphQL()` maps the integrated endpoint tree to `/graphql`. This covers more than a single URL. Under the base path, Hot Chocolate handles HTTP POST, HTTP GET, multipart uploads, WebSocket upgrades (when enabled), schema SDL downloads (when enabled), and Nitro (when enabled).

Pick a single, stable public route and configure your clients, probes, and proxies to use it:

```csharp
app.MapGraphQL("/api/graphql");
```

When you use `app.MapGraphQL("/graphql")`, schema SDL requests are available if you enable `EnableSchemaRequests` and `EnableSchemaFileSupport`. The integrated endpoint recognizes `?SDL`, `/graphql/schema`, `/graphql/schema/`, and `/graphql/schema.graphql`.

Use split endpoints only if you need separate routes or policies:

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

Avoid exposing both integrated and split endpoints publicly unless you intend to support both route shapes. Duplicating routes complicates proxy rules, authentication policies, and client configuration.

# Order ASP.NET Core Middleware Correctly

The order of ASP.NET Core middleware affects which features are available to your GraphQL endpoint. Always place middleware that modifies the request before you call `MapGraphQL()`.

```csharp
app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseCors("GraphQLClients");
app.UseAuthentication();
app.UseAuthorization();
app.UseWebSockets();

app.MapGraphQL("/graphql");
```

| Middleware                       | When Required                                              | Must Appear Before                        | Common Symptom if Misordered                          |
| -------------------------------- | ---------------------------------------------------------- | ----------------------------------------- | ----------------------------------------------------- |
| `UseForwardedHeaders()`          | TLS terminates at a proxy or load balancer                 | Code that reads scheme, host, remote IP   | Redirects use `http`, host is internal, wrong IP      |
| `UseHttpsRedirection()` and HSTS | ASP.NET Core manages HTTPS redirect or HSTS policy         | Public endpoints                          | Redirect loops if forwarded headers are misconfigured |
| `UseCors()`                      | Browser clients call GraphQL cross-origin                  | `MapGraphQL()`                            | Browser preflight fails before GraphQL runs           |
| `UseAuthentication()`            | Requests use cookies, bearer tokens, or other auth schemes | `UseAuthorization()` and `MapGraphQL()`   | `HttpContext.User` is empty in resolvers              |
| `UseAuthorization()`             | Endpoint or GraphQL authorization is needed                | `MapGraphQL()`                            | Protected endpoints or fields ignore policies         |
| `UseWebSockets()`                | Clients use WebSocket subscriptions                        | `MapGraphQL()` or `MapGraphQLWebSocket()` | WebSocket upgrade returns HTTP failure                |

Minimal APIs do not require `UseRouting()` or `UseEndpoints()` for this pattern. Use legacy routing only if your application already depends on it.

# Secure Browser Access: HTTPS, CORS, and Preflight Headers

Always use HTTPS for public GraphQL traffic. Configure CORS to allow only the specific browser origins and headers you need. Never use `AllowAnyOrigin()` for production GraphQL APIs.

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

Hot Chocolate can require a custom header on browser requests that would otherwise look like simple cross-site requests:

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

By default, `EnforceGetRequestsPreflightHeader` is `false`. Enable it if you allow public browser GET requests and want to require a preflight header. `EnforceMultipartRequestsPreflightHeader` is `true` by default, so upload clients must send an allowed header such as `GraphQL-Preflight`.

GET requests are enabled for queries by default. If you do not need cacheable query URLs, disable GET:

```csharp
app.MapGraphQL("/graphql")
    .WithOptions(options => options.EnableGetRequests = false);
```

# Add Authentication and Authorization at the Right Layer

ASP.NET Core authentication validates incoming requests and sets `HttpContext.User`. Authorization policies are registered with ASP.NET Core. Hot Chocolate's authorization enables the GraphQL `@authorize` directive and supports field or type-level authorization middleware.

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

Use GraphQL field or type authorization when you want to restrict access to specific data. Only require authorization for the entire endpoint if every operation needs an authenticated user:

```csharp
app.MapGraphQL("/graphql").RequireAuthorization();
```

If authorization fails within the GraphQL execution pipeline, Hot Chocolate returns GraphQL errors for protected fields rather than exposing data. See the Authorization documentation for more examples.

# Separate Development, Staging, and Production Behavior

Keep developer-friendly features in development and avoid exposing sensitive details in production. Capture the environment before calling `builder.Build()` so you can use it in your configuration.

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

By default, Hot Chocolate v16 applies default security unless you pass `disableDefaultSecurity: true` to `AddGraphQL` or `AddGraphQLServer`. Default security enables cost analysis, disables introspection outside development, and enforces a maximum field cycle depth. Only disable default security if you intentionally replace every control.

Use `.DisableIntrospection(...)` to control introspection in v16. For request-level allowlisting, use `OperationRequestBuilder.AllowIntrospection()` in a request interceptor.

Never enable `IncludeExceptionDetails` on public production endpoints. This setting exposes exception messages and stack traces in GraphQL errors.

# Set Limits: Request Size, Execution Time, Batching, and Concurrency

Production GraphQL servers must enforce limits because a single request can include large JSON payloads, multiple operations, deep selection sets, long-running resolvers, or multipart file uploads.

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

Key defaults and recommendations:

| Limit                     | Default value                     | Production guidance                                                                                         |
| ------------------------- | --------------------------------- | ----------------------------------------------------------------------------------------------------------- |
| GraphQL request body size | `20 * 1000 * 1024` bytes          | Lower this for APIs that do not accept large documents or uploads.                                          |
| Execution timeout         | 30 seconds (30 min with debugger) | Set a production timeout that matches resolver and downstream service behavior. Minimum is 100 ms.          |
| Concurrent executions     | `64`                              | This acts as backpressure. `null` disables the limit. Load test before increasing.                          |
| Batching                  | `AllowedBatching.None`            | Enable batching only for clients that require it. `MaxBatchSize` defaults to `1024` if batching is enabled. |

For file uploads, align limits across Hot Chocolate, `FormOptions`, Kestrel, IIS or Azure App Service, and your reverse proxy. All layers must agree on the maximum multipart body size. See the Files documentation for upload-specific configuration.

For more control, combine these hosting limits with request limits, cost analysis, batching, trusted documents, and automatic persisted operations.

# Host Behind Kestrel, IIS, Azure App Service, or a Reverse Proxy

Hot Chocolate runs inside ASP.NET Core. You can serve traffic directly with Kestrel, or run behind IIS, Azure App Service, or a reverse proxy. Your edge infrastructure must support the GraphQL transports you expose.

If TLS terminates before Kestrel, configure forwarded headers:

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

Check your edge infrastructure for the following:

- POST bodies reach the GraphQL route
- GET query strings reach the route if GET is enabled
- Multipart uploads are allowed at every layer if uploads are enabled
- WebSocket upgrades are enabled if you use subscriptions
- Streaming responses are not buffered for SSE, incremental delivery, or JSON Lines
- Proxy idle timeouts are longer than your longest operation streams and compatible with WebSocket keep-alive
- Health probes target readiness endpoints, not Nitro or the GraphQL IDE
- Request size limits are consistent across proxy, hosting platform, Kestrel, form parsing, and Hot Chocolate
- Multiple app instances use a shared subscription provider if subscription events must cross instances (avoid in-memory subscriptions for scaled-out production unless sticky sessions and disconnects are acceptable)

For Azure App Service and IIS, ensure WebSockets are enabled if needed, and that request filtering, ARR, and platform timeouts match your GraphQL traffic profile.

# Support WebSockets, SSE, and Graceful Deployments

To use WebSocket subscriptions, add the ASP.NET Core WebSocket middleware before the GraphQL endpoint:

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

The default `ConnectionInitializationTimeout` is 10 seconds. `KeepAliveInterval` defaults to 5 seconds and can be set to `null` to disable keep-alive pings. If you use split endpoints, configure socket options on `MapGraphQLWebSocket()` for routes that need different settings.

SSE (Server-Sent Events) does not require a separate endpoint. Clients request it on the standard GraphQL HTTP endpoint using `Accept: text/event-stream`. Incremental delivery can use `multipart/mixed`, `text/event-stream`, or `application/jsonl` depending on the `Accept` header.

Rolling deployments may close long-lived connections. Rely on client reconnect logic, host shutdown timeouts, and durable subscription providers. Do not expect WebSocket deployments to be free of disconnects.

# Warm the Schema Before Receiving Traffic

By default, Hot Chocolate v16 eagerly initializes the schema and request executor during startup. The app does not become ready until schema creation and warmup tasks complete. This approach catches schema failures early and reduces latency for the first request.

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

A request marked with `MarkAsWarmupRequest()` warms the parse, validation, and preparation paths without executing resolvers. It also skips checks like persisted operation enforcement, so you can warm the executor with known documents. If your clients send an operation name, include it, as the operation name is part of the operation cache key:

```csharp
var request = OperationRequestBuilder.New()
    .SetDocument("query StatusWarmup { __typename }")
    .SetOperationName("StatusWarmup")
    .MarkAsWarmupRequest()
    .Build();
```

Lazy initialization defers schema creation until the first request. This is rarely suitable for production APIs that require predictable readiness and latency.

# Expose Health Checks and Observability

Use ASP.NET Core health checks to monitor liveness and readiness. Because Hot Chocolate eagerly initializes the schema and runs warmup tasks at startup, a standard readiness endpoint reflects GraphQL startup success. Add additional readiness checks for databases, caches, message brokers, and subscription backplanes as needed.

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

Restrict health endpoints to your network or secure them according to your platform's requirements.

To collect OpenTelemetry traces and metrics, add Hot Chocolate instrumentation:

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

Combine GraphQL telemetry with ASP.NET Core, HttpClient, runtime, and database instrumentation. This lets you trace slow operations through downstream calls. Avoid expensive synchronous diagnostic listeners on hot paths, as they run during request execution.

# Troubleshoot Production Hosting Failures

| Symptom                                    | Likely Layer             | What to Check                                                                                                         |
| ------------------------------------------ | ------------------------ | --------------------------------------------------------------------------------------------------------------------- |
| `404` on `/graphql`                        | Routing or proxy         | Public path, app base path, proxy rewrite, and whether you split endpoints                                            |
| Nitro opens in production                  | Endpoint options         | Gate `Tool.Enable` by environment                                                                                     |
| Schema downloads in production             | Endpoint options         | Gate `EnableSchemaRequests` and `EnableSchemaFileSupport` by environment                                              |
| Public introspection works                 | Security configuration   | Keep default security enabled or configure `.DisableIntrospection(disable: true)`                                     |
| `401`, `403`, or missing user in resolvers | Auth pipeline            | Authentication scheme, token/cookie forwarding, `UseAuthentication()`, and `UseAuthorization()` order                 |
| Browser preflight fails                    | CORS                     | Origin, method, and headers such as `Authorization`, `Content-Type`, `GraphQL-Preflight`, and client-specific headers |
| WebSocket subscriptions fail               | WebSocket or proxy       | `UseWebSockets()`, proxy upgrade support, auth token protocol, and idle timeout                                       |
| SSE or incremental delivery stalls         | Proxy or client headers  | Response buffering and `Accept` values such as `text/event-stream`, `multipart/mixed`, or `application/jsonl`         |
| `413` or multipart failures                | Request size limits      | Hot Chocolate request size, `FormOptions`, Kestrel, IIS or Azure App Service, and proxy body limits                   |
| First request is slow                      | Startup and cache warmup | Eager initialization, warmup tasks, and whether lazy initialization is enabled                                        |
| Unexpected status code or content type     | HTTP negotiation         | Clients sending `Accept: application/json` receive legacy-style GraphQL HTTP behavior                                 |

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
