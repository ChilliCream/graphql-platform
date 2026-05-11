---
title: Server configuration
---

# Server Configuration

This page guides you through configuring a Hot Chocolate server, starting from the ASP.NET Core endpoint that receives requests, and moving inward to the Hot Chocolate executor, which builds the schema, processes operation requests, runs middleware, calls resolvers, and writes results.

Use this page as your starting point for server configuration. Here, you'll find a working `Program.cs`, an overview of the main configuration areas, and links to detailed child pages for further options.

## Start with a Minimal Server

A minimal ASP.NET Core application can expose a GraphQL endpoint, define a schema, and support command-line schema export, all from a single `Program.cs` file.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQL()
    .AddQueryType<Query>();

var app = builder.Build();

app.MapGraphQL();

return await app.RunWithGraphQLCommandsAsync(args);

public sealed class Query
{
    public string Hello() => "world";
}
```

After starting the server, you can send the following operation to `/graphql`:

```graphql
query {
  hello
}
```

You should receive a response like this:

```json
{
  "data": {
    "hello": "world"
  }
}
```

The expected SDL for this schema is:

```graphql
type Query {
  hello: String!
}
```

The method `builder.AddGraphQL()` is the minimal hosting entry point. In some setups, you might see `builder.Services.AddGraphQLServer()` used with the service collection. Both approaches configure a Hot Chocolate request executor.

The call to `app.MapGraphQL()` maps the combined GraphQL endpoint at `/graphql`. The final line, `RunWithGraphQLCommandsAsync(args)`, allows the same application to serve HTTP traffic or handle GraphQL CLI commands. If you prefer a synchronous method, `RunWithGraphQLCommands(args)` is also available and returns an exit code.

## Understand the Two Configuration Layers

Most server settings fall into one of two main layers:

### ASP.NET Core Endpoint Layer

This layer manages how requests enter your application. It includes route mapping, ASP.NET Core middleware, protocol endpoints, Nitro, schema download, and endpoint-specific transport settings.

Common endpoint APIs include:

| API                                     | Default Path                                | When to Use                                                                    |
| --------------------------------------- | ------------------------------------------- | ------------------------------------------------------------------------------ |
| `app.MapGraphQL()`                      | `/graphql`                                  | Map the combined GraphQL endpoint for HTTP, WebSocket, Nitro, and SDL support. |
| `app.MapGraphQLHttp()`                  | `/graphql`                                  | Map HTTP GraphQL traffic without the combined endpoint.                        |
| `app.MapGraphQLWebSocket()`             | `/graphql/ws`                               | Separate WebSocket traffic, often for subscriptions or infrastructure routing. |
| `app.MapNitroApp()`                     | `/graphql/ui`                               | Host Nitro on a browser URL different from the GraphQL endpoint.               |
| `app.MapGraphQLSchema()`                | `/graphql/sdl`                              | Expose SDL download on a dedicated route.                                      |
| `app.MapGraphQLSemanticNonNullSchema()` | `/graphql/semantic-non-null-schema.graphql` | Export semantic non-null SDL for compatible clients.                           |
| `app.MapGraphQLPersistedOperations()`   | `/graphql/persisted`                        | Serve registered persisted operations through deterministic URLs.              |

If a protocol requires it, register ASP.NET Core middleware before mapping endpoints. For example, WebSocket subscriptions need `UseWebSockets()` before `MapGraphQL()` or `MapGraphQLWebSocket()`:

```csharp
var app = builder.Build();

app.UseWebSockets();

app.MapGraphQL();

return await app.RunWithGraphQLCommandsAsync(args);
```

Features like authentication, authorization, CORS, header forwarding, and reverse proxy behavior are handled by ASP.NET Core. Configure these in the ASP.NET Core pipeline, then use Hot Chocolate configuration for GraphQL-specific options.

Read more: [Endpoint mapping](/docs/hotchocolate/v16/build/server-configuration/endpoints), [HTTP transport](/docs/hotchocolate/v16/build/server-configuration/http-transport), [WebSockets](/docs/hotchocolate/v16/build/server-configuration/websocket-transport).

### Hot Chocolate Executor Layer

This layer controls what happens after Hot Chocolate receives a GraphQL request. It covers schema registration, execution options, parser and validation limits, cost analysis, request middleware, cache control, persisted operations, warmup, instrumentation, and DataLoader integration.

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .ModifyOptions(options =>
    {
        options.EnableOneOf = true;
    })
    .ModifyRequestOptions(options =>
    {
        options.ExecutionTimeout = TimeSpan.FromSeconds(10);
    })
    .ModifyParserOptions(options =>
    {
        options.MaxAllowedFields = 1_000;
    });
```

