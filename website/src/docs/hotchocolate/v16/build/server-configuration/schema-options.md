---
title: Schema options
---

# Configuring Schema Options

Schema options let you control how Hot Chocolate v16 builds the schema and type system. These options are applied before any GraphQL requests are processed, making them the right place to set root type names, binding conventions, schema validation, directive metadata, schema output ordering, startup behavior, and per-executor schema caches.

You can configure schema options using `ModifyOptions` on the `IRequestExecutorBuilder` returned by `builder.AddGraphQL()` or `builder.Services.AddGraphQLServer()`:

```csharp
using HotChocolate.Types;

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .ModifyOptions(options =>
    {
        options.StrictValidation = true;
        options.DefaultBindingBehavior = BindingBehavior.Implicit;
        options.SortFieldsByName = builder.Environment.IsDevelopment();
        options.PreparedOperationCacheSize = 512;
        options.OperationDocumentCacheSize = 512;
    });
```

Do not use `ModifyOptions` for endpoint, HTTP transport, request validation, cost analysis, or Nitro settings. These are configured through other APIs.

| Goal                                                            | Use                                                                                 | Schema option page? |
| --------------------------------------------------------------- | ----------------------------------------------------------------------------------- | ------------------- |
| Rename operation root types                                     | `ModifyOptions` with `QueryTypeName`, `MutationTypeName`, or `SubscriptionTypeName` | Yes                 |
| Change implicit or explicit member binding                      | `ModifyOptions` or type descriptors                                                 | Yes                 |
| Control XML documentation extraction                            | `ModifyOptions` with `UseXmlDocumentation`                                          | Yes                 |
| Control directive visibility or applied directive introspection | `ModifyOptions`                                                                     | Yes                 |
| Disable operation introspection queries                         | Use validation APIs like `DisableIntrospection(...)` or `AllowIntrospection(false)` | Boundary only       |
| Hide SDL downloads or configure Nitro                           | Use endpoint or server options                                                      | No                  |
| Configure GET, multipart requests, batching, or WebSockets      | Use server and transport options                                                    | No                  |
| Limit parser, validation, execution, or cost work               | Use parser, validation, request, or cost options                                    | No                  |

# Selecting Options for Common Tasks

| Task                                            | Option or API                                               | Default    | Guidance                                                                                              |
| ----------------------------------------------- | ----------------------------------------------------------- | ---------- | ----------------------------------------------------------------------------------------------------- |
| Keep schema validation strict                   | `StrictValidation = true`                                   | `true`     | Keep this enabled. Only disable for brief migration diagnostics.                                      |
| Validate data middleware order                  | `ValidatePipelineOrder = true`                              | `true`     | Leave enabled so invalid paging, projection, filtering, and sorting order fails at schema build time. |
| Rename root operation types                     | `QueryTypeName`, `MutationTypeName`, `SubscriptionTypeName` | `null`     | Use custom names for schema-first compatibility or a published SDL contract.                          |
| Make field discovery explicit                   | `DefaultBindingBehavior = BindingBehavior.Explicit`         | `Implicit` | Use for the whole schema only when every type is intentionally configured.                            |
| Include static members during discovery         | `DefaultFieldBindingFlags`                                  | `Instance` | Set to instance plus static when root or extension types use static resolver members by convention.   |
| Stabilize SDL field order                       | `SortFieldsByName = true`                                   | `false`    | Enable for schema reviews, snapshots, and generated artifacts.                                        |
| Remove unreachable registered types             | `RemoveUnreachableTypes = true` or `TrimTypes()`            | `false`    | Use when you register broad assemblies but want the executable schema trimmed to reachable types.     |
| Keep unused directive definitions               | `RemoveUnusedTypeSystemDirectives = false`                  | `true`     | Use when schema registries or SDL consumers need unused type-system directive definitions.            |
| Expose applied directives through introspection | `EnableDirectiveIntrospection = true`                       | `false`    | Enable when tooling must read directive applications from introspection.                              |
| Enable opt-in feature metadata                  | `EnableOptInFeatures = true`                                | `false`    | Required before using `@requiresOptIn`.                                                               |
| Enable incremental delivery directives          | `EnableDefer`, `EnableStream`                               | `false`    | Enable only when your transport and clients support incremental delivery.                             |
| Keep startup eager                              | `LazyInitialization = false`                                | `false`    | Leave eager initialization on so schema errors surface during startup.                                |
| Tune operation caches                           | `PreparedOperationCacheSize`, `OperationDocumentCacheSize`  | `256`      | Use values of at least `16`.                                                                          |

