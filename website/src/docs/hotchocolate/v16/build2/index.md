---
title: Build and operate a Hot Chocolate server
---

You have a running GraphQL endpoint. Now shape it into a maintainable production API. This page maps the Hot Chocolate v16 build and operations docs, with a standalone Hot Chocolate server as the main path.

Use it as a navigation hub. Start with the task you need, read the linked child page for details, and return here when the next concern changes.

# Choose your next task

| If you need to...                                | Start with                                                                                                                         | Then read                                                                                                                                                                                                                                                                                                                                                                       |
| ------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Model the API contract                           | [Schema Elements](/docs/hotchocolate/v16/build2/schema-elements) and [Building a Schema](/docs/hotchocolate/v16/building-a-schema) | [Queries](/docs/hotchocolate/v16/building-a-schema/queries), [Mutations](/docs/hotchocolate/v16/building-a-schema/mutations), [Object Types](/docs/hotchocolate/v16/building-a-schema/object-types), [Arguments](/docs/hotchocolate/v16/building-a-schema/arguments), [Versioning](/docs/hotchocolate/v16/building-a-schema/versioning)                                         |
| Fetch data for fields                            | [Resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers)                                                                   | [Dependency Injection](/docs/hotchocolate/v16/resolvers-and-data/dependency-injection), [Fetching from Databases](/docs/hotchocolate/v16/resolvers-and-data/fetching-from-databases), [Fetching from REST](/docs/hotchocolate/v16/resolvers-and-data/fetching-from-rest)                                                                                                        |
| Remove N+1 data access                           | [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader)                                                                 | [Instrumentation](/docs/hotchocolate/v16/server/instrumentation) to verify batching                                                                                                                                                                                                                                                                                             |
| Add paging, filtering, sorting, or projections   | [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination)                                                                 | [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections), [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering), [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting), [Integrations](/docs/hotchocolate/v16/integrations)                                                                                                                       |
| Host and configure the endpoint                  | [Server](/docs/hotchocolate/v16/server)                                                                                            | [Endpoints](/docs/hotchocolate/v16/server/endpoints), [HTTP Transport](/docs/hotchocolate/v16/server/http-transport), [Interceptors](/docs/hotchocolate/v16/server/interceptors), [Global State](/docs/hotchocolate/v16/server/global-state)                                                                                                                                    |
| Prepare startup or deployment workflows          | [Warmup](/docs/hotchocolate/v16/server/warmup)                                                                                     | [Command Line](/docs/hotchocolate/v16/server/command-line) and schema export                                                                                                                                                                                                                                                                                                    |
| Protect a public API                             | [Securing Your API](/docs/hotchocolate/v16/securing-your-api)                                                                      | [Authentication](/docs/hotchocolate/v16/securing-your-api/authentication), [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization), [Request Limits](/docs/hotchocolate/v16/securing-your-api/request-limits), [Cost Analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis), [Introspection](/docs/hotchocolate/v16/securing-your-api/introspection) |
| Protect a private or first-party API             | [Trusted Documents](/docs/hotchocolate/v16/performance/trusted-documents)                                                          | operation storage, hash providers, deployment workflow, [Private API guide](/docs/hotchocolate/v16/guides/private-api)                                                                                                                                                                                                                                                          |
| Improve latency or resource use                  | [Performance](/docs/hotchocolate/v16/performance)                                                                                  | [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader), [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections), [Batching](/docs/hotchocolate/v16/server/batching), [Trusted Documents](/docs/hotchocolate/v16/performance/trusted-documents)                                                                                                         |
| Diagnose requests or errors                      | [Instrumentation](/docs/hotchocolate/v16/server/instrumentation)                                                                   | [Errors](/docs/hotchocolate/v16/api-reference/errors), [Error Handling](/docs/hotchocolate/v16/guides/error-handling), [Execution Engine](/docs/hotchocolate/v16/execution-engine)                                                                                                                                                                                              |
| Understand middleware order or request lifecycle | [Execution Engine](/docs/hotchocolate/v16/execution-engine)                                                                        | [Field Middleware](/docs/hotchocolate/v16/execution-engine/field-middleware), [Options Reference](/docs/hotchocolate/v16/api-reference/options)                                                                                                                                                                                                                                 |
| Evolve or test a schema                          | [Schema Evolution](/docs/hotchocolate/v16/guides/schema-evolution)                                                                 | [Testing](/docs/hotchocolate/v16/guides/testing), [Command Line](/docs/hotchocolate/v16/server/command-line)                                                                                                                                                                                                                                                                    |
| Build a subgraph                                 | [Apollo Federation](/docs/hotchocolate/v16/api-reference/apollo-federation)                                                        | Use [Fusion](/docs/hotchocolate/v16/fusion) only when your architecture needs distributed composition                                                                                                                                                                                                                                                                           |

