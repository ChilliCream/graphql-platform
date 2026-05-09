---
title: "Installation and setup"
description: "Choose the right Hot Chocolate v16 setup path, install matching packages, wire ASP.NET Core, and verify your local endpoint."
---

Setting up Hot Chocolate involves three main steps: registering the necessary packages to create your GraphQL server, mapping the endpoint to expose it, and configuring your host to handle incoming requests. Use this page to select the right setup path before you start writing your schema. You'll find guidance for new and existing ASP.NET Core apps, local development, containers, proxies, Azure Functions, Aspire, and worker-style execution.

# Start with the default path unless your host requires something else

If you're starting your first Hot Chocolate server, begin with an ASP.NET Core web app that maps GraphQL to `/graphql`.

This approach provides:

- HTTP GraphQL requests managed by ASP.NET Core endpoint routing
- WebSocket support when both the ASP.NET Core WebSocket middleware and your host allow it
- Nitro in the browser at the GraphQL endpoint for local development
- Schema download via endpoint options when enabled
- Integration with your application's dependency injection
- A deployment model compatible with Kestrel, containers, IIS, reverse proxies, and most cloud hosts

Choose a specialized setup if your host changes how HTTP requests reach your application. For example, Azure Functions, .NET Aspire, proxy-heavy deployments, and worker-style processes each have unique requirements.

If you want a guided first server, see [Install and scaffold](/docs/hotchocolate/v16/get-started/install-and-scaffold/). If you already have an ASP.NET Core app, follow [Add to an existing app](/docs/hotchocolate/v16/get-started/add-to-existing-app/) for the quickest integration.

# Select the hosting shape for your application

Find the row that matches your application type. This table summarizes compatibility. Linked pages and the server reference provide details on endpoints, transports, and host-specific options.

| You have | Choose | Why | Next page | Production notes |
| --- | --- | --- | --- | --- |
| A new web API or service | ASP.NET Core with Minimal APIs | The default .NET web host shape; maps GraphQL alongside other endpoints. | [Minimal APIs](/docs/hotchocolate/v16/learn/4-installation-and-setup/minimal-apis/) or [Install and scaffold](/docs/hotchocolate/v16/get-started/install-and-scaffold/) | Review endpoint options, request limits, authentication, authorization, and logging before exposing your API. |
| An ASP.NET Core app with controllers, Razor Pages, health checks, or existing middleware | Existing ASP.NET Core app | Add GraphQL as another endpoint without replacing current routes. | [Existing ASP.NET Core app](/docs/hotchocolate/v16/learn/4-installation-and-setup/existing-aspnet-core-app/) or [Add to an existing app](/docs/hotchocolate/v16/get-started/add-to-existing-app/) | Keep middleware order, CORS, authentication, authorization, and route conventions consistent with your app. |
| A standard ASP.NET Core GraphQL service | ASP.NET Core setup | Use this for a complete server setup overview, not only the tutorial. | [ASP.NET Core setup](/docs/hotchocolate/v16/learn/4-installation-and-setup/aspnet-core/) | See [Endpoints](/docs/hotchocolate/v16/server/endpoints/) for `MapGraphQL`, Nitro, schema download, HTTP, and WebSocket options. |
| Serverless HTTP triggers | Azure Functions | The host and trigger pipeline differ from ASP.NET Core endpoint routing. | [Azure Functions](/docs/hotchocolate/v16/learn/4-installation-and-setup/azure-functions/) | Confirm which transport features your function host supports before relying on browser tooling or subscriptions. |
| A .NET Aspire solution | Aspire | Aspire helps with local orchestration, service discovery, configuration, and dashboard visibility. | [Aspire](/docs/hotchocolate/v16/learn/4-installation-and-setup/aspire/) | Treat the GraphQL server as an ASP.NET Core service within the Aspire app model. |
| A gateway or distributed graph host | Fusion | Gateways involve composition and routing beyond a single server. | [Fusion](/docs/fusion/v16) | Keep single-server setup separate from gateway composition and operations. |

At this point, you should know which host will receive GraphQL requests and which guide to follow next.

# Confirm prerequisites and install the right packages

Before you change any code, make sure your environment can restore and run the project:

- Use a supported .NET SDK for Hot Chocolate v16. The recommended path assumes .NET 8 SDK or later.
- Run commands from the directory containing the `.csproj` you want to modify.
- Ensure NuGet can access your configured package sources.
- Keep all Hot Chocolate packages in your app at the same version.
- Add feature packages only when your schema needs them, such as filtering, sorting, projections, subscriptions, authorization, Entity Framework integration, instrumentation, or Fusion components.

For a new project, install the template package once:

```bash
dotnet new install HotChocolate.Templates
```

Then create your server project:

```bash
dotnet new graphql --name LibraryServer --output LibraryServer
```

The template short name is `graphql`. The generated project includes an ASP.NET Core app, starter types, package references, and a `/graphql` endpoint.

For an existing ASP.NET Core app, add the core ASP.NET Core server package and the analyzer package used by v16 implementation-first examples:

```bash
dotnet add package HotChocolate.AspNetCore
dotnet add package HotChocolate.Types.Analyzers
```

`HotChocolate.AspNetCore` provides server integration, including `AddGraphQL`, `MapGraphQL`, and Nitro hosting. `HotChocolate.Types.Analyzers` enables source-generated type registration for examples using `[QueryType]` and `AddTypes()`.

For more on package selection and version alignment, see [Packages](/docs/hotchocolate/v16/learn/4-installation-and-setup/packages/). For .NET project and package management, refer to the Microsoft docs for [`dotnet new`](https://learn.microsoft.com/dotnet/core/tools/dotnet-new), [`dotnet add package`](https://learn.microsoft.com/dotnet/core/tools/dotnet-add-package), and [Central Package Management](https://learn.microsoft.com/nuget/consume-packages/central-package-management).

Checkpoint:

- `dotnet --info` shows the expected SDK
- `dotnet restore` can reach your package sources
- Hot Chocolate package versions are aligned across the project
- `dotnet build` succeeds before you diagnose endpoint issues

# Integrate Hot Chocolate with ASP.NET Core

A minimal ASP.NET Core setup involves three steps:

1. Register the GraphQL server.
2. Add at least one schema type.
3. Map the GraphQL endpoint.

For the v16 implementation-first approach, your code should look like this:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddGraphQL().AddTypes();

var app = builder.Build();

app.MapGraphQL();

app.Run();
```

Add a starter query type so your schema has a root field. First, create the analyzer module metadata in your project:

```csharp
// Properties/ModuleInfo.cs
using HotChocolate;

[assembly: Module("Types")]
```

`Module("Types")` names the generated type module used by `AddTypes()`.

Next, define your query type:

```csharp
using HotChocolate.Types;

namespace LibraryServer.Types;

[QueryType]
public static partial class Query
{
    public static string GetHello()
        => "Hello from Hot Chocolate.";
}
```

By default, `MapGraphQL()` exposes the server at `/graphql`. It also registers middleware for HTTP requests, WebSocket requests (when enabled), schema requests, and Nitro at the endpoint.

In an existing app, add GraphQL alongside your current services and endpoints. Do not remove controllers, Minimal API routes, health checks, static files, authentication, authorization, or CORS setup your app already uses.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.AddGraphQL().AddTypes();

var app = builder.Build();

app.MapControllers();
app.MapGraphQL();

app.Run();
```

For details on endpoint paths, Nitro options, schema download, HTTP-only endpoints, WebSockets, batching, file uploads, and persisted operations, see [Endpoints](/docs/hotchocolate/v16/server/endpoints/) and [HTTP Transport](/docs/hotchocolate/v16/server/http-transport/).

# Prepare your local development workflow

After setup, make your feedback loop repeatable:

1. Run the app with `dotnet run` or your repository's launch profile.
2. Copy the listening URL from the console output.
3. Add `/graphql` to the URL unless you mapped a different endpoint path.
4. Open the endpoint in a browser for Nitro or send a GraphQL HTTP request.
5. Run a starter query and confirm you get a `data` response.
6. Read startup logs separately from request logs to distinguish schema construction errors from execution errors.

You should see:

- The app starts without schema construction errors
- `/graphql` responds on the port printed by the running process
- Nitro or another client can reach the endpoint
- A starter query returns the expected value
- Logs show a request when you send an operation

If HTTPS fails in the browser, check your local development certificate and the URL printed by the app. See [Trust the ASP.NET Core HTTPS development certificate](https://learn.microsoft.com/aspnet/core/security/enforcing-ssl#trust-the-aspnet-core-https-development-certificate) for help.

For a guided local workflow, see [Local development](/docs/hotchocolate/v16/learn/4-installation-and-setup/local-development/) or the first tutorial chapter, [Set up the project](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/01-set-up-the-project/).

# Plan for deployment constraints early

Your deployment environment can affect how clients reach `/graphql`, even if your code is correct. Before treating local success as production readiness, consider these questions:

| Concern | Check |
| --- | --- |
| Public routing | Does the public URL forward to the same internal endpoint path you mapped with `MapGraphQL()`? |
| Process lifetime | Does the host keep the ASP.NET Core process running and restart it when needed? |
| Configuration | Are environment variables, app settings, secrets, and endpoint options set for the deployment environment? |
| Proxy headers | Does the proxy forward scheme, host, and path base information expected by ASP.NET Core? |
| Request limits | Do Kestrel, IIS, proxies, and cloud gateways allow the body sizes your GraphQL operations and file uploads require? |
| WebSockets | If you use subscriptions, do the host and proxy allow WebSocket upgrades? |
| Development tooling | Is Nitro enabled only where your team intends to expose it? |
| Schema visibility | Are schema download and introspection configured for your security posture? |
| Browser clients | Are CORS, authentication, and authorization aligned with the clients that call `/graphql`? |
| Observability | Can you see startup failures, request errors, traces, and health signals after deployment? |

Use the focused setup pages for host details. When moving from local development to a public service, see [Securing your API](/docs/hotchocolate/v16/securing-your-api/), [Request Limits](/docs/hotchocolate/v16/securing-your-api/request-limits/), [Cost Analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis/), [Introspection](/docs/hotchocolate/v16/securing-your-api/introspection/), and [Instrumentation](/docs/hotchocolate/v16/server/instrumentation/).

For more on ASP.NET Core hosting, see [ASP.NET Core fundamentals](https://learn.microsoft.com/aspnet/core/fundamentals/), [host and deploy ASP.NET Core](https://learn.microsoft.com/aspnet/core/host-and-deploy/), and [proxy and load balancer configuration](https://learn.microsoft.com/aspnet/core/host-and-deploy/proxy-load-balancer).

# Troubleshoot setup issues by layer

When setup fails, identify the affected layer before making multiple changes.

| Symptom | Likely layer | What to check |
| --- | --- | --- |
| `dotnet new graphql` is not recognized | Template install | Install `HotChocolate.Templates`, then run `dotnet new list graphql`. |
| Restore succeeds but build fails with missing Hot Chocolate extension methods | Packages | Confirm `HotChocolate.AspNetCore` is referenced and all Hot Chocolate packages use compatible versions. |
| `AddTypes()` or `[QueryType]` is not found | Analyzer and type registration | Confirm `HotChocolate.Types.Analyzers` is referenced, restored, and the project rebuilt. |
| The app fails before listening on a port | Startup or schema construction | Read the first exception in the console, then fix DI, schema type, or package errors before testing HTTP. |
| `/graphql` returns 404 | Endpoint routing | Confirm `app.MapGraphQL()` ran, the app restarted, the route path matches, and a proxy is not changing the path. |
| Nitro does not load | Tooling or endpoint options | Confirm the browser points at the mapped endpoint and the tool is enabled for the current environment. |
| Queries fail before resolvers run | Schema | Add a valid query type, rebuild, and inspect the schema in Nitro or through schema download if enabled. |
| It works locally but fails behind IIS, nginx, YARP, or a cloud gateway | Hosting | Check forwarded headers, path base, HTTPS termination, request limits, and WebSocket upgrades. |
| Subscriptions do not connect | WebSocket transport | Confirm ASP.NET Core WebSockets and every proxy between client and app support upgrades. |

For first-server setup recovery, see [Get Started troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting/). For endpoint behavior, see [Endpoints](/docs/hotchocolate/v16/server/endpoints/).

# Choose the guide that matches your next task

Once you know your host, package path, endpoint path, and local verification step, pick the next page that fits your goal. There's no need to open every setup page.

## Build

- [Install and scaffold](/docs/hotchocolate/v16/get-started/install-and-scaffold/) to generate a starter server
- [Build your first GraphQL server](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/) for the full tutorial
- [Quick start lessons](/docs/hotchocolate/v16/learn/1-quick-start/) if you already have a running server and want to make small schema edits

## Integrate

- [Add to an existing app](/docs/hotchocolate/v16/get-started/add-to-existing-app/) for the fastest way to add GraphQL to an existing ASP.NET Core app
- [Existing ASP.NET Core app](/docs/hotchocolate/v16/learn/4-installation-and-setup/existing-aspnet-core-app/) for setup decisions in an app you already own
- [Dependency Injection](/docs/hotchocolate/v16/resolvers-and-data/dependency-injection/) and [Resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers/) when your resolvers need application services

## Configure

- [Packages](/docs/hotchocolate/v16/learn/4-installation-and-setup/packages/) for package selection and version alignment
- [Endpoints](/docs/hotchocolate/v16/server/endpoints/) for `MapGraphQL`, custom paths, Nitro, schema requests, HTTP, and WebSocket middleware
- [HTTP Transport](/docs/hotchocolate/v16/server/http-transport/) for GraphQL over HTTP request and response details
- [Files](/docs/hotchocolate/v16/server/files/) for multipart uploads
- [Subscriptions](/docs/hotchocolate/v16/building-a-schema/subscriptions/) for realtime schema behavior

## Deploy

- [Azure Functions](/docs/hotchocolate/v16/learn/4-installation-and-setup/azure-functions/) for serverless hosts
- [Aspire](/docs/hotchocolate/v16/learn/4-installation-and-setup/aspire/) for orchestrated local development
- [Securing your API](/docs/hotchocolate/v16/securing-your-api/) and [Instrumentation](/docs/hotchocolate/v16/server/instrumentation/) before exposing your API publicly

## Migrate

- [Migrate from 15 to 16](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16/) if you're upgrading an existing Hot Chocolate application
- [Coming from another stack](/docs/hotchocolate/v16/learn/5-coming-from/) if your previous experience with REST, OData, Apollo Server, GraphQL.NET, or older Hot Chocolate versions affects your setup choices

# Next steps

If you're still deciding, use the hosting table above and open the one setup page that matches your host.

If you want a working server now, continue with [Install and scaffold](/docs/hotchocolate/v16/get-started/install-and-scaffold/) for a new project or [Add to an existing app](/docs/hotchocolate/v16/get-started/add-to-existing-app/) for an app you already have.
