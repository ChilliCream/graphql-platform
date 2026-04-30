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
builder.AddGraphQL()
-   .InitializeOnStartup(warmup: (executor, ct) => { /* ... */ });
+   .AddWarmupTask((executor, ct) => { /* ... */ });
```

Warmup tasks registered with `AddWarmupTask` run at startup **and** when the schema is updated at runtime by default. Checkout the [documentation](/docs/hotchocolate/v16/server/warmup), if you need your warmup task to only run at startup.

If you need to preserve lazy initialization for specific scenarios (though this is rarely recommended), you can opt out by setting the `LazyInitialization` option to `true`:

```csharp
builder.AddGraphQL()
    .ModifyOptions(options => options.LazyInitialization = true);
```

## Clearer separation between schema and application services

Hot Chocolate has long maintained a second `IServiceProvider` for schema services, separate from the application service provider where you register your services and configuration. This schema service provider is scoped to a particular schema and contains all of Hot Chocolate's internal services.

To access application services within schema services like diagnostic event listeners or error filters, we previously used a combined service provider for activating various Hot Chocolate components. However, this approach made it difficult to track service origins and created challenges for AOT compatibility.

Starting with v16, we're introducing a more explicit model where Hot Chocolate configuration is instantiated exclusively through the internal schema service provider. Application services must now be explicitly cross-registered in the schema service provider to be accessible.

```diff
builder.Services.AddSingleton<MyService>();
builder.AddGraphQL()
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

builder.AddGraphQL()
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
builder
    .AddGraphQL()
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

builder.AddGraphQL()
+    .AddSha256DocumentHashProvider()
```

If your application contains multiple GraphQL servers, the hash provider configuration has to be repeated for each one as the configuration is now scoped to a particular GraphQL server.

## NATS subscriptions now use the official NATS v2 client

The `HotChocolate.Subscriptions.Nats` package now uses the official NATS v2 client packages.
If you are migrating an application that previously used `AlterNats.Hosting`, replace it with `NATS.Extensions.Microsoft.DependencyInjection` and update your NATS client registration from `AddNats(...)` to `AddNatsClient(...)`.

```diff
builder.Services
-   .AddNats(poolSize: 1, opts => opts with
-   {
-       Url = "nats://localhost:4222"
-   });
+   .AddNatsClient(nats => nats.ConfigureOptions(
+       options => options.Configure(
+           opts => opts.Opts = opts.Opts with
+           {
+               Url = "nats://localhost:4222"
+           })));

builder
    .AddGraphQL()
    .AddSubscriptionType<Subscription>()
    .AddNatsSubscriptions();
```

If your code directly references NATS client types, add the `NATS.Client.Core` package as well.

## MaxAllowedNodeBatchSize & EnsureAllNodesCanBeResolved options moved

```diff
builder.AddGraphQL()
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

## Schema.DefaultName moved to ISchemaDefinition.DefaultName

The `Schema.DefaultName` constant is no longer available in v16.
Use `ISchemaDefinition.DefaultName` instead:

```diff
-var schemaName = Schema.DefaultName;
+var schemaName = ISchemaDefinition.DefaultName;
```

If you previously used a string literal for the default schema name, replace it with `ISchemaDefinition.DefaultName` (current value: `_Default`).

## Resolver Selection API changes

In v16, `context.Selection` is a compiled execution selection. The old `context.Selection.SelectionSet` is no longer available.

- `context.Selection.DeclaringSelectionSet` is the parent selection set (where the current field is declared), not the current field's child selection set.
- `context.Selection.SyntaxNodes` now returns `FieldSelectionNode` wrappers. Use `.Node` to access the underlying `FieldNode`.
- Because selections are merged during operation compilation, one execution selection can map to multiple syntax nodes.

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

## Page and cursor API changes

### Page\<T> is now abstract

`Page<T>` can no longer be instantiated directly. Use the static factory methods instead:

- Use `Page<T>.Empty` when you just need to return an empty page.
- Use `Page<T>.Create(...)` when you need to construct a page yourself.

```diff
-return new Page<Product>(
-    items,
-    hasNextPage: hasNext,
-    hasPreviousPage: false,
-    createCursor: product => CreateCursor(product),
-    totalCount: totalCount);
+return Page<Product>.Create(
+    items,
+    hasNextPage: hasNext,
+    hasPreviousPage: false,
+    createCursor: product => CreateCursor(product),
+    totalCount: totalCount);
```

### CreateCursor now takes an index instead of an item

`Page<T>.CreateCursor` previously accepted a `T` item. It now accepts a zero-based `int` index into the page's `Items` array. This enables cursor generation from the underlying source element when a `valueSelector` projection is used.

```diff
-string cursor = page.CreateCursor(page.First);
+string cursor = page.CreateCursor(page.FirstIndex!.Value);
```