# Follow the build-to-production path

Most teams move through these concerns in order. Treat the path as a learning map, not a checklist that every API must complete.

```text
Schema contract
    -> Resolvers and data access
        -> Data shaping: paging, filtering, sorting, projections
            -> Server runtime: endpoints, transport, DI, interceptors
                -> Production controls: security, limits, performance, observability
                    -> Advanced extension: execution engine and middleware
                        -> Optional boundary: subgraph or distributed graph
```

| Concern                       | Typical docs path                                                                                                                                                                       |
| ----------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Schema contract               | [Schema Elements](/docs/hotchocolate/v16/build2/schema-elements), [Building a Schema](/docs/hotchocolate/v16/building-a-schema)                                                         |
| Field implementation          | [Resolvers and Data](/docs/hotchocolate/v16/resolvers-and-data)                                                                                                                         |
| Provider-specific data access | [Integrations](/docs/hotchocolate/v16/integrations)                                                                                                                                     |
| Endpoint and transport        | [Server](/docs/hotchocolate/v16/server)                                                                                                                                                 |
| Security controls             | [Securing Your API](/docs/hotchocolate/v16/securing-your-api) plus [Trusted Documents](/docs/hotchocolate/v16/performance/trusted-documents)                                            |
| Performance controls          | [Performance](/docs/hotchocolate/v16/performance), DataLoader, projections, pagination, request limits, batching                                                                        |
| Observability and errors      | [Instrumentation](/docs/hotchocolate/v16/server/instrumentation), [Errors](/docs/hotchocolate/v16/api-reference/errors), [Error Handling](/docs/hotchocolate/v16/guides/error-handling) |
| Internals and extension       | [Execution Engine](/docs/hotchocolate/v16/execution-engine), selected [API Reference](/docs/hotchocolate/v16/api-reference/options) pages                                               |

# Start configuration in Program.cs

Most v16 configuration starts on the request executor builder returned by `AddGraphQL()`. Attach schema types, resolvers, security, performance, instrumentation, `ModifyRequestOptions(...)`, and other options there. Attach ASP.NET Core endpoint behavior through `MapGraphQL()`, specialized endpoint methods, and endpoint `WithOptions(...)`.

```csharp
#nullable enable

var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQL()
    .AddQueryType<Query>();

var app = builder.Build();

app.MapGraphQL();

app.Run();

public sealed class Query
{
    public string Hello() => "world";
}
```

Run this operation:

```graphql
query {
  hello
}
```

Expected result shape:

```json
{
  "data": {
    "hello": "world"
  }
}
```

Expected SDL:

```graphql
type Query {
  hello: String!
}
```

Some existing samples use `AddGraphQLServer()`. For new v16 ASP.NET Core examples, prefer `AddGraphQL()`.

When you use source-generated attributes such as `[QueryType]`, include the generated `.AddTypes()` call in `Program.cs`. Use `.AddQueryType<T>()` when you register an explicit root type.

# Build the schema contract first

The schema is the public contract. Model the operations clients can run, the fields they can select, the arguments they can pass, and the values they receive.

Use implementation-first for the default v16 path. Write C# types and attributes, then inspect the generated SDL. Use code-first descriptor types when you need explicit control over names, fields, directives, or generated infrastructure.

```csharp
[QueryType]
public static partial class BookQueries
{
    public static Task<Book?> GetBookByIdAsync(
        int id,
        IBookRepository books,
        CancellationToken cancellationToken)
        => books.GetByIdAsync(id, cancellationToken);
}
```

