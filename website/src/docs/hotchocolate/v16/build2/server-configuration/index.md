---
title: Server configuration
---

Configure a Hot Chocolate server from the outside in. Start with the ASP.NET Core endpoint that receives requests, then configure the Hot Chocolate executor that builds the schema, creates operation requests, runs middleware, calls resolvers, and writes results.

Use this page as the entry point for v16 server configuration. It gives you a working `Program.cs`, shows the main configuration surfaces, and points you to the child pages for detailed options.

# Start with a minimal server

A small ASP.NET Core app can expose a GraphQL endpoint, a schema, and command-line schema export from one `Program.cs`.

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

Run the server and execute this operation at `/graphql`:

```graphql
query {
  hello
}
```

Expected response shape:

```json
{
  "data": {
    "hello": "world"
  }
}
```

Expected SDL shape:

```graphql
type Query {
  hello: String!
}
```

`builder.AddGraphQL()` is the v16 minimal hosting entry point. You may also see `builder.Services.AddGraphQLServer()` in service-collection based ASP.NET Core setup. Both configure a Hot Chocolate request executor.

`app.MapGraphQL()` maps the combined GraphQL endpoint at `/graphql`. The final line uses `RunWithGraphQLCommandsAsync(args)` so the same app can serve HTTP traffic or run GraphQL CLI commands. The synchronous `RunWithGraphQLCommands(args)` method is also available and returns an exit code in v16.

# Know the two configuration layers

Most server settings belong to one of two layers.

## ASP.NET Core endpoint layer

Endpoint configuration controls how requests enter your application. It covers routes, ASP.NET Core middleware boundaries, protocol endpoints, Nitro, schema download, and endpoint-specific transport options.

Common endpoint APIs:

| API                                     | Default path                                | Use it when you need to                                                        |
| --------------------------------------- | ------------------------------------------- | ------------------------------------------------------------------------------ |
| `app.MapGraphQL()`                      | `/graphql`                                  | Map the combined GraphQL endpoint for HTTP, WebSocket, Nitro, and SDL support. |
| `app.MapGraphQLHttp()`                  | `/graphql`                                  | Map HTTP GraphQL traffic without the combined endpoint.                        |
| `app.MapGraphQLWebSocket()`             | `/graphql/ws`                               | Split WebSocket traffic, often for subscriptions or infrastructure routing.    |
| `app.MapNitroApp()`                     | `/graphql/ui`                               | Host Nitro on a browser URL that differs from the GraphQL endpoint.            |
| `app.MapGraphQLSchema()`                | `/graphql/sdl`                              | Expose SDL download on an explicit route.                                      |
| `app.MapGraphQLSemanticNonNullSchema()` | `/graphql/semantic-non-null-schema.graphql` | Export semantic non-null SDL for clients that consume that shape.              |
| `app.MapGraphQLPersistedOperations()`   | `/graphql/persisted`                        | Serve registered persisted operations through deterministic URLs.              |

Register ASP.NET Core middleware before mapping endpoints when the protocol needs it. For example, WebSocket subscriptions require `UseWebSockets()` before `MapGraphQL()` or `MapGraphQLWebSocket()`.

```csharp
var app = builder.Build();

app.UseWebSockets();

app.MapGraphQL();

return await app.RunWithGraphQLCommandsAsync(args);
```

Authentication, authorization, CORS, header forwarding, and reverse proxy behavior remain ASP.NET Core hosting concerns. Configure those in the ASP.NET Core pipeline, then use Hot Chocolate configuration for GraphQL-specific behavior.

Read next: [Endpoint mapping](/docs/hotchocolate/v16/build2/server-configuration/endpoints), [HTTP transport](/docs/hotchocolate/v16/build2/server-configuration/http-transport), and [WebSockets](/docs/hotchocolate/v16/build2/server-configuration/websockets).

## Hot Chocolate executor layer

Executor configuration controls what happens after Hot Chocolate receives a GraphQL request. It covers schema registration, execution options, parser and validation limits, cost analysis, request middleware, cache control, persisted operations, warmup, instrumentation, and DataLoader integration.

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