Use the new convenience extension methods `CreateStartCursor()` and `CreateEndCursor()` when you only need boundary cursors:

```diff
-var startCursor = page.First is not null ? page.CreateCursor(page.First) : null;
-var endCursor = page.Last is not null ? page.CreateCursor(page.Last) : null;
+var startCursor = page.CreateStartCursor();
+var endCursor = page.CreateEndCursor();
```

Two new properties, `FirstIndex` and `LastIndex`, return the zero-based indices of the first and last items (or `null` for an empty page).

### Edge\<T> constructor changes

A new constructor overload accepts the item, its zero-based index, and a `Func<int, string>` cursor resolver:

```diff
-new Edge<T>(item, cursor: page.CreateCursor)
+new Edge<T>(item, index, cursor: page.CreateCursor)
```

The existing `Edge<T>(T node, Func<T, string> resolveCursor)` constructor is still available for cases where the cursor is resolved from the item itself.

### ToConnectionAsync with custom edge factory

The `ToConnectionAsync` overloads that accept a custom edge factory now pass the zero-based item index instead of the item's cursor:

```diff
-.ToConnectionAsync((source, page) =>
-    new MyEdge(source, edge => page.CreateCursor(edge.Node)));
+.ToConnectionAsync((source, page, index) =>
+    new MyEdge(source, page.CreateCursor(index)));
```

## OperationResult changes

We've removed the `IOperationResult` abstraction. If you've previously pattern-matched on this, you can simply replace it with `OperationResult`. To assert that an `IExecutionResult` is an `OperationResult` in tests, use `result.ExpectOperationResult();`.

We've also switched the `OperationResult.Errors` and `OperationResult.Extensions` properties to always be initialized instead of being nullable. If you were previously asserting these properties as `null` in tests, switch to asserting them as empty instead.

## Skip/include disallowed on root subscription fields

