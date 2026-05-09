---
title: "Azure Functions"
description: "Host a Hot Chocolate v16 GraphQL HTTP endpoint in Azure Functions, choose the worker model, register a schema, verify the route, and understand serverless limits."
---

You can run Hot Chocolate in Azure Functions when you need an HTTP-triggered serverless endpoint for your API. However, Azure Functions is not the default or recommended host for most GraphQL APIs. ASP.NET Core remains the primary host for Hot Chocolate because it provides the full endpoint pipeline, middleware ordering, WebSockets, richer transport options, and predictable production behavior. Unless your deployment requirements specifically call for Azure Functions, start with the [ASP.NET Core setup](/docs/hotchocolate/v16/learn/4-installation-and-setup/aspnet-core/).

## When to Use Azure Functions for GraphQL

Choose Azure Functions only when the benefits of serverless hosting outweigh the features of ASP.NET Core. Azure Functions can handle GraphQL requests through an HTTP trigger and delegate them to Hot Chocolate. This approach works best for workloads that need serverless scaling or consumption-based hosting, rather than the full capabilities of ASP.NET Core.

| You need | Choose | Why | Read next |
| --- | --- | --- | --- |
| A normal public GraphQL API with full HTTP endpoint control | ASP.NET Core | It is the primary Hot Chocolate server host and supports the standard endpoint pipeline. | [ASP.NET Core setup](/docs/hotchocolate/v16/learn/4-installation-and-setup/aspnet-core/) |
| GraphQL beside controllers, Minimal APIs, health checks, or custom middleware | ASP.NET Core | The app can share ASP.NET Core middleware, authentication, CORS, routing, and observability. | [Existing ASP.NET Core app](/docs/hotchocolate/v16/learn/4-installation-and-setup/existing-aspnet-core-app/) |
| A small HTTP-triggered endpoint with variable traffic or consumption-based hosting requirements | Azure Functions | The Functions host owns the HTTP trigger and Hot Chocolate executes the GraphQL schema. | Continue on this page. |
| Low and predictable latency for the first request after scale-out | ASP.NET Core or a measured Functions plan | Cold starts and schema initialization can affect the first request in serverless environments. | [Performance](/docs/hotchocolate/v16/guides/performance/) |
| Long-lived connections such as WebSocket subscriptions | ASP.NET Core | Functions is a specialized HTTP-trigger host and should not be assumed to have ASP.NET Core transport parity. | [Subscriptions](/docs/hotchocolate/v16/building-a-schema/subscriptions/) |

Before you install any packages, decide which scenario fits your needs:

- Azure Functions is a good fit for my HTTP-triggered query and mutation workload.
- I should use the ASP.NET Core setup guide instead.

# Confirm Supported Azure Functions Model and Prerequisites

Hot Chocolate v16 supports both Azure Functions worker models. Choose the right one for your project:

| Worker model | Hot Chocolate package | Setup location | Request type |
| --- | --- | --- | --- |
| Isolated worker | `HotChocolate.AzureFunctions.IsolatedProcess` | `Program.cs` with `IHostBuilder` | `HttpRequestData` |
| In-process | `HotChocolate.AzureFunctions` | `FunctionsStartup` with `IFunctionsHostBuilder` | `HttpRequest` |

For new projects, use the isolated worker model. Microsoft recommends this model for current .NET Azure Functions development. Only use the in-process model if you have an existing in-process Function app or specific requirements. For more details, see Microsoft's [isolated worker guide](https://learn.microsoft.com/azure/azure-functions/dotnet-isolated-process-guide) and [in-process guide](https://learn.microsoft.com/azure/azure-functions/functions-dotnet-class-library).

Before adding GraphQL, make sure you:

- Target a .NET version supported by both your Azure Functions runtime and your Hot Chocolate v16 package.
- Use Azure Functions runtime 4.
- Have [Azure Functions Core Tools](https://learn.microsoft.com/azure/azure-functions/functions-run-local) installed for local development.
- Can build the project before adding GraphQL.
- Use the same concrete v16 version for all `HotChocolate.*` package references.
- Know whether your app uses the isolated worker or in-process model.

From your project directory (where the `.csproj` file is located), run:

```bash
dotnet --info
func --version
dotnet build
```

You should see:

```text
Build succeeded.
```

For more on package version alignment, see [Packages](/docs/hotchocolate/v16/learn/4-installation-and-setup/packages/).

# Create a GraphQL-Ready Azure Functions Project

Begin with an Azure Functions project that can handle HTTP requests. Hot Chocolate integrates directly with the HTTP trigger; you do not use `MapGraphQL()` as you would in ASP.NET Core. The HTTP trigger acts as the entry point, and your function passes requests to Hot Chocolate.

For a new isolated worker project, run:

```bash
func init LibraryFunctions --worker-runtime dotnet-isolated
cd LibraryFunctions
func new --template "HTTP trigger" --name GraphQLFunction
dotnet add package HotChocolate.AzureFunctions.IsolatedProcess
dotnet add package HotChocolate.Types.Analyzers
dotnet build
```

For an in-process Function app, install the in-process package:

```bash
dotnet add package HotChocolate.AzureFunctions
dotnet add package HotChocolate.Types.Analyzers
dotnet build
```

Always keep all Hot Chocolate packages on the same v16 version. If your project uses central package management, add the Azure Functions package with the same version as the others.

# Add Analyzer Module Metadata

Before you register types with the analyzer-generated `AddTypes()` method, create a `Properties/ModuleInfo.cs` file:

```csharp
// Properties/ModuleInfo.cs
using HotChocolate;

[assembly: Module("Types")]
```

The `Module("Types")` attribute names the module for generated type registration. The `AddTypes()` method uses this module when registering the schema. If you skip this file, the no-argument `AddTypes()` overload may compile, but generated types will be missing from the loaded module.

Checkpoint:
- `Properties/ModuleInfo.cs` exists.
- The file contains `[assembly: Module("Types")]`.

By default, Azure Functions uses the `/api` route prefix. If your HTTP trigger uses `Route = "graphql"`, your local URL will be:

```text
http://localhost:7071/api/graphql
```

This matches the Hot Chocolate Azure Functions default API route.

Set the trigger authorization level intentionally:
- Use `AuthorizationLevel.Anonymous` for local development and samples.
- Use `AuthorizationLevel.Function`, platform authentication, or another protection method for production.

For more on HTTP trigger routing and authorization, see [Azure Functions HTTP triggers](https://learn.microsoft.com/azure/azure-functions/functions-bindings-http-webhook-trigger).

Checkpoint: The local Functions host lists an HTTP trigger before you connect it to GraphQL.

# Register Hot Chocolate in the Functions Host

Register GraphQL services in the Functions host setup, not inside individual resolvers. The registration pattern is similar to ASP.NET Core: add Hot Chocolate to dependency injection, then register your schema types.

## Isolated Worker Registration

For isolated worker apps, register GraphQL in `Program.cs`:

```csharp
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .AddGraphQLFunction(graphQL => graphQL.AddTypes())
    .Build();

host.Run();
```

This example uses `IHostBuilder` on purpose. Some newer Azure Functions templates use a different builder, but Hot Chocolate v16 for Azure Functions is designed for this host builder approach.

Add a starter query type:

```csharp
// Types/Query.cs
using HotChocolate.Types;

namespace LibraryFunctions.Types;

[QueryType]
public static partial class Query
{
    public static string GetHello()
        => "Hello from Azure Functions.";
}
```

`AddGraphQLFunction(...)` registers the Hot Chocolate server and the Azure Functions request executor. `AddTypes()` registers all analyzer-generated GraphQL types, including those marked with `[QueryType]`.

## In-Process Registration

For in-process Function apps, register GraphQL in a `FunctionsStartup` class:

```csharp
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(LibraryFunctions.Startup))]

namespace LibraryFunctions;

public sealed class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        builder
            .AddGraphQLFunction()
            .AddTypes();
    }
}
```

Use the same `Types/Query.cs` starter query as in the isolated worker example.

Resolvers can request services from the Functions DI container. Register those services in the same place you register GraphQL, then inject them into resolver methods. For more on resolver service injection, see [Dependency Injection](/docs/hotchocolate/v16/resolvers-and-data/dependency-injection/) and [Resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers/).

Checkpoint:
- `AddGraphQLFunction` compiles.
- `AddTypes()` compiles.
- `Properties/ModuleInfo.cs` still exists and contains `[assembly: Module("Types")]`.
- The starter `[QueryType]` compiles.
- `dotnet build` succeeds.

# Handle GraphQL Requests with the HTTP Trigger

The HTTP trigger receives incoming requests. Your function method should pass these requests to `IGraphQLRequestExecutor`.

## Isolated Worker HTTP Trigger

```csharp
using HotChocolate.AzureFunctions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace LibraryFunctions;

public sealed class GraphQLFunction
{
    private readonly IGraphQLRequestExecutor _executor;

    public GraphQLFunction(IGraphQLRequestExecutor executor)
    {
        _executor = executor;
    }

    [Function("GraphQL")]
    public Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "graphql")]
        HttpRequestData request)
        => _executor.ExecuteAsync(request);
}
```

## In-Process HTTP Trigger

```csharp
using HotChocolate.AzureFunctions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace LibraryFunctions;

public static class GraphQLFunction
{
    [FunctionName("GraphQL")]
    public static Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "graphql")]
        HttpRequest request,
        [GraphQL] IGraphQLRequestExecutor executor)
        => executor.ExecuteAsync(request);
}
```

The route consists of two parts:
- Azure Functions applies the host route prefix, usually `/api`.
- The HTTP trigger route is `graphql`.

Combined, the endpoint is `/api/graphql`. If you change the Functions route prefix or the trigger route, pass the public GraphQL path to `AddGraphQLFunction` so Nitro and schema requests use the correct route:

```csharp
.AddGraphQLFunction(
    graphQL => graphQL.AddTypes(),
    apiRoute: "/graphql")
```

Use the overload that matches your worker model. The value should match the URL path clients use after any Functions route prefix is applied.

Checkpoint: A POST request to the route reaches Hot Chocolate, not a Functions 404.

# Verify the Endpoint Locally

Start the Functions host:

```bash
func start
```

Look for the HTTP trigger URL in the host output. With the default route prefix and `Route = "graphql"`, the URL is:

```text
http://localhost:7071/api/graphql
```

Test the endpoint with a GraphQL-over-HTTP POST request:

```bash
curl -X POST http://localhost:7071/api/graphql \
  -H "Content-Type: application/json" \
  -d '{"query":"query SayHello { hello }"}'
```

You should receive:

```json
{
  "data": {
    "hello": "Hello from Azure Functions."
  }
}
```

This confirms the full path works: Azure Functions received the HTTP request, the trigger called the Hot Chocolate executor, Hot Chocolate built the schema, the resolver ran, and the endpoint returned a GraphQL response.

You can also open the local route in a browser:

```text
http://localhost:7071/api/graphql
```

When tooling is enabled, Hot Chocolate v16 serves Nitro from the Azure Functions GraphQL route. Use the HTTP POST test above as your primary smoke test, especially in environments where browser tooling is disabled.

If schema requests are enabled, you can download the schema SDL by appending `?sdl`:

```text
http://localhost:7071/api/graphql?sdl
```

You should see SDL like this:

```graphql
type Query {
  hello: String!
}
```

For more on request semantics, see [HTTP Transport](/docs/hotchocolate/v16/server/http-transport/) and [Endpoints](/docs/hotchocolate/v16/server/endpoints/).

# Understand Endpoint Behavior and Transport Limits

Azure Functions hosting is a specialized HTTP-trigger integration. Not every ASP.NET Core endpoint feature works the same way in Functions.

| Feature | Functions support in v16 | Notes | Read next |
| --- | --- | --- | --- |
| Default route | Supported | The default is `/api/graphql`, combining the Functions route prefix `/api` and `Route = "graphql"`. | [HTTP triggers](https://learn.microsoft.com/azure/azure-functions/functions-bindings-http-webhook-trigger) |
| HTTP POST GraphQL requests | Supported | Use `Content-Type: application/json` with a GraphQL-over-HTTP body. | [HTTP Transport](/docs/hotchocolate/v16/server/http-transport/) |
| HTTP GET GraphQL requests | Supported when enabled by server options | GET is for query operations. Configure options carefully for production. | [Endpoints](/docs/hotchocolate/v16/server/endpoints/) |
| Nitro | Supported through the GraphQL route when tooling is enabled | Nitro is served in embedded mode. Disable or restrict tooling as needed. | [Nitro](/products/nitro) |
| Schema SDL download | Supported when schema requests are enabled | Use `?sdl` on the GraphQL route. Disable if your security policy requires. | [Endpoints](/docs/hotchocolate/v16/server/endpoints/) |
| Multipart requests | Supported by the Hot Chocolate pipeline when enabled | Also check Azure Functions and hosting plan request limits. | [Uploading files](/docs/hotchocolate/v16/server/files/) |
| Persisted operations | Conditional | Configure and verify as you would for the server. | [Trusted documents](/docs/hotchocolate/v16/performance/trusted-documents/) |
| WebSocket subscriptions | Not supported as the default path | Use ASP.NET Core for long-lived WebSocket GraphQL connections. | [Subscriptions](/docs/hotchocolate/v16/building-a-schema/subscriptions/) |
| ASP.NET Core middleware ordering | Not available | The Functions host owns the HTTP trigger. Use Functions config, platform features, and Hot Chocolate options instead of `app.Use...` middleware. | [ASP.NET Core setup](/docs/hotchocolate/v16/learn/4-installation-and-setup/aspnet-core/) |

By default, the maximum GraphQL request size is 20 MB (20,000,000 bytes). Azure Functions and any gateway in front of it may have their own request size, timeout, and body parsing limits. Measure the entire deployed path before promising client behavior.

To configure Hot Chocolate server options for Functions, use `ModifyFunctionOptions`:

```csharp
.AddGraphQLFunction(graphQL =>
{
    graphQL.AddTypes();
    graphQL.ModifyFunctionOptions(options =>
    {
        options.EnableSchemaRequests = false;
        options.Tool.Enable = false;
    });
});
```

Checkpoint: List the transport features your clients need, then verify each one against the Functions endpoint. If you require full ASP.NET Core endpoint composition, switch hosts before your API grows around the wrong constraint.

# Plan for Cold Start, Timeouts, and Production Behavior

Getting the endpoint working locally is only the first step. Before you go to production, plan and measure the following:

- **Cold start latency:** The first request after scale-out or idle time can include Functions startup, dependency loading, schema initialization, and data-source warmup.
- **Hosting plan:** Consumption, Premium, Dedicated, and Flex Consumption plans all have different scaling, timeout, networking, and warmup characteristics. See [Azure Functions scale and hosting](https://learn.microsoft.com/azure/azure-functions/functions-scale).
- **Timeout budget:** Make sure client operations, resolver calls, downstream services, and the Functions plan all fit within your timeout requirements.
- **Request size:** Check Hot Chocolate options, Azure Functions limits, reverse proxies, API gateways, and client upload behavior together.
- **Observability:** Ensure startup logs, request logs, GraphQL errors, and dependency failures appear in your team's telemetry. For Hot Chocolate instrumentation, see [Instrumentation](/docs/hotchocolate/v16/server/instrumentation/).
- **Security:** Deliberately choose function authorization levels, platform authentication, CORS policy, Nitro exposure, schema download policy, introspection policy, and error-detail behavior. Start with [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization/) and [Introspection](/docs/hotchocolate/v16/securing-your-api/introspection/).

A practical production checklist:

1. Measure cold start and warm request latency in an Azure environment that matches production.
2. Verify the deployed route and authorization behavior.
3. Confirm browser clients pass CORS and HTTPS requirements.
4. Confirm required GraphQL transports work through the deployed URL.
5. Disable or restrict Nitro and SDL download if your environment requires it.
6. Confirm logs and telemetry show startup, schema, resolver, and request failures.

Switch to ASP.NET Core hosting if you need richer middleware control, long-lived connections, predictable low latency, endpoint-level authorization composition, or transport features that Azure Functions cannot provide.

# Deploy and Validate the Azure Function

Deploy your Azure Function using your team's standard workflow, such as:

- [Azure Developer CLI](https://learn.microsoft.com/azure/developer/azure-developer-cli/)
- [Azure Functions Core Tools](https://learn.microsoft.com/azure/azure-functions/functions-run-local)
- Visual Studio
- GitHub Actions
- Azure Pipelines

For GraphQL, keep your deployment checklist focused:
- The deployed app uses the same Hot Chocolate v16 package versions as your local environment.
- Environment-specific app settings are configured in Azure.
- The public URL, route prefix, trigger route, and function authorization level are intentional.
- Browser clients have the required CORS configuration.
- Nitro and SDL download behavior match your environment policy.
- Application logs show the first deployed request.

After deployment, run the same smoke test against the public URL:

```bash
curl -X POST https://<your-function-app>.azurewebsites.net/api/graphql \
  -H "Content-Type: application/json" \
  -d '{"query":"query SayHello { hello }"}'
```

If your trigger uses `AuthorizationLevel.Function`, include the function key as required by your team's policy, either as a `code` query string parameter or an `x-functions-key` header. See the [HTTP trigger authorization section](https://learn.microsoft.com/azure/azure-functions/functions-bindings-http-webhook-trigger#authorization-keys) for details.

Deployment checkpoint: The deployed endpoint returns the starter GraphQL response, the authorization behavior is intentional, and logs show the request.

# Troubleshooting Setup

| Symptom | Likely cause | Fix | Verification |
| --- | --- | --- | --- |
| Build fails after adding the Hot Chocolate package. | Wrong package for the worker model, unsupported target framework, or mixed Hot Chocolate versions. | Use `HotChocolate.AzureFunctions.IsolatedProcess` for isolated worker, `HotChocolate.AzureFunctions` for in-process, and align all `HotChocolate.*` versions. | `dotnet build` succeeds. |
| The host starts, but `/api/graphql` returns 404. | The trigger route, host route prefix, or `apiRoute` value does not match the URL. The function might also be undiscovered. | Compare `func start` output with the URL you are calling. Keep `Route = "graphql"` with the default `/api` prefix, or update `AddGraphQLFunction(apiRoute: "...")` to match your public path. | The listed trigger URL receives the request. |
| The endpoint asks for a key when the sample request omits one. | The HTTP trigger uses `AuthorizationLevel.Function` or another protected setting. | Use the correct key for the environment, or set anonymous access only for local learning. | The request reaches GraphQL instead of being rejected by Functions. |
| The response is 400 or contains GraphQL validation errors. | The JSON body is malformed, the content type is missing, or the queried field is not in the schema. | Send the minimal POST request from this page and query `hello`. Rebuild after adding `[QueryType]` and `Properties/ModuleInfo.cs`. | The response contains `data.hello`. |
| Startup fails with schema or dependency errors. | No query type was registered, `AddTypes()` did not run, analyzer module metadata is missing, or a resolver dependency is missing from the Functions DI container. | Register the starter query and required services in `Program.cs` or `FunctionsStartup`. Confirm `Properties/ModuleInfo.cs` exists and contains `[assembly: Module("Types")]`. | The host starts and the query executes. |
| Nitro or SDL is not available. | The route differs, tooling or schema requests are disabled, or the environment blocks browser access. | Verify the HTTP POST request first. Then check `ModifyFunctionOptions`, route values, authorization, and browser network errors. | The exposure matches your environment policy. |
| It works locally but fails in Azure. | App settings, route prefix, function keys, CORS, package versions, or runtime version differ between local and Azure. | Compare local and Azure configuration. Run the same POST smoke test against the deployed URL and check logs. | The deployed endpoint returns the same starter response. |
| The first request is slow. | Cold start, schema initialization, dependency loading, or hosting-plan behavior. | Measure startup latency, reduce startup work, evaluate the hosting plan, or choose ASP.NET Core for the API. | Latency meets the service objective or the host choice changes. |
| Browser clients fail while server-to-server tests work. | CORS, HTTPS, function authorization, or route differences. | Configure the browser-facing Azure layer and Function app deliberately. | Browser network requests reach GraphQL and receive the expected response. |

# Next Steps

After your endpoint works, choose your next step:

- [Building a schema](/docs/hotchocolate/v16/building-a-schema/): Add real schema types beyond the starter query.
- [Resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers/) and [Dependency Injection](/docs/hotchocolate/v16/resolvers-and-data/dependency-injection/): Connect fields to application services.
- [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader/): Avoid N+1 query behavior when loading related data.
- [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization/): Add policies and roles to fields or operations.
- [HTTP Transport](/docs/hotchocolate/v16/server/http-transport/) and [Endpoints](/docs/hotchocolate/v16/server/endpoints/): Learn about request semantics and endpoint options.
- [Prepare for production](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/13-prepare-for-production/): Review production readiness.
- [ASP.NET Core setup](/docs/hotchocolate/v16/learn/4-installation-and-setup/aspnet-core/): Switch if Functions no longer fits your API.
- [Migrate from 15 to 16](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16/): Upgrade from an older Hot Chocolate server.
