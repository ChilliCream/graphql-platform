---
title: "Existing ASP.NET Core app"
description: "Add Hot Chocolate v16 to an ASP.NET Core app you already have, map GraphQL beside existing routes, and verify the endpoint."
---

You can integrate Hot Chocolate into your existing ASP.NET Core project without disrupting your current routes, services, middleware, controllers, Minimal APIs, Razor Pages, health checks, or static files.

This guide walks you through a safe, incremental integration:

1. Install the required ASP.NET Core server packages.
2. Register GraphQL services alongside your existing services.
3. Add a starter query field to initialize the schema.
4. Map the GraphQL endpoint next to your current endpoints.
5. Verify the `/graphql` endpoint before applying authentication, CORS, or production settings.

If you prefer a generated starter project, see [Install and scaffold](/docs/hotchocolate/v16/get-started/install-and-scaffold/). For a broader overview of setup options, visit [Installation and setup](/docs/hotchocolate/v16/learn/4-installation-and-setup/).

# Decide where GraphQL fits in your app

GraphQL becomes another endpoint in your ASP.NET Core application. It shares the same dependency injection container, configuration, logging, hosting environment, and middleware pipeline as the rest of your app.

Before you start coding, open your `Program.cs` and locate these three areas:

| Area | What to find | Where to add GraphQL |
| --- | --- | --- |
| Services | Look for code near `var builder = WebApplication.CreateBuilder(args);`, such as `AddControllers`, `AddAuthentication`, `AddCors`, or your own service registrations. | Add `builder.AddGraphQL().AddTypes();` here. |
| Middleware | Find calls after `var app = builder.Build();`, like `UseStaticFiles`, `UseCors`, `UseAuthentication`, `UseAuthorization`, or `UseWebSockets`. | Keep the existing order. Add GraphQL after any middleware that should affect GraphQL requests. |
| Endpoints | Look for endpoint mappings such as `MapControllers`, `MapGet`, `MapRazorPages`, `MapHealthChecks`, or route groups. | Add `app.MapGraphQL()` alongside these mappings. |

For your first integration, keep the default `/graphql` endpoint and leave existing routes unchanged. Only choose a different path if `/graphql` conflicts with an existing route or your app uses a convention like `/api/graphql`.

# Review the integration checklist

Before you make any changes, review these decisions to ensure a smooth integration:

| Decision | Recommended default | Change it when |
| --- | --- | --- |
| Package set | Add `HotChocolate.AspNetCore` and `HotChocolate.Types.Analyzers`. | You already use other Hot Chocolate feature packages or Central Package Management. Always keep all Hot Chocolate packages on the same version. |
| Schema entry point | Add a `[QueryType]` class and call `AddTypes()`. | Your app already registers Hot Chocolate types using another v16 pattern. |
| Endpoint path | Use `/graphql` as the starting point. | The path conflicts with an existing route, proxy, or API convention. |
| First security pass | Test anonymously in local development. | The app requires authentication from the start. Add security after you confirm the endpoint path. |
| Browser tooling | Use Nitro at the GraphQL endpoint during development. | Your environment must disable or restrict tooling outside development. |
| Existing middleware | Preserve your app’s current behavior. | GraphQL needs to share authentication, CORS, WebSockets, forwarded headers, or other policies. |

At this point, you should know which `.csproj` file to update, where to edit `Program.cs`, and which endpoint path you will test.

# Add the Hot Chocolate packages

From the directory containing your ASP.NET Core `.csproj` file, run these commands:

```bash
dotnet add package HotChocolate.AspNetCore
dotnet add package HotChocolate.Types.Analyzers
```

`HotChocolate.AspNetCore` provides the ASP.NET Core server integration, including `AddGraphQL`, `MapGraphQL`, and Nitro. `HotChocolate.Types.Analyzers` enables source-generated type registration, which is used in v16 implementation-first examples.

If your project already references any Hot Chocolate packages, make sure every Hot Chocolate package uses the same version. Version mismatches often cause restore, build, or missing extension method errors.

Build your project before editing code:

```bash
dotnet build
```

You should see:

