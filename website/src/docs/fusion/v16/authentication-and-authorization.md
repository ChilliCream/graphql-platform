---
title: "Authentication and Authorization"
---

# Authentication and Authorization

The mental model for auth in Fusion is straightforward: **authentication terminates at the gateway, authorization is a subgraph concern.**

The gateway validates tokens, extracts identity, and forwards the relevant information to subgraphs via HTTP headers. Each subgraph then uses standard HotChocolate authorization (`[Authorize]`, policies, claims) to enforce access control on its own fields. There is nothing Fusion-specific about subgraph authorization -- your subgraph is a HotChocolate server, and you use the same auth patterns you already know.

This page walks through the full auth chain: gateway-level authentication, header propagation, gateway-to-subgraph trust, and subgraph-level authorization.

## Gateway-Level Authentication

The gateway is the single entry point for all client requests. It is the right place to validate authentication tokens because:

- Clients only talk to the gateway. They never reach subgraphs directly.
- Token validation happens once, not N times across N subgraphs.
- Invalid requests are rejected before any subgraph work begins.

### JWT Bearer Setup

The most common pattern is JWT (JSON Web Token) authentication. Configure the gateway to validate tokens against your identity provider:

```csharp
// Gateway/Program.cs
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

// AddServiceDefaults registers shared configuration (OpenTelemetry, health checks, etc.)
// from a shared defaults project. Remove this line if your project does not use shared defaults.
builder.AddServiceDefaults("gateway-api", "1.0.0");

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

This is standard ASP.NET Core authentication -- the only Fusion-specific parts are `AddGraphQLGateway()` and the header propagation setup (covered next).

The numbered comments highlight the six key pieces:

1. **Header propagation registration** -- declares which headers the gateway forwards to subgraphs.
2. **Named HTTP client `"fusion"`** -- this is the client the gateway uses to call subgraphs. Adding `.AddHeaderPropagation()` to it ensures the declared headers are forwarded on every subgraph request.
3. **JWT authentication** -- standard ASP.NET Core JWT Bearer setup. Configure `Authority` and `Audience` for your identity provider (Keycloak, Auth0, Azure AD, etc.).
4. **Authorization** -- registers the authorization services.
5. **Fusion gateway** -- registers the gateway with its configuration source.
6. **Middleware order** -- `UseHeaderPropagation()` must come before `UseAuthentication()` so that the `Authorization` header is captured for propagation before the auth middleware consumes it. Then `UseAuthentication()` must come before `UseAuthorization()`.

### Allowing Anonymous Access

By default, ASP.NET Core does not require authentication unless you configure a fallback policy. If you want some queries to be accessible without a token (while still validating tokens when present), do not set a restrictive fallback:

```csharp
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = null; // Allow anonymous by default
});
```

With this configuration, unauthenticated requests pass through the gateway to subgraphs. Individual subgraph fields can then require authentication using `[Authorize]`.

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
- Internal lookups (marked `[Internal]`) would be exposed -- they are hidden from the composite schema but still exist as real HTTP endpoints on the subgraph.
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

## Subgraph-Level Authorization

Once headers arrive at a subgraph, authorization is entirely standard HotChocolate. There is nothing Fusion-specific here -- your subgraph is a HotChocolate server, and you use the same `[Authorize]` patterns you would use on a standalone GraphQL API.

### Subgraph Auth Setup

Each subgraph configures authentication and authorization in its `Program.cs`:

```csharp
// Products/Program.cs
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://your-identity-provider.com/realms/your-realm";
        options.Audience = "graphql-api";
        options.RequireHttpsMetadata = false; // For local development only
        options.TokenValidationParameters = new()
        {
            ValidateAudience = false,
            ValidateIssuer = true,
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization();

builder
    .AddGraphQL("products-api")
    .AddAuthorization()   // Enables HotChocolate's [Authorize] attribute
    .AddTypes();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapGraphQL();

app.RunWithGraphQLCommands(args);
```

Key details:

- **`AddGraphQL("products-api")`** is the named schema pattern used in production setups. The string argument is the schema name that matches the subgraph's `schema-settings.json` name. This is equivalent to `AddGraphQLServer()` used in the Getting Started tutorial -- `AddGraphQLServer()` is shorthand for an unnamed schema, while `AddGraphQL("name")` gives the schema an explicit name. Both work the same way for authorization.
- **`.AddAuthorization()`** on the GraphQL builder (not just `builder.Services`) enables HotChocolate's authorization integration, which makes `[Authorize]` work on GraphQL fields and types.
- The JWT configuration can mirror the gateway's configuration, or the subgraph can validate the forwarded `Authorization` header against the same identity provider.
- Subgraphs receive the raw `Authorization` header from the gateway via header propagation, so the JWT middleware validates the same token the gateway already validated.

### Using `[Authorize]` on Fields

Apply `[Authorize]` to restrict access to specific fields or types:

```csharp
[QueryType]
public static partial class ProductQueries
{
    // Anyone can browse products
    [Lookup, NodeResolver]
    public static async Task<Product?> GetProductByIdAsync(
        int id,
        IProductByIdDataLoader productById,
        CancellationToken cancellationToken)
        => await productById.LoadAsync(id, cancellationToken);

    // Only authenticated users can see pricing analytics
    [Authorize]
    public static PricingAnalytics GetPricingAnalytics(int productId)
        => AnalyticsService.GetForProduct(productId);
}
```

### Policy-Based Authorization

For more granular control, define authorization policies and reference them in `[Authorize]`:

```csharp
// Program.cs
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim("role", "admin"));

    options.AddPolicy("CanManageProducts", policy =>
        policy.RequireClaim("permissions", "products:write"));
});
```

```csharp
[MutationType]
public static partial class ProductMutations
{
    [Authorize(Policy = "CanManageProducts")]
    public static async Task<Product> UpdateProductAsync(
        int productId,
        string name,
        double price,
        ProductContext context,
        CancellationToken cancellationToken)
    {
        // Only users with "products:write" permission reach here
        var product = await context.Products.FindAsync(productId);
        product!.Name = name;
        product.Price = price;
        await context.SaveChangesAsync(cancellationToken);
        return product;
    }
}
```

### How Claims and Headers Arrive

When the gateway propagates the `Authorization` header, the subgraph's JWT middleware validates the token and populates `HttpContext.User` with the token's claims. You access claims the same way you would in any ASP.NET Core application:

```csharp
public static UserProfile GetMyProfile(
    IHttpContextAccessor httpContextAccessor)
{
    var user = httpContextAccessor.HttpContext?.User;
    var userId = user?.FindFirst("sub")?.Value;
    var email = user?.FindFirst("email")?.Value;
    // ...
}
```

If you injected custom headers at the gateway (like `X-User-Id` or `X-Tenant-Id`), read them from the request:

```csharp
var tenantId = httpContextAccessor.HttpContext?.Request.Headers["X-Tenant-Id"].ToString();
```

## Common Patterns

### Multi-Tenant Header Propagation

For multi-tenant applications, extract the tenant identifier at the gateway and propagate it as a header so every subgraph can filter data by tenant:

```csharp
// Gateway/Program.cs
builder.Services.AddHeaderPropagation(c =>
{
    c.Headers.Add("Authorization");
    c.Headers.Add("X-Tenant-Id", context =>
    {
        // Extract tenant from the JWT token's claims
        var tenantId = context.HttpContext.User.FindFirst("tenant_id")?.Value
            ?? "default";
        return new StringValues(tenantId);
    });
});
```

Each subgraph reads the tenant header and applies it as a data filter:

```csharp
// Subgraph middleware or resolver
public static async Task<List<Product>> GetProducts(
    IHttpContextAccessor httpContextAccessor,
    ProductContext context,
    CancellationToken cancellationToken)
{
    var tenantId = httpContextAccessor.HttpContext?.Request.Headers["X-Tenant-Id"].ToString();
    return await context.Products
        .Where(p => p.TenantId == tenantId)
        .ToListAsync(cancellationToken);
}
```

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

This is standard GraphQL error behavior -- the unauthorized field returns `null`, and the rest of the query succeeds. Design your schema accordingly: fields that might be unauthorized should be nullable so the query can still return useful data even when some fields are denied.

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

### Known Issues

**`ApplyPolicy` type name collision ([GitHub #6333](https://github.com/ChilliCream/graphql-platform/issues/6333)):** When both the gateway and subgraphs register authorization, the `ApplyPolicy` enum type can collide during composition. The workaround is to ensure authorization configuration is consistent across all services. This issue is tracked and being addressed.

**Header propagation with `InitializeOnStartup` ([GitHub #5547](https://github.com/ChilliCream/graphql-platform/issues/5547)):** In some configurations, header propagation may not work correctly when the gateway initializes eagerly. If you encounter missing headers on subgraph requests, verify that `UseHeaderPropagation()` appears before `UseAuthentication()` in the middleware pipeline, and that the `"fusion"` HTTP client has `.AddHeaderPropagation()` attached.

## Next Steps

- **"I need to deploy this securely"** -- [Deployment & CI/CD](/docs/fusion/v16/deployment-and-ci-cd) covers production deployment patterns including network isolation and CI pipeline setup.
- **"I need to handle subgraph failures gracefully"** -- Error handling and resilience, including partial results and retry policies, will be covered in future documentation.
- **"I want to monitor auth-related issues"** -- Monitoring and observability, including distributed tracing across gateway and subgraphs, will be covered in future documentation.
- **"Something is broken"** -- Check the middleware order section above and ensure `UseHeaderPropagation()` appears before `UseAuthentication()` in the pipeline.
