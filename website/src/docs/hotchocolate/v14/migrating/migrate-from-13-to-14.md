---
title: Migrate Hot Chocolate from 13 to 14
---

This guide will walk you through the manual migration steps to update your Hot Chocolate GraphQL server to version 14.

Start by installing the latest `14.x.x` version of **all** of the `HotChocolate.*` packages referenced by your project.

> This guide is still a work in progress with more updates to follow.

# Breaking changes

Things that have been removed or had a change in behavior that may cause your code not to compile or lead to unexpected behavior at runtime if not addressed.

## Banana Cake Pop and Barista renamed to Nitro

| Old                                           | New                                   | Notes                                    |
| --------------------------------------------- | ------------------------------------- | ---------------------------------------- |
| AddBananaCakePopExporter                      | AddNitroExporter                      |                                          |
| AddBananaCakePopServices                      | AddNitro                              |                                          |
| BananaCakePop.Middleware                      | ChilliCream.Nitro.App                 |                                          |
| BananaCakePop.Services                        | ChilliCream.Nitro                     |                                          |
| BananaCakePop.Services.Azure                  | ChilliCream.Nitro.Azure               |                                          |
| BananaCakePop.Services.Fusion                 | ChilliCream.Nitro.Fusion              |                                          |
| barista                                       | nitro                                 | CLI executable                           |
| Barista                                       | ChilliCream.Nitro.CLI                 | CLI NuGet package                        |
| BARISTA_API_ID                                | NITRO_API_ID                          |                                          |
| BARISTA_API_KEY                               | NITRO_API_KEY                         |                                          |
| BARISTA_CLIENT_ID                             | NITRO_CLIENT_ID                       |                                          |
| BARISTA_OPERATIONS_FILE                       | NITRO_OPERATIONS_FILE                 |                                          |
| BARISTA_OUTPUT_FILE                           | NITRO_OUTPUT_FILE                     |                                          |
| BARISTA_SCHEMA_FILE                           | NITRO_SCHEMA_FILE                     |                                          |
| BARISTA_STAGE                                 | NITRO_STAGE                           |                                          |
| BARISTA_SUBGRAPH_ID                           | NITRO_SUBGRAPH_ID                     |                                          |
| BARISTA_SUBGRAPH_NAME                         | NITRO_SUBGRAPH_NAME                   |                                          |
| BARISTA_TAG                                   | NITRO_TAG                             |                                          |
| bcp                                           | nitro                                 | Key in `subgraph-config.json`            |
| bcp-config.json                               | nitro-config.json                     |                                          |
| BCP_API_ID                                    | NITRO_API_ID                          |                                          |
| BCP_API_KEY                                   | NITRO_API_KEY                         |                                          |
| BCP_STAGE                                     | NITRO_STAGE                           |                                          |
| eat.bananacakepop.com                         | nitro.chillicream.com                 |                                          |
| MapBananaCakePop                              | MapNitroApp                           |                                          |
| @chillicream/bananacakepop-express-middleware | @chillicream/nitro-express-middleware |                                          |
| @chillicream/bananacakepop-graphql-ide        | @chillicream/nitro-embedded           | `mode: "self"` is now `mode: "embedded"` |

## Dependency injection changes

- It is no longer necessary to use the `[Service]` attribute unless you're using keyed services, in which case the attribute is used to specify the key.
  - Hot Chocolate will identify services automatically.
- Support for the `[FromServices]` attribute has been removed.
  - As with the `[Service]` attribute above, this attribute is no longer necessary.
- Since the `RegisterService` method is no longer required, it has been removed, along with the `ServiceKind` enum.
- Scoped services injected into query resolvers are now resolver-scoped by default (not request scoped). For mutation resolvers, services are request-scoped by default.
- The default scope can be changed in two ways:

  1. Globally, using `ModifyOptions`:

     ```csharp
     builder.Services
         .AddGraphQLServer()
         .ModifyOptions(o =>
         {
             o.DefaultQueryDependencyInjectionScope =
                 DependencyInjectionScope.Resolver;
             o.DefaultMutationDependencyInjectionScope =
                 DependencyInjectionScope.Request;
         });
     ```

  2. On a per-resolver basis, with the `[UseRequestScope]` or `[UseResolverScope]` attribute.
     - Note: The `[UseServiceScope]` attribute has been removed.

