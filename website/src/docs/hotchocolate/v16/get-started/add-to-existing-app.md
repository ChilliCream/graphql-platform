---
title: "Add to an existing app"
description: "Add Hot Chocolate to an ASP.NET Core app you already have, map /graphql beside existing routes, and verify the first query."
---

If you already have an ASP.NET Core application, you can add a Hot Chocolate GraphQL endpoint without disrupting your existing controllers, Minimal API routes, Razor Pages, health checks, or static files. This guide walks you through integrating Hot Chocolate into your current app, mapping `/graphql` alongside your existing endpoints, and verifying your first GraphQL query.

By following these steps, you will:

- Install the required Hot Chocolate packages for ASP.NET Core
- Register GraphQL services in `Program.cs`
- Add a read-only query field
- Map `/graphql` next to your existing endpoints
- Run the app and confirm a GraphQL response

If you prefer to start with a new project scaffolded for Hot Chocolate, see [Install and scaffold](/docs/hotchocolate/v16/get-started/install-and-scaffold). This page assumes you have an existing, working ASP.NET Core app.

# Is this the right starting point?

Use this guide if you want to add a GraphQL endpoint to an existing ASP.NET Core application.

| Your situation | Start here |
| --- | --- |
| You have an ASP.NET Core app and want `/graphql` beside existing routes. | Continue with this page. |
| You want a new starter project from a template. | See [Install and scaffold](/docs/hotchocolate/v16/get-started/install-and-scaffold). |
| You need custom endpoint paths, HTTP-only middleware, WebSockets, schema download, or Nitro options. | After this integration, read [Endpoints](/docs/hotchocolate/v16/server/endpoints). |
| Your first concern is authentication, authorization, CORS, request limits, trusted documents, or deployment. | Start with the local endpoint here, then review [Securing your API](/docs/hotchocolate/v16/securing-your-api), [Performance](/docs/hotchocolate/v16/performance), and [Server](/docs/hotchocolate/v16/server). |

Before making changes, run your app as usual and confirm that an existing route responds. This gives you a checkpoint to compare against if you need to revert.

# Install Hot Chocolate packages

From the directory containing your ASP.NET Core `.csproj` file, run:

```bash
dotnet add package HotChocolate.AspNetCore
dotnet add package HotChocolate.Types.Analyzers
```

- `HotChocolate.AspNetCore` provides the ASP.NET Core integration, including `AddGraphQL`, `MapGraphQL`, and Nitro.
- `HotChocolate.Types.Analyzers` enables source-generated type registration for the implementation-first approach in v16.

If your project already references other Hot Chocolate packages, ensure all are on the same version. Version mismatches often cause restore or build errors.

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

If restore fails or packages do not align, see [Troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting) before proceeding.

# Register GraphQL services

Open `Program.cs` and locate your service registrations, typically near `var builder = WebApplication.CreateBuilder(args);`.

Add the following line alongside your other `builder.Services` or `builder` configuration calls:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.AddGraphQL().AddTypes();

var app = builder.Build();

app.MapControllers();

app.Run();
```

Keep your existing registrations. This line registers the GraphQL server and the source-generated type registrations for your GraphQL types.

If your app uses Minimal APIs, place the GraphQL registration in the same area:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddGraphQL().AddTypes();

var app = builder.Build();

app.MapGet("/", () => "Hello from the existing app.");

app.Run();
```

Do not remove any existing services or endpoint mappings. GraphQL is added as an additional endpoint.

Next, create the analyzer module metadata for your project:

```csharp
// Properties/ModuleInfo.cs
using HotChocolate;

[assembly: Module("Types")]
```

The `Module("Types")` attribute names the generated type module used by `AddTypes()`.

# Add a query field

Create a C# file for your first query type. The example below uses a `Types` folder, but you can use your project's folder structure.

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

Replace `YourApp.Types` with your app's namespace.

- The `Query` class is the root for GraphQL read operations.
- `[QueryType]` marks this class as contributing fields to the root type.
- The `partial` keyword allows the source generator to add registration code at build time.

The method `GetHello()` becomes a GraphQL field named `hello`. Hot Chocolate removes the `Get` prefix and applies camel casing. You will use this field in the verification step.

Build your project again:

```bash
dotnet build
```

If the compiler cannot find `QueryTypeAttribute`, check that both `HotChocolate.Types.Analyzers` and `HotChocolate.AspNetCore` are installed and restored. If `AddTypes()` is missing, ensure the analyzer package is present and rebuild the project.

# Map the GraphQL endpoint

After `var app = builder.Build();`, map GraphQL alongside your existing endpoints:

```csharp
var app = builder.Build();

app.MapControllers();
app.MapGraphQL();

app.Run();
```

For Minimal API apps, add `app.MapGraphQL()` with your other routes:

```csharp
var app = builder.Build();

app.MapGet("/", () => "Hello from the existing app.");
app.MapGraphQL();

app.Run();
```

By default, `MapGraphQL()` exposes the GraphQL server at `/graphql` and serves Nitro at that endpoint during local development. For custom paths and endpoint options, see [Endpoints](/docs/hotchocolate/v16/server/endpoints).