When `IBookRepository` is registered in dependency injection, it is injected into the resolver and does not become a GraphQL argument.

Expected SDL shape:

```graphql
type Query {
  bookById(id: Int!): Book
}
```

Use these pages when you model the contract:

| Task                                      | Read next                                                                                                                                                                                                                                                                                                                 |
| ----------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Define read, write, or event entry points | [Queries](/docs/hotchocolate/v16/building-a-schema/queries), [Mutations](/docs/hotchocolate/v16/building-a-schema/mutations), [Subscriptions](/docs/hotchocolate/v16/building-a-schema/subscriptions)                                                                                                                     |
| Model returned data                       | [Object Types](/docs/hotchocolate/v16/building-a-schema/object-types), [Interfaces](/docs/hotchocolate/v16/building-a-schema/interfaces), [Unions](/docs/hotchocolate/v16/building-a-schema/unions), [Enums](/docs/hotchocolate/v16/building-a-schema/enums), [Scalars](/docs/hotchocolate/v16/building-a-schema/scalars) |
| Model field input                         | [Arguments](/docs/hotchocolate/v16/building-a-schema/arguments), [Input Object Types](/docs/hotchocolate/v16/building-a-schema/input-object-types), [Non-Null](/docs/hotchocolate/v16/building-a-schema/non-null), [Lists](/docs/hotchocolate/v16/building-a-schema/lists)                                                |
| Organize and document the schema          | [Extending Types](/docs/hotchocolate/v16/building-a-schema/extending-types), [Directives](/docs/hotchocolate/v16/building-a-schema/directives), [Documentation](/docs/hotchocolate/v16/building-a-schema/documentation)                                                                                                   |
| Add conventions or lifecycle signals      | [Relay](/docs/hotchocolate/v16/building-a-schema/relay), [Versioning](/docs/hotchocolate/v16/building-a-schema/versioning), [Dynamic Schemas](/docs/hotchocolate/v16/building-a-schema/dynamic-schemas)                                                                                                                   |

Schema choices affect authorization, cost analysis, nullability, pagination, and future schema evolution. Check the SDL before you publish a contract.

# Implement fields with resolvers and data access

Resolvers produce field values. They can read from parent objects, call services, load from a database, invoke REST APIs, or delegate batching to DataLoader.

Registered services are injected into resolver parameters automatically. The normal resolver case does not need `[Service]`. Use keyed service guidance when you need a named service. Use `AddApplicationService<T>()` only when schema-level components, such as diagnostic listeners, error filters, or interceptors, need an application service.

```csharp
[QueryType]
public static partial class BookQueries
{
    public static async Task<Book?> GetBookByIdAsync(
        int id,
        IBookByIdDataLoader books,
        CancellationToken cancellationToken)
        => await books.LoadAsync(id, cancellationToken);
}
```

The field stays small. The DataLoader batches all requested book IDs for the current GraphQL request and caches repeated loads during that request.

| Need                           | Read next                                                                                                                                                                        |
| ------------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Understand resolver signatures | [Resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers)                                                                                                                 |
| Inject services correctly      | [Dependency Injection](/docs/hotchocolate/v16/resolvers-and-data/dependency-injection)                                                                                           |
| Batch related data             | [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader)                                                                                                               |
| Connect data stores            | [Fetching from Databases](/docs/hotchocolate/v16/resolvers-and-data/fetching-from-databases), [Fetching from REST](/docs/hotchocolate/v16/resolvers-and-data/fetching-from-rest) |

# Add data shaping features deliberately

Add paging, filtering, sorting, and projections after you know how the field loads data. These features are powerful, but they should match your provider and security model.

```csharp
builder
    .AddGraphQL()
    .AddProjections()
    .AddFiltering()
    .AddSorting();

[QueryType]
public static partial class BookQueries
{
    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Book> GetBooks(BookDbContext db)
        => db.Books;
}
```

Keep the middleware order: `UsePaging` > `UseProjection` > `UseFiltering` > `UseSorting`.

A client can then request a bounded, shaped result:

```graphql
query {
  books(
    first: 10
    where: { title: { contains: "GraphQL" } }
    order: [{ title: ASC }]
  ) {
    nodes {
      title
    }
    pageInfo {
      hasNextPage
    }
  }
}
```

If you use `QueryContext<T>` in v16, do not combine it with `[UseProjection]` on the same field. Pick one projection path for a field.

| Feature                   | Start with                                                           | Provider setup                                                                                                                                                                         |
| ------------------------- | -------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Bounded lists             | [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination)   | provider pages when you leave `IQueryable`                                                                                                                                             |
| Client filters            | [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering)     | [Entity Framework](/docs/hotchocolate/v16/integrations/entity-framework), [MongoDB](/docs/hotchocolate/v16/integrations/mongodb), [Marten](/docs/hotchocolate/v16/integrations/marten) |
| Client ordering           | [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting)         | provider pages                                                                                                                                                                         |
| Database column selection | [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections) | provider pages and projection limitations                                                                                                                                              |

Never expose unbounded collections on a public endpoint. Pair list fields with pagination, request limits, and cost analysis.

# Configure the server runtime

`MapGraphQL()` is the standard endpoint. It handles HTTP GraphQL requests, WebSocket GraphQL requests when ASP.NET Core WebSockets are enabled, schema download, Nitro, multipart uploads, and persisted-operation support where configured.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .ModifyServerOptions(options =>
    {
        options.EnableGetRequests = false;
        options.Batching = AllowedBatching.VariableBatching;
        options.Tool.Enable = builder.Environment.IsDevelopment();
    });

var app = builder.Build();

app.UseWebSockets();

app.MapGraphQL("/graphql")
    .WithOptions(options =>
    {
        options.EnableSchemaRequests = app.Environment.IsDevelopment();
    });

app.Run();
```

Use schema-level `ModifyServerOptions(...)` for defaults that apply across endpoints. Use endpoint `WithOptions(...)` when one mapped endpoint needs different behavior.

Use specialized mappings such as `MapGraphQLHttp()`, `MapGraphQLWebSocket()`, and `MapGraphQLPersistedOperations()` when you need to split HTTP, WebSocket, or persisted-operation endpoints.

| Decision                                                                                   | Where to configure                                        | Read next                                                                                                                                              |
| ------------------------------------------------------------------------------------------ | --------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Which route exposes GraphQL?                                                               | `MapGraphQL("/graphql")`                                  | [Endpoints](/docs/hotchocolate/v16/server/endpoints)                                                                                                   |
| Should GET, schema requests, Nitro, multipart uploads, WebSockets, or batching be enabled? | `ModifyServerOptions(...)` or endpoint `WithOptions(...)` | [Endpoints](/docs/hotchocolate/v16/server/endpoints), [Batching](/docs/hotchocolate/v16/server/batching), [Files](/docs/hotchocolate/v16/server/files) |
| Which HTTP response format should clients request?                                         | HTTP `Accept` header                                      | [HTTP Transport](/docs/hotchocolate/v16/server/http-transport)                                                                                         |
| Do requests need custom state or services?                                                 | interceptors and global state                             | [Interceptors](/docs/hotchocolate/v16/server/interceptors), [Global State](/docs/hotchocolate/v16/server/global-state)                                 |
| Do deployments need schema export?                                                         | warmup and command-line commands                          | [Warmup](/docs/hotchocolate/v16/server/warmup), [Command Line](/docs/hotchocolate/v16/server/command-line)                                             |

Remember these runtime gotchas:

- Batching is disabled by default. Enable the needed `AllowedBatching` flags.
- WebSocket transport requires `app.UseWebSockets()` before `MapGraphQL()`.
- Schema file exposure and introspection policy are separate controls.
- `Accept` negotiation controls standard JSON, multipart responses, Server-Sent Events, and JSON Lines.

# Protect the API before exposing it

Authentication answers who made the request. Authorization answers what that user can access. GraphQL-specific controls answer whether the requested operation is safe to execute.

| API posture                | Main controls                                                                          | Start with                                                                |
| -------------------------- | -------------------------------------------------------------------------------------- | ------------------------------------------------------------------------- |
| Public API                 | authentication, authorization, request limits, cost analysis, introspection policy     | [Securing Your API](/docs/hotchocolate/v16/securing-your-api)             |
| Private or first-party API | trusted documents, operation storage, hash providers, deployment workflow              | [Trusted Documents](/docs/hotchocolate/v16/performance/trusted-documents) |
| Mixed API                  | trusted documents for owned clients, cost controls for arbitrary or partner operations | security and performance pages                                            |

A public API usually needs bounded requests:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddAuthorization()
    .AllowIntrospection(builder.Environment.IsDevelopment())
    .ModifyParserOptions(options =>
    {
        options.MaxAllowedFields = 512;
    })
    .AddMaxExecutionDepthRule(10)
    .ModifyCostOptions(options =>
    {
        options.MaxFieldCost = 1_000;
        options.MaxTypeCost = 1_000;
    });
```