```text
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

For more on package selection and version alignment, see [Packages](/docs/hotchocolate/v16/learn/4-installation-and-setup/packages/). For .NET package commands, refer to the Microsoft docs for [`dotnet add package`](https://learn.microsoft.com/dotnet/core/tools/dotnet-add-package) and [Central Package Management](https://learn.microsoft.com/nuget/consume-packages/central-package-management).

# Register GraphQL with your existing services

Open `Program.cs` and add GraphQL to the service registration area. Keep all your existing registrations.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.AddGraphQL().AddTypes();

var app = builder.Build();

app.MapControllers();

app.Run();
```

For Minimal API apps, add the same registration before `builder.Build()`:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddGraphQL().AddTypes();

var app = builder.Build();

app.MapGet("/", () => "Hello from the existing app.");

app.Run();
```

`AddTypes()` uses analyzer-generated type registrations for your GraphQL types. This lets your resolvers use the same scoped services as controllers or Minimal APIs. For more on this flow, see [Dependency Injection](/docs/hotchocolate/v16/resolvers-and-data/dependency-injection/) and [Resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers/).

You need to create the analyzer module metadata once for your project:

```csharp
// Properties/ModuleInfo.cs
using HotChocolate;

[assembly: Module("Types")]
```

`Module("Types")` names the generated type module that `AddTypes()` will use.

# Add a starter query field

Create a new C# file for your initial query type. Follow your project’s folder conventions; the example below uses a `Types` folder.

```csharp
// Types/Query.cs
using HotChocolate.Types;

namespace YourApp.Types;

[QueryType]
public static partial class Query
{
    public static string GetHello()
        => "Hello from Hot Chocolate.";
}
```

Replace `YourApp.Types` with your app’s actual namespace.

The `[QueryType]` attribute marks this class as contributing fields to the GraphQL query root. The `partial` keyword allows the source generator to add registration code at build time. The `GetHello()` method becomes a GraphQL field named `hello`.

Build your project again:

```bash
dotnet build
```

If you see an error that `QueryTypeAttribute` is not found, make sure you have `using HotChocolate.Types;` at the top of the file and that package restore completed. If `AddTypes()` is not found, check that `HotChocolate.Types.Analyzers` is referenced and rebuild the project.

# Map the GraphQL endpoint

After `var app = builder.Build();`, map the GraphQL endpoint alongside your existing endpoints.

```csharp
var app = builder.Build();

app.MapControllers();
app.MapGraphQL();

app.Run();
```

For Minimal APIs, add GraphQL next to your other route mappings:

```csharp
var app = builder.Build();

app.MapGet("/", () => "Hello from the existing app.");
app.MapGraphQL();

app.Run();
```

By default, `MapGraphQL()` serves the GraphQL server at `/graphql`. It handles HTTP and WebSocket GraphQL requests (when ASP.NET Core WebSockets are enabled), schema SDL requests (when enabled), and Nitro in the browser at the endpoint.

If `/graphql` conflicts with an existing route or your app uses a base API convention, specify a custom path:

```csharp
app.MapGraphQL("/api/graphql");
```

If your app still uses the legacy `UseRouting` and `UseEndpoints` pattern, keep GraphQL in the endpoint mapping block with your other routes:

```csharp
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapGraphQL();
});
```

For more on endpoint options, Nitro settings, HTTP-only endpoints, WebSockets, schema download, persisted operations, batching, and file uploads, see [Endpoints](/docs/hotchocolate/v16/server/endpoints/) and [HTTP Transport](/docs/hotchocolate/v16/server/http-transport/).

# Preserve middleware order

GraphQL runs in the same ASP.NET Core pipeline as your other endpoints. Any middleware that should affect GraphQL requests must run before you map the GraphQL endpoint.

Here’s a typical ordering for an app that already uses CORS and authentication:

```csharp
var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGraphQL();

