---
title: "Schema Exposure and Evolution"
---

Not everything in your source schema should be visible to clients, and not everything should stay the same forever. As your distributed graph grows, you need control over two things: what clients can see today, and how the schema changes over time.

This page covers five directives that handle exposure and evolution. `@inaccessible` and `@internal` control visibility in the composite schema. `@deprecated` and `@requiresOptIn` manage the lifecycle of fields and values. `@override` migrates field ownership between subgraphs. If you have completed the [Getting Started](/docs/fusion/v16/getting-started) tutorial and worked through [Entities and Lookups](/docs/fusion/v16/entities-and-lookups), you already used `@internal` on lookup fields. Here, you will see the full picture of visibility control and schema evolution.

## Controlling Client Visibility

Your source schemas contain fields and types that serve different audiences. Some are for clients, some carry internal data shared between subgraphs, and some are infrastructure that only the gateway uses. Fusion provides two directives for hiding schema elements from the composite schema. They differ in how they interact with composition merging.

### Hidden Fields

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

### Internal Lookups

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

```csharp
// Reviews/Types/InternalLookups.cs

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

For a deeper look at internal vs. public lookups, composite keys, and the node pattern, see [Entities and Lookups](/docs/fusion/v16/entities-and-lookups).

### Choosing Between Hidden and Internal

These directives serve different purposes. `@inaccessible` hides data from clients while keeping it available across subgraphs. `@internal` keeps lookups local to one subgraph so they can vary freely without merge conflicts.

| Behavior                          | `@inaccessible`                        | `@internal`                         |
| --------------------------------- | -------------------------------------- | ----------------------------------- |
| Visible to clients                | No                                     | No                                  |
| Participates in merging           | Yes                                    | No                                  |
| Can conflict across subgraphs     | Yes (types must be compatible)         | No                                  |
| Usable in `@require` dependencies | Yes                                    | No                                  |
| Primary use case                  | Internal data shared between subgraphs | Lookup entry points for the gateway |

Use `@inaccessible` when the field carries data that other subgraphs need but clients should not see. Use `@internal` on lookups that exist only for gateway entity resolution.

## Deprecating Fields and Values

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

```csharp
// Products/Types/ProductQueries.cs

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

You can also use .NET's built-in `[Obsolete]` attribute. Hot Chocolate treats it the same as `[GraphQLDeprecated]`.

```csharp
[Obsolete("Use `productById` instead.")]
[Lookup]
public static async Task<Product?> GetProductAsync(...)
    => ...;
```

Deprecation applies to output fields, input fields, arguments, and enum values.

**Enum value deprecation**

```graphql
enum SortOrder {
  ASC
  DESC
  RELEVANCE @deprecated(reason: "Use full-text search instead.")
}
```

**Constraint:** You cannot deprecate a non-null argument or input field without a default value. If clients must provide a value, they cannot stop using the field.

### Deprecation Across Subgraphs

If a shareable field is deprecated in at least one subgraph, it is deprecated in the composite schema. You do not need to deprecate it in every subgraph that defines it. With shared ownership comes the power for any owner to deprecate the field for all clients.

If you only want to remove a shared field from one subgraph, you do not need to deprecate it. Remove the field from that subgraph and the gateway will resolve it from the remaining subgraphs that still provide it.

## Experimental and Preview Features

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

```csharp
// Products/Types/Product.cs

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

### Enabling Opt-In Support

Opt-in features are disabled by default. Enable them in your schema configuration.

**C# configuration**

```csharp
// Products/Program.cs

builder
    .AddGraphQL("Products")
    .AddTypes()
    .ModifyOptions(o => o.EnableOptInFeatures = true);
```

### Discovering Opt-In Fields

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

### Feature Stability Levels

You can declare the stability level of each opt-in feature at the schema level. This lets consumers know whether a feature is experimental, preview, or any other stability level you define.

**C# configuration**

```csharp
// Products/Program.cs

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

### Constraints

Like `@deprecated`, you cannot apply `@requiresOptIn` to non-null arguments or input fields without a default value. Hiding a required field would break queries that need to provide it.

### Opt-In Across Subgraphs

If a shareable field is marked `@requiresOptIn` in at least one subgraph, it requires opt-in in the composite schema. To make the field generally available again, every subgraph that defines it must remove the `@requiresOptIn` directive. This is the inverse of `@deprecated`, where a single subgraph can deprecate a field for all clients. With `@requiresOptIn`, a single subgraph can gate a shared field behind opt-in, and it stays gated until all owners agree to remove the restriction.

## Migrating Field Ownership Between Subgraphs

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

### Migration Workflow

1. Add the field to the new subgraph with `[Override(from: "old-subgraph")]`.
2. Export schemas and compose. Composition validates that the override is valid.
3. Deploy the new subgraph. The gateway routes the field to it.
4. Remove the old resolver from the original subgraph when ready.

The old resolver stays in place during the transition. Both subgraphs can define the field simultaneously because `[Override]` tells composition which one wins. This avoids duplicate-field errors without requiring `[Shareable]`.

## Next Steps

- **Need entity resolution patterns?** See [Entities and Lookups](/docs/fusion/v16/entities-and-lookups) for public vs. internal lookups, composite keys, and the node pattern.
- **Need cross-subgraph field dependencies?** See [Data Requirements](/docs/fusion/v16/data-requirements-and-mapping) for `@require`, `@is`, and FieldSelectionMap patterns.
- **Need field sharing and ownership rules?** See [Field Ownership](/docs/fusion/v16/field-ownership-and-sharing) for `@shareable`, `@external`, and `@provides` patterns.
- **Adding a new subgraph?** See [Adding a Subgraph](/docs/fusion/v16/adding-a-subgraph) for the full walkthrough of creating and composing a new subgraph.
