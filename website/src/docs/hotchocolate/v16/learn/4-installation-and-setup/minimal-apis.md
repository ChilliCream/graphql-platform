---
title: "Minimal APIs"
description: "Add Hot Chocolate v16 beside ASP.NET Core Minimal API routes, share services through DI, choose endpoint paths, and verify both API styles."
---

This guide helps you add a GraphQL endpoint to an existing ASP.NET Core Minimal API app, so you can keep your current routes and expand your API surface.

GraphQL and Minimal APIs are both first-class endpoints in ASP.NET Core. They share the same host, dependency injection container, configuration, logging, authentication, CORS, and endpoint routing. The main difference is in the client contract: Minimal API routes expose specific HTTP resources or commands, while GraphQL provides a single endpoint for client operations against a schema.

If you need a refresher on Minimal APIs, see the Microsoft [Minimal APIs overview](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis). This page focuses on integrating Hot Chocolate into that app structure.

# Should you add GraphQL to your Minimal API app?

Add GraphQL to your Minimal API app when:

- The same team owns both the Minimal API routes and the GraphQL schema.
- Both API surfaces use the same domain services and data sources.
- You want to incrementally add graph-shaped reads or client-selected fields.
- Existing routes like `/api/todos`, `/health`, webhooks, downloads, or commands need to keep their current contracts.
- The app can share a single deployment, scaling profile, and security boundary.

Consider a separate GraphQL host if you need a different release cadence, runtime dependencies, security boundary, scaling profile, or a different owning team for GraphQL.

**Before you start:** List which routes will remain Minimal APIs and which client needs should go through GraphQL. A typical starting point looks like this:

| Endpoint      | Purpose                                                      |
|--------------|--------------------------------------------------------------|
| `/api/todos` | Existing Minimal API route for targeted REST-style access.   |
| `/health`    | Existing operational endpoint.                               |
| `/graphql`   | New Hot Chocolate endpoint for GraphQL and local Nitro.      |

# Start with a Minimal API host

A Minimal API app registers services before `builder.Build()` and maps endpoints after.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<TodoService>();

var app = builder.Build();

var api = app.MapGroup("/api");

api.MapGet("/todos", (TodoService todos) => todos.GetTodos());

app.MapGet("/health", () => Results.Ok("Healthy"));

app.Run();
```

Key placements:

- Register shared services in `builder.Services`.
- Call `builder.AddGraphQL()` before `builder.Build()`.
- Call `app.MapGraphQL()` alongside your other endpoint mappings.

If you haven't already, install the required Hot Chocolate packages:

```bash
dotnet add package HotChocolate.AspNetCore
dotnet add package HotChocolate.Types.Analyzers
```

Make sure all `HotChocolate.*` packages use the same v16 version. For help with package selection and version alignment, see [Packages](/docs/hotchocolate/v16/learn/4-installation-and-setup/packages/).

**Checkpoint:** The app still builds and your Minimal API route works before you add GraphQL.

# Register Hot Chocolate in the shared container

Register GraphQL with the same `WebApplicationBuilder` you use for Minimal API services.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<TodoService>();

builder.AddGraphQL().AddTypes();

var app = builder.Build();

var api = app.MapGroup("/api");

api.MapGet("/todos", (TodoService todos) => todos.GetTodos());

app.MapGraphQL();

app.Run();
```

- `builder.AddGraphQL()` registers the Hot Chocolate server.
- `AddTypes()` registers analyzer-generated GraphQL types, including those marked with `[QueryType]`.

For new Minimal API projects, create a `Properties/ModuleInfo.cs` file:

```csharp
// Properties/ModuleInfo.cs
using HotChocolate;

[assembly: Module("Types")]
```

The `Module("Types")` attribute gives the analyzer a module name for generated type registration. `AddTypes()` uses this module when registering the schema. Without this file, the no-argument `AddTypes()` overload may compile, but generated types could be missing from the loaded module.

Resolvers and Minimal API handlers can both request the same services. Register a service once in `builder.Services` and use it from both surfaces.