app.Run();
```

If you want to require authorization for the entire GraphQL endpoint, apply it directly to the mapped endpoint:

```csharp
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGraphQL().RequireAuthorization();
```

Always call `UseAuthentication()` before `UseAuthorization()`, and both before any endpoint mappings that require them. Endpoint-level authorization protects the entire GraphQL endpoint, including Nitro. Use field-level authorization if you need different access rules for different operations or fields. See [Authentication](/docs/hotchocolate/v16/securing-your-api/authentication/) and [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization/).

For CORS policy placement and browser client rules, refer to the Microsoft [ASP.NET Core CORS documentation](https://learn.microsoft.com/aspnet/core/security/cors).

# Verify the endpoint before adding policies

Start your app using the command you normally use. For most projects, that’s:

```bash
dotnet run
```

Leave the terminal open and note the listening URL in the output:

```text
Now listening on: http://localhost:5095
Application started. Press Ctrl+C to shut down.
```

Your port may be different. Add your GraphQL path to the listening URL. For the default path, open:

```text
http://localhost:5095/graphql
```

Nitro should appear in your browser during local development. Your existing routes should still work at their original URLs.

In Nitro, run this query:

```graphql
query SayHello {
  hello
}
```

You should get this response:

```json
{
  "data": {
    "hello": "Hello from Hot Chocolate."
  }
}
```

If Nitro is disabled or you want to test with a direct HTTP request, send a POST request to the endpoint:

```bash
curl -X POST http://localhost:5095/graphql \
  -H "Content-Type: application/json" \
  -d '{"query":"query SayHello { hello }"}'
