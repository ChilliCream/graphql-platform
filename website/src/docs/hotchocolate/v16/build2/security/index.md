---
title: Security
---

# Security

GraphQL security is a layered design problem. A Hot Chocolate server often exposes one HTTP endpoint, but each request can vary field selection, depth, aliases, fragments, variables, batching, file uploads, and transport. Authentication tells you who the caller is. Authorization tells you what that caller may access. Operation controls and limits tell you how much work a valid caller may ask the server to perform.

Use this page as the entry point for securing a Hot Chocolate v16 API. It routes detailed setup to the dedicated pages while showing the decisions you should make before production.

## Secure GraphQL in layers

Hot Chocolate builds on ASP.NET Core and adds GraphQL-specific controls. A production setup should align every layer with your threat model:

1. Endpoint and transport surface: CORS, preflight requirements, GET, multipart uploads, WebSockets, schema SDL, Nitro, batching, and persisted operation endpoints.
2. ASP.NET Core authentication: JWT bearer, cookies, OpenID Connect, or your application scheme.
3. Hot Chocolate authorization: field, type, mutation, role, policy, and tenant checks.
4. Operation-shape control: trusted documents for known clients, or cost analysis for dynamic clients.
5. Parser, validation, execution, paging, batching, and depth limits.
6. Introspection and schema exposure policy.
7. Logging, metrics, and review of rejected operations.

Hot Chocolate provides useful defaults when you use the standard server registration path. Your application still owns authentication schemes, authorization rules, endpoint exposure, trusted document policy, and schema-specific budgets.

## Start with your exposure model

### Public or third-party clients: budget every operation

When clients can write dynamic operations, the server needs budgets for work that can happen before resolver execution and during execution. Start with [cost analysis](cost-analysis.md), then review [execution depth and limits](execution-depth-and-limits.md), paging boundaries, [authorization](authorization.md), endpoint hardening, and [introspection](introspection.md).

Use this model when:

- External clients compose their own GraphQL documents.
- Your API is reachable from browsers, partners, mobile apps, or automation.
- You cannot pre-register every operation during client builds.

### First-party or private clients: trust only known operations

For controlled clients, prefer [trusted documents](trusted-documents.md). In current Hot Chocolate APIs this is configured through persisted operations. The client sends an operation identifier that was registered at build or deployment time, and the server rejects unknown operation text.

Automatic persisted queries are not the same security posture. APQ is a performance and caching handshake unless you also block non-persisted operation text.

Use this model when:

- Client builds are controlled by your team.
- Operations can be registered before deployment.
- Unknown operation text should be rejected in production.

### Internal APIs still need limits

Internal traffic can be harmful through compromised credentials, automation bugs, overly broad service permissions, or accidental fan-out. Keep default security enabled unless you have equivalent controls configured and documented.

## Security checklist

