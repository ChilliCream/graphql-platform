---
title: Schema options
---

# Configure schema options

Use schema options when you want to change how Hot Chocolate v16 builds the schema and type system. Schema options run before any GraphQL request exists, so they are the right place for root type names, binding conventions, schema validation, directive metadata, schema output ordering, startup behavior, and per-executor schema caches.

Configure them with `ModifyOptions` on the `IRequestExecutorBuilder` returned by `builder.AddGraphQL()` or `builder.Services.AddGraphQLServer()`.

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

Keep endpoint, HTTP transport, request validation, cost analysis, and Nitro settings out of `ModifyOptions`. Those settings use other configuration APIs.

| Goal                                                            | Use                                                                                 | Schema option page? |
| --------------------------------------------------------------- | ----------------------------------------------------------------------------------- | ------------------- |
| Rename operation root types                                     | `ModifyOptions` with `QueryTypeName`, `MutationTypeName`, or `SubscriptionTypeName` | Yes                 |
| Change implicit or explicit member binding                      | `ModifyOptions` or type descriptors                                                 | Yes                 |
| Control XML documentation extraction                            | `ModifyOptions` with `UseXmlDocumentation`                                          | Yes                 |
| Control directive visibility or applied directive introspection | `ModifyOptions`                                                                     | Yes                 |
| Disable operation introspection queries                         | Validation APIs such as `DisableIntrospection(...)` or `AllowIntrospection(false)`  | Boundary only       |
| Hide SDL downloads or configure Nitro                           | Endpoint or server options                                                          | No                  |
| Configure GET, multipart requests, batching, or WebSockets      | Server and transport options                                                        | No                  |
| Limit parser, validation, execution, or cost work               | Parser, validation, request, or cost options                                        | No                  |

# Choose an option by task

| Task                                            | Option or API                                               | Default    | Guidance                                                                                                   |
| ----------------------------------------------- | ----------------------------------------------------------- | ---------- | ---------------------------------------------------------------------------------------------------------- |
| Keep schema validation strict                   | `StrictValidation = true`                                   | `true`     | Keep this enabled. Disable it only for short migration diagnostics.                                        |
| Validate data middleware order                  | `ValidatePipelineOrder = true`                              | `true`     | Leave this enabled so invalid paging, projection, filtering, and sorting order fails at schema build time. |
| Rename root operation types                     | `QueryTypeName`, `MutationTypeName`, `SubscriptionTypeName` | `null`     | Use custom names for schema-first compatibility or a published SDL contract.                               |
| Make field discovery explicit                   | `DefaultBindingBehavior = BindingBehavior.Explicit`         | `Implicit` | Use this for a whole schema only when every type is intentionally configured.                              |
| Include static members during discovery         | `DefaultFieldBindingFlags`                                  | `Instance` | Set to instance plus static when root or extension types use static resolver members by convention.        |
| Stabilize SDL field order                       | `SortFieldsByName = true`                                   | `false`    | Use for schema reviews, snapshots, and generated artifacts.                                                |
| Remove unreachable registered types             | `RemoveUnreachableTypes = true` or `TrimTypes()`            | `false`    | Use when you register broad assemblies but want the executable schema trimmed to reachable types.          |
| Keep unused directive definitions               | `RemoveUnusedTypeSystemDirectives = false`                  | `true`     | Use when schema registries or SDL consumers need unused type-system directive definitions.                 |
| Expose applied directives through introspection | `EnableDirectiveIntrospection = true`                       | `false`    | Use when tooling must read directive applications from introspection.                                      |
| Enable opt-in feature metadata                  | `EnableOptInFeatures = true`                                | `false`    | Required before using `@requiresOptIn`.                                                                    |
| Enable incremental delivery directives          | `EnableDefer`, `EnableStream`                               | `false`    | Enable only when your transport and clients support incremental delivery.                                  |
| Keep startup eager                              | `LazyInitialization = false`                                | `false`    | Leave eager initialization on so schema errors surface during startup.                                     |
| Tune operation caches                           | `PreparedOperationCacheSize`, `OperationDocumentCacheSize`  | `256`      | Use values of at least `16`.                                                                               |

