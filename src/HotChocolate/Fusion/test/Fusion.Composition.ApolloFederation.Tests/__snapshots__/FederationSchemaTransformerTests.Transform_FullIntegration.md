# Transform_FullIntegration

## Apollo Federation SDL

```graphql
schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key", "@requires", "@provides", "@external"]) {
  query: Query
}
type Product @key(fields: "id") @key(fields: "sku package") {
  id: ID!
  sku: String!
  package: String!
  name: String
  price: Float
  weight: Float
  inStock: Boolean
  createdBy: User @provides(fields: "totalProductsCreated")
}
type User @key(fields: "id") {
  id: ID!
  username: String @external
  totalProductsCreated: Int
}
type Review {
  body: String
  author: User
}
type Query {
  product(id: ID!): Product
  reviews: [Review]
  _service: _Service!
  _entities(representations: [_Any!]!): [_Entity]!
}
type _Service { sdl: String! }
union _Entity = Product | User
scalar FieldSet
scalar _Any
directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
directive @requires(fields: FieldSet!) on FIELD_DEFINITION
directive @provides(fields: FieldSet!) on FIELD_DEFINITION
directive @external on FIELD_DEFINITION
directive @link(url: String! import: [String!]) repeatable on SCHEMA
```

## Transformed SDL

```graphql
type Product @key(fields: "id") @key(fields: "sku package") {
  id: ID!
  sku: String!
  package: String!
  name: String
  price: Float
  weight: Float
  inStock: Boolean
  createdBy: User @provides(fields: "totalProductsCreated")
}

type User @key(fields: "id") {
  id: ID!
  username: String @external
  totalProductsCreated: Int
}

type Review {
  body: String
  author: User
}

type Query {
  product(id: ID!): Product
  reviews: [Review]
  productById(id: ID!): Product @internal @lookup
  productBySkuAndPackage(sku: String! package: String!): Product @internal @lookup
  userById(id: ID!): User @internal @lookup
}
```
