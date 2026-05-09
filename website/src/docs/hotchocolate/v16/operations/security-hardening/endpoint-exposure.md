---
title: Harden endpoint exposure
---

This page guides you through securing Hot Chocolate v16 endpoints in production. You will learn how to control which routes are accessible, who can use them, and how to enforce these decisions using ASP.NET Core and edge controls. This guidance applies to Hot Chocolate source-schema services. For Fusion gateway deployments, refer to the separate documentation.

By the end, you should be able to answer these questions for every route:

- Who can access it?
- Which HTTP methods or streaming transports are allowed?
- What schema discovery features are exposed?
- Which app, proxy, firewall, or CDN rules enforce the policy?

# Prerequisites: Understand your deployment surfaces

Before you begin, ensure you have:

- An ASP.NET Core app hosting Hot Chocolate v16.
- A clear decision about your API audience: public, first-party, partner, admin, internal, or developer-only.
- A list of clients that require features like ad-hoc operations, persisted operations, HTTP GET, multipart uploads, WebSocket subscriptions, SSE streaming, Nitro, schema files, or introspection.
- ASP.NET Core authentication and authorization set up before protecting private routes.
- The ability to test from outside your trusted network, not just from `localhost`.

Start by creating an endpoint inventory. Complete this before applying configuration from this page.

| Route                      | Audience                       | Auth policy           | Methods and transports       | CORS origins              | Proxy visibility                              | Schema discovery                |
| -------------------------- | ------------------------------ | --------------------- | ---------------------------- | ------------------------- | --------------------------------------------- | ------------------------------- |
| `/graphql`                 | App clients                    | `GraphQLAccess`       | POST                         | `https://app.example.com` | Public ingress                                | Introspection off, SDL off      |
| `/graphql/ws`              | App clients with subscriptions | `GraphQLAccess`       | WebSocket                    | Same as app               | Public ingress only if subscriptions are used | Introspection off               |
| `/graphql/ui`              | Developers                     | `Developers`          | Browser GET, POST from Nitro | Internal origin only      | VPN or internal ingress                       | Follows GraphQL endpoint policy |
| `/internal/graphql/schema` | CI or developers               | `Developers`          | GET                          | None or internal          | Internal only                                 | SDL file                        |
| `/graphql/persisted`       | First-party clients            | `GraphQLAccess`       | GET and POST                 | `https://app.example.com` | Public ingress                                | No ad-hoc documents             |
| `/health/live`             | Platform probe                 | None or platform auth | GET                          | None                      | Probe network                                 | None                            |

Your goal: classify every surface as public, authenticated, internal-only, development-only, or disabled.

# Start with a secure production baseline

When execution, tooling, schema files, and subscriptions require different policies, use split routes. The following `Program.cs` example configures authenticated HTTP execution, strict CORS, disables GET and multipart uploads, disables batching, enables Nitro only for development, and separates health checks.

```csharp
using HotChocolate.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);
var isDevelopment = builder.Environment.IsDevelopment();
var enableSubscriptions = builder.Configuration.GetValue("GraphQL:EnableSubscriptions", false);

builder.Services.AddCors(options =>
{
    options.AddPolicy("GraphQLClients", policy =>
    {
        policy
            .WithOrigins("https://app.example.com")
            .WithMethods("POST", "OPTIONS")
            .WithHeaders("Authorization", "Content-Type", "GraphQL-Preflight")
            .AllowCredentials();
    });
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(); // Configure issuer, audience, and signing keys for your identity provider.

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("GraphQLAccess", policy =>
        policy.RequireAuthenticatedUser());

    options.AddPolicy("Developers", policy =>
        policy.RequireAuthenticatedUser().RequireRole("Developer"));
});

builder.Services.AddHealthChecks();

builder
    .AddGraphQL(maxAllowedRequestSize: 1_048_576)
    .AddQueryType<Query>()
    .AddAuthorization()
    .ModifyRequestOptions(options =>
    {
        options.IncludeExceptionDetails = isDevelopment;
        options.ExecutionTimeout = TimeSpan.FromSeconds(10);
    })
    .ModifyServerOptions(options =>
    {
        options.MaxConcurrentExecutions = 64;
    });

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

if (enableSubscriptions)
{
    app.UseWebSockets();
}

app.MapGraphQLHttp("/graphql")
    .RequireCors("GraphQLClients")
    .RequireAuthorization("GraphQLAccess")
    .WithOptions(options =>
    {
        options.EnableGetRequests = false;
        options.EnableMultipartRequests = false;
        options.Batching = AllowedBatching.None;
        options.MaxConcurrentExecutions = 64;
    });

if (enableSubscriptions)
{
    app.MapGraphQLWebSocket("/graphql/ws")
        .RequireAuthorization("GraphQLAccess");
}

if (isDevelopment)
{
    app.MapNitroApp("/graphql/ui", "../graphql")
        .WithOptions(options =>
        {
            options.DisableTelemetry = true;
            options.IncludeCookies = false;
        });
}

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready")
    .RequireAuthorization("GraphQLAccess");

app.Run();
```