# Configure root operation type names

Hot Chocolate uses the conventional operation type names when the root name options are `null`: `Query`, `Mutation`, and `Subscription`. Set a custom root name when you import SDL with non-conventional root names, or when an existing client contract already depends on those names.

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

Expected SDL shape:

```graphql
schema {
  query: RootQuery
}

type RootQuery {
  product: Product!
}
```

If you only need to register normal root types, keep the default names and use `AddQueryType<T>()`, `AddMutationType<T>()`, or generated root type attributes.

# Control naming and member binding

Hot Chocolate infers fields from public members by default. That behavior is useful for small DTOs, but it can expose helper members, foreign keys, or static members you did not intend to publish.

Use global binding options when your entire schema follows the same convention.

```csharp
using HotChocolate.Types;

builder
    .AddGraphQL()
    .ModifyOptions(options =>
    {
        options.DefaultBindingBehavior = BindingBehavior.Explicit;
    });
```

With global explicit binding, convention-bound object types expose no fields until you configure them. Root operation types are included in that rule.

Prefer descriptor-level binding when one type needs a stricter contract.

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

Expected SDL shape:

```graphql
type Product {
  id: ID!
  name: String!
  price: Decimal!
}
```

`DefaultFieldBindingFlags` controls whether implicit object type binding includes instance members, static members, or both. The default is `FieldBindingFlags.Instance`.

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

There is an intentional interaction between the two binding options:

- Setting `DefaultBindingBehavior` to `Explicit` resets `DefaultFieldBindingFlags` to `FieldBindingFlags.Default`.
- Setting `DefaultFieldBindingFlags` to any non-default value switches `DefaultBindingBehavior` back to `Implicit`.

# Keep schema validation strict

`StrictValidation` is enabled by default. Keep it enabled so schema errors fail at startup instead of appearing after deployment. Strict validation checks schema rules such as operation root validity, object fields, directive usage, input definitions, and other type-system constraints.

```csharp
builder
    .AddGraphQL()
    .ModifyOptions(options =>
    {
        options.StrictValidation = true;
        options.ValidatePipelineOrder = true;
    });
```

`ValidatePipelineOrder` is also enabled by default. It checks known data middleware order, including paging, projection, filtering, and sorting. Fix the middleware order instead of turning this off.

Use these advanced runtime type options only when you own the abstract type resolution strategy:

- `StrictRuntimeTypeValidation` defaults to `false`. Enable it when interface and union runtime values must match declared possible types without fallback behavior.
- `DefaultIsOfTypeCheck` defaults to `null`. Provide a fallback only when you need custom abstract type checks across the schema.

# Configure nullability and one-of behavior

Base GraphQL nullability comes from your C# nullability annotations and descriptor configuration, not from a schema option. Enable nullable reference types in your project and publish the weakest contract that still tells clients the truth.

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

Expected SDL shape:

```graphql
type Product {
  name: String!
  description: String
}
```

Hot Chocolate v16 does not expose `EnableSemanticNonNull` on `SchemaOptions`. The experimental `@semanticNonNull` opt-in from v15 was removed. If a legacy client still needs semantic non-null SDL, use the schema formatter option `RewriteToSemanticNonNull`, `MapGraphQLSemanticNonNullSchema()`, or the schema export command with `--semantic-non-null`. Those are schema printing and endpoint concerns, not schema options.

One-of input objects are built in. Do not use an `EnableOneOf` schema option in v16. Mark the input with `[OneOf]` or configure the input descriptor with `OneOf()`.

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

public sealed class ProductSelectorInputType
    : InputObjectType<ProductSelectorInput>
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

Expected SDL shape:

```graphql
input ProductSelectorInput @oneOf {
  id: ID
  sku: String
}
```

A one-of input must define nullable fields, must not define field defaults, and must receive exactly one non-null value from the client.

# Manage descriptions, deprecations, and opt-in metadata