```

Expected response:

```json
{"data":{"hello":"Hello from Hot Chocolate."}}
```

The GraphQL response format follows the [GraphQL specification](https://spec.graphql.org/). The [GraphQL over HTTP specification](https://graphql.github.io/graphql-over-http/) describes how GraphQL operations are sent over HTTP.

Checkpoint:

- The app starts without schema construction errors.
- `/graphql` or your custom path responds on the current listening port.
- A valid query returns a `data` object.
- Existing controllers, Minimal API routes, health checks, and static assets still respond.

# Decide how GraphQL should coexist with your existing endpoints

Keep GraphQL in the same ASP.NET Core app when it can share deployment, scaling, security, and ownership with your existing endpoints.

| Shape | Use it when | Setup note |
| --- | --- | --- |
| Same app, default `/graphql` path | You want GraphQL alongside existing REST, MVC, Razor Pages, or Minimal API routes. | This page covers that scenario. |
| Same app, custom path such as `/api/graphql` | Your public API uses a base path or `/graphql` conflicts with another route. | Use `app.MapGraphQL("/api/graphql")` and test the exact URL your clients will use. |
| Separate host or service | GraphQL needs different scaling, ownership, deployment cadence, or security boundaries. | Treat it as a separate ASP.NET Core setup and share application code through services or packages as needed. |

When writing resolvers, call your existing application services through dependency injection instead of duplicating business logic from controllers or Minimal APIs. Keep your existing REST endpoints for contracts like webhooks, downloads, health checks, or public routes that clients already depend on.

# Configure development tooling deliberately

Nitro is a valuable tool for local development and verification. In v16, `MapGraphQL()` serves Nitro at the GraphQL endpoint when enabled.

To enable Nitro only in development, configure endpoint options like this:

```csharp
app.MapGraphQL().WithOptions(o => o.Tool.Enable = app.Environment.IsDevelopment());
```

If you want to host Nitro on a separate path, connect it to your GraphQL endpoint:

```csharp
app.MapGraphQLHttp("/graphql");
app.MapNitroApp("/graphql/ui");
```

Schema SDL download can help with client tooling and CI, but you may want to disable or protect it outside development:

```csharp
app.MapGraphQL().WithOptions(o => o.EnableSchemaRequests = app.Environment.IsDevelopment());
```

For more on local developer workflow, see [Local development](/docs/hotchocolate/v16/learn/4-installation-and-setup/local-development/). For a full reference of endpoint options, see [Endpoints](/docs/hotchocolate/v16/server/endpoints/).

# Apply production settings before exposing your endpoint

Do not treat your first local GraphQL endpoint as production-ready. Review these settings before exposing it in an existing application:

| Concern | What to decide | Where to continue |
| --- | --- | --- |
| Authentication and authorization | Does the endpoint require a policy? Do fields need finer-grained authorization? | [Authentication](/docs/hotchocolate/v16/securing-your-api/authentication/) and [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization/) |
| Browser clients | Which origins, credentials, headers, and methods can call GraphQL? | [ASP.NET Core CORS documentation](https://learn.microsoft.com/aspnet/core/security/cors) |
| Request size and complexity | What are the limits for body size, depth, cost, batching, and uploads? | [Request Limits](/docs/hotchocolate/v16/securing-your-api/request-limits/), [Cost Analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis/), and [Files](/docs/hotchocolate/v16/server/files/) |
| Schema visibility | Should introspection and schema SDL download be available? | [Introspection](/docs/hotchocolate/v16/securing-your-api/introspection/) and [Endpoints](/docs/hotchocolate/v16/server/endpoints/) |
| Known operations | Should clients be required to send trusted documents? | [Trusted Documents](/docs/hotchocolate/v16/performance/trusted-documents/) |
| Proxies and base paths | Does the public URL differ from the internal endpoint path? | [Endpoints](/docs/hotchocolate/v16/server/endpoints/) and Microsoft [proxy and load balancer configuration](https://learn.microsoft.com/aspnet/core/host-and-deploy/proxy-load-balancer) |
| Observability | How should GraphQL requests appear in logs, metrics, and traces? | [Instrumentation](/docs/hotchocolate/v16/server/instrumentation/) |

# Troubleshoot your first integration

If you run into issues, use this table to diagnose and resolve common problems:

| Symptom | What to check |
| --- | --- |
| `dotnet add package` or restore fails | Check NuGet source access, SDK selection, network policy, and that all Hot Chocolate packages use the same version. |
| `AddGraphQL` is not found | Make sure `HotChocolate.AspNetCore` is referenced and restored. |
| `AddTypes` is not found | Ensure `HotChocolate.Types.Analyzers` is referenced, restore is complete, and the project is rebuilt after adding the package. |
| `QueryTypeAttribute` is not found | Add `using HotChocolate.Types;` and confirm packages are restored. |
| The app fails before listening on a port | Read the first startup exception. Fix schema construction, DI registration, or package errors before testing HTTP. |
| `/graphql` returns 404 | Confirm `app.MapGraphQL()` ran, the app restarted, the path matches the URL, and no proxy or route group changed the path. |
| Browser opens a non-GraphQL page at `/graphql` | Check for route conflicts with controllers, static files, fallback routes, or proxy rewrites. Use a dedicated GraphQL path if needed. |
| Nitro loads but requests return 401 or 403 | The endpoint likely requires credentials. Configure the development auth flow or verify with an authenticated client request. |
| CORS requests fail from a frontend | Make sure the app’s CORS policy includes the GraphQL endpoint, origin, headers, credentials mode, and methods your client uses. |
| HTTP queries work but subscription clients cannot connect | Confirm ASP.NET Core WebSockets are enabled with `app.UseWebSockets()` if your app uses WebSocket subscriptions, the client uses the correct GraphQL path, the proxy supports WebSockets, and the auth handshake matches your endpoint policy. |
| The query returns `Cannot query field "hello"` | Make sure the resolver method is named `GetHello`, the query selects `hello`, the class is `partial`, and the app is rebuilt after adding the file. |
| An existing route stopped working | Restore the previous route mapping and add `app.MapGraphQL()` without replacing existing `MapGet`, `MapControllers`, `MapRazorPages`, health check, or static file configuration. |

For more recovery steps, see [Get Started troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting/). For endpoint behavior, see [Endpoints](/docs/hotchocolate/v16/server/endpoints/).

# Next steps

Choose the page that matches your next goal:

- **Set up a baseline ASP.NET Core project:** See [ASP.NET Core setup](/docs/hotchocolate/v16/learn/4-installation-and-setup/aspnet-core/).
- **Work with route groups or Minimal APIs:** See [Minimal APIs](/docs/hotchocolate/v16/learn/4-installation-and-setup/minimal-apis/).
- **Configure local tooling and diagnostics:** See [Local development](/docs/hotchocolate/v16/learn/4-installation-and-setup/local-development/).
- **Secure your endpoint:** See [Securing your API](/docs/hotchocolate/v16/securing-your-api/).
- **Explore endpoint options:** See [Endpoints](/docs/hotchocolate/v16/server/endpoints/).
- **Model real data:** Continue with [Building a schema](/docs/hotchocolate/v16/building-a-schema/), [Resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers/), and [Dependency Injection](/docs/hotchocolate/v16/resolvers-and-data/dependency-injection/).