# Configuring Root Operation Type Names

By default, Hot Chocolate uses the conventional root type names: `Query`, `Mutation`, and `Subscription` when the root name options are set to `null`. Set custom root names if you are importing SDL with different root names or if an existing client contract requires them.

```csharp
using HotChocolate.Types;

builder
    .AddGraphQL()
    .ModifyOptions(options =>
    {
        options.QueryTypeName = "RootQuery";
        options.MutationTypeName = "RootMutation";
        options.SubscriptionTypeName = "RootSubscription";
    })
    .AddType<RootQueryType>();

public sealed class RootQuery
{
    public Product GetProduct() => new("Trail Backpack");
}

public sealed class RootQueryType : ObjectType<RootQuery>
{
    protected override void Configure(IObjectTypeDescriptor<RootQuery> descriptor)
    {
        descriptor.Name("RootQuery");
        descriptor.Field(t => t.GetProduct()).Name("product");
    }
}

public sealed record Product(string Name);
```

The resulting SDL will look like this:

```graphql
schema {
  query: RootQuery
}

type RootQuery {
  product: Product!
}
```

If you only need to register standard root types, you can keep the default names and use `AddQueryType<T>()`, `AddMutationType<T>()`, or generated root type attributes.

# Controlling Naming and Member Binding

By default, Hot Chocolate infers fields from public members. This is convenient for small DTOs, but it can unintentionally expose helper members, foreign keys, or static members.

Use global binding options when your schema follows a consistent convention.

```csharp
using HotChocolate.Types;

builder
    .AddGraphQL()
    .ModifyOptions(options =>
    {
        options.DefaultBindingBehavior = BindingBehavior.Explicit;
    });
```

With global explicit binding, convention-bound object types do not expose any fields until you configure them. This rule also applies to root operation types.

For stricter contracts on specific types, prefer descriptor-level binding:

```csharp
using HotChocolate.Types;

public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.Field(t => t.Id).Type<NonNullType<IdType>>();
        descriptor.Field(t => t.Name);
        descriptor.Field(t => t.Price);
    }
}

public sealed class Product
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int DatabaseId { get; init; }
}
```

The expected SDL:

```graphql
type Product {
  id: ID!
  name: String!
  price: Decimal!
}
```

The `DefaultFieldBindingFlags` option controls whether implicit object type binding includes instance members, static members, or both. The default is `FieldBindingFlags.Instance`.

```csharp
using HotChocolate.Types;

builder
    .AddGraphQL()
    .ModifyOptions(options =>
    {
        options.DefaultFieldBindingFlags =
            FieldBindingFlags.Instance | FieldBindingFlags.Static;
    });
```

There is a designed interaction between these two options:

- Setting `DefaultBindingBehavior` to `Explicit` resets `DefaultFieldBindingFlags` to `FieldBindingFlags.Default`.
- Setting `DefaultFieldBindingFlags` to any non-default value switches `DefaultBindingBehavior` back to `Implicit`.

# Keeping Schema Validation Strict

The `StrictValidation` option is enabled by default. Keep this setting on so schema errors are caught at startup, not after deployment. Strict validation enforces rules such as operation root validity, object fields, directive usage, input definitions, and other type-system constraints.

```csharp
builder
    .AddGraphQL()
    .ModifyOptions(options =>
    {
        options.StrictValidation = true;
        options.ValidatePipelineOrder = true;
    });
```

`ValidatePipelineOrder` is also enabled by default. It checks the order of known data middleware, including paging, projection, filtering, and sorting. If you encounter errors, fix the middleware order rather than disabling this check.

Use the following advanced runtime type options only if you control the abstract type resolution strategy:

- `StrictRuntimeTypeValidation` (default: `false`): Enable when interface and union runtime values must match declared possible types without fallback behavior.
- `DefaultIsOfTypeCheck` (default: `null`): Provide a fallback only if you need custom abstract type checks across the schema.

# Configuring Nullability and One-Of Behavior

GraphQL nullability is determined by your C# nullability annotations and descriptor configuration, not by a schema option. Enable nullable reference types in your project and publish the weakest contract that accurately describes your API to clients.

```xml
<PropertyGroup>
  <Nullable>enable</Nullable>
</PropertyGroup>
```

```csharp
public sealed class Product
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}
```

The resulting SDL:

```graphql
type Product {
  name: String!
  description: String
}
```

Hot Chocolate v16 does not include `EnableSemanticNonNull` in `SchemaOptions`. The experimental `@semanticNonNull` opt-in from v15 was removed. If a legacy client still requires semantic non-null SDL, use the schema formatter option `RewriteToSemanticNonNull`, `MapGraphQLSemanticNonNullSchema()`, or the schema export command with `--semantic-non-null`. These are schema printing and endpoint concerns, not schema options.

One-of input objects are now built in. Do not use an `EnableOneOf` schema option in v16. Instead, mark the input with `[OneOf]` or configure the input descriptor with `OneOf()`:

```csharp
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Relay;

[OneOf]
public sealed class ProductSelectorInput
{
    [ID<Product>]
    public int? Id { get; set; }
    public string? Sku { get; set; }
}
```

```csharp
using HotChocolate.Types;

public sealed class ProductSelectorInputType : InputObjectType<ProductSelectorInput>
{
    protected override void Configure(
        IInputObjectTypeDescriptor<ProductSelectorInput> descriptor)
    {
        descriptor.OneOf();
        descriptor.Field(t => t.Id).ID<Product>();
        descriptor.Field(t => t.Sku);
    }
}
```

The expected SDL:

```graphql
input ProductSelectorInput @oneOf {
  id: ID
  sku: String
}
```

A one-of input must define nullable fields, must not define field defaults, and must receive exactly one non-null value from the client.

# Managing Descriptions, Deprecations, and Opt-In Metadata

By default, XML documentation extraction is enabled with `UseXmlDocumentation = true`. This setting reads generated XML documentation files and uses summaries and parameter documentation as schema descriptions.

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>$(NoWarn);1591</NoWarn>
</PropertyGroup>
```

Turn this option off if you want descriptions to come only from attributes, schema-first SDL, or descriptor configuration:

```csharp
builder
    .AddGraphQL()
    .ModifyOptions(options => options.UseXmlDocumentation = false);
```

Use `ResolveXmlDocumentationFileName` if your XML documentation files have non-standard names.

There is no global schema option for deprecation. Add deprecations directly where you define the schema member:

```csharp
using HotChocolate;
using HotChocolate.Types;

public sealed class Product
{
    [GraphQLDeprecated("Use sku instead.")]
    public string LegacySku { get; init; } = string.Empty;
}

public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor
            .Field(t => t.LegacySku)
            .Deprecated("Use sku instead.");
    }
}
```

Set `EnableOptInFeatures = true` before applying `@requiresOptIn` metadata. Use `EnableTag = true` to make the built-in `@tag` directive available. `EnableTag` is enabled by default.

# Configuring Directive and Type-System Behavior

Directive options affect type-system metadata and introspection. They do not block standard operation introspection queries and do not control SDL endpoint exposure.

```csharp
using HotChocolate.Configuration;

builder
    .AddGraphQL()
    .ModifyOptions(options =>
    {
        options.EnableDirectiveIntrospection = true;
        options.DefaultDirectiveVisibility = DirectiveVisibility.Public;
        options.RemoveUnusedTypeSystemDirectives = false;
        options.EnableOptInFeatures = true;
    });