The `@skip` and `@include` directives are now disallowed on root subscription fields, as specified in the RFC: [Prevent @skip and @include on root subscription selection set](https://github.com/graphql/graphql-spec/pull/860).

## Deprecation of fields not deprecated in the interface

Deprecating a field now requires the implemented field in the interface to also be deprecated, as specified in the [draft specification](https://spec.graphql.org/draft/#sec-Objects.Type-Validation).

## Global ID formatter conditionally added to filter fields

Previously, the global ID input value formatter was added to ID filter fields regardless of whether or not Global Object Identification was enabled. This is now conditional.

## fieldCoordinate renamed to coordinate in error extensions

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

## FileValueNode renamed to UploadValueNode

The upload literal node has been renamed from `FileValueNode` to `UploadValueNode`.
If you are referencing this type directly in custom scalar logic or tests, update your code accordingly:

```diff
-if (valueLiteral is FileValueNode fileValue)
+if (valueLiteral is UploadValueNode uploadValue)
 {
    var file = uploadValue.File;
    var key = uploadValue.Key;
 }
```

If you are constructing upload value nodes manually, note that the constructor now also requires the multipart key:

```diff
-var valueNode = new FileValueNode(file);
+var valueNode = new UploadValueNode("0", file);
```

## Errors from TypeConverters are now accessible in the ErrorFilter

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

## Generic ID\<Type>-attribute now infers the actual GraphQL type name

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

## HotChocolate.Fusion.SourceSchema

The `HotChocolate.Fusion.SourceSchema` package has been removed and you can safely remove any references to it from your project. The `[Internal]`, `[Lookup]`, `[Is]`, and `[Require]` attributes have moved to the `HotChocolate.Types` package under the `HotChocolate.Types.Composite` namespace. You don't need to install `HotChocolate.Types` separately — it's already included in the `HotChocolate.AspNetCore` meta-package.

## Merged Assemblies HotChocolate.Types, HotChocolate.Execution, HotChocolate.Fetching

With Hot Chocolate 16 we introduced a lot more abstractions, meaning we pulled out abstractions of the type system or the execution into separate libraries. But at the same time we simplified the implementation of the type system and the execution by moving the implementations of HotChocolate.Execution and HotChocolate.Fetching into HotChocolate.Types. This allowed us to simplify the implementation and make it more efficient.

So, if you were referencing HotChocolate.Execution or HotChocolate.Fetching directly make sure to remove references to these libraries and replace them with HotChocolate.Types.

## Simpler Scalar Type

In v16, creating custom scalar types is more straightforward. The `ScalarType<TRuntimeType>` base class now uses a streamlined API. Instead of overriding both `Serialize`/`Deserialize` and `ParseLiteral`/`ParseValue`/`ParseResult`, you override a smaller set of methods:

- `OnCoerceOutputValue(TRuntimeType runtimeValue, ResultElement resultValue)` -- writes the serialized value directly to the result element
- `OnValueToLiteral(TRuntimeType runtimeValue)` -- converts a runtime value to an AST literal node
- `OnLiteralToValue(IValueNode valueLiteral)` -- converts an AST literal node to a runtime value

The old `Serialize`, `Deserialize`, `ParseLiteral`, `ParseValue`, and `ParseResult` methods still exist on the base `ScalarType` class for backward compatibility, but the new methods on `ScalarType<TRuntimeType>` are the recommended approach.

```diff
-public class MyScalar : ScalarType
+public class MyScalar : ScalarType<MyRuntimeType>
 {
-    public MyScalar() : base("MyScalar") { }
-
-    public override Type RuntimeType => typeof(MyRuntimeType);
-
-    public override bool IsInstanceOfType(IValueNode valueSyntax) => ...;
-    public override object? ParseLiteral(IValueNode valueSyntax) => ...;
-    public override IValueNode ParseValue(object? runtimeValue) => ...;
-    public override IValueNode ParseResult(object? resultValue) => ...;
-    public override bool TrySerialize(object? runtimeValue, out object? resultValue) => ...;
-    public override bool TryDeserialize(object? resultValue, out object? runtimeValue) => ...;
+    public MyScalar() : base("MyScalar") { }
+
+    protected override MyRuntimeType OnLiteralToValue(IValueNode valueLiteral) => ...;
+
+    protected override IValueNode OnValueToLiteral(MyRuntimeType runtimeValue) => ...;
+
+    protected override void OnCoerceOutputValue(
+        MyRuntimeType runtimeValue, ResultElement resultValue) => ...;
 }
```

## Removed Scalars

The following scalar types have been removed in v16. If your schema uses any of them, you need to either remove the usage or re-implement them as custom scalars.

| Removed Scalar     | Description                                          |
| ------------------ | ---------------------------------------------------- |
| `NegativeFloat`    | Represented a float value less than 0                |
| `NonNegativeFloat` | Represented a float value greater than or equal to 0 |
| `NegativeInt`      | Represented an int value less than 0                 |
| `NonPositiveInt`   | Represented an int value less than or equal to 0     |
| `NonEmptyString`   | Represented a non-empty string value                 |
| `NonNegativeInt`   | Represented an int value greater than or equal to 0  |

If you need equivalent validation behavior, create a custom scalar that extends `ScalarType<TRuntimeType>` and validates the value in `OnLiteralToValue` and `OnCoerceOutputValue`.

## OperationRequestBuilder

The `OperationRequestBuilder` has been updated in v16. The most notable changes:

**`AddVariableValues` renamed to `SetVariableValues`**

```diff
var request = OperationRequestBuilder.New()
    .SetDocument("{ hero { name } }")
-   .AddVariableValues(new Dictionary<string, object?> { ["id"] = 1 })
+   .SetVariableValues(new Dictionary<string, object?> { ["id"] = 1 })
    .Build();
```

**Variable values are now JSON-based**

`SetVariableValues` now accepts JSON strings, `JsonDocument`, `IEnumerable<KeyValuePair<string, JsonElement>>`, or `IReadOnlyDictionary<string, object?>`. When you pass a dictionary of CLR objects, values are serialized to JSON internally. You can also pass variables directly as a JSON string:

```csharp
var request = OperationRequestBuilder.New()
    .SetDocument("query ($id: ID!) { node(id: $id) { id } }")
    .SetVariableValues("""{ "id": "42" }""")
    .Build();
```

**Global state methods**

The context data methods have been renamed:

```diff
-builder.AddProperty("key", value);
+builder.SetGlobalState("key", value);
```

Additional methods include `AddGlobalState`, `TryAddGlobalState`, and `RemoveGlobalState`.

**`From` factory method**

Use `OperationRequestBuilder.From(request)` to create a builder pre-populated from an existing request, instead of manually copying properties.

**Features collection**

The builder now exposes a `Features` property of type `IFeatureCollection` for attaching extensibility features (such as `IFileLookup` for file uploads).

## Any and Json scalars merged

The `Json` scalar has been removed and its functionality merged into the `Any` scalar. The `Any` scalar now uses `System.Text.Json.JsonElement` as its .NET runtime type, which was previously the runtime type of the `Json` scalar.

**`JsonElement` is now inferred as `Any` instead of `Json`.** If you used `[GraphQLType<JsonType>]` annotations or explicit `JsonType` bindings, replace them with `AnyType`:

```csharp
// before
[GraphQLType<JsonType>]
public JsonElement GetData() => ...;

// after
[GraphQLType<AnyType>]
public JsonElement GetData() => ...;
```

### Returning dictionaries or arbitrary .NET types

If you previously returned `Dictionary<string, object>` or other .NET types from a field typed as `Json` or `Any`, you now need to register the JSON type converter explicitly. Without it, the type system has no way to convert arbitrary .NET types to `JsonElement`:

```csharp
builder
    .AddGraphQL()
    .AddJsonTypeConverter();
```

For custom reference types that need specific serialization, register a dedicated converter instead:

```csharp
builder
    .AddGraphQL()
    .AddTypeConverter<TimeZoneInfo, JsonElement>(
        value => JsonSerializer.SerializeToElement(value.Id));
```

### Any input fields now deserialize complex types as JsonElement

Previously, complex input values for `Any`-typed input variables were deserialized as `IDictionary<string, object?>`. They are now deserialized as `JsonElement`, aligning input behavior with arbitrary output types.

```csharp
public string Foo([GraphQLType<AnyType>]object? input) => input?.GetType().Name;
```

```graphql
query {
  foo(input: { key: "value" })
  # Now returns: "JsonElement"
  # Previously (v15): "Dictionary`2"
}
```

### Runtime objects passed as variables to OperationRequestBuilder are now serialized as JSON

Passing CLR objects via `OperationRequestBuilder.SetVariableValues(Dictionary<string, object?>)` now serializes the values as JSON.

You may prefer providing variables directly as JSON:

```csharp
var requestBuilder = new OperationRequestBuilder();
requestBuilder.SetVariableValues("""{ "id": 42 }""");
```

Note that this can lead to errors if the emitted JSON for a type is not valid for the corresponding GraphQL scalar, f. e. du to format restrictions.
For example, a `DateTime` value can no longer be used to fill a `Date` scalar since the JSON format does not match the expected yyyy-MM-dd format.

You can also bypass this by annotating your types with custom JsonConverters.

If you need to pass an Upload scalar value, you can do the following:

```csharp
var requestBuilder = new OperationRequestBuilder();
requestBuilder.SetVariableValues("""{ "file" : "yourKey" }""");
requestBuilder.Features.Set<IFileLookup>(fileLookup);

public class FileLookup : IFileLookup
{
    public bool TryGetFile(string name, [NotNullWhen(true)] out IFile? file)
    {
        if (name == "yourKey")
        {
            file = new StreamFile("Foo.txt", () => new MemoryStream());
            return true;
        }

        file = null;
        return false;
    }
}
```

## Byte and SignedByte types renamed

- The GraphQL type `Byte` has been renamed to `UnsignedByte` (CLR type: `byte`).
- The GraphQL type `SignedByte` has been renamed to `Byte` (CLR type: `sbyte`).

This is to align the GraphQL type names with the core types (`Int`, etc.), which are signed.

## Byte arrays now mapped to Base64String

C# byte arrays (`byte[]`) are now mapped to the GraphQL `Base64String` type by default, as the `ByteArray` type has been deprecated.

## Uri now mapped to URI scalar instead of URL

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

## GUIDs converted to strings using the "D" format

The conversion from GUID to string in the default type converter has been updated to format with hyphens (format "D") instead of without (format "N"), to follow the documented behavior.

## EnableOneOf option removed

The `EnableOneOf` option has been removed, as the `@oneOf` directive is now built in.

## GraphQLToolOptions replaced by NitroAppOptions

The `GraphQLToolOptions` class has been removed. Nitro configuration is now done directly through `NitroAppOptions` from the `ChilliCream.Nitro.App` namespace.

The `GraphQLServerOptions.Tool` property is now of type `NitroAppOptions` instead of `GraphQLToolOptions`.

### WithOptions now uses a delegate pattern

Per-endpoint `WithOptions` overrides now use a delegate pattern instead of object initializers:

```diff
endpoints.MapGraphQL()
-   .WithOptions(o => o.Tool.Enable = false);
+   .WithOptions(o => o.Tool.Enable = false);
// No change for GraphQLServerOptions — already used delegates

endpoints.MapNitroApp()
-   .WithOptions(new GraphQLToolOptions { Enable = false });
+   .WithOptions(o => o.Enable = false);
```

### GraphQLToolServeMode replaced by ServeMode

Replace `GraphQLToolServeMode` with `ServeMode` from `ChilliCream.Nitro.App`:

```diff
-using HotChocolate.AspNetCore;
+using ChilliCream.Nitro.App;

-GraphQLToolServeMode.Embedded   → ServeMode.Embedded
-GraphQLToolServeMode.Latest     → ServeMode.Latest
-GraphQLToolServeMode.Insider    → ServeMode.Insider
-GraphQLToolServeMode.Version(v) → ServeMode.Version(v)
```

### DefaultHttpMethod replaced by UseGet

The `DefaultHttpMethod` enum has been removed. Use the `UseGet` boolean property on `NitroAppOptions` instead:

```diff
-o.HttpMethod = DefaultHttpMethod.Get;
+o.UseGet = true;
```

## Server options now configured via ModifyServerOptions

`GraphQLServerOptions` (GET requests, multipart, batching, schema requests, etc.) are now configured at the schema level using `ModifyServerOptions` instead of per-endpoint:

```diff
builder.AddGraphQL()
+   .ModifyServerOptions(o =>
+   {
+       o.EnableGetRequests = false;
+       o.Batching = AllowedBatching.All;
+   });
```

Per-endpoint overrides are still supported via `WithOptions` on the endpoint builder:

```csharp
endpoints.MapGraphQL().WithOptions(o => o.EnableGetRequests = false);
```

## Batching is now disabled by default

In v15, request batching was enabled by default (`EnableBatching = true`). In v16, batching is **disabled by default** as a security measure. The `EnableBatching` property has been replaced by `Batching`, which uses the `AllowedBatching` flags enum for fine-grained control:

```diff
-o.EnableBatching = true;
+o.Batching = AllowedBatching.All;
```

If you were relying on the previous default, you need to explicitly enable batching:

```csharp
builder.AddGraphQL()
    .ModifyServerOptions(o => o.Batching = AllowedBatching.All);
```

Additionally, a new `MaxBatchSize` property limits the number of operations in a single batch. The default is **1024**. Set it to `0` for unlimited.

> Note: Fusion subgraphs automatically enable batching via `AddSourceSchemaDefaults()`. No action is needed for subgraphs.

For more details, see [Batching](/docs/hotchocolate/v16/server/batching).

## New default incremental delivery format for @defer and @stream

Hot Chocolate v16 changes the default wire format for incremental delivery (`@defer` / `@stream`) from the legacy path-based format (v0.1) to the newer id-based format (v0.2). This affects all streaming transports: multipart, SSE, and JSON Lines.

**v0.1 (legacy)** used `path` and `label` to identify deferred fragments:

```json
{"data":{"product":{"name":"Abc"}},"hasNext":true}
{"incremental":[{"data":{"description":"Abc desc"},"path":["product"]}],"hasNext":false}
```

**v0.2 (new default)** uses `pending`, `incremental` with `id`, and `completed`:

```json
{"data":{"product":{"name":"Abc"}},"pending":[{"id":"2","path":["product"]}],"hasNext":true}
{"incremental":[{"id":"2","data":{"description":"Abc desc"}}],"completed":[{"id":"2"}],"hasNext":false}
```

If your clients depend on the legacy format, you have two options:

**Option 1: Client sends `incrementalSpec=v0.1` in the `Accept` header**

Clients can opt into the legacy format per-request by adding the `incrementalSpec` parameter to the `Accept` header:

```text
Accept: multipart/mixed; incrementalSpec=v0.1
Accept: text/event-stream; incrementalSpec=v0.1
Accept: application/jsonl; incrementalSpec=v0.1
```

**Option 2: Change the server default**

To restore v0.1 as the server-wide default (used when the client doesn't specify `incrementalSpec`):

```csharp
builder
    .AddGraphQL()
    .AddHttpResponseFormatter(
        incrementalDeliveryFormat: IncrementalDeliveryFormat.Version_0_1);
```

Or with the options overload:

```csharp
builder
    .AddGraphQL()
    .AddHttpResponseFormatter(
        new HttpResponseFormatterOptions { /* ... */ },
        incrementalDeliveryFormat: IncrementalDeliveryFormat.Version_0_1);
```

## OperationRequestBuilder.AddVariableValues renamed to SetVariableValues

`OperationRequestBuilder.AddVariableValues` has been renamed to `SetVariableValues`.

## TimeSpan scalar renamed to Duration

The `TimeSpan` scalar has been renamed to `Duration` to better reflect the underlying specification (ISO 8601), and move away from .NET-oriented naming.

For backwards compatibility, you can rename the type as follows:

```csharp
builder
    .AddGraphQL()
    .AddType(new DurationType("TimeSpan"));
```

## NodaTime scalars now implement the GraphQL scalar specifications

The `HotChocolate.Types.NodaTime` package was rewritten in v16 to align its scalar behavior with the specifications published on [scalars.graphql.org](https://scalars.graphql.org/).
This is a breaking change if you relied on the old NodaTime scalar set or on the looser parsing behavior of the previous implementation.

### Only five NodaTime scalars remain built in

The package now only ships these spec-based scalar implementations:

- `DateTimeType`
- `DurationType`
- `LocalDateType`
- `LocalDateTimeType`
- `LocalTimeType`

These scalars expose `@specifiedBy` URLs and follow the corresponding scalar specifications for parsing and serialization.

### Legacy NodaTime scalars were removed

The following scalar types are no longer included in `HotChocolate.Types.NodaTime`:

- `DateTimeZoneType`
- `InstantType`
- `IsoDayOfWeekType`
- `OffsetDateType`
- `OffsetTimeType`
- `OffsetType`
- `PeriodType`
- `ZonedDateTimeType`

If your schema used any of these scalars in v15, your project will no longer compile after upgrading until you remove them or provide your own replacement implementations.

If you still need one of the removed scalars, add it back manually in your application as a custom scalar.

### Use AddNodaTime() to register the new scalars

v16 adds a dedicated `AddNodaTime()` extension method that registers all five built-in NodaTime scalars and the related CLR bindings and converters:

```diff
builder
    .AddGraphQL()
-   .AddType<DateTimeType>()
-   .AddType<DurationType>()
-   .AddType<LocalDateType>()
-   .AddType<LocalDateTimeType>()
-   .AddType<LocalTimeType>();
+   .AddNodaTime();
```

`AddNodaTime()` also configures these runtime type mappings:

- `DateTimeOffset` to `DateTimeType`
- `DateTime` to `LocalDateTimeType`
- `DateOnly` to `LocalDateType`
- `TimeOnly` to `LocalTimeType`

If you prefer, you can still register the remaining scalar types individually instead of using `AddNodaTime()`.

## AddInstrumentation

### InstrumentationOptions changes

- `RenameRootActivity` was removed.
- `RequestDetails.Operation` was renamed to `RequestDetails.OperationName`.
- `RequestDetails.Query` was renamed to `RequestDetails.Document`.

## OpenTelemetry GraphQL semantic conventions alignment

The OpenTelemetry spans, events, and attributes emitted by `AddInstrumentation()` (and the Fusion equivalent) have been aligned with the [proposed OpenTelemetry GraphQL semantic conventions](https://github.com/open-telemetry/semantic-conventions/pull/3515).

If you have dashboards, alerts, or span processors that filter on `graphql.*` attribute names, you will need to update them.

Besides the attribute renames, the most notable changes are:

- The root GraphQL span's display name now contains only the operation type (`query`, `mutation`, `subscription`), keeping cardinality low. The operation name remains available on the `graphql.operation.name` span attribute.
- The `graphql.error` event is now emitted on the root `GraphQL Operation` span only, with combined GraphQL error and exception attributes.
- A new `MaxErrorEvents` option (default `10`) caps the number of error events per request.

### Removed attributes

| Attribute                                  | Reason                                                      |
| ------------------------------------------ | ----------------------------------------------------------- |
| `graphql.operation.id`                     | Not part of the OpenTelemetry GraphQL semantic conventions. |
| `graphql.selection.type`                   | Not part of the OpenTelemetry GraphQL semantic conventions. |
| `graphql.selection.hierarchy`              | Not part of the OpenTelemetry GraphQL semantic conventions. |
| `graphql.source.id` _(Fusion)_             | Replaced by `graphql.source_schema.name`.                   |
| `graphql.source.operation.type` _(Fusion)_ | Duplicated `graphql.operation.type`.                        |

### Renamed attributes

| Old Attribute                              | New Attribute                          |
| ------------------------------------------ | -------------------------------------- |
| `graphql.operation.kind`                   | `graphql.operation.type`               |
| `graphql.selection.name`                   | `graphql.field.alias`                  |
| `graphql.selection.path`                   | `graphql.field.path`                   |
| `graphql.selection.field.name`             | `graphql.field.name`                   |
| `graphql.selection.field.parent_type`      | `graphql.field.parent_type`            |
| `graphql.selection.field.coordinate`       | `graphql.field.coordinate`             |
| `graphql.selection.field.declaringType`    | `graphql.field.parent_type`            |
| `graphql.dataLoader.keys.count`            | `graphql.dataloader.batch.size`        |
| `graphql.dataLoader.keys`                  | `graphql.dataloader.batch.keys`        |
| `graphql.error.path`                       | `graphql.field.path` _(reused)_        |
| `graphql.error.locations`                  | `graphql.document.locations`           |
| `graphql.error.location.line/column`       | `graphql.document.locations`           |
| `graphql.fusion.node.schema`               | `graphql.source_schema.name`           |
| `graphql.fusion.node.type`                 | `graphql.operation.step.kind`          |
| `graphql.source.name` _(Fusion)_           | `graphql.source_schema.name`           |
| `graphql.source.operation.name` _(Fusion)_ | `graphql.source_schema.operation.name` |
| `graphql.source.operation.hash` _(Fusion)_ | `graphql.source_schema.operation.hash` |

### Added attributes

| Attribute                         | Where                                                     |
| --------------------------------- | --------------------------------------------------------- |
| `graphql.processing.type=request` | Root `GraphQL Operation` span (now required by spec).     |
| `graphql.field.schema_coordinate` | `graphql.error` event (when present in error extensions). |

### Changed attribute values

| Attribute                | Old Value                             | New Value                                           |
| ------------------------ | ------------------------------------- | --------------------------------------------------- |
| `graphql.operation.type` | `Query` / `Mutation` / `Subscription` | `query` / `mutation` / `subscription`               |
| `graphql.http.kind`      | `operation-batch`                     | `operation_batch`                                   |
| `graphql.document.hash`  | `<hash>`                              | `<hash-algorithm>:<hash>` , e.g. `md5:<hash>`       |
| `graphql.document.id`    | -                                     | Value is only set if document is a trusted document |

### `graphql.error` event moved to the root span

Previously the `graphql.error` event was attached to the resolver, validation, or parsing span where the error originated. The event is now emitted on the root `GraphQL Operation` span, with the field path preserved as the `graphql.field.path` attribute on the event. This aligns with the spec, supports long-lived subscription operations, and lets you aggregate errors per request.

The event always carries `exception.type`, `exception.message`, and `exception.stacktrace` derived from the underlying exception (or the GraphQL error itself, if no exception is attached).

The number of `graphql.error` events per request is capped via the new `MaxErrorEvents` option (default `10`). The total error count remains available on the root span as `graphql.error.count`, independent of the cap.

```csharp
builder.AddGraphQL()
    .AddInstrumentation(o => o.MaxErrorEvents = 25);
```

### `error.type` value policy

`error.type` now reflects the GraphQL error's `extensions.code` when present, falling back to the underlying exception type or to a phase-appropriate identifier (`EXECUTION_ERROR`, `GRAPHQL_VALIDATION_FAILED`, `GRAPHQL_PARSE_FAILED`).

### Custom enricher changes

If you've implemented a custom `ActivityEnricher`, you no longer need to pass the `ObjectPool<StringBuilder>` down to the base class:

```diff
public class CustomActivityEnricher(
-  ObjectPool<StringBuilder> stringBuilderPool,
  InstrumentationOptions options
-) : ActivityEnricher(stringBuilderPool, options);
+) : ActivityEnricher(options);
```

There have also been some changes to the methods you can override in your enricher:

| v15                                                                       | v16                                                                                                                                                        |
| ------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `EnrichParserErrors(HttpContext, IError, Activity)`                       | Replaced by `EnrichParserErrors(HttpContext, IReadOnlyList<IError>, Activity)`.                                                                            |
| `EnrichRequestError(RequestContext, Activity, Exception)`                 | Replaced by `EnrichRequestError(RequestContext, Exception, Activity)`.                                                                                     |
| `EnrichRequestError(RequestContext, Activity, IError)`                    | Replaced by `EnrichRequestError(RequestContext, IError, Activity)`.                                                                                        |
| `EnrichValidationError(RequestContext, Activity, IError)`                 | Replaced by `EnrichValidationErrors(RequestContext, IReadOnlyList<IError>, Activity)`.                                                                     |
| `EnrichAnalyzeOperationComplexity(RequestContext, Activity)`              | Replaced by `EnrichAnalyzeOperationCost(RequestContext, Activity)`.                                                                                        |
| `EnrichDataLoaderBatch<TKey>(IDataLoader, IReadOnlyList<TKey>, Activity)` | Replaced by `EnrichExecuteBatch<TKey>(IDataLoader, IReadOnlyList<TKey>, Activity)`.                                                                        |
| `EnrichResolverError(RequestContext, IError, Activity)`                   | Removed. Use `EnrichRequestError(...)` for request-level errors and `EnrichResolverError(IMiddlewareContext, IError, Activity)` for field resolver errors. |
| `EnrichRequestVariables(...)`                                             | Removed.                                                                                                                                                   |
| `EnrichBatchVariables(...)`                                               | Removed.                                                                                                                                                   |
| `EnrichRequestExtensions(...)`                                            | Removed.                                                                                                                                                   |
| `EnrichBatchExtensions(...)`                                              | Removed.                                                                                                                                                   |
| `CreateOperationDisplayName(...)`                                         | Removed.                                                                                                                                                   |
| `CreateRootActivityName(...)`                                             | Removed.                                                                                                                                                   |
| `EnrichError(...)`                                                        | Removed.                                                                                                                                                   |

> Note: Overriding enricher methods without calling `base` no longer prevents the standard span attributes from being emitted. The semantic-convention attributes are now applied by the instrumentation itself, and custom enrichers are only intended for adding extra information.

## Diagnostic Listeners

We removed the following methods from the `IExecutionDiagnosticEventListener` since they no longer apply:

- `ExecuteStream`
- `ExecuteDeferredTask`
- `DispatchBatch`
- `SubscriptionTransportError`
- `SubscriptionEventResult`

Some other methods also had a change in their signature - simply override them again to fix any compilation issues.

## Experimental @semanticNonNull support removed

Hot Chocolate v15 included experimental support for the `@semanticNonNull` directive, which let you mark fields as semantically non-null while still returning `null` (rather than propagating to the parent) when a resolver errored. We've removed this feature in v16 in favor of the [`onError` proposal](https://github.com/graphql/graphql-spec/pull/1163).

If you previously opted in to this feature, remove the option:

```diff
builder.AddGraphQL()
    .ModifyOptions(o =>
    {
-       o.EnableSemanticNonNull = true;
    });