Expected behavior for this baseline:

- Unauthenticated POST requests to `/graphql` return `401 Unauthorized` or `403 Forbidden`, depending on your authentication scheme.
- Authenticated POST requests to `/graphql` execute GraphQL operations.
- GET requests to `/graphql` fall through the GraphQL HTTP pipeline and return `404 Not Found`.
- Multipart requests to `/graphql` return `404 Not Found` unless you enable uploads.
- `/graphql/ui` is not mapped in production, so it returns `404 Not Found`.
- SDL downloads are not mapped because the baseline uses `MapGraphQLHttp()`, not the integrated `MapGraphQL()` endpoint.
- Health checks are independent from GraphQL execution and tooling.

`builder.AddGraphQL(...)` uses the v16 hosting-builder API. It delegates to `AddGraphQLServer(...)` and keeps the default security policy unless you pass `disableDefaultSecurity: true`. Default security disables executable introspection outside development, adds cost analysis, and adds the max field-cycle validation rule. It does not disable Nitro or schema file routes when you map endpoints that serve them.

# Choose an exposure pattern before configuring endpoints

Endpoint exposure is a product decision before it is a code decision. Pick the row that matches the route you operate, then configure Hot Chocolate, ASP.NET Core, and the edge to match it.

| Pattern                       | Execution route                      | Nitro                    | Introspection                                      | SDL or schema route      | GET                                         | WebSocket              | SSE                            | CORS                     | Auth                            | Proxy exposure                                |
| ----------------------------- | ------------------------------------ | ------------------------ | -------------------------------------------------- | ------------------------ | ------------------------------------------- | ---------------------- | ------------------------------ | ------------------------ | ------------------------------- | --------------------------------------------- |
| Public API                    | Public `/graphql` may be intentional | Off                      | On only if your public API policy supports tooling | Separate decision        | Queries only, or off                        | Only for subscriptions | Only if streaming is supported | Exact browser origins    | Public or optional              | Public execution path only                    |
| Private or admin API          | Private `/graphql`                   | Off or developer-only    | Trusted users or environments only                 | Internal or off          | Off unless needed                           | Only if required       | Internal or tuned at proxy     | Exact app origins        | Required at endpoint and fields | Private ingress or strict public auth         |
| First-party trusted documents | `/graphql/persisted` in production   | Development-only         | Off in production                                  | Registry or CI export    | Often on for cacheable persisted operations | Only if required       | Only if required               | Exact app origins        | Required                        | Persisted route only                          |
| Partner API                   | Partner route or host                | Off                      | Contract-specific                                  | Private docs or registry | Contract-specific                           | Contract-specific      | Contract-specific              | Partner origins          | Required with scopes            | Partner ingress or WAF rules                  |
| Internal API                  | Internal route or host               | Internal developer route | Internal only                                      | Internal or CI export    | Usually off                                 | Only if required       | Only if required               | Internal origins         | Required                        | VPN, private ingress, or identity-aware proxy |
| Multi-tenant API              | Tenant route or host                 | Off                      | Tenant policy                                      | Registry or tenant docs  | Tenant policy                               | Tenant policy          | Tenant policy                  | Tenant origin validation | Required before execution       | Tenant-aware edge rules                       |

