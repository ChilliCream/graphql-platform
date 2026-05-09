---
title: ".NET Aspire setup"
description: "Run a Hot Chocolate v16 server from a .NET Aspire AppHost, find the GraphQL endpoint in the dashboard, verify a request, and inspect local telemetry."
---

Use this guide when you want to run your Hot Chocolate server as part of a local .NET Aspire app model.

By following this page, you will:

- Add your GraphQL ASP.NET Core project to an Aspire AppHost
- Start the service alongside the rest of your local application
- Locate the service endpoint in the Aspire dashboard
- Send a GraphQL request to the mapped Hot Chocolate route
- Inspect logs and traces for that request
- Identify what to address later in deployment

# Should you use Aspire for your setup?

Aspire is ideal when your GraphQL server is one of several services in a local distributed application. Aspire provides a single AppHost to start your app, a dashboard to find endpoints, and a unified place to inspect local logs and telemetry.

Choose this setup if your Hot Chocolate server needs to work with:

- A database, cache, message broker, or other local resource
- Downstream HTTP or gRPC services
- A client app that calls GraphQL during development
- A Fusion gateway or multiple GraphQL services
- Shared local observability through the Aspire dashboard

Consider a different setup if:

- You have only one GraphQL server and `dotnet run` is sufficient
- You are still choosing packages or registering your first schema
- You need guidance for Docker, Kubernetes, IIS, reverse proxy, or cloud deployment

For a standard server setup, see [ASP.NET Core setup](/docs/hotchocolate/v16/learn/4-installation-and-setup/aspnet-core/). For help choosing a host, see [Installation and setup](/docs/hotchocolate/v16/learn/4-installation-and-setup/).

**Checkpoint:** Before you continue, make sure you know the names of your GraphQL project, your AppHost project, and the local resource or service that makes Aspire valuable for your solution.

# Prepare your GraphQL service for orchestration

Start with a GraphQL service that runs independently, outside Aspire. This helps you separate routing, schema, and package issues from AppHost concerns.

Your GraphQL project should already include the standard Hot Chocolate registration:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddGraphQL().AddTypes();

var app = builder.Build();

app.MapGraphQL();

app.Run();
```

By default, `app.MapGraphQL()` maps the GraphQL endpoint to `/graphql`. If you use a different route, note it now because you will need to append it to the service URL shown in the Aspire dashboard.

Run your GraphQL project by itself:

```bash
dotnet run
```

Open the local GraphQL endpoint and run a simple query:

```graphql
query CheckServer {
  __typename
}
```

You should see a response like:

```json
{
  "data": {
    "__typename": "Query"
  }
}
```

Before moving on, check that:

- The GraphQL project restores and builds
- `dotnet run` starts the project outside Aspire
- You know the endpoint path (usually `/graphql`)
- A query returns `data`
- You have all required configuration keys, connection strings, and secrets
- Development tools like Nitro are enabled only in the environments where you want them

If any of these steps fail, resolve them using [ASP.NET Core setup](/docs/hotchocolate/v16/learn/4-installation-and-setup/aspnet-core/), [Local development](/docs/hotchocolate/v16/learn/4-installation-and-setup/local-development/), or [Get started troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting/) before adding the AppHost.

# Add your GraphQL service to the Aspire AppHost

The AppHost is responsible for starting projects and resources. Hot Chocolate still runs inside your ASP.NET Core project and continues to own the GraphQL schema and endpoint mapping.

In your AppHost project, register the GraphQL project with a resource name your team will recognize in the dashboard:

```csharp
// AppHost.cs or Program.cs in the AppHost project
var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Library_GraphQL>("graphql");

