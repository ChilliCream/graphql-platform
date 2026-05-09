---
title: "ASP.NET Core setup"
description: "Install Hot Chocolate in a standard ASP.NET Core host, register a starter schema, map /graphql, open Nitro, and verify the first query."
---

This guide walks you through setting up a standard Hot Chocolate v16 GraphQL server in a new or clean ASP.NET Core web application.

By following these steps, you will:

- Install the `HotChocolate.AspNetCore` integration package
- Add the analyzer package for implementation-first development
- Create a starter `Query` type
- Expose a GraphQL endpoint at `/graphql`
- Access Nitro in your browser
- Run your first query and see a JSON response

In this setup, ASP.NET Core manages the web host, routing, middleware, configuration, logging, and deployment. Hot Chocolate handles GraphQL schema registration and request execution. You only need to register one service, define one schema type, and map one endpoint to get started.

If your project already includes controllers, Razor Pages, health checks, authentication, static files, or other routes that must remain, see [Existing ASP.NET Core app](/docs/hotchocolate/v16/learn/4-installation-and-setup/existing-aspnet-core-app/) for integration guidance.

# Start from a clean ASP.NET Core app

Begin by creating or opening an ASP.NET Core web project that builds successfully before you add GraphQL. If you want to start from scratch, you can create a new empty web app with:

```bash
dotnet new web --name LibraryServer
cd LibraryServer
```

Run the application once to confirm it works and to see the local URL:

```bash
dotnet run
```

When you see the listening URL in the terminal, stop the app with <kbd>Ctrl</kbd> + <kbd>C</kbd>. You’ll use this port again after you add GraphQL.

Before moving on, make sure:

- The current directory contains your `.csproj` file
- `dotnet run` starts the ASP.NET Core app
- You know the local URL printed by the app

If you’d rather use a generated Hot Chocolate starter, see [Install and scaffold](/docs/hotchocolate/v16/get-started/install-and-scaffold/).

# Install the ASP.NET Core integration package

From the directory containing your ASP.NET Core `.csproj` file, add the required packages:

```bash
dotnet add package HotChocolate.AspNetCore
dotnet add package HotChocolate.Types.Analyzers
```

- `HotChocolate.AspNetCore` integrates Hot Chocolate with ASP.NET Core, providing `AddGraphQL`, `MapGraphQL`, Nitro hosting, and the standard GraphQL endpoint middleware.
- `HotChocolate.Types.Analyzers` enables source-generated type registration for implementation-first development. It supports attributes like `[QueryType]` and the `AddTypes()` registration method.

Make sure all `HotChocolate.*` packages in your project use the same v16 version. If you use central package management or have pinned versions, specify the same version for these packages.

After installing the packages, build the project:

```bash
dotnet build
```

You should see:

```text
Build succeeded.
```

For more on package selection and version alignment, see [Packages](/docs/hotchocolate/v16/learn/4-installation-and-setup/packages/).

# Add the analyzer module metadata

Next, create a module metadata file so the analyzer can generate type registrations. For a new `dotnet new web` project, add a file at `Properties/ModuleInfo.cs`:

```csharp
// Properties/ModuleInfo.cs
using HotChocolate;

[assembly: Module("Types")]
```

The `[assembly: Module("Types")]` attribute tells the analyzer to use the module name "Types" for generated type registration. The `AddTypes()` method will then load types from this module. If you skip this file, the no-argument `AddTypes()` overload may compile, but the generated types will not be included in your schema.

Before continuing, check:

- `Properties/ModuleInfo.cs` exists
- The file contains `[assembly: Module("Types")]`

# Register GraphQL and add your first query type

Open `Program.cs` and register Hot Chocolate before you call `builder.Build()`:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddGraphQL().AddTypes();

var app = builder.Build();

app.Run();
```

- `builder.AddGraphQL()` registers the GraphQL server with ASP.NET Core.
- `AddTypes()` registers the source-generated schema types from your project.

Now, create a `Types` folder and add a starter query type:

```csharp
// Types/Query.cs
using HotChocolate.Types;

namespace LibraryServer.Types;

[QueryType]
public static partial class Query
{
    public static string GetHello()
        => "Hello from Hot Chocolate.";
}
```

Replace `LibraryServer.Types` with your project’s namespace if it differs.

This `Query` class is the root for GraphQL read operations. The `[QueryType]` attribute marks it as contributing fields to the root query type. Declaring the class as `partial` allows the analyzer to generate registration code. The `GetHello()` method becomes a GraphQL field named `hello`.

Build the project again:

```bash
dotnet build
```

Check that:

- `AddGraphQL()` compiles
- `AddTypes()` compiles
- `[QueryType]` compiles
- `Properties/ModuleInfo.cs` still exists and contains `[assembly: Module("Types")]`
- The project builds after adding the query type

If `AddTypes()` or `[QueryType]` is not found, make sure `HotChocolate.Types.Analyzers` is referenced and restored, then rebuild. If the build succeeds but `hello` is missing from the schema, confirm that `Properties/ModuleInfo.cs` is present. For more schema registration options, see [Building a schema](/docs/hotchocolate/v16/building-a-schema/).

# Map the GraphQL endpoint

After building the app, map the GraphQL endpoint before calling `app.Run()`:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddGraphQL().AddTypes();

var app = builder.Build();

app.MapGraphQL();

app.Run();
```