| Layer                   | Risk addressed                                    | Control                                                                                                          | Default posture                                                                  | Action                                                                      | Detailed page                                                                                                     |
| ----------------------- | ------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------- | --------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------- |
| Identity                | Anonymous callers                                 | ASP.NET Core authentication                                                                                      | No scheme is configured by Hot Chocolate                                         | Configure JWT, cookies, OIDC, or your app scheme                            | [Authentication](authentication.md)                                                                               |
| Field and type access   | Overbroad data exposure                           | `HotChocolate.AspNetCore.Authorization`, `[Authorize]`, `@authorize`, roles, policies, descriptor `.Authorize()` | Requires package and registration                                                | Protect sensitive fields, types, mutations, and ownership rules             | [Authorization](authorization.md)                                                                                 |
| Endpoint gate           | Coarse API access                                 | `app.MapGraphQL().RequireAuthorization()`                                                                        | Not enabled automatically                                                        | Use for broad endpoint access, not as a replacement for field authorization | [Authorization](authorization.md), [endpoints](../../server/endpoints.md)                                         |
| Operation allowlist     | Arbitrary operation text                          | `UsePersistedOperationPipeline()` and `OnlyAllowPersistedDocuments`                                              | Not enabled automatically                                                        | Register trusted documents and reject unknown documents                     | [Trusted documents](trusted-documents.md)                                                                         |
| Operation cost          | Expensive but valid dynamic operations            | Cost analyzer, `[Cost]`, `[ListSize]`, `ModifyCostOptions`                                                       | Added by default security                                                        | Measure and tune budgets for your schema                                    | [Cost analysis](cost-analysis.md)                                                                                 |
| Parser limits           | Oversized documents before validation             | `ModifyParserOptions`                                                                                            | Defaults include max fields 2048, directives per location 4, recursion depth 200 | Review limits for public endpoints                                          | [Execution depth and limits](execution-depth-and-limits.md)                                                       |
| Validation limits       | Fragment, merge, introspection, and cycle attacks | Validation options and depth rules                                                                               | Defaults include validation error and comparison limits                          | Tune based on observed traffic                                              | [Execution depth and limits](execution-depth-and-limits.md)                                                       |
| Execution limits        | Long-running operations                           | `ModifyRequestOptions`, `ExecutionTimeout`                                                                       | 30 seconds, longer while a debugger is attached                                  | Set a production timeout and observe timeouts                               | [Execution depth and limits](execution-depth-and-limits.md)                                                       |
| Pagination and fan-out  | Large lists and node amplification                | `MaxPageSize`, required paging boundaries, node batch size                                                       | Depends on schema configuration                                                  | Set list boundaries before exposing large collections                       | [Execution depth and limits](execution-depth-and-limits.md), [pagination](../../resolvers-and-data/pagination.md) |
| Introspection and SDL   | Schema discovery and recursive introspection cost | `AllowIntrospection(false)`, endpoint schema options                                                             | Introspection is disabled outside development by default security                | Decide separate policies for introspection, SDL requests, and schema files  | [Introspection](introspection.md), [endpoints](../../server/endpoints.md)                                         |
| Endpoint surface        | Unneeded tools and transports                     | `GraphQLServerOptions`                                                                                           | Several features are available unless configured                                 | Disable what your clients do not use                                        | [Endpoints](../../server/endpoints.md)                                                                            |
| Browser transport       | CSRF-style browser requests                       | GET and multipart preflight enforcement, CORS, cookie posture                                                    | Multipart preflight enforcement is enabled, GET preflight enforcement is off     | Review browser and BFF scenarios                                            | [HTTP transport](../../server/http-transport.md)                                                                  |
| Batching                | Request amplification                             | `AllowedBatching`, `MaxBatchSize`, `MaxConcurrentExecutions`                                                     | Batching is disabled by default                                                  | If enabled, set a batch size that matches infrastructure limits             | [Batching](../../server/batching.md), [endpoints](../../server/endpoints.md)                                      |
| WebSocket subscriptions | Unauthenticated or stale sessions                 | `ISocketSessionInterceptor`, connection init timeout, per-operation checks                                       | Timeout and keepalive have defaults                                              | Authenticate connection init and authorize each operation when needed       | [Interceptors](../../server/interceptors.md), [HTTP transport](../../server/http-transport.md)                    |
| Error disclosure        | Leaking exception details                         | `IncludeExceptionDetails`                                                                                        | Depends on debugger attachment                                                   | Keep exception details off in production and log server-side                | [Options](../../api-reference/options.md)                                                                         |

## What Hot Chocolate protects by default

When default security is enabled through the standard server registration, Hot Chocolate v16 adds important GraphQL protections:

- Cost analysis is added.
- Introspection is disabled when the host environment is not development.
- A maximum field cycle depth rule is added.
- Batching is disabled by default.
- Multipart request preflight enforcement is enabled by default.
- WebSocket initialization timeout and keepalive have defaults.
- Request execution timeout defaults to 30 seconds, with a longer timeout while a debugger is attached.

These defaults do not configure:

- Your authentication scheme.
- Field, type, mutation, tenant, or ownership authorization rules.
- Trusted documents.
- Schema-specific cost budgets.
- Production decisions for Nitro, schema SDL, schema file support, GET, multipart uploads, batching, CORS, cookies, or WebSockets.

> Warning: `disableDefaultSecurity: true` removes the default security protections. Use it only when equivalent controls are configured manually and documented.

## Choose the right operation-shape control

### Trusted documents

Trusted documents are best for controlled client builds and private first-party clients. Configure the persisted operation pipeline and block non-persisted operation text:

```csharp
builder.Services
    .AddGraphQLServer()
    .UsePersistedOperationPipeline()
    .ModifyRequestOptions(o =>
        o.PersistedOperations.OnlyAllowPersistedDocuments = true);
```

