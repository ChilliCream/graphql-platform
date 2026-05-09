---
title: Build and operate a Hot Chocolate server
---

You have a running GraphQL endpoint. Now it is time to shape it into a maintainable production API. This page provides an overview of the Hot Chocolate v16 build and operations documentation, focusing on the standalone Hot Chocolate server path.

Use this page as your navigation hub. Begin with the task you want to accomplish, follow the linked child page for details, and return here when you need to address a new concern.

# Choose your next task

| If you need to...                                | Start with                                                                                                                            | Then read                                                                                                                                                                                                                                                                                                                                                                             |
| ------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Model the API contract                           | [Schema Elements](/docs/hotchocolate/v16/build/schema-elements) and [Building a Schema](/docs/hotchocolate/v16/build/schema-elements) | [Queries](/docs/hotchocolate/v16/build/schema-elements/operations-queries), [Mutations](/docs/hotchocolate/v16/build/schema-elements/operations-mutations), [Object Types](/docs/hotchocolate/v16/build/schema-elements/object-types), [Arguments](/docs/hotchocolate/v16/build/schema-elements/arguments), [Versioning](/docs/hotchocolate/v16/_leagcy/building-a-schema/versioning) |
| Fetch data for fields                            | [Resolvers](/docs/hotchocolate/v16/build/resolvers)                                                                                   | [Dependency Injection](/docs/hotchocolate/v16/build/resolvers/service-injection), [Fetching from Databases](/docs/hotchocolate/v16/_leagcy/resolvers-and-data/fetching-from-databases), [Fetching from REST](/docs/hotchocolate/v16/_leagcy/resolvers-and-data/fetching-from-rest)                                                                                                    |
| Remove N+1 data access                           | [DataLoader](/docs/hotchocolate/v16/build/dataloader)                                                                                 | [Instrumentation](/docs/hotchocolate/v16/build/observability) to verify batching                                                                                                                                                                                                                                                                                                      |
| Add paging, filtering, sorting, or projections   | [Pagination](/docs/hotchocolate/v16/build/pagination)                                                                                 | [Projections](/docs/hotchocolate/v16/build/filtering-sorting-projections/projection-options), [Filtering](/docs/hotchocolate/v16/build/filtering-sorting-projections/filter-types), [Sorting](/docs/hotchocolate/v16/build/filtering-sorting-projections/sort-types), [Integrations](/docs/hotchocolate/v16/_leagcy/integrations)                                                     |
| Host and configure the endpoint                  | [Server](/docs/hotchocolate/v16/build/server-configuration)                                                                           | [Endpoints](/docs/hotchocolate/v16/build/server-configuration/endpoints), [HTTP Transport](/docs/hotchocolate/v16/build/server-configuration/http-transport), [Interceptors](/docs/hotchocolate/v16/build/server-configuration/interceptors), [Global State](/docs/hotchocolate/v16/build/server-configuration/global-state)                                                          |
| Prepare startup or deployment workflows          | [Warmup](/docs/hotchocolate/v16/build/performance/warmup)                                                                             | [Command Line](/docs/hotchocolate/v16/build/server-configuration/command-line) and schema export                                                                                                                                                                                                                                                                                      |
| Protect a public API                             | [Securing Your API](/docs/hotchocolate/v16/build/security)                                                                            | [Authentication](/docs/hotchocolate/v16/build/security/authentication), [Authorization](/docs/hotchocolate/v16/build/security/authorization), [Request Limits](/docs/hotchocolate/v16/build/security/execution-depth-and-limits), [Cost Analysis](/docs/hotchocolate/v16/build/security/cost-analysis), [Introspection](/docs/hotchocolate/v16/build/security/introspection)          |
| Protect a private or first-party API             | [Trusted Documents](/docs/hotchocolate/v16/build/security/trusted-documents)                                                          | operation storage, hash providers, deployment workflow, [Private API guide](/docs/hotchocolate/v16/_leagcy/guides/private-api)                                                                                                                                                                                                                                                        |
| Improve latency or resource use                  | [Performance](/docs/hotchocolate/v16/build/performance)                                                                               | [DataLoader](/docs/hotchocolate/v16/build/dataloader), [Projections](/docs/hotchocolate/v16/build/filtering-sorting-projections/projection-options), [Batching](/docs/hotchocolate/v16/build/performance/batching), [Trusted Documents](/docs/hotchocolate/v16/build/security/trusted-documents)                                                                                      |
| Diagnose requests or errors                      | [Instrumentation](/docs/hotchocolate/v16/build/observability)                                                                         | [Errors](/docs/hotchocolate/v16/build/errors), [Error Handling](/docs/hotchocolate/v16/_leagcy/guides/error-handling), [Execution Engine](/docs/hotchocolate/v16/build/execution-engine)                                                                                                                                                                                              |
| Understand middleware order or request lifecycle | [Execution Engine](/docs/hotchocolate/v16/build/execution-engine)                                                                     | [Field Middleware](/docs/hotchocolate/v16/build/execution-engine/field-middleware), [Options Reference](/docs/hotchocolate/v16/build/server-configuration/schema-options)                                                                                                                                                                                                             |
| Evolve or test a schema                          | [Schema Evolution](/docs/hotchocolate/v16/_leagcy/guides/schema-evolution)                                                            | [Testing](/docs/hotchocolate/v16/_leagcy/guides/testing), [Command Line](/docs/hotchocolate/v16/build/server-configuration/command-line)                                                                                                                                                                                                                                              |
| Build a subgraph                                 | [Apollo Federation](/docs/hotchocolate/v16/build/fusion-subgraph/apollo-federation)                                                   | Use [Fusion](/docs/hotchocolate/v16/_leagcy/fusion) only when your architecture requires distributed composition                                                                                                                                                                                                                                                                      |

