---
title: Authorization
---

Authorization determines whether the current user can perform an operation, execute a resolver, or access specific data. Authentication always comes first, creating the `ClaimsPrincipal`. Authorization then evaluates this principal against endpoint rules, GraphQL schema rules, and any application-specific resource rules.

A clear threat model for a Hot Chocolate API separates these responsibilities:

| Need                                                               | Use                                                                                  | Result shape                                                                             |
| ------------------------------------------------------------------ | ------------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------- |
| Block anonymous callers before GraphQL runs                        | ASP.NET Core endpoint authorization with `RequireAuthorization()`                    | ASP.NET Core challenge or forbid response. No GraphQL partial data.                      |
| Protect selected fields, mutations, subscriptions, or object types | Hot Chocolate `[Authorize]`, descriptor `.Authorize()`, or schema-first `@authorize` | GraphQL errors with partial data when possible.                                          |
| Check whether a user owns a loaded domain object                   | ASP.NET Core policy with a resource, or service-level domain check                   | Application-specific result. Often return a normalized not-found or not-allowed payload. |
| Reject an operation before execution                               | `ApplyPolicy.Validation`                                                             | Operation-level authorization failure. No field-level partial result for that operation. |

# Before you start

Begin by installing the ASP.NET Core authorization integration package:

<PackageInstallation packageName="HotChocolate.AspNetCore.Authorization" />

For GraphQL schema code, import:

```csharp
using HotChocolate.Authorization;
```

For ASP.NET Core policies and handlers, import:

```csharp
using Microsoft.AspNetCore.Authorization;
```

> Warning: Use `HotChocolate.Authorization.AuthorizeAttribute` and `HotChocolate.Authorization.AllowAnonymousAttribute` on GraphQL schema members. The Microsoft attributes only protect ASP.NET Core endpoints and MVC actions. They do not add authorization to the Hot Chocolate schema.

# Enable authorization

To enable authorization, register authentication, ASP.NET Core authorization, and Hot Chocolate authorization. The `.AddAuthorization()` method for GraphQL registers the `@authorize` directive and the middleware that enforces it.

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

The call to `builder.Services.AddAuthorization(...)` sets up ASP.NET Core policies and role management. The `.AddAuthorization()` call for GraphQL links these policies to schema-level authorization. Always ensure authentication middleware runs before authorization middleware.

# Protect a field

Apply `[Authorize]` to a field when it should only be accessible to authenticated users or those meeting a specific policy.

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

If you do not specify a `Policy` or `Roles`, `[Authorize]` uses the default ASP.NET Core policy. Typically, this means the user must be authenticated.

Example anonymous request:

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

Public sibling fields can still resolve. Field-level authorization failures are returned as GraphQL execution results, so the HTTP status is usually `200 OK` if the request was valid. If a protected field is non-nullable, GraphQL null propagation may cause the nearest nullable parent to become `null` as well.

# Apply authorization to schema members

## Resolver methods

You can apply `[Authorize]` to query, mutation, and subscription resolver methods.

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

Always derive the current user ID from claims or a trusted server-side context. Avoid accepting a `userId` argument for current-user operations unless a separate admin policy protects that field.

## Object types and fields

Apply `[Authorize]` to an object type if every field returning that object should require the same authorization rule.

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

A field-specific rule adds an extra layer of protection. Use field-level authorization when only certain fields are private.

## Descriptor APIs

Descriptors allow you to define authorization rules in type configuration rather than with attributes. This approach is often clearer when rules are shared, generated, conditional, or applied to types you do not own.

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

Common descriptor APIs include:

| API                                                      | Meaning                                                     |
| -------------------------------------------------------- | ----------------------------------------------------------- |
| `descriptor.Authorize()`                                 | Require the default ASP.NET Core policy for an object type. |
| `descriptor.Authorize("PolicyName")`                     | Require a named policy for an object type.                  |
| `descriptor.Authorize("Guest", "Administrator")`         | Require at least one listed role for an object type.        |
| `descriptor.Field(t => t.Email).Authorize()`             | Require the default policy for a field.                     |
| `descriptor.Field(t => t.Email).Authorize("PolicyName")` | Require a named policy for a field.                         |
| `descriptor.Field(t => t.Email).AllowAnonymous()`        | Allow a field through a surrounding authorization rule.     |

If you need a single role requirement on a descriptor, prefer a named policy or use an attribute with `Roles = [...]` to avoid confusion with the single-string policy overload.

## Schema-first directives

If you build your schema from SDL, use the `@authorize` directive on object types and field definitions.

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

The directive is repeatable and also supports the `apply` argument:

```graphql
type Query {
  order(id: ID!): Order
    @authorize(policy: "CanReadOrders", apply: BEFORE_RESOLVER)

  auditLog: [AuditEntry!]!
    @authorize(roles: ["Administrator"], apply: VALIDATION)
}
```

