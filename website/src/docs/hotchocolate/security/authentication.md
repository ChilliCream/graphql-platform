---
title: Authentication
---

import { ExampleTabs } from "../../../components/mdx/example-tabs"

Authentication allows us to determine a user's identity. This is of course a prerequisite for authorization, but it also allows us to for example expose a `me` field in our query that uses the `Id` of the authenticated user to fetch his details.

Hot Chocolate fully embraces the authentication capabilities of ASP.NET Core, making it easy to reuse existing authentication configuration and integrating a variety of authentication providers.

[Learn more about authentication in ASP.NET Core](https://docs.microsoft.com/aspnet/core/security/authentication)

# Setup

Setting up authentication is largly the same as in any other ASP.NET Core application.

## General

In the following example we are using [JWTs](https://jwt.io/introduction), but we could use any other authentication scheme supported by ASP.NET Core.

1. Install the `Microsoft.AspNetCore.Authentication.JwtBearer` package

```bash
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
```

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

> Note: This is only an exemplary configuration, not intended for production.

3. Register the `UseAuthentication` middleware with the request pipeline

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

TODO: Link to JWT authentication tutorial

## Hot Chocolate

The above takes care of parsing and validating an incoming HTTP request. To make the authentication result available to our resolvers we need to complete some additional, Hot Chocolate specific steps.

1. Install the `HotChocolate.AspNetCore.Authorization` package

```bash
dotnet add package HotChocolate.AspNetCore.Authorization
```

2. Call `AddAuthorization()` on the `IRequestExecutorBuilder`

```csharp
services
    .AddGraphQLServer()
    .AddAuthorization()
    .AddQueryType<Query>();
```

All of this does not yet lock out unauthenticated users. It only exposes the identity of the authenticated user to our application through a `ClaimsPrincipal`. If we want to prevent certain users from querying our graph, we need to utilize authorization.

[Learn more about authorization](/docs/hotchocolate/security/authorization)

# Accessing the ClaimsPrincipal

The [ClaimsPrincipal](https://docs.microsoft.com/dotnet/api/system.security.claims.claimsprincipal) of an authenticated user can be accessed in our resolvers through the global state.

<ExampleTabs>
<ExampleTabs.Annotation>

```csharp
public class Query
{
    public User GetMe(
        [GlobalState(nameof(ClaimsPrincipal))] ClaimsPrincipal claimsPrincipal)
    {
        // Omitted code for brevity
    }
}
```

</ExampleTabs.Annotation>
<ExampleTabs.Code>

```csharp
public class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("me")
            .Resolve(context =>
            {
                var claimsPrincipal = context.GetGlobalValue<ClaimsPrincipal>(
                                        nameof(ClaimsPrincipal));

                // Omitted code for brevity
            });
    }
}
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

TODO

</ExampleTabs.Schema>
</ExampleTabs>

After injecting the `ClaimsPrincipal` we can access specific claims of the authenticated user.

```csharp
var userId = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
```
