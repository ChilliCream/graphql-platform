---
title: "Control introspection and schema download"
---

This page guides you through controlling schema discovery in a Hot Chocolate v16 ASP.NET Core server. You'll learn how to set a production policy for GraphQL introspection, SDL download routes, and hosted Nitro tooling, and how to verify that only the intended surfaces are exposed to public traffic.

This guidance applies to Hot Chocolate source-schema services. Fusion gateway behavior is separate and not covered here.

# Prerequisites

Before you begin, ensure you have:

- A Hot Chocolate v16 ASP.NET Core server set up with `builder.AddGraphQL()` or `builder.Services.AddGraphQLServer()`.
- A mapped GraphQL endpoint, such as `app.MapGraphQL()` for development or split endpoints like `app.MapGraphQLHttp()` in production.
- ASP.NET Core authentication and authorization if you want to restrict schema access to developers.
- A reliable way to check the environment using `builder.Environment` or `app.Environment`.
- The ability to test from the same network path as production users, not just from `localhost`.

A minimal server setup looks like this:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQL()
    .AddQueryType<Query>();

var app = builder.Build();

app.MapGraphQL();

app.Run();
```

Apply server-level options on the GraphQL builder. Use endpoint-level options with the mapped endpoint via `WithOptions`. For per-request exceptions, use an HTTP request interceptor.

# Set a production schema visibility policy

Start with a clear policy, then adapt it to your product and developer workflow.

| Traffic                   | Introspection                 | SDL/schema download            | Nitro                       | Recommended access pattern                              |
| ------------------------- | ----------------------------- | ------------------------------ | --------------------------- | ------------------------------------------------------- |
| Local development         | Allowed                       | Allowed if tooling needs it    | Enabled                     | Developer machine only                                  |
| Public production clients | Disabled                      | Disabled                       | Disabled                    | Normal operation execution only                         |
| Internal developers       | Allowed by policy             | Protected route or CI artifact | Protected or separate route | Authenticated users, roles, claims, or internal ingress |
| CI/CD and registries      | Not needed against production | Export from build output       | Not needed                  | `schema export` command or protected artifact           |

A typical baseline keeps schema discovery open in development and closes it for public production traffic:

```csharp
var isDevelopment = builder.Environment.IsDevelopment();

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .DisableIntrospection(disable: !isDevelopment)
    .ModifyServerOptions(options =>
    {
        options.EnableSchemaRequests = isDevelopment;
        options.EnableSchemaFileSupport = isDevelopment;
        options.Tool.Enable = isDevelopment;
    });
```

In production, you should see:

- `/graphql` continues to execute allowed operations.
- Introspection queries return HTTP 400 with GraphQL error code `HC0046`.
- Integrated SDL routes return `404 Not Found`.
- Browser requests to the public GraphQL endpoint do not load Nitro.

`AddGraphQLServer(..., disableDefaultSecurity: false)` applies the default security policy, which disables executable introspection outside development, adds cost analysis, and enforces maximum field-cycle depth. Always keep your policy explicit if you use `AddGraphQL()`, set `disableDefaultSecurity: true`, or want reviewers to see the production decision in one place.

# Understand the schema discovery surfaces

Disabling introspection does not close every path to schema discovery. Treat each surface as a separate control.

| Surface                 | What it is                                                | Common route or query                                                                | Control                                                                            | Production recommendation                                 |
| ----------------------- | --------------------------------------------------------- | ------------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------- | --------------------------------------------------------- |
| GraphQL introspection   | Executable GraphQL fields such as `__schema` and `__type` | `{ __schema { types { name } } }`                                                    | `.DisableIntrospection(...)` and per-request `requestBuilder.AllowIntrospection()` | Disable for public production traffic.                    |
| Runtime `__typename`    | Meta field for unions and interfaces                      | `{ node { __typename } }`                                                            | Normal GraphQL execution and authorization                                         | Usually keep available; not a main discovery risk.        |
| Integrated SDL download | Schema file support on `MapGraphQL()`                     | `GET /graphql?sdl`, `/graphql/schema`, `/graphql/schema/`, `/graphql/schema.graphql` | `EnableSchemaRequests` and `EnableSchemaFileSupport`                               | Disable publicly or protect via endpoint design.          |
| Explicit SDL endpoint   | Separate endpoint mapped by your app                      | `app.MapGraphQLSchema()` defaults to `/graphql/sdl`                                  | Map or remove the endpoint, plus `.RequireAuthorization(...)`                      | Do not map publicly, or require developer authorization.  |
| Hosted Nitro            | Browser IDE and operation tooling                         | Browser `GET /graphql` or `MapNitroApp("/graphql/ui")`                               | `Tool.Enable` or Nitro endpoint options                                            | Disable publicly, or host on a protected developer route. |

This distinction is important for security reviews. For example, disabling introspection does not fix a finding for `/graphql?sdl`. Nitro can load even if introspection is disabled, but may not display a schema.

# Disable public introspection and allow developer access by policy

Use `DisableIntrospection()` to block executable introspection. If authenticated developers need temporary access, add a targeted interceptor.

```csharp
using HotChocolate.AspNetCore;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Authorization;