builder.Build().Run();
```

Replace `Projects.Library_GraphQL` with the generated project type for your GraphQL project. Aspire generates these types from your project names. For example, a project named `Library.GraphQL` is typically exposed as `Projects.Library_GraphQL`.

Start the AppHost:

```bash
dotnet run
```

You should see:

- The Aspire dashboard opens or prints a dashboard URL
- The resource list includes `graphql`
- The resource reaches a running state
- The resource row displays logs and at least one endpoint

Keep endpoint configuration in your GraphQL project unless Aspire needs to vary them for local orchestration. For example, keep `app.MapGraphQL("/graphql")` in your service and use the AppHost for project references, resources, environment variables, and dependency wiring.

For more on AppHost concepts and syntax, see Microsoft's [.NET Aspire AppHost overview](https://learn.microsoft.com/dotnet/aspire/fundamentals/app-host-overview).

# Find and verify the GraphQL endpoint in the dashboard

The dashboard URL is not your GraphQL endpoint. Use the endpoint listed for the GraphQL service resource.

To verify the endpoint:

1. Open the Aspire dashboard
2. Select the `graphql` resource
3. Copy an HTTP or HTTPS endpoint for that resource
4. Append your Hot Chocolate route (usually `/graphql`)
5. Send a GraphQL request to the full URL

For example, if the dashboard shows:

```text
https://localhost:7241
```

your default GraphQL endpoint is:

```text
https://localhost:7241/graphql
```

Verify it with a POST request:

```bash
curl -s -X POST https://localhost:7241/graphql \
  -H "Content-Type: application/json" \
  --data '{"query":"query CheckAspire { __typename }"}'
```

If HTTPS fails in your local tools, trust the ASP.NET Core development certificate or use an HTTP endpoint for the GraphQL resource from the dashboard.

You should see:

```json
{
  "data": {
    "__typename": "Query"
  }
}
```

If you open the same `/graphql` URL in a browser, Nitro loads when enabled for your environment. If Nitro asks for an endpoint, provide the full GraphQL service URL, not the dashboard URL.

For custom `MapGraphQL` paths, endpoint options, schema requests, Nitro hosting, HTTP transport, and subscriptions, see [Endpoints](/docs/hotchocolate/v16/server/endpoints/).

# Observe GraphQL requests in the Aspire dashboard

When a local query fails, start by checking the resource logs. Use traces if the failure crosses service boundaries or you need timing details.

In the Aspire dashboard:

- Use **Logs** to confirm startup, schema construction, endpoint mapping, and request errors
- Use **Traces** to follow an operation through ASP.NET Core, Hot Chocolate, and downstream HTTP calls (when instrumentation is configured)
- Use the resource name to distinguish the GraphQL service from the AppHost, database, gateway, or client app

Send a named operation to make it easier to spot in traces:

```graphql
query DashboardTraceCheck {
  __typename
}
```

Then check the dashboard for activity from the `graphql` resource after the request.

Aspire service defaults often configure OpenTelemetry for ASP.NET Core and HTTP clients. To emit GraphQL-specific spans, install the Hot Chocolate diagnostics package in your GraphQL service. The `.AddInstrumentation()` method is provided by this package:

```bash
dotnet add package HotChocolate.Diagnostics
```

Register Hot Chocolate instrumentation with your GraphQL server:

```csharp
builder
    .AddGraphQL()
    .AddTypes()
    .AddInstrumentation();
```

Add the Hot Chocolate activity source to OpenTelemetry tracing, usually in your ServiceDefaults project:

```csharp
builder.Services
    .AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddHotChocolateInstrumentation();
    });
```

Any project that calls `.AddHotChocolateInstrumentation()` must reference the `HotChocolate.Diagnostics` package. If OpenTelemetry is configured in a separate ServiceDefaults project, add the package there as well as to the GraphQL service.

The dashboard running does not guarantee GraphQL telemetry is configured. Make sure at least one request appears in logs or traces and identifies the GraphQL service, operation timing, or operation name according to your instrumentation settings.

For more on package details, options, and span behavior, see [Instrumentation](/docs/hotchocolate/v16/server/instrumentation/). For Aspire dashboard details, see Microsoft's [.NET Aspire dashboard documentation](https://learn.microsoft.com/dotnet/aspire/fundamentals/dashboard/overview).

# Connect a local dependency using Aspire service discovery

Resolvers should depend on services registered through dependency injection. Avoid hard-coded local URLs in your resolver code.

Suppose you want your GraphQL service to call a downstream catalog service using Aspire service discovery. Here is how you can set it up.

In the AppHost:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var catalog = builder.AddProject<Projects.Catalog_Api>("catalog");

builder
    .AddProject<Projects.Library_GraphQL>("graphql")
    .WithReference(catalog)
    .WaitFor(catalog);

builder.Build().Run();
```

