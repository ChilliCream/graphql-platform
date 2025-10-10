---
title: Migrate Hot Chocolate from 15 to 16
---

This guide will walk you through the manual migration steps to update your Hot Chocolate GraphQL server to version 16.

Start by installing the latest `16.x.x` version of **all** of the `HotChocolate.*` packages referenced by your project.

> This guide is still a work in progress with more updates to follow.

# Breaking changes

Things that have been removed or had a change in behavior that may cause your code not to compile or lead to unexpected behavior at runtime if not addressed.

## Eager initialization by default

Previously, Hot Chocolate would only construct the schema and request executor upon the first request. This deferred initialization could create a performance penalty on initial requests and delayed the discovery of schema errors until runtime.

To address this, we previously offered an `InitializeOnStartup` helper that would initialize the schema and request executor in a blocking hosted service during startup. This ensured everything GraphQL-related was ready before Kestrel began accepting requests.

Since we believe eager initialization is the right default, it's now the standard behavior. This means your schema and request executor are constructed during application startup, before your server begins accepting traffic.
As a bonus, this tightens your development loop, since schema errors surface immediately when you start debugging rather than only appearing when you send your first request.

If you're currently using `InitializeOnStartup`, you can safely remove it. If you also provided the `warmup` argument to run a task during the initialization, you can migrate that task to the new `AddWarmupTask` API:

```diff
builder.Services.AddGraphQLServer()
-   .InitializeOnStartup(warmup: (executor, ct) => { /* ... */ });
+   .AddWarmupTask((executor, ct) => { /* ... */ });
```

Warmup tasks registered with `AddWarmupTask` run at startup **and** when the schema is updated at runtime by default. Checkout the [documentation](/docs/hotchocolate/v16/server/warmup), if you need your warmup task to only run at startup.

If you need to preserve lazy initialization for specific scenarios (though this is rarely recommended), you can opt out by setting the `LazyInitialization` option to `true`:

```csharp
builder.Services.AddGraphQLServer()
    .ModifyOptions(options => options.LazyInitialization = true);
```

## Cache size configuration

Previously, configuring document and operation cache sizes required calling methods directly on IServiceCollection rather than using the standard IRequestExecutorBuilder pattern. We've now consolidated cache configuration with other GraphQL options for consistency.
If you're currently using AddOperationCache or AddDocumentCache, update your code as follows:

```diff
-builder.Services.AddDocumentCache(200);
-builder.Services.AddOperationCache(100);

builder.Services.AddGraphQLServer()
+    .ModifyOptions(options =>
+    {
+        options.OperationDocumentCacheSize = 200;
+        options.PreparedOperationCacheSize = 100;
+    });
```

## MaxAllowedNodeBatchSize & EnsureAllNodesCanBeResolved options moved

```diff
builder.Services.AddGraphQLServer()
-    .ModifyOptions(options =>
-    {
-        options.MaxAllowedNodeBatchSize = 100;
-        options.EnsureAllNodesCanBeResolved = false;
-    })
-    .AddGlobalObjectIdentification()
+    .AddGlobalObjectIdentification(options =>
+    {
+        options.MaxAllowedNodeBatchSize = 100;
+        options.EnsureAllNodesCanBeResolved = false;
+    });
```

## IRequestContext

We've removed the `IRequestContext` abstraction in favor of the concrete `RequestContext` class.
Additionally, all information related to the parsed operation document has been consolidated into a new `OperationDocumentInfo` class, accessible via `RequestContext.OperationDocumentInfo`.

| Before                      | After                                     |
| --------------------------- | ----------------------------------------- |
| context.DocumentId          | context.OperationDocumentInfo.Id.Value    |
| context.Document            | context.OperationDocumentInfo.Document    |
| context.DocumentHash        | context.OperationDocumentInfo.Hash.Value  |
| context.ValidationResult    | context.OperationDocumentInfo.IsValidated |
| context.IsCachedDocument    | context.OperationDocumentInfo.IsCached    |
| context.IsPersistedDocument | context.OperationDocumentInfo.IsPersisted |

Here's how you would update a custom request middleware implementation:

```diff
public class CustomRequestMiddleware
{
-   public async ValueTask InvokeAsync(IRequestContext context)
+   public async ValueTask InvokeAsync(RequestContext context)
    {
-       string documentId = context.DocumentId;
+       string documentId = context.OperationDocumentInfo.Id.Value;

        await _next(context).ConfigureAwait(false);
    }
}
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

## Errors from `TypeConverter`s are now accessible in the `ErrorFilter`

Previously, exceptions thrown by a `TypeConverter` were not forwarded to the `ErrorFilter`. Such exceptions are now properly propagated and can therefore be intercepted.

In addition, the default output for such errors has been standardized: earlier, type conversion errors resulted in different responses depending on where in the document they occurred. Now, all exceptions thrown by type converters are reported in a unified format:

```json
{
  "errors": [
    {
      "message": "The value provided for `[name of field or argument that caused the error]` is not in a valid format.",
      "locations": [
        {
          "line": <lineNumber>,
          "column": <columnNumber>
        }
      ],
      "path": [ path to output field that caused the error],
      "extensions": {
        "code": "HC0001",
        "coordinate": "schema coordinate pointing to the field or argument that caused the error",
        "inputPath": [path to nested input field or argument (if any) that caused the error]
        "...": "other extensions"
      }
    }
  ],
  "data": {
    ...
  }
}
```

# Deprecations

Things that will continue to function this release, but we encourage you to move away from.

# Noteworthy changes

## RunWithGraphQLCommandsAsync returns exit code

`RunWithGraphQLCommandsAsync` and `RunWithGraphQLCommands` now return exit codes (`Task<int>` and `int` respectively, instead of `Task` and `void`).

We recommend updating your `Program.cs` to return this exit code. This ensures that command failures signal an error to shell scripts, CI/CD pipelines, and other tools:

```diff
var app = builder.Build();

- await app.RunWithGraphQLCommandsAsync(args);
+ return await app.RunWithGraphQLCommandsAsync(args);
```
