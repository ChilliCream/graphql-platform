---
title: Options Reference
description: Comprehensive reference for all configuration options in Hot Chocolate v16, including schema, request, parser, cost, server, socket, subscription, paging, global object identification, and cache control options.
---

Hot Chocolate provides several option groups that control different aspects of the GraphQL server. You configure them through methods on the `IRequestExecutorBuilder`.

# Schema Options (ModifyOptions)

Schema options control the type system and schema behavior. Configure them with `ModifyOptions`:

```csharp
builder.Services
    .AddGraphQLServer()
    .ModifyOptions(o =>
    {
        o.StrictValidation = true;
        o.SortFieldsByName = true;
    });
```

| Property                                  | Type                         | Default             | Description                                                                                                  |
| ----------------------------------------- | ---------------------------- | ------------------- | ------------------------------------------------------------------------------------------------------------ |
| `QueryTypeName`                           | `string?`                    | `null`              | The name of the query type. When `null`, defaults to `Query`.                                                |
| `MutationTypeName`                        | `string?`                    | `null`              | The name of the mutation type. When `null`, defaults to `Mutation`.                                          |
| `SubscriptionTypeName`                    | `string?`                    | `null`              | The name of the subscription type. When `null`, defaults to `Subscription`.                                  |
| `StrictValidation`                        | `bool`                       | `true`              | When `true`, the schema must pass all validation rules (e.g., every field must have a resolver).             |
| `UseXmlDocumentation`                     | `bool`                       | `true`              | Extracts descriptions from XML documentation comments on .NET types.                                         |
| `ResolveXmlDocumentationFileName`         | `Func<Assembly, string>?`    | `null`              | Custom resolver for XML documentation file paths.                                                            |
| `SortFieldsByName`                        | `bool`                       | `false`             | Sorts fields alphabetically in the schema.                                                                   |
| `RemoveUnreachableTypes`                  | `bool`                       | `false`             | Removes types that are not reachable from any root type.                                                     |
| `RemoveUnusedTypeSystemDirectives`        | `bool`                       | `true`              | Removes type system directives that are not applied anywhere.                                                |
| `DefaultBindingBehavior`                  | `BindingBehavior`            | `Implicit`          | Controls whether type members are included by default (`Implicit`) or must be explicitly bound (`Explicit`). |
| `DefaultFieldBindingFlags`                | `FieldBindingFlags`          | `Instance`          | Controls which members are bound as fields (e.g., instance members, static members).                         |
| `FieldMiddleware`                         | `FieldMiddlewareApplication` | `UserDefinedFields` | Controls which fields have middleware applied.                                                               |
| `EnableDirectiveIntrospection`            | `bool`                       | `false`             | Exposes custom directives via introspection.                                                                 |
| `DefaultDirectiveVisibility`              | `DirectiveVisibility`        | `Public`            | The default visibility of directives in the schema.                                                          |
| `DefaultResolverStrategy`                 | `ExecutionStrategy`          | `Parallel`          | The default execution strategy for resolvers (`Parallel` or `Serial`).                                       |
| `ValidatePipelineOrder`                   | `bool`                       | `true`              | Validates the order of field middleware (e.g., paging before filtering).                                     |
| `StrictRuntimeTypeValidation`             | `bool`                       | `false`             | Enforces strict runtime type validation for union and interface types.                                       |
| `DefaultIsOfTypeCheck`                    | `IsOfTypeFallback?`          | `null`              | Fallback for `IsOfType` checks on abstract types.                                                            |
| `EnableFlagEnums`                         | `bool`                       | `false`             | Treats `[Flags]` enums as flag enums in GraphQL.                                                             |
| `EnableDefer`                             | `bool`                       | `false`             | Enables the `@defer` directive.                                                                              |
| `EnableStream`                            | `bool`                       | `false`             | Enables the `@stream` directive.                                                                             |
| `EnableSemanticNonNull`                   | `bool`                       | `false`             | Enables the semantic non-null feature.                                                                       |
| `StripLeadingIFromInterface`              | `bool`                       | `false`             | Strips the leading `I` from C# interface names when generating GraphQL interface type names.                 |
| `EnableTag`                               | `bool`                       | `true`              | Enables the `@tag` directive for schema metadata.                                                            |
| `EnableOptInFeatures`                     | `bool`                       | `false`             | Enables the `@requiresOptIn` directive.                                                                      |
| `DefaultQueryDependencyInjectionScope`    | `DependencyInjectionScope`   | `Resolver`          | The DI scope for query resolvers.                                                                            |
| `DefaultMutationDependencyInjectionScope` | `DependencyInjectionScope`   | `Request`           | The DI scope for mutation resolvers.                                                                         |
| `PublishRootFieldPagesToPromiseCache`     | `bool`                       | `true`              | Whether root field pagination results are published to the DataLoader promise cache.                         |
| `LazyInitialization`                      | `bool`                       | `false`             | When `true`, defers schema construction until the first request.                                             |
| `PreparedOperationCacheSize`              | `int`                        | `256`               | Size of the compiled operation cache. Minimum: `16`.                                                         |
| `OperationDocumentCacheSize`              | `int`                        | `256`               | Size of the parsed document cache. Minimum: `16`.                                                            |

