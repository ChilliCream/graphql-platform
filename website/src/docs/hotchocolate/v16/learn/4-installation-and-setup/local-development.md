---
title: "Local development"
description: "Set local Hot Chocolate v16 development defaults for tooling endpoints, configuration, secrets, authentication, logging, and OpenTelemetry without carrying them into production."
---

This guide helps you set up a productive local development environment for your Hot Chocolate server, once you have it running locally.

Before you continue, make sure you have:

- A project that restores and builds successfully
- GraphQL services registered in `Program.cs`
- A mapped GraphQL endpoint (usually `/graphql`)
- At least one operation that returns `data`
- A way to run the app, such as `dotnet run` or a launch profile

If your endpoint is not running yet, start with [ASP.NET Core setup](/docs/hotchocolate/v16/learn/4-installation-and-setup/aspnet-core/) or [Install and scaffold](/docs/hotchocolate/v16/get-started/install-and-scaffold/). If you run into errors on first run, see [Get started troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting/).

# Start from a working development baseline

Set up your local development environment to maximize feedback speed, but avoid changing your production contract by accident. Before you add local tools or diagnostics, record these key values for your app:

| Value | Example | Where to check |
| --- | --- | --- |
| Active environment | `Development` | Console output, launch profile, or `ASPNETCORE_ENVIRONMENT` |
| Local base URL | `https://localhost:5001` | `dotnet run` output or `Properties/launchSettings.json` |
| GraphQL endpoint | `/graphql` | `app.MapGraphQL()` or your custom route |
| Known operation | `{ __typename }` | Nitro, curl, or your client |
| Configuration source | `appsettings.Development.json` and user secrets | Project files and `dotnet user-secrets list` |

To confirm your baseline:

1. Run the app.
2. Open the mapped endpoint in your browser or client.
3. Execute a known operation.
4. Make sure the app is running in the `Development` environment.

If you hit startup, routing, package, or certificate issues, resolve those first. Don’t add fallbacks or workarounds in schema code or resolvers at this stage.

# Keep development-only behavior behind environment checks

Make local development features opt-in. Always protect each development-only feature with a clear environment check, so reviewers can see what is enabled locally.

| Local feature | Good guard | Production expectation |
| --- | --- | --- |
| Nitro hosted by the server | `app.Environment.IsDevelopment()` or an internal-only flag | Disabled unless you have an approved internal tooling route |
| Schema download | Development endpoint option or development-only `MapGraphQLSchema` | Reviewed against your schema exposure policy |
| Verbose logging | `appsettings.Development.json` | Reduced to production levels |
| Local tokens and connection strings | User secrets or environment variables | Stored in the production secret store |
| Test identity or local auth shortcut | Development-only authentication scheme | Removed or replaced by the real identity provider |
| Local telemetry exporter | Development configuration | Replaced by the production exporter or disabled |

> Warning: Never run development-only settings unconditionally. Do not put local shortcuts in resolvers, schema types, always-on middleware, committed tokens, or shared configuration.

A common pattern is to check the environment once after building the app:

```csharp
var app = builder.Build();
var isDevelopment = app.Environment.IsDevelopment();

app.MapGraphQL().WithOptions(o =>
{
    o.Tool.Enable = isDevelopment;
    o.EnableSchemaRequests = isDevelopment;
});

app.Run();
```

Start with this approach, then move production-specific decisions to your security, endpoint, and deployment configuration.

# Store local configuration values in the right place

Keep shared, repeatable defaults in configuration files. Store machine-specific or sensitive values outside of source control.

| Value | Put it here | Why |
| --- | --- | --- |
| Shared endpoint path | `appsettings.json` or code | It is part of the app contract. |
| Local Nitro enabled flag | `appsettings.Development.json` | It changes only the development surface. |
| Local schema download flag | `appsettings.Development.json` | It supports local client generation or schema review. |
| Local logging levels | `appsettings.Development.json` | Developers can tune diagnostics without code changes. |
| Local issuer or authority URL | `appsettings.Development.json` when not secret | It documents the development identity source. |
| Bearer token, signing key, API key, or password | User secrets | It must not be committed. |
| Per-run override | Environment variable or launch profile | It can differ by developer or terminal session. |
| Production secret | Production secret store | It should not depend on a developer machine. |

Use `appsettings.Development.json` for local, non-secret defaults:

```json
{
  "GraphQL": {
    "EnableNitro": true,
    "EnableSchemaRequests": true
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "HotChocolate": "Information"
    }
  }
}
```