Public APIs may keep introspection on for ecosystem tooling, but Nitro is an operation runner and should usually stay off. Private and admin APIs should use endpoint authorization and field authorization together. Internal APIs still need authentication because internal paths often become reachable through proxies, debugging tunnels, or reused ingress rules.

# Split execution, tooling, schema, and persisted operation routes

`MapGraphQL()` is convenient for development and simple deployments. It maps an integrated route under `/graphql/{**slug}` by default and includes WebSocket, POST, multipart, GET, schema download, and Nitro. Use it when every integrated surface shares the same policy.

For production, split routes when policies differ:

```csharp
app.MapGraphQLHttp("/graphql")
    .RequireAuthorization("GraphQLAccess")
    .RequireCors("GraphQLClients");

app.MapGraphQLWebSocket("/graphql/ws")
    .RequireAuthorization("GraphQLAccess");

if (app.Environment.IsDevelopment())
{
    app.MapNitroApp("/graphql/ui", "../graphql");
}

app.MapGraphQLSchema("/internal/graphql/schema")
    .RequireAuthorization("Developers");

app.MapGraphQLPersistedOperations(
        "/graphql/persisted",
        requireOperationName: true)
    .RequireAuthorization("GraphQLAccess")
    .RequireCors("GraphQLClients");
```

Use the split middleware this way:

| API                                 | Default path                                | What it exposes                                                           | Production note                                      |
| ----------------------------------- | ------------------------------------------- | ------------------------------------------------------------------------- | ---------------------------------------------------- |
| `MapGraphQLHttp()`                  | `/graphql`                                  | GraphQL HTTP POST, multipart, and GET according to `GraphQLServerOptions` | Does not serve Nitro or schema files.                |
| `MapGraphQLWebSocket()`             | `/graphql/ws`                               | GraphQL WebSocket subscriptions                                           | Map only when subscriptions are part of the product. |
| `MapNitroApp()`                     | `/graphql/ui`                               | Nitro browser app                                                         | Protect or restrict to development.                  |
| `MapGraphQLSchema()`                | `/graphql/sdl`                              | SDL schema file                                                           | Treat as sensitive documentation for private APIs.   |
| `MapGraphQLSemanticNonNullSchema()` | `/graphql/semantic-non-null-schema.graphql` | SDL with semantic non-null metadata                                       | Protect like other schema files.                     |
| `MapGraphQLPersistedOperations()`   | `/graphql/persisted`                        | Persisted operation execution                                             | Treat as an execution route, not as a static asset.  |

Expected result: a request to `/graphql/ui` returns `404 Not Found` in production unless the app intentionally maps Nitro or protects it with a developer policy.

# Gate execution with endpoint authorization and schema authorization

Endpoint authorization and GraphQL authorization solve different problems. Endpoint authorization rejects unauthenticated HTTP traffic before Hot Chocolate executes an operation. GraphQL authorization enforces policies inside the schema for fields and types.

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("GraphQLAccess", policy =>
        policy.RequireAuthenticatedUser());
});

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGraphQLHttp("/graphql")
    .RequireAuthorization("GraphQLAccess");
```

Use `HotChocolate.Authorization.AuthorizeAttribute` or the `@authorize` directive for schema members. Do not use `Microsoft.AspNetCore.Authorization.AuthorizeAttribute` on GraphQL fields or types, because it does not run through the Hot Chocolate authorization pipeline.

```csharp
using HotChocolate.Authorization;

public sealed class Query
{
    public string Version() => "v1";

    [Authorize]
    public User GetMe(ClaimsPrincipal user) =>
        new(user.Identity?.Name ?? "unknown");
}
```

Expected outcomes:

| Request                                                       | Expected result                                                                |
| ------------------------------------------------------------- | ------------------------------------------------------------------------------ |
| Missing credentials to a protected endpoint                   | `401 Unauthorized` or `403 Forbidden`, no GraphQL execution.                   |
| Authenticated request without a required field role or policy | GraphQL authorization error and protected data is `null` or absent.            |
| Authenticated request with the required policy                | Data is returned.                                                              |
| Anonymous request to a public field on a public endpoint      | Public data is returned, unless endpoint authorization blocks the whole route. |

Use split routes when Nitro needs a `Developers` policy while execution needs a `GraphQLAccess` policy. If you call `RequireAuthorization()` on the integrated `MapGraphQL()` route, the policy applies to execution, Nitro, and integrated schema downloads together.

# Disable or protect Nitro in production

Nitro is useful for development, testing, and operations. It can run operations, send configured headers, include cookies if enabled, inspect schema information when discovery is allowed, and point to endpoints. Treat it as an interactive client, not as a static help page.

For integrated `MapGraphQL()` deployments, disable the tool outside development:

```csharp
app.MapGraphQL("/graphql")
    .WithOptions(options =>
    {
        options.Tool.Enable = app.Environment.IsDevelopment();
    });