# Follow the build-to-production path

Most teams address these concerns in sequence. Treat this path as a learning map rather than a checklist that every API must complete.

```text
Schema contract
    -> Resolvers and data access
        -> Data shaping: paging, filtering, sorting, projections
            -> Server runtime: endpoints, transport, DI, interceptors
                -> Production controls: security, limits, performance, observability
                    -> Advanced extension: execution engine and middleware
                        -> Optional boundary: subgraph or distributed graph
```

| Concern                       | Typical docs path                                                                                                                                                                    |
| ----------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Schema contract               | [Schema Elements](/docs/hotchocolate/v16/build/schema-elements), [Building a Schema](/docs/hotchocolate/v16/build/schema-elements)                                                   |
| Field implementation          | [Resolvers and Data](/docs/hotchocolate/v16/build/resolvers)                                                                                                                         |
| Provider-specific data access | [Integrations](/docs/hotchocolate/v16/_leagcy/integrations)                                                                                                                          |
| Endpoint and transport        | [Server](/docs/hotchocolate/v16/build/server-configuration)                                                                                                                          |
| Security controls             | [Securing Your API](/docs/hotchocolate/v16/build/security) plus [Trusted Documents](/docs/hotchocolate/v16/build/security/trusted-documents)                                         |
| Performance controls          | [Performance](/docs/hotchocolate/v16/build/performance), DataLoader, projections, pagination, request limits, batching                                                               |
| Observability and errors      | [Instrumentation](/docs/hotchocolate/v16/build/observability), [Errors](/docs/hotchocolate/v16/build/errors), [Error Handling](/docs/hotchocolate/v16/_leagcy/guides/error-handling) |
| Internals and extension       | [Execution Engine](/docs/hotchocolate/v16/build/execution-engine), selected [API Reference](/docs/hotchocolate/v16/build/server-configuration/schema-options) pages                  |

# Start configuration in Program.cs

In v16, most configuration begins with the request executor builder returned by `AddGraphQL()`. Here, you attach schema types, resolvers, security, performance, instrumentation, `ModifyRequestOptions(...)`, and other options. Use `MapGraphQL()`, specialized endpoint methods, and endpoint `WithOptions(...)` to configure ASP.NET Core endpoint behavior.

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

You can run this operation:

```graphql
query {
  hello
}
```

The expected result is:

```json
{
  "data": {
    "hello": "world"
  }
}
```

