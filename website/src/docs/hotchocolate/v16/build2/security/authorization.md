---
title: Authorization
---

Authorization decides whether the current user may execute an operation, run a resolver, or read selected data. Authentication comes first. It creates the `ClaimsPrincipal`; authorization evaluates that principal against endpoint rules, GraphQL schema rules, and application-specific resource rules.

A useful threat model for a Hot Chocolate API separates these concerns:

| Need                                                               | Use                                                                                  | Result shape                                                                             |
| ------------------------------------------------------------------ | ------------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------- |
| Block anonymous callers before GraphQL runs                        | ASP.NET Core endpoint authorization with `RequireAuthorization()`                    | ASP.NET Core challenge or forbid response. No GraphQL partial data.                      |
| Protect selected fields, mutations, subscriptions, or object types | Hot Chocolate `[Authorize]`, descriptor `.Authorize()`, or schema-first `@authorize` | GraphQL errors with partial data when possible.                                          |
| Check whether a user owns a loaded domain object                   | ASP.NET Core policy with a resource, or service-level domain check                   | Application-specific result. Often return a normalized not-found or not-allowed payload. |
| Reject an operation before execution                               | `ApplyPolicy.Validation`                                                             | Operation-level authorization failure. No field-level partial result for that operation. |

# Before you start

Install the ASP.NET Core authorization integration package:

<PackageInstallation packageName="HotChocolate.AspNetCore.Authorization" />

Use these namespaces in GraphQL schema code:

```csharp
using HotChocolate.Authorization;
```

Use these namespaces for ASP.NET Core policies and handlers:

```csharp
using Microsoft.AspNetCore.Authorization;
```

> Warning: Use `HotChocolate.Authorization.AuthorizeAttribute` and `HotChocolate.Authorization.AllowAnonymousAttribute` on GraphQL schema members. The Microsoft attributes protect ASP.NET Core endpoints and MVC actions. They do not add Hot Chocolate schema authorization.

# Enable authorization

Register authentication, ASP.NET Core authorization, and Hot Chocolate authorization. The GraphQL `.AddAuthorization()` call registers the `@authorize` directive and the execution middleware that evaluates it.

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.CanManageBasket, policy =>
        policy.RequireClaim("scope", "basket:write"));
});

builder
    .AddGraphQL()
    .AddAuthorization()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGraphQL();

app.Run();

public static class Policies
{
    public const string CanManageBasket = "CanManageBasket";
}
```

`builder.Services.AddAuthorization(...)` defines ASP.NET Core policies and role handling. GraphQL `.AddAuthorization()` connects those policies to schema authorization. Authentication middleware must run before authorization middleware.

# Protect a field

Use `[Authorize]` when a field requires an authenticated user or a policy.

```csharp
#nullable enable

using System.Security.Claims;
using HotChocolate.Authorization;
using HotChocolate.Types;

[QueryType]
public static partial class Query
{
    public static Task<IReadOnlyList<Product>> GetProductsAsync(
        CatalogService catalog,
        CancellationToken cancellationToken)
    {
        return catalog.GetProductsAsync(cancellationToken);
    }

    [Authorize]
    public static async Task<Basket?> GetViewerBasketAsync(
        ClaimsPrincipal user,
        BasketService baskets,
        CancellationToken cancellationToken)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

        return userId is null
            ? null
            : await baskets.GetByUserIdAsync(userId, cancellationToken);
    }
}
```

With no `Policy` or `Roles`, `[Authorize]` uses the ASP.NET Core default policy. In the common setup, that means the user must be authenticated.

Anonymous request:

```graphql
query GetHomePage {
  products {
    id
    name
  }
  viewerBasket {
    id
  }
}
```

Typical field-level response:

```json
{
  "errors": [
    {
      "message": "The current user is not authorized to access this resource.",
      "path": ["viewerBasket"],
      "extensions": {
        "code": "AUTH_NOT_AUTHENTICATED"
      }
    }
  ],
  "data": {
    "products": [
      {
        "id": "1",
        "name": "Chili Oil"
      }
    ],
    "viewerBasket": null
  }
}
```

The public sibling field can still resolve. Field-level authorization failures are GraphQL execution results, so HTTP is commonly `200 OK` when the request itself was valid. If the protected field is non-null, normal GraphQL null propagation can make the nearest nullable parent `null`.

# Apply authorization to schema members

## Resolver methods

Apply `[Authorize]` to query, mutation, and subscription resolver methods.

```csharp
using System.Security.Claims;
using HotChocolate.Authorization;
using HotChocolate.Types;

