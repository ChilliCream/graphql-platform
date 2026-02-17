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

## Clearer separation between schema and application services

Hot Chocolate has long maintained a second `IServiceProvider` for schema services, separate from the application service provider where you register your services and configuration. This schema service provider is scoped to a particular schema and contains all of Hot Chocolate's internal services.

To access application services within schema services like diagnostic event listeners or error filters, we previously used a combined service provider for activating various Hot Chocolate components. However, this approach made it difficult to track service origins and created challenges for AOT compatibility.

Starting with v16, we're introducing a more explicit model where Hot Chocolate configuration is instantiated exclusively through the internal schema service provider. Application services must now be explicitly cross-registered in the schema service provider to be accessible.

```diff
builder.Services.AddSingleton<MyService>();
builder.Services.AddGraphQLServer()
+   .AddApplicationService<MyService>()
    .AddDiagnosticEventListener<MyDiagnosticEventListener>()
    // or
    .AddDiagnosticEventListener(sp => new MyService(sp.GetRequiredService<MyService>()));

public class MyDiagnosticEventListener(MyService service) : ExecutionDiagnosticEventListener;
```

Services registered via `AddApplicationService<T>()` are resolved once during schema initialization from the application service provider and registered as singletons in the schema service provider.

If you're using any of the following configuration APIs, ensure that the application services required for their activation are registered via `AddApplicationService<T>()`:

- `AddHttpRequestInterceptor`
- `AddSocketSessionInterceptor`
- `AddErrorFilter`
- `AddDiagnosticEventListener`
- `AddOperationCompilerOptimizer`
- `AddTransactionScopeHandler`
- `AddRedisOperationDocumentStorage`
- `AddAzureBlobStorageOperationDocumentStorage`
- `AddInstrumentation` with a custom `ActivityEnricher`

**Note:** Service injection into resolvers is not affected by this change.

If you need to access the application service provider from within the schema service provider, you can use:

```csharp
IServiceProvider applicationServices = schemaServices.GetRootServiceProvider();
```

## Cache size configuration

Previously, document and operation cache sizes were globally configured through the `IServiceCollection`. In an effort to align and properly scope our configuration APIs, we've moved the configuration of these caches to the `IRequestExecutorBuilder`. If you're currently calling `AddDocumentCache` or `AddOperationCache` directly on the `IServiceCollection`, move the configuration to `ModifyOptions` on the `IRequestExecutorBuilder`:

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

If your application contains multiple GraphQL servers, the cache configuration has to be repeated for each one as the configuration is now scoped to a particular GraphQL server.

If you were previously accessing `IDocumentCache` or `IPreparedOperationCache` through the root service provider, you now need to access it through the schema-specific service provider instead.
For instance, to populate the document cache during startup, create a custom `IRequestExecutorWarmupTask` that injects `IDocumentCache`:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddWarmupTask<MyWarmupTask>();

public class MyWarmupTask(IDocumentCache cache) : IRequestExecutorWarmupTask
{
    public bool ApplyOnlyOnStartup => false;

    public async Task WarmupAsync(
        IRequestExecutor executor,
        CancellationToken cancellationToken)
    {
        // Modify the cache
    }
}
```

## Document hash provider configuration

Previously, document hash providers were globally configured through the `IServiceCollection`. In an effort to align and properly scope our configuration APIs, we've moved the configuration of the hash provider to the `IRequestExecutorBuilder`. If you're currently calling `AddMD5DocumentHashProvider`, `AddSha256DocumentHashProvider` or `AddSha1DocumentHashProvider` directly on the `IServiceCollection`, move the call to the `IRequestExecutorBuilder`:

```diff
-builder.Services.AddSha256DocumentHashProvider();

builder.Services.AddGraphQLServer()
+    .AddSha256DocumentHashProvider()
```

If your application contains multiple GraphQL servers, the hash provider configuration has to be repeated for each one as the configuration is now scoped to a particular GraphQL server.

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

## OperationResultBuilder is now internal

If you've previously used the `OperationResultBuilder` to construct an `OperationResult`, switch to constructing it directly instead:

```csharp
var errors = ImmutableList.Create<IError>([]);
var extensions = ImmutableOrderedDictionary.Create([]);

