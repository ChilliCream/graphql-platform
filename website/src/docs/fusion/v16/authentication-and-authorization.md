---
title: "Authentication and Authorization"
---

The mental model for auth in Fusion is straightforward: **authentication terminates at the gateway, authorization is a subgraph concern.**

The gateway validates tokens, extracts identity, and forwards the relevant information to subgraphs via HTTP headers. Each subgraph then enforces access control on its own fields using its own authorization framework.

This page walks through the gateway side of the auth chain: authentication, header propagation, and gateway-to-subgraph trust.

## Gateway-Level Authentication

The gateway is the single entry point for all client requests. It is the right place to validate authentication tokens because:

- Clients only talk to the gateway. They never reach subgraphs directly.
- Token validation happens once, not N times across N subgraphs.
- Invalid requests are rejected before any subgraph work begins.

### JWT Bearer Setup

The most common pattern is JWT (JSON Web Token) authentication. Configure the gateway to validate tokens against your identity provider:

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

// 1. Register header propagation
builder.Services
    .AddCors()
    .AddHeaderPropagation(c =>
    {
        c.Headers.Add("GraphQL-Preflight");
        c.Headers.Add("Authorization");
    });

// 2. Configure the named HTTP client for subgraph communication
builder.Services
    .AddHttpClient("fusion")
    .AddHeaderPropagation();

// 3. Set up JWT authentication
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://your-identity-provider.com/realms/your-realm";
        options.Audience = "graphql-api";
        options.TokenValidationParameters = new()
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateLifetime = true
        };
    });

// 4. Set up authorization
builder.Services.AddAuthorization();

// 5. Configure the Fusion gateway
builder
    .AddGraphQLGateway()
    .AddFileSystemConfiguration("./gateway.far");

var app = builder.Build();

app.UseCors(c => c.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());

// 6. Middleware order matters
app.UseHeaderPropagation();
app.UseAuthentication();
app.UseAuthorization();

app.MapGraphQL();
app.Run();
```

This is standard ASP.NET Core authentication. The only Fusion-specific parts are `AddGraphQLGateway()` and the header propagation setup (covered next).

The numbered comments highlight the six key pieces:

1. **Header propagation registration.** Declares which headers the gateway forwards to subgraphs.
2. **Named HTTP client `"fusion"`.** This is the client the gateway uses to call subgraphs. Adding `.AddHeaderPropagation()` to it ensures the declared headers are forwarded on every subgraph request.
3. **JWT authentication.** Standard ASP.NET Core JWT Bearer setup. Configure `Authority` and `Audience` for your identity provider (Keycloak, Auth0, Azure AD, etc.).
4. **Authorization.** Registers the authorization services.
5. **Fusion gateway.** Registers the gateway with its configuration source.
6. **Middleware order.** `UseHeaderPropagation()` must come before `UseAuthentication()` so that the `Authorization` header is captured for propagation before the auth middleware consumes it. Then `UseAuthentication()` must come before `UseAuthorization()`.

### Allowing Anonymous Access

By default, ASP.NET Core does not require authentication unless you configure a fallback policy. If you want some queries to be accessible without a token (while still validating tokens when present), do not set a restrictive fallback:

```csharp
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = null; // Allow anonymous by default
});
```

With this configuration, unauthenticated requests pass through the gateway to subgraphs. Individual subgraph fields can then enforce their own authorization rules as needed.

## Header Propagation

Header propagation is how authentication context travels from the gateway to subgraphs. When a client sends a request with an `Authorization: Bearer <token>` header, the gateway forwards that header to each subgraph it calls during query execution.

### How It Works

The setup involves two parts:

**1. Declare which headers to propagate:**

```csharp
builder.Services.AddHeaderPropagation(c =>
{
    c.Headers.Add("GraphQL-Preflight");
    c.Headers.Add("Authorization");
});
```

This tells the header propagation middleware to capture these headers from incoming requests and make them available for outgoing requests.

**2. Attach propagation to the named HTTP client:**

```csharp
builder.Services
    .AddHttpClient("fusion")
    .AddHeaderPropagation();
