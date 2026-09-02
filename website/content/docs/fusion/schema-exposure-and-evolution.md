---
title: "Schema Exposure and Evolution"
description: "Control client visibility and schema evolution in Fusion with @inaccessible, @internal, @deprecated, @requiresOptIn, and @override field migration."
---

Not everything in your source schema should be visible to clients, and not everything should stay the same forever. As your distributed graph grows, you need control over two things: what clients can see today, and how the schema changes over time.

This page covers five directives that handle exposure and evolution. `@inaccessible` and `@internal` control visibility in the composite schema. `@deprecated` and `@requiresOptIn` manage the lifecycle of fields and values. `@override` migrates field ownership between subgraphs. If you have completed the [Getting Started](./getting-started.md) tutorial and worked through [Entities and Lookups](./entities-and-lookups.md), you already used `@internal` on lookup fields. Here, you will see the full picture of visibility control and schema evolution.

# Controlling Client Visibility

Your source schemas contain fields and types that serve different audiences. Some are for clients, some carry internal data shared between subgraphs, and some are infrastructure that only the gateway uses. Fusion provides two directives for hiding schema elements from the composite schema. They differ in how they interact with composition merging.

## Hidden Fields

Mark a field or type `@inaccessible` to hide it from the public client-facing composite schema while keeping it available for internal. The element still participates in composition merging and can be referenced by `@require` dependencies in other subgraphs.

**GraphQL schema**

```graphql
type Product @key(fields: "id") {
  id: ID!
  name: String!
  price: Float!
  internalSkuCode: Int! @inaccessible
}
```

**C# declaration**

```csharp
[EntityKey("id")]
public class Product
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public double Price { get; set; }

    [Inaccessible]
    public int InternalSkuCode { get; set; }
}
```

Clients cannot query `internalSkuCode`. But other subgraphs can depend on it through `[Require]`. For example, a Warehouse subgraph could require the SKU code for inventory lookups without exposing it to clients.

Apart from `@require` inaccessible fields can also be used as lookups or as keys.

You can apply `@inaccessible` to fields, types, arguments, enum values, input fields, scalars, interfaces, and unions. Any schema element that can appear in the composite schema can be hidden.

**Enum values**

Hiding individual enum values is useful when different subgraphs define the same enum with slightly different values. Mark the values that should not be in the composite schema as `@inaccessible` to resolve merge conflicts.

```graphql
enum OrderStatus {
  PENDING
  SHIPPED
  DELIVERED
  CANCELLED @inaccessible
}
```

The `CANCELLED` value does not appear in the composite schema. Subgraphs can still return it internally, but clients never see it.

**Constraint:** You cannot mark a required input field as `@inaccessible`. If a client must provide a value, they need to see the field. Composition fails if you try.

## Internal Lookups

The `@internal` directive is designed for lookups. An internal lookup is a query field that the gateway uses for entity resolution but that clients cannot call. Internal lookups do not participate in composition merging, which means multiple subgraphs can define lookups with the same field name and different argument shapes without causing a conflict. This gives each subgraph the flexibility to resolve an entity in whatever way makes sense for its data, without coordinating field signatures across teams.

**GraphQL schema**

```graphql
type Query {
  productById(id: ID!): Product @lookup @internal
}
```

**C# resolver**

```csharp
[QueryType]
public static partial class ProductQueries
{
    [Internal, Lookup]
    public static Product? GetProductById(int id)
        => new(id);
}
```

Without `[Internal]`, this lookup would appear in the composite schema as a second `productById` query field, conflicting with the Products subgraph's public lookup. With `[Internal]`, the gateway can still use it for entity resolution, but clients never see it.

You can also group internal lookups under a dedicated root object to keep routing infrastructure in one place.

**GraphQL schema (grouped internal lookups)**

```graphql
type Query {
  internalLookups: InternalLookups @internal
}

type InternalLookups @internal {
  productByTenantAndSku(tenantId: ID!, sku: String!): Product @lookup
}
```

**C# declaration**

```csharp filename="Reviews/Types/InternalLookups.cs"
[QueryType]
public static partial class Query
{
    [Internal]
    public static InternalLookups GetInternalLookups { get; } = new();
}

[Internal, ObjectType]
public partial class InternalLookups
{
    [Lookup]
    public Product? GetProductByTenantAndSku(int tenantId, string sku)
        => ProductRepository.GetByTenantAndSku(tenantId, sku);
}
```

For a deeper look at internal vs. public lookups, composite keys, and the node pattern, see [Entities and Lookups](./entities-and-lookups.md).

## Choosing Between Hidden and Internal

