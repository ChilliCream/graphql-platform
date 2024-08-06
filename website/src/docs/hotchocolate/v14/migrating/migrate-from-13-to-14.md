---
title: Migrate Hot Chocolate from 12 to 13
---

This guide will walk you through the manual migration steps to update your Hot Chocolate GraphQL server to version 14.

Start by installing the latest `14.x.x` version of **all** of the `HotChocolate.*` packages referenced by your project.

> This guide is still a work in progress with more updates to follow.

# Breaking changes

Things that have been removed or had a change in behavior that may cause your code not to compile or lead to unexpected behavior at runtime if not addressed.

## Builder APIs

We have aligned all builder APIs to be more consistent and easier to use. Builders can now be created by using the static method `Builder.New()` and the `Build()` method to create the final object.

## `IQueryRequestBuilder` replaced by `OperationRequestBuilder`

The interface `IQueryRequestBuilder` and its implementations were replaced with `OperationRequestBuilder` which now supports building standard GraphQL operation requests as well as variable batch requests.

The `Build()` method returns now a `IOperationRequest` which is implemented by `OperatuionRequest` and `VariableBatchRequest`.

We have also simplified what the builder does and removed a lot of the convenience methods that allowed to add single variables to it. This has todo with the support of variable batching. Now, you have to provide the variable map directly.

## `IQueryResultBuilder` replaced by `OperationResultBuilder`

The interface `IQueryResultBuilder` and its implementations were replaced with `OperationResultBuilder` which produces an `OperationResult` on `Build()`.

## `IQueryResult` replaced by `OperationResult`

The interface `IQueryResultBuilder` and its implementations were replaced with `OperationResultBuilder` which produces an `OperationResult` on `Build()`.

# Deprecations

Things that will continue to function this release, but we encourage you to move away from.
