---
title: "Authentication and Authorization"
---

If you have configured auth in ASP.NET Core before, the Fusion gateway should feel familiar. The gateway uses the same middleware and options. The Fusion-specific part is how the gateway forwards identity to subgraphs.

Keep field-level authorization in the subgraph that owns the field. That keeps the rule close to the data it protects.

## Authentication

### Built on ASP.NET Core

Fusion does not define its own auth configuration format. Use the same ASP.NET Core APIs in the gateway that you use for JWT bearer tokens, OAuth, cookies, multiple schemes, and JWKS rotation.

That is the main difference from routers that configure JWT and JWKS in YAML. In Fusion, the auth code lives in `Program.cs`, next to the rest of your ASP.NET Core pipeline.

Useful links:

- [Authentication overview](https://learn.microsoft.com/aspnet/core/security/authentication/)
- [Authorization overview](https://learn.microsoft.com/aspnet/core/security/authorization/introduction)

### JWT bearer setup

For JWT bearer auth, configure `AddJwtBearer` with your identity provider's `Authority` and `Audience`.

The Fusion-specific part is the middleware order. Header propagation must run before authentication:

1. `UseHeaderPropagation`
2. `UseAuthentication`
3. `UseAuthorization`

That order lets the gateway capture the inbound `Authorization` header before the authentication middleware reads it. Fusion can then forward the same header through the named HTTP client.

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHeaderPropagation(options =>
{
    options.Headers.Add("Authorization");
    options.Headers.Add("GraphQL-Preflight");
});

builder.Services
    .AddHttpClient("fusion")
    .AddHeaderPropagation();

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
    .AddInMemoryConfiguration("./gateway.far");

var app = builder.Build();

app.UseHeaderPropagation();
app.UseAuthentication();
app.UseAuthorization();

app.MapGraphQL().RequireAuthorization();

app.Run();
```

Useful links:

- [Configure JWT bearer authentication](https://learn.microsoft.com/aspnet/core/security/authentication/configure-jwt-bearer-authentication)
- [ASP.NET Core middleware](https://learn.microsoft.com/aspnet/core/fundamentals/middleware/)
- `JwtBearerOptions` reference for `Authority`, `Audience`, multi-issuer settings, and JWKS settings.

### Anonymous and mixed access

Not every graph is fully private. If the gateway should allow both anonymous and authenticated requests, set `FallbackPolicy = null` and map GraphQL without `RequireAuthorization`.

```csharp
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = null;
});

app.MapGraphQL();
```

With that setup, subgraphs decide access per field. Use this for graphs with public fields, mixed public and private data, or a gradual migration from anonymous to authenticated traffic.

Useful link:

- [Authorization policies](https://learn.microsoft.com/aspnet/core/security/authorization/policies)

### Beyond JWT

Everything outside the Fusion wiring is standard ASP.NET Core:

- Use `AddCookie` for first-party browser clients.
- Use `AddPolicyScheme` and `ForwardDefaultSelector` for multiple schemes or tenant-based routing.
- Use `JwtBearerOptions` and `ConfigurationManager` for JWKS rotation, multiple issuers, and audience handling.

Useful links:

- [Cookie authentication](https://learn.microsoft.com/aspnet/core/security/authentication/cookie)
- [Policy schemes](https://learn.microsoft.com/aspnet/core/security/authentication/policyschemes)
- [`ConfigurationManager<T>`](https://learn.microsoft.com/dotnet/api/microsoft.identitymodel.protocols.configurationmanager-1)

### WebSocket and subscription auth

Subscriptions use `graphql-ws` or `graphql-sse`. For WebSocket subscriptions, browsers cannot rely on the normal `Authorization` header during the upgrade.

Send the token in the `connection_init` payload instead. Read that payload in HotChocolate's `OnConnect` hook on the gateway. Then apply the same auth rules you use for HTTP requests.

```csharp
builder.Services
    .AddGraphQLGateway()
    .AddSocketSessionInterceptor<JwtSocketSessionInterceptor>();
```

```csharp
public sealed class JwtSocketSessionInterceptor(ITokenValidator tokens)
    : DefaultSocketSessionInterceptor
{
    public override async ValueTask<ConnectionStatus> OnConnectAsync(
        ISocketSession session,
        IOperationMessagePayload connectionInitMessage,
        CancellationToken cancellationToken = default)
    {
        var payload = connectionInitMessage.As<Dictionary<string, string?>>();

        if (payload is null ||
            !payload.TryGetValue("authorization", out var header) ||
            string.IsNullOrWhiteSpace(header))
        {
            return ConnectionStatus.Reject("missing token");
        }

        var token = header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? header["Bearer ".Length..]
            : header;

        session.Connection.HttpContext.User =
            await tokens.ValidateAsync(token, cancellationToken);

        return ConnectionStatus.Accept();
    }
}

