---
title: Authorize attribute
---

The `[Authorize]` attribute secures GraphQL object types and fields using the Hot Chocolate authorization pipeline. This attribute is the primary code-first method for schema authorization in Hot Chocolate.

When you use `[Authorize]`, it checks the current `ClaimsPrincipal` against the ASP.NET Core default policy, a named policy, or specified roles. For authorization to succeed, authentication must first create the `ClaimsPrincipal`.

> **Use `HotChocolate.Authorization.AuthorizeAttribute`.** Do not use `Microsoft.AspNetCore.Authorization.AuthorizeAttribute` on schema members. The Microsoft attribute is intended for ASP.NET Core endpoints and MVC actions, not for Hot Chocolate schema authorization.

# Enable authorization before using the attribute

First, install the ASP.NET Core authorization integration package:

<PackageInstallation packageName="HotChocolate.AspNetCore.Authorization" />

Next, configure authentication, ASP.NET Core authorization, and Hot Chocolate authorization in your `Program.cs`:

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.CanReadBilling, policy =>
        policy.RequireClaim("scope", "billing:read"));
});

builder
    .AddGraphQL()
    .AddAuthorization()
    .AddQueryType<Query>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGraphQL();

app.Run();

public static class Policies
{
    public const string CanReadBilling = "CanReadBilling";
}
```

The call to `builder.Services.AddAuthorization(...)` registers ASP.NET Core policies and role handling. Adding `.AddAuthorization()` to the GraphQL builder enables Hot Chocolate's internal `@authorize` directive and execution middleware.

Note that `[Authorize]` does not perform authentication and does not secure the entire HTTP or WebSocket endpoint. It only protects the schema members where it is applied.

# Require an authenticated user for a field

Apply `[Authorize]` to a resolver method when you want the entire field to require an authenticated user.

```csharp
#nullable enable

using System.Security.Claims;
using HotChocolate.Authorization;
using HotChocolate.Types;
using Microsoft.IdentityModel.JsonWebTokens;

[QueryType]
public static partial class Query
{
    [Authorize]
    public static async Task<User?> GetViewerAsync(
        ClaimsPrincipal user,
        UserByIdDataLoader userById,
        CancellationToken cancellationToken)
    {
        var userId = user.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return userId is null
            ? null
            : await userById.LoadAsync(userId, cancellationToken);
    }
}

public sealed class User
{
    public string Id { get; init; } = default!;

    public string DisplayName { get; init; } = default!;
}
```

If you do not specify a `Policy` or `Roles`, `[Authorize]` uses the ASP.NET Core default authorization policy. Typically, this means the user must be authenticated.

Example client query:

```graphql
query GetViewer {
  viewer {
    id
    displayName
  }
}
```

If there is no authenticated user, Hot Chocolate returns a GraphQL error and the protected nullable field is set to `null`:

```json
{
  "errors": [
    {
      "message": "The current user is not authorized to access this resource.",
      "path": ["viewer"],
      "extensions": {
        "code": "AUTH_NOT_AUTHENTICATED"
      }
    }
  ],
  "data": {
    "viewer": null
  }
}
```

If the protected field is non-nullable, standard GraphQL null bubbling applies.

# Protect a property or object field

Use `[Authorize]` on a property when most of the object is public, but you want to restrict access to a specific field.

```csharp
using HotChocolate.Authorization;

public sealed class User
{
    public string Id { get; init; } = default!;

    public string DisplayName { get; init; } = default!;

    [Authorize]
    public string? Email { get; init; }
}
```

The public fields remain accessible, while the `email` field requires authentication:

```graphql
type User {
  id: String!
  displayName: String!
  email: String
}
```

You can also authorize a resolver on a type class or extension:

```csharp
using HotChocolate.Authorization;
using HotChocolate.Types;

[ObjectType<User>]
public static partial class UserNode
{
    [Authorize(Policy = Policies.CanReadBilling)]
    public static async Task<BillingAccount?> GetBillingAccountAsync(
        [Parent] User user,
        BillingService billing,
        CancellationToken cancellationToken)
    {
        return await billing.GetAccountAsync(user.Id, cancellationToken);
    }
}
```

# Protect all fields returned as a type

Apply `[Authorize]` to an object type when you want the same rule to protect every entry point that returns that type.

```csharp
using HotChocolate.Authorization;

[Authorize]
public sealed class Account
{
    public string Id { get; init; } = default!;

    public string Name { get; init; } = default!;

