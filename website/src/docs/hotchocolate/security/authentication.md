---
title: Authentication
---

import { ExampleTabs } from "../../../components/mdx/example-tabs"

<!-- todo: authentication itself doesn't add security -->

Authentication allows us to determine a user's identity.

When it comes to authentication, Hot Chocolate does not reinvent the wheel: Authentication works like it would in any other ASP.NET Core application.

# Setup

For authentication to work, we first need to specify an authentication mechanism.

We are using [JWTs](https://jwt.io/introduction) in the following example, but we can use any authentication mechanism supported by ASP.NET Core.

TODO: Refine this example

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("MySuperSecretKey"))
                };
            });
    }
}
```

[Learn more about authentication in ASP.NET Core](https://docs.microsoft.com/aspnet/core/security/authentication)

Next we need to call `AddAuthorization()` on our `IRequestExecutorBuilder`. This will register the necessary middleware to make the authenticated user available to our resolvers.

```csharp
services
    .AddGraphQLServer()
    .AddAuthorization()
    .AddQueryType<Query>();
```

The above is the only step that differs from how we would setup authentication in a regular ASP.NET Core application.

Lastly we need to register the `UseAuthentication` middleware with the request pipeline.

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

All of this does not yet lock out users that are not authenticated. For this we need authorization.

[Learn more about authorization](/docs/hotchocolate/security/authorization)

# Accessing the ClaimsPrincipal

If a user was able to successfully authenticate to our application, we can access details about this user through his [`ClaimsPrincipal`](https://docs.microsoft.com/dotnet/api/system.security.claims.claimsprincipal).

The `ClaimsPrincipal` can be accessed in our resolvers through the global state.

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

When using the `Resolve` method, we can access th

```csharp
public class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("me")
            .Resolve(context =>
            {
                var claimsPrincipal =
                    context.GetGlobalValue<ClaimsPrincipal>(nameof(ClaimsPrincipal));

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
