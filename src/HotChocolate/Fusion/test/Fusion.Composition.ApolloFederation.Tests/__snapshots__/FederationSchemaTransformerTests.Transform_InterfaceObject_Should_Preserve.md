# Transform_InterfaceObject_Should_Preserve

## Apollo Federation SDL

```graphql
schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key", "@interfaceObject"]) {
  query: Query
}

type Product @key(fields: "id") @interfaceObject {
  id: ID!
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
directive @interfaceObject on OBJECT
directive @link(url: String! import: [String!]) repeatable on SCHEMA
```

## Transformed SDL

```graphql
schema {
  query: Query
}

type Query {
  fusion__lookup_productById(id: ID!): Product @internal @lookup
  products: [Product]
}

type Product @key(fields: "id") @interfaceObject {
  id: ID!
  name: String
}

directive @interfaceObject on OBJECT
```