Request middleware is part of the Hot Chocolate execution pipeline, not the ASP.NET Core middleware pipeline. Use execution middleware when you need to inspect or modify GraphQL request execution after the operation request has been created.

Read more: [Options](/docs/hotchocolate/v16/build/server-configuration/schema-options), [Request pipeline](/docs/hotchocolate/v16/build/execution-engine/pipeline), [Request limits](/docs/hotchocolate/v16/build/security/execution-depth-and-limits), [Cost analysis](/docs/hotchocolate/v16/build/security/cost-analysis).

## Register Schema Types at Startup

The server cannot execute operations until the schema is registered. For a small API, register an explicit root type:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>();
```

If you use implementation-first types with generated attributes like `[QueryType]`, include the generated type registration call:

```csharp
builder
    .AddGraphQL()
    .AddTypes();

[QueryType]
public static partial class BookQueries
{
    public static Book GetBookById(int id)
        => new(id, "GraphQL in Action");
}

public sealed record Book(int Id, string Title);
```

The expected SDL for this schema is:

```graphql
type Query {
  bookById(id: Int!): Book!
}

type Book {
  id: Int!
  title: String!
}
```

Read more: [Type system](/docs/hotchocolate/v16/build/type-system), [Resolvers](/docs/hotchocolate/v16/build/resolvers).

## Configure Endpoint Behavior Deliberately

Use schema-level server options when all endpoints for a schema should share the same transport defaults. If a specific route requires different behavior, use the endpoint's `WithOptions(...)` method.

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .ModifyServerOptions(options =>
    {
        options.EnableGetRequests = true;
        options.AllowedGetOperations = AllowedGetOperations.Query;
        options.Batching = AllowedBatching.None;
    });

var app = builder.Build();

app.MapGraphQL()
    .WithOptions(options =>
    {
        options.EnableSchemaRequests = app.Environment.IsDevelopment();
    });
```

Schema-level configuration applies to the executor, while endpoint overrides apply only to the mapped route. This approach allows you to maintain a default policy and modify specific endpoints for development, migration, or infrastructure needs.

Common endpoint settings to review:

| Setting                                   | Default or Key Behavior          | Why Review It                                                    |
| ----------------------------------------- | -------------------------------- | ---------------------------------------------------------------- |
| `EnableGetRequests`                       | GET requests are enabled.        | Disable or restrict GET if clients do not require it.            |
| `AllowedGetOperations`                    | Queries are allowed through GET. | Avoid allowing mutations via GET unless your HTTP policy allows. |
| `EnforceGetRequestsPreflightHeader`       | Disabled by default.             | Enable for CSRF protection on GET operations.                    |
| `EnableMultipartRequests`                 | Multipart requests are enabled.  | Disable if uploads are not supported.                            |
| `EnforceMultipartRequestsPreflightHeader` | Enabled by default.              | Keep enabled for upload endpoints.                               |
| `Batching`                                | `AllowedBatching.None`.          | Enable only the batching mode your clients need.                 |
| `MaxBatchSize`                            | `1024`.                          | Lower for public or high-traffic APIs if batching is enabled.    |
| `MaxConcurrentExecutions`                 | `64`.                            | Tune for throughput and downstream capacity.                     |
| `EnableSchemaRequests`                    | SDL requests are enabled.        | Manage SDL download separately from introspection policy.        |

Read more: [Endpoint mapping](/docs/hotchocolate/v16/build/server-configuration/endpoints), [Batching](/docs/hotchocolate/v16/build/performance/batching), [File uploads](/docs/hotchocolate/v16/_leagcy/server/files).

## Choose the Transports You Need

Hot Chocolate supports both HTTP and WebSocket GraphQL traffic. For many applications, the combined endpoint is sufficient. However, you can split endpoints if clients, gateways, reverse proxies, or security policies require separate paths.

```csharp
var app = builder.Build();

app.UseWebSockets();

app.MapGraphQLHttp("/graphql");
app.MapGraphQLWebSocket("/graphql/ws");
app.MapNitroApp("/graphql/ui")
    .WithOptions(options =>
    {
        options.GraphQLEndpoint = "/graphql";
    });

return await app.RunWithGraphQLCommandsAsync(args);
```

HTTP clients typically use POST for GraphQL operations, and may use GET for operations that allow it. The response format depends on the `Accept` header. WebSocket clients connect to the socket endpoint for subscriptions and long-lived operation streams. Multipart requests are used for the `Upload` scalar.