[MutationType]
public static partial class Mutation
{
    [Authorize(Policy = Policies.CanManageBasket)]
    public static async Task<BasketPayload> AddToBasketAsync(
        AddToBasketInput input,
        ClaimsPrincipal user,
        BasketService baskets,
        CancellationToken cancellationToken)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId is null)
        {
            return BasketPayload.NotAllowed();
        }

        var basket = await baskets.AddItemAsync(
            userId,
            input.ProductId,
            input.Quantity,
            cancellationToken);

        return BasketPayload.FromBasket(basket);
    }
}
```

Derive the current user ID from claims or trusted server-side context. Do not accept a `userId` argument for current-user operations unless a separate admin policy protects that field.

## Object types and fields

Apply `[Authorize]` to an object type when every field that returns that object should require the same rule.

```csharp
using HotChocolate.Authorization;

[Authorize(Policy = Policies.CanReadOrders)]
public sealed class Order
{
    public string Id { get; init; } = default!;

    public decimal Total { get; init; }

    [Authorize(Policy = Policies.CanReadOrderInternalNotes)]
    public string? InternalNotes { get; init; }
}
```

A field-specific rule is an additional rule. Use field-level authorization when only selected fields are private.

## Descriptor APIs

Descriptors keep authorization rules in type configuration instead of attributes. They are often clearer when rules are shared, generated, conditional, or applied to types you do not own.

```csharp
using HotChocolate.Authorization;
using HotChocolate.Types;

public sealed class OrderType : ObjectType<Order>
{
    protected override void Configure(IObjectTypeDescriptor<Order> descriptor)
    {
        descriptor.Authorize(Policies.CanReadOrders);

        descriptor
            .Field(t => t.InternalNotes)
            .Authorize(Policies.CanReadOrderInternalNotes);
    }
}
```

Common descriptor APIs are:

| API                                                      | Meaning                                                     |
| -------------------------------------------------------- | ----------------------------------------------------------- |
| `descriptor.Authorize()`                                 | Require the default ASP.NET Core policy for an object type. |
| `descriptor.Authorize("PolicyName")`                     | Require a named policy for an object type.                  |
| `descriptor.Authorize("Guest", "Administrator")`         | Require at least one listed role for an object type.        |
| `descriptor.Field(t => t.Email).Authorize()`             | Require the default policy for a field.                     |
| `descriptor.Field(t => t.Email).Authorize("PolicyName")` | Require a named policy for a field.                         |
| `descriptor.Field(t => t.Email).AllowAnonymous()`        | Allow a field through a surrounding authorization rule.     |

For a single role requirement on a descriptor, prefer a named policy or an attribute with `Roles = [...]` so the intent is not confused with the single-string policy overload.

## Schema-first directives

When you build a schema from SDL, use `@authorize` on object types and field definitions.

```graphql
type Query {
  viewerBasket: Basket @authorize
  order(id: ID!): Order @authorize(policy: "CanReadOrders")
  auditLog: [AuditEntry!]! @authorize(roles: ["Support", "Administrator"])
}

type Mutation {
  addToBasket(input: AddToBasketInput!): BasketPayload!
    @authorize(policy: "CanManageBasket")
}
```

The directive is repeatable and also supports `apply`:

```graphql
type Query {
  order(id: ID!): Order
    @authorize(policy: "CanReadOrders", apply: BEFORE_RESOLVER)

  auditLog: [AuditEntry!]!
    @authorize(roles: ["Administrator"], apply: VALIDATION)
}
```

The SDL enum values are `BEFORE_RESOLVER`, `AFTER_RESOLVER`, and `VALIDATION`. Internal directives such as `@authorize` are hidden from downloaded SDL by default, so verify authorization by executing protected operations.

# Roles

Roles are coarse-grained checks based on role claims in the `ClaimsPrincipal`.

```csharp
using HotChocolate.Authorization;
using HotChocolate.Types;

[QueryType]
public static partial class Query
{
    [Authorize(Roles = ["Administrator"])]
    public static Task<IReadOnlyList<AuditEntry>> GetAuditLogAsync(
        AuditStore audit,
        CancellationToken cancellationToken)
    {
        return audit.GetEntriesAsync(cancellationToken);
    }