By default, `app.MapGraphQL()` exposes your GraphQL server at `/graphql`. When you open this endpoint in a browser, Nitro loads if tooling is enabled. You can also download the schema SDL by appending `?sdl` to the endpoint when schema requests are enabled.

If you want to use a custom path, you can do so after confirming the default works:

```csharp
app.MapGraphQL("/api/graphql");
```

For more on custom paths, endpoint options, Nitro, schema download, HTTP-only endpoints, WebSockets, multipart requests, and persisted operations, see [Endpoints](/docs/hotchocolate/v16/server/endpoints/).

Now run the app:

```bash
dotnet run
```

You should see output like:

```text
Now listening on: http://localhost:5095
Application started. Press Ctrl+C to shut down.
```

Your port may be different. Add `/graphql` to the URL printed by your app.

Make sure:

- The terminal stays running
- The app prints a listening URL
- The browser URL uses the current port and ends with `/graphql`

# Verify the endpoint with Nitro

With the app running, open the `/graphql` URL in your browser. For example:

```text
http://localhost:5095/graphql
```

Nitro should load and connect to your local endpoint. Depending on your browser’s state, Nitro may show a landing screen, an editor, or prompt you to create a document. If Nitro asks for an endpoint, enter the same `/graphql` URL.

Try running this query:

```graphql
query SayHello {
  hello
}
```

You should get a response like:

```json
{
  "data": {
    "hello": "Hello from Hot Chocolate."
  }
}
```

This confirms that ASP.NET Core routed the request to `/graphql`, Hot Chocolate built a schema with the `hello` field, the resolver executed, and the server returned a valid GraphQL response.

If Nitro loads but the query fails with a validation error, refresh Nitro’s schema information or reload the browser page after rebuilding and restarting the app.

# Download the schema (SDL)

The schema document defines the contract that clients can query. If schema requests are enabled, you can download the SDL by appending `?sdl` to your GraphQL endpoint:

```text
http://localhost:5095/graphql?sdl
```

You should see SDL like this, including your starter field:

```graphql
type Query {
  hello: String!
}
```

Downloading the schema is useful for local inspection, client tooling, CI checks, or schema review. Before exposing this in production, decide if schema download should remain enabled. See [Endpoints](/docs/hotchocolate/v16/server/endpoints/) for endpoint options and [Introspection](/docs/hotchocolate/v16/securing-your-api/introspection/) for schema visibility guidance.

# Add ASP.NET Core middleware as needed

`MapGraphQL()` only maps the endpoint. It does not configure authentication, authorization, or CORS for you.

Apply the same ASP.NET Core middleware rules to your GraphQL endpoint as you do for other endpoints in your app:

| Scenario | Add this ASP.NET Core piece | Continue here |
| --- | --- | --- |
| The endpoint should require an authenticated user | Configure authentication and authorization, then require authorization on the endpoint. | [Authentication](/docs/hotchocolate/v16/securing-your-api/authentication/) and [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization/) |
| A browser app calls `/graphql` from another origin | Configure an ASP.NET Core CORS policy and apply it to the app or endpoint. | [ASP.NET Core CORS documentation](https://learn.microsoft.com/aspnet/core/security/cors) |
| GraphQL fields need roles or policies | Add the Hot Chocolate authorization package and schema authorization configuration. | [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization/) |
| Public clients can send arbitrary requests | Review request limits, cost analysis, and trusted documents. | [Request Limits](/docs/hotchocolate/v16/securing-your-api/request-limits/), [Cost Analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis/), and [Trusted Documents](/docs/hotchocolate/v16/performance/trusted-documents/) |

For example, after you configure authentication and authorization, you can require authorization on the GraphQL endpoint:

```csharp
app.MapGraphQL().RequireAuthorization();
```

If browsers need to call the endpoint from another origin, apply your CORS policy:

```csharp
app.MapGraphQL().RequireCors("GraphQLClients");
```

This example assumes you have registered the `GraphQLClients` policy with ASP.NET Core. Apply CORS middleware or endpoint metadata according to the routing and middleware rules you use elsewhere in your app.

Remember, endpoint authorization controls access to the GraphQL endpoint itself. Field-level authorization controls which schema fields or operations an authenticated user can access.

# Choose development and production defaults

During development, you want fast feedback. In production, only expose the surfaces your team intends to support.

| Area | Development checkpoint | Production checkpoint |
| --- | --- | --- |
| Nitro | Opening `/graphql` in a browser helps you test queries. | Decide where Nitro is enabled and who can reach it. |
| Schema SDL | `/graphql?sdl` helps you inspect the running schema. | Decide whether schema download is enabled for the environment. |
| Errors | Detailed errors can help local debugging. | Review error details, logging, and tracing before public exposure. |
| Requests | Test the default endpoint first. | Review request limits, allowed GET behavior, batching, file uploads, and trusted documents if you use them. |
| Browser callers | Local tools often use the same origin. | Configure CORS for the origins that should call the endpoint. |
| Security | Start with a local endpoint and known query. | Add authentication, authorization, introspection policy, cost controls, and observability as needed. |

A common pattern is to enable endpoint tooling only in development:

```csharp
app.MapGraphQL().WithOptions(options =>
{
    options.Tool.Enable = app.Environment.IsDevelopment();
    options.EnableSchemaRequests = app.Environment.IsDevelopment();
});
```

Before exposing your endpoint beyond local development, review [Securing your API](/docs/hotchocolate/v16/securing-your-api/), [Request Limits](/docs/hotchocolate/v16/securing-your-api/request-limits/), [Cost Analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis/), [Introspection](/docs/hotchocolate/v16/securing-your-api/introspection/), and [Instrumentation](/docs/hotchocolate/v16/server/instrumentation/).

# Troubleshooting checkpoints

Use this table to diagnose common setup issues:

| Symptom | Likely cause | What to check |
| --- | --- | --- |
| `AddGraphQL` or `MapGraphQL` is not recognized | The ASP.NET Core integration package is missing or restore did not complete. | Confirm `HotChocolate.AspNetCore` is referenced, restore, and build. |
| `AddTypes()` is not recognized | The analyzer package is missing or the project has not rebuilt. | Confirm `HotChocolate.Types.Analyzers` is referenced, restore, and rebuild. |
| `[QueryType]` is not recognized | The analyzer package or namespace is missing. | Add `using HotChocolate.Types;`, confirm package restore, and rebuild. |
| `AddTypes()` compiles but `hello` is missing from the schema | The analyzer module metadata is missing or the query type was not included in the generated module. | Confirm `Properties/ModuleInfo.cs` exists, contains `[assembly: Module("Types")]`, and the query class is under the project being built. |
| The app fails before listening on a port | Startup or schema construction failed. | Read the first exception in the terminal, then fix package, schema, or DI errors before testing HTTP. |
| `/graphql` returns 404 | The endpoint was not mapped, the app was not restarted, or the URL uses the wrong port or path. | Confirm `app.MapGraphQL()` is present and open the current listening URL with `/graphql`. |
| Nitro opens but `hello` is not queryable | The schema did not include the query type or Nitro has stale schema information. | Confirm `Properties/ModuleInfo.cs`, the `partial` query class, and `builder.AddGraphQL().AddTypes();` are present, then rebuild, restart, and refresh Nitro. |
| A browser client is blocked | CORS is missing or not applied to the endpoint. | Configure an ASP.NET Core CORS policy for the caller origin and apply it to the app or endpoint. |
| Authorized requests never reach GraphQL | Authentication or authorization middleware is incomplete. | Verify the ASP.NET Core authentication scheme, authorization policy, middleware order, and endpoint policy. |

For more recovery steps, see [Get Started troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting/).

# Next steps

You’re ready to move on when:

- `dotnet build` succeeds
- `dotnet run` starts the app
- `/graphql` opens Nitro
- The `hello` query returns `data.hello`
- `/graphql?sdl` shows the starter `Query` type (when schema requests are enabled)

Choose your next step based on your goal:

| Goal | Go here |
| --- | --- |
| Learn the schema registration model | [Building a schema](/docs/hotchocolate/v16/building-a-schema/) |
| Build a complete server tutorial | [Build your first GraphQL server](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/) |
| Add object types and query fields | [Object Types](/docs/hotchocolate/v16/building-a-schema/object-types/) and [Queries](/docs/hotchocolate/v16/building-a-schema/queries/) |
| Connect resolvers to services and data | [Resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers/) and [Dependency Injection](/docs/hotchocolate/v16/resolvers-and-data/dependency-injection/) |
| Add filtering, sorting, projections, paging, or DataLoader | [Resolvers and data](/docs/hotchocolate/v16/resolvers-and-data/) |
| Configure endpoint behavior | [Endpoints](/docs/hotchocolate/v16/server/endpoints/) |
| Add GraphQL to an app you already own | [Existing ASP.NET Core app](/docs/hotchocolate/v16/learn/4-installation-and-setup/existing-aspnet-core-app/) |
| Prepare for public exposure | [Securing your API](/docs/hotchocolate/v16/securing-your-api/) and [Performance](/docs/hotchocolate/v16/performance/) |