public sealed class DeveloperIntrospectionInterceptor(
    IAuthorizationService authorization)
    : DefaultHttpRequestInterceptor
{
    public override async ValueTask OnCreateAsync(
        HttpContext context,
        IRequestExecutor requestExecutor,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        var result = await authorization.AuthorizeAsync(
            context.User,
            resource: null,
            policyName: "GraphQLDevelopers");

        if (result.Succeeded)
        {
            requestBuilder.AllowIntrospection();
        }

        await base.OnCreateAsync(
            context,
            requestExecutor,
            requestBuilder,
            cancellationToken);
    }
}
```

Register the policy and interceptor:

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("GraphQLDevelopers", policy =>
        policy.RequireAuthenticatedUser().RequireRole("GraphQLDeveloper"));
});

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .DisableIntrospection()
    .AddHttpRequestInterceptor<DeveloperIntrospectionInterceptor>();
```

With this setup:

- Anonymous public introspection receives `HC0046`.
- Users who meet the `GraphQLDevelopers` policy can introspect.

Do not use shared headers as a production bypass. In production, exceptions should rely on authentication, authorization policies, claims, roles, mTLS, API key validation, or a protected internal network path.

To customize the denial message, update the interceptor:

```csharp
if (!result.Succeeded)
{
    requestBuilder.SetIntrospectionNotAllowedMessage(
        "Introspection requires the GraphQLDevelopers policy.");
}
```

A denied request returns a GraphQL error like this:

```json
{
  "errors": [
    {
      "message": "Introspection is not allowed for the current request.",
      "extensions": {
        "field": "__schema",
        "code": "HC0046"
      }
    }
  ]
}
```

# Disable SDL and schema download routes

SDL download is independent of introspection. Configure schema file options directly.

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .ModifyServerOptions(options =>
    {
        options.EnableSchemaRequests = false;
        options.EnableSchemaFileSupport = false;
    });
```

If you configure a specific endpoint, check endpoint overrides as well:

```csharp
app.MapGraphQL().WithOptions(options =>
{
    options.EnableSchemaRequests = false;
    options.EnableSchemaFileSupport = false;
});
```

Setting `EnableSchemaRequests = false` prevents the schema middleware from handling schema download candidates. Setting `EnableSchemaFileSupport = false` returns `404 Not Found` for those requests. Set both options in production to keep your policy clear, even if routing changes later.

Integrated schema download candidates include:

```text
GET /graphql?sdl
GET /graphql/schema
GET /graphql/schema/
GET /graphql/schema.graphql
GET /graphql?sdl&types=Query
```

Because the `types=` query can export selected types, block the entire schema download surface, not just the full schema file.

If your team needs an SDL endpoint, map it explicitly and protect it:

```csharp
app.MapGraphQLSchema("/internal/graphql/sdl")
    .RequireAuthorization("GraphQLDevelopers");