```

If you still need to keep the behavior of not propagating nulls for errors on non-null fields, set the `DefaultErrorHandlingMode` to `ErrorHandlingMode.Null`:

```csharp
builder
    .AddGraphQL()
    .ModifyOptions(o => o.DefaultErrorHandlingMode = ErrorHandlingMode.Null);
```

### Clients that still need a schema with @semanticNonNull annotations

If you have a client that still relies on the schema being annotated with `@semanticNonNull`, you have a few options to obtain such a schema.

**Schema snapshot tests**

If you're producing a schema string for snapshot tests like this:

```csharp
ISchemaDefinition schema = await new ServiceCollection()
    .AddGraphQL()
    // ...
    .BuildSchemaAsync();

string schemaStr = schema.ToString();

// assert schemaStr ...
```

Switch to `SchemaFormatter` with `RewriteToSemanticNonNull` enabled:

```csharp
string schemaStr = SchemaFormatter.FormatAsString(
    schema,
    new SchemaFormatterOptions { RewriteToSemanticNonNull = true });
```

**Downloading the schema from the server**

If you're using `MapGraphQLSchema()` to expose the schema at `/graphql/schema`, you can additionally call `MapGraphQLSemanticNonNullSchema()` to expose a variant annotated with `@semanticNonNull` at `/graphql/semantic-non-null-schema.graphql`:

```csharp
app.MapGraphQLSchema();
app.MapGraphQLSemanticNonNullSchema();
```

**Exporting the schema via the CLI**

If you're using the schema export command, add the `--semantic-non-null` flag to emit the schema with `@semanticNonNull` annotations:

```bash
dotnet run -- schema export --output schema.graphql --semantic-non-null
```

# Deprecations

Things that will continue to function this release, but we encourage you to move away from.

## ByteArray

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

## Parser recursion depth limit

The parser now enforces a maximum recursion depth of **200** by default. Deeply nested selection sets, list values, object values, or type references that exceed this depth are rejected with a `SyntaxException` instead of causing a stack overflow. If your queries legitimately exceed this depth, increase the limit:

```csharp
builder
    .AddGraphQL()
    .ModifyParserOptions(o =>
    {
        o.MaxAllowedRecursionDepth = 500;
    });