    [Authorize(Roles = ["Support", "Administrator"])]
    public static Task<IReadOnlyList<Ticket>> GetSupportTicketsAsync(
        TicketStore tickets,
        CancellationToken cancellationToken)
    {
        return tickets.GetOpenTicketsAsync(cancellationToken);
    }
}
```

One role in a single authorization rule is enough. Repeated authorization rules are cumulative.

| Configuration                                              | Meaning                                         |
| ---------------------------------------------------------- | ----------------------------------------------- |
| `[Authorize]`                                              | User must pass the ASP.NET Core default policy. |
| `[Authorize(Roles = ["Admin", "Support"])]`                | User must have at least one listed role.        |
| `[Authorize(Policy = "CanReadOrders", Roles = ["Admin"])]` | User must pass the policy and have the role.    |
| `[Authorize(Policy = "A")][Authorize(Policy = "B")]`       | User must pass policy `A` and policy `B`.       |

Prefer policies for domain permissions such as `CanReadOrders`, `CanManageBasket`, and tenant-specific decisions. Roles are better for broad administrative categories.

# Policies

Policies keep permission logic out of resolvers and centralize it in ASP.NET Core authorization.

```csharp
using Microsoft.AspNetCore.Authorization;

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.CanReadOrders, policy =>
        policy.RequireClaim("scope", "orders:read"));

    options.AddPolicy(Policies.CanManageBasket, policy =>
        policy.Requirements.Add(new SameTenantRequirement()));
});

builder.Services.AddSingleton<IAuthorizationHandler, SameTenantHandler>();
```

Apply a policy to a field:

```csharp
using HotChocolate.Authorization;
using HotChocolate.Types;

[QueryType]
public static partial class Query
{
    [Authorize(Policy = Policies.CanReadOrders)]
    public static Task<Order?> GetOrderAsync(
        string id,
        OrderService orders,
        CancellationToken cancellationToken)
    {
        return orders.GetByIdAsync(id, cancellationToken);
    }
}
```

Policy names are strings at the authorization boundary. Use constants to reduce typo-driven `AUTH_POLICY_NOT_FOUND` errors.

## Use GraphQL context in a policy handler

For field-level authorization, the ASP.NET Core authorization resource can be the Hot Chocolate resolver context. This lets a handler inspect arguments, path, field metadata, services, or request state.

```csharp
using HotChocolate.Resolvers;
using Microsoft.AspNetCore.Authorization;

public sealed class SameTenantRequirement : IAuthorizationRequirement;