XML documentation extraction is enabled by default through `UseXmlDocumentation = true`. It reads generated XML documentation files and turns summaries and parameter documentation into schema descriptions.

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>$(NoWarn);1591</NoWarn>
</PropertyGroup>
```

Turn it off when descriptions should come only from attributes, schema-first SDL, or descriptor configuration.

```csharp
builder
    .AddGraphQL()
    .ModifyOptions(options => options.UseXmlDocumentation = false);
```

Use `ResolveXmlDocumentationFileName` when your XML documentation files are deployed under non-standard names.

Deprecation has no global schema option. Add deprecations where you define the schema member.

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

Use `EnableOptInFeatures = true` before you apply `@requiresOptIn` metadata. Use `EnableTag = true` when you want the built-in `@tag` directive available. `EnableTag` is already enabled by default.

# Configure directive and type-system behavior

Directive options affect type-system metadata and introspection. They do not block normal operation introspection queries and they do not control SDL endpoint exposure.

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

Use these settings together with directive descriptors:

- `EnableDirectiveIntrospection` adds applied directive introspection fields such as `appliedDirectives`.
- `DefaultDirectiveVisibility` sets whether directive types are public or internal by default.
- `DisableInternalDirectives = true` treats internal directives as public. Use this mainly for v15 compatibility, because it can expose metadata such as authorization directives.
- `RemoveUnusedTypeSystemDirectives` controls whether unused type-system directive definitions remain in the final schema.

Other type-system feature switches include:

- `EnableDefer` and `EnableStream` register the incremental delivery directives.
- `EnableFlagEnums` changes how `[Flags]` enums are inferred.
- `StripLeadingIFromInterface` removes a leading `I` from inferred GraphQL interface names.
- `EnableSemanticIntrospection` adds Hot Chocolate semantic introspection fields such as `__search` and `__definitions`. It defaults to `true` and is separate from semantic non-null.

# Stabilize SDL and trim schema artifacts

Use `SortFieldsByName` when field order matters for schema reviews, snapshots, generated artifacts, or registry diffs.

```csharp
builder
    .AddGraphQL()
    .ModifyOptions(options => options.SortFieldsByName = true);
```

Example output with sorting enabled:

```graphql
type Query {
  apple: String!
  zebra: String!
}
```

Use `RemoveUnreachableTypes` when you register many types but only want types reachable from query, mutation, and subscription roots in the executable schema. The `TrimTypes()` helper sets the same option.

```csharp
builder
    .AddGraphQL()
    .AddTypes()
    .TrimTypes();