The `Projects.Catalog_Api` and `Projects.Library_GraphQL` types are generated by Aspire from your project names.

`WithReference(catalog)` wires the AppHost resource relationship. In your GraphQL service, enable Aspire service discovery, typically with `builder.AddServiceDefaults()` in `Program.cs`. If you do not use ServiceDefaults, register equivalent service discovery and HTTP client defaults before configuring clients.

In the GraphQL service, enable service defaults and register an HTTP client using the service discovery name:

```csharp
builder.AddServiceDefaults();

builder.Services.AddHttpClient<CatalogClient>(client =>
{
    client.BaseAddress = new Uri("https+http://catalog");
});
```

Then inject your client or an abstraction into resolvers:

```csharp
using HotChocolate.Types;

namespace Library.GraphQL.Types;

[QueryType]
public static partial class Query
{
    public static async Task<IReadOnlyList<Book>> GetBooksAsync(
        CatalogClient catalog,
        CancellationToken cancellationToken)
        => await catalog.GetBooksAsync(cancellationToken);
}
```

Your schema does not need to know that Aspire provided the local address. The AppHost wires projects together. The GraphQL service manages HTTP client registration, schema code, endpoint mapping, and Hot Chocolate options.

**Checkpoint:**

- The dashboard shows both `graphql` and `catalog`
- The GraphQL query returns data from the dependency
- Logs or traces show a downstream call from the GraphQL service to the catalog service

For more on service discovery, see Microsoft's [.NET Aspire service discovery documentation](https://learn.microsoft.com/dotnet/aspire/service-discovery/overview). For resolver and DI guidance, see [Resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers/) and [Dependency Injection](/docs/hotchocolate/v16/resolvers-and-data/dependency-injection/).

# Add more local resources after verifying the endpoint

After your GraphQL endpoint works through Aspire, add one resource at a time. Verify each resource in the dashboard before changing schema or resolver code again.

| Resource | Why GraphQL might need it | Verify in Aspire | Continue here |
| --- | --- | --- | --- |
| Database | Resolvers read or write application data. | Database resource is running, connection string is provided, one query exercises it. | [Fetching from databases](/docs/hotchocolate/v16/resolvers-and-data/fetching-from-databases/) |
| Downstream HTTP service | Resolvers compose data from another service. | Both services run and one trace or log entry shows the outbound call. | [Fetching from REST](/docs/hotchocolate/v16/resolvers-and-data/fetching-from-rest/) |
| Cache | Resolvers or DataLoaders reuse local data. | Cache resource starts and cache configuration flows through DI. | [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader/) |
| Fusion gateway | A gateway composes or fronts multiple GraphQL services. | Gateway and subgraph resources are named by role and expose distinct endpoints. | [Fusion](/docs/hotchocolate/v16/fusion/) |
| Client app | A local UI or worker calls the GraphQL endpoint. | Client resource points to the GraphQL service endpoint, not the dashboard. | [Connecting a client](/docs/hotchocolate/v16/get-started/connecting-a-client/) |

Choose meaningful resource names. If you run multiple GraphQL services, names like `accounts-graphql`, `inventory-graphql`, and `gateway` are easier to read in logs than names based only on project folders.

Let configuration flow through Aspire resource references, connection strings, options, and DI. Do not put AppHost resource names in schema descriptions, object type names, or resolver logic unless the name is part of your application domain.

# Move from Aspire local orchestration to deployment

Aspire helps you confirm that your service starts, exposes an endpoint, calls dependencies, and emits useful diagnostics locally. It is not your full production plan.

Before exposing your GraphQL endpoint beyond local development, answer these questions:

- Which deployment target will run the service?
- Which public route maps to the GraphQL endpoint?
- Does a proxy or path base change the `/graphql` path?
- Are WebSockets required for subscriptions?
- Which origins can call the endpoint from browsers?
- How are authentication and authorization configured?
- Is Nitro enabled only where it is approved?
- Are schema requests and introspection aligned with your API boundary?
- Which OpenTelemetry exporter, collector, sampling rules, and sensitive data policy apply?
- Where do logs, traces, health checks, and readiness checks go?