For more information, see the [Dependency Injection](/docs/hotchocolate/v14/server/dependency-injection) documentation.

## Entity framework integration changes

- The `RegisterDbContext` method is no longer required, and has therefore been removed, along with the `DbContextKind` enum.
- Use `RegisterDbContextFactory` to register a DbContext factory.

For more information, see the [Entity Framework integration](/docs/hotchocolate/v14/integrations/entity-framework) documentation.

## New GID format

This release introduces a more performant GID serializer, which also simplifies the underlying format of globally unique IDs.

By default, the new serializer will be able to parse both the old and new ID format, while only emitting the new format.

This change is breaking if your consumers depend on the format of the GIDs, by for example parsing them (which they shouldn't). If possible, strive to decouple your consumers from the internal ID format and exposing the underlying ID as a separate field on your type if necessary.

If you don't want to switch to the new format yet, you can register the legacy serializer, which only supports parsing and emitting the old ID format:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddLegacyNodeIdSerializer()
    .AddGlobalObjectIdentification();
```

> Note: `AddLegacyNodeIdSerializer()` needs to be called before `AddGlobalObjectIdentification()`.

### How to adopt incrementally in a distributed system

None of your services can start to emit the new ID format, as long as there are services that can't parse the new format.

Therefore, you'll first want to make sure that all of your services support parsing both the old and new format, while still emitting the old format.

This can be done, by configuring the new default serializer to not yet emit the new format:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddDefaultNodeIdSerializer(outputNewIdFormat: false)
    .AddGlobalObjectIdentification();
```

> Note: `AddDefaultNodeIdSerializer()` needs to be called before `AddGlobalObjectIdentification()`.

Once all of your services have been updated to this, you can start emitting the new format service-by-service, by removing the `AddDefaultNodeIdSerializer()` call and switching to the new default behavior:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddGlobalObjectIdentification();
```

## Node Resolver validation

We now enforce that each object type implementing the `Node` interface also defines a resolver, so that the object can be refetched through the `node(id: ID!)` field.

You can opt out of this new behavior by setting the `EnsureAllNodesCanBeResolved` option to `false`.

```csharp
builder.Services
    .AddGraphQLServer()
    .ModifyOptions(o => o.EnsureAllNodesCanBeResolved = false)
```

## Builder APIs

We have aligned all builder APIs to be more consistent and easier to use. Builders can now be created by using the static method `Builder.New()` and the `Build()` method to create the final object.

### IQueryRequestBuilder replaced by OperationRequestBuilder

The interface `IQueryRequestBuilder` and its implementations were replaced with `OperationRequestBuilder` which now supports building standard GraphQL operation requests as well as variable batch requests.

The `Build()` method returns now a `IOperationRequest` which is implemented by `OperationRequest` and `VariableBatchRequest`.

We have also simplified what the builder does and removed a lot of the convenience methods that allowed to add single variables to it. This has todo with the support of variable batching. Now, you have to provide the variable map directly.

### IQueryResultBuilder replaced by OperationResultBuilder

The interface `IQueryResultBuilder` and its implementations were replaced with `OperationResultBuilder` which produces an `OperationResult` on `Build()`.

### IQueryResult replaced by IOperationResult

The interface `IQueryResult` was replaced with `IOperationResult`.

## Operation complexity analyzer replaced

The Operation Complexity Analyzer in v13 has been replaced by Cost Analysis in v14, based on the draft [IBM Cost Analysis specification](https://ibm.github.io/graphql-specs/cost-spec.html).

- The `Complexity` property on `RequestExecutorOptions` (accessed via `ModifyRequestOptions`) has been removed.
- Cost analysis is enabled by default.

Please see the [documentation](/docs/hotchocolate/v14/security/cost-analysis) for further information.

## DateTime scalar enforces a specific format

The `DateTime` scalar will now enforce a specific format. The time and offset are now required, and fractional seconds are limited to 7. This aligns it with the DateTime Scalar spec (<https://www.graphql-scalars.com/date-time/>), with the one difference being that fractions of a second are optional, and 0-7 digits may be specified.

Please ensure that your clients are sending date/time strings in the correct format to avoid errors.

## Persisted Queries renamed to Persisted Operations

### Packages renamed

| Old package name                         | New package name                            |
| ---------------------------------------- | ------------------------------------------- |
| HotChocolate.PersistedQueries.FileSystem | HotChocolate.PersistedOperations.FileSystem |
| HotChocolate.PersistedQueries.InMemory   | HotChocolate.PersistedOperations.InMemory   |
| HotChocolate.PersistedQueries.Redis      | HotChocolate.PersistedOperations.Redis      |

### Interfaces renamed

| Old interface name             | New interface name                 |
| ------------------------------ | ---------------------------------- |
| IPersistedQueryOptionsAccessor | IPersistedOperationOptionsAccessor |

### Methods renamed

| Old method name                    | New method name                        |
| ---------------------------------- | -------------------------------------- |
| UsePersistedQueryPipeline          | UsePersistedOperationPipeline          |
| UseAutomaticPersistedQueryPipeline | UseAutomaticPersistedOperationPipeline |
| AddFileSystemQueryStorage          | AddFileSystemOperationDocumentStorage  |
| AddInMemoryQueryStorage            | AddInMemoryOperationDocumentStorage    |
| AddRedisQueryStorage               | AddRedisOperationDocumentStorage       |
| AllowNonPersistedQuery             | AllowNonPersistedOperation             |
| UseReadPersistedQuery              | UseReadPersistedOperation              |
| UseAutomaticPersistedQueryNotFound | UseAutomaticPersistedOperationNotFound |
| UseWritePersistedQuery             | UseWritePersistedOperation             |

### Options renamed

| Old option name                     | New option name                                 |
| ----------------------------------- | ----------------------------------------------- |
| OnlyAllowPersistedQueries           | PersistedOperations.OnlyAllowPersistedDocuments |
| OnlyPersistedQueriesAreAllowedError | PersistedOperations.OperationNotAllowedError    |

### Defaults changed

| Parameter      | Old default         | New default            |
| -------------- | ------------------- | ---------------------- |
| cacheDirectory | "persisted_queries" | "persisted_operations" |

## MutationResult renamed to FieldResult

| Old name                      | New name                   |
| ----------------------------- | -------------------------- |
| MutationResult&lt;TResult&gt; | FieldResult&lt;TResult&gt; |
| IMutationResult               | IFieldResult               |

## IReadStoredQueries and IWriteStoredQueries now IOperationDocumentStorage

`IReadStoredQueries` and `IWriteStoredQueries` have been merged into a single interface named `IOperationDocumentStorage`.

Renamed interface methods:

| Old name          | New name     |
| ----------------- | ------------ |
| TryReadQueryAsync | TryReadAsync |
| WriteQueryAsync   | SaveAsync    |

## Required keyed services

Accessing a keyed service that has not been registered will now throw, instead of returning `null`. The return type is now non-nullable.

This change aligns the API with the regular (non-keyed) service access API.

## Change to OnlyAllowPersistedOperations option

**Before**

```csharp
ModifyRequestOptions(o => o.OnlyAllowPersistedOperations = true);
```

**After**

```csharp
ModifyRequestOptions(o => o.PersistedOperations.OnlyAllowPersistedDocuments = true);
```

# Deprecations

Things that will continue to function this release, but we encourage you to move away from.

## SetPagingOptions

In an effort to align our configuration APIs, we're now also offering a delegate based configuration API for pagination options.

**Before**

```csharp
builder.Services
    .AddGraphQLServer()
    .SetPagingOptions(new PagingOptions
    {
        MaxPageSize = 100,
        DefaultPageSize = 25
    });
```

**After**

```csharp
builder.Services
    .AddGraphQLServer()
    .ModifyPagingOptions(opt =>
    {
        opt.MaxPageSize = 100;
        opt.DefaultPageSize = 25;
    });
```