Request middleware belongs to the Hot Chocolate execution pipeline, not the ASP.NET Core middleware pipeline. Use execution middleware when you need to inspect or change GraphQL request execution after an operation request exists.

Read next: [Options](/docs/hotchocolate/v16/build2/server-configuration/options), [Request pipeline](/docs/hotchocolate/v16/build2/execution/request-pipeline), [Request limits](/docs/hotchocolate/v16/build2/security/request-limits), and [Cost analysis](/docs/hotchocolate/v16/build2/security/cost-analysis).

# Register schema types at startup

The server cannot execute operations until the schema is registered. For a small API, register an explicit root type.

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>();
```

For implementation-first types that use generated attributes such as `[QueryType]`, include the generated type registration call.

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

Expected SDL shape:

```graphql
type Query {
  bookById(id: Int!): Book!
}

type Book {
  id: Int!
  title: String!
}
```

Read next: [Schema elements](/docs/hotchocolate/v16/build2/schema-elements) and [Resolvers](/docs/hotchocolate/v16/build2/resolvers).

# Configure endpoint behavior deliberately

Use schema-level server options when every endpoint for the schema should share the same transport defaults. Use endpoint `WithOptions(...)` when one route needs different behavior.

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

The schema-level configuration applies to the executor. The endpoint override applies to that mapped route. This lets you keep one default policy and narrow a specific endpoint for development, migration, or infrastructure needs.

Common endpoint settings to review:

| Setting                                   | Default or key behavior          | Why you review it                                                  |
| ----------------------------------------- | -------------------------------- | ------------------------------------------------------------------ |
| `EnableGetRequests`                       | GET requests are enabled.        | Disable or restrict GET if clients do not need it.                 |
| `AllowedGetOperations`                    | Queries are allowed through GET. | Keep mutations off GET unless your HTTP policy supports them.      |
| `EnforceGetRequestsPreflightHeader`       | Disabled by default.             | Enable when you need CSRF protection for GET operations.           |
| `EnableMultipartRequests`                 | Multipart requests are enabled.  | Disable if the API does not support uploads.                       |
| `EnforceMultipartRequestsPreflightHeader` | Enabled by default.              | Keep enabled for upload endpoints.                                 |
| `Batching`                                | `AllowedBatching.None`.          | Enable only the batching mode your clients need.                   |
| `MaxBatchSize`                            | `1024`.                          | Lower it when batching is enabled for public or high-traffic APIs. |
| `MaxConcurrentExecutions`                 | `64`.                            | Tune for throughput and downstream capacity.                       |
| `EnableSchemaRequests`                    | SDL requests are enabled.        | Treat SDL download separately from introspection policy.           |

Read next: [Endpoint mapping](/docs/hotchocolate/v16/build2/server-configuration/endpoints), [Batching](/docs/hotchocolate/v16/build2/server-configuration/batching), and [File uploads](/docs/hotchocolate/v16/build2/server-configuration/file-uploads).

# Choose the transports you need

Hot Chocolate supports HTTP and WebSocket GraphQL traffic. The combined endpoint is enough for many applications. Split endpoints when clients, gateways, reverse proxies, or security policy need separate paths.

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

HTTP clients use POST for GraphQL operations and may use GET for allowed operations. Response formats depend on the `Accept` header. WebSocket clients use the socket endpoint for subscriptions and long-lived operation streams. Multipart requests are the transport path for the `Upload` scalar.

Read next: [HTTP transport](/docs/hotchocolate/v16/build2/server-configuration/http-transport), [WebSockets](/docs/hotchocolate/v16/build2/server-configuration/websockets), and [File uploads](/docs/hotchocolate/v16/build2/server-configuration/file-uploads).

# Use interceptors to bridge hosting and execution

Interceptors are the bridge between protocol data and GraphQL request state. Use them to copy headers, claims, tenant IDs, culture, feature flags, or WebSocket connection payload values into the operation request.

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

Resolvers can read that per-request value through global state.

```csharp
public sealed class Query
{
    public string Tenant([GlobalState("TenantId")] string tenantId)
        => tenantId;
}
```

For WebSockets, use a socket session interceptor to validate the connection initialization message and set state for each operation sent over the connection. Always preserve the default interceptor behavior when you derive from `DefaultHttpRequestInterceptor` or `DefaultSocketSessionInterceptor`, because the default implementation adds services and important request state.

Read next: [Interceptors](/docs/hotchocolate/v16/build2/server-configuration/interceptors) and [Request state](/docs/hotchocolate/v16/build2/server-configuration/request-state).

# Export the schema from the same app

Use the command-line package and run methods when CI, local tooling, or schema registries need SDL from the configured server.

```shell
dotnet run -- schema export --output schema.graphql
```

The command builds the same schema as the server. It returns a nonzero exit code when the command fails, so return the value from `RunWithGraphQLCommandsAsync(args)` or `RunWithGraphQLCommands(args)` in `Program.cs`.

Useful command options:

| Option                | Purpose                                                         |
| --------------------- | --------------------------------------------------------------- |
| `--output`            | Writes SDL to a file instead of standard output.                |
| `--schema-name`       | Exports a named schema when the app hosts more than one.        |
| `--semantic-non-null` | Exports the semantic non-null SDL shape for compatible clients. |

You can also export during startup with `ExportSchemaOnStartup(...)` when the deployment process expects a schema artifact from the app.

Read next: [Command line](/docs/hotchocolate/v16/build2/server-configuration/command-line) and [Warmup](/docs/hotchocolate/v16/build2/server-configuration/warmup).

# Review development conveniences before production

Development settings help you inspect and iterate. Make the production behavior explicit before launch.

| Development need          | Setting or API                                       | Production review                                                                    |
| ------------------------- | ---------------------------------------------------- | ------------------------------------------------------------------------------------ |
| Open Nitro in a browser   | `MapGraphQL()` tool support or `MapNitroApp()`       | Decide whether the browser UI should be exposed.                                     |
| Download SDL through HTTP | `?sdl`, `MapGraphQLSchema()`, `EnableSchemaRequests` | Separate SDL download from introspection policy.                                     |
| Run introspection queries | `AllowIntrospection(...)`                            | Default security disables introspection in production. Configure an explicit policy. |
| See exception details     | request options and error filters                    | Avoid leaking exception details to untrusted clients.                                |
| Tune cost budgets         | `GraphQL-Cost: report`, `ModifyCostOptions(...)`     | Measure representative operations before enforcing budgets.                          |

Keep default security enabled unless you replace it with equivalent controls.

```csharp
builder
    .AddGraphQL(disableDefaultSecurity: false)
    .AddQueryType<Query>();