Expected result:

- Requests without a registered operation ID are rejected.
- Operation text is not accepted unless you create a controlled exception path.
- The client hash algorithm, storage provider, and deployed registry must match.

Continue with [Trusted documents](trusted-documents.md). If you use APQ for performance, also read [Automatic persisted operations](../../performance/automatic-persisted-operations.md) so the allowlist boundary stays clear.

### Cost analysis

Cost analysis is best when clients can write dynamic operations. Set budgets after observing real traffic and schema behavior:

```csharp
builder.Services
    .AddGraphQLServer()
    .ModifyCostOptions(o =>
    {
        o.MaxFieldCost = 2_000;
        o.MaxTypeCost = 2_000;
    });
```

Expected result:

- Over-budget operations are rejected before resolver execution.
- `GraphQL-Cost: report` and `GraphQL-Cost: validate` can help measure traffic before you enforce stricter budgets.
- Cost limits should be paired with list size annotations, paging boundaries, filtering and sorting review, depth limits, and endpoint controls.

Continue with [Cost analysis](cost-analysis.md).

### Execution depth and limits

Cost budgets do not replace parser, validation, execution, depth, cycle, paging, batching, and timeout limits. Use [Execution depth and limits](execution-depth-and-limits.md) to tune document size, recursion, fragment visits, field merge comparisons, introspection recursion, global object identification batching, request timeout, and concurrent execution behavior.

## Secure access to data

### Authenticate with ASP.NET Core

Hot Chocolate reads the ASP.NET Core user for GraphQL requests. It does not configure JWT bearer, cookies, OpenID Connect, or another scheme for your app.

```csharp
app.UseAuthentication();
app.UseAuthorization();

app.MapGraphQL();
```

Resolvers can access the current user through resolver context APIs, and `IHttpRequestInterceptor` can add or transform request state. If you override interceptor methods, preserve the base behavior where the detailed guide requires it so services, request state, and the `ClaimsPrincipal` remain available.

Continue with [Authentication](authentication.md).

### Authorize fields, types, and business actions

Endpoint authorization is a broad gate. It does not replace field, type, mutation, ownership, or tenant checks inside the schema.

```csharp
builder.Services.AddAuthorization();

builder.Services
    .AddGraphQLServer()
    .AddAuthorization();

app.MapGraphQL().RequireAuthorization();
```

Use the Hot Chocolate authorization package and APIs for schema authorization: `[Authorize]`, `@authorize`, roles, policies, descriptor `.Authorize()`, and `[AllowAnonymous]` where a public field is intentional. Design ownership and tenant checks for nested fields and mutations, not only for top-level queries.

Continue with [Authorization](authorization.md).

## Harden endpoints and transports

### HTTP endpoint

Review each feature exposed by your GraphQL endpoint:

- Disable GET if clients do not need it. Keep GET limited to queries when it remains enabled.
- Consider `EnforceGetRequestsPreflightHeader` for browser or cookie scenarios.
- Disable multipart requests if uploads are not used. Keep multipart preflight enforcement enabled when uploads are enabled.
- Disable schema requests and schema file support if they are not part of your production contract.
- Decide whether Nitro should be available in production.
- Review request size and maximum concurrent executions.

```csharp
app.MapGraphQL().WithOptions(o =>
{
    o.Tool.Enable = false;
    o.EnableSchemaRequests = false;
    o.EnableMultipartRequests = false;
    o.EnforceGetRequestsPreflightHeader = true;
});
```

Continue with [Endpoints](../../server/endpoints.md) and [HTTP transport](../../server/http-transport.md).

### Batching and persisted operation endpoints

Batching is disabled by default. If you enable it, set a maximum batch size and review concurrency so one request cannot amplify work beyond infrastructure limits.

`MapGraphQLPersistedOperations()` exposes REST-like URLs for persisted operations. Use it only when that endpoint is part of your documented client contract and protected by the same transport and authorization decisions as your GraphQL endpoint.

Continue with [Batching](../../server/batching.md) and [Trusted documents](trusted-documents.md).

### WebSocket and subscriptions