context.Result = new OperationResult(errors, extensions);
```

If you've used `OperationResultBuilder.FromResult()` to alter an existing `OperationResult`, switch to directly modifying the `OperationResult`:

```diff
if (context.Result is OperationResult result)
{
-    var resultBuilder = OperationResultBuilder.FromResult(result);
-    resultBuilder.SetExtension("foo", "bar");
-    context.Result = resultBuilder.Build();
+    result.Extensions = result.Extensions.SetItem("foo", "bar");
}
```

Most of the properties you'd want to modify are now immutable data structures that can be modified.

`OperationResultBuilder.CreateError(error)` can be simply replaced with `new OperationResult([error])`.

## OperationResult changes

We've removed the `IOperationResult` abstraction. If you've previously pattern-matched on this, you can simply replace it with `OperationResult`. To assert that an `IExecutionResult` is an `OperationResult` in tests, use `result.ExpectOperationResult();`.

We've also switched the `OperationResult.Errors` and `OperationResult.Extensions` properties to always be initialized instead of being nullable. If you were previously asserting these properties as `null` in tests, switch to asserting them as empty instead.

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

## Generic `ID<Type>`-attribute now infers the actual GraphQL type name

Previously, `[ID<Type>]` used the CLR type name (`nameof(Type)`), even when a different GraphQL type name was configured via `[GraphQLName]` or `descriptor.Name()`.
It now uses the actual GraphQL type name if one is defined, for example:

```csharp
[GraphQLName("Book")]
public sealed class BookDTO
{
    [ID]
    public int Id { get; set; }

    public string Title { get; set; }
}

[ID<BookDTO>] // uses "Book" now, not "BookDTO" anymore
```

Note that this change implies that all type parameters of the generic `ID<Type>`-attribute must now be valid GraphQL types.
If you need the old behavior, use can still use the non-generic `ID`-attribute and set the type name explicitly: `[ID("BookDTO")]`.

## DescriptorAttribute attributeProvider is nullable

Previously the `TryConfigure` or `OnConfigure` methods carried a non-nullable parameter of the member the descriptor attribute was annotated to. With the new source generator we moved away from pure reflection based APIs. This means that when you use the source generator

## Merged Assemblies HotChocolate.Types, HotChocolate.Execution, HotChocolate.Fetching

With Hot Chocolate 16 we introduced a lot more abstractions, meaning we pulled out abstractions of the type system or the execution into separate libraries. But at the same time we simplified the implementation of the type system and the execution by moving the implementations of HotChocolate.Execution and HotChocolate.Fetching into HotChocolate.Types. This allowed us to simplify the implementation and make it more efficient.

So, if you were referencing HotChocolate.Execution or HotChocolate.Fetching directly make sure to remove references to these libraries and replace them with HotChocolate.Types.

## Simpler Scalar Type

TODO

## Removed Scalars

TODO

NegativeFloat
NonNegativeFloat
NegativeInt
NonPositiveInt
NonEmptyString
NonNegativeInt

## OperationRequestBuilder

TODO

## AnyType

TODO
`JsonElement` is now inferred as `Any` instead of `Json`.

## `Byte` and `SignedByte` types renamed

- The GraphQL type `Byte` has been renamed to `UnsignedByte` (CLR type: `byte`).
- The GraphQL type `SignedByte` has been renamed to `Byte` (CLR type: `sbyte`).

This is to align the GraphQL type names with the core types (`Int`, etc.), which are signed.

## Byte arrays now mapped to `Base64String`

C# byte arrays (`byte[]`) are now mapped to the GraphQL `Base64String` type by default, as the `ByteArray` type has been deprecated.

## `Uri` now mapped to `URI` scalar instead of `URL`

The CLR type `Uri` is now mapped to a new `URI` scalar, instead of the `URL` scalar.

- The `URI` scalar should be used for absolute or relative URIs.
- The `URL` scalar should be used for absolute URIs/URLs only.

For backwards compatibility, you can set `allowRelativeUris` to `true`:

```csharp
AddGraphQL().AddType(new UrlType(allowRelativeUris: true))
```

Note that this option is likely to be removed in a later release, so it's recommended that you switch types as soon as possible.

## DateTime scalar serialization

The `DateTime` scalar now serializes with up to 7 fractional seconds (`FFFFFFF`) as opposed to exactly 3 (`fff`).

## IHasRuntimeType is now IRuntimeTypeProvider

In an effort to standardize our abstractions, we've renamed `IHasRuntimeType` to `IRuntimeTypeProvider`.

# Deprecations

Things that will continue to function this release, but we encourage you to move away from.

## `ByteArray`

The GraphQL `ByteArray` type has been deprecated. Use the `Base64String` type instead.

# Noteworthy changes

## RunWithGraphQLCommandsAsync returns exit code

`RunWithGraphQLCommandsAsync` and `RunWithGraphQLCommands` now return exit codes (`Task<int>` and `int` respectively, instead of `Task` and `void`).

We recommend updating your `Program.cs` to return this exit code. This ensures that command failures signal an error to shell scripts, CI/CD pipelines, and other tools:

```diff
var app = builder.Build();

- await app.RunWithGraphQLCommandsAsync(args);
+ return await app.RunWithGraphQLCommandsAsync(args);
```
