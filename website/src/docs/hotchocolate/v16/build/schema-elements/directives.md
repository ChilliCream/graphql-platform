---
title: Directives
---

Directives are annotations in GraphQL. In Hot Chocolate v16, clients use executable directives within operations, while schema authors use type-system directives to add metadata or behavior to schema elements.

```graphql
# Schema SDL
type Product @tag(name: "catalog") {
  id: ID!
  name: String!
  sku: String!
  oldSku: String @deprecated(reason: "Use sku instead.")
  experimentalLabel: String @requiresOptIn(feature: "experimentalCatalog")
  recommendations(first: Int = 5): [Product!] @cost(weight: "25")
}
```

```graphql
# Client operation
query ProductCard($withRecommendations: Boolean!, $withDetails: Boolean!) {
  productById(id: 1) {
    name
    recommendations @include(if: $withRecommendations) {
      name
    }
    ...ProductDetails @defer(if: $withDetails)
  }
}

fragment ProductDetails on Product {
  sku
  oldSku
}
```

Before creating a custom directive, check if an existing directive-based feature meets your needs. Use attributes or descriptor APIs when Hot Chocolate already provides a feature, and verify the generated SDL. Create custom directives only when clients, schema registries, or tooling require metadata or schema-visible behavior.

> Note: This page covers GraphQL directives in Hot Chocolate v16. It does not address Apollo Federation or Fusion subgraph directives and attributes such as `@key`, `@requires`, `@provides`, `@lookup`, `[Shareable]`, `[Lookup]`, or `[Internal]`. For those, see the Fusion subgraph documentation.

# Understanding directive categories

GraphQL organizes directives by their location.

| Category              | Where it appears         | Examples                                                                                                                                                      |
| --------------------- | ------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Executable directive  | In a client operation    | `FIELD`, `FRAGMENT_SPREAD`, `INLINE_FRAGMENT`, `QUERY`, `MUTATION`, `SUBSCRIPTION`, `VARIABLE_DEFINITION`, `FRAGMENT_DEFINITION`                              |
| Type-system directive | In the schema definition | `OBJECT`, `FIELD_DEFINITION`, `ARGUMENT_DEFINITION`, `INPUT_OBJECT`, `INPUT_FIELD_DEFINITION`, `INTERFACE`, `UNION`, `ENUM`, `ENUM_VALUE`, `SCALAR`, `SCHEMA` |

Hot Chocolate features can also be grouped by their practical use.

| Use                         | Directives                                          | Typical action                                                      |
| --------------------------- | --------------------------------------------------- | ------------------------------------------------------------------- |
| Client operation control    | `@skip`, `@include`, `@defer`, `@stream`            | Allow clients to include, omit, defer, or stream selections.        |
| Schema lifecycle metadata   | `@deprecated`, `@requiresOptIn`                     | Communicate migration status or feature stability.                  |
| Schema metadata for tooling | `@tag`, custom metadata directives                  | Label schema elements for registries, documentation, or governance. |
| Server policy metadata      | `@authorize`, `@cost`, `@listSize`, `@cacheControl` | Configure security, cost, or caching features.                      |
| Runtime directive behavior  | Custom directive middleware                         | Attach schema-visible runtime behavior to a directive.              |

Avoid using directives for every reusable concern. Use descriptions for human-readable documentation, descriptor attributes or field middleware for server-only behavior, and the dedicated APIs for authorization, cost, and cache control when available.

# Choosing a directive feature

Some directives are standard GraphQL features, others are core to Hot Chocolate, and some require additional packages or options. Do not assume every directive is available by default.