The expected SDL is:

```graphql
type Query {
  hello: String!
}
```

Some older samples use `AddGraphQLServer()`. For new v16 ASP.NET Core projects, use `AddGraphQL()`.

If you use source-generated attributes like `[QueryType]`, include the generated `.AddTypes()` call in `Program.cs`. Use `.AddQueryType<T>()` when registering an explicit root type.

# Build the schema contract first

The schema defines your public contract. It models the operations clients can run, the fields they can select, the arguments they can pass, and the values they receive.

The default v16 approach is implementation-first: write C# types and attributes, then inspect the generated SDL. Use code-first descriptor types when you need precise control over names, fields, directives, or generated infrastructure.

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

When `IBookRepository` is registered with dependency injection, it is injected into the resolver and does not appear as a GraphQL argument.

The expected SDL shape is:

```graphql
type Query {
  bookById(id: Int!): Book
}
```

Refer to these pages as you model the contract:

| Task                                      | Read next                                                                                                                                                                                                                                                                                                                                     |
| ----------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Define read, write, or event entry points | [Queries](/docs/hotchocolate/v16/build/schema-elements/operations-queries), [Mutations](/docs/hotchocolate/v16/build/schema-elements/operations-mutations), [Subscriptions](/docs/hotchocolate/v16/build/schema-elements/operations-subscriptions)                                                                                            |
| Model returned data                       | [Object Types](/docs/hotchocolate/v16/build/schema-elements/object-types), [Interfaces](/docs/hotchocolate/v16/build/schema-elements/interfaces), [Unions](/docs/hotchocolate/v16/build/schema-elements/unions), [Enums](/docs/hotchocolate/v16/build/schema-elements/enums), [Scalars](/docs/hotchocolate/v16/build/schema-elements/scalars) |
| Model field input                         | [Arguments](/docs/hotchocolate/v16/build/schema-elements/arguments), [Input Object Types](/docs/hotchocolate/v16/build/schema-elements/input-object-types), [Non-Null](/docs/hotchocolate/v16/build/schema-elements/lists-and-non-null), [Lists](/docs/hotchocolate/v16/build/schema-elements/lists-and-non-null)                             |
| Organize and document the schema          | [Extending Types](/docs/hotchocolate/v16/build/schema-elements/extending-types), [Directives](/docs/hotchocolate/v16/build/schema-elements/directives), [Documentation](/docs/hotchocolate/v16/build/schema-elements/documentation-comments)                                                                                                  |
| Add conventions or lifecycle signals      | [Relay](/docs/hotchocolate/v16/build/schema-elements/relay), [Versioning](/docs/hotchocolate/v16/_leagcy/building-a-schema/versioning), [Dynamic Schemas](/docs/hotchocolate/v16/build/schema-elements/dynamic-schemas)                                                                                                                       |

Your schema choices influence authorization, cost analysis, nullability, pagination, and future schema evolution. Always check the SDL before publishing a contract.

# Implement fields with resolvers and data access

Resolvers are responsible for producing field values. They can read from parent objects, call services, load data from a database, invoke REST APIs, or delegate batching to DataLoader.

Services registered with dependency injection are automatically injected into resolver parameters. Most resolvers do not require the `[Service]` attribute. Use keyed service guidance for named services. Use `AddApplicationService<T>()` only for schema-level components such as diagnostic listeners, error filters, or interceptors.

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

This approach keeps the field implementation concise. DataLoader batches all requested book IDs for the current GraphQL request and caches repeated loads during that request.

| Need                           | Read next                                                                                                                                                                                        |
| ------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Understand resolver signatures | [Resolvers](/docs/hotchocolate/v16/build/resolvers)                                                                                                                                              |
| Inject services correctly      | [Dependency Injection](/docs/hotchocolate/v16/build/resolvers/service-injection)                                                                                                                 |
| Batch related data             | [DataLoader](/docs/hotchocolate/v16/build/dataloader)                                                                                                                                            |
| Connect data stores            | [Fetching from Databases](/docs/hotchocolate/v16/_leagcy/resolvers-and-data/fetching-from-databases), [Fetching from REST](/docs/hotchocolate/v16/_leagcy/resolvers-and-data/fetching-from-rest) |