```

For split routes, map Nitro only where it belongs:

```csharp
if (app.Environment.IsDevelopment())
{
    app.MapNitroApp("/graphql/ui", "../graphql")
        .WithOptions(options =>
        {
            options.DisableTelemetry = true;
            options.IncludeCookies = false;
            options.Title = "Local GraphQL";
        });
}
```

If production operations require Nitro, protect it with developer identity and network controls:

```csharp
app.MapNitroApp("/internal/graphql/ui", "/graphql")
    .WithOptions(options =>
    {
        options.DisableTelemetry = true;
        options.IncludeCookies = false;
        options.GraphQLEndpoint = "/graphql";
    })
    .RequireAuthorization("Developers");
```

Expected result: `/graphql/ui` returns `404 Not Found` when disabled or unmapped, and `401 Unauthorized` or `403 Forbidden` when protected. Do not use Nitro as a health probe, because opening an IDE does not prove that production execution, auth, or dependencies are healthy.

# Make schema discovery intentional

Hot Chocolate has separate controls for executable introspection and schema files.

Introspection controls the GraphQL fields `__schema` and `__type`. It does not disable `__typename`, which remains normal GraphQL behavior for clients that need runtime type names.

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .DisableIntrospection((services, _) =>
    {
        var environment = services.GetRequiredService<IHostEnvironment>();
        return !environment.IsDevelopment();
    });
```

When introspection is disabled, you can still allow it for trusted requests with an HTTP request interceptor:

```csharp
using HotChocolate.AspNetCore;
using HotChocolate.Execution;

public sealed class IntrospectionInterceptor : DefaultHttpRequestInterceptor
{
    public override ValueTask OnCreateAsync(
        HttpContext context,
        IRequestExecutor requestExecutor,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        if (context.User.IsInRole("Developer"))
        {
            requestBuilder.AllowIntrospection();
        }

        return base.OnCreateAsync(
            context,
            requestExecutor,
            requestBuilder,
            cancellationToken);
    }
}
```

Register the interceptor with the GraphQL server:

```csharp
builder
    .AddGraphQL()
    .DisableIntrospection()
    .AddHttpRequestInterceptor<IntrospectionInterceptor>();
```

Schema file exposure is different. Integrated `MapGraphQL()` can serve SDL when schema requests and schema file support are enabled. Candidates include `?sdl`, `?SDL`, `/schema`, `/schema/`, and `/schema.graphql` under the mapped GraphQL path. Explicit schema routes such as `MapGraphQLSchema()` and `MapGraphQLSemanticNonNullSchema()` serve schema files at their own paths.

Disable integrated schema downloads when you use the combined endpoint:

```csharp
app.MapGraphQL("/graphql")
    .WithOptions(options =>
    {
        options.EnableSchemaRequests = false;
        options.EnableSchemaFileSupport = false;
    });
```

Avoid public `MapGraphQLSchema()` routes for private APIs. Use CI schema export, a private schema registry, or an authenticated internal route instead.

Expected production checks:

```bash
curl -i https://api.example.com/graphql \
    -H 'Content-Type: application/json' \
    --data '{"query":"{ __schema { queryType { name } } }"}'
```

Expected response body when introspection is disabled:

```json
{
  "errors": [
    {
      "message": "Introspection is not allowed for the current request.",
      "extensions": {
        "field": "__schema",
        "code": "HC0046"
      }
    }
  ]
}
```

```bash
curl -i 'https://api.example.com/graphql?sdl'
curl -i 'https://api.example.com/graphql/schema.graphql'
```

Expected result for the baseline and for combined endpoints with schema file support disabled: `404 Not Found`.