| Directive                                                     | Category                 | Common locations                                                   | Enable or apply in Hot Chocolate                                                                                     | More information                                                                   |
| ------------------------------------------------------------- | ------------------------ | ------------------------------------------------------------------ | -------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------- |
| `@skip(if: Boolean!)`                                         | Standard executable      | `FIELD`, `FRAGMENT_SPREAD`, `INLINE_FRAGMENT`                      | Available in client operations by default.                                                                           | [Operation directives](#use-executable-directives-in-operations)                   |
| `@include(if: Boolean!)`                                      | Standard executable      | `FIELD`, `FRAGMENT_SPREAD`, `INLINE_FRAGMENT`                      | Available in client operations by default.                                                                           | [Operation directives](#use-executable-directives-in-operations)                   |
| `@defer(label: String, if: Boolean)`                          | Incremental executable   | `FRAGMENT_SPREAD`, `INLINE_FRAGMENT`                               | Set `ModifyOptions(o => o.EnableDefer = true)`.                                                                      | [HTTP Transport](/docs/hotchocolate/v16/build/server-configuration/http-transport) |
| `@stream(label: String, initialCount: Int! = 0, if: Boolean)` | Incremental executable   | `FIELD` on list fields                                             | Set `ModifyOptions(o => o.EnableStream = true)`.                                                                     | [HTTP Transport](/docs/hotchocolate/v16/build/server-configuration/http-transport) |
| `@deprecated(reason: String)`                                 | Type-system lifecycle    | Field definitions, argument definitions, input fields, enum values | Use `[GraphQLDeprecated]`, `[Obsolete]`, or `.Deprecated(...)`.                                                      | [Versioning](/docs/hotchocolate/v16/_leagcy/building-a-schema/versioning)          |
| `@requiresOptIn(feature: String!)`                            | Type-system lifecycle    | Field definitions, argument definitions, input fields, enum values | Set `EnableOptInFeatures = true`, then use `[RequiresOptIn]` or `.RequiresOptIn(...)`.                               | [Versioning](/docs/hotchocolate/v16/_leagcy/building-a-schema/versioning)          |
| `@tag(name: String!)`                                         | Type-system metadata     | Schema, types, fields, arguments, input fields, enum values        | Enabled by default with `EnableTag = true`. Use `[Tag]` or `.Tag(...)`.                                              | [Apply tags](#apply-existing-directives-from-c)                                    |
| `@authorize`                                                  | Security policy metadata | Object types, field definitions                                    | Add the authorization package and `.AddAuthorization()`, then use `[Authorize]` or `.Authorize(...)`.                | [Authorization](/docs/hotchocolate/v16/build/security/authorization)               |
| `@cost(weight:)`                                              | Cost metadata            | Objects, fields, arguments, input fields, enums, scalars           | Use `HotChocolate.CostAnalysis`, then `[Cost]` or `.Cost(...)`.                                                      | [Cost Analysis](/docs/hotchocolate/v16/build/security/cost-analysis)               |
| `@listSize(...)`                                              | Cost metadata            | Field definitions                                                  | Use `HotChocolate.CostAnalysis`, then `[ListSize]` or `.ListSize(...)`. Pagination can generate it for paged fields. | [Cost Analysis](/docs/hotchocolate/v16/build/security/cost-analysis)               |
| `@cacheControl(...)`                                          | Cache policy metadata    | Objects, fields, interfaces, unions                                | Use `HotChocolate.Caching`, `[CacheControl]`, `.AddCacheControl()`, and `.UseQueryCache()` when writing headers.     | [Cache Control](/docs/hotchocolate/v16/build/performance/cache-control)            |

# Applying existing directives from C#

Use attributes when a directive is closely related to a CLR member. Use descriptor APIs if you manage schema configuration in type classes.

## Deprecating, opting in, and tagging fields

Enable opt-in features before applying `@requiresOptIn`.

```csharp
// Program.cs, attribute-based configuration
builder
    .AddGraphQL()
    .ModifyOptions(o => o.EnableOptInFeatures = true)
    .AddQueryType<Query>();
```

If you use an `ObjectType<T>` for schema configuration, be sure to register the type class as well.

```csharp
// Program.cs, descriptor-based configuration
builder
    .AddGraphQL()
    .ModifyOptions(o => o.EnableOptInFeatures = true)
    .AddQueryType<Query>()
    .AddType<ProductType>();
```

<ExampleTabs>
<Implementation>

```csharp
#nullable enable

using HotChocolate;
using HotChocolate.Types;

namespace Catalog.Types;

public sealed class Query
{
    public Product GetProduct() => new();
}

public sealed class Product
{
    public string Name => "Trail Backpack";

    [GraphQLDeprecated("Use sku instead.")]
    public string? LegacySku => "BK-1";

    [RequiresOptIn("experimentalCatalog")]
    [Tag("catalog")]
    public string? ExperimentalLabel => "Recommended";
}
```

</Implementation>
<Code>

```csharp
using HotChocolate.Types;

namespace Catalog.Types;

public sealed class Query
{
    public Product GetProduct() => new();
}

public sealed class Product
{
    public string Name => "Trail Backpack";

    public string? LegacySku => "BK-1";

    public string? ExperimentalLabel => "Recommended";
}

public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor
            .Field(t => t.LegacySku)
            .Deprecated("Use sku instead.");

        descriptor
            .Field(t => t.ExperimentalLabel)
            .RequiresOptIn("experimentalCatalog")
            .Tag("catalog");
    }
}
```

</Code>
</ExampleTabs>

Relevant SDL:

```graphql
type Product {
  name: String!
  legacySku: String @deprecated(reason: "Use sku instead.")
  experimentalLabel: String
    @requiresOptIn(feature: "experimentalCatalog")
    @tag(name: "catalog")
}
```

`@requiresOptIn` is repeatable and is valid on output fields, input fields, arguments, and enum values. Do not apply it to non-null arguments or non-null input fields unless they have a default value.

For authorization, cost analysis, list-size metadata, and cache-control headers, use the dedicated pages linked in the feature table. Those pages cover setup, packages, middleware, and validation behavior.

# Deciding whether to create a custom directive

Create a custom directive only when it needs to be part of the GraphQL contract.

Consider these questions before proceeding:

| Question                                                                                                                        | Prefer a directive when the answer is yes                                 |
| ------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------- |
| Will clients, schema registries, gateways, or documentation tools need to read this metadata?                                   | Use a type-system directive such as `@audience` or `@dataClassification`. |
| Does the behavior need to be declared in SDL or in a client operation?                                                          | Use a directive with middleware.                                          |
| Is the concern already covered by Hot Chocolate authorization, cost analysis, cache control, descriptions, or field middleware? | Use the existing feature instead.                                         |
| Is the concern internal logging, tracing, or service injection?                                                                 | Use server middleware or instrumentation instead.                         |

Good candidates for custom directives include `@audience(name:)` for schema consumers and `@dataClassification(level:)` for governance tooling. Avoid using directives for resolver logging, internal tracing, or documentation text.

# Creating a custom type-system directive

The most common custom directive is a type-system directive. For example, you might add `@audience(name:)` to field definitions so that tooling can identify the intended audience for a field.

## Defining the directive value and type

Use `DirectiveType<T>` when your directive has arguments. Hot Chocolate binds public properties on `T` to directive arguments unless you configure binding explicitly.

```csharp
using HotChocolate.Types;

namespace Catalog.Types;

public sealed class AudienceDirective
{
    public string Name { get; set; } = string.Empty;
}

public sealed class AudienceDirectiveType : DirectiveType<AudienceDirective>
{
    protected override void Configure(
        IDirectiveTypeDescriptor<AudienceDirective> descriptor)
    {
        descriptor
            .Name("audience")
            .Description("Identifies the intended schema audience.")
            .Location(DirectiveLocation.FieldDefinition);

        descriptor
            .Argument(t => t.Name)
            .Name("name")
            .Type<NonNullType<StringType>>();
    }
}
```

## Register and apply the directive

Register directive types with `.AddDirectiveType<T>()`. Apply a type-system directive with `descriptor.Directive(...)` on the descriptor for the schema element.

```csharp
using HotChocolate.Types;

namespace Catalog.Types;

public sealed class Query
{
    public Report GetInternalReport() => new("Revenue");
}

public sealed record Report(string Title);

public sealed class QueryType : ObjectType<Query>
{
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor
            .Field(t => t.GetInternalReport())
            .Name("internalReport")
            .Directive(new AudienceDirective { Name = "internal" });
    }
}
```

```csharp
// Program.cs
builder
    .AddGraphQL()
    .AddQueryType<QueryType>()
    .AddDirectiveType<AudienceDirectiveType>();
```

Expected SDL:

```graphql
"""
Identifies the intended schema audience.
"""
directive @audience(name: String!) on FIELD_DEFINITION

type Query {
  internalReport: Report! @audience(name: "internal")
}

type Report {
  title: String!
}
```

The typed form survives refactoring better than the string form. If you need the string form, pass `ArgumentNode` values from `HotChocolate.Language`.

```csharp
using HotChocolate.Language;

// Inside Configure(...)
descriptor
    .Field(t => t.GetInternalReport())
    .Directive("audience", new ArgumentNode("name", "internal"));
```

# Making a directive repeatable

By default, a directive can appear only once at a given location. If you want to allow multiple instances, mark the directive type as repeatable by calling `descriptor.Repeatable()`.

```csharp
public sealed class AudienceDirectiveType : DirectiveType<AudienceDirective>
{
    protected override void Configure(
        IDirectiveTypeDescriptor<AudienceDirective> descriptor)
    {
        descriptor
            .Name("audience")
            .Location(DirectiveLocation.FieldDefinition)
            .Repeatable();

        descriptor
            .Argument(t => t.Name)
            .Name("name")
            .Type<NonNullType<StringType>>();
    }
}
```

You can then apply the directive more than once:

```csharp
descriptor
    .Field(t => t.GetInternalReport())
    .Directive(new AudienceDirective { Name = "internal" })
    .Directive(new AudienceDirective { Name = "partner" });
```

Expected SDL:

```graphql
directive @audience(name: String!) repeatable on FIELD_DEFINITION

type Query {
  internalReport: Report! @audience(name: "internal") @audience(name: "partner")
}
```

Hot Chocolate also has repeatable directives such as `@tag`, `@authorize`, and `@requiresOptIn`.

# Choose directive locations

A directive type must declare at least one location. You can call `.Location(...)` more than once or combine locations with the pipe operator.

```csharp
descriptor.Location(DirectiveLocation.Object | DirectiveLocation.FieldDefinition);
```

| SDL location             | `DirectiveLocation` value                | Category    | Example use                                            |
| ------------------------ | ---------------------------------------- | ----------- | ------------------------------------------------------ |
| `QUERY`                  | `DirectiveLocation.Query`                | Executable  | Operation-level behavior for queries.                  |
| `MUTATION`               | `DirectiveLocation.Mutation`             | Executable  | Operation-level behavior for mutations.                |
| `SUBSCRIPTION`           | `DirectiveLocation.Subscription`         | Executable  | Operation-level behavior for subscriptions.            |
| `FIELD`                  | `DirectiveLocation.Field`                | Executable  | A client applies a directive to a selected field.      |
| `FRAGMENT_DEFINITION`    | `DirectiveLocation.FragmentDefinition`   | Executable  | A client annotates a named fragment.                   |
| `FRAGMENT_SPREAD`        | `DirectiveLocation.FragmentSpread`       | Executable  | `@include`, `@skip`, or `@defer` on a fragment spread. |
| `INLINE_FRAGMENT`        | `DirectiveLocation.InlineFragment`       | Executable  | `@defer` on inline fragment data.                      |
| `VARIABLE_DEFINITION`    | `DirectiveLocation.VariableDefinition`   | Executable  | Metadata on operation variables.                       |
| `SCHEMA`                 | `DirectiveLocation.Schema`               | Type system | Schema-wide metadata.                                  |
| `SCALAR`                 | `DirectiveLocation.Scalar`               | Type system | Scalar specification or tooling metadata.              |
| `OBJECT`                 | `DirectiveLocation.Object`               | Type system | Metadata for object types.                             |
| `FIELD_DEFINITION`       | `DirectiveLocation.FieldDefinition`      | Type system | Metadata or middleware for a schema field.             |
| `ARGUMENT_DEFINITION`    | `DirectiveLocation.ArgumentDefinition`   | Type system | Metadata for field or directive arguments.             |
| `INTERFACE`              | `DirectiveLocation.Interface`            | Type system | Metadata for interfaces.                               |
| `UNION`                  | `DirectiveLocation.Union`                | Type system | Metadata for unions.                                   |
| `ENUM`                   | `DirectiveLocation.Enum`                 | Type system | Metadata for an enum type.                             |
| `ENUM_VALUE`             | `DirectiveLocation.EnumValue`            | Type system | Deprecation or opt-in metadata on enum values.         |
| `INPUT_OBJECT`           | `DirectiveLocation.InputObject`          | Type system | Metadata for input object types.                       |
| `INPUT_FIELD_DEFINITION` | `DirectiveLocation.InputFieldDefinition` | Type system | Metadata for input object fields.                      |

# Controlling directive visibility and introspection

It is important to distinguish between these concepts:

| Concept                         | What it affects                                                                  |
| ------------------------------- | -------------------------------------------------------------------------------- |
| SDL directive definition        | The `directive @name(...) on ...` line printed with the schema.                  |
| SDL directive application       | The `@name(...)` annotation on a type, field, argument, or operation.            |
| Standard introspection          | `__schema { directives { name locations } }` and the usual type fields.          |
| Applied directive introspection | Hot Chocolate fields such as `appliedDirectives`, which are disabled by default. |

Schema options control these behaviors.

| Option                             | Default                      | Effect                                                                                             | When to change it                                                                            |
| ---------------------------------- | ---------------------------- | -------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------- |
| `RemoveUnusedTypeSystemDirectives` | `true`                       | Removes type-system directive definitions that are not applied anywhere.                           | Disable for SDL exports or snapshots where unused directive definitions must remain visible. |
| `EnableDirectiveIntrospection`     | `false`                      | Enables applied-directive introspection fields and related introspection types.                    | Enable when tooling needs to inspect directive applications through introspection.           |
| `DefaultDirectiveVisibility`       | `DirectiveVisibility.Public` | Sets whether directive types are public unless a directive calls `.Public()` or `.Internal()`.     | Set to `Internal` when directive introspection should expose only selected directives.       |
| `DisableInternalDirectives`        | `false`                      | Treats internal directives as public and overrides `.Internal()` and `DefaultDirectiveVisibility`. | Prefer explicit `.Public()` on your own directive. Use this mainly for compatibility.        |
| `EnableTag`                        | `true`                       | Registers the core `@tag` directive.                                                               | Disable only when you do not want `@tag` in the schema.                                      |
| `EnableOptInFeatures`              | `false`                      | Registers opt-in feature support, including `@requiresOptIn`.                                      | Enable before applying `@requiresOptIn`.                                                     |
| `EnableDefer`                      | `false`                      | Registers `@defer`.                                                                                | Enable for incremental fragment delivery.                                                    |
| `EnableStream`                     | `false`                      | Registers `@stream`.                                                                               | Enable for incremental list delivery.                                                        |

Configure these options on the request executor builder:

```csharp
using HotChocolate.Types;

builder
    .AddGraphQL()
    .ModifyOptions(o =>
    {
        o.EnableDirectiveIntrospection = true;
        o.RemoveUnusedTypeSystemDirectives = false;
        o.DefaultDirectiveVisibility = DirectiveVisibility.Public;
    });
```

For a custom directive, you can also control visibility on the directive descriptor:

```csharp
protected override void Configure(
    IDirectiveTypeDescriptor<AudienceDirective> descriptor)
{
    descriptor
        .Name("audience")
        .Location(DirectiveLocation.FieldDefinition)
        .Public();
}
```

# Adding directive middleware for runtime behavior

Directive middleware is an advanced feature. Use it when runtime behavior must be visible in the GraphQL contract. For server-only behavior, prefer field middleware, request middleware, instrumentation, or a descriptor attribute.

The following example shows an `@upperCase` directive that can be applied to a schema field definition or a selected field. It reads its argument with `directive.ToValue<T>()`, continues the pipeline with `next.Invoke(context)`, and modifies `context.Result` after the field resolves.

```csharp
using HotChocolate.Types;

namespace Catalog.Types;

public sealed class UpperCaseDirective
{
    public bool Enabled { get; set; } = true;
}

public sealed class UpperCaseDirectiveType : DirectiveType<UpperCaseDirective>
{
    protected override void Configure(
        IDirectiveTypeDescriptor<UpperCaseDirective> descriptor)
    {
        descriptor
            .Name("upperCase")
            .Location(DirectiveLocation.FieldDefinition | DirectiveLocation.Field);

        descriptor
            .Argument(t => t.Enabled)
            .Name("enabled")
            .Type<BooleanType>();

        descriptor.Use((next, directive) => async context =>
        {
            await next.Invoke(context);

            var options = directive.ToValue<UpperCaseDirective>();

            if (options.Enabled && context.Result is string value)
            {
                context.Result = value.ToUpperInvariant();
            }
        });
    }
}
```

Apply and register it:

```csharp
public sealed class Product
{
    public string Name => "Trail Backpack";
}

public sealed class Query
{
    public Product GetProduct() => new();
}

public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor
            .Field(t => t.Name)
            .Directive(new UpperCaseDirective { Enabled = true });
    }
}
```

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddType<ProductType>()
    .AddDirectiveType<UpperCaseDirectiveType>();
```

Request:

```graphql
{
  product {
    name
  }
}
```

Expected result:

```json
{
  "data": {
    "product": {
      "name": "TRAIL BACKPACK"
    }
  }
}
```

If middleware does not call `next.Invoke(context)`, it short-circuits the rest of the field pipeline. Test this behavior because clients can depend on directive semantics.

## Understand middleware order

Hot Chocolate enters directive middleware in this order:

1. Object type directives.
2. Field-definition directives.
3. Query or field-selection directives.
4. Within each group, the order in SDL or the operation.

If middleware changes the result after `next.Invoke(context)`, the final mutation happens as the middleware stack returns. Given a resolver that returns `Trail Backpack`, two executable directives that lowercase or uppercase after `next` produce different values:

```graphql
directive @lower on FIELD
directive @upper on FIELD

type Query {
  title: String!
}
```

```graphql
{
  first: title @lower @upper
  second: title @upper @lower
}
```

Expected result with post-processing middleware:

```json
{
  "data": {
    "first": "trail backpack",
    "second": "TRAIL BACKPACK"
  }
}
```

# Using executable directives in operations

Clients can use executable directives in GraphQL operations. The `@skip` and `@include` directives are available by default.

```graphql
query ProductPage($showInventory: Boolean!, $hideReviews: Boolean!) {
  productById(id: 1) {
    name
    inventory @include(if: $showInventory) {
      available
    }
    reviews @skip(if: $hideReviews) {
      rating
      comment
    }
  }
}
```

Enable incremental delivery directives before clients use them:

```csharp
builder
    .AddGraphQL()
    .ModifyOptions(o =>
    {
        o.EnableDefer = true;
        o.EnableStream = true;
    });
```

Use `@defer` on fragment spreads or inline fragments:

```graphql
query ProductPage($withDetails: Boolean!) {
  productById(id: 1) {
    name
    ...ProductDetails @defer(label: "details", if: $withDetails)
  }
}

fragment ProductDetails on Product {
  description
  specifications
}
```

Use `@stream` on list fields:

```graphql
query ProductFeed {
  products @stream(label: "feed", initialCount: 10) {
    id
    name
  }
}
```

`@include` and `@skip` take precedence over `@defer` and `@stream`. For details on response formats, `Accept` headers, and streaming transport, see [HTTP Transport](/docs/hotchocolate/v16/build/server-configuration/http-transport).

# Verifying directive output and behavior

Check directives at the level where they are relevant.

| What to verify                  | How to verify                                                                                                        |
| ------------------------------- | -------------------------------------------------------------------------------------------------------------------- |
| Generated SDL                   | Download SDL from a `MapGraphQL()` endpoint with `?sdl`, export the schema, or inspect `executor.Schema.ToString()`. |
| Schema snapshots                | Build a schema in a test and call `schema.MatchSnapshot()`.                                                          |
| Deprecation metadata            | Query introspection with `includeDeprecated: true` and inspect `isDeprecated` plus `deprecationReason`.              |
| Opt-in metadata                 | Enable opt-in features and use `includeOptIn` on `fields`, `args`, `inputFields`, or `enumValues`.                   |
| Applied directive introspection | Enable `EnableDirectiveIntrospection` before querying `appliedDirectives`.                                           |
| Runtime behavior                | Execute an operation and assert the result, especially for directive middleware and executable directives.           |

Example: taking a schema snapshot in a test

```csharp
using CookieCrumble;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public sealed class DirectiveSchemaTests
{
    [Fact]
    public async Task Schema_Should_Contain_AudienceDirective()
    {
        // arrange & act
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryType>()
            .AddDirectiveType<AudienceDirectiveType>()
            .BuildSchemaAsync();

        // assert
        schema.MatchSnapshot();
    }
}
```

Introspection example for deprecated and opt-in fields:

```graphql
{
  __type(name: "Product") {
    fields(includeDeprecated: true, includeOptIn: ["experimentalCatalog"]) {
      name
      isDeprecated
      deprecationReason
    }
  }
}
```

The `includeOptIn` argument controls whether opt-in fields appear in the introspection result. Query applied directives separately when tooling needs to inspect directive applications.

Operation behavior example:

```graphql
query ProductPage($showInventory: Boolean!) {
  productById(id: 1) {
    name
    inventory @include(if: $showInventory) {
      available
    }
  }
}
```

When `showInventory` is `false`, the response does not contain the `inventory` field.

# Troubleshooting directives

| Symptom                                                                            | Likely cause                                                                                                                 | Solution                                                                                                                                                    |
| ---------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------- |
| A custom directive definition is missing from SDL.                                 | The directive type was not registered, or `RemoveUnusedTypeSystemDirectives` removed an unused type-system directive.        | Call `.AddDirectiveType<T>()` and apply the directive somewhere, or set `RemoveUnusedTypeSystemDirectives = false` for SDL export scenarios.                |
| A directive application is missing from an object, field, argument, or enum value. | The directive location does not include that schema element, or the descriptor API was applied to the wrong descriptor.      | Add the correct `DirectiveLocation` and apply the directive on the descriptor for the target element.                                                       |
| `appliedDirectives` is missing from introspection.                                 | Applied directive introspection is disabled.                                                                                 | Set `EnableDirectiveIntrospection = true`.                                                                                                                  |
| An internal directive is hidden from directive introspection.                      | The directive is internal or the default directive visibility is internal.                                                   | Call `.Public()` on your directive, adjust `DefaultDirectiveVisibility`, or use `DisableInternalDirectives` for compatibility.                              |
| `@requiresOptIn` does not appear.                                                  | Opt-in feature support is disabled.                                                                                          | Set `ModifyOptions(o => o.EnableOptInFeatures = true)`.                                                                                                     |
| A field with `@requiresOptIn` is hidden from introspection.                        | Opt-in fields are hidden unless requested.                                                                                   | Pass the feature name through `includeOptIn`.                                                                                                               |
| `@requiresOptIn` fails on an argument or input field.                              | The argument or input field is non-null and has no default value.                                                            | Make it nullable or provide a default value before applying the directive.                                                                                  |
| `@defer` or `@stream` is rejected.                                                 | The option is disabled or the location is invalid.                                                                           | Enable `EnableDefer` or `EnableStream` and apply the directive only to valid locations.                                                                     |
| Authorization directives do not run.                                               | ASP.NET Core authorization or GraphQL authorization was not registered, or the wrong attribute namespace was used.           | Follow [Authorization](/docs/hotchocolate/v16/build/security/authorization) and use Hot Chocolate authorization APIs.                                       |
| Cost or list-size directives do not affect validation.                             | Cost analysis was not configured.                                                                                            | Follow [Cost Analysis](/docs/hotchocolate/v16/build/security/cost-analysis) and inspect with `GraphQL-Cost: report` or `GraphQL-Cost: validate`.            |
| Cache-control directives do not write headers.                                     | Cache-control services or query-cache middleware are missing.                                                                | Follow [Cache Control](/docs/hotchocolate/v16/build/performance/cache-control) and register `.AddCacheControl()` plus `.UseQueryCache()` where appropriate. |
| Directive middleware runs in an unexpected order.                                  | Object, field-definition, and operation directives all contribute middleware, and post-processing runs as the stack returns. | Review directive order in SDL and the operation, then add an execution test for the expected result.                                                        |

# Next steps

- Learn how to manage `@deprecated` and `@requiresOptIn` on the [Versioning](/docs/hotchocolate/v16/_leagcy/building-a-schema/versioning) page.
- Configure authorization policies on the [Authorization](/docs/hotchocolate/v16/build/security/authorization) page.
- Set up `@cost` and `@listSize` on the [Cost Analysis](/docs/hotchocolate/v16/build/security/cost-analysis) page.
- Configure `@cacheControl` on the [Cache Control](/docs/hotchocolate/v16/build/performance/cache-control) page.
- Test schema shape and execution results with [Testing](/docs/hotchocolate/v16/_leagcy/guides/testing).
- Review endpoint schema downloads and streaming responses in [Endpoints](/docs/hotchocolate/v16/build/server-configuration/endpoints) and [HTTP Transport](/docs/hotchocolate/v16/build/server-configuration/http-transport).