    public decimal Balance { get; init; }
}
```

Object type authorization is evaluated when a field returns the authorized type. It is not re-evaluated for each selected child field. Use field-level authorization if only certain fields on a type should be private.

You can also apply `[Authorize]` to a source-generator type class:

```csharp
using HotChocolate.Authorization;
using HotChocolate.Types;

[ObjectType<Account>]
[Authorize]
public static partial class AccountNode
{
    public static string GetDisplayName([Parent] Account account)
    {
        return account.Name;
    }
}
```

When you authorize a root operation type, Hot Chocolate applies that authorization to all non-introspection root fields.

# Restrict access by role

Use the `Roles` property when role claims are sufficient to determine access.

```csharp
using HotChocolate.Authorization;
using HotChocolate.Types;

[QueryType]
public static partial class AuditQueries
{
    [Authorize(Roles = ["Administrator"])]
    public static IReadOnlyList<AuditEntry> GetAuditLog(AuditStore store)
    {
        return store.GetEntries();
    }
}
```

You can specify multiple roles as alternatives. A user with either the `Support` or `Administrator` role can access this field:

```csharp
[Authorize(Roles = ["Support", "Administrator"])]
public static IReadOnlyList<AuditEntry> GetSupportAuditLog(AuditStore store)
{
    return store.GetEntries();
}
```

Roles are taken from the authenticated `ClaimsPrincipal` and evaluated by ASP.NET Core authorization.

# Restrict access by policy

Policies help keep authorization logic out of resolvers. Register the policy with ASP.NET Core authorization, then reference it by name in `[Authorize]`.

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.CanReadBilling, policy =>
        policy.RequireClaim("scope", "billing:read"));
});

public static class Policies
{
    public const string CanReadBilling = "CanReadBilling";
}
```

```csharp
using HotChocolate.Authorization;
using HotChocolate.Types;

[QueryType]
public static partial class BillingQueries
{
    [Authorize(Policy = Policies.CanReadBilling)]
    public static async Task<BillingAccount?> GetBillingAccountAsync(
        BillingService billing,
        CancellationToken cancellationToken)
    {
        return await billing.GetCurrentAccountAsync(cancellationToken);
    }
}
```

Use constants for policy names to avoid errors such as `AUTH_POLICY_NOT_FOUND` due to typos.

For custom requirements and handlers, see [Authorization](/docs/hotchocolate/v16/build/security/authorization). Hot Chocolate passes the resolver context as the authorization resource for field authorization.

# Combine rules and use repeated attributes

A single `[Authorize]` attribute can require both a policy and at least one of several roles:

```csharp
[Authorize(Policy = "HasSupportContract", Roles = ["Support", "Administrator"])]
public static CustomerAccount GetCustomerAccount(CustomerService customers, string id)
{
    return customers.GetAccount(id);
}
```

In this case, the policy must pass and the user must have at least one of the listed roles.

You can repeat `[Authorize]` attributes. When repeated, all listed policies must pass:

```csharp
[Authorize(Policy = "AtLeast21")]
[Authorize(Policy = "HasCountry")]
public static MemberBenefits GetMemberBenefits(MemberService members)
{
    return members.GetBenefits();
}
```

# Allow a public field in an authorized area

Use `HotChocolate.Authorization.AllowAnonymousAttribute` to opt out a field from authorization that would otherwise apply.

```csharp
using HotChocolate.Authorization;
using HotChocolate.Types;

[MutationType]
[Authorize]
public static partial class Mutation
{
    [AllowAnonymous]
    public static async Task<RegisterPayload> RegisterAsync(
        RegisterInput input,
        AccountService accounts,
        CancellationToken cancellationToken)
    {
        return await accounts.RegisterAsync(input, cancellationToken);
    }

    public static async Task<ChangeEmailPayload> ChangeEmailAsync(
        ChangeEmailInput input,
        AccountService accounts,
        CancellationToken cancellationToken)
    {
        return await accounts.ChangeEmailAsync(input, cancellationToken);
    }
}
```

In this example, the `register` field is public, while `changeEmail` still requires authorization from the mutation type.

Do not use `Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute` for schema fields.

# Choose when authorization runs

Most fields should use the default `ApplyPolicy.BeforeResolver` behavior.

| Value            | Behavior                                                                                   | When to use                                                                                                   |
| ---------------- | ------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------- |
| `BeforeResolver` | Authorizes before the resolver runs. This is the default.                                  | When you want to avoid resolver work unless access is allowed.                                                |
| `AfterResolver`  | Runs the resolver, then authorizes if the result is non-null and not already an error.     | When a policy needs information from the resolved value. Use with care, as data access happens before denial. |
| `Validation`     | Collects the authorization rule during request validation and can reject before execution. | When you need request-level authorization before resolvers run.                                               |

