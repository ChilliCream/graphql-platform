---
title: "Directive Reference"
---

Fusion implements the [GraphQL Composite Schemas Specification](https://graphql.github.io/composite-schemas-spec/draft/). The directives defined in this specification are applied to source schemas (subgraph schemas) to control how they compose into a unified composite schema. Each directive entry below shows its SDL definition, what it does, and a practical example with the resulting composed output.

In HotChocolate, these directives are expressed using C# attributes. See the individual guide pages for attribute usage and tutorials.

# Quick Reference

| Directive                        | Locations                    | Repeatable | Purpose                                           |
| -------------------------------- | ---------------------------- | :--------: | ------------------------------------------------- |
| [`@key`](#key)                   | `OBJECT`, `INTERFACE`        |    Yes     | Define entity identity                            |
| [`@lookup`](#lookup)             | `FIELD_DEFINITION`           |     No     | Mark entity lookup resolvers                      |
| [`@is`](#is)                     | `ARGUMENT_DEFINITION`        |     No     | Map lookup arguments to entity fields             |
| [`@require`](#require)           | `ARGUMENT_DEFINITION`        |     No     | Declare cross-subgraph data dependencies          |
| [`@shareable`](#shareable)       | `OBJECT`, `FIELD_DEFINITION` |    Yes     | Allow multiple subgraphs to define the same field |
| [`@provides`](#provides)         | `FIELD_DEFINITION`           |     No     | Declare locally-resolvable subfields              |
| [`@external`](#external)         | `FIELD_DEFINITION`           |     No     | Mark field as owned by another subgraph           |
| [`@override`](#override)         | `FIELD_DEFINITION`           |     No     | Migrate field ownership between subgraphs         |
| [`@internal`](#internal)         | `OBJECT`, `FIELD_DEFINITION` |     No     | Hide from composite schema and merge process      |
| [`@inaccessible`](#inaccessible) | 10 locations                 |     No     | Hide from client-facing composite schema          |

---

# Entity Identity and Resolution

## `@key`

Designates an entity's unique key, which identifies how to uniquely reference an instance of an entity across different source schemas.

```graphql
directive @key(fields: FieldSelectionSet!) repeatable on OBJECT | INTERFACE
```

| Argument | Type                 | Description                                                                      |
| -------- | -------------------- | -------------------------------------------------------------------------------- |
| `fields` | `FieldSelectionSet!` | A field selection set that forms the unique key (e.g. `"id"` or `"tenantId id"`) |

Each `@key` directive on a type specifies one distinct unique key for that entity. Apply multiple `@key` directives to define alternative keys that the gateway can use to resolve the entity. Fields referenced in a key are implicitly shareable across subgraphs -- you do not need to add `@shareable` to key fields.

**Example -- single key:**

```graphql
# Source schema
type Product @key(fields: "id") {
  id: ID!
  name: String!
  price: Float!
}
```

```graphql
# Composed schema
type Product {
  id: ID!
  name: String!
  price: Float!
}
```

**Example -- multiple keys:**

```graphql
# Source schema
type Product @key(fields: "id") @key(fields: "sku") {
  id: ID!
  sku: String!
  name: String!
}
```

**Example -- composite key:**

```graphql
# Source schema (both fields required together)
type Product @key(fields: "id sku") {
  id: ID!
  sku: String!
  name: String!
}
```

> **In C#:** `[EntityKey("id")]` attribute. See [Entities and Lookups](/docs/fusion/v16/entities-and-lookups).

---

## `@lookup`

Marks a field as an entity lookup resolver that the gateway uses to resolve an entity by a stable key.

```graphql
directive @lookup on FIELD_DEFINITION
```

Lookup fields provide the gateway with entry points into a subgraph for entity resolution. A source schema can define multiple lookup fields for the same entity to support resolution by different keys. Lookup fields must return a nullable type and must not return a list.

**Example:**

```graphql
# Source schema
type Query {
  productById(id: ID!): Product @lookup
  productByName(name: String!): Product @lookup
}

type Product @key(fields: "id") @key(fields: "name") {
  id: ID!
  name: String!
}
```

```graphql
# Composed schema
type Query {
  productById(id: ID!): Product
  productByName(name: String!): Product
}

type Product {
  id: ID!
  name: String!
}
```

> **In C#:** `[Lookup]` attribute. See [Entities and Lookups](/docs/fusion/v16/entities-and-lookups).

---

## `@is`

Maps a lookup argument to a field on the entity type when the argument name does not match the field name.

```graphql
directive @is(field: FieldSelectionMap!) on ARGUMENT_DEFINITION
```

| Argument | Type                 | Description                                                                   |
| -------- | -------------------- | ----------------------------------------------------------------------------- |
| `field`  | `FieldSelectionMap!` | A selection map that describes the mapping from entity fields to the argument |

When a lookup argument name matches the corresponding field on the return type, you can omit `@is`. Use `@is` when the names differ or when the mapping involves nested fields.

**Example -- argument name differs from field name:**

```graphql
# Source schema
type Query {
  personById(personId: ID! @is(field: "id")): Person @lookup
}

type Person @key(fields: "id") {
  id: ID!
  name: String!
}
```

```graphql
# Composed schema
type Query {
  personById(personId: ID!): Person
}

type Person {
  id: ID!
  name: String!
}
```

**Example -- nested field reference:**

```graphql
# Source schema
type Query {
  personByAddressId(id: ID! @is(field: "address.id")): Person @lookup
}
```

> **In C#:** The `@is` mapping is inferred automatically from the argument name. When it does not match, use the field parameter convention described in [Entities and Lookups](/docs/fusion/v16/entities-and-lookups).

---

# Data Requirements

## `@require`

Declares that a resolver argument needs data from fields owned by other subgraphs. The gateway resolves the required data first, then passes it to the resolver. Arguments annotated with `@require` are removed from the composed client-facing schema.

```graphql
directive @require(field: FieldSelectionMap!) on ARGUMENT_DEFINITION
```

| Argument | Type                 | Description                                                             |
| -------- | -------------------- | ----------------------------------------------------------------------- |
| `field`  | `FieldSelectionMap!` | A selection map describing which fields from the entity type are needed |

Use `@require` when a resolver in one subgraph needs data that another subgraph owns. The gateway handles the data fetching automatically. This shifts cross-service data dependencies from hidden runtime failures to validated build-time contracts.

**Example -- scalar requirement:**

```graphql
# Source schema (Shipping subgraph)
type Product {
  shippingEstimate(zip: String!, weight: Float! @require(field: "weight")): Int!
}
```

```graphql
# Composed schema (weight argument removed)
type Product {
  shippingEstimate(zip: String!): Int!
}
```

**Example -- structured requirement with input type:**

```graphql
# Source schema
type Product {
  delivery(
    zip: String!
    dimension: ProductDimensionInput!
      @require(field: "{ size: dimension.size, weight: dimension.weight }")
  ): DeliveryEstimates
}
```

> **In C#:** `[Require]` attribute on method parameters. See [Data Requirements](/docs/fusion/v16/data-requirements-and-mapping).

---

# Field Ownership and Sharing

## `@shareable`

Allows multiple subgraphs to define the same field. Without `@shareable`, defining the same non-key field in two subgraphs causes a composition error.

```graphql
directive @shareable repeatable on OBJECT | FIELD_DEFINITION
```

When multiple subgraphs mark the same field as `@shareable`, they declare that the field is semantically equivalent across all definitions. The gateway is free to resolve the field from any subgraph that defines it. Apply `@shareable` to an object type to make all its fields shareable.

**Example:**

```graphql
# Source schema A (Products subgraph)
type Product @key(fields: "id") {
  id: ID!
  name: String! @shareable
  description: String!
}
```

```graphql
# Source schema B (Inventory subgraph)
type Product @key(fields: "id") {
  id: ID!
  name: String! @shareable
  inStock: Boolean!
}
```

```graphql
# Composed schema
type Product {
  id: ID!
  name: String!
  description: String!
  inStock: Boolean!
}
```

> **In C#:** `[Shareable]` attribute. See [Field Ownership](/docs/fusion/v16/field-ownership-and-sharing).

---

## `@provides`

Declares that a field returning an entity can resolve specific subfields of that entity locally, without requiring an additional call to another subgraph.

```graphql
directive @provides(fields: FieldSelectionSet!) on FIELD_DEFINITION
```

| Argument | Type                 | Description                                                                                        |
| -------- | -------------------- | -------------------------------------------------------------------------------------------------- |
| `fields` | `FieldSelectionSet!` | A field selection set describing the subfields of the returned type that this subgraph can resolve |

This is a query-planning optimization. When a client requests provided subfields through this particular field path, the gateway resolves them from the current subgraph instead of making a separate call. Fields referenced in `@provides` must be marked `@external` on the return type.

**Example:**

```graphql
# Source schema (Reviews subgraph)
type Review {
  id: ID!
  body: String!
  author: User @provides(fields: "email")
}

type User @key(fields: "id") {
  id: ID!
  email: String! @external
}
```

```graphql
# Composed schema
type Review {
  id: ID!
  body: String!
  author: User
}

type User {
  id: ID!
  email: String!
}
```

> **In C#:** `[Provides("email")]` attribute. See [Field Ownership](/docs/fusion/v16/field-ownership-and-sharing).

---

## `@external`

Marks a field as owned by another subgraph. The current subgraph references the field for entity identification (via `@key`) or to provide it locally through `@provides`.

```graphql
directive @external on FIELD_DEFINITION
```

Every `@external` field must be referenced by at least one `@provides` directive or used in a `@key`. An unused `@external` field causes a composition error. External fields cannot be combined with `@override` or `@provides` on the same field.

**Example -- external field used with `@provides`:**

```graphql
# Source schema (Reviews subgraph)
type Review {
  id: ID!
  author: User @provides(fields: "email")
}

type User @key(fields: "id") {
  id: ID!
  email: String! @external
}
```

**Example -- external field used as entity key:**

```graphql
# Source schema (Payments subgraph)
type Product @key(fields: "sku") {
  sku: String! @external
  price: Float!
}

type Query {
  productBySku(sku: String!): Product @lookup
}
```

> **In C#:** `[External]` attribute (from `HotChocolate.ApolloFederation.Types`). See [Field Ownership](/docs/fusion/v16/field-ownership-and-sharing).

---

## `@override`

Migrates field ownership from one subgraph to another. The current subgraph takes responsibility for resolving the field, and the original subgraph stops serving it. The original subgraph does not need to be modified.

```graphql
directive @override(from: String!) on FIELD_DEFINITION
```

| Argument | Type      | Description                                                       |
| -------- | --------- | ----------------------------------------------------------------- |
| `from`   | `String!` | The name of the source schema that originally provided this field |

Use `@override` to move a field to a new subgraph during schema evolution. The overriding subgraph typically marks the entity's key fields as `@external`. Cyclic overrides cause a composition error.

**Example:**

```graphql
# Source schema: original Catalog subgraph (unchanged)
type Product @key(fields: "id") {
  id: ID!
  name: String!
  price: Float!
}
```

```graphql
# Source schema: new Payments subgraph (takes over price)
type Product @key(fields: "id") {
  id: ID! @external
  price: Float! @override(from: "Catalog")
  tax: Float!
}
```

```graphql
# Composed schema
type Product {
  id: ID!
  name: String!
  price: Float!
  tax: Float!
}
```

> **In C#:** `[Override("Catalog")]` attribute (from `HotChocolate.ApolloFederation.Types`). See [Schema Exposure and Evolution](/docs/fusion/v16/schema-exposure-and-evolution).

---

# Visibility

## `@internal`

Hides a type or field from the composite schema and excludes it from the standard schema-merging process. The gateway can still use internal fields as lookup entry points for entity resolution.

```graphql
directive @internal on OBJECT | FIELD_DEFINITION
```

Internal types and fields do not collide with similarly named fields or types on other source schemas, because they bypass merge rules entirely. Use `@internal` to create resolution-only entry points that clients cannot query directly.

**Example:**

```graphql
# Source schema A
type Query {
  productById(id: ID!): Product @lookup
  productBySku(sku: ID!): Product @lookup @internal
}

type Product @key(fields: "id") @key(fields: "sku") {
  id: ID!
  sku: ID!
  name: String!
}
```

```graphql
# Composed schema (internal lookup removed)
type Query {
  productById(id: ID!): Product
}

type Product {
  id: ID!
  sku: ID!
  name: String!
}
```

> **In C#:** `[Internal]` attribute. See [Schema Exposure and Evolution](/docs/fusion/v16/schema-exposure-and-evolution).

---

## `@inaccessible`

Prevents a type system member from appearing in the client-facing composite schema, even if it is accessible in the underlying source schemas.

```graphql
directive @inaccessible on FIELD_DEFINITION | OBJECT | INTERFACE | UNION | ARGUMENT_DEFINITION | SCALAR | ENUM | ENUM_VALUE | INPUT_OBJECT | INPUT_FIELD_DEFINITION
```

Unlike `@internal`, inaccessible elements still participate in composition merging and can be referenced by `@require` dependencies in other subgraphs. If any source schema marks a type system member as `@inaccessible`, it is hidden from the composite schema -- even if other schemas expose the same member without `@inaccessible`.

**Example:**

```graphql
# Source schema A
type Product @key(fields: "id") @key(fields: "sku") {
  id: ID!
  sku: String! @inaccessible
  name: String!
}
```

```graphql
# Source schema B
type Product @key(fields: "sku") {
  sku: String!
  price: Float!
}
```

```graphql
# Composed schema (sku hidden by schema A's @inaccessible)
type Product {
  id: ID!
  name: String!
  price: Float!
}
```

> **In C#:** `[Inaccessible]` attribute. See [Schema Exposure and Evolution](/docs/fusion/v16/schema-exposure-and-evolution).

---

# `@internal` vs `@inaccessible`

These two directives both hide elements from the composite schema, but they behave differently during composition:

| Aspect         | `@internal`                      | `@inaccessible`                       |
| -------------- | -------------------------------- | ------------------------------------- |
| Scope          | Local to its source schema       | Global across all subgraphs           |
| Merge behavior | Bypasses merge rules entirely    | Participates in merge, then hidden    |
| Collision      | No collisions with other schemas | Hides even if other schemas expose it |
| Use case       | Internal lookup entry points     | Restrict client access to fields      |

Use `@internal` when a field or type exists solely for the gateway's entity resolution and should not interact with other schemas at all. Use `@inaccessible` when a field carries data that other subgraphs may depend on through `@require`, but clients should not query it directly.

---

# See Also

- [GraphQL Composite Schemas Specification](https://graphql.github.io/composite-schemas-spec/draft/) -- The specification that defines these directives
- [Getting Started](/docs/fusion/v16/getting-started) -- Introduction to Fusion in practice
- [Entities and Lookups](/docs/fusion/v16/entities-and-lookups) -- Entity resolution patterns with `@key`, `@lookup`, and `@is`
- [Field Ownership](/docs/fusion/v16/field-ownership-and-sharing) -- Ownership model with `@shareable`, `@external`, and `@provides`
- [Data Requirements](/docs/fusion/v16/data-requirements-and-mapping) -- Cross-subgraph data dependencies with `@require`
- [Schema Exposure and Evolution](/docs/fusion/v16/schema-exposure-and-evolution) -- Visibility control with `@internal`, `@inaccessible`, and `@override`
- [Cache Control](/docs/fusion/v16/cache-control) -- CDN and HTTP caching behavior
- [Composition](/docs/fusion/v16/composition) -- How directives affect schema merging
