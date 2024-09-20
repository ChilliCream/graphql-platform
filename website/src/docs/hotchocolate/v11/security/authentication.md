---
title: Authentication
---

Authentication allows us to determine a user's identity. This is of course a prerequisite for authorization, but it also allows us to access the authenticated user in our resolvers. This is useful, if we for example want to build a `me` field that fetches details about the authenticated user.

Hot Chocolate fully embraces the authentication capabilities of ASP.NET Core, making it easy to reuse existing authentication configuration and integrating a variety of authentication providers.

[Learn more about authentication in ASP.NET Core](https://docs.microsoft.com/aspnet/core/security/authentication)

# Setup

Setting up authentication is largely the same as in any other ASP.NET Core application.

**In the following example we are using JWTs, but we could use any other authentication scheme supported by ASP.NET Core.**

1. Install the `Microsoft.AspNetCore.Authentication.JwtBearer` package

<PackageInstallation packageName="Microsoft.AspNetCore.Authentication.JwtBearer" external />

2. Register the JWT authentication scheme

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes("MySuperSecretKey"));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        ValidIssuer = "https://auth.chillicream.com",
                        ValidAudience = "https://graphql.chillicream.com",
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = signingKey
                    };
            });
    }
}
```

> Warning: This is an example configuration that's not intended for use in a real world application.

3. Register the ASP.NET Core authentication middleware with the request pipeline by calling `UseAuthentication`

```csharp
public class Startup
{
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();

        app.UseAuthentication();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGraphQL();
        });
    }
}
```

The above takes care of parsing and validating an incoming HTTP request.

In order to make the authentication result available to our resolvers, we need to complete some additional, Hot Chocolate specific steps.

1. Install the `HotChocolate.AspNetCore.Authorization` package

<PackageInstallation packageName="HotChocolate.AspNetCore.Authorization" />

2. Call `AddAuthorization()` on the `IRequestExecutorBuilder`

```csharp
services
    .AddGraphQLServer()
    .AddAuthorization()
    .AddQueryType<Query>();
```

All of this does not yet lock out unauthenticated users. It only exposes the identity of the authenticated user to our application through a `ClaimsPrincipal`. If we want to prevent certain users from querying our graph, we need to utilize authorization.

[Learn more about authorization](/docs/hotchocolate/v11/security/authorization)

# Accessing the ClaimsPrincipal

The [ClaimsPrincipal](https://docs.microsoft.com/dotnet/api/system.security.claims.claimsprincipal) of an authenticated user can be accessed in our resolvers like the following.

<ExampleTabs>
<Implementation>

```csharp
public class Query
{
    public User GetMe(ClaimsPrincipal claimsPrincipal)
    {
        // Omitted code for brevity
    }

    // before v11.3.1
    public User GetMeLegacy(
        [GlobalState(nameof(ClaimsPrincipal))] ClaimsPrincipal claimsPrincipal)
    {
        // Omitted code for brevity
    }
}
```

</Implementation>
<Code>

```csharp
public class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("me")
            .Resolve(context =>
            {
                var claimsPrincipal = context.GetUser();
                // before v11.3.1
                var claimsPrincipal = context.GetGlobalValue<ClaimsPrincipal>(
                                        nameof(ClaimsPrincipal));

                // Omitted code for brevity
            });
    }
}
```

</Code>
<Schema>

```csharp
services
    .AddGraphQLServer()
    .AddDocumentFromString(@"
        type Query {
          me: User
          meLegacy: User
        }
    ")
    .AddResolver("Query", "me", (context) =>
    {
        var claimsPrincipal = context.GetUser();
        // before v11.3.1
        var claimsPrincipal = context.GetGlobalValue<ClaimsPrincipal>(
                                        nameof(ClaimsPrincipal));

        // Omitted code for brevity
    })
```

</Schema>
</ExampleTabs>

With the authenticated user's `ClaimsPrincipal`, we can now access their claims.

```csharp
var userId = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
```
