---
title: "Authentication and Authorization"
---

The Fusion gateway builds on ASP.NET Core authentication and authorization. You can use the same features you use in any ASP.NET Core service, including JWT validation, cookie authentication, OpenID Connect, and mTLS.

Fusion does not add its own authentication or authorization mechanisms. It relies on ASP.NET Core, so you keep the same configuration model, extension points, and security-hardened framework you already use.

## Authenticating clients at the gateway

The gateway authenticates incoming requests with the same APIs as any ASP.NET Core service. There is no Fusion-specific configuration; use `AddAuthentication(...)` and `UseAuthentication()` as usual.

A minimal JWT bearer setup for the gateway:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://login.example.com/";
        options.Audience = "https://api.example.com";
    });

builder.Services.AddAuthorization();

builder.Services
    .AddGraphQLGateway()
    .AddFileSystemConfiguration("./gateway.far");

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGraphQL();

app.Run();
```

Everything in the `AddJwtBearer` callback is standard ASP.NET Core. For details on JWKS rotation, multi-issuer setups, custom audience validation, refresh policies, and clock skew handling, see the [Microsoft Learn reference](https://learn.microsoft.com/aspnet/core/security/authentication/configure-jwt-bearer-authentication). You do not need to implement those features yourself.

The example above configures your service to validate JWT access tokens at the gateway. It does not reject requests when the token is missing or invalid. To require authentication, add authorization to the GraphQL endpoint.

### Requiring authentication on the GraphQL endpoint

To reject anonymous requests at the gateway before they reach the GraphQL pipeline, attach an authorization policy to the endpoint:

```csharp
app.MapGraphQL().RequireAuthorization();
```

Or apply a named policy that requires a specific scope:

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("graphql:read", policy =>
        policy.RequireAuthenticatedUser()
              .RequireClaim("scope", "graphql:read"));
});

app.MapGraphQL().RequireAuthorization("graphql:read");
```

Without `RequireAuthorization`, anonymous requests reach the GraphQL pipeline, and any field not protected by the subgraphs is accessible without credentials. Whether this is acceptable depends on your API. For example, a public catalog with an authenticated checkout flow may leave the endpoint open and let subgraphs protect sensitive fields. An internal back-office gateway typically requires authentication at the edge.

### Cookies Authentication & Browser Clients