ASP.NET Core authentication and authorization still belong in the host pipeline. The security pages show the full middleware order for authenticated APIs.

A private or first-party API can accept only pre-registered operations:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .UsePersistedOperationPipeline()
    .AddFileSystemOperationDocumentStorage("./persisted_operations")
    .ModifyRequestOptions(options =>
    {
        options.PersistedOperations.OnlyAllowPersistedDocuments = true;
        options.PersistedOperations.AllowDocumentBody = false;
    });

var app = builder.Build();

app.MapGraphQLPersistedOperations();

app.Run();
```

Troubleshoot security from the failing layer outward:

| Symptom                                                      | Check                                                                                                                               |
| ------------------------------------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------- |
| An authenticated user still receives an authorization error. | Confirm ASP.NET Core authentication runs before endpoint execution, then check Hot Chocolate authorization policies and directives. |
| A public endpoint still allows schema inspection.            | Check `AllowIntrospection(...)`, schema file endpoint options, and environment-specific endpoint options.                           |
| A valid query is rejected.                                   | Inspect parser limits, execution depth, cost limits, and pagination bounds with representative client operations.                   |
| Trusted documents work locally but fail after deployment.    | Verify operation storage path, published hashes, hash provider, and deployment artifact contents.                                   |

# Tune performance and startup behavior

Measure first. Instrument the server, send representative operations, and then change one lever at a time.

| Lever                 | What it improves                                    | Read next                                                                                                                                                                                                              |
| --------------------- | --------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| DataLoader            | Fewer data-store calls for nested fields            | [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader)                                                                                                                                                     |
| Projections           | Smaller provider queries                            | [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections)                                                                                                                                                   |
| Pagination and limits | Bounded result size and operation shape             | [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination), [Request Limits](/docs/hotchocolate/v16/securing-your-api/request-limits), [Cost Analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis) |
| Batching              | Fewer network round trips for selected clients      | [Batching](/docs/hotchocolate/v16/server/batching)                                                                                                                                                                     |
| Trusted documents     | Smaller requests and reused parsed operations       | [Trusted Documents](/docs/hotchocolate/v16/performance/trusted-documents), [Automatic Persisted Operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations)                                         |
| Warmup                | Prepared schema and operation caches before traffic | [Warmup](/docs/hotchocolate/v16/server/warmup)                                                                                                                                                                         |

Use warmup tasks and schema export when startup behavior is part of deployment readiness:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddWarmupTask(async (executor, cancellationToken) =>
    {
        await executor.ExecuteAsync("{ __typename }", cancellationToken);
    })
    .ExportSchemaOnStartup("./schema.graphql");
```

Expected deployment outputs can include a warmed executor at startup and a `schema.graphql` file for CI, schema registries, or client checks.

# Observe, diagnose, and test production behavior

Production readiness includes traces, metrics, error behavior, and regression tests. Add instrumentation before tuning, and decide how much exception detail clients should see.

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddInstrumentation()
    .ModifyRequestOptions(options =>
    {
        options.IncludeExceptionDetails = builder.Environment.IsDevelopment();
    });