public sealed class SameTenantHandler
    : AuthorizationHandler<SameTenantRequirement, IResolverContext>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SameTenantRequirement requirement,
        IResolverContext resolverContext)
    {
        var requestedTenant = resolverContext.ArgumentValue<string>("tenantId");

        if (context.User.HasClaim("tenant_id", requestedTenant))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
```

Use this pattern for GraphQL-specific checks. Do not rely on it to filter collections. A list resolver or service must query only rows the user may see.

## Authorize loaded resources

When the decision depends on a domain object, load the object through a service and call ASP.NET Core `IAuthorizationService` with that object as the resource.

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

[Authorize]
public static async Task<CancelOrderPayload> CancelOrderAsync(
    string orderId,
    ClaimsPrincipal user,
    IAuthorizationService authorization,
    OrderService orders,
    CancellationToken cancellationToken)
{
    var order = await orders.GetByIdAsync(orderId, cancellationToken);

    if (order is null)
    {
        return CancelOrderPayload.NotFound();
    }

    var result = await authorization.AuthorizeAsync(
        user,
        order,
        Policies.CanCancelOrder);

    if (!result.Succeeded)
    {
        return CancelOrderPayload.NotFound();
    }

    await orders.CancelAsync(order, cancellationToken);

    return CancelOrderPayload.FromOrder(order);
}
```

Returning the same payload for not-found and not-allowed can avoid leaking whether a protected resource exists.

# Choose when authorization runs

`ApplyPolicy` is a Hot Chocolate enum. It controls when the GraphQL authorization middleware invokes a rule.

| Value            | When it runs                                       | Best for                                                        | Watch for                                                                                  |
| ---------------- | -------------------------------------------------- | --------------------------------------------------------------- | ------------------------------------------------------------------------------------------ |
| `BeforeResolver` | Before the resolver executes. This is the default. | Most queries, mutations, and subscriptions.                     | The resolved object is not available yet.                                                  |
| `AfterResolver`  | After the resolver returns a non-null result.      | Policies that need resolver context after data has been loaded. | Resolver work has already happened. Avoid side-effectful mutations and sensitive preloads. |
| `Validation`     | During request validation before execution.        | Operation-level checks and infrastructure fields.               | Produces an operation-level failure rather than field-level partial data.                  |

Attribute examples:

```csharp
[Authorize]
public static Task<Basket?> GetViewerBasketAsync(/* ... */)
{
    // Uses ApplyPolicy.BeforeResolver.
}

[Authorize(Policy = Policies.CanReadOrders, Apply = ApplyPolicy.AfterResolver)]
public static Task<Order?> GetOrderAsync(/* ... */)
{
    // The resolver runs before the policy is evaluated.
}

[Authorize(Policy = Policies.CanReadAuditLog, Apply = ApplyPolicy.Validation)]
public static Task<IReadOnlyList<AuditEntry>> GetAuditLogAsync(/* ... */)
{
    // Authorization is collected during validation.
}
```

Descriptor examples:

```csharp
descriptor
    .Field(t => t.GetOrderAsync(default!, default!, default))
    .Authorize(Policies.CanReadOrders, ApplyPolicy.AfterResolver);

descriptor
    .Field(t => t.GetAuditLogAsync(default!, default!))
    .Authorize(Policies.CanReadAuditLog, ApplyPolicy.Validation);
```

Validation-level authorization is not a field resolver denial. In v16 executor tests, validation failures carry HTTP status code context `401`. If HTTP status matters to your clients, cover that behavior with an integration test for your endpoint setup.

# Allow anonymous fields

`[AllowAnonymous]` from `HotChocolate.Authorization` is supported on fields and resolver methods. It bypasses authorization that would otherwise apply to that field. Descriptor configuration supports the same field-level exception with `.AllowAnonymous()`.

```csharp
using HotChocolate.Authorization;
using HotChocolate.Types;

[MutationType]
[Authorize]
public static partial class Mutation
{
    [AllowAnonymous]
    public static Task<RegisterPayload> RegisterAsync(
        RegisterInput input,
        AccountService accounts,
        CancellationToken cancellationToken)
    {
        return accounts.RegisterAsync(input, cancellationToken);
    }

    public static Task<ChangeEmailPayload> ChangeEmailAsync(
        ChangeEmailInput input,
        AccountService accounts,
        CancellationToken cancellationToken)
    {
        return accounts.ChangeEmailAsync(input, cancellationToken);
    }
}
```

Here `register` is public and `changeEmail` inherits authorization from the mutation type. Review child fields, nested selections, and nullability before adding an anonymous exception.

# Endpoint authorization vs GraphQL authorization

Endpoint authorization is a coarse ASP.NET Core gate:

```csharp
app.MapGraphQL().RequireAuthorization();
```

Use it when every request through that endpoint must be authenticated before GraphQL starts. ASP.NET Core handles the challenge or forbid response, commonly as HTTP `401` or `403`, and GraphQL execution does not produce partial data.

Schema authorization is finer grained:

```csharp
[Authorize(Policy = Policies.CanReadOrders)]
public static Task<Order?> GetOrderAsync(/* ... */)
{
    // Protected field on a shared endpoint.
}
```

Use schema authorization when public and private fields share one schema or when clients benefit from partial data.

`MapGraphQL()` combines GraphQL HTTP, WebSocket, schema SDL download, and Nitro on one path. If Nitro or schema tooling needs different access rules, map the pieces separately:

```csharp
app.MapGraphQLHttp("/graphql").RequireAuthorization();
app.MapGraphQLWebSocket("/graphql").RequireAuthorization();

app.MapNitroApp("/graphql/ui").WithOptions(o =>
{
    o.GraphQLEndpoint = "/graphql";
});
```

Endpoint authorization does not replace field, type, mutation, tenant, or ownership checks inside the schema.

# Current user access in resolvers

Hot Chocolate can bind the current `ClaimsPrincipal` as a resolver parameter:

```csharp
public static string? GetViewerId(ClaimsPrincipal user)
{
    return user.FindFirstValue(ClaimTypes.NameIdentifier);
}
```

You can also read the user from `IResolverContext`:

```csharp
using HotChocolate.Resolvers;

public static string? GetViewerId(IResolverContext context)
{
    return context.GetUser()?.FindFirstValue(ClaimTypes.NameIdentifier);
}
```

Keep identity creation, token validation, claim mapping, and login flows in authentication code. Authorization code should consume trusted claims or trusted application context.

# Subscriptions and WebSockets

Apply `[Authorize]`, descriptor `.Authorize()`, roles, and policies to subscription fields the same way you apply them to query and mutation fields.

```csharp
using HotChocolate.Authorization;
using HotChocolate.Types;

[SubscriptionType]
public static partial class Subscription
{
    [Authorize(Policy = Policies.CanReadOrders)]
    public static OrderUpdated OnOrderUpdated(
        [EventMessage] OrderUpdated message)
    {
        return message;
    }
}
```

WebSockets add transport concerns:

- `MapGraphQLWebSocket().RequireAuthorization()` is endpoint authorization for the socket endpoint.
- `ISocketSessionInterceptor.OnConnectAsync` can accept or reject a `connection_init` message.
- `DefaultSocketSessionInterceptor.OnRequestAsync` adds services and important global state, including the `ClaimsPrincipal`, to each operation request.
- If you override `OnRequestAsync`, call the base implementation.
- Long-lived connections can outlive token lifetimes unless your app reconnects, refreshes, or revalidates.
- Authorize subscription setup and filter topics or published payloads by user and tenant. Topic names must not leak cross-tenant data.

If you validate a token from the WebSocket payload, also design how that identity becomes the per-operation user before relying on field authorization. The exact wiring depends on your authentication setup.

# Error behavior

| Failure point                                            | Typical response                                                                      | Code                                 |
| -------------------------------------------------------- | ------------------------------------------------------------------------------------- | ------------------------------------ |
| Field or type authorization, unauthenticated             | GraphQL error at the field path. Nullable field becomes `null`; siblings may resolve. | `AUTH_NOT_AUTHENTICATED`             |
| Field or type authorization, authenticated but forbidden | GraphQL error at the field path. Nullable field becomes `null`; siblings may resolve. | `AUTH_NOT_AUTHORIZED`                |
| Missing named policy                                     | GraphQL error where the rule ran.                                                     | `AUTH_POLICY_NOT_FOUND`              |
| Missing default policy                                   | GraphQL error when a default policy is required but unavailable.                      | `AUTH_NO_DEFAULT_POLICY`             |
| `ApplyPolicy.Validation`                                 | Operation-level authorization failure before resolver execution.                      | Depends on the failed policy result. |
| Endpoint `RequireAuthorization()`                        | ASP.NET Core challenge or forbid before GraphQL execution.                            | No GraphQL error body is guaranteed. |

Authorization error messages should not reveal sensitive details. Log enough server-side context to investigate denials, but keep client messages generic.

# Test authorization

## Executor tests

Executor tests are fast and precise for field/type authorization. Register policies, add GraphQL authorization, and pass a `ClaimsPrincipal` with `OperationRequestBuilder.SetUser(...)`.

```csharp
using System.Security.Claims;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

var user = new ClaimsPrincipal(
    new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "user-1"),
            new Claim(ClaimTypes.Role, "Administrator")
        ],
        authenticationType: "Test"));

var executor = await new ServiceCollection()
    .AddAuthorization(options =>
    {
        options.AddPolicy(Policies.CanReadOrders, policy =>
            policy.RequireClaim("scope", "orders:read"));
    })
    .AddGraphQLServer()
    .AddAuthorization()
    .AddQueryType<Query>()
    .BuildRequestExecutorAsync();

var result = await executor.ExecuteAsync(
    OperationRequestBuilder.New()
        .SetDocument("{ viewerBasket { id } }")
        .SetUser(user)
        .Build());

result.MatchInlineSnapshot(
    """
    {
      "data": {
        "viewerBasket": {
          "id": "basket-1"
        }
      }
    }
    """);
```

Test at least these cases:

- Anonymous user gets `AUTH_NOT_AUTHENTICATED`.
- Authenticated user without a role or policy gets `AUTH_NOT_AUTHORIZED`.
- Allowed role or policy succeeds.
- Policy name typo returns `AUTH_POLICY_NOT_FOUND`.
- `[AllowAnonymous]` bypasses a surrounding field rule only where intended.
- Non-null fields bubble authorization nulls the way clients expect.

Use snapshots for complete GraphQL response shapes rather than checking only one property.

## HTTP integration tests

Use an ASP.NET Core test authentication handler or an interceptor to set `HttpContext.User`. Assert endpoint authorization with HTTP status codes, and assert GraphQL field authorization with response snapshots. If you use WebSockets, include connection and per-operation cases for your interceptor and subscription fields.

# Troubleshooting

| Symptom                                           | Likely cause                                                                                        | Fix                                                                                                                                          |
| ------------------------------------------------- | --------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------- |
| `[Authorize]` has no effect.                      | Microsoft attribute namespace, missing package, or missing GraphQL `.AddAuthorization()`.           | Import `HotChocolate.Authorization`, install `HotChocolate.AspNetCore.Authorization`, and call `.AddAuthorization()` on the GraphQL builder. |
| Every request is anonymous.                       | Authentication handler did not run, wrong scheme, or unauthenticated test identity.                 | Configure authentication, call `UseAuthentication()` before `UseAuthorization()`, and create test identities with an authentication type.    |
| Roles never match.                                | Role claims use a different claim type or are not mapped from the token.                            | Review authentication claim mapping and ASP.NET Core role settings.                                                                          |
| `AUTH_POLICY_NOT_FOUND`.                          | Policy name typo or policy registered in a different service collection.                            | Register the policy in `builder.Services.AddAuthorization(...)` and use constants for names.                                                 |
| `AUTH_NO_DEFAULT_POLICY`.                         | A default policy is required but the policy provider does not provide one.                          | Restore the ASP.NET Core default policy or use named policies.                                                                               |
| Field returns `null` with an authorization error. | Field-level denial on a nullable field.                                                             | This is expected. Review nullability and client handling.                                                                                    |
| Parent field becomes `null`.                      | Protected child or field is non-null and GraphQL null propagation applies.                          | Revisit schema nullability and payload design.                                                                                               |
| Nitro, SDL, or schema tooling is blocked.         | `MapGraphQL().RequireAuthorization()` protects the combined endpoint.                               | Split `MapGraphQLHttp()`, `MapGraphQLWebSocket()`, and `MapNitroApp()`.                                                                      |
| WebSocket user is missing.                        | Custom socket interceptor skipped base request setup or identity was not carried to each operation. | Call `base.OnRequestAsync(...)` and verify per-operation user state.                                                                         |
| Unauthorized users trigger mutation side effects. | `ApplyPolicy.AfterResolver` was used on a side-effectful resolver.                                  | Use `BeforeResolver` for mutations that must not run before authorization.                                                                   |
| List contains items from other tenants.           | Field authorization checked access to the field, not each list item.                                | Filter in the resolver, data loader, or service query.                                                                                       |

# Advanced reference

| API                                       | Purpose                                                                                        |
| ----------------------------------------- | ---------------------------------------------------------------------------------------------- |
| `[Authorize]`                             | Apply default policy, named policy, roles, or `ApplyPolicy` to an object type or field.        |
| `[AllowAnonymous]`                        | Bypass authorization on a field or resolver method.                                            |
| `IObjectTypeDescriptor.Authorize(...)`    | Apply authorization to an object type.                                                         |
| `IObjectFieldDescriptor.Authorize(...)`   | Apply authorization to a field.                                                                |
| `IObjectFieldDescriptor.AllowAnonymous()` | Allow a field inside a protected area.                                                         |
| `ModifyAuthorizationOptions(...)`         | Configure advanced authorization for fields such as `node`, `nodes`, `__schema`, and `__type`. |
| `AddAuthorizationHandler<T>()`            | Replace the Hot Chocolate authorization handler for non-standard authorization systems.        |

Reach for `ModifyAuthorizationOptions(...)` and `AddAuthorizationHandler<T>()` only when the built-in ASP.NET Core authorization integration does not match your security architecture.

# Next steps

- Set up identity in [Authentication](authentication.md).
- Review the complete attribute reference in [Authorize attribute](../attributes/authorize.md).
- Review endpoint mapping in [Endpoints](../server-configuration/endpoints.md).
- Review WebSocket hooks in [Interceptors](../server-configuration/interceptors.md).
- Review subscription fields in [Operations: subscriptions](../schema-elements/operations-subscriptions.md).
- Review resolver parameters in [Resolver signatures](../resolvers/resolver-signature.md).