# Restrict HTTP methods, uploads, batching, WebSockets, and SSE

Disable transports and request shapes until a client requirement proves that you need them.

```csharp
app.MapGraphQLHttp("/graphql")
    .WithOptions(options =>
    {
        options.EnableGetRequests = false;
        options.AllowedGetOperations = AllowedGetOperations.Query;
        options.EnforceGetRequestsPreflightHeader = true;
        options.EnableMultipartRequests = false;
        options.EnforceMultipartRequestsPreflightHeader = true;
        options.Batching = AllowedBatching.None;
        options.MaxBatchSize = 1;
        options.MaxConcurrentExecutions = 64;
    });
```

Use this checklist for each transport:

| Surface           | Default                                                        | Harden when unused                    | Keep when needed                                                                          |
| ----------------- | -------------------------------------------------------------- | ------------------------------------- | ----------------------------------------------------------------------------------------- |
| POST              | Enabled                                                        | Keep for normal execution             | Limit body size, timeout, cost, and auth.                                                 |
| GET               | Enabled, queries only                                          | Set `EnableGetRequests = false`       | Keep `AllowedGetOperations = Query`; consider `EnforceGetRequestsPreflightHeader = true`. |
| GET mutations     | Disabled by default                                            | Keep disabled                         | Enable only with a documented cache and CSRF model.                                       |
| Multipart uploads | Enabled by default                                             | Set `EnableMultipartRequests = false` | Register `Upload`, require preflight headers, and align upload limits.                    |
| Batching          | Disabled by default                                            | Keep `AllowedBatching.None`           | Enable selected flags and set `MaxBatchSize`.                                             |
| WebSocket         | Available when mapped and ASP.NET Core WebSockets are enabled  | Do not map `/graphql/ws`              | Map only for subscriptions and configure proxy upgrade support.                           |
| SSE               | Selected on the HTTP endpoint with `Accept: text/event-stream` | Do not advertise streaming operations | Configure proxy buffering, timeouts, CORS, and client auth for long-lived responses.      |

POST is the safest default for dynamic operations because proxies, analytics, and logs often capture query strings from GET. GET can be valuable for cacheable queries and persisted operations. Keep mutations off GET unless you have a specific reason and tests for every cache layer.

Expected checks for the baseline:

```bash
curl -i 'https://api.example.com/graphql?query={__typename}'
```

Expected result: `404 Not Found` because GET is disabled on the mapped HTTP pipeline.

```bash
curl -i https://api.example.com/graphql \
    -H 'GraphQL-Preflight: 1' \
    -F operations='{ "query": "mutation ($file: Upload!) { uploadFile(file: $file) }", "variables": { "file": null } }' \
    -F map='{ "0": ["variables.file"] }' \
    -F 0=@file.txt
```

Expected result: `404 Not Found` for the baseline because multipart requests are disabled. If your schema uses `Upload`, enable multipart deliberately and follow the [files](/docs/hotchocolate/v16/server/files) guidance.

# Configure CORS for known browser clients

CORS is ASP.NET Core behavior, not Hot Chocolate-specific behavior. It controls which browser origins may call your endpoint. It does not replace authentication, authorization, rate limits, or network controls.

