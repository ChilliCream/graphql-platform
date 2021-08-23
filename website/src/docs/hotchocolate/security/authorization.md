---
title: Authorization
---

import { ExampleTabs } from "../../../components/mdx/example-tabs"

Authorization allows us to determine a user's permissions within our system. We can for example limit access to resources or only allow certain users to execute specific mutations.

Authentication is a prerequisite of Authorization, as we first need to validate a user's "authenticity" before we can evaluate his authorization claims.

[Learn how to setup authentication](/docs/hotchocolate/security/authentication)

# Setup

After we have successfully setup authentication, there are only a few things left to do.

1. Register the necessary ASP.NET Core services

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddAuthorization();

        // Omitted code for brevity

        services
            .AddGraphQLServer()
            .AddAuthorization()
            .AddQueryType<Query>();
    }
}
```

> ⚠️ Note: We need to call `AddAuthorization()` on the `IServiceCollection`, to register the services needed by ASP.NET Core, and on the `IRequestExecutorBuilder` to register the `@authorize` directive and middleware.

2. Register the ASP.NET Core authorization middleware with the request pipeline by calling `UseAuthorization`

```csharp
public class Startup
{
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGraphQL();
        });
    }
}
```

# Usage

At the core of authorization with Hot Chocolate is the `@authorize` directive. It can be applied to fields and types to denote that they require authorization.

<ExampleTabs>
<ExampleTabs.Annotation>

In the Annotation-based approach we can use the `[Authorize]` attribute to add the `@authorize` directive.

```csharp
[Authorize]
public class User
{
    public string Name { get; set; }

    [Authorize]
    public Address Address { get; set; }
}
```

> ⚠️ Note: We need to use the `HotChocolate.AspNetCore.AuthorizationAttribute` instead of the `Microsoft.AspNetCore.AuthorizationAttribute`.

</ExampleTabs.Annotation>
<ExampleTabs.Code>

```csharp
public class UserType : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor.Authorize();

        descriptor.Field(f => f.Address).Authorize();
    }
}
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

```sdl
type User @authorize {
  name: String!
  address: Address! @authorize
}
```

</ExampleTabs.Schema>
</ExampleTabs>

Specified on a type the `@authorize` directive will be applied to each field of that type. Its authorization logic is executed once for each individual field, depending on whether it was selected by the requestor or not. If the directive is placed on an individual field, it overrules the one on the type.

If we do not specify any arguments to the `@authorize` directive, it will only enforce that the requestor is authenticated, nothing more. If he is not and tries to access an authorized field, a GraphQL error will be raised and the field result set to `null`.

## Roles

Roles provide a very intuitive way of dividing our users into groups with different access rights.

When building our `ClaimsPrincipal`, we just have to add one or more role claims.

```csharp
claims.Add(new Claim(ClaimTypes.Role, "Administrator"));
```

We can then check whether an authenticated user has these role claims.

<ExampleTabs>
<ExampleTabs.Annotation>

```csharp
[Authorize(Roles = new [] { "Guest", "Administrator" })]
public class User
{
    public string Name { get; set; }

    [Authorize(Roles = new[] { "Administrator" })]
    public Address Address { get; set; }
}
```

</ExampleTabs.Annotation>
<ExampleTabs.Code>

```csharp
public class UserType : ObjectType<User>
{
    protected override Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor.Authorize(new[] { "Guest", "Administrator" });

        descriptor.Field(t => t.Address).Authorize(new[] { "Administrator" });
    }
}
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

```sdl
type User @authorize(roles: [ "Guest", "Administrator" ]) {
  name: String!
  address: Address! @authorize(roles: "Administrator")
}
```

</ExampleTabs.Schema>
</ExampleTabs>

> ⚠️ Note: If multiple roles are specified, a user only has to match one of the specified roles, in order to be able to execute the resolver.

[Learn more about role-based authorization in ASP.NET Core](https://docs.microsoft.com/aspnet/core/security/authorization/roles)

## Policies

Policies allow us to create richer validation logic and decouple the authorization rules from our GraphQL resolvers.

A policy consists of an [IAuthorizationRequirement](https://docs.microsoft.com/aspnet/core/security/authorization/policies#requirements) and an [AuthorizationHandler&#x3C;T&#x3E;](https://docs.microsoft.com/aspnet/core/security/authorization/policies#authorization-handlers).

Once defined, we can register our policies like the following.

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("AtLeast21", policy =>
                policy.Requirements.Add(new MinimumAgeRequirement(21)));

            options.AddPolicy("HasCountry", policy =>
                policy.RequireAssertion(context =>
                    context.User.HasClaim(c => c.Type == ClaimTypes.Country)));
        });

        services.AddSingleton<IAuthorizationHandler, MinimumAgeHandler>();

        // Omitted code for brevity

        services
            .AddGraphQLServer()
            .AddAuthorization()
            .AddQueryType<Query>();
    }
}
```