These directives serve different purposes. `@inaccessible` hides data from clients while keeping it available across subgraphs. `@internal` keeps lookups local to one subgraph so they can vary freely without merge conflicts.

| Behavior                          | `@inaccessible`                        | `@internal`                         |
| --------------------------------- | -------------------------------------- | ----------------------------------- |
| Visible to clients                | No                                     | No                                  |
| Participates in merging           | Yes                                    | No                                  |
| Can conflict across subgraphs     | Yes (types must be compatible)         | No                                  |
| Usable in `@require` dependencies | Yes                                    | No                                  |
| Primary use case                  | Internal data shared between subgraphs | Lookup entry points for the gateway |

Use `@inaccessible` when the field carries data that other subgraphs need but clients should not see. Use `@internal` on lookups that exist only for gateway entity resolution.

# Deprecating Fields, Values, and Types

The `@deprecated` directive signals that a field, argument, or enum value is being phased out. Clients see the deprecation reason in introspection, and GraphQL tooling (IDEs, linters, code generators) can warn consumers to migrate away. The field continues to work. Deprecation is a soft signal, not a hard removal.

**GraphQL schema**

```graphql
type Query {
  product(id: ID!): Product
    @lookup
    @deprecated(reason: "Use `productById` instead.")
  productBySku(sku: String!): Product @lookup
}
```

**C# resolver**

```csharp filename="Products/Types/ProductQueries.cs"
[QueryType]
public static partial class ProductQueries
{
    [GraphQLDeprecated("Use `productBySku` instead.")]
    [Lookup]
    public static async Task<Product?> GetProductAsync(
        int id,
        IProductByIdDataLoader productById,
        CancellationToken cancellationToken)
        => await productById.LoadAsync(id, cancellationToken);

    [Lookup]
    public static async Task<Product?> GetProductBySkuAsync(
        string sku,
        IProductBySkuDataLoader productBySku,
        CancellationToken cancellationToken)
        => await productBySku.LoadAsync(id, cancellationToken);
}
```

You can also use .NET's built-in `[Obsolete]` attribute. Hot Chocolate treats it the same as `[GraphQLDeprecated]` for fields, arguments, input fields, and enum values. Deprecating an object type itself is different: see [Deprecating Object Types](#deprecating-object-types).

```csharp
[Obsolete("Use `productById` instead.")]
[Lookup]
public static async Task<Product?> GetProductAsync(...)
    => ...;
```