# Add data shaping features deliberately

Introduce paging, filtering, sorting, and projections after you have established how the field loads its data. These features are powerful, but they should align with your data provider and security model.

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

Maintain the middleware order: `UsePaging` > `UseProjection` > `UseFiltering` > `UseSorting`.

Clients can then request a bounded, shaped result:

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

If you use `QueryContext<T>` in v16, do not combine it with `[UseProjection]` on the same field. Choose one projection approach per field.

| Feature                   | Start with                                                                                   | Provider setup                                                                                                                                                                                                 |
| ------------------------- | -------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Bounded lists             | [Pagination](/docs/hotchocolate/v16/build/pagination)                                        | provider pages when you leave `IQueryable`                                                                                                                                                                     |
| Client filters            | [Filtering](/docs/hotchocolate/v16/build/filtering-sorting-projections/filter-types)         | [Entity Framework](/docs/hotchocolate/v16/_leagcy/integrations/entity-framework), [MongoDB](/docs/hotchocolate/v16/_leagcy/integrations/mongodb), [Marten](/docs/hotchocolate/v16/_leagcy/integrations/marten) |
| Client ordering           | [Sorting](/docs/hotchocolate/v16/build/filtering-sorting-projections/sort-types)             | provider pages                                                                                                                                                                                                 |
| Database column selection | [Projections](/docs/hotchocolate/v16/build/filtering-sorting-projections/projection-options) | provider pages and projection limitations                                                                                                                                                                      |

Never expose unbounded collections on a public endpoint. Always pair list fields with pagination, request limits, and cost analysis.

# Configure the server runtime

`MapGraphQL()` is the standard endpoint. It handles HTTP GraphQL requests, WebSocket GraphQL requests (when ASP.NET Core WebSockets are enabled), schema download, Nitro, multipart uploads, and persisted-operation support if configured.

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

Use schema-level `ModifyServerOptions(...)` for defaults that apply across all endpoints. Use endpoint `WithOptions(...)` when a specific mapped endpoint requires different behavior.

When you need to split HTTP, WebSocket, or persisted-operation endpoints, use specialized mappings such as `MapGraphQLHttp()`, `MapGraphQLWebSocket()`, and `MapGraphQLPersistedOperations()`.

| Decision                                                                                   | Where to configure                                        | Read next                                                                                                                                                                                     |
| ------------------------------------------------------------------------------------------ | --------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Which route exposes GraphQL?                                                               | `MapGraphQL("/graphql")`                                  | [Endpoints](/docs/hotchocolate/v16/build/server-configuration/endpoints)                                                                                                                      |
| Should GET, schema requests, Nitro, multipart uploads, WebSockets, or batching be enabled? | `ModifyServerOptions(...)` or endpoint `WithOptions(...)` | [Endpoints](/docs/hotchocolate/v16/build/server-configuration/endpoints), [Batching](/docs/hotchocolate/v16/build/performance/batching), [Files](/docs/hotchocolate/v16/_leagcy/server/files) |
| Which HTTP response format should clients request?                                         | HTTP `Accept` header                                      | [HTTP Transport](/docs/hotchocolate/v16/build/server-configuration/http-transport)                                                                                                            |
| Do requests need custom state or services?                                                 | interceptors and global state                             | [Interceptors](/docs/hotchocolate/v16/build/server-configuration/interceptors), [Global State](/docs/hotchocolate/v16/build/server-configuration/global-state)                                |
| Do deployments need schema export?                                                         | warmup and command-line commands                          | [Warmup](/docs/hotchocolate/v16/build/performance/warmup), [Command Line](/docs/hotchocolate/v16/build/server-configuration/command-line)                                                     |

Keep these runtime considerations in mind:

- Batching is disabled by default. Enable the required `AllowedBatching` flags.
- WebSocket transport requires `app.UseWebSockets()` before `MapGraphQL()`.
- Schema file exposure and introspection policy are controlled separately.
- `Accept` negotiation determines standard JSON, multipart responses, Server-Sent Events, and JSON Lines.