# Request Options (ModifyRequestOptions)

Request options control the execution engine behavior. Configure them with `ModifyRequestOptions`:

```csharp
builder.Services
    .AddGraphQLServer()
    .ModifyRequestOptions(o =>
    {
        o.ExecutionTimeout = TimeSpan.FromSeconds(60);
        o.IncludeExceptionDetails = false;
    });
```

| Property                  | Type                        | Default                                           | Description                                                                |
| ------------------------- | --------------------------- | ------------------------------------------------- | -------------------------------------------------------------------------- |
| `ExecutionTimeout`        | `TimeSpan`                  | 30 seconds (30 minutes when debugger is attached) | Maximum execution time for a request. Minimum: 100ms.                      |
| `IncludeExceptionDetails` | `bool`                      | `true` when debugger is attached                  | When `true`, exception messages and stack traces appear in GraphQL errors. |
| `PersistedOperations`     | `PersistedOperationOptions` | Default instance                                  | Configuration for the persisted operation pipeline behavior.               |

# Cost Options (ModifyCostOptions)

Cost options configure the cost analysis feature. Install the `HotChocolate.CostAnalysis` package and configure with `ModifyCostOptions`:

```csharp
builder.Services
    .AddGraphQLServer()
    .ModifyCostOptions(o =>
    {
        o.MaxFieldCost = 1000;
        o.MaxTypeCost = 2000;
        o.EnforceCostLimits = true;
    });
```

Refer to the cost analysis documentation for the full list of configurable properties.

# Server Options (ModifyServerOptions)

Server options control HTTP-level behavior such as GET requests, batching, multipart requests, and schema retrieval. This is new in v16. Configure with `ModifyServerOptions`:

```csharp
builder.Services
    .AddGraphQLServer()
    .ModifyServerOptions(o =>
    {
        o.EnableGetRequests = true;
        o.EnableMultipartRequests = true;
        o.Batching = AllowedBatching.All;
        o.MaxBatchSize = 50;
        o.EnableSchemaRequests = true;
    });
```

