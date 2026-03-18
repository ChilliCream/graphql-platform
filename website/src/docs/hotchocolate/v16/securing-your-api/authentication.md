---
title: Authentication
---

Authentication determines a user's identity. It is a prerequisite for authorization and also lets you access the authenticated user in your resolvers, for example to build a `me` field that returns the current user's profile.

Hot Chocolate integrates with the ASP.NET Core authentication system, so you can reuse existing authentication configuration and any supported authentication provider.

[Learn more about authentication in ASP.NET Core](https://docs.microsoft.com/aspnet/core/security/authentication)

# Setup

Setting up authentication follows the same pattern as any ASP.NET Core application. The example below uses JWT bearer tokens, but you can substitute any authentication scheme that ASP.NET Core supports.

## 1. Install the JWT Bearer Package

<PackageInstallation packageName="Microsoft.AspNetCore.Authentication.JwtBearer" external />

## 2. Register the Authentication Scheme

```csharp
// Program.cs
var signingKey = new SymmetricSecurityKey(
    Encoding.UTF8.GetBytes("MySuperSecretKey"));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = "https://auth.chillicream.com",
            ValidAudience = "https://graphql.chillicream.com",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey
        };
    });
```

> This is an example configuration. Use a proper key management solution for production.

## 3. Add Authentication Middleware

Register the authentication middleware in the request pipeline:

```csharp
// Program.cs
app.UseRouting();
app.UseAuthentication();

app.UseEndpoints(endpoints =>
{
    endpoints.MapGraphQL();
});
```

## 4. Install the Hot Chocolate Authorization Package

<PackageInstallation packageName="HotChocolate.AspNetCore.Authorization" />

## 5. Register Authorization on the Schema

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddAuthorization()
    .AddQueryType<Query>();
```

Calling `AddAuthorization()` on the `IRequestExecutorBuilder` registers the `@authorize` directive and makes the authenticated user's identity available to resolvers. It does not lock out unauthenticated users. To restrict access, use [authorization](/docs/hotchocolate/v16/securing-your-api/authorization).

# Accessing the ClaimsPrincipal

After authentication, the `ClaimsPrincipal` of the current user is available in your resolvers.

<ExampleTabs>
<Implementation>

```csharp
// Types/UserQueries.cs
[QueryType]
public static partial class UserQueries
{
    public static User? GetMe(ClaimsPrincipal claimsPrincipal, UserService users)
    {
        var userId = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId is null)
        {
            return null;
        }

        return users.GetById(userId);
    }
}
```

</Implementation>
<Code>

```csharp
// Types/UserQueriesType.cs
public class UserQueriesType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("me")
            .Resolve(context =>
            {
                var claimsPrincipal = context.GetUser();
                var userId = claimsPrincipal?.FindFirstValue(ClaimTypes.NameIdentifier);

                if (userId is null)
                {
                    return null;
                }

                var users = context.Service<UserService>();
                return users.GetById(userId);
            });
    }
}
```

</Code>
</ExampleTabs>

Use `ClaimsPrincipal` to read claims such as the user ID, email, or roles:

```csharp
var userId = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
var email = claimsPrincipal.FindFirstValue(ClaimTypes.Email);
var isAdmin = claimsPrincipal.IsInRole("Administrator");
```

# Modifying the ClaimsPrincipal

If you need to add claims or identities to the `ClaimsPrincipal` before it reaches your resolvers, register an `IHttpRequestInterceptor`:

```csharp
// Interceptors/HttpRequestInterceptor.cs
public class HttpRequestInterceptor : DefaultHttpRequestInterceptor
{
    public override ValueTask OnCreateAsync(
        HttpContext context,
        IRequestExecutor requestExecutor,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        var identity = new ClaimsIdentity();
        identity.AddClaim(new Claim(ClaimTypes.Country, "us"));
        context.User.AddIdentity(identity);

        return base.OnCreateAsync(context, requestExecutor, requestBuilder, cancellationToken);
    }
}
```

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddHttpRequestInterceptor<HttpRequestInterceptor>();
```

[Learn more about interceptors](/docs/hotchocolate/v16/server/interceptors)

# Troubleshooting

## ClaimsPrincipal is empty or unauthenticated

Verify that `UseAuthentication()` is called before `MapGraphQL()` in the middleware pipeline. If the middleware order is incorrect, the authentication handler never processes the request.

## JWT token is not validated

Check that the token validation parameters (issuer, audience, signing key) match the values used to generate the token. Enable detailed errors in development to see the specific validation failure.

## "Bearer error=invalid_token" in response headers

This usually means the token is expired or the signing key does not match. Inspect the token at [jwt.io](https://jwt.io) to verify its claims and expiration.

## Claims are missing

Verify that the identity provider includes the expected claims in the token. Some providers require explicit scope or claim configuration. Check the `ClaimsPrincipal.Claims` collection in a resolver to see what was parsed.

# Next Steps

- **Need to restrict access to fields?** See [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization).
- **Need an overview of security options?** See [Security Overview](/docs/hotchocolate/v16/security).
- **Need to customize request handling?** See [Interceptors](/docs/hotchocolate/v16/server/interceptors).
