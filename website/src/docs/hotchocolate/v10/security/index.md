---
title: Authorization
---

# Authentication

GraphQL as defined by the spec does not specify how a user has to authenticate against a schema in order to execute queries. GraphQL does not even specify how requests are sent to the server using HTTP or any other protocol. _Facebook_ specified GraphQL as transport agnostic, meaning GraphQL focuses on one specific problem domain and does not try to solve other problems like how the transport might work, how authentication might work or how a schema implements authorization. These subjects are considered out of scope.

If we are accessing GraphQL servers through HTTP then authenticating against a GraphQL server can be done in various ways and Hot Chocolate does not prescribe any particular.

We basically can do it in any way ASP.NET core allows us to.

[Overview of ASP.NET Core authentication](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/?view=aspnetcore-3.1)

# Authorization

Authorization on the other hand is something Hot Chocolate can provide some value to by introducing a way to authorize access to fields with the `@authorize`-directive.

But let's start at the beginning with this. In order to add authorization capabilities to our schema add the following package to our project:

```bash
dotnet add package HotChocolate.AspNetCore.Authorization
```

In order to use the `@authorize`-directive we have to register it like the following with our schema:

```csharp
SchemaBuilder.New()
  ...
  .AddAuthorizeDirectiveType()
  ...
  .Create();
```

Once we have done that we can add the `@authorize`-directive to object types or their fields.

The `@authorize`-directive on a field takes precedence over one that is added on the object type definition.

SDL-First:

```sdl
type Person @authorize {
  name: String!
  address: Address!
}
```

Pure Code-First:

```csharp
[Authorize]
public class Person
{
    public string Name { get; }
    public Address Address { get; }
}
```

Code-First:

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

If we just add the `@authorize`-directive without specifying any arguments the authorize middleware will basically just enforces that a user is authenticated.

If no user is authenticated the field middleware will raise a GraphQL error and the field value is set to null.

> If the field is a non-null field the standard GraphQL non-null violation propagation rule is applied like with any other GraphQL error and the fields along the path are removed until the execution engine reaches a nullable field or the while result was removed.

## Roles

In many cases role based authorization is sufficient and was already available with ASP.NET classic on the .NET Framework.

Moreover, role based authorization is setup quickly and does not need any other setup then providing the roles.

SDL-First:

```sdl
type Person @authorize(roles: "foo") {
  name: String!
  address: Address! @authorize(roles: ["foo", "bar"])
}
```

Pure Code-First:

```csharp
[Authorize]
public class Person
{
    public string Name { get; }

    [Authorize(Roles = new[] { "foo", "bar" })]
    public Address Address { get; }
}
```

Code-First:

```csharp
public class PersonType : ObjectType<Person>
{
    protected override Configure(IObjectTypeDescriptor<Person> descriptor)
    {
        descriptor.Authorize(new [] {"foo"});
        descriptor.Field(t => t.Address).Authorize(new [] {"foo", "bar"});
    }
}
```

## Policies

If we are using ASP.NET core then we can also opt-in using authorization policies.

So taking our example from earlier we are instead of providing a role just provide a policy name:

SDL-First:

```sdl
type Person @authorize(policy: "AllEmployees") {
  name: String!
  address: Address! @authorize(policy: "SalesDepartment")
}
```

Pure Code-First:

```csharp
[Authorize(Policy = "AllEmployees")]
public class Person
{
    public string Name { get; }

    [Authorize(Policy = "SalesDepartment")]
    public Address Address { get; }
}
```

Code-First:

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

In the above example the name field is accessible to all users that fall under the `AllEmployees` policy, whereas the directive on the address field takes precedence over the `@authorize`-directive on the object type. This means that only users that fall under the `SalesDepartment` policy can access the address field.

> It is important to note that _policy-based authorization_ is only available with ASP.NET core.

The `@authorize`-directive is repeatable, that means that we are able to chain the directives and only if all annotated conditions are true will we gain access to the data of the annotated field.

SDL-First:

```sdl
type Person {
  name: String!
  address: Address!
  @authorize(policy: "AllEmployees")
  @authorize(policy: "SalesDepartment")
  @authorize(roles: "FooBar")
}
```

Pure Code-First:

```csharp
public class Person
{
    public string Name { get; }

    [Authorize(Policy = "AllEmployees")]
    [Authorize(Policy = "SalesDepartment")]
    [Authorize(Policy = "FooBar")]
    public Address Address { get; }
}
```

Code-First:

```csharp
public class PersonType : ObjectType<Person>
{
    protected override Configure(IObjectTypeDescriptor<Person> descriptor)
    {
        descriptor.Field(t => t.Address)
          .Authorize("AllEmployees")
          .Authorize("SalesDepartment")
          .Authorize("FooBar");
    }
}
```

# Policy-based authorization in ASP.NET Core

Policy-based authorization in ASP.NET Core does not any longer prescribe us in which way we describe our requirements. Now, with policy-based authorization we could just say that a certain field can only be accessed if the user is 21 or older or that a user did provide his passport as evidence of his/her identity.

So, in order to define those requirements we can define policies that essentially describe and validate our requirements and the rules that enforce them.

```csharp
services.AddAuthorization(options =>
{
    options.AddPolicy("HasCountry", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => (c.Type == ClaimTypes.Country))));
});
```

The good thing with policies is that we decouple the actual authorization rules from our GraphQL resolver logic which makes the whole thing better testable.

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

The `@authorize`-directive essentially uses the provided policy and runs it against the `ClaimsPrinciple` that is associated with the current request.

More about policy-based authorization can be found in the Microsoft Documentation:
[Policy-based authorization in ASP.NET Core | Microsoft Docs](https://docs.microsoft.com/en-us/aspnet/core/security/authorization/policies?view=aspnetcore-2.1)

# Query Requests

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
