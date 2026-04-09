# Transform_RequiresDirective

## Apollo Federation SDL

```graphql
schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key", "@requires", "@external"]) {
  query: Query
}
type Product @key(fields: "id") {
  id: ID!
  price: Float @external
  weight: Float @external
  shippingEstimate: Float @requires(fields: "price weight")
}
type Query {
  product(id: ID!): Product
  _service: _Service!
  _entities(representations: [_Any!]!): [_Entity]!
}
type _Service { sdl: String! }
union _Entity = Product
scalar FieldSet
scalar _Any
directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
directive @requires(fields: FieldSet!) on FIELD_DEFINITION
directive @external on FIELD_DEFINITION
directive @link(url: String! import: [String!]) repeatable on SCHEMA
```

## Transformed SDL

```graphql
schema {
  query: Query
}

type Query {
  product(id: ID!): Product
  productById(id: ID!): Product @internal @lookup
}

type Product @key(fields: "id") {
  id: ID!
  shippingEstimate(
    price: Float! @require(field: "price")
    weight: Float! @require(field: "weight")
  ): Float
}
```