```

Schema printing has additional formatter options. For example, semantic non-null rewriting uses `SchemaFormatterOptions`, and SDL endpoint exposure uses endpoint or server options.

# Tune startup and cache behavior

Hot Chocolate v16 initializes the schema and request executor eagerly by default. Leave `LazyInitialization` at `false` for production so schema errors fail during application startup.

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

Both cache sizes default to `256` and require a value of at least `16`.

- `PreparedOperationCacheSize` configures the prepared operation cache.
- `OperationDocumentCacheSize` configures the parsed operation document cache.

If you need work that runs after the executor is built, use a warmup task instead of lazy initialization.

# Configure resolver and middleware defaults carefully

Resolver and field middleware options are global defaults. Change them only when you understand how they affect every field.

| Option                                    | Default                                        | Effect                                                                            |
| ----------------------------------------- | ---------------------------------------------- | --------------------------------------------------------------------------------- |
| `FieldMiddleware`                         | `FieldMiddlewareApplication.UserDefinedFields` | Applies custom field middleware to user-defined fields, not introspection fields. |
| `DefaultResolverStrategy`                 | `ExecutionStrategy.Parallel`                   | Runs resolver work in parallel by default.                                        |
| `DefaultQueryDependencyInjectionScope`    | `DependencyInjectionScope.Resolver`            | Creates the default DI scope for query resolvers.                                 |
| `DefaultMutationDependencyInjectionScope` | `DependencyInjectionScope.Request`             | Creates the default DI scope for mutation resolvers.                              |
| `PublishRootFieldPagesToPromiseCache`     | `true`                                         | Publishes paged root field items to the DataLoader promise cache.                 |

Do not use these options to hide thread-safety problems in resolver code. Fix shared state and service lifetimes first.

# Prefer descriptor-level configuration for local changes

Use schema options for conventions that apply to the whole request executor. Use attributes or descriptor APIs when the change belongs to one type, field, argument, input field, enum value, or directive.

| Need                                  | Prefer                                                     | Why                                                  |
| ------------------------------------- | ---------------------------------------------------------- | ---------------------------------------------------- |
| Rename one type or field              | `[GraphQLName]` or `.Name(...)`                            | The schema contract stays near the member.           |
| Add one description                   | `[GraphQLDescription]`, XML docs, or `.Description(...)`   | You avoid global documentation policy changes.       |
| Deprecate one field or enum value     | `[GraphQLDeprecated]`, `[Obsolete]`, or `.Deprecated(...)` | Deprecation is schema member metadata.               |
| Hide one CLR member                   | `[GraphQLIgnore]` or `.Ignore(...)`                        | You do not change binding for unrelated types.       |
| Bind one object type explicitly       | `descriptor.BindFieldsExplicitly()`                        | Other types keep normal inference.                   |
| Mark one directive public or internal | `.Public()` or `.Internal()` on the directive type         | You avoid changing directive visibility globally.    |
| Override one nullability wrapper      | `[GraphQLType<T>]` or `.Type<T>()`                         | The SDL contract is explicit at the affected member. |

# Migrate v15 schema option usage to v16

| Older expectation                                     | v16 action                                                                                         | Why                                                             |
| ----------------------------------------------------- | -------------------------------------------------------------------------------------------------- | --------------------------------------------------------------- |
| `EnableOneOf` in schema options                       | Remove it. Use `[OneOf]` or `descriptor.OneOf()`.                                                  | One-of input objects are built in.                              |
| `EnableSemanticNonNull` in schema options             | Remove it. Use formatter, endpoint, or CLI semantic non-null export only for legacy SDL consumers. | The experimental schema option was removed in v16.              |
| `InitializeOnStartup`                                 | Remove it or move work to `AddWarmupTask`.                                                         | Eager initialization is the v16 default.                        |
| `AddDocumentCache(size)`                              | Set `OperationDocumentCacheSize` in `ModifyOptions`.                                               | Cache size is scoped to each request executor.                  |
| `AddOperationCache(size)`                             | Set `PreparedOperationCacheSize` in `ModifyOptions`.                                               | Cache size is scoped to each request executor.                  |
| Internal directives visible in SDL                    | Keep the v16 default, or set `DisableInternalDirectives = true` only for compatibility.            | v16 hides internal directives by default.                       |
| Global object identification limits in schema options | Configure global object identification APIs.                                                       | Node batching and node resolution moved out of `SchemaOptions`. |
| Operation introspection as a schema option            | Use validation introspection APIs.                                                                 | Operation introspection is request validation behavior.         |
| SDL endpoint exposure as a schema option              | Use endpoint or server options.                                                                    | SDL downloads are endpoint behavior.                            |

# Supported v16 schema options reference

The following table lists the `SchemaOptions` properties present in the v16 source for this repository.

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

# Troubleshoot schema options

| Symptom                                                                        | Likely cause                                                                             | Fix                                                                                       |
| ------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------- |
| `ModifyOptions` did not affect GET, uploads, batching, Nitro, or SDL downloads | Those are server, transport, or endpoint settings                                        | Use server configuration and endpoint pages for those settings.                           |
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

# Next steps

- Shape object fields with [Object Types](/docs/hotchocolate/v16/build2/schema-elements/object-types).
- Shape input contracts with [Input Object Types](/docs/hotchocolate/v16/build2/schema-elements/input-object-types).
- Configure nullability with [Lists and Non-Null](/docs/hotchocolate/v16/build2/schema-elements/lists-and-non-null).
- Author directive metadata with [Directives](/docs/hotchocolate/v16/build2/schema-elements/directives).
- Configure endpoint behavior with [Endpoint mapping](/docs/hotchocolate/v16/build2/server-configuration/endpoints).
- Control operation introspection with [Introspection](/docs/hotchocolate/v16/securing-your-api/introspection).
- Review v16 migration details in [Migrate from 15 to 16](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16).