Read more: [HTTP transport](/docs/hotchocolate/v16/build/server-configuration/http-transport), [WebSockets](/docs/hotchocolate/v16/build/server-configuration/websocket-transport), [File uploads](/docs/hotchocolate/v16/_leagcy/server/files).

## Use Interceptors to Bridge Hosting and Execution

Interceptors connect protocol data with GraphQL request state. Use them to transfer headers, claims, tenant IDs, culture, feature flags, or WebSocket connection payload values into the operation request.

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddHttpRequestInterceptor(
        (context, executor, requestBuilder, cancellationToken) =>
        {
            var tenantId = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                requestBuilder.SetProperty("TenantId", tenantId);
            }

            return ValueTask.CompletedTask;
        });
```

Resolvers can access these per-request values through global state:

```csharp
public sealed class Query
{
    public string Tenant([GlobalState("TenantId")] string tenantId)
        => tenantId;
}
```

For WebSockets, use a socket session interceptor to validate the connection initialization message and set state for each operation sent over the connection. If you derive from `DefaultHttpRequestInterceptor` or `DefaultSocketSessionInterceptor`, always preserve the default behavior, as it adds services and important request state.

Read more: [Interceptors](/docs/hotchocolate/v16/build/server-configuration/interceptors), [Request state](/docs/hotchocolate/v16/build/server-configuration/global-state).

## Export the Schema from the Same App

When CI, local tooling, or schema registries require SDL from your configured server, use the command-line package and run methods:

```shell
dotnet run -- schema export --output schema.graphql
```

This command builds the same schema as the server. It returns a nonzero exit code if the command fails, so ensure you return the value from `RunWithGraphQLCommandsAsync(args)` or `RunWithGraphQLCommands(args)` in `Program.cs`.

Useful command options:

| Option                | Purpose                                                         |
| --------------------- | --------------------------------------------------------------- |
| `--output`            | Writes SDL to a file instead of standard output.                |
| `--schema-name`       | Exports a named schema when the app hosts more than one.        |
| `--semantic-non-null` | Exports the semantic non-null SDL shape for compatible clients. |

You can also export the schema during startup with `ExportSchemaOnStartup(...)` if your deployment process expects a schema artifact from the app.

Read more: [Command line](/docs/hotchocolate/v16/build/server-configuration/command-line), [Warmup](/docs/hotchocolate/v16/build/performance/warmup).

## Review Development Conveniences Before Production

Development settings help you inspect and iterate, but make sure to set explicit production behavior before launch.

| Development Need          | Setting or API                                       | Production Review                                                              |
| ------------------------- | ---------------------------------------------------- | ------------------------------------------------------------------------------ |
| Open Nitro in a browser   | `MapGraphQL()` tool support or `MapNitroApp()`       | Decide whether the browser UI should be exposed.                               |
| Download SDL via HTTP     | `?sdl`, `MapGraphQLSchema()`, `EnableSchemaRequests` | Separate SDL download from introspection policy.                               |
| Run introspection queries | `AllowIntrospection(...)`                            | Default security disables introspection in production. Set an explicit policy. |
| See exception details     | Request options and error filters                    | Avoid exposing exception details to untrusted clients.                         |
| Tune cost budgets         | `GraphQL-Cost: report`, `ModifyCostOptions(...)`     | Measure representative operations before enforcing budgets.                    |

Keep default security enabled unless you have equivalent controls in place.

```csharp
builder
    .AddGraphQL(disableDefaultSecurity: false)
    .AddQueryType<Query>();