Read these values when you configure your endpoint:

```csharp
var app = builder.Build();
var graphQL = app.Configuration.GetSection("GraphQL");
var isDevelopment = app.Environment.IsDevelopment();

app.MapGraphQL().WithOptions(o =>
{
    o.Tool.Enable = isDevelopment && graphQL.GetValue("EnableNitro", false);
    o.EnableSchemaRequests =
        isDevelopment && graphQL.GetValue("EnableSchemaRequests", false);
});

app.Run();
```

Use user secrets for local values that must never appear in source control:

```bash
dotnet user-secrets init
dotnet user-secrets set "Authentication:BearerToken" "paste-development-token-here"
dotnet user-secrets list
```

Document the required secret keys in your repository instructions, but never commit real values. Local secrets should not appear in Nitro default headers, resolver code, schema descriptions, snapshots, issue reports, or committed documentation.

For more on ASP.NET Core configuration, see Microsoft's [configuration overview](https://learn.microsoft.com/aspnet/core/fundamentals/configuration/), [safe storage of app secrets in development](https://learn.microsoft.com/aspnet/core/security/app-secrets/), and [environments overview](https://learn.microsoft.com/aspnet/core/fundamentals/environments/).

# Configure Nitro for local development

Nitro is your local workbench for composing GraphQL operations, inspecting responses, setting variables, and adding request headers. When enabled, `MapGraphQL()` serves Nitro at your GraphQL endpoint.

For most projects, enable Nitro on the same local endpoint:

```csharp
var app = builder.Build();
var isDevelopment = app.Environment.IsDevelopment();

app.MapGraphQL().WithOptions(o => o.Tool.Enable = isDevelopment);

app.Run();
```

If you need to separate the tooling route (for example, if your app serves the GraphQL transport and UI from different paths), map Nitro separately in development:

```csharp
var app = builder.Build();

app.MapGraphQL("/graphql").WithOptions(o => o.Tool.Enable = false);

if (app.Environment.IsDevelopment())
{
    app.MapNitroApp("/graphql/ui").WithOptions(o =>
    {
        o.GraphQLEndpoint = "/graphql";
        o.Title = "Local GraphQL";
        o.Document = "{ __typename }";
    });
}

app.Run();
```

> Warning: Never commit default Nitro headers containing bearer tokens, cookies, API keys, or user data. Store these values in your local browser session, user secrets, or your development identity provider.

To verify your setup:

- Open the local Nitro route and confirm the UI loads.
- Create a new document targeting your intended GraphQL endpoint.
- Run a known operation and check for returned data.
- Ensure the same route is disabled or reviewed outside `Development`.

For more on endpoint options and Nitro settings, see [Endpoints](/docs/hotchocolate/v16/server/endpoints/).

# Decide how to provide local schema access

Accessing your schema locally is helpful for generating clients, reviewing schema changes, diffing SDL in pull requests, or onboarding teammates. However, it is not required for every project.

| Local need | Option | Check |
| --- | --- | --- |
| Download SDL from the main endpoint | Enable schema requests and request `/graphql?sdl` | The endpoint returns SDL in `Development`. |
| Keep SDL on a separate local route | Map `MapGraphQLSchema("/graphql/schema")` in development | The route returns `schema.graphql`. |
| No local schema workflow | Keep schema download disabled | The team knows how to inspect the schema through approved tooling. |

To enable schema download from the main endpoint in development:

```csharp
var app = builder.Build();
var isDevelopment = app.Environment.IsDevelopment();

app.MapGraphQL().WithOptions(o => o.EnableSchemaRequests = isDevelopment);

app.Run();
```

To serve the schema on a separate route only in development:

```csharp
var app = builder.Build();

app.MapGraphQL().WithOptions(o => o.EnableSchemaRequests = false);

if (app.Environment.IsDevelopment())
{
    app.MapGraphQLSchema("/graphql/schema");
}

app.Run();
```

> Warning: Local SDL access and production schema governance are separate concerns. Always review your schema download, introspection, and API boundary policies before exposing the same behavior in production.