Use the deployment page that matches your next step:

- [ASP.NET Core setup](/docs/hotchocolate/v16/learn/4-installation-and-setup/aspnet-core/) for host, endpoint, and deployment checks
- [Endpoints](/docs/hotchocolate/v16/server/endpoints/) for path base, endpoint options, and public routing
- [HTTP transport](/docs/hotchocolate/v16/server/http-transport/) for GraphQL over HTTP behavior
- [Securing your API](/docs/hotchocolate/v16/securing-your-api/) for authentication, authorization, introspection, limits, and cost analysis
- [Instrumentation](/docs/hotchocolate/v16/server/instrumentation/) for diagnostics and OpenTelemetry details

# Troubleshooting Aspire setup

Start with the symptom and change one layer at a time.

| Symptom | Likely cause | What to check |
| --- | --- | --- |
| GraphQL service does not appear in the dashboard | The AppHost does not reference the project, the generated project type is wrong, the wrong AppHost was launched, or startup failed. | Verify `builder.AddProject<Projects.Your_Project>("graphql")`, run the AppHost project, and read AppHost output. |
| Service appears but `/graphql` returns 404 | The URL is the service base URL without the GraphQL path, `MapGraphQL` uses a custom path, or path base settings differ. | Confirm the `MapGraphQL` path in the GraphQL project and append that path to the service endpoint. |
| Browser opens the dashboard instead of Nitro | The dashboard URL was copied instead of the resource endpoint URL. | Copy the endpoint from the GraphQL service resource row. |
| HTTPS requests fail locally | The development certificate is not trusted or the copied endpoint uses a scheme your tool does not accept. | Trust the ASP.NET Core development certificate or use the expected local HTTP endpoint. |
| Query works with `dotnet run` but fails through Aspire | AppHost launch uses different configuration, environment variables, ports, secrets, or dependencies. | Compare standalone launch output with AppHost resource environment and configuration. |
| Resolver cannot reach a downstream service | The service discovery name is wrong, `.WithReference` is missing, the HTTP client is not registered through DI, or resolver code uses hard-coded `localhost`. | Wire the dependency in the AppHost and register the client in the GraphQL service. |
| Logs appear but traces are missing | OpenTelemetry is not configured, the exporter is not connected to the dashboard, or no request was sent after startup. | Check service defaults, exporter configuration, and send one named operation. |
| ASP.NET Core traces appear but GraphQL spans do not | `HotChocolate.Diagnostics`, `AddInstrumentation`, or `AddHotChocolateInstrumentation` is missing. | Follow [Instrumentation](/docs/hotchocolate/v16/server/instrumentation/) and verify one GraphQL operation again. |
| Startup fails after adding a database or broker | Resource startup, container runtime, port conflict, or connection string binding failed. | Isolate the failing resource in dashboard logs before changing GraphQL schema code. |
| Subscriptions work outside Aspire but fail from the dashboard URL | The client is using the dashboard URL, the wrong service endpoint, or a route that does not support WebSockets. | Test the GraphQL service endpoint with a WebSocket-capable GraphQL client and confirm subscription setup. |

For help with certificates, see [Trust the ASP.NET Core HTTPS development certificate](https://learn.microsoft.com/aspnet/core/security/enforcing-ssl#trust-the-aspnet-core-https-development-certificate).

# Aspire setup checklist

You have completed this setup when you can confirm each item in your terminal, dashboard, or GraphQL response:

- [ ] The GraphQL service runs independently with `dotnet run`
- [ ] The GraphQL endpoint path is known, usually `/graphql`
- [ ] The AppHost references the GraphQL project with a recognizable resource name
- [ ] The Aspire dashboard shows the GraphQL service status, logs, and endpoints
- [ ] The service endpoint plus the GraphQL path accepts a POST request
- [ ] Nitro loads from the service endpoint when enabled for local development
- [ ] Dependencies are wired through AppHost references, configuration, and DI
- [ ] One named GraphQL request is visible in logs or traces
- [ ] Production concerns are routed to the correct hosting, security, endpoint, and instrumentation guides