```csharp
using HotChocolate.Authorization;

[Authorize(Apply = ApplyPolicy.BeforeResolver)]
public static SecretReport GetSecretReport(ReportService reports)
{
    return reports.GetSecretReport();
}
```

`ApplyPolicy.Validation` can produce a request-level authorization failure. Field-level denials usually appear as GraphQL errors at the selected field path.

# Keep endpoint authorization separate

Schema authorization and endpoint authorization address different concerns.

```csharp
app.MapGraphQL().RequireAuthorization();
```

`RequireAuthorization()` secures the ASP.NET Core endpoint. When used with the combined `MapGraphQL()` endpoint, it can also affect GraphQL HTTP, WebSocket connections, SDL download, and Nitro, depending on how endpoints are mapped.

Use endpoint authorization when every operation through that endpoint should require authorization. Use `[Authorize]` when you want to mix public and private fields within the same schema or endpoint.

If you want Nitro to be public but GraphQL HTTP to be protected, split the endpoints:

```csharp
app.MapGraphQLHttp().RequireAuthorization();
app.MapGraphQLWebSocket();
app.MapNitroApp();
```

# Use attributes or descriptors

Attributes are well suited for static, local rules that should be visible next to the type or resolver.

Use descriptor-based authorization when the rule is shared, conditional, generated, or easier to review in central schema configuration.

```csharp
using HotChocolate.Types;

public sealed class BillingQueriesType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("billingAccount")
            .Resolve(context => context.Service<BillingService>().GetCurrentAccount())
            .Authorize(Policies.CanReadBilling);
    }
}
```

Common descriptor equivalents include `.Authorize()`, `.Authorize(policy)`, `.Authorize(roles)`, and `.AllowAnonymous()`.

# Troubleshoot authorization attributes

| Symptom                                 | What to check                                                                                                                                         |
| --------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------- |
| `[Authorize]` has no effect.            | Ensure you use `HotChocolate.Authorization`, install `HotChocolate.AspNetCore.Authorization`, and call `.AddGraphQL().AddAuthorization()`.            |
| Every request is anonymous.             | Verify authentication setup, `app.UseAuthentication()`, `app.UseAuthorization()`, and the order of middleware.                                        |
| `AUTH_NOT_AUTHENTICATED`                | The user is missing or `Identity.IsAuthenticated` is false.                                                                                           |
| `AUTH_NOT_AUTHORIZED`                   | The user is authenticated but does not meet a role or policy requirement.                                                                             |
| `AUTH_POLICY_NOT_FOUND`                 | Register the policy in `builder.Services.AddAuthorization(...)` and check the policy name for typos.                                                  |
| `AUTH_NO_DEFAULT_POLICY`                | Restore the ASP.NET Core default policy or specify a policy name in `[Authorize]`.                                                                    |
| A field becomes `null`.                 | This is expected for nullable field-level denial. Check schema nullability if a parent field also becomes `null`.                                     |
| Nitro or schema download is blocked.    | You may have protected the combined ASP.NET Core endpoint. Split endpoint mappings if needed.                                                         |
| WebSocket subscription user is missing. | Ensure the WebSocket connection and each operation carry the authenticated user context. If you customize interceptors, call the base implementation. |

# API quick reference

| Item                   | Value                                                |
| ---------------------- | ---------------------------------------------------- |
| Namespace              | `HotChocolate.Authorization`                         |
| Package                | `HotChocolate.AspNetCore.Authorization`              |
| `[Authorize]` targets  | Class, struct, property, method                      |
| Repeatable             | Yes                                                  |
| Members                | `Policy`, `Roles`, `Apply`                           |
| Default apply mode     | `ApplyPolicy.BeforeResolver`                         |
| Unsupported MVC member | `AuthenticationSchemes`                              |
| Public field opt-out   | `[AllowAnonymous]` from `HotChocolate.Authorization` |

Internal directives such as `@authorize` are hidden from downloaded SDL by default. You typically verify authorization by executing protected operations, not by inspecting the directive in the schema SDL.

# Next steps

- Set up identity in [Authentication](/docs/hotchocolate/v16/build/security/authentication).
- Learn about policy handlers and endpoint rules in [Authorization](/docs/hotchocolate/v16/build/security/authorization).
- Review endpoint mapping in [Endpoints](/docs/hotchocolate/v16/build/server-configuration/endpoints).
- Review resolver signatures in [Resolver Parameter Attributes](../resolvers/parameter-attributes).
