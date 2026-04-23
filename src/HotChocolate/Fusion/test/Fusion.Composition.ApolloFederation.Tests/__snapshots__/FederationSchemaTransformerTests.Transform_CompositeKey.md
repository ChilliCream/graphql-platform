# Transform_CompositeKey

## Apollo Federation SDL

```graphql
schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"]) {
  query: Query
}

type Product @key(fields: "sku package") {
  sku: String!
  package: String!
  name: String
}

type Query {
  products: [Product]
  _service: _Service!
  _entities(representations: [_Any!]!): [_Entity]!
}

type _Service { sdl: String! }

union _Entity = Product

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
  productBySkuAndPackage(package: String!, sku: String!): Product
    @internal
    @lookup
  products: [Product]
}

type Product @key(fields: "sku package") {
  name: String
  package: String!
  sku: String!
}
```