```

## Parser directive limit

The parser now limits the number of directives per location (field, operation, fragment definition) to **4** by default. Documents with more directives on a single location are rejected at parse time. If you use more than 4 directives per location, increase the limit:

```csharp
builder
    .AddGraphQL()
    .ModifyParserOptions(o =>
    {
        o.MaxAllowedDirectives = 8;
    });
```

## Fragment visit budget

Validation now caps the total number of fragment visits per operation at **1,000** by default. Each time a fragment spread is entered during validation counts as one visit. Queries with deeply nested or heavily reused fragment spreads that exceed this budget will have remaining fragments skipped during validation. If you have complex queries with many fragment spreads, increase the limit:

```csharp
builder
    .AddGraphQL()
    .ModifyValidationOptions(o =>
    {
        o.MaxAllowedFragmentVisits = 5_000;
    });
```

## Field merge comparison budget

The overlapping-fields-can-be-merged validation rule now caps comparison work at **100,000** by default. Queries that exceed this budget are rejected. If you have very complex queries that trigger this limit, increase it:

```csharp
builder
    .AddGraphQL()
    .SetMaxAllowedFieldMergeComparisons(200_000);
```

## Concurrent execution gate

Hot Chocolate v16 introduces a concurrency gate that limits how many GraphQL operations execute at the same time. The gate sits in the request pipeline just before operation execution and applies uniformly to queries, mutations, subscription handshakes, and each subscription event.

Configure the limit through `ModifyServerOptions`:

```csharp
builder
    .AddGraphQL()
    .ModifyServerOptions(o => o.MaxConcurrentExecutions = 128);
```

The default is **64**. Operations that arrive while the gate is full queue up and run as slots free. Set the limit to `null` to disable the gate entirely.

Every execution is bounded by the `ExecutionTimeout` option (default 30 seconds). This applies uniformly to queries, mutations, subscription handshakes, and each subscription event. The budget covers both the time an execution spends waiting for a concurrency slot and the time it spends running. When the budget is exceeded, the execution is cancelled and the caller receives a clean timeout error. `ExecutionTimeout` is the single setting that controls cancellation for every execution.

Subscriptions participate in the limit like any other operation. The initial subscribe consumes a slot while the subscribe resolver runs, and each emitted event consumes a slot while its result is being produced. Idle subscriptions (waiting on the next event) cost nothing. The slot is released between events.