```

With this configuration:

- `GET /graphql?sdl`, `/graphql/schema`, `/graphql/schema/`, and `/graphql/schema.graphql` return `404 Not Found` when integrated schema download is disabled.
- `/graphql/sdl` exists only if you mapped `MapGraphQLSchema()`.
- A protected explicit route returns `401 Unauthorized` or `403 Forbidden` for callers outside the developer policy.

# Expose or disable Nitro intentionally

Nitro is developer tooling. You can serve it from the integrated `MapGraphQL()` endpoint for browser requests or from a separate `MapNitroApp()` route. Nitro is not an authorization boundary and does not replace endpoint or field authorization.

To disable hosted Nitro on the integrated endpoint in production:

```csharp
app.MapGraphQL().WithOptions(options =>
{
    options.Tool.Enable = app.Environment.IsDevelopment();
});
```

If developers need a browser IDE in production, split execution and tooling routes:

```csharp
app.MapGraphQLHttp("/graphql")
    .RequireAuthorization("GraphQLAccess");

app.MapNitroApp("/graphql/ui", "../graphql")
    .RequireAuthorization("GraphQLDevelopers");
```

Nitro may load but fail to show a schema if introspection or SDL downloads are disabled, or if the user lacks authorization. This is expected when schema discovery is closed. Use authenticated developer access, a local environment, an exported SDL file, or a registry workflow instead of opening public discovery for the UI.

# Replace production discovery with schema export and registry workflows

Production clients and tools should not introspect the live public endpoint. Instead, export the schema during CI and publish the artifact for your tooling.

First, install `HotChocolate.AspNetCore.CommandLine`, then return the command exit code from `Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQL()
    .AddQueryType<Query>();

var app = builder.Build();

app.MapGraphQL();

return await app.RunWithGraphQLCommandsAsync(args);
```

Export the schema in CI with:

```bash
dotnet run -- schema export --output schema.graphql
```

You should see:

```text
schema.graphql
```

If schema construction or export fails, the command returns a non-zero exit code, allowing CI to stop deployment. Use the exported SDL for client code generation, schema review, and publishing to the [Nitro schema registry](/docs/nitro/apis/schema-registry).

You can also export during startup if the deployment environment can write and protect the file:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .ExportSchemaOnStartup("./schema.graphql");
```

Never write exported SDL into a public web root. Treat schema artifacts for private APIs as internal documentation.

# Pair closed discovery with trusted documents

Closing schema discovery limits what public callers can learn at runtime, but does not prevent arbitrary executable operations. For first-party clients, use trusted documents so production clients send operation IDs instead of ad hoc query text.

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .UsePersistedOperationPipeline()
    .ModifyRequestOptions(options =>
    {
        options.PersistedOperations.OnlyAllowPersistedDocuments = true;
        options.PersistedOperations.AllowDocumentBody = false;
    });
```

With `OnlyAllowPersistedDocuments = true`, the server rejects operations not in persisted storage. Setting `AllowDocumentBody = false` enforces strict transport for clients that no longer send operation text. If developer tooling needs an exception, use an authenticated interceptor and `requestBuilder.AllowNonPersistedOperation()` for that specific path.

For the full allowlist workflow, see [Trusted documents](/docs/hotchocolate/v16/operations/security-hardening/trusted-documents) and the [Nitro client registry](/docs/nitro/apis/client-registry).

# Verify your controls before deployment

Test these controls against the actual host, scheme, route, and authentication state that public users will see. Replace `https://api.example.com/graphql` with your production route.

## Verify introspection

