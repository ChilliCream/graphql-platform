---
title: Authorization
---

Authorization allows us to determine a user's permissions within our system. We can for example limit access to resources or only allow certain users to execute specific mutations.

Authentication is a prerequisite of Authorization, as we first need to validate a user's "authenticity" before we can evaluate his authorization claims.

[Learn how to setup authentication](/docs/hotchocolate/v12/security/authentication)

# Setup

After we have successfully setup authentication, there are only a few things left to do.

1. Install the `HotChocolate.AspNetCore.Authorization` package

<PackageInstallation packageName="HotChocolate.AspNetCore.Authorization" />

2. Register the necessary ASP.NET Core services

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

> Warning: We need to call `AddAuthorization()` on the `IServiceCollection`, to register the services needed by ASP.NET Core, and on the `IRequestExecutorBuilder` to register the `@authorize` directive and middleware.

3. Register the ASP.NET Core authorization middleware with the request pipeline by calling `UseAuthorization`

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
<Implementation>

In the implementation-first approach we can use the `[Authorize]` attribute to add the `@authorize` directive.

```csharp
[Authorize]
public class User
{
    public string Name { get; set; }

    [Authorize]
    public Address Address { get; set; }
}
```

> Warning: We need to use the `HotChocolate.AspNetCore.Authorization.AuthorizeAttribute` instead of the `Microsoft.AspNetCore.AuthorizationAttribute`.

</Implementation>
<Code>

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

</Code>
<Schema>

```sdl
type User @authorize {
  name: String!
  address: Address! @authorize
}
```

</Schema>
</ExampleTabs>

Specified on a type the `@authorize` directive will be applied to each field of that type. Its authorization logic is executed once for each individual field, depending on whether it was selected by the requestor or not. If the directive is placed on an individual field, it overrules the one on the type.

If we do not specify any arguments to the `@authorize` directive, it will only enforce that the requestor is authenticated, nothing more. If he is not and tries to access an authorized field, a GraphQL error will be raised and the field result set to `null`.

> Warning: Using the @authorize directive, all unauthorized requests by default will return status code 200 and a payload like this:

```json
{
  "errors": [
    {
      "message": "The current user is not authorized to access this resource.",
      "locations": [
        {
          "line": 2,
          "column": 3
        }
      ],
      "path": ["welcome"],
      "extensions": {
        "code": "AUTH_NOT_AUTHENTICATED"
      }
    }
  ],
  "data": {
    "welcome": null
  }
}
```

## Roles

Roles provide a very intuitive way of dividing our users into groups with different access rights.

When building our `ClaimsPrincipal`, we just have to add one or more role claims.

```csharp
claims.Add(new Claim(ClaimTypes.Role, "Administrator"));
```

We can then check whether an authenticated user has these role claims.

<ExampleTabs>
<Implementation>

```csharp
[Authorize(Roles = new [] { "Guest", "Administrator" })]
public class User
{
    public string Name { get; set; }

    [Authorize(Roles = new[] { "Administrator" })]
    public Address Address { get; set; }
}
```

</Implementation>
<Code>

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

</Code>
<Schema>

```sdl
type User @authorize(roles: [ "Guest", "Administrator" ]) {
  name: String!
  address: Address! @authorize(roles: "Administrator")
}
```

</Schema>
</ExampleTabs>

> Warning: If multiple roles are specified, a user only has to match one of the specified roles, in order to be able to execute the resolver.

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
<Implementation>

```csharp
[Authorize(Policy = "AllEmployees")]
public class User
{
    public string Name { get; }

    [Authorize(Policy = "SalesDepartment")]
    public Address Address { get; }
}
```

</Implementation>
<Code>

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

</Code>
<Schema>

```sdl
type User @authorize(policy: "AllEmployees") {
  name: String!
  address: Address! @authorize(policy: "SalesDepartment")
}
```

</Schema>
</ExampleTabs>

This essentially uses the provided policy and runs it against the `ClaimsPrincipal` that is associated with the current request.

The `@authorize` directive is also repeatable, which means that we are able to chain the directive and a user is only allowed to access the field if they meet all of the specified conditions.

<ExampleTabs>
<Implementation>

```csharp
[Authorize(Policy = "AtLeast21")]
[Authorize(Policy = "HasCountry")]
public class User
{
    public string Name { get; set; }
}
```

</Implementation>
<Code>

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

</Code>
<Schema>

```sdl
type User
 @authorize(policy: "AtLeast21")
 @authorize(policy: "HasCountry") {
 name: String!
}
```

</Schema>
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

This method also accepts [roles](#roles) and [policies](#policies) as arguments, similar to the `Authorize` attribute / methods.

> Warning: Unlike the `@authorize directive` this will return status code 401 and prevent unauthorized access to all middleware included in `MapGraphQL`. This includes our GraphQL IDE Banana Cake Pop. If we do not want to block unauthorized access to Banana Cake Pop, we can split up the `MapGraphQL` middleware and for example only apply the `RequireAuthorization` to the `MapGraphQLHttp` middleware.

[Learn more about available middleware](/docs/hotchocolate/v12/server/endpoints)

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

[Learn more about interceptors](/docs/hotchocolate/v12/server/interceptors)