```

Default security includes production-oriented safeguards such as cost analysis. Disabling it is an advanced choice for applications that configure replacement protections manually.

Read next: [Introspection](/docs/hotchocolate/v16/build2/security/introspection), [Request limits](/docs/hotchocolate/v16/build2/security/request-limits), [Cost analysis](/docs/hotchocolate/v16/build2/security/cost-analysis), and [Trusted documents](/docs/hotchocolate/v16/build2/performance/trusted-documents).

# Prepare the server for production traffic

Use this checklist as a routing guide, not as a full security reference.

| Concern                        | Configure with                                   | Details                                                                                                    |
| ------------------------------ | ------------------------------------------------ | ---------------------------------------------------------------------------------------------------------- |
| Request body size              | `AddGraphQL(maxAllowedRequestSize: ...)`         | [Request limits](/docs/hotchocolate/v16/build2/security/request-limits)                                    |
| Parser limits                  | `ModifyParserOptions(...)`                       | [Request limits](/docs/hotchocolate/v16/build2/security/request-limits)                                    |
| Execution timeout              | `ModifyRequestOptions(...)`                      | [Options](/docs/hotchocolate/v16/build2/server-configuration/options)                                      |
| Cost analysis                  | `ModifyCostOptions(...)`                         | [Cost analysis](/docs/hotchocolate/v16/build2/security/cost-analysis)                                      |
| Persisted or trusted documents | `UsePersistedOperationPipeline(...)`             | [Trusted documents](/docs/hotchocolate/v16/build2/performance/trusted-documents)                           |
| Automatic persisted operations | `UseAutomaticPersistedOperationPipeline(...)`    | [Automatic persisted operations](/docs/hotchocolate/v16/build2/performance/automatic-persisted-operations) |
| Cache headers                  | `UseQueryCache(...)`, `AddCacheControl(...)`     | [Cache control](/docs/hotchocolate/v16/build2/server-configuration/cache-control)                          |
| Startup readiness              | eager initialization, `AddWarmupTask(...)`       | [Warmup](/docs/hotchocolate/v16/build2/server-configuration/warmup)                                        |
| Observability                  | `AddDiagnosticEventListener(...)`, OpenTelemetry | [Instrumentation](/docs/hotchocolate/v16/build2/server-configuration/instrumentation)                      |

In v16, Hot Chocolate initializes the schema eagerly at startup by default. Add warmup tasks when you want to prepare representative operations or caches before the server accepts traffic. Keep diagnostic event handlers fast, because they run synchronously on the execution path.

# Troubleshoot common configuration issues

## Nitro opens but sends requests to the wrong URL

Check the Nitro endpoint options. If Nitro is mapped separately, set `GraphQLEndpoint` to the GraphQL route clients should call. Review reverse proxy path rewriting and `UseBrowserUrlAsGraphQLEndpoint` when the browser URL differs from the backend route.

## WebSocket subscriptions do not connect

Call `app.UseWebSockets()` before mapping GraphQL endpoints. Check the WebSocket path, selected subprotocol, proxy support, connection initialization timeout, and keep-alive settings.

## GET or multipart requests are rejected

Review `EnableGetRequests`, `AllowedGetOperations`, `EnforceGetRequestsPreflightHeader`, `EnableMultipartRequests`, and `EnforceMultipartRequestsPreflightHeader`. A request may be rejected by endpoint options before resolver execution begins.

## Introspection is disabled but SDL download still works

Introspection queries and SDL download endpoints are separate controls. Use `AllowIntrospection(false)` for introspection. Use `EnableSchemaRequests`, `EnableSchemaFileSupport`, or explicit schema endpoint mapping for SDL download behavior.

## The first request or startup takes longer than expected

Hot Chocolate eagerly builds the schema at startup in v16. Warmup tasks can add more startup work. If startup time is more important than first-request latency, review lazy initialization tradeoffs on the warmup page.

## A request is rejected before resolver execution

Check request body size, parser limits, validation limits, cost analysis, persisted document enforcement, batching settings, timeout, and concurrency limits. These controls protect the server before field resolvers run.

# Choose the next page

| If you need to                                             | Read next                                                                                                                                                             |
| ---------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Change paths or split protocol routes                      | [Endpoint mapping](/docs/hotchocolate/v16/build2/server-configuration/endpoints)                                                                                      |
| Tune HTTP formats, GET, POST, SSE, or incremental delivery | [HTTP transport](/docs/hotchocolate/v16/build2/server-configuration/http-transport)                                                                                   |
| Add subscriptions over WebSockets                          | [WebSockets](/docs/hotchocolate/v16/build2/server-configuration/websockets)                                                                                           |
| Copy headers, claims, or tenant IDs into GraphQL requests  | [Interceptors](/docs/hotchocolate/v16/build2/server-configuration/interceptors) and [Request state](/docs/hotchocolate/v16/build2/server-configuration/request-state) |
| Export SDL in CI                                           | [Command line](/docs/hotchocolate/v16/build2/server-configuration/command-line)                                                                                       |
| Configure schema, execution, parser, and server options    | [Options](/docs/hotchocolate/v16/build2/server-configuration/options)                                                                                                 |
| Prepare startup and operation caches                       | [Warmup](/docs/hotchocolate/v16/build2/server-configuration/warmup)                                                                                                   |
| Enable request batching                                    | [Batching](/docs/hotchocolate/v16/build2/server-configuration/batching)                                                                                               |
| Accept file uploads                                        | [File uploads](/docs/hotchocolate/v16/build2/server-configuration/file-uploads)                                                                                       |
| Emit cache headers                                         | [Cache control](/docs/hotchocolate/v16/build2/server-configuration/cache-control)                                                                                     |
| Trace and measure execution                                | [Instrumentation](/docs/hotchocolate/v16/build2/server-configuration/instrumentation)                                                                                 |