# Run the app and access the endpoint

Start your app using your usual command. For many projects, this is:

```bash
dotnet run
```

Leave the terminal open and copy the listening URL from the output, for example:

```text
Now listening on: http://localhost:5095
Application started. Press Ctrl+C to shut down.
```

Your port may differ. Add `/graphql` to the URL shown in your terminal. For the example above, open:

```text
http://localhost:5095/graphql
```

Nitro should appear in your browser, and your existing routes should continue to work as before.

If `/graphql` returns 404, check the following:

- `app.MapGraphQL()` was added
- The app was restarted after editing `Program.cs`
- You are using the port shown in the current run
- The browser URL ends with `/graphql`

# Verify with a query

In Nitro, run this operation:

```graphql
query SayHello {
  hello
}
```

You should receive this response:

```json
{
  "data": {
    "hello": "Hello from Hot Chocolate."
  }
}
```

This confirms that:

1. ASP.NET Core routed the request to `/graphql`
2. Hot Chocolate built a schema with the `hello` field
3. The `GetHello()` resolver executed
4. The server returned a valid GraphQL response

The response format follows the [GraphQL specification](https://spec.graphql.org/), which defines the shape of successful responses. The [GraphQL over HTTP specification](https://graphql.github.io/graphql-over-http/) describes how GraphQL operations are sent over HTTP.

For a detailed walkthrough of Nitro and response handling, see [Run your first query](/docs/hotchocolate/v16/get-started/run-your-first-query).

# GraphQL and your existing endpoints

Your app now includes an additional ASP.NET Core endpoint. Clients can continue using your REST, MVC, Razor Pages, health, or static file routes as before. GraphQL clients send operations to `/graphql`.

Later, you can inject registered services into resolver method parameters, allowing your resolvers to reuse the same application services as your existing endpoints. Learn more in [Dependency Injection](/docs/hotchocolate/v16/resolvers-and-data/dependency-injection) and [Resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers).

Before exposing `/graphql` beyond local development, review the following concerns:

| Concern | Where to learn more |
| --- | --- |
| Endpoint options, custom path, HTTP, WebSockets, schema download, Nitro options | [Endpoints](/docs/hotchocolate/v16/server/endpoints) |
| Authentication and authorization | [Authentication](/docs/hotchocolate/v16/securing-your-api/authentication) and [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization) |
| Cross-origin browser clients | [ASP.NET Core CORS documentation](https://learn.microsoft.com/aspnet/core/security/cors) |
| Request size, depth, cost, and public exposure | [Request Limits](/docs/hotchocolate/v16/securing-your-api/request-limits) and [Cost Analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis) |
| Accepting only known client operations | [Trusted Documents](/docs/hotchocolate/v16/performance/trusted-documents) |

# Troubleshooting

| Symptom | What to check |
| --- | --- |
| `dotnet add package` or restore fails | NuGet source access, network policy, SDK selection, and Hot Chocolate package version alignment. |
| `AddGraphQL` is not found | Ensure `HotChocolate.AspNetCore` is referenced and restored. If implicit usings are disabled, add the required `using` statements or call the extension from a file that already imports ASP.NET Core hosting namespaces. |
| `AddTypes` is not found | Confirm `HotChocolate.Types.Analyzers` is referenced, restore completed, and the project rebuilt after the package was added. |
| `QueryTypeAttribute` is not found | Add `using HotChocolate.Types;` to `Types/Query.cs` and confirm Hot Chocolate packages restored. |
| `/graphql` returns 404 | Check that `app.MapGraphQL()` is present, the app restarted, and the browser uses the current listening URL and `/graphql` path. |
| Nitro does not load | Ensure the server is running, the URL is correct, and endpoint tooling has not been disabled through endpoint options. |
| The query returns `Cannot query field "hello"` | Confirm the resolver method is named `GetHello`, the query selects `hello`, the class is `partial`, and the app rebuilt after the file was added. |
| An existing route stopped working | Restore the previous route mapping and add `app.MapGraphQL()` without replacing existing `MapGet`, `MapControllers`, `MapRazorPages`, health check, or static file configuration. |

For more troubleshooting steps, see [Troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting).

# Next steps

You now have a working GraphQL endpoint in your existing ASP.NET Core app.

Choose what to do next:

- **Understand the first response:** See [Run your first query](/docs/hotchocolate/v16/get-started/run-your-first-query).
- **Add real fields and data:** Learn about [Resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers), [Dependency Injection](/docs/hotchocolate/v16/resolvers-and-data/dependency-injection), and [Building a schema](/docs/hotchocolate/v16/building-a-schema).
- **Connect a client:** See [Connect a client](/docs/hotchocolate/v16/get-started/connecting-a-client).
- **Configure hosting behavior:** Read [Endpoints](/docs/hotchocolate/v16/server/endpoints).
- **Prepare for production:** Review [Securing your API](/docs/hotchocolate/v16/securing-your-api), [Performance](/docs/hotchocolate/v16/performance), and [Server](/docs/hotchocolate/v16/server).