SDL enum values for `apply` are `BEFORE_RESOLVER`, `AFTER_RESOLVER`, and `VALIDATION`. Internal directives like `@authorize` are hidden from downloaded SDL by default, so always verify authorization by executing protected operations.

# Roles

Roles provide coarse-grained checks based on role claims in the `ClaimsPrincipal`.

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

A single role in an authorization rule is sufficient. Multiple `[Authorize]` attributes are cumulative.

| Configuration                                              | Meaning                                         |
| ---------------------------------------------------------- | ----------------------------------------------- |
| `[Authorize]`                                              | User must pass the ASP.NET Core default policy. |
| `[Authorize(Roles = ["Admin", "Support"])]`                | User must have at least one listed role.        |
| `[Authorize(Policy = "CanReadOrders", Roles = ["Admin"])]` | User must pass the policy and have the role.    |
| `[Authorize(Policy = "A")][Authorize(Policy = "B")]`       | User must pass policy `A` and policy `B`.       |

Prefer policies for domain-specific permissions such as `CanReadOrders`, `CanManageBasket`, or tenant-specific checks. Use roles for broad administrative categories.

# Policies

Policies allow you to keep permission logic out of resolvers and centralize it within ASP.NET Core authorization.

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

To apply a policy to a field:

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

Policy names are string values at the authorization boundary. Use constants to help prevent `AUTH_POLICY_NOT_FOUND` errors caused by typos.

## Use GraphQL context in a policy handler

For field-level authorization, you can use the Hot Chocolate resolver context as the ASP.NET Core authorization resource. This allows a handler to inspect arguments, the path, field metadata, services, or request state.

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

Use this approach for GraphQL-specific checks. Do not use it to filter collections; a list resolver or service should only query rows the user is allowed to see.

## Authorize loaded resources

If your authorization decision depends on a domain object, load the object through a service and call ASP.NET Core's `IAuthorizationService` with that object as the resource.

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

Returning the same payload for both not-found and not-allowed cases helps prevent leaking information about the existence of protected resources.

# Choose when authorization runs

The `ApplyPolicy` enum in Hot Chocolate controls when the GraphQL authorization middleware evaluates a rule.

| Value            | When it runs                                       | Best for                                                        | Watch for                                                                                  |
| ---------------- | -------------------------------------------------- | --------------------------------------------------------------- | ------------------------------------------------------------------------------------------ |
| `BeforeResolver` | Before the resolver executes. This is the default. | Most queries, mutations, and subscriptions.                     | The resolved object is not available yet.                                                  |
| `AfterResolver`  | After the resolver returns a non-null result.      | Policies that need resolver context after data has been loaded. | Resolver work has already happened. Avoid side-effectful mutations and sensitive preloads. |
| `Validation`     | During request validation before execution.        | Operation-level checks and infrastructure fields.               | Produces an operation-level failure rather than field-level partial data.                  |

Examples with attributes:

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

Examples with descriptors:

```csharp
descriptor
    .Field(t => t.GetOrderAsync(default!, default!, default))
    .Authorize(Policies.CanReadOrders, ApplyPolicy.AfterResolver);

descriptor
    .Field(t => t.GetAuditLogAsync(default!, default!))
    .Authorize(Policies.CanReadAuditLog, ApplyPolicy.Validation);
```

Validation-level authorization does not deny a field resolver. In executor tests, validation failures carry HTTP status code `401`. If HTTP status codes are important for your clients, cover this behavior with an integration test for your endpoint setup.

# Allow anonymous fields

The `[AllowAnonymous]` attribute from `HotChocolate.Authorization` can be used on fields and resolver methods. It allows a field to bypass authorization that would otherwise apply. You can also use `.AllowAnonymous()` in descriptor configuration for the same effect at the field level.

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

In this example, `register` is public, while `changeEmail` inherits authorization from the mutation type. Always review child fields, nested selections, and nullability before adding an anonymous exception.

# Endpoint authorization vs GraphQL authorization

Endpoint authorization acts as a broad ASP.NET Core gate:

```csharp
app.MapGraphQL().RequireAuthorization();
```

Use this when every request to the endpoint must be authenticated before GraphQL processing begins. ASP.NET Core will handle the challenge or forbid response, usually returning HTTP `401` or `403`, and GraphQL execution will not produce partial data.

Schema authorization provides more granular control:

```csharp
[Authorize(Policy = Policies.CanReadOrders)]
public static Task<Order?> GetOrderAsync(/* ... */)
{
    // Protected field on a shared endpoint.
}
```

Use schema authorization when your schema contains both public and private fields, or when clients should receive partial data.

`MapGraphQL()` combines GraphQL HTTP, WebSocket, schema SDL download, and Nitro on a single path. If Nitro or schema tooling requires different access rules, map each part separately:

```csharp
app.MapGraphQLHttp("/graphql").RequireAuthorization();
app.MapGraphQLWebSocket("/graphql").RequireAuthorization();

app.MapNitroApp("/graphql/ui").WithOptions(o =>
{
    o.GraphQLEndpoint = "/graphql";
});
```

Endpoint authorization does not replace field, type, mutation, tenant, or ownership checks within the schema.

# Current user access in resolvers

Hot Chocolate can inject the current `ClaimsPrincipal` as a resolver parameter:

```csharp
public static string? GetViewerId(ClaimsPrincipal user)
{
    return user.FindFirstValue(ClaimTypes.NameIdentifier);
}
```

Alternatively, you can access the user from `IResolverContext`:

```csharp
using HotChocolate.Resolvers;

public static string? GetViewerId(IResolverContext context)
{
    return context.GetUser()?.FindFirstValue(ClaimTypes.NameIdentifier);
}
```

Keep identity creation, token validation, claim mapping, and login flows within authentication code. Authorization code should only consume trusted claims or application context.

# Subscriptions and WebSockets

You can apply `[Authorize]`, descriptor `.Authorize()`, roles, and policies to subscription fields in the same way as for query and mutation fields.

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

WebSockets introduce additional transport considerations:

- `MapGraphQLWebSocket().RequireAuthorization()` applies endpoint authorization to the socket endpoint.
- `ISocketSessionInterceptor.OnConnectAsync` can accept or reject a `connection_init` message.
- `DefaultSocketSessionInterceptor.OnRequestAsync` adds services and global state, including the `ClaimsPrincipal`, to each operation request.
- If you override `OnRequestAsync`, always call the base implementation.
- Long-lived connections may outlast token lifetimes unless your app reconnects, refreshes, or revalidates.
- Authorize subscription setup and filter topics or published payloads by user and tenant. Topic names must not leak cross-tenant data.

If you validate a token from the WebSocket payload, ensure that identity is set as the per-operation user before relying on field authorization. The exact setup depends on your authentication configuration.

# Error behavior

| Failure point                                            | Typical response                                                                      | Code                                 |
| -------------------------------------------------------- | ------------------------------------------------------------------------------------- | ------------------------------------ |
| Field or type authorization, unauthenticated             | GraphQL error at the field path. Nullable field becomes `null`; siblings may resolve. | `AUTH_NOT_AUTHENTICATED`             |
| Field or type authorization, authenticated but forbidden | GraphQL error at the field path. Nullable field becomes `null`; siblings may resolve. | `AUTH_NOT_AUTHORIZED`                |
| Missing named policy                                     | GraphQL error where the rule ran.                                                     | `AUTH_POLICY_NOT_FOUND`              |
| Missing default policy                                   | GraphQL error when a default policy is required but unavailable.                      | `AUTH_NO_DEFAULT_POLICY`             |
| `ApplyPolicy.Validation`                                 | Operation-level authorization failure before resolver execution.                      | Depends on the failed policy result. |
| Endpoint `RequireAuthorization()`                        | ASP.NET Core challenge or forbid before GraphQL execution.                            | No GraphQL error body is guaranteed. |

Authorization error messages should never reveal sensitive details. Log enough server-side context to investigate denials, but keep client-facing messages generic.

# Test authorization

## Executor tests

Executor tests provide fast, precise validation for field and type authorization. Register your policies, add GraphQL authorization, and pass a `ClaimsPrincipal` using `OperationRequestBuilder.SetUser(...)`.

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

Be sure to test these scenarios:

- Anonymous user receives `AUTH_NOT_AUTHENTICATED`.
- Authenticated user without the required role or policy receives `AUTH_NOT_AUTHORIZED`.
- User with the correct role or policy succeeds.
- Policy name typo returns `AUTH_POLICY_NOT_FOUND`.
- `[AllowAnonymous]` bypasses a surrounding field rule only where intended.
- Non-null fields propagate authorization nulls as expected by clients.

Use snapshots to verify the complete GraphQL response shape, not only individual properties.

## HTTP integration tests

For HTTP integration tests, use an ASP.NET Core test authentication handler or an interceptor to set `HttpContext.User`. Assert endpoint authorization using HTTP status codes, and verify GraphQL field authorization with response snapshots. If your application uses WebSockets, include tests for connection and per-operation scenarios for your interceptor and subscription fields.

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

Use `ModifyAuthorizationOptions(...)` and `AddAuthorizationHandler<T>()` only if the built-in ASP.NET Core authorization integration does not fit your security requirements.

# Next steps

- Set up identity in [Authentication](authentication.md).
- Review the full attribute reference in [Authorize attribute](../attributes/authorize.md).
- Review endpoint mapping in [Endpoints](../server-configuration/endpoints.md).
- Review WebSocket hooks in [Interceptors](../server-configuration/interceptors.md).
- Review subscription fields in [Operations: subscriptions](../type-system/operations-subscriptions.md).
- Review resolver parameters in [Resolver signatures](../resolvers/resolver-signature.md).
