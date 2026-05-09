---
title: "Control introspection and schema download"
---

This page helps you control schema discovery in a Hot Chocolate v16 ASP.NET Core server. You will choose a production policy for GraphQL introspection, SDL download routes, and hosted Nitro tooling, then verify that public traffic sees only the surfaces you intend to expose.

Use this page for Hot Chocolate source-schema services. Fusion gateway behavior is separate and is not covered here.

# Prerequisites

You need:

- A Hot Chocolate v16 ASP.NET Core server configured with `builder.AddGraphQL()` or `builder.Services.AddGraphQLServer()`.
- A mapped GraphQL endpoint, often `app.MapGraphQL()` during development or split endpoints such as `app.MapGraphQLHttp()` in production.
- ASP.NET Core authentication and authorization if you want to protect developer-only schema access.
- A reliable environment check through `builder.Environment` or `app.Environment`.
- A way to test from the same network path that production users use, not only from `localhost`.

A minimal server looks like this:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQL()
    .AddQueryType<Query>();

var app = builder.Build();

app.MapGraphQL();

app.Run();
```

Server-level options belong on the GraphQL builder. Endpoint-level options belong on the mapped endpoint with `WithOptions`. Per-request exceptions belong in an HTTP request interceptor.

# Choose a production schema visibility policy

Start with an explicit policy, then adjust it for your product and developer workflow.

| Traffic                   | Introspection                 | SDL/schema download            | Nitro                       | Recommended access pattern                              |
| ------------------------- | ----------------------------- | ------------------------------ | --------------------------- | ------------------------------------------------------- |
| Local development         | Allowed                       | Allowed if tooling needs it    | Enabled                     | Developer machine only                                  |
| Public production clients | Disabled                      | Disabled                       | Disabled                    | Normal operation execution only                         |
| Internal developers       | Allowed by policy             | Protected route or CI artifact | Protected or separate route | Authenticated users, roles, claims, or internal ingress |
| CI/CD and registries      | Not needed against production | Export from build output       | Not needed                  | `schema export` command or protected artifact           |

The following baseline keeps schema discovery open in development and closes it for production public traffic:

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

Expected production behavior:

- `/graphql` still executes allowed operations.
- Introspection queries return HTTP 400 with GraphQL error code `HC0046`.
- Integrated SDL routes return `404 Not Found`.
- Browser requests to the public GraphQL endpoint do not load Nitro.

`AddGraphQLServer(..., disableDefaultSecurity: false)` already adds the default security policy, which disables executable introspection outside development, adds cost analysis, and validates maximum field-cycle depth. Keep your policy explicit when you use `AddGraphQL()`, when you pass `disableDefaultSecurity: true`, or when you want reviewers to see the production decision in one place.

# Know which surface you are controlling

Disabling introspection does not close every way to discover a schema. Treat these surfaces as separate controls.

| Surface                 | What it is                                                | Common route or query                                                                | Control                                                                            | Production recommendation                                  |
| ----------------------- | --------------------------------------------------------- | ------------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------- | ---------------------------------------------------------- |
| GraphQL introspection   | Executable GraphQL fields such as `__schema` and `__type` | `{ __schema { types { name } } }`                                                    | `.DisableIntrospection(...)` and per-request `requestBuilder.AllowIntrospection()` | Disable for public production traffic.                     |
| Runtime `__typename`    | A meta field clients often use for unions and interfaces  | `{ node { __typename } }`                                                            | Normal GraphQL execution and authorization                                         | Usually keep available. It is not the main discovery risk. |
| Integrated SDL download | Schema file support on `MapGraphQL()`                     | `GET /graphql?sdl`, `/graphql/schema`, `/graphql/schema/`, `/graphql/schema.graphql` | `EnableSchemaRequests` and `EnableSchemaFileSupport`                               | Disable publicly or protect through endpoint design.       |
| Explicit SDL endpoint   | A separate endpoint mapped by your app                    | `app.MapGraphQLSchema()` defaults to `/graphql/sdl`                                  | Map or remove the endpoint, plus `.RequireAuthorization(...)`                      | Do not map publicly, or require developer authorization.   |
| Hosted Nitro            | Browser IDE and operation tooling                         | Browser `GET /graphql` or `MapNitroApp("/graphql/ui")`                               | `Tool.Enable` or Nitro endpoint options                                            | Disable publicly, or host on a protected developer route.  |

This distinction matters during security reviews. A finding for `/graphql?sdl` is not fixed by changing introspection validation. A Nitro route can load even when introspection is disabled, but it may not be able to show a schema.

# Disable public introspection and allow developers by policy

Use `DisableIntrospection()` to block executable introspection. Add a narrow interceptor when authenticated developers need temporary access.

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

Expected result:

- Anonymous public introspection receives `HC0046`.
- Requests from users who satisfy `GraphQLDevelopers` can introspect.

Avoid bare shared headers as a production bypass. A header can be useful in a local test, but production exceptions should come from authentication, authorization policies, claims, roles, mTLS, an API key validator, or a protected internal network path.

You can customize the denial message in the same interceptor:

```csharp
if (!result.Succeeded)
{
    requestBuilder.SetIntrospectionNotAllowedMessage(
        "Introspection requires the GraphQLDevelopers policy.");
}
```

A denied request returns a GraphQL error similar to this:

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

SDL download is independent from introspection validation. Configure schema file options directly.

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

When you configure a specific endpoint, check endpoint overrides too:

```csharp
app.MapGraphQL().WithOptions(options =>
{
    options.EnableSchemaRequests = false;
    options.EnableSchemaFileSupport = false;
});
```

`EnableSchemaRequests = false` prevents the schema middleware from handling schema download candidates. `EnableSchemaFileSupport = false` returns `404 Not Found` when a candidate reaches the middleware. Set both in production so the policy remains clear even if routing changes later.

Integrated schema candidates include:

```text
GET /graphql?sdl
GET /graphql/schema
GET /graphql/schema/
GET /graphql/schema.graphql
GET /graphql?sdl&types=Query
```

The `types=` query can export selected types, so block the whole schema download surface instead of blocking only the full schema file.

If your team needs an SDL endpoint, map it explicitly and protect it:

```csharp
app.MapGraphQLSchema("/internal/graphql/sdl")
    .RequireAuthorization("GraphQLDevelopers");
