---
title: Authorization
---

Authorization controls what an authenticated user can access. Hot Chocolate provides the `@authorize` directive for field-level and type-level access control, integrating with ASP.NET Core roles and policies.

Authentication is a prerequisite. You must first validate a user's identity before evaluating their permissions.

[Learn how to set up authentication](/docs/hotchocolate/v16/securing-your-api/authentication)

# Setup

After configuring authentication, complete these steps to enable authorization.

## 1. Install the Authorization Package

<PackageInstallation packageName="HotChocolate.AspNetCore.Authorization" />

## 2. Register the Required Services

Call `AddAuthorization()` on both `IServiceCollection` (for ASP.NET Core services) and `IRequestExecutorBuilder` (for the `@authorize` directive and middleware):

```csharp
// Program.cs
builder.Services.AddAuthorization();

builder.Services
    .AddGraphQLServer()
    .AddAuthorization()
    .AddQueryType<Query>();
```

## 3. Add Authorization Middleware

```csharp
// Program.cs
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapGraphQL();
});
```

# Applying Authorization

The `@authorize` directive can be applied to types and fields. When applied to a type, it applies to every field on that type. A directive on a specific field overrides the one on the type.

> **Use `HotChocolate.Authorization.AuthorizeAttribute`**, not `Microsoft.AspNetCore.Authorization.AuthorizeAttribute`. The Microsoft attribute does not integrate with the Hot Chocolate authorization pipeline. Using the wrong attribute is a common source of authorization not working.

<ExampleTabs>
<Implementation>

```csharp
// Models/User.cs
[Authorize]
public class User
{
    public string Name { get; set; }

    [Authorize(Roles = ["Administrator"])]
    public Address Address { get; set; }
}
```

With the source generator, you can apply `[Authorize]` to resolver methods:

```csharp
// Types/UserQueries.cs
[QueryType]
public static partial class UserQueries
{
    [Authorize]
    public static async Task<User?> GetMeAsync(
        ClaimsPrincipal claimsPrincipal,
        UserService users,
        CancellationToken ct)
    {
        var userId = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        return userId is not null ? await users.GetByIdAsync(userId, ct) : null;
    }
}
```

</Implementation>
<Code>

```csharp
// Types/UserType.cs
public class UserType : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor.Authorize();

        descriptor.Field(f => f.Address).Authorize(["Administrator"]);
    }
}
```

</Code>
</ExampleTabs>

If no arguments are specified on `[Authorize]`, the directive requires the user to be authenticated. Unauthenticated users who access an authorized field receive a GraphQL error with the code `AUTH_NOT_AUTHENTICATED`, and the field value is set to `null`.

```json
{
  "errors": [
    {
      "message": "The current user is not authorized to access this resource.",
      "path": ["me"],
      "extensions": {
        "code": "AUTH_NOT_AUTHENTICATED"
      }
    }
  ],
  "data": {
    "me": null
  }
}
```

# Roles

Roles provide a straightforward way to group users by access level. Add role claims to the `ClaimsPrincipal`:

```csharp
claims.Add(new Claim(ClaimTypes.Role, "Administrator"));
```

Then restrict access by role:

<ExampleTabs>
<Implementation>

```csharp
// Models/User.cs
[Authorize(Roles = ["Guest", "Administrator"])]
public class User
{
    public string Name { get; set; }

    [Authorize(Roles = ["Administrator"])]
    public Address Address { get; set; }
}
```

</Implementation>
<Code>

```csharp
// Types/UserType.cs
public class UserType : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor.Authorize(["Guest", "Administrator"]);

        descriptor.Field(f => f.Address).Authorize(["Administrator"]);
    }
}
```

</Code>
</ExampleTabs>

When multiple roles are specified, a user needs to match only one of them to gain access.

