# Transform_Should_GenerateSingleLookup_When_KeysDifferOnlyInWhitespaceOrFieldOrder

## Apollo Federation SDL

```graphql
schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"]) {
  query: Query
}

type Product @key(fields: "sku package") @key(fields: "sku      package") @key(fields: "package sku") {
  sku: String!
  package: String!
  name: String
}

type Order @key(fields: "meta { id region }") @key(fields: "meta { region id }") {
  meta: OrderMeta!
  total: Int
}

type OrderMeta {
  id: ID!
  region: String!
}

type Query {
  products: [Product]
  _service: _Service!
  _entities(representations: [_Any!]!): [_Entity]!
}

type _Service { sdl: String! }

union _Entity = Product | Order

scalar FieldSet
scalar _Any

directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
directive @link(url: String! import: [String!]) repeatable on SCHEMA
```

## Transformed SDL

```graphql
schema {
  query: Query
}

type Query {
  fusion__lookup_orderByMetaAndIdAndRegion(
    key: OrderByMetaAndIdAndRegionInput! @is(field: "{ meta: meta.{ id, region } }")
  ): Order @internal @lookup
  fusion__lookup_productBySkuAndPackage(package: String!, sku: String!): Product
    @internal
    @lookup
  products: [Product]
}

type Order @key(fields: "meta { id region }") {
  meta: OrderMeta!
  total: Int
}

type OrderMeta {
  id: ID!
  region: String!
}

type Product @key(fields: "sku package") {
  name: String
  package: String!
  sku: String!
}

input OrderByMetaAndIdAndRegionInput {
  meta: OrderByMetaAndIdAndRegionInput_Meta
}

input OrderByMetaAndIdAndRegionInput_Meta {
  id: ID!
  region: String!
}
```