public interface ITokenValidator
{
    ValueTask<ClaimsPrincipal> ValidateAsync(
        string token,
        CancellationToken cancellationToken);
}
```

`ITokenValidator` is application code. Use the same validation settings you use for HTTP requests.

## Forwarding identity to subgraphs

### Header propagation

Fusion calls subgraphs through a named HTTP client. The default client name is `"fusion"`.

Use `AddHeaderPropagation` on that client. Make sure the `clientName` in each subgraph's `schema-settings.json` matches the client you configured in the gateway.

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

Forward only the headers subgraphs need. Common examples are `Authorization` and `GraphQL-Preflight`.

```csharp
builder.Services.AddHeaderPropagation(options =>
{
    options.Headers.Add("Authorization");
    options.Headers.Add("GraphQL-Preflight");
});

builder.Services
    .AddHttpClient("fusion")
    .AddHeaderPropagation();

// Later in the pipeline.
app.UseHeaderPropagation();
```

### Derive headers from claims

You can also derive headers from validated claims. For example, the gateway can send `X-User-Id` or `X-Tenant-Id` instead of making every subgraph parse the JWT again.

Use this when you want to avoid repeated JWT work or when subgraphs should not need signing-key configuration.

Treat these headers as trusted input only when the gateway-to-subgraph path is protected.

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

```csharp
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<ClaimsHeaderHandler>();

builder.Services
    .AddHttpClient("fusion")
    .AddHeaderPropagation()
    .AddHttpMessageHandler<ClaimsHeaderHandler>();
```

## Gateway-to-subgraph trust

### Keep subgraphs private

Subgraphs should not be public endpoints. If clients can reach a subgraph directly, they can bypass gateway-level rate limits, complexity checks, and any gateway-derived identity headers.

`[Internal]` hides fields from the composite schema. It does not remove the underlying HTTP endpoint from the subgraph.

### Use a private network by default

The default deployment shape is a private network: a VPC, an internal Kubernetes service, or an Azure VNet. Expose the gateway. Keep subgraphs internal.

### Add mTLS for production hardening

Use mTLS when subgraphs must verify that a request came from the gateway. The gateway presents a client certificate, and the subgraph validates it before accepting the request.

Useful links:

- [Certificate authentication](https://learn.microsoft.com/aspnet/core/security/authentication/certauth)
- [Kestrel client certificates](https://learn.microsoft.com/aspnet/core/fundamentals/servers/kestrel/endpoints#client-certificates)

Do not use a static `X-Internal-Api-Key` header as the trust boundary.

## Authorization

### Keep authorization in subgraphs

Subgraphs own the data and the schema. They should also own the field-level authorization rules.

That keeps the rule for who can see a field close to the field itself.

Fusion does not provide gateway-level `@authenticated` or `@requiresScopes` directives. Put those rules in the subgraph with HotChocolate's `@authorize`.

```csharp
public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor.Field(p => p.Price)
            .Authorize();

        descriptor.Field(p => p.WholesalePrice)
            .Authorize(policy: "internal");
    }
}
```

The tradeoff is that the gateway cannot reject every unauthorized operation before fanout. Use gateway-level authorization for coarse checks, and use subgraph authorization for field-level checks.

### Return partial results on authorization failure

GraphQL can return partial data when authorization denies a nullable field. In that case, the denied field is `null`, and the response includes an `errors[]` entry.

Make fields nullable when authorization can deny access. Use `AUTH_NOT_AUTHORIZED` for authorization failures.

```json
{
  "data": {
    "product": {
      "id": "1",
      "wholesalePrice": null
    }
  },
  "errors": [
    {
      "message": "The current user is not authorized to access this resource.",
      "path": ["product", "wholesalePrice"],
      "extensions": {
        "code": "AUTH_NOT_AUTHORIZED"
      }
    }
  ]
}
```

### Link to subgraph authorization

For more on `@authorize`, policies, and claims in Hot Chocolate subgraphs, see [Hot Chocolate authorization](/docs/hotchocolate/v16/securing-your-api/authorization).

## Pitfalls and reference

### Full middleware chain

Keep the gateway middleware in this order:

```csharp
app.UseHeaderPropagation();
app.UseAuthentication();
app.UseAuthorization();

app.MapGraphQL().RequireAuthorization();
```

### Common pitfalls

Check these first when auth does not work:

- `UseAuthentication` runs before `UseHeaderPropagation`.
- `.AddHeaderPropagation()` is missing on the `"fusion"` client.
- `clientName` in `schema-settings.json` does not match the gateway client.
- `IHttpContextAccessor` is used in a static resolver but is not registered.

## Next steps

- [Deployment and CI/CD](/docs/fusion/v16/deployment-and-ci-cd)
- [Hot Chocolate subgraph authorization](/docs/hotchocolate/v16/securing-your-api/authorization)