You can also use cookie authentication at the gateway and serve UI assets directly from it. For guidance, see [Cookie Authentication in ASP.NET Core](https://learn.microsoft.com/aspnet/core/security/authentication/cookie). If you are building a browser-based client, review [UI and SPA considerations](https://learn.microsoft.com/en-us/aspnet/core/client-side/spa/intro?view=aspnetcore-10.0).

In general, we **do NOT recommend** serving SPA applications directly from the gateway. Instead, put a Backend for Frontend (BFF) in front of the gateway. You can build your own BFF or [use Duende BFF](https://duendesoftware.com/products/bff). The BFF handles browser-specific authentication and session management, then calls the gateway with a token-based approach. This keeps the gateway focused on GraphQL traffic while the BFF manages browser authentication and session concerns.

## Forwarding identity to subgraphs

By default, the gateway does not forward headers to subgraphs. This means subgraphs do not receive the caller's access token or other identity information. Because authorization is usually handled by the subgraph, you typically need to pass the access token or relevant identity data so the subgraph can authorize requests correctly.

### Header propagation

Most often, you want to forward the `Authorization` header containing the access token to the subgraph. The subgraph can then validate the JWT and run authorization checks.

Fusion uses the official ASP.NET Core header propagation mechanism to forward headers to subgraphs:

```csharp
builder.Services.AddHeaderPropagation(options =>
{
    options.Headers.Add("Authorization");
    options.Headers.Add("GraphQL-Preflight");
});
```

See the [Microsoft Learn documentation on header propagation](https://learn.microsoft.com/aspnet/core/fundamentals/http-requests?view=aspnetcore-10.0#header-propagation) for more details.

You must configure header propagation on each HTTP client individually. Otherwise, it will not take effect. By default, Fusion uses a named HTTP client called `"fusion"` to call subgraphs. You can change this name in the `schema-settings.json` of each subgraph. If you keep the default, configure header propagation on the `"fusion"` client:

```json
{
  "transports": {
    "http": {
      "url": "http://products:5001/graphql",
      "clientName": "fusion"
    }
  }
}
```

Register header propagation for that client:

```csharp
builder.Services
    .AddHttpClient("fusion")
    .AddHeaderPropagation();
```

Add the `UseHeaderPropagation` middleware before the GraphQL middleware in your pipeline so headers are forwarded:

```csharp
// Later in the pipeline.
app.UseHeaderPropagation();

app.MapGraphQL();
```

### Authentication termination

In many enterprise scenarios, you may not want to forward the access token through the entire stack. With Fusion, you have full control over outgoing requests, so you can adapt the gateway to your authentication model and support custom authentication flows.

To intercept outgoing requests, add a `DelegatingHandler` to the HTTP client that calls subgraphs. This lets you derive headers from validated claims, exchange the incoming token for a new token with the correct audience, or call an external service to get additional caller information to forward.

```csharp
public sealed class ClaimsHeaderHandler(IHttpContextAccessor httpContextAccessor)
    : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var user = httpContextAccessor.HttpContext?.User;

        var userId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            request.Headers.TryAddWithoutValidation("X-User-Id", userId);
        }

        var tenantId = user?.FindFirst("tenant_id")?.Value;
        if (!string.IsNullOrEmpty(tenantId))
        {
            request.Headers.TryAddWithoutValidation("X-Tenant-Id", tenantId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
```

Register the handler and configure the client:

```csharp
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<ClaimsHeaderHandler>();

builder.Services
    .AddHttpClient("fusion")
    .AddHeaderPropagation()
    .AddHttpMessageHandler<ClaimsHeaderHandler>();
```

## Client-to-gateway trust (mTLS)

In some scenarios, only trusted clients should be able to call your gateway. Mutual TLS (mTLS) supports this by requiring the client to present a certificate. The gateway validates that certificate against a trusted certificate authority (CA).

Kestrel supports mTLS directly, and you can configure it with the `ConfigureKestrel` API. For details, see the [Microsoft Learn documentation](https://learn.microsoft.com/aspnet/core/security/authentication/certauth).

## Gateway-to-subgraph trust

### Keep subgraphs private

Subgraphs should not be public endpoints. GraphQL federation features like `@internal` assume subgraphs are not publicly accessible.

Place subgraphs on a private network, such as a VPC, Kubernetes cluster, or Azure VNet, that only the gateway can reach. This ensures all subgraph requests pass through the gateway and are subject to the gateway's composition, authentication, and authorization policies.

### mTLS to subgraphs

To ensure that only the gateway can call the subgraphs, use mutual TLS (mTLS) for gateway-to-subgraph communication.

Useful links:

- [Certificate authentication](https://learn.microsoft.com/aspnet/core/security/authentication/certauth)
- [Kestrel client certificates](https://learn.microsoft.com/aspnet/core/fundamentals/servers/kestrel/endpoints#client-certificates)

## Authorization

Authorization is a cross-cutting concern that involves both the gateway and the subgraphs. In internal or back-office scenarios, you usually want to reject unauthorized requests as early as possible at the gateway. In public scenarios, you may allow unauthorized requests to reach the subgraph and handle rejection there.

The subgraph ultimately decides whether a request can access specific data. Only the subgraph has the full context about the data and its business rules, so it is best suited to make authorization decisions.

For more information on subgraph-level authorization, `@authorize`, policies, and claims in Hot Chocolate subgraphs, see [Hot Chocolate authorization](../hotchocolate/securing-your-api/authorization.md).