[Learn more about role-based authorization in ASP.NET Core](https://docs.microsoft.com/aspnet/core/security/authorization/roles)

# Policies

Policies decouple authorization logic from your GraphQL resolvers. A policy consists of an `IAuthorizationRequirement` and an `AuthorizationHandler<T>`.

Register policies on the service collection:

```csharp
// Program.cs
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AtLeast21", policy =>
        policy.Requirements.Add(new MinimumAgeRequirement(21)));

    options.AddPolicy("HasCountry", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => c.Type == ClaimTypes.Country)));
});

builder.Services.AddSingleton<IAuthorizationHandler, MinimumAgeHandler>();
```

Apply policies to fields:

<ExampleTabs>
<Implementation>

```csharp
// Models/User.cs
[Authorize(Policy = "AllEmployees")]
public class User
{
    public string Name { get; set; }

    [Authorize(Policy = "SalesDepartment")]
    public Address Address { get; set; }
}
```

</Implementation>
<Code>

```csharp
// Types/UserType.cs
public class UserType : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor.Authorize("AllEmployees");

        descriptor.Field(f => f.Address).Authorize("SalesDepartment");
    }
}
```

</Code>
</ExampleTabs>

The `@authorize` directive is repeatable. When multiple policies are specified, the user must satisfy all of them:

<ExampleTabs>
<Implementation>

```csharp
// Models/User.cs
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
// Types/UserType.cs
public class UserType : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor
            .Authorize("AtLeast21")
            .Authorize("HasCountry");
    }
}
```

</Code>
</ExampleTabs>

[Learn more about policy-based authorization in ASP.NET Core](https://docs.microsoft.com/aspnet/core/security/authorization/policies)

## Accessing IResolverContext in an AuthorizationHandler

When you need access to GraphQL-specific data in your authorization handler, use `IResolverContext` as the resource type:

```csharp
// Authorization/MinimumAgeHandler.cs
public class MinimumAgeHandler
    : AuthorizationHandler<MinimumAgeRequirement, IResolverContext>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        MinimumAgeRequirement requirement,
        IResolverContext resolverContext)
    {
        // Access GraphQL context data, arguments, etc.
        // Omitted for brevity
    }
}
```

# Allow Anonymous Access

Use `[AllowAnonymous]` to bypass authorization on specific fields. This is useful for registration or public content endpoints.

> **Use `HotChocolate.AspNetCore.Authorization.AllowAnonymousAttribute`**, not `Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute`.

```csharp
// Types/AccountMutations.cs
[MutationType]
public static partial class AccountMutations
{
    [Authorize]
    public static async Task<User> AddAddressAsync(/* ... */)
    {
        // Requires authentication
    }

    [AllowAnonymous]
    public static async Task<User> RegisterAsync(/* ... */)
    {
        // Open to everyone
    }
}
```

`[AllowAnonymous]` removes all other authorization requirements on the field. Use it carefully to avoid exposing sensitive data.

# Global Authorization

Apply authorization to the entire GraphQL endpoint by calling `RequireAuthorization()`:

```csharp
// Program.cs
app.UseEndpoints(endpoints =>
{
    endpoints.MapGraphQL().RequireAuthorization();
});
```

This returns HTTP 401 for unauthorized requests and blocks access to all middleware including Nitro. To keep Nitro accessible while protecting the GraphQL endpoint, split the middleware:

```csharp
app.UseEndpoints(endpoints =>
{
    endpoints.MapGraphQLHttp().RequireAuthorization();
    endpoints.MapNitroApp();
});
```

[Learn more about available middleware](/docs/hotchocolate/v16/server/endpoints)

# Troubleshooting

## Authorization not enforced

Verify you are using `HotChocolate.Authorization.AuthorizeAttribute`, not `Microsoft.AspNetCore.Authorization.AuthorizeAttribute`. The Microsoft attribute is ignored by the Hot Chocolate pipeline.

## "AUTH_NOT_AUTHENTICATED" for authenticated users

Check the middleware order: `UseAuthentication()` must come before `UseAuthorization()`, and both must come before `MapGraphQL()`. Also verify that `AddAuthorization()` is called on both `IServiceCollection` and `IRequestExecutorBuilder`.

## Policy always fails

Verify that the `AuthorizationHandler` is registered in the DI container and that the handler calls `context.Succeed(requirement)` when the requirement is met. A handler that does not call `Succeed` results in an implicit failure.

## Nitro is blocked by global authorization

When `RequireAuthorization()` is applied to `MapGraphQL()`, it blocks all sub-middleware including Nitro. Split the middleware into `MapGraphQLHttp()` and `MapNitroApp()` and apply authorization only to `MapGraphQLHttp()`.

# Next Steps

- **Need to set up authentication first?** See [Authentication](/docs/hotchocolate/v16/securing-your-api/authentication).
- **Need to protect against expensive queries?** See [Cost Analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis).
- **Need an overview of security options?** See [Security Overview](/docs/hotchocolate/v16/security).