We can then use these policies to restrict access to our fields.

<ExampleTabs>
<ExampleTabs.Annotation>

```csharp
[Authorize(Policy = "AllEmployees")]
public class User
{
    public string Name { get; }

    [Authorize(Policy = "SalesDepartment")]
    public Address Address { get; }
}
```

</ExampleTabs.Annotation>
<ExampleTabs.Code>

```csharp
public class UserType : ObjectType<User>
{
    protected override Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor.Authorize("AllEmployees");

        descriptor.Field(t => t.Address).Authorize("SalesDepartment");
    }
}
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

```sdl
type User @authorize(policy: "AllEmployees") {
  name: String!
  address: Address! @authorize(policy: "SalesDepartment")
}
```

</ExampleTabs.Schema>
</ExampleTabs>

This essentially uses the provided policy and runs it against the `ClaimsPrincipal` that is associated with the current request.

The `@authorize` directive is also repeatable, which means that we are able to chain the directive and a user is only allowed to access the field if they meet all of the specified conditions.

<ExampleTabs>
<ExampleTabs.Annotation>

```csharp
[Authorize(Policy = "AtLeast21")]
[Authorize(Policy = "HasCountry")]
public class User
{
    public string Name { get; set; }
}
```

</ExampleTabs.Annotation>
<ExampleTabs.Code>

```csharp
public class UserType : ObjectType<User>
{
    protected override Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor
            .Authorize("AtLeast21")
            .Authorize("HasCountry");
    }
}
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

```sdl
type User
	@authorize(policy: "AtLeast21")
	@authorize(policy: "HasCountry") {
	name: String!
}
```

</ExampleTabs.Schema>
</ExampleTabs>

[Learn more about policy-based authorization in ASP.NET Core](https://docs.microsoft.com/aspnet/core/security/authorization/policies)

### IResolverContext within an AuthorizationHandler

If we need to, we can also access the `IResolverContext` in our `AuthorizationHandler`.

```csharp
public class MinimumAgeHandler
    : AuthorizationHandler<MinimumAgeRequirement, IResolverContext>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        MinimumAgeRequirement requirement,
        IResolverContext resolverContext)
    {
        // Omitted code for brevity
    }
}
```

# Global authorization

We can also apply authorization to our entire GraphQL endpoint. To do this, simply call `RequireAuthorization()` on the `GraphQLEndpointConventionBuilder`.

```csharp
public class Startup
{
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGraphQL().RequireAuthorization();
        });
    }
}
```

> ⚠️ Note: This will also block unauthenticated access to GraphQL IDEs hosted on that endpoint, like Banana Cake Pop.

This method also accepts [roles](#roles) and [policies](#policies) as arguments, similiar to the `Authorize` attribute / methods.

# Modifying the ClaimsPrincipal

Sometimes we might want to add additional [ClaimsIdentity](https://docs.microsoft.com/dotnet/api/system.security.claims.claimsidentity) to our `ClaimsPrincipal` or modify the default identity.

Hot Chocolate provides the ability to register an `IHttpRequestInterceptor`, allowing us to modify the incoming HTTP request, before it is passed along to the execution engine.

```csharp
public class HttpRequestInterceptor : DefaultHttpRequestInterceptor
{
    public override ValueTask OnCreateAsync(HttpContext context,
        IRequestExecutor requestExecutor, IQueryRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        var identity = new ClaimsIdentity();
        identity.AddClaim(new Claim(ClaimTypes.Country, "us"));

        context.User.AddIdentity(identity);

        return base.OnCreateAsync(context, requestExecutor, requestBuilder,
            cancellationToken);
    }
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddHttpRequestInterceptor<HttpRequestInterceptor>();

        // Omitted code for brevity
    }
}
```

The `IHttpRequestInterceptor` can be used for many other things as well, not just for modifying the `ClaimsPrincipal`.