```

Use these settings along with directive descriptors:

- `EnableDirectiveIntrospection` adds applied directive introspection fields such as `appliedDirectives`.
- `DefaultDirectiveVisibility` sets whether directive types are public or internal by default.
- `DisableInternalDirectives = true` treats internal directives as public. Use this mainly for v15 compatibility, as it can expose metadata like authorization directives.
- `RemoveUnusedTypeSystemDirectives` determines whether unused type-system directive definitions remain in the final schema.

Other type-system feature switches include:

- `EnableDefer` and `EnableStream` register the incremental delivery directives.
- `EnableFlagEnums` changes how `[Flags]` enums are inferred.
- `StripLeadingIFromInterface` removes a leading `I` from inferred GraphQL interface names.
- `EnableSemanticIntrospection` adds Hot Chocolate semantic introspection fields such as `__search` and `__definitions`. This is enabled by default and is separate from semantic non-null.

# Stabilizing SDL and Trimming Schema Artifacts

Enable `SortFieldsByName` when field order is important for schema reviews, snapshots, generated artifacts, or registry diffs.

```csharp
builder
    .AddGraphQL()
    .ModifyOptions(options => options.SortFieldsByName = true);
```

With sorting enabled, the SDL output might look like:

```graphql
type Query {
  apple: String!
  zebra: String!
}
```

Use `RemoveUnreachableTypes` if you register many types but want only those reachable from query, mutation, and subscription roots in the executable schema. The `TrimTypes()` helper sets this option for you:

```csharp
builder
    .AddGraphQL()
    .AddTypes()
    .TrimTypes();
```

Schema printing offers additional formatter options. For example, semantic non-null rewriting uses `SchemaFormatterOptions`, and SDL endpoint exposure is controlled by endpoint or server options.

# Tuning Startup and Cache Behavior

Hot Chocolate v16 initializes the schema and request executor eagerly by default. For production, keep `LazyInitialization` set to `false` so schema errors are caught during application startup.

```csharp
builder
    .AddGraphQL()
    .ModifyOptions(options =>
    {
        options.LazyInitialization = false;
        options.PreparedOperationCacheSize = 1024;
        options.OperationDocumentCacheSize = 1024;
    });