# Protect the API before exposing it

Authentication determines who made the request. Authorization determines what that user can access. GraphQL-specific controls determine whether the requested operation is safe to execute.

| API posture                | Main controls                                                                          | Start with                                                                   |
| -------------------------- | -------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------- |
| Public API                 | authentication, authorization, request limits, cost analysis, introspection policy     | [Securing Your API](/docs/hotchocolate/v16/build/security)                   |
| Private or first-party API | trusted documents, operation storage, hash providers, deployment workflow              | [Trusted Documents](/docs/hotchocolate/v16/build/security/trusted-documents) |
| Mixed API                  | trusted documents for owned clients, cost controls for arbitrary or partner operations | security and performance pages                                               |

A public API typically requires bounded requests:

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

ASP.NET Core authentication and authorization remain part of the host pipeline. The security documentation shows the complete middleware order for authenticated APIs.

A private or first-party API can be restricted to pre-registered operations only:

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

When troubleshooting security, start from the failing layer and work outward:

| Symptom                                                      | Check                                                                                                                               |
| ------------------------------------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------- |
| An authenticated user still receives an authorization error. | Confirm ASP.NET Core authentication runs before endpoint execution, then check Hot Chocolate authorization policies and directives. |
| A public endpoint still allows schema inspection.            | Check `AllowIntrospection(...)`, schema file endpoint options, and environment-specific endpoint options.                           |
| A valid query is rejected.                                   | Inspect parser limits, execution depth, cost limits, and pagination bounds with representative client operations.                   |
| Trusted documents work locally but fail after deployment.    | Verify operation storage path, published hashes, hash provider, and deployment artifact contents.                                   |

# Tune performance and startup behavior

Begin by measuring. Instrument the server, send representative operations, and adjust one lever at a time.

| Lever                 | What it improves                                    | Read next                                                                                                                                                                                                       |
| --------------------- | --------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| DataLoader            | Fewer data-store calls for nested fields            | [DataLoader](/docs/hotchocolate/v16/build/dataloader)                                                                                                                                                           |
| Projections           | Smaller provider queries                            | [Projections](/docs/hotchocolate/v16/build/filtering-sorting-projections/projection-options)                                                                                                                    |
| Pagination and limits | Bounded result size and operation shape             | [Pagination](/docs/hotchocolate/v16/build/pagination), [Request Limits](/docs/hotchocolate/v16/build/security/execution-depth-and-limits), [Cost Analysis](/docs/hotchocolate/v16/build/security/cost-analysis) |
| Batching              | Fewer network round trips for selected clients      | [Batching](/docs/hotchocolate/v16/build/performance/batching)                                                                                                                                                   |
| Trusted documents     | Smaller requests and reused parsed operations       | [Trusted Documents](/docs/hotchocolate/v16/build/security/trusted-documents), [Automatic Persisted Operations](/docs/hotchocolate/v16/build/performance/automatic-persisted-operations)                         |
| Warmup                | Prepared schema and operation caches before traffic | [Warmup](/docs/hotchocolate/v16/build/performance/warmup)                                                                                                                                                       |

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

Deployment outputs may include a warmed executor at startup and a `schema.graphql` file for CI, schema registries, or client checks.

# Observe, diagnose, and test production behavior

Production readiness includes traces, metrics, error handling, and regression tests. Add instrumentation before tuning, and decide how much exception detail clients should see.

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

Use the following guidance when troubleshooting:

| Problem                                                       | Go to                                                                                                                                                            |
| ------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| You need request traces, metrics, or diagnostic events.       | [Instrumentation](/docs/hotchocolate/v16/build/observability)                                                                                                    |
| A resolver throws, but the client receives a sanitized error. | [Errors](/docs/hotchocolate/v16/build/errors), [Error Handling](/docs/hotchocolate/v16/_leagcy/guides/error-handling)                                            |
| No telemetry appears after adding OpenTelemetry.              | [Instrumentation](/docs/hotchocolate/v16/build/observability) setup and package guidance                                                                         |
| A request times out or is canceled.                           | [Options Reference](/docs/hotchocolate/v16/build/server-configuration/schema-options), request options, execution engine docs                                    |
| Field middleware runs in an unexpected order.                 | [Execution Engine](/docs/hotchocolate/v16/build/execution-engine), [Field Middleware](/docs/hotchocolate/v16/build/execution-engine/field-middleware)            |
| A schema change breaks clients.                               | [Schema Evolution](/docs/hotchocolate/v16/_leagcy/guides/schema-evolution), [Testing](/docs/hotchocolate/v16/_leagcy/guides/testing), command-line schema export |