Use named policies and exact origins:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("GraphQLClients", policy =>
    {
        policy
            .WithOrigins("https://app.example.com")
            .WithMethods("POST", "OPTIONS")
            .WithHeaders("Authorization", "Content-Type", "GraphQL-Preflight")
            .AllowCredentials();
    });

    options.AddPolicy("GraphQLStreamingClients", policy =>
    {
        policy
            .WithOrigins("https://app.example.com")
            .WithMethods("POST", "OPTIONS")
            .WithHeaders("Authorization", "Content-Type", "Accept", "GraphQL-Preflight")
            .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors();

app.MapGraphQLHttp("/graphql")
    .RequireCors("GraphQLClients");
```

Avoid production policies such as `AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()`. Credentials require explicit origins. Do not combine wildcard origins with cookies or bearer-token flows that browsers can send automatically.

Verify allowed and blocked preflight behavior:

```bash
curl -i -X OPTIONS https://api.example.com/graphql \
    -H 'Origin: https://app.example.com' \
    -H 'Access-Control-Request-Method: POST' \
    -H 'Access-Control-Request-Headers: content-type, authorization, graphql-preflight'
```

Expected result: response headers include `Access-Control-Allow-Origin: https://app.example.com`.

```bash
curl -i -X OPTIONS https://api.example.com/graphql \
    -H 'Origin: https://unknown.example' \
    -H 'Access-Control-Request-Method: POST' \
    -H 'Access-Control-Request-Headers: content-type, authorization'
```

Expected result: no `Access-Control-Allow-Origin` header for the unknown origin.

# Enforce the same policy at reverse proxies, firewalls, and CDNs

Kestrel is not the only place that controls exposure. The public surface is the combination of app routes, load balancers, reverse proxies, CDN rules, WAF rules, firewall rules, DNS, and logs.

Use a vendor-neutral allowlist like this:

| Path or pattern                                                                 | Public verbs              | Headers to preserve                                            | Cache policy                                           | Edge action                             |
| ------------------------------------------------------------------------------- | ------------------------- | -------------------------------------------------------------- | ------------------------------------------------------ | --------------------------------------- |
| `/graphql`                                                                      | `POST`                    | `Authorization`, `Content-Type`, `Accept`, `GraphQL-Preflight` | Do not cache authenticated responses                   | Allow only intended clients             |
| `/graphql/ws`                                                                   | WebSocket upgrade         | `Authorization`, protocol headers                              | No cache                                               | Allow only if subscriptions are enabled |
| `/graphql/persisted`                                                            | `GET`, `POST` when needed | `Authorization`, `Content-Type`, `Accept`                      | Cache only safe persisted queries with auth-aware keys | Allow when trusted documents are used   |
| `/graphql/ui`                                                                   | None publicly             | None                                                           | No cache                                               | Block, or internal-only                 |
| `/graphql/sdl`, `/graphql/schema*`, `/graphql/semantic-non-null-schema.graphql` | None publicly             | None                                                           | No cache                                               | Block, or internal-only                 |
| `?sdl` and `?SDL`                                                               | None publicly             | None                                                           | No cache                                               | Block query-string schema downloads     |
| `/health/live`                                                                  | `GET`                     | None                                                           | No cache                                               | Allow probe network                     |
| `/health/ready`                                                                 | `GET`                     | Platform auth if required                                      | No cache                                               | Internal or authenticated               |

Also review these edge settings:

- Preserve `Authorization`, `Content-Type`, `Accept`, and GraphQL preflight headers.
- Configure forwarded headers, HTTPS redirects, HSTS, and host allowlists according to ASP.NET Core guidance.
- Decide whether WebSocket upgrades are allowed.
- Disable response buffering and tune idle timeouts when SSE or incremental delivery is required.
- Do not cache authenticated GraphQL responses unless cache keys include every authorization-relevant input and the response is safe to share.
- Treat query strings as sensitive because GET operations and variables can appear in access logs.

Expected result: an external scan sees only the intended public paths and verbs. If local tests and internet scans disagree, inspect ingress, CDN, and WAF rules before changing Hot Chocolate settings.

# Expose health checks separately from GraphQL

Health checks should answer operational questions without exposing schema discovery or a developer tool. Do not use Nitro, introspection, schema download, or `{ __typename }` as a production health probe for a private API.

```csharp
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready")
    .RequireAuthorization("GraphQLAccess");
```

Use liveness for process-level checks that can be public to the platform probe network. Use readiness for dependency state and make it internal or authenticated when it reveals database, broker, or downstream status.

Expected checks:

```bash
curl -i https://api.example.com/health/live
```

Expected result: `200 OK` if public liveness is intended.

```bash
curl -i https://api.example.com/graphql
```

Expected result for private APIs: GraphQL remains protected even when health checks are reachable.

# Handle public, partner, and multi-tenant edge cases

Some APIs need a different exposure model. Keep the same inventory, but change the route policy intentionally.

| Scenario                      | Route policy                            | Auth                              | CORS                       | Schema discovery                             | Edge note                                                   |
| ----------------------------- | --------------------------------------- | --------------------------------- | -------------------------- | -------------------------------------------- | ----------------------------------------------------------- |
| Public developer API          | Public execution may be intentional     | Optional or required by operation | Exact docs and app origins | Introspection may stay on, SDL is separate   | Disable Nitro unless hosted as a protected tool.            |
| Partner API                   | Partner route, host, or gateway rule    | Partner tokens and scopes         | Partner origins            | Private docs, registry, or authenticated SDL | Log partner identity and enforce quotas.                    |
| Admin API                     | Separate route, host, or deployment     | Strong admin policy               | Admin app origins          | Internal only                                | Avoid sharing public customer routes.                       |
| Internal API                  | Private ingress or VPN                  | Required                          | Internal origins           | Internal or CI export                        | Keep auth even behind private networks.                     |
| First-party trusted documents | Persisted operation route in production | Required                          | App origins                | Registry or CI export                        | Use trusted documents to block ad-hoc operations.           |
| Tenant-specific domains       | Tenant-aware route or host              | Required before execution         | Dynamic origin validation  | Tenant policy                                | Resolve tenant before execution and before cache decisions. |

For first-party clients, combine endpoint exposure with trusted documents:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .UsePersistedOperationPipeline()
    .ModifyRequestOptions(options =>
    {
        options.PersistedOperations.OnlyAllowPersistedDocuments = true;
    });

app.MapGraphQLPersistedOperations("/graphql/persisted", requireOperationName: true)
    .RequireAuthorization("GraphQLAccess")
    .RequireCors("GraphQLClients");
```

Persisted operation routes are still execution routes. Protect them with the same auth, CORS, cache, and proxy rules as any other route that can return application data.

# Verify endpoint exposure before deployment

Run verification from outside the trusted network and record the results in the release checklist. Replace the host and token with values from your environment.

| Check                 | Command                                                                                                                                                                                                                | Expected result for the baseline                                           |
| --------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------- |
| Unauthorized POST     | `curl -i -X POST https://api.example.com/graphql -H 'Content-Type: application/json' --data '{"query":"{ __typename }"}'`                                                                                              | `401 Unauthorized` or `403 Forbidden`.                                     |
| Authorized POST       | `curl -i -X POST https://api.example.com/graphql -H 'Authorization: Bearer <token>' -H 'Content-Type: application/json' --data '{"query":"{ __typename }"}'`                                                           | `200 OK` with GraphQL JSON.                                                |
| GET disabled          | `curl -i 'https://api.example.com/graphql?query={__typename}'`                                                                                                                                                         | `404 Not Found`.                                                           |
| Introspection blocked | `curl -i -X POST https://api.example.com/graphql -H 'Authorization: Bearer <token>' -H 'Content-Type: application/json' --data '{"query":"{ __schema { types { name } } }"}'`                                          | GraphQL error with code `HC0046`, unless your policy allows introspection. |
| SDL blocked           | `curl -i 'https://api.example.com/graphql?sdl'`                                                                                                                                                                        | `404 Not Found`.                                                           |
| Schema file blocked   | `curl -i 'https://api.example.com/graphql/schema.graphql'`                                                                                                                                                             | `404 Not Found`.                                                           |
| Nitro blocked         | `curl -i https://api.example.com/graphql/ui`                                                                                                                                                                           | `404 Not Found`, or `401/403` if intentionally protected.                  |
| Allowed CORS          | `curl -i -X OPTIONS https://api.example.com/graphql -H 'Origin: https://app.example.com' -H 'Access-Control-Request-Method: POST' -H 'Access-Control-Request-Headers: content-type, authorization, graphql-preflight'` | Includes `Access-Control-Allow-Origin: https://app.example.com`.           |
| Blocked CORS          | `curl -i -X OPTIONS https://api.example.com/graphql -H 'Origin: https://unknown.example' -H 'Access-Control-Request-Method: POST'`                                                                                     | No `Access-Control-Allow-Origin` for the unknown origin.                   |
| Health                | `curl -i https://api.example.com/health/live`                                                                                                                                                                          | `200 OK` if public liveness is intended.                                   |
| WebSocket             | Use `websocat` or an HTTP/1.1 upgrade check against `/graphql/ws` when subscriptions are enabled.                                                                                                                      | Upgrade succeeds only when WebSockets are mapped and allowed at the proxy. |
| SSE                   | `curl -N -H 'Accept: text/event-stream' ...` for a streaming operation.                                                                                                                                                | Streams only when the schema and proxy support SSE.                        |

Example authorized GraphQL response:

```json
{ "data": { "__typename": "Query" } }
```

# Troubleshoot accidental exposure

| Symptom                                              | Likely cause                                                                                          | Fix                                                                                                |
| ---------------------------------------------------- | ----------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------- |
| Nitro appears in production                          | `MapGraphQL()` includes Nitro, or `MapNitroApp()` is mapped unconditionally.                          | Set `options.Tool.Enable = false`, gate `MapNitroApp()` by environment, or split routes.           |
| `RequireAuthorization()` hides Nitro too             | The integrated `MapGraphQL()` route shares one endpoint policy.                                       | Split Nitro and execution routes, then apply different policies.                                   |
| Schema still downloads after disabling introspection | SDL download is separate from executable introspection.                                               | Disable `EnableSchemaRequests` and `EnableSchemaFileSupport`, and remove public schema routes.     |
| Introspection still works in production              | `disableDefaultSecurity: true` is set, or `.DisableIntrospection(disable: false)` overrides defaults. | Remove the override or call `.DisableIntrospection()` outside trusted contexts.                    |
| Endpoint accepts unauthenticated requests            | `.AddAuthorization()` enables GraphQL field authorization but does not close the HTTP endpoint.       | Add `UseAuthentication()`, `UseAuthorization()`, and `RequireAuthorization()` to execution routes. |
| Browser calls work from unknown origins              | CORS is global, too broad, or added by a proxy.                                                       | Use named policies with exact origins and check edge headers.                                      |
| GET operations appear in logs                        | GET is enabled, persisted routes use GET, or Nitro is configured to use GET.                          | Disable GET for dynamic operations or adjust logging and cache policy.                             |
| WebSockets work unexpectedly                         | Integrated `MapGraphQL()` plus `UseWebSockets()` and proxy upgrades expose the WebSocket middleware.  | Split routes or block upgrades unless subscriptions are required.                                  |
| SSE is buffered or times out                         | Proxy buffering or idle timeouts do not allow `text/event-stream`.                                    | Disable buffering and tune timeouts for streaming routes.                                          |
| Local tests pass but internet scans differ           | Ingress, CDN, WAF, or firewall rules expose a different surface than Kestrel.                         | Test from outside and align edge path, verb, and header rules.                                     |

# Acceptance checklist

Before you deploy, confirm each item:

- Every public route has an owner, audience, auth policy, CORS policy, allowed methods, and proxy rule.
- Execution routes require the intended endpoint authorization or are explicitly public.
- Field and type authorization protect mixed public and private schemas.
- Nitro is disabled, development-only, or protected by developer auth and network controls.
- Introspection policy is explicit and tested.
- SDL and schema file policy is explicit and tested separately from introspection.
- GET, multipart, batching, WebSocket, and SSE are enabled only when required.
- CORS uses named policies with explicit origins, methods, and headers.
- Persisted operation routes are treated as execution routes and cacheable only when safe.
- Health checks are separate from GraphQL and do not reveal sensitive dependency details publicly.
- Proxy, CDN, WAF, firewall, logging, forwarded headers, and TLS rules match the app policy.
- Verification commands ran from outside trusted networks and results were recorded.

# Next steps

- Review [endpoints](/docs/hotchocolate/v16/server/endpoints) for the complete mapping API.
- Review [HTTP transport](/docs/hotchocolate/v16/server/http-transport) for response formats, GET, SSE, and incremental delivery.
- Review [authentication](/docs/hotchocolate/v16/securing-your-api/authentication) and [authorization](/docs/hotchocolate/v16/securing-your-api/authorization) before you protect private data.
- Review [introspection](/docs/hotchocolate/v16/securing-your-api/introspection), [request limits](/docs/hotchocolate/v16/securing-your-api/request-limits), and [cost analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis) for deeper hardening.
- Review [batching](/docs/hotchocolate/v16/server/batching), [trusted documents](/docs/hotchocolate/v16/performance/trusted-documents), and [automatic persisted operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations) if clients need request shape or operation storage features.
