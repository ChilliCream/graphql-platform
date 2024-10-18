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

## IIdSerializer replaced by INodeIdSerializer

Previously, you could grab the `IIdSerializer` from your dependency injection container to manually parse and serialize globally unique identifiers (GID).
As part of the changes to the GID format mentioned above, the `IIdSerializer` interface has been renamed to `INodeIdSerializer`.

The methods used for parsing and serialization have also been renamed:

| Before                             | After                                                                                    |
| ---------------------------------- | ---------------------------------------------------------------------------------------- |
| `.Deserialize("<gid-value>")`      | `.Parse("<gid-value>", typeof(string))` where `string` is the underlying type of the GID |
| `.Serialize("MyType", "<raw-id>")` | `.Format("MyType", "<raw-id>")`                                                          |

The `Parse()` (previously `Deserialize()`) method has also changed its return type from `IdValue` to `NodeId`. The parsed Id value can now be accessed through the `NodeId.InternalId` instead of the `IdValue.Value` property.

## Node Resolver validation

We now enforce that each object type implementing the `Node` interface also defines a resolver, so that the object can be refetched through the `node(id: ID!)` field.

You can opt out of this new behavior by setting the `EnsureAllNodesCanBeResolved` option to `false`.

```csharp
builder.Services
    .AddGraphQLServer()
    .ModifyOptions(o => o.EnsureAllNodesCanBeResolved = false)
```

## IDataLoader<TKey, TValue> arguments now need to be marked as service

Previously, you could inject `IDataLoader<TKey, TValue>` without any attribute. Now you need to mark it as a service.

```csharp
public string GetMyType([Service] IDataLoader<int, MyType?> dataLoader)
```

## DataLoader.LoadAsync always returns nullable type

Previously, the `LoadAsync` method on a DataLoader was typed as non-nullable, even though `null` could be returned.
This release changes the return type of `LoadAsync` to always be nullable.

## Builder APIs

We have aligned all builder APIs to be more consistent and easier to use. Builders can now be created by using the static method `Builder.New()` and the `Build()` method to create the final object.

### IQueryRequestBuilder replaced by OperationRequestBuilder

The interface `IQueryRequestBuilder` and its implementations were replaced with `OperationRequestBuilder` which now supports building standard GraphQL operation requests as well as variable batch requests.

The `Build()` method now returns a `IOperationRequest` which is implemented by `OperationRequest` and `VariableBatchRequest`.

We've also renamed and consolidated some methods on the `OperationRequestBuilder`:

| Before                              | After                                                                       |
| ----------------------------------- | --------------------------------------------------------------------------- |
| `SetQuery("{ __typename }")`        | `SetDocument("{ __typename }")`                                             |
| `AddVariableValue("name", "value")` | `AddVariableValues(new Dictionary<string, object?> { ["name"] = "value" })` |

### IQueryResultBuilder replaced by OperationResultBuilder

The interface `IQueryResultBuilder` and its implementations were replaced with `OperationResultBuilder` which produces an `OperationResult` on `Build()`.

### IQueryResult replaced by OperationResult

The interface `IQueryResult` has been replaced by `OperationResult`.

### IExecutionResult.ExpectQueryResult replaced by .ExpectOperationResult

In your unit tests you might have been using `result.ExpectQueryResult()` to assert that a result is not a streamed response and rather a completed result.
This assertion method has been renamed to `ExpectOperationResult()`.

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

| Old method name                     | New method name                        |
| ----------------------------------- | -------------------------------------- |
| UsePersistedQueryPipeline           | UsePersistedOperationPipeline          |
| UseAutomaticPersistedQueryPipeline  | UseAutomaticPersistedOperationPipeline |
| AddFileSystemQueryStorage           | AddFileSystemOperationDocumentStorage  |
| AddInMemoryQueryStorage             | AddInMemoryOperationDocumentStorage    |
| AddRedisQueryStorage                | AddRedisOperationDocumentStorage       |
| OnlyAllowPersistedQueries           | OnlyAllowPersistedOperations           |
| OnlyPersistedQueriesAreAllowedError | OnlyPersistedOperationsAreAllowedError |
| AllowNonPersistedQuery              | AllowNonPersistedOperation             |
| UseReadPersistedQuery               | UseReadPersistedOperation              |
| UseAutomaticPersistedQueryNotFound  | UseAutomaticPersistedOperationNotFound |
| UseWritePersistedQuery              | UseWritePersistedOperation             |

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

## Connection getTotalCount constructor argument replaced with totalCount

Previously, you could supply an async method to the `getTotalCount` constructor argument when instantiating a `Connection<T>`. This method would only be evaluated to calculate the total count, if the `totalCount` field was selected on that Connection in a query.

```csharp
return new Connection<MyType>(
    edges: [/* ... */],
    info: new ConnectionPageInfo(/* ... */),
    getTotalCount: async cancellationToken => 123)
```

In this release the constructor argument was renamed to `totalCount` and now only accepts an `int` for the total count, no longer a method to compute the total count.
If you want to re-create the old behavior, you can use the new `[IsSelected]` attribute to conditionally compute the total count.

```csharp
public Connection<MyType> GetMyTypes(
  [IsSelected("totalCount")] bool hasSelectedTotalCount,
  CancellationToken cancellationToken)
{
    var totalCount = 0;
    if (hasSelectedTotalCount)
    {
        totalCount = /* ... */;
    }

    return new Connection<MyType>(
        edges: [/* ... */],
        info: new ConnectionPageInfo(/* ... */),
        totalCount: totalCount)
}
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
