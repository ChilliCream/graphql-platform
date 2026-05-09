---
title: Authorize attribute
---

Use `[Authorize]` to protect GraphQL object types and fields with the Hot Chocolate authorization pipeline. The attribute is the code-first entry point for schema authorization in Hot Chocolate v16.

`[Authorize]` checks the current `ClaimsPrincipal` against the ASP.NET Core default policy, a named policy, or roles. Authentication must create that `ClaimsPrincipal` before authorization can pass.

> **Use `HotChocolate.Authorization.AuthorizeAttribute`.** Do not import `Microsoft.AspNetCore.Authorization.AuthorizeAttribute` for schema members. The Microsoft attribute protects ASP.NET Core endpoints and MVC actions. It does not add Hot Chocolate schema authorization.

# Enable authorization before using the attribute

Install the ASP.NET Core authorization integration package:

<PackageInstallation packageName="HotChocolate.AspNetCore.Authorization" />

Configure authentication, ASP.NET Core authorization, and Hot Chocolate authorization in `Program.cs`:

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

`builder.Services.AddAuthorization(...)` registers ASP.NET Core policies and role handling. `AddAuthorization()` on the GraphQL builder registers Hot Chocolate's internal `@authorize` directive and execution middleware.

`[Authorize]` does not authenticate requests and does not protect the whole HTTP or WebSocket endpoint. It protects the schema members where you place it.

# Require an authenticated user for one field

Apply `[Authorize]` to a resolver method when the whole field needs an authenticated user.

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

With no `Policy` or `Roles` argument, `[Authorize]` uses the ASP.NET Core default authorization policy. In a typical setup, that means the user must be authenticated.

Client query:

```graphql
query GetViewer {
  viewer {
    id
    displayName
  }
}
```

If no authenticated user is available, Hot Chocolate returns a GraphQL error and the protected nullable field is `null`:

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

If the protected field is non-null, normal GraphQL null bubbling applies.

# Protect a property or object field

Apply `[Authorize]` to a property when most of the object is public but one field is private.

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

The public fields stay available. The `email` field requires an authenticated user:

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

# Protect every field returned as a type

Apply `[Authorize]` to an object type when the same rule should protect all entry points that return that type.

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

In v16, object type authorization is evaluated when a field returns the authorized type. It is not evaluated again for each selected child field. Use field-level authorization when only selected fields on a type are private.

You can also place `[Authorize]` on a source-generator type class:

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

When you authorize a root operation type, Hot Chocolate applies that authorization to non-introspection root fields.

# Restrict access by role

Use `Roles` when role claims are enough to decide access.

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

Multiple roles are alternatives. A user with either `Support` or `Administrator` can access this field:

```csharp
[Authorize(Roles = ["Support", "Administrator"])]
public static IReadOnlyList<AuditEntry> GetSupportAuditLog(AuditStore store)
{
    return store.GetEntries();
}
```

Roles come from the authenticated `ClaimsPrincipal` and are evaluated by ASP.NET Core authorization.

# Restrict access by policy

Policies keep authorization logic out of resolvers. Register the policy with ASP.NET Core authorization, then reference it by name from `[Authorize]`.

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

Use constants for policy names to avoid typo-driven `AUTH_POLICY_NOT_FOUND` errors.

For custom requirements and handlers, see [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization). Hot Chocolate passes the resolver context as the authorization resource for field authorization.

# Combine rules and repeated attributes

A single `[Authorize]` attribute can require both a policy and one of several roles:

```csharp
[Authorize(Policy = "HasSupportContract", Roles = ["Support", "Administrator"])]
public static CustomerAccount GetCustomerAccount(CustomerService customers, string id)
{
    return customers.GetAccount(id);
}
```

The policy must pass, and the user must have at least one listed role.

`[Authorize]` is repeatable. Repeated attributes are cumulative, so all listed policies must pass:

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

In this example, `register` is public and `changeEmail` still requires authorization from the mutation type.

Do not import `Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute` for schema fields.

# Choose when authorization runs

Most fields should keep the default `ApplyPolicy.BeforeResolver` behavior.

| Value            | Behavior                                                                                   | Use it when                                                                                                     |
| ---------------- | ------------------------------------------------------------------------------------------ | --------------------------------------------------------------------------------------------------------------- |
| `BeforeResolver` | Authorizes before the resolver runs. This is the default.                                  | You want to avoid resolver work unless access is allowed.                                                       |
| `AfterResolver`  | Runs the resolver, then authorizes if the result is non-null and not already an error.     | A policy needs information from the resolved value. Use deliberately because data access happens before denial. |
| `Validation`     | Collects the authorization rule during request validation and can reject before execution. | You need request-level authorization before resolvers run.                                                      |

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

Schema authorization and endpoint authorization solve different problems.

```csharp
app.MapGraphQL().RequireAuthorization();
```

`RequireAuthorization()` protects the ASP.NET Core endpoint. When used with the combined `MapGraphQL()` endpoint, it can also affect GraphQL HTTP, WebSocket connections, SDL download, and Nitro depending on how you map endpoints.

Use endpoint authorization when every operation through that endpoint should require authorization. Use `[Authorize]` when public and private fields share the same schema or endpoint.

If you want Nitro public but GraphQL HTTP protected, split the endpoints:

```csharp
app.MapGraphQLHttp().RequireAuthorization();
app.MapGraphQLWebSocket();
app.MapNitroApp();
```

# Use attributes or descriptors

Attributes work well for static, local rules that should be visible next to the type or resolver.

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

Common descriptor equivalents are `.Authorize()`, `.Authorize(policy)`, `.Authorize(roles)`, and `.AllowAnonymous()`.

# Troubleshoot authorization attributes

| Symptom                                 | Check                                                                                                                                                 |
| --------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------- |
| `[Authorize]` has no effect.            | Use `HotChocolate.Authorization`, install `HotChocolate.AspNetCore.Authorization`, and call `.AddGraphQL().AddAuthorization()`.                       |
| Every request is anonymous.             | Verify authentication setup, `app.UseAuthentication()`, `app.UseAuthorization()`, and middleware order.                                               |
| `AUTH_NOT_AUTHENTICATED`                | The user is missing or `Identity.IsAuthenticated` is false.                                                                                           |
| `AUTH_NOT_AUTHORIZED`                   | The user is authenticated but does not satisfy a role or policy requirement.                                                                          |
| `AUTH_POLICY_NOT_FOUND`                 | Register the policy in `builder.Services.AddAuthorization(...)` and check the policy name.                                                            |
| `AUTH_NO_DEFAULT_POLICY`                | Restore the ASP.NET Core default policy or pass a policy name to `[Authorize]`.                                                                       |
| A field becomes `null`.                 | This is expected for nullable field-level denial. Check schema nullability if a parent field also becomes `null`.                                     |
| Nitro or schema download is blocked.    | You likely protected the combined ASP.NET Core endpoint. Split endpoint mappings if needed.                                                           |
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

Internal directives such as `@authorize` are hidden from downloaded SDL by default in v16. You usually verify authorization by executing protected operations, not by looking for the directive in schema SDL.

# Next steps

- Set up identity in [Authentication](/docs/hotchocolate/v16/securing-your-api/authentication).
- Learn policy handlers and endpoint rules in [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization).
- Review endpoint mapping in [Endpoints](/docs/hotchocolate/v16/server/endpoints).
- Review resolver signatures in [Resolver Parameter Attributes](../resolvers/parameter-attributes).