```csharp
// Types/TodoQueries.cs
using HotChocolate.Types;

namespace YourApp.Types;

[QueryType]
public static partial class TodoQueries
{
    public static IReadOnlyList<Todo> GetTodos(TodoService todos)
        => todos.GetTodos();
}
```

```csharp
public sealed record Todo(int Id, string Title, bool IsComplete);

public sealed class TodoService
{
    private readonly List<Todo> _todos =
    [
        new(1, "Map Minimal API route", true),
        new(2, "Add GraphQL endpoint", false)
    ];

    public IReadOnlyList<Todo> GetTodos() => _todos;

    public Todo? GetById(int id) => _todos.FirstOrDefault(t => t.Id == id);
}
```

Now, both the Minimal API handler and the GraphQL resolver share `TodoService`. You don't need separate containers or duplicate registrations.

For more on resolver service injection, see [Dependency Injection](/docs/hotchocolate/v16/resolvers-and-data/dependency-injection/) and [Resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers/).

**Checkpoint:**

- `TodoService` is registered once.
- Both Minimal API handlers and GraphQL resolvers receive it from DI.
- `Properties/ModuleInfo.cs` exists and contains `[assembly: Module("Types")]`.
- The app builds after you add the `[QueryType]` class.

# Map GraphQL as an endpoint

Map GraphQL after `builder.Build()`, alongside your other endpoint mappings.

```csharp
var app = builder.Build();

var api = app.MapGroup("/api");

api.MapGet("/todos", (TodoService todos) => todos.GetTodos());

app.MapGraphQL("/graphql");

app.Run();
```

By default, `MapGraphQL()` uses `/graphql`. Passing the path explicitly makes your route choice clear:

```csharp
app.MapGraphQL("/graphql");
```

Use a custom path if `/graphql` would conflict with an existing route or if your API convention requires a prefix:

```csharp
app.MapGraphQL("/api/graphql");
```

The recommended pattern is to group Minimal API routes under `/api` and map GraphQL separately at `/graphql`:

```csharp
var api = app.MapGroup("/api");

api.MapGet("/todos", (TodoService todos) => todos.GetTodos());

app.MapGraphQL("/graphql");
```

Map GraphQL inside a route group only if every endpoint in that group should share the same prefix, authorization, CORS, filters, and metadata.

```csharp
var api = app.MapGroup("/api");

api.MapGet("/todos", (TodoService todos) => todos.GetTodos());
api.MapGraphQL("/graphql");
```

In this example, the GraphQL endpoint is `/api/graphql`. If Nitro sends requests to a different GraphQL URL in a grouped or proxied app, configure Nitro's endpoint URL to match the public endpoint.

Watch for route conflicts with catch-all routes, fallback endpoints, versioned REST groups, or proxy prefixes. If `/graphql` returns a Minimal API response, static file, or fallback page, move GraphQL to a dedicated path and test the exact public URL.

`MapGraphQL()` also hosts Nitro at the endpoint in development, handles GraphQL HTTP requests, supports WebSocket GraphQL requests (when ASP.NET Core WebSockets are enabled), and can serve schema SDL requests when configured. For more endpoint options, see [Endpoints](/docs/hotchocolate/v16/server/endpoints/) and [HTTP Transport](/docs/hotchocolate/v16/server/http-transport/).

**Checkpoint:**

- `/api/todos` returns the Minimal API response.
- `/graphql` opens Nitro in a browser during local development or accepts a GraphQL POST request.
- No Minimal API route changed path unless you changed it intentionally.

# Keep business logic in services

Put your business logic in application services. Let both Minimal API handlers and GraphQL resolvers call those services directly.

```csharp
api.MapGet("/todos/{id:int}", (int id, TodoService todos) =>
{
    var todo = todos.GetById(id);
    return todo is null ? Results.NotFound() : Results.Ok(todo);
});
```

```csharp
// Types/TodoQueries.cs
[QueryType]
public static partial class TodoQueries
{
    public static Todo? GetTodoById(int id, TodoService todos)
        => todos.GetById(id);
}
```

This approach keeps each API surface focused:

- Minimal API routes handle HTTP-specific results, status codes, route parameters, webhooks, file downloads, health checks, and targeted commands.
- GraphQL fields expose graph-shaped reads, nested data, and client-selected fields.
- Shared services contain validation, data access, and domain logic needed by both surfaces.

Don't turn every existing route into a one-to-one GraphQL field. Design your schema around the data clients need, not the route table. For example, `GET /api/todos/{id}` might map to a `todoById` field, but webhooks, health checks, or file downloads should usually remain Minimal API endpoints.

**Checkpoint:** You can point to the shared service layer and explain why each public contract is Minimal API or GraphQL.

# Apply authentication and authorization consistently

GraphQL uses the same ASP.NET Core authentication and authorization middleware as the rest of your app. Configure the host first, then apply policies to the endpoints that need them.

To use Hot Chocolate's `.AddAuthorization()`, add the `HotChocolate.AspNetCore.Authorization` package. JWT bearer authentication comes from `Microsoft.AspNetCore.Authentication.JwtBearer`. For a full package checklist, see [Packages](/docs/hotchocolate/v16/learn/4-installation-and-setup/packages/).

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://your-identity-provider.example/";
        options.Audience = "todos-api";
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ApiUser", policy => policy.RequireAuthenticatedUser());
});

builder.AddGraphQL()
    .AddAuthorization()
    .AddTypes();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

var api = app.MapGroup("/api")
    .RequireAuthorization("ApiUser");

api.MapGet("/todos", (TodoService todos) => todos.GetTodos());

app.MapGraphQL("/graphql")
    .RequireAuthorization("ApiUser");

app.Run();
```

- Endpoint authorization answers: who can reach this endpoint?
- Field-level GraphQL authorization answers: once a caller reaches GraphQL, which fields or operations can they run?

Use endpoint policies for broad access rules, such as requiring authentication for the entire `/graphql` endpoint. Use Hot Chocolate's field-level authorization when different fields need different roles or policies.

```csharp
using System.Security.Claims;
using HotChocolate.Authorization;
using HotChocolate.Types;

namespace YourApp.Types;

[QueryType]
public static partial class ViewerQueries
{
    [Authorize(Policy = "ApiUser")]
    public static User? GetMe(ClaimsPrincipal user, UserService users)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return userId is null ? null : users.GetById(userId);
    }
}
```

If browser clients call Minimal APIs and GraphQL from different origins, configure CORS carefully. A policy on a route group does not protect or enable a separately mapped `/graphql` endpoint unless you also apply it there or at the app level. See the Microsoft [ASP.NET Core CORS documentation](https://learn.microsoft.com/aspnet/core/security/cors).

Nitro and schema access are also endpoint behaviors. Protect or disable them outside the environments where your team wants them available:

```csharp
app.MapGraphQL("/graphql")
    .WithOptions(o => o.Tool.Enable = app.Environment.IsDevelopment())
    .RequireAuthorization("ApiUser");
```

For full setup, see [Authentication](/docs/hotchocolate/v16/securing-your-api/authentication/) and [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization/).

**Checkpoint:**

- `UseAuthentication()` runs before `UseAuthorization()`.
- Minimal API route groups and `/graphql` have the intended policies.
- Field-level rules are on GraphQL fields that need finer-grained access.
- Anonymous requests receive 401 or 403 as expected.

# Verify your mixed application

Run the app:

```bash
dotnet run
```

Copy the listening URL from the terminal. The port may differ on your machine.

```text
Now listening on: http://localhost:5095
Application started. Press Ctrl+C to shut down.
```

Check the Minimal API route:

```bash
curl http://localhost:5095/api/todos
```

You should see:

```json
[
  {
    "id": 1,
    "title": "Map Minimal API route",
    "isComplete": true
  },
  {
    "id": 2,
    "title": "Add GraphQL endpoint",
    "isComplete": false
  }
]
```

Open Nitro at:

```text
http://localhost:5095/graphql
```

Run a query:

```graphql
query GetTodos {
  todos {
    id
    title
    isComplete
  }
}
```

You should get a GraphQL response like:

```json
{
  "data": {
    "todos": [
      {
        "id": 1,
        "title": "Map Minimal API route",
        "isComplete": true
      },
      {
        "id": 2,
        "title": "Add GraphQL endpoint",
        "isComplete": false
      }
    ]
  }
}
```

If Nitro is disabled or you want to check the endpoint directly, send a POST request:

```bash
curl -X POST http://localhost:5095/graphql \
  -H "Content-Type: application/json" \
  -d '{"query":"query GetTodos { todos { id title isComplete } }"}'
