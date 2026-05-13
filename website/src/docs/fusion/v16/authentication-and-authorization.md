---
title: "Authentication and Authorization"
---

The fusion gateway builds on top of the authentication and authorization capabilities of ASP.NET Core. This means that you can use all the features you are used to, such as JWT validation, cookie authentication, open id connect, MTLS and much more.
Fusion itself does not add any new authentication or authorization mechanisms on top of what ASP.NET core already provides. You have the full power of ASP.NET Core at your disposal, to make authentication and authorization your own while at the same time using a battle tested and security hardened framework.

## Authenticating clients at the gateway

The gateway authenticates incoming requests using the same APIs you would use on any ASP.NET Core service. There is no Fusion-specific configuration shape; you call `AddAuthentication(...)` and `UseAuthentication()`.

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
    .AddInMemoryConfiguration("./gateway.far");

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGraphQL();

app.Run();
```

Everything in the `AddJwtBearer` callback is standard ASP.NET Core. For JWKS rotation, multi-issuer setups, custom audience validation, refresh policies, and clock skew handling, see the Microsoft Learn reference at <https://learn.microsoft.com/aspnet/core/security/authentication/configure-jwt-bearer-authentication>. You do not need to wire any of that yourself.

The example above does configure your service to validate the JWT access token at the gateway, but it does not restrcts access to anything in case you do not provide a token or you present a invalid token.

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

Without `RequireAuthorization`, anonymous requests reach the GraphQL pipeline and any field that the subgraphs do not protect is reachable without credentials. Whether you want that depends on the API. A public catalog with a small authenticated checkout flow typically leaves the endpoint open and lets the subgraphs reject the protected fields. An internal back-office gateway typically requires authentication at the edge.

### Cookies Authentication & Browser Clients

You can also use cookie authentication at the gateway and servce the UI assets directly from there. If you are interested in doing so, [Cookie Authentication in ASP.NET Core](https://learn.microsoft.com/aspnet/core/security/authentication/cookie) is a great resource to get you started. [UI and SPA considerations](https://learn.microsoft.com/en-us/aspnet/core/client-side/spa/intro?view=aspnetcore-10.0) are especially relevant if you are building a browser-based client application.

Generally, we **do NOT recommend** service SPA applications directly from the gateway. Instead, we recommend fronting the gateway with a Backend for Frontend (BFF) (build your own or [use Duende BFF](https://duendesoftware.com/products/bff)) that handles the browser-specific authentication and session management concerns, and then have the BFF call the gateway with a more traditional token-based approach. This allows you to keep the gateway focused on doing one thing right, while the BFF can handle the complexities of browser-based authentication and session management.

## Forwarding identity to subgraphs

By default, the gateway does not forward any headers to the subgraphs. This also means that your subgraph does not receive the access token of the caller or other identity information. As authorization typically is a concern of the subgraph, you most likely want to pass the access token or other identity information to the subgraph so it can make informed decisions about allowing or rejecting the request.

### Header propagation

Most commonly, you just want to forward the `Authorization` header with the access token to the subgraph and do the JWT validation and authorization checks there.

Fusion uses the offical header propagation mechanism of ASP.NET Core to forward headers to the subgraphs.

```csharp
builder.Services.AddHeaderPropagation(options =>
{
    options.Headers.Add("Authorization");
    options.Headers.Add("GraphQL-Preflight");
});
```

You can read more about [header propagation in the Microsoft Learn documentation](https://learn.microsoft.com/aspnet/core/fundamentals/http-requests?view=aspnetcore-10.0#header-propagation).

Header propagation must be configured on each http client specificially otherwise it will not have any affect. By default fusion uses a named http client with the name `"fusion"` to call the subgraphs. You can change this name in the `schema-settings.json` of each subgraph, but if you do not change it, you need to configure header propagation on the `"fusion"` client.

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

Use `AddHeaderPropagation` on that client.

```csharp

builder.Services
    .AddHttpClient("fusion")
    .AddHeaderPropagation();

```

Lastly, you need to add the `UseHeaderPropagation` middleware in the pipeline before the GraphQL middleware to ensure that the headers are actually forwarded to the subgraphs.

```csharp
// Later in the pipeline.
app.UseHeaderPropagation();

app.MapGraphQL();
```

### Authentication Termination

It's farily common in enterprises to not _just_ route the access token through the whole stack. With fusion, you have full control over outgoing requests. This way every authentication scenario you can think of is possible.

To intercept outgoing requests, you can add a `DelegatingHandler` to the http client that calls the subgraphs. This allows you to do things like, deriving headers from validated claims, exchanging the incoming token for a new token with the right audience for the subgraph, or even call an external service to get additional information about the caller that you can then forward to the subgraph.

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

## Client-to-gateway trust (mtls)

In some scenarios, you want to ensure that only trusted clients can call your gateway. This is typically done using mutual TLS (mTLS) where the client presents a certificate to the gateway and the gateway validates that certificate against a trusted certificate authority (CA).

This feature is built into Kestrel and can be configured using the `ConfigureKestrel` API. You can find more information about how to do this [in the Microsoft Learn documentation](https://learn.microsoft.com/aspnet/core/security/authentication/certauth)

## Gateway-to-subgraph trust

### Keep subgraphs private

Subgraphs should not be public endpoints. GraphQL federation has concepts like `@internal` that assume no public access to the subgraph.
It generally is a good idea to keep them on a private network (vpc, kuberentes, azure vnet ) that only allows the gateway to call them. This way you can be sure that all requests to the subgraph go through the gateway and are subject to the composition, authentication and authorization policies of the gateway.

### mTLS to subgraphs

If you want to ensure that only the gateway can call the subgraphs, you can also use mutual TLS (mTLS) for the gateway-to-subgraph communication.

Useful links:

- [Certificate authentication](https://learn.microsoft.com/aspnet/core/security/authentication/certauth)
- [Kestrel client certificates](https://learn.microsoft.com/aspnet/core/fundamentals/servers/kestrel/endpoints#client-certificates)

## Authorization

Authorization is generally a cross-cutting concern that affects both the gateway and the subgraphs. In internal, back-office scenarios, you typically want to reject unauthorized requests as early as possible at the gateway, while in public scenarios you might want to let unauthorized requests reach the subgraph and only reject them there.
In any case, it's the subgraph that will decide whether a request is authorized to access specific data or not. As only the subgraph has the full context about the data and the business rules around it, it is the only one that can make informed decisions about whether a request should be allowed or not.

For more on subgraph-level authorization, `@authorize`, policies, and claims in Hot Chocolate subgraphs, see [Hot Chocolate authorization](../hotchocolate/securing-your-api/authorization.md).