For more on schema endpoint APIs, see [MapGraphQLSchema](/docs/hotchocolate/v16/server/endpoints/#mapgraphqlschema). For introspection policy, see [Introspection](/docs/hotchocolate/v16/securing-your-api/introspection/).

# Choose your local authentication path intentionally

When developing, you often need to call operations that require a user. Keep your authorization model active locally, and decide how you want to provide identity for requests.

| Situation | Local path | Verification |
| --- | --- | --- |
| The API has no authenticated fields yet | Run anonymous requests | Protected fields are not expected yet. |
| Your team can issue development tokens | Use the same bearer or cookie middleware shape as production with development credentials | One allowed operation succeeds and one denied operation fails. |
| You use a development identity provider | Point ASP.NET Core authentication at the local or sandbox authority | Claims and policies match the real model. |
| You are prototyping a protected field | Use a development-only test identity, guarded by environment | The shortcut cannot run outside `Development`. |

Use the same authentication and authorization middleware shape you plan to use in production:

The JWT bearer APIs are provided by `Microsoft.AspNetCore.Authentication.JwtBearer`, and the GraphQL authorization extension comes from `HotChocolate.AspNetCore.Authorization`. For package setup and full JWT configuration, see [Authentication](/docs/hotchocolate/v16/securing-your-api/authentication/).

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services.AddAuthorization();

builder
    .AddGraphQL()
    .AddAuthorization()
    .AddTypes();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapGraphQL();

app.Run();
```

Send your local token from Nitro using an `Authorization` header:

```text
Authorization: Bearer <development-token>
```

> Warning: Never add a global bypass flag, permissive production policy, hard-coded production user, or committed token to make local authentication pass. If you need a temporary test identity, guard it with `Development` and add a review note to remove or replace it.

To verify your setup:

1. Run an operation that requires an authenticated user.
2. Run the same operation without credentials.
3. Confirm the failure is an authentication or authorization error, not a resolver error.

For full setup, see [Authentication](/docs/hotchocolate/v16/securing-your-api/authentication/) and [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization/). For API boundary decisions, see [Security and API boundaries](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/security-and-api-boundaries/).

# Tune local logs for request-level debugging

Start with logs before adding heavier diagnostics. Your logs should help you quickly identify whether a failure is due to routing, validation, authorization, resolver execution, or response formatting.

Set verbose log levels in your development configuration:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Information",
      "HotChocolate": "Debug"
    }
  }
}
```

Use this level while diagnosing a specific issue. When the logs become too noisy, reduce the level:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "HotChocolate": "Information"
    }
  }
}
```

Watch for these log signals:

- Endpoint routing and HTTP method mismatches
- Request parsing and validation errors
- Authentication and authorization failures
- Resolver exceptions
- Schema construction errors during startup
- Transport issues, such as WebSocket upgrade failures

> Warning: Never log bearer tokens, cookies, secret headers, full sensitive variables, passwords, connection strings, or personal data. Always redact sensitive values before sharing logs in issues or chat.

To verify your logging setup, create a known failing operation and confirm the log entry includes a category, level, request context, and a useful error message. You should be able to reduce the log level without changing code.

# Preview GraphQL traces locally with OpenTelemetry

Logs tell you what happened. Traces show you where time was spent across HTTP handling, GraphQL execution, resolvers, DataLoader batches, and outbound calls.

Use local tracing when:

- An operation is slower than expected
- You need resolver or DataLoader timing
- Your team wants to align local telemetry with production naming
- You need to confirm that GraphQL spans reach a collector or viewer

First, install the Hot Chocolate diagnostics package and the required OpenTelemetry packages:

```bash
dotnet add package HotChocolate.Diagnostics
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Instrumentation.AspNetCore
dotnet add package OpenTelemetry.Instrumentation.Http
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
```

Register instrumentation in your GraphQL server:

```csharp
builder
    .AddGraphQL()
    .AddTypes()
    .AddInstrumentation();
```

Register tracing with a local exporter:

```csharp
builder.Services
    .AddOpenTelemetry()
    .WithTracing(b =>
    {
        b.AddAspNetCoreInstrumentation();
        b.AddHttpClientInstrumentation();
        b.AddHotChocolateInstrumentation();
        b.AddOtlpExporter();
    });
