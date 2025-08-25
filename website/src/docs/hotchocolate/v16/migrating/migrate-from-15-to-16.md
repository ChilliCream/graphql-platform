---
title: Migrate Hot Chocolate from 15 to 16
---

This guide will walk you through the manual migration steps to update your Hot Chocolate GraphQL server to version 16.

Start by installing the latest `16.x.x` version of **all** of the `HotChocolate.*` packages referenced by your project.

> This guide is still a work in progress with more updates to follow.

# Breaking changes

Things that have been removed or had a change in behavior that may cause your code not to compile or lead to unexpected behavior at runtime if not addressed.

## MaxAllowedNodeBatchSize & EnsureAllNodesCanBeResolved options moved

**Before**

```csharp
builder.Services.AddGraphQLServer()
    .ModifyOptions(options =>
    {
        options.MaxAllowedNodeBatchSize = 100;
        options.EnsureAllNodesCanBeResolved = false;
    });
```

**After**

```csharp
cbuilder.Services.AddGraphQLServer()
    .AddGlobalObjectIdentification(options =>
    {
        options.MaxAllowedNodeBatchSize = 100;
        options.EnsureAllNodesCanBeResolved = false;
    });
```

## Skip/include disallowed on root subscription fields

The `@skip` and `@include` directives are now disallowed on root subscription fields, as specified in the RFC: [Prevent @skip and @include on root subscription selection set](https://github.com/graphql/graphql-spec/pull/860).

## Deprecation of fields not deprecated in the interface

Deprecating a field now requires the implemented field in the interface to also be deprecated, as specified in the [draft specification](https://spec.graphql.org/draft/#sec-Objects.Type-Validation).

# Deprecations

Things that will continue to function this release, but we encourage you to move away from.