```

Use this route when something fails:

| Problem                                                       | Go to                                                                                                                                            |
| ------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------ |
| You need request traces, metrics, or diagnostic events.       | [Instrumentation](/docs/hotchocolate/v16/server/instrumentation)                                                                                 |
| A resolver throws, but the client receives a sanitized error. | [Errors](/docs/hotchocolate/v16/api-reference/errors), [Error Handling](/docs/hotchocolate/v16/guides/error-handling)                            |
| No telemetry appears after adding OpenTelemetry.              | [Instrumentation](/docs/hotchocolate/v16/server/instrumentation) setup and package guidance                                                      |
| A request times out or is canceled.                           | [Options Reference](/docs/hotchocolate/v16/api-reference/options), request options, execution engine docs                                        |
| Field middleware runs in an unexpected order.                 | [Execution Engine](/docs/hotchocolate/v16/execution-engine), [Field Middleware](/docs/hotchocolate/v16/execution-engine/field-middleware)        |
| A schema change breaks clients.                               | [Schema Evolution](/docs/hotchocolate/v16/guides/schema-evolution), [Testing](/docs/hotchocolate/v16/guides/testing), command-line schema export |

# Use reference and internals only when needed

Do not start with internals. Reach for them when you extend Hot Chocolate or debug behavior that the task pages do not explain.

| Need                                       | Read                                                                                                                                                                                |
| ------------------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Attribute syntax and custom attributes     | [Custom Attributes](/docs/hotchocolate/v16/api-reference/custom-attributes)                                                                                                         |
| Configuration lookup                       | [Options Reference](/docs/hotchocolate/v16/api-reference/options)                                                                                                                   |
| Error shape and filters                    | [Errors](/docs/hotchocolate/v16/api-reference/errors)                                                                                                                               |
| Executable documents, visitors, or tooling | [Executable](/docs/hotchocolate/v16/api-reference/executable), [Visitors](/docs/hotchocolate/v16/api-reference/visitors), [Language](/docs/hotchocolate/v16/api-reference/language) |
| Custom filtering extensions                | [Extending Filtering](/docs/hotchocolate/v16/api-reference/extending-filtering)                                                                                                     |
| Request lifecycle and middleware order     | [Execution Engine](/docs/hotchocolate/v16/execution-engine), [Field Middleware](/docs/hotchocolate/v16/execution-engine/field-middleware)                                           |

# Evolve and integrate safely

After the core server path is clear, use scenario guides for production planning:

- [Public API guide](/docs/hotchocolate/v16/guides/public-api) for external clients and cost controls.
- [Private API guide](/docs/hotchocolate/v16/guides/private-api) for trusted documents and owned clients.
- [Performance guide](/docs/hotchocolate/v16/guides/performance) for end-to-end tuning.
- [Schema Evolution](/docs/hotchocolate/v16/guides/schema-evolution) for compatible schema changes.
- [Error Handling](/docs/hotchocolate/v16/guides/error-handling) and [Testing](/docs/hotchocolate/v16/guides/testing) for regression prevention.
- [Dynamic Schemas](/docs/hotchocolate/v16/guides/dynamic-schemas), [OpenAPI Adapter](/docs/hotchocolate/v16/guides/openapi-adapter), and [MCP Adapter](/docs/hotchocolate/v16/guides/mcp-adapter) for optional Hot Chocolate scenarios.

# Know when this server becomes a subgraph

A standalone Hot Chocolate server does not need to become a subgraph. Consider that path only when independent services, team boundaries, or graph composition become architectural requirements.

For Apollo Federation-compatible subgraph features in Hot Chocolate, read [Apollo Federation](/docs/hotchocolate/v16/api-reference/apollo-federation). If you move into distributed graph composition, leave this standalone server path and follow the [Fusion](/docs/hotchocolate/v16/fusion) documentation.

# Next steps

- If you are designing a new API, start with [Schema Elements](/docs/hotchocolate/v16/build2/schema-elements).
- If you are implementing data access, continue with [Resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers) and [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader).
- If you are preparing for production, review [Server](/docs/hotchocolate/v16/server), [Securing Your API](/docs/hotchocolate/v16/securing-your-api), [Performance](/docs/hotchocolate/v16/performance), and [Instrumentation](/docs/hotchocolate/v16/server/instrumentation).