```

**Verification checklist:**

- The app starts without schema construction errors.
- `/api/todos` returns the Minimal API JSON response.
- `/graphql` accepts a GraphQL operation and returns a `data` object.
- Data from shared services is consistent across both surfaces.
- Protected routes reject anonymous requests and allow authenticated requests as intended.

# Troubleshooting

| Symptom | What to check |
| --- | --- |
| `/graphql` returns 404 | Make sure `app.MapGraphQL()` ran, the app restarted, and the URL matches the mapped path. If a proxy adds a base path, test both the public and internal paths. |
| An existing Minimal API route stopped working | Look for route conflicts, catch-all routes, fallback routes, or route group prefixes. Map Minimal APIs under `/api` and GraphQL under `/graphql` for your first integration. |
| A resolver cannot resolve `TodoService` | Register the service in `builder.Services` before `builder.Build()`. Do not create a separate container for GraphQL. |
| Authorization protects `/api` but not `/graphql` | A policy on a Minimal API route group does not apply to GraphQL unless GraphQL is mapped in that group or has its own `.RequireAuthorization(...)` call. |
| CORS works for REST but fails for GraphQL | Apply the CORS policy at the app level or to both the Minimal API group and GraphQL endpoint. Check the client origin, headers, credentials mode, and methods. |
| Nitro sends requests to the wrong URL | Check the GraphQL endpoint path, route group prefix, proxy path base, and Nitro endpoint options. See [Endpoints](/docs/hotchocolate/v16/server/endpoints/) and Microsoft's [proxy and load balancer configuration](https://learn.microsoft.com/aspnet/core/host-and-deploy/proxy-load-balancer). |

# When to split the host

Keep the combined app if the same team, domain model, deployment, security policies, and operational profile fit both Minimal APIs and GraphQL.

Split GraphQL into a separate ASP.NET Core host if:

- GraphQL and Minimal APIs need different scaling behavior
- Separate teams need independent releases
- GraphQL needs different authentication, authorization, CORS, or network boundaries
- The GraphQL server has different infrastructure dependencies
- Operational limits or observability requirements diverge

As an intermediate step, you can keep shared domain and data-access code in class libraries, while hosting Minimal APIs and GraphQL in separate apps. This lets you separate public endpoints without duplicating business logic.

**Checkpoint:** Decide which outcome fits your project:

- **Stay combined for now.** Keep `/api` and `/graphql` in the same host and harden the shared pipeline.
- **Plan a split.** Move GraphQL service registration, schema types, and `MapGraphQL()` into a separate ASP.NET Core host, while sharing application services through packages or projects.

# Next steps

- **Need broader app setup?** See [Existing ASP.NET Core app](/docs/hotchocolate/v16/learn/4-installation-and-setup/existing-aspnet-core-app/).
- **Need baseline ASP.NET Core setup?** See [ASP.NET Core setup](/docs/hotchocolate/v16/learn/4-installation-and-setup/aspnet-core/).
- **Need endpoint options?** See [Endpoints](/docs/hotchocolate/v16/server/endpoints/).
- **Need local tooling guidance?** See [Local development](/docs/hotchocolate/v16/learn/4-installation-and-setup/local-development/).
- **Need security setup?** See [Authentication](/docs/hotchocolate/v16/securing-your-api/authentication/) and [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization/).
- **Translating REST thinking to GraphQL?** See [Coming from REST controllers](/docs/hotchocolate/v16/learn/5-coming-from/rest-controllers/).