WebSocket subscriptions have connection state that differs from ordinary HTTP requests. Authenticate the connection initialization request, reject unauthorized sessions, and authorize each operation when permissions can change during a long-lived connection. Use `ISocketSessionInterceptor` for connection init and request hooks, and preserve base interceptor behavior where required.

Continue with [Interceptors](../../server/interceptors.md) and [HTTP transport](../../server/http-transport.md).

## Introspection and schema visibility

Disabling introspection reduces schema discovery and recursive introspection cost, but it is not a secrecy boundary by itself. It does not disable SDL schema requests or schema file support. `__typename` is also not the same as full schema introspection and is commonly required by clients.

```csharp
builder.Services
    .AddGraphQLServer()
    .AllowIntrospection(false);
```

Use endpoint options for SDL and schema file exposure, and use per-request overrides only for trusted callers that should inspect the schema.

Continue with [Introspection](introspection.md), [Endpoints](../../server/endpoints.md), and [Interceptors](../../server/interceptors.md).

## Error disclosure and observability

Keep exception details disabled in production. Return client-safe GraphQL errors while logging enough server-side context to investigate failures.

Track and review:

- Rejected trusted document requests.
- Cost reports and over-budget operations.
- Validation errors, parser failures, depth failures, and timeouts.
- Authorization failures and denied fields.
- WebSocket connection init failures.
- GET, multipart, batching, and upload rejections.

Operational feedback is part of security. Tune budgets using observed traffic, not guesses alone.

## Production readiness checklist

Before production, verify:

- Authentication scheme and middleware order are configured.
- Hot Chocolate authorization is registered where field or type authorization is used.
- Sensitive fields, types, mutations, and subscriptions are protected.
- Ownership and tenant isolation are covered by policies or resolver logic.
- Public APIs have measured and tuned cost analysis.
- Private APIs have trusted documents registered and non-persisted operations blocked.
- Parser, validation, depth, field cycle, fragment, execution timeout, paging, node batch, batch size, and concurrency limits are reviewed.
- Introspection, SDL, schema file support, and Nitro exposure are intentional for production.
- GET, multipart, preflight, CORS, cookies, BFF behavior, and upload posture are reviewed.
- WebSocket authentication, connection init timeout, and per-operation authorization are reviewed.
- Exception details are disabled in production.
- Rejected operations, authorization failures, cost reports, validation errors, and timeouts are observable.

## Common mistakes and quick routes

| Symptom                                                   | Check                                                                                                                                                       | Go to                                                                             |
| --------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------- |
| `[Authorize]` is ignored                                  | Use Hot Chocolate authorization attributes, install the package, register ASP.NET Core authorization, and call `.AddAuthorization()` on the GraphQL builder | [Authorization](authorization.md)                                                 |
| The authenticated user is missing in a resolver           | Check ASP.NET Core middleware order and interceptor base calls                                                                                              | [Authentication](authentication.md), [Interceptors](../../server/interceptors.md) |
| Introspection is disabled but the schema is still visible | Separate introspection from SDL endpoint and schema file support                                                                                            | [Introspection](introspection.md), [Endpoints](../../server/endpoints.md)         |
| A valid operation is rejected by cost analysis            | Use report or validate mode, then inspect list sizes, paging limits, `[Cost]`, `[ListSize]`, and budgets                                                    | [Cost analysis](cost-analysis.md)                                                 |
| Trusted document requests are rejected                    | Verify operation ID, hash algorithm, storage provider, deployed registry contents, and `OnlyAllowPersistedDocuments`                                        | [Trusted documents](trusted-documents.md)                                         |
| WebSocket user state disappears                           | Check `ISocketSessionInterceptor` hooks and required base calls                                                                                             | [Interceptors](../../server/interceptors.md)                                      |
| GET or upload requests fail in browsers                   | Check preflight header requirements, CORS, cookies, and BFF behavior                                                                                        | [HTTP transport](../../server/http-transport.md)                                  |
| Endpoint authorization works but nested data leaks        | Add field, type, policy, ownership, or tenant authorization in the schema                                                                                   | [Authorization](authorization.md)                                                 |

## Next steps

- [Authentication](authentication.md)
- [Authorization](authorization.md)
- [Cost analysis](cost-analysis.md)
- [Execution depth and limits](execution-depth-and-limits.md)
- [Introspection](introspection.md)
- [Trusted documents](trusted-documents.md)