```

Point the OTLP exporter at your local collector or viewer using configuration or environment variables. Keep this endpoint local unless your team has approved a shared development backend.

> Warning: Detailed traces can include operation documents, variable shapes, field names, timing data, and service topology. Never enable detailed scopes or export development traces to a shared system without a data exposure review.

To verify your tracing setup:

1. Start the local collector or viewer your team uses.
2. Run a named GraphQL operation.
3. Confirm that a trace contains the GraphQL operation type or name and request timing.
4. Turn off detailed scopes or the local exporter when you no longer need them.

For more on package names, options, span attributes, and scope detail, see [Instrumentation](/docs/hotchocolate/v16/server/instrumentation/). If you use Aspire for local orchestration and dashboard visibility, see [Aspire](/docs/hotchocolate/v16/learn/4-installation-and-setup/aspire/) when that page matches your host setup.

# Troubleshoot your local setup

Start with the symptom you see, and change one layer at a time.

| Symptom | Likely cause | Fix |
| --- | --- | --- |
| Development settings are ignored | The app is not running in `Development`, the wrong launch profile is active, or the key path is wrong | Print or inspect the active environment, select the expected launch profile, then change one setting and verify behavior changes. |
| Nitro does not load | Tooling is disabled, the path is wrong, HTTPS does not match the browser URL, or the endpoint does not run | Confirm the URL from `dotnet run`, the mapped path, the environment guard, and the local development certificate. |
| Nitro loads but requests fail | Nitro targets the wrong GraphQL endpoint, an auth header is missing, cookies are not included, or HTTPS redirection changes the URL | Set the correct endpoint and credentials, then run the known operation again. |
| Schema download returns 404 | Schema requests are disabled, `MapGraphQLSchema` is not mapped, or the path is wrong | Enable the local option or map the local schema route, then retrieve SDL from the expected URL. |
| Local auth always succeeds | A bypass flag or test identity runs outside the intended branch | Guard the shortcut with `Development`, then run an unauthenticated request that must fail. |
| Authorized operation fails in Nitro | The token is missing, expired, uses the wrong scheme, or lacks the expected claims | Set the `Authorization` header or cookies, inspect logs, and verify the policy with a known user. |
| Logs are noisy | Category levels are too broad | Narrow `Microsoft.AspNetCore` or `HotChocolate` levels in `appsettings.Development.json`. |
| Logs miss the useful error | Category levels are too restrictive or the failure happens before request logging | Raise the relevant category and check startup logs separately from request logs. |
| Traces do not appear | Instrumentation is not registered, the diagnostics package is missing, or the exporter cannot reach the local collector | Confirm package references, `AddInstrumentation`, `AddHotChocolateInstrumentation`, and the exporter endpoint. |
| Local tracing slows requests | Detailed scopes or field-level instrumentation create too much overhead | Reduce instrumentation detail and disable the local exporter until you need it again. |

If HTTPS fails in your browser, trust the ASP.NET Core development certificate for your platform. See Microsoft's guide: [Trust the ASP.NET Core HTTPS development certificate](https://learn.microsoft.com/aspnet/core/security/enforcing-ssl#trust-the-aspnet-core-https-development-certificate).

# Review your local setup before production

Before you treat local success as a sign you’re ready for deployment, review each development surface:

- [ ] Nitro is disabled, removed, or explicitly approved for the deployed environment.
- [ ] Schema download and introspection match your intended API boundary.
- [ ] Local user secrets do not contain production values.
- [ ] Production secrets are loaded from the production secret store.
- [ ] Development authentication shortcuts cannot run outside `Development`.
- [ ] Authorization was tested with allowed, unauthenticated, and forbidden requests.
- [ ] Verbose logging is limited to development.
- [ ] Logs redact tokens, cookies, secret headers, sensitive variables, and personal data.
- [ ] Local OpenTelemetry exporters are disabled or replaced by the production telemetry path.
- [ ] GET requests, multipart requests, CORS, CSRF, request limits, cost analysis, and [trusted documents](/docs/hotchocolate/v16/performance/trusted-documents/) are reviewed on their dedicated pages.

Next, follow the page that matches your production path:

- [Installation and setup](/docs/hotchocolate/v16/learn/4-installation-and-setup/) for host selection
- [ASP.NET Core setup](/docs/hotchocolate/v16/learn/4-installation-and-setup/aspnet-core/) for host and endpoint checks
- [Endpoints](/docs/hotchocolate/v16/server/endpoints/) for path, Nitro, schema request, HTTP, and WebSocket options
- [Securing your API](/docs/hotchocolate/v16/securing-your-api/) for authentication, authorization, introspection, request limits, and cost analysis
- [HTTP transport](/docs/hotchocolate/v16/server/http-transport/) for request envelopes, GET and POST behavior, batching, and CSRF-related preflight settings
- [Server](/docs/hotchocolate/v16/server/), [Securing your API](/docs/hotchocolate/v16/securing-your-api/), and [Performance](/docs/hotchocolate/v16/performance/) for production readiness topics