```

Both cache sizes default to `256` and must be set to at least `16`.

- `PreparedOperationCacheSize` sets the size of the prepared operation cache.
- `OperationDocumentCacheSize` sets the size of the parsed operation document cache.

If you need to run work after the executor is built, use a warmup task rather than enabling lazy initialization.

# Configuring Resolver and Middleware Defaults

Resolver and field middleware options are global defaults. Change these settings only if you fully understand their impact on every field in your schema.

| Option                                    | Default                                        | Effect                                                                            |
| ----------------------------------------- | ---------------------------------------------- | --------------------------------------------------------------------------------- |
| `FieldMiddleware`                         | `FieldMiddlewareApplication.UserDefinedFields` | Applies custom field middleware to user-defined fields, not introspection fields. |
| `DefaultResolverStrategy`                 | `ExecutionStrategy.Parallel`                   | Runs resolver work in parallel by default.                                        |
| `DefaultQueryDependencyInjectionScope`    | `DependencyInjectionScope.Resolver`            | Sets the default DI scope for query resolvers.                                    |
| `DefaultMutationDependencyInjectionScope` | `DependencyInjectionScope.Request`             | Sets the default DI scope for mutation resolvers.                                 |
| `PublishRootFieldPagesToPromiseCache`     | `true`                                         | Publishes paged root field items to the DataLoader promise cache.                 |

Do not use these options to work around thread-safety issues in resolver code. Address shared state and service lifetimes directly.

# Prefer Descriptor-Level Configuration for Local Changes

Use schema options for conventions that apply to the entire request executor. For changes that affect only a single type, field, argument, input field, enum value, or directive, use attributes or descriptor APIs instead.

| Need                                  | Prefer                                                     | Why                                                  |
| ------------------------------------- | ---------------------------------------------------------- | ---------------------------------------------------- |
| Rename one type or field              | `[GraphQLName]` or `.Name(...)`                            | Keeps the schema contract close to the member.       |
| Add one description                   | `[GraphQLDescription]`, XML docs, or `.Description(...)`   | Avoids global documentation policy changes.          |
| Deprecate one field or enum value     | `[GraphQLDeprecated]`, `[Obsolete]`, or `.Deprecated(...)` | Deprecation is schema member metadata.               |
| Hide one CLR member                   | `[GraphQLIgnore]` or `.Ignore(...)`                        | Does not affect binding for unrelated types.         |
| Bind one object type explicitly       | `descriptor.BindFieldsExplicitly()`                        | Other types retain normal inference.                 |
| Mark one directive public or internal | `.Public()` or `.Internal()` on the directive type         | Avoids changing directive visibility globally.       |
| Override one nullability wrapper      | `[GraphQLType<T>]` or `.Type<T>()`                         | The SDL contract is explicit at the affected member. |

# Migrating v15 Schema Option Usage to v16

| Older expectation                                     | v16 action                                                                                         | Why                                                             |
| ----------------------------------------------------- | -------------------------------------------------------------------------------------------------- | --------------------------------------------------------------- |
| `EnableOneOf` in schema options                       | Remove it. Use `[OneOf]` or `descriptor.OneOf()`.                                                  | One-of input objects are now built in.                          |
| `EnableSemanticNonNull` in schema options             | Remove it. Use formatter, endpoint, or CLI semantic non-null export only for legacy SDL consumers. | The experimental schema option was removed in v16.              |
| `InitializeOnStartup`                                 | Remove it or move work to `AddWarmupTask`.                                                         | Eager initialization is the v16 default.                        |
| `AddDocumentCache(size)`                              | Set `OperationDocumentCacheSize` in `ModifyOptions`.                                               | Cache size is scoped to each request executor.                  |
| `AddOperationCache(size)`                             | Set `PreparedOperationCacheSize` in `ModifyOptions`.                                               | Cache size is scoped to each request executor.                  |
| Internal directives visible in SDL                    | Keep the v16 default, or set `DisableInternalDirectives = true` only for compatibility.            | v16 hides internal directives by default.                       |
| Global object identification limits in schema options | Configure global object identification APIs.                                                       | Node batching and node resolution moved out of `SchemaOptions`. |
| Operation introspection as a schema option            | Use validation introspection APIs.                                                                 | Operation introspection is request validation behavior.         |
| SDL endpoint exposure as a schema option              | Use endpoint or server options.                                                                    | SDL downloads are endpoint behavior.                            |

# Reference: Supported v16 Schema Options

The table below lists the `SchemaOptions` properties available in v16 for this repository.

| Option                                    | Type                         | Default             | Effect                                                                           | Example guidance                                                                       |
| ----------------------------------------- | ---------------------------- | ------------------- | -------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------- |
| `QueryTypeName`                           | `string?`                    | `null`              | Selects the query root type by GraphQL type name.                                | Set to a custom root name for schema-first SDL without a `schema` block.               |
| `MutationTypeName`                        | `string?`                    | `null`              | Selects the mutation root type by GraphQL type name.                             | Set when an existing SDL contract uses a non-conventional mutation root.               |
| `SubscriptionTypeName`                    | `string?`                    | `null`              | Selects the subscription root type by GraphQL type name.                         | Set when an existing SDL contract uses a non-conventional subscription root.           |
| `StrictValidation`                        | `bool`                       | `true`              | Requires the schema to pass validation during schema build.                      | Keep enabled and fix schema errors at startup.                                         |
| `UseXmlDocumentation`                     | `bool`                       | `true`              | Reads XML documentation comments as schema descriptions.                         | Set to `false` when descriptions come only from explicit schema metadata.              |
| `ResolveXmlDocumentationFileName`         | `Func<Assembly, string>?`    | `null`              | Resolves custom XML documentation file names.                                    | Provide a delegate when deployed XML file names differ from assembly names.            |
| `SortFieldsByName`                        | `bool`                       | `false`             | Orders fields by name when completing schema types.                              | Enable for deterministic SDL in snapshots and schema reviews.                          |
| `RemoveUnreachableTypes`                  | `bool`                       | `false`             | Removes types not reachable from operation roots.                                | Enable through `TrimTypes()` when broad type registration adds unused types.           |
| `RemoveUnusedTypeSystemDirectives`        | `bool`                       | `true`              | Removes unused type-system directive definitions.                                | Set to `false` for SDL exports that must include unused directive definitions.         |
| `DefaultBindingBehavior`                  | `BindingBehavior`            | `Implicit`          | Controls whether members are included by convention or require explicit binding. | Use `Explicit` only when every affected type is configured.                            |
| `DefaultFieldBindingFlags`                | `FieldBindingFlags`          | `Instance`          | Controls which runtime members implicit object binding can infer.                | Include static members when static resolver members should be inferred.                |
| `FieldMiddleware`                         | `FieldMiddlewareApplication` | `UserDefinedFields` | Controls whether custom field middleware applies to user fields or all fields.   | Keep default unless middleware must also wrap introspection fields.                    |
| `EnableDirectiveIntrospection`            | `bool`                       | `false`             | Adds applied directive introspection fields and related types.                   | Enable for tooling that reads directive applications through introspection.            |
| `DefaultDirectiveVisibility`              | `DirectiveVisibility`        | `Public`            | Sets default directive visibility.                                               | Set to `Internal` when directive introspection should expose only selected directives. |
| `DisableInternalDirectives`               | `bool`                       | `false`             | Treats internal directives as public and overrides explicit internal visibility. | Use mainly to restore v15 SDL exposure after reviewing metadata risk.                  |
| `DefaultResolverStrategy`                 | `ExecutionStrategy`          | `Parallel`          | Sets the default resolver execution strategy.                                    | Prefer field-level or resolver design changes before changing this globally.           |
| `ValidatePipelineOrder`                   | `bool`                       | `true`              | Validates known field middleware order.                                          | Keep enabled so invalid data middleware order fails during schema build.               |
| `StrictRuntimeTypeValidation`             | `bool`                       | `false`             | Tightens runtime type checks for abstract type results.                          | Enable when interface and union runtime values must match declared object types.       |
| `DefaultIsOfTypeCheck`                    | `IsOfTypeFallback?`          | `null`              | Provides a fallback runtime type check.                                          | Use for custom abstract type resolution policies.                                      |
| `EnableFlagEnums`                         | `bool`                       | `false`             | Infers `[Flags]` enums as flag enum object shapes.                               | Enable only when clients need flag enum components in the schema.                      |
| `EnableDefer`                             | `bool`                       | `false`             | Registers the `@defer` directive.                                                | Enable with matching transport and client support.                                     |
| `EnableStream`                            | `bool`                       | `false`             | Registers the `@stream` directive.                                               | Enable with matching transport and client support.                                     |
| `StripLeadingIFromInterface`              | `bool`                       | `false`             | Removes a leading `I` from inferred interface names.                             | Enable for .NET interface names such as `INode` when SDL should use `Node`.            |
| `EnableTag`                               | `bool`                       | `true`              | Registers the built-in `@tag` directive.                                         | Keep enabled for schema metadata consumed by registries and tooling.                   |
| `EnableOptInFeatures`                     | `bool`                       | `false`             | Registers opt-in feature metadata such as `@requiresOptIn`.                      | Enable before applying `[RequiresOptIn]` or `.RequiresOptIn(...)`.                     |
| `EnableSemanticIntrospection`             | `bool`                       | `true`              | Adds semantic introspection fields for schema discovery.                         | Disable when you do not want `__search` and `__definitions` available.                 |
| `DefaultQueryDependencyInjectionScope`    | `DependencyInjectionScope`   | `Resolver`          | Sets the default DI scope for query field resolvers.                             | Change only after reviewing resolver service lifetimes.                                |
| `DefaultMutationDependencyInjectionScope` | `DependencyInjectionScope`   | `Request`           | Sets the default DI scope for mutation field resolvers.                          | Keep request scope when mutation fields coordinate shared work per request.            |
| `PublishRootFieldPagesToPromiseCache`     | `bool`                       | `true`              | Publishes root field page items into the DataLoader promise cache.               | Keep enabled for paged root fields that can feed later DataLoader lookups.             |
| `LazyInitialization`                      | `bool`                       | `false`             | Defers schema and executor creation until first access when enabled.             | Keep `false` for startup validation. Use warmup tasks for startup work.                |
| `PreparedOperationCacheSize`              | `int`                        | `256`               | Sets the prepared operation cache size. Minimum is `16`.                         | Increase for high-throughput schemas with many repeated operations.                    |
| `OperationDocumentCacheSize`              | `int`                        | `256`               | Sets the parsed operation document cache size. Minimum is `16`.                  | Increase when many parsed documents should remain cached.                              |
| `ApplyShareableToPageInfo`                | `bool`                       | `false`             | Applies `@shareable` to `PageInfo`.                                              | Prefer `AddSourceSchemaDefaults()` for source schemas.                                 |
| `ApplyShareableToConnections`             | `bool`                       | `false`             | Applies `@shareable` to connection and edge types.                               | Prefer `AddSourceSchemaDefaults()` for source schemas.                                 |
| `ApplyShareableToNodeFields`              | `bool`                       | `false`             | Applies `@shareable` to `node` and `nodes` fields.                               | Prefer `AddSourceSchemaDefaults()` for source schemas.                                 |
| `ApplySerializeAsToScalars`               | `bool`                       | `false`             | Applies `@serializeAs` to scalars with serialization formats.                    | Prefer `AddSourceSchemaDefaults()` for composite schema source schemas.                |

# Troubleshooting Schema Options

| Symptom                                                                        | Likely cause                                                                             | Fix                                                                                       |
| ------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------- |
| `ModifyOptions` did not affect GET, uploads, batching, Nitro, or SDL downloads | These are server, transport, or endpoint settings                                        | Use server configuration and endpoint pages for those settings.                           |
| Introspection queries are blocked but SDL can still be downloaded              | Operation introspection policy and SDL endpoints are separate                            | Configure operation introspection in validation and SDL exposure on endpoints.            |
| A custom directive does not appear in applied directive introspection          | Applied directive introspection is disabled or the directive is internal                 | Enable `EnableDirectiveIntrospection` and check directive visibility.                     |
| Internal directives disappeared from SDL after v16 migration                   | v16 hides internal directives by default                                                 | Keep the safer default, or set `DisableInternalDirectives = true` only for compatibility. |
| Fields disappeared after enabling explicit binding                             | Explicit binding requires field registration                                             | Add descriptor fields or use type-level explicit binding for the affected type only.      |
| Schema startup fails after a migration                                         | Strict validation is catching schema errors earlier                                      | Keep `StrictValidation` enabled and fix the reported schema error.                        |
| Data middleware order fails validation                                         | Known middleware appears in the wrong order or more than once                            | Use paging, projection, filtering, sorting order and remove duplicates.                   |
| A one-of input is rejected                                                     | Fields are non-null, fields have defaults, or the client sent the wrong number of values | Make all one-of fields nullable, remove defaults, and send exactly one non-null value.    |
| Cache-size configuration throws                                                | A cache size is below the minimum                                                        | Set `PreparedOperationCacheSize` and `OperationDocumentCacheSize` to at least `16`.       |
| The first request is slow                                                      | `LazyInitialization` may be enabled                                                      | Keep eager initialization on and use warmup tasks for startup work.                       |
| SDL snapshots changed order                                                    | `SortFieldsByName` changed field order                                                   | Review the SDL diff and update snapshots intentionally.                                   |

# Next Steps

- Shape object fields with [Object Types](/docs/hotchocolate/v16/build/schema-elements/object-types).
- Shape input contracts with [Input Object Types](/docs/hotchocolate/v16/build/schema-elements/input-object-types).
- Configure nullability with [Lists and Non-Null](/docs/hotchocolate/v16/build/schema-elements/lists-and-non-null).
- Author directive metadata with [Directives](/docs/hotchocolate/v16/build/schema-elements/directives).
- Configure endpoint behavior with [Endpoint mapping](/docs/hotchocolate/v16/build/server-configuration/endpoints).
- Control operation introspection with [Introspection](/docs/hotchocolate/v16/build/security/introspection).
- Review v16 migration details in [Migrate from 15 to 16](/docs/hotchocolate/v16/_leagcy/migrating/migrate-from-15-to-16).