```

Expected result:

- `GET /graphql?sdl`, `/graphql/schema`, `/graphql/schema/`, and `/graphql/schema.graphql` return `404 Not Found` when integrated schema download is disabled.
- `/graphql/sdl` exists only if you mapped `MapGraphQLSchema()`.
- A protected explicit route returns `401 Unauthorized` or `403 Forbidden` for callers outside the developer policy.

# Expose or disable Nitro intentionally

Nitro is developer tooling. It can be served by the integrated `MapGraphQL()` endpoint for browser requests, or from a separate `MapNitroApp()` route. Nitro is not an authorization boundary and does not replace endpoint or field authorization.

Disable hosted Nitro on the integrated endpoint in production:

```csharp
app.MapGraphQL().WithOptions(options =>
{
    options.Tool.Enable = app.Environment.IsDevelopment();
});
```

For production systems where developers need a browser IDE, split execution and tooling routes:

```csharp
app.MapGraphQLHttp("/graphql")
    .RequireAuthorization("GraphQLAccess");

app.MapNitroApp("/graphql/ui", "../graphql")
    .RequireAuthorization("GraphQLDevelopers");
```

Nitro may load but fail to show a schema when introspection or SDL downloads are disabled, or when the user lacks authorization. That is expected when schema discovery is closed. Use authenticated developer access, a local environment, an exported SDL file, or a registry workflow instead of opening public discovery for the UI.

# Replace production discovery with schema export and registry workflows

Production clients and tools do not need to introspect the live public endpoint. Export the schema during CI and publish the artifact where your tooling can read it.

Install `HotChocolate.AspNetCore.CommandLine`, then return the command exit code from `Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQL()
    .AddQueryType<Query>();

var app = builder.Build();

app.MapGraphQL();

return await app.RunWithGraphQLCommandsAsync(args);
```

Export the schema in CI:

```bash
dotnet run -- schema export --output schema.graphql
```

Expected output:

```text
schema.graphql
```

The command fails with a non-zero exit code if schema construction or export fails, which lets CI stop the deployment. Use the exported SDL for client code generation, schema review, and publishing to the [Nitro schema registry](/docs/nitro/apis/schema-registry).

You can also export during startup when the deployment environment can write and protect the file:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .ExportSchemaOnStartup("./schema.graphql");
```

Do not write exported SDL into a public web root. Treat schema artifacts for private APIs as internal documentation.

# Pair closed discovery with trusted documents

Closed schema discovery reduces what public callers can learn at runtime. It does not stop arbitrary executable operations by itself. For first-party clients, pair it with trusted documents so production clients send operation IDs instead of ad hoc query text.

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

`OnlyAllowPersistedDocuments = true` rejects operations that are not in persisted operation storage. `AllowDocumentBody = false` enables strict transport behavior for clients that no longer send operation text. If developer tooling needs an exception, use an authenticated interceptor and `requestBuilder.AllowNonPersistedOperation()` for that narrow path.

For the full allowlist workflow, see [Trusted documents](/docs/hotchocolate/v16/operations/security-hardening/trusted-documents) and the [Nitro client registry](/docs/nitro/apis/client-registry).

# Verify the controls before deployment

Run these checks against the real host, scheme, route, and authentication state that public users have. Replace `https://api.example.com/graphql` with your production route.

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

Add integration tests around the same routes if your application has test infrastructure. Assert the public contract you intend to support: `HC0046` for denied introspection and 404, 401, or 403 for SDL and Nitro routes based on your policy.

# Troubleshoot schema discovery and tooling failures

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
