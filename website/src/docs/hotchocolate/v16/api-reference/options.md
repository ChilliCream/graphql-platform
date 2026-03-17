---
title: Options Reference
description: Comprehensive reference for all configuration options in Hot Chocolate v16, including schema, request, cost, server, and paging options.
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

| Property                  | Type              | Default | Description                                                                     |
| ------------------------- | ----------------- | ------- | ------------------------------------------------------------------------------- |
| `EnableGetRequests`       | `bool`            | `true`  | Allows GraphQL queries over HTTP GET.                                           |
| `EnableMultipartRequests` | `bool`            | `true`  | Allows multipart HTTP requests (file uploads).                                  |
| `Batching`                | `AllowedBatching` | `None`  | Controls which batching modes are allowed. Use `AllowedBatching.All` to enable. |
| `MaxBatchSize`            | `int`             | `1024`  | Maximum number of operations in a single batch. Set to `0` for unlimited.       |
| `EnableSchemaRequests`    | `bool`            | `true`  | Allows schema introspection requests.                                           |

Per-endpoint overrides are still supported through `WithOptions` on the endpoint builder:

```csharp
app.MapGraphQL().WithOptions(o => o.EnableGetRequests = false);
```

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

| Property                       | Type   | Default | Description                                                                   |
| ------------------------------ | ------ | ------- | ----------------------------------------------------------------------------- |
| `DefaultPageSize`              | `int`  | `10`    | The default number of items per page when `first` or `last` is not specified. |
| `MaxPageSize`                  | `int`  | `50`    | The maximum number of items a client can request per page.                    |
| `IncludeTotalCount`            | `bool` | `false` | When `true`, includes a `totalCount` field on connection types.               |
| `AllowBackwardPagination`      | `bool` | `true`  | Allows clients to paginate backward using `last` and `before`.                |
| `RequirePagingBoundaries`      | `bool` | `false` | Requires clients to provide either `first` or `last`.                         |
| `InferConnectionNameFromField` | `bool` | `true`  | Infers the connection type name from the field name.                          |

# Troubleshooting

**Options changes do not take effect**
Verify that you are calling the correct `Modify*` method. For example, `ModifyOptions` modifies schema options, while `ModifyRequestOptions` modifies execution options. These are separate configuration APIs.

**Execution timeout errors in development**
When the debugger is attached, the default timeout is 30 minutes. In production, it defaults to 30 seconds. You can override it with `ModifyRequestOptions(o => o.ExecutionTimeout = TimeSpan.FromSeconds(60))`.

**Batching returns an error**
Batching is disabled by default in v16. Enable it with `ModifyServerOptions(o => o.Batching = AllowedBatching.All)`.

# Next Steps

- [Execution engine](/docs/hotchocolate/v16/execution-engine) for pipeline configuration
- [Pagination](/docs/hotchocolate/v16/fetching-data/pagination) for paging setup
- [Persisted operations](/docs/hotchocolate/v16/performance/persisted-operations) for operation caching
- [Migration guide](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16) for breaking option changes