```bash
curl -i https://api.example.com/graphql \
  -H 'content-type: application/json' \
  --data '{"query":"{ __schema { types { name } } }"}'
```

Expected public production result: HTTP 400 with error extension code `HC0046`.

## Verify integrated SDL routes

```bash
curl -i https://api.example.com/graphql?sdl
curl -i https://api.example.com/graphql/schema
curl -i https://api.example.com/graphql/schema/
curl -i https://api.example.com/graphql/schema.graphql
```

Expected public production result: HTTP 404 when integrated SDL download is disabled.

## Verify explicit schema endpoints

```bash
curl -i https://api.example.com/graphql/sdl
curl -i https://api.example.com/internal/graphql/sdl
```

Expected public production result: HTTP 404 when no explicit SDL endpoint is mapped, or HTTP 401/403 when it is mapped and protected.

## Verify Nitro

```bash
curl -i -H 'accept: text/html' https://api.example.com/graphql
curl -i https://api.example.com/graphql/ui
```

Expected public production result: Nitro is not served publicly, or the internal Nitro route returns HTTP 401/403.

If your application has test infrastructure, add integration tests for these routes. Assert the public contract you intend to support: `HC0046` for denied introspection, and 404, 401, or 403 for SDL and Nitro routes based on your policy.

# Troubleshooting schema discovery and tooling

| Symptom                                                                       | Check                                                                                                                                             | Fix                                                                                                 |
| ----------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------- |
| Nitro loads but shows no schema.                                              | Introspection is disabled, SDL download is disabled, or the Nitro user lacks authorization.                                                       | Use authenticated developer access, local development, exported SDL, or a registry workflow.        |
| Apollo, Relay, Strawberry Shake, or code generation fails against production. | The tool depends on runtime introspection.                                                                                                        | Point tooling at CI-exported SDL or the schema registry.                                            |
| `/graphql?sdl` still returns SDL in production.                               | `EnableSchemaRequests`, `EnableSchemaFileSupport`, endpoint `WithOptions`, environment conditions, and custom mappings.                           | Set both schema options to `false` for the public endpoint and retest every schema candidate route. |
| `/graphql/schema.graphql` is blocked but `/graphql/sdl` works.                | An explicit `app.MapGraphQLSchema()` endpoint is mapped.                                                                                          | Remove it, move it to an internal route, or require authorization.                                  |
| Introspection still succeeds publicly.                                        | Default security was disabled, `.DisableIntrospection(disable: false)` is configured, or an interceptor calls `AllowIntrospection()` too broadly. | Configure `.DisableIntrospection()` for production and narrow the interceptor condition.            |
| Trusted-document clients fail after ad hoc operations are disabled.           | The client sends `query` instead of `id`, operation registration is missing, or `AllowDocumentBody = false` rejects compatibility traffic.        | Publish operations before deployment and update the client transport to send operation IDs.         |

# Next steps

- [Introspection](/docs/hotchocolate/v16/securing-your-api/introspection) for GraphQL introspection fields and denial behavior.
- [Endpoints](/docs/hotchocolate/v16/server/endpoints) for endpoint middleware and `GraphQLServerOptions`.
- [Interceptors](/docs/hotchocolate/v16/server/interceptors) for per-request overrides.
- [Command Line](/docs/hotchocolate/v16/server/command-line) for schema export in CI.
- [Warmup](/docs/hotchocolate/v16/server/warmup) for startup schema export.
- [Authentication](/docs/hotchocolate/v16/securing-your-api/authentication) and [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization) for access control.
- [Trusted documents](/docs/hotchocolate/v16/operations/security-hardening/trusted-documents) for production operation allowlisting.
- [Harden endpoint exposure](/docs/hotchocolate/v16/operations/security-hardening/endpoint-exposure) for route-level production hardening.
- [Nitro schema registry](/docs/nitro/apis/schema-registry) and [Nitro client registry](/docs/nitro/apis/client-registry) for schema and client workflows.