Deprecation applies to output fields, input fields, arguments, and enum values without any extra configuration. Object types can be deprecated too, but only once you enable a separate opt-in option (see [Deprecating Object Types](#deprecating-object-types)).

**Enum value deprecation**

```graphql
enum SortOrder {
  ASC
  DESC
  RELEVANCE @deprecated(reason: "Use full-text search instead.")
}
```

**Constraint:** You cannot deprecate a non-null argument or input field without a default value. If clients must provide a value, they cannot stop using the field.

## Deprecating Object Types

Object type deprecation is not yet part of the released GraphQL specification. It tracks [graphql-spec RFC #997](https://github.com/graphql/graphql-spec/pull/997), which is still open, so its final shape could still change. It is disabled by default; enable it separately on each subgraph and on the Fusion gateway.

**Subgraph configuration**

```csharp filename="Products/Program.cs"
builder
    .AddGraphQL("Products")
    .AddTypes()
    .ModifyOptions(o => o.EnableObjectDeprecation = true);
```

**Gateway configuration**

```csharp filename="Gateway/Program.cs"
builder
    .AddGraphQLGateway()
    .AddFileSystemConfiguration("./gateway.far")
    .ModifyOptions(o => o.EnableObjectDeprecation = true);
```

When `EnableObjectDeprecation` is enabled on the gateway, the introspection schema exposes `isDeprecated` and `deprecationReason` on `__Type`, plus the `includeDeprecated` argument on `__schema.types` and `__Type.possibleTypes`. Deprecated object types are hidden from both by default; clients pass `includeDeprecated: true` to see them.

**GraphQL schema**

```graphql
type Query {
  animals: [Animal]
}

interface Animal {
  name: String
}

type Dog implements Animal {
  name: String
}

type Baiji implements Animal @deprecated(reason: "No longer known to exist.") {
  name: String
}
```

**C# declaration**

```csharp
[GraphQLDeprecated("No longer known to exist.")]
public class Baiji
{
    public string? Name { get; set; }
}
```

The .NET `[Obsolete]` attribute does not deprecate an object type; only `[GraphQLDeprecated]` on the class does. Unifying the two attributes for object types is deferred to a future major version.

A field that is not itself deprecated cannot return a deprecated object type. Here, `Query.animals` returns the interface `Animal`, not `Baiji` directly, so the subgraph's schema is valid. A field that returns `Baiji` directly would need to be deprecated too:

```graphql
type Query {
  baiji: Baiji
}

type Baiji @deprecated(reason: "No longer known to exist.") {
  name: String
}
```

**Constraint:** `Query.baiji` is not deprecated but returns the deprecated `Baiji` type, so building the subgraph's schema fails. Deprecate the field, or change its return type.

A deprecated object type remains a valid union member and interface implementation. Only the field returning it is checked.

## Deprecation Across Subgraphs

If a shareable field is deprecated in at least one subgraph, it is deprecated in the composite schema. You do not need to deprecate it in every subgraph that defines it. With shared ownership comes the power for any owner to deprecate the field for all clients.

If you only want to remove a shared field from one subgraph, you do not need to deprecate it. Remove the field from that subgraph and the gateway will resolve it from the remaining subgraphs that still provide it.

Object types follow the same rule. If at least one subgraph deprecates a type, it is deprecated in the composite schema, and the reason is taken from the first subgraph that provides one.

# Experimental and Preview Features

The `@requiresOptIn` directive is the counterpart to `@deprecated`. Where `@deprecated` signals that a field is going away, `@requiresOptIn` signals that a field is not yet stable. Fields marked with `@requiresOptIn` are hidden from introspection by default. Clients must explicitly opt in to discover and use them.

This is useful for rolling out experimental features, expensive operations, or anything where the consumer should make a conscious decision before using it.

**GraphQL schema**

```graphql
type Product {
  id: ID!
  name: String!
  price: Float!
  dynamicPrice: Decimal @requiresOptIn(feature: "experimentalPricing")
}
```

**C# declaration**

```csharp filename="Products/Types/Product.cs"
public class Product
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public double Price { get; set; }

    [RequiresOptIn("experimentalPricing")]
    public decimal? DynamicPrice { get; set; }
}
```

The `dynamicPrice` field does not appear in standard introspection results. Clients must opt in to see it.

The directive is repeatable. A single field can be part of multiple features.

```csharp
[RequiresOptIn("experimentalPricing")]
[RequiresOptIn("betaApi")]
public decimal? DynamicPrice { get; set; }
```

## Enabling Opt-In Support

Opt-in features are disabled by default. You must enable them separately on each subgraph and on the Fusion gateway.

**Subgraph configuration**

```csharp filename="Products/Program.cs"
builder
    .AddGraphQL("Products")
    .AddTypes()
    .ModifyOptions(o => o.EnableOptInFeatures = true);
```

**Gateway configuration**

```csharp filename="Gateway/Program.cs"
builder
    .AddGraphQLGateway()
    .AddFileSystemConfiguration("./gateway.far")
    .ModifyOptions(o => o.EnableOptInFeatures = true);
```

When `EnableOptInFeatures` is enabled on the gateway, the introspection schema exposes the `includeOptIn` argument and `__schema.optInFeatures` / `optInFeatureStability` fields. Members marked `@requiresOptIn` are hidden from introspection unless the client opts into their feature via `includeOptIn`. Opt-in affects introspection visibility only: hidden members remain fully executable, and the gateway never rejects a request that selects an opt-in field.

## Empty Selection Sets

Enable empty selection sets with `EnableEmptySelectionSets`:

```csharp filename="Gateway/Program.cs"
builder
    .AddGraphQLGateway()
    .ModifyOptions(o => o.EnableEmptySelectionSets = true);
```

The gateway resolves empty selection sets locally and never sends one to a source schema. It requests `__typename` downstream and removes the synthetic field from the result.

The default remains `false` until [RFC PR 1227](https://github.com/graphql/graphql-spec/pull/1227) merges into the specification draft and is planned to change to `true` in the next major release.

## Discovering Opt-In Fields

Clients pass the `includeOptIn` argument in introspection queries to discover opt-in fields.

```graphql
{
  __type(name: "Product") {
    fields(includeOptIn: ["experimentalPricing"]) {
      name
      requiresOptIn
    }
  }
}
```

The `includeOptIn` argument is available on `fields`, `args`, `inputFields`, and `enumValues` in introspection queries.

To discover which opt-in features exist in the schema:

```graphql
{
  __schema {
    optInFeatures
  }
}
```

## Feature Stability Levels

You can declare the stability level of each opt-in feature at the schema level. This lets consumers know whether a feature is experimental, preview, or any other stability level you define.

**C# configuration**

```csharp filename="Products/Program.cs"
builder
    .AddGraphQL("Products")
    .AddTypes()
    .ModifyOptions(o => o.EnableOptInFeatures = true)
    .OptInFeatureStability("experimentalPricing", "experimental");
```

Consumers can query feature stability through introspection:

```graphql
{
  __schema {
    optInFeatureStability {
      feature
      stability
    }
  }
}
```

## Constraints

Like `@deprecated`, you cannot apply `@requiresOptIn` to non-null arguments or input fields without a default value. Hiding a required field would break queries that need to provide it.

## Opt-In Across Subgraphs

If a shareable field is marked `@requiresOptIn` in at least one subgraph, it requires opt-in in the composite schema. To make the field generally available again, every subgraph that defines it must remove the `@requiresOptIn` directive. This is the inverse of `@deprecated`, where a single subgraph can deprecate a field for all clients. With `@requiresOptIn`, a single subgraph can gate a shared field behind opt-in, and it stays gated until all owners agree to remove the restriction.

## End-to-End Flow

The full lifecycle of an opt-in feature across a Fusion deployment is:

1. A subgraph marks a field (or enum value, or argument) with `@requiresOptIn(feature: "featureName")` and enables opt-in support with `ModifyOptions(o => o.EnableOptInFeatures = true)`.
2. Composition merges `@requiresOptIn` by union: if a shareable member is opt-in in any source schema, it is opt-in in the execution schema.
3. The gateway, configured with `EnableOptInFeatures` enabled, hides opt-in members from introspection by default. Clients that pass `includeOptIn: ["featureName"]` in their introspection query see those members.
4. Opt-in members are fully executable regardless of whether the client opted in at the introspection level. The gateway does not reject execution-time requests for opt-in fields.

## Stability Mismatch Across Subgraphs

When multiple subgraphs declare stability for the same opt-in feature, they must agree. If two source schemas declare different stability values for the same feature name, composition fails with `OPT_IN_FEATURE_STABILITY_MISMATCH`. Align the `OptInFeatureStability` call for that feature across all subgraphs that declare it.

# Migrating Field Ownership Between Subgraphs

As your system evolves, you may need to move a field from one subgraph to another. A team might split a subgraph, or a field might belong more naturally in a different domain. The `@override` directive migrates field ownership without breaking existing queries.

When you apply `[Override(from: "source-subgraph")]`, the gateway routes requests for that field to the new subgraph instead of the original. The old subgraph's resolver is no longer called. No client-facing changes are needed.

**Before: Products subgraph owns the reviews field**

```csharp
[ObjectType<Product>]
public static partial class ProductNode
{
    public static async Task<IEnumerable<Review>> GetReviewsAsync(
        [Parent] Product product,
        ReviewService reviewService)
        => await reviewService.GetReviewsByProductIdAsync(product.Id);
}
```

**After: Reviews subgraph takes ownership**

```csharp
[ObjectType<Product>]
public static partial class ProductNode
{
    [Override(from: "products-api")]
    public static async Task<Connection<Review>> GetReviewsAsync(
        [Parent] Product product,
        PagingArguments args,
        IReviewsByProductIdDataLoader loader,
        CancellationToken ct)
        => await loader
            .With(args)
            .LoadAsync(product.Id, ct)
            .ToConnectionAsync();
}
```

**GraphQL schema**

```graphql
# Reviews subgraph
type Product {
  id: ID!
  reviews: [Review!]! @override(from: "products-api")
}
```

The `from` argument is the subgraph name (from `schema-settings.json`) that originally owned the field.

## Migration Workflow

1. Add the field to the new subgraph with `[Override(from: "old-subgraph")]`.
2. Export schemas and compose. Composition validates that the override is valid.
3. Deploy the new subgraph. The gateway routes the field to it.
4. Remove the old resolver from the original subgraph when ready.

The old resolver stays in place during the transition. Both subgraphs can define the field simultaneously because `[Override]` tells composition which one wins. This avoids duplicate-field errors without requiring `[Shareable]`.

# Next Steps

- **Need entity resolution patterns?** See [Entities and Lookups](./entities-and-lookups.md) for public vs. internal lookups, composite keys, and the node pattern.
- **Need cross-subgraph field dependencies?** See [Data Requirements](./data-requirements-and-mapping.md) for `@require`, `@is`, and FieldSelectionMap patterns.
- **Need field sharing and ownership rules?** See [Field Ownership](./field-ownership-and-sharing.md) for `@shareable`, `@external`, and `@provides` patterns.
- **Adding a new subgraph?** See [Adding a Subgraph](./adding-a-subgraph.md) for the full walkthrough of creating and composing a new subgraph.
