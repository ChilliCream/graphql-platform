---
title: Migrate Hot Chocolate from 13 to 14
---

This guide will walk you through the manual migration steps to update your Hot Chocolate GraphQL server to version 14.

Start by installing the latest `14.x.x` version of **all** of the `HotChocolate.*` packages referenced by your project.

> This guide is still a work in progress with more updates to follow.

# Breaking changes

Things that have been removed or had a change in behavior that may cause your code not to compile or lead to unexpected behavior at runtime if not addressed.

## New GID format

This release introduces a more performant GID serializer, which also simplifies the underlying format of globally unique IDs.

By default, the new serializer will be able to parse both the old and new ID format, while only emitting the new format.

This change is breaking if your consumers depend on the format of the GIDs, by for example parsing them (which they shouldn't). If possible, strive to decouple your consumers from the internal ID format and exposing the underlying ID as a separate field on your type if necessary.

If you don't want to switch to the new format yet, you can register the legacy serializer, which only supports parsing and emitting the old ID format:

```csharp
services
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
services
  .AddGraphQLServer()
  .AddDefaultNodeIdSerializer(outputNewIdFormat: false)
  .AddGlobalObjectIdentification();
```

> Note: `AddDefaultNodeIdSerializer()` needs to be called before `AddGlobalObjectIdentification()`.

Once all of your services have been updated to this, you can start emitting the new format service-by-service, by removing the `AddDefaultNodeIdSerializer()` call and switching to the new default behavior:

```csharp
services
  .AddGraphQLServer()
  .AddGlobalObjectIdentification();
```

## Builder APIs

We have aligned all builder APIs to be more consistent and easier to use. Builders can now be created by using the static method `Builder.New()` and the `Build()` method to create the final object.

### IQueryRequestBuilder replaced by OperationRequestBuilder

The interface `IQueryRequestBuilder` and its implementations were replaced with `OperationRequestBuilder` which now supports building standard GraphQL operation requests as well as variable batch requests.

The `Build()` method returns now a `IOperationRequest` which is implemented by `OperationRequest` and `VariableBatchRequest`.

We have also simplified what the builder does and removed a lot of the convenience methods that allowed to add single variables to it. This has todo with the support of variable batching. Now, you have to provide the variable map directly.

### IQueryResultBuilder replaced by OperationResultBuilder

The interface `IQueryResultBuilder` and its implementations were replaced with `OperationResultBuilder` which produces an `OperationResult` on `Build()`.

### IQueryResult replaced by OperationResult

The interface `IQueryResultBuilder` and its implementations were replaced with `OperationResultBuilder` which produces an `OperationResult` on `Build()`.

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
|------------------------------------------|---------------------------------------------|
| HotChocolate.PersistedQueries.FileSystem | HotChocolate.PersistedOperations.FileSystem |
| HotChocolate.PersistedQueries.InMemory   | HotChocolate.PersistedOperations.InMemory   |
| HotChocolate.PersistedQueries.Redis      | HotChocolate.PersistedOperations.Redis      |

### Interfaces renamed

| Old interface name             | New interface name                 |
|--------------------------------|------------------------------------|
| IPersistedQueryOptionsAccessor | IPersistedOperationOptionsAccessor |

### Methods renamed

| Old method name                     | New method name                        |
|-------------------------------------|----------------------------------------|
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
|----------------|---------------------|------------------------|
| cacheDirectory | "persisted_queries" | "persisted_operations" |

# Deprecations

Things that will continue to function this release, but we encourage you to move away from.

## SetPagingOptions

In an effort to align our configuration APIs, we're now also offering a delegate based configuration API for pagination options.

**Before**

```csharp
services
  .AddGraphQLServer()
  .SetPagingOptions(new PagingOptions
  {
      MaxPageSize = 100,
      DefaultPageSize = 25
  });
```

**After**

```csharp
services
  .AddGraphQLServer()
  .ModifyPagingOptions(opt =>
  {
      opt.MaxPageSize = 100;
      opt.DefaultPageSize = 25;
  });
```