```

The name `"fusion"` matters. This is the default named HTTP client that the Fusion gateway uses to communicate with subgraphs. The name must match the `clientName` in your subgraphs' `schema-settings.json` files (which defaults to `"fusion"` if not specified):

```json
{
  "name": "products-api",
  "transports": {
    "http": {
      "clientName": "fusion",
      "url": "http://localhost:5001/graphql"
    }
  }
}
```

When the gateway makes HTTP requests to subgraphs, the `"fusion"` client automatically includes the propagated headers.

### Custom Header Injection

Sometimes you need to forward derived information rather than raw headers. For example, after validating a JWT token, you might want to inject the user's ID or tenant ID as custom headers:

```csharp
builder.Services.AddHeaderPropagation(c =>
{
    c.Headers.Add("Authorization");

    // Inject custom headers derived from the authenticated user
    c.Headers.Add("X-User-Id", context =>
    {
        var userId = context.HttpContext.User.FindFirst("sub")?.Value;
        return new StringValues(userId);
    });

    c.Headers.Add("X-Tenant-Id", context =>
    {
        var tenantId = context.HttpContext.User.FindFirst("tenant_id")?.Value;
        return new StringValues(tenantId);
    });
});
```

Subgraphs can then read these headers from `HttpContext`:

```csharp
// In a subgraph resolver
public static Product? GetProductById(
    int id,
    IHttpContextAccessor httpContextAccessor)
{
    var tenantId = httpContextAccessor.HttpContext?.Request.Headers["X-Tenant-Id"].ToString();
    // Use tenantId for data filtering...
}
```

This pattern is useful when subgraphs should not parse JWT tokens themselves but still need identity information.

## Gateway-to-Subgraph Trust

In a production deployment, subgraphs should not be publicly accessible. The gateway is the only service clients talk to; subgraphs are internal services.

### Why This Matters

If subgraphs are publicly accessible:

- Clients could bypass the gateway and query subgraphs directly, skipping authentication.
- Internal lookups (marked `[Internal]`) would be exposed. They are hidden from the composite schema but still exist as real HTTP endpoints on the subgraph.
- Rate limiting, query complexity analysis, and other gateway-level protections are bypassed.

### Network Isolation Options

**Private network (recommended):** Deploy subgraphs in a private network (VPC, internal Kubernetes service, Azure VNet) that the gateway can reach but external clients cannot. This is the simplest and most secure option.

**Mutual TLS (mTLS):** Configure TLS certificates on both the gateway and subgraphs so they authenticate each other. This adds transport-level security even within a private network.

**Shared secret / API key:** Add a custom header (like `X-Internal-Api-Key`) via header propagation and validate it in subgraph middleware. This is simpler than mTLS but less secure:

```csharp
// Gateway: inject a shared secret header
builder.Services.AddHeaderPropagation(c =>
{
    c.Headers.Add("Authorization");
    c.Headers.Add("X-Internal-Api-Key", _ =>
        new StringValues("your-shared-secret"));
});
```

```csharp
// Subgraph: validate the shared secret in middleware
app.Use(async (context, next) =>
{
    var apiKey = context.Request.Headers["X-Internal-Api-Key"].ToString();
    if (apiKey != "your-shared-secret")
    {
        context.Response.StatusCode = 401;
        return;
    }
    await next();
});
```

In practice, most teams use private networking and do not need additional subgraph-level authentication for internal traffic. Choose the approach that matches your infrastructure and security requirements.

## Subgraph Authorization

Authorization is a subgraph concern. Since each subgraph is a standalone GraphQL server, you configure authorization using whatever framework your subgraph is built with. The gateway's role ends at forwarding identity information via headers. How subgraphs enforce access control is up to them.

For subgraphs built with HotChocolate, see the [HotChocolate Authorization docs](/docs/hotchocolate/v16/securing-your-api/authorization) for a complete guide on field-level authorization, policies, and claims-based access control.

### What Happens When Auth Fails Mid-Query

When a query touches multiple subgraphs and authorization fails on one field, the gateway returns a **partial result**. Authorized fields resolve normally, and unauthorized fields return `null` with an error in the `errors` array:

```json
{
  "data": {
    "productById": {
      "name": "Table",
      "price": 899.99,
      "costBreakdown": null
    }
  },
  "errors": [
    {
      "message": "The current user is not authorized to access this resource.",
      "path": ["productById", "costBreakdown"],
      "extensions": {
        "code": "AUTH_NOT_AUTHORIZED"
      }
    }
  ]
}
```

This is standard GraphQL error behavior. The unauthorized field returns `null`, and the rest of the query succeeds. Design your schema accordingly: fields that might be unauthorized should be nullable so the query can still return useful data even when some fields are denied.

### The Full Auth Middleware Chain

Here is the complete middleware chain for a gateway with JWT authentication and header propagation, annotated with what each step does:

```csharp
// Gateway/Program.cs
var builder = WebApplication.CreateBuilder(args);

// --- Service Registration ---

// Propagate headers to subgraphs
builder.Services
    .AddHeaderPropagation(c =>
    {
        c.Headers.Add("GraphQL-Preflight");
        c.Headers.Add("Authorization");
    });

// Named HTTP client for subgraph communication
builder.Services
    .AddHttpClient("fusion")
    .AddHeaderPropagation();

// JWT token validation
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://your-idp.com/realms/your-realm";
        options.Audience = "graphql-api";
    });

// Authorization policies
builder.Services.AddAuthorization();

// Fusion gateway
builder
    .AddGraphQLGateway()
    .AddFileSystemConfiguration("./gateway.far");

var app = builder.Build();

// --- Middleware Pipeline (order matters!) ---

app.UseHeaderPropagation();  // Capture headers for forwarding
app.UseAuthentication();      // Validate JWT tokens
app.UseAuthorization();       // Enforce authorization policies
app.MapGraphQL();             // Serve the GraphQL endpoint
app.Run();
```

## Next Steps

- **"I need to set up subgraph authorization."** The [HotChocolate Authorization docs](/docs/hotchocolate/v16/securing-your-api/authorization) cover field-level authorization, policies, and claims-based access control for HotChocolate subgraphs.
- **"I need to deploy this securely."** [Deployment & CI/CD](/docs/fusion/v16/deployment-and-ci-cd) covers production deployment patterns including network isolation and CI pipeline setup.
- **"Something is broken."** Check the middleware order section above and ensure `UseHeaderPropagation()` appears before `UseAuthentication()` in the pipeline.
