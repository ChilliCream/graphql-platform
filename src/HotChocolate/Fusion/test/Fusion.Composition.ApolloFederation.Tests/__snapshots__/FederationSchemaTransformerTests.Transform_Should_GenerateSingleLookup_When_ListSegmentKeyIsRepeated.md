# Transform_Should_GenerateSingleLookup_When_ListSegmentKeyIsRepeated

## Apollo Federation SDL

```graphql
schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"]) {
  query: Query
}

type Item @key(fields: "products { id }") @key(fields: "products { id }") {
  id: ID!
  products: [Product!]!
}

type Product {
  id: ID!
}

type Query {
  items: [Item]
  _service: _Service!
  _entities(representations: [_Any!]!): [_Entity]!
}

type _Service { sdl: String! }

union _Entity = Item

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
  fusion__lookup_itemByProductsAndId(
    key: ItemByProductsAndIdInput! @is(field: "{ products: products[{ id }] }")
  ): Item @internal @lookup
  items: [Item]
}

type Item {
  id: ID!
  products: [Product!]! @shareable
}

type Product {
  id: ID!
}

input ItemByProductsAndIdInput {
  products: [ItemByProductsAndIdInput_Products!]!
}

input ItemByProductsAndIdInput_Products {
  id: ID!
}
```
