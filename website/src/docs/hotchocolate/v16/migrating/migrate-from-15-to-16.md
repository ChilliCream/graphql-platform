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
builder.Services.AddGraphQLServer()
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

## Global ID formatter conditionally added to filter fields

Previously, the global ID input value formatter was added to ID filter fields regardless of whether or not Global Object Identification was enabled. This is now conditional.

## `fieldCoordinate` renamed to `coordinate` in error extensions

Some GraphQL validation errors included an extension named `fieldCoordinate` that provided a schema coordinate pointing to the field or argument that caused the error. Since schema coordinates can reference various schema elements (not just fields), we've renamed this extension to `coordinate` for clarity.

```diff
{
  "errors": [
    {
      "message": "Some error",
      "locations": [
        {
          "line": 3,
          "column": 21
        }
      ],
      "path": [
        "field"
      ],
      "extensions": {
        "code": "HC0001",
-       "fieldCoordinate": "Query.field"
+       "coordinate": "Query.field"
      }
    }
  ],
  "data": {
    "field": null
  }
}
```

# Deprecations

Things that will continue to function this release, but we encourage you to move away from.
