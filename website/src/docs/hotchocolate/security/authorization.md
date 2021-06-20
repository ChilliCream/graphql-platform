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

> ⚠️ Note: We need to call `AddAuthorization()` on the `IRequestExecutorBuilder` and the `IServiceCollection`.

2. Register the `UseAuthorization` middleware with the request pipeline

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

If the `@authorize` directive is specified on a type, it is applied to all fields of that type. Specified on an individual field the directive precedences the one on the type.

<ExampleTabs>
<ExampleTabs.Annotation>

In the Annotation-based approach we can use the `[Authorize]` attribute to add the `@authorize` directive.

```csharp
[Authorize]
public class Person
{
    public string Name { get; }

    [Authorize]
    public Address Address { get; }
}
```

> ⚠️ Note: We need to use the `HotChocolate.AspNetCore.AuthorizationAttribute` instead of the `Microsoft.AspNetCore.AuthorizationAttribute`.

</ExampleTabs.Annotation>
<ExampleTabs.Code>

```csharp
public class PersonType : ObjectType<Person>
{
    protected override Configure(IObjectTypeDescriptor<Person> descriptor)
    {
        descriptor.Authorize();

        descriptor.Field(t => t.Address).Authorize();
    }
}
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

```sdl
type Person @authorize {
  name: String!
  address: Address! @authorize
}
```

</ExampleTabs.Schema>
</ExampleTabs>

If we do not specify any arguments to the `@authorize` directive, it will only enforce that the requestor is authenticated, nothing more.

A GraphQL error will be raised and the field result set to `null`, if the requestor is unauthenticated and tries to access an authorized field.

## Roles

TODO: Test

Roles provide a very intuitive way of dividing our users into groups.

When building our `ClaimsPrincipal`, we just have to add one or more role claims.

```csharp
claims.Add(new Claim(ClaimTypes.Role, "Administrator"));
```

We can then really easily check, whether an authenticated user has these role claims.

<ExampleTabs>
<ExampleTabs.Annotation>

```csharp
[Authorize(Roles = new [] { "Administrator" })]
public class Person
{
    public string Name { get; }

    [Authorize(Roles = new[] { "foo", "bar" })]
    public Address Address { get; }
}
```

</ExampleTabs.Annotation>
<ExampleTabs.Code>

```csharp
public class PersonType : ObjectType<Person>
{
    protected override Configure(IObjectTypeDescriptor<Person> descriptor)
    {
        descriptor.Authorize(new[] { "Administrator" });

        descriptor.Field(t => t.Address).Authorize(new[] { "foo", "bar" });
    }
}
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

TODO: Check if this is correct for a singular role

```sdl
type Person @authorize(roles: "Administrator") {
  name: String!
  address: Address! @authorize(roles: ["foo", "bar"])
}
```

</ExampleTabs.Schema>
</ExampleTabs>

If multiple roles are specified, all of them have to be included in the `ClaimsPrincipal` in order to execute a resolver.

[Learn more about role-based authorization in ASP.NET Core](https://docs.microsoft.com/aspnet/core/security/authorization/roles)

## Policies

TODO: Test

Policies allow us to create richer validation logic and decouple the authorization rules from our GraphQL resolvers.

<ExampleTabs>
<ExampleTabs.Annotation>

```csharp
[Authorize(Policy = "AllEmployees")]
public class Person
{
    public string Name { get; }

    [Authorize(Policy = "SalesDepartment")]
    public Address Address { get; }
}
```

</ExampleTabs.Annotation>
<ExampleTabs.Code>

```csharp
public class PersonType : ObjectType<Person>
{
    protected override Configure(IObjectTypeDescriptor<Person> descriptor)
    {
        descriptor.Authorize("AllEmployees");

        descriptor.Field(t => t.Address).Authorize("SalesDepartment");
    }
}
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

```sdl
type Person @authorize(policy: "AllEmployees") {
  name: String!
  address: Address! @authorize(policy: "SalesDepartment")
}
```

</ExampleTabs.Schema>
</ExampleTabs>

<!-- Policy-based authorization in ASP.NET Core does not any longer prescribe us in which way we describe our requirements. Now, with policy-based authorization we could just say that a certain field can only be accessed if the user is 21 or older or that a user did provide his passport as evidence of his/her identity.

So, in order to define those requirements we can define policies that essentially describe and validate our requirements and the rules that enforce them.

```csharp
services.AddAuthorization(options =>
{
    options.AddPolicy("HasCountry", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => (c.Type == ClaimTypes.Country))));
});
```

One important aspect with policies is also that we are passing the resolver context as resource into the policy so that we have access to all the data of our resolver.

```csharp
public class SalesDepartmentAuthorizationHandler
    :  AuthorizationHandler<SalesDepartmentRequirement, IResolverContext>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SalesDepartmentRequirement requirement,
        IResolverContext resource)
    {
        if (context.User.HasClaim(...))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

public class SalesDepartmentRequirement : IAuthorizationRequirement { }

services.AddAuthorization(options =>
{
    options.AddPolicy("SalesDepartment",
        policy => policy.Requirements.Add(new SalesDepartmentRequirement()));
});

services.AddSingleton<IAuthorizationHandler, SalesDepartmentAuthorizationHandler>();
```

The `@authorize`-directive essentially uses the provided policy and runs it against the `ClaimsPrinciple` that is associated with the current request. -->

TODO: Examples for the below

The `@authorize`-directive is repeatable, that means that we are able to chain the directives and only if all annotated conditions are true will we gain access to the data of the annotated field.

[Learn more about policy-based authorization in ASP.NET Core](https://docs.microsoft.com/aspnet/core/security/authorization/policies)

## Global authorization

We can also apply authorization to our entire GraphQL endpoint, by calling `RequireAuthorization()` on the `GraphQLEndpointConventionBuilder`.

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

## Adding claims dynamically

TODO: Rework and test

Our query middleware creates a request and passes the request with additional meta-data to the query-engine. For example we provide a property called `ClaimsIdentity` that contains the user associated with the current request. These meta-data or custom request properties can be used within a field-middleware like the authorize middleware to change the default execution of a field resolver.

So, we could use an authentication-middleware in ASP.NET core to add all the user meta-data that we need to our claim-identity or we could hook up some code in our middleware and add additional meta-data or even modify the `ClaimsPrincipal`.

```csharp
services.AddQueryRequestInterceptor((ctx, builder, ct) =>
{
    var identity = new ClaimsIdentity();
    identity.AddClaim(new Claim(ClaimTypes.Country, "us"));
    ctx.User.AddIdentity(identity);
    return Task.CompletedTask;
});
```

The `OnCreateRequestAsync`-delegate can be used for many other things and is not really for authorization but can be useful in dev-scenarios where we want to simulate a certain user etc..

# Decoupling authorization

TODO