# Use reference and internals only when needed

Do not begin with internals. Refer to them when you need to extend Hot Chocolate or debug behavior that is not covered by the main task pages.

| Need                                       | Read                                                                                                                                                                                                                            |
| ------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Attribute syntax and custom attributes     | [Custom Attributes](/docs/hotchocolate/v16/build/attributes/custom-descriptor-attributes)                                                                                                                                       |
| Configuration lookup                       | [Options Reference](/docs/hotchocolate/v16/build/server-configuration/schema-options)                                                                                                                                           |
| Error shape and filters                    | [Errors](/docs/hotchocolate/v16/build/errors)                                                                                                                                                                                   |
| Executable documents, visitors, or tooling | [Executable](/docs/hotchocolate/v16/build/execution-internals/executable), [Visitors](/docs/hotchocolate/v16/build/execution-internals/visitors), [Language](/docs/hotchocolate/v16/build/execution-internals/language-and-ast) |
| Custom filtering extensions                | [Extending Filtering](/docs/hotchocolate/v16/build/filtering-sorting-projections/extending-filtering)                                                                                                                           |
| Request lifecycle and middleware order     | [Execution Engine](/docs/hotchocolate/v16/build/execution-engine), [Field Middleware](/docs/hotchocolate/v16/build/execution-engine/field-middleware)                                                                           |

# Evolve and integrate safely

Once you have established the core server path, use scenario guides to plan for production:

- [Public API guide](/docs/hotchocolate/v16/_leagcy/guides/public-api) for external clients and cost controls
- [Private API guide](/docs/hotchocolate/v16/_leagcy/guides/private-api) for trusted documents and owned clients
- [Performance guide](/docs/hotchocolate/v16/_leagcy/guides/performance) for end-to-end tuning
- [Schema Evolution](/docs/hotchocolate/v16/_leagcy/guides/schema-evolution) for compatible schema changes
- [Error Handling](/docs/hotchocolate/v16/_leagcy/guides/error-handling) and [Testing](/docs/hotchocolate/v16/_leagcy/guides/testing) for regression prevention
- [Dynamic Schemas](/docs/hotchocolate/v16/_leagcy/guides/dynamic-schemas), [OpenAPI Adapter](/docs/hotchocolate/v16/_leagcy/guides/openapi-adapter), and [MCP Adapter](/docs/hotchocolate/v16/_leagcy/guides/mcp-adapter) for optional Hot Chocolate scenarios

# Know when this server becomes a subgraph

A standalone Hot Chocolate server does not need to become a subgraph. Consider this path only if independent services, team boundaries, or graph composition are architectural requirements.

For Apollo Federation-compatible subgraph features in Hot Chocolate, see [Apollo Federation](/docs/hotchocolate/v16/build/fusion-subgraph/apollo-federation). If you move to distributed graph composition, leave the standalone server path and follow the [Fusion](/docs/hotchocolate/v16/_leagcy/fusion) documentation.

# Next steps

- If you are designing a new API, start with [Schema Elements](/docs/hotchocolate/v16/build/schema-elements).
- If you are implementing data access, continue with [Resolvers](/docs/hotchocolate/v16/build/resolvers) and [DataLoader](/docs/hotchocolate/v16/build/dataloader).
- If you are preparing for production, review [Server](/docs/hotchocolate/v16/build/server-configuration), [Securing Your API](/docs/hotchocolate/v16/build/security), [Performance](/docs/hotchocolate/v16/build/performance), and [Instrumentation](/docs/hotchocolate/v16/build/observability).