```

Default security includes production-oriented safeguards such as cost analysis. Disabling it is only recommended if you have configured alternative protections manually.

Read more: [Introspection](/docs/hotchocolate/v16/build/security/introspection), [Request limits](/docs/hotchocolate/v16/build/security/execution-depth-and-limits), [Cost analysis](/docs/hotchocolate/v16/build/security/cost-analysis), [Trusted documents](/docs/hotchocolate/v16/build/security/trusted-documents).

## Prepare the Server for Production Traffic

Use this checklist as a routing guide, not as a complete security reference.

| Concern                        | Configure With                                   | Details                                                                                                   |
| ------------------------------ | ------------------------------------------------ | --------------------------------------------------------------------------------------------------------- |
| Request body size              | `AddGraphQL(maxAllowedRequestSize: ...)`         | [Request limits](/docs/hotchocolate/v16/build/security/execution-depth-and-limits)                        |
| Parser limits                  | `ModifyParserOptions(...)`                       | [Request limits](/docs/hotchocolate/v16/build/security/execution-depth-and-limits)                        |
| Execution timeout              | `ModifyRequestOptions(...)`                      | [Options](/docs/hotchocolate/v16/build/server-configuration/schema-options)                               |
| Cost analysis                  | `ModifyCostOptions(...)`                         | [Cost analysis](/docs/hotchocolate/v16/build/security/cost-analysis)                                      |
| Persisted or trusted documents | `UsePersistedOperationPipeline(...)`             | [Trusted documents](/docs/hotchocolate/v16/build/security/trusted-documents)                              |
| Automatic persisted operations | `UseAutomaticPersistedOperationPipeline(...)`    | [Automatic persisted operations](/docs/hotchocolate/v16/build/performance/automatic-persisted-operations) |
| Cache headers                  | `UseQueryCache(...)`, `AddCacheControl(...)`     | [Cache control](/docs/hotchocolate/v16/build/performance/cache-control)                                   |
| Startup readiness              | Eager initialization, `AddWarmupTask(...)`       | [Warmup](/docs/hotchocolate/v16/build/performance/warmup)                                                 |
| Observability                  | `AddDiagnosticEventListener(...)`, OpenTelemetry | [Instrumentation](/docs/hotchocolate/v16/build/observability)                                             |

Hot Chocolate eagerly initializes the schema at startup by default. Add warmup tasks if you want to prepare representative operations or caches before the server accepts traffic. Keep diagnostic event handlers efficient, as they run synchronously on the execution path.

## Troubleshoot Common Configuration Issues

### Nitro opens but sends requests to the wrong URL

Check the Nitro endpoint options. If Nitro is mapped separately, set `GraphQLEndpoint` to the correct GraphQL route for clients. Also review reverse proxy path rewriting and `UseBrowserUrlAsGraphQLEndpoint` if the browser URL differs from the backend route.

### WebSocket subscriptions do not connect

Ensure you call `app.UseWebSockets()` before mapping GraphQL endpoints. Also check the WebSocket path, selected subprotocol, proxy support, connection initialization timeout, and keep-alive settings.

### GET or multipart requests are rejected

Review the settings for `EnableGetRequests`, `AllowedGetOperations`, `EnforceGetRequestsPreflightHeader`, `EnableMultipartRequests`, and `EnforceMultipartRequestsPreflightHeader`. Endpoint options may reject a request before resolver execution begins.

### Introspection is disabled but SDL download still works

Introspection queries and SDL download endpoints are controlled separately. Use `AllowIntrospection(false)` to disable introspection. Use `EnableSchemaRequests`, `EnableSchemaFileSupport`, or explicit schema endpoint mapping to manage SDL download behavior.

### The first request or startup takes longer than expected

Hot Chocolate builds the schema eagerly at startup, and warmup tasks can add to startup time. If startup time is more important than first-request latency, review lazy initialization tradeoffs on the warmup page.

### A request is rejected before resolver execution

Check request body size, parser limits, validation limits, cost analysis, persisted document enforcement, batching settings, timeout, and concurrency limits. These controls protect the server before field resolvers run.

# Next Steps

| If you need to                                             | Read Next                                                                                                                                                          |
| ---------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Change paths or split protocol routes                      | [Endpoint mapping](/docs/hotchocolate/v16/build/server-configuration/endpoints)                                                                                    |
| Tune HTTP formats, GET, POST, SSE, or incremental delivery | [HTTP transport](/docs/hotchocolate/v16/build/server-configuration/http-transport)                                                                                 |
| Add subscriptions over WebSockets                          | [WebSockets](/docs/hotchocolate/v16/build/server-configuration/websocket-transport)                                                                                |
| Copy headers, claims, or tenant IDs into GraphQL requests  | [Interceptors](/docs/hotchocolate/v16/build/server-configuration/interceptors) and [Request state](/docs/hotchocolate/v16/build/server-configuration/global-state) |
| Export SDL in CI                                           | [Command line](/docs/hotchocolate/v16/build/server-configuration/command-line)                                                                                     |
| Configure schema, execution, parser, and server options    | [Options](/docs/hotchocolate/v16/build/server-configuration/schema-options)                                                                                        |
| Prepare startup and operation caches                       | [Warmup](/docs/hotchocolate/v16/build/performance/warmup)                                                                                                          |
| Enable request batching                                    | [Batching](/docs/hotchocolate/v16/build/performance/batching)                                                                                                      |
| Accept file uploads                                        | [File uploads](/docs/hotchocolate/v16/_leagcy/server/files)                                                                                                        |
| Emit cache headers                                         | [Cache control](/docs/hotchocolate/v16/build/performance/cache-control)                                                                                            |
| Trace and measure execution                                | [Instrumentation](/docs/hotchocolate/v16/build/observability)                                                                                                      |