| Property                                  | Type                   | Default   | Description                                                                                                                              |
| ----------------------------------------- | ---------------------- | --------- | ---------------------------------------------------------------------------------------------------------------------------------------- |
| `AllowedGetOperations`                    | `AllowedGetOperations` | `Query`   | Controls which operation types are allowed via HTTP GET. Values: `None`, `Query`, `Mutation`, `Subscription`, `QueryAndMutation`, `All`. |
| `EnableGetRequests`                       | `bool`                 | `true`    | Allows GraphQL queries over HTTP GET.                                                                                                    |
| `EnableMultipartRequests`                 | `bool`                 | `true`    | Allows multipart HTTP requests (file uploads).                                                                                           |
| `EnableSchemaRequests`                    | `bool`                 | `true`    | Allows schema SDL downloads.                                                                                                             |
| `EnableSchemaFileSupport`                 | `bool`                 | `true`    | Allows the schema SDL to be served as a file download.                                                                                   |
| `EnforceGetRequestsPreflightHeader`       | `bool`                 | `false`   | Requires a preflight header on GET requests for CSRF protection.                                                                         |
| `EnforceMultipartRequestsPreflightHeader` | `bool`                 | `true`    | Requires a preflight header on multipart requests for CSRF protection.                                                                   |
| `Batching`                                | `AllowedBatching`      | `None`    | Controls which batching modes are allowed. Use `AllowedBatching.All` to enable.                                                          |
| `MaxBatchSize`                            | `int`                  | `1024`    | Maximum number of operations in a single batch. Set to `0` for unlimited.                                                                |
| `Sockets`                                 | `GraphQLSocketOptions` | See below | WebSocket transport options. See [WebSocket options](#websocket-options-graphqlsocketoptions) for details.                               |
| `Tool`                                    | `NitroAppOptions`      | Default   | Nitro IDE tool options.                                                                                                                  |

Per-endpoint overrides are still supported through `WithOptions` on the endpoint builder:

```csharp
app.MapGraphQL().WithOptions(o => o.EnableGetRequests = false);
```

## WebSocket Options (GraphQLSocketOptions)

The `Sockets` property on `GraphQLServerOptions` holds WebSocket-specific settings. You configure them through `ModifyServerOptions`:

```csharp
builder.Services
    .AddGraphQLServer()
    .ModifyServerOptions(o =>
    {
        o.Sockets.ConnectionInitializationTimeout = TimeSpan.FromSeconds(30);
        o.Sockets.KeepAliveInterval = TimeSpan.FromSeconds(10);
    });
```

| Property                          | Type        | Default    | Description                                                                                                    |
| --------------------------------- | ----------- | ---------- | -------------------------------------------------------------------------------------------------------------- |
| `ConnectionInitializationTimeout` | `TimeSpan`  | 10 seconds | The time a client has to send a `connection_init` message before the server closes the connection.             |
| `KeepAliveInterval`               | `TimeSpan?` | 5 seconds  | The interval at which the server sends keep-alive ping messages. Set to `null` to disable keep-alive messages. |

# Paging Options (ModifyPagingOptions)

Paging options control the default behavior for cursor-based pagination. Configure with `ModifyPagingOptions`:

```csharp
builder.Services
    .AddGraphQLServer()
    .ModifyPagingOptions(o =>
    {
        o.DefaultPageSize = 25;
        o.MaxPageSize = 100;
        o.IncludeTotalCount = true;
    });
```

| Property                       | Type           | Default       | Description                                                                                                 |
| ------------------------------ | -------------- | ------------- | ----------------------------------------------------------------------------------------------------------- |
| `DefaultPageSize`              | `int?`         | `10`          | The default number of items per page when `first` or `last` is not specified.                               |
| `MaxPageSize`                  | `int?`         | `50`          | The maximum number of items a client can request per page.                                                  |
| `IncludeTotalCount`            | `bool?`        | `false`       | When `true`, includes a `totalCount` field on connection types.                                             |
| `AllowBackwardPagination`      | `bool?`        | `true`        | Allows clients to paginate backward using `last` and `before`.                                              |
| `RequirePagingBoundaries`      | `bool?`        | `false`       | Requires clients to provide either `first` or `last`.                                                       |
| `InferConnectionNameFromField` | `bool?`        | `true`        | Infers the connection type name from the field name.                                                        |
| `IncludeNodesField`            | `bool?`        | `null`        | When `true`, exposes a `nodes` field on the Connection type that returns a flattened list without edges.    |
| `EnableRelativeCursors`        | `bool?`        | `null`        | When `true`, enables relative cursor support for pagination.                                                |
| `NullOrdering`                 | `NullOrdering` | `Unspecified` | Defines how your database orders null values. Values: `Unspecified`, `NativeNullsFirst`, `NativeNullsLast`. |
| `ProviderName`                 | `string?`      | `null`        | The name of the paging provider to use. When `null`, the default provider is used.                          |

# Parser Options (ModifyParserOptions)

Parser options control limits on the GraphQL document parser. These are important security and performance settings that protect against excessively large or complex queries. Configure them with `ModifyParserOptions`:

```csharp
builder.Services
    .AddGraphQLServer()
    .ModifyParserOptions(o =>
    {
        o.MaxAllowedFields = 500;
        o.MaxAllowedNodes = 5000;
    });
```

| Property           | Type   | Default        | Description                                                                                                                                   |
| ------------------ | ------ | -------------- | --------------------------------------------------------------------------------------------------------------------------------------------- |
| `MaxAllowedNodes`  | `int`  | `int.MaxValue` | Maximum number of syntax nodes allowed in a document. Prevents excessive memory and CPU usage during parsing.                                 |
| `MaxAllowedTokens` | `int`  | `int.MaxValue` | Maximum number of tokens allowed in a document. Prevents excessive memory and CPU usage during lexing.                                        |
| `MaxAllowedFields` | `int`  | `2048`         | Maximum number of fields allowed in a document. Provides a convenient way to limit query size since fields are an intuitive measure of scope. |
| `IncludeLocations` | `bool` | `true`         | Preserves location information in syntax nodes so that errors can reference positions in the original source. Disabling reduces memory usage. |

Parsing happens before validation, so even invalid queries consume resources. Setting `MaxAllowedNodes`, `MaxAllowedTokens`, and `MaxAllowedFields` to reasonable values for your schema protects against denial-of-service attacks.

# Subscription Options

Subscription options control topic buffer behavior for subscription providers. You pass them when registering a subscription provider:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddInMemorySubscriptions(new SubscriptionOptions
    {
        TopicBufferCapacity = 128,
        TopicBufferFullMode = TopicBufferFullMode.DropOldest,
    });
```

| Property              | Type                  | Default      | Description                                                                                                                                                                       |
| --------------------- | --------------------- | ------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `TopicPrefix`         | `string?`             | `null`       | A prefix prepended to all topic names. Useful when multiple services share the same message broker.                                                                               |
| `TopicBufferCapacity` | `int`                 | `64`         | The in-memory buffer size for messages per topic. When the buffer fills, the `TopicBufferFullMode` policy applies.                                                                |
| `TopicBufferFullMode` | `TopicBufferFullMode` | `DropOldest` | The behavior when writing to a full topic buffer. Values: `DropOldest` (remove oldest message), `DropNewest` (remove newest message), `DropWrite` (discard the incoming message). |

All subscription providers (in-memory, Redis, NATS, RabbitMQ, Postgres) accept these options.

# Global Object Identification Options

Global object identification options configure the Relay-style `node` and `nodes` fields. You configure them through `AddGlobalObjectIdentification`:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddGlobalObjectIdentification(o =>
    {
        o.MaxAllowedNodeBatchSize = 25;
    });
```

| Property                      | Type   | Default | Description                                                                                                              |
| ----------------------------- | ------ | ------- | ------------------------------------------------------------------------------------------------------------------------ |
| `RegisterNodeInterface`       | `bool` | `true`  | Registers the `Node` interface and adds the `node(id: ID!): Node` field to the Query type.                               |
| `AddNodesField`               | `bool` | `true`  | Adds a `nodes(ids: [ID!]!): [Node]!` field to the Query type for batch node fetching.                                    |
| `EnsureAllNodesCanBeResolved` | `bool` | `true`  | Validates during schema building that every type implementing `Node` has a corresponding node resolver configured.       |
| `MaxAllowedNodeBatchSize`     | `int`  | `50`    | The maximum number of IDs a client can pass to the `nodes` field in a single request. Prevents excessive batch fetching. |

# Cache Control Options (ModifyCacheControlOptions)

Cache control options configure HTTP response caching hints based on the `@cacheControl` directive. Install the `HotChocolate.Caching` package and configure with `ModifyCacheControlOptions`:

```csharp
builder.Services
    .AddGraphQLServer()
    .UseQueryCachePipeline()
    .AddCacheControl()
    .ModifyCacheControlOptions(o =>
    {
        o.DefaultMaxAge = 60;
        o.ApplyDefaults = true;
    });
```

| Property        | Type                | Default  | Description                                                                                                                                                                                   |
| --------------- | ------------------- | -------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Enable`        | `bool`              | `true`   | Enables or disables query result caching.                                                                                                                                                     |
| `DefaultMaxAge` | `int`               | `0`      | The default `max-age` value (in seconds) applied to fields when `ApplyDefaults` is `true`.                                                                                                    |
| `DefaultScope`  | `CacheControlScope` | `Public` | The default cache scope applied to fields when `ApplyDefaults` is `true`. Values: `Public`, `Private`.                                                                                        |
| `ApplyDefaults` | `bool`              | `true`   | When `true`, applies `DefaultMaxAge` and `DefaultScope` to all fields that do not already have a `@cacheControl` directive, are on the Query root type, or are responsible for fetching data. |

# Troubleshooting

**Options changes do not take effect**
Verify that you are calling the correct `Modify*` method. For example, `ModifyOptions` modifies schema options, while `ModifyRequestOptions` modifies execution options. These are separate configuration APIs.

**Execution timeout errors in development**
When the debugger is attached, the default timeout is 30 minutes. In production, it defaults to 30 seconds. You can override it with `ModifyRequestOptions(o => o.ExecutionTimeout = TimeSpan.FromSeconds(60))`.

**Batching returns an error**
Batching is disabled by default in v16. Enable it with `ModifyServerOptions(o => o.Batching = AllowedBatching.All)`.

# Next Steps

- [Execution engine](/docs/hotchocolate/v16/execution-engine) for pipeline configuration
- [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination) for paging setup
- [Persisted operations](/docs/hotchocolate/v16/performance/trusted-documents) for operation caching
- [Migration guide](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16) for breaking option changes
